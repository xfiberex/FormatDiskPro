using System.Runtime.InteropServices;

namespace FormatDiskPro;

/// <summary>
/// Lector de las métricas del equipo que alimenta el panel de rendimiento del pie de la ventana:
/// CPU, memoria física y conjunto de trabajo del proceso. Vía Win32 (<c>kernel32.dll</c>).
/// </summary>
/// <remarks>
/// Es <b>con estado</b>: la CPU se calcula por diferencia entre dos lecturas, así que el monitor guarda
/// la anterior y suaviza el resultado. Una instancia por ventana, y <see cref="Reset"/> al empezar una
/// operación para no arrastrar lo que se midió durante la anterior.
/// </remarks>
public interface IPerformanceMonitor
{
    /// <inheritdoc cref="PerformanceMonitor.Sample"/>
    LoadSample Sample(double diskBytesPerSec);

    /// <inheritdoc cref="PerformanceMonitor.Reset"/>
    void Reset();
}

/// <inheritdoc cref="IPerformanceMonitor"/>
/// <remarks>
/// <para><b>Por qué Win32 y no <c>System.Diagnostics.PerformanceCounter</c>.</b> Dos razones, y las dos
/// se ven en producción y no en el banco de pruebas:</para>
/// <list type="number">
///   <item><description>Los nombres de las categorías de PDH están <b>traducidos</b>. En un Windows en
///   español la categoría no es <c>"Processor"</c> sino <c>"Procesador"</c>, y esta app se instala en
///   cinco idiomas: un contador buscado por nombre en inglés lanza en la mitad de las máquinas. Hay
///   formas de esquivarlo (índices del registro), pero son más código y más frágiles que la llamada
///   directa.</description></item>
///   <item><description>La primera lectura de un <c>PerformanceCounter</c> tarda y siempre devuelve 0,
///   con lo que el panel arrancaría en blanco justo cuando el usuario lo abre.</description></item>
/// </list>
///
/// <para><b>Nunca lanza.</b> Si una llamada falla, la métrica que dependía de ella sale a 0 y las demás
/// siguen. Un panel informativo no puede tumbar —ni interrumpir— la operación que está describiendo.</para>
/// </remarks>
public sealed class PerformanceMonitor : IPerformanceMonitor
{
    // Ventana de suavizado. 4 muestras a 1 s: medio segundo largo de retardo aparente, suficiente para
    // que el número se pueda leer y corto para que una caída del caudal se vea enseguida.
    private const int SmoothingWindow = 4;

    private readonly MovingAverage _cpu  = new(SmoothingWindow);
    private readonly MovingAverage _disk = new(SmoothingWindow);
    private CpuTimes? _previous;

    [StructLayout(LayoutKind.Sequential)]
    private struct MEMORYSTATUSEX
    {
        public uint  dwLength;
        public uint  dwMemoryLoad;
        public ulong ullTotalPhys;
        public ulong ullAvailPhys;
        public ulong ullTotalPageFile;
        public ulong ullAvailPageFile;
        public ulong ullTotalVirtual;
        public ulong ullAvailVirtual;
        public ulong ullAvailExtendedVirtual;
    }

    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GlobalMemoryStatusEx(ref MEMORYSTATUSEX lpBuffer);

    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GetSystemTimes(out long lpIdleTime, out long lpKernelTime, out long lpUserTime);

    /// <summary>
    /// Toma una muestra del equipo y la combina con el caudal que reporta la operación en curso.
    /// </summary>
    /// <param name="diskBytesPerSec">
    /// Bytes por segundo de la operación, o 0 si no hay ninguna o no informa de bytes. No se mide aquí:
    /// lo sabe quien la está ejecutando, y medirlo con un contador del sistema daría el tráfico de toda
    /// la máquina en vez del de la operación.
    /// </param>
    /// <returns>La muestra ya suavizada, lista para pintar.</returns>
    public LoadSample Sample(double diskBytesPerSec)
    {
        double cpu = _cpu.Add(ReadCpuPercent());
        double disk = _disk.Add(Math.Max(0, diskBytesPerSec));
        (long ramUsed, long ramTotal) = ReadMemory();

        return new LoadSample(cpu, ramUsed, ramTotal, ReadAppMemory(), disk);
    }

    /// <summary>
    /// Olvida las lecturas acumuladas: la referencia de CPU y las ventanas de suavizado.
    /// </summary>
    /// <remarks>
    /// Se llama al <b>empezar</b> una operación. Sin esto, los primeros ticks arrastrarían el caudal de
    /// la operación anterior —o el reposo de antes de abrir el panel— y el usuario vería un número que
    /// no corresponde a lo que está pasando.
    /// </remarks>
    public void Reset()
    {
        _previous = null;
        _cpu.Reset();
        _disk.Reset();
    }

    /// <summary>Uso de CPU desde la lectura anterior; 0 en la primera llamada o si la API falla.</summary>
    private double ReadCpuPercent()
    {
        try
        {
            if (!GetSystemTimes(out long idle, out long kernel, out long user)) return 0;

            // Los FILETIME vienen como pares de 32 bits que .NET entrega en un long con signo; el valor
            // es un contador sin signo, así que se reinterpreta antes de restar.
            var now = new CpuTimes((ulong)idle, (ulong)kernel, (ulong)user);
            double percent = _previous is CpuTimes before ? SystemLoad.CpuPercent(before, now) : 0;
            _previous = now;
            return percent;
        }
        catch { return 0; }
    }

    /// <summary>Memoria física (usada, total) del equipo; (0, 0) si la consulta falla.</summary>
    private static (long Used, long Total) ReadMemory()
    {
        try
        {
            var status = new MEMORYSTATUSEX { dwLength = (uint)Marshal.SizeOf<MEMORYSTATUSEX>() };
            if (!GlobalMemoryStatusEx(ref status)) return (0, 0);

            long total = (long)Math.Min(status.ullTotalPhys, long.MaxValue);
            long free  = (long)Math.Min(status.ullAvailPhys, long.MaxValue);
            return (Math.Max(0, total - free), total);
        }
        catch { return (0, 0); }
    }

    /// <summary>
    /// Conjunto de trabajo del propio proceso; 0 si no se puede leer.
    /// </summary>
    /// <remarks>
    /// Es el consumo de <b>FormatDiskPro</b>, no el de la operación: formatear, comprobar y reinicializar
    /// los hace <c>format.com</c>, <c>chkdsk.exe</c> o PowerShell en procesos aparte. Por eso el panel lo
    /// enseña como pie de la fila de RAM y no como una métrica propia — decir «la operación usa 118 MB»
    /// sería falso.
    /// </remarks>
    private static long ReadAppMemory()
    {
        try { return Environment.WorkingSet; }
        catch { return 0; }
    }
}
