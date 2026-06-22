namespace FormatDiskPro;

/// <summary>Estilo de tabla de particiones con el que se reinicializa un disco.</summary>
public enum DiskPartitionStyle { Mbr, Gpt }

/// <summary>Resultado de reinicializar una unidad: éxito, nueva letra asignada y detalle.</summary>
public sealed record ReinitResult(bool Ok, char? NewLetter, string Detail);

/// <summary>
/// Lógica pura y testeable para la reinicialización de unidades (limpiar el disco y recrear una
/// única partición). La ejecución real (limpiar/inicializar/particionar/formatear) vive en
/// <see cref="ReinitDrive"/>, que es E/S.
/// </summary>
public static class ReinitPlan
{
    /// <summary>Límite de direccionamiento de MBR: 2 TB. Por encima se requiere GPT.</summary>
    public const long MbrLimitBytes = 2L * 1024 * 1024 * 1024 * 1024;

    /// <summary>
    /// Elige el estilo de partición según el tamaño del disco: <see cref="DiskPartitionStyle.Gpt"/>
    /// si supera el límite de MBR (2 TB); si no, <see cref="DiskPartitionStyle.Mbr"/> (máxima
    /// compatibilidad para memorias USB). Lógica pura.
    /// </summary>
    /// <param name="diskSizeBytes">Tamaño total del disco en bytes.</param>
    public static DiskPartitionStyle StyleFor(long diskSizeBytes)
        => diskSizeBytes > MbrLimitBytes ? DiskPartitionStyle.Gpt : DiskPartitionStyle.Mbr;

    /// <summary>Nombre del estilo tal como lo espera <c>Initialize-Disk -PartitionStyle</c>.</summary>
    public static string ToPowerShell(this DiskPartitionStyle style)
        => style == DiskPartitionStyle.Gpt ? "GPT" : "MBR";

    /// <summary>
    /// Extrae la letra de unidad emitida por el script de reinicialización en una línea
    /// <c>LETTER:X</c>. Devuelve <c>null</c> si no aparece o no es una letra válida. Lógica pura.
    /// </summary>
    /// <param name="output">Salida combinada del proceso de PowerShell.</param>
    public static char? ParseNewLetter(string? output)
    {
        if (string.IsNullOrEmpty(output)) return null;

        foreach (string raw in output.Split('\n'))
        {
            string line = raw.Trim();
            const string marker = "LETTER:";
            if (!line.StartsWith(marker, StringComparison.OrdinalIgnoreCase)) continue;

            string rest = line[marker.Length..].Trim();
            if (rest.Length >= 1 && char.IsLetter(rest[0]))
                return char.ToUpperInvariant(rest[0]);
        }
        return null;
    }
}
