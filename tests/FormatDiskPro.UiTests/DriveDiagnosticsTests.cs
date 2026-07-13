using FlaUI.Core.AutomationElements;

namespace FormatDiskPro.UiTests;

/// <summary>
/// Operaciones reales pero NO destructivas contra la unidad USB de pruebas ("utilidades"): ninguna
/// de ellas borra ni sobrescribe los datos existentes del usuario en esa partición.
/// - S.M.A.R.T.: solo lectura.
/// - chkdsk (solo comprobar, sin /f): solo lectura.
/// - Benchmark: escribe/lee un archivo temporal propio y lo borra al terminar.
/// - Verificar capacidad real: escribe archivos temporales nuevos (no toca los existentes) y los
///   borra al terminar; solo falla limpiamente si no hay espacio libre suficiente.
/// Cada test envuelve toda su secuencia de diálogos en try/finally con
/// <see cref="DialogHelper.SafeCloseAnyDialog"/>: un fallo a media secuencia no debe dejar ningún
/// ContentDialog abierto (ver <see cref="MenuDialogsTests"/> para el porqué).
/// </summary>
[Collection(AppCollection.Name)]
public sealed class DriveDiagnosticsTests(AppFixture fixture)
{
    private Window Window => fixture.MainWindow;

    private void SelectTestDrive()
    {
        char letter = TestDrive.RequireLetter(TestDrive.PrimaryLabel);
        bool found = MainWindowActions.SelectDriveByLetter(Window, letter);
        Assert.True(found, $"La unidad de pruebas ({letter}:) no aparece en el selector — ¿está conectada?");
    }

    [TestDriveFact]
    public void HealthDialog_OpensForTestDrive()
    {
        SelectTestDrive();
        MainWindowActions.ClickMenuPath(Window, "MnuTools", "MnuHealth");

        var dialog = DialogHelper.WaitForDialog(fixture);
        try
        {
            // RowsPanel es un StackPanel puramente de layout: UIA lo omite de la vista de control
            // (control view) por no aportar contenido propio. NoteText sí aparece.
            DialogHelper.WaitForChild(dialog, "NoteText");
        }
        finally
        {
            DialogHelper.SafeCloseAnyDialog(fixture);
        }
    }

    [TestDriveFact]
    public void CheckDisk_ScanOnly_CompletesForTestDrive()
    {
        SelectTestDrive();
        MainWindowActions.ClickMenuPath(Window, "MnuTools", "MnuCheck");

        try
        {
            var modeDialog = DialogHelper.WaitForDialog(fixture);
            DialogHelper.PrimaryButton(modeDialog).Invoke();   // "Solo comprobar" — nunca /f
            DialogHelper.WaitForNoDialog(fixture);

            // El escaneo corre en segundo plano; el resultado llega en un segundo ContentDialog.
            DialogHelper.WaitForDialog(fixture, TimeSpan.FromMinutes(5));
        }
        finally
        {
            DialogHelper.SafeCloseAnyDialog(fixture);
        }
    }

    [TestDriveFact]
    public void Benchmark_CompletesForTestDrive()
    {
        SelectTestDrive();
        MainWindowActions.ClickMenuPath(Window, "MnuTools", "MnuBenchmark");

        try
        {
            var confirmDialog = DialogHelper.WaitForDialog(fixture);
            DialogHelper.PrimaryButton(confirmDialog).Invoke();   // confirma "Sí" para arrancar
            DialogHelper.WaitForNoDialog(fixture);

            DialogHelper.WaitForDialog(fixture, TimeSpan.FromMinutes(5));
        }
        finally
        {
            DialogHelper.SafeCloseAnyDialog(fixture);
        }
    }

    /// <summary>
    /// CapacityVerifier escribe/relee prácticamente todo el espacio libre reportado (deja solo un
    /// margen de 64 MB) — es justo lo que necesita para detectar una unidad de capacidad falsificada,
    /// pero en una unidad real de decenas de GB puede tardar mucho más de lo razonable para una corrida
    /// de suite rutinaria (confirmado contra la USB de pruebas de 62 GB: no terminó ni en 30 minutos).
    /// Por eso NO corre en el filtro por defecto — excluida vía Category=Slow. Para ejecutarla a
    /// propósito: <c>dotnet test --filter "Category=Slow"</c> (con tiempo de sobra delante).
    /// </summary>
    [TestDriveFact]
    [Trait("Category", "Slow")]
    public void VerifyCapacity_CompletesForTestDrive()
    {
        SelectTestDrive();
        MainWindowActions.ClickMenuPath(Window, "MnuTools", "MnuVerify");

        try
        {
            var confirmDialog = DialogHelper.WaitForDialog(fixture);
            DialogHelper.PrimaryButton(confirmDialog).Invoke();   // el default es "No" (defaultNo: true): hay que confirmar explícito
            DialogHelper.WaitForNoDialog(fixture);

            DialogHelper.WaitForDialog(fixture, TimeSpan.FromHours(3));
        }
        finally
        {
            DialogHelper.SafeCloseAnyDialog(fixture);
        }
    }
}
