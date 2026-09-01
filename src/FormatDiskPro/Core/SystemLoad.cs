namespace FormatDiskPro;

/// <summary>
/// Contadores de tiempo de CPU del sistema, tal como los devuelve <c>GetSystemTimes</c> de Win32.
/// Son acumulativos desde el arranque: un valor suelto no dice nada, lo que informa es la
/// <b>diferencia</b> entre dos lecturas (ver <see cref="SystemLoad.CpuPercent"/>).
/// </summary>
/// <param name="IdleTicks">Tiempo ocioso.</param>
/// <param name="KernelTicks">
/// Tiempo en modo núcleo. <b>Incluye el ocioso</b>, que es la trampa clásica de esta API: restar el
/// ocioso del total no es una corrección opcional, es la fórmula.
/// </param>
/// <param name="UserTicks">Tiempo en modo usuario.</param>
public readonly record struct CpuTimes(ulong IdleTicks, ulong KernelTicks, ulong UserTicks);

/// <summary>
/// Una muestra instantánea del rendimiento que enseña el panel del pie de la ventana.
/// </summary>
/// <param name="CpuPercent">Uso de CPU del equipo entero, 0–100.</param>
/// <param name="RamUsedBytes">RAM física en uso en el equipo.</param>
/// <param name="RamTotalBytes">RAM física instalada.</param>
/// <param name="AppRamBytes">Conjunto de trabajo del propio proceso de FormatDiskPro.</param>
/// <param name="DiskBytesPerSec">
/// Caudal de la operación en curso. Sale de los bytes que la propia operación reporta —no de un
/// contador del sistema—, así que es 0 cuando no hay nada corriendo o cuando la operación no informa
/// de bytes (el formato por <c>format.com</c>, que solo da porcentaje).
/// </param>
public readonly record struct LoadSample(
    double CpuPercent,
    long RamUsedBytes,
    long RamTotalBytes,
    long AppRamBytes,
    double DiskBytesPerSec);

/// <summary>
/// Lógica PURA del panel de rendimiento: la aritmética de los contadores, los umbrales de color y el
/// escalado de las barras. Sin Win32, sin UI y sin estado global — lo que toca el sistema vive en
/// <see cref="PerformanceMonitor"/>, para que esto se pueda probar sin una máquina concreta.
/// </summary>
/// <remarks>
/// <para><b>Por qué está aquí y no en el servicio.</b> Lo único que puede salir mal de un panel de
/// métricas es la aritmética: una división entre cero cuando dos lecturas caen en el mismo tick, un
/// porcentaje por encima de 100 porque los contadores se reiniciaron, una barra que se escala contra un
/// pico de 0. Nada de eso necesita un disco ni un contador de Windows para reproducirse, y todo eso
/// rompe la UI de forma visible.</para>
/// </remarks>
public static class SystemLoad
{
    /// <summary>Porcentaje de <paramref name="used"/> sobre <paramref name="total"/>, acotado a 0–100.</summary>
    /// <remarks>
    /// Total ≤ 0 devuelve 0: no hay porcentaje que enseñar, y que la consulta de memoria falle no es
    /// motivo para tumbar el panel entero.
    /// </remarks>
    /// <param name="used">Parte usada.</param>
    /// <param name="total">Total disponible.</param>
    public static double Percent(long used, long total)
        => total <= 0 ? 0 : Math.Clamp(used * 100.0 / total, 0, 100);

    /// <summary>
    /// Uso de CPU del equipo entre dos lecturas de <see cref="CpuTimes"/>, en 0–100.
    /// </summary>
    /// <remarks>
    /// Devuelve 0 —no <c>NaN</c>, no una excepción— cuando el intervalo no da información: dos lecturas
    /// idénticas (el panel refresca más rápido que la resolución del contador) o un delta negativo
    /// (contadores reiniciados). Un tick sin dato se pinta como 0 y el siguiente corrige.
    /// </remarks>
    /// <param name="previous">Lectura anterior.</param>
    /// <param name="current">Lectura actual.</param>
    public static double CpuPercent(CpuTimes previous, CpuTimes current)
    {
        double idle   = Delta(previous.IdleTicks,   current.IdleTicks);
        double kernel = Delta(previous.KernelTicks, current.KernelTicks);
        double user   = Delta(previous.UserTicks,   current.UserTicks);

        // KernelTicks ya lleva dentro el tiempo ocioso: el total del intervalo es núcleo + usuario, y lo
        // ocupado es ese total menos el ocioso.
        double total = kernel + user;
        if (total <= 0) return 0;

        return Math.Clamp((total - idle) * 100.0 / total, 0, 100);

        static double Delta(ulong before, ulong after) => after >= before ? after - before : 0;
    }

    /// <summary>
    /// Severidad de una métrica expresada en porcentaje, para elegir el color de su barra.
    /// </summary>
    /// <remarks>
    /// Mismos cortes (80 / 90) que la barra de ocupación de la tarjeta de unidad, y a propósito: dos
    /// barras del mismo grosor y el mismo sitio que cambiaran de color en umbrales distintos enseñarían
    /// al usuario que el color no significa nada.
    /// </remarks>
    /// <param name="percent">Valor de la métrica, 0–100.</param>
    public static SmartLevel LevelFor(double percent)
        => percent >= 90 ? SmartLevel.Critical
         : percent >= 80 ? SmartLevel.Warning
         : SmartLevel.Ok;

    /// <summary>
    /// Relleno de una barra que no tiene máximo absoluto, medido contra el mayor valor visto: 0–100.
    /// </summary>
    /// <remarks>
    /// Es lo que necesita el caudal de disco. No existe un «100 % de MB/s» —depende del medio, del bus y
    /// de la operación—, así que la barra se escala contra el <b>pico de esta operación</b>: dice «vas al
    /// 70 % de tu mejor momento», que es la pregunta útil (¿se está frenando?) y no una comparación
    /// inventada contra un máximo teórico que la app no conoce.
    /// </remarks>
    /// <param name="value">Valor actual.</param>
    /// <param name="peak">Mayor valor observado (ver <see cref="Peak"/>).</param>
    public static double RelativeFill(double value, double peak)
        => peak <= 0 || double.IsNaN(value) || value <= 0 ? 0 : Math.Clamp(value * 100.0 / peak, 0, 100);

    /// <summary>Nuevo pico tras observar <paramref name="value"/>; ignora negativos, ceros y <c>NaN</c>.</summary>
    /// <param name="previousPeak">Pico acumulado hasta ahora.</param>
    /// <param name="value">Valor observado.</param>
    public static double Peak(double previousPeak, double value)
        => double.IsNaN(value) || value <= 0 ? previousPeak : Math.Max(previousPeak, value);
}

/// <summary>
/// Media móvil de ventana fija, para que las barras del panel no tiemblen.
/// </summary>
/// <remarks>
/// <para><b>Por qué hace falta.</b> La CPU del equipo y el caudal de disco medidos cada segundo saltan
/// decenas de puntos entre ticks. Sin suavizar, la barra parpadea y el número es ilegible: nadie puede
/// leer un valor que cambia entero cada segundo, y lo que el usuario quiere saber —si la operación se
/// está frenando— es justo la tendencia que el ruido tapa.</para>
///
/// <para>Ventana corta (3–5) a propósito: suavizar más haría que una caída real del caudal tardara
/// demasiado en verse, y esa caída es el único aviso que da un medio que está fallando.</para>
/// </remarks>
public sealed class MovingAverage
{
    private readonly double[] _window;
    private int _next;
    private int _count;

    /// <summary>Crea una media móvil sobre las últimas <paramref name="size"/> muestras.</summary>
    /// <param name="size">Tamaño de la ventana; debe ser al menos 1.</param>
    /// <exception cref="ArgumentOutOfRangeException">Si <paramref name="size"/> es menor que 1.</exception>
    public MovingAverage(int size)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(size, 1);
        _window = new double[size];
    }

    /// <summary>Añade una muestra y devuelve la media de las que caben en la ventana.</summary>
    /// <remarks>
    /// Hasta llenarla, promedia solo lo que hay: así el primer valor se pinta entero en vez de aparecer
    /// dividido entre el tamaño de la ventana, que haría que el panel arrancara siempre en casi cero.
    /// Un <c>NaN</c> o un infinito se descartan y devuelven la media actual, para que una lectura fallida
    /// no envenene la ventana entera.
    /// </remarks>
    /// <param name="value">Muestra nueva.</param>
    public double Add(double value)
    {
        if (double.IsNaN(value) || double.IsInfinity(value)) return Current;

        _window[_next] = value;
        _next = (_next + 1) % _window.Length;
        if (_count < _window.Length) _count++;
        return Current;
    }

    /// <summary>Media de las muestras acumuladas; 0 si no hay ninguna.</summary>
    public double Current
    {
        get
        {
            if (_count == 0) return 0;
            double sum = 0;
            for (int i = 0; i < _count; i++) sum += _window[i];
            return sum / _count;
        }
    }

    /// <summary>Vacía la ventana. Se llama al empezar una operación, para no arrastrar la anterior.</summary>
    public void Reset()
    {
        Array.Clear(_window);
        _next = _count = 0;
    }
}
