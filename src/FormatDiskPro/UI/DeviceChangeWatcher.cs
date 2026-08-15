using System.Runtime.InteropServices;
using Microsoft.UI.Xaml;

namespace FormatDiskPro.UI;

/// <summary>
/// Avisa cuando se conecta o se desconecta una unidad, para que la lista se refresque sola (#17).
///
/// <para>Windows no ofrece esto a WinUI: hay que <b>subclasar</b> la ventana y leer
/// <c>WM_DEVICECHANGE</c> a mano. Ese trozo de Win32 —cuatro <c>DllImport</c>, un delegado que hay que
/// mantener vivo y un <i>handle</i> que hay que soltar al cerrar— no tiene nada que ver con lo que hace
/// la ventana principal, y estando dentro de ella era una de las cosas que la hacían ilegible.</para>
///
/// <para>La <b>interpretación</b> del mensaje sigue en <see cref="DeviceChange"/> (lógica pura, bajo
/// test); aquí solo está el enganche.</para>
/// </summary>
public sealed class DeviceChangeWatcher : IDisposable
{
    private delegate nint SubclassProc(IntPtr hWnd, uint uMsg, nuint wParam, nint lParam, nuint uIdSubclass, nuint dwRefData);

    [DllImport("comctl32.dll", SetLastError = true)]
    private static extern bool SetWindowSubclass(IntPtr hWnd, SubclassProc pfnSubclass, nuint uIdSubclass, nuint dwRefData);
    [DllImport("comctl32.dll")]
    private static extern nint DefSubclassProc(IntPtr hWnd, uint uMsg, nuint wParam, nint lParam);
    [DllImport("comctl32.dll")]
    private static extern bool RemoveWindowSubclass(IntPtr hWnd, SubclassProc pfnSubclass, nuint uIdSubclass);

    private const nuint SubclassId = 1;

    private SubclassProc? _subclassProc;     // mantener viva la referencia (evita que la recoja el GC)
    private IntPtr _hwnd;
    private DispatcherTimer? _debounce;
    private readonly Action _onChanged;

    /// <summary>
    /// Engancha la ventana indicada y empieza a vigilar.
    /// </summary>
    /// <param name="hwnd">Handle de la ventana a subclasar.</param>
    /// <param name="onChanged">
    /// Qué hacer cuando cesa una ráfaga de cambios. Se invoca en el hilo de UI.
    /// </param>
    /// <remarks>
    /// El <i>debounce</i> de 600 ms no es cosmético: montar un volumen dispara <b>varias</b>
    /// notificaciones seguidas, y recargar la lista en cada una la haría parpadear y competir consigo
    /// misma.
    /// </remarks>
    public DeviceChangeWatcher(IntPtr hwnd, Action onChanged)
    {
        _onChanged = onChanged;
        _hwnd = hwnd;

        _debounce = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(600) };
        _debounce.Tick += (_, _) =>
        {
            _debounce!.Stop();
            _onChanged();
        };

        _subclassProc = WindowSubclassProc;
        SetWindowSubclass(_hwnd, _subclassProc, SubclassId, 0);
    }

    private nint WindowSubclassProc(IntPtr hWnd, uint uMsg, nuint wParam, nint lParam, nuint uIdSubclass, nuint dwRefData)
    {
        if (uMsg == DeviceChange.WmDeviceChange && DeviceChange.IsArrivalOrRemoval(wParam))
        {
            // Reinicia el debounce: se recarga cuando cesa la ráfaga (montaje de volumen, etc.).
            _debounce?.Stop();
            _debounce?.Start();
        }
        return DefSubclassProc(hWnd, uMsg, wParam, lParam);
    }

    /// <summary>Suelta el subclassing. Nunca lanza: esto corre al cerrar la ventana.</summary>
    public void Dispose()
    {
        try
        {
            _debounce?.Stop();
            if (_subclassProc is not null && _hwnd != IntPtr.Zero)
            {
                RemoveWindowSubclass(_hwnd, _subclassProc, SubclassId);
                _subclassProc = null;
            }
        }
        catch { /* teardown: nunca debe lanzar */ }
    }
}
