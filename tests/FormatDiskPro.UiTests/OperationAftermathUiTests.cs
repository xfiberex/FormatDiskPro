// SPDX-License-Identifier: GPL-3.0-only
using System.Diagnostics;
using System.Globalization;
using FlaUI.Core.AutomationElements;
using Xunit.Abstractions;

namespace FormatDiskPro.UiTests;

/// <summary>
/// Lo que queda después de una operación, con la app real y la USB de pruebas: la confirmación de
/// «Reinicializar» nombra todo el disco (`AUD-035`), una carpeta de trabajo abandonada se ofrece borrar
/// (`AUD-038`) y una operación cancelada deja su línea en el historial (`AUD-036`).
/// Ninguna borra datos: la confirmación se cancela, y lo demás escribe solo en carpetas de la app.
/// </summary>
[Collection(AppCollection.Name)]
public sealed class OperationAftermathUiTests(AppFixture fixture, ITestOutputHelper output)
{
    private Window Window => fixture.MainWindow;

    private static string HistoryPath => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "FormatDiskPro", "history.log");

    private static string ReadHistory() => File.Exists(HistoryPath) ? File.ReadAllText(HistoryPath) : "";

    /// <summary>Ejecuta PowerShell y devuelve su salida, sin espacios al final.</summary>
    private static string PowerShell(string script)
    {
        var psi = new ProcessStartInfo("powershell.exe", $"-NoProfile -NonInteractive -Command \"{script}\"")
        {
            RedirectStandardOutput = true, UseShellExecute = false, CreateNoWindow = true,
        };
        using var p = Process.Start(psi)!;
        string text = p.StandardOutput.ReadToEnd();
        p.WaitForExit();
        return text.Trim();
    }

    /// <summary>
    /// Selecciona la unidad y recarga la lista: seleccionar la que ya lo estaba no dispara nada, y la recarga
    /// vuelve a pasar por OnDriveSelected sí o sí.
    /// </summary>
    private void Reselect(char letter)
    {
        Assert.True(MainWindowActions.SelectDriveByLetter(Window, letter),
            $"La unidad de pruebas ({letter}:) no aparece en el selector.");
        MainWindowActions.Button(Window, "RefreshButton").Invoke();
    }

    /// <summary>
    /// `AUD-035`: «Reinicializar» borra el disco físico entero. La confirmación tiene que nombrar TODAS las
    /// unidades del disco —la USB de pruebas tiene dos particiones— y su tamaño, no solo la seleccionada.
    /// Las letras y el tamaño esperados se leen del sistema, no de la app.
    /// </summary>
    [TestDriveFact]
    public void ReinitConfirm_NamesEveryDriveOnTheDisk_AndItsSize()
    {
        char letter = TestDrive.RequireLetter(TestDrive.PrimaryLabel);
        string lettersOnDisk = PowerShell(
            $"$d = Get-Partition -DriveLetter {letter} | Get-Disk; " +
            "(Get-Partition -DiskNumber $d.Number | ForEach-Object { $_.DriveLetter } | Where-Object { [char]::IsLetter($_) }) -join ''");
        long diskSize = long.Parse(PowerShell($"(Get-Partition -DriveLetter {letter} | Get-Disk).Size"), CultureInfo.InvariantCulture);
        output.WriteLine($"Disco de {letter}: {diskSize} bytes, unidades '{lettersOnDisk}'");
        Assert.True(lettersOnDisk.Length >= 1);

        Reselect(letter);
        DialogHelper.SafeCloseAnyDialog(fixture);
        MainWindowActions.ClickMenuPath(Window, "MnuTools", "MnuReinit");
        var dialog = DialogHelper.WaitForDialogContaining(fixture, "InputBox");
        try
        {
            string summary = DialogHelper.ReadText(DialogHelper.WaitForChild(dialog, "SummaryText"));
            output.WriteLine(summary);

            foreach (char l in lettersOnDisk)
                Assert.Contains($"{char.ToUpperInvariant(l)}:", summary);

            // El tamaño se escribe con el formato del idioma («29,3 GB» / «29.3 GB»): se exige la parte entera
            // en GB, que no depende del separador decimal.
            string wholeGb = ((long)(diskSize / (1024.0 * 1024 * 1024))).ToString(CultureInfo.InvariantCulture);
            Assert.Contains(wholeGb, summary);
        }
        finally
        {
            DialogHelper.SafeCloseAnyDialog(fixture);   // Cancelar: nunca se pulsa Primary
        }
    }

    /// <summary>
    /// `AUD-038`: con una carpeta <c>__fdp_verify__</c> en la unidad —lo que deja una verificación cuyo
    /// proceso murió—, la app lo dice al seleccionarla y la borra al aceptarlo.
    /// </summary>
    [TestDriveFact]
    public void LeftoverWorkFolder_IsOffered_AndDeletedOnAccept()
    {
        char letter = TestDrive.RequireLetter(TestDrive.PrimaryLabel);
        string dir = $"{letter}:\\__fdp_verify__";
        string historyBefore = ReadHistory();
        try
        {
            Directory.CreateDirectory(dir);
            File.WriteAllBytes(Path.Combine(dir, "verify_0000.bin"), new byte[64 * 1024]);

            DialogHelper.SafeCloseAnyDialog(fixture);
            Reselect(letter);

            var dialog = DialogHelper.WaitForDialog(fixture);
            // El cuerpo es un texto sin AutomationId dentro del diálogo: se leen todos los textos que contiene,
            // no solo el del diálogo (que es su título).
            string text = string.Join("\n", dialog
                .FindAllDescendants(cf => cf.ByControlType(FlaUI.Core.Definitions.ControlType.Text))
                .Select(DialogHelper.ReadText));
            output.WriteLine(text);
            Assert.Contains("__fdp_verify__", text);

            DialogHelper.PrimaryButton(dialog).Invoke();
            DialogHelper.WaitForNoDialog(fixture);

            var deadline = DateTime.UtcNow + TimeSpan.FromSeconds(10);
            while (Directory.Exists(dir) && DateTime.UtcNow < deadline) Thread.Sleep(200);
            Assert.False(Directory.Exists(dir), "La carpeta de trabajo sigue en la unidad tras aceptar borrarla.");

            string added = ReadHistory()[historyBefore.Length..];
            Assert.Contains($"WORKFOLDERS DELETED {letter}:", added);
        }
        finally
        {
            DialogHelper.SafeCloseAnyDialog(fixture);
            TestDrive.RemoveWorkFolders(letter);
        }
    }

    /// <summary>
    /// `AUD-028`: si la pregunta «¿Cancelar?» sigue abierta cuando la operación termina, el resultado no se
    /// descarta: la pregunta se cierra y el resultado se ve. Antes solo quedaba «DIALOG SKIPPED» en el
    /// historial y la persona no llegaba a saber cómo había ido.
    /// </summary>
    [TestDriveFact]
    public void ResultOfAnOperation_IsShown_WhenTheCancelQuestionWasLeftOpen()
    {
        char letter = TestDrive.RequireLetter(TestDrive.PrimaryLabel);
        TestDrive.RemoveWorkFolders(letter);
        Reselect(letter);
        DialogHelper.SafeCloseAnyDialog(fixture);
        string historyBefore = ReadHistory();

        var footerCancel = MainWindowActions.Button(Window, "CloseButton");
        try
        {
            MainWindowActions.ClickMenuPath(Window, "MnuTools", "MnuBenchmark");
            DialogHelper.PrimaryButton(DialogHelper.WaitForDialog(fixture)).Invoke();
            DialogHelper.WaitForNoDialog(fixture);

            Thread.Sleep(TimeSpan.FromSeconds(2));
            footerCancel.Invoke();
            var question = DialogHelper.WaitForDialog(fixture);
            Assert.NotNull(DialogHelper.TryFindButton(question, "PrimaryButton"));   // «Sí» / «No»: es la pregunta

            // No se contesta. Se espera a que aparezca un diálogo SIN botón primario: el resultado del
            // benchmark, que solo tiene «Cerrar».
            Window? result = null;
            var deadline = DateTime.UtcNow + TimeSpan.FromMinutes(4);
            while (result is null && DateTime.UtcNow < deadline)
            {
                Thread.Sleep(500);
                if (DialogHelper.IsDialogOpen(fixture)
                    && DialogHelper.WaitForDialog(fixture) is { } open
                    && DialogHelper.TryFindButton(open, "PrimaryButton") is null)
                    result = open;
            }
            Assert.NotNull(result);

            string added = ReadHistory()[historyBefore.Length..];
            output.WriteLine(added);
            Assert.Contains($"\tBENCH {letter}: seq", added);
            Assert.DoesNotContain("DIALOG SKIPPED", added);
        }
        finally
        {
            DialogHelper.SafeCloseAnyDialog(fixture);
            if (!MainWindowActions.Require(Window, "DrivePicker").IsEnabled)
            {
                footerCancel.Invoke();
                DialogHelper.PrimaryButton(DialogHelper.WaitForDialog(fixture)).Invoke();
                DialogHelper.SafeCloseAnyDialog(fixture);
            }
            TestDrive.RemoveWorkFolders(letter);
        }
    }

    /// <summary>
    /// `AUD-036`: cancelar un benchmark deja exactamente UNA línea que lo nombra, ningún resultado, y la
    /// unidad sin su carpeta de trabajo. Antes no quedaba ninguna línea.
    /// </summary>
    [TestDriveFact]
    public void CancellingABenchmark_LeavesExactlyOneLine_AndNoWorkFolder()
    {
        char letter = TestDrive.RequireLetter(TestDrive.PrimaryLabel);
        TestDrive.RemoveWorkFolders(letter);
        Reselect(letter);
        DialogHelper.SafeCloseAnyDialog(fixture);
        string historyBefore = ReadHistory();

        // Antes de abrir ningún diálogo: los ContentDialog tienen otro botón con el AutomationId «CloseButton».
        var footerCancel = MainWindowActions.Button(Window, "CloseButton");
        try
        {
            MainWindowActions.ClickMenuPath(Window, "MnuTools", "MnuBenchmark");
            DialogHelper.PrimaryButton(DialogHelper.WaitForDialog(fixture)).Invoke();
            DialogHelper.WaitForNoDialog(fixture);

            Thread.Sleep(TimeSpan.FromSeconds(2));   // que esté midiendo, no preparando
            Assert.False(MainWindowActions.Require(Window, "DrivePicker").IsEnabled,
                "El benchmark terminó antes de poder cancelarlo: la prueba no probaría nada.");
            footerCancel.Invoke();
            DialogHelper.PrimaryButton(DialogHelper.WaitForDialog(fixture)).Invoke();   // «¿Cancelar?» → Sí
            DialogHelper.WaitForNoDialog(fixture);
            MainWindowActions.WaitUntilEnabled(Window, "DrivePicker", TimeSpan.FromSeconds(60));

            string added = ReadHistory()[historyBefore.Length..];
            output.WriteLine(added);
            string[] benchLines = [.. added.Split('\n', StringSplitOptions.RemoveEmptyEntries)
                .Where(l => l.Contains("\tBENCH ", StringComparison.Ordinal))];
            Assert.Single(benchLines);
            Assert.Contains($"BENCH CANCELLED {letter}:", benchLines[0]);

            Assert.False(Directory.Exists($"{letter}:\\__fdp_bench__"), "El benchmark cancelado dejó su carpeta en la unidad.");
        }
        finally
        {
            DialogHelper.SafeCloseAnyDialog(fixture);
            if (!MainWindowActions.Require(Window, "DrivePicker").IsEnabled)
            {
                footerCancel.Invoke();
                DialogHelper.SafeCloseAnyDialog(fixture);
            }
            TestDrive.RemoveWorkFolders(letter);
        }
    }
}
