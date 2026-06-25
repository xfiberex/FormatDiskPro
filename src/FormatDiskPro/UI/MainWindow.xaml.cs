using Microsoft.UI.Composition.SystemBackdrops;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using Windows.Graphics;
using Windows.UI;
using Windows.UI.ViewManagement;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Text;

namespace FormatDiskPro.UI;

public sealed partial class MainWindow : Window
{
    // ── Static lookup tables (same as MainForm) ───────────────────

    private static readonly Dictionary<string, (long[] Sizes, long Default)> FsDefaults = new()
    {
        ["NTFS"]  = ([512, 1024, 2048, 4096, 8192, 16384, 32768, 65536], 4096),
        ["exFAT"] = ([4096, 8192, 16384, 32768, 65536, 131072, 262144, 524288, 1048576], 131072),
        ["ReFS"]  = ([4096, 65536], 65536),
        ["FAT32"] = ([512, 1024, 2048, 4096, 8192, 16384, 32768, 65536], 4096),
        ["FAT"]   = ([512, 1024, 2048, 4096, 8192, 16384, 32768], 4096),
    };

    private static readonly Dictionary<string, string> FsDescEs = new()
    {
        ["NTFS"]  = "Ideal para discos internos Windows. Soporta archivos grandes, permisos y cifrado BitLocker.",
        ["exFAT"] = "Recomendado para memorias USB grandes (> 32 GB). Compatible con Windows, macOS y Linux sin límite de tamaño de archivo.",
        ["ReFS"]  = "Sistema resiliente a errores. Óptimo para almacenamiento de datos críticos. Requiere Windows Pro o superior.",
        ["FAT32"] = "Alta compatibilidad con dispositivos y consolas. Límite máximo de 4 GB por archivo.",
        ["FAT"]   = "Sistema heredado para unidades muy pequeñas (< 2 GB). Compatibilidad máxima con hardware antiguo.",
    };

    private static readonly Dictionary<string, string> FsDescEn = new()
    {
        ["NTFS"]  = "Ideal for internal Windows disks. Supports large files, permissions and BitLocker encryption.",
        ["exFAT"] = "Recommended for large USB drives (> 32 GB). Works on Windows, macOS and Linux with no file-size limit.",
        ["ReFS"]  = "Error-resilient file system. Optimal for critical data storage. Requires Windows Pro or higher.",
        ["FAT32"] = "High compatibility with devices and consoles. Maximum 4 GB per file.",
        ["FAT"]   = "Legacy system for very small drives (< 2 GB). Maximum compatibility with old hardware.",
    };

    // ── State ─────────────────────────────────────────────────────

    private bool _isBusy, _cancelRequested, _isDriveProtected, _darkMode, _autoTheme = true;
    // Cierre intencional para auto-actualizarse: la app debe cerrarse (aunque _isBusy siga
    // activo por la descarga) para soltar el AppMutex y los archivos y que el instalador la reemplace.
    private bool _closingForUpdate;
    private readonly UISettings _uiSettings = new();
    private readonly AppSettings _settings = AppSettings.Load();
    private char? _pendingInitialLetter;
    private Process? _activeProcess;
    private CancellationTokenSource? _cts;
    private DateTime _opStart;
    // Umbral mínimo de duración para avisar al terminar (operaciones cortas no avisan).
    private static readonly TimeSpan OperationNotifyThreshold = TimeSpan.FromSeconds(10);
    // Seguimiento de rendimiento para operaciones con bytes (velocidad/ETA, ventana deslizante de 1 s).
    private long _opBytesDone, _opTotalBytes, _speedLastBytes;
    private DateTime _speedLastTime;
    // Pasadas del borrado seguro (NIST 800-88: 1 pasada basta en discos modernos).
    private const int SecureWipePasses = 1;
    private char _healthLetter;
    private DiskService.HealthInfo? _lastHealth;
    private DispatcherTimer _elapsedTimer = null!;
    private readonly ObservableCollection<DriveViewModel> _driveItems = new();
    private readonly List<long> _allocBytes = new();
    private bool _firstActivated = true;
    private ElementTheme CurrentTheme => ((FrameworkElement)Content).RequestedTheme;

    // ── Constructor ───────────────────────────────────────────────

    public MainWindow()
    {
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
        AppWindow.Resize(new SizeInt32(500, 900));
        CenterWindow();

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

        ((FrameworkElement)Content).ActualThemeChanged += OnActualThemeChanged;

        // Restaurar preferencias persistidas: idioma, tema y última unidad seleccionada.
        // (ApplyLanguage construye el menú de presets.)
        L.Set(L.FromCode(_settings.Language));
        _pendingInitialLetter = ParseDriveLetter(_settings.LastDriveLetter);
        ApplyThemeMode(_settings.Theme, save: false);
        MnuNotify.IsChecked = _settings.NotifyOnFinish;
        ApplyLanguage();
        LoadDrives();

        Activated += OnFirstActivated;
    }

    private void CenterWindow()
    {
        var area = DisplayArea.GetFromWindowId(AppWindow.Id, DisplayAreaFallback.Nearest).WorkArea;
        AppWindow.Move(new PointInt32(
            (area.Width  - 500) / 2,
            (area.Height - 900) / 2));
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
        await MaybeShowWhatsNewAsync();
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
        if (_closingForUpdate) return;
        if (!_isBusy) return;
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

    // ── Drive selection ───────────────────────────────────────────

    private void DrivePicker_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (DrivePicker.SelectedItem is not DriveViewModel item)
        {
            _isDriveProtected = false;
            _lastHealth = null;
            ClearInfo();
            return;
        }

        _isDriveProtected = item.IsProtected;
        if (_settings.LastDriveLetter != item.Letter.ToString())
        {
            _settings.LastDriveLetter = item.Letter.ToString();
            _settings.Save();
        }
        UpdateInfo(item.Info);
        UpdateFileSystemOptions(item.Info);
        try { if (item.Info.IsReady) VolumeLabelBox.Text = item.Info.VolumeLabel; }
        catch { VolumeLabelBox.Text = ""; }

        ApplyProtection();
        LoadHealthAsync(item);
    }

    private async void LoadHealthAsync(DriveViewModel item)
    {
        char letter = item.Letter;
        _healthLetter = letter;
        _lastHealth = null;
        InfoHealthText.Text = L.T("info.health", L.T("info.loading"));
        InfoBusText.Text    = L.T("info.bus", L.T("info.loading"));

        var info = await DiskService.GetHealthAsync(letter);
        if (_healthLetter != letter) return;

        _lastHealth = info;
        RenderHealth(info);
    }

    private void RenderHealth(DiskService.HealthInfo? h)
    {
        if (h is null)
        {
            InfoHealthText.Text = L.T("info.health", L.T("info.dash"));
            InfoBusText.Text    = L.T("info.bus", L.T("info.dash"));
            return;
        }
        InfoHealthText.Text = L.T("info.health", h.Health);
        InfoBusText.Text    = L.T("info.bus", $"{h.Bus} · {h.Media}");
    }

    private void ApplyProtection()
    {
        if (_isDriveProtected)
        {
            FileSystemPicker.IsEnabled  = false;
            AllocUnitPicker.IsEnabled   = false;
            VolumeLabelBox.IsEnabled    = false;
            RestoreButton.IsEnabled     = false;
            StartButton.IsEnabled       = false;
            QuickFormatCheck.IsEnabled  = false;
            CompressCheck.IsEnabled     = false;
            SecureWipeCheck.IsEnabled   = false;
            StatusText.Foreground = new SolidColorBrush(ProtectedColor());
            StatusText.Text       = L.T("protected.status");
        }
        else
        {
            FileSystemPicker.IsEnabled  = true;
            AllocUnitPicker.IsEnabled   = true;
            VolumeLabelBox.IsEnabled    = true;
            RestoreButton.IsEnabled     = true;
            StartButton.IsEnabled       = true;
            QuickFormatCheck.IsEnabled  = true;
            SecureWipeCheck.IsEnabled   = true;
            CompressCheck.IsEnabled     = FileSystemPicker.SelectedItem?.ToString() == "NTFS";
            StatusText.ClearValue(TextBlock.ForegroundProperty);
            StatusText.Text = "";
        }
    }

    // Fluent SystemFillColorCritical: #C42B1C (light) / #FF99A4 (dark).
    private Color ProtectedColor() =>
        _darkMode ? Color.FromArgb(255, 255, 153, 164) : Color.FromArgb(255, 196, 43, 28);

    // Fluent TextFillColorPrimary: #E4000000 (light) / #FFFFFFFF (dark).
    private SolidColorBrush DriveBrush(bool isProtected) =>
        isProtected
            ? new SolidColorBrush(ProtectedColor())
            : new SolidColorBrush(_darkMode ? Color.FromArgb(255, 255, 255, 255) : Color.FromArgb(228, 0, 0, 0));

    private void UpdateInfo(DriveInfo drive)
    {
        try
        {
            if (!drive.IsReady) { ClearInfo(); return; }
            InfoTotalText.Text = L.T("info.total", FormatBytes(drive.TotalSize));
            InfoFreeText.Text  = L.T("info.free", FormatBytes(drive.AvailableFreeSpace));
            InfoFsText.Text    = L.T("info.fs", drive.DriveFormat);
            InfoTypeText.Text  = L.T("info.type", DriveTypeName(drive.DriveType));
        }
        catch { ClearInfo(); }
    }

    private void ClearInfo()
    {
        InfoTotalText.Text  = L.T("info.total", L.T("info.dash"));
        InfoFreeText.Text   = L.T("info.free", L.T("info.dash"));
        InfoFsText.Text     = L.T("info.fs", L.T("info.dash"));
        InfoTypeText.Text   = L.T("info.type", L.T("info.dash"));
        InfoHealthText.Text = L.T("info.health", L.T("info.dash"));
        InfoBusText.Text    = L.T("info.bus", L.T("info.dash"));
    }

    // ── File system ───────────────────────────────────────────────

    private void UpdateFileSystemOptions(DriveInfo drive)
    {
        string? previous = FileSystemPicker.SelectedItem?.ToString();
        FileSystemPicker.Items.Clear();

        long bytes = 0;
        try { bytes = drive.IsReady ? drive.TotalSize : 0; } catch { }

        FileSystemPicker.Items.Add("NTFS");
        FileSystemPicker.Items.Add("exFAT");
        FileSystemPicker.Items.Add("ReFS");
        if (bytes == 0 || bytes < 32L * 1024 * 1024 * 1024) FileSystemPicker.Items.Add("FAT32");
        if (bytes == 0 || bytes < 2L * 1024 * 1024 * 1024)  FileSystemPicker.Items.Add("FAT");

        int idx = previous is not null ? FileSystemPicker.Items.IndexOf(previous) : -1;
        if (idx >= 0) FileSystemPicker.SelectedIndex = idx;
        else          SuggestFileSystem(drive, bytes);
    }

    private void SuggestFileSystem(DriveInfo drive, long totalBytes)
    {
        string suggested = drive.DriveType == DriveType.Removable
            ? (totalBytes > 32L * 1024 * 1024 * 1024 ? "exFAT" : "FAT32")
            : "NTFS";
        int idx = FileSystemPicker.Items.IndexOf(suggested);
        FileSystemPicker.SelectedIndex = idx >= 0 ? idx : 0;
    }

    private void FileSystemPicker_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        UpdateAllocationUnits();
        UpdateFsDescription();
        UpdateCompressionOption();
        UpdateLabelMaxLength();
    }

    // Ajusta el máximo de caracteres de la etiqueta al límite del FS seleccionado (FAT/FAT32/exFAT: 11;
    // NTFS/ReFS: 32), para dar feedback inmediato en vez de fallar solo al pulsar Iniciar.
    private void UpdateLabelMaxLength()
    {
        string? fs = FileSystemPicker.SelectedItem?.ToString();
        VolumeLabelBox.MaxLength = fs is not null ? FormatLogic.MaxLabelLength(fs) : 32;
    }

    private void UpdateAllocationUnits()
    {
        string? fs = FileSystemPicker.SelectedItem?.ToString();
        if (fs is null || !FsDefaults.TryGetValue(fs, out var cfg)) return;

        AllocUnitPicker.Items.Clear();
        _allocBytes.Clear();

        foreach (long size in cfg.Sizes)
        {
            string label = size >= 1024 * 1024 ? $"{size / (1024 * 1024)} MB"
                         : size >= 1024 ? $"{size / 1024} KB" : $"{size} bytes";
            AllocUnitPicker.Items.Add(label);
            _allocBytes.Add(size);
        }

        int defIdx = Array.IndexOf(cfg.Sizes, cfg.Default);
        AllocUnitPicker.SelectedIndex = defIdx >= 0 ? defIdx : 0;
    }

    private void UpdateFsDescription()
    {
        string? fs = FileSystemPicker.SelectedItem?.ToString();
        var dict = L.Current == AppLang.Es ? FsDescEs : FsDescEn;
        FsDescText.Text = fs is not null && dict.TryGetValue(fs, out string? desc) ? desc : "";
    }

    private void UpdateCompressionOption()
    {
        bool isNtfs = FileSystemPicker.SelectedItem?.ToString() == "NTFS";
        CompressCheck.IsEnabled = isNtfs && !_isDriveProtected;
        if (!isNtfs) CompressCheck.IsChecked = false;
    }

    private void RestoreButton_Click(object sender, RoutedEventArgs e)
    {
        if (DrivePicker.SelectedItem is DriveViewModel item)
        {
            UpdateFileSystemOptions(item.Info);
            try { if (item.Info.IsReady) VolumeLabelBox.Text = item.Info.VolumeLabel; }
            catch { VolumeLabelBox.Text = ""; }
        }
        QuickFormatCheck.IsChecked = true;
        CompressCheck.IsChecked    = false;
        SecureWipeCheck.IsChecked  = false;
    }

    // ── Presets ───────────────────────────────────────────────────

    private void BuildPresetsMenu()
    {
        MnuPresets.Items.Clear();

        foreach (var preset in Presets.All)
            MnuPresets.Items.Add(MakePresetItem(preset));

        if (_settings.UserPresets.Count > 0)
        {
            MnuPresets.Items.Add(new MenuFlyoutSeparator());
            foreach (var preset in _settings.UserPresets)
                MnuPresets.Items.Add(MakePresetItem(preset));
        }

        MnuPresets.Items.Add(new MenuFlyoutSeparator());
        var manage = new MenuFlyoutItem { Text = L.T("menu.managePresets") };
        manage.Click += MnuManagePresets_Click;
        MnuPresets.Items.Add(manage);
    }

    private MenuFlyoutItem MakePresetItem(FormatPreset preset)
    {
        var item = new MenuFlyoutItem { Text = preset.Name, Tag = preset };
        item.Click += MnuPreset_Click;
        return item;
    }

    private async void MnuManagePresets_Click(object sender, RoutedEventArgs e)
    {
        if (_isBusy) return;

        string fs       = FileSystemPicker.SelectedItem?.ToString() ?? "NTFS";
        long allocBytes = GetSelectedAllocBytes();
        bool quick      = QuickFormatCheck.IsChecked == true;
        bool compress   = CompressCheck.IsChecked    == true;
        bool secure     = SecureWipeCheck.IsChecked  == true;
        var current     = new FormatPreset("", fs, allocBytes, quick, compress, secure);

        string mode = quick ? L.T("fmt.quick") : L.T("fmt.full");
        if (secure) mode += " + " + L.T("confirm.secure");
        string summary = $"{fs} · {AllocUnitPicker.SelectedItem} · {mode}";

        var dlg = new PresetsDialog(current, summary, _settings) { XamlRoot = Content.XamlRoot, RequestedTheme = CurrentTheme };
        await dlg.ShowAsync();
        BuildPresetsMenu();
    }

    private async void MnuPreset_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not MenuFlyoutItem { Tag: FormatPreset preset }) return;
        if (DrivePicker.SelectedItem is not DriveViewModel || _isDriveProtected || _isBusy) return;

        int idx = FileSystemPicker.Items.IndexOf(preset.FileSystem);
        if (idx < 0)
        {
            await ShowInfoAsync(L.T("msg.warning"), L.T("preset.na", preset.Name));
            return;
        }

        FileSystemPicker.SelectedIndex = idx;
        for (int i = 0; i < _allocBytes.Count; i++)
            if (_allocBytes[i] == preset.AllocationUnit) { AllocUnitPicker.SelectedIndex = i; break; }

        QuickFormatCheck.IsChecked = preset.QuickFormat;
        CompressCheck.IsChecked    = preset.Compress && preset.FileSystem == "NTFS";
        SecureWipeCheck.IsChecked  = preset.SecureWipe;

        StatusText.ClearValue(TextBlock.ForegroundProperty);
        StatusText.Text = L.T("preset.body", preset.Name);
    }

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
        if (await DiskService.IsDiskReadOnlyAsync(driveItem.Letter) == true)
        {
            if (!await ShowConfirmAsync(L.T("unlock.confirmTitle"), L.T("unlock.confirmBody", driveItem.Letter)))
                return;
            if (!await DiskService.ClearReadOnlyAsync(driveItem.Letter))
            {
                await ShowInfoAsync(L.T("unlock.confirmTitle"), L.T("unlock.failed", driveItem.Letter));
                return;
            }
        }

        string summary =
            $"{L.T("confirm.warning")}\n\n" +
            $"  {L.T("confirm.drive")}:   {driveItem.DisplayText}\n" +
            $"  {L.T("confirm.fs")}:  {fs}\n" +
            $"  {L.T("confirm.cluster")}:  {AllocUnitPicker.SelectedItem}\n" +
            $"  {L.T("confirm.label")}: {(string.IsNullOrEmpty(label) ? L.T("confirm.nolabel") : label)}\n" +
            $"  {L.T("confirm.mode")}:     {(quick ? L.T("fmt.quick") : L.T("fmt.full"))}" +
            (secure ? $" + {L.T("confirm.secure")}" : "");

        var dlg = new ConfirmDialog(driveItem.Letter, summary) { XamlRoot = Content.XamlRoot, RequestedTheme = CurrentTheme };
        if (await dlg.ShowAsync() != ContentDialogResult.Primary) return;

        await RunFormatAsync(driveItem.Letter, fs, allocBytes, label, quick, compress, secure);
    }

    /// <summary>
    /// Valida la etiqueta de volumen para el sistema de archivos dado: caracteres permitidos y longitud
    /// máxima por FS. Muestra el diálogo correspondiente y devuelve <c>false</c> si no es válida; una
    /// etiqueta vacía siempre es válida. Compartido por formatear (Iniciar) y reinicializar.
    /// </summary>
    private async Task<bool> ValidateLabelAsync(string label, string fs, bool focusOnError)
    {
        if (string.IsNullOrEmpty(label)) return true;

        char[] invalidChars = ['\\', '/', ':', '*', '?', '"', '<', '>', '|'];
        if (label.Any(c => invalidChars.Contains(c)))
        {
            await ShowInfoAsync(L.T("msg.invalidTitle"), L.T("msg.invalidLabel"));
            if (focusOnError) VolumeLabelBox.Focus(FocusState.Programmatic);
            return false;
        }

        int maxLabel = FormatLogic.MaxLabelLength(fs);
        if (label.Length > maxLabel)
        {
            await ShowInfoAsync(L.T("msg.labelLongTitle"), L.T("msg.labelLong", maxLabel, fs));
            if (focusOnError) VolumeLabelBox.Focus(FocusState.Programmatic);
            return false;
        }
        return true;
    }

    private async Task RunFormatAsync(
        char driveLetter, string fs, long allocBytes,
        string label, bool quickFormat, bool compress, bool secureWipe)
    {
        BeginOperation();
        bool useFormatCom = !quickFormat && !compress && fs is "NTFS" or "FAT32" or "FAT";

        StatusText.ClearValue(TextBlock.ForegroundProperty);
        StatusText.Text        = L.T("status.formatting", driveLetter, quickFormat ? L.T("fmt.quick") : L.T("fmt.full"));
        FormatProgress.Value   = 0;
        FormatProgress.IsIndeterminate = !useFormatCom;

        try
        {
            int code; string output;
            if (useFormatCom)
                (code, output) = await RunFormatComAsync(driveLetter, fs, allocBytes, label, _cts!.Token);
            else
            {
                var (c, so, se) = await RunFormatVolumeAsync(driveLetter, fs, allocBytes, label, quickFormat, compress, _cts!.Token);
                code = c;
                output = string.IsNullOrWhiteSpace(se) ? so : se;
            }

            FormatProgress.IsIndeterminate = false;

            if (_cancelRequested)
            {
                FormatProgress.Value = 0;
                StatusText.Text = L.T("status.cancelled");
                History.Log($"FORMAT CANCELLED {driveLetter}: {fs}");
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
                        await SecureWipe.RunAsync(driveLetter, SecureWipePasses, wipeProgress, _cts!.Token);
                    }
                    catch (OperationCanceledException)
                    {
                        _opTotalBytes = 0;
                        FormatProgress.Value = 0;
                        StatusText.Text = L.T("status.cancelled");
                        History.Log($"WIPE CANCELLED {driveLetter}:");
                        return;
                    }

                    _opTotalBytes = 0;   // detener velocidad/ETA: el resto del flujo no maneja bytes
                    FormatProgress.Value = 100;
                    if (_cancelRequested)
                    {
                        StatusText.Text = L.T("status.cancelled");
                        History.Log($"WIPE CANCELLED {driveLetter}:");
                        return;
                    }
                }

                StatusText.Text = L.T("status.success");
                History.Log($"FORMAT OK {driveLetter}: fs={fs} alloc={allocBytes} quick={quickFormat} compress={compress} wipe={secureWipe} label='{label}'");
                await ShowInfoAsync(L.T("success.title"), L.T("success.body", driveLetter, fs));
                LoadDrives();
            }
            else
            {
                FormatProgress.Value = 0;
                StatusText.Text = L.T("status.error");
                History.Log($"FORMAT FAIL {driveLetter}: fs={fs} code={code}");
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
            StatusText.Text = _cancelRequested ? L.T("status.cancelled") : L.T("status.unexpected");
            History.Log($"FORMAT ERROR {driveLetter}: {ex.Message}");
            if (!_cancelRequested)
                await ShowInfoAsync(L.T("msg.error"), $"{L.T("status.unexpected")}\n{ex.Message}");
        }
        finally
        {
            EndOperation();
        }
    }

    private async Task<(int code, string stdout, string stderr)> RunFormatVolumeAsync(
        char driveLetter, string fs, long allocBytes, string label,
        bool quickFormat, bool compress, CancellationToken ct)
    {
        string script = FormatLogic.BuildVolumeScript(driveLetter, fs, allocBytes, label, quickFormat, compress);
        string args   = FormatLogic.EncodeArguments(script);
        var psi = new ProcessStartInfo
        {
            FileName               = "powershell.exe",
            Arguments              = args,
            UseShellExecute        = false,
            RedirectStandardOutput = true,
            RedirectStandardError  = true,
            CreateNoWindow         = true,
        };

        _activeProcess = new Process { StartInfo = psi };
        _activeProcess.Start();
        using var reg = ct.Register(() => { try { _activeProcess?.Kill(entireProcessTree: true); } catch { } });

        var outTask = _activeProcess.StandardOutput.ReadToEndAsync();
        var errTask = _activeProcess.StandardError.ReadToEndAsync();
        await _activeProcess.WaitForExitAsync(CancellationToken.None);

        return (_activeProcess.ExitCode, await outTask, await errTask);
    }

    private async Task<(int code, string output)> RunFormatComAsync(
        char driveLetter, string fs, long allocBytes, string label, CancellationToken ct)
    {
        string formatExe = Path.Combine(Environment.SystemDirectory, "format.com");
        var psi = new ProcessStartInfo
        {
            FileName               = formatExe,
            UseShellExecute        = false,
            RedirectStandardInput  = true,
            RedirectStandardOutput = true,
            RedirectStandardError  = true,
            CreateNoWindow         = true,
        };
        foreach (string a in FormatLogic.BuildComArgumentList(driveLetter, fs, allocBytes, label))
            psi.ArgumentList.Add(a);

        _activeProcess = new Process { StartInfo = psi };
        _activeProcess.Start();
        using var reg = ct.Register(() => { try { _activeProcess?.Kill(entireProcessTree: true); } catch { } });

        try
        {
            await _activeProcess.StandardInput.WriteLineAsync("Y");
            await _activeProcess.StandardInput.WriteLineAsync("S");
            await _activeProcess.StandardInput.FlushAsync(ct);
        }
        catch { }

        var errTask = _activeProcess.StandardError.ReadToEndAsync();
        var sb      = new StringBuilder();
        var buffer  = new char[512];
        int read, lastPct = -1;
        string carry = "";
        var reader = _activeProcess.StandardOutput;
        while ((read = await reader.ReadAsync(buffer.AsMemory(0, buffer.Length))) > 0)
        {
            string chunk = new(buffer, 0, read);
            sb.Append(chunk);

            string scan = carry + chunk;
            int pct = FormatLogic.ExtractPercent(scan);
            if (pct >= 0 && pct != lastPct)
            {
                lastPct = pct;
                FormatProgress.Value = Math.Clamp(pct, 0, 100);
            }
            carry = scan.Length > 16 ? scan[^16..] : scan;
        }

        string err = await errTask;
        await _activeProcess.WaitForExitAsync(CancellationToken.None);

        string output = sb.ToString();
        if (!string.IsNullOrWhiteSpace(err)) output += "\n" + err;
        return (_activeProcess.ExitCode, output);
    }

    // ── Capacity verification ─────────────────────────────────────

    private async void MnuVerify_Click(object sender, RoutedEventArgs e)
    {
        if (_isBusy || DrivePicker.SelectedItem is not DriveViewModel item) return;

        if (item.IsProtected || IsSystemDrive(item.Letter))
        {
            await ShowInfoAsync(L.T("msg.protTitle"), L.T("msg.protBody"));
            return;
        }

        if (!await ShowConfirmAsync(L.T("verify.title"), L.T("verify.warn", item.Letter), defaultNo: true))
            return;

        BeginOperation();
        FormatProgress.IsIndeterminate = false;
        FormatProgress.Value = 0;

        var progress = new Progress<(CapacityVerifier.Phase phase, int percent, long bytes)>(p =>
        {
            FormatProgress.Value = Math.Clamp(p.percent, 0, 100);
            _opBytesDone  = p.bytes;
            _opTotalBytes = p.percent > 0 ? p.bytes * 100L / p.percent : 0;
            StatusText.ClearValue(TextBlock.ForegroundProperty);
            StatusText.Text = p.phase == CapacityVerifier.Phase.Writing
                ? L.T("verify.writing", FormatBytes(p.bytes))
                : L.T("verify.reading", FormatBytes(p.bytes));
        });

        try
        {
            var result = await CapacityVerifier.RunAsync(item.Letter, progress, _cts!.Token);

            if (_cancelRequested || result.FailureDetail == "cancelled")
            {
                FormatProgress.Value = 0;
                StatusText.Text = L.T("status.cancelled");
                return;
            }

            if (result.Ok)
            {
                FormatProgress.Value = 100;
                StatusText.Text = L.T("verify.okTitle");
                History.Log($"VERIFY OK {item.Letter}: written={result.WrittenBytes}");
                await ShowInfoAsync(L.T("verify.okTitle"), L.T("verify.okBody", item.Letter, FormatBytes(result.WrittenBytes)));
            }
            else
            {
                FormatProgress.Value = 0;
                StatusText.Foreground = new SolidColorBrush(ProtectedColor());
                StatusText.Text = L.T("verify.failTitle");
                History.Log($"VERIFY FAIL {item.Letter}: {result.FailureDetail} ok-until={result.WrittenBytes}");
                await ShowInfoAsync(L.T("verify.failTitle"), L.T("verify.failBody", item.Letter, FormatBytes(result.WrittenBytes)));
            }
        }
        finally
        {
            EndOperation();
        }
    }

    // ── Eject ─────────────────────────────────────────────────────

    private async void MnuEject_Click(object sender, RoutedEventArgs e)
    {
        if (_isBusy || DrivePicker.SelectedItem is not DriveViewModel item) return;

        if (item.Info.DriveType != DriveType.Removable)
        {
            await ShowInfoAsync(L.T("msg.warning"), L.T("eject.fixed"));
            return;
        }

        bool ok = await DiskService.EjectAsync(item.Letter);
        if (ok)
        {
            StatusText.ClearValue(TextBlock.ForegroundProperty);
            StatusText.Text = L.T("status.ejected");
            History.Log($"EJECT {item.Letter}:");
            LoadDrives();
        }
        else
        {
            await ShowInfoAsync(L.T("msg.warning"), L.T("eject.fail"));
        }
    }

    private async void MnuHealth_Click(object sender, RoutedEventArgs e)
    {
        if (_isBusy) return;
        if (DrivePicker.SelectedItem is not DriveViewModel item)
        {
            await ShowInfoAsync(L.T("msg.warning"), L.T("msg.selectDrive"));
            return;
        }
        var dlg = new HealthDialog(_darkMode, item.Letter, item.DisplayText)
        {
            XamlRoot = Content.XamlRoot,
            RequestedTheme = CurrentTheme,
        };
        await dlg.ShowAsync();
    }

    // ── Write protection (#7) ─────────────────────────────────────

    private async void MnuUnlock_Click(object sender, RoutedEventArgs e)
    {
        if (_isBusy || DrivePicker.SelectedItem is not DriveViewModel item) return;

        if (item.IsProtected || IsSystemDrive(item.Letter))
        {
            await ShowInfoAsync(L.T("unlock.confirmTitle"), L.T("unlock.blockedSystem"));
            return;
        }

        if (await DiskService.IsDiskReadOnlyAsync(item.Letter) != true)
        {
            await ShowInfoAsync(L.T("unlock.confirmTitle"), L.T("unlock.notProtected", item.Letter));
            return;
        }

        if (!await ShowConfirmAsync(L.T("unlock.confirmTitle"), L.T("unlock.confirmBody", item.Letter)))
            return;

        if (await DiskService.ClearReadOnlyAsync(item.Letter))
        {
            StatusText.ClearValue(TextBlock.ForegroundProperty);
            StatusText.Text = L.T("unlock.cleared", item.Letter);
            History.Log($"UNLOCK {item.Letter}:");
            LoadDrives();
        }
        else
        {
            await ShowInfoAsync(L.T("unlock.confirmTitle"), L.T("unlock.failed", item.Letter));
        }
    }

    // ── chkdsk (#6) ───────────────────────────────────────────────

    private async void MnuCheck_Click(object sender, RoutedEventArgs e)
    {
        if (_isBusy || DrivePicker.SelectedItem is not DriveViewModel item) return;

        bool protectedDrive = item.IsProtected || IsSystemDrive(item.Letter);

        // Modo: Solo comprobar (read-only, por defecto) / Comprobar y reparar / Cancelar.
        // La reparación (/f) no se ofrece en el disco de sistema (programaría un reinicio).
        var modeDlg = new ContentDialog
        {
            Title             = L.T("check.modeTitle"),
            Content           = L.T("check.modeBody", item.Letter),
            PrimaryButtonText = L.T("check.scanOnly"),
            CloseButtonText   = L.T("btn.cancel"),
            DefaultButton     = ContentDialogButton.Primary,
            XamlRoot          = Content.XamlRoot,
            RequestedTheme    = CurrentTheme,
        };
        if (!protectedDrive) modeDlg.SecondaryButtonText = L.T("check.repair");

        var choice = await modeDlg.ShowAsync();
        if (choice == ContentDialogResult.None) return;
        bool repair = choice == ContentDialogResult.Secondary;

        BeginOperation();
        FormatProgress.IsIndeterminate = false;
        FormatProgress.Value = 0;
        StatusText.ClearValue(TextBlock.ForegroundProperty);
        StatusText.Text = repair ? L.T("check.repairing", item.Letter) : L.T("check.scanning", item.Letter);

        var progress = new Progress<int>(p => FormatProgress.Value = Math.Clamp(p, 0, 100));

        try
        {
            var (code, _) = await CheckDisk.RunAsync(item.Letter, repair, progress, _cts!.Token);

            if (_cancelRequested)
            {
                FormatProgress.Value = 0;
                StatusText.Text = L.T("status.cancelled");
                return;
            }

            FormatProgress.Value = 100;
            CheckResult res = CheckDisk.Interpret(code, repair);
            History.Log($"CHKDSK {item.Letter}: repair={repair} code={code} result={res}");

            string msg = res switch
            {
                CheckResult.Clean    => L.T("check.resultClean", item.Letter),
                CheckResult.Repaired => L.T("check.resultRepaired", item.Letter),
                CheckResult.Errors   => L.T("check.resultErrors", item.Letter),
                _                    => L.T("check.resultFailed", item.Letter),
            };
            StatusText.Text = msg;
            await ShowInfoAsync(L.T("check.modeTitle"), msg);
        }
        catch (OperationCanceledException)
        {
            FormatProgress.Value = 0;
            StatusText.Text = L.T("status.cancelled");
        }
        finally
        {
            EndOperation();
        }
    }

    // ── Reinicializar unidad (#8) ─────────────────────────────────

    private async void MnuReinit_Click(object sender, RoutedEventArgs e)
    {
        if (_isBusy || DrivePicker.SelectedItem is not DriveViewModel item) return;

        // Solo unidades extraíbles (USB): es el caso de uso y minimiza el riesgo.
        DriveType type;
        try { type = item.Info.DriveType; } catch { type = DriveType.Unknown; }
        if (type != DriveType.Removable)
        {
            await ShowInfoAsync(L.T("reinit.title"), L.T("reinit.onlyRemovable"));
            return;
        }

        // Nunca el disco del sistema ni una unidad protegida.
        if (item.IsProtected || IsSystemDrive(item.Letter))
        {
            await ShowInfoAsync(L.T("reinit.title"), L.T("reinit.blockedSystem"));
            return;
        }

        // Guarda crítica: el disco físico objetivo no puede ser el mismo que el de Windows
        // (Clear-Disk borra TODO el disco, no solo la partición seleccionada).
        char sysLetter  = Path.GetPathRoot(Environment.SystemDirectory)![0];
        int? targetDisk = await DiskService.GetDiskNumberAsync(item.Letter);
        int? sysDisk    = await DiskService.GetDiskNumberAsync(sysLetter);
        if (targetDisk is null || (sysDisk is not null && targetDisk == sysDisk))
        {
            await ShowInfoAsync(L.T("reinit.title"), L.T("reinit.sameDisk"));
            return;
        }

        // Configuración de formato tomada del formulario.
        string fs    = FileSystemPicker.SelectedItem?.ToString() ?? "NTFS";
        string label = VolumeLabelBox.Text.Trim();
        if (!await ValidateLabelAsync(label, fs, focusOnError: false))
            return;

        DiskPartitionStyle style;
        try { style = ReinitPlan.StyleFor(item.Info.TotalSize); } catch { style = DiskPartitionStyle.Mbr; }

        // Confirmación reforzada: escribir la letra de la unidad (reutiliza ConfirmDialog).
        string summary = L.T("reinit.summary", item.Letter, style.ToPowerShell(), fs);
        var confirm = new ConfirmDialog(item.Letter, summary) { XamlRoot = Content.XamlRoot, RequestedTheme = CurrentTheme };
        if (await confirm.ShowAsync() != ContentDialogResult.Primary) return;

        BeginOperation();
        FormatProgress.IsIndeterminate = true;
        StatusText.ClearValue(TextBlock.ForegroundProperty);
        StatusText.Text = L.T("reinit.stage.clean", item.Letter);

        var stage = new Progress<string>(s => StatusText.Text = L.T($"reinit.stage.{s}", item.Letter));

        try
        {
            var r = await ReinitDrive.RunAsync(item.Letter, style, fs, label, stage, _cts!.Token);
            FormatProgress.IsIndeterminate = false;

            if (_cancelRequested || r.Detail == "cancelled")
            {
                FormatProgress.Value = 0;
                StatusText.Text = L.T("status.cancelled");
                return;
            }

            if (r.Ok && r.NewLetter is char newLetter)
            {
                FormatProgress.Value = 100;
                History.Log($"REINIT {item.Letter}: -> {newLetter}: fs={fs} style={style.ToPowerShell()}");
                _pendingInitialLetter = newLetter;
                DrivePicker.SelectedIndex = -1;   // fuerza que LoadDrives use la nueva letra
                LoadDrives();
                await ShowInfoAsync(L.T("reinit.doneTitle"), L.T("reinit.doneBody", newLetter));
            }
            else
            {
                FormatProgress.Value = 0;
                History.Log($"REINIT FAIL {item.Letter}: {r.Detail}");
                StatusText.Foreground = new SolidColorBrush(ProtectedColor());
                StatusText.Text = L.T("reinit.failed");
                await ShowInfoAsync(L.T("reinit.title"), L.T("reinit.failed"));
            }
        }
        finally
        {
            FormatProgress.IsIndeterminate = false;
            EndOperation();
        }
    }

    // ── Benchmark rápido (#9) ──────────────────────────────────────

    private async void MnuBenchmark_Click(object sender, RoutedEventArgs e)
    {
        if (_isBusy || DrivePicker.SelectedItem is not DriveViewModel item) return;

        if (!await ShowConfirmAsync(L.T("bench.confirmTitle"), L.T("bench.confirmBody", item.Letter)))
            return;

        BeginOperation();
        FormatProgress.IsIndeterminate = false;
        FormatProgress.Value = 0;
        StatusText.ClearValue(TextBlock.ForegroundProperty);
        StatusText.Text = L.T("bench.preparing", item.Letter);

        // Tras terminar, ignora cualquier callback de progreso aún en cola (no debe pisar el estado final).
        bool benchRunning = true;
        var progress = new Progress<(BenchPhase phase, int percent)>(p =>
        {
            if (!benchRunning) return;
            FormatProgress.Value = Math.Clamp(p.percent, 0, 100);
            StatusText.Text = L.T(p.phase switch
            {
                BenchPhase.SeqWrite => "bench.seqWrite",
                BenchPhase.SeqRead  => "bench.seqRead",
                BenchPhase.RndWrite => "bench.rndWrite",
                BenchPhase.RndRead  => "bench.rndRead",
                _                   => "bench.preparing",
            }, item.Letter);
        });

        BenchmarkResult? res = null;
        bool cancelled = false;
        try
        {
            res = await BenchmarkRunner.RunAsync(item.Letter, progress, _cts!.Token);
        }
        catch (OperationCanceledException)
        {
            cancelled = true;   // se trata abajo como cancelación
        }
        finally
        {
            // Cierra la operación (para el cronómetro, repone los botones, avisa si procede) ANTES de mostrar
            // ningún diálogo modal, para que el pie de página no quede en estado "ocupado" tras el resultado.
            benchRunning = false;
            EndOperation();
        }

        if (cancelled || _cancelRequested)
        {
            FormatProgress.Value = 0;
            StatusText.Text = L.T("status.cancelled");
            return;
        }

        if (res is null)
        {
            FormatProgress.Value = 0;
            StatusText.Text = L.T("bench.failed", item.Letter);
            await ShowInfoAsync(L.T("bench.resultTitle"), L.T("bench.noSpace", item.Letter));
            return;
        }

        FormatProgress.Value = 100;
        string seqW = Throughput.FormatSpeed(res.Sequential.WriteBytesPerSec);
        string seqR = Throughput.FormatSpeed(res.Sequential.ReadBytesPerSec);
        string rndW = Throughput.FormatSpeed(res.Random4K.WriteBytesPerSec);
        string rndR = Throughput.FormatSpeed(res.Random4K.ReadBytesPerSec);
        History.Log($"BENCH {item.Letter}: seq w={seqW} r={seqR} · rnd4k w={rndW} r={rndR} bytes={res.TestBytes}");
        StatusText.Text = L.T("bench.resultTitle");
        await ShowDialogAsync(
            L.T("bench.resultTitle"),
            L.T("bench.resultBody", item.Letter, seqW, seqR, rndW, rndR) + "\n\n" + L.T("bench.note"),
            null, null, L.T("btn.close"));
    }

    private async void MnuHistory_Click(object sender, RoutedEventArgs e)
    {
        var dlg = new HistoryDialog(_darkMode) { XamlRoot = Content.XamlRoot, RequestedTheme = CurrentTheme };
        await dlg.ShowAsync();
    }

    private async void MnuAbout_Click(object sender, RoutedEventArgs e) =>
        await ShowInfoAsync(L.T("about.title"), L.T("about.body", AppInfo.VersionString));

    // ── Updates ───────────────────────────────────────────────────

    private async void MnuUpdates_Click(object sender, RoutedEventArgs e) =>
        await CheckForUpdatesAsync(manual: true);

    private async void MnuWhatsNew_Click(object sender, RoutedEventArgs e) =>
        await ShowWhatsNewAsync();

    /// <summary>
    /// Muestra las novedades una sola vez tras una actualización y persiste la versión actual como
    /// "vista". Se considera actualización si la versión cambió respecto a la última registrada, o si
    /// no había versión registrada pero la app ya se había usado (actualización desde una versión sin
    /// el campo, p. ej. 1.6.0 → 1.7.0). En una instalación nueva no se muestra.
    /// </summary>
    private async Task MaybeShowWhatsNewAsync()
    {
        string current = AppInfo.VersionString;
        string? seen = _settings.LastVersionSeen;

        bool updated = string.IsNullOrEmpty(seen)
            ? _settings.LoadedFromFile   // sin versión previa: solo si ya existía configuración (uso previo)
            : seen != current;           // con versión previa: mostrar si cambió

        _settings.LastVersionSeen = current;
        _settings.Save();

        if (updated) await ShowWhatsNewAsync();
    }

    /// <summary>
    /// Carga las notas de la versión instalada desde GitHub (por tag; si no, la última publicada) y
    /// las muestra en el diálogo de novedades. Si no hay red, el diálogo cae a un mensaje informativo.
    /// </summary>
    private async Task ShowWhatsNewAsync()
    {
        ReleaseInfo? rel = null;
        try { rel = await UpdateService.GetReleaseByTagAsync("v" + AppInfo.VersionString) ?? await UpdateService.GetLatestAsync(); }
        catch (Exception ex) { History.Log($"WHATSNEW ERROR: {ex.Message}"); }

        var dlg = new WhatsNewDialog(
            rel?.Version ?? AppInfo.VersionString,
            rel?.Notes ?? "",
            string.IsNullOrEmpty(rel?.HtmlUrl) ? AppInfo.ReleasesPageUrl : rel!.HtmlUrl)
        {
            XamlRoot = Content.XamlRoot,
            RequestedTheme = CurrentTheme,
        };
        await dlg.ShowAsync();
    }

    private async Task CheckForUpdatesAsync(bool manual)
    {
        if (_isBusy) return;

        if (manual)
        {
            StatusText.ClearValue(TextBlock.ForegroundProperty);
            StatusText.Text = L.T("update.checking");
        }

        ReleaseInfo? rel;
        try { rel = await UpdateService.CheckForUpdateAsync(); }
        catch (Exception ex)
        {
            History.Log($"UPDATE CHECK ERROR: {ex.Message}");
            if (manual)
            {
                StatusText.Text = "";
                await ShowInfoAsync(L.T("menu.updates"), L.T("update.error", ex.Message));
            }
            return;
        }

        if (manual) StatusText.Text = "";

        if (rel is null)
        {
            if (manual)
                await ShowInfoAsync(L.T("menu.updates"), L.T("update.uptodate", AppInfo.VersionString));
            return;
        }

        if (_isBusy) return;

        if (!await ShowConfirmAsync(L.T("update.availTitle"), L.T("update.available", rel.Version, AppInfo.VersionString)))
            return;

        if (string.IsNullOrEmpty(rel.AssetUrl))
        {
            await ShowInfoAsync(L.T("update.availTitle"), L.T("update.noasset", rel.Version));
            UpdateService.OpenUrl(rel.HtmlUrl);
            return;
        }

        await DownloadAndRunUpdateAsync(rel);
    }

    private async Task DownloadAndRunUpdateAsync(ReleaseInfo rel)
    {
        BeginOperation();
        FormatProgress.IsIndeterminate = false;
        FormatProgress.Value = 0;

        var progress = new Progress<int>(p =>
        {
            FormatProgress.Value = Math.Clamp(p, 0, 100);
            StatusText.ClearValue(TextBlock.ForegroundProperty);
            StatusText.Text = L.T("update.downloading", p);
        });

        try
        {
            string path = await UpdateService.DownloadAsync(rel, progress, _cts!.Token);
            History.Log($"UPDATE DOWNLOADED {rel.Version}: {path}");
            StatusText.Text = L.T("update.launching");
            // Instalación silenciosa: el instalador cierra esta app, actualiza y la relanza.
            // Marcamos el cierre como intencional ANTES de salir para que AppWindow_Closing no lo
            // cancele por _isBusy; así la app suelta el AppMutex/los archivos y el instalador procede.
            _closingForUpdate = true;
            UpdateService.LaunchInstaller(path, silent: true);
            Application.Current.Exit();
        }
        catch (OperationCanceledException)
        {
            FormatProgress.Value = 0;
            StatusText.Text = L.T("status.cancelled");
        }
        catch (Exception ex)
        {
            FormatProgress.Value = 0;
            StatusText.Text = "";
            History.Log($"UPDATE DOWNLOAD ERROR {rel.Version}: {ex.Message}");
            await ShowInfoAsync(L.T("menu.updates"), L.T("update.error", ex.Message));
        }
        finally
        {
            EndOperation();
        }
    }

    // ── Language / theme ──────────────────────────────────────────

    private void MnuLangEs_Click(object sender, RoutedEventArgs e) => SetLanguage(AppLang.Es);
    private void MnuLangEn_Click(object sender, RoutedEventArgs e) => SetLanguage(AppLang.En);
    private void MnuLangPt_Click(object sender, RoutedEventArgs e) => SetLanguage(AppLang.Pt);
    private void MnuLangFr_Click(object sender, RoutedEventArgs e) => SetLanguage(AppLang.Fr);
    private void MnuLangIt_Click(object sender, RoutedEventArgs e) => SetLanguage(AppLang.It);

    private void SetLanguage(AppLang lang)
    {
        L.Set(lang);
        ApplyLanguage();
        _settings.Language = L.ToCode(lang);
        _settings.Save();
    }

    private void MnuNotify_Click(object sender, RoutedEventArgs e)
    {
        _settings.NotifyOnFinish = MnuNotify.IsChecked;
        _settings.Save();
    }

    private void MnuThemeAuto_Click(object sender, RoutedEventArgs e)  => ApplyThemeMode("auto",  save: true);
    private void MnuThemeLight_Click(object sender, RoutedEventArgs e) => ApplyThemeMode("light", save: true);
    private void MnuThemeDark_Click(object sender, RoutedEventArgs e)  => ApplyThemeMode("dark",  save: true);

    /// <summary>Aplica el modo de tema ("auto"/"light"/"dark"), sincroniza el menú y, opcionalmente, lo persiste.</summary>
    private void ApplyThemeMode(string mode, bool save)
    {
        var root = (FrameworkElement)Content;
        switch (mode)
        {
            case "light":
                _autoTheme = false;
                root.RequestedTheme = ElementTheme.Light;
                ApplyTheme(dark: false);
                break;
            case "dark":
                _autoTheme = false;
                root.RequestedTheme = ElementTheme.Dark;
                ApplyTheme(dark: true);
                break;
            default: // "auto"
                _autoTheme = true;
                root.RequestedTheme = ElementTheme.Default;
                ApplyTheme(IsSystemDark());
                break;
        }
        SyncThemeMenu();
        if (save)
        {
            _settings.Theme = mode is "light" or "dark" ? mode : "auto";
            _settings.Save();
        }
    }

    private static char? ParseDriveLetter(string? s)
        => !string.IsNullOrEmpty(s) && char.IsLetter(s[0]) ? char.ToUpperInvariant(s[0]) : null;

    private void SyncThemeMenu()
    {
        MnuThemeAuto.IsChecked  =  _autoTheme;
        MnuThemeLight.IsChecked = !_autoTheme && !_darkMode;
        MnuThemeDark.IsChecked  = !_autoTheme &&  _darkMode;
    }

    private void ApplyLanguage()
    {
        MnuTools.Title   = L.T("menu.tools");
        MnuVerify.Text   = L.T("menu.verify");
        MnuHealth.Text   = L.T("menu.health");
        MnuCheck.Text    = L.T("menu.check");
        MnuBenchmark.Text = L.T("menu.benchmark");
        MnuUnlock.Text   = L.T("menu.unlock");
        MnuReinit.Text   = L.T("menu.reinit");
        MnuEject.Text    = L.T("menu.eject");
        MnuHistory.Text  = L.T("menu.history");
        MnuConfig.Title  = L.T("menu.config");
        MnuLang.Text     = L.T("menu.lang");
        MnuLangEs.Text   = L.T("menu.lang.es");
        MnuLangEn.Text   = L.T("menu.lang.en");
        MnuLangPt.Text   = L.T("menu.lang.pt");
        MnuLangFr.Text   = L.T("menu.lang.fr");
        MnuLangIt.Text   = L.T("menu.lang.it");
        MnuTheme.Text       = L.T("menu.theme");
        MnuThemeAuto.Text   = L.T("menu.theme.auto");
        MnuThemeLight.Text  = L.T("menu.theme.light");
        MnuThemeDark.Text   = L.T("menu.theme.dark");
        MnuPresets.Text  = L.T("menu.presets");
        MnuNotify.Text   = L.T("menu.notify");
        MnuHelp.Title    = L.T("menu.help");
        MnuUpdates.Text  = L.T("menu.updates");
        MnuWhatsNew.Text = L.T("menu.whatsnew");
        MnuAbout.Text    = L.T("menu.about");

        UnitGroupLbl.Text       = L.T("section.drive");
        FormatGroupLbl.Text     = L.T("section.format");
        FileSystemPicker.Header = L.T("fs.label");
        AllocUnitPicker.Header  = L.T("alloc.label");
        VolumeLabelBox.Header   = L.T("label.label");
        OptionsGroupLbl.Text = L.T("options.group");
        QuickFormatCheck.Content = L.T("opt.quick");
        CompressCheck.Content    = L.T("opt.compress");
        SecureWipeCheck.Content  = L.T("opt.secure");
        RestoreButton.Content    = L.T("btn.restore");
        StartButton.Content      = L.T("btn.start");
        if (!_isBusy) CloseButton.Content = L.T("btn.close");
        RefreshTooltip.Content   = L.T("tip.refresh");
        AutomationProperties.SetName(RefreshButton, L.T("tip.refresh"));

        MnuLangEs.IsChecked = L.Current == AppLang.Es;
        MnuLangEn.IsChecked = L.Current == AppLang.En;
        MnuLangPt.IsChecked = L.Current == AppLang.Pt;
        MnuLangFr.IsChecked = L.Current == AppLang.Fr;
        MnuLangIt.IsChecked = L.Current == AppLang.It;

        // Reconstruir el menú de presets para refrescar la etiqueta «Gestionar presets…».
        BuildPresetsMenu();

        if (DrivePicker.SelectedItem is DriveViewModel item)
        {
            UpdateInfo(item.Info);
            RenderHealth(_lastHealth);
        }
        else
        {
            ClearInfo();
        }
        UpdateFsDescription();

        if (_isDriveProtected)
        {
            StatusText.Foreground = new SolidColorBrush(ProtectedColor());
            StatusText.Text       = L.T("protected.status");
        }
    }

    private bool IsSystemDark()
    {
        var bg = _uiSettings.GetColorValue(UIColorType.Background);
        return bg.R < 128;
    }

    // Se dispara en el hilo de UI cuando cambia el tema EFECTIVO del contenido —incluye los
    // cambios del tema de Windows cuando RequestedTheme = Default (modo Automático)—. Sustituye a
    // UISettings.ColorValuesChanged, que se disparaba en un hilo en segundo plano y provocaba
    // cierres inesperados de la app al cambiar el tema del sistema. En modo forzado (Claro/Oscuro)
    // el tema efectivo no cambia con el del sistema, así que este handler no se dispara (correcto).
    private void OnActualThemeChanged(FrameworkElement sender, object args)
        => ApplyTheme(sender.ActualTheme == ElementTheme.Dark);

    private void ApplyTheme(bool dark)
    {
        _darkMode = dark;
        UpdateCaptionButtonColors(dark);

        foreach (var vm in _driveItems)
            vm.ForegroundBrush = DriveBrush(vm.IsProtected);

        if (_isDriveProtected)
            StatusText.Foreground = new SolidColorBrush(ProtectedColor());
    }

    // Tematiza los botones de caption (minimizar/maximizar/cerrar) según el tema EFECTIVO.
    // Con ExtendsContentIntoTitleBar a nivel de Window, WinUI NO refresca de forma fiable estos
    // botones en un cambio de tema en caliente, y sus colores POR DEFECTO siguen el tema del
    // SISTEMA (no el RequestedTheme forzado de la app); al forzar Claro con Windows en Oscuro (o
    // viceversa) el fondo hover/pressed quedaba con el tema contrario. Por eso fijamos TODOS los
    // colores —incluidos los fondos hover/pressed— derivándolos del tema efectivo (no de UISettings,
    // que reflejaba el modo de app del sistema y causaba el contraste incorrecto).
    // Compromiso: al fijar el fondo hover, el botón Cerrar deja de ponerse rojo (la API es global
    // para todos los botones de caption); se prioriza la consistencia con el tema forzado.
    private void UpdateCaptionButtonColors(bool dark)
    {
        var titleBar = AppWindow.TitleBar;

        Color fg          = dark ? Color.FromArgb(255, 255, 255, 255) : Color.FromArgb(255, 0x19, 0x19, 0x19);
        Color inactiveFg  = dark ? Color.FromArgb(255, 0x9B, 0x9B, 0x9B) : Color.FromArgb(255, 0x86, 0x86, 0x86);
        Color transparent = Color.FromArgb(0, 0, 0, 0);
        // Overlays sutiles acordes al tema efectivo: blanco sobre oscuro, negro sobre claro.
        Color hover       = dark ? Color.FromArgb(0x17, 255, 255, 255) : Color.FromArgb(0x17, 0, 0, 0);
        Color pressed     = dark ? Color.FromArgb(0x0F, 255, 255, 255) : Color.FromArgb(0x0F, 0, 0, 0);

        titleBar.ButtonForegroundColor         = fg;
        titleBar.ButtonHoverForegroundColor    = fg;
        titleBar.ButtonPressedForegroundColor  = fg;
        titleBar.ButtonInactiveForegroundColor = inactiveFg;

        titleBar.ButtonBackgroundColor         = transparent;
        titleBar.ButtonInactiveBackgroundColor = transparent;
        titleBar.ButtonHoverBackgroundColor    = hover;
        titleBar.ButtonPressedBackgroundColor  = pressed;
    }

    // ── Operation lifecycle ───────────────────────────────────────

    private void BeginOperation()
    {
        _isBusy = true;
        _cancelRequested = false;
        _cts = new CancellationTokenSource();
        SetFormEnabled(false);
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
        _isBusy = false;
        _activeProcess?.Dispose();
        _activeProcess = null;
        _cts?.Dispose();
        _cts = null;
        // Si nos estamos cerrando para actualizar, la ventana ya se va: no tocar la UI.
        if (_closingForUpdate) return;
        SetFormEnabled(true);
        CloseButton.Content = L.T("btn.close");

        // Aviso al terminar operaciones largas: sonido + parpadeo de la barra (solo si el usuario
        // no está mirando la ventana). No aplica a operaciones cortas ni canceladas.
        if (Notifier.ShouldNotify(DateTime.Now - _opStart, _settings.NotifyOnFinish, _cancelRequested, OperationNotifyThreshold))
            Notifier.OperationFinished(WinRT.Interop.WindowNative.GetWindowHandle(this));
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
            if (dt > 0 && db > 0)
            {
                double speed = db / dt;
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
    }

    // ── Helpers ───────────────────────────────────────────────────

    private static bool IsSystemDrive(char letter)
    {
        char sys = Path.GetPathRoot(Environment.SystemDirectory)![0];
        return char.ToUpper(letter) == char.ToUpper(sys);
    }

    private void SetFormEnabled(bool enabled)
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

        if (enabled && _isDriveProtected)
        {
            StatusText.Foreground = new SolidColorBrush(ProtectedColor());
            StatusText.Text       = L.T("protected.status");
        }
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
