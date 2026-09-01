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
    /// Dejar desplegado el panel de rendimiento del pie (disco / CPU / RAM) entre sesiones (`T11-01`).
    /// </summary>
    /// <remarks>
    /// <c>false</c> por defecto: el panel es informativo y ocupa alto en una ventana de tamaño fijo, así
    /// que la app no se lo impone a quien no lo ha pedido. Empezar una operación lo despliega igualmente
    /// —ahí es donde sirve—, y a partir de ese momento la preferencia queda a <c>true</c>: quien lo
    /// vuelva a plegar lo dice explícitamente y se respeta.
    /// </remarks>
    public bool ShowPerformance { get; set; }

    /// <summary>
    /// Comprobar si hay una versión nueva al arrancar. <c>true</c> por defecto (`T9-18`).
    /// </summary>
    /// <remarks>
    /// <para><b>Por qué se puede desactivar.</b> Es la <b>única</b> conexión a Internet que hace la app, y
    /// hasta ahora ocurría en cada arranque sin preguntar y sin forma de evitarla. Contactar con
    /// <c>api.github.com</c> transmite la dirección IP a un tercero: la app no recopila nada, pero quien
    /// no quiera esa conexión tenía que quedarse sin usarla. Con esto es una decisión.</para>
    ///
    /// <para>Tiene además un lado práctico: esta es una utilidad de disco que se usa mucho en equipos
    /// recién montados, en mantenimiento o sin red, donde una comprobación que no puede salir solo añade
    /// una espera inútil.</para>
    ///
    /// <para>Desactivarla NO desactiva las actualizaciones: <i>Ayuda → Buscar actualizaciones…</i> sigue
    /// funcionando, y sigue verificando el instalador por SHA-256 antes de ejecutarlo. Lo que se
    /// desactiva es la comprobación <b>automática</b>.</para>
    /// </remarks>
    public bool CheckUpdatesOnStartup { get; set; } = true;

    /// <summary>
    /// Número de pasadas del borrado seguro: <c>1</c>, <c>3</c> o <c>7</c> (ver <see cref="SecureWipe.AllowedPasses"/>).
    /// <c>1</c> basta en discos modernos (NIST 800-88). <see cref="Load"/> lo normaliza con
    /// <see cref="SecureWipe.NormalizePasses"/>, así que un valor imposible en el archivo nunca llega vivo.
    /// </summary>
    public int SecureWipePasses { get; set; } = 1;

    /// <summary>
    /// Tamaño en GB de la partición FAT32 pequeña al reinicializar en discos grandes (ver
    /// <see cref="ReinitPlan.AllowedSmallFat32SizesGb"/>). <c>32</c> (el máximo) por defecto.
    /// <see cref="Load"/> lo normaliza con <see cref="ReinitPlan.NormalizeSmallFat32SizeGb"/>.
    /// </summary>
    public int SmallFat32SizeGb { get; set; } = 32;

    /// <summary>
    /// Qué hacer con el espacio que sobra al crear una partición FAT32 pequeña: <c>false</c> lo deja sin
    /// asignar (el comportamiento de siempre, que sigue siendo el valor por defecto) y <c>true</c> crea una
    /// segunda partición con todo el sobrante.
    /// </summary>
    public bool CreateSecondPartition { get; set; }

    /// <summary>
    /// Sistema de archivos de esa segunda partición, uno de
    /// <see cref="PartitionPlan.SecondPartitionFileSystems"/>. <see cref="Load"/> lo normaliza con
    /// <see cref="PartitionPlan.NormalizeSecondPartitionFileSystem"/>.
    /// </summary>
    public string SecondPartitionFileSystem { get; set; } = "exFAT";

    /// <summary>
    /// Indica si la configuración se cargó desde un archivo existente (la app ya se había usado),
    /// en contraste con los valores por defecto de una instalación nueva. No se serializa; permite
    /// distinguir una <b>actualización</b> (mostrar novedades) de una <b>instalación nueva</b> aun
    /// cuando la versión previa no guardaba <see cref="LastVersionSeen"/>.
    /// </summary>
    [JsonIgnore]
    public bool LoadedFromFile { get; private set; }

    /// <summary>
    /// Ruta a la que se apartó un <c>settings.json</c> ilegible durante <see cref="Load"/>, o
    /// <c>null</c> si no hubo ninguno (`T9-08`).
    /// </summary>
    /// <remarks>
    /// Se expone en vez de registrarlo aquí porque esta clase <b>no conoce el historial</b>, y darle esa
    /// dependencia solo para una línea rompería que la configuración se pueda cargar desde cualquier
    /// sitio —incluidas las pruebas— sin arrastrar nada más. Quien llama sí tiene dónde escribirlo: es el
    /// mismo reparto que en <c>History.Open</c>, donde el servicio deja salir el fallo y la UI lo cuenta.
    /// </remarks>
    [JsonIgnore]
    public string? PreservedUnreadablePath { get; private set; }

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

            AppSettings? loaded;
            try
            {
                loaded = JsonSerializer.Deserialize<AppSettings>(json);
            }
            catch (JsonException)
            {
                // El archivo existe pero no se puede interpretar. Arrancar con los valores por defecto
                // es correcto —un settings dañado no puede impedir usar la app—, pero NO se puede dejar
                // ahí: el primer Save() lo sobrescribe y con él se van los presets del usuario, que son
                // lo único que la app no sabe reconstruir. Se aparta antes de seguir (`T9-08`).
                return new AppSettings { PreservedUnreadablePath = PreserveUnreadable(path) };
            }

            if (loaded is null) return new AppSettings();
            loaded.LoadedFromFile = true;

            // La documentación de estas dos propiedades decía "se valida al cargar" y no era cierto: la
            // normalización ocurría solo en la UI, al construir sus ComboBox. Un settings.json editado a
            // mano (o escrito por una versión futura) entraba con un valor imposible —0 pasadas, 7 GB de
            // partición— y la UI lo silenciaba eligiendo otro, pero el objeto seguía llevándolo. Se
            // normaliza aquí: es lo que la documentación prometía y el sitio correcto para hacerlo.
            loaded.SecureWipePasses = SecureWipe.NormalizePasses(loaded.SecureWipePasses);
            loaded.SmallFat32SizeGb = ReinitPlan.NormalizeSmallFat32SizeGb(loaded.SmallFat32SizeGb);
            loaded.SecondPartitionFileSystem =
                PartitionPlan.NormalizeSecondPartitionFileSystem(loaded.SecondPartitionFileSystem);
            return loaded;
        }
        catch
        {
            return new AppSettings();
        }
    }

    /// <summary>
    /// Sufijo del archivo al que se aparta un <c>settings.json</c> ilegible.
    /// </summary>
    public const string UnreadableSuffix = ".corrupt.json";

    /// <summary>
    /// Ruta a la que se aparta un archivo de configuración ilegible: la misma con
    /// <see cref="UnreadableSuffix"/> en vez de <c>.json</c>.
    /// </summary>
    /// <param name="path">Ruta del archivo de configuración.</param>
    public static string UnreadablePathFor(string path)
        => Path.ChangeExtension(path, null) + UnreadableSuffix;

    /// <summary>
    /// Aparta un <c>settings.json</c> que no se pudo interpretar, en vez de dejar que el siguiente
    /// <see cref="Save"/> lo pise (`T9-08`).
    /// </summary>
    /// <remarks>
    /// <para><b>Por qué importa.</b> La carga es defensiva a propósito: ante un archivo dañado se arranca
    /// con los valores por defecto y la app sigue siendo utilizable. Pero eso, combinado con que las
    /// preferencias se guardan al salir, convertía un archivo <b>recuperable</b> —al que a lo mejor solo
    /// le falta una llave— en uno <b>perdido</b>, y con él los presets que la persona hubiera creado. Un
    /// fallo de lectura no debería destruir el dato que no se pudo leer.</para>
    ///
    /// <para>Se sobrescribe el <c>.corrupt.json</c> anterior si lo hubiera: dos generaciones no aportan
    /// nada y la buena es siempre la última que falló.</para>
    ///
    /// <para>Defensivo como el resto de esta clase: si apartarlo falla, se sigue con los valores por
    /// defecto. Perder el archivo es malo; no arrancar es peor.</para>
    /// </remarks>
    /// <param name="path">Ruta del archivo ilegible.</param>
    /// <returns>La ruta a la que se apartó, o <c>null</c> si no se pudo.</returns>
    private static string? PreserveUnreadable(string path)
    {
        string target = UnreadablePathFor(path);
        try
        {
            File.Move(path, target, overwrite: true);
            return target;
        }
        catch (IOException) { return null; }
        catch (UnauthorizedAccessException) { return null; }
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
