using Microsoft.UI.Composition.SystemBackdrops;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using Windows.Graphics;
using Windows.UI;
using Windows.UI.ViewManagement;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Text;

namespace FormatDiskPro.UI;

public sealed partial class MainWindow : Window
{
    // ── Static lookup tables ──────────────────────────────────────

    private static readonly Dictionary<string, (long[] Sizes, long Default)> FsDefaults = new()
    {
        ["NTFS"]  = ([512, 1024, 2048, 4096, 8192, 16384, 32768, 65536], 4096),
        ["exFAT"] = ([4096, 8192, 16384, 32768, 65536, 131072, 262144, 524288, 1048576], 131072),
        ["ReFS"]  = ([4096, 65536], 65536),
        ["FAT32"] = ([512, 1024, 2048, 4096, 8192, 16384, 32768, 65536], 4096),
        ["FAT"]   = ([512, 1024, 2048, 4096, 8192, 16384, 32768], 4096),
    };

    /// <summary>
    /// Clave de localización con la descripción de cada sistema de archivos (<c>fs.desc.*</c>).
    /// El texto vive en <see cref="L"/>, no aquí: hasta la v1.15.2 estas descripciones estaban
    /// incrustadas como dos diccionarios ES/EN, así que PT/FR/IT veían inglés.
    /// </summary>
    private static string FsDescriptionKey(string fs) => fs switch
    {
        "NTFS"  => "fs.desc.ntfs",
        "exFAT" => "fs.desc.exfat",
        "ReFS"  => "fs.desc.refs",
        "FAT32" => "fs.desc.fat32",
        "FAT"   => "fs.desc.fat",
        _       => "",
    };

    // ── State ─────────────────────────────────────────────────────

    private bool _isBusy, _cancelRequested, _isDriveProtected, _darkMode, _autoTheme = true;
    // Falla real (no cancelación) de la operación en curso: la fija cada flujo en su rama de error
    // no-cancelación; EndOperation la combina con _cancelRequested para el estado visual de la barra.
    private bool _lastOperationFailed;
    // Cierre intencional para auto-actualizarse: la app debe cerrarse (aunque _isBusy siga
    // activo por la descarga) para soltar el AppMutex y los archivos y que el instalador la reemplace.
    private bool _closingForUpdate;
    private readonly UISettings _uiSettings = new();
    private readonly AppSettings _settings = AppSettings.Load();
    // Servicios inyectados (T4-02): la ventana orquesta, no construye. Ver AppServices.
    private readonly AppServices _services;
    private char? _pendingInitialLetter;
    private IProcessHandle? _activeProcess;
    private CancellationTokenSource? _cts;
    private DateTime _opStart;
    // Umbral mínimo de duración para avisar al terminar (operaciones cortas no avisan).
    private static readonly TimeSpan OperationNotifyThreshold = TimeSpan.FromSeconds(10);
    // Seguimiento de rendimiento para operaciones con bytes (velocidad/ETA, ventana deslizante de 1 s).
    private long _opBytesDone, _opTotalBytes, _speedLastBytes;
    private DateTime _speedLastTime;
    private char _healthLetter;
    private DiskService.HealthInfo? _lastHealth;
    private DispatcherTimer _elapsedTimer = null!;
    private readonly ObservableCollection<DriveViewModel> _driveItems = new();
    private readonly List<long> _allocBytes = new();
    // Tamaño del DISCO físico de la unidad seleccionada, cuando se ha podido consultar (ver
    // LoadDiskSizeAsync). Es el tope de la partición FAT32 pequeña: Info.TotalSize mide el volumen.
    private long? _selectedDiskSizeBytes;
    private char _diskSizeLetter;
    // Tamaños ofrecidos ahora mismo en SmallFat32SizePicker, en el mismo orden que sus items.
    private readonly List<int> _smallFat32Sizes = new();
    private long _smallFat32Ceiling;
    private bool _repopulatingSizes;
    private bool _firstActivated = true;
    // Evita persistir preferencias por los eventos que disparan los controles durante la construcción.
    private bool _uiReady;
    private ElementTheme CurrentTheme => ((FrameworkElement)Content).RequestedTheme;

    // ── Constructor ───────────────────────────────────────────────

    /// <param name="services">
    /// Grafo de servicios de la app. Lo construye <see cref="App"/> (la raíz de composición) y lo pasa
    /// aquí; el valor por omisión existe solo para el diseñador de XAML y para código de prueba que no
    /// necesite sustituir nada.
    /// </param>
    public MainWindow(AppServices? services = null)
    {
        _services = services ?? new AppServices();

        InitializeComponent();

        // Window-level title bar extension: WinUI draws and themes the caption
        // (minimize/maximize/close) buttons automatically, following the content's
        // effective theme — including when the user forces Light/Dark from the menu.
        ExtendsContentIntoTitleBar = true;
        AppWindow.TitleBar.PreferredHeightOption = TitleBarHeightOption.Tall;

        Title = "FormatDiskPro";
        SetSystemBackdrop();

        // Fixed-size utility window (per design): disable resize/maximize.
        if (AppWindow.Presenter is OverlappedPresenter presenter)
        {
            presenter.IsResizable   = false;
            presenter.IsMaximizable = false;
        }
        SizeAndCenterWindow();

        _elapsedTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
        _elapsedTimer.Tick += TimerElapsed_Tick;
        // Antes de ApplyThemeMode y ApplyLanguage: los dos repintan el panel, y sin su temporizador
        // creado se caerían con NullReference en el arranque.
        InitPerformancePanel();
        AppWindow.Closing += AppWindow_Closing;

        var icoPath = Path.Combine(AppContext.BaseDirectory, "FormatDiskPro.ico");
        if (File.Exists(icoPath))
        {
            AppWindow.SetIcon(icoPath);
            TitleBarIcon.Source = new BitmapImage(new Uri(icoPath));
        }
        SetTitleBar(AppTitleBar);

        DrivePicker.ItemsSource = _driveItems;

        // El error de etiqueta aparece DEBAJO del campo, sin relación programática con él: un lector de
        // pantalla situado en VolumeLabelBox leía "Etiqueta del volumen" y nada más, así que el usuario no
        // sabía por qué no le dejaban continuar. DescribedBy es una colección y no admite x:Reference en
        // XAML de WinUI, así que se enlaza aquí, una sola vez.
        AutomationProperties.GetDescribedBy(VolumeLabelBox).Add(LabelErrorText);

        ((FrameworkElement)Content).ActualThemeChanged += OnActualThemeChanged;

        // Restaurar preferencias persistidas: idioma, tema y última unidad seleccionada.
        // (ApplyLanguage construye el menú de presets.)
        // Primer arranque (sin settings.json): sembrar el idioma desde la cultura del sistema
        // (ES/EN/PT/FR/IT, fallback ES). A partir de ahí manda la elección del usuario, ya persistida.
        if (!_settings.LoadedFromFile)
            _settings.Language = L.ToCode(L.FromCulture(CultureInfo.CurrentUICulture.Name));
        L.Set(L.FromCode(_settings.Language));
        _pendingInitialLetter = ParseDriveLetter(_settings.LastDriveLetter);
        ApplyThemeMode(_settings.Theme, save: false);
        MnuNotify.IsChecked = _settings.NotifyOnFinish;
        MnuCheckUpdatesOnStartup.IsChecked = _settings.CheckUpdatesOnStartup;
        InitWipePasses();
        ApplyLanguage();
        LoadDrives();
        HookDeviceNotifications();

        _uiReady = true;
        Activated += OnFirstActivated;
    }

    // Tamaño de diseño en DIP (píxeles independientes de la resolución). Se escala por el DPI del
    // monitor para que el contenido reciba el mismo espacio efectivo en cualquier escalado de Windows.
    private const int DesignWidthDip  = 500;
    private const int DesignHeightDip = 900;

    [System.Runtime.InteropServices.DllImport("user32.dll")]
    private static extern uint GetDpiForWindow(IntPtr hwnd);

    /// <summary>
    /// Redimensiona y centra la ventana según el DPI del monitor: convierte el tamaño de diseño (DIP)
    /// a píxeles físicos y lo acota al área de trabajo (si no cabe a lo alto, el contenido tiene scroll).
    /// Evita que en pantallas con escalado alto (p. ej. portátiles de alta densidad con la misma resolución
    /// que un monitor grande) los diálogos y el texto queden comprimidos o cortados.
    /// </summary>
    private void SizeAndCenterWindow()
    {
        var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(this);
        uint dpi = GetDpiForWindow(hwnd);
        double scale = dpi > 0 ? dpi / 96.0 : 1.0;

        var work = DisplayArea.GetFromWindowId(AppWindow.Id, DisplayAreaFallback.Nearest).WorkArea;
        int margin = (int)Math.Round(16 * scale);   // respiro respecto a los bordes del escritorio
        int w = Math.Min((int)Math.Round(DesignWidthDip  * scale), work.Width  - margin);
        int h = Math.Min((int)Math.Round(DesignHeightDip * scale), work.Height - margin);

        AppWindow.Resize(new SizeInt32(w, h));
        AppWindow.Move(new PointInt32(
            work.X + (work.Width  - w) / 2,
            work.Y + (work.Height - h) / 2));
    }

    // ── Refresco automático de unidades (#17, WM_DEVICECHANGE) ────────
    private DeviceChangeWatcher? _deviceWatcher;

    /// <summary>
    /// Vigila conexiones/desconexiones de unidades para recargar la lista. El Win32 de esto vive en
    /// <see cref="DeviceChangeWatcher"/>; aquí solo queda la decisión de qué hacer con el aviso.
    /// </summary>
    private void HookDeviceNotifications()
    {
        _deviceWatcher = new DeviceChangeWatcher(
            WinRT.Interop.WindowNative.GetWindowHandle(this),
            () => { if (!_isBusy) LoadDrives(); });   // no recargar en mitad de una operación
    }

    private void RemoveDeviceHook()
    {
        _deviceWatcher?.Dispose();
        _deviceWatcher = null;
    }

    /// <summary>Aplica Mica si el sistema lo soporta; si no, degrada a Acrylic de escritorio.</summary>
    private void SetSystemBackdrop()
    {
        if (MicaController.IsSupported())
            SystemBackdrop = new MicaBackdrop();
        else if (DesktopAcrylicController.IsSupported())
            SystemBackdrop = new DesktopAcrylicBackdrop();
    }

    private async void OnFirstActivated(object sender, WindowActivatedEventArgs e)
    {
        if (!_firstActivated) return;
        _firstActivated = false;
        Activated -= OnFirstActivated;

        // Si la configuración no se pudo leer, se apartó a un .corrupt.json en vez de dejar que el
        // primer guardado la pisara (`T9-08`). El servicio no conoce el historial, así que lo registra
        // quien sí: la persona se encuentra la app con los valores por defecto y sus presets vacíos, y
        // esta línea es lo único que después explica por qué y dónde quedó el archivo.
        if (_settings.PreservedUnreadablePath is { } preserved)
            _services.History.Log($"SETTINGS UNREADABLE: se apartó a {preserved}; se arrancó con los valores por defecto");

        await MaybeShowWhatsNewAsync();

        // La comprobación automática es opcional desde `T9-18`. Es la ÚNICA conexión a Internet de la
        // app, y hasta ahora salía en cada arranque sin preguntar y sin forma de evitarla. Quien la
        // desactiva no se queda sin actualizaciones: *Ayuda → Buscar actualizaciones…* sigue ahí.
        if (_settings.CheckUpdatesOnStartup)
            await CheckForUpdatesAsync(manual: false);
    }

    // ── Dialog helpers ────────────────────────────────────────────

    private Task ShowInfoAsync(string title, string message) =>
        ShowDialogAsync(title, message, null, null, L.T("btn.close"));

    private async Task<bool> ShowConfirmAsync(string title, string message, bool defaultNo = false)
    {
        var dlg = new ContentDialog
        {
            Title = title,
            Content = message,
            PrimaryButtonText = L.T("confirm.yes"),
            CloseButtonText = L.T("confirm.no"),
            DefaultButton = defaultNo ? ContentDialogButton.Close : ContentDialogButton.Primary,
            XamlRoot = Content.XamlRoot,
            RequestedTheme = CurrentTheme,
        };
        return await dlg.ShowAsync() == ContentDialogResult.Primary;
    }

    /// <summary>
    /// Deja la UI en estado de error tras una excepción no esperada de una operación, la registra y
    /// avisa al usuario. Comparte el tratamiento de <see cref="RunFormatAsync"/> con los flujos que
    /// no lo tenían (verificar capacidad, chkdsk, reinicializar, benchmark).
    ///
    /// <para>Existe porque esos cuatro son <c>async void</c>: sin un <c>catch</c> propio, un
    /// <see cref="IOException"/> —una USB que se desconecta a mitad, justo lo que la verificación de
    /// capacidad provoca a propósito— escapaba del handler y terminaba el proceso. El handler global
    /// de <see cref="App"/> es la red de último recurso; esto es el mensaje con contexto.</para>
    ///
    /// <para>Una cancelación NO pasa por aquí: cada flujo la trata antes, y no es un fallo.</para>
    /// </summary>
    /// <param name="operation">Etiqueta de la operación para el historial (p. ej. <c>"VERIFY"</c>).</param>
    /// <param name="letter">Letra de la unidad implicada.</param>
    /// <param name="ex">Excepción capturada.</param>
    private async Task ReportOperationErrorAsync(string operation, char letter, Exception ex)
    {
        FormatProgress.IsIndeterminate = false;
        FormatProgress.Value = 0;
        _lastOperationFailed = true;
        StatusText.Text = L.T("status.unexpected");
        _services.History.Log(OperationFailure.LogLine(operation, letter, ex));
        await ShowInfoAsync(L.T("msg.error"), $"{L.T("status.unexpected")}\n{ErrorText.Describe(ex)}");
    }

    private async Task ShowDialogAsync(string title, string message,
        string? primary, string? secondary, string close)
    {
        var dlg = new ContentDialog
        {
            Title = title,
            Content = message,
            CloseButtonText = close,
            XamlRoot = Content.XamlRoot,
            RequestedTheme = CurrentTheme,
        };
        if (primary is not null)   dlg.PrimaryButtonText   = primary;
        if (secondary is not null) dlg.SecondaryButtonText = secondary;
        await dlg.ShowAsync();
    }

    // ── Window closing ────────────────────────────────────────────

    private async void AppWindow_Closing(AppWindow sender, AppWindowClosingEventArgs args)
    {
        // Auto-actualización en curso: dejamos cerrar para que el instalador pueda reemplazar la app.
        // El muestreo del panel se para en los dos caminos que SÍ cierran: un tick contra controles ya
        // desmontados no aporta nada y es una excepción esperando a pasar.
        if (_closingForUpdate) { StopPerformanceSampling(); RemoveDeviceHook(); return; }
        if (!_isBusy) { StopPerformanceSampling(); RemoveDeviceHook(); return; }
        args.Cancel = true;
        await ShowInfoAsync(L.T("closing.title"), L.T("closing.body"));
    }

    // ── Drive loading ─────────────────────────────────────────────

    private void LoadDrives()
    {
        // En el primer arranque no hay selección previa: usamos la última unidad persistida.
        var prevLetter = (DrivePicker.SelectedItem as DriveViewModel)?.Letter ?? _pendingInitialLetter;
        _pendingInitialLetter = null;
        _driveItems.Clear();

        foreach (var d in DriveInfo.GetDrives()
            .Where(d => d.DriveType is DriveType.Fixed or DriveType.Removable or DriveType.Ram))
        {
            string label;
            try { label = d.IsReady && !string.IsNullOrEmpty(d.VolumeLabel) ? $"{d.Name.TrimEnd('\\')} ({d.VolumeLabel})" : d.Name.TrimEnd('\\'); }
            catch { label = d.Name.TrimEnd('\\'); }

            bool prot = IsSystemDrive(d.Name[0]);
            _driveItems.Add(new DriveViewModel(d.Name[0], label, d, prot, DriveBrush(prot)));
        }

        int idx = -1;
        if (prevLetter.HasValue)
            for (int i = 0; i < _driveItems.Count; i++)
                if (_driveItems[i].Letter == prevLetter.Value) { idx = i; break; }

        DrivePicker.SelectedIndex = idx >= 0 ? idx : (_driveItems.Count > 0 ? 0 : -1);
    }

    private void RefreshButton_Click(object sender, RoutedEventArgs e) => LoadDrives();

    // ── Format ────────────────────────────────────────────────────

    private async void StartButton_Click(object sender, RoutedEventArgs e)
    {
        if (DrivePicker.SelectedItem is not DriveViewModel driveItem)
        {
            await ShowInfoAsync(L.T("msg.warning"), L.T("msg.selectDrive"));
            return;
        }
        if (_isDriveProtected)
        {
            await ShowInfoAsync(L.T("msg.protTitle"), L.T("msg.protBody"));
            return;
        }
        if (FileSystemPicker.SelectedItem is null || AllocUnitPicker.SelectedIndex < 0)
        {
            await ShowInfoAsync(L.T("msg.warning"), L.T("msg.selectFsAlloc"));
            return;
        }
        if (IsSystemDrive(driveItem.Letter))
        {
            await ShowInfoAsync(L.T("msg.systemTitle"), L.T("msg.systemBody"));
            return;
        }

        string fs       = FileSystemPicker.SelectedItem.ToString()!;
        long allocBytes = GetSelectedAllocBytes();
        string label    = VolumeLabelBox.Text.Trim();
        bool quick      = QuickFormatCheck.IsChecked == true;
        bool compress   = CompressCheck.IsChecked    == true;
        bool secure     = SecureWipeCheck.IsChecked  == true;

        bool driveReady;
        try { driveReady = driveItem.Info.IsReady; } catch { driveReady = false; }
        if (!driveReady)
        {
            await ShowInfoAsync(L.T("msg.goneTitle"), L.T("msg.goneBody", driveItem.Letter));
            LoadDrives();
            return;
        }

        if (!await ValidateLabelAsync(label, fs, focusOnError: true))
            return;

        // Protección de escritura: si el disco está en solo lectura, el formateo fallaría con un error
        // poco claro. Lo detectamos y ofrecemos quitarla antes de continuar.
        if (await _services.Disk.IsDiskReadOnlyAsync(driveItem.Letter) == true)
        {
            if (!await ShowConfirmAsync(L.T("unlock.confirmTitle"), L.T("unlock.confirmBody", driveItem.Letter)))
                return;
            if (!await _services.Disk.ClearReadOnlyAsync(driveItem.Letter))
            {
                await ShowInfoAsync(L.T("unlock.confirmTitle"), L.T("unlock.failed", driveItem.Letter));
                return;
            }
        }

        int securePasses = SelectedWipePasses();
        // La opción de FAT32 pequeña solo aplica a Reinicializar unidad: si está marcada, avisar aquí
        // para que nadie formatee creyendo que obtendrá la partición pequeña.
        bool smallFat32Ignored = SmallFat32Check.Visibility == Visibility.Visible && SmallFat32Check.IsChecked == true;
        string summary =
            $"{L.T("confirm.warning")}\n\n" +
            $"  {L.T("confirm.drive")}:   {driveItem.DisplayText}\n" +
            $"  {L.T("confirm.fs")}:  {fs}\n" +
            $"  {L.T("confirm.cluster")}:  {AllocUnitPicker.SelectedItem}\n" +
            $"  {L.T("confirm.label")}: {(string.IsNullOrEmpty(label) ? L.T("confirm.nolabel") : label)}\n" +
            $"  {L.T("confirm.mode")}:     {(quick ? L.T("fmt.quick") : L.T("fmt.full"))}" +
            (secure ? $" + {L.T("confirm.secure")}" + (securePasses > 1 ? $" ×{securePasses}" : "") : "") +
            (smallFat32Ignored ? $"\n\n{L.T("confirm.smallFat32Ignored")}" : "");

        var dlg = new ConfirmDialog(driveItem.Letter, L.T("confirm.title"), summary)
            { XamlRoot = Content.XamlRoot, RequestedTheme = CurrentTheme };
        if (await dlg.ShowAsync() != ContentDialogResult.Primary) return;

        await RunFormatAsync(driveItem.Letter, fs, allocBytes, label, quick, compress, secure, securePasses);
    }

    /// <summary>
    /// Valida la etiqueta de volumen para el sistema de archivos dado: caracteres permitidos y longitud
    /// máxima por FS. Muestra el diálogo correspondiente y devuelve <c>false</c> si no es válida; una
    /// etiqueta vacía siempre es válida. Compartido por formatear (Iniciar) y reinicializar.
    /// </summary>
    private async Task<bool> ValidateLabelAsync(string label, string fs, bool focusOnError)
    {
        switch (FormatLogic.ValidateLabel(label, fs))
        {
            case FormatLogic.LabelValidation.InvalidChars:
                await ShowInfoAsync(L.T("msg.invalidTitle"), L.T("msg.invalidLabel"));
                if (focusOnError) VolumeLabelBox.Focus(FocusState.Programmatic);
                return false;
            case FormatLogic.LabelValidation.TooLong:
                await ShowInfoAsync(L.T("msg.labelLongTitle"), L.T("msg.labelLong", FormatLogic.MaxLabelLength(fs), fs));
                if (focusOnError) VolumeLabelBox.Focus(FocusState.Programmatic);
                return false;
            default:
                return true;
        }
    }

    // Hint en vivo bajo la etiqueta (mientras se escribe / al cambiar de FS): el modal de
    // ValidateLabelAsync queda como respaldo al enviar (Iniciar / Reinicializar).
    private void UpdateLabelHint()
    {
        string fs = FileSystemPicker.SelectedItem?.ToString() ?? "NTFS";
        var result = FormatLogic.ValidateLabel(VolumeLabelBox.Text, fs);
        string previous = LabelErrorText.Text;
        LabelErrorText.Text = result switch
        {
            FormatLogic.LabelValidation.InvalidChars => L.T("msg.invalidLabel"),
            FormatLogic.LabelValidation.TooLong       => L.T("msg.labelLong", FormatLogic.MaxLabelLength(fs), fs),
            _                                          => "",
        };
        LabelErrorText.Visibility = result == FormatLogic.LabelValidation.Ok ? Visibility.Collapsed : Visibility.Visible;

        // El LiveSetting="Assertive" del XAML solo dice CÓMO leerlo; el evento es lo que dispara la
        // lectura. Solo al aparecer o cambiar el mensaje: se escribe letra a letra, y repetir el mismo
        // error en cada pulsación sería insoportable con un lector de pantalla.
        if (LabelErrorText.Text.Length > 0 && LabelErrorText.Text != previous)
            RaiseLiveRegionChanged(LabelErrorText);
    }

    /// <summary>
    /// Avisa a los lectores de pantalla de que una región activa ha cambiado. Defensivo: la accesibilidad
    /// no puede tumbar la UI que describe.
    /// </summary>
    private static void RaiseLiveRegionChanged(UIElement element)
    {
        try
        {
            var peer = FrameworkElementAutomationPeer.FromElement(element)
                    ?? FrameworkElementAutomationPeer.CreatePeerForElement(element);
            peer?.RaiseAutomationEvent(AutomationEvents.LiveRegionChanged);
        }
        catch { /* ignorar */ }
    }

    private void VolumeLabelBox_TextChanged(object sender, TextChangedEventArgs e) => UpdateLabelHint();

    private async Task RunFormatAsync(
        char driveLetter, string fs, long allocBytes,
        string label, bool quickFormat, bool compress, bool secureWipe, int securePasses)
    {
        BeginOperation();
        bool useFormatCom = !quickFormat && !compress && fs is "NTFS" or "FAT32" or "FAT";

        StatusText.ClearValue(TextBlock.ForegroundProperty);
        SetStatusAndAnnounce(L.T("status.formatting", driveLetter, quickFormat ? L.T("fmt.quick") : L.T("fmt.full")));
        FormatProgress.Value   = 0;
        FormatProgress.IsIndeterminate = !useFormatCom;

        try
        {
            int code; string output;
            if (useFormatCom)
            {
                var percent = new Progress<int>(p => FormatProgress.Value = p);
                (code, output) = await _services.Format.RunComAsync(
                    driveLetter, fs, allocBytes, label, percent, p => _activeProcess = p, _cts!.Token);
            }
            else
            {
                var (c, so, se) = await _services.Format.RunVolumeAsync(
                    driveLetter, fs, allocBytes, label, quickFormat, compress, p => _activeProcess = p, _cts!.Token);
                code = c;
                output = string.IsNullOrWhiteSpace(se) ? so : se;
            }

            FormatProgress.IsIndeterminate = false;

            if (_cancelRequested)
            {
                FormatProgress.Value = 0;
                StatusText.Text = L.T("status.cancelled");
                _services.History.Log($"FORMAT CANCELLED {driveLetter}: {fs}");
                return;
            }

            if (code == 0)
            {
                FormatProgress.Value = 100;

                if (secureWipe)
                {
                    StatusText.ClearValue(TextBlock.ForegroundProperty);
                    StatusText.Text = L.T("status.wiping");
                    FormatProgress.IsIndeterminate = false;
                    FormatProgress.Value = 0;

                    var wipeProgress = new Progress<(int percent, long bytesDone, long totalBytes)>(p =>
                    {
                        FormatProgress.Value = Math.Clamp(p.percent, 0, 100);
                        _opBytesDone  = p.bytesDone;
                        _opTotalBytes = p.totalBytes;
                        StatusText.Text = L.T("status.wiping.progress", FormatBytes(p.bytesDone));
                    });

                    try
                    {
                        await _services.Wipe.RunAsync(driveLetter, securePasses, wipeProgress, _cts!.Token);
                    }
                    catch (OperationCanceledException)
                    {
                        _opTotalBytes = 0;
                        FormatProgress.Value = 0;
                        StatusText.Text = L.T("status.cancelled");
                        _services.History.Log($"WIPE CANCELLED {driveLetter}:");
                        return;
                    }

                    _opTotalBytes = 0;   // detener velocidad/ETA: el resto del flujo no maneja bytes
                    FormatProgress.Value = 100;
                    if (_cancelRequested)
                    {
                        StatusText.Text = L.T("status.cancelled");
                        _services.History.Log($"WIPE CANCELLED {driveLetter}:");
                        return;
                    }
                }

                StatusText.Text = L.T("status.success");
                _services.History.Log($"FORMAT OK {driveLetter}: fs={fs} alloc={allocBytes} quick={quickFormat} compress={compress} wipe={secureWipe}{(secureWipe ? $" passes={securePasses}" : "")} label='{label}'");
                await ShowInfoAsync(L.T("success.title"), L.T("success.body", driveLetter, fs));
                LoadDrives();
            }
            else
            {
                FormatProgress.Value = 0;
                _lastOperationFailed = true;
                StatusText.Text = L.T("status.error");
                _services.History.Log($"FORMAT FAIL {driveLetter}: fs={fs} code={code}");
                await ShowInfoAsync(L.T("error.formatTitle"), L.T("error.formatBody", driveLetter, output.Trim()));
            }
        }
        catch (OperationCanceledException)
        {
            FormatProgress.IsIndeterminate = false;
            FormatProgress.Value = 0;
            StatusText.Text = L.T("status.cancelled");
        }
        catch (Exception ex)
        {
            FormatProgress.IsIndeterminate = false;
            FormatProgress.Value = 0;
            _lastOperationFailed = true;
            StatusText.Text = _cancelRequested ? L.T("status.cancelled") : L.T("status.unexpected");
            _services.History.Log($"FORMAT ERROR {driveLetter}: {ErrorText.Describe(ex)}");
            if (!_cancelRequested)
                await ShowInfoAsync(L.T("msg.error"), $"{L.T("status.unexpected")}\n{ErrorText.Describe(ex)}");
        }
        finally
        {
            EndOperation();
        }
    }

    // ── Operation lifecycle ───────────────────────────────────────

    /// <summary>
    /// Anuncia un hito de la operación a los lectores de pantalla (UIA <c>Notification</c>).
    ///
    /// <para><b>Por qué hace falta.</b> <c>StatusText</c> y <c>FormatProgress</c> cambian durante
    /// operaciones que duran minutos u horas, pero **nada mueve el foco**: sin esto, quien usa un lector
    /// de pantalla no se entera de si el formateo avanza, ha fallado o ha terminado. El
    /// <c>LiveSetting="Polite"</c> del XAML hace que Narrador pueda leer el texto cuando cambia; la
    /// notificación es lo que garantiza que los **hitos** se anuncian aunque el usuario esté en otra
    /// parte de la ventana.</para>
    ///
    /// <para><b>Solo en los hitos</b> (inicio, fin, error, cancelación), nunca en cada tick de progreso:
    /// una notificación por porcentaje convertiría el lector de pantalla en ruido continuo durante una
    /// hora — que es peor que el silencio de partida. Por eso el progreso se deja al <i>live region</i>,
    /// que el usuario consulta cuando quiere.</para>
    ///
    /// <para><c>MostRecent</c>: si llegan dos hitos seguidos, se lee el último, no la cola entera.</para>
    /// </summary>
    /// <param name="text">Texto a anunciar; si es null, se usa el de <c>StatusText</c>.</param>
    /// <param name="kind">Tipo de hito, para que el lector pueda darle el tono que corresponda.</param>
    private void AnnounceStatus(string? text = null, AutomationNotificationKind kind = AutomationNotificationKind.Other)
    {
        string message = (text ?? StatusText.Text ?? "").Trim();
        if (message.Length == 0) return;

        try
        {
            var peer = FrameworkElementAutomationPeer.FromElement(StatusText)
                    ?? FrameworkElementAutomationPeer.CreatePeerForElement(StatusText);
            peer?.RaiseNotificationEvent(
                kind, AutomationNotificationProcessing.MostRecent, message, "FormatDiskProStatus");
        }
        catch { /* accesibilidad: nunca puede tumbar la operación que está describiendo */ }
    }

    /// <summary>Fija el estado visible y lo anuncia: es el hito de <b>inicio</b> de una operación.</summary>
    private void SetStatusAndAnnounce(string text)
    {
        StatusText.Text = text;
        AnnounceStatus(text);
    }

    private void BeginOperation()
    {
        _isBusy = true;
        BeginPerformanceTracking();
        _cancelRequested = false;
        _lastOperationFailed = false;
        FormatProgress.ShowError = false;
        _cts = new CancellationTokenSource();
        SetControlsEnabled(false);
        StatusText.ClearValue(TextBlock.ForegroundProperty);
        ElapsedText.Text = "00:00";
        CloseButton.Content = L.T("btn.cancel");
        _opStart = DateTime.Now;
        _opBytesDone = _opTotalBytes = _speedLastBytes = 0;
        _speedLastTime = _opStart;
        _elapsedTimer.Start();
    }

    private void EndOperation()
    {
        _elapsedTimer.Stop();
        EndPerformanceTracking();
        _isBusy = false;
        _activeProcess?.Dispose();
        _activeProcess = null;
        _cts?.Dispose();
        _cts = null;
        _services.Taskbar.Clear(WinRT.Interop.WindowNative.GetWindowHandle(this));
        // Si nos estamos cerrando para actualizar, la ventana ya se va: no tocar la UI.
        if (_closingForUpdate) return;
        SetControlsEnabled(true);
        CloseButton.Content = L.T("btn.close");
        // Barra en rojo (Fluent ShowError) al fallar o cancelar, hasta el próximo BeginOperation.
        FormatProgress.ShowError = _cancelRequested || _lastOperationFailed;

        // Hito de FIN, en un solo sitio: aquí pasan las cinco operaciones, terminen bien, mal o
        // canceladas. Se anuncia el estado que la operación acaba de dejar en StatusText.
        AnnounceStatus(kind: FormatProgress.ShowError
            ? AutomationNotificationKind.ActionAborted
            : AutomationNotificationKind.ActionCompleted);

        // Con la operación ya terminada, el panel deja de muestrear si el usuario lo tenía plegado.
        SyncPerformanceSampling();

        // Aviso al terminar operaciones largas: sonido + parpadeo de la barra (solo si el usuario
        // no está mirando la ventana). No aplica a operaciones cortas ni canceladas.
        if (Notifier.ShouldNotify(DateTime.Now - _opStart, _settings.NotifyOnFinish, _cancelRequested, OperationNotifyThreshold))
            _services.Notifier.OperationFinished(WinRT.Interop.WindowNative.GetWindowHandle(this));
    }

    private async void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        if (!_isBusy) { App.MainWindow?.Close(); return; }

        if (await ShowConfirmAsync(L.T("cancel.title"), L.T("cancel.body"), defaultNo: true))
        {
            _cancelRequested = true;
            try { _cts?.Cancel(); } catch { }
            try { _activeProcess?.Kill(entireProcessTree: true); } catch { }
        }
    }

    // ── Timer ─────────────────────────────────────────────────────

    private void TimerElapsed_Tick(object? sender, object e)
    {
        var now = DateTime.Now;
        string text = (now - _opStart).ToString(@"mm\:ss");

        // Operaciones con bytes (verificación / borrado seguro): añadir velocidad y ETA.
        // Velocidad instantánea por ventana deslizante (delta de bytes entre ticks de 1 s),
        // robusta frente a operaciones por fases (un delta negativo simplemente omite ese tick).
        if (_opTotalBytes > 0)
        {
            double dt = (now - _speedLastTime).TotalSeconds;
            long db = _opBytesDone - _speedLastBytes;
            // Un tick sin avance deja el caudal en 0 a propósito: el panel de rendimiento lo pinta, y
            // una barra que se cae es exactamente lo que hay que ver cuando la operación se atasca.
            _diskBytesPerSec = 0;
            if (dt > 0 && db > 0)
            {
                double speed = db / dt;
                _diskBytesPerSec = speed;
                var eta = Throughput.Eta(Math.Max(0, _opTotalBytes - _opBytesDone), speed);
                string spd = Throughput.FormatSpeed(speed);
                string etaStr = Throughput.FormatEta(eta);
                if (spd.Length > 0)    text += $"  ·  {spd}";
                if (etaStr.Length > 0) text += $"  ·  ETA {etaStr}";
            }
            _speedLastBytes = _opBytesDone;
            _speedLastTime  = now;
        }

        ElapsedText.Text = text;

        // Espeja el estado de FormatProgress en el icono de la barra de tareas (visible con la app
        // minimizada), a la misma cadencia de 1 s del cronómetro — complementa el aviso al terminar.
        var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(this);
        if (FormatProgress.IsIndeterminate) _services.Taskbar.SetIndeterminate(hwnd);
        else                                _services.Taskbar.SetValue(hwnd, (int)FormatProgress.Value);
    }

    // ── Helpers ───────────────────────────────────────────────────

    // La comparación va por DriveLetter.Same (invariante de cultura) y NO por char.ToUpper: esta es la
    // guarda que impide formatear el disco de Windows, y con cultura turca ToUpper('i') no da 'I'.
    private static bool IsSystemDrive(char letter)
    {
        char sys = Path.GetPathRoot(Environment.SystemDirectory)![0];
        return DriveLetter.Same(letter, sys);
    }

    /// <summary>
    /// Habilita o deshabilita los controles de la ventana según si hay una operación en curso. Los de
    /// formato quedan además sujetos a la protección de la unidad: sobre una unidad protegida siguen
    /// deshabilitados aunque no haya nada corriendo.
    /// </summary>
    private void SetControlsEnabled(bool enabled)
    {
        bool canFormat = enabled && !_isDriveProtected;

        MnuTools.IsEnabled   = enabled;
        MnuConfig.IsEnabled  = enabled;
        MnuHelp.IsEnabled    = enabled;
        DrivePicker.IsEnabled   = enabled;
        RefreshButton.IsEnabled = enabled;
        FileSystemPicker.IsEnabled  = canFormat;
        AllocUnitPicker.IsEnabled   = canFormat;
        VolumeLabelBox.IsEnabled    = canFormat;
        RestoreButton.IsEnabled     = canFormat;
        StartButton.IsEnabled       = canFormat;
        QuickFormatCheck.IsEnabled  = canFormat;
        SecureWipeCheck.IsEnabled   = canFormat;
        CompressCheck.IsEnabled     = canFormat && FileSystemPicker.SelectedItem?.ToString() == "NTFS";
        SmallFat32Check.IsEnabled   = canFormat;
        // El detalle del sobrante se apaga como bloque, igual que los otros tres (T6-08): sus dos
        // etiquetas no se atenúan solas por estar dentro de un panel deshabilitado.
        SetSubOptionEnabled(canFormat, [RestFsLbl, RestLabelLbl], RestFsPicker, RestLabelBox);
        UpdateWipePassesEnabled();
        UpdateSmallFat32SizeEnabled();   // gobierna también RestPicker (ver UpdateSmallFat32SizeEnabled)
        // El menú vuelve habilitado tras una operación, pero cada ítem manda sobre sí mismo: lo que la
        // unidad seleccionada no admite tiene que seguir apagado (T7-02).
        if (enabled) UpdateToolsMenuAvailability();
    }

    private long GetSelectedAllocBytes() =>
        AllocUnitPicker.SelectedIndex >= 0 && AllocUnitPicker.SelectedIndex < _allocBytes.Count
            ? _allocBytes[AllocUnitPicker.SelectedIndex]
            : 4096;

    private static string FormatBytes(long bytes) => FormatLogic.FormatBytes(bytes);

    private static string DriveTypeName(DriveType t) => t switch
    {
        DriveType.Fixed     => L.T("type.fixed"),
        DriveType.Removable => L.T("type.removable"),
        DriveType.Ram       => L.T("type.ram"),
        DriveType.Network   => L.T("type.network"),
        DriveType.CDRom     => L.T("type.cdrom"),
        _                   => L.T("type.unknown"),
    };
}
