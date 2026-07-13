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
    public void EverySeverityColor_MeetsWcagAaContrast_AgainstItsCardBackground()
    {
        var offenders = new List<string>();

        foreach (SmartLevel level in Enum.GetValues<SmartLevel>())
        {
            foreach (bool dark in (bool[])[false, true])
            {
                double ratio = SeverityPalette.ContrastRatio(
                    SeverityPalette.For(level, dark), SeverityPalette.Background(dark));

                if (ratio < WcagAaNormalText)
                    offenders.Add($"{level} en tema {(dark ? "oscuro" : "claro")}: {ratio:F2}:1");
            }
        }

        Assert.True(offenders.Count == 0,
            $"WCAG AA exige {WcagAaNormalText}:1 para texto normal. No llegan: {string.Join(" | ", offenders)}");
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
