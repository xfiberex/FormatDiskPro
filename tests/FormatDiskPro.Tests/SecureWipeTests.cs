using FormatDiskPro;
using Xunit;

namespace FormatDiskPro.Tests;

/// <summary>
/// Pruebas de los helpers puros del borrado seguro: bytes planificados (con margen de
/// seguridad y pasadas) y selección del patrón por pasada. La escritura real a disco
/// (<see cref="SecureWipe.RunAsync"/>) es E/S y no se cubre con pruebas unitarias.
/// </summary>
public sealed class SecureWipeTests
{
    [Fact]
    public void PlannedBytes_SubtractsSafetyMargin_TimesPasses()
    {
        long free = SecureWipe.SafetyMarginBytes + 1000;
        Assert.Equal(1000, SecureWipe.PlannedBytes(free, 1));
        Assert.Equal(3000, SecureWipe.PlannedBytes(free, 3));
    }

    [Fact]
    public void PlannedBytes_FreeBelowMargin_IsZero()
        => Assert.Equal(0, SecureWipe.PlannedBytes(0, 1));

    [Fact]
    public void PlannedBytes_ZeroPasses_TreatedAsOne()
        => Assert.Equal(500, SecureWipe.PlannedBytes(SecureWipe.SafetyMarginBytes + 500, 0));

    [Fact]
    public void PassPattern_SinglePass_IsRandom()
        => Assert.True(SecureWipe.PassPattern(0, 1).Random);

    [Fact]
    public void PassPattern_ThreePasses_ZeroThenOnesThenRandom()
    {
        Assert.Equal((false, (byte)0x00), SecureWipe.PassPattern(0, 3));
        Assert.Equal((false, (byte)0xFF), SecureWipe.PassPattern(1, 3));
        Assert.True(SecureWipe.PassPattern(2, 3).Random);
    }

    [Fact]
    public void PassPattern_LastPassAlwaysRandom()
        => Assert.True(SecureWipe.PassPattern(1, 2).Random);

    [Theory]
    [InlineData(1, 1)]
    [InlineData(3, 3)]
    [InlineData(7, 7)]
    public void NormalizePasses_AllowedValuesUnchanged(int passes, int expected)
        => Assert.Equal(expected, SecureWipe.NormalizePasses(passes));

    [Theory]
    [InlineData(0)]
    [InlineData(2)]
    [InlineData(-5)]
    [InlineData(100)]
    public void NormalizePasses_InvalidValuesFallBackToOne(int passes)
        => Assert.Equal(1, SecureWipe.NormalizePasses(passes));

    [Fact]
    public void AllowedPasses_AreExactlyOneThreeSeven()
        => Assert.Equal(new[] { 1, 3, 7 }, SecureWipe.AllowedPasses);
}
