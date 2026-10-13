using System.Globalization;

namespace FormatDiskPro;

/// <summary>
/// Qué hay <b>de verdad</b> detrás de una letra de unidad: el disco físico y la partición.
/// </summary>
/// <remarks>
/// <para><b>Por qué existe</b> (`AUD-013`). Una operación destructiva se confirma sobre una letra, y entre
/// que se abre la confirmación y se ejecuta pasa el tiempo que el usuario tarde en teclearla. Una letra no
/// es una identidad: se retira un pendrive, se conecta otro disco y Windows le da la misma. Lo que se
/// confirma y lo que se borra tienen que ser lo mismo, y eso solo se puede comprobar comparando lo que
/// había con lo que hay.</para>
///
/// <para><b>Qué se compara.</b> Todo: número, identificador único, número de serie, tamaño y bus del
/// disco, si es de sistema o de arranque, y número, posición y tamaño de la partición. Es deliberadamente
/// estricto —volver a conectar el mismo pendrive puede cambiarle el número de disco y se rechazaría—,
/// porque el coste de un rechazo de más es volver a pulsar el botón, y el de uno de menos, un disco.</para>
/// </remarks>
/// <param name="DiskNumber">Número de disco físico.</param>
/// <param name="DiskSize">Tamaño del disco físico, en bytes.</param>
/// <param name="BusType">Tipo de bus (<c>USB</c>, <c>NVMe</c>, <c>SATA</c>…), como lo da <c>Get-Disk</c>.</param>
/// <param name="IsSystem">Si el disco contiene la partición de sistema.</param>
/// <param name="IsBoot">Si el disco contiene la partición de arranque.</param>
/// <param name="PartitionNumber">Número de la partición dentro del disco.</param>
/// <param name="PartitionOffset">Posición de la partición en el disco, en bytes.</param>
/// <param name="PartitionSize">Tamaño de la partición, en bytes.</param>
/// <param name="DiskSerial">Número de serie del disco; puede venir vacío.</param>
/// <param name="DiskUniqueId">Identificador único del disco; puede venir vacío.</param>
public sealed record DriveIdentity(
    int DiskNumber, long DiskSize, string BusType, bool IsSystem, bool IsBoot,
    int PartitionNumber, long PartitionOffset, long PartitionSize,
    string DiskSerial, string DiskUniqueId)
{
    /// <summary>Número de campos de la línea que entiende <see cref="Parse"/>.</summary>
    public const int FieldCount = 10;

    /// <summary>
    /// Interpreta la línea <c>número|tamaño|bus|sistema|arranque|partición|posición|tamaño|serie|id</c>
    /// que escribe la consulta de <see cref="DiskService"/>. Devuelve <c>null</c> si no es eso.
    /// </summary>
    /// <remarks>
    /// El identificador único va <b>el último</b> y se corta con tope de campos: es el único dato de
    /// formato libre (el de un USB lleva <c>&amp;</c>, <c>\</c>, <c>:</c> y el nombre del equipo), y así una
    /// barra dentro de él no descoloca a los demás.
    /// </remarks>
    public static DriveIdentity? Parse(string? line)
    {
        if (string.IsNullOrWhiteSpace(line)) return null;

        string[] f = line.Trim().Split('|', FieldCount);
        if (f.Length != FieldCount) return null;

        var inv = CultureInfo.InvariantCulture;
        if (!int.TryParse(f[0], NumberStyles.None, inv, out int diskNumber)) return null;
        if (!long.TryParse(f[1], NumberStyles.None, inv, out long diskSize) || diskSize <= 0) return null;
        if (!bool.TryParse(f[3], out bool isSystem)) return null;
        if (!bool.TryParse(f[4], out bool isBoot)) return null;
        if (!int.TryParse(f[5], NumberStyles.None, inv, out int partNumber)) return null;
        if (!long.TryParse(f[6], NumberStyles.None, inv, out long partOffset)) return null;
        if (!long.TryParse(f[7], NumberStyles.None, inv, out long partSize)) return null;

        return new DriveIdentity(diskNumber, diskSize, f[2].Trim(), isSystem, isBoot,
                                 partNumber, partOffset, partSize, f[8].Trim(), f[9].Trim());
    }

    /// <summary>
    /// ¿Describen <c>this</c> y <paramref name="other"/> la misma partición del mismo disco? Los textos se
    /// comparan sin distinguir mayúsculas; todo lo demás, exacto.
    /// </summary>
    public bool SameAs(DriveIdentity? other) =>
        other is not null
        && DiskNumber == other.DiskNumber
        && DiskSize == other.DiskSize
        && IsSystem == other.IsSystem
        && IsBoot == other.IsBoot
        && PartitionNumber == other.PartitionNumber
        && PartitionOffset == other.PartitionOffset
        && PartitionSize == other.PartitionSize
        && string.Equals(BusType, other.BusType, StringComparison.OrdinalIgnoreCase)
        && string.Equals(DiskSerial, other.DiskSerial, StringComparison.OrdinalIgnoreCase)
        && string.Equals(DiskUniqueId, other.DiskUniqueId, StringComparison.OrdinalIgnoreCase);
}
