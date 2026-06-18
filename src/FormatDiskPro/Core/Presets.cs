namespace FormatDiskPro;

/// <summary>
/// Configuraciones de formato predefinidas aplicables con un clic.
/// </summary>
public sealed record FormatPreset(
    string Name,
    string FileSystem,
    long AllocationUnit,
    bool QuickFormat,
    bool Compress,
    bool SecureWipe);

public static class Presets
{
    public static readonly IReadOnlyList<FormatPreset> All =
    [
        new("USB universal (Windows / macOS / Linux)", "exFAT", 131072, QuickFormat: true,  Compress: false, SecureWipe: false),
        new("Consola / TV / Cámara",                   "FAT32", 32768,  QuickFormat: true,  Compress: false, SecureWipe: false),
        new("Disco de datos Windows",                  "NTFS",  4096,   QuickFormat: true,  Compress: false, SecureWipe: false),
        new("Almacenamiento comprimido (NTFS)",        "NTFS",  4096,   QuickFormat: true,  Compress: true,  SecureWipe: false),
        new("Borrado seguro + NTFS",                   "NTFS",  4096,   QuickFormat: false, Compress: false, SecureWipe: true),
    ];
}
