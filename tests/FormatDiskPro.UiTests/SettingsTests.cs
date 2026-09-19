using FlaUI.Core.AutomationElements;
using FlaUI.Core.Input;
using FlaUI.Core.WindowsAPI;

namespace FormatDiskPro.UiTests;

/// <summary>
/// Idioma y tema — sin tocar ninguna unidad. Cada test deja la app en el estado con el que la
/// encontró (español / automático) para no afectar a otros tests que dependen de texto en español
/// (p. ej. <c>SettingsBackup</c> ya protege el settings.json real del usuario aparte de esto).
/// </summary>
[Collection(AppCollection.Name)]
public sealed class SettingsTests(AppFixture fixture)
{
    private Window Window => fixture.MainWindow;

    [Fact]
    public void LanguageSwitch_UpdatesUiText_ThenRestoresSpanish()
    {
        try
        {
            MainWindowActions.ClickMenuPath(Window, "MnuConfig", "MnuLang", "MnuLangEn");
            // StartsWith y no Equal: desde `T12-02` el botón nombra la unidad seleccionada («Format H:»),
            // y cuál sea depende de la máquina donde corra esta prueba. Lo que se comprueba aquí es que
            // el texto siguió al idioma, no cuál es la unidad.
            Assert.StartsWith("Format", MainWindowActions.Button(Window, "StartButton").Name);
        }
        finally
        {
            // Siempre se intenta volver a español, incluso si el Assert de arriba falló, para no
            // dejar el idioma cambiado de cara al resto de tests de esta corrida.
            MainWindowActions.ClickMenuPath(Window, "MnuConfig", "MnuLang", "MnuLangEs");
        }

        Assert.StartsWith("Formatear", MainWindowActions.Button(Window, "StartButton").Name);
    }

    [Fact]
    public void ThemeSwitch_DoesNotBreakUi_ThenRestoresAuto()
    {
        // StartButton.IsEnabled NO sirve de señal aquí: depende de si la unidad seleccionada en ese
        // momento es la protegida (sin relación con el tema). DrivePicker, en cambio, siempre está
        // habilitado y presente — sirve para comprobar que la ventana sigue viva y respondiendo.
        try
        {
            MainWindowActions.ClickMenuPath(Window, "MnuConfig", "MnuTheme", "MnuThemeDark");
            Assert.NotNull(MainWindowActions.DrivePicker(Window));

            MainWindowActions.ClickMenuPath(Window, "MnuConfig", "MnuTheme", "MnuThemeLight");
            Assert.NotNull(MainWindowActions.DrivePicker(Window));
        }
        finally
        {
            MainWindowActions.ClickMenuPath(Window, "MnuConfig", "MnuTheme", "MnuThemeAuto");
        }
    }

    /// <summary>
    /// Idioma y tema son dos elecciones de «uno de N», y la exclusión la hace el control: son
    /// <c>RadioMenuFlyoutItem</c>, cada submenú con su <c>GroupName</c>, así que el código marca solo el
    /// activo (`T13-13`). Se elige inglés y tema oscuro, y después se abren los dos submenús: tiene que
    /// quedar marcado exactamente uno en cada uno, y los elegidos.
    /// </summary>
    /// <remarks>
    /// <para>Lo que cubre: con <c>ToggleMenuFlyoutItem</c> (lo de antes) y el marcado de uno solo, el
    /// «Automático» de partida se queda marcado junto al oscuro —visto al revertir—. El <c>GroupName</c>,
    /// en cambio, no hace falta para que funcione: quitándolo de los ocho ítems la prueba sigue pasando,
    /// porque WinUI acota el grupo por defecto a cada flyout. Está puesto por ser explícito.</para>
    /// <para>Lo que <b>no</b> cambia en la 1.8 es el árbol de automatización: el peer de
    /// <c>RadioMenuFlyoutItem</c> sigue siendo <c>MenuItem</c> con patrón <i>Toggle</i>, igual que el del
    /// <c>ToggleMenuFlyoutItem</c> —comprobado con la app en marcha—. Lo que cambia para quien usa la app
    /// es el punto de opción en vez de la marca de verificación, y eso se ve en la galería.</para>
    /// </remarks>
    [Fact]
    public void TheChoiceMenus_LeaveExactlyOneMarkedInEachGroup()
    {
        string[] langIds  = ["MnuLangEs", "MnuLangEn", "MnuLangPt", "MnuLangFr", "MnuLangIt"];
        string[] themeIds = ["MnuThemeAuto", "MnuThemeLight", "MnuThemeDark"];
        try
        {
            MainWindowActions.ClickMenuPath(Window, "MnuConfig", "MnuLang", "MnuLangEn");
            MainWindowActions.ClickMenuPath(Window, "MnuConfig", "MnuTheme", "MnuThemeDark");

            Assert.Equal(["MnuLangEn"], MarkedItems("MnuLang", langIds));
            Assert.Equal(["MnuThemeDark"], MarkedItems("MnuTheme", themeIds));
        }
        finally
        {
            MainWindowActions.ClickMenuPath(Window, "MnuConfig", "MnuLang", "MnuLangEs");
            MainWindowActions.ClickMenuPath(Window, "MnuConfig", "MnuTheme", "MnuThemeAuto");
        }
    }

    /// <summary>
    /// Abre un submenú y devuelve cuáles de sus ítems están marcados. Cierra el menú al salir: dejarlo
    /// desplegado taparía la ventana para el resto de la corrida.
    /// </summary>
    private List<string> MarkedItems(string submenuId, string[] itemIds)
    {
        try
        {
            MainWindowActions.ClickMenuPath(Window, "MnuConfig", submenuId);

            var marked = new List<string>();
            foreach (string id in itemIds)
            {
                var item = Window.FindFirstDescendant(cf => cf.ByAutomationId(id))
                    ?? throw new InvalidOperationException($"No se encontró el ítem de menú '{id}'.");
                if (item.Patterns.Toggle.Pattern.ToggleState == FlaUI.Core.Definitions.ToggleState.On)
                    marked.Add(id);
            }
            return marked;
        }
        finally
        {
            // Dos Esc: uno cierra el submenú y otro la barra de menús.
            Keyboard.Press(VirtualKeyShort.ESCAPE);
            Thread.Sleep(150);
            Keyboard.Press(VirtualKeyShort.ESCAPE);
            Thread.Sleep(150);
        }
    }
}
