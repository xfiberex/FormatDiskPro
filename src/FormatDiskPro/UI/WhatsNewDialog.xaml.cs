using Microsoft.UI.Xaml.Controls;

namespace FormatDiskPro.UI;

/// <summary>
/// Diálogo de novedades: muestra las notas de la versión instalada (cuerpo del release de GitHub,
/// convertido a texto plano por <see cref="ReleaseNotes.ToPlainText"/>). Se abre automáticamente una
/// sola vez tras una actualización y también bajo demanda desde <em>Ayuda → Novedades…</em>.
/// </summary>
public sealed partial class WhatsNewDialog : ContentDialog
{
    private readonly string _url;

    /// <summary>Crea el diálogo para una versión, sus notas Markdown y la URL del release.</summary>
    /// <param name="version">Versión legible a mostrar (p. ej. "1.7.0").</param>
    /// <param name="notesMarkdown">Cuerpo Markdown de las notas (puede venir vacío).</param>
    /// <param name="url">URL del release en GitHub (botón "Ver en GitHub").</param>
    /// <param name="updates">Servicio que abre la URL en el navegador.</param>
    public WhatsNewDialog(string version, string notesMarkdown, string url, IUpdateService updates)
    {
        InitializeComponent();
        _url = url;

        Title             = L.T("whatsnew.title");
        VersionText.Text  = L.T("whatsnew.version", version);
        PrimaryButtonText = L.T("whatsnew.viewOnGitHub");
        CloseButtonText   = L.T("btn.close");
        DefaultButton     = ContentDialogButton.Close;

        string plain = ReleaseNotes.ToPlainText(notesMarkdown);
        NotesText.Text = string.IsNullOrWhiteSpace(plain) ? L.T("whatsnew.empty") : plain;

        // Si el navegador abre, el diálogo se cierra —que es lo que se espera de «Ver en GitHub»—. Solo
        // se queda abierto cuando NO abre, y entonces para algo: enseñar la dirección, que antes se
        // perdía en un catch vacío junto con cualquier señal de que el botón había hecho algo.
        PrimaryButtonClick += (_, args) =>
        {
            LinkBar.IsOpen = false;
            if (updates.OpenUrl(_url)) return;
            args.Cancel     = true;
            LinkBar.Message = L.T("link.failed", _url);
            LinkBar.IsOpen  = true;
        };
    }
}
