// SPDX-License-Identifier: GPL-3.0-only
using Xunit;

namespace FormatDiskPro.Tests;

/// <summary>
/// La línea de contexto de la tarjeta de unidad y de las confirmaciones (`AUD-072`, `AUD-075`): un dato que
/// no hay no deja un guion ni un separador suelto.
/// </summary>
public sealed class InfoLineTests
{
    [Fact]
    public void Join_SkipsMissingParts_WithoutLeavingASeparator()
        => Assert.Equal("FAT32 · USB / Extraíble", InfoLine.Join("FAT32", "USB / Extraíble", "–"));

    [Fact]
    public void Join_KeepsTheOrderOfThePartsThatHaveAValue()
        => Assert.Equal("Samsung SSD · 223,6 GB · SATA · SSD",
                        InfoLine.Join("Samsung SSD", null, "223,6 GB", "  ", "SATA", "SSD"));

    [Fact]
    public void Join_WithNothingToShow_IsEmpty()
        => Assert.Equal("", InfoLine.Join(null, "", " – "));

    [Theory]
    [InlineData(null, false)]
    [InlineData("", false)]
    [InlineData("   ", false)]
    [InlineData("–", false)]
    [InlineData(" – ", false)]
    [InlineData("NTFS", true)]
    [InlineData("0 B", true)]
    public void HasValue_TreatsBlankAndThePlaceholderAsMissing(string? part, bool expected)
        => Assert.Equal(expected, InfoLine.HasValue(part));
}
