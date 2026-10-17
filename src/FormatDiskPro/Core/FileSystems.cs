// SPDX-License-Identifier: GPL-3.0-only
namespace FormatDiskPro;

/// <summary>
/// Lo que la aplicación sabe de un sistema de archivos: tamaños de clúster, el recomendado, la longitud
/// máxima de la etiqueta, el tamaño máximo de volumen y la clave de su descripción.
/// </summary>
/// <param name="Name">Nombre tal como lo aceptan <c>Format-Volume</c> y <c>format.com</c>.</param>
/// <param name="ClusterSizes">Tamaños de unidad de asignación que se ofrecen, en bytes y de menor a mayor.</param>
/// <param name="DefaultClusterSize">Tamaño de unidad de asignación recomendado, uno de <paramref name="ClusterSizes"/>.</param>
/// <param name="MaxLabelLength">Caracteres que admite la etiqueta del volumen.</param>
/// <param name="MaxVolumeBytes">Volumen más grande que se crea con él, o <see langword="null"/> si no hay tope.</param>
/// <param name="DescriptionKey">Clave de localización de su descripción (<c>fs.desc.*</c>).</param>
public sealed record FileSystemSpec(
    string Name, long[] ClusterSizes, long DefaultClusterSize, int MaxLabelLength, long? MaxVolumeBytes,
    string DescriptionKey);

/// <summary>
/// El catálogo de los sistemas de archivos que la aplicación sabe crear, en el orden del selector.
/// </summary>
/// <remarks>
/// Una sola definición de cada dato (`AUD-041`). Antes los tamaños de clúster y las descripciones vivían en
/// la ventana, la lista de nombres en <see cref="PartitionPlan"/>, el tope de FAT32 en <see cref="FormatLogic"/>,
/// el de FAT en <see cref="PartitionPlan"/> y otra vez, como número suelto, en el selector, y la longitud de la
/// etiqueta en un <c>switch</c> aparte: añadir o corregir uno obligaba a encontrar todos los sitios.
/// </remarks>
public static class FileSystems
{
    /// <summary>Límite real de Windows para crear un volumen FAT32 (32 GiB): ni <c>Format-Volume</c> ni
    /// <c>format.com</c> permiten uno mayor. Usado para ocultar FAT32 del selector en discos grandes y
    /// como umbral de la opción de partición FAT32 pequeña en Reinicializar unidad.</summary>
    public const long Fat32MaxBytes = 32L * 1024 * 1024 * 1024;

    /// <summary>Límite de un volumen FAT (FAT16) en esta aplicación: 2 GB, el mismo umbral con el que el
    /// selector de sistema de archivos ofrece FAT.</summary>
    public const long FatMaxBytes = 2L * 1024 * 1024 * 1024;

    /// <summary>Los cinco sistemas de archivos, en el orden en que los ofrece el selector.</summary>
    public static readonly IReadOnlyList<FileSystemSpec> All =
    [
        new("NTFS",  [512, 1024, 2048, 4096, 8192, 16384, 32768, 65536], 4096, 32, null, "fs.desc.ntfs"),
        new("exFAT", [4096, 8192, 16384, 32768, 65536, 131072, 262144, 524288, 1048576], 131072, 11, null, "fs.desc.exfat"),
        new("ReFS",  [4096, 65536], 65536, 32, null, "fs.desc.refs"),
        new("FAT32", [512, 1024, 2048, 4096, 8192, 16384, 32768, 65536], 4096, 11, Fat32MaxBytes, "fs.desc.fat32"),
        new("FAT",   [512, 1024, 2048, 4096, 8192, 16384, 32768], 4096, 11, FatMaxBytes, "fs.desc.fat"),
    ];

    /// <summary>Los nombres de <see cref="All"/>, en el mismo orden.</summary>
    public static readonly string[] Names = All.Select(f => f.Name).ToArray();

    /// <summary>Busca un sistema de archivos por su nombre exacto.</summary>
    /// <param name="name">El nombre, con las mayúsculas de <see cref="FileSystemSpec.Name"/>.</param>
    /// <returns>Su entrada del catálogo, o <see langword="null"/> si la aplicación no lo crea.</returns>
    public static FileSystemSpec? Find(string? name) =>
        All.FirstOrDefault(f => string.Equals(f.Name, name, StringComparison.Ordinal));

    /// <summary>Indica si la aplicación sabe crear el sistema de archivos <paramref name="name"/>.</summary>
    /// <param name="name">El nombre, con las mayúsculas de <see cref="FileSystemSpec.Name"/>.</param>
    /// <returns><see langword="true"/> si está en el catálogo; en caso contrario, <see langword="false"/>.</returns>
    public static bool IsSupported(string? name) => Find(name) is not null;

    /// <summary>Longitud máxima de etiqueta de volumen permitida por sistema de archivos.</summary>
    /// <param name="name">El nombre del sistema de archivos.</param>
    /// <returns>La longitud de su entrada; 32 para uno que no está en el catálogo.</returns>
    public static int MaxLabelLength(string name) => Find(name)?.MaxLabelLength ?? 32;

    /// <summary>
    /// Indica si el sistema de archivos admite un volumen de <paramref name="volumeBytes"/> bytes.
    /// </summary>
    /// <param name="spec">La entrada del catálogo.</param>
    /// <param name="volumeBytes">El tamaño del volumen, o 0 si no se conoce (se admite).</param>
    /// <returns><see langword="true"/> si cabe o no se conoce el tamaño; en caso contrario, <see langword="false"/>.</returns>
    public static bool Fits(FileSystemSpec spec, long volumeBytes) =>
        volumeBytes == 0 || spec.MaxVolumeBytes is not { } max || volumeBytes < max;
}
