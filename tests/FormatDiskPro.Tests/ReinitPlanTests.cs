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

    // ── Tamaños que caben en el disco ─────────────────────────────

    private const long Gib = 1024L * 1024 * 1024;

    /// <summary>
    /// Un pendrive de 16 GB nominales son ~14,9 GiB reales: el motivo de que esta función exista. Antes el
    /// selector ofrecía siempre 1/2/4/8/16/32 GB, así que elegir 16 pedía una partición que no cabe — y
    /// <c>New-Partition</c> habría fallado con el disco ya borrado.
    /// </summary>
    [Fact]
    public void SmallFat32SizesFor_SixteenGbStick_ExcludesSizesThatDoNotFit()
        => Assert.Equal([1, 2, 4, 8], ReinitPlan.SmallFat32SizesFor(15_500_000_000L));   // ~14,4 GiB

    [Fact]
    public void SmallFat32SizesFor_LargeDisk_OffersEverySize()
        => Assert.Equal(ReinitPlan.AllowedSmallFat32SizesGb, ReinitPlan.SmallFat32SizesFor(64 * Gib));

    /// <summary>Esta es la razón de ser del margen: sin él, un tamaño que cabe al byte se ofrecería y la
    /// alineación de la partición lo haría fallar.</summary>
    [Fact]
    public void SmallFat32SizesFor_DiskExactlyTheRequestedSize_DoesNotOfferIt()
    {
        Assert.DoesNotContain(8, ReinitPlan.SmallFat32SizesFor(8 * Gib));
        Assert.Contains(8, ReinitPlan.SmallFat32SizesFor(8 * Gib + ReinitPlan.PartitionReserveBytes));
    }

    [Theory]
    [InlineData(0)]                          // disco desconocido: la consulta no llegó o falló
    [InlineData(-1)]
    [InlineData(512L * 1024 * 1024)]         // 512 MiB: no cabe ni el menor de los tamaños
    public void SmallFat32SizesFor_TooSmallOrUnknown_IsEmpty(long diskBytes)
        => Assert.Empty(ReinitPlan.SmallFat32SizesFor(diskBytes));

    [Fact]
    public void SmallFat32SizesFor_IsAscendingSubsetOfTheAllowedSizes()
    {
        int[] sizes = ReinitPlan.SmallFat32SizesFor(20 * Gib);
        Assert.All(sizes, gb => Assert.Contains(gb, ReinitPlan.AllowedSmallFat32SizesGb));
        Assert.Equal(sizes.Order(), sizes);
    }

    // ── Preselección ──────────────────────────────────────────────

    [Fact]
    public void PickSmallFat32Size_PreferredIsAvailable_KeepsIt()
        => Assert.Equal(4, ReinitPlan.PickSmallFat32Size(4, [1, 2, 4, 8]));

    /// <summary>Cae al mayor que quepa, no al menor: en discos grandes el máximo era el valor por defecto
    /// y ese comportamiento no debe cambiar.</summary>
    [Fact]
    public void PickSmallFat32Size_PreferredDoesNotFit_FallsBackToTheLargestAvailable()
        => Assert.Equal(8, ReinitPlan.PickSmallFat32Size(32, [1, 2, 4, 8]));

    [Fact]
    public void PickSmallFat32Size_NothingAvailable_IsNull()
        => Assert.Null(ReinitPlan.PickSmallFat32Size(32, []));
}
