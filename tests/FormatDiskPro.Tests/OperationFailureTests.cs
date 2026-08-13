using FormatDiskPro;
using Xunit;

namespace FormatDiskPro.Tests;

/// <summary>
/// El camino de error de las operaciones (`T2-05`), por el lado que sí es lógica pura: qué se escribe en
/// el historial cuando algo falla, y si eso se vuelve a leer bien.
///
/// Los <c>catch</c> de `T0-02` nunca se habían ejecutado en una corrida real, así que nada garantizaba
/// que la entrada que producen fuera siquiera legible por el visor de historial.
/// </summary>
public sealed class OperationFailureTests
{
    private static Exception Boom(string message) => new IOException(message);

    [Theory]
    [InlineData("VERIFY", HistoryCategory.Verify)]
    [InlineData("FORMAT", HistoryCategory.Format)]
    [InlineData("WIPE",   HistoryCategory.SecureWipe)]
    [InlineData("CHECK",  HistoryCategory.Other)]     // chkdsk no tiene categoría propia: cae en Other
    public void LogLine_RoundTripsThroughTheHistoryParser(string operation, HistoryCategory expected)
    {
        string line = OperationFailure.LogLine(operation, 'g', Boom("El dispositivo no está listo."));

        var entry = HistoryEntry.Parse($"2026-08-13 10:00:00\t{line}");

        Assert.NotNull(entry);
        Assert.Equal(expected, entry!.Category);
        Assert.Equal(HistoryResult.Error, entry.Result);
        Assert.Contains("El dispositivo no está listo.", entry.Detail);
    }

    /// <summary>La letra se normaliza, igual que en la guarda del disco de sistema (`T1-01`).</summary>
    [Fact]
    public void LogLine_NormalizesTheDriveLetter()
        => Assert.Contains("VERIFY ERROR G:", OperationFailure.LogLine("VERIFY", 'g', Boom("x")));

    /// <summary>
    /// El defecto real que encontró esta tarea. <c>history.log</c> es de una entrada por línea, y los
    /// mensajes de excepción —y sobre todo las trazas de pila del registro de caídas— son multilínea. Sin
    /// aplanar, una sola caída se parte en decenas de entradas fantasma y el registro que uno consulta
    /// justo cuando algo ha ido mal queda inservible.
    /// </summary>
    [Fact]
    public void LogLine_MultiLineException_StaysOnASingleEntry()
    {
        var ex = Boom("No se puede leer del disco.\r\n   en Foo.Bar()\n   en Baz.Qux()");

        string line = OperationFailure.LogLine("VERIFY", 'G', ex);

        Assert.DoesNotContain('\n', line);
        Assert.DoesNotContain('\r', line);

        // Y lo importante: sigue siendo UNA entrada, no cuatro, y no se ha perdido la traza.
        var entries = HistoryEntry.ParseAll([$"2026-08-13 10:00:00\t{line}"]);
        Assert.Single(entries);
        Assert.Equal(HistoryResult.Error, entries[0].Result);
        Assert.Contains("en Baz.Qux()", entries[0].Detail);
    }

    /// <summary>
    /// Sin aplanar, esto es lo que pasaba: la línea 2 en adelante se convierten en entradas propias, sin
    /// marca de tiempo y clasificadas como <c>Info</c>/<c>Other</c>. Esta prueba fija el comportamiento
    /// **antiguo** para dejar constancia de qué se estaba arreglando; si alguien quita el aplanado, la
    /// prueba de arriba falla y esta explica por qué importaba.
    /// </summary>
    [Fact]
    public void UnsanitizedMultiLineText_WouldHaveBecomeSeveralPhantomEntries()
    {
        string raw = "VERIFY ERROR G: No se puede leer.\r\n   en Foo.Bar()";
        string[] asWrittenToDisk = $"2026-08-13 10:00:00\t{raw}".Split("\r\n");

        var entries = HistoryEntry.ParseAll(asWrittenToDisk);

        Assert.Equal(2, entries.Count);
        Assert.Equal(DateTime.MinValue, entries[1].Time);          // sin marca de tiempo
        Assert.Equal(HistoryCategory.Other, entries[1].Category);
        Assert.Equal(HistoryResult.Info, entries[1].Result);       // un fallo disfrazado de información
    }

    [Theory]
    [InlineData(null, "")]
    [InlineData("", "")]
    [InlineData("una línea", "una línea")]
    [InlineData("  con espacios  ", "con espacios")]
    public void SanitizeDetail_LeavesSingleLineTextAlone(string? input, string expected)
        => Assert.Equal(expected, HistoryEntry.SanitizeDetail(input));

    /// <summary>Un salto de Windows (<c>\r\n</c>) es UNO, no dos: no debe producir dos marcas.</summary>
    [Fact]
    public void SanitizeDetail_WindowsLineBreak_ProducesASingleMarker()
        => Assert.Equal($"a{HistoryEntry.LineBreakMarker}b", HistoryEntry.SanitizeDetail("a\r\nb"));

    [Theory]
    [InlineData("a\nb")]
    [InlineData("a\rb")]
    public void SanitizeDetail_LoneLineBreaks_AreAlsoFlattened(string input)
        => Assert.Equal($"a{HistoryEntry.LineBreakMarker}b", HistoryEntry.SanitizeDetail(input));
}
