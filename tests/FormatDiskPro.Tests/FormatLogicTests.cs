using System.Globalization;
using FormatDiskPro;
using Xunit;

namespace FormatDiskPro.Tests;

/// <summary>
/// Pruebas de la lógica pura crítica: construcción de comandos de formato,
/// parseo de progreso y formateo de bytes. Cubre además el blindaje anti-inyección.
/// </summary>
public sealed class FormatLogicTests : IDisposable
{
    private readonly CultureInfo _prevCulture = CultureInfo.CurrentCulture;

    public FormatLogicTests()
    {
        // FormatBytes usa formato de cultura actual; fijamos invariante para asertar el separador "." de forma estable.
        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
    }

    public void Dispose() => CultureInfo.CurrentCulture = _prevCulture;

    // ── BuildVolumeScript ────────────────────────────────────────

    [Fact]
    public void BuildVolumeScript_QuickNtfs_EmitsCoreParameters()
    {
        string s = FormatLogic.BuildVolumeScript('G', "NTFS", 4096, "DATA", quickFormat: true, compress: false);

        Assert.Contains("Format-Volume", s);
        Assert.Contains("-DriveLetter G", s);
        Assert.Contains("-FileSystem NTFS", s);
        Assert.Contains("-AllocationUnitSize 4096", s);
        Assert.Contains("-NewFileSystemLabel 'DATA'", s);
        Assert.Contains("-Confirm:$false -Force", s);
    }

    [Fact]
    public void BuildVolumeScript_QuickFormat_OmitsFullFlag()
    {
        string s = FormatLogic.BuildVolumeScript('G', "NTFS", 4096, "DATA", quickFormat: true, compress: false);
        Assert.DoesNotContain("-Full", s);
    }

    [Fact]
    public void BuildVolumeScript_FullFormat_IncludesFullFlag()
    {
        string s = FormatLogic.BuildVolumeScript('G', "NTFS", 4096, "DATA", quickFormat: false, compress: false);
        Assert.Contains(" -Full", s);
    }

    [Fact]
    public void BuildVolumeScript_CompressOnNtfs_IncludesCompress()
    {
        string s = FormatLogic.BuildVolumeScript('G', "NTFS", 4096, "DATA", quickFormat: true, compress: true);
        Assert.Contains("-Compress", s);
    }

    [Fact]
    public void BuildVolumeScript_CompressOnNonNtfs_OmitsCompress()
    {
        string s = FormatLogic.BuildVolumeScript('G', "exFAT", 131072, "DATA", quickFormat: true, compress: true);
        Assert.DoesNotContain("-Compress", s);
    }

    [Fact]
    public void BuildVolumeScript_EmptyLabel_OmitsLabelParameter()
    {
        string s = FormatLogic.BuildVolumeScript('G', "NTFS", 4096, "", quickFormat: true, compress: false);
        Assert.DoesNotContain("-NewFileSystemLabel", s);
    }

    [Fact]
    public void BuildVolumeScript_LabelWithSingleQuote_IsEscaped()
    {
        // Anti-inyección: la comilla simple debe duplicarse para no cerrar la cadena de PowerShell.
        string s = FormatLogic.BuildVolumeScript('G', "NTFS", 4096, "My'Drive", quickFormat: true, compress: false);
        Assert.Contains("-NewFileSystemLabel 'My''Drive'", s);
    }

    [Fact]
    public void BuildVolumeScript_MaliciousLabel_StaysInsideQuotedString()
    {
        // Un intento de inyección queda neutralizado: las comillas se duplican y todo permanece como literal.
        const string evil = "'; Remove-Item C:\\ -Recurse -Force #";
        string s = FormatLogic.BuildVolumeScript('G', "NTFS", 4096, evil, quickFormat: true, compress: false);

        Assert.Contains("''; Remove-Item", s);                 // la comilla de apertura fue escapada
        Assert.EndsWith("-Confirm:$false -Force", s);          // el comando real no fue alterado por el payload
    }

    // ── Encode / Decode ──────────────────────────────────────────

    [Fact]
    public void EncodeArguments_ProducesNonInteractiveEncodedCommand()
    {
        string args = FormatLogic.EncodeArguments("Format-Volume -DriveLetter G");
        Assert.Contains("-NonInteractive", args);
        Assert.Contains("-NoProfile", args);
        Assert.Contains("-EncodedCommand ", args);
    }

    [Fact]
    public void EncodeThenDecode_RoundTripsScript()
    {
        string script = FormatLogic.BuildVolumeScript('G', "NTFS", 4096, "My'Drive", quickFormat: false, compress: true);
        string decoded = FormatLogic.DecodeArguments(FormatLogic.EncodeArguments(script));
        Assert.Equal(script, decoded);
    }

    // ── BuildComArgumentList ─────────────────────────────────────

    [Fact]
    public void BuildComArgumentList_HasExpectedElements()
    {
        var args = FormatLogic.BuildComArgumentList('G', "NTFS", 4096, "DATA");
        Assert.Equal(["G:", "/FS:NTFS", "/A:4096", "/V:DATA"], args);
    }

    [Fact]
    public void BuildComArgumentList_EmptyLabel_OmitsVolumeArgument()
    {
        var args = FormatLogic.BuildComArgumentList('G', "NTFS", 4096, "");
        Assert.Equal(["G:", "/FS:NTFS", "/A:4096"], args);
    }

    [Fact]
    public void BuildComArgumentList_LabelWithSpaces_StaysSingleElement()
    {
        // El runtime escapa cada elemento; un espacio o comillas en la etiqueta no inyecta argumentos extra.
        var args = FormatLogic.BuildComArgumentList('G', "NTFS", 4096, "my \" evil");
        Assert.Equal(4, args.Count);
        Assert.Equal("/V:my \" evil", args[^1]);
    }

    // ── MaxLabelLength ───────────────────────────────────────────

    [Theory]
    [InlineData("NTFS", 32)]
    [InlineData("ReFS", 32)]
    [InlineData("exFAT", 11)]
    [InlineData("FAT32", 11)]
    [InlineData("FAT", 11)]
    public void MaxLabelLength_MatchesFileSystemLimits(string fs, int expected)
        => Assert.Equal(expected, FormatLogic.MaxLabelLength(fs));

    // ── ValidateLabel ────────────────────────────────────────────

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public void ValidateLabel_EmptyOrNull_IsOk(string? label)
        => Assert.Equal(FormatLogic.LabelValidation.Ok, FormatLogic.ValidateLabel(label!, "NTFS"));

    [Fact]
    public void ValidateLabel_ValidLabel_IsOk()
        => Assert.Equal(FormatLogic.LabelValidation.Ok, FormatLogic.ValidateLabel("My Drive", "NTFS"));

    [Theory]
    [InlineData("a\\b")]
    [InlineData("a/b")]
    [InlineData("a:b")]
    [InlineData("a*b")]
    [InlineData("a?b")]
    [InlineData("a\"b")]
    [InlineData("a<b")]
    [InlineData("a>b")]
    [InlineData("a|b")]
    public void ValidateLabel_InvalidChar_ReturnsInvalidChars(string label)
        => Assert.Equal(FormatLogic.LabelValidation.InvalidChars, FormatLogic.ValidateLabel(label, "NTFS"));

    [Fact]
    public void ValidateLabel_ExceedsMaxLength_ReturnsTooLong()
        // 12 caracteres > límite de 11 para FAT32.
        => Assert.Equal(FormatLogic.LabelValidation.TooLong, FormatLogic.ValidateLabel("123456789012", "FAT32"));

    [Fact]
    public void ValidateLabel_AtMaxLength_IsOk()
        => Assert.Equal(FormatLogic.LabelValidation.Ok, FormatLogic.ValidateLabel("12345678901", "FAT32"));

    [Fact]
    public void ValidateLabel_InvalidCharsTakesPriorityOverTooLong()
        // Excede el límite de FAT32 (11) Y tiene un carácter inválido: se reporta el carácter, más accionable.
        => Assert.Equal(FormatLogic.LabelValidation.InvalidChars, FormatLogic.ValidateLabel("123456789012:", "FAT32"));

    // ── ExtractPercent ───────────────────────────────────────────

    [Theory]
    [InlineData("50%", 50)]
    [InlineData("Completed 25 percent", 25)]
    [InlineData("Formateando 75 por ciento", 75)]
    [InlineData("100%", 100)]
    [InlineData("0%", 0)]
    public void ExtractPercent_ParsesSingleValue(string chunk, int expected)
        => Assert.Equal(expected, FormatLogic.ExtractPercent(chunk));

    [Fact]
    public void ExtractPercent_MultipleValues_ReturnsLast()
        => Assert.Equal(80, FormatLogic.ExtractPercent("10% ... 40% ... 80%"));

    [Fact]
    public void ExtractPercent_NoMatch_ReturnsMinusOne()
        => Assert.Equal(-1, FormatLogic.ExtractPercent("formatting drive, please wait"));

    // ── FormatBytes ──────────────────────────────────────────────

    [Theory]
    [InlineData(0L, "0 B")]
    [InlineData(512L, "512 B")]
    [InlineData(1024L, "1 KB")]
    [InlineData(1536L, "1.5 KB")]
    [InlineData(1048576L, "1 MB")]
    [InlineData(1073741824L, "1 GB")]
    [InlineData(1099511627776L, "1 TB")]
    public void FormatBytes_FormatsAcrossUnits(long bytes, string expected)
        => Assert.Equal(expected, FormatLogic.FormatBytes(bytes));

    [Theory]
    [InlineData(2L * 1024 * 1024 * 1024, "2 GB")]              // entero: sin ".0"
    [InlineData(62060003328L, "57.8 GB")]                       // no entero: un decimal
    public void FormatBytes_OmitsTrailingZeroDecimal(long bytes, string expected)
        => Assert.Equal(expected, FormatLogic.FormatBytes(bytes));
}
