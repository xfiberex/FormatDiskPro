using System.Runtime.InteropServices;
using System.Text;
using FlaUI.Core.AutomationElements;
using FlaUI.Core.Input;
using FlaUI.Core.WindowsAPI;

namespace FormatDiskPro.UiTests;

/// <summary>
/// <i>Exportar CSV</i> tiene que llegar a <b>preguntar dónde guardar</b>.
///
/// <para>Esta prueba existe porque durante mucho tiempo no lo hacía. La app corre siempre elevada
/// (<c>requireAdministrator</c>) y el <c>FileSavePicker</c> de WinRT rechaza a los procesos elevados:
/// lanzaba <c>COMException 0x80004005</c> en el acto, sin abrir ninguna ventana. Ninguna prueba lo
/// cubría —ni unitaria ni de UI—, así que el fallo viajó en todas las versiones publicadas; lo que lo
/// destapó fue una captura del historial con cuatro <c>EXPORT ERROR:</c> sin nada detrás.</para>
///
/// <para><b>Por qué se enumeran las ventanas por Win32 y no por UI Automation:</b> el diálogo del
/// sistema es modal y bloquea el hilo de UI de la app, así que toda consulta UIA contra ella caduca con
/// «Operation timed out». Ese <i>timeout</i> es un síntoma del bloqueo, no una medida — tomarlo por
/// resultado sería el error que <c>T6-11</c> existe para no repetir. <c>EnumWindows</c> no pasa por el
/// hilo de la app y contesta igual con el modal abierto.</para>
/// </summary>
[Collection(AppCollection.Name)]
public sealed class HistoryExportTests(AppFixture fixture)
{
    /// <summary>Clase de ventana de los diálogos comunes de Windows («Guardar como» entre ellos).</summary>
    private const string CommonDialogClass = "#32770";

    private delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);

    [DllImport("user32.dll")] private static extern bool EnumWindows(EnumWindowsProc cb, IntPtr lParam);
    [DllImport("user32.dll")] private static extern bool IsWindowVisible(IntPtr hWnd);
    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern int GetClassNameW(IntPtr hWnd, StringBuilder text, int count);
    [DllImport("user32.dll")] private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint pid);

    /// <summary>¿Tiene el proceso alguna ventana visible de la clase dada?</summary>
    private static bool HasVisibleWindowOfClass(int pid, string className)
    {
        bool found = false;
        EnumWindows((h, _) =>
        {
            GetWindowThreadProcessId(h, out uint owner);
            if (owner != (uint)pid || !IsWindowVisible(h)) return true;
            var cls = new StringBuilder(256);
            GetClassNameW(h, cls, cls.Capacity);
            if (cls.ToString() == className) { found = true; return false; }
            return true;
        }, IntPtr.Zero);
        return found;
    }

    private static bool WaitFor(Func<bool> condition, TimeSpan timeout)
    {
        var until = DateTime.UtcNow + timeout;
        while (DateTime.UtcNow < until)
        {
            if (condition()) return true;
            Thread.Sleep(200);
        }
        return false;
    }

    [Fact]
    public void ExportCsv_OpensTheSystemSaveDialog()
    {
        MainWindowActions.ClickMenuPath(fixture.MainWindow, "MnuTools", "MnuHistory");
        var history = DialogHelper.WaitForDialogContaining(fixture, "ExportButton");
        var export  = history.FindFirstDescendant(cf => cf.ByAutomationId("ExportButton"));
        Assert.NotNull(export);

        int pid = fixture.App.ProcessId;
        Assert.False(HasVisibleWindowOfClass(pid, CommonDialogClass),
            "Había ya un diálogo común abierto antes de pulsar: la prueba no mediría lo suyo.");

        // En otro hilo a propósito: el diálogo es modal y el Invoke() no vuelve hasta que se cierre.
        // Un Thread y no un Task: aquí hace falta bloquear esperándolo, y bloquear sobre un Task dentro
        // de un test es justo lo que xUnit desaconseja (xUnit1031) por riesgo de interbloqueo.
        var click = new Thread(() => { try { export!.AsButton().Invoke(); } catch { } }) { IsBackground = true };
        click.Start();
        try
        {
            Assert.True(WaitFor(() => HasVisibleWindowOfClass(pid, CommonDialogClass), TimeSpan.FromSeconds(10)),
                "Al pulsar 'Exportar CSV' no se abrió ningún diálogo de guardar. Es el fallo del " +
                "FileSavePicker de WinRT en proceso elevado: revienta con COMException 0x80004005 sin " +
                "mostrar nada, y el usuario solo ve una InfoBar de error.");
        }
        finally
        {
            // Cancelar: dejar un modal vivo rompería el resto de la suite. Y cancelar es además lo que
            // esta prueba quiere — no debe escribir ningún archivo en la máquina que la ejecuta.
            Keyboard.Press(VirtualKeyShort.ESCAPE);
            click.Join(TimeSpan.FromSeconds(10));
            WaitFor(() => !HasVisibleWindowOfClass(pid, CommonDialogClass), TimeSpan.FromSeconds(5));
        }

        // Y cancelar NO es un error: la InfoBar tiene que seguir cerrada.
        var errorBar = history.FindFirstDescendant(cf => cf.ByAutomationId("ExportErrorBar"));
        Assert.True(errorBar is null || errorBar.IsOffscreen,
            "Cancelar el diálogo de guardar dejó abierta la InfoBar de error: cancelar es la respuesta " +
            "'no', no un fallo.");

        fixture.MainWindow.Focus();
        Keyboard.Press(VirtualKeyShort.ESCAPE);
        DialogHelper.WaitForNoDialog(fixture);
    }
}
