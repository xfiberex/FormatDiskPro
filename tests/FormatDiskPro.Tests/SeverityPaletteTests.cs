using Windows.UI;
using Xunit;

namespace FormatDiskPro.Tests;

/// <summary>
/// Los colores verde/ámbar/rojo de la salud S.M.A.R.T. se eligieron a ojo para los dos temas, y a ojo se
/// puede elegir mal: un ámbar pensado para fondo claro se vuelve ilegible sobre la tarjeta oscura, y
/// justo el valor crítico —lo que el usuario necesita ver— sería lo peor de leer.
///
/// Estos tests miden el contraste real de cada color contra el fondo de su tema y exigen el 4.5:1 de
/// WCAG AA para texto normal, así que retocar un color y pasarse de claro (u oscuro) rompe el build en
/// vez de degradar la app en silencio.
/// </summary>
public sealed class SeverityPaletteTests
{
    private const double WcagAaNormalText = 4.5;

    // Se recorre por dentro en vez de con [Theory] para dar un único fallo con TODOS los colores que no
    // llegan al mínimo, en lugar de tener que arreglarlos de uno en uno.
    [Fact]
    public void EverySemanticColor_MeetsItsWcagContrast_AgainstItsReference()
    {
        var offenders = new List<string>();

        foreach (PaletteColor entry in SeverityPalette.All())
        {
            double ratio = SeverityPalette.ContrastAgainstReference(entry);
            if (ratio < entry.MinimumRatio)
                offenders.Add($"{entry.Name} en tema {(entry.Dark ? "oscuro" : "claro")}: " +
                              $"{ratio:F2}:1 (mínimo {entry.MinimumRatio:F1}:1)");
        }

        Assert.True(offenders.Count == 0,
            $"Colores por debajo de su umbral WCAG: {string.Join(" | ", offenders)}");
    }

    /// <summary>
    /// El barrido solo protege lo que recorre. Hasta la v1.15.2 medía únicamente <c>For(SmartLevel)</c>
    /// mientras los mismos RGB estaban copiados en <c>HistoryDialog</c> y <c>MainWindow</c>, y por ahí
    /// entró un gris de 3.52:1. Esto fija que el inventario siga cubriendo las cuatro familias.
    /// </summary>
    [Theory]
    [InlineData("SmartLevel.")]
    [InlineData("HistoryResult.")]
    [InlineData("Text")]
    [InlineData("NeutralFill")]
    [InlineData("TrackFill")]
    public void All_CoversEverySemanticColorFamily_InBothThemes(string namePrefix)
    {
        var matching = SeverityPalette.All().Where(e => e.Name.StartsWith(namePrefix, StringComparison.Ordinal)).ToList();

        Assert.NotEmpty(matching);
        Assert.Contains(matching, e => !e.Dark);
        Assert.Contains(matching, e =>  e.Dark);
    }

    /// <summary>
    /// La pista de la barra de ocupación es el ÚNICO color que no se mide contra el fondo, y tiene que
    /// seguir así: contra la tarjeta no llega al 3:1 a propósito (es un hueco, no una segunda barra).
    /// Si alguien le quita el <c>Against</c> «para que se mida como los demás», el barrido lo daría por
    /// suspendido y la reacción natural sería oscurecerla hasta que compita con el relleno. Esto fija que
    /// cada entrada de la pista declare contra QUÉ relleno se compara.
    /// </summary>
    [Fact]
    public void TrackFill_IsMeasuredAgainstEveryFillItCanShareTheBarWith()
    {
        foreach (bool dark in (bool[])[false, true])
        {
            var entries = SeverityPalette.All()
                .Where(e => e.Dark == dark && e.Name.StartsWith("TrackFill", StringComparison.Ordinal))
                .ToList();

            Assert.All(entries, e => Assert.NotNull(e.Against));

            // Los tres rellenos que CapacityBrush puede pintar: neutro, ámbar (≥80 %) y rojo (≥90 %).
            Assert.Equal(3, entries.Count);
            Assert.Contains(entries, e => e.Against == SeverityPalette.NeutralFill(dark));
            Assert.Contains(entries, e => e.Against == SeverityPalette.For(SmartLevel.Warning, dark));
            Assert.Contains(entries, e => e.Against == SeverityPalette.For(SmartLevel.Critical, dark));
        }
    }

    /// <summary>
    /// Cuando una entrada declara un vecino, el alfa se compone sobre ESE vecino, no sobre la tarjeta: un
    /// texto semitransparente encima de la pista de la barra deja ver la pista, no el fondo de la tarjeta.
    /// Componerlo sobre el fondo equivocado da un número que no corresponde a lo que se ve — exactamente el
    /// fallo que este barrido existe para cazar.
    /// </summary>
    [Fact]
    public void ContrastAgainstReference_CompositesAlphaOverTheAdjacentColor_NotTheCard()
    {
        Color adjacent    = Color.FromArgb(255, 0x40, 0x40, 0x40);   // vecino opaco, más oscuro que la tarjeta clara
        Color translucent = Color.FromArgb(0x80, 0xFF, 0xFF, 0xFF);  // blanco al 50 %

        var entry = new PaletteColor("prueba", translucent, Dark: false, ContrastRequirement.NormalText, adjacent);

        double expected = SeverityPalette.ContrastRatio(SeverityPalette.Flatten(translucent, adjacent), adjacent);
        Assert.Equal(expected, SeverityPalette.ContrastAgainstReference(entry), precision: 6);

        // Y no es lo mismo que componerlo sobre la tarjeta: si lo fuera, el test anterior no probaría nada.
        double overCard = SeverityPalette.ContrastRatio(
            SeverityPalette.Flatten(translucent, SeverityPalette.LightBackground), adjacent);
        Assert.NotEqual(overCard, expected, precision: 2);
    }

    /// <summary>Usado y libre no pueden acabar siendo el mismo color en ningún tema.</summary>
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void UsedAndFreeFills_AreDifferentColors(bool dark)
        => Assert.NotEqual(SeverityPalette.NeutralFill(dark), SeverityPalette.TrackFill(dark));

    /// <summary>
    /// El resultado del historial y la severidad S.M.A.R.T. comparten significado: «correcto» y «fallo»
    /// deben pintarse igual en los dos sitios, o el usuario aprendería dos códigos de color distintos.
    /// </summary>
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void ForResult_ReusesSeverityColors_ForOkAndFailure(bool dark)
    {
        Assert.Equal(SeverityPalette.For(SmartLevel.Ok, dark),       SeverityPalette.ForResult(HistoryResult.Ok, dark));
        Assert.Equal(SeverityPalette.For(SmartLevel.Critical, dark), SeverityPalette.ForResult(HistoryResult.Fail, dark));
        Assert.Equal(SeverityPalette.For(SmartLevel.Critical, dark), SeverityPalette.ForResult(HistoryResult.Error, dark));
    }

    /// <summary>
    /// La fórmula de contraste de WCAG solo está definida entre colores opacos. El color de texto claro
    /// lleva alfa (Fluent <c>TextFillColorPrimary</c> = <c>#E4000000</c>), así que medirlo sin componerlo
    /// sobre el fondo daría un número que no corresponde a lo que se ve.
    /// </summary>
    [Fact]
    public void Flatten_CompositesAlphaOverBackground()
    {
        Color opaque = Color.FromArgb(255, 0x12, 0x34, 0x56);
        Assert.Equal(opaque, SeverityPalette.Flatten(opaque, SeverityPalette.LightBackground));

        // Totalmente transparente sobre el fondo: se ve el fondo.
        Assert.Equal(SeverityPalette.LightBackground,
            SeverityPalette.Flatten(Color.FromArgb(0, 0, 0, 0), SeverityPalette.LightBackground));

        // Negro al 20 % sobre blanco puro deja el 80 % del fondo: 255 * 0.8 = 204, exacto.
        // (Se usa 51/255 = 0.2 justo, y no 128/255 = 0.50196, que daría 127 y parecería un error de ±1.)
        Color white   = Color.FromArgb(255, 255, 255, 255);
        Color faded   = SeverityPalette.Flatten(Color.FromArgb(51, 0, 0, 0), white);
        Assert.Equal(255, faded.A);   // el resultado siempre es opaco: es lo que se puede medir
        Assert.Equal(204, faded.R);
    }

    /// <summary>
    /// El sentido de tener una paleta por tema: un nivel con significado NO puede pintarse igual en claro
    /// que en oscuro. Si alguien "simplifica" volviendo a un color único por nivel, esto lo caza.
    /// </summary>
    [Theory]
    [InlineData(SmartLevel.Ok)]
    [InlineData(SmartLevel.Warning)]
    [InlineData(SmartLevel.Critical)]
    public void SignificantLevels_UseADifferentColorPerTheme(SmartLevel level)
        => Assert.NotEqual(SeverityPalette.For(level, dark: false), SeverityPalette.For(level, dark: true));

    /// <summary>Un disco sano y uno crítico no pueden confundirse dentro del mismo tema.</summary>
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void OkAndCritical_AreDistinguishable(bool dark)
        => Assert.NotEqual(SeverityPalette.For(SmartLevel.Ok, dark), SeverityPalette.For(SmartLevel.Critical, dark));

    [Fact]
    public void ContrastRatio_MatchesTheWcagReferenceValues()
    {
        Color black = Color.FromArgb(255, 0, 0, 0);
        Color white = Color.FromArgb(255, 255, 255, 255);

        // Los dos extremos que fija la propia norma: 21:1 negro sobre blanco, 1:1 un color consigo mismo.
        Assert.Equal(21.0, SeverityPalette.ContrastRatio(black, white), precision: 2);
        Assert.Equal(1.0, SeverityPalette.ContrastRatio(white, white), precision: 2);
    }
}
