// SPDX-License-Identifier: GPL-3.0-only
namespace FormatDiskPro;

/// <summary>
/// Cómo llenan el espacio libre de una unidad el borrado seguro y la verificación de capacidad: el tamaño
/// de cada escritura, el de cada archivo y lo que se deja sin llenar.
/// </summary>
/// <remarks>
/// Una sola definición (`AUD-041`): <see cref="SecureWipe"/> y <see cref="CapacityVerifier"/> llevaban cada
/// uno su copia de los tres valores.
/// </remarks>
public static class FreeSpaceFill
{
    /// <summary>Tamaño de cada escritura: 8 MB. En la verificación es además la unidad del patrón anti-aliasing.</summary>
    public const int BlockSize = 8 * 1024 * 1024;

    /// <summary>Tamaño máximo de cada archivo de relleno: 1 GB, que cabe incluso en FAT32.</summary>
    public const long MaxFileSize = 1L * 1024 * 1024 * 1024;

    /// <summary>Margen de seguridad: deja este espacio libre para no llenar la unidad por completo.</summary>
    public const long SafetyMarginBytes = 64L * 1024 * 1024;
}
