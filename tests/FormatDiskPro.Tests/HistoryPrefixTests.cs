using System.Text.RegularExpressions;
using FormatDiskPro;
using Xunit;

namespace FormatDiskPro.Tests;

/// <summary>
/// Barrido de las palabras clave con las que la app escribe en el historial (`T13-07`).
/// </summary>
/// <remarks>
/// <para><b>El hueco que cierra.</b> La clasificación tenía cinco categorías y la app escribía
/// <b>dieciséis</b> palabras clave. Las otras once caían en <c>Other</c> —«Operación»—, así que el filtro
/// del historial no podía aislar una <i>reinicialización</i>, que borra el disco físico entero. Nadie se
/// enteró porque añadir un <c>Log("NUEVO …")</c> en cualquier parte de la app no rompía nada.</para>
///
/// <para><b>Por qué barre el código y no una lista.</b> Por lo mismo que <c>TextContrastTests</c> recorre
/// el XAML y <c>LocalizationCoverageTests</c> las tablas de cadenas: una lista mide lo que alguien se
/// acordó de apuntar. Esta prueba mide <b>lo que la app escribe de verdad</b>, así que registrar algo
/// nuevo obliga a decidir su categoría.</para>
/// </remarks>
public sealed class HistoryPrefixTests
{
    private static string SourceRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            string src = Path.Combine(dir.FullName, "src", "FormatDiskPro");
            if (Directory.Exists(src)) return src;
            dir = dir.Parent;
        }
        throw new InvalidOperationException(
            $"No se encontró src/FormatDiskPro subiendo desde {AppContext.BaseDirectory}.");
    }

    // Cualquier llamada .Log("PALABRA… o .Log($"PALABRA…, venga del servicio o de una variable
    // (_history, History, _services.History…). La palabra clave es lo primero de la línea escrita.
    private static readonly Regex LoggedPrefix =
        new(@"\.Log\(\s*\$?""([A-Z][A-Z0-9]*)", RegexOptions.Compiled);

    private static List<(string File, string Prefix)> LoggedPrefixes()
    {
        var found = new List<(string, string)>();
        foreach (string file in Directory.EnumerateFiles(SourceRoot(), "*.cs", SearchOption.AllDirectories))
        {
            if (file.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.Ordinal)
             || file.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}", StringComparison.Ordinal))
                continue;

            foreach (Match m in LoggedPrefix.Matches(File.ReadAllText(file)))
                found.Add((Path.GetFileName(file), m.Groups[1].Value));
        }
        return [.. found.Distinct()];
    }

    /// <summary>
    /// Toda palabra clave que la app escribe tiene categoría decidida.
    /// </summary>
    [Fact]
    public void EveryLoggedPrefix_HasACategory()
    {
        var prefixes = LoggedPrefixes();

        // Un barrido que no encuentra nada pasaría siempre.
        Assert.True(prefixes.Count >= 15,
            $"Solo se encontraron {prefixes.Count} palabras clave: el barrido no está mirando donde debe.");

        var missing = prefixes
            .Where(p => !HistoryEntry.CategoryByPrefix.ContainsKey(p.Prefix))
            .Select(p => $"{p.File}: {p.Prefix}")
            .Distinct()
            .ToList();

        Assert.True(missing.Count == 0,
            "La app escribe en el historial palabras clave sin categoría, así que saldrán como «Operación» "
            + "y el filtro no podrá aislarlas. Decide la suya en HistoryEntry.CategoryByPrefix:\n  "
            + string.Join("\n  ", missing));
    }

    /// <summary>
    /// Y al revés: la tabla no declara palabras clave que ya no se escriben.
    /// </summary>
    /// <remarks>
    /// Una entrada de más no rompe nada hoy, pero es una promesa que el código ya no cumple: el filtro
    /// ofrecería una categoría que no puede tener resultados.
    /// </remarks>
    [Fact]
    public void TheTable_DeclaresNothingTheAppNoLongerWrites()
    {
        var written = LoggedPrefixes().Select(p => p.Prefix).ToHashSet(StringComparer.Ordinal);

        var stale = HistoryEntry.CategoryByPrefix.Keys.Where(k => !written.Contains(k)).ToList();

        Assert.True(stale.Count == 0,
            "HistoryEntry.CategoryByPrefix declara palabras clave que la app ya no escribe:\n  "
            + string.Join("\n  ", stale));
    }

    /// <summary>Las entradas que hay hoy se clasifican donde se decidió.</summary>
    [Theory]
    [InlineData("REINIT I: -> J: (MBR, NTFS)",                 HistoryCategory.Reinit)]
    [InlineData("REINIT REJECTED F: Fat32VolumeTooLarge",      HistoryCategory.Reinit)]
    [InlineData("CHKDSK I: repair=False code=0 result=OK",     HistoryCategory.CheckDisk)]
    [InlineData("BENCH I: seq w=31 r=118",                     HistoryCategory.Benchmark)]
    [InlineData("UNLOCK I: OK",                                HistoryCategory.WriteProtect)]
    [InlineData("HEALTH ERROR I: la unidad no responde",       HistoryCategory.Health)]
    [InlineData("DISKSIZE ERROR I: la unidad no responde",     HistoryCategory.Health)]
    [InlineData("WHATSNEW ERROR: no se pudo leer",             HistoryCategory.Update)]
    [InlineData("SETTINGS UNREADABLE: se apartó a …",          HistoryCategory.App)]
    [InlineData("CRASH: System.Exception: algo",               HistoryCategory.App)]
    [InlineData("OTRACOSA I: de otro programa",                HistoryCategory.Other)]
    public void Category_ComesFromTheKeyword(string message, HistoryCategory expected)
        => Assert.Equal(expected, Parse(message).Category);

    /// <summary>
    /// Una caída es un error. Salía como «Info» porque su línea no lleva la palabra ERROR.
    /// </summary>
    [Fact]
    public void ACrash_IsAnError()
        => Assert.Equal(HistoryResult.Error, Parse("CRASH: System.Exception: algo").Result);

    /// <summary>
    /// La palabra clave no se confunde con otra más larga que empiece igual.
    /// </summary>
    [Theory]
    [InlineData("VERIFYX I: de otro programa")]
    [InlineData("CRASHED I: de otro programa")]
    public void ALongerWord_IsNotTheKeyword(string message)
        => Assert.Equal(HistoryCategory.Other, Parse(message).Category);

    private static HistoryEntry Parse(string message)
    {
        var entry = HistoryEntry.Parse($"2026-09-19 10:00:00\t{message}");
        Assert.NotNull(entry);
        return entry;
    }
}
