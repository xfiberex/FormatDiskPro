using System.Globalization;
using FormatDiskPro;
using Xunit;

namespace FormatDiskPro.Tests;

/// <summary>
/// Pruebas de la lógica pura crítica: construcción de comandos de formato,
/// parseo de progreso y formateo de bytes. Cubre además el blindaje anti-inyección.
/// </summary>
[Collection(LanguageCollection.Name)]
public sealed class FormatLogicTests : IDisposable
{
    private readonly CultureInfo _prevCulture = CultureInfo.CurrentCulture;

    public FormatLogicTests()
    {
        // Desde `T6-12`, FormatBytes NO usa la cultura del hilo: la recibe o la toma de L.Culture. Esta
        // fijación se queda como suelo estable para el resto de la clase —y para dejar constancia de que
        // la cultura del hilo ya no decide nada aquí, que es justo lo que comprueba
        // SettingTheAppLanguage_DoesNotTouchTheThreadCulture.
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

    /// <summary>
    /// La codificación se ancla al <b>Base64 concreto</b>, no solo a que <c>DecodeArguments</c> la
    /// deshaga (`T9-13`).
    ///
    /// <para><b>Por qué hace falta las dos cosas.</b> La prueba de ida y vuelta de arriba comprueba que
    /// dos funciones nuestras son inversas — y eso <b>seguiría pasando</b> si ambas compartieran el mismo
    /// error, por ejemplo codificando en UTF-8 en vez de en UTF-16LE. Lo que ejecuta el script no es
    /// <c>DecodeArguments</c>: es <c>powershell.exe -EncodedCommand</c>, que exige <b>Base64 de
    /// UTF-16LE</b>. Esta prueba compara contra eso, calculado aparte.</para>
    /// </summary>
    [Fact]
    public void EncodeArguments_ProducesBase64OfUtf16LittleEndian()
    {
        const string script = "Format-Volume -DriveLetter G";
        string expected = Convert.ToBase64String(System.Text.Encoding.Unicode.GetBytes(script));

        string args = FormatLogic.EncodeArguments(script);

        Assert.EndsWith($"-EncodedCommand {expected}", args, StringComparison.Ordinal);
    }

    /// <summary>
    /// Los dos constructores de comandos rechazan lo que no está en la lista blanca (`T9-09`).
    ///
    /// <para>No cierra un agujero explotable hoy —el valor sale del <c>ComboBox</c> del XAML, y aplicar un
    /// preset exige que su sistema de archivos coincida con un ítem del selector—, sino que impide que la
    /// seguridad de la ruta que formatea dependa de que todos los llamantes validen antes. Es la misma
    /// guarda que <c>DiskService</c> aplica en sus cinco métodos.</para>
    /// </summary>
    [Theory]
    [InlineData("NTFS; Remove-Item C:\\ -Recurse")]
    [InlineData("ntfs")]                               // la comparación es ordinal: no vale otra caja
    [InlineData("")]
    public void CommandBuilders_RejectFileSystemsOutsideTheWhitelist(string fs)
    {
        Assert.Throws<ArgumentException>(
            () => FormatLogic.BuildVolumeScript('G', fs, 4096, "DATA", quickFormat: true, compress: false));

        Assert.Throws<ArgumentException>(
            () => FormatLogic.BuildComArgumentList('G', fs, 4096, "DATA"));
    }

    /// <summary>Y tampoco aceptan algo que no sea una letra de unidad.</summary>
    [Theory]
    [InlineData(';')]
    [InlineData('1')]
    [InlineData(' ')]
    public void CommandBuilders_RejectNonLetterDrives(char letter)
    {
        Assert.Throws<ArgumentException>(
            () => FormatLogic.BuildVolumeScript(letter, "NTFS", 4096, "DATA", quickFormat: true, compress: false));

        Assert.Throws<ArgumentException>(
            () => FormatLogic.BuildComArgumentList(letter, "NTFS", 4096, "DATA"));
    }

    // ── BuildComArgumentList ─────────────────────────────────────

    [Fact]
    public void BuildComArgumentList_HasExpectedElements()
    {
        var args = FormatLogic.BuildComArgumentList('G', "NTFS", 4096, "DATA");
        Assert.Equal(["G:", "/FS:NTFS", "/A:4096", "/Y", "/V:DATA"], args);
    }

    [Fact]
    public void BuildComArgumentList_EmptyLabel_OmitsVolumeArgument()
    {
        var args = FormatLogic.BuildComArgumentList('G', "NTFS", 4096, "");
        Assert.Equal(["G:", "/FS:NTFS", "/A:4096", "/Y"], args);
    }

    /// <summary>
    /// `/Y` es la diferencia entre "formatea" y "se queda esperando una tecla que nunca llega". Sin él,
    /// format.com pregunta por consola y hay que contestarle en el idioma de Windows: escribir "Y"/"S"
    /// acertaba en inglés y español, y colgaba el formato a medias en francés (O) o alemán (J).
    /// Va en TODAS las invocaciones, con etiqueta y sin ella.
    /// </summary>
    [Theory]
    [InlineData("MI ETIQUETA")]
    [InlineData("")]
    public void BuildComArgumentList_AlwaysSuppressesPrompts(string label)
        => Assert.Contains("/Y", FormatLogic.BuildComArgumentList('G', "NTFS", 4096, label));

    [Fact]
    public void BuildComArgumentList_LabelWithSpaces_StaysSingleElement()
    {
        // El runtime escapa cada elemento; un espacio o comillas en la etiqueta no inyecta argumentos extra.
        var args = FormatLogic.BuildComArgumentList('G', "NTFS", 4096, "my \" evil");
        Assert.Equal(5, args.Count);
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

    /// <summary>
    /// format.com escribe la PALABRA, no el símbolo «%», y en el idioma de <b>Windows</b> — que no es el
    /// idioma de la app: se puede tener FormatDiskPro en español sobre un Windows alemán. El patrón cubría
    /// solo inglés y español (el mismo par que la respuesta "Y"/"S" que había en RunFormatComAsync), así
    /// que en un Windows francés o italiano la barra se quedaba clavada en 0 durante todo un formato
    /// completo sin que nada fallara.
    /// </summary>
    [Theory]
    [InlineData("42 percent completed.", 42)]           // en
    [InlineData("42 por ciento completado.", 42)]       // es
    [InlineData("42 por cento concluído.", 42)]         // pt
    [InlineData("42 per cento completato.", 42)]        // it
    [InlineData("42 pour cent effectué.", 42)]          // fr
    [InlineData("42 Prozent abgeschlossen.", 42)]       // de
    [InlineData("42% completado", 42)]                  // por si alguna build sí usa el símbolo
    public void ExtractPercent_UnderstandsEachLanguage(string chunk, int expected)
        => Assert.Equal(expected, FormatLogic.ExtractPercent(chunk));

    /// <summary>
    /// La lista de idiomas es incompleta por naturaleza y no puede dejar de serlo. Lo que importa es CÓMO
    /// se degrada: sin coincidencia devuelve -1, que el llamador traduce en "no muevas la barra". El
    /// formato sigue siendo correcto; lo único que se pierde es el progreso.
    /// </summary>
    [Fact]
    public void ExtractPercent_UnknownLanguage_DegradesToNoProgress()
        => Assert.Equal(-1, FormatLogic.ExtractPercent("42 procent voltooid."));   // nl

    // ── FormatBytes ──────────────────────────────────────────────

    // La escalera de unidades no depende del idioma, así que se fija la cultura en la llamada: si estas
    // pruebas no la dijeran, estarían midiendo dos cosas a la vez. La cultura tiene sus propias pruebas
    // justo debajo.
    [Theory]
    [InlineData(0L, "0 B")]
    [InlineData(512L, "512 B")]
    [InlineData(1024L, "1 KB")]
    [InlineData(1536L, "1.5 KB")]
    [InlineData(1048576L, "1 MB")]
    [InlineData(1073741824L, "1 GB")]
    [InlineData(1099511627776L, "1 TB")]
    public void FormatBytes_FormatsAcrossUnits(long bytes, string expected)
        => Assert.Equal(expected, FormatLogic.FormatBytes(bytes, CultureInfo.InvariantCulture));

    [Theory]
    [InlineData(2L * 1024 * 1024 * 1024, "2 GB")]              // entero: sin ".0"
    [InlineData(62060003328L, "57.8 GB")]                       // no entero: un decimal
    public void FormatBytes_OmitsTrailingZeroDecimal(long bytes, string expected)
        => Assert.Equal(expected, FormatLogic.FormatBytes(bytes, CultureInfo.InvariantCulture));

    /// <summary>
    /// `T6-12`: el separador decimal lo pone el idioma elegido en la app, no Windows. Antes de esto,
    /// estas cuatro pruebas afirmaban el separador inglés con la app arrancando en español — el propio
    /// fallo, dentro de la suite.
    /// </summary>
    [Theory]
    [InlineData(AppLang.Es, "1,5 KB")]
    [InlineData(AppLang.En, "1.5 KB")]
    [InlineData(AppLang.Pt, "1,5 KB")]
    [InlineData(AppLang.Fr, "1,5 KB")]
    [InlineData(AppLang.It, "1,5 KB")]
    public void FormatBytes_UsesTheDecimalSeparatorOfTheAppLanguage(AppLang lang, string expected)
    {
        var prev = L.Current;
        try
        {
            L.Set(lang);
            Assert.Equal(expected, FormatLogic.FormatBytes(1536));
        }
        finally { L.Set(prev); }
    }

    /// <summary>
    /// El «cuidado» de `T6-12`: <see cref="L.Culture"/> es solo para formatear lo que se muestra. Si
    /// además se asignara a <see cref="CultureInfo.CurrentCulture"/>, volvería `T1-01` — la guarda de
    /// disco de sistema fallando bajo cultura turca, donde <c>ToUpper('i')</c> no da <c>'I'</c>.
    /// </summary>
    [Fact]
    public void SettingTheAppLanguage_DoesNotTouchTheThreadCulture()
    {
        var prevLang    = L.Current;
        var prevCulture = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("tr-TR");
            L.Set(AppLang.Fr);
            Assert.Equal("tr-TR", CultureInfo.CurrentCulture.Name);
            Assert.Equal("fr-FR", L.Culture.Name);
        }
        finally { CultureInfo.CurrentCulture = prevCulture; L.Set(prevLang); }
    }
}
