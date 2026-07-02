using System.Globalization;

namespace FormatDiskPro;

/// <summary>Nivel de severidad de una métrica S.M.A.R.T., para colorearla y describir su estado (accesibilidad).</summary>
public enum SmartLevel { Unknown, Ok, Warning, Critical }

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

    /// <summary>
    /// Clasifica una temperatura (°C) en niveles: ≤ 50 normal, 51–60 atención, &gt; 60 crítico.
    /// <c>null</c> → <see cref="SmartLevel.Unknown"/>. Lógica pura.
    /// </summary>
    public static SmartLevel TemperatureLevel(int? celsius) =>
        celsius is not int c ? SmartLevel.Unknown
        : c <= 50 ? SmartLevel.Ok
        : c <= 60 ? SmartLevel.Warning
        : SmartLevel.Critical;

    /// <summary>
    /// Clasifica el desgaste de SSD (% consumido, mayor = peor): &lt; 70 normal, 70–89 atención,
    /// ≥ 90 crítico. <c>null</c> → <see cref="SmartLevel.Unknown"/>. Lógica pura.
    /// </summary>
    public static SmartLevel WearLevel(int? wearPercent) =>
        wearPercent is not int w ? SmartLevel.Unknown
        : w < 70 ? SmartLevel.Ok
        : w < 90 ? SmartLevel.Warning
        : SmartLevel.Critical;

    /// <summary>
    /// Clasifica el <c>HealthStatus</c> que reporta el disco físico (enumeración de Storage, siempre
    /// en inglés: "Healthy" / "Warning" / "Unhealthy") en niveles. Cualquier otro valor (vacío, "?",
    /// no reportado) → <see cref="SmartLevel.Unknown"/>. Lógica pura.
    /// </summary>
    public static SmartLevel HealthLevel(string? health) => (health ?? "").Trim().ToUpperInvariant() switch
    {
        "HEALTHY"   => SmartLevel.Ok,
        "WARNING"   => SmartLevel.Warning,
        "UNHEALTHY" => SmartLevel.Critical,
        _           => SmartLevel.Unknown,
    };

    /// <summary>
    /// Clasifica un contador de errores de lectura/escritura: 0 normal, 1–99 atención, ≥ 100 crítico.
    /// <c>null</c> → <see cref="SmartLevel.Unknown"/>. Lógica pura.
    /// </summary>
    public static SmartLevel ErrorLevel(long? errors) =>
        errors is not long e ? SmartLevel.Unknown
        : e == 0 ? SmartLevel.Ok
        : e < 100 ? SmartLevel.Warning
        : SmartLevel.Critical;

    private static string Text(string v) => v.Length == 0 ? "?" : v;

    private static int?  IntOrNull(string v)
        => int.TryParse(v, NumberStyles.Integer, CultureInfo.InvariantCulture, out int n) ? n : null;

    private static long? LongOrNull(string v)
        => long.TryParse(v, NumberStyles.Integer, CultureInfo.InvariantCulture, out long n) ? n : null;

    private static uint? UIntOrNull(string v)
        => uint.TryParse(v, NumberStyles.Integer, CultureInfo.InvariantCulture, out uint n) ? n : null;
}
