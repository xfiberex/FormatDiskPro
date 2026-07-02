using System.Globalization;
using FormatDiskPro;
using Xunit;

namespace FormatDiskPro.Tests;

/// <summary>
/// Pruebas de la lógica de rendimiento: estimación de ETA y formateo de velocidad/tiempo restante.
/// </summary>
public sealed class ThroughputTests : IDisposable
{
    private readonly CultureInfo _prevCulture = CultureInfo.CurrentCulture;

    // FormatSpeed reutiliza FormatLogic.FormatBytes (formato según cultura); fijamos invariante.
    public ThroughputTests() => CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
    public void Dispose() => CultureInfo.CurrentCulture = _prevCulture;

    // ── Eta ──────────────────────────────────────────────────────

    [Fact]
    public void Eta_NormalCase_ReturnsRemainingOverSpeed()
        => Assert.Equal(TimeSpan.FromSeconds(10), Throughput.Eta(1000, 100));

    [Fact]
    public void Eta_ZeroSpeed_ReturnsNull()
        => Assert.Null(Throughput.Eta(1000, 0));

    [Fact]
    public void Eta_NegativeRemaining_ReturnsNull()
        => Assert.Null(Throughput.Eta(-1, 100));

    // ── FormatSpeed ──────────────────────────────────────────────

    [Fact]
    public void FormatSpeed_Zero_ReturnsEmpty()
        => Assert.Equal("", Throughput.FormatSpeed(0));

    [Theory]
    [InlineData(1048576d, "1 MB/s")]
    [InlineData(1073741824d, "1 GB/s")]
    [InlineData(1024d, "1 KB/s")]
    [InlineData(1572864d, "1.5 MB/s")]
    public void FormatSpeed_FormatsAcrossUnits(double bytesPerSec, string expected)
        => Assert.Equal(expected, Throughput.FormatSpeed(bytesPerSec));

    // ── FormatEta ────────────────────────────────────────────────

    [Fact]
    public void FormatEta_Null_ReturnsEmpty()
        => Assert.Equal("", Throughput.FormatEta(null));

    [Fact]
    public void FormatEta_UnderOneHour_FormatsMinutesSeconds()
        => Assert.Equal("01:05", Throughput.FormatEta(TimeSpan.FromSeconds(65)));

    [Fact]
    public void FormatEta_OverOneHour_IncludesHours()
        => Assert.Equal("1:01:01", Throughput.FormatEta(TimeSpan.FromSeconds(3661)));
}
