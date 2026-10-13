using Xunit;

namespace FormatDiskPro.Tests;

/// <summary>
/// Operaciones que decían algo distinto de lo que había pasado (`AUD-006` a `AUD-009`, `AUD-011`).
///
/// <para>Las cinco tienen la misma forma: el proceso de debajo responde con algo que la app no miraba
/// —un código de salida, una segunda pregunta, un volumen que sigue montado— y la interfaz contaba el
/// caso feliz. Aquí se fija cada respuesta con lo que de verdad devuelven las herramientas, capturado
/// en hardware.</para>
/// </summary>
public sealed class OperationOutcomeTests
{
    // ── AUD-011: el error de PowerShell ───────────────────────────

    /// <summary>
    /// Lo que PowerShell escribió por su salida de error en un <c>Format-Volume</c> fallido, tal como
    /// lo capturó la auditoría (<c>evidencias/format-volume-fallido-stderr.txt</c>, recortado a los
    /// nodos que importan).
    /// </summary>
    private const string RealClixml =
        "#< CLIXML\r\n" +
        "<Objs Version=\"1.1.0.1\" xmlns=\"http://schemas.microsoft.com/powershell/2004/04\">" +
        "<Obj S=\"progress\" RefId=\"0\"><TN RefId=\"0\"><T>System.Management.Automation.PSCustomObject</T><T>System.Object</T></TN>" +
        "<MS><I64 N=\"SourceId\">1</I64><PR N=\"Record\"><AV>Preparando módulos para el primer uso.</AV><AI>0</AI><Nil /><PI>-1</PI><PC>-1</PC><T>Completed</T><SR>-1</SR><SD> </SD></PR></MS></Obj>" +
        "<S S=\"Error\">Format-Volume : No se encontraron objetos MSFT_Volume cuya propiedad 'DriveLetter' sea igual a 'Q'. Compruebe el valor _x000D__x000A_</S>" +
        "<S S=\"Error\">de la propiedad e inténtelo de nuevo._x000D__x000A_</S>" +
        "<S S=\"Error\">En línea: 1 Carácter: 1_x000D__x000A_</S>" +
        "<S S=\"Error\">+ Format-Volume -DriveLetter Q -FileSystem exFAT -AllocationUnitSize 13 ..._x000D__x000A_</S>" +
        "<S S=\"Error\">+ ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~_x000D__x000A_</S>" +
        "<S S=\"Error\">    + CategoryInfo          : ObjectNotFound: (Q:Char) [Format-Volume], CimJobException_x000D__x000A_</S>" +
        "<S S=\"Error\">    + FullyQualifiedErrorId : CmdletizationQuery_NotFound_DriveLetter,Format-Volume_x000D__x000A_</S>" +
        "<S S=\"Error\"> _x000D__x000A_</S></Objs>";

    [Fact]
    public void PowerShellError_RealFailure_BecomesTheMessageAndNothingElse()
    {
        string clean = PowerShellError.Clean(RealClixml);

        Assert.Equal(
            "Format-Volume : No se encontraron objetos MSFT_Volume cuya propiedad 'DriveLetter' sea igual a 'Q'. " +
            "Compruebe el valor de la propiedad e inténtelo de nuevo.",
            clean);
    }

    /// <summary>Lo que sobra es lo que describe el script interno de la app, en cualquier idioma.</summary>
    [Theory]
    [InlineData("<S S=\"Error\">Set-Disk : Access denied_x000D__x000A_</S><S S=\"Error\">At line:1 char:1_x000D__x000A_</S><S S=\"Error\">+ Set-Disk -IsReadOnly $false_x000D__x000A_</S>", "Set-Disk : Access denied")]
    [InlineData("<S S=\"Error\">Clear-Disk : Zugriff verweigert_x000D__x000A_</S><S S=\"Error\">In Zeile:1 Zeichen:1_x000D__x000A_</S><S S=\"Error\">+ Clear-Disk_x000D__x000A_</S>", "Clear-Disk : Zugriff verweigert")]
    [InlineData("<S S=\"Error\">El valor &lt;3&gt; no vale &amp; punto_x000D__x000A_</S>", "El valor <3> no vale & punto")]
    public void PowerShellError_DropsThePositionAndUnescapes(string nodes, string expected)
        => Assert.Equal(expected, PowerShellError.Clean("#< CLIXML\r\n<Objs>" + nodes + "</Objs>"));

    /// <summary>
    /// Un mensaje feo es mejor que uno vacío: lo que no es CLIXML, o no trae ningún error dentro, se
    /// devuelve como llegó.
    /// </summary>
    [Theory]
    [InlineData("Format-Volume : El acceso al dispositivo está denegado.", "Format-Volume : El acceso al dispositivo está denegado.")]
    [InlineData("  texto con espacios  ", "texto con espacios")]
    [InlineData("#< CLIXML\r\n<Objs><Obj S=\"progress\" /></Objs>", "#< CLIXML\r\n<Objs><Obj S=\"progress\" /></Objs>")]
    [InlineData("", "")]
    [InlineData(null, "")]
    public void PowerShellError_WhatItDoesNotRecognise_IsLeftAlone(string? stderr, string expected)
        => Assert.Equal(expected, PowerShellError.Clean(stderr));

    /// <summary>Los dos servicios que enseñan ese error lo entregan ya limpio.</summary>
    [Fact]
    public async Task FormatVolume_Failure_HandsBackTheMessageAndNotTheXml()
    {
        var format = new FormatProcess(FakeProcessRunner.Returning("", exitCode: 1, stderr: RealClixml));

        var (_, _, stderr) = await format.RunVolumeAsync(
            'Q', "exFAT", 131072, "", quickFormat: true, compress: false, null, CancellationToken.None);

        Assert.StartsWith("Format-Volume : No se encontraron objetos", stderr, StringComparison.Ordinal);
        Assert.DoesNotContain("<", stderr, StringComparison.Ordinal);
        Assert.DoesNotContain("_x000D_", stderr, StringComparison.Ordinal);
    }

    // ── AUD-006 y AUD-007: chkdsk ─────────────────────────────────

    /// <summary>Salida real de <c>chkdsk D:</c> sobre la USB de pruebas, sana (2026-10-13).</summary>
    private const string RealCleanScan =
        "El tipo del sistema de archivos es FAT32.\r\n" +
        "El volumen especificado no admite la creación de instantáneas.\r\n\r\n" +
        "Windows está comprobando archivos y carpetas...\r\n" +
        "0 por ciento completado.                  \r33 por ciento completado..                 \r" +
        "66 por ciento completado...                \r100 por ciento completado....               \r" +
        "Comprobación de archivo y carpeta completa.\r\n\r\n\r\n" +
        "Se examinó el sistema de archivos sin encontrar problemas.\r\n";

    /// <summary>Salida real de <c>chkdsk D: /f</c> con un archivo abierto en la unidad.</summary>
    private const string RealVolumeInUse =
        "El tipo del sistema de archivos es FAT32.\r\n\r\n" +
        "Chkdsk no puede ejecutarse ya que el volumen está siendo usado por otro\r\n" +
        "proceso. Chkdsk puede ejecutarse si se desmonta previamente este volumen.\r\n" +
        "¿Desea exigir el desmontaje de este volumen? (S/N) N\r\n";

    /// <summary>
    /// El código 3 significa dos cosas opuestas, y lo que las separa es si <c>chkdsk</c> llegó a
    /// examinar el volumen. En solo lectura y tras examinar, 3 es «hay errores» —no se corrigieron
    /// porque no se pidió—; decir «no se pudo comprobar (¿está en uso?)» era dar por ocupada una unidad
    /// dañada.
    /// </summary>
    [Theory]
    [InlineData(3, false, true,  CheckResult.Errors)]   // examinó, no reparó: errores
    [InlineData(3, false, false, CheckResult.Failed)]   // ni empezó
    [InlineData(3, true,  true,  CheckResult.Failed)]   // con /f, 3 es «no pude corregir»
    [InlineData(3, true,  false, CheckResult.Failed)]
    public void CheckDisk_ExitCodeThree_DependsOnWhetherTheScanRan(int code, bool repair, bool scanRan, CheckResult expected)
        => Assert.Equal(expected, CheckDisk.Interpret(code, repair, scanRan));

    [Fact]
    public void CheckDiskOutput_ScanRan_IsToldByTheProgressItReported()
    {
        Assert.True(CheckDiskOutput.ScanRan(RealCleanScan));
        Assert.False(CheckDiskOutput.ScanRan(RealVolumeInUse));
        Assert.False(CheckDiskOutput.ScanRan(""));
        Assert.False(CheckDiskOutput.ScanRan(null));
    }

    /// <summary>
    /// La salida se enseña como se vería en una consola: el progreso reescribe la misma línea con
    /// retornos de carro, y capturado son todos los porcentajes pegados.
    /// </summary>
    [Fact]
    public void CheckDiskOutput_Clean_KeepsTheLastRewriteOfEachLine()
    {
        string clean = CheckDiskOutput.Clean(RealCleanScan);

        Assert.Equal(
            "El tipo del sistema de archivos es FAT32.\n" +
            "El volumen especificado no admite la creación de instantáneas.\n\n" +
            "Windows está comprobando archivos y carpetas...\n" +
            "Comprobación de archivo y carpeta completa.\n\n" +
            "Se examinó el sistema de archivos sin encontrar problemas.",
            clean.Replace("\r\n", "\n"));
        Assert.Equal("", CheckDiskOutput.Clean(null));
        Assert.Equal("", CheckDiskOutput.Clean("\r\n\r\n"));
    }

    /// <summary>
    /// Con el volumen en uso <c>chkdsk /f</c> hace <b>dos</b> preguntas. Se responde a la primera y se
    /// cierra la entrada: con una «N» y la entrada abierta, la segunda se quedaba esperando para siempre
    /// y la app con ella. Medido: así no terminó en 30 s; con la entrada cerrada sale en 0,2 s.
    /// </summary>
    [Fact]
    public async Task CheckDisk_AnswersOnce_AndClosesItsInput()
    {
        var runner = FakeProcessRunner.Returning(RealVolumeInUse, exitCode: 3);

        var (code, output) = await new CheckDisk(runner).RunAsync('G', repair: true, new Progress<int>(), CancellationToken.None);

        var handle = Assert.Single(runner.Handles);
        Assert.True(handle.InputClosed, "La entrada estándar de chkdsk quedó abierta: se colgaría en la segunda pregunta.");
        Assert.Equal("N", handle.InputWritten.Trim());
        Assert.Equal(3, code);
        Assert.Equal(CheckResult.Failed, CheckDisk.Interpret(code, repair: true, CheckDiskOutput.ScanRan(output)));
    }

    /// <summary>La salida se lee en la página de códigos en la que <c>chkdsk</c> la escribe.</summary>
    [Fact]
    public async Task CheckDisk_ReadsItsOutputInTheAnsiCodePage()
    {
        var runner = FakeProcessRunner.Returning("", exitCode: 0);

        await new CheckDisk(runner).RunAsync('G', repair: false, new Progress<int>(), CancellationToken.None);

        Assert.Same(SystemTools.AnsiEncoding, Assert.Single(runner.Started).OutputEncoding);
    }

    // ── AUD-008: Expulsar ─────────────────────────────────────────

    /// <summary>
    /// El verbo «Eject» termina con código 0 también cuando Windows se niega a expulsar porque hay un
    /// archivo abierto. Lo que decide es si el volumen desaparece.
    /// </summary>
    [Fact]
    public async Task Eject_VolumeStillMounted_IsNotReportedAsEjected()
    {
        var disk = new DiskService(
            FakeProcessRunner.Returning("", exitCode: 0),
            isMounted: _ => true,
            ejectWait: TimeSpan.FromMilliseconds(600));

        Assert.False(await disk.EjectAsync('G'));
    }

    /// <summary>Expulsar tarda un instante: se espera a que el volumen se vaya antes de responder.</summary>
    [Fact]
    public async Task Eject_VolumeGoesAwayAfterAMoment_IsEjected()
    {
        int asked = 0;
        var disk = new DiskService(
            FakeProcessRunner.Returning("", exitCode: 0),
            isMounted: _ => ++asked < 3,
            ejectWait: TimeSpan.FromSeconds(10));

        Assert.True(await disk.EjectAsync('G'));
        Assert.Equal(3, asked);
    }

    /// <summary>Si el script ya falla, no hay nada que esperar.</summary>
    [Fact]
    public async Task Eject_ScriptFails_IsAFailureWithoutWaiting()
    {
        int asked = 0;
        var disk = new DiskService(
            FakeProcessRunner.Returning("", exitCode: 1),
            isMounted: _ => { asked++; return true; },
            ejectWait: TimeSpan.FromSeconds(30));

        Assert.False(await disk.EjectAsync('G'));
        Assert.Equal(0, asked);
    }

    // ── AUD-009: el borrado seguro ────────────────────────────────

    /// <summary>
    /// El historial dice <c>wipe=True</c> solo si el borrado se <b>completó</b>. El servicio devuelve
    /// <c>-1</c> cuando la unidad no estaba lista y no sobrescribe nada; antes eso se registraba —y se
    /// anunciaba— como hecho.
    /// </summary>
    [Theory]
    [InlineData(true,  0,  true)]
    [InlineData(true,  -1, false)]
    [InlineData(true,  5,  false)]
    [InlineData(false, 0,  false)]
    [InlineData(false, -1, false)]
    public void FormatHistory_SaysWipedOnlyWhenTheWipeCompleted(bool requested, int wipeCode, bool wiped)
    {
        string line = FormatHistory.Success('G', "NTFS", 4096, quick: true, compress: false,
            requested, wipeCode, passes: 3, "DATOS");

        Assert.Equal(wiped, FormatHistory.WipeDone(requested, wipeCode));
        Assert.Contains($"wipe={wiped}", line, StringComparison.Ordinal);
        Assert.Equal(wiped, line.Contains("passes=3", StringComparison.Ordinal));
        Assert.StartsWith("FORMAT OK G: fs=NTFS alloc=4096 quick=True compress=False", line, StringComparison.Ordinal);
        Assert.EndsWith("label='DATOS'", line, StringComparison.Ordinal);
    }

    /// <summary>El borrado que no se hizo deja su propia línea, y el visor la lee como un fallo.</summary>
    [Fact]
    public void FormatHistory_WipeFailed_IsReadBackAsAFailedSecureWipe()
    {
        string line = FormatHistory.WipeFailed('G', -1);

        Assert.Equal("WIPE FAIL G: code=-1", line);
        HistoryEntry entry = HistoryEntry.Parse($"2026-10-13 12:00:00\t{line}")!;
        Assert.Equal(HistoryCategory.SecureWipe, entry.Category);
        Assert.Equal(HistoryResult.Fail, entry.Result);
    }
}
