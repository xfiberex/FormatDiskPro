using System.Text.RegularExpressions;
using FormatDiskPro;
using Windows.UI;
using Xunit;

namespace FormatDiskPro.Tests;

/// <summary>
/// Barrido de contraste de los colores de TEXTO que la app toma de Fluent por <c>ThemeResource</c>
/// (`T12-01`).
/// </summary>
/// <remarks>
/// <para><b>El hueco que cierra.</b> <c>SeverityPaletteTests</c> recorre <see cref="SeverityPalette.All"/>
/// y mide los colores que elegimos a mano — y la propia documentación de esa clase decía que eran «los
/// únicos que no salen de un <c>ThemeResource</c> de Windows». Eso dejaba fuera de la medición a los que
/// sí salen de uno, y por ahí entró el mismo fallo que aquel inventario existe para evitar:
/// <c>TextFillColorTertiaryBrush</c> da <b>3,29:1</b> en tema claro —por debajo del 4,5:1 de WCAG AA— y
/// pintaba dieciocho controles de la ventana principal, incluidas las pistas que explican qué sistema de
/// archivos y qué tamaño de clúster elegir.</para>
///
/// <para><b>Por qué recorre el XAML y no una lista.</b> Una lista mide lo que alguien se acordó de
/// apuntar; el barrido mide <b>lo que hay puesto</b>. Es la misma razón por la que
/// <see cref="SeverityPalette.All"/> es enumerable en vez de una tanda de comprobaciones sueltas: añadir
/// un color a la app tiene que ser lo mismo que ponerlo bajo test, sin un segundo paso que se pueda
/// olvidar.</para>
/// </remarks>
public sealed class TextContrastTests
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

    /// <summary>XAML de la app, sin lo generado (<c>obj/</c>, <c>bin/</c>).</summary>
    private static List<string> AppXamlFiles()
    {
        string src = Path.Combine(RepoRoot(), "src", "FormatDiskPro");
        return [.. Directory.EnumerateFiles(src, "*.xaml", SearchOption.AllDirectories)
            .Where(f => !f.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.Ordinal)
                     && !f.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}", StringComparison.Ordinal))];
    }

    private static readonly Regex TextBrush =
        new(@"TextFillColor(\w+)Brush", RegexOptions.Compiled);

    /// <summary>
    /// Todo pincel de texto de Fluent usado en el XAML de la app cumple el 4,5:1 de WCAG AA en los dos
    /// temas — salvo el de deshabilitado, que la propia norma exime.
    /// </summary>
    [Fact]
    public void EveryFluentTextBrushUsedInXaml_MeetsAA()
    {
        var files = AppXamlFiles();

        // Un barrido que no encuentra archivos pasaría siempre: eso es lo que hay que evitar aquí.
        Assert.True(files.Count >= 8, $"Solo se encontraron {files.Count} XAML: el barrido no está mirando donde debe.");

        var offenders = new List<string>();
        var unknown = new List<string>();

        foreach (string file in files)
        {
            string text = File.ReadAllText(file);
            foreach (Match m in TextBrush.Matches(text))
            {
                string brush = m.Value;
                if (FluentTextPalette.IsExemptFromNormalText(brush)) continue;

                foreach (bool dark in (bool[])[false, true])
                {
                    if (!FluentTextPalette.TryGet(brush, dark, out Color color))
                    {
                        unknown.Add($"{Path.GetFileName(file)}: {brush}");
                        break;
                    }

                    var entry = new PaletteColor(brush, color, dark, ContrastRequirement.NormalText);
                    double ratio = SeverityPalette.ContrastAgainstReference(entry);
                    if (ratio < entry.MinimumRatio)
                        offenders.Add(
                            $"{Path.GetFileName(file)}: {brush} en tema {(dark ? "oscuro" : "claro")} " +
                            $"da {ratio:F2}:1, por debajo de {entry.MinimumRatio:F1}:1");
                }
            }
        }

        Assert.True(unknown.Count == 0,
            "La app usa colores de texto que nadie ha medido. Declara su valor en FluentTextPalette:\n  "
            + string.Join("\n  ", unknown.Distinct()));

        Assert.True(offenders.Count == 0,
            "Colores de texto por debajo de WCAG AA (4.5:1). Sube el token o usa "
            + "SeverityPalette.MutedText, que está medido:\n  "
            + string.Join("\n  ", offenders.Distinct()));
    }

    /// <summary>
    /// El terciario de Fluent sigue midiéndose por debajo de AA en claro.
    /// </summary>
    /// <remarks>
    /// Ancla el número que justifica todo lo demás. Si una versión del Windows App SDK lo sube por encima
    /// de 4,5:1, este test falla y la decisión de `T12-01` deja de tener motivo — que es exactamente
    /// cuándo hay que revisarla, y no antes.
    /// </remarks>
    [Fact]
    public void FluentTertiary_IsStillBelowAA_InLightTheme()
    {
        Assert.True(FluentTextPalette.TryGet("TextFillColorTertiaryBrush", dark: false, out Color color));
        double ratio = SeverityPalette.ContrastAgainstReference(
            new PaletteColor("TextFillColorTertiary", color, false, ContrastRequirement.NormalText));

        Assert.True(ratio < 4.5,
            $"El terciario de Fluent da ahora {ratio:F2}:1 en claro. Si ya cumple AA, revisa si "
            + "SeverityPalette.MutedText sigue haciendo falta.");
    }

    /// <summary>
    /// El pincel del XAML es EL MISMO color que <see cref="SeverityPalette.MutedText"/>.
    /// </summary>
    /// <remarks>
    /// <para>Un <c>ResourceDictionary</c> no puede llamar a <c>Core</c>, así que los dos hex están
    /// duplicados a mano en <c>AppTheme.xaml</c>. Esta prueba es lo que impide que la copia se separe del
    /// original: sin ella, cambiar el gris en el XAML dejaría a la app pintando un color que el barrido
    /// de <c>SeverityPalette</c> no mide — que es, literalmente, cómo entró el fallo que `T12-01`
    /// arregla.</para>
    /// </remarks>
    [Fact]
    public void TheMutedBrushInXaml_MatchesTheMeasuredColor()
    {
        string theme = File.ReadAllText(
            Path.Combine(RepoRoot(), "src", "FormatDiskPro", "UI", "Theme", "AppTheme.xaml"));

        foreach ((string themeKey, bool dark) in ((string, bool)[])[("Light", false), ("Dark", true)])
        {
            var match = Regex.Match(
                theme,
                $@"<ResourceDictionary x:Key=""{themeKey}"">\s*<SolidColorBrush x:Key=""AppMutedTextBrush"" Color=""#([0-9A-Fa-f]{{6}})""");

            Assert.True(match.Success, $"Falta AppMutedTextBrush del tema {themeKey} en AppTheme.xaml.");

            Color expected = SeverityPalette.MutedText(dark);
            string hex = $"{expected.R:X2}{expected.G:X2}{expected.B:X2}";
            Assert.Equal(hex, match.Groups[1].Value.ToUpperInvariant());
        }
    }
}
