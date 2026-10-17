// SPDX-License-Identifier: GPL-3.0-only
using System.Globalization;
using System.Text.RegularExpressions;
using Xunit;

namespace FormatDiskPro.Tests;

/// <summary>
/// Textos que afirmaban algo que la interfaz o el código ya no hacen (`AUD-044` a `AUD-047`). Ninguno
/// rompía nada, y por eso duraron: cada uno se escribió a mano con un dato que vivía en otro sitio. Las
/// pruebas comparan el texto con ese otro sitio, en los cinco idiomas.
/// </summary>
[Collection(LanguageCollection.Name)]
public sealed class InterfaceTextTests
{
    public static TheoryData<AppLang> EveryLanguage()
    {
        var data = new TheoryData<AppLang>();
        foreach (AppLang lang in Enum.GetValues<AppLang>()) data.Add(lang);
        return data;
    }

    /// <summary>Ejecuta <paramref name="read"/> con el idioma <paramref name="lang"/> puesto, y lo devuelve como estaba.</summary>
    private static T In<T>(AppLang lang, Func<T> read)
    {
        var prev = L.Current;
        try { L.Set(lang); return read(); }
        finally { L.Set(prev); }
    }

    // ── AUD-044 · el botón que ya no existe ─────────────────────────────────────

    /// <summary>
    /// El botón primario se llama «Formatear» desde `T12-02`. Las dos pistas de la FAT32 pequeña seguían
    /// mandando no usar «el botón Iniciar», que el usuario no puede encontrar.
    /// </summary>
    [Theory]
    [MemberData(nameof(EveryLanguage))]
    public void TheSmallFat32Hints_NameTheButtonAsItIsShown(AppLang lang)
    {
        var (button, hint, hintSmall) = In(lang, () =>
            (L.T("btn.start"), L.T("opt.smallFat32Hint", "32 GB", "1 GB"), L.T("opt.smallFat32HintSmall", "1 GB")));

        Assert.Contains(button, hint);
        Assert.Contains(button, hintSmall);
    }

    /// <summary>Ninguna cadena de ningún idioma menciona ya el botón con su nombre antiguo.</summary>
    [Fact]
    public void NoText_MentionsTheOldStartButton()
    {
        string[] oldNames = ["botón Iniciar", "Start button", "botão Iniciar", "bouton Démarrer", "pulsante Avvia"];

        var offenders = L.Map
            .SelectMany(e => e.Value.Select(text => (e.Key, text)))
            .Where(x => oldNames.Any(old => x.text.Contains(old, StringComparison.Ordinal)))
            .Select(x => x.Key)
            .Distinct()
            .ToList();

        Assert.Empty(offenders);
    }

    // ── AUD-045 · el menú del aviso de privacidad ───────────────────────────────

    /// <summary>
    /// El aviso dice dónde desactivar la conexión automática. El nombre del menú se escribía a mano, y en
    /// cuatro de cinco idiomas no era el que se ve («Configuration →» donde el menú es «Settings»). Ahora el
    /// texto recibe la propia clave <c>menu.config</c>, como hace <c>AboutDialog</c>.
    /// </summary>
    [Theory]
    [MemberData(nameof(EveryLanguage))]
    public void ThePrivacyNotice_NamesTheSettingsMenuAsItIsShown(AppLang lang)
    {
        var (menu, notice) = In(lang, () => (L.T("menu.config"), L.T("about.privacy", L.T("menu.config"))));

        Assert.Contains(menu + " →", notice);
        Assert.DoesNotContain("{0}", notice);
    }

    // ── AUD-046 · las cifras de la etiqueta ─────────────────────────────────────

    /// <summary>
    /// El rechazo por etiqueta no válida dice cuántos caracteres admite cada sistema de archivos. Decía 32
    /// para exFAT mientras <see cref="FileSystems.MaxLabelLength"/> valida 11. Para cada uno de los cinco,
    /// la primera cifra que sigue a su nombre en el texto tiene que ser su límite real.
    /// </summary>
    [Theory]
    [MemberData(nameof(EveryLanguage))]
    public void TheInvalidLabelText_GivesEachFileSystemItsRealLimit(AppLang lang)
    {
        var plan = PartitionPlan.WholeDisk(DiskPartitionStyle.Mbr, "FAT32", "DATOS");
        var (key, args) = PlanRejection.Describe(new PlanValidation(PlanProblem.InvalidLabel, 0), plan, 0);
        string text = In(lang, () => L.T(key, args));

        foreach (string fs in (string[])["FAT32", "FAT", "exFAT", "NTFS", "ReFS"])
        {
            var match = Regex.Match(text, $@"\b{fs}\b\D*(\d+)");
            Assert.True(match.Success, $"El texto ({lang}) no nombra {fs} seguido de una cifra: «{text}»");
            Assert.Equal(FileSystems.MaxLabelLength(fs), int.Parse(match.Groups[1].Value, CultureInfo.InvariantCulture));
        }
    }

    // ── AUD-047 · el separador de millares de las IOPS ──────────────────────────

    /// <summary>
    /// Las IOPS se formateaban con la cultura de Windows; el resto de cifras, con la del idioma de la app.
    /// 12.345 IOPS son 12.345 × 4096 bytes por segundo de 4 KiB aleatorio.
    /// </summary>
    [Theory]
    [InlineData(AppLang.Es, "12.345 IOPS")]
    [InlineData(AppLang.En, "12,345 IOPS")]
    [InlineData(AppLang.Pt, "12.345 IOPS")]
    [InlineData(AppLang.It, "12.345 IOPS")]
    public void Iops_UseTheThousandsSeparatorOfTheAppLanguage(AppLang lang, string expected)
        => Assert.Equal(expected, In(lang, () => Benchmark.FormatIops(12_345d * Benchmark.Random4KBlockBytes)));

    /// <summary>
    /// En francés el separador es un espacio, normal o estrecho según la versión de ICU, así que no se fija
    /// el carácter: se exige el de la cultura de la app y que no sea ni punto ni coma.
    /// </summary>
    [Fact]
    public void Iops_InFrench_UseASpaceAsThousandsSeparator()
    {
        var (text, separator) = In(AppLang.Fr, () =>
            (Benchmark.FormatIops(12_345d * Benchmark.Random4KBlockBytes), L.Culture.NumberFormat.NumberGroupSeparator));

        Assert.Equal($"12{separator}345 IOPS", text);
        Assert.True(string.IsNullOrWhiteSpace(separator), $"Se esperaba un espacio y el separador es «{separator}».");
    }
}
