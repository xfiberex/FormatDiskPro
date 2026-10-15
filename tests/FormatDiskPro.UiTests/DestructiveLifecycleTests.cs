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

    /// <summary>
    /// `T6-01`: formatear y reinicializar comparten <c>ConfirmDialog</c>, y hasta la revisión de
    /// UX/UI del 2026-08-17 compartían también el título — el diálogo fijaba «Confirmar formato» en su
    /// constructor. Reinicializar borra el disco físico ENTERO, así que anunciarlo con el nombre de la
    /// operación menos grave hace que quien lee solo el título confirme algo distinto de lo que ocurre.
    ///
    /// <para>No se comparan contra un texto fijo a propósito: este proyecto conduce el <c>.exe</c> como
    /// caja negra (sin referencia al ensamblado de la app), y anclar la cadena en español haría fallar la
    /// prueba con la app en otro idioma. Lo que se exige es lo que define el fallo: que cada operación
    /// tenga su propio título y ninguno esté vacío.</para>
    /// </summary>
    [TestDriveFact]
    public void ConfirmDialogs_EachDestructiveOperationHasItsOwnTitle()
    {
        char letter = TestDrive.RequireLetter(TestDrive.PrimaryLabel);
        SelectTestDrive(letter);

        string formatTitle;
        MainWindowActions.Button(Window, "StartButton").Invoke();
        try   { formatTitle = DialogHelper.ReadTitle(DialogHelper.WaitForDialog(fixture)); }
        finally { DialogHelper.SafeCloseAnyDialog(fixture); }

        string reinitTitle;
        MainWindowActions.ClickMenuPath(Window, "MnuTools", "MnuReinit");
        try   { reinitTitle = DialogHelper.ReadTitle(DialogHelper.WaitForDialog(fixture)); }
        finally { DialogHelper.SafeCloseAnyDialog(fixture); }

        output.WriteLine($"Título de formatear:      '{formatTitle}'");
        output.WriteLine($"Título de reinicializar:  '{reinitTitle}'");

        Assert.NotEmpty(formatTitle);
        Assert.NotEmpty(reinitTitle);
        Assert.False(formatTitle == reinitTitle,
            $"Las dos operaciones destructivas se anuncian con el mismo título ('{formatTitle}'): " +
            "reinicializar borra el disco entero y no puede presentarse con el nombre de formatear.");
    }

    /// <summary>
    /// `T13-03`: lo que `T6-01` arregló en el título seguía roto en el botón. Reinicializar se confirmaba
    /// con «Formatear», y en cuanto la letra coincide, <c>Enter</c> lo pulsa: es el verbo de lo que se
    /// ejecuta, no un adorno.
    ///
    /// <para>Mismo criterio de caja negra que la prueba del título: no se ancla el texto en español.
    /// Se exige que cada operación tenga su propio verbo y que los dos nombren la unidad, como el botón
    /// principal de la ventana (`T12-02`).</para>
    /// </summary>
    [TestDriveFact]
    public void ConfirmDialogs_EachDestructiveOperationHasItsOwnVerb_NamingTheDrive()
    {
        char letter = TestDrive.RequireLetter(TestDrive.PrimaryLabel);
        SelectTestDrive(letter);

        string formatVerb;
        MainWindowActions.Button(Window, "StartButton").Invoke();
        try   { formatVerb = DialogHelper.PrimaryButton(DialogHelper.WaitForDialog(fixture)).Name.Trim(); }
        finally { DialogHelper.SafeCloseAnyDialog(fixture); }

        string reinitVerb;
        MainWindowActions.ClickMenuPath(Window, "MnuTools", "MnuReinit");
        try   { reinitVerb = DialogHelper.PrimaryButton(DialogHelper.WaitForDialog(fixture)).Name.Trim(); }
        finally { DialogHelper.SafeCloseAnyDialog(fixture); }

        output.WriteLine($"Botón de formatear:      '{formatVerb}'");
        output.WriteLine($"Botón de reinicializar:  '{reinitVerb}'");

        Assert.False(formatVerb == reinitVerb,
            $"Formatear y reinicializar se confirman con el mismo botón ('{formatVerb}'): reinicializar borra " +
            "el disco físico entero, y quien pulsa Enter tiene que leer lo que va a pasar.");
        foreach (string verb in (string[])[formatVerb, reinitVerb])
            Assert.True(verb.Contains($"{letter}:", StringComparison.OrdinalIgnoreCase),
                $"El botón '{verb}' no nombra la unidad ({letter}:), como sí hace el de la ventana principal.");
    }

    /// <summary>
    /// `T6-02`: el campo de confirmación llevaba como <c>PlaceholderText</c> la propia letra que hay que
    /// teclear. Una letra gris dentro de una caja vacía es indistinguible de una escrita —el campo se leía
    /// como ya relleno— y además ponía la respuesta dentro del hueco donde hay que transcribirla, que es
    /// el único punto de fricción deliberada de la app.
    ///
    /// <para><b>Dónde se comprueba, y por qué ahí.</b> El primer intento buscaba un elemento del diálogo
    /// cuyo texto visible fuera justo la letra, y pasaba **igual con el fallo puesto**: WinUI no publica el
    /// placeholder como texto de ningún elemento. Lo que hace es usarlo como <b>nombre accesible</b> del
    /// propio <c>TextBox</c> cuando no hay otro — o sea que el campo se llamaba «I» y un lector de pantalla
    /// cantaba la respuesta. Por eso se comprueba el <c>Name</c>: es donde el fallo existía de verdad, y de
    /// paso cubre la mitad de accesibilidad que el arreglo visual no tocaba.</para>
    /// </summary>
    [TestDriveFact]
    public void ConfirmDialog_DoesNotEchoTheLetterInsideTheInputBox()
    {
        char letter = TestDrive.RequireLetter(TestDrive.PrimaryLabel);
        SelectTestDrive(letter);

        MainWindowActions.Button(Window, "StartButton").Invoke();
        var dialog = DialogHelper.WaitForDialog(fixture);
        try
        {
            var inputBox = DialogHelper.WaitForChild(dialog, "InputBox").AsTextBox();

            Assert.Equal("", inputBox.Text);
            Assert.False(DialogHelper.PrimaryButton(dialog).IsEnabled);

            string name = inputBox.Name;
            output.WriteLine($"Nombre accesible del campo: '{name}'");

            Assert.False(string.Equals(name.Trim(), letter.ToString(), StringComparison.OrdinalIgnoreCase),
                $"El campo de confirmación se llama '{name}': es la letra que el usuario tiene que teclear. " +
                "Visualmente hace que el campo parezca ya relleno, y un lector de pantalla anuncia la " +
                "respuesta en voz alta — justo donde la app pone su única fricción deliberada.");

            // Y que no se haya quedado sin nombre útil al quitar el placeholder: «…» tampoco sirve.
            Assert.False(string.IsNullOrWhiteSpace(name.Trim('…', '.', ' ')),
                $"El campo de confirmación no tiene nombre accesible propio (Name='{name}').");
        }
        finally
        {
            DialogHelper.SafeCloseAnyDialog(fixture);
        }
    }

    // SendMessage y no PostMessage: Windows rechaza encolar WM_DEVICECHANGE (ERROR_MESSAGE_SYNC_ONLY),
    // porque su lParam suele ser un puntero. Con tiempo límite, para no colgar la prueba si la app no atiende.
    [System.Runtime.InteropServices.DllImport("user32.dll", SetLastError = true)]
    private static extern nint SendMessageTimeout(
        IntPtr hWnd, uint msg, nuint wParam, ref DevBroadcastVolume lParam, uint flags, uint timeoutMs, out nuint result);

    /// <summary>
    /// <c>DEV_BROADCAST_VOLUME</c>. Hace falta uno de verdad: con <c>DBT_DEVICEARRIVAL</c> el <c>lParam</c>
    /// es un puntero que Windows copia al otro proceso, y con un nulo rechaza el envío (error 87).
    /// </summary>
    [System.Runtime.InteropServices.StructLayout(System.Runtime.InteropServices.LayoutKind.Sequential)]
    private struct DevBroadcastVolume
    {
        public uint Size, DeviceType, Reserved, UnitMask;
        public ushort Flags;
    }

    /// <summary>
    /// Una confirmación destructiva abierta <b>se cierra sola</b> si la lista de unidades cambia debajo
    /// de ella (`AUD-013`): lo que enseña puede haber dejado de ser cierto, y cerrarla equivale a no
    /// confirmar. Antes la lista se recargaba detrás del diálogo y este seguía ofreciendo su botón.
    ///
    /// <para>El aviso de Windows se simula con el mismo mensaje que llega al conectar o retirar un
    /// dispositivo (<c>WM_DEVICECHANGE</c> / <c>DBT_DEVICEARRIVAL</c>): no hace falta tocar ninguna
    /// unidad, y por eso la prueba no pide opt-in.</para>
    /// </summary>
    [TestDriveFact]
    public void Confirm_ClosesByItself_WhenTheDriveListChanges()
    {
        const uint WmDeviceChange = 0x0219;
        const nuint DbtDeviceArrival = 0x8000;

        char letter = TestDrive.RequireLetter(TestDrive.PrimaryLabel);
        SelectTestDrive(letter);
        MainWindowActions.SelectComboText(Window, "FileSystemPicker", "NTFS");

        MainWindowActions.Button(Window, "StartButton").Invoke();
        var dialog = DialogHelper.WaitForDialog(fixture);
        try
        {
            // Con la letra ya escrita: el botón está habilitado y a un Enter de ejecutarse.
            DialogHelper.WaitForChild(dialog, "InputBox").AsTextBox().Text = letter.ToString();
            MainWindowActions.WaitUntilEnabled(dialog, "PrimaryButton");

            IntPtr hwnd = Window.Properties.NativeWindowHandle.Value;
            var volume = new DevBroadcastVolume
            {
                Size       = (uint)System.Runtime.InteropServices.Marshal.SizeOf<DevBroadcastVolume>(),
                DeviceType = 2,                                   // DBT_DEVTYP_VOLUME
                UnitMask   = 1u << (char.ToUpperInvariant(letter) - 'A'),
            };
            nint sent = SendMessageTimeout(hwnd, WmDeviceChange, DbtDeviceArrival, ref volume,
                                           flags: 0x0002 /* SMTO_ABORTIFHUNG */, timeoutMs: 5000, out _);
            int error = System.Runtime.InteropServices.Marshal.GetLastWin32Error();
            Assert.True(sent != 0,
                $"No se pudo entregar WM_DEVICECHANGE a la ventana de la app (hwnd=0x{hwnd:X}, error Win32 {error}).");

            DialogHelper.WaitForNoDialog(fixture, TimeSpan.FromSeconds(10));

            // No se ha formateado nada: la unidad sigue con su etiqueta, y la app está ociosa.
            Assert.Equal(letter, TestDrive.FindLetter(TestDrive.PrimaryLabel));
            MainWindowActions.WaitUntilEnabled(Window, "DrivePicker");
            Assert.False(string.IsNullOrWhiteSpace(MainWindowActions.Require(Window, "StatusText").Name),
                "La confirmación se cerró sin decir por qué.");
        }
        finally
        {
            DialogHelper.SafeCloseAnyDialog(fixture);
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
            // Este paso espera UNA partición de disco entero, así que la opción de FAT32 pequeña tiene que
            // estar desmarcada de forma explícita: los [Fact] de esta clase comparten instancia de la app y
            // xUnit no garantiza su orden, así que heredarla marcada convertiría este paso en otra cosa.
            if (Window.FindFirstDescendant(cf => cf.ByAutomationId("SmallFat32Check")) is not null)
                MainWindowActions.SetChecked(Window, "SmallFat32Check", false);
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
            //
            // Se ESPERA a la casilla antes de darla por ausente. Recién reinicializado, el volumen puede
            // tardar un instante en montarse y la opción no aparece hasta que llega el tamaño del disco:
            // con una sola consulta, este paso y el siguiente se omitían en silencio y la prueba salía
            // en verde sin haber probado ninguno de los dos (visto el 2026-10-13: la USB quedó con una
            // sola partición tras una pasada «correcta»).
            var smallFat32Check = FlaUI.Core.Tools.Retry.WhileNull(
                () => Window.FindFirstDescendant(cf => cf.ByAutomationId("SmallFat32Check")),
                timeout: TimeSpan.FromSeconds(15),
                interval: TimeSpan.FromMilliseconds(250),
                ignoreException: true).Result;
            if (smallFat32Check is null)
            {
                output.WriteLine("SmallFat32Check no visible (la unidad de pruebas no llega a 1 GB): se omite el paso 3.");
                return;
            }

            MainWindowActions.WaitUntilEnabled(Window, "SmallFat32Check");
            MainWindowActions.SetChecked(Window, "SmallFat32Check", true);
            MainWindowActions.SelectComboText(Window, "SmallFat32SizePicker", "1 GB");
            // «Dejarlo sin asignar» se fija EXPLÍCITAMENTE, aunque sea el valor por defecto de fábrica:
            // es una preferencia PERSISTIDA, así que en la máquina de alguien que haya usado la app puede
            // valer lo contrario. Dar por supuesto el valor por defecto hacía que este paso creara dos
            // particiones y el aserto fallara — y el culpable no era la app, que recordó bien la elección.
            MainWindowActions.SelectComboIndex(Window, "RestPicker", 0);
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

    // ── Cancelar un formato (`AUD-001`) ────────────────────────────────────────────

    /// <summary>
    /// Cancelar un formato tiene que hacer lo que dice, por las dos vías.
    ///
    /// <para><b>Completo (exFAT)</b>: se cancela, el formato <b>se detiene de verdad</b> y la app dice que
    /// la unidad quedó sin formato. Antes iba por <c>Format-Volume</c>: la app decía «Cancelado» y Windows
    /// seguía formateando hasta el final, 46 s después. Lo que se mira es la <b>unidad</b>, no el texto:
    /// pasado más tiempo del que tarda un formato completo de esta partición, sigue sin sistema de
    /// archivos.</para>
    ///
    /// <para><b>Rápido</b>: va por <c>Format-Volume</c>, que no se puede detener, así que el botón
    /// Cancelar está deshabilitado mientras dura. Este paso es además el que devuelve la unidad a su
    /// estado: formatea desde la app un volumen que no está «listo», cosa que antes la app rechazaba.</para>
    /// </summary>
    [DestructiveFact]
    public void CancellingAFormat_DoesWhatItSays_OnBothRoutes()
    {
        TestDrive.RequireDestructiveOptIn();
        char letter = TestDrive.RequireLetter(TestDrive.PrimaryLabel);
        bool restored = false;

        try
        {
            // ── 1) Formato completo exFAT, cancelado a mitad ──
            SelectTestDrive(letter);
            MainWindowActions.SelectComboText(Window, "FileSystemPicker", "exFAT");
            MainWindowActions.TextBox(Window, "VolumeLabelBox").Text = "CANCELAR";
            MainWindowActions.SetChecked(Window, "QuickFormatCheck", false);
            MainWindowActions.SetChecked(Window, "SecureWipeCheck", false);
            ConfirmStart(letter);

            Thread.Sleep(TimeSpan.FromSeconds(4));   // que esté formateando, no arrancando
            var cancel = MainWindowActions.Button(Window, "CloseButton");
            Assert.True(cancel.IsEnabled, "Un formato completo va por format.com y se tiene que poder cancelar.");
            cancel.Invoke();
            DialogHelper.PrimaryButton(DialogHelper.WaitForDialog(fixture)).Invoke();
            DialogHelper.WaitForNoDialog(fixture);
            MainWindowActions.WaitUntilEnabled(Window, "DrivePicker", TimeSpan.FromSeconds(30));

            Assert.False(new DriveInfo(letter.ToString()).IsReady,
                "Tras cancelar, el volumen debería haber quedado sin sistema de archivos.");

            // Solo el aviso de «quedó sin formato» nombra la unidad; «Operación cancelada» a secas no.
            string status = MainWindowActions.Require(Window, "StatusText").Name;
            output.WriteLine($"Estado tras cancelar: '{status}'");
            Assert.Contains($"{letter}:", status);

            // Un formato completo de esta partición tarda unos 50 s. Si siguiera en marcha por detrás,
            // pasado ese tiempo el volumen reaparecería formateado: es lo que ocurría.
            Thread.Sleep(TimeSpan.FromSeconds(75));
            Assert.False(new DriveInfo(letter.ToString()).IsReady,
                "El volumen ha vuelto formateado: el formato siguió después de «cancelado».");

            // ── 2) Formato rápido: sin Cancelar que ofrecer, y la unidad vuelve ──
            SelectTestDrive(letter);
            MainWindowActions.SelectComboText(Window, "FileSystemPicker", "FAT32");
            MainWindowActions.TextBox(Window, "VolumeLabelBox").Text = TestDrive.PrimaryLabel;
            MainWindowActions.SetChecked(Window, "QuickFormatCheck", true);
            MainWindowActions.SetChecked(Window, "SecureWipeCheck", false);
            // El botón del pie se localiza ANTES de abrir ningún diálogo: los ContentDialog tienen un botón
            // con el mismo AutomationId («CloseButton»), y buscarlo con uno abierto podría devolver ese.
            var footerCancel = MainWindowActions.Button(Window, "CloseButton");
            ConfirmStart(letter);

            // Se muestrea mientras la app está ocupada. El formato rápido dura segundos, así que no vale
            // una sola lectura: se anota lo visto en cada una hasta que aparece el diálogo de resultado.
            int samples = 0, cancellable = 0;
            var deadline = DateTime.UtcNow + TimeSpan.FromMinutes(2);
            while (DateTime.UtcNow < deadline && !DialogHelper.IsDialogOpen(fixture))
            {
                if (!MainWindowActions.Require(Window, "DrivePicker").IsEnabled)
                {
                    samples++;
                    if (footerCancel.IsEnabled) cancellable++;
                }
                Thread.Sleep(50);
            }
            output.WriteLine($"Formato rápido: {samples} muestras con la app ocupada, {cancellable} con Cancelar habilitado.");

            var result = DialogHelper.WaitForDialog(fixture, TimeSpan.FromMinutes(2));
            DialogHelper.CloseButton(result).Invoke();
            DialogHelper.WaitForNoDialog(fixture);

            Assert.True(samples > 0, "No se llegó a ver la app ocupada durante el formato rápido.");
            Assert.Equal(0, cancellable);

            Assert.True(new DriveInfo(letter.ToString()).IsReady, "El formato rápido no dejó la unidad lista.");
            restored = true;
        }
        finally
        {
            DialogHelper.SafeCloseAnyDialog(fixture);

            // Si la prueba se cayó con la unidad sin formato, las demás no la encontrarían por etiqueta.
            // Se devuelve a FAT32 «utilidades» sin pasar por la UI: no es lo que se está probando.
            if (!restored)
                RunPowerShell($"Format-Volume -DriveLetter {letter} -FileSystem FAT32 " +
                              $"-NewFileSystemLabel '{TestDrive.PrimaryLabel}' -Confirm:$false -Force | Out-Null");
            else
                try { new DriveInfo(letter.ToString()).VolumeLabel = TestDrive.PrimaryLabel; } catch { }
        }
    }

    // ── Formatear con borrado seguro (`AUD-016`) ────────────────────────────────────

    /// <summary>
    /// El borrado seguro, por el camino de la app: tras el formato rápido arranca el borrado, que
    /// <b>escribe de verdad</b> en la unidad (aparece su directorio de trabajo), se cancela a mitad y
    /// <b>no deja nada</b>: ni el directorio ni el espacio ocupado, y el historial dice que se canceló.
    ///
    /// <para>Se cancela en vez de esperar al final porque lo que tarda un borrado completo depende del
    /// tamaño y la velocidad de la USB. Lo que se escribe en cada pasada, y que termine, lo prueban las
    /// unitarias de <c>SecureWipeTests</c> sobre una carpeta temporal; aquí se prueba que la app lo lanza
    /// contra la unidad correcta y que cancelar lo detiene y limpia.</para>
    ///
    /// <para>Formatea en FAT32 con la etiqueta de la unidad de pruebas: si todo va bien, la unidad acaba
    /// como la encuentran las demás pruebas.</para>
    /// </summary>
    [DestructiveFact]
    public void SecureWipe_AfterFormat_WritesToTheDrive_AndCancelCleansUp()
    {
        TestDrive.RequireDestructiveOptIn();
        char letter = TestDrive.RequireLetter(TestDrive.PrimaryLabel);
        string wipeDir = $"{letter}:\\__fdp_wipe__";
        string historyPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "FormatDiskPro", "history.log");

        try
        {
            SelectTestDrive(letter);
            MainWindowActions.SelectComboText(Window, "FileSystemPicker", "FAT32");
            MainWindowActions.TextBox(Window, "VolumeLabelBox").Text = TestDrive.PrimaryLabel;
            MainWindowActions.SetChecked(Window, "QuickFormatCheck", true);
            MainWindowActions.SetChecked(Window, "SecureWipeCheck", true);
            MainWindowActions.SelectComboIndex(Window, "WipePassesPicker", 0);   // 1 pasada
            // Antes de abrir ningún diálogo: los ContentDialog tienen un botón con el mismo AutomationId.
            var footerCancel = MainWindowActions.Button(Window, "CloseButton");
            ConfirmStart(letter);

            // Que el borrado esté escribiendo en la unidad, no solo que la app diga que ha empezado.
            bool writing = FlaUI.Core.Tools.Retry.WhileFalse(
                () => Directory.Exists(wipeDir) && Directory.EnumerateFiles(wipeDir).Any(),
                timeout: TimeSpan.FromMinutes(2),
                interval: TimeSpan.FromMilliseconds(250),
                ignoreException: true).Success;
            Assert.True(writing, $"El borrado seguro no llegó a escribir en {wipeDir}.");
            Thread.Sleep(TimeSpan.FromSeconds(3));

            Assert.True(footerCancel.IsEnabled, "El borrado seguro se tiene que poder cancelar.");
            footerCancel.Invoke();
            DialogHelper.PrimaryButton(DialogHelper.WaitForDialog(fixture)).Invoke();
            DialogHelper.WaitForNoDialog(fixture);
            MainWindowActions.WaitUntilEnabled(Window, "DrivePicker", TimeSpan.FromSeconds(60));

            Assert.False(Directory.Exists(wipeDir), "Cancelar el borrado dejó su directorio en la unidad.");

            var drive = new DriveInfo(letter.ToString());
            Assert.True(drive.IsReady, "Tras cancelar el borrado, la unidad (ya formateada) debería estar lista.");
            output.WriteLine($"Libre tras cancelar: {drive.AvailableFreeSpace:N0} de {drive.TotalSize:N0} bytes.");
            Assert.True(drive.AvailableFreeSpace >= drive.TotalSize * 95 / 100,
                "Tras cancelar, lo escrito por el borrado sigue ocupando la unidad.");

            string history = File.Exists(historyPath) ? File.ReadAllText(historyPath) : "";
            Assert.Contains($"WIPE CANCELLED {letter}:", history);
        }
        finally
        {
            DialogHelper.SafeCloseAnyDialog(fixture);
            // Si la app sigue ocupada la casilla está deshabilitada, y lanzar aquí taparía el fallo real.
            try { MainWindowActions.SetChecked(Window, "SecureWipeCheck", false); } catch { }

            // Si la prueba se cayó con la unidad sin formato o con otra etiqueta, las demás no la
            // encontrarían. Se devuelve a FAT32 «utilidades» sin pasar por la UI.
            if (TestDrive.FindLetter(TestDrive.PrimaryLabel) is null)
                RunPowerShell($"Format-Volume -DriveLetter {letter} -FileSystem FAT32 " +
                              $"-NewFileSystemLabel '{TestDrive.PrimaryLabel}' -Confirm:$false -Force | Out-Null");
        }
    }

    /// <summary>Pulsa el botón de formatear y confirma escribiendo la letra.</summary>
    private void ConfirmStart(char letter)
    {
        MainWindowActions.WaitUntilEnabled(Window, "StartButton", TimeSpan.FromSeconds(15));
        MainWindowActions.Button(Window, "StartButton").Invoke();
        var confirm = DialogHelper.WaitForDialog(fixture);
        DialogHelper.WaitForChild(confirm, "InputBox").AsTextBox().Text = letter.ToString();
        MainWindowActions.WaitUntilEnabled(confirm, "PrimaryButton");
        DialogHelper.PrimaryButton(confirm).Invoke();
        DialogHelper.WaitForNoDialog(fixture);
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
