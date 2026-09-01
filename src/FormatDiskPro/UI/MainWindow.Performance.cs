using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Windows.UI;

namespace FormatDiskPro.UI;

/// <summary>
/// Panel de rendimiento del pie: disco, CPU y RAM mientras corre una operación.
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
/// <para><b>Coste.</b> El temporizador corre con el panel desplegado <b>o</b> con una operación en
/// curso, y en ningún otro momento. Esta app arranca elevada en toda sesión y muchas se pasan enteras
/// sin formatear nada: un tick por segundo perpetuo sería coste sin beneficio. Que siga muestreando con
/// el panel plegado <b>durante</b> una operación es lo que mantiene vivo el resumen del encabezado, que
/// es lo único que ve quien lo plegó. Ver <see cref="ShouldSample"/>.
/// </remarks>
public sealed partial class MainWindow
{
    private DispatcherTimer _perfTimer = null!;

    // Caudal de la operación en curso, en bytes/s. Lo mantiene TimerElapsed_Tick, que ya calcula la
    // velocidad por ventana deslizante para el cronómetro: medirla otra vez aquí daría dos números
    // distintos para lo mismo en la misma pantalla.
    private double _diskBytesPerSec;
    private double _diskPeakBytesPerSec;

    /// <summary>
    /// Si la operación en curso informa de bytes procesados (verificación, borrado seguro, benchmark).
    /// El formateo por <c>format.com</c> y <c>chkdsk</c> solo dan porcentaje.
    /// </summary>
    /// <remarks>
    /// Se deriva de <c>_opTotalBytes</c> —el mismo dato del que ya depende la velocidad del cronómetro—
    /// en vez de guardarse aparte: un segundo campo que dijera lo mismo podría contradecirlo.
    /// </remarks>
    private bool OperationReportsBytes => _opTotalBytes > 0;

    /// <summary>Prepara el temporizador del panel y su estado inicial. Se llama una vez, al construir.</summary>
    private void InitPerformancePanel()
    {
        _perfTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
        _perfTimer.Tick += PerfTimer_Tick;

        PerfExpander.IsExpanded = _settings.ShowPerformance;
        if (ShouldSample) StartPerformanceSampling();
        else              RenderPerformance(null);
    }

    /// <summary>Arranca el muestreo: pinta una muestra ya y sigue cada segundo.</summary>
    /// <remarks>
    /// Muestrea inmediatamente antes de arrancar el temporizador para que el panel no aparezca en blanco
    /// el primer segundo, que es justo cuando el usuario acaba de abrirlo y está mirándolo.
    /// </remarks>
    private void StartPerformanceSampling()
    {
        if (_perfTimer.IsEnabled) return;
        PerfTimer_Tick(null, null!);
        _perfTimer.Start();
    }

    /// <summary>Para el muestreo. El panel conserva lo último pintado.</summary>
    private void StopPerformanceSampling() => _perfTimer.Stop();

    /// <summary>
    /// Si hay motivo para muestrear: el panel está abierto, o hay una operación cuyo resumen se enseña
    /// en el encabezado aunque el panel esté plegado.
    /// </summary>
    private bool ShouldSample => PerfExpander.IsExpanded || _isBusy;

    /// <summary>Arranca o para el muestreo según <see cref="ShouldSample"/>.</summary>
    private void SyncPerformanceSampling()
    {
        if (ShouldSample) StartPerformanceSampling();
        else              StopPerformanceSampling();
    }

    private void PerfExpander_Expanding(Expander sender, ExpanderExpandingEventArgs args)
    {
        StartPerformanceSampling();
        SavePerformancePreference(true);
    }

    private void PerfExpander_Collapsed(Expander sender, ExpanderCollapsedEventArgs args)
    {
        SyncPerformanceSampling();
        SavePerformancePreference(false);
    }

    /// <summary>Persiste si el panel queda abierto entre sesiones; nunca durante el arranque.</summary>
    /// <param name="expanded">Estado nuevo del panel.</param>
    private void SavePerformancePreference(bool expanded)
    {
        // _uiReady lo pone el constructor al terminar: sin esta guarda, fijar IsExpanded desde
        // InitPerformancePanel dispararía el evento y guardaría el valor que se acaba de leer.
        if (!_uiReady || _settings.ShowPerformance == expanded) return;
        _settings.ShowPerformance = expanded;
        _settings.Save();
    }

    private void PerfTimer_Tick(object? sender, object e)
        => RenderPerformance(_services.Performance.Sample(_diskBytesPerSec));

    /// <summary>Reinicia el seguimiento del caudal para una operación nueva y despliega el panel.</summary>
    private void BeginPerformanceTracking()
    {
        _diskBytesPerSec = _diskPeakBytesPerSec = 0;
        _services.Performance.Reset();

        // Desplegar el panel al empezar es lo que le da sentido: los números tienen contexto porque hay
        // una operación que los produce. No se persiste como preferencia (no lo pidió el usuario), así
        // que se pone IsExpanded directamente y SavePerformancePreference queda al margen.
        if (!PerfExpander.IsExpanded)
        {
            _settings.ShowPerformance = true;   // que el Collapsed posterior no lo guarde como "cerrado"
            PerfExpander.IsExpanded = true;     // dispara Expanding → arranca el muestreo
        }
        else
        {
            StartPerformanceSampling();
        }
    }

    /// <summary>
    /// Cierra el seguimiento al terminar la operación: deja de contar caudal y pinta el último estado.
    /// </summary>
    /// <remarks>
    /// El panel se queda <b>desplegado</b> y con el pico: en ese momento deja de ser un monitor y pasa a
    /// ser el resumen de lo que acaba de pasar, que es cuando el dato vale más. El muestreo sigue vivo
    /// porque el panel sigue abierto; lo que se para es el reloj de la operación.
    /// </remarks>
    private void EndPerformanceTracking()
    {
        _diskBytesPerSec = 0;
        // _isBusy sigue en true cuando EndOperation llama aquí: la sincronización real la hace el propio
        // EndOperation al terminar de bajarlo. Aquí solo se corta el caudal, que ya no existe.
    }

    /// <summary>
    /// Pinta una muestra en las tres filas. Con <paramref name="sample"/> nulo deja el panel en su
    /// estado de reposo (guiones), que es como arranca si el usuario lo dejó plegado.
    /// </summary>
    /// <param name="sample">Muestra a pintar, o <c>null</c> para el estado de reposo.</param>
    private void RenderPerformance(LoadSample? sample)
    {
        if (sample is not LoadSample s)
        {
            string dash = L.T("info.dash");
            PerfDiskValue.Text = PerfCpuValue.Text = PerfRamValue.Text = dash;
            PerfDiskCaption.Text = PerfCpuCaption.Text = PerfRamCaption.Text = "";
            PerfSummaryText.Text = "";
            SetBar(PerfDiskColumns, PerfDiskFill, PerfDiskBar, 0, neutral: true);
            SetBar(PerfCpuColumns,  PerfCpuFill,  PerfCpuBar,  0, neutral: false);
            SetBar(PerfRamColumns,  PerfRamFill,  PerfRamBar,  0, neutral: false);
            return;
        }

        // ── Disco ──
        _diskPeakBytesPerSec = SystemLoad.Peak(_diskPeakBytesPerSec, s.DiskBytesPerSec);
        bool hasFlow = s.DiskBytesPerSec > 0 || _diskPeakBytesPerSec > 0;

        // FormatSpeed devuelve cadena vacía con velocidad 0: aquí eso sería un hueco en blanco durante
        // cada pausa de la operación, así que el 0 se pinta como guion, igual que el reposo.
        PerfDiskValue.Text = s.DiskBytesPerSec > 0
            ? Throughput.FormatSpeed(s.DiskBytesPerSec)
            : L.T("info.dash");
        PerfDiskCaption.Text = _diskPeakBytesPerSec > 0
            ? L.T("perf.disk.peak", Throughput.FormatSpeed(_diskPeakBytesPerSec))
            : _isBusy && !OperationReportsBytes ? L.T("perf.disk.noBytes")
            : _isBusy ? ""
            : L.T("perf.disk.idle");

        // Neutro siempre: en esta fila un valor ALTO es lo bueno, así que los umbrales de alarma de las
        // otras dos (80/90 en ámbar y rojo) dirían justo lo contrario de lo que pasa.
        SetBar(PerfDiskColumns, PerfDiskFill, PerfDiskBar,
               SystemLoad.RelativeFill(s.DiskBytesPerSec, _diskPeakBytesPerSec), neutral: true);

        // ── CPU ──
        PerfCpuValue.Text   = L.T("perf.percent", s.CpuPercent.ToString("0", L.Culture));
        PerfCpuCaption.Text = L.T("perf.cpu.cores", Environment.ProcessorCount.ToString(L.Culture));
        SetBar(PerfCpuColumns, PerfCpuFill, PerfCpuBar, s.CpuPercent, neutral: false);

        // ── RAM ──
        double ramPct = SystemLoad.Percent(s.RamUsedBytes, s.RamTotalBytes);
        PerfRamValue.Text = s.RamTotalBytes > 0
            ? L.T("perf.ram.value", FormatBytes(s.RamUsedBytes), FormatBytes(s.RamTotalBytes))
            : L.T("info.dash");
        PerfRamCaption.Text = L.T("perf.ram.app", FormatBytes(s.AppRamBytes));
        SetBar(PerfRamColumns, PerfRamFill, PerfRamBar, ramPct, neutral: false);

        // Resumen del encabezado: lo único que se ve con el panel plegado.
        PerfSummaryText.Text = hasFlow
            ? $"{Throughput.FormatSpeed(s.DiskBytesPerSec)}  ·  {L.T("perf.cpu")} {L.T("perf.percent", s.CpuPercent.ToString("0", L.Culture))}"
            : $"{L.T("perf.cpu")} {L.T("perf.percent", s.CpuPercent.ToString("0", L.Culture))}  ·  {L.T("perf.ram")} {L.T("perf.percent", ramPct.ToString("0", L.Culture))}";
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

    /// <summary>Reescribe las etiquetas fijas del panel al cambiar de idioma.</summary>
    private void ApplyPerformanceLanguage()
    {
        PerfTitleLbl.Text = L.T("perf.title");
        PerfDiskLbl.Text  = L.T("perf.disk");
        PerfCpuLbl.Text   = L.T("perf.cpu");
        PerfRamLbl.Text   = L.T("perf.ram");
        Microsoft.UI.Xaml.Automation.AutomationProperties.SetName(PerfExpander, L.T("perf.title"));

        // Los valores y los pies los reescribe el próximo tick; con el panel parado hay que forzarlo,
        // o quedarían en el idioma anterior hasta que alguien lo despliegue.
        if (!_perfTimer.IsEnabled) RenderPerformance(null);
    }
}
