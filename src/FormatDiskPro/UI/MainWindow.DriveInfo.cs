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
/// Unidad seleccionada: salud S.M.A.R.T., protección de escritura, ocupación e información de la unidad.
///
/// <para>Parte de <see cref="MainWindow"/>: es la MISMA clase, repartida en archivos por
/// asunto (T2-08). No es un rediseño y no cambia comportamiento — el archivo único pasaba de
/// 2.000 líneas y encontrar algo en él era el problema.
/// </summary>
public sealed partial class MainWindow
{
    // ── Drive selection ───────────────────────────────────────────

    private void DrivePicker_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (DrivePicker.SelectedItem is not DriveViewModel item)
        {
            _isDriveProtected = false;
            _lastHealth = null;
            ClearInfo();
            SmallFat32Check.Visibility     = Visibility.Collapsed;
            SmallFat32Check.IsChecked      = false;
            SmallFat32SizePanel.Visibility = Visibility.Collapsed;
            UpdateSmallFat32Hint();
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
            InfoHealthText.ClearValue(TextBlock.ForegroundProperty);
            return;
        }
        // Un USB puede no reportar salud/bus/medio: mostrar el guion en vez de dejar el valor vacío
        // (línea "Salud:" sin nada) o un separador "·" huérfano en "Conexión:".
        string dash = L.T("info.dash");
        bool hasBus = !string.IsNullOrWhiteSpace(h.Bus), hasMedia = !string.IsNullOrWhiteSpace(h.Media);
        string conn = hasBus && hasMedia ? $"{h.Bus} · {h.Media}" : hasBus ? h.Bus : hasMedia ? h.Media : dash;
        InfoHealthText.Text = L.T("info.health", string.IsNullOrWhiteSpace(h.Health) ? dash : h.Health);
        InfoBusText.Text    = L.T("info.bus", conn);

        // Umbrales de #16: colorear el estado reportado (el texto ya transmite el estado; el color refuerza).
        var level = SmartInfo.HealthLevel(h.Health);
        if (level == SmartLevel.Unknown)
            InfoHealthText.ClearValue(TextBlock.ForegroundProperty);
        else
            InfoHealthText.Foreground = HealthDialog.LevelBrush(level, _darkMode);
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
            ProtectedBar.Message = L.T("protected.status");
            ProtectedBar.IsOpen  = true;
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
            ProtectedBar.IsOpen = false;
            StatusText.ClearValue(TextBlock.ForegroundProperty);
            StatusText.Text = "";
        }
        UpdateWipePassesEnabled();
    }

    // Una unidad protegida es la misma señal que una salud crítica, así que lleva el mismo color: se
    // reusa SeverityPalette en vez de repetir el RGB. Repetirlo es exactamente como entró un fallo de
    // contraste sin romper el build (ver el comentario de SeverityPalette.All).
    private Color ProtectedColor() => SeverityPalette.For(SmartLevel.Critical, _darkMode);

    private SolidColorBrush DriveBrush(bool isProtected) =>
        new(isProtected ? ProtectedColor() : SeverityPalette.Text(_darkMode));

    private void UpdateInfo(DriveInfo drive)
    {
        try
        {
            if (!drive.IsReady) { ClearInfo(); return; }
            long total = drive.TotalSize, free = drive.AvailableFreeSpace;
            InfoTotalText.Text = L.T("info.total", FormatBytes(total));
            InfoFreeText.Text  = L.T("info.free", FormatBytes(free));
            InfoFsText.Text    = L.T("info.fs", drive.DriveFormat);
            InfoTypeText.Text  = L.T("info.type", DriveTypeName(drive.DriveType));
            double usedPct = total > 0 ? (total - free) * 100.0 / total : 0;
            CapacityBar.Value      = usedPct;
            CapacityBar.Foreground = CapacityBrush(usedPct);
            CapacityBar.Visibility = Visibility.Visible;
        }
        catch { ClearInfo(); }
    }

    /// <summary>
    /// Pincel de la barra de OCUPACIÓN: neutro cuando hay espacio de sobra, ámbar al llenarse (≥80 %),
    /// rojo casi llena (≥90 %). Una barra de capacidad no debe usar el color de ACENTO del sistema
    /// (lo que hace un <c>ProgressBar</c> por defecto): en un equipo con acento rojo se veía roja con el
    /// disco medio vacío y leía como alarma. El color codifica cuánto queda, no la marca. Ámbar/rojo
    /// reusan <see cref="SeverityPalette"/> (theme-aware, contraste medido por tests); el neutro sigue el
    /// tema efectivo (<c>_darkMode</c>), y se re-deriva al cambiar de tema porque <see cref="UpdateInfo"/>
    /// vuelve a correr (ver <c>ApplyThemeMode</c>).
    /// </summary>
    private Brush CapacityBrush(double usedPct)
    {
        Color c = usedPct >= 90 ? SeverityPalette.For(SmartLevel.Critical, _darkMode)
                : usedPct >= 80 ? SeverityPalette.For(SmartLevel.Warning, _darkMode)
                : SeverityPalette.NeutralFill(_darkMode);
        return new SolidColorBrush(c);
    }

    private void ClearInfo()
    {
        InfoTotalText.Text  = L.T("info.total", L.T("info.dash"));
        InfoFreeText.Text   = L.T("info.free", L.T("info.dash"));
        InfoFsText.Text     = L.T("info.fs", L.T("info.dash"));
        InfoTypeText.Text   = L.T("info.type", L.T("info.dash"));
        InfoHealthText.Text = L.T("info.health", L.T("info.dash"));
        InfoBusText.Text    = L.T("info.bus", L.T("info.dash"));
        InfoHealthText.ClearValue(TextBlock.ForegroundProperty);
        CapacityBar.Visibility = Visibility.Collapsed;
    }
}
