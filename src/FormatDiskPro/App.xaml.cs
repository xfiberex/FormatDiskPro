using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace FormatDiskPro;

public partial class App : Application
{
    public static Window? MainWindow { get; private set; }

    // Evita apilar diálogos si varias excepciones caen seguidas (p. ej. una operación por fases).
    private bool _showingCrashDialog;

    protected override void OnLaunched(LaunchActivatedEventArgs args)
    {
        // Red de seguridad: sin esto, cualquier excepción que escape de un handler `async void`
        // TERMINA el proceso en silencio. El único UnhandledException que genera WinUI vive bajo
        // `#if DEBUG` y solo rompe en el depurador, así que en Release no había ninguna red.
        // No sustituye al try/catch de cada operación (que da un mensaje con contexto): es el
        // último recurso para que un fallo de E/S no se lleve por delante la aplicación entera.
        UnhandledException += OnUnhandledException;

        MainWindow = new UI.MainWindow();
        MainWindow.Activate();
    }

    /// <summary>
    /// Registra la excepción no controlada, impide que termine el proceso y avisa al usuario.
    ///
    /// <para><b>El orden importa:</b> <c>e.Handled</c> se fija de forma SÍNCRONA, antes del primer
    /// <c>await</c>. Al ser un <c>async void</c>, el método "vuelve" en ese primer await y es
    /// entonces cuando WinUI lee la propiedad; fijarla después no evitaría el cierre.</para>
    ///
    /// <para>Todo el cuerpo es defensivo: una excepción aquí dentro no tendría dónde ser atrapada.
    /// Mostrar el diálogo puede fallar legítimamente (ya había un <c>ContentDialog</c> abierto, o la
    /// ventana se está cerrando), y eso no debe impedir que el fallo quede registrado.</para>
    /// </summary>
    // El tipo se cualifica: `UnhandledExceptionEventArgs` a secas es ambiguo entre Microsoft.UI.Xaml y
    // System (que entra por ImplicitUsings).
    private async void OnUnhandledException(object sender, Microsoft.UI.Xaml.UnhandledExceptionEventArgs e)
    {
        e.Handled = true;

        try { History.Log($"CRASH: {e.Exception}"); } catch { /* el log nunca debe empeorar un fallo */ }

        if (_showingCrashDialog) return;
        _showingCrashDialog = true;
        try
        {
            if (MainWindow?.Content is FrameworkElement root && root.XamlRoot is not null)
            {
                var dlg = new ContentDialog
                {
                    Title           = L.T("msg.error"),
                    Content         = L.T("crash.body", e.Message),
                    CloseButtonText = L.T("btn.close"),
                    XamlRoot        = root.XamlRoot,
                    RequestedTheme  = root.RequestedTheme,
                };
                await dlg.ShowAsync();
            }
        }
        catch { /* ya había un diálogo abierto o la ventana se va: el fallo ya quedó en el historial */ }
        finally { _showingCrashDialog = false; }
    }
}
