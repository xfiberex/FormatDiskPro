using Windows.UI;

namespace FormatDiskPro;

/// <summary>
/// Colores de severidad S.M.A.R.T. (verde / ámbar / rojo), uno por tema.
///
/// Son los únicos colores de la app que no salen de un <c>ThemeResource</c> de Windows: se eligen aquí
/// a mano, y a mano se pueden elegir mal. Los usa la línea «Salud:» de la tarjeta principal
/// (<c>MainWindow.RenderHealth</c>) y cada métrica con umbral del diálogo S.M.A.R.T.
/// (<c>HealthDialog.LevelBrush</c>), que es quien los envuelve en un <c>Brush</c>: aquí solo vive el RGB,
/// sin dependencia de la capa de UI, para poder medirlos.
///
/// Y se miden: <c>SeverityPaletteTests</c> calcula el contraste real de cada color contra el fondo de la
/// tarjeta de su tema y exige el 4.5:1 de WCAG AA, así que un color mal elegido rompe el build en vez de
/// quedarse ilegible en producción. El color nunca va solo —cada valor lleva además su texto de estado
/// (Normal / Atención / Crítico), ver <c>HealthDialog.LevelLabel</c>—, pero eso no excusa que no se lea.
/// </summary>
public static class SeverityPalette
{
    // Fondo contra el que se miden: CardBackgroundFillColorDefault ya resuelto sobre el fondo de página
    // (es el `AppCardStyle` de Theme/AppTheme.xaml, donde se pintan tanto la tarjeta como el diálogo).
    public static readonly Color LightBackground = Color.FromArgb(255, 251, 251, 251);
    public static readonly Color DarkBackground  = Color.FromArgb(255, 43, 43, 43);

    /// <summary>Color de un nivel de severidad en el tema efectivo.</summary>
    public static Color For(SmartLevel level, bool dark) => (level, dark) switch
    {
        (SmartLevel.Ok,       false) => Color.FromArgb(255, 0x0F, 0x7B, 0x0F),
        (SmartLevel.Ok,       true)  => Color.FromArgb(255, 0x6C, 0xCB, 0x5F),
        (SmartLevel.Warning,  false) => Color.FromArgb(255, 0x9D, 0x5D, 0x00),
        (SmartLevel.Warning,  true)  => Color.FromArgb(255, 0xFC, 0xC8, 0x4A),
        (SmartLevel.Critical, false) => Color.FromArgb(255, 0xC4, 0x2B, 0x1C),
        (SmartLevel.Critical, true)  => Color.FromArgb(255, 0xFF, 0x99, 0xA4),
        // Unknown no se colorea en la UI (se deja el color de texto del tema); el par blanco/negro está
        // aquí para que el barrido de contraste cubra el enum entero sin casos especiales.
        (_,                   false) => Color.FromArgb(255, 0x00, 0x00, 0x00),
        (_,                   true)  => Color.FromArgb(255, 0xFF, 0xFF, 0xFF),
    };

    /// <summary>Fondo de la tarjeta del tema efectivo: la referencia contra la que se mide el contraste.</summary>
    public static Color Background(bool dark) => dark ? DarkBackground : LightBackground;

    /// <summary>Razón de contraste WCAG 2.x entre dos colores opacos (1:1 = idénticos, 21:1 = negro sobre blanco).</summary>
    public static double ContrastRatio(Color a, Color b)
    {
        double la = RelativeLuminance(a);
        double lb = RelativeLuminance(b);
        (double lighter, double darker) = la >= lb ? (la, lb) : (lb, la);
        return (lighter + 0.05) / (darker + 0.05);
    }

    private static double RelativeLuminance(Color c) =>
        (0.2126 * Linearize(c.R)) + (0.7152 * Linearize(c.G)) + (0.0722 * Linearize(c.B));

    private static double Linearize(byte channel)
    {
        double s = channel / 255.0;
        return s <= 0.03928 ? s / 12.92 : Math.Pow((s + 0.055) / 1.055, 2.4);
    }
}
