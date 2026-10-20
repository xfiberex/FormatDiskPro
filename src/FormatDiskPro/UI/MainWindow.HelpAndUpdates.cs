// SPDX-License-Identifier: GPL-3.0-only
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
namespace FormatDiskPro.UI;

/// <summary>
/// Menú Ayuda (historial, acerca de, licencia, avisos) y el flujo de actualización desde GitHub Releases.
///
/// <para>Parte de <see cref="MainWindow"/>: es la MISMA clase, repartida en archivos por
/// asunto (T2-08). No es un rediseño y no cambia comportamiento — el archivo único pasaba de
/// 2.000 líneas y encontrar algo en él era el problema.</para>
/// </summary>
public sealed partial class MainWindow
{
    private async void MnuHistory_Click(object sender, RoutedEventArgs e)
    {
        // Hasta T7-04 bastaba con que el menú entero se deshabilitara durante una operación. Ahora hay
        // un atajo (Ctrl+H) que llega aquí sin pasar por el menú, así que la guarda se hace explícita:
        // un ContentDialog modal encima de un formateo en marcha tapa el progreso y el botón de cancelar.
        if (_isBusy) return;

        var dlg = new HistoryDialog(_darkMode, WinRT.Interop.WindowNative.GetWindowHandle(this), _services.History)
        {
            XamlRoot = Content.XamlRoot,
            RequestedTheme = CurrentTheme,
        };
        await ShowOneAsync(dlg);
    }

    private async void MnuAbout_Click(object sender, RoutedEventArgs e)
    {
        var dlg = new AboutDialog(_services.Updates) { XamlRoot = Content.XamlRoot, RequestedTheme = CurrentTheme };
        await ShowOneAsync(dlg);
    }

    private async void MnuLicense_Click(object sender, RoutedEventArgs e)
    {
        var dlg = new LegalTextDialog(L.T("menu.license"), LegalText.License())
        {
            XamlRoot = Content.XamlRoot,
            RequestedTheme = CurrentTheme,
        };
        await ShowOneAsync(dlg);
    }

    private async void MnuThirdParty_Click(object sender, RoutedEventArgs e)
    {
        var dlg = new LegalTextDialog(L.T("menu.thirdParty"), LegalText.ThirdParty())
        {
            XamlRoot = Content.XamlRoot,
            RequestedTheme = CurrentTheme,
        };
        await ShowOneAsync(dlg);
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
    /// Muestra las notas de la versión instalada, que salen del CHANGELOG embebido: sin red (`AUD-067`).
    /// </summary>
    /// <remarks>
    /// Antes se pedían a la API de GitHub, también en el arranque que sigue a una actualización y aunque la
    /// comprobación automática estuviera desactivada. Ahora la única conexión de la app es buscar
    /// actualizaciones; «Ver en GitHub» solo abre el navegador si se pulsa.
    /// </remarks>
    private async Task ShowWhatsNewAsync()
    {
        string version = AppInfo.VersionString;
        var dlg = new WhatsNewDialog(
            version,
            Changelog.ForVersion(version),
            AppInfo.ReleasePageUrl(version),
            _services.Updates)
        {
            XamlRoot = Content.XamlRoot,
            RequestedTheme = CurrentTheme,
        };
        await ShowOneAsync(dlg);
    }

    private async Task CheckForUpdatesAsync(bool manual)
    {
        if (_isBusy) return;

        if (manual)
        {
            StatusText.ClearValue(TextBlock.ForegroundProperty);
            SetStatusAndAnnounce(L.T("update.checking"));
        }

        ReleaseInfo? rel;
        try { rel = await _services.Updates.CheckForUpdateAsync(); }
        catch (Exception ex)
        {
            _services.History.Log($"UPDATE CHECK ERROR: {ErrorText.Describe(ex)}");
            if (manual)
            {
                StatusText.Text = "";
                await ShowInfoAsync(L.T("menu.updates"), L.T("update.error", ErrorText.Describe(ex)));
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
            await ShowInfoAsync(L.T("update.availTitle"), L.T("update.noasset", rel.TagName));
            // Si el navegador no abre, se dice la dirección: mandar al usuario a una página que nunca
            // apareció, sin decirle cuál era, es peor que no ofrecerla.
            if (!_services.Updates.OpenUrl(rel.HtmlUrl))
                await ShowInfoAsync(L.T("menu.updates"), L.T("link.failed", rel.HtmlUrl));
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
            Text = L.T("update.availBody", rel.TagName, AppInfo.VersionString),
            TextWrapping = TextWrapping.Wrap,
        });

        string notes = ReleaseNotes.ToPlainText(rel.Notes);
        if (!string.IsNullOrWhiteSpace(notes))
        {
            // Semibold y el color de texto de siempre. Llevaba el acento, y por dos motivos deja de
            // llevarlo: el pincel salía de `Application.Current.Resources`, que resuelve con el tema de la
            // aplicación y no con el del elemento (`T13-17`), y un encabezado en color de acento es lo
            // mismo que se retiró de los títulos de tarjeta en `T13-15`.
            panel.Children.Add(new TextBlock
            {
                Text = L.T("update.changelog"),
                FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
            });
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
        return await ShowOneAsync(dlg) == ContentDialogResult.Primary;
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
            // Descargar, marcar el cierre y lanzar viven en UpdateInstallFlow, donde se prueban (`AUD-030`).
            var (outcome, error) = await UpdateInstallFlow.RunAsync(
                _services.Updates, _services.History, rel, progress,
                launching: () => StatusText.Text = L.T("update.launching"),
                setClosingForUpdate: closing => _closingForUpdate = closing,
                _cts!.Token);

            switch (outcome)
            {
                case UpdateInstallOutcome.Launched:
                    Application.Current.Exit();
                    break;
                case UpdateInstallOutcome.Cancelled:
                    FormatProgress.Value = 0;
                    StatusText.Text = L.T("status.cancelled");
                    break;
                default:
                    FormatProgress.Value = 0;
                    _lastOperationFailed = true;
                    StatusText.Text = "";
                    await ShowInfoAsync(L.T("menu.updates"), L.T("update.error", ErrorText.Describe(error!)));
                    break;
            }
        }
        finally
        {
            EndOperation();
        }
    }
}
