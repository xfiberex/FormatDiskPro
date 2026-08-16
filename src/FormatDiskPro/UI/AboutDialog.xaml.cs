using Microsoft.UI.Xaml.Controls;

namespace FormatDiskPro.UI;

/// <summary>
/// Diálogo "Acerca de" ampliado: descripción, versión, copyright/licencia (GPLv3), aviso de uso
/// destructivo (disclaimer) y aviso de privacidad. Incluye accesos a GitHub y, opcionalmente, a la
/// página de donaciones (las donaciones son voluntarias y nunca bloquean ninguna función).
/// </summary>
public sealed partial class AboutDialog : ContentDialog
{
    /// <param name="updates">Servicio que abre los enlaces externos (GitHub, donaciones).</param>
    public AboutDialog(IUpdateService updates)
    {
        InitializeComponent();

        Title                 = L.T("about.title");
        VersionText.Text      = L.T("about.version", AppInfo.VersionString);
        DescText.Text         = L.T("about.desc");
        CopyrightText.Text    = L.T("about.copyright");
        DisclaimerHeader.Text = L.T("about.disclaimerHeader");
        DisclaimerText.Text   = L.T("about.disclaimer");
        PrivacyHeader.Text    = L.T("about.privacyHeader");
        PrivacyText.Text      = L.T("about.privacy");

        SecondaryButtonText = L.T("about.github");
        CloseButtonText     = L.T("btn.close");
        if (!string.IsNullOrEmpty(AppInfo.DonateUrl))
            PrimaryButtonText = L.T("about.donate");

        // Abrir enlaces sin cerrar el diálogo (Cancel = true).
        PrimaryButtonClick   += (_, args) => { args.Cancel = true; updates.OpenUrl(AppInfo.DonateUrl); };
        SecondaryButtonClick += (_, args) => { args.Cancel = true; updates.OpenUrl(AppInfo.RepoUrl); };
    }
}
