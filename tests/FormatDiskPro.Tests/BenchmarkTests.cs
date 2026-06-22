using System;
using FormatDiskPro;
using Xunit;

namespace FormatDiskPro.Tests;

/// <summary>
/// Pruebas de los helpers puros del benchmark: planificación del tamaño de prueba (con margen) y
/// cálculo de velocidad. La E/S real (<see cref="BenchmarkRunner.RunAsync"/>) no se cubre con unitarias.
/// </summary>
public sealed class BenchmarkTests
{
    private const long Mb = 1024L * 1024;

    [Fact]
    public void PlanTestBytes_AmpleSpace_IsCappedAtTarget()
        => Assert.Equal(Benchmark.TargetTestBytes, Benchmark.PlanTestBytes(10L * 1024 * Mb));

    [Fact]
    public void PlanTestBytes_LimitedSpace_SubtractsSafetyMargin()
    {
        long free = Benchmark.SafetyMarginBytes + 100 * Mb;   // 100 MB utilizables < 256 MB objetivo
        Assert.Equal(100 * Mb, Benchmark.PlanTestBytes(free));
    }

    [Fact]
    public void PlanTestBytes_BelowMargin_IsZero()
    {
        Assert.Equal(0, Benchmark.PlanTestBytes(0));
        Assert.Equal(0, Benchmark.PlanTestBytes(Benchmark.SafetyMarginBytes));
    }

    [Theory]
    [InlineData(1000, 1.0, 1000)]    // 1000 bytes en 1 s
    [InlineData(500, 0.5, 1000)]     // 500 bytes en 0,5 s
    public void BytesPerSec_ComputesRate(long bytes, double seconds, double expected)
        => Assert.Equal(expected, Benchmark.BytesPerSec(bytes, TimeSpan.FromSeconds(seconds)), 3);

    [Fact]
    public void BytesPerSec_NonPositiveElapsed_IsZero()
    {
        Assert.Equal(0, Benchmark.BytesPerSec(1000, TimeSpan.Zero));
        Assert.Equal(0, Benchmark.BytesPerSec(1000, TimeSpan.FromSeconds(-1)));
    }
}
