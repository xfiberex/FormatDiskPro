using FlaUI.Core.AutomationElements;
using FlaUI.Core.Definitions;

namespace FormatDiskPro.UiTests;

/// <summary>
/// Cubre los controles de la tarjeta "ConfiguraciÃ³n de formato"/"Opciones de formato" sin tocar
/// ninguna unidad real: son cambios puramente de UI (nunca se pulsa Iniciar/Reinicializar aquÃ­).
/// </summary>
[Collection(AppCollection.Name)]
public sealed class FormatOptionsUiTests
{
    private Window Window { get; }

    public FormatOptionsUiTests(AppFixture fixture)
    {
        Window = fixture.MainWindow;
        // La unidad seleccionada al arrancar (o la que haya dejado otro test) puede ser la de
        // sistema, protegida: SetControlsEnabled deshabilita casi todos los controles de esta tarjeta
        // sobre ella. Estos tests son de UI pura (nunca pulsan Iniciar), asÃ­ que cualquier otra
        // unidad sirve.
        MainWindowActions.SelectAnyNonSystemDrive(Window);
    }

    [NonSystemDriveFact]
    public void VolumeLabelBox_RoundTripsText()
    {
        var box = MainWindowActions.TextBox(Window, "VolumeLabelBox");
        string original = box.Text;
        try
        {
            box.Text = "UITEST";
            Assert.Equal("UITEST", box.Text);
        }
        finally
        {
            box.Text = original;
        }
    }

    [NonSystemDriveFact]
    public void SecureWipeCheck_TogglesWipePassesPicker()
    {
        MainWindowActions.SetChecked(Window, "SecureWipeCheck", false);
        var picker = MainWindowActions.Require(Window, "WipePassesPicker");
        Assert.False(picker.IsEnabled);

        MainWindowActions.SetChecked(Window, "SecureWipeCheck", true);
        Assert.True(picker.IsEnabled);

        MainWindowActions.SetChecked(Window, "SecureWipeCheck", false);
        Assert.False(picker.IsEnabled);
    }

    /// <summary>
    /// El resumen del pie cambia en cuanto se marca el borrado seguro (`T13-05`).
    /// </summary>
    /// <remarks>
    /// Es la opción que convierte segundos en horas, y la única del resumen que no lo repintaba: la
    /// casilla solo avisaba a <c>SecureWipeCheck_Toggled</c>, y el pie seguía diciendo «rápido» hasta
    /// que cambiara otra opción. Se compara el texto antes y después, sin anclar el idioma.
    /// </remarks>
    [NonSystemDriveFact]
    public void FooterSummary_ChangesWhenSecureWipeIsToggled()
    {
        MainWindowActions.SetChecked(Window, "SecureWipeCheck", false);
        var summary = MainWindowActions.Require(Window, "FormatSummaryText");
        string without = summary.Name;

        try
        {
            MainWindowActions.SetChecked(Window, "SecureWipeCheck", true);
            string with = summary.Name;

            Assert.False(with == without,
                $"El pie sigue diciendo '{without}' con el borrado seguro marcado.");
            Assert.StartsWith(without, with);
        }
        finally
        {
            MainWindowActions.SetChecked(Window, "SecureWipeCheck", false);
        }
    }

    [NonSystemDriveFact]
    public void CompressCheck_OnlyEnabledForNtfs()
    {
        MainWindowActions.SelectComboText(Window, "FileSystemPicker", "NTFS");
        Assert.True(MainWindowActions.CheckBox(Window, "CompressCheck").IsEnabled);

        MainWindowActions.SelectComboText(Window, "FileSystemPicker", "exFAT");
        Assert.False(MainWindowActions.CheckBox(Window, "CompressCheck").IsEnabled);

        MainWindowActions.SelectComboText(Window, "FileSystemPicker", "NTFS");
    }

    /// <summary>
    /// Restaurar los valores predeterminados sigue funcionando desde donde vive ahora: la primera entrada
    /// de la lista de presets (`T13-10`), en vez de un botón a todo el ancho de la tarjeta.
    /// </summary>
    [NonSystemDriveFact]
    public void RestoreDefaults_ResetsOptionsToDefaults()
    {
        MainWindowActions.SetChecked(Window, "QuickFormatCheck", false);
        MainWindowActions.SetChecked(Window, "SecureWipeCheck", true);

        MainWindowActions.ClickMenuPath(Window, "MnuConfig", "MnuPresets", "RestoreDefaultsItem");

        Assert.Equal(ToggleState.On, MainWindowActions.CheckBox(Window, "QuickFormatCheck").ToggleState);
        Assert.Equal(ToggleState.Off, MainWindowActions.CheckBox(Window, "SecureWipeCheck").ToggleState);
    }
}

