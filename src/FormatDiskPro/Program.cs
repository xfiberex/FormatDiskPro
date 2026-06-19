using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using System.Threading;

namespace FormatDiskPro;

internal class Program
{
    // Mutex con nombre que declara la presencia del proceso. El instalador (Inno Setup,
    // AppMutex=Global\FormatDiskPro.Instance) lo consulta para detectar que la app está en
    // ejecución y cerrarla de forma fiable antes de una actualización in-place. Se conserva
    // en un campo estático para que viva durante todo el proceso (se libera al terminar).
    private static Mutex? _instanceMutex;

    [STAThread]
    static void Main()
    {
        try { _instanceMutex = new Mutex(initiallyOwned: false, "Global\\FormatDiskPro.Instance"); }
        catch { /* sin privilegio para el namespace Global: la detección por mutex es opcional */ }

        global::WinRT.ComWrappersSupport.InitializeComWrappers();
        Application.Start(p =>
        {
            var ctx = new DispatcherQueueSynchronizationContext(DispatcherQueue.GetForCurrentThread());
            SynchronizationContext.SetSynchronizationContext(ctx);
            _ = new App();
        });
    }
}
