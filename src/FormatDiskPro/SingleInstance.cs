// SPDX-License-Identifier: GPL-3.0-only
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace FormatDiskPro;

/// <summary>
/// Una sola instancia de la app (`AUD-037`). Antes el mutex de <see cref="Program"/> se creaba sin mirar
/// si ya existía, y podían correr dos instancias elevadas a la vez, operando sobre la misma unidad.
/// </summary>
/// <remarks>
/// <para>Que el mutex exista no basta para salir. Puede ser de una instancia que se está cerrando —el script
/// de capturas y el instalador cierran la app y la vuelven a lanzar enseguida—, y salir entonces dejaría
/// al usuario sin app. Por eso se mira si queda otro proceso vivo con el mismo nombre:</para>
/// <list type="bullet">
/// <item>con ventana: se trae al frente, y esta instancia sale;</item>
/// <item>ninguno: el mutex era de uno que ya murió, y esta instancia sigue;</item>
/// <item>vivo pero aún sin ventana: está arrancando, se le espera un poco y se decide con lo que haya.</item>
/// </list>
/// </remarks>
internal static class SingleInstance
{
    private const int SwRestore = 9;

    /// <summary>Cuánto se espera a que la otra instancia muestre su ventana antes de darse por vencido.</summary>
    private static readonly TimeSpan WaitForWindow = TimeSpan.FromSeconds(5);

    /// <summary>
    /// ¿Hay ya otra instancia que debe atender al usuario? Si la hay, trae su ventana al frente y devuelve
    /// <c>true</c>: quien llama tiene que salir.
    /// </summary>
    /// <param name="mutexAlreadyExisted">Si el mutex de la app ya existía al crearlo.</param>
    public static bool HandOffToExisting(bool mutexAlreadyExisted)
    {
        if (!mutexAlreadyExisted) return false;

        var deadline = DateTime.UtcNow + WaitForWindow;
        while (true)
        {
            var others = OtherInstances();
            try
            {
                if (others.Count == 0) return false;   // el mutex era de una instancia que ya no está

                IntPtr window = others.Select(p => p.MainWindowHandle).FirstOrDefault(h => h != IntPtr.Zero);
                if (window != IntPtr.Zero)
                {
                    ShowWindow(window, SwRestore);
                    SetForegroundWindow(window);
                    return true;
                }

                // Hay otra viva y aún sin ventana: está arrancando. Pasado el plazo, se le cede igual,
                // porque dos instancias son peores que un doble clic que no abre nada.
                if (DateTime.UtcNow >= deadline) return true;
            }
            finally
            {
                foreach (var p in others) p.Dispose();
            }
            Thread.Sleep(200);
        }
    }

    /// <summary>Los demás procesos vivos de esta app (mismo nombre de ejecutable, otro identificador).</summary>
    private static List<Process> OtherInstances()
    {
        using var self = Process.GetCurrentProcess();
        var others = new List<Process>();
        foreach (var p in Process.GetProcessesByName(self.ProcessName))
        {
            bool keep;
            try { keep = p.Id != self.Id && !p.HasExited; }
            catch { keep = false; }   // sin acceso o ya terminado
            if (keep) others.Add(p); else p.Dispose();
        }
        return others;
    }

    [DllImport("user32.dll")]
    private static extern bool SetForegroundWindow(IntPtr hWnd);

    [DllImport("user32.dll")]
    private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);
}
