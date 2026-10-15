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
            // "Solo comprobar" — nunca /f. Desde la v1.15.2 las dos acciones son botones APILADOS dentro
            // del Content, no el Primary/Secondary del ContentDialog: con tres botones en una fila,
            // "Comprobar y reparar" se truncaba. Este test siguió buscando 'PrimaryButton' hasta que la
            // USB de pruebas volvió a estar conectada y lo destapó.
            DialogHelper.WaitForChild(modeDialog, "CheckScanButton").AsButton().Invoke();
            DialogHelper.WaitForNoDialog(fixture);

            // El escaneo corre en segundo plano; el resultado llega en un segundo ContentDialog.
            var result = DialogHelper.WaitForDialog(fixture, TimeSpan.FromMinutes(5));

            // El resultado deja leer la salida completa de chkdsk (`AUD-007`): el resumen de una línea
            // es una interpretación del código de salida, y antes lo demás se descartaba. Se despliega
            // y se comprueba que lo que hay dentro es texto de chkdsk —trae el número de bytes del
            // volumen— y no un hueco.
            var expander = DialogHelper.WaitForChild(result, "CheckOutputExpander");
            expander.Patterns.ExpandCollapse.Pattern.Expand();
            var shown = FlaUI.Core.Tools.Retry.WhileNull(
                () => expander.FindAllDescendants(cf => cf.ByControlType(FlaUI.Core.Definitions.ControlType.Text))
                    .Select(t => t.Name)
                    .FirstOrDefault(t => t.Contains("bytes", StringComparison.OrdinalIgnoreCase)),
                timeout: TimeSpan.FromSeconds(5), interval: TimeSpan.FromMilliseconds(200),
                ignoreException: true).Result;
            Assert.False(string.IsNullOrWhiteSpace(shown), "El desplegable no enseña la salida de chkdsk.");
            // Los acentos llegan bien: leída en otra página de códigos, «asignación» sale como «asignaci¾n».
            Assert.DoesNotContain("¾", shown, StringComparison.Ordinal);
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
    /// Durante una operación, los tres botones de la barra rápida están apagados, como los menús que
    /// duplican (`AUD-033`). Antes seguían con aspecto de activos y no hacían nada. Se usa el benchmark
    /// porque dura lo bastante para mirar y se puede cancelar; al terminar, los botones vuelven.
    /// </summary>
    [TestDriveFact]
    public void QuickBar_IsDisabledDuringAnOperation_AndComesBackAfter()
    {
        SelectTestDrive();
        // Antes de abrir ningún diálogo: los ContentDialog tienen un botón con el mismo AutomationId.
        var footerCancel = MainWindowActions.Button(Window, "CloseButton");
        MainWindowActions.ClickMenuPath(Window, "MnuTools", "MnuBenchmark");

        try
        {
            DialogHelper.PrimaryButton(DialogHelper.WaitForDialog(fixture)).Invoke();   // «Sí», arrancar
            DialogHelper.WaitForNoDialog(fixture);

            var busy = FlaUI.Core.Tools.Retry.WhileTrue(
                () => MainWindowActions.Require(Window, "DrivePicker").IsEnabled,
                timeout: TimeSpan.FromSeconds(10), interval: TimeSpan.FromMilliseconds(100), ignoreException: true);
            Assert.True(busy.Success, "El benchmark no llegó a arrancar.");

            foreach (string id in (string[])["BtnHealth", "BtnBenchmark", "BtnHistory"])
                Assert.False(MainWindowActions.Require(Window, id).IsEnabled, $"{id} sigue activo durante la operación.");

            footerCancel.Invoke();
            DialogHelper.PrimaryButton(DialogHelper.WaitForDialog(fixture)).Invoke();   // «¿Cancelar?» → sí
            DialogHelper.WaitForNoDialog(fixture);
            MainWindowActions.WaitUntilEnabled(Window, "DrivePicker", TimeSpan.FromSeconds(60));

            DialogHelper.SafeCloseAnyDialog(fixture);
            foreach (string id in (string[])["BtnHealth", "BtnBenchmark", "BtnHistory"])
                MainWindowActions.WaitUntilEnabled(Window, id, TimeSpan.FromSeconds(10));
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
