using System.Text.Json;
using System.Text.Json.Serialization;

namespace FormatDiskPro;

/// <summary>
/// Preferencias persistentes del usuario almacenadas en
/// <c>%AppData%\FormatDiskPro\settings.json</c> (junto al historial).
/// </summary>
/// <remarks>
/// La carga y el guardado son <b>defensivos</b>: nunca lanzan excepciones. Ante un archivo
/// ausente, vacío o corrupto se devuelven los valores por defecto, de modo que un settings.json
/// dañado nunca impide arrancar la aplicación.
/// </remarks>
public sealed class AppSettings
{
    /// <summary>Idioma de la interfaz: <c>"es"</c>, <c>"en"</c>, <c>"pt"</c>, <c>"fr"</c> o <c>"it"</c>.</summary>
    public string Language { get; set; } = "es";

    /// <summary>Modo de tema: <c>"auto"</c>, <c>"light"</c> u <c>"dark"</c>.</summary>
    public string Theme { get; set; } = "auto";

    /// <summary>Letra de la última unidad seleccionada (como cadena), o <c>null</c> si ninguna.</summary>
    public string? LastDriveLetter { get; set; }

    /// <summary>
    /// Última versión de la app con la que se arrancó. Permite mostrar las novedades una sola vez
    /// tras una actualización. <c>null</c> hasta que se registra por primera vez.
    /// </summary>
    public string? LastVersionSeen { get; set; }

    /// <summary>Presets de formato creados por el usuario (se aplican igual que los integrados).</summary>
    public List<FormatPreset> UserPresets { get; set; } = [];

    /// <summary>Avisar (sonido + parpadeo de la barra de tareas) al terminar operaciones largas.</summary>
    public bool NotifyOnFinish { get; set; } = true;

    /// <summary>
    /// Número de pasadas del borrado seguro: <c>1</c>, <c>3</c> o <c>7</c> (ver <see cref="SecureWipe.AllowedPasses"/>).
    /// <c>1</c> basta en discos modernos (NIST 800-88); se valida con <see cref="SecureWipe.NormalizePasses"/> al cargar.
    /// </summary>
    public int SecureWipePasses { get; set; } = 1;

    /// <summary>
    /// Indica si la configuración se cargó desde un archivo existente (la app ya se había usado),
    /// en contraste con los valores por defecto de una instalación nueva. No se serializa; permite
    /// distinguir una <b>actualización</b> (mostrar novedades) de una <b>instalación nueva</b> aun
    /// cuando la versión previa no guardaba <see cref="LastVersionSeen"/>.
    /// </summary>
    [JsonIgnore]
    public bool LoadedFromFile { get; private set; }

    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

    /// <summary>Ruta por defecto del archivo de configuración (mismo directorio que el historial).</summary>
    public static string DefaultPath
    {
        get
        {
            string dir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "FormatDiskPro");
            return Path.Combine(dir, "settings.json");
        }
    }

    /// <summary>
    /// Carga la configuración desde <paramref name="path"/> (o <see cref="DefaultPath"/> si es <c>null</c>).
    /// Devuelve una instancia con valores por defecto ante cualquier error.
    /// </summary>
    /// <param name="path">Ruta del archivo; útil para pruebas. Si es <c>null</c> usa la ruta por defecto.</param>
    public static AppSettings Load(string? path = null)
    {
        try
        {
            path ??= DefaultPath;
            if (!File.Exists(path)) return new AppSettings();
            string json = File.ReadAllText(path);
            var loaded = JsonSerializer.Deserialize<AppSettings>(json);
            if (loaded is null) return new AppSettings();
            loaded.LoadedFromFile = true;
            return loaded;
        }
        catch
        {
            return new AppSettings();
        }
    }

    /// <summary>
    /// Guarda la configuración en <paramref name="path"/> (o <see cref="DefaultPath"/> si es <c>null</c>).
    /// No lanza ante errores de E/S: persistir nunca debe romper la aplicación.
    /// </summary>
    /// <param name="path">Ruta del archivo; útil para pruebas. Si es <c>null</c> usa la ruta por defecto.</param>
    public void Save(string? path = null)
    {
        try
        {
            path ??= DefaultPath;
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            File.WriteAllText(path, JsonSerializer.Serialize(this, JsonOptions));
        }
        catch
        {
            /* persistir nunca debe romper la app */
        }
    }
}
