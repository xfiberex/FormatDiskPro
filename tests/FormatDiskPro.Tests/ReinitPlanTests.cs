using FormatDiskPro;
using Xunit;

namespace FormatDiskPro.Tests;

/// <summary>
/// Pruebas de la lógica pura de reinicialización: elección de estilo de partición según el tamaño y
/// parseo de la nueva letra. La ejecución real (<see cref="ReinitDrive.RunAsync"/>) es E/S y no se cubre.
/// </summary>
public sealed class ReinitPlanTests
{
    private const long Tb = 1024L * 1024 * 1024 * 1024;

    [Theory]
    [InlineData(16L * 1024 * 1024 * 1024, DiskPartitionStyle.Mbr)]  // 16 GB → MBR
    [InlineData(2L * 1024 * 1024 * 1024 * 1024, DiskPartitionStyle.Mbr)]  // exactamente 2 TB → MBR (no supera el límite)
    public void StyleFor_BelowOrAtMbrLimit_IsMbr(long size, DiskPartitionStyle expected)
        => Assert.Equal(expected, ReinitPlan.StyleFor(size));

    [Fact]
    public void StyleFor_AboveMbrLimit_IsGpt()
    {
        Assert.Equal(DiskPartitionStyle.Gpt, ReinitPlan.StyleFor(2 * Tb + 1));
        Assert.Equal(DiskPartitionStyle.Gpt, ReinitPlan.StyleFor(4 * Tb));
    }

    [Theory]
    [InlineData(DiskPartitionStyle.Mbr, "MBR")]
    [InlineData(DiskPartitionStyle.Gpt, "GPT")]
    public void ToPowerShell_MapsStyle(DiskPartitionStyle style, string expected)
        => Assert.Equal(expected, style.ToPowerShell());

    [Theory]
    [InlineData("LETTER:E", 'E')]
    [InlineData("LETTER:f", 'F')]                       // normaliza a mayúscula
    [InlineData("STAGE:format\nLETTER:G\n", 'G')]       // toma la línea del marcador entre otras
    [InlineData("LETTER: H ", 'H')]                     // tolera espacios
    public void ParseNewLetter_ExtractsLetter(string output, char expected)
        => Assert.Equal(expected, ReinitPlan.ParseNewLetter(output));

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("STAGE:clean\nSTAGE:format")]   // sin marcador LETTER
    [InlineData("LETTER:")]                      // marcador sin letra
    [InlineData("LETTER:1")]                     // no es letra
    public void ParseNewLetter_WhenAbsentOrInvalid_IsNull(string? output)
        => Assert.Null(ReinitPlan.ParseNewLetter(output));

    [Theory]
    [InlineData(1, 1)]
    [InlineData(2, 2)]
    [InlineData(4, 4)]
    [InlineData(8, 8)]
    [InlineData(16, 16)]
    [InlineData(32, 32)]
    public void NormalizeSmallFat32SizeGb_AllowedValuesUnchanged(int gb, int expected)
        => Assert.Equal(expected, ReinitPlan.NormalizeSmallFat32SizeGb(gb));

    [Theory]
    [InlineData(0)]
    [InlineData(3)]
    [InlineData(-5)]
    [InlineData(64)]
    public void NormalizeSmallFat32SizeGb_InvalidValuesFallBackToMax(int gb)
        => Assert.Equal(32, ReinitPlan.NormalizeSmallFat32SizeGb(gb));

    [Fact]
    public void AllowedSmallFat32SizesGb_AreExactlyOneToThirtyTwo()
        => Assert.Equal(new[] { 1, 2, 4, 8, 16, 32 }, ReinitPlan.AllowedSmallFat32SizesGb);

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(4)]
    [InlineData(8)]
    [InlineData(16)]
    public void SmallFat32PartitionBytes_BelowMax_IsExactSize(int gb)
        => Assert.Equal((long)gb * 1024 * 1024 * 1024, ReinitPlan.SmallFat32PartitionBytes(gb));

    [Fact]
    public void SmallFat32PartitionBytes_AtMax_HasSafetyMarginBelowFat32MaxBytes()
    {
        long bytes = ReinitPlan.SmallFat32PartitionBytes(32);
        Assert.True(bytes < FormatLogic.Fat32MaxBytes);
        Assert.Equal(FormatLogic.Fat32MaxBytes - 4L * 1024 * 1024, bytes);
    }
}
