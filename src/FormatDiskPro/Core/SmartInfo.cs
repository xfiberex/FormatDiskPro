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

    /// <summary>Unidad en la que se expresa la equivalencia legible de las horas de encendido.</summary>
    public enum PowerOnUnit { None, Days, Months, Years }

    /// <summary>Equivalencia legible de unas horas de encendido: cuánto, y en qué unidad.</summary>
    /// <param name="Unit">Unidad elegida; <see cref="PowerOnUnit.None"/> si no merece la pena traducirlo.</param>
    /// <param name="Value">Cantidad en esa unidad, redondeada a un decimal.</param>
    public readonly record struct PowerOnSpan(PowerOnUnit Unit, double Value);

    // 365,25 días/año repartidos en 12 meses. Se usa el año juliano, no 365, para que «2 años» sean dos
    // años de reloj y no dos años y medio día.
    private const double HoursPerDay   = 24;
    private const double HoursPerMonth = 365.25 * 24 / 12;   // 730,5
    private const double HoursPerYear  = 365.25 * 24;        // 8766

    /// <summary>
    /// Traduce unas horas de encendido a una escala que se entienda de un vistazo. Lógica pura.
    /// </summary>
    /// <remarks>
    /// <para>Nace de `T6-04`: la fila decía «32161 h». El dato existe para responder «¿cuánto ha vivido
    /// este disco?», y en horas nadie lo responde de cabeza.</para>
    ///
    /// <para><b>Siempre con un decimal, y por eso nunca hay que pluralizar.</b> «1,0 años» concuerda en
    /// los cinco idiomas; «1 años» no. Evitar la concordancia singular/plural en cinco traducciones vale
    /// más que ahorrar un decimal.</para>
    ///
    /// <para>La unidad se elige por tramos para que el número tenga siempre magnitud útil: por debajo de
    /// un día no se traduce nada (las horas ya se leen), hasta dos meses en días, hasta dos años en meses,
    /// y a partir de ahí en años. Los cortes están a <b>dos</b> unidades, no a una, para no mostrar
    /// «≈ 1,1 meses» pudiendo decir «≈ 33,5 días».</para>
    /// </remarks>
    /// <param name="hours">Horas de encendido, o <c>null</c> si el disco no las reporta.</param>
    public static PowerOnSpan PowerOnEquivalent(long? hours)
    {
        if (hours is not long h || h < HoursPerDay) return new(PowerOnUnit.None, 0);

        (PowerOnUnit unit, double divisor) =
            h < 2 * HoursPerMonth ? (PowerOnUnit.Days,   HoursPerDay)
          : h < 2 * HoursPerYear  ? (PowerOnUnit.Months, HoursPerMonth)
          :                         (PowerOnUnit.Years,  HoursPerYear);

        return new(unit, Math.Round(h / divisor, 1));
    }

    /// <summary>
    /// ¿Tiene esta unidad un eje que gire? <c>false</c> en estado sólido, donde la velocidad de rotación
    /// no es un dato desconocido: es una pregunta que no aplica. Lógica pura.
    /// </summary>
    /// <remarks>
    /// <para>Dos señales, en este orden. <b>RPM = 0</b> es la respuesta explícita del propio disco («no
    /// giro»), así que manda sobre cualquier otra cosa. Si no reporta RPM en absoluto, se mira el tipo de
    /// medio: un USB que no expone contadores puede seguir diciendo que es SSD.</para>
    ///
    /// <para>Cuando no hay ninguna de las dos, la respuesta es <c>true</c> —«asume que gira»— a propósito:
    /// significa «no lo sé», y en ese caso la interfaz debe mostrar la fila como *no disponible* en vez de
    /// esconderla. Esconder por desconocimiento sería afirmar algo que no sabemos.</para>
    ///
    /// <para>Nace de `T6-03`: la fila decía «Velocidad de rotación: SSD» —una velocidad cuyo valor es un
    /// tipo de medio— con «Tipo de medio: SSD» justo encima.</para>
    /// </remarks>
    public static bool HasSpindle(SmartInfo? info)
    {
        if (info is null) return false;
        if (info.SpindleSpeedRpm is uint rpm) return rpm != 0;
        return !info.Media.Contains("SSD", StringComparison.OrdinalIgnoreCase);
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
