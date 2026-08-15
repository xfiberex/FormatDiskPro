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
/// Menú Ayuda (historial, acerca de, licencia, avisos) y el flujo de actualización desde GitHub Releases.
///
/// <para>Parte de <see cref="MainWindow"/>: es la MISMA clase, repartida en archivos por
/// asunto (T2-08). No es un rediseño y no cambia comportamiento — el archivo único pasaba de
/// 2.000 líneas y encontrar algo en él era el problema.
/// </summary>
public sealed partial class MainWindow
{
    private async void MnuHistory_Click(object sender, RoutedEventArgs e)
    {
        var dlg = new HistoryDialog(_darkMode, WinRT.Interop.WindowNative.GetWindowHandle(this))
        {
            XamlRoot = Content.XamlRoot,
            RequestedTheme = CurrentTheme,
        };
        await dlg.ShowAsync();
    }

    private async void MnuAbout_Click(object sender, RoutedEventArgs e)
    {
        var dlg = new AboutDialog { XamlRoot = Content.XamlRoot, RequestedTheme = CurrentTheme };
        await dlg.ShowAsync();
    }

    private async void MnuLicense_Click(object sender, RoutedEventArgs e)
    {
        var dlg = new LegalTextDialog(L.T("menu.license"), LegalText.License())
        {
            XamlRoot = Content.XamlRoot,
            RequestedTheme = CurrentTheme,
        };
        await dlg.ShowAsync();
    }

    private async void MnuThirdParty_Click(object sender, RoutedEventArgs e)
    {
        var dlg = new LegalTextDialog(L.T("menu.thirdParty"), LegalText.ThirdParty())
        {
            XamlRoot = Content.XamlRoot,
            RequestedTheme = CurrentTheme,
        };
        await dlg.ShowAsync();
    }

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

        if (!await ShowUpdateAvailableAsync(rel))
            return;

        if (string.IsNullOrEmpty(rel.AssetUrl))
        {
            await ShowInfoAsync(L.T("update.availTitle"), L.T("update.noasset", rel.Version));
            UpdateService.OpenUrl(rel.HtmlUrl);
            return;
        }

        await DownloadAndRunUpdateAsync(rel);
    }

    /// <summary>
    /// Diálogo "Actualización disponible" que muestra el <b>changelog</b> de la nueva versión (cuerpo del
    /// release, ya incluido en <see cref="ReleaseInfo.Notes"/>, convertido con <see cref="ReleaseNotes.ToPlainText"/>)
    /// antes de descargar. Devuelve <c>true</c> si el usuario elige instalar.
    /// </summary>
    private async Task<bool> ShowUpdateAvailableAsync(ReleaseInfo rel)
    {
        var panel = new StackPanel { MaxWidth = 380, Spacing = 10 };
        panel.Children.Add(new TextBlock
        {
            Text = L.T("update.availBody", rel.Version, AppInfo.VersionString),
            TextWrapping = TextWrapping.Wrap,
        });

        string notes = ReleaseNotes.ToPlainText(rel.Notes);
        if (!string.IsNullOrWhiteSpace(notes))
        {
            var changelogLbl = new TextBlock
            {
                Text = L.T("update.changelog"),
                FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
            };
            if (Application.Current.Resources.TryGetValue("AccentTextFillColorPrimaryBrush", out var accent) && accent is Brush accentBrush)
                changelogLbl.Foreground = accentBrush;
            panel.Children.Add(changelogLbl);
            panel.Children.Add(new ScrollViewer
            {
                MaxHeight = 240,
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                HorizontalScrollMode = ScrollMode.Disabled,
                HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
                Content = new TextBlock { Text = notes, TextWrapping = TextWrapping.Wrap, FontSize = 13 },
            });
        }

        var dlg = new ContentDialog
        {
            Title = L.T("update.availTitle"),
            Content = panel,
            PrimaryButtonText = L.T("update.download"),
            CloseButtonText = L.T("update.later"),
            DefaultButton = ContentDialogButton.Primary,
            XamlRoot = Content.XamlRoot,
            RequestedTheme = CurrentTheme,
        };
        return await dlg.ShowAsync() == ContentDialogResult.Primary;
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
            _lastOperationFailed = true;
            StatusText.Text = "";
            History.Log($"UPDATE DOWNLOAD ERROR {rel.Version}: {ex.Message}");
            await ShowInfoAsync(L.T("menu.updates"), L.T("update.error", ex.Message));
        }
        finally
        {
            EndOperation();
        }
    }
}
