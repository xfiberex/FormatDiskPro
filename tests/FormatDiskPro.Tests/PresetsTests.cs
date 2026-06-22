using FormatDiskPro;
using Xunit;

namespace FormatDiskPro.Tests;

/// <summary>
/// Verifica que los presets predefinidos sean internamente consistentes
/// (sistemas de archivos válidos, compresión solo en NTFS, clúster positivo).
/// </summary>
public sealed class PresetsTests
{
    private static readonly HashSet<string> KnownFileSystems =
        ["NTFS", "exFAT", "ReFS", "FAT32", "FAT"];

    [Fact]
    public void All_IsNotEmpty()
        => Assert.NotEmpty(Presets.All);

    [Fact]
    public void All_UseKnownFileSystems()
        => Assert.All(Presets.All, p => Assert.Contains(p.FileSystem, KnownFileSystems));

    [Fact]
    public void All_HavePositiveAllocationUnit()
        => Assert.All(Presets.All, p => Assert.True(p.AllocationUnit > 0, $"'{p.Name}' tiene clúster no positivo"));

    [Fact]
    public void Compression_IsOnlyEnabledOnNtfs()
        => Assert.All(Presets.All, p =>
        {
            if (p.Compress) Assert.Equal("NTFS", p.FileSystem);
        });

    [Fact]
    public void All_HaveNonEmptyName()
        => Assert.All(Presets.All, p => Assert.False(string.IsNullOrWhiteSpace(p.Name)));

    [Theory]
    [InlineData("  Mi  preset ", "Mi preset")]   // recorta y colapsa espacios
    [InlineData("Normal", "Normal")]
    [InlineData("   ", "")]
    [InlineData(null, "")]
    public void NormalizeName_TrimsAndCollapses(string? input, string expected)
        => Assert.Equal(expected, Presets.NormalizeName(input));

    [Fact]
    public void IsNameAvailable_RejectsEmptyOrTooLong()
    {
        Assert.False(Presets.IsNameAvailable("", []));
        Assert.False(Presets.IsNameAvailable("   ", []));
        Assert.False(Presets.IsNameAvailable(new string('x', Presets.MaxNameLength + 1), []));
        Assert.True(Presets.IsNameAvailable(new string('x', Presets.MaxNameLength), []));
    }

    [Fact]
    public void IsNameAvailable_RejectsDuplicates_CaseInsensitiveAndNormalized()
    {
        string[] existing = ["Disco de datos Windows", "Mi preset"];
        Assert.False(Presets.IsNameAvailable("mi preset", existing));      // distinta caja
        Assert.False(Presets.IsNameAvailable("  Mi   preset ", existing)); // distinta separación
        Assert.True(Presets.IsNameAvailable("Otro", existing));
    }
}
