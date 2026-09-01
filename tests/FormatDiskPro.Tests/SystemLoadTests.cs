using FormatDiskPro;
using Xunit;

namespace FormatDiskPro.Tests;

/// <summary>
/// Pruebas de la lógica pura del panel de rendimiento (`T11-01`): la aritmética de los contadores, los
/// umbrales de color y el escalado de las barras.
/// </summary>
/// <remarks>
/// Lo que se prueba aquí es justo lo que <b>no</b> se puede provocar en una máquina real a voluntad:
/// dos lecturas de CPU idénticas, contadores que retroceden, una barra escalada contra un pico de 0 o
/// una consulta de memoria que devuelve 0 bytes instalados. Todos esos casos rompen la UI de forma
/// visible (un <c>NaN</c> pintado, una división entre cero) y ninguno necesita un disco para salir.
/// </remarks>
public sealed class SystemLoadTests
{
    // ── Percent ──────────────────────────────────────────────────

    [Theory]
    [InlineData(0, 100, 0)]
    [InlineData(50, 100, 50)]
    [InlineData(100, 100, 100)]
    [InlineData(1, 4, 25)]
    public void Percent_NormalCases_ReturnsRatio(long used, long total, double expected)
        => Assert.Equal(expected, SystemLoad.Percent(used, total), 6);

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Percent_NonPositiveTotal_ReturnsZero(long total)
        => Assert.Equal(0, SystemLoad.Percent(10, total));

    [Fact]
    public void Percent_UsedAboveTotal_ClampsToHundred()
        => Assert.Equal(100, SystemLoad.Percent(200, 100));

    [Fact]
    public void Percent_NegativeUsed_ClampsToZero()
        => Assert.Equal(0, SystemLoad.Percent(-5, 100));

    // ── CpuPercent ───────────────────────────────────────────────

    [Fact]
    public void CpuPercent_HalfIdle_ReturnsFifty()
    {
        // KernelTicks INCLUYE el ocioso: 100 de núcleo (de los cuales 50 ociosos) + 0 de usuario.
        var before = new CpuTimes(0, 0, 0);
        var after  = new CpuTimes(50, 100, 0);
        Assert.Equal(50, SystemLoad.CpuPercent(before, after), 6);
    }

    [Fact]
    public void CpuPercent_FullyBusy_ReturnsHundred()
        => Assert.Equal(100, SystemLoad.CpuPercent(new CpuTimes(0, 0, 0), new CpuTimes(0, 40, 60)), 6);

    [Fact]
    public void CpuPercent_FullyIdle_ReturnsZero()
        => Assert.Equal(0, SystemLoad.CpuPercent(new CpuTimes(0, 0, 0), new CpuTimes(100, 100, 0)), 6);

    [Fact]
    public void CpuPercent_IdenticalReadings_ReturnsZeroNotNaN()
    {
        var same = new CpuTimes(1000, 2000, 500);
        double result = SystemLoad.CpuPercent(same, same);
        Assert.False(double.IsNaN(result));
        Assert.Equal(0, result);
    }

    [Fact]
    public void CpuPercent_CountersWentBackwards_ReturnsZero()
        => Assert.Equal(0, SystemLoad.CpuPercent(new CpuTimes(500, 900, 400), new CpuTimes(0, 0, 0)));

    [Fact]
    public void CpuPercent_UsesDeltaNotAbsoluteValues()
    {
        // Los contadores son acumulativos desde el arranque: con la MISMA diferencia, el resultado no
        // puede depender de cuánto llevaba encendido el equipo.
        double fresh = SystemLoad.CpuPercent(new CpuTimes(0, 0, 0), new CpuTimes(25, 100, 0));
        double aged  = SystemLoad.CpuPercent(new CpuTimes(9_000, 40_000, 0), new CpuTimes(9_025, 40_100, 0));
        Assert.Equal(fresh, aged, 6);
    }

    // ── LevelFor ─────────────────────────────────────────────────

    [Theory]
    [InlineData(0, SmartLevel.Ok)]
    [InlineData(79.9, SmartLevel.Ok)]
    [InlineData(80, SmartLevel.Warning)]
    [InlineData(89.9, SmartLevel.Warning)]
    [InlineData(90, SmartLevel.Critical)]
    [InlineData(100, SmartLevel.Critical)]
    public void LevelFor_UsesSameThresholdsAsCapacityBar(double percent, SmartLevel expected)
        => Assert.Equal(expected, SystemLoad.LevelFor(percent));

    // ── RelativeFill ─────────────────────────────────────────────

    [Fact]
    public void RelativeFill_HalfOfPeak_ReturnsFifty()
        => Assert.Equal(50, SystemLoad.RelativeFill(25_000_000, 50_000_000), 6);

    [Fact]
    public void RelativeFill_AtPeak_ReturnsHundred()
        => Assert.Equal(100, SystemLoad.RelativeFill(50_000_000, 50_000_000), 6);

    [Fact]
    public void RelativeFill_NoPeakYet_ReturnsZeroNotInfinity()
    {
        double result = SystemLoad.RelativeFill(10, 0);
        Assert.False(double.IsInfinity(result));
        Assert.Equal(0, result);
    }

    [Theory]
    [InlineData(double.NaN)]
    [InlineData(0)]
    [InlineData(-5)]
    public void RelativeFill_NoUsableValue_ReturnsZero(double value)
        => Assert.Equal(0, SystemLoad.RelativeFill(value, 100));

    [Fact]
    public void RelativeFill_AbovePeak_ClampsToHundred()
        => Assert.Equal(100, SystemLoad.RelativeFill(200, 100));

    // ── Peak ─────────────────────────────────────────────────────

    [Fact]
    public void Peak_HigherValue_Wins()
        => Assert.Equal(80, SystemLoad.Peak(50, 80));

    [Fact]
    public void Peak_LowerValue_KeepsPrevious()
        => Assert.Equal(50, SystemLoad.Peak(50, 20));

    [Theory]
    [InlineData(double.NaN)]
    [InlineData(0)]
    [InlineData(-1)]
    public void Peak_UnusableValue_KeepsPrevious(double value)
        => Assert.Equal(50, SystemLoad.Peak(50, value));
}

/// <summary>
/// Pruebas del suavizado de las barras del panel de rendimiento.
/// </summary>
public sealed class MovingAverageTests
{
    [Fact]
    public void Current_NoSamples_IsZero()
        => Assert.Equal(0, new MovingAverage(4).Current);

    [Fact]
    public void Add_FirstSample_ReturnsItWhole()
    {
        // Si promediara contra la ventana vacía, el panel arrancaría siempre en casi cero.
        Assert.Equal(40, new MovingAverage(4).Add(40));
    }

    [Fact]
    public void Add_PartialWindow_AveragesOnlyWhatIsThere()
    {
        var avg = new MovingAverage(4);
        avg.Add(10);
        Assert.Equal(20, avg.Add(30));
    }

    [Fact]
    public void Add_BeyondWindow_DropsOldestSample()
    {
        var avg = new MovingAverage(2);
        avg.Add(100);
        avg.Add(0);
        // La ventana es de 2: la muestra de 100 sale y solo cuentan 0 y 50.
        Assert.Equal(25, avg.Add(50));
    }

    [Theory]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    public void Add_UnusableValue_DoesNotPoisonTheWindow(double bad)
    {
        var avg = new MovingAverage(3);
        avg.Add(60);
        Assert.Equal(60, avg.Add(bad));
        Assert.Equal(60, avg.Current);
    }

    [Fact]
    public void Reset_ForgetsEverything()
    {
        var avg = new MovingAverage(3);
        avg.Add(90);
        avg.Reset();
        Assert.Equal(0, avg.Current);
        Assert.Equal(10, avg.Add(10));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Constructor_NonPositiveWindow_Throws(int size)
        => Assert.Throws<ArgumentOutOfRangeException>(() => new MovingAverage(size));
}
