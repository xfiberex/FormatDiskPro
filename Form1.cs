using System.Diagnostics;
using System.Text;

namespace FormatDiskPro;

public partial class Form1 : Form
{
    private static readonly Dictionary<string, (long[] Sizes, long Default)> FsDefaults = new()
    {
        ["NTFS"]  = ([512, 1024, 2048, 4096, 8192, 16384, 32768, 65536], 4096),
        ["exFAT"] = ([4096, 8192, 16384, 32768, 65536, 131072, 262144, 524288, 1048576], 131072),
        ["ReFS"]  = ([4096, 65536], 65536),
        ["FAT32"] = ([512, 1024, 2048, 4096, 8192, 16384, 32768, 65536], 4096),
        ["FAT"]   = ([512, 1024, 2048, 4096, 8192, 16384, 32768], 4096),
    };

    private static readonly Dictionary<string, string> FsDescriptions = new()
    {
        ["NTFS"]  = "Ideal para discos internos Windows. Soporta archivos grandes, permisos y cifrado BitLocker.",
        ["exFAT"] = "Recomendado para memorias USB grandes (> 32 GB). Compatible con Windows, macOS y Linux sin límite de tamaño de archivo.",
        ["ReFS"]  = "Sistema resiliente a errores. Óptimo para almacenamiento de datos críticos. Requiere Windows Pro o superior.",
        ["FAT32"] = "Alta compatibilidad con dispositivos y consolas. Límite máximo de 4 GB por archivo.",
        ["FAT"]   = "Sistema heredado para unidades muy pequeñas (< 2 GB). Compatibilidad máxima con hardware antiguo.",
    };

    private bool _isFormatting;
    private bool _cancelRequested;
    private bool _isDriveProtected;
    private Process? _activeProcess;
    private DateTime _formatStart;

    public Form1()
    {
        InitializeComponent();
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
            bool isFixed = d.DriveType == DriveType.Fixed;
            cboDrive.Items.Add(new DriveItem(d.Name[0], label, d, isFixed));
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
            lblStatus.ForeColor    = Color.FromArgb(192, 0, 0);
            lblStatus.Text         = "⚠  Disco fijo protegido — el formateo está deshabilitado.";
        }
        else
        {
            cboFileSystem.Enabled  = true;
            cboAllocUnit.Enabled   = true;
            txtLabel.Enabled       = true;
            btnRestore.Enabled     = true;
            btnStart.Enabled       = true;
            chkQuickFormat.Enabled = true;
            chkCompress.Enabled    = cboFileSystem.SelectedItem?.ToString() == "NTFS";
            lblStatus.ForeColor    = SystemColors.ControlText;
            lblStatus.Text         = "";
        }
    }

    private void UpdateHeader(DriveItem? item)
    {
        if (item is null) { lblHeaderSub.Text = "Seleccione una unidad para formatear"; return; }
        try
        {
            string size = item.Info.IsReady ? FormatBytes(item.Info.TotalSize) : "–";
            lblHeaderSub.Text = $"{item} · {size} · {DriveTypeName(item.Info.DriveType)}";
        }
        catch { lblHeaderSub.Text = item.ToString(); }
    }

    private void UpdateInfo(DriveInfo drive)
    {
        try
        {
            if (!drive.IsReady) { ClearInfo(); return; }
            lblInfoTotal.Text = $"Total: {FormatBytes(drive.TotalSize)}";
            lblInfoFree.Text  = $"Libre: {FormatBytes(drive.AvailableFreeSpace)}";
            lblInfoFs.Text    = $"Sistema actual: {drive.DriveFormat}";
            lblInfoType.Text  = $"Tipo: {DriveTypeName(drive.DriveType)}";
        }
        catch { ClearInfo(); }
    }

    private void ClearInfo()
    {
        lblInfoTotal.Text = "Total: –";
        lblInfoFree.Text  = "Libre: –";
        lblInfoFs.Text    = "Sistema actual: –";
        lblInfoType.Text  = "Tipo: –";
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
        if (bytes == 0 || bytes < 2L * 1024 * 1024 * 1024)        cboFileSystem.Items.Add("FAT");

        int idx = previous is not null ? cboFileSystem.Items.IndexOf(previous) : -1;
        if (idx >= 0) cboFileSystem.SelectedIndex = idx;
        else          SuggestFileSystem(drive, bytes);
    }

    private void SuggestFileSystem(DriveInfo drive, long totalBytes)
    {
        // Auto-suggest: removable → exFAT (>32 GB) or FAT32 (≤32 GB), fixed → NTFS
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
        lblFsDesc.Text = fs is not null && FsDescriptions.TryGetValue(fs, out string? desc) ? desc : "";
    }

    private void UpdateCompressionOption()
    {
        bool isNtfs = cboFileSystem.SelectedItem?.ToString() == "NTFS";
        chkCompress.Enabled = isNtfs;
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
    }

    // ── Format ───────────────────────────────────────────────────

    private async void btnStart_Click(object sender, EventArgs e)
    {
        if (cboDrive.SelectedItem is not DriveItem driveItem)
        {
            MessageBox.Show("Seleccione una unidad.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }
        if (cboFileSystem.SelectedItem is null || cboAllocUnit.SelectedItem is null)
        {
            MessageBox.Show("Seleccione el sistema de archivos y el tamaño de unidad.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        char systemLetter = Path.GetPathRoot(Environment.SystemDirectory)![0];
        if (char.ToUpper(driveItem.Letter) == char.ToUpper(systemLetter))
        {
            MessageBox.Show("No se puede formatear la unidad que contiene Windows.",
                "Operación no permitida", MessageBoxButtons.OK, MessageBoxIcon.Error);
            return;
        }

        string fs        = cboFileSystem.SelectedItem.ToString()!;
        var    allocUnit = (AllocUnitItem)cboAllocUnit.SelectedItem!;
        string label     = txtLabel.Text.Trim();
        bool   quick     = chkQuickFormat.Checked;
        bool   compress  = chkCompress.Checked;

        bool driveReady;
        try { driveReady = driveItem.Info.IsReady; } catch { driveReady = false; }
        if (!driveReady)
        {
            MessageBox.Show($"La unidad {driveItem.Letter}: ya no está disponible. Actualice la lista.",
                "Unidad no disponible", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            LoadDrives();
            return;
        }

        if (!string.IsNullOrEmpty(label))
        {
            char[] invalidChars = ['\\', '/', ':', '*', '?', '"', '<', '>', '|'];
            if (label.Any(c => invalidChars.Contains(c)))
            {
                MessageBox.Show("La etiqueta contiene caracteres no válidos:\n\\ / : * ? \" < > |",
                    "Etiqueta inválida", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                txtLabel.Focus();
                return;
            }
        }

        string warning =
            $"ADVERTENCIA: Se destruirán TODOS los datos en:\n\n" +
            $"  Unidad:   {driveItem}\n" +
            $"  Sistema:  {fs}\n" +
            $"  Cluster:  {allocUnit}\n" +
            $"  Etiqueta: {(string.IsNullOrEmpty(label) ? "(sin etiqueta)" : label)}\n" +
            $"  Tipo:     {(quick ? "Formato rápido" : "Formato completo")}\n\n" +
            "¿Desea continuar?";

        if (MessageBox.Show(warning, "Confirmar formato", MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning, MessageBoxDefaultButton.Button2) != DialogResult.Yes)
            return;

        await RunFormatAsync(driveItem.Letter, fs, allocUnit.Bytes, label, quick, compress);
    }

    private async Task RunFormatAsync(
        char driveLetter, string fs, long allocBytes,
        string label, bool quickFormat, bool compress)
    {
        _isFormatting    = true;
        _cancelRequested = false;
        SetFormEnabled(false);

        progressBar.Style = ProgressBarStyle.Marquee;
        lblStatus.Text    = $"Formateando {driveLetter}: ({(quickFormat ? "rápido" : "completo")})...";
        lblElapsed.Text   = "00:00";
        btnClose.Text     = "Cancelar";

        _formatStart = DateTime.Now;
        timerElapsed.Start();

        try
        {
            string args = BuildEncodedCommand(driveLetter, fs, allocBytes, label, quickFormat, compress);

            var psi = new ProcessStartInfo
            {
                FileName              = "powershell.exe",
                Arguments             = args,
                UseShellExecute       = false,
                RedirectStandardOutput = true,
                RedirectStandardError  = true,
                CreateNoWindow        = true,
            };

            _activeProcess = new Process { StartInfo = psi };
            _activeProcess.Start();

            var outTask = _activeProcess.StandardOutput.ReadToEndAsync();
            var errTask = _activeProcess.StandardError.ReadToEndAsync();

            await _activeProcess.WaitForExitAsync();

            string stdout = await outTask;
            string stderr = await errTask;
            int    code   = _activeProcess.ExitCode;

            progressBar.Style = ProgressBarStyle.Blocks;

            if (_cancelRequested)
            {
                progressBar.Value = 0;
                lblStatus.Text    = "Formato cancelado.";
                return;
            }

            if (code == 0)
            {
                progressBar.Value = 100;
                lblStatus.Text    = "Formato completado con éxito.";
                MessageBox.Show($"La unidad {driveLetter}: se formateó correctamente con {fs}.",
                    "Éxito", MessageBoxButtons.OK, MessageBoxIcon.Information);
                LoadDrives();
            }
            else
            {
                progressBar.Value = 0;
                lblStatus.Text    = "Error durante el formato.";
                string msg = (!string.IsNullOrWhiteSpace(stderr) ? stderr : stdout).Trim();
                MessageBox.Show($"Error al formatear la unidad {driveLetter}:\n\n{msg}",
                    "Error de formato", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        catch (Exception ex)
        {
            progressBar.Style = ProgressBarStyle.Blocks;
            progressBar.Value = 0;
            lblStatus.Text    = _cancelRequested ? "Formato cancelado." : "Error inesperado.";
            if (!_cancelRequested)
                MessageBox.Show($"Error inesperado:\n{ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally
        {
            timerElapsed.Stop();
            _isFormatting = false;
            _activeProcess?.Dispose();
            _activeProcess = null;
            SetFormEnabled(true);
            btnClose.Text = "Cerrar";
        }
    }

    private static string BuildEncodedCommand(
        char driveLetter, string fs, long allocBytes,
        string label, bool quickFormat, bool compress)
    {
        var ps = new StringBuilder("Format-Volume");
        ps.Append($" -DriveLetter {driveLetter}");
        ps.Append($" -FileSystem {fs}");
        ps.Append($" -AllocationUnitSize {allocBytes}");
        if (!string.IsNullOrEmpty(label)) ps.Append($" -NewFileSystemLabel '{label.Replace("'", "''")}'");
        if (!quickFormat)                 ps.Append(" -Full");
        if (compress && fs == "NTFS")     ps.Append(" -Compress");
        ps.Append(" -Confirm:$false -Force");
        byte[] encoded = Encoding.Unicode.GetBytes(ps.ToString());
        return $"-NonInteractive -NoProfile -EncodedCommand {Convert.ToBase64String(encoded)}";
    }

    // ── Cancel ───────────────────────────────────────────────────

    private void btnClose_Click(object sender, EventArgs e)
    {
        if (!_isFormatting) { Close(); return; }

        if (MessageBox.Show(
                "¿Cancelar el formato en curso?\n\nNota: la unidad puede quedar en un estado no utilizable.",
                "Cancelar formato", MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning, MessageBoxDefaultButton.Button2) == DialogResult.Yes)
        {
            _cancelRequested = true;
            try { _activeProcess?.Kill(entireProcessTree: true); } catch { }
        }
    }

    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        if (_isFormatting)
        {
            MessageBox.Show("Utilice el botón Cancelar para detener el formato.",
                "Formato en progreso", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            e.Cancel = true;
            return;
        }
        base.OnFormClosing(e);
    }

    // ── Timer ─────────────────────────────────────────────────────

    private void timerElapsed_Tick(object? sender, EventArgs e)
    {
        var elapsed = DateTime.Now - _formatStart;
        lblElapsed.Text = elapsed.ToString(@"mm\:ss");
    }

    // ── Helpers ───────────────────────────────────────────────────

    private void SetFormEnabled(bool enabled)
    {
        bool canFormat = enabled && !_isDriveProtected;

        cboDrive.Enabled       = enabled;
        btnRefresh.Enabled     = enabled;
        cboFileSystem.Enabled  = canFormat;
        cboAllocUnit.Enabled   = canFormat;
        txtLabel.Enabled       = canFormat;
        btnRestore.Enabled     = canFormat;
        btnStart.Enabled       = canFormat;
        chkQuickFormat.Enabled = canFormat;
        chkCompress.Enabled    = canFormat && cboFileSystem.SelectedItem?.ToString() == "NTFS";

        // Restore protection message once the format operation ends
        if (enabled && _isDriveProtected)
        {
            lblStatus.ForeColor = Color.FromArgb(192, 0, 0);
            lblStatus.Text      = "⚠  Disco fijo protegido — el formateo está deshabilitado.";
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

    private static string FormatBytes(long bytes)
    {
        string[] u = ["B", "KB", "MB", "GB", "TB"];
        double v = bytes; int i = 0;
        while (v >= 1024 && i < u.Length - 1) { v /= 1024; i++; }
        return $"{v:F1} {u[i]}";
    }

    private static string DriveTypeName(DriveType t) => t switch
    {
        DriveType.Fixed    => "Disco fijo",
        DriveType.Removable => "USB / Removible",
        DriveType.Ram      => "Disco RAM",
        DriveType.Network  => "Red",
        DriveType.CDRom    => "CD/DVD",
        _                  => "Desconocido",
    };

    private record DriveItem(char Letter, string Label, DriveInfo Info, bool IsProtected = false)
    {
        public override string ToString() => IsProtected ? $"[Protegido] {Label}" : Label;
    }

    private record AllocUnitItem(long Bytes)
    {
        public override string ToString() => Bytes >= 1024 * 1024
            ? $"{Bytes / (1024 * 1024)} MB"
            : Bytes >= 1024 ? $"{Bytes / 1024} KB" : $"{Bytes} bytes";
    }
}
