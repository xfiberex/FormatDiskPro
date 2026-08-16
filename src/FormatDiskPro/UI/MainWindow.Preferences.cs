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
/// Preferencias del usuario: idioma, tema, aviso al terminar y pasadas del borrado seguro. Todas persisten en AppSettings.
///
/// <para>Parte de <see cref="MainWindow"/>: es la MISMA clase, repartida en archivos por
/// asunto (T2-08). No es un rediseño y no cambia comportamiento — el archivo único pasaba de
/// 2.000 líneas y encontrar algo en él era el problema.
/// </summary>
public sealed partial class MainWindow
{
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

    /// <summary>Primera letra (mayúscula) de un texto, para el acelerador Alt del menú; "" si vacío.</summary>
    private static string FirstLetter(string s) =>
        string.IsNullOrEmpty(s) ? "" : s.Substring(0, 1).ToUpperInvariant();

    private void MnuNotify_Click(object sender, RoutedEventArgs e)
    {
        _settings.NotifyOnFinish = MnuNotify.IsChecked;
        _settings.Save();
    }

    // ── Secure-wipe passes (#14) ──────────────────────────────────

    /// <summary>Sincroniza el selector de pasadas con la preferencia persistida (validada a 1/3/7) y su estado.</summary>
    private void InitWipePasses()
    {
        int idx = Array.IndexOf(SecureWipe.AllowedPasses, SecureWipe.NormalizePasses(_settings.SecureWipePasses));
        WipePassesPicker.SelectedIndex = idx >= 0 ? idx : 0;
        UpdateWipePassesEnabled();
    }

    /// <summary>Pasadas seleccionadas en la UI (1/3/7); 1 si no hay selección válida.</summary>
    private int SelectedWipePasses()
    {
        int idx = WipePassesPicker.SelectedIndex;
        return idx >= 0 && idx < SecureWipe.AllowedPasses.Length ? SecureWipe.AllowedPasses[idx] : 1;
    }

    /// <summary>El selector de pasadas solo está activo cuando el borrado seguro está habilitado y marcado.</summary>
    private void UpdateWipePassesEnabled()
    {
        bool on = SecureWipeCheck.IsEnabled && SecureWipeCheck.IsChecked == true;
        WipePassesPicker.IsEnabled = on;
        WipePassesPanel.Opacity = on ? 1.0 : 0.5;
    }

    private void SecureWipeCheck_Toggled(object sender, RoutedEventArgs e) => UpdateWipePassesEnabled();

    private void WipePassesPicker_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (!_uiReady) return;   // selección programática durante la construcción
        _settings.SecureWipePasses = SelectedWipePasses();
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
        // Aceleradores de teclado del menú (Alt + primera letra del título localizado).
        MnuTools.AccessKey  = FirstLetter(L.T("menu.tools"));
        MnuConfig.AccessKey = FirstLetter(L.T("menu.config"));
        MnuHelp.AccessKey   = FirstLetter(L.T("menu.help"));
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
        MnuLicense.Text  = L.T("menu.license");
        MnuThirdParty.Text = L.T("menu.thirdParty");
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
        WipePassesLbl.Text       = L.T("opt.passes");
        SmallFat32Check.Content     = L.T("opt.smallFat32");
        SmallFat32SizeLbl.Text      = L.T("opt.smallFat32Size");
        SmallFat32GoButton.Content  = L.T("opt.smallFat32Go");
        RestLbl.Text                = L.T("opt.rest");
        RestFsLbl.Text              = L.T("opt.restFs");
        RestLabelLbl.Text           = L.T("opt.restLabel");
        // Los items de RestPicker son texto traducido: hay que repoblarlos al cambiar de idioma, no solo
        // al construir. La preferencia persistida manda, así que la selección sobrevive al cambio.
        InitRestPickers();
        UpdateSmallFat32Hint();
        RestoreButton.Content    = L.T("btn.restore");
        StartButton.Content      = L.T("btn.start");
        if (!_isBusy) CloseButton.Content = L.T("btn.close");
        RefreshTooltip.Content   = L.T("tip.refresh");
        // Solo visible cuando no hay unidades elegibles (sin selección): explica el estado vacío.
        DrivePicker.PlaceholderText = L.T("drive.none");
        AutomationProperties.SetName(RefreshButton, L.T("tip.refresh"));
        AutomationProperties.SetName(WipePassesPicker, L.T("opt.passes"));
        AutomationProperties.SetName(SmallFat32SizePicker, L.T("opt.smallFat32Size"));
        AutomationProperties.SetName(RestPicker,   L.T("opt.rest"));
        AutomationProperties.SetName(RestFsPicker, L.T("opt.restFs"));
        AutomationProperties.SetName(RestLabelBox, L.T("opt.restLabel"));
        AutomationProperties.SetName(CapacityBar, L.T("info.used"));
        ToolTipService.SetToolTip(CapacityBar, L.T("info.used"));
        UpdateLabelHint();   // refresca el hint visible (si lo hay) al cambiar de idioma

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
            ProtectedBar.Message = L.T("protected.status");
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

        // Re-derivar el color de la línea «Salud:» con la paleta del tema efectivo.
        if (_lastHealth is not null) RenderHealth(_lastHealth);
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
}
