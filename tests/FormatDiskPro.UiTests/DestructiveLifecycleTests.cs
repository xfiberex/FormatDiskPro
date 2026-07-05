using FlaUI.Core.AutomationElements;
using Xunit.Abstractions;

namespace FormatDiskPro.UiTests;

/// <summary>
/// Cubre Iniciar (formatear) y Reinicializar unidad: las dos únicas operaciones irreversibles de la
/// app. Los tests de "guarda" (Confirm_*) nunca llegan a confirmar de verdad — solo comprueban que
/// el ConfirmDialog exige escribir la letra exacta y que Cancelar no ejecuta nada; corren siempre.
/// El test de ciclo de vida completo SÍ borra datos reales y solo corre si se define
/// <see cref="TestDrive.DestructiveOptInVar"/>=1, contra la unidad USB de pruebas dedicada
/// (autorizada explícitamente por el usuario; ambas particiones vacías/desechables). Cada test
/// envuelve su(s) diálogo(s) en try/finally con <see cref="DialogHelper.SafeCloseAnyDialog"/> (ver
/// <see cref="MenuDialogsTests"/> para el porqué).
/// </summary>
[Collection(AppCollection.Name)]
public sealed class DestructiveLifecycleTests(AppFixture fixture, ITestOutputHelper output)
{
    private Window Window => fixture.MainWindow;

    private static char WrongLetterFor(char correct) => correct == 'Z' ? 'Y' : 'Z';

    private void SelectTestDrive(char letter)
    {
        bool found = MainWindowActions.SelectDriveByLetter(Window, letter);
        Assert.True(found, $"La unidad de pruebas ({letter}:) no aparece en el selector — ¿está conectada?");
    }

    // ── Guardas de ConfirmDialog (seguras, sin opt-in) ─────────────────────────────

    [Fact]
    public void StartConfirm_WrongLetterDisabled_CancelDoesNotFormat()
    {
        char letter = TestDrive.RequireLetter(TestDrive.PrimaryLabel);
        SelectTestDrive(letter);
        MainWindowActions.SelectComboText(Window, "FileSystemPicker", "NTFS");

        MainWindowActions.Button(Window, "StartButton").Invoke();
        var dialog = DialogHelper.WaitForDialog(fixture);
        try
        {
            var inputBox = DialogHelper.WaitForChild(dialog, "InputBox").AsTextBox();
            var primary = DialogHelper.PrimaryButton(dialog);

            inputBox.Text = WrongLetterFor(letter).ToString();
            Assert.False(primary.IsEnabled);

            // Este segundo cambio SÍ exige una transición real False→True del binding
            // IsPrimaryButtonEnabled, disparada por el TextChanged del InputBox — a diferencia del
            // primero (que ya arrancaba en False por defecto, así que no probaba nada async). Leer
            // primary.IsEnabled justo tras el SetValue de automatización puede adelantarse al
            // manejador del hilo de UI (misma familia de carreras que SmallFat32Check tras Reinicializar).
            inputBox.Text = letter.ToString();
            MainWindowActions.WaitUntilEnabled(dialog, "PrimaryButton");
            Assert.True(primary.IsEnabled);
        }
        finally
        {
            DialogHelper.SafeCloseAnyDialog(fixture);   // Cancelar: nunca se pulsa Primary
        }
    }

    [Fact]
    public void ReinitConfirm_WrongLetterDisabled_CancelDoesNotReinit()
    {
        char letter = TestDrive.RequireLetter(TestDrive.PrimaryLabel);
        SelectTestDrive(letter);

        MainWindowActions.ClickMenuPath(Window, "MnuTools", "MnuReinit");
        var dialog = DialogHelper.WaitForDialog(fixture);
        try
        {
            var inputBox = DialogHelper.WaitForChild(dialog, "InputBox").AsTextBox();
            var primary = DialogHelper.PrimaryButton(dialog);

            inputBox.Text = WrongLetterFor(letter).ToString();
            Assert.False(primary.IsEnabled);

            inputBox.Text = letter.ToString();
            MainWindowActions.WaitUntilEnabled(dialog, "PrimaryButton");
            Assert.True(primary.IsEnabled);
        }
        finally
        {
            DialogHelper.SafeCloseAnyDialog(fixture);   // Cancelar: nunca se pulsa Primary
        }
    }

    // ── Ciclo de vida destructivo completo (requiere opt-in explícito) ─────────────

    /// <summary>
    /// Formatea, reinicializa (normal) y reinicializa de nuevo con FAT32 pequeña (si la unidad
    /// cualifica, ≥32 GB) la unidad USB de pruebas. Un único test secuencial: el orden entre
    /// [Fact] de xUnit no está garantizado, y cada paso depende del resultado del anterior
    /// (Reinicializar puede reasignar la letra de unidad).
    /// </summary>
    [Fact]
    public void FullLifecycle_FormatThenReinit_OnDedicatedTestUsb()
    {
        TestDrive.RequireDestructiveOptIn();
        char letter = TestDrive.RequireLetter(TestDrive.PrimaryLabel);
        char? finalLetter = letter;

        try
        {
            // ── 1) Formatear (Iniciar) ──
            SelectTestDrive(letter);
            MainWindowActions.SelectComboText(Window, "FileSystemPicker", "NTFS");
            MainWindowActions.TextBox(Window, "VolumeLabelBox").Text = "UITESTFMT";
            MainWindowActions.SetChecked(Window, "QuickFormatCheck", true);
            MainWindowActions.SetChecked(Window, "SecureWipeCheck", false);

            MainWindowActions.Button(Window, "StartButton").Invoke();
            var confirmFormat = DialogHelper.WaitForDialog(fixture);
            DialogHelper.WaitForChild(confirmFormat, "InputBox").AsTextBox().Text = letter.ToString();
            DialogHelper.PrimaryButton(confirmFormat).Invoke();
            DialogHelper.WaitForNoDialog(fixture);

            var formatResult = DialogHelper.WaitForDialog(fixture, TimeSpan.FromMinutes(10));
            DialogHelper.CloseButton(formatResult).Invoke();
            DialogHelper.WaitForNoDialog(fixture);
            output.WriteLine($"Formateo de {letter}: completado.");

            // El formateo normal no reasigna letra (a diferencia de Reinicializar): sigue siendo `letter`.
            SelectTestDrive(letter);

            // ── 2) Reinicializar unidad (normal, NTFS) ──
            MainWindowActions.SelectComboText(Window, "FileSystemPicker", "NTFS");
            MainWindowActions.ClickMenuPath(Window, "MnuTools", "MnuReinit");
            var confirmReinit = DialogHelper.WaitForDialog(fixture);
            DialogHelper.WaitForChild(confirmReinit, "InputBox").AsTextBox().Text = letter.ToString();
            DialogHelper.PrimaryButton(confirmReinit).Invoke();
            DialogHelper.WaitForNoDialog(fixture);

            var reinitResult = DialogHelper.WaitForDialog(fixture, TimeSpan.FromMinutes(10));
            DialogHelper.CloseButton(reinitResult).Invoke();
            DialogHelper.WaitForNoDialog(fixture);

            // Reinicializar SÍ puede reasignar la letra: releemos la selección actual del picker
            // (la propia app ya se auto-selecciona sobre la nueva letra tras LoadDrives()).
            char currentLetter = MainWindowActions.GetSelectedDriveLetter(Window)
                ?? throw new InvalidOperationException("No hay ninguna unidad seleccionada tras Reinicializar.");
            finalLetter = currentLetter;
            output.WriteLine($"Reinicializar (normal) completado. Letra actual: {currentLetter}:");

            // ── 3) Reinicializar con FAT32 pequeña, si la unidad cualifica (removable ≥ 32 GB) ──
            var smallFat32Check = Window.FindFirstDescendant(cf => cf.ByAutomationId("SmallFat32Check"));
            if (smallFat32Check is null)
            {
                output.WriteLine("SmallFat32Check no visible (la unidad de pruebas es menor de 32 GB): se omite el paso 3.");
                return;
            }

            MainWindowActions.WaitUntilEnabled(Window, "SmallFat32Check");
            MainWindowActions.SetChecked(Window, "SmallFat32Check", true);
            MainWindowActions.SelectComboText(Window, "SmallFat32SizePicker", "1 GB");
            MainWindowActions.ClickMenuPath(Window, "MnuTools", "MnuReinit");
            var confirmSmall = DialogHelper.WaitForDialog(fixture);
            DialogHelper.WaitForChild(confirmSmall, "InputBox").AsTextBox().Text = currentLetter.ToString();
            DialogHelper.PrimaryButton(confirmSmall).Invoke();
            DialogHelper.WaitForNoDialog(fixture);

            var smallResult = DialogHelper.WaitForDialog(fixture, TimeSpan.FromMinutes(10));
            DialogHelper.CloseButton(smallResult).Invoke();
            DialogHelper.WaitForNoDialog(fixture);
            finalLetter = MainWindowActions.GetSelectedDriveLetter(Window) ?? finalLetter;
            output.WriteLine("Reinicializar con FAT32 pequeña (1 GB) completado.");
        }
        finally
        {
            DialogHelper.SafeCloseAnyDialog(fixture);

            // Este test es el único que cambia la etiqueta de la unidad de pruebas de verdad (es su
            // propio propósito). Los demás [Fact] de esta clase y de DriveDiagnosticsTests localizan
            // la unidad por TestDrive.PrimaryLabel — y xUnit NO garantiza el orden entre [Fact] de una
            // misma clase — así que, corra este test antes o después de los otros dentro del mismo
            // proceso, hay que devolver la etiqueta a la esperada para no dejarlos en flaky por orden.
            // Mejor esfuerzo directo por .NET (no por la UI): no es la operación que se está probando.
            if (finalLetter is char l)
            {
                try { new DriveInfo(l.ToString()).VolumeLabel = TestDrive.PrimaryLabel; } catch { }
            }
        }
    }
}
