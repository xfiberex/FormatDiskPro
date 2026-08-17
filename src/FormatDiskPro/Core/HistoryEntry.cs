using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace FormatDiskPro;

/// <summary>Categoría de una operación registrada en el historial.</summary>
public enum HistoryCategory { Format, SecureWipe, Verify, Eject, Update, Other }

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
    private const string TimeFormat = "yyyy-MM-dd HH:mm:ss";

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

    private static HistoryCategory ParseCategory(string message) => message switch
    {
        _ when message.StartsWith("FORMAT", StringComparison.Ordinal) => HistoryCategory.Format,
        _ when message.StartsWith("WIPE",   StringComparison.Ordinal) => HistoryCategory.SecureWipe,
        _ when message.StartsWith("VERIFY", StringComparison.Ordinal) => HistoryCategory.Verify,
        _ when message.StartsWith("EJECT",  StringComparison.Ordinal) => HistoryCategory.Eject,
        _ when message.StartsWith("UPDATE", StringComparison.Ordinal) => HistoryCategory.Update,
        _                                                             => HistoryCategory.Other,
    };

    private static HistoryResult ParseResult(string message)
    {
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
