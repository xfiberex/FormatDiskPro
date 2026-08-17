using FormatDiskPro;
using Xunit;

namespace FormatDiskPro.Tests;

/// <summary>
/// Pruebas de la conversión de notas Markdown a texto plano para el diálogo de novedades (lógica pura).
/// </summary>
public sealed class ReleaseNotesTests
{
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   \n  \n")]
    public void ToPlainText_NullOrBlank_IsEmpty(string? input)
        => Assert.Equal("", ReleaseNotes.ToPlainText(input));

    [Fact]
    public void ToPlainText_StripsHeadingMarkers()
        => Assert.Equal("Novedades", ReleaseNotes.ToPlainText("## Novedades"));

    [Fact]
    public void ToPlainText_ConvertsBulletsToDots()
    {
        string result = ReleaseNotes.ToPlainText("- uno\n* dos\n+ tres");
        Assert.Equal("• uno\n• dos\n• tres", result);
    }

    [Fact]
    public void ToPlainText_RemovesBoldAndCodeMarkers()
        => Assert.Equal("texto importante y codigo",
            ReleaseNotes.ToPlainText("**texto** importante y `codigo`"));

    [Fact]
    public void ToPlainText_ReducesLinksToTheirText()
        => Assert.Equal("ver el repo",
            ReleaseNotes.ToPlainText("ver el [repo](https://github.com/xfiberex/FormatDiskPro)"));

    [Fact]
    public void ToPlainText_CollapsesExtraBlankLines()
        => Assert.Equal("a\n\nb", ReleaseNotes.ToPlainText("a\n\n\n\nb"));

    // ── T6-13: cursivas y párrafos ────────────────────────────────────

    /// <summary>
    /// `T6-13`: se quitaban <c>**</c> y <c>__</c> pero no el marcador simple, así que la pantalla de
    /// novedades —la primera que se ve tras actualizar— mostraba los asteriscos a la vista.
    /// </summary>
    [Theory]
    [InlineData("*Reinicializar unidad* ahora borra el disco", "Reinicializar unidad ahora borra el disco")]
    [InlineData("_Reinicializar unidad_ ahora", "Reinicializar unidad ahora")]
    [InlineData("**negrita** y *cursiva*", "negrita y cursiva")]
    public void ToPlainText_RemovesSingleMarkerEmphasis(string input, string expected)
        => Assert.Equal(expected, ReleaseNotes.ToPlainText(input));

    /// <summary>
    /// El «cuidado» de `T6-13`: quitar todos los <c>*</c> y <c>_</c> a lo bruto se llevaría también los
    /// que no son énfasis. Un marcador sin pareja, o pegado a un espacio, no marca nada en Markdown.
    /// </summary>
    [Theory]
    [InlineData("2 * 3 = 6")]
    [InlineData("el fichero se llama notas_de_version_final")]
    [InlineData("un asterisco suelto * al final")]
    public void ToPlainText_KeepsMarkersThatAreNotEmphasis(string input)
        => Assert.Equal(input, ReleaseNotes.ToPlainText(input));

    /// <summary>
    /// `T6-13`: el Markdown de origen viene ajustado a ~100 columnas y el diálogo ajusta por su cuenta
    /// encima, así que los párrafos salían partidos a mitad de frase. Dentro de un bloque, un salto
    /// simple es un espacio — que es lo que significa en Markdown.
    /// </summary>
    [Fact]
    public void ToPlainText_JoinsSoftLineBreaksInsideAParagraph()
        => Assert.Equal("para actualizar el BIOS de una placa base",
            ReleaseNotes.ToPlainText("para actualizar el\nBIOS de una placa base"));

    /// <summary>Una línea en blanco sí separa párrafos: eso no se puede unir.</summary>
    [Fact]
    public void ToPlainText_KeepsTheSeparationBetweenParagraphs()
        => Assert.Equal("primero\n\nsegundo", ReleaseNotes.ToPlainText("primero\n\nsegundo"));

    /// <summary>
    /// El otro «cuidado»: desenvolver a lo bruto pegaría las viñetas unas con otras. Cada viñeta abre su
    /// propio bloque, pero su continuación ajustada sí se le une.
    /// </summary>
    [Fact]
    public void ToPlainText_DoesNotGlueBulletsTogetherButFoldsTheirContinuation()
    {
        string result = ReleaseNotes.ToPlainText("- primera viñeta que sigue\n  en la línea de abajo\n- segunda");
        Assert.Equal("• primera viñeta que sigue en la línea de abajo\n• segunda", result);
    }

    /// <summary>Un encabezado ocupa una línea y solo una: el párrafo que le sigue no se le pega.</summary>
    [Fact]
    public void ToPlainText_DoesNotFoldTheParagraphIntoTheHeadingAboveIt()
        => Assert.Equal("Novedades\ntexto del párrafo", ReleaseNotes.ToPlainText("## Novedades\ntexto del párrafo"));

    /// <summary>Dos espacios al final son un salto forzado de Markdown: ese sí se respeta.</summary>
    [Fact]
    public void ToPlainText_RespectsAnExplicitHardBreak()
        => Assert.Equal("primera\nsegunda", ReleaseNotes.ToPlainText("primera  \nsegunda"));
}
