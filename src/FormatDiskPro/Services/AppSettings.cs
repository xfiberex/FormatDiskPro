using System.Text.Json;

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
    /// <summary>Idioma de la interfaz: <c>"es"</c> o <c>"en"</c>.</summary>
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
            return JsonSerializer.Deserialize<AppSettings>(json) ?? new AppSettings();
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
