using Microsoft.UI.Xaml;

namespace FormatDiskPro;

public partial class App : Application
{
    public static Window? MainWindow { get; private set; }

    protected override void OnLaunched(LaunchActivatedEventArgs args)
    {
        MainWindow = new UI.MainWindow();
        MainWindow.Activate();
    }
}
