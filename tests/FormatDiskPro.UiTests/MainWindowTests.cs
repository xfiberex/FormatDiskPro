using FlaUI.Core.AutomationElements;
using FlaUI.Core.Definitions;

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
