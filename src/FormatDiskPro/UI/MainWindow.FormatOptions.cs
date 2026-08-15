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

/// <summary>
/// Tarjeta de configuración de formato: sistema de archivos, unidad de asignación, etiqueta, partición FAT32 pequeña (#37) y presets.
///
/// <para>Parte de <see cref="MainWindow"/>: es la MISMA clase, repartida en archivos por
/// asunto (T2-08). No es un rediseño y no cambia comportamiento — el archivo único pasaba de
/// 2.000 líneas y encontrar algo en él era el problema.
/// </summary>
public sealed partial class MainWindow
{
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
        if (bytes == 0 || bytes < FormatLogic.Fat32MaxBytes) FileSystemPicker.Items.Add("FAT32");
        if (bytes == 0 || bytes < 2L * 1024 * 1024 * 1024)  FileSystemPicker.Items.Add("FAT");

        int idx = previous is not null ? FileSystemPicker.Items.IndexOf(previous) : -1;
        if (idx >= 0) FileSystemPicker.SelectedIndex = idx;
        else          SuggestFileSystem(drive, bytes);

        UpdateSmallFat32Option(drive, bytes);
    }

    private void SuggestFileSystem(DriveInfo drive, long totalBytes)
    {
        string suggested = drive.DriveType == DriveType.Removable
            ? (totalBytes > FormatLogic.Fat32MaxBytes ? "exFAT" : "FAT32")
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
        // Un cambio de FS puede volver inválida la etiqueta ya escrita (p. ej. NTFS→FAT32 acorta el máximo).
        UpdateLabelHint();
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
        string key = fs is not null ? FsDescriptionKey(fs) : "";
        FsDescText.Text = key.Length > 0 ? L.T(key) : "";
    }

    private void UpdateCompressionOption()
    {
        bool isNtfs = FileSystemPicker.SelectedItem?.ToString() == "NTFS";
        CompressCheck.IsEnabled = isNtfs && !_isDriveProtected;
        if (!isNtfs) CompressCheck.IsChecked = false;
    }

    // ── Reinicializar: partición FAT32 pequeña en discos grandes (#37) ──

    // Visible solo en unidades extraíbles ≥ Fat32MaxBytes: exactamente donde FAT32 se oculta del picker.
    // Solo surte efecto vía "Reinicializar unidad…"; el flujo normal de Iniciar la ignora.
    private void UpdateSmallFat32Option(DriveInfo drive, long bytes)
    {
        DriveType type = DriveType.Unknown;
        try { type = drive.DriveType; } catch { }

        bool qualifies = type == DriveType.Removable && bytes >= FormatLogic.Fat32MaxBytes;
        SmallFat32Check.Visibility     = qualifies ? Visibility.Visible : Visibility.Collapsed;
        SmallFat32SizePanel.Visibility = qualifies ? Visibility.Visible : Visibility.Collapsed;
        if (!qualifies) SmallFat32Check.IsChecked = false;
        UpdateSmallFat32SizeEnabled();
        UpdateSmallFat32Hint();
    }

    private void SmallFat32Check_Toggled(object sender, RoutedEventArgs e)
    {
        UpdateSmallFat32SizeEnabled();
        UpdateSmallFat32Hint();
    }

    /// <summary>Sincroniza el selector de tamaño con la preferencia persistida (validada al conjunto
    /// permitido) y su estado. Mismo patrón que <see cref="InitWipePasses"/>.</summary>
    private void InitSmallFat32Size()
    {
        int idx = Array.IndexOf(ReinitPlan.AllowedSmallFat32SizesGb, ReinitPlan.NormalizeSmallFat32SizeGb(_settings.SmallFat32SizeGb));
        SmallFat32SizePicker.SelectedIndex = idx >= 0 ? idx : ReinitPlan.AllowedSmallFat32SizesGb.Length - 1;
        UpdateSmallFat32SizeEnabled();
    }

    /// <summary>Tamaño en GB elegido en la UI; 32 (el máximo) si no hay selección válida.</summary>
    private int SelectedSmallFat32SizeGb()
    {
        int idx = SmallFat32SizePicker.SelectedIndex;
        return idx >= 0 && idx < ReinitPlan.AllowedSmallFat32SizesGb.Length ? ReinitPlan.AllowedSmallFat32SizesGb[idx] : 32;
    }

    /// <summary>El selector de tamaño y el enlace de reinicializar solo están activos cuando la casilla
    /// de FAT32 pequeña está visible, habilitada y marcada.</summary>
    private void UpdateSmallFat32SizeEnabled()
    {
        bool on = SmallFat32Check.Visibility == Visibility.Visible && SmallFat32Check.IsEnabled && SmallFat32Check.IsChecked == true;
        SmallFat32SizePicker.IsEnabled = on;
        SmallFat32SizePanel.Opacity    = on ? 1.0 : 0.5;
        SmallFat32GoButton.IsEnabled   = on;
    }

    private void SmallFat32SizePicker_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (!_uiReady) return;   // selección programática durante la construcción
        _settings.SmallFat32SizeGb = SelectedSmallFat32SizeGb();
        _settings.Save();
        UpdateSmallFat32Hint();
    }

    private void UpdateSmallFat32Hint()
    {
        bool show = SmallFat32Check.Visibility == Visibility.Visible && SmallFat32Check.IsChecked == true;
        SmallFat32HintText.Visibility = show ? Visibility.Visible : Visibility.Collapsed;
        SmallFat32GoButton.Visibility = show ? Visibility.Visible : Visibility.Collapsed;
        if (show)
        {
            // {0} = límite real de Windows (fijo, 32 GB); {1} = tamaño elegido en el selector.
            long bytes = ReinitPlan.SmallFat32PartitionBytes(SelectedSmallFat32SizeGb());
            SmallFat32HintText.Text = L.T("opt.smallFat32Hint",
                FormatLogic.FormatBytes(FormatLogic.Fat32MaxBytes), FormatLogic.FormatBytes(bytes));
        }
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
        SmallFat32Check.IsChecked  = false;
        UpdateSmallFat32Hint();
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
        var item = new MenuFlyoutItem { Text = Presets.DisplayName(preset), Tag = preset };
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
            await ShowInfoAsync(L.T("msg.warning"), L.T("preset.na", Presets.DisplayName(preset)));
            return;
        }

        FileSystemPicker.SelectedIndex = idx;
        for (int i = 0; i < _allocBytes.Count; i++)
            if (_allocBytes[i] == preset.AllocationUnit) { AllocUnitPicker.SelectedIndex = i; break; }

        QuickFormatCheck.IsChecked = preset.QuickFormat;
        CompressCheck.IsChecked    = preset.Compress && preset.FileSystem == "NTFS";
        SecureWipeCheck.IsChecked  = preset.SecureWipe;

        StatusText.ClearValue(TextBlock.ForegroundProperty);
        StatusText.Text = L.T("preset.body", Presets.DisplayName(preset));
    }
}
