// SPDX-License-Identifier: GPL-3.0-only
using Xunit;

namespace FormatDiskPro.Tests;

/// <summary>
/// <i>Novedades</i> lee las notas del CHANGELOG embebido, sin red (`AUD-067`).
/// </summary>
public sealed class ChangelogTests
{
    private const string Sample = """
        # Changelog

        ## [Sin publicar]

        ## [1.2.0] — 2026-01-02

        **Resumen.** Cosas nuevas.

        ### Corregido

        - Un arreglo.

        ## [1.1.0] — 2026-01-01

        - Lo de antes.

        [1.2.0]: https://example.invalid/v1.2.0
        """;

    [Fact]
    public void Section_TakesFromItsHeaderToTheNextOne()
    {
        string section = Changelog.Section(Sample, "1.2.0");

        Assert.StartsWith("**Resumen.** Cosas nuevas.", section, StringComparison.Ordinal);
        Assert.EndsWith("- Un arreglo.", section, StringComparison.Ordinal);
        Assert.Contains("### Corregido", section, StringComparison.Ordinal);   // «###» no corta: solo «## »
        Assert.DoesNotContain("Lo de antes", section, StringComparison.Ordinal);
    }

    [Fact]
    public void Section_OfTheOldestVersion_RunsToTheEnd()
        => Assert.StartsWith("- Lo de antes.", Changelog.Section(Sample, "1.1.0"), StringComparison.Ordinal);

    [Theory]
    [InlineData("9.9.9")]   // no está
    [InlineData("1.2")]     // no es un prefijo: «1.2» no puede encontrar «1.2.0»
    [InlineData("")]
    public void Section_OfAnUnknownVersion_IsEmpty(string version)
        => Assert.Equal("", Changelog.Section(Sample, version));

    [Fact]
    public void Section_WithWindowsLineEndings_KeepsNoCarriageReturns()
        => Assert.DoesNotContain('\r', Changelog.Section(Sample.Replace("\n", "\r\n"), "1.2.0"));

    /// <summary>
    /// El ejecutable lleva las notas de su propia versión. El corte ya exige que el CHANGELOG tenga la sección
    /// de la versión que publica; esto comprueba además que viaja dentro y que se encuentra.
    /// </summary>
    [Fact]
    public void TheEmbeddedChangelog_HasTheNotesOfThisVersion()
    {
        string notes = Changelog.ForVersion(AppInfo.VersionString);
        Assert.False(string.IsNullOrWhiteSpace(notes),
            $"El CHANGELOG embebido no tiene la sección [{AppInfo.VersionString}].");
    }
}
