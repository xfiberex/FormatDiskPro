using FlaUI.Core.AutomationElements;
using FlaUI.Core.Definitions;

namespace FormatDiskPro.UiTests;

/// <summary>
/// Cubre los controles de la tarjeta "Configuración de formato"/"Opciones de formato" sin tocar
/// ninguna unidad real: son cambios puramente de UI (nunca se pulsa Iniciar/Reinicializar aquí).
/// </summary>
[Collection(AppCollection.Name)]
public sealed class FormatOptionsUiTests
{
    private Window Window { get; }

    public FormatOptionsUiTests(AppFixture fixture)
    {
        Window = fixture.MainWindow;
        // La unidad seleccionada al arrancar (o la que haya dejado otro test) puede ser la de
        // sistema, protegida: SetFormEnabled deshabilita casi todos los controles de esta tarjeta
        // sobre ella. Estos tests son de UI pura (nunca pulsan Iniciar), así que cualquier otra
        // unidad sirve.
        MainWindowActions.SelectAnyNonSystemDrive(Window);
    }

    [Fact]
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

    [Fact]
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

    [Fact]
    public void CompressCheck_OnlyEnabledForNtfs()
    {
        MainWindowActions.SelectComboText(Window, "FileSystemPicker", "NTFS");
        Assert.True(MainWindowActions.CheckBox(Window, "CompressCheck").IsEnabled);

        MainWindowActions.SelectComboText(Window, "FileSystemPicker", "exFAT");
        Assert.False(MainWindowActions.CheckBox(Window, "CompressCheck").IsEnabled);

        MainWindowActions.SelectComboText(Window, "FileSystemPicker", "NTFS");
    }

    [Fact]
    public void RestoreButton_ResetsOptionsToDefaults()
    {
        MainWindowActions.SetChecked(Window, "QuickFormatCheck", false);
        MainWindowActions.SetChecked(Window, "SecureWipeCheck", true);

        MainWindowActions.Button(Window, "RestoreButton").Invoke();

        Assert.Equal(ToggleState.On, MainWindowActions.CheckBox(Window, "QuickFormatCheck").ToggleState);
        Assert.Equal(ToggleState.Off, MainWindowActions.CheckBox(Window, "SecureWipeCheck").ToggleState);
    }
}
