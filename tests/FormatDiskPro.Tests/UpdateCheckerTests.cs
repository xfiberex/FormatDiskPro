using FormatDiskPro;
using Xunit;

namespace FormatDiskPro.Tests;

/// <summary>
/// Pruebas de la comparación de versiones para actualizaciones (lógica pura, sin red).
/// </summary>
public sealed class UpdateCheckerTests
{
    [Theory]
    [InlineData("v1.2.3", 1, 2, 3)]
    [InlineData("1.2.3", 1, 2, 3)]
    [InlineData("V2.0.0", 2, 0, 0)]
    [InlineData("1.2", 1, 2, 0)]
    [InlineData("1.4.0-beta", 1, 4, 0)]
    [InlineData("2.1.0+build.7", 2, 1, 0)]
    public void TryParseTag_ParsesValidTags(string tag, int major, int minor, int build)
    {
        Assert.True(UpdateChecker.TryParseTag(tag, out var v));
        Assert.Equal(major, v.Major);
        Assert.Equal(minor, v.Minor);
        Assert.Equal(build, v.Build);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("v")]
    [InlineData("latest")]
    public void TryParseTag_RejectsInvalidTags(string? tag)
        => Assert.False(UpdateChecker.TryParseTag(tag, out _));

    [Theory]
    [InlineData("v1.2.0", "1.1.0")]
    [InlineData("1.1.1", "1.1.0")]
    [InlineData("2.0.0", "1.9.9")]
    [InlineData("v1.1.0", "1.0.0")]
    public void IsNewer_True_WhenTagGreater(string tag, string current)
        => Assert.True(UpdateChecker.IsNewer(tag, Version.Parse(current)));

    [Theory]
    [InlineData("v1.1.0", "1.1.0")]   // igual
    [InlineData("1.0.0", "1.1.0")]    // menor
    [InlineData("v1.1.0", "2.0.0")]   // menor
    [InlineData("garbage", "1.1.0")]  // no parseable
    public void IsNewer_False_WhenNotGreater(string tag, string current)
        => Assert.False(UpdateChecker.IsNewer(tag, Version.Parse(current)));
}
