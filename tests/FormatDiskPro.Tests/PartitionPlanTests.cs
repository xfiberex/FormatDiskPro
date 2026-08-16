using FormatDiskPro;
using Xunit;

namespace FormatDiskPro.Tests;

/// <summary>
/// El plan de particiones y su validación (`T5-01`).
///
/// <para><b>Por qué esto se prueba aquí y no con un disco.</b> Un layout mal calculado no se descubre al
/// pedirlo: se descubre a mitad de la operación, <b>con el disco ya borrado</b>. Separar el plan del
/// ejecutor existe justamente para que todos esos errores se puedan reproducir en milisegundos y sin
/// hardware. Si alguna regla nueva no se puede probar aquí, es señal de que está en el sitio equivocado.</para>
/// </summary>
public sealed class PartitionPlanTests
{
    private const long Gib = 1024L * 1024 * 1024;
    private const long Disk64Gb = 64 * Gib;

    private static PartitionSpec Exact(long bytes, string fs = "NTFS", string label = "")
        => new(new PartitionSize.Exact(bytes), fs, label);

    private static PartitionSpec Rest(string fs = "NTFS", string label = "")
        => new(new PartitionSize.Remainder(), fs, label);

    private static PartitionPlan Plan(params PartitionSpec[] parts)
        => new(DiskPartitionStyle.Gpt, parts);

    private static PlanProblem ProblemOf(PartitionPlan plan, long disk = Disk64Gb)
        => plan.Validate(disk).Problem;

    // ── Planes válidos ────────────────────────────────────────────

    [Fact]
    public void WholeDisk_IsASingleRemainderPartition()
    {
        PartitionPlan plan = PartitionPlan.WholeDisk(DiskPartitionStyle.Mbr, "exFAT", "DATOS");

        PartitionSpec only = Assert.Single(plan.Partitions);
        Assert.IsType<PartitionSize.Remainder>(only.Size);
        Assert.Equal("exFAT", only.FileSystem);
        Assert.True(plan.Validate(Disk64Gb).Ok);
    }

    /// <summary>El caso que motiva todo el tier: FAT32 para la BIOS y el resto aprovechable.</summary>
    [Fact]
    public void Fat32PlusRemainder_IsTheShapeTheFeatureExistsFor()
    {
        PartitionPlan plan = Plan(Exact(Gib, "FAT32", "BIOS"), Rest("exFAT", "DATOS"));

        Assert.True(plan.Validate(Disk64Gb).Ok);
    }

    [Fact]
    public void ValidPlan_ReportsNoGuiltyPartition()
        => Assert.Equal(-1, Plan(Exact(Gib), Rest()).Validate(Disk64Gb).PartitionIndex);

    // ── Estructura ────────────────────────────────────────────────

    [Fact]
    public void NoPartitions_IsRejected()
        => Assert.Equal(PlanProblem.NoPartitions, ProblemOf(Plan()));

    /// <summary>MBR admite 4 primarias. Y MBR es lo que se elige en toda memoria USB, así que este es
    /// <b>el</b> tope real de la función, no un caso de laboratorio.</summary>
    [Fact]
    public void MoreThanFourPartitionsOnMbr_IsRejected()
    {
        var plan = new PartitionPlan(DiskPartitionStyle.Mbr,
            [Exact(Gib), Exact(Gib), Exact(Gib), Exact(Gib), Rest()]);

        Assert.Equal(PlanProblem.TooManyForMbr, plan.Validate(Disk64Gb).Problem);
    }

    [Fact]
    public void FourPartitionsOnMbr_IsAccepted()
    {
        var plan = new PartitionPlan(DiskPartitionStyle.Mbr, [Exact(Gib), Exact(Gib), Exact(Gib), Rest()]);

        Assert.True(plan.Validate(Disk64Gb).Ok);
    }

    [Fact]
    public void FivePartitionsOnGpt_IsFine()
        => Assert.True(Plan(Exact(Gib), Exact(Gib), Exact(Gib), Exact(Gib), Rest()).Validate(Disk64Gb).Ok);

    [Fact]
    public void TwoRemainders_IsRejected()
    {
        PlanValidation v = Plan(Rest(), Rest()).Validate(Disk64Gb);

        Assert.Equal(PlanProblem.MultipleRemainders, v.Problem);
        Assert.Equal(1, v.PartitionIndex);
    }

    /// <summary>«El resto» solo tiene sentido al final: si va antes, lo que viene después no tiene sitio.</summary>
    [Fact]
    public void RemainderBeforeTheEnd_IsRejected()
    {
        PlanValidation v = Plan(Rest(), Exact(Gib)).Validate(Disk64Gb);

        Assert.Equal(PlanProblem.RemainderIsNotLast, v.Problem);
        Assert.Equal(0, v.PartitionIndex);
    }

    [Theory]
    [InlineData(0L)]
    [InlineData(-1L)]
    public void NonPositiveSize_IsRejected(long bytes)
        => Assert.Equal(PlanProblem.NonPositiveSize, ProblemOf(Plan(Exact(bytes))));

    [Theory]
    [InlineData("ext4")]
    [InlineData("NT;FS")]
    [InlineData("")]
    [InlineData("ntfs")]   // sensible a mayúsculas: es lo que se interpola en el comando de PowerShell
    public void UnknownFileSystem_IsRejected(string fs)
        => Assert.Equal(PlanProblem.UnknownFileSystem, ProblemOf(Plan(Rest(fs))));

    [Fact]
    public void SupportedFileSystems_AreTheFiveTheUiOffers()
        => Assert.Equal(["NTFS", "exFAT", "ReFS", "FAT32", "FAT"], PartitionPlan.SupportedFileSystems);

    /// <summary>La etiqueta se valida por partición y contra SU sistema de archivos: 20 caracteres valen en
    /// NTFS y no en FAT32, y el plan no puede dar por buena una etiqueta que <c>Format-Volume</c> rechazará.</summary>
    [Fact]
    public void LabelTooLongForItsOwnFileSystem_IsRejected()
    {
        Assert.True(Plan(Rest("NTFS", "VEINTE-CARACTERES-XX")).Validate(Disk64Gb).Ok);
        Assert.Equal(PlanProblem.InvalidLabel, ProblemOf(Plan(Rest("FAT32", "VEINTE-CARACTERES-XX"))));
    }

    [Fact]
    public void LabelWithForbiddenCharacters_IsRejected()
        => Assert.Equal(PlanProblem.InvalidLabel, ProblemOf(Plan(Rest("NTFS", "a/b"))));

    // ── Dimensiones ───────────────────────────────────────────────

    [Fact]
    public void PlanBiggerThanTheDisk_IsRejected()
        => Assert.Equal(PlanProblem.DoesNotFit, ProblemOf(Plan(Exact(32 * Gib), Exact(48 * Gib))));

    /// <summary>Cabe al byte, pero no con el margen de alineación. Sin esta regla, el plan pasaría la
    /// validación y <c>New-Partition</c> fallaría con el disco borrado.</summary>
    [Fact]
    public void PlanThatFitsOnlyWithoutTheAlignmentMargin_IsRejected()
    {
        Assert.Equal(PlanProblem.DoesNotFit, ProblemOf(Plan(Exact(Disk64Gb)), Disk64Gb));
        Assert.True(Plan(Exact(Disk64Gb - ReinitPlan.PartitionReserveBytes)).Validate(Disk64Gb).Ok);
    }

    /// <summary>Un «resto» al que no le queda casi nada no es una partición útil: <c>Format-Volume</c>
    /// fallaría sobre ella.</summary>
    [Fact]
    public void RemainderLeftTooSmall_IsRejected()
    {
        long almostEverything = Disk64Gb - PartitionPlan.MinPartitionBytes;
        PlanValidation v = Plan(Exact(almostEverything), Rest()).Validate(Disk64Gb);

        Assert.Equal(PlanProblem.PartitionTooSmall, v.Problem);
        Assert.Equal(1, v.PartitionIndex);   // la culpable es la segunda, no la que se pasó
    }

    /// <summary>
    /// El motivo por el que «el resto» se calcula al validar aunque se delegue al ejecutar: para saber si
    /// un FAT32 cabe en 32 GB hay que conocer su tamaño, y el de «el resto» solo se sabe con el del disco.
    /// </summary>
    [Fact]
    public void Fat32AsTheRemainderOfABigDisk_IsRejected()
    {
        PlanValidation v = Plan(Exact(Gib, "NTFS"), Rest("FAT32")).Validate(256 * Gib);

        Assert.Equal(PlanProblem.Fat32VolumeTooLarge, v.Problem);
        Assert.Equal(1, v.PartitionIndex);
    }

    [Fact]
    public void Fat32PartitionOverTheWindowsLimit_IsRejected()
        => Assert.Equal(PlanProblem.Fat32VolumeTooLarge,
                        ProblemOf(Plan(Exact(FormatLogic.Fat32MaxBytes + 1, "FAT32")), 256 * Gib));

    [Fact]
    public void Fat32PartitionExactlyAtTheLimit_IsAccepted()
        => Assert.True(Plan(Exact(FormatLogic.Fat32MaxBytes, "FAT32")).Validate(256 * Gib).Ok);

    [Fact]
    public void FatPartitionOverTwoGigabytes_IsRejected()
        => Assert.Equal(PlanProblem.FatVolumeTooLarge,
                        ProblemOf(Plan(Exact(PartitionPlan.FatMaxBytes + 1, "FAT"))));

    [Fact]
    public void MbrCannotAddressADiskOverTwoTerabytes()
    {
        var plan = new PartitionPlan(DiskPartitionStyle.Mbr, [Rest()]);

        Assert.Equal(PlanProblem.MbrCannotAddressDisk, plan.Validate(ReinitPlan.MbrLimitBytes + 1).Problem);
    }

    // ── Tamaño de disco desconocido ───────────────────────────────

    /// <summary>
    /// <b>Reinicializar existe para unidades RAW</b>, donde <c>Get-Disk</c> puede no devolver el tamaño.
    /// Un plan de disco entero no lo necesita —<c>-UseMaximumSize</c> lo resuelve Windows—, así que
    /// exigirlo siempre bloquearía justo el caso de uso original.
    /// </summary>
    [Fact]
    public void WholeDiskPlan_IsValidEvenWithoutKnowingTheDiskSize()
        => Assert.True(PartitionPlan.WholeDisk(DiskPartitionStyle.Mbr, "NTFS", "").Validate(0).Ok);

    [Theory]
    [InlineData(0L)]
    [InlineData(-1L)]
    public void PlanWithAnExactSize_NeedsTheDiskSize(long disk)
        => Assert.Equal(PlanProblem.UnknownDiskSize, ProblemOf(Plan(Exact(Gib)), disk));

    [Fact]
    public void TwoPartitionPlan_NeedsTheDiskSize()
        => Assert.Equal(PlanProblem.UnknownDiskSize, ProblemOf(Plan(Exact(Gib), Rest()), 0));

    /// <summary>Aun sin tamaño de disco, lo estructural se sigue comprobando: un plan imposible es
    /// imposible antes de mirar cuánto mide nada.</summary>
    [Fact]
    public void StructuralProblems_AreReportedEvenWithoutTheDiskSize()
    {
        Assert.Equal(PlanProblem.NoPartitions, ProblemOf(Plan(), 0));
        Assert.Equal(PlanProblem.UnknownFileSystem, ProblemOf(Plan(Rest("ext4")), 0));
        Assert.Equal(PlanProblem.MultipleRemainders, ProblemOf(Plan(Rest(), Rest()), 0));
    }

    // ── EffectiveSizes ────────────────────────────────────────────

    [Fact]
    public void EffectiveSizes_ResolvesTheRemainderAgainstTheDisk()
    {
        long[] sizes = Plan(Exact(Gib), Rest()).EffectiveSizes(Disk64Gb);

        Assert.Equal(Gib, sizes[0]);
        Assert.Equal(Disk64Gb - Gib - 2 * ReinitPlan.PartitionReserveBytes, sizes[1]);
    }

    [Fact]
    public void EffectiveSizes_WithoutARemainder_IsJustTheExactSizes()
        => Assert.Equal([Gib, 2 * Gib], Plan(Exact(Gib), Exact(2 * Gib)).EffectiveSizes(Disk64Gb));

    // ── Sistema de archivos de la segunda partición (`T5-02`) ─────

    /// <summary>
    /// FAT32 y FAT quedan fuera del sobrante a propósito: el resto de un pendrive grande supera sus
    /// límites (32 GB y 2 GB), así que ofrecerlos sería ofrecer un fallo con el disco ya borrado. ReFS
    /// queda fuera por no ser un sistema para medios extraíbles.
    /// </summary>
    [Fact]
    public void SecondPartitionFileSystems_AreExFatAndNtfsWithExFatFirst()
        => Assert.Equal(["exFAT", "NTFS"], PartitionPlan.SecondPartitionFileSystems);

    [Theory]
    [InlineData("exFAT", "exFAT")]
    [InlineData("NTFS",  "NTFS")]
    public void NormalizeSecondPartitionFileSystem_KeepsAllowedValues(string fs, string expected)
        => Assert.Equal(expected, PartitionPlan.NormalizeSecondPartitionFileSystem(fs));

    /// <summary>Un <c>settings.json</c> editado a mano no puede colar un sistema de archivos que
    /// reventaría al formatear el sobrante.</summary>
    [Theory]
    [InlineData("FAT32")]
    [InlineData("FAT")]
    [InlineData("ReFS")]
    [InlineData("ext4")]
    [InlineData("ntfs")]
    [InlineData("")]
    [InlineData(null)]
    public void NormalizeSecondPartitionFileSystem_RejectsAnythingElse(string? fs)
        => Assert.Equal("exFAT", PartitionPlan.NormalizeSecondPartitionFileSystem(fs));

    /// <summary>
    /// El plan que produce `T5-02` sobre la USB de pruebas: FAT32 de 1 GB primero y el resto en exFAT.
    /// El disco no queda con nada sin asignar — que es el hueco que este tier existe para cerrar.
    /// </summary>
    [Fact]
    public void TheShapeT502Produces_IsValidAndLeavesNothingUnallocated()
    {
        const long stick = 29_360_128_000L;   // ~27,3 GiB: la USB de pruebas
        var plan = new PartitionPlan(DiskPartitionStyle.Mbr, [
            new PartitionSpec(new PartitionSize.Exact(Gib), "FAT32", "BIOS"),
            new PartitionSpec(new PartitionSize.Remainder(), "exFAT", "DATOS"),
        ]);

        Assert.True(plan.Validate(stick).Ok);

        long[] sizes = plan.EffectiveSizes(stick);
        Assert.Equal(stick, sizes[0] + sizes[1] + 2 * ReinitPlan.PartitionReserveBytes);
    }
}
