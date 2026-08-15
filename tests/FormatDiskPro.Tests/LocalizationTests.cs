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
