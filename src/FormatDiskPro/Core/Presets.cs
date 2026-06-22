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

    /// <summary>Longitud máxima admitida para el nombre de un preset propio.</summary>
    public const int MaxNameLength = 40;

    /// <summary>
    /// Normaliza el nombre de un preset: recorta extremos y colapsa espacios internos repetidos.
    /// Lógica pura.
    /// </summary>
    public static string NormalizeName(string? name)
    {
        if (string.IsNullOrWhiteSpace(name)) return "";
        return string.Join(' ', name.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));
    }

    /// <summary>
    /// Indica si <paramref name="name"/> es un nombre de preset válido y disponible: no vacío, dentro
    /// del límite de longitud y no usado por ningún preset existente (comparación sin distinción de
    /// mayúsculas/minúsculas, tras normalizar). Lógica pura. <paramref name="existing"/> son los nombres
    /// ya en uso (integrados + propios).
    /// </summary>
    public static bool IsNameAvailable(string? name, IEnumerable<string> existing)
    {
        string n = NormalizeName(name);
        if (n.Length == 0 || n.Length > MaxNameLength) return false;
        return !existing.Any(e => string.Equals(NormalizeName(e), n, StringComparison.OrdinalIgnoreCase));
    }
}
