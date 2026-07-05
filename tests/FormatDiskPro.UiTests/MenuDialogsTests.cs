using FlaUI.Core.AutomationElements;
using FlaUI.Core.Definitions;

namespace FormatDiskPro.UiTests;

/// <summary>
/// Cubre los diálogos estáticos/de solo lectura accesibles desde los menús Ayuda/Herramientas/
/// Configuración — ninguno de estos tests toca una unidad de forma destructiva.
/// Cada test envuelve su diálogo en try/finally con <see cref="DialogHelper.SafeCloseAnyDialog"/>:
/// un assert fallido nunca debe dejar un ContentDialog abierto (WinUI solo permite uno a la vez; el
/// siguiente test intentaría abrir un segundo y tumbaría el proceso entero).
/// </summary>
[Collection(AppCollection.Name)]
public sealed class MenuDialogsTests(AppFixture fixture)
{
    private Window Window => fixture.MainWindow;

    [Fact]
    public void AboutDialog_OpensWithVersionAndCloses()
    {
        MainWindowActions.ClickMenuPath(Window, "MnuHelp", "MnuAbout");
        var dialog = DialogHelper.WaitForDialog(fixture);
        try
        {
            var versionText = DialogHelper.WaitForChild(dialog, "VersionText");
            Assert.False(string.IsNullOrWhiteSpace(DialogHelper.ReadText(versionText)));
        }
        finally
        {
            DialogHelper.SafeCloseAnyDialog(fixture);
        }
    }

    [Fact]
    public void LicenseDialog_OpensWithGplTextAndCloses()
    {
        MainWindowActions.ClickMenuPath(Window, "MnuHelp", "MnuLicense");
        var dialog = DialogHelper.WaitForDialog(fixture);
        try
        {
            var bodyText = DialogHelper.WaitForChild(dialog, "BodyText");
            Assert.Contains("GNU GENERAL PUBLIC LICENSE", DialogHelper.ReadText(bodyText), StringComparison.OrdinalIgnoreCase);
        }
        finally
        {
            DialogHelper.SafeCloseAnyDialog(fixture);
        }
    }

    [Fact]
    public void ThirdPartyDialog_OpensAndCloses()
    {
        MainWindowActions.ClickMenuPath(Window, "MnuHelp", "MnuThirdParty");
        var dialog = DialogHelper.WaitForDialog(fixture);
        try
        {
            var bodyText = DialogHelper.WaitForChild(dialog, "BodyText");
            Assert.False(string.IsNullOrWhiteSpace(DialogHelper.ReadText(bodyText)));
        }
        finally
        {
            DialogHelper.SafeCloseAnyDialog(fixture);
        }
    }

    [Fact]
    public void WhatsNewDialog_OpensAndCloses()
    {
        MainWindowActions.ClickMenuPath(Window, "MnuHelp", "MnuWhatsNew");
        var dialog = DialogHelper.WaitForDialog(fixture);
        try
        {
            DialogHelper.WaitForChild(dialog, "VersionText");
        }
        finally
        {
            DialogHelper.SafeCloseAnyDialog(fixture);
        }
    }

    [Fact]
    public void HistoryDialog_OpensAndCloses()
    {
        MainWindowActions.ClickMenuPath(Window, "MnuTools", "MnuHistory");
        var dialog = DialogHelper.WaitForDialog(fixture);
        try
        {
            DialogHelper.WaitForChild(dialog, "SearchBox");
        }
        finally
        {
            DialogHelper.SafeCloseAnyDialog(fixture);
        }
    }

    [Fact]
    public void PresetsDialog_OpensAndCloses()
    {
        MainWindowActions.ClickMenuPath(Window, "MnuConfig", "MnuPresets");

        // MnuPresets es un MenuFlyoutSubItem reconstruido en runtime (BuildPresetsMenu): su último
        // ítem, tras un separador, es "Gestionar presets…" (localización "menu.managePresets") — no
        // tiene AutomationId propio porque se crea por código. Se localiza por Name exacto (español,
        // idioma por defecto de estas pruebas) en vez de "el último MenuItem visible": esa posición
        // es frágil si hay más elementos de menú abiertos/cerrándose al mismo tiempo. Buscamos desde
        // Window (no desde el propio MnuPresets): su submenú es otro Popup de WinUI y, como con
        // ComboBox/ContentDialog, sus hijos no siempre aparecen como descendientes del elemento que
        // abre el popup.
        var manageItemResult = FlaUI.Core.Tools.Retry.WhileNull(
            () => Window.FindAllDescendants(cf => cf.ByControlType(ControlType.MenuItem))
                .FirstOrDefault(mi => mi.Name == "Gestionar presets…"),
            timeout: TimeSpan.FromSeconds(5),
            interval: TimeSpan.FromMilliseconds(200),
            ignoreException: true);
        Assert.NotNull(manageItemResult.Result);
        manageItemResult.Result!.Patterns.Invoke.Pattern.Invoke();

        // WaitForDialog (heurístico "más hijos") puede confundirse con el MenuFlyout de MnuPresets si
        // no ha terminado de cerrarse; se exige explícitamente contenido propio de PresetsDialog.
        var dialog = DialogHelper.WaitForDialogContaining(fixture, "SaveHeader");
        try
        {
            DialogHelper.WaitForChild(dialog, "ListHeader");
        }
        finally
        {
            DialogHelper.SafeCloseAnyDialog(fixture);
        }
    }

    /// <summary>
    /// El diálogo de modo de chkdsk se cancela sin ejecutar nada: cubre la navegación de menú y el
    /// propio diálogo sin correr chkdsk contra la unidad que esté seleccionada en ese momento.
    /// </summary>
    [Fact]
    public void CheckDiskModeDialog_CancelDoesNotRun()
    {
        MainWindowActions.ClickMenuPath(Window, "MnuTools", "MnuCheck");
        DialogHelper.WaitForDialog(fixture);
        DialogHelper.SafeCloseAnyDialog(fixture);
    }
}
