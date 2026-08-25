using FlaUI.Core.AutomationElements;
using FlaUI.Core.Definitions;
using FlaUI.Core.Input;
using FlaUI.Core.WindowsAPI;

namespace FormatDiskPro.UiTests;

[Collection(AppCollection.Name)]
public sealed class MainWindowTests(AppFixture fixture)
{
    [Fact]
    public void MainWindow_Opens()
    {
        Assert.False(fixture.MainWindow.IsOffscreen);
    }

    [Fact]
    public void DrivePicker_IsPresent()
    {
        var drivePicker = fixture.MainWindow.FindFirstDescendant(cf => cf.ByAutomationId("DrivePicker"));

        Assert.NotNull(drivePicker);
    }

    [Fact]
    public void StartAndCloseButtons_ArePresent()
    {
        var startButton = fixture.MainWindow.FindFirstDescendant(cf => cf.ByAutomationId("StartButton"));
        var closeButton = fixture.MainWindow.FindFirstDescendant(cf => cf.ByAutomationId("CloseButton"));

        Assert.NotNull(startButton);
        Assert.NotNull(closeButton);
    }

    /// <summary>
    /// `T7-02`: con el disco de sistema seleccionado, *Herramientas* apaga lo que esa unidad no admite
    /// —verificar capacidad, quitar la protección, reinicializar y expulsar— y deja lo que sí
    /// —salud, chkdsk en solo lectura, benchmark—. Se ejerce sobre el disco de sistema porque es la
    /// única unidad que hay en cualquier máquina: no necesita la USB de pruebas.
    ///
    /// <para>Y comprueba el <c>HelpText</c>, no solo el estado: un ítem apagado sin motivo es peor que
    /// el diálogo de rechazo al que sustituye, y el motivo es justo lo que un lector de pantalla lee.</para>
    /// </summary>
    [Fact]
    public void ToolsMenu_DisablesWhatTheSystemDriveDoesNotAllow_AndSaysWhy()
    {
        char systemLetter = char.ToUpperInvariant(Path.GetPathRoot(Environment.SystemDirectory)![0]);
        MainWindowActions.SelectDriveByLetter(fixture.MainWindow, systemLetter);

        MainWindowActions.ClickMenuPath(fixture.MainWindow, "MnuTools");
        try
        {
            foreach (string id in (string[])["MnuVerify", "MnuUnlock", "MnuReinit", "MnuEject"])
            {
                var item = MainWindowActions.Require(fixture.MainWindow, id);
                Assert.False(item.IsEnabled, $"'{id}' debería estar apagado sobre el disco de sistema.");
                Assert.False(string.IsNullOrWhiteSpace(item.HelpText),
                    $"'{id}' está apagado sin decir por qué (HelpText vacío).");
            }

            foreach (string id in (string[])["MnuHealth", "MnuCheck", "MnuBenchmark", "MnuHistory"])
                Assert.True(MainWindowActions.Require(fixture.MainWindow, id).IsEnabled,
                    $"'{id}' sí aplica al disco de sistema y debería seguir disponible.");
        }
        finally
        {
            fixture.MainWindow.Focus();
            Keyboard.Press(VirtualKeyShort.ESCAPE);
        }
    }

    [Fact]
    public void QuickFormatCheck_IsCheckedByDefault()
    {
        var checkBox = fixture.MainWindow
            .FindFirstDescendant(cf => cf.ByAutomationId("QuickFormatCheck"))
            ?.AsCheckBox();

        Assert.NotNull(checkBox);
        Assert.Equal(ToggleState.On, checkBox!.ToggleState);
    }
}
