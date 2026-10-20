// SPDX-License-Identifier: GPL-3.0-only
using Microsoft.UI.Composition.SystemBackdrops;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using Windows.Graphics;
using Windows.UI.ViewManagement;
using System.Collections.ObjectModel;
using System.Globalization;

namespace FormatDiskPro.UI;

public sealed partial class MainWindow : Window
{
    // ── State ─────────────────────────────────────────────────────

    /// <summary>
    /// Si <c>InitializeComponent</c> ya terminó, es decir, si los campos que genera el XAML existen.
    /// </summary>
    /// <remarks>
    /// <para><b>Trampa de WinUI, y costó un arranque en negro.</b> Asignar <c>IsChecked="True"</c> en el
    /// XAML de un <c>CheckBox</c> que además declara <c>Checked="…"</c> <b>dispara el manejador durante
    /// el propio parseo</b>, cuando los controles que vienen más abajo en el archivo todavía no se han
    /// creado. El manejador tocaba el pie de la ventana, que se declara después, y el
    /// <c>NullReferenceException</c> salía envuelto como <c>XamlParseException: Failed to assign to
    /// property 'ToggleButton.IsChecked'</c> — un mensaje que señala al atributo y no a la causa.</para>
    ///
    /// <para>La alternativa era enganchar los eventos en código tras <c>InitializeComponent</c>, pero eso
    /// esconde el cableado justo donde se espera verlo. Una guarda explícita lo deja dicho: durante el
    /// parseo no hay ventana que repintar, y el primer repintado real lo hace el constructor.</para>
    ///
    /// <para>No confundir con <c>_uiReady</c>, que marca el final del constructor entero y sirve para no
    /// persistir preferencias mientras se restauran.</para>
    /// </remarks>
    private bool _uiBuilt;

    private bool _isBusy, _cancelRequested, _isDriveProtected, _darkMode, _autoTheme = true;
    // Falla real (no cancelación) de la operación en curso: la fija cada flujo en su rama de error
    // no-cancelación; EndOperation la combina con _cancelRequested para el estado visual de la barra.
    private bool _lastOperationFailed;
    // ¿Se puede cancelar el paso que está corriendo? Un formato por Format-Volume no (`AUD-001`): matar
    // su PowerShell no lo detiene. Mientras valga false, el botón Cancelar está deshabilitado.
    private bool _cancelAvailable = true;
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
    /// <summary>Tamaños de clúster que ofrece el selector, en bytes y en su orden.</summary>
    private readonly List<long> _allocBytes = new();

    /// <summary>Índice del tamaño recomendado para el sistema de archivos elegido.</summary>
    private int _allocRecommended;
    // Tamaño del DISCO físico de la unidad seleccionada, cuando se ha podido consultar (ver
    // LoadDiskSizeAsync). Es el tope de la partición FAT32 pequeña: Info.TotalSize mide el volumen.
    private long? _selectedDiskSizeBytes;
    private char _diskSizeLetter;
    // Tamaños ofrecidos ahora mismo en SmallFat32SizePicker, en el mismo orden que sus items.
    private readonly List<int> _smallFat32Sizes = new();
    private long _smallFat32Ceiling;
    private bool _repopulatingSizes;
    private bool _firstActivated = true;

    /// <summary>
    /// <see cref="LoadDrives"/> está vaciando y rellenando la lista: los cambios de selección que eso
    /// provoca no son una elección de nadie y no deben restablecer las opciones (`AUD-029`).
    /// </summary>
    private bool _reloadingDrives;
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
        // A partir de aquí los campos que genera el XAML existen todos. Antes NO: ver _uiBuilt.
        _uiBuilt = true;

        // La ventana dibuja su propia barra de título. Los botones de caption (minimizar, maximizar,
        // cerrar) los sigue pintando WinUI, pero por omisión con el tema del SISTEMA, no con el de la
        // app: quien elige el tema efectivo es `ApplyTitleBarTheme` con `PreferredTheme`.
        ExtendsContentIntoTitleBar = true;
        // Altura estándar, no Tall (`T13-10`): Microsoft recomienda la alta para barras con contenido
        // interactivo, y esta solo lleva el icono y el título. Son 16 DIP de los ~150 que se recuperan
        // para que «Opciones de formato» quepa en una pantalla baja.
        AppWindow.TitleBar.PreferredHeightOption = TitleBarHeightOption.Standard;

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

        // Y lo mismo con las pistas de los demás campos (`T13-08`). Están debajo de su control y en gris:
        // quien no las ve no sabía siquiera que existen. La de `AllocUnitPicker` es la que `T7-03` añadió
        // porque era «el único campo esotérico sin ayuda» — y hasta ahora solo ayudaba a quien ve.
        // Las dos de reinicializar viven ocultas hasta que aplican; describir un elemento Collapsed no
        // molesta, porque ni siquiera está en el árbol de automatización mientras lo está.
        // La fila de estado del pie se muestra sola cuando hay algo que decir, y se retira cuando el
        // texto se vacía (`T13-10`). Va por callback y no por cada asignación de StatusText.Text —hay
        // más de treinta repartidas por cinco archivos— para que no pueda quedarse una sin enterarse.
        StatusText.RegisterPropertyChangedCallback(TextBlock.TextProperty, (_, _) => UpdateStatusRowVisibility());
        UpdateStatusRowVisibility();

        AutomationProperties.GetDescribedBy(FileSystemPicker).Add(FsDescText);
        AutomationProperties.GetDescribedBy(AllocUnitPicker).Add(AllocHintText);
        AutomationProperties.GetDescribedBy(SmallFat32Check).Add(SmallFat32HintText);
        AutomationProperties.GetDescribedBy(RestPicker).Add(RestNoteText);
        // El selector de unidad toma su nombre del título de su tarjeta («Unidad»), que ya se traduce
        // (`AUD-003`). Era el único desplegable sin nombre: solo tenía texto de sugerencia, que se
        // anuncia cuando está vacío y nunca cuando hay algo elegido.
        AutomationProperties.SetLabeledBy(DrivePicker, UnitGroupLbl);

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
            () =>
            {
                if (_isBusy) return;   // no recargar en mitad de una operación

                // Con una confirmación destructiva abierta, lo que enseña puede haber dejado de ser cierto:
                // se cierra, y quien la abrió vuelve a empezar con la lista al día (`AUD-013`). Cerrarla
                // devuelve None, que es «no confirmado», así que la operación no sigue.
                bool closedConfirm = false;
                if (_openDialog is ConfirmDialog confirm)
                {
                    confirm.Hide();
                    closedConfirm = true;
                }
                LoadDrives();

                // DESPUÉS de recargar: seleccionar una unidad vacía la fila de estado (ApplyProtection), así
                // que dicho antes no llegaría a verse y la confirmación se cerraría sin explicación.
                if (closedConfirm) StatusText.Text = L.T("status.confirmClosed");
            });
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

        // Si el archivo existía y no se pudo leer (bloqueado, sin permiso), esta sesión no guarda nada
        // encima: los presets siguen en el disco (`AUD-037`). Sin esta línea, la persona vería sus
        // preferencias «perdidas» durante la sesión y bien en la siguiente, sin saber por qué.
        if (_settings.ReadFailed)
            _services.History.Log("SETTINGS READ ERROR: no se pudo leer settings.json; esta sesión usa los valores por defecto y no lo sobrescribe");

        // Si la carpeta de estado es un enlace a otro sitio, ni el historial ni las preferencias se
        // tocan (`AUD-025`). Eso incluye esta misma línea de historial, que no se escribiría: el único
        // sitio donde se puede decir es la pantalla. Se dice una vez, al arrancar, porque lo que la
        // persona va a notar —la app «olvida» sus preferencias y el historial sale vacío— no se explica
        // solo.
        if (StateFolder.IsRedirected(History.DefaultPath, AppSettings.DefaultPath))
        {
            // En la primera activación el contenido todavía no tiene XamlRoot, y un ContentDialog sin
            // él lanza. Los demás diálogos de arranque no lo notan porque antes esperan a la red; este
            // no espera a nada. Visto en hardware: sin esta espera el aviso no salía, y la excepción
            // la recogía la red de seguridad de App, que tampoco tenía dónde escribirla.
            await WhenContentLoadedAsync();
            await ShowInfoAsync(L.T("state.redirectedTitle"), L.T("state.redirected", StateFolder.DefaultDirectory));
        }

        // Carpetas de trabajo que dejó una operación que no terminó (`AUD-038`). Al seleccionar la unidad en
        // el arranque la ventana aún no podía alojar un diálogo; ahora sí.
        await WhenContentLoadedAsync();
        if (DrivePicker.SelectedItem is DriveViewModel startupDrive)
            await OfferWorkFolderCleanupAsync(startupDrive.Letter);

        await MaybeShowWhatsNewAsync();

        // La comprobación automática es opcional desde `T9-18`. Es la ÚNICA conexión a Internet de la
        // app, y hasta ahora salía en cada arranque sin preguntar y sin forma de evitarla. Quien la
        // desactiva no se queda sin actualizaciones: *Ayuda → Buscar actualizaciones…* sigue ahí.
        if (_settings.CheckUpdatesOnStartup)
            await CheckForUpdatesAsync(manual: false);
    }

    /// <summary>Se completa cuando el contenido de la ventana está cargado y ya puede alojar un diálogo.</summary>
    private Task WhenContentLoadedAsync()
    {
        if (Content is not FrameworkElement root || root.IsLoaded) return Task.CompletedTask;

        var loaded = new TaskCompletionSource();
        void OnLoaded(object sender, RoutedEventArgs e)
        {
            root.Loaded -= OnLoaded;
            loaded.TrySetResult();
        }
        root.Loaded += OnLoaded;
        return loaded.Task;
    }

    // ── Drive loading ─────────────────────────────────────────────

    /// <summary>
    /// Recarga la lista de unidades y vuelve a seleccionar la que estaba.
    /// </summary>
    /// <remarks>
    /// <b>Si la unidad seleccionada sigue siendo la misma, no se tocan las opciones</b> (`AUD-029`). La
    /// lista se recarga con cada aviso de dispositivo —conectar un ratón basta—, y recargar es vaciarla
    /// y volver a llenarla: la selección pasaba por «ninguna», que desmarca la FAT32 pequeña, y volvía a
    /// la misma unidad restableciendo la etiqueta escrita y las opciones a medio configurar. Ahora los
    /// cambios de selección que provoca la propia recarga se ignoran, y al terminar se decide una vez.
    /// </remarks>
    private void LoadDrives()
    {
        // En el primer arranque no hay selección previa: usamos la última unidad persistida.
        var previous = DrivePicker.SelectedItem as DriveViewModel;
        var prevLetter = previous?.Letter ?? _pendingInitialLetter;
        string? before = previous is null ? null : VolumeFingerprint(previous.Info);
        _pendingInitialLetter = null;

        _reloadingDrives = true;
        try
        {
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
        finally
        {
            _reloadingDrives = false;
        }

        var current = DrivePicker.SelectedItem as DriveViewModel;
        bool sameVolume = previous is not null && current is not null
                       && current.Letter == previous.Letter
                       && VolumeFingerprint(current.Info) == before;
        OnDriveSelected(keepOptions: sameVolume, sameLetter: current?.Letter == previous?.Letter);
    }

    /// <summary>
    /// Lo que se ve de un volumen sin consultar nada lento: si está listo, su tipo, tamaño, sistema de
    /// archivos y etiqueta. Si no cambia entre dos recargas, para la ventana es la misma unidad.
    /// </summary>
    /// <remarks>
    /// No es la identidad del disco —esa es <see cref="DriveIdentity"/>, que cuesta una consulta a
    /// PowerShell y protege lo destructivo—. Esto solo decide si se respetan unas opciones a medio
    /// escribir, y tiene que poder responderse en el acto.
    /// </remarks>
    private static string VolumeFingerprint(DriveInfo drive)
    {
        try
        {
            return drive.IsReady
                ? $"{drive.DriveType}|{drive.TotalSize}|{drive.DriveFormat}|{drive.VolumeLabel}"
                : $"{drive.DriveType}|sin preparar";
        }
        catch (IOException) { return "ilegible"; }
        catch (UnauthorizedAccessException) { return "ilegible"; }
    }

    private void RefreshButton_Click(object sender, RoutedEventArgs e) => LoadDrives();

    // ── Indicio de desbordamiento del contenido (T12-06) ──────────

    private void ContentScroller_SizeChanged(object sender, SizeChangedEventArgs e)
        => UpdateScrollAffordance();

    private void ContentScroller_ViewChanged(object? sender, ScrollViewerViewChangedEventArgs e)
        => UpdateScrollAffordance();

    /// <summary>
    /// Enseña el galón de «hay más abajo» mientras quede contenido sin ver.
    /// </summary>
    /// <remarks>
    /// <para><b>El problema.</b> La ventana es de <b>tamaño fijo</b>, así que el contenido casi siempre
    /// desborda; y Windows oculta las barras de desplazamiento hasta que alguien interactúa. Quedaba una
    /// tarjeta cortada a media altura por el borde inferior sin nada que dijera que había más abajo — y
    /// lo que quedaba abajo eran las <i>opciones de formato</i>.</para>
    ///
    /// <para><b>Dos intentos anteriores, los dos descartados con la app delante.</b> Constan para que no
    /// se reintenten pensando que son lo obvio:</para>
    /// <list type="number">
    ///   <item><description>Un <b>degradado</b> en el borde inferior. Sobre el material <b>Mica</b> no
    ///   hay color de fondo que igualar —el degradado tiene que acabar en algo opaco y la ventana no lo
    ///   es—, así que se leyó como una franja clara; y al superponerse a un <c>TextBox</c> lo dejaba con
    ///   aspecto de deshabilitado. Cualquier velo sobre el contenido tiene ese segundo problema.</description></item>
    ///   <item><description>Forzar <c>VerticalScrollBarVisibility="Visible"</c>. <b>No hace nada</b> con
    ///   las barras auto-ocultas del sistema (el valor por defecto de Windows 11): el <c>ScrollBar</c> se
    ///   hace visible pero su estado de indicador lo sigue colapsando hasta que hay interacción. Se
    ///   comprobó ampliando la franja derecha de una captura: no había rail.</description></item>
    /// </list>
    ///
    /// <para><b>Por eso el galón ocupa su propia fila</b> en vez de flotar sobre el contenido: no tapa
    /// nada, no lava ningún control y no depende de ningún color de fondo. Cuesta 14 px, y solo cuando
    /// hay algo que desplazar.</para>
    ///
    /// <para>No oscila: al aparecer quita alto al <c>ScrollViewer</c>, lo que solo puede <i>aumentar</i>
    /// el desbordamiento, así que la condición que lo encendió sigue siendo cierta.</para>
    /// </remarks>
    private void UpdateScrollAffordance()
    {
        double remaining = ContentScroller.ScrollableHeight - ContentScroller.VerticalOffset;
        ScrollMoreHint.Visibility = remaining > 1 ? Visibility.Visible : Visibility.Collapsed;
    }

    // ── Helpers ───────────────────────────────────────────────────

    // La comparación va por DriveLetter.Same (invariante de cultura) y NO por char.ToUpper: esta es la
    // guarda que impide formatear el disco de Windows, y con cultura turca ToUpper('i') no da 'I'.
    private static bool IsSystemDrive(char letter) => DestructiveGuard.IsSystemDrive(letter, SystemLetter);

    /// <summary>Letra de la unidad que contiene Windows.</summary>
    private static char SystemLetter => Path.GetPathRoot(Environment.SystemDirectory)![0];

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
