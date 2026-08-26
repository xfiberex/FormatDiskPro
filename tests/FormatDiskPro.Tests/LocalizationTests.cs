using FormatDiskPro;
using Xunit;

namespace FormatDiskPro.Tests;

/// <summary>
/// Verifica el comportamiento defensivo del proveedor de localización <see cref="L"/>.
/// </summary>
[Collection(LanguageCollection.Name)]
public sealed class LocalizationTests
{
    [Fact]
    public void T_UnknownKey_ReturnsKeyItself()
        => Assert.Equal("clave.inexistente", L.T("clave.inexistente"));

    [Fact]
    public void T_KnownKey_ReturnsLocalizedText()
        => Assert.False(string.IsNullOrWhiteSpace(L.T("btn.start")));

    [Fact]
    public void T_WithArguments_FormatsPlaceholders()
        => Assert.Contains("G", L.T("success.body", 'G', "NTFS"));

    /// <summary>
    /// La sobrecarga con argumentos promete lo mismo que <c>T(string)</c>: <b>nunca lanza</b>. Antes
    /// llamaba directamente a <c>string.Format</c>, así que un marcador mal escrito en cualquiera de las
    /// cinco traducciones —o una llamada con menos argumentos de los que la plantilla espera— tumbaba la
    /// pantalla que solo quería mostrar un texto. Un error de traducción debe verse como un texto raro,
    /// no como una app que se cae.
    /// </summary>
    [Fact]
    public void T_WithBadPlaceholder_ReturnsTemplateInsteadOfThrowing()
    {
        // "success.body" espera dos argumentos; se le pasa uno.
        string result = L.T("success.body", 'G');

        Assert.False(string.IsNullOrWhiteSpace(result));
        Assert.Equal(L.T("success.body"), result);   // la plantilla sin formatear, que delata el fallo
    }

    /// <summary>Una clave inexistente con argumentos tampoco lanza: devuelve la clave.</summary>
    [Fact]
    public void T_UnknownKeyWithArguments_ReturnsKey()
        => Assert.Equal("clave.inexistente", L.T("clave.inexistente", 1, 2, 3));

    [Fact]
    public void EveryEntry_HasFiveNonEmptyTranslations()
    {
        int langs = Enum.GetValues<AppLang>().Length;   // Es, En, Pt, Fr, It
        Assert.All(L.Map, kv =>
        {
            Assert.Equal(langs, kv.Value.Length);
            Assert.All(kv.Value, s => Assert.False(string.IsNullOrWhiteSpace(s), $"'{kv.Key}' tiene una traducción vacía"));
        });
    }

    /// <summary>
    /// Las descripciones de sistema de archivos vivían como dos diccionarios ES/EN dentro de
    /// <c>MainWindow</c>, fuera del alcance de <see cref="EveryEntry_HasFiveNonEmptyTranslations"/>:
    /// portugués, francés e italiano mostraban el texto en inglés y la suite seguía en verde. Ahora
    /// están en <see cref="L.Map"/>, y esto comprueba que siguen ahí.
    /// </summary>
    [Theory]
    [InlineData("fs.desc.ntfs")]
    [InlineData("fs.desc.exfat")]
    [InlineData("fs.desc.refs")]
    [InlineData("fs.desc.fat32")]
    [InlineData("fs.desc.fat")]
    public void FileSystemDescriptions_AreLocalized(string key)
    {
        Assert.True(L.Map.ContainsKey(key), $"Falta la clave '{key}' en el diccionario de traducciones.");

        // No basta con que existan cinco entradas: el fallo original era precisamente que PT/FR/IT
        // repetían el texto inglés. Estas cinco descripciones difieren en los cinco idiomas.
        string[] translations = L.Map[key];
        string english = translations[(int)AppLang.En];
        foreach (AppLang lang in (AppLang[])[AppLang.Pt, AppLang.Fr, AppLang.It])
            Assert.False(translations[(int)lang] == english,
                $"'{key}' en {lang} es idéntico al inglés: probablemente quedó sin traducir.");
    }

    [Theory]
    [InlineData("es", AppLang.Es)]
    [InlineData("en", AppLang.En)]
    [InlineData("pt", AppLang.Pt)]
    [InlineData("fr", AppLang.Fr)]
    [InlineData("it", AppLang.It)]
    [InlineData("EN", AppLang.En)]      // sin distinción de mayúsculas
    [InlineData("xx", AppLang.Es)]      // desconocido → Es
    [InlineData(null, AppLang.Es)]
    public void FromCode_MapsLanguage(string? code, AppLang expected)
        => Assert.Equal(expected, L.FromCode(code));

    [Fact]
    public void ToCode_RoundTripsWithFromCode()
        => Assert.All(Enum.GetValues<AppLang>(), lang => Assert.Equal(lang, L.FromCode(L.ToCode(lang))));

    [Theory]
    [InlineData("es-ES", AppLang.Es)]
    [InlineData("en-US", AppLang.En)]
    [InlineData("pt-BR", AppLang.Pt)]
    [InlineData("fr-FR", AppLang.Fr)]
    [InlineData("it-IT", AppLang.It)]
    [InlineData("fr", AppLang.Fr)]        // solo idioma, sin región
    [InlineData("DE-de", AppLang.Es)]     // idioma no soportado → Es
    [InlineData("", AppLang.Es)]
    [InlineData(null, AppLang.Es)]
    public void FromCulture_MapsLanguagePart(string? culture, AppLang expected)
        => Assert.Equal(expected, L.FromCulture(culture));

    /// <summary>
    /// Las dos operaciones irreversibles comparten <c>ConfirmDialog</c>, y hasta `T6-01` compartían
    /// también el título: reinicializar —que borra el disco físico entero— se anunciaba como «Confirmar
    /// formato». Esto fija que sean títulos distintos en los CINCO idiomas, para que una traducción
    /// perezosa no rehaga el fallo en un idioma mientras el español sigue bien.
    /// </summary>
    [Fact]
    public void ConfirmTitles_NameTheirOwnOperation_InEveryLanguage()
    {
        var prev = L.Current;
        try
        {
            foreach (AppLang lang in Enum.GetValues<AppLang>())
            {
                L.Set(lang);
                string format = L.T("confirm.title");
                string reinit = L.T("confirm.titleReinit");

                Assert.False(string.IsNullOrWhiteSpace(format), $"confirm.title vacío en {lang}");
                Assert.False(string.IsNullOrWhiteSpace(reinit), $"confirm.titleReinit vacío en {lang}");
                Assert.NotEqual(format, reinit);
            }
        }
        finally { L.Set(prev); }
    }

    /// <summary>
    /// `T6-06`: los tres campos de la tarjeta de formato van uno debajo de otro, y «Etiqueta del volumen:»
    /// era el único que terminaba en dos puntos. Puestos en fila, se nota. El francés se comprueba igual:
    /// allí la forma sería « :», con espacio fino delante, y tampoco debe aparecer.
    /// </summary>
    [Theory]
    [InlineData("fs.label")]
    [InlineData("alloc.label")]
    [InlineData("label.label")]
    public void FieldHeaders_DoNotEndInAColon_InAnyLanguage(string key)
    {
        var prev = L.Current;
        try
        {
            foreach (AppLang lang in Enum.GetValues<AppLang>())
            {
                L.Set(lang);
                string header = L.T(key).TrimEnd();
                Assert.False(header.EndsWith(':'), $"{key} en {lang} termina en dos puntos: '{header}'");
            }
        }
        finally { L.Set(prev); }
    }

    /// <summary>
    /// `T6-09`: la RAE retiró la tilde de «solo» en 2010. Es el tipo de detalle que vuelve solo al
    /// escribir una cadena nueva, así que se barre el diccionario entero en vez de arreglar la que había.
    /// </summary>
    [Fact]
    public void SpanishStrings_DoNotUseTheObsoleteAccentOnSolo()
    {
        var offenders = L.Map
            .Where(kv => kv.Value.Length > 0 && kv.Value[0].Contains("sólo", StringComparison.OrdinalIgnoreCase))
            .Select(kv => kv.Key)
            .ToList();

        Assert.True(offenders.Count == 0,
            "«sólo» con tilde (obsoleto desde 2010) en: " + string.Join(", ", offenders));
    }

    /// <summary>
    /// `T6-15`: los resúmenes de reinicialización traían saltos de línea puestos a mano para maquetar,
    /// y el <c>TextBlock</c> ajusta por su cuenta encima: la frase salía partida donde no tocaba, y en un
    /// sitio distinto en cada idioma porque no mide lo mismo. Este es el texto que hay que leer antes de
    /// borrar un disco entero, así que del ajuste se encarga el control y en la cadena solo quedan los
    /// saltos que separan párrafos o abren un elemento de la lista numerada.
    /// </summary>
    [Theory]
    [InlineData("reinit.summary")]
    [InlineData("reinit.summaryFat32Small")]
    [InlineData("reinit.summaryTwoPartitions")]
    public void ReinitSummaries_OnlyBreakBetweenParagraphs_InEveryLanguage(string key)
    {
        Assert.True(L.Map.ContainsKey(key), $"Falta la clave '{key}'.");

        foreach ((string text, int i) in L.Map[key].Select((t, i) => (t, i)))
        {
            var lang = (AppLang)i;
            for (int at = text.IndexOf('\n'); at >= 0; at = text.IndexOf('\n', at + 1))
            {
                string rest = text[(at + 1)..];

                // Permitido: separar párrafos (línea en blanco) o abrir un elemento de lista ("  1) ").
                bool paragraph = rest.StartsWith('\n') || (at > 0 && text[at - 1] == '\n');
                bool listItem  = System.Text.RegularExpressions.Regex.IsMatch(rest, @"^\s*\d+\)");

                Assert.True(paragraph || listItem,
                    $"'{key}' en {lang} corta a mitad de frase en el carácter {at}: " +
                    $"…{text[Math.Max(0, at - 25)..at]}⏎{rest[..Math.Min(25, rest.Length)]}…");
            }
        }
    }

    /// <summary>
    /// `T7-01` y `T7-05`: las dos cadenas nuevas llevan dentro el dato que las hace útiles —el nombre
    /// del preset que se va a borrar, y cuántas entradas se ven de cuántas hay—. Una traducción que se
    /// deje un marcador fuera no rompe nada (<c>string.Format</c> ignora los sobrantes): simplemente
    /// pregunta «¿Eliminar?» sin decir cuál, que es el fallo que la confirmación venía a evitar.
    /// </summary>
    [Theory]
    [InlineData("preset.deleteConfirm", 1)]
    [InlineData("history.count", 2)]
    [InlineData("link.failed", 1)]
    public void ParameterizedStrings_KeepEveryPlaceholder_InEveryLanguage(string key, int placeholders)
    {
        Assert.True(L.Map.ContainsKey(key), $"Falta la clave '{key}'.");

        foreach ((string text, int i) in L.Map[key].Select((t, i) => (t, i)))
            for (int n = 0; n < placeholders; n++)
                Assert.True(text.Contains($"{{{n}}}", StringComparison.Ordinal),
                    $"'{key}' en {(AppLang)i} no usa el marcador {{{n}}}: '{text}'");
    }

    /// <summary>
    /// `T7-03`: la pista del tamaño de asignación no puede nombrar una opción que no existe. El combo se
    /// puebla con tamaños concretos («4 KB», «64 KB») y el recomendado llega <b>preseleccionado</b>: no
    /// hay ningún elemento llamado «Predeterminado» al que mandar al usuario.
    /// </summary>
    [Fact]
    public void AllocationHint_DoesNotNameANonexistentOption()
    {
        Assert.True(L.Map.ContainsKey("alloc.hint"), "Falta la clave 'alloc.hint'.");

        string[] ghosts = ["Predeterminado", "Default", "Padrão", "Par défaut", "Predefinito"];
        foreach ((string text, int i) in L.Map["alloc.hint"].Select((t, i) => (t, i)))
            Assert.All(ghosts, ghost => Assert.False(text.Contains(ghost, StringComparison.OrdinalIgnoreCase),
                $"alloc.hint en {(AppLang)i} nombra «{ghost}», que no es una opción del selector."));
    }

    /// <summary>
    /// `T7-08`: la etiqueta que se pega al texto de un ítem apagado tiene que <b>caber en el menú</b>.
    /// El motivo largo ya existe —va en el <c>HelpText</c>—; esto es su resumen visible, y una traducción
    /// que copie la frase completa duplicaría el ancho del menú <i>Herramientas</i> en lugar de decir en
    /// dos palabras por qué el ítem está gris. Los paréntesis son parte del contrato: la etiqueta se
    /// concatena al nombre del ítem, no lo sustituye.
    /// </summary>
    [Theory]
    [InlineData("menu.tagNoDrive")]
    [InlineData("menu.tagProtected")]
    [InlineData("menu.tagRemovable")]
    public void DisabledMenuTags_AreShortAndParenthesized_InEveryLanguage(string key)
    {
        Assert.True(L.Map.ContainsKey(key), $"Falta la clave '{key}'.");

        foreach ((string text, int i) in L.Map[key].Select((t, i) => (t, i)))
        {
            var lang = (AppLang)i;
            Assert.True(text.StartsWith('(') && text.EndsWith(')'),
                $"'{key}' en {lang} no va entre paréntesis: se concatena al nombre del ítem — '{text}'");
            Assert.True(text.Length <= 30,
                $"'{key}' en {lang} mide {text.Length} caracteres: es una etiqueta de menú, no la frase " +
                $"completa (esa va en '{key.Replace("tag", "why")}') — '{text}'");
        }
    }

    /// <summary>
    /// La etiqueta corta y la frase larga son <b>dos textos distintos</b>, no el mismo repetido: si
    /// alguien pega la frase en la etiqueta, el test de longitud lo caza; si abrevia la frase hasta la
    /// etiqueta, el lector de pantalla pierde el motivo. Esta prueba cubre el segundo caso.
    /// </summary>
    [Theory]
    [InlineData("menu.tagNoDrive",   "menu.whyNoDrive")]
    [InlineData("menu.tagProtected", "menu.whyProtected")]
    [InlineData("menu.tagRemovable", "menu.whyRemovable")]
    public void DisabledMenuReasons_AreFullSentences_NotTheShortTag(string tagKey, string whyKey)
    {
        foreach ((string why, int i) in L.Map[whyKey].Select((t, i) => (t, i)))
        {
            var lang = (AppLang)i;
            Assert.True(why.Length > L.Map[tagKey][i].Length,
                $"'{whyKey}' en {lang} no dice más que su etiqueta corta '{L.Map[tagKey][i]}'.");
            Assert.EndsWith(".", why);
        }
    }

    [Fact]
    public void T_ReturnsActiveLanguageString()
    {
        var prev = L.Current;
        try
        {
            L.Set(AppLang.Fr);
            Assert.Equal("Démarrer", L.T("btn.start"));
            L.Set(AppLang.It);
            Assert.Equal("Avvia", L.T("btn.start"));
            L.Set(AppLang.Pt);
            Assert.Equal("Iniciar", L.T("btn.start"));
        }
        finally { L.Set(prev); }
    }
}
