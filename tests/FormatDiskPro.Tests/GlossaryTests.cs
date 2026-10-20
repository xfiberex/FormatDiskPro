// SPDX-License-Identifier: GPL-3.0-only
using System.Text.RegularExpressions;
using Xunit;

namespace FormatDiskPro.Tests;

/// <summary>
/// El glosario de <c>.github/CONTRIBUTING.md</c> se cumple (`AUD-048`): ningún término descartado de un
/// idioma aparece en sus textos.
/// </summary>
/// <remarks>
/// La lista no está aquí, sino en la guía, entre <c>&lt;!-- glosario:inicio --&gt;</c> y
/// <c>&lt;!-- glosario:fin --&gt;</c>: así el documento que lee quien traduce y lo que se comprueba no pueden
/// decir cosas distintas. Las palabras se comparan enteras y sin distinguir mayúsculas, para que descartar
/// «removible» no tropiece con nada más largo.
/// </remarks>
public sealed partial class GlossaryTests
{
    private static readonly string[] Languages = ["ES", "EN", "PT", "FR", "IT"];

    /// <summary>Raíz del repo, localizada subiendo hasta encontrar <c>FormatDiskPro.slnx</c>.</summary>
    private static string RepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "FormatDiskPro.slnx"))) dir = dir.Parent;
        return dir?.FullName ?? throw new InvalidOperationException("No se encontró la raíz del repo.");
    }

    [GeneratedRegex(@"<!-- glosario:inicio -->(.*?)<!-- glosario:fin -->", RegexOptions.Singleline)]
    private static partial Regex GlossaryBlock();

    [GeneratedRegex(@"`([^`]+)`")]
    private static partial Regex Backticked();

    /// <summary>Los términos descartados de cada idioma, tal como los lista la guía.</summary>
    private static List<(string Lang, string Term)> DiscardedTerms()
    {
        string guide = File.ReadAllText(Path.Combine(RepoRoot(), ".github", "CONTRIBUTING.md"));
        var block = GlossaryBlock().Match(guide);
        Assert.True(block.Success, "CONTRIBUTING.md ya no tiene el bloque del glosario.");

        var terms = new List<(string, string)>();
        foreach (string line in block.Groups[1].Value.Split('\n'))
        {
            string[] cells = line.Split('|', StringSplitOptions.TrimEntries);
            // | Idioma | Se usa | Descartado |  →  "", idioma, se usa, descartado, ""
            if (cells.Length < 5 || !Languages.Contains(cells[1])) continue;
            foreach (Match m in Backticked().Matches(cells[3])) terms.Add((cells[1], m.Groups[1].Value));
        }
        return terms;
    }

    private static Regex Matcher(string term) =>
        new(@"(?<![\p{L}\p{N}])" + Regex.Escape(term) + @"(?![\p{L}\p{N}])", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

    [Fact]
    public void TheGuide_ListsDiscardedTermsForEveryLanguage()
    {
        var terms = DiscardedTerms();
        // Si el formato de la tabla cambia y la lectura deja de ver filas, la otra prueba pasaría sin mirar nada.
        Assert.All(Languages, lang => Assert.Contains(terms, t => t.Lang == lang));
    }

    [Fact]
    public void NoDiscardedTerm_AppearsInItsLanguage()
    {
        var hits = new List<string>();
        foreach (var (lang, term) in DiscardedTerms())
        {
            int index = Array.IndexOf(Languages, lang);
            var rx = Matcher(term);
            foreach (var (key, values) in L.Map)
                if (index < values.Length && rx.IsMatch(values[index]))
                    hits.Add($"{lang} «{term}» en {key}: {values[index]}");
        }
        Assert.True(hits.Count == 0, "Términos descartados por el glosario de CONTRIBUTING.md:\n  " + string.Join("\n  ", hits));
    }

    /// <summary>La comparación es de palabra entera: descartar «removible» no puede tropezar con otra cosa.</summary>
    [Theory]
    [InlineData("removible", "USB / Removible", true)]
    [InlineData("removible", "unidades removibles", false)]
    [InlineData("GB", "4 GB", true)]
    [InlineData("GB", "GBit", false)]
    [InlineData("disco de sistema", "el disco de sistema.", true)]
    public void Matcher_MatchesWholeWordsOnly(string term, string text, bool expected)
        => Assert.Equal(expected, Matcher(term).IsMatch(text));
}
