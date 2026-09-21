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
///
/// <para><b>Tres puntos ciegos que tuvo</b> (`T13-02`), y por los que pasaron cinco textos por debajo de
/// AA: leía los nombres sin anclar (el acento salía medido como texto primario), solo buscaba pinceles
/// llamados <c>TextFillColor…</c> (el rojo de error, <c>SystemFillColorCriticalBrush</c>, pinta texto y no
/// se medía) y no miraba la <c>Opacity</c>, que cambia el color de verdad sin cambiar el pincel.</para>
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

    /// <summary>Code-behind de la interfaz (<c>UI/*.cs</c>), sin lo generado.</summary>
    private static List<string> AppUiCodeFiles()
    {
        string ui = Path.Combine(RepoRoot(), "src", "FormatDiskPro", "UI");
        return [.. Directory.EnumerateFiles(ui, "*.cs", SearchOption.AllDirectories)
            .Where(f => !f.EndsWith(".g.cs", StringComparison.Ordinal) && !f.EndsWith(".g.i.cs", StringComparison.Ordinal))];
    }

    /// <summary>
    /// El XAML sin comentarios. El barrido de `T12-01` leía también los comentarios, y en uno de
    /// <c>AppTheme.xaml</c> «medía» un color que no pinta nada (`T13-02`).
    /// </summary>
    /// <remarks>Conserva los saltos de línea, para que los números de línea de los fallos sigan siendo los del archivo.</remarks>
    internal static string WithoutXmlComments(string xaml)
        => Regex.Replace(xaml, "<!--.*?-->", m => new string('\n', m.Value.Count(c => c == '\n')), RegexOptions.Singleline);

    // Todos anclan el nombre ENTERO. `TextFillColor(\w+)Brush` sin anclar encontraba
    // «TextFillColorPrimaryBrush» dentro de «AccentTextFillColorPrimaryBrush» y medía el acento de cada
    // usuario como si fuera texto casi negro (16,65:1): un aprobado falso (`T13-02`).
    internal static readonly Regex AnyTextFillBrush =
        new(@"\b\w*TextFillColor\w*Brush\b", RegexOptions.Compiled);
    private static readonly Regex ForegroundAttribute =
        new(@"\bForeground=""\{(?:Theme|Static)Resource\s+(\w+)\s*\}""", RegexOptions.Compiled);
    private static readonly Regex ForegroundSetter =
        new(@"<Setter\s+Property=""Foreground""\s+Value=""\{(?:Theme|Static)Resource\s+(\w+)\s*\}""", RegexOptions.Compiled);
    private static readonly Regex ForegroundFromCode =
        new(@"\bForeground\s*=\s*\(\s*Brush\s*\)[^;]*?Resources\[""(\w+)""\]", RegexOptions.Compiled);

    /// <summary>
    /// Todo pincel que la app usa como color de texto, con el archivo donde aparece: todo
    /// <c>Foreground</c> de un recurso —en atributo, en <c>Setter</c> o desde código— y todo
    /// <c>*TextFillColor*Brush</c> nombrado en el XAML, esté donde esté.
    /// </summary>
    private static List<(string File, string Brush)> TextBrushUsages()
    {
        var usages = new List<(string, string)>();

        foreach (string file in AppXamlFiles())
        {
            string xaml = WithoutXmlComments(File.ReadAllText(file));
            string name = Path.GetFileName(file);
            foreach (Regex rx in (Regex[])[ForegroundAttribute, ForegroundSetter])
                foreach (Match m in rx.Matches(xaml)) usages.Add((name, m.Groups[1].Value));
            foreach (Match m in AnyTextFillBrush.Matches(xaml)) usages.Add((name, m.Value));
        }

        foreach (string file in AppUiCodeFiles())
            foreach (Match m in ForegroundFromCode.Matches(File.ReadAllText(file)))
                usages.Add((Path.GetFileName(file), m.Groups[1].Value));

        return [.. usages.Distinct()];
    }

    /// <summary>
    /// Color real de un pincel de texto: los de Fluent, de <see cref="FluentTextPalette"/>; el gris propio,
    /// de <see cref="SeverityPalette.MutedText"/>, del que <see cref="TheMutedBrushInXaml_MatchesTheMeasuredColor"/>
    /// garantiza que es copia exacta.
    /// </summary>
    private static bool TryResolve(string brush, bool dark, out Color color)
    {
        if (brush == "AppMutedTextBrush")
        {
            color = SeverityPalette.MutedText(dark);
            return true;
        }
        return FluentTextPalette.TryGet(brush, dark, out color);
    }

    /// <summary>
    /// Todo color de texto que la app toma de un recurso cumple el 4,5:1 de WCAG AA en los dos temas, o
    /// está exento con su motivo escrito en <see cref="FluentTextPalette.ExemptionReason"/>.
    /// </summary>
    [Fact]
    public void EveryTextBrushUsed_MeetsAA()
    {
        var files = AppXamlFiles();

        // Un barrido que no encuentra archivos pasaría siempre: eso es lo que hay que evitar aquí.
        Assert.True(files.Count >= 8, $"Solo se encontraron {files.Count} XAML: el barrido no está mirando donde debe.");

        var usages = TextBrushUsages();
        Assert.Contains(usages, u => u.Brush == "SystemFillColorCriticalBrush");

        var offenders = new List<string>();
        var unknown = new List<string>();

        foreach ((string file, string brush) in usages)
        {
            if (FluentTextPalette.ExemptionReason(brush) is not null) continue;

            foreach (bool dark in (bool[])[false, true])
            {
                if (!TryResolve(brush, dark, out Color color))
                {
                    unknown.Add($"{file}: {brush}");
                    break;
                }

                var entry = new PaletteColor(brush, color, dark, ContrastRequirement.NormalText);
                double ratio = SeverityPalette.ContrastAgainstReference(entry);
                if (ratio < entry.MinimumRatio)
                    offenders.Add(
                        $"{file}: {brush} en tema {(dark ? "oscuro" : "claro")} " +
                        $"da {ratio:F2}:1, por debajo de {entry.MinimumRatio:F1}:1");
            }
        }

        Assert.True(unknown.Count == 0,
            "La app usa colores de texto que nadie ha medido. Declara su valor en FluentTextPalette, o su "
            + "excepción, con el motivo, en FluentTextPalette.ExemptionReason:\n  "
            + string.Join("\n  ", unknown.Distinct()));

        Assert.True(offenders.Count == 0,
            "Colores de texto por debajo de WCAG AA (4.5:1). Sube el token o usa "
            + "SeverityPalette.MutedText, que está medido:\n  "
            + string.Join("\n  ", offenders.Distinct()));
    }

    /// <summary>
    /// El nombre del pincel se lee entero: el acento no se confunde con el texto primario.
    /// </summary>
    [Theory]
    [InlineData("Foreground=\"{ThemeResource AccentTextFillColorPrimaryBrush}\"", "AccentTextFillColorPrimaryBrush")]
    [InlineData("Value=\"{ThemeResource TextFillColorSecondaryBrush}\"", "TextFillColorSecondaryBrush")]
    public void TheBrushName_IsReadWhole(string xaml, string expected)
        => Assert.Equal(new[] { expected }, AnyTextFillBrush.Matches(xaml).Select(m => m.Value).ToArray());

    /// <summary>Lo comentado no se mide: no pinta nada.</summary>
    [Fact]
    public void XmlComments_AreNotSwept()
        => Assert.Empty(AnyTextFillBrush.Matches(WithoutXmlComments(
            "<!-- usa TextFillColorTertiaryBrush -->\n<TextBlock Text=\"x\" />")));

    // `Opacity` en un TextBlock: en XAML (atributo del elemento, o Setter de un estilo de TextBlock) y en
    // código (inicializador de un `new TextBlock`).
    private static readonly Regex TextBlockOpacityAttribute =
        new(@"<TextBlock\b[^>]*?\bOpacity=""[^""]*""", RegexOptions.Compiled | RegexOptions.Singleline);
    private static readonly Regex TextBlockStyle =
        new(@"<Style\b[^>]*\bTargetType=""TextBlock""[^>]*>(.*?)</Style>", RegexOptions.Compiled | RegexOptions.Singleline);
    private static readonly Regex OpacitySetter =
        new(@"<Setter\s+Property=""Opacity""", RegexOptions.Compiled);
    private static readonly Regex NewTextBlockWithOpacity =
        new(@"new\s+TextBlock\s*(?:\(\s*\))?\s*\{[^}]*?\bOpacity\s*=", RegexOptions.Compiled | RegexOptions.Singleline);

    /// <summary>
    /// Ningún texto se atenúa con <c>Opacity</c>.
    /// </summary>
    /// <remarks>
    /// <para><b>Por qué se prohíbe en vez de medirse</b> (`T13-01`/`T13-02`). La opacidad compone el color
    /// con lo que haya DETRÁS, y el barrido solo sabe medir un pincel contra el fondo de referencia: un
    /// <c>TextFillColorPrimaryBrush</c> al 0,55 salía como 16,65:1 y en pantalla daba 3,83:1. Así entraron
    /// cinco textos por debajo de AA, entre ellos el cronómetro de las operaciones largas. Un gris que se
    /// quiere más suave tiene su pincel medido: <c>AppMutedTextBrush</c> o
    /// <c>TextFillColorSecondaryBrush</c>.</para>
    /// <para><b>Lo que no ve:</b> la opacidad puesta en un contenedor (un <c>StackPanel</c> atenúa todo lo
    /// que lleva dentro) o asignada con <c>x.Opacity = …</c> fuera de un inicializador.</para>
    /// </remarks>
    [Fact]
    public void NoTextIsDimmedWithOpacity()
    {
        var offenders = new List<string>();

        foreach (string file in AppXamlFiles())
        {
            string xaml = WithoutXmlComments(File.ReadAllText(file));
            string name = Path.GetFileName(file);

            foreach (Match m in TextBlockOpacityAttribute.Matches(xaml))
                offenders.Add($"{name}:{LineOf(xaml, m.Index)}: TextBlock con Opacity");
            foreach (Match style in TextBlockStyle.Matches(xaml))
                if (OpacitySetter.IsMatch(style.Groups[1].Value))
                    offenders.Add($"{name}:{LineOf(xaml, style.Index)}: estilo de TextBlock con Opacity");
        }

        foreach (string file in AppUiCodeFiles())
        {
            string code = File.ReadAllText(file);
            foreach (Match m in NewTextBlockWithOpacity.Matches(code))
                offenders.Add($"{Path.GetFileName(file)}:{LineOf(code, m.Index)}: new TextBlock con Opacity");
        }

        Assert.True(offenders.Count == 0,
            "Texto atenuado con Opacity: su contraste real no lo mide nadie. Usa AppMutedTextBrush o "
            + "TextFillColorSecondaryBrush, que están medidos:\n  " + string.Join("\n  ", offenders));
    }

    private static int LineOf(string text, int index) => text.AsSpan(0, index).Count('\n') + 1;

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

    /// <summary>
    /// El título de una tarjeta se pinta con un pincel <b>medible</b>, no con el color de acento (`T13-15`,
    /// decisión del mantenedor del 2026-09-21).
    /// </summary>
    /// <remarks>
    /// <para>Dos motivos, y el segundo pesa más. <b>Uno:</b> en la 1.8, `HyperlinkButtonForeground` <i>es</i>
    /// `AccentTextFillColorPrimaryBrush`, así que el título de *Opciones de formato* y el enlace
    /// «Reinicializar unidad ahora…» que tiene debajo salían exactamente del mismo color, y solo uno se
    /// pulsa. <b>Dos:</b> el acento lo elige cada usuario, así que no hay valor que medir de antemano — en
    /// el equipo del mantenedor es rojo, y cada título de una app que formatea discos se leía como un
    /// aviso.</para>
    /// <para>El acento no desaparece: se queda en el <b>icono</b> de cada sección, que es un objeto
    /// gráfico (3:1, no 4,5:1) y además decorativo (`AccessibilityView=Raw`). Por eso esta prueba mira el
    /// estilo del título y no el del icono.</para>
    /// </remarks>
    [Fact]
    public void SectionTitles_UseAMeasurableBrush_NotTheAccent()
    {
        string theme = WithoutXmlComments(
            File.ReadAllText(Path.Combine(RepoRoot(), "src", "FormatDiskPro", "UI", "Theme", "AppTheme.xaml")));

        var style = Regex.Match(theme,
            @"<Style\s+x:Key=""SectionTitleStyle""[^>]*>(.*?)</Style>", RegexOptions.Singleline);
        Assert.True(style.Success, "No se encontró SectionTitleStyle en AppTheme.xaml.");

        var foreground = Regex.Match(style.Groups[1].Value,
            @"<Setter\s+Property=""Foreground""\s+Value=""\{(?:Theme|Static)Resource\s+(\w+)\s*\}""");
        Assert.True(foreground.Success, "SectionTitleStyle no fija Foreground.");

        string brush = foreground.Groups[1].Value;
        Assert.Null(FluentTextPalette.ExemptionReason(brush));   // medible: sin exención
        Assert.True(TryResolve(brush, dark: false, out _), $"{brush} no se puede resolver a un color.");
    }
}
