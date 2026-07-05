using FlaUI.Core.AutomationElements;

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
            Assert.Equal("Start", MainWindowActions.Button(Window, "StartButton").Name);
        }
        finally
        {
            // Siempre se intenta volver a español, incluso si el Assert de arriba falló, para no
            // dejar el idioma cambiado de cara al resto de tests de esta corrida.
            MainWindowActions.ClickMenuPath(Window, "MnuConfig", "MnuLang", "MnuLangEs");
        }

        Assert.Equal("Iniciar", MainWindowActions.Button(Window, "StartButton").Name);
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
}
