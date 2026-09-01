using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Windows.UI;

namespace FormatDiskPro.UI;

/// <summary>
/// Franja de rendimiento del pie: disco, CPU y RAM, siempre visibles.
///
/// <para>Parte de <see cref="MainWindow"/>: es la MISMA clase, repartida en archivos por asunto
/// (T2-08).</para>
/// </summary>
/// <remarks>
/// <para><b>Qué enseña y qué NO.</b> El caudal de disco es el de la operación en curso —sale de los
/// bytes que ella misma reporta, no de un contador del sistema—, así que mide lo que el usuario está
/// esperando y no el tráfico de toda la máquina. La CPU y la RAM sí son del equipo: formatear,
/// comprobar y reinicializar los ejecutan <c>format.com</c>, <c>chkdsk.exe</c> o PowerShell en procesos
/// aparte, de modo que un «CPU del proceso» diría casi 0 durante un formateo y sería mentira útil para
/// nadie.</para>
///
/// <para><b>Por qué ya no es un desplegable (`T11-04`).</b> Empezó siéndolo para no gastar alto en una
/// ventana de tamaño fijo. Pero plegar cuesta un clic para ver un dato que se consulta de un vistazo, y
/// obliga a inventar un resumen para que el encabezado diga algo estando plegado. Compactada a tres
/// columnas, la franja entera ocupa menos que aquel encabezado: el motivo de plegarla desapareció, y
/// con él el desplegable.</para>
///
/// <para><b>Coste.</b> El temporizador corre mientras la ventana está <b>al frente</b> o hay una
/// operación en curso, y en ningún otro momento (ver <see cref="ShouldSample"/>). Con la app detrás no
/// hay nadie mirando la franja; con una operación en curso hay que seguir midiendo aunque el usuario se
/// haya ido a otra ventana, porque al volver el pico tiene que ser el de toda la operación.</para>
/// </remarks>
public sealed partial class MainWindow
{
    private DispatcherTimer _perfTimer = null!;

    // Caudal de la operación en curso, en bytes/s. Lo mantiene TimerElapsed_Tick, que ya calcula la
    // velocidad por ventana deslizante para el cronómetro: medirla otra vez aquí daría dos números
    // distintos para lo mismo en la misma pantalla.
    private double _diskBytesPerSec;
    private double _diskPeakBytesPerSec;
    private bool _windowActive;

    /// <summary>
    /// Si la operación en curso informa de bytes procesados (verificación, borrado seguro, benchmark).
    /// El formateo por <c>format.com</c> y <c>chkdsk</c> solo dan porcentaje.
    /// </summary>
    /// <remarks>
    /// Se deriva de <c>_opTotalBytes</c> —el mismo dato del que ya depende la velocidad del cronómetro—
    /// en vez de guardarse aparte: un segundo campo que dijera lo mismo podría contradecirlo.
    /// </remarks>
    private bool OperationReportsBytes => _opTotalBytes > 0;

    /// <summary>
    /// Si hay motivo para muestrear: alguien está mirando la ventana, o hay una operación cuyo pico no
    /// puede tener agujeros aunque el usuario se haya ido a otra parte.
    /// </summary>
    private bool ShouldSample => _windowActive || _isBusy;

    /// <summary>Prepara el temporizador de la franja y su estado inicial. Se llama una vez, al construir.</summary>
    private void InitPerformancePanel()
    {
        _perfTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
        _perfTimer.Tick += PerfTimer_Tick;

        // La activación de la ventana es lo que enciende y apaga el muestreo. Este manejador NO se da de
        // baja (a diferencia de OnFirstActivated): tiene que oír también las desactivaciones.
        Activated += OnActivatedForPerformance;

        RenderPerformance(null);
    }

    private void OnActivatedForPerformance(object sender, WindowActivatedEventArgs e)
    {
        _windowActive = e.WindowActivationState != WindowActivationState.Deactivated;
        SyncPerformanceSampling();
    }

    /// <summary>Arranca el muestreo: pinta una muestra ya y sigue cada segundo.</summary>
    /// <remarks>
    /// Muestrea inmediatamente antes de arrancar el temporizador para que la franja no se quede en
    /// guiones el primer segundo, que es justo cuando el usuario acaba de volver a la ventana.
    /// </remarks>
    private void StartPerformanceSampling()
    {
        if (_perfTimer.IsEnabled) return;
        PerfTimer_Tick(null, null!);
        _perfTimer.Start();
    }

    /// <summary>Para el muestreo. La franja conserva lo último pintado.</summary>
    private void StopPerformanceSampling() => _perfTimer.Stop();

    /// <summary>Arranca o para el muestreo según <see cref="ShouldSample"/>.</summary>
    private void SyncPerformanceSampling()
    {
        if (ShouldSample) StartPerformanceSampling();
        else              StopPerformanceSampling();
    }

    private void PerfTimer_Tick(object? sender, object e)
        => RenderPerformance(_services.Performance.Sample(_diskBytesPerSec));

    /// <summary>Reinicia el seguimiento del caudal para una operación nueva.</summary>
    private void BeginPerformanceTracking()
    {
        _diskBytesPerSec = _diskPeakBytesPerSec = 0;
        _services.Performance.Reset();
        StartPerformanceSampling();
    }

    /// <summary>
    /// Cierra el seguimiento al terminar la operación: deja de contar caudal.
    /// </summary>
    /// <remarks>
    /// El <b>pico</b> no se borra aquí: se queda en el tooltip de la columna de disco hasta la operación
    /// siguiente, que es cuando deja de describir nada. Borrarlo al terminar quitaría el dato justo en el
    /// momento en que el usuario va a mirarlo.
    /// </remarks>
    private void EndPerformanceTracking() => _diskBytesPerSec = 0;

    /// <summary>
    /// Pinta una muestra en las tres columnas. Con <paramref name="sample"/> nulo deja la franja en su
    /// estado de reposo (guiones), que es como arranca antes del primer muestreo.
    /// </summary>
    /// <param name="sample">Muestra a pintar, o <c>null</c> para el estado de reposo.</param>
    private void RenderPerformance(LoadSample? sample)
    {
        string dash = L.T("info.dash");

        if (sample is not LoadSample s)
        {
            PerfDiskValue.Text = PerfCpuValue.Text = PerfRamValue.Text = dash;
            SetCell(PerfDiskCell, "perf.disk", dash, "");
            SetCell(PerfCpuCell,  "perf.cpu",  dash, "");
            SetCell(PerfRamCell,  "perf.ram",  dash, "");
            SetBar(PerfDiskColumns, PerfDiskFill, PerfDiskBar, 0, neutral: true);
            SetBar(PerfCpuColumns,  PerfCpuFill,  PerfCpuBar,  0, neutral: false);
            SetBar(PerfRamColumns,  PerfRamFill,  PerfRamBar,  0, neutral: false);
            return;
        }

        // ── Disco ──
        _diskPeakBytesPerSec = SystemLoad.Peak(_diskPeakBytesPerSec, s.DiskBytesPerSec);

        // FormatSpeed devuelve cadena vacía con velocidad 0: aquí eso sería un hueco en blanco durante
        // cada pausa de la operación, así que el 0 se pinta como guion, igual que el reposo.
        string diskValue = s.DiskBytesPerSec > 0 ? Throughput.FormatSpeed(s.DiskBytesPerSec) : dash;
        string diskDetail = _diskPeakBytesPerSec > 0
            ? L.T("perf.disk.peak", Throughput.FormatSpeed(_diskPeakBytesPerSec))
            : _isBusy && !OperationReportsBytes ? L.T("perf.disk.noBytes")
            : _isBusy ? ""
            : L.T("perf.disk.idle");

        PerfDiskValue.Text = diskValue;
        SetCell(PerfDiskCell, "perf.disk", diskValue, diskDetail);

        // Neutro siempre: en esta columna un valor ALTO es lo bueno, así que los umbrales de alarma de
        // las otras dos (80/90 en ámbar y rojo) dirían justo lo contrario de lo que pasa.
        SetBar(PerfDiskColumns, PerfDiskFill, PerfDiskBar,
               SystemLoad.RelativeFill(s.DiskBytesPerSec, _diskPeakBytesPerSec), neutral: true);

        // ── CPU ──
        string cpuValue = L.T("perf.percent", s.CpuPercent.ToString("0", L.Culture));
        PerfCpuValue.Text = cpuValue;
        SetCell(PerfCpuCell, "perf.cpu", cpuValue,
                L.T("perf.cpu.cores", Environment.ProcessorCount.ToString(L.Culture)));
        SetBar(PerfCpuColumns, PerfCpuFill, PerfCpuBar, s.CpuPercent, neutral: false);

        // ── RAM ──
        double ramPct = SystemLoad.Percent(s.RamUsedBytes, s.RamTotalBytes);
        string ramValue = s.RamTotalBytes > 0
            ? L.T("perf.ram.value", FormatBytes(s.RamUsedBytes), FormatBytes(s.RamTotalBytes))
            : dash;
        PerfRamValue.Text = ramValue;
        SetCell(PerfRamCell, "perf.ram", ramValue, L.T("perf.ram.app", FormatBytes(s.AppRamBytes)));
        SetBar(PerfRamColumns, PerfRamFill, PerfRamBar, ramPct, neutral: false);
    }

    /// <summary>
    /// Pone en una columna su tooltip y su nombre de automatización: etiqueta, valor y el detalle que ya
    /// no cabe en pantalla.
    /// </summary>
    /// <remarks>
    /// <para>Al compactar la franja (`T11-04`) los pies de cada métrica —el pico, los núcleos, el consumo
    /// de la propia app— dejaron de caber. Van aquí, y no se pierden: <b>el tooltip sí se muestra</b>
    /// porque estos controles no se deshabilitan nunca, que era justo el problema de los ítems de menú de
    /// <c>T7-08</c>.</para>
    ///
    /// <para>El nombre de automatización lleva lo mismo, porque un lector de pantalla que recorre el pie
    /// necesita saber que «42,3 MB/s» es el disco y contra qué se compara.</para>
    /// </remarks>
    /// <param name="cell">Contenedor de la columna.</param>
    /// <param name="labelKey">Clave de la etiqueta de la métrica.</param>
    /// <param name="value">Valor ya formateado.</param>
    /// <param name="detail">Detalle de contexto, o cadena vacía si no hay ninguno que dar.</param>
    private static void SetCell(FrameworkElement cell, string labelKey, string value, string detail)
    {
        string text = detail.Length > 0
            ? $"{L.T(labelKey)}: {value} — {detail}"
            : $"{L.T(labelKey)}: {value}";

        ToolTipService.SetToolTip(cell, text);
        AutomationProperties.SetName(cell, text);
    }

    /// <summary>
    /// Pinta una barra de métrica: reparto de columnas, color del relleno y color de la pista.
    /// </summary>
    /// <remarks>
    /// Mismo mecanismo que la barra de ocupación de la tarjeta de unidad —pista = el propio
    /// <see cref="Border"/>, relleno = su hijo, ancho por columnas estrella— y los colores salen de
    /// <see cref="SeverityPalette"/>, que es el inventario que el barrido de contraste mide. Un
    /// <c>Color.FromArgb</c> aquí sería un color fuera de esa medición.
    /// </remarks>
    /// <param name="columns">Rejilla de dos columnas estrella (relleno, resto).</param>
    /// <param name="fill">Borde del relleno.</param>
    /// <param name="track">Borde de la pista.</param>
    /// <param name="percent">Relleno, 0–100.</param>
    /// <param name="neutral">
    /// <c>true</c> para una métrica sin alarma (el caudal de disco); <c>false</c> para las que empeoran
    /// al subir y se tiñen por umbral.
    /// </param>
    private void SetBar(Grid columns, Border fill, Border track, double percent, bool neutral)
    {
        double pct = Math.Clamp(double.IsNaN(percent) ? 0 : percent, 0, 100);
        columns.ColumnDefinitions[0].Width = new GridLength(pct, GridUnitType.Star);
        columns.ColumnDefinitions[1].Width = new GridLength(100 - pct, GridUnitType.Star);

        Color color = neutral
            ? SeverityPalette.NeutralFill(_darkMode)
            : SystemLoad.LevelFor(pct) switch
            {
                SmartLevel.Critical => SeverityPalette.For(SmartLevel.Critical, _darkMode),
                SmartLevel.Warning  => SeverityPalette.For(SmartLevel.Warning, _darkMode),
                _                   => SeverityPalette.NeutralFill(_darkMode),
            };

        fill.Background  = new SolidColorBrush(color);
        track.Background = new SolidColorBrush(SeverityPalette.TrackFill(_darkMode));
    }

    /// <summary>Reescribe las etiquetas fijas de la franja al cambiar de idioma.</summary>
    private void ApplyPerformanceLanguage()
    {
        PerfDiskLbl.Text = L.T("perf.disk");
        PerfCpuLbl.Text  = L.T("perf.cpu");
        PerfRamLbl.Text  = L.T("perf.ram");
        AutomationProperties.SetName(PerfStrip, L.T("perf.title"));

        // Los valores y los tooltips los reescribe el próximo tick; con el muestreo parado hay que
        // forzarlo, o quedarían en el idioma anterior hasta que la ventana vuelva al frente.
        if (!_perfTimer.IsEnabled) RenderPerformance(null);
    }
}
