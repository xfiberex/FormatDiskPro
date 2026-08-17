using FormatDiskPro;
using Xunit;

namespace FormatDiskPro.Tests;

/// <summary>
/// Pruebas del parseo del historial: clasificación de categoría/resultado a partir de las
/// líneas reales que escribe <see cref="History.Log"/>, y descarte de comentarios/vacías.
/// </summary>
[Collection(LanguageCollection.Name)]
public sealed class HistoryEntryTests
{
    private const string Ts = "2026-06-21 14:30:05";

    [Theory]
    [InlineData("FORMAT OK G: fs=NTFS alloc=4096", HistoryCategory.Format, HistoryResult.Ok)]
    [InlineData("FORMAT FAIL G: fs=NTFS code=1",   HistoryCategory.Format, HistoryResult.Fail)]
    [InlineData("FORMAT CANCELLED G: NTFS",        HistoryCategory.Format, HistoryResult.Cancelled)]
    [InlineData("FORMAT ERROR G: boom",            HistoryCategory.Format, HistoryResult.Error)]
    [InlineData("WIPE CANCELLED G:",               HistoryCategory.SecureWipe, HistoryResult.Cancelled)]
    [InlineData("VERIFY OK G: written=123",        HistoryCategory.Verify, HistoryResult.Ok)]
    [InlineData("VERIFY FAIL G: mismatch@5 ok-until=9", HistoryCategory.Verify, HistoryResult.Fail)]
    [InlineData("EJECT G:",                         HistoryCategory.Eject, HistoryResult.Info)]
    [InlineData("UPDATE DOWNLOADED 1.3.0: C:\\x",  HistoryCategory.Update, HistoryResult.Info)]
    [InlineData("UPDATE CHECK ERROR: timeout",     HistoryCategory.Update, HistoryResult.Error)]
    public void Parse_ClassifiesCategoryAndResult(string message, HistoryCategory cat, HistoryResult res)
    {
        var e = HistoryEntry.Parse($"{Ts}\t{message}");
        Assert.NotNull(e);
        Assert.Equal(cat, e!.Category);
        Assert.Equal(res, e.Result);
        Assert.Equal(message, e.Detail);
    }

    [Fact]
    public void Parse_ParsesTimestamp()
    {
        var e = HistoryEntry.Parse($"{Ts}\tEJECT G:");
        Assert.NotNull(e);
        Assert.Equal(new DateTime(2026, 6, 21, 14, 30, 5), e!.Time);
    }

    [Theory]
    [InlineData("# FormatDiskPro — historial de operaciones")]
    [InlineData("")]
    [InlineData("   ")]
    public void Parse_CommentOrBlank_ReturnsNull(string line)
        => Assert.Null(HistoryEntry.Parse(line));

    [Fact]
    public void ParseAll_SkipsInvalidLines()
    {
        string[] lines =
        [
            "# cabecera",
            $"{Ts}\tFORMAT OK G: fs=NTFS",
            "",
            $"{Ts}\tEJECT H:",
        ];
        var entries = HistoryEntry.ParseAll(lines);
        Assert.Equal(2, entries.Count);
        Assert.Equal(HistoryCategory.Format, entries[0].Category);
        Assert.Equal(HistoryCategory.Eject,  entries[1].Category);
    }

    private static HistoryEntry Entry(string message) => HistoryEntry.Parse($"{Ts}\t{message}")!;

    [Fact]
    public void Matches_NullFilters_AlwaysMatch()
        => Assert.True(Entry("FORMAT OK G: fs=NTFS").Matches(null, null, null));

    [Fact]
    public void Matches_CategoryAndResult_Filter()
    {
        var e = Entry("FORMAT OK G: fs=NTFS");
        Assert.True(e.Matches(null, HistoryCategory.Format, HistoryResult.Ok));
        Assert.False(e.Matches(null, HistoryCategory.Eject, null));
        Assert.False(e.Matches(null, null, HistoryResult.Fail));
    }

    [Theory]
    [InlineData("ntfs", true)]    // sin distinción de mayúsculas
    [InlineData("EXFAT", false)]
    [InlineData("  g: ", true)]   // se recorta
    [InlineData("", true)]        // vacío no filtra
    public void Matches_SearchIsCaseInsensitiveAndTrimmed(string search, bool expected)
        => Assert.Equal(expected, Entry("FORMAT OK G: fs=NTFS").Matches(search, null, null));

    [Fact]
    public void ToCsv_HasHeaderAndRowPerEntry()
    {
        var entries = HistoryEntry.ParseAll([$"{Ts}\tFORMAT OK G: fs=NTFS", $"{Ts}\tEJECT H:"]);
        string csv = HistoryEntry.ToCsv(entries);
        var lines = csv.Replace("\r\n", "\n").TrimEnd('\n').Split('\n');
        Assert.Equal("Time,Category,Result,Detail", lines[0]);
        Assert.Equal(3, lines.Length);   // cabecera + 2 filas
        Assert.Contains("Format", lines[1]);
        Assert.Contains("Ok", lines[1]);
    }

    [Fact]
    public void ToCsv_EscapesCommasAndQuotes()
    {
        var e = HistoryEntry.Parse($"{Ts}\tFORMAT OK G: label=\"a,b\"");
        string csv = HistoryEntry.ToCsv([e!]);
        // El detalle contiene coma y comillas → debe ir entrecomillado con comillas duplicadas.
        Assert.Contains("\"FORMAT OK G: label=\"\"a,b\"\"\"", csv);
    }

    // Un detalle que empieza por uno de estos cuatro caracteres lo ejecuta Excel/Calc como FÓRMULA al
    // abrir el CSV exportado, no lo muestra como texto. history.log es texto plano en %AppData% y Parse
    // convierte fielmente en Detail cualquier línea que haya allí, así que el exportador no puede
    // confiar en lo que le llega.
    [Theory]
    [InlineData("=cmd|'/c calc'!A1")]
    [InlineData("+1+1")]
    [InlineData("-2+3")]
    [InlineData("@SUM(1:2)")]
    public void ToCsv_NeutralizesFormulaInjection(string detail)
    {
        string csv = HistoryEntry.ToCsv([HistoryEntry.Parse($"{Ts}\t{detail}")!]);

        // El apóstrofo delante es lo que fuerza a tratarlo como texto (OWASP).
        Assert.Contains($"'{detail}", csv);
        // Y el peligroso no puede quedar nunca al principio de la celda.
        Assert.DoesNotContain($",{detail}", csv);
        Assert.DoesNotContain($"\"{detail}", csv);
    }

    /// <summary>
    /// Un espacio delante no salva: Excel lo recorta y evalúa la fórmula igual. Hoy <see cref="HistoryEntry.Parse"/>
    /// recorta el mensaje, así que este detalle no puede llegar por ahí; se construye la entrada a mano
    /// para ejercitar la guarda, que es justamente lo que protege si el parseo dejara de recortar.
    /// </summary>
    [Fact]
    public void ToCsv_NeutralizesFormula_EvenWithLeadingWhitespace()
    {
        var e = new HistoryEntry(DateTime.MinValue, HistoryCategory.Other, HistoryResult.Info, " =1+1", " =1+1");
        Assert.Contains("' =1+1", HistoryEntry.ToCsv([e]));
    }

    /// <summary>Neutralizar no puede romper el escape de RFC 4180: una fórmula con coma lleva las dos cosas.</summary>
    [Fact]
    public void ToCsv_FormulaWithComma_IsBothPrefixedAndQuoted()
        => Assert.Contains("\"'=1,2\"", HistoryEntry.ToCsv([HistoryEntry.Parse($"{Ts}\t=1,2")!]));

    /// <summary>Lo normal no se toca: sin apóstrofos espurios en las líneas que escribe la app.</summary>
    [Fact]
    public void ToCsv_OrdinaryDetail_IsNotPrefixed()
    {
        string csv = HistoryEntry.ToCsv([HistoryEntry.Parse($"{Ts}\tFORMAT OK G: fs=NTFS")!]);
        Assert.Contains("FORMAT OK G: fs=NTFS", csv);
        Assert.DoesNotContain("'FORMAT", csv);
    }

    // ── T6-05: los tamaños en bytes, legibles al MOSTRARLOS ────────────────────────

    /// <summary>
    /// El historial enseñaba la línea de log cruda: <c>small-fat32=2147483648</c>. Quien lo abre está
    /// comprobando qué le hizo a un disco, no depurando.
    /// </summary>
    [Theory]
    [InlineData("REINIT I: -> G: fs=FAT32 style=MBR small-fat32=2147483648",
                "REINIT I: -> G: fs=FAT32 style=MBR small-fat32=2 GB")]
    [InlineData("VERIFY OK G: written=32010928128", "VERIFY OK G: written=29.8 GB")]
    [InlineData("VERIFY FAIL G: bloque corrupto ok-until=1048576", "VERIFY FAIL G: bloque corrupto ok-until=1 MB")]
    [InlineData("BENCH H: rnd4k r=34.7 MB/s bytes=536870912", "BENCH H: rnd4k r=34.7 MB/s bytes=512 MB")]
    public void Humanize_TurnsRawByteCountsIntoSizes(string raw, string expected)
    {
        // Humanize es presentación, así que desde `T6-12` el separador lo pone el idioma activo; se fija
        // para que estos casos hablen de la conversión a tamaños y no del separador.
        var prev = L.Current;
        try
        {
            L.Set(AppLang.En);
            Assert.Equal(expected, HistoryEntry.Humanize(raw));
        }
        finally { L.Set(prev); }
    }

    /// <summary>
    /// Lista blanca, no heurístico: en la MISMA línea hay números que no son tamaños. Convertir
    /// <c>code=1</c> en «1 B» sería peor que no tocar nada.
    /// </summary>
    [Theory]
    [InlineData("CHKDSK G: repair=False code=0 result=OK")]
    [InlineData("FORMAT OK G: fs=NTFS quick=True compress=False wipe=True passes=3 label='utilidades'")]
    [InlineData("REINIT REJECTED G: DoesNotFit (partición 1)")]
    public void Humanize_LeavesNonSizeNumbersAlone(string raw)
        => Assert.Equal(raw, HistoryEntry.Humanize(raw));

    /// <summary>`alloc` sí es un tamaño: el clúster de 4096 bytes se lee como 4 KB.</summary>
    [Fact]
    public void Humanize_ConvertsAllocationUnit()
        => Assert.Contains("alloc=4 KB", HistoryEntry.Humanize("FORMAT OK G: fs=NTFS alloc=4096 quick=True"));

    /// <summary>Un valor que no cabe en un long se deja como está, en vez de perderlo o lanzar.</summary>
    [Fact]
    public void Humanize_UnparseableValue_IsLeftUntouched()
    {
        string raw = "VERIFY OK G: written=99999999999999999999999";
        Assert.Equal(raw, HistoryEntry.Humanize(raw));
    }

    [Fact]
    public void Humanize_EmptyOrNull_DoesNotThrow()
    {
        Assert.Equal("", HistoryEntry.Humanize(null));
        Assert.Equal("", HistoryEntry.Humanize(""));
    }

    /// <summary>
    /// La transformación es de PRESENTACIÓN: el CSV y `history.log` tienen consumidores y siguen llevando
    /// el byte exacto, que es justo el dato que sirve al depurar. Si alguien mueve `Humanize` a las
    /// llamadas de `History.Log`, esto lo caza.
    /// </summary>
    [Fact]
    public void ToCsv_KeepsTheExactByteCount()
    {
        var e = HistoryEntry.Parse($"{Ts}	REINIT I: -> G: small-fat32=2147483648")!;
        string csv = HistoryEntry.ToCsv([e]);

        Assert.Contains("small-fat32=2147483648", csv);
        Assert.DoesNotContain("2 GB", csv);
        Assert.Equal("REINIT I: -> G: small-fat32=2147483648", e.Detail);
    }

    /// <summary>
    /// El otro lado de la misma regla, y el «cuidado» explícito de `T6-12`: lo que se GUARDA no depende
    /// del idioma. El CSV sale igual con la app en los cinco, marca de tiempo incluida — si algún día se
    /// escribe con <c>L.Culture</c> en vez de con la invariante, un fichero exportado en francés dejaría
    /// de poder leerse con la app en inglés.
    /// </summary>
    [Fact]
    public void ToCsv_IsTheSameInEveryLanguage()
    {
        var e = HistoryEntry.Parse($"{Ts}\tVERIFY OK G: written=32010928128")!;
        var prev = L.Current;
        try
        {
            L.Set(AppLang.En);
            string reference = HistoryEntry.ToCsv([e]);

            foreach (AppLang lang in Enum.GetValues<AppLang>())
            {
                L.Set(lang);
                Assert.Equal(reference, HistoryEntry.ToCsv([e]));
            }
        }
        finally { L.Set(prev); }
    }

    /// <summary>
    /// El buscador tiene que encontrar lo que está EN PANTALLA. La lista muestra «2 GB» y el fichero
    /// guarda «2147483648»: buscar en uno solo haría que teclear lo que se ve no devolviera nada.
    /// </summary>
    [Fact]
    public void Matches_FindsBothTheRawBytesAndWhatIsOnScreen()
    {
        var e = HistoryEntry.Parse($"{Ts}	REINIT I: -> G: small-fat32=2147483648")!;

        Assert.True(e.Matches("2147483648", null, null));   // lo que hay en el log
        Assert.True(e.Matches("2 GB", null, null));         // lo que hay en la lista
        Assert.False(e.Matches("3 GB", null, null));
    }
}
