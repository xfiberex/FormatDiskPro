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

    [TestDriveFact]
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

    [TestDriveFact]
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
    [DestructiveFact]
    public void FullLifecycle_FormatThenReinit_OnDedicatedTestUsb()
    {
        // Redundante con [DestructiveFact] (que ya salta el test sin opt-in), pero se mantiene como
        // segunda barrera: este es el único test que BORRA datos reales, y no debe depender de que nadie
        // se equivoque al copiar el atributo.
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

            // ── 3) Reinicializar con FAT32 pequeña, si la unidad cualifica ──
            // Cualifica cualquier extraíble donde quepa el menor de los tamaños ofrecidos (1 GB + margen);
            // antes hacía falta que llegara a 32 GB, y con la USB de pruebas este paso se omitía siempre.
            // Se elige "1 GB" precisamente porque es el único tamaño que está en el selector en TODA unidad
            // que cualifique: los demás dependen del disco.
            var smallFat32Check = Window.FindFirstDescendant(cf => cf.ByAutomationId("SmallFat32Check"));
            if (smallFat32Check is null)
            {
                output.WriteLine("SmallFat32Check no visible (la unidad de pruebas no llega a 1 GB): se omite el paso 3.");
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
            finalLetter   = MainWindowActions.GetSelectedDriveLetter(Window) ?? finalLetter;
            currentLetter = finalLetter ?? currentLetter;
            output.WriteLine("Reinicializar con FAT32 pequeña (1 GB) completado.");

            AssertDiskLayout(currentLetter, output, expectedPartitions: 1, expectUnallocated: true);

            // ── 4) Y ahora el mismo plan aprovechando el resto (`T5-02`/`T5-05`) ──
            // Es el paso que distingue la función de su versión anterior: el disco tiene que quedar SIN
            // espacio sin asignar y con dos volúmenes montados. Comprobar solo que el diálogo dice que
            // fue bien no valdría: es exactamente lo que diría si la segunda partición no se hubiera creado.
            MainWindowActions.WaitUntilEnabled(Window, "SmallFat32Check");
            MainWindowActions.SetChecked(Window, "SmallFat32Check", true);
            MainWindowActions.SelectComboText(Window, "SmallFat32SizePicker", "1 GB");
            MainWindowActions.SelectComboIndex(Window, "RestPicker", 1);   // "crear una segunda partición"
            MainWindowActions.SelectComboText(Window, "RestFsPicker", "exFAT");

            MainWindowActions.ClickMenuPath(Window, "MnuTools", "MnuReinit");
            var confirmTwo = DialogHelper.WaitForDialog(fixture);
            DialogHelper.WaitForChild(confirmTwo, "InputBox").AsTextBox().Text = currentLetter.ToString();
            DialogHelper.PrimaryButton(confirmTwo).Invoke();
            DialogHelper.WaitForNoDialog(fixture);

            var twoResult = DialogHelper.WaitForDialog(fixture, TimeSpan.FromMinutes(10));
            DialogHelper.CloseButton(twoResult).Invoke();
            DialogHelper.WaitForNoDialog(fixture);
            finalLetter   = MainWindowActions.GetSelectedDriveLetter(Window) ?? finalLetter;
            currentLetter = finalLetter ?? currentLetter;
            output.WriteLine("Reinicializar con FAT32 pequeña (1 GB) + resto en exFAT completado.");

            AssertDiskLayout(currentLetter, output, expectedPartitions: 2, expectUnallocated: false);
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

    /// <summary>
    /// Comprueba el <b>disco físico</b>, no lo que dijo el diálogo. Es la diferencia entre probar la
    /// función y probar su mensaje de éxito: si la segunda partición no se creara, el diálogo diría
    /// exactamente lo mismo.
    /// </summary>
    /// <param name="letter">Cualquier letra montada del disco a inspeccionar.</param>
    /// <param name="output">Salida de la prueba, para dejar dicho lo que se encontró.</param>
    /// <param name="expectedPartitions">Número de particiones que debe tener el disco.</param>
    /// <param name="expectUnallocated">Si debe quedar espacio sin asignar (el camino «dejarlo sin asignar»).</param>
    private static void AssertDiskLayout(char letter, ITestOutputHelper output, int expectedPartitions, bool expectUnallocated)
    {
        string script =
            "$ErrorActionPreference='Stop';" +
            $"$d = (Get-Partition -DriveLetter {letter} | Get-Disk);" +
            "$n = @(Get-Partition -DiskNumber $d.Number).Count;" +
            "\"$n|$($d.LargestFreeExtent)\"";

        string raw = RunPowerShell(script).Trim();
        string[] parts = raw.Split('|');
        Assert.True(parts.Length == 2 && int.TryParse(parts[0], out _),
            $"No se pudo leer el layout del disco de {letter}: '{raw}'");

        int partitions = int.Parse(parts[0]);
        long free = long.TryParse(parts[1], out long f) ? f : 0;
        output.WriteLine($"Layout de {letter}: {partitions} partición(es), {free / 1024 / 1024} MB sin asignar.");

        Assert.Equal(expectedPartitions, partitions);

        // Umbrales holgados a propósito: lo que se comprueba es "queda medio disco libre" frente a "no
        // queda nada aprovechable", no un número exacto de bytes que dependería de la alineación.
        const long OneGb = 1024L * 1024 * 1024;
        if (expectUnallocated)
            Assert.True(free > OneGb, $"Se esperaba espacio sin asignar y solo hay {free} bytes.");
        else
            Assert.True(free < 64L * 1024 * 1024, $"No debería quedar espacio sin asignar, y quedan {free} bytes.");
    }

    private static string RunPowerShell(string script)
    {
        var psi = new System.Diagnostics.ProcessStartInfo
        {
            FileName               = "powershell.exe",
            Arguments              = $"-NonInteractive -NoProfile -EncodedCommand {Convert.ToBase64String(System.Text.Encoding.Unicode.GetBytes(script))}",
            RedirectStandardOutput = true,
            RedirectStandardError  = true,
            UseShellExecute        = false,
            CreateNoWindow         = true,
        };

        using var proc = System.Diagnostics.Process.Start(psi)
            ?? throw new InvalidOperationException("No se pudo lanzar powershell.exe para inspeccionar el disco.");
        string stdout = proc.StandardOutput.ReadToEnd();
        proc.WaitForExit();
        return stdout;
    }
}
