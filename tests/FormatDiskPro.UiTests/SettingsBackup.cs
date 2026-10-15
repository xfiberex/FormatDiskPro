namespace FormatDiskPro.UiTests;

/// <summary>
/// Copia y restaura los archivos de <c>%AppData%\FormatDiskPro\</c> que la app escribe
/// (<c>settings.json</c>, <c>history.log</c> y su generación rotada <c>history.1.log</c>) — la app es
/// unpackaged y no tiene almacenamiento aislado, así que sin esto las pruebas dejarían idioma/tema/
/// última unidad e historial de operaciones de prueba mezclados con el uso real del usuario.
///
/// <para>El respaldo vive en memoria <b>y en disco</b> (`AUD-040`). Antes solo en memoria: si la corrida
/// moría (Ctrl+C, un tiempo agotado, una caída), no quedaba de dónde restaurar, y los ajustes reales se
/// quedaban como los dejaron las pruebas. Ahora la copia en disco se escribe al capturar, se borra al
/// restaurar, y si la siguiente corrida encuentra una sin consumir, la restaura <b>antes</b> de capturar:
/// lo que hay en ese momento en <c>%AppData%</c> es lo que dejó la corrida muerta, no lo del usuario.</para>
/// </summary>
public sealed class SettingsBackup
{
    /// <summary>
    /// history.1.log incluida aunque haga falta rotar 2 MB para que exista: si algún día aparece, se
    /// restaura como el resto en vez de quedarse en el %AppData% del usuario.
    /// </summary>
    internal static readonly string[] Files = ["settings.json", "history.log", "history.1.log"];

    /// <summary>
    /// Se escribe el <b>último</b>: una copia sin él es una captura que murió a medias, y restaurarla
    /// podría borrar archivos que no llegó a copiar.
    /// </summary>
    internal const string CompleteMarker = "respaldo-completo";

    /// <summary>Junto a una copia, indica que el archivo no existía: restaurar es borrarlo.</summary>
    private const string AbsentSuffix = ".no-existia";

    private readonly Dictionary<string, byte[]?> _originals;
    private readonly string _backupDir;

    /// <summary>
    /// <c>false</c> si quedó pendiente una copia de una corrida anterior que no se pudo restaurar entera:
    /// esa copia es la buena, así que ni se sobrescribe al capturar ni se borra al restaurar.
    /// </summary>
    private readonly bool _ownsDiskCopy;

    private SettingsBackup(Dictionary<string, byte[]?> originals, string backupDir, bool ownsDiskCopy)
    {
        _originals    = originals;
        _backupDir    = backupDir;
        _ownsDiskCopy = ownsDiskCopy;
    }

    /// <summary>Carpeta de la app: la misma que usa la instalación real del usuario.</summary>
    private static string DefaultAppDir => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "FormatDiskPro");

    /// <summary>
    /// Copia en disco: fuera de la carpeta de la app, para que la app no la vea ni la toque, y en
    /// <c>%LocalAppData%</c> porque no tiene que viajar con un perfil móvil.
    /// </summary>
    private static string DefaultBackupDir => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "FormatDiskPro.UiTests", "respaldo-ajustes");

    /// <summary>Respalda los archivos reales del usuario.</summary>
    public static SettingsBackup Capture() => Capture(DefaultAppDir, DefaultBackupDir);

    /// <summary>
    /// Respalda los archivos de <paramref name="appDir"/> en memoria y en <paramref name="backupDir"/>.
    /// Si en <paramref name="backupDir"/> queda una copia completa de una corrida que no terminó, la
    /// restaura primero.
    /// </summary>
    /// <param name="appDir">Carpeta donde la app escribe sus archivos.</param>
    /// <param name="backupDir">Carpeta de la copia en disco; se crea y se borra entera.</param>
    internal static SettingsBackup Capture(string appDir, string backupDir)
    {
        bool pendingCleared = RestorePending(appDir, backupDir);

        var originals = new Dictionary<string, byte[]?>();
        foreach (string name in Files)
        {
            string path = Path.Combine(appDir, name);
            originals[path] = File.Exists(path) ? File.ReadAllBytes(path) : null;
        }

        if (pendingCleared) WriteToDisk(originals, backupDir);
        return new SettingsBackup(originals, backupDir, ownsDiskCopy: pendingCleared);
    }

    /// <summary>
    /// Devuelve los archivos a como estaban al capturar y, si todo fue bien, borra la copia en disco. Si
    /// algo falla, la copia se queda: la siguiente corrida lo volverá a intentar.
    /// </summary>
    public void Restore()
    {
        bool allRestored = true;
        foreach (var (path, original) in _originals)
            allRestored &= Restore(path, original);

        if (allRestored && _ownsDiskCopy) DeleteFromDisk(_backupDir);
    }

    /// <summary>
    /// Restaura la copia en disco de una corrida que murió antes de restaurar, si la hay. Devuelve
    /// <c>false</c> solo si había una y no se pudo restaurar entera: entonces se conserva.
    /// </summary>
    private static bool RestorePending(string appDir, string backupDir)
    {
        if (!File.Exists(Path.Combine(backupDir, CompleteMarker)))
        {
            // Sin marcador no hay nada fiable que restaurar: o no hay copia, o es de una captura a medias,
            // cuando los archivos de la app aún no se habían tocado.
            DeleteFromDisk(backupDir);
            return true;
        }

        bool allRestored = true;
        foreach (string name in Files)
        {
            string copy = Path.Combine(backupDir, name);
            byte[]? original = File.Exists(copy) ? File.ReadAllBytes(copy) : null;
            if (original is null && !File.Exists(copy + AbsentSuffix)) continue;   // no se respaldó: no tocar
            allRestored &= Restore(Path.Combine(appDir, name), original);
        }

        if (allRestored) DeleteFromDisk(backupDir);
        return allRestored;
    }

    private static void WriteToDisk(Dictionary<string, byte[]?> originals, string backupDir)
    {
        try
        {
            DeleteFromDisk(backupDir);
            Directory.CreateDirectory(backupDir);
            foreach (var (path, original) in originals)
            {
                string copy = Path.Combine(backupDir, Path.GetFileName(path));
                if (original is null) File.WriteAllBytes(copy + AbsentSuffix, []);
                else                  File.WriteAllBytes(copy, original);
            }
            File.WriteAllBytes(Path.Combine(backupDir, CompleteMarker), []);
        }
        catch { /* mejor esfuerzo: sin copia en disco queda la de memoria, como antes de AUD-040 */ }
    }

    private static void DeleteFromDisk(string backupDir)
    {
        try { if (Directory.Exists(backupDir)) Directory.Delete(backupDir, recursive: true); }
        catch { /* mejor esfuerzo: la siguiente captura la sobrescribe */ }
    }

    private static bool Restore(string path, byte[]? original)
    {
        try
        {
            if (original is null)
            {
                if (File.Exists(path)) File.Delete(path);
            }
            else
            {
                Directory.CreateDirectory(Path.GetDirectoryName(path)!);
                File.WriteAllBytes(path, original);
            }
            return true;
        }
        catch { return false; /* mejor esfuerzo: no tapar el resultado real de las pruebas por esto */ }
    }
}
