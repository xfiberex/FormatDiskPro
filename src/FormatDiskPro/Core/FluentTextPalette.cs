using Windows.UI;

namespace FormatDiskPro;

/// <summary>
/// Los colores de TEXTO de Fluent que la aplicación consume por <c>ThemeResource</c>, con su valor real
/// en cada tema — para poder <b>medirlos</b>.
/// </summary>
/// <remarks>
/// <para><b>Por qué existe</b> (`T12-01`). <see cref="SeverityPalette"/> mide los colores que elegimos a
/// mano, y su propia documentación decía que eran «los únicos que no salen de un <c>ThemeResource</c> de
/// Windows». Eso dejaba fuera de la medición a los que sí salen de uno — y por ahí entró exactamente el
/// mismo fallo que aquel inventario existe para evitar: <c>TextFillColorTertiaryBrush</c> da
/// <b>3,29:1</b> en tema claro, por debajo del 4,5:1 que WCAG AA exige al texto normal, y estaba pintando
/// dieciocho controles de la ventana principal — entre ellos las descripciones que explican qué sistema
/// de archivos y qué tamaño de clúster elegir.</para>
///
/// <para><b>Que un color venga de Windows no lo hace correcto para cualquier uso.</b> Fluent define el
/// terciario para texto <i>de apoyo</i> sobre superficies grandes, no para contenido; la decisión de
/// usarlo en una etiqueta es nuestra, y por tanto también lo es su contraste. Esta clase no elige
/// colores: solo declara los que la app usa, con el valor exacto de cada token, para que el barrido de
/// <c>TextContrastTests</c> pueda recorrer el XAML y medir lo que hay puesto de verdad.</para>
///
/// <para><b>Los valores son los de WinUI 3</b> (<c>Common_themeresources.xaml</c>). Si un día cambian con
/// una versión del Windows App SDK, lo que falla es el test, que es exactamente lo que tiene que
/// pasar.</para>
/// </remarks>
public static class FluentTextPalette
{
    /// <summary>
    /// Color real de un pincel de texto de Fluent en el tema indicado.
    /// </summary>
    /// <param name="brushName">
    /// Nombre del recurso tal como aparece en el XAML, p. ej. <c>"TextFillColorSecondaryBrush"</c>.
    /// </param>
    /// <param name="dark">Tema oscuro.</param>
    /// <param name="color">Color del token, con su alfa. Sin componer sobre el fondo.</param>
    /// <returns>
    /// <c>false</c> si el token no está declarado aquí — para el barrido eso es un fallo, no un permiso:
    /// significa que la app usa un color de texto que nadie ha medido.
    /// </returns>
    public static bool TryGet(string brushName, bool dark, out Color color)
    {
        color = brushName switch
        {
            "TextFillColorPrimaryBrush"   => dark ? Color.FromArgb(0xFF, 0xFF, 0xFF, 0xFF)
                                                  : Color.FromArgb(0xE4, 0x00, 0x00, 0x00),
            "TextFillColorSecondaryBrush" => dark ? Color.FromArgb(0xC5, 0xFF, 0xFF, 0xFF)
                                                  : Color.FromArgb(0x9E, 0x00, 0x00, 0x00),
            // 3,29:1 en claro. Declarado a propósito —para que el barrido pueda SEÑALARLO— aunque la app
            // ya no lo use: si alguien vuelve a ponerlo en un estilo, el test tiene con qué medirlo y
            // falla, en vez de dejarlo pasar por token desconocido.
            "TextFillColorTertiaryBrush"  => dark ? Color.FromArgb(0x87, 0xFF, 0xFF, 0xFF)
                                                  : Color.FromArgb(0x72, 0x00, 0x00, 0x00),
            "TextFillColorDisabledBrush"  => dark ? Color.FromArgb(0x5D, 0xFF, 0xFF, 0xFF)
                                                  : Color.FromArgb(0x5C, 0x00, 0x00, 0x00),
            // No se llama «TextFillColor…», pero pinta TEXTO: la instrucción de la confirmación y los errores
            // de etiqueta y de presets. El barrido de `T12-01` solo buscaba ese nombre y no lo veía (`T13-02`).
            "SystemFillColorCriticalBrush" => dark ? Color.FromArgb(0xFF, 0xFF, 0x99, 0xA4)
                                                   : Color.FromArgb(0xFF, 0xC4, 0x2B, 0x1C),
            _ => default,
        };
        return color.A != 0;
    }

    /// <summary>
    /// Por qué un pincel de texto <b>no</b> tiene que cumplir el 4,5:1 del texto normal, o <c>null</c> si
    /// tiene que cumplirlo.
    /// </summary>
    /// <remarks>
    /// La excepción se devuelve <b>escrita</b> a propósito: una lista de nombres sin motivo es un alias
    /// silencioso, y por un alias así —el barrido leía <c>AccentTextFillColorPrimaryBrush</c> como si fuera
    /// el texto primario— pasó un color sin medir (`T13-02`). La lista corta es la que la hace útil.
    /// </remarks>
    public static string? ExemptionReason(string brushName) => brushName switch
    {
        "TextFillColorDisabledBrush" =>
            "WCAG 2.x (1.4.3) no exige contraste a los controles deshabilitados: su bajo contraste es la información.",
        "AccentTextFillColorPrimaryBrush" =>
            "Sale del color de acento que elige cada usuario (SystemAccentColorDark2 en claro, SystemAccentColorLight3 "
            + "en oscuro), así que no hay un valor que medir de antemano; Fluent lo deriva para que sirva de texto. "
            + "Desde T13-15 ya no tiñe los títulos de sección —ahí quedó el icono, que es objeto gráfico—: lo que "
            + "sigue usándolo como TEXTO son los encabezados de Acerca de… y Novedades.",
        _ => null,
    };
}
