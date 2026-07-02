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

    /// <summary>Tamaños permitidos (en GB) para la partición FAT32 pequeña al reinicializar, hasta el
    /// límite real de Windows (<see cref="FormatLogic.Fat32MaxBytes"/> = 32 GB).</summary>
    public static readonly int[] AllowedSmallFat32SizesGb = [1, 2, 4, 8, 16, 32];

    /// <summary>Ajusta un tamaño en GB al conjunto permitido (<see cref="AllowedSmallFat32SizesGb"/>): lo
    /// devuelve si es válido, o cae al máximo (32 GB, el comportamiento de antes de que fuera seleccionable).</summary>
    public static int NormalizeSmallFat32SizeGb(int gb)
        => Array.IndexOf(AllowedSmallFat32SizesGb, gb) >= 0 ? gb : 32;

    /// <summary>
    /// Tamaño real solicitado a <c>New-Partition -Size</c> para "FAT32 pequeña", a partir del tamaño
    /// elegido en GB. En el tramo máximo (32 GB, el límite real de Windows) se resta un margen de 4 MiB
    /// frente a <see cref="FormatLogic.Fat32MaxBytes"/>, para que el redondeo/alineación de la partición no
    /// lo iguale o supere (lo que haría fallar el formato ya con el disco borrado); en tramos menores se usa
    /// el valor exacto, sin margen, porque no hay riesgo de alcanzar el límite real de FAT32.
    /// </summary>
    /// <param name="sizeGb">Tamaño elegido en GB (normalizado con <see cref="NormalizeSmallFat32SizeGb"/> si procede).</param>
    public static long SmallFat32PartitionBytes(int sizeGb)
    {
        long bytes = (long)sizeGb * 1024 * 1024 * 1024;
        return bytes >= FormatLogic.Fat32MaxBytes ? FormatLogic.Fat32MaxBytes - 4L * 1024 * 1024 : bytes;
    }

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
