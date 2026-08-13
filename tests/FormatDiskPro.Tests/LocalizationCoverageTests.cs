using System.Text.RegularExpressions;
using FormatDiskPro;
using Xunit;

namespace FormatDiskPro.Tests;

/// <summary>
/// Red de seguridad para la i18n. <c>LocalizationTests.EveryEntry_HasFiveNonEmptyTranslations</c> solo
/// recorre <see cref="L.Map"/>, así que daba luz verde mientras las descripciones de sistema de archivos
/// (dos diccionarios ES/EN dentro de <c>MainWindow</c>) y los nombres de los presets integrados (texto
/// fijo en español dentro de <c>Core/Presets.cs</c>) se mostraban sin traducir en PT/FR/IT. El problema no
/// era que faltara un test, sino que el que había cubría menos de lo que su nombre sugería: comprobaba
/// que lo registrado estuviera traducido, no que todo lo mostrado estuviera registrado.
///
/// Estas pruebas cierran ese hueco por los dos lados: una recorre el CÓDIGO FUENTE buscando tablas de
/// cadenas fuera de <c>Localization/</c>, y la otra ancla los presets integrados a claves reales.
/// </summary>
[Collection(LanguageCollection.Name)]
public sealed class LocalizationCoverageTests
{
    /// <summary>Raíz del repo, localizada subiendo hasta encontrar <c>FormatDiskPro.slnx</c>.</summary>
    private static string RepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            if (File.Exists(Path.Combine(dir.FullName, "FormatDiskPro.slnx"))) return dir.FullName;
            dir = dir.Parent;
        }
        throw new InvalidOperationException(
            $"No se encontró la raíz del repo (FormatDiskPro.slnx) subiendo desde {AppContext.BaseDirectory}.");
    }

    /// <summary>Fuentes de la app, sin lo generado (<c>obj/</c>, <c>bin/</c>).</summary>
    private static List<string> AppSourceFiles()
    {
        string src = Path.Combine(RepoRoot(), "src", "FormatDiskPro");
        return [.. Directory.EnumerateFiles(src, "*.cs", SearchOption.AllDirectories)
            .Where(f => !f.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.Ordinal)
                     && !f.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}", StringComparison.Ordinal))];
    }

    // `Dictionary<string, string>` y `Dictionary<string, string[]>`: la forma que tomaron FsDescEs/FsDescEn.
    private static readonly Regex StringTable =
        new(@"Dictionary<\s*string\s*,\s*string\s*(\[\s*\])?\s*>", RegexOptions.Compiled);

    /// <summary>
    /// Ninguna tabla de cadenas fuera de <c>Localization/</c>. Es la firma exacta del fallo original: un
    /// diccionario por idioma junto al código que lo pinta, invisible para el test de completitud.
    /// Si algún día hace falta un diccionario legítimo de <c>string→string</c> fuera de esa carpeta (uno
    /// que no contenga texto de cara al usuario), la respuesta correcta es discutirlo y añadir aquí una
    /// excepción nombrada, no borrar la prueba.
    /// </summary>
    [Fact]
    public void NoStringTablesOutsideLocalization()
    {
        var files = AppSourceFiles();

        // Un barrido que no encuentra ficheros pasaría siempre: eso es lo que hay que evitar aquí.
        Assert.True(files.Count >= 15, $"Solo se encontraron {files.Count} fuentes: el barrido no está mirando donde debe.");

        string localizationDir = $"{Path.DirectorySeparatorChar}Localization{Path.DirectorySeparatorChar}";
        var offenders = new List<string>();

        foreach (string file in files.Where(f => !f.Contains(localizationDir, StringComparison.Ordinal)))
        {
            string[] lines = File.ReadAllLines(file);
            for (int i = 0; i < lines.Length; i++)
                if (StringTable.IsMatch(lines[i]))
                    offenders.Add($"{Path.GetFileName(file)}:{i + 1}: {lines[i].Trim()}");
        }

        Assert.True(offenders.Count == 0,
            "Tabla(s) de cadenas fuera de Localization/ — si contienen texto de cara al usuario, PT/FR/IT " +
            "se quedarán sin traducir y el resto de la suite no lo notará:\n  " + string.Join("\n  ", offenders));
    }

    /// <summary>
    /// El propio <c>Localization.cs</c> SÍ contiene una tabla de cadenas. Sin esto,
    /// <see cref="NoStringTablesOutsideLocalization"/> seguiría en verde aunque el patrón dejara de
    /// reconocer la forma que busca — un barrido que ya no detecta nada no se distingue de uno limpio.
    /// </summary>
    [Fact]
    public void StringTableDetector_MatchesTheRealLocalizationTable()
    {
        string map = Path.Combine(RepoRoot(), "src", "FormatDiskPro", "Localization", "Localization.cs");
        Assert.True(StringTable.IsMatch(File.ReadAllText(map)),
            "El patrón ya no reconoce el diccionario de Localization.cs: NoStringTablesOutsideLocalization " +
            "estaría pasando sin comprobar nada.");
    }

    /// <summary>
    /// Los cinco presets integrados se muestran traducidos. Sus nombres estaban fijos en español dentro de
    /// <c>Presets.All</c> y el menú los pintaba tal cual en los cinco idiomas.
    /// </summary>
    [Fact]
    public void BuiltInPresets_ResolveKnownLocalizationKeys()
    {
        Assert.All(Presets.All, p =>
        {
            Assert.False(string.IsNullOrEmpty(p.NameKey), $"El preset integrado '{p.Name}' no tiene clave de traducción.");
            Assert.True(L.Map.ContainsKey(p.NameKey!), $"La clave '{p.NameKey}' del preset '{p.Name}' no existe en L.Map.");
        });
    }

    /// <summary>
    /// Y se muestran DISTINTOS en cada idioma: que exista la clave no basta si las cinco entradas repiten
    /// el español, que es la forma en que el bug volvería a colarse.
    /// </summary>
    [Fact]
    public void BuiltInPresets_DisplayNameFollowsActiveLanguage()
    {
        var prev = L.Current;
        try
        {
            foreach (var preset in Presets.All)
            {
                L.Set(AppLang.Es);
                string spanish = Presets.DisplayName(preset);
                Assert.Equal(preset.Name, spanish);   // el Name del record es la reserva en español

                L.Set(AppLang.En);
                Assert.False(string.IsNullOrWhiteSpace(Presets.DisplayName(preset)));

                // Al menos un idioma debe diferir del español; si los cinco coinciden, no se tradujo nada.
                bool anyDifferent = false;
                foreach (AppLang lang in (AppLang[])[AppLang.En, AppLang.Pt, AppLang.Fr, AppLang.It])
                {
                    L.Set(lang);
                    if (Presets.DisplayName(preset) != spanish) { anyDifferent = true; break; }
                }
                Assert.True(anyDifferent, $"'{preset.Name}' muestra el mismo texto en los cinco idiomas: sin traducir.");
            }
        }
        finally { L.Set(prev); }
    }

    /// <summary>
    /// Los presets del usuario NO se traducen: su nombre lo escribió una persona y debe mostrarse literal
    /// en cualquier idioma. Es el contrapeso de <see cref="BuiltInPresets_DisplayNameFollowsActiveLanguage"/>.
    /// </summary>
    [Fact]
    public void UserPresets_DisplayNameIsTheirOwnName()
    {
        var mine = new FormatPreset("Mi USB de trabajo", "exFAT", 131072, true, false, false);
        Assert.Null(mine.NameKey);

        var prev = L.Current;
        try
        {
            foreach (AppLang lang in Enum.GetValues<AppLang>())
            {
                L.Set(lang);
                Assert.Equal("Mi USB de trabajo", Presets.DisplayName(mine));
            }
        }
        finally { L.Set(prev); }
    }
}
