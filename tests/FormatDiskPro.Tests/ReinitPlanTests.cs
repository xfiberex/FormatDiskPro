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
}
