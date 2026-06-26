using System;
using FormatDiskPro;
using Xunit;

namespace FormatDiskPro.Tests;

/// <summary>
/// Pruebas de los helpers puros del benchmark: planificación del tamaño de prueba (con margen, alineado a
/// bloque), cálculo de velocidad, mediana de pasadas y desplazamiento aleatorio alineado. La E/S real
/// (<see cref="BenchmarkRunner.RunAsync"/>) no se cubre con unitarias.
/// </summary>
public sealed class BenchmarkTests
{
    private const long Mb = 1024L * 1024;
    private const long Block = 1 * Mb;   // bloque secuencial de BenchmarkRunner (1 MiB)

    [Fact]
    public void PlanTestBytes_AmpleSpace_IsCappedAtTarget()
        => Assert.Equal(Benchmark.TargetTestBytes, Benchmark.PlanTestBytes(10L * 1024 * Mb, Block));

    [Fact]
    public void PlanTestBytes_LimitedSpace_SubtractsMarginAndFloorsToBlock()
    {
        long free = Benchmark.SafetyMarginBytes + 100 * Mb + 512 * 1024;   // 100,5 MiB utilizables
        Assert.Equal(100 * Mb, Benchmark.PlanTestBytes(free, Block));        // → múltiplo de 1 MiB (100)
    }

    [Fact]
    public void PlanTestBytes_BelowOneBlock_IsZero()
    {
        Assert.Equal(0, Benchmark.PlanTestBytes(0, Block));
        Assert.Equal(0, Benchmark.PlanTestBytes(Benchmark.SafetyMarginBytes, Block));
        Assert.Equal(0, Benchmark.PlanTestBytes(Benchmark.SafetyMarginBytes + 512 * 1024, Block));   // < 1 MiB
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

    [Fact]
    public void Median_EmptyIsZero()
        => Assert.Equal(0d, Benchmark.Median(ReadOnlySpan<double>.Empty), 3);

    [Fact]
    public void Median_SingleValue()
        => Assert.Equal(42d, Benchmark.Median(new double[] { 42 }), 3);

    [Fact]
    public void Median_OddCount_PicksMiddleAndIgnoresOutlier()
        => Assert.Equal(200d, Benchmark.Median(new double[] { 300, 100, 200 }), 3);   // descarta el frío (100)

    [Fact]
    public void Median_EvenCount_AveragesTwoMiddle()
        => Assert.Equal(25d, Benchmark.Median(new double[] { 40, 10, 30, 20 }), 3);

    [Fact]
    public void Median_DoesNotMutateInput()
    {
        var values = new double[] { 300, 100, 200 };
        _ = Benchmark.Median(values);
        Assert.Equal(new double[] { 300, 100, 200 }, values);
    }

    [Fact]
    public void RandomAlignedOffset_InRangeAndAligned()
    {
        var rng = new Random(12345);
        long length = 1 * Mb;
        const int block = 4096;
        for (int i = 0; i < 1000; i++)
        {
            long o = Benchmark.RandomAlignedOffset(length, block, rng);
            Assert.True(o >= 0 && o <= length - block, $"fuera de rango: {o}");
            Assert.Equal(0, o % block);   // alineado al bloque
        }
    }

    [Fact]
    public void RandomAlignedOffset_TooSmall_IsZero()
        => Assert.Equal(0, Benchmark.RandomAlignedOffset(2048, 4096, new Random(1)));

    [Fact]
    public void Iops_DividesBytesPerSecByBlock()
    {
        // 40 MiB/s en bloques de 4 KiB → 10 240 IOPS.
        Assert.Equal(10240d, Benchmark.Iops(40 * Mb, Benchmark.Random4KBlockBytes), 3);
        Assert.Equal(1d, Benchmark.Iops(4096, 4096), 3);
    }

    [Fact]
    public void Iops_NonPositiveBlock_IsZero()
    {
        Assert.Equal(0d, Benchmark.Iops(1_000_000, 0), 3);
        Assert.Equal(0d, Benchmark.Iops(1_000_000, -4096), 3);
    }

    [Fact]
    public void Iops_ZeroSpeed_IsZero()
        => Assert.Equal(0d, Benchmark.Iops(0, Benchmark.Random4KBlockBytes), 3);
}
