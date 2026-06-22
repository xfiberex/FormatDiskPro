using FormatDiskPro;
using Xunit;

namespace FormatDiskPro.Tests;

/// <summary>
/// Verifica el comportamiento defensivo del proveedor de localización <see cref="L"/>.
/// </summary>
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
