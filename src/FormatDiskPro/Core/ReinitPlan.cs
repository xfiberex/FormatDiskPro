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
    /// Margen que se reserva en el disco por encima de la partición pedida: 16 MiB. <c>New-Partition</c>
    /// alinea el inicio de la partición (típicamente a 1 MiB) y GPT guarda una copia de la tabla al final
    /// del disco, así que el espacio realmente disponible es algo menor que el tamaño del disco. Sin este
    /// margen, un tamaño que "cabe" por los pelos haría fallar <c>New-Partition</c> — y a esas alturas el
    /// disco ya está borrado.
    /// </summary>
    public const long PartitionReserveBytes = 16L * 1024 * 1024;

    /// <summary>
    /// Tamaños de <see cref="AllowedSmallFat32SizesGb"/> que caben de verdad en un disco de
    /// <paramref name="diskSizeBytes"/>, contando <see cref="PartitionReserveBytes"/>. Vacío si el disco es
    /// desconocido (<c>0</c>) o no da ni para el menor de ellos. Lógica pura.
    /// </summary>
    /// <remarks>
    /// El tamaño que se pasa debe ser el del <b>disco físico</b>, no el del volumen: la reinicialización
    /// borra el disco entero, así que la partición nueva puede ser mayor que la que hay ahora. Usar
    /// <see cref="DriveInfo.TotalSize"/> deja el tope clavado en la partición actual y convierte la función
    /// en un trinquete que solo baja (crear una de 2 GB en un disco de 16 impediría volver a 8).
    /// </remarks>
    /// <param name="diskSizeBytes">Tamaño total del disco físico en bytes.</param>
    public static int[] SmallFat32SizesFor(long diskSizeBytes)
        => diskSizeBytes <= 0
            ? []
            : [.. AllowedSmallFat32SizesGb.Where(gb => SmallFat32PartitionBytes(gb) + PartitionReserveBytes <= diskSizeBytes)];

    /// <summary>
    /// Tamaño a preseleccionar entre los <paramref name="availableGb"/> disponibles: el
    /// <paramref name="preferredGb"/> del usuario si está entre ellos y, si no, el mayor que quepa.
    /// <c>null</c> si no hay ninguno. Lógica pura.
    /// </summary>
    /// <remarks>
    /// Caer al mayor disponible (y no al menor) mantiene el comportamiento de antes en discos grandes,
    /// donde el máximo era el valor por defecto. Quien llama <b>no</b> debe persistir el resultado: la
    /// preferencia guardada es la del usuario, y sustituirla porque hoy hay un pendrive pequeño conectado
    /// la perdería para el siguiente disco.
    /// </remarks>
    /// <param name="preferredGb">Tamaño preferido (el persistido en ajustes).</param>
    /// <param name="availableGb">Tamaños disponibles, en orden ascendente.</param>
    public static int? PickSmallFat32Size(int preferredGb, IReadOnlyList<int> availableGb)
        => availableGb.Count == 0 ? null
         : availableGb.Contains(preferredGb) ? preferredGb
         : availableGb[^1];

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
