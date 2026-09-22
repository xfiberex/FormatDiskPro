using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace FormatDiskPro;

/// <summary>
/// Categoría de una entrada del historial, en el orden en que se ofrecen en el filtro: primero lo que
/// escribe en el disco, luego lo que solo lo lee, y al final la propia aplicación.
/// </summary>
/// <remarks>
/// `T13-07`: había cinco, y la app escribía dieciséis prefijos distintos. Los otros once caían en
/// <see cref="Other"/>, así que el filtro no podía aislar <b>una reinicialización</b> —la operación más
/// destructiva de la app—, ni una comprobación de errores, ni una caída.
/// </remarks>
public enum HistoryCategory
{
    /// <summary>Formateo de un volumen.</summary>
    Format,
    /// <summary>Borrado seguro del espacio libre.</summary>
    SecureWipe,
    /// <summary>Reinicialización del disco físico.</summary>
    Reinit,
    /// <summary>Comprobación o reparación del sistema de archivos (chkdsk).</summary>
    CheckDisk,
    /// <summary>Verificación de la capacidad real.</summary>
    Verify,
    /// <summary>Benchmark de lectura/escritura.</summary>
    Benchmark,
    /// <summary>Consultas de estado del disco: S.M.A.R.T. y tamaño.</summary>
    Health,
    /// <summary>Protección de escritura.</summary>
    WriteProtect,
    /// <summary>Expulsión de una unidad.</summary>
    Eject,
    /// <summary>Actualizaciones y novedades de versión.</summary>
    Update,
    /// <summary>La propia aplicación: preferencias, historial y caídas.</summary>
    App,
    /// <summary>Lo que no encaja en ninguna: una línea escrita por otro programa, o de una versión futura.</summary>
    Other,
}

/// <summary>Resultado de una operación registrada en el historial.</summary>
public enum HistoryResult { Ok, Fail, Error, Cancelled, Info }

/// <summary>
/// Entrada del historial ya interpretada: marca de tiempo, categoría, resultado y mensaje.
/// El parseo es puro y tolerante: las líneas de comentario (<c>#</c>) o vacías se descartan.
/// </summary>
/// <param name="Time">Marca de tiempo (o <see cref="DateTime.MinValue"/> si no se pudo parsear).</param>
/// <param name="Category">Categoría de la operación.</param>
/// <param name="Result">Resultado de la operación.</param>
/// <param name="Detail">Mensaje (sin la marca de tiempo).</param>
/// <param name="Raw">Línea original completa.</param>
public sealed partial record HistoryEntry(
    DateTime Time, HistoryCategory Category, HistoryResult Result, string Detail, string Raw)
{
    /// <summary>
    /// Formato de la marca de tiempo del historial, en el archivo y en el CSV exportado.
    ///
    /// <para><b>Siempre con <see cref="CultureInfo.InvariantCulture"/>, tanto al leer como al escribir</b>
    /// (`T9-07`). Sin proveedor explícito, un formato personalizado usa el <b>calendario</b> de la cultura
    /// del hilo: en un Windows tailandés (<c>th-TH</c>, budista) <c>yyyy</c> produce <b>2569</b> en vez de
    /// 2026, y en <c>ar-SA</c> el año híjri. Como <see cref="Parse"/> reinterpreta esa cifra como año
    /// gregoriano, la entrada no se rechaza: queda 543 años en el futuro y encabeza el orden. Es
    /// <c>internal</c> para que <c>Services/History</c> escriba con la misma constante con la que se lee.</para>
    /// </summary>
    internal const string TimeFormat = "yyyy-MM-dd HH:mm:ss";

    /// <summary>Marca que sustituye a un salto de línea aplanado por <see cref="SanitizeDetail"/>.</summary>
    public const string LineBreakMarker = " ⏎ ";

    /// <summary>
    /// Aplana un mensaje para que ocupe <b>una sola línea</b> del historial.
    ///
    /// <para><b>Por qué hace falta.</b> <c>history.log</c> es un formato de una entrada por línea
    /// (<c>marca de tiempo TAB mensaje</c>) y <see cref="Parse"/> lo lee así. Los caminos de error escriben
    /// texto que no controlamos: <c>ex.Message</c> puede traer saltos de línea, y el registro de caídas
    /// guarda la excepción completa —con su <b>traza de pila</b>, que siempre es multilínea—. Sin aplanar,
    /// **una sola caída se convierte en decenas de entradas fantasma** sin marca de tiempo, categoría
    /// <c>Other</c> y resultado <c>Info</c>: justo el registro que uno va a consultar cuando algo ha ido
    /// mal queda inservible.</para>
    ///
    /// <para>No se recorta la longitud a propósito: en una entrada <c>CRASH:</c> la traza es precisamente
    /// lo que se quiere leer.</para>
    ///
    /// Lógica pura.
    /// </summary>
    public static string SanitizeDetail(string? detail)
    {
        if (string.IsNullOrEmpty(detail)) return "";

        // \r\n primero, para que un salto de Windows no produzca DOS marcas.
        return detail.Replace("\r\n", LineBreakMarker)
                     .Replace("\n", LineBreakMarker)
                     .Replace("\r", LineBreakMarker)
                     .Trim();
    }

    /// <summary>
    /// Claves del log cuyo valor son BYTES en crudo. Es una <b>lista blanca</b>, no un heurístico: en la
    /// misma línea conviven <c>code=1</c>, <c>passes=3</c> o <c>quick=True</c>, y convertir esos a «1 B»
    /// sería peor que no hacer nada. Si mañana se registra un tamaño nuevo, hay que añadirlo aquí — que es
    /// justo la decisión que conviene tomar a conciencia.
    /// </summary>
    private static readonly HashSet<string> ByteValueKeys =
        new(StringComparer.OrdinalIgnoreCase) { "written", "ok-until", "small-fat32", "bytes", "alloc" };

    /// <summary>
    /// Devuelve el detalle con los tamaños en bytes convertidos a algo legible:
    /// <c>small-fat32=2147483648</c> → <c>small-fat32=2 GB</c>. Lógica pura.
    /// </summary>
    /// <remarks>
    /// <para>Nace de `T6-05`: el historial mostraba la línea de log tal cual. Quien abre *Historial de
    /// operaciones* no está depurando —está comprobando qué le hizo a un disco— y <c>2147483648</c> no
    /// responde a eso.</para>
    ///
    /// <para><b>Transforma lo que se MUESTRA, nunca lo que se guarda.</b> <c>history.log</c> y el CSV
    /// siguen llevando el número exacto: son formatos con consumidores, y el byte exacto es justo el dato
    /// que sirve al depurar. Por eso esto es una función de presentación y no un cambio en las llamadas a
    /// <c>History.Log</c> — que además dejaría ilegibles las entradas ya escritas.</para>
    ///
    /// <para>La conversión es de <b>valor</b>, no de línea: se conserva <c>clave=</c> porque identifica el
    /// campo. El objetivo es que el número se entienda, no reescribir el registro en prosa.</para>
    /// </remarks>
    public static string Humanize(string? detail)
    {
        string s = detail ?? "";
        if (s.Length == 0) return s;

        return ByteValueRegex().Replace(s, m =>
        {
            string key = m.Groups["key"].Value;
            if (!ByteValueKeys.Contains(key)) return m.Value;
            return long.TryParse(m.Groups["value"].Value, NumberStyles.Integer, CultureInfo.InvariantCulture,
                                 out long bytes) && bytes >= 0
                ? $"{key}={FormatLogic.FormatBytes(bytes)}"
                : m.Value;
        });
    }

    // clave=valor con la clave alfanumérica (admite guion, como en `small-fat32`) y el valor entero.
    [GeneratedRegex(@"(?<key>[A-Za-z][A-Za-z0-9-]*)=(?<value>\d+)\b")]
    private static partial Regex ByteValueRegex();

    /// <summary>Interpreta una línea del historial. Devuelve <c>null</c> para comentarios o líneas vacías.</summary>
    public static HistoryEntry? Parse(string line)
    {
        if (string.IsNullOrWhiteSpace(line) || line.StartsWith('#')) return null;

        string timePart, message;
        int tab = line.IndexOf('\t');
        if (tab >= 0) { timePart = line[..tab]; message = line[(tab + 1)..].Trim(); }
        else          { timePart = "";          message = line.Trim(); }

        if (message.Length == 0) return null;

        DateTime.TryParseExact(timePart, TimeFormat, CultureInfo.InvariantCulture,
            DateTimeStyles.None, out DateTime time);

        var category = ParseCategory(message);
        var result   = ParseResult(message);
        return new HistoryEntry(time, category, result, message, line);
    }

    /// <summary>Interpreta varias líneas, descartando las no válidas, preservando el orden.</summary>
    public static IReadOnlyList<HistoryEntry> ParseAll(IEnumerable<string> lines)
    {
        var list = new List<HistoryEntry>();
        foreach (var line in lines)
            if (Parse(line) is HistoryEntry e) list.Add(e);
        return list;
    }

    /// <summary>
    /// ¿La entrada cumple el filtro? <paramref name="category"/>/<paramref name="result"/> en <c>null</c>
    /// significan "cualquiera"; la <paramref name="search"/> (sin distinción de mayúsculas, recortada) se
    /// compara contra el detalle. Cadena de búsqueda vacía no filtra. Lógica pura.
    /// </summary>
    /// <remarks>
    /// Se busca en el detalle crudo <b>y</b> en el legible (<see cref="Humanize"/>). Desde `T6-05` la lista
    /// enseña «small-fat32=2 GB» mientras el fichero guarda «2147483648»: buscar solo en uno de los dos
    /// haría que teclear justo lo que se está viendo no encontrara nada. Un buscador que no encuentra lo
    /// que hay en pantalla es peor que no tener buscador.
    /// </remarks>
    public bool Matches(string? search, HistoryCategory? category, HistoryResult? result)
    {
        if (category is HistoryCategory c && Category != c) return false;
        if (result   is HistoryResult   r && Result   != r) return false;
        string s = (search ?? "").Trim();
        return s.Length == 0
            || Detail.Contains(s, StringComparison.OrdinalIgnoreCase)
            || Humanize(Detail).Contains(s, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Serializa entradas a CSV (estilo RFC 4180): cabecera + una fila por entrada con columnas
    /// <c>Time,Category,Result,Detail</c>. Los campos con coma, comillas o saltos de línea se
    /// entrecomillan y las comillas internas se duplican. Salto de línea CRLF. Lógica pura.
    /// Los campos que Excel/Calc interpretarían como fórmula se neutralizan (ver <see cref="CsvField"/>).
    /// </summary>
    public static string ToCsv(IEnumerable<HistoryEntry> entries)
    {
        var sb = new StringBuilder();
        sb.Append("Time,Category,Result,Detail\r\n");
        foreach (var e in entries)
        {
            string time = e.Time == DateTime.MinValue
                ? ""
                : e.Time.ToString(TimeFormat, CultureInfo.InvariantCulture);
            sb.Append(CsvField(time)).Append(',')
              .Append(CsvField(e.Category.ToString())).Append(',')
              .Append(CsvField(e.Result.ToString())).Append(',')
              .Append(CsvField(e.Detail)).Append("\r\n");
        }
        return sb.ToString();
    }

    /// <summary>
    /// Escapa un campo (RFC 4180) y lo neutraliza si Excel/Calc lo interpretarían como <b>fórmula</b>.
    ///
    /// Un valor que empieza por <c>=</c>, <c>+</c>, <c>-</c> o <c>@</c> no se abre como texto sino como
    /// fórmula (CSV injection): <c>=cmd|'/c calc'!A1</c> en una celda intenta ejecutar un programa al
    /// abrir el archivo. Prefijar con apóstrofo obliga a tratarlo como texto (mitigación estándar, OWASP).
    /// Se mira el valor <b>sin espacios delanteros</b>, porque <c>" =cmd|…"</c> también dispara la fórmula.
    ///
    /// Escapar comillas no basta: el escape de RFC 4180 protege la <i>estructura</i> del CSV (que un valor
    /// con comas no parta la fila), no al programa que lo abre después.
    ///
    /// Alcance honesto: hoy las líneas que escribe la propia app siempre empiezan por una palabra clave
    /// (<c>FORMAT</c>, <c>WIPE</c>, <c>EJECT</c>…), y la etiqueta de volumen —lo único que elige el
    /// usuario— va incrustada a mitad del detalle, así que NO alcanza la primera posición del campo. Esto
    /// blinda los dos caminos que sí quedan: <c>history.log</c> es un archivo de texto plano en
    /// <c>%AppData%</c> que cualquier otro proceso puede haber tocado, y <see cref="Parse"/> convierte
    /// fielmente en <c>Detail</c> cualquier línea que encuentre allí; y un futuro formato de log que
    /// empiece por un dato variable dejaría de ser seguro sin que nadie se acordase de esto.
    /// </summary>
    private static string CsvField(string v)
    {
        string trimmed = v.TrimStart();
        if (trimmed.Length > 0 && trimmed[0] is '=' or '+' or '-' or '@')
            v = "'" + v;

        return v.IndexOfAny(['"', ',', '\n', '\r']) < 0 ? v : "\"" + v.Replace("\"", "\"\"") + "\"";
    }

    /// <summary>
    /// Cada palabra clave con la que la app abre una línea del historial, y su categoría.
    /// </summary>
    /// <remarks>
    /// <para>Es una <b>tabla</b> y no una cadena de <c>StartsWith</c> por lo mismo que
    /// <c>SeverityPalette.All()</c> es enumerable: para poder recorrerla. <c>HistoryPrefixTests</c> barre
    /// el código fuente buscando cada <c>Log("PALABRA…</c> y falla si alguna no está aquí, así que añadir
    /// un registro nuevo obliga a decidir su categoría en vez de dejarlo caer en
    /// <see cref="HistoryCategory.Other"/> sin que nadie lo note (`T13-07`).</para>
    ///
    /// <para>Se clasifica por la palabra clave, así que <b>las líneas ya escritas se releen bien</b>: no
    /// hay migración del archivo.</para>
    /// </remarks>
    public static readonly IReadOnlyDictionary<string, HistoryCategory> CategoryByPrefix =
        new Dictionary<string, HistoryCategory>(StringComparer.Ordinal)
        {
            ["FORMAT"]   = HistoryCategory.Format,
            ["WIPE"]     = HistoryCategory.SecureWipe,
            ["REINIT"]   = HistoryCategory.Reinit,
            ["CHKDSK"]   = HistoryCategory.CheckDisk,
            ["VERIFY"]   = HistoryCategory.Verify,
            ["BENCH"]    = HistoryCategory.Benchmark,
            ["HEALTH"]   = HistoryCategory.Health,
            // El tamaño del disco se consulta al seleccionar la unidad y al planificar una
            // reinicialización: es la misma pregunta de estado que S.M.A.R.T., no una operación aparte.
            ["DISKSIZE"] = HistoryCategory.Health,
            ["UNLOCK"]   = HistoryCategory.WriteProtect,
            ["EJECT"]    = HistoryCategory.Eject,
            ["UPDATE"]   = HistoryCategory.Update,
            ["WHATSNEW"] = HistoryCategory.Update,
            ["SETTINGS"] = HistoryCategory.App,
            ["HISTORY"]  = HistoryCategory.App,
            ["EXPORT"]   = HistoryCategory.App,
            ["CRASH"]    = HistoryCategory.App,
            // Un diálogo que no se llegó a abrir porque ya había otro (`T13-19`). Es la única
            // huella de que algo se descartó, y por eso se escribe en vez de callarlo.
            ["DIALOG"]   = HistoryCategory.App,
        };

    /// <summary>Palabra clave con la que empieza el mensaje, sin el <c>:</c> final.</summary>
    private static string Keyword(string message)
    {
        int end = message.IndexOfAny([' ', ':']);
        return end < 0 ? message : message[..end];
    }

    private static HistoryCategory ParseCategory(string message) =>
        CategoryByPrefix.TryGetValue(Keyword(message), out HistoryCategory c) ? c : HistoryCategory.Other;

    private static HistoryResult ParseResult(string message)
    {
        // Una caída es un error, aunque su línea no lleve la palabra: `CRASH: System.…Exception` salía
        // como «Info», con el icono ⓘ, en el registro que uno consulta justo cuando algo ha ido mal
        // (`T13-07`).
        if (Keyword(message) == "CRASH") return HistoryResult.Error;

        if (HasToken(message, "CANCELLED")) return HistoryResult.Cancelled;
        if (HasToken(message, "ERROR"))     return HistoryResult.Error;
        if (HasToken(message, "FAIL"))      return HistoryResult.Fail;
        if (HasToken(message, "OK"))        return HistoryResult.Ok;
        return HistoryResult.Info;
    }

    /// <summary>¿Aparece <paramref name="token"/> como palabra completa? (ignora el ':' final, p. ej. "ERROR:").</summary>
    private static bool HasToken(string message, string token)
    {
        foreach (string part in message.Split(' '))
            if (part.TrimEnd(':') == token) return true;
        return false;
    }
}
