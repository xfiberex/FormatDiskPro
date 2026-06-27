using FormatDiskPro;
using Xunit;

namespace FormatDiskPro.Tests;

/// <summary>
/// Pruebas del parseo del historial: clasificación de categoría/resultado a partir de las
/// líneas reales que escribe <see cref="History.Log"/>, y descarte de comentarios/vacías.
/// </summary>
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
}
