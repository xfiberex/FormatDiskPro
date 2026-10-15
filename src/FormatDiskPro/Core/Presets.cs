namespace FormatDiskPro;

/// <summary>
/// Configuraciones de formato predefinidas aplicables con un clic.
/// </summary>
/// <param name="NameKey">
/// Clave de <see cref="L"/> con la que se traduce el nombre en los presets integrados. Los presets del
/// usuario son <c>null</c> aquí: su nombre lo escribe la persona y no se traduce. Se serializa junto al
/// resto en <see cref="AppSettings.UserPresets"/>; al ser opcional, los ajustes guardados por versiones
/// anteriores se siguen leyendo sin migración.
/// </param>
public sealed record FormatPreset(
    string Name,
    string FileSystem,
    long AllocationUnit,
    bool QuickFormat,
    bool Compress,
    bool SecureWipe,
    string? NameKey = null);

public static class Presets
{
    // El `Name` de un preset integrado es su nombre en español y actúa de reserva si la clave faltara;
    // lo que ve la persona usuaria sale siempre de `DisplayName`.
    public static readonly IReadOnlyList<FormatPreset> All =
    [
        new("USB universal (Windows / macOS / Linux)", "exFAT", 131072, QuickFormat: true,  Compress: false, SecureWipe: false, NameKey: "preset.builtin.usb"),
        new("Consola / TV / Cámara",                   "FAT32", 32768,  QuickFormat: true,  Compress: false, SecureWipe: false, NameKey: "preset.builtin.console"),
        new("Disco de datos Windows",                  "NTFS",  4096,   QuickFormat: true,  Compress: false, SecureWipe: false, NameKey: "preset.builtin.windowsData"),
        new("Almacenamiento comprimido (NTFS)",        "NTFS",  4096,   QuickFormat: true,  Compress: true,  SecureWipe: false, NameKey: "preset.builtin.compressed"),
        new("Borrado seguro + NTFS",                   "NTFS",  4096,   QuickFormat: false, Compress: false, SecureWipe: true,  NameKey: "preset.builtin.secureWipe"),
    ];

    /// <summary>
    /// Nombre a mostrar de un preset: el traducido al idioma activo si es integrado, o el que escribió la
    /// persona usuaria si es propio. **Es también el nombre frente al que se comprueban los duplicados**
    /// (<see cref="IsNameAvailable"/>), para que en inglés no se pueda crear un preset propio llamado
    /// «Windows data disk» mientras el integrado se llama así en pantalla.
    /// </summary>
    public static string DisplayName(FormatPreset preset) =>
        string.IsNullOrEmpty(preset.NameKey) ? preset.Name : L.T(preset.NameKey);

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
        => CheckName(name, existing) == PresetNameProblem.None;

    /// <summary>
    /// Por qué no vale <paramref name="name"/> como nombre de preset, o <see cref="PresetNameProblem.None"/>
    /// si vale. Es <see cref="IsNameAvailable"/> con el motivo (`AUD-034`): el diálogo tenía un solo
    /// mensaje, el de nombre repetido, y lo daba también para un nombre vacío. Lógica pura.
    /// </summary>
    public static PresetNameProblem CheckName(string? name, IEnumerable<string> existing)
    {
        string n = NormalizeName(name);
        if (n.Length == 0)             return PresetNameProblem.Empty;
        if (n.Length > MaxNameLength) return PresetNameProblem.TooLong;
        return existing.Any(e => string.Equals(NormalizeName(e), n, StringComparison.OrdinalIgnoreCase))
            ? PresetNameProblem.Duplicate
            : PresetNameProblem.None;
    }

    /// <summary>
    /// ¿Es válido renombrar a <paramref name="newName"/>? Igual que <see cref="IsNameAvailable"/> pero
    /// permite conservar el propio nombre actual (<paramref name="currentName"/>), que se excluye de
    /// <paramref name="existing"/>. Así, al editar un preset, dejar el mismo nombre (o cambiar solo
    /// mayúsculas/espacios) se considera válido. Lógica pura.
    /// </summary>
    public static bool IsRenameAvailable(string? newName, string currentName, IEnumerable<string> existing)
        => CheckRename(newName, currentName, existing) == PresetNameProblem.None;

    /// <summary>Como <see cref="CheckName"/>, excluyendo el nombre actual del preset que se edita.</summary>
    public static PresetNameProblem CheckRename(string? newName, string currentName, IEnumerable<string> existing)
    {
        string cur = NormalizeName(currentName);
        var others = existing.Where(e => !string.Equals(NormalizeName(e), cur, StringComparison.OrdinalIgnoreCase));
        return CheckName(newName, others);
    }

    /// <summary>Clave del texto que explica cada motivo de rechazo de un nombre.</summary>
    public static string MessageKey(PresetNameProblem problem) => problem switch
    {
        PresetNameProblem.Empty   => "preset.emptyName",
        PresetNameProblem.TooLong => "preset.longName",
        _                         => "preset.dupName",
    };
}

/// <summary>Motivo por el que un nombre de preset no vale (`AUD-034`).</summary>
public enum PresetNameProblem { None, Empty, TooLong, Duplicate }
