using Windows.UI;

namespace FormatDiskPro;

/// <summary>Exigencia de contraste WCAG que aplica a un color, según cómo se use.</summary>
public enum ContrastRequirement
{
    /// <summary>Texto normal: 4.5:1 (WCAG 2.x AA, criterio 1.4.3).</summary>
    NormalText,

    /// <summary>Objeto gráfico o de interfaz (barra, icono): 3:1 (criterio 1.4.11).</summary>
    Graphical,
}

/// <summary>
/// Un color semántico del inventario, con el tema al que pertenece y el umbral que debe cumplir.
/// </summary>
/// <param name="Name">Nombre legible, para que un fallo del test diga cuál es.</param>
/// <param name="Color">Color (puede llevar alfa: se compone sobre el fondo antes de medir).</param>
/// <param name="Dark">Tema al que pertenece; determina el fondo de referencia.</param>
/// <param name="Requirement">Umbral WCAG aplicable según el uso.</param>
public sealed record PaletteColor(string Name, Color Color, bool Dark, ContrastRequirement Requirement)
{
    /// <summary>Razón de contraste mínima exigible a este color.</summary>
    public double MinimumRatio => Requirement == ContrastRequirement.NormalText ? 4.5 : 3.0;
}

/// <summary>
/// Inventario ÚNICO de los colores semánticos de la aplicación (los que comunican un significado:
/// salud, resultado, ocupación, protección), uno por tema.
///
/// Son los únicos colores que no salen de un <c>ThemeResource</c> de Windows: se eligen aquí a mano, y a
/// mano se pueden elegir mal. Por eso viven en <c>Core</c>, sin dependencia de la capa de UI: para poder
/// medirlos. <c>SeverityPaletteTests</c> recorre <see cref="All"/> y exige a cada uno su umbral WCAG, de
/// modo que un color mal elegido rompe el build en vez de quedarse ilegible en producción.
/// </summary>
/// <remarks>
/// <para><b>Por qué el inventario es enumerable y no una lista de comprobaciones sueltas.</b> Hasta la
/// v1.15.2 el test recorría solo <see cref="For"/>, mientras los mismos RGB estaban duplicados a mano en
/// <c>HistoryDialog.ColorFor</c> y <c>MainWindow.ProtectedColor</c>. Las copias quedaban fuera de la
/// medición y por ahí entró un gris de 3.52:1 sin romper nada. Con <see cref="All"/>, añadir un color
/// semántico al inventario es lo mismo que ponerlo bajo test: no hay forma de hacer una cosa sin la otra.
/// Si añades un color aquí, aparece en el barrido automáticamente.</para>
///
/// <para><b>Qué NO entra aquí:</b> los colores de los botones de caption de la barra de título
/// (<c>MainWindow.UpdateCaptionButtonColors</c>). No comunican significado —son cromo de ventana— y sus
/// estados hover/pressed son superposiciones translúcidas sobre el material del sistema (Mica/Acrylic),
/// no sobre la tarjeta: no hay un fondo fijo contra el que medirlos.</para>
/// </remarks>
public static class SeverityPalette
{
    // Fondo contra el que se miden: CardBackgroundFillColorDefault ya resuelto sobre el fondo de página
    // (es el `AppCardStyle` de Theme/AppTheme.xaml, donde se pintan tanto la tarjeta como el diálogo).
    public static readonly Color LightBackground = Color.FromArgb(255, 251, 251, 251);
    public static readonly Color DarkBackground  = Color.FromArgb(255, 43, 43, 43);

    /// <summary>Color de un nivel de severidad S.M.A.R.T. en el tema efectivo.</summary>
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

    /// <summary>
    /// Color del resultado de una operación del historial (<c>UI/HistoryDialog</c>), en el tema efectivo.
    /// Tiñe el TÍTULO de cada fila, no solo el glifo: es texto normal y le aplica el 4.5:1.
    /// </summary>
    /// <remarks>
    /// Correcto y fallido reutilizan la severidad (verde de <see cref="SmartLevel.Ok"/>, rojo de
    /// <see cref="SmartLevel.Critical"/>): son el mismo significado, así que deben ser el mismo color.
    /// </remarks>
    public static Color ForResult(HistoryResult result, bool dark) => result switch
    {
        HistoryResult.Ok                          => For(SmartLevel.Ok, dark),
        HistoryResult.Fail or HistoryResult.Error => For(SmartLevel.Critical, dark),
        // Gris atenuado: una cancelación no es un fallo, pero debe leerse. El claro era #868686 (3.52:1),
        // por debajo de AA; #6E6E6E da 4.93:1. No se tomó el primero que pasa (#747474, 4.52:1): sin margen.
        HistoryResult.Cancelled                   => dark ? Color.FromArgb(255, 0x9B, 0x9B, 0x9B)
                                                          : Color.FromArgb(255, 0x6E, 0x6E, 0x6E),
        _                                         => Text(dark),
    };

    /// <summary>
    /// Color de texto primario del tema (Fluent <c>TextFillColorPrimary</c>). Lo usan la lista de unidades
    /// y las entradas informativas del historial.
    /// </summary>
    /// <remarks>El claro lleva alfa (0xE4) a propósito, como el token de Fluent; el barrido lo compone
    /// sobre el fondo con <see cref="Flatten"/> antes de medirlo.</remarks>
    public static Color Text(bool dark) =>
        dark ? Color.FromArgb(255, 0xFF, 0xFF, 0xFF) : Color.FromArgb(0xE4, 0x00, 0x00, 0x00);

    /// <summary>
    /// Relleno neutro de la barra de ocupación cuando queda espacio de sobra (&lt; 80 %).
    /// </summary>
    /// <remarks>
    /// Existe para que la barra NO use el color de acento del sistema, que es lo que hace un
    /// <c>ProgressBar</c> por defecto: en un equipo con acento rojo se veía roja con el disco medio vacío
    /// y leía como alarma. Es un objeto gráfico, así que le basta el 3:1 (no el 4.5:1 del texto).
    /// </remarks>
    public static Color NeutralFill(bool dark) =>
        dark ? Color.FromArgb(255, 0xA0, 0xA0, 0xA0) : Color.FromArgb(255, 0x8A, 0x8A, 0x8A);

    /// <summary>Fondo de la tarjeta del tema efectivo: la referencia contra la que se mide el contraste.</summary>
    public static Color Background(bool dark) => dark ? DarkBackground : LightBackground;

    /// <summary>
    /// Inventario completo de colores semánticos, en ambos temas, con el umbral que le toca a cada uno.
    /// Es lo que recorre el barrido de contraste: añadir un color aquí es ponerlo bajo test.
    /// </summary>
    public static IReadOnlyList<PaletteColor> All()
    {
        var list = new List<PaletteColor>();
        foreach (bool dark in (bool[])[false, true])
        {
            foreach (SmartLevel level in Enum.GetValues<SmartLevel>())
                list.Add(new($"SmartLevel.{level}", For(level, dark), dark, ContrastRequirement.NormalText));

            foreach (HistoryResult result in Enum.GetValues<HistoryResult>())
                list.Add(new($"HistoryResult.{result}", ForResult(result, dark), dark, ContrastRequirement.NormalText));

            list.Add(new("Text", Text(dark), dark, ContrastRequirement.NormalText));
            list.Add(new("NeutralFill", NeutralFill(dark), dark, ContrastRequirement.Graphical));
        }
        return list;
    }

    /// <summary>Contraste real de una entrada del inventario contra el fondo de su tema.</summary>
    /// <param name="entry">Entrada del inventario (ver <see cref="All"/>).</param>
    public static double ContrastAgainstBackground(PaletteColor entry)
    {
        Color background = Background(entry.Dark);
        return ContrastRatio(Flatten(entry.Color, background), background);
    }

    /// <summary>
    /// Compone un color con alfa sobre un fondo opaco y devuelve el color resultante, opaco.
    /// La fórmula de contraste de WCAG solo está definida entre colores opacos: medir un color
    /// semitransparente sin componerlo primero da un número que no corresponde a lo que se ve.
    /// </summary>
    /// <param name="foreground">Color de primer plano (puede llevar alfa).</param>
    /// <param name="background">Fondo opaco sobre el que se pinta.</param>
    public static Color Flatten(Color foreground, Color background)
    {
        if (foreground.A == 255) return foreground;

        double a = foreground.A / 255.0;
        static byte Mix(byte f, byte b, double alpha) => (byte)Math.Round((f * alpha) + (b * (1 - alpha)));
        return Color.FromArgb(
            255,
            Mix(foreground.R, background.R, a),
            Mix(foreground.G, background.G, a),
            Mix(foreground.B, background.B, a));
    }

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
