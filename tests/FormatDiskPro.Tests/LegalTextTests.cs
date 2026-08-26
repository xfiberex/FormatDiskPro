using FormatDiskPro;
using Xunit;

namespace FormatDiskPro.Tests;

/// <summary>
/// Verifica que los textos legales embebidos (licencia GPLv3 y avisos de terceros) se cargan desde
/// los recursos del ensamblado para poder mostrarse dentro de la app.
/// </summary>
public sealed class LegalTextTests
{
    [Fact]
    public void License_IsGplV3()
    {
        string text = LegalText.License();
        Assert.False(string.IsNullOrWhiteSpace(text));
        Assert.Contains("GNU GENERAL PUBLIC LICENSE", text);
        Assert.Contains("Version 3", text);
    }

    [Fact]
    public void ThirdParty_ListsComponentsAndLicenses()
    {
        string text = LegalText.ThirdParty();
        Assert.False(string.IsNullOrWhiteSpace(text));
        Assert.Contains("Windows App SDK", text);
        Assert.Contains("MIT", text);
    }

    /// <summary>
    /// Ancho máximo, en columnas, que cabe en el visor de textos legales.
    ///
    /// <para>No es un número redondo elegido a ojo: `T6-14` fijó el ancho del <c>LegalTextDialog</c> en
    /// 430 px con el cuerpo a 10 px y <c>NoWrap</c>, y a esa medida entran ~78 columnas de Consolas. Es
    /// justo lo que mide <c>LICENSE</c> (la GPLv3 viene formateada a 78), que es lo que hizo que 78 fuera
    /// la medida y no otra.</para>
    /// </summary>
    private const int MaxColumns = 78;

    /// <summary>
    /// Ningún texto legal se sale del ancho del visor (`T9-19`).
    ///
    /// <para><b>Por qué hace falta la prueba.</b> `T6-14` arregló el diálogo, pero nada impedía que el
    /// <b>contenido</b> creciera después: estos archivos se editan a mano y una línea de más no rompe
    /// nada visible al escribirla. Y el fallo que produce es de los malos — con <c>NoWrap</c> y sin barra
    /// de desplazamiento horizontal visible, el texto se <b>trunca</b>: se pierde sin avisar, que es peor
    /// que verlo mal. Ocurrió al ampliar los avisos de terceros en `T9-19`: cinco líneas se pasaron.</para>
    ///
    /// <para>Se comprueban los dos textos, no solo el editable: si algún día se actualiza el <c>LICENSE</c>
    /// desde la FSF, conviene enterarse aquí y no en una captura.</para>
    /// </summary>
    [Theory]
    [InlineData("THIRD-PARTY-NOTICES.txt")]
    [InlineData("LICENSE")]
    public void LegalTexts_FitTheViewerWidth(string which)
    {
        string text = which == "LICENSE" ? LegalText.License() : LegalText.ThirdParty();

        var tooWide = text
            .Split('\n')
            .Select((line, i) => (Number: i + 1, Text: line.TrimEnd('\r')))
            .Where(l => l.Text.Length > MaxColumns)
            .Select(l => $"  línea {l.Number} ({l.Text.Length} columnas): {l.Text}")
            .ToList();

        Assert.True(tooWide.Count == 0,
            $"{which}: {tooWide.Count} línea(s) pasan de {MaxColumns} columnas y el visor las TRUNCA " +
            $"(ancho fijo + NoWrap, ver `T6-14`):\n" + string.Join("\n", tooWide));
    }
}
