using System.Globalization;
using System.Text;

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
public sealed record HistoryEntry(
    DateTime Time, HistoryCategory Category, HistoryResult Result, string Detail, string Raw)
{
    private const string TimeFormat = "yyyy-MM-dd HH:mm:ss";

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
    public bool Matches(string? search, HistoryCategory? category, HistoryResult? result)
    {
        if (category is HistoryCategory c && Category != c) return false;
        if (result   is HistoryResult   r && Result   != r) return false;
        string s = (search ?? "").Trim();
        return s.Length == 0 || Detail.Contains(s, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Serializa entradas a CSV (estilo RFC 4180): cabecera + una fila por entrada con columnas
    /// <c>Time,Category,Result,Detail</c>. Los campos con coma, comillas o saltos de línea se
    /// entrecomillan y las comillas internas se duplican. Salto de línea CRLF. Lógica pura.
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

    private static string CsvField(string v) =>
        v.IndexOfAny(['"', ',', '\n', '\r']) < 0 ? v : "\"" + v.Replace("\"", "\"\"") + "\"";

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
