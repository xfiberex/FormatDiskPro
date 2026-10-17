// SPDX-License-Identifier: GPL-3.0-only
using FormatDiskPro;
using Xunit;

namespace FormatDiskPro.Tests;

/// <summary>
/// Lo que queda después de una operación: la línea del historial de cada desenlace (`AUD-036`), las carpetas
/// de trabajo que una operación no llegó a borrar (`AUD-038`) y qué hay en el disco que «Reinicializar» va a
/// borrar entero (`AUD-035`).
/// </summary>
public sealed class OperationAftermathTests
{
    // ── AUD-036: cada desenlace, con su categoría y su resultado ──────────────────────────────────────

    /// <summary>
    /// Las líneas que se añadieron para los desenlaces que antes no dejaban rastro. Se escriben igual que
    /// en <c>MainWindow</c>; aquí se comprueba que el visor las clasifica bien, que es lo que hace útil la
    /// línea: una cancelación tiene que salir como cancelación, no como «Info».
    /// </summary>
    [Theory]
    [InlineData("VERIFY CANCELLED G: written=1048576",                       HistoryCategory.Verify,     HistoryResult.Cancelled)]
    [InlineData("CHKDSK CANCELLED G: repair=False",                          HistoryCategory.CheckDisk,  HistoryResult.Cancelled)]
    [InlineData("REINIT CANCELLED G: creadas=1/2",                           HistoryCategory.Reinit,     HistoryResult.Cancelled)]
    [InlineData("BENCH CANCELLED G:",                                        HistoryCategory.Benchmark,  HistoryResult.Cancelled)]
    [InlineData("BENCH FAIL G: sin espacio libre para el archivo de prueba", HistoryCategory.Benchmark,  HistoryResult.Fail)]
    [InlineData("WIPE ERROR G: IOException: dispositivo no listo",           HistoryCategory.SecureWipe, HistoryResult.Error)]
    [InlineData("FORMAT CANCELLED G: NTFS unformatted",                      HistoryCategory.Format,     HistoryResult.Cancelled)]
    [InlineData("WORKFOLDERS FOUND G: G:\\__fdp_verify__",                   HistoryCategory.App,        HistoryResult.Info)]
    [InlineData("WORKFOLDERS DELETE FAIL G: G:\\__fdp_wipe__",               HistoryCategory.App,        HistoryResult.Fail)]
    public void EveryOutcomeLine_IsReadBackWithItsCategoryAndResult(string line, HistoryCategory category, HistoryResult result)
    {
        HistoryEntry entry = HistoryEntry.Parse($"2026-10-17 12:00:00\t{line}")!;
        Assert.Equal(category, entry.Category);
        Assert.Equal(result, entry.Result);
    }

    /// <summary>
    /// Un borrado seguro cancelado o que falla deja DOS líneas, una por desenlace: el formato, que sí se
    /// hizo, y el borrado, que no. Antes solo quedaba la del borrado, o un «FORMAT ERROR» de un formato bueno.
    /// </summary>
    [Fact]
    public void FormatWithoutWipe_IsAFormatOkThatSaysTheWipeWasNotDone()
    {
        string line = FormatHistory.Success('G', "NTFS", 4096, quick: true, compress: false,
            wipeRequested: false, FormatHistory.WipeCompleted, passes: 3, "DATOS");

        Assert.Contains("wipe=False", line, StringComparison.Ordinal);
        Assert.DoesNotContain("passes=", line, StringComparison.Ordinal);
        HistoryEntry entry = HistoryEntry.Parse($"2026-10-17 12:00:00\t{line}")!;
        Assert.Equal(HistoryCategory.Format, entry.Category);
        Assert.Equal(HistoryResult.Ok, entry.Result);
    }

    // ── AUD-038: carpetas de trabajo que se quedan en la unidad ──────────────────────────────────────

    [Fact]
    public void WorkFolders_AreTheOnesTheServicesUse()
    {
        Assert.Equal(["__fdp_wipe__", "__fdp_verify__", "__fdp_bench__"], WorkFolders.All);
        Assert.Equal("G:\\__fdp_verify__", WorkFolders.On('G', WorkFolders.Verify));
    }

    [Fact]
    public void FindLeftovers_FindsOnlyTheWorkFolders_AndDeleteRemovesThem()
    {
        string root = Path.Combine(Path.GetTempPath(), "fdp-aftermath-" + Guid.NewGuid().ToString("N"));
        try
        {
            Directory.CreateDirectory(Path.Combine(root, WorkFolders.Verify));
            File.WriteAllText(Path.Combine(root, WorkFolders.Verify, "verify_0000.bin"), "x");
            Directory.CreateDirectory(Path.Combine(root, WorkFolders.Bench));
            Directory.CreateDirectory(Path.Combine(root, "Fotos"));            // de la persona: no se toca
            Directory.CreateDirectory(Path.Combine(root, "__fdp_otra__"));     // parecida, pero no es nuestra

            IReadOnlyList<string> found = WorkFolders.FindLeftovers(root);

            Assert.Equal(
                [Path.Combine(root, WorkFolders.Verify), Path.Combine(root, WorkFolders.Bench)],
                found);

            Assert.Empty(WorkFolders.Delete(found));
            Assert.Empty(WorkFolders.FindLeftovers(root));
            Assert.True(Directory.Exists(Path.Combine(root, "Fotos")));
            Assert.True(Directory.Exists(Path.Combine(root, "__fdp_otra__")));
        }
        finally
        {
            if (Directory.Exists(root)) Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public void FindLeftovers_OnARootThatDoesNotExist_FindsNothing()
        => Assert.Empty(WorkFolders.FindLeftovers(Path.Combine(Path.GetTempPath(), "no-existe-" + Guid.NewGuid())));

    [Fact]
    public void Delete_ReportsTheFolderItCouldNotDelete()
    {
        string root = Path.Combine(Path.GetTempPath(), "fdp-aftermath-" + Guid.NewGuid().ToString("N"));
        string dir  = Path.Combine(root, WorkFolders.Wipe);
        Directory.CreateDirectory(dir);
        string file = Path.Combine(dir, "wipe_0000.bin");
        try
        {
            // Un archivo abierto sin compartir: es lo que hace un antivirus que lo está escaneando.
            using (new FileStream(file, FileMode.Create, FileAccess.Write, FileShare.None))
            {
                Assert.Equal([dir], WorkFolders.Delete([dir]));
            }
            Assert.Empty(WorkFolders.Delete([dir]));
        }
        finally
        {
            if (Directory.Exists(root)) Directory.Delete(root, recursive: true);
        }
    }

    // ── AUD-035: qué hay en el disco que se va a borrar ──────────────────────────────────────────────

    [Fact]
    public void DiskContents_ParsesModelAndLetters()
    {
        DiskContents c = DiskContents.Parse("SanDisk Ultra|IG\r\n")!;
        Assert.Equal("SanDisk Ultra", c.Model);
        Assert.Equal(['G', 'I'], c.Letters);
        Assert.Equal("G:, I:", DiskContents.FormatLetters(c.Letters));
    }

    [Theory]
    [InlineData("Modelo|raro|GH", "Modelo|raro", "GH")]   // el modelo puede llevar «|»: manda el último
    [InlineData("|G",             "",            "G")]    // sin nombre: el visor dirá «modelo desconocido»
    [InlineData("USB Disk|",      "USB Disk",    "")]     // sin letras: el visor dirá que no se pudieron leer
    [InlineData("USB Disk|g\0h1", "USB Disk",    "GH")]   // solo letras, en mayúscula
    public void DiskContents_ToleratesWhatPowerShellMayWrite(string line, string model, string letters)
    {
        DiskContents c = DiskContents.Parse(line)!;
        Assert.Equal(model, c.Model);
        Assert.Equal(letters, new string([.. c.Letters]));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("sin barra")]
    public void DiskContents_WithoutTheExpectedShape_IsNull(string? line)
        => Assert.Null(DiskContents.Parse(line));

    [Fact]
    public void ReinitDiskLine_NamesTheDiskAndTheDrivesInEveryLanguage()
    {
        Assert.All(L.Map["reinit.diskLine"], t =>
        {
            Assert.Contains("{0}", t);
            Assert.Contains("{1}", t);
            Assert.Contains("{2}", t);
        });
        Assert.All(L.Map["reinit.diskLettersUnknown"], t => Assert.Contains("{0}:", t));
    }
}
