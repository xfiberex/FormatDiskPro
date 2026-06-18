using System.Diagnostics;
using System.Text;

namespace FormatDiskPro;

public partial class MainForm : Form
{
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

    private bool _isBusy;
    private bool _cancelRequested;
    private bool _isDriveProtected;
    private bool _darkMode;
    private Color _normalText = SystemColors.ControlText;
    private Process? _activeProcess;
    private CancellationTokenSource? _cts;
    private DateTime _opStart;
    private char _healthLetter;
    private DiskService.HealthInfo? _lastHealth;

    public MainForm()
    {
        InitializeComponent();
        try { Icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath); } catch { }
        BuildPresetsMenu();
        ApplyTheme(dark: false);
        ApplyLanguage();
        LoadDrives();
    }

    // ── Drive loading ────────────────────────────────────────────

    private void LoadDrives()
    {
        char? prev = (cboDrive.SelectedItem as DriveItem)?.Letter;
        cboDrive.Items.Clear();

        foreach (var d in DriveInfo.GetDrives()
            .Where(d => d.DriveType is DriveType.Fixed or DriveType.Removable or DriveType.Ram))
        {
            string label;
            try { label = d.IsReady && !string.IsNullOrEmpty(d.VolumeLabel) ? $"{d.Name.TrimEnd('\\')} ({d.VolumeLabel})" : d.Name.TrimEnd('\\'); }
            catch { label = d.Name.TrimEnd('\\'); }
            cboDrive.Items.Add(new DriveItem(d.Name[0], label, d, IsSystemDrive(d.Name[0])));
        }

        int idx = -1;
        if (prev.HasValue)
            for (int i = 0; i < cboDrive.Items.Count; i++)
                if ((cboDrive.Items[i] as DriveItem)?.Letter == prev.Value) { idx = i; break; }

        cboDrive.SelectedIndex = idx >= 0 ? idx : (cboDrive.Items.Count > 0 ? 0 : -1);
    }

    private void btnRefresh_Click(object sender, EventArgs e) => LoadDrives();

    // ── Drive selection ──────────────────────────────────────────

    private void cboDrive_SelectedIndexChanged(object sender, EventArgs e)
    {
        if (cboDrive.SelectedItem is not DriveItem item)
        {
            _isDriveProtected = false;
            _lastHealth = null;
            UpdateHeader(null);
            ClearInfo();
            return;
        }

        _isDriveProtected = item.IsProtected;

        UpdateHeader(item);
        UpdateInfo(item.Info);
        UpdateFileSystemOptions(item.Info);
        try { if (item.Info.IsReady) txtLabel.Text = item.Info.VolumeLabel; }
        catch { txtLabel.Text = ""; }

        ApplyProtection();
        LoadHealthAsync(item);
    }

    private async void LoadHealthAsync(DriveItem item)
    {
        char letter = item.Letter;
        _healthLetter = letter;
        _lastHealth = null;
        lblInfoHealth.Text = L.T("info.health", L.T("info.loading"));
        lblInfoBus.Text    = L.T("info.bus", L.T("info.loading"));

        var info = await DiskService.GetHealthAsync(letter);
        if (_healthLetter != letter) return; // la selección cambió mientras consultaba

        _lastHealth = info;
        RenderHealth(info);
    }

    private void RenderHealth(DiskService.HealthInfo? h)
    {
        if (h is null)
        {
            lblInfoHealth.Text = L.T("info.health", L.T("info.dash"));
            lblInfoBus.Text    = L.T("info.bus", L.T("info.dash"));
            return;
        }
        lblInfoHealth.Text = L.T("info.health", h.Health);
        lblInfoBus.Text    = L.T("info.bus", $"{h.Bus} · {h.Media}");
    }

    private void ApplyProtection()
    {
        if (_isDriveProtected)
        {
            cboFileSystem.Enabled  = false;
            cboAllocUnit.Enabled   = false;
            txtLabel.Enabled       = false;
            btnRestore.Enabled     = false;
            btnStart.Enabled       = false;
            chkQuickFormat.Enabled = false;
            chkCompress.Enabled    = false;
            chkSecureWipe.Enabled  = false;
            lblStatus.ForeColor    = ProtectedColor();
            lblStatus.Text         = L.T("protected.status");
        }
        else
        {
            cboFileSystem.Enabled  = true;
            cboAllocUnit.Enabled   = true;
            txtLabel.Enabled       = true;
            btnRestore.Enabled     = true;
            btnStart.Enabled       = true;
            chkQuickFormat.Enabled = true;
            chkSecureWipe.Enabled  = true;
            chkCompress.Enabled    = cboFileSystem.SelectedItem?.ToString() == "NTFS";
            lblStatus.ForeColor    = _normalText;
            lblStatus.Text         = "";
        }
    }

    private Color ProtectedColor() => _darkMode ? Color.FromArgb(255, 120, 120) : Color.FromArgb(192, 0, 0);

    private void UpdateHeader(DriveItem? item)
    {
        if (item is null) { lblHeaderSub.Text = L.T("app.subtitle"); return; }
        try
        {
            string size = item.Info.IsReady ? FormatBytes(item.Info.TotalSize) : L.T("info.dash");
            lblHeaderSub.Text = $"{item} · {size} · {DriveTypeName(item.Info.DriveType)}";
        }
        catch { lblHeaderSub.Text = item.ToString(); }
    }

    private void UpdateInfo(DriveInfo drive)
    {
        try
        {
            if (!drive.IsReady) { ClearInfo(); return; }
            lblInfoTotal.Text = L.T("info.total", FormatBytes(drive.TotalSize));
            lblInfoFree.Text  = L.T("info.free", FormatBytes(drive.AvailableFreeSpace));
            lblInfoFs.Text    = L.T("info.fs", drive.DriveFormat);
            lblInfoType.Text  = L.T("info.type", DriveTypeName(drive.DriveType));
        }
        catch { ClearInfo(); }
    }

    private void ClearInfo()
    {
        lblInfoTotal.Text  = L.T("info.total", L.T("info.dash"));
        lblInfoFree.Text   = L.T("info.free", L.T("info.dash"));
        lblInfoFs.Text     = L.T("info.fs", L.T("info.dash"));
        lblInfoType.Text   = L.T("info.type", L.T("info.dash"));
        lblInfoHealth.Text = L.T("info.health", L.T("info.dash"));
        lblInfoBus.Text    = L.T("info.bus", L.T("info.dash"));
    }

    // ── File system ──────────────────────────────────────────────

    private void UpdateFileSystemOptions(DriveInfo drive)
    {
        string? previous = cboFileSystem.SelectedItem?.ToString();
        cboFileSystem.Items.Clear();

        long bytes = 0;
        try { bytes = drive.IsReady ? drive.TotalSize : 0; } catch { }

        cboFileSystem.Items.Add("NTFS");
        cboFileSystem.Items.Add("exFAT");
        cboFileSystem.Items.Add("ReFS");
        if (bytes == 0 || bytes < 32L * 1024 * 1024 * 1024) cboFileSystem.Items.Add("FAT32");
        if (bytes == 0 || bytes < 2L * 1024 * 1024 * 1024)  cboFileSystem.Items.Add("FAT");

        int idx = previous is not null ? cboFileSystem.Items.IndexOf(previous) : -1;
        if (idx >= 0) cboFileSystem.SelectedIndex = idx;
        else          SuggestFileSystem(drive, bytes);
    }

    private void SuggestFileSystem(DriveInfo drive, long totalBytes)
    {
        string suggested = drive.DriveType == DriveType.Removable
            ? (totalBytes > 32L * 1024 * 1024 * 1024 ? "exFAT" : "FAT32")
            : "NTFS";
        int idx = cboFileSystem.Items.IndexOf(suggested);
        cboFileSystem.SelectedIndex = idx >= 0 ? idx : 0;
    }

    private void cboFileSystem_SelectedIndexChanged(object sender, EventArgs e)
    {
        UpdateAllocationUnits();
        UpdateFsDescription();
        UpdateCompressionOption();
    }

    private void UpdateAllocationUnits()
    {
        string? fs = cboFileSystem.SelectedItem?.ToString();
        if (fs is null || !FsDefaults.TryGetValue(fs, out var cfg)) return;

        cboAllocUnit.Items.Clear();
        foreach (long size in cfg.Sizes) cboAllocUnit.Items.Add(new AllocUnitItem(size));

        for (int i = 0; i < cboAllocUnit.Items.Count; i++)
            if ((cboAllocUnit.Items[i] as AllocUnitItem)?.Bytes == cfg.Default) { cboAllocUnit.SelectedIndex = i; return; }

        if (cboAllocUnit.Items.Count > 0) cboAllocUnit.SelectedIndex = 0;
    }

    private void UpdateFsDescription()
    {
        string? fs = cboFileSystem.SelectedItem?.ToString();
        var dict = L.Current == AppLang.Es ? FsDescEs : FsDescEn;
        lblFsDesc.Text = fs is not null && dict.TryGetValue(fs, out string? desc) ? desc : "";
    }

    private void UpdateCompressionOption()
    {
        bool isNtfs = cboFileSystem.SelectedItem?.ToString() == "NTFS";
        chkCompress.Enabled = isNtfs && !_isDriveProtected;
        if (!isNtfs) chkCompress.Checked = false;
    }

    private void btnRestore_Click(object sender, EventArgs e)
    {
        if (cboDrive.SelectedItem is DriveItem item)
        {
            UpdateFileSystemOptions(item.Info);
            try { if (item.Info.IsReady) txtLabel.Text = item.Info.VolumeLabel; }
            catch { txtLabel.Text = ""; }
        }
        chkQuickFormat.Checked = true;
        chkCompress.Checked    = false;
        chkSecureWipe.Checked  = false;
    }

    // ── Presets ──────────────────────────────────────────────────

    private void BuildPresetsMenu()
    {
        foreach (var preset in Presets.All)
        {
            var item = new ToolStripMenuItem(preset.Name) { Tag = preset };
            item.Click += mnuPreset_Click;
            mnuPresets.DropDownItems.Add(item);
        }
    }

    private void mnuPreset_Click(object? sender, EventArgs e)
    {
        if (sender is not ToolStripMenuItem { Tag: FormatPreset preset }) return;
        if (cboDrive.SelectedItem is not DriveItem || _isDriveProtected || _isBusy) return;

        int idx = cboFileSystem.Items.IndexOf(preset.FileSystem);
        if (idx < 0)
        {
            MessageBox.Show(L.T("preset.na", preset.Name), L.T("msg.warning"),
                MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        cboFileSystem.SelectedIndex = idx; // dispara la actualización de clusters
        for (int i = 0; i < cboAllocUnit.Items.Count; i++)
            if ((cboAllocUnit.Items[i] as AllocUnitItem)?.Bytes == preset.AllocationUnit) { cboAllocUnit.SelectedIndex = i; break; }

        chkQuickFormat.Checked = preset.QuickFormat;
        chkCompress.Checked    = preset.Compress && preset.FileSystem == "NTFS";
        chkSecureWipe.Checked  = preset.SecureWipe;

        lblStatus.ForeColor = _normalText;
        lblStatus.Text      = L.T("preset.body", preset.Name);
    }

    // ── Format ───────────────────────────────────────────────────

    private async void btnStart_Click(object sender, EventArgs e)
    {
        if (cboDrive.SelectedItem is not DriveItem driveItem)
        {
            MessageBox.Show(L.T("msg.selectDrive"), L.T("msg.warning"), MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }
        if (_isDriveProtected)
        {
            MessageBox.Show(L.T("msg.protBody"), L.T("msg.protTitle"), MessageBoxButtons.OK, MessageBoxIcon.Error);
            return;
        }
        if (cboFileSystem.SelectedItem is null || cboAllocUnit.SelectedItem is null)
        {
            MessageBox.Show(L.T("msg.selectFsAlloc"), L.T("msg.warning"), MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        if (IsSystemDrive(driveItem.Letter))
        {
            MessageBox.Show(L.T("msg.systemBody"), L.T("msg.systemTitle"), MessageBoxButtons.OK, MessageBoxIcon.Error);
            return;
        }

        string fs        = cboFileSystem.SelectedItem.ToString()!;
        var    allocUnit = (AllocUnitItem)cboAllocUnit.SelectedItem!;
        string label     = txtLabel.Text.Trim();
        bool   quick     = chkQuickFormat.Checked;
        bool   compress  = chkCompress.Checked;
        bool   secure    = chkSecureWipe.Checked;

        bool driveReady;
        try { driveReady = driveItem.Info.IsReady; } catch { driveReady = false; }
        if (!driveReady)
        {
            MessageBox.Show(L.T("msg.goneBody", driveItem.Letter), L.T("msg.goneTitle"),
                MessageBoxButtons.OK, MessageBoxIcon.Warning);
            LoadDrives();
            return;
        }

        if (!string.IsNullOrEmpty(label))
        {
            char[] invalidChars = ['\\', '/', ':', '*', '?', '"', '<', '>', '|'];
            if (label.Any(c => invalidChars.Contains(c)))
            {
                MessageBox.Show(L.T("msg.invalidLabel"), L.T("msg.invalidTitle"),
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                txtLabel.Focus();
                return;
            }

            int maxLabel = FormatLogic.MaxLabelLength(fs);
            if (label.Length > maxLabel)
            {
                MessageBox.Show(L.T("msg.labelLong", maxLabel, fs), L.T("msg.labelLongTitle"),
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                txtLabel.Focus();
                return;
            }
        }

        string summary =
            $"{L.T("confirm.warning")}\n\n" +
            $"  {L.T("confirm.drive")}:   {driveItem}\n" +
            $"  {L.T("confirm.fs")}:  {fs}\n" +
            $"  {L.T("confirm.cluster")}:  {allocUnit}\n" +
            $"  {L.T("confirm.label")}: {(string.IsNullOrEmpty(label) ? L.T("confirm.nolabel") : label)}\n" +
            $"  {L.T("confirm.mode")}:     {(quick ? L.T("fmt.quick") : L.T("fmt.full"))}" +
            (secure ? $" + {L.T("confirm.secure")}" : "");

        using var dlg = new ConfirmFormatDialog(driveItem.Letter, summary);
        if (dlg.ShowDialog(this) != DialogResult.OK) return;

        await RunFormatAsync(driveItem.Letter, fs, allocUnit.Bytes, label, quick, compress, secure);
    }

    private async Task RunFormatAsync(
        char driveLetter, string fs, long allocBytes,
        string label, bool quickFormat, bool compress, bool secureWipe)
    {
        BeginOperation();
        bool useFormatCom = !quickFormat && !compress && fs is "NTFS" or "FAT32" or "FAT";

        lblStatus.Text    = L.T("status.formatting", driveLetter, quickFormat ? L.T("fmt.quick") : L.T("fmt.full"));
        progressBar.Value = 0;
        progressBar.Style = useFormatCom ? ProgressBarStyle.Blocks : ProgressBarStyle.Marquee;

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

            progressBar.Style = ProgressBarStyle.Blocks;

            if (_cancelRequested)
            {
                progressBar.Value = 0;
                lblStatus.Text = L.T("status.cancelled");
                History.Log($"FORMAT CANCELLED {driveLetter}: {fs}");
                return;
            }

            if (code == 0)
            {
                progressBar.Value = 100;

                if (secureWipe)
                {
                    lblStatus.Text = L.T("status.wiping");
                    progressBar.Style = ProgressBarStyle.Marquee;
                    await DiskService.SecureWipeAsync(driveLetter, _cts!.Token);
                    progressBar.Style = ProgressBarStyle.Blocks;
                    progressBar.Value = 100;
                    if (_cancelRequested)
                    {
                        lblStatus.Text = L.T("status.cancelled");
                        History.Log($"WIPE CANCELLED {driveLetter}:");
                        return;
                    }
                }

                lblStatus.Text = L.T("status.success");
                History.Log($"FORMAT OK {driveLetter}: fs={fs} alloc={allocBytes} quick={quickFormat} compress={compress} wipe={secureWipe} label='{label}'");
                MessageBox.Show(L.T("success.body", driveLetter, fs), L.T("success.title"),
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                LoadDrives();
            }
            else
            {
                progressBar.Value = 0;
                lblStatus.Text = L.T("status.error");
                History.Log($"FORMAT FAIL {driveLetter}: fs={fs} code={code}");
                string msg = output.Trim();
                MessageBox.Show(L.T("error.formatBody", driveLetter, msg), L.T("error.formatTitle"),
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        catch (OperationCanceledException)
        {
            progressBar.Style = ProgressBarStyle.Blocks;
            progressBar.Value = 0;
            lblStatus.Text = L.T("status.cancelled");
        }
        catch (Exception ex)
        {
            progressBar.Style = ProgressBarStyle.Blocks;
            progressBar.Value = 0;
            lblStatus.Text = _cancelRequested ? L.T("status.cancelled") : L.T("status.unexpected");
            History.Log($"FORMAT ERROR {driveLetter}: {ex.Message}");
            if (!_cancelRequested)
                MessageBox.Show($"{L.T("status.unexpected")}\n{ex.Message}", L.T("msg.error"), MessageBoxButtons.OK, MessageBoxIcon.Error);
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
        // ArgumentList escapa cada argumento por separado: la etiqueta no puede romper la línea de comandos.
        foreach (string a in FormatLogic.BuildComArgumentList(driveLetter, fs, allocBytes, label))
            psi.ArgumentList.Add(a);

        _activeProcess = new Process { StartInfo = psi };
        _activeProcess.Start();
        using var reg = ct.Register(() => { try { _activeProcess?.Kill(entireProcessTree: true); } catch { } });

        // Auto-responder al "¿Continuar con el formato? (S/N)" en cualquier idioma.
        try
        {
            await _activeProcess.StandardInput.WriteLineAsync("Y");
            await _activeProcess.StandardInput.WriteLineAsync("S");
            await _activeProcess.StandardInput.FlushAsync(ct);
        }
        catch { }

        var errTask = _activeProcess.StandardError.ReadToEndAsync();
        var sb = new StringBuilder();
        var buffer = new char[512];
        int read, lastPct = -1;
        string carry = "";  // cola del fragmento previo, por si un token "NN%" queda partido entre lecturas
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
                progressBar.Value = Math.Clamp(pct, 0, 100);
            }
            carry = scan.Length > 16 ? scan[^16..] : scan;
        }

        string err = await errTask;
        await _activeProcess.WaitForExitAsync(CancellationToken.None);

        string output = sb.ToString();
        if (!string.IsNullOrWhiteSpace(err)) output += "\n" + err;
        return (_activeProcess.ExitCode, output);
    }

    // ── Capacity verification ────────────────────────────────────

    private async void mnuVerify_Click(object? sender, EventArgs e)
    {
        if (_isBusy || cboDrive.SelectedItem is not DriveItem item) return;

        if (item.IsProtected || IsSystemDrive(item.Letter))
        {
            MessageBox.Show(L.T("msg.protBody"), L.T("msg.protTitle"), MessageBoxButtons.OK, MessageBoxIcon.Error);
            return;
        }

        if (MessageBox.Show(L.T("verify.warn", item.Letter), L.T("verify.title"),
                MessageBoxButtons.YesNo, MessageBoxIcon.Warning, MessageBoxDefaultButton.Button2) != DialogResult.Yes)
            return;

        BeginOperation();
        progressBar.Style = ProgressBarStyle.Blocks;
        progressBar.Value = 0;

        var progress = new Progress<(CapacityVerifier.Phase phase, int percent, long bytes)>(p =>
        {
            progressBar.Value = Math.Clamp(p.percent, 0, 100);
            lblStatus.ForeColor = _normalText;
            lblStatus.Text = p.phase == CapacityVerifier.Phase.Writing
                ? L.T("verify.writing", FormatBytes(p.bytes))
                : L.T("verify.reading", FormatBytes(p.bytes));
        });

        try
        {
            var result = await CapacityVerifier.RunAsync(item.Letter, progress, _cts!.Token);

            if (_cancelRequested || result.FailureDetail == "cancelled")
            {
                progressBar.Value = 0;
                lblStatus.Text = L.T("status.cancelled");
                return;
            }

            if (result.Ok)
            {
                progressBar.Value = 100;
                lblStatus.Text = L.T("verify.okTitle");
                History.Log($"VERIFY OK {item.Letter}: written={result.WrittenBytes}");
                MessageBox.Show(L.T("verify.okBody", item.Letter, FormatBytes(result.WrittenBytes)),
                    L.T("verify.okTitle"), MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            else
            {
                progressBar.Value = 0;
                lblStatus.ForeColor = ProtectedColor();
                lblStatus.Text = L.T("verify.failTitle");
                History.Log($"VERIFY FAIL {item.Letter}: {result.FailureDetail} ok-until={result.WrittenBytes}");
                MessageBox.Show(L.T("verify.failBody", item.Letter, FormatBytes(result.WrittenBytes)),
                    L.T("verify.failTitle"), MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        finally
        {
            EndOperation();
        }
    }

    // ── Eject ────────────────────────────────────────────────────

    private async void mnuEject_Click(object? sender, EventArgs e)
    {
        if (_isBusy || cboDrive.SelectedItem is not DriveItem item) return;

        if (item.Info.DriveType != DriveType.Removable)
        {
            MessageBox.Show(L.T("eject.fixed"), L.T("msg.warning"), MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        bool ok = await DiskService.EjectAsync(item.Letter);
        if (ok)
        {
            lblStatus.ForeColor = _normalText;
            lblStatus.Text = L.T("status.ejected");
            History.Log($"EJECT {item.Letter}:");
            LoadDrives();
        }
        else
        {
            MessageBox.Show(L.T("eject.fail"), L.T("msg.warning"), MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
    }

    private void mnuHistory_Click(object? sender, EventArgs e) => History.Open();

    private void mnuAbout_Click(object? sender, EventArgs e) =>
        MessageBox.Show(L.T("about.body"), L.T("about.title"), MessageBoxButtons.OK, MessageBoxIcon.Information);

    // ── Language / theme ─────────────────────────────────────────

    private void mnuLangEs_Click(object? sender, EventArgs e) { L.Set(AppLang.Es); ApplyLanguage(); }
    private void mnuLangEn_Click(object? sender, EventArgs e) { L.Set(AppLang.En); ApplyLanguage(); }
    private void mnuThemeLight_Click(object? sender, EventArgs e) => ApplyTheme(dark: false);
    private void mnuThemeDark_Click(object? sender, EventArgs e) => ApplyTheme(dark: true);

    private void ApplyLanguage()
    {
        mnuTools.Text    = L.T("menu.tools");
        mnuVerify.Text   = L.T("menu.verify");
        mnuEject.Text    = L.T("menu.eject");
        mnuHistory.Text  = L.T("menu.history");
        mnuConfig.Text   = L.T("menu.config");
        mnuLang.Text     = L.T("menu.lang");
        mnuLangEs.Text   = L.T("menu.lang.es");
        mnuLangEn.Text   = L.T("menu.lang.en");
        mnuTheme.Text    = L.T("menu.theme");
        mnuThemeLight.Text = L.T("menu.theme.light");
        mnuThemeDark.Text  = L.T("menu.theme.dark");
        mnuPresets.Text  = L.T("menu.presets");
        mnuHelp.Text     = L.T("menu.help");
        mnuAbout.Text    = L.T("menu.about");

        lblDrive.Text       = L.T("drive.label");
        lblFileSystem.Text  = L.T("fs.label");
        lblAllocUnit.Text   = L.T("alloc.label");
        lblVolumeLabel.Text = L.T("label.label");
        grpOptions.Text     = L.T("options.group");
        chkQuickFormat.Text = L.T("opt.quick");
        chkCompress.Text    = L.T("opt.compress");
        chkSecureWipe.Text  = L.T("opt.secure");
        btnRestore.Text     = L.T("btn.restore");
        btnStart.Text       = L.T("btn.start");
        if (!_isBusy) btnClose.Text = L.T("btn.close");
        toolTip.SetToolTip(btnRefresh, L.T("tip.refresh"));

        mnuLangEs.Checked = L.Current == AppLang.Es;
        mnuLangEn.Checked = L.Current == AppLang.En;

        if (cboDrive.SelectedItem is DriveItem item)
        {
            UpdateHeader(item);
            UpdateInfo(item.Info);
            RenderHealth(_lastHealth);
        }
        else
        {
            lblHeaderSub.Text = L.T("app.subtitle");
            ClearInfo();
        }
        UpdateFsDescription();

        if (_isDriveProtected)
        {
            lblStatus.ForeColor = ProtectedColor();
            lblStatus.Text      = L.T("protected.status");
        }
    }

    private void ApplyTheme(bool dark)
    {
        _darkMode = dark;
        try
        {
#pragma warning disable WFO5001
            Application.SetColorMode(dark ? SystemColorMode.Dark : SystemColorMode.Classic);
#pragma warning restore WFO5001
        }
        catch { }

        _normalText = dark ? Color.Gainsboro : SystemColors.ControlText;
        pnlInfo.BackColor = dark ? Color.FromArgb(50, 52, 56) : Color.FromArgb(242, 248, 255);
        foreach (Control c in pnlInfo.Controls)
            c.ForeColor = dark ? Color.Gainsboro : SystemColors.ControlText;
        lblFsDesc.ForeColor = dark ? Color.DarkGray : Color.DimGray;
        lblElapsed.ForeColor = dark ? Color.DarkGray : Color.DimGray;

        mnuThemeLight.Checked = !dark;
        mnuThemeDark.Checked  = dark;

        lblStatus.ForeColor = _isDriveProtected ? ProtectedColor() : _normalText;
    }

    // ── Operation lifecycle / cancel ─────────────────────────────

    private void BeginOperation()
    {
        _isBusy = true;
        _cancelRequested = false;
        _cts = new CancellationTokenSource();
        SetFormEnabled(false);
        lblStatus.ForeColor = _normalText;
        lblElapsed.Text = "00:00";
        btnClose.Text = L.T("btn.cancel");
        _opStart = DateTime.Now;
        timerElapsed.Start();
    }

    private void EndOperation()
    {
        timerElapsed.Stop();
        _isBusy = false;
        _activeProcess?.Dispose();
        _activeProcess = null;
        _cts?.Dispose();
        _cts = null;
        SetFormEnabled(true);
        btnClose.Text = L.T("btn.close");
    }

    private void btnClose_Click(object sender, EventArgs e)
    {
        if (!_isBusy) { Close(); return; }

        if (MessageBox.Show(L.T("cancel.body"), L.T("cancel.title"),
                MessageBoxButtons.YesNo, MessageBoxIcon.Warning, MessageBoxDefaultButton.Button2) == DialogResult.Yes)
        {
            _cancelRequested = true;
            try { _cts?.Cancel(); } catch { }
            try { _activeProcess?.Kill(entireProcessTree: true); } catch { }
        }
    }

    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        if (_isBusy)
        {
            MessageBox.Show(L.T("closing.body"), L.T("closing.title"), MessageBoxButtons.OK, MessageBoxIcon.Warning);
            e.Cancel = true;
            return;
        }
        base.OnFormClosing(e);
    }

    // ── Timer ─────────────────────────────────────────────────────

    private void timerElapsed_Tick(object? sender, EventArgs e)
    {
        var elapsed = DateTime.Now - _opStart;
        lblElapsed.Text = elapsed.ToString(@"mm\:ss");
    }

    // ── Helpers ───────────────────────────────────────────────────

    /// <summary>True si <paramref name="letter"/> es la unidad que contiene Windows.</summary>
    private static bool IsSystemDrive(char letter)
    {
        char sys = Path.GetPathRoot(Environment.SystemDirectory)![0];
        return char.ToUpper(letter) == char.ToUpper(sys);
    }

    private void SetFormEnabled(bool enabled)
    {
        bool canFormat = enabled && !_isDriveProtected;

        menuStrip.Enabled      = enabled;
        cboDrive.Enabled       = enabled;
        btnRefresh.Enabled     = enabled;
        cboFileSystem.Enabled  = canFormat;
        cboAllocUnit.Enabled   = canFormat;
        txtLabel.Enabled       = canFormat;
        btnRestore.Enabled     = canFormat;
        btnStart.Enabled       = canFormat;
        chkQuickFormat.Enabled = canFormat;
        chkSecureWipe.Enabled  = canFormat;
        chkCompress.Enabled    = canFormat && cboFileSystem.SelectedItem?.ToString() == "NTFS";

        if (enabled && _isDriveProtected)
        {
            lblStatus.ForeColor = ProtectedColor();
            lblStatus.Text      = L.T("protected.status");
        }
    }

    private void cboDrive_DrawItem(object? sender, DrawItemEventArgs e)
    {
        if (e.Index < 0) return;

        var  item        = cboDrive.Items[e.Index] as DriveItem;
        bool isProtected = item?.IsProtected ?? false;

        e.DrawBackground();

        Color fore = isProtected
            ? (e.State.HasFlag(DrawItemState.Selected)
                ? Color.FromArgb(180, 180, 180)
                : Color.FromArgb(145, 145, 145))
            : (e.State.HasFlag(DrawItemState.Selected)
                ? SystemColors.HighlightText
                : SystemColors.ControlText);

        string text = item?.ToString() ?? "";
        using var brush = new SolidBrush(fore);
        e.Graphics.DrawString(text, e.Font ?? Font, brush, e.Bounds.X + 4, e.Bounds.Y + 2);
        e.DrawFocusRectangle();
    }

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

    private record DriveItem(char Letter, string Label, DriveInfo Info, bool IsProtected = false)
    {
        public override string ToString() => IsProtected ? $"{L.T("protected.tag")}{Label}" : Label;
    }

    private record AllocUnitItem(long Bytes)
    {
        public override string ToString() => Bytes >= 1024 * 1024
            ? $"{Bytes / (1024 * 1024)} MB"
            : Bytes >= 1024 ? $"{Bytes / 1024} KB" : $"{Bytes} bytes";
    }
}
