using System.Globalization;

namespace FormatDiskPro;

/// <summary>
/// Detalle S.M.A.R.T. de un disco físico. Los campos numéricos son anulables: muchas unidades
/// (típicamente USB) no exponen contadores de fiabilidad y se devuelven como <c>null</c>.
/// </summary>
/// <param name="Health">Estado de salud (p. ej. "Healthy", "Warning"); "?" si no se reporta.</param>
/// <param name="Bus">Tipo de bus/conexión (p. ej. "NVMe", "SATA", "USB").</param>
/// <param name="Media">Tipo de medio (p. ej. "SSD", "HDD", "Unspecified").</param>
/// <param name="SpindleSpeedRpm">RPM del eje (0 en SSD), o <c>null</c> si no se reporta.</param>
/// <param name="TemperatureC">Temperatura en °C, o <c>null</c>.</param>
/// <param name="PowerOnHours">Horas de encendido, o <c>null</c>.</param>
/// <param name="WearPercent">Desgaste de SSD en %, o <c>null</c>.</param>
/// <param name="ReadErrors">Total de errores de lectura, o <c>null</c>.</param>
/// <param name="WriteErrors">Total de errores de escritura, o <c>null</c>.</param>
public sealed record SmartInfo(
    string Health, string Bus, string Media,
    uint? SpindleSpeedRpm, int? TemperatureC, long? PowerOnHours,
    int? WearPercent, long? ReadErrors, long? WriteErrors)
{
    /// <summary>
    /// Interpreta la línea `|`-delimitada que emite la consulta de salud
    /// (<c>Health|Bus|Media|Spindle|Temp|Hours|Wear|ReadErr|WriteErr</c>).
    /// Devuelve <c>null</c> si la línea está vacía o no contiene separadores.
    /// </summary>
    public static SmartInfo? Parse(string? line)
    {
        string s = (line ?? "").Trim();
        if (s.Length == 0 || !s.Contains('|')) return null;

        string[] p = s.Split('|');
        string F(int i) => i < p.Length ? p[i].Trim() : "";

        return new SmartInfo(
            Health: Text(F(0)), Bus: Text(F(1)), Media: Text(F(2)),
            SpindleSpeedRpm: UIntOrNull(F(3)),
            TemperatureC:    IntOrNull(F(4)),
            PowerOnHours:    LongOrNull(F(5)),
            WearPercent:     IntOrNull(F(6)),
            ReadErrors:      LongOrNull(F(7)),
            WriteErrors:     LongOrNull(F(8)));
    }

    private static string Text(string v) => v.Length == 0 ? "?" : v;

    private static int?  IntOrNull(string v)
        => int.TryParse(v, NumberStyles.Integer, CultureInfo.InvariantCulture, out int n) ? n : null;

    private static long? LongOrNull(string v)
        => long.TryParse(v, NumberStyles.Integer, CultureInfo.InvariantCulture, out long n) ? n : null;

    private static uint? UIntOrNull(string v)
        => uint.TryParse(v, NumberStyles.Integer, CultureInfo.InvariantCulture, out uint n) ? n : null;
}
