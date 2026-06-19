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
    private readonly UISettings _uiSettings = new();
    private Process? _activeProcess;
    private CancellationTokenSource? _cts;
    private DateTime _opStart;
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
        AppWindow.Resize(new SizeInt32(500, 840));
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

        ((FrameworkElement)Content).RequestedTheme = ElementTheme.Default;
        _uiSettings.ColorValuesChanged += OnSystemThemeChanged;

        BuildPresetsMenu();
        ApplyTheme(IsSystemDark());
        ApplyLanguage();
        LoadDrives();

        Activated += OnFirstActivated;
    }

    private void CenterWindow()
    {
        var area = DisplayArea.GetFromWindowId(AppWindow.Id, DisplayAreaFallback.Nearest).WorkArea;
        AppWindow.Move(new PointInt32(
            (area.Width  - 500) / 2,
            (area.Height - 840) / 2));
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
        if (!_isBusy) return;
        args.Cancel = true;
        await ShowInfoAsync(L.T("closing.title"), L.T("closing.body"));
    }

    // ── Drive loading ─────────────────────────────────────────────

    private void LoadDrives()
    {
        var prevLetter = (DrivePicker.SelectedItem as DriveViewModel)?.Letter;
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
        foreach (var preset in Presets.All)
        {
            var item = new MenuFlyoutItem { Text = preset.Name, Tag = preset };
            item.Click += MnuPreset_Click;
            MnuPresets.Items.Add(item);
        }
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

        if (!string.IsNullOrEmpty(label))
        {
            char[] invalidChars = ['\\', '/', ':', '*', '?', '"', '<', '>', '|'];
            if (label.Any(c => invalidChars.Contains(c)))
            {
                await ShowInfoAsync(L.T("msg.invalidTitle"), L.T("msg.invalidLabel"));
                VolumeLabelBox.Focus(FocusState.Programmatic);
                return;
            }

            int maxLabel = FormatLogic.MaxLabelLength(fs);
            if (label.Length > maxLabel)
            {
                await ShowInfoAsync(L.T("msg.labelLongTitle"), L.T("msg.labelLong", maxLabel, fs));
                VolumeLabelBox.Focus(FocusState.Programmatic);
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
                    StatusText.Text = L.T("status.wiping");
                    FormatProgress.IsIndeterminate = true;
                    await DiskService.SecureWipeAsync(driveLetter, _cts!.Token);
                    FormatProgress.IsIndeterminate = false;
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

    private void MnuHistory_Click(object sender, RoutedEventArgs e) => History.Open();

    private async void MnuAbout_Click(object sender, RoutedEventArgs e) =>
        await ShowInfoAsync(L.T("about.title"), L.T("about.body", AppInfo.VersionString));

    // ── Updates ───────────────────────────────────────────────────

    private async void MnuUpdates_Click(object sender, RoutedEventArgs e) =>
        await CheckForUpdatesAsync(manual: true);

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

    private void MnuLangEs_Click(object sender, RoutedEventArgs e) { L.Set(AppLang.Es); ApplyLanguage(); }
    private void MnuLangEn_Click(object sender, RoutedEventArgs e) { L.Set(AppLang.En); ApplyLanguage(); }

    private void MnuThemeAuto_Click(object sender, RoutedEventArgs e)
    {
        _autoTheme = true;
        ((FrameworkElement)Content).RequestedTheme = ElementTheme.Default;
        ApplyTheme(IsSystemDark());
        SyncThemeMenu();
    }

    private void MnuThemeLight_Click(object sender, RoutedEventArgs e)
    {
        _autoTheme = false;
        ((FrameworkElement)Content).RequestedTheme = ElementTheme.Light;
        ApplyTheme(dark: false);
        SyncThemeMenu();
    }

    private void MnuThemeDark_Click(object sender, RoutedEventArgs e)
    {
        _autoTheme = false;
        ((FrameworkElement)Content).RequestedTheme = ElementTheme.Dark;
        ApplyTheme(dark: true);
        SyncThemeMenu();
    }

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
        MnuEject.Text    = L.T("menu.eject");
        MnuHistory.Text  = L.T("menu.history");
        MnuConfig.Title  = L.T("menu.config");
        MnuLang.Text     = L.T("menu.lang");
        MnuLangEs.Text   = L.T("menu.lang.es");
        MnuLangEn.Text   = L.T("menu.lang.en");
        MnuTheme.Text       = L.T("menu.theme");
        MnuThemeAuto.Text   = L.T("menu.theme.auto");
        MnuThemeLight.Text  = L.T("menu.theme.light");
        MnuThemeDark.Text   = L.T("menu.theme.dark");
        MnuPresets.Text  = L.T("menu.presets");
        MnuHelp.Title    = L.T("menu.help");
        MnuUpdates.Text  = L.T("menu.updates");
        MnuAbout.Text    = L.T("menu.about");

        DrivePicker.Header      = L.T("drive.label");
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

    private void OnSystemThemeChanged(UISettings sender, object args)
    {
        if (_autoTheme)
            Content.DispatcherQueue.TryEnqueue(() => ApplyTheme(IsSystemDark()));
    }

    private void ApplyTheme(bool dark)
    {
        _darkMode = dark;

        foreach (var vm in _driveItems)
            vm.ForegroundBrush = DriveBrush(vm.IsProtected);

        if (_isDriveProtected)
            StatusText.Foreground = new SolidColorBrush(ProtectedColor());
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
        SetFormEnabled(true);
        CloseButton.Content = L.T("btn.close");
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
        ElapsedText.Text = (DateTime.Now - _opStart).ToString(@"mm\:ss");
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
