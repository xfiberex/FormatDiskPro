namespace FormatDiskPro.UiTests;

/// <summary>
/// Copia y restaura los archivos de <c>%AppData%\FormatDiskPro\</c> que la app escribe
/// (<c>settings.json</c>, <c>history.log</c> y su generación rotada <c>history.1.log</c>) — la app es
/// unpackaged y no tiene almacenamiento aislado, así que sin esto las pruebas dejarían idioma/tema/
/// última unidad e historial de operaciones de prueba mezclados con el uso real del usuario.
/// </summary>
public sealed class SettingsBackup
{
    private readonly Dictionary<string, byte[]?> _originals;

    private SettingsBackup(Dictionary<string, byte[]?> originals) => _originals = originals;

    public static SettingsBackup Capture()
    {
        var dir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "FormatDiskPro");

        // history.1.log incluida aunque haga falta rotar 2 MB para que exista: si algún día aparece, se
        // restaura como el resto en vez de quedarse en el %AppData% del usuario.
        string[] files = ["settings.json", "history.log", "history.1.log"];

        var originals = new Dictionary<string, byte[]?>();
        foreach (string name in files)
        {
            string path = Path.Combine(dir, name);
            originals[path] = File.Exists(path) ? File.ReadAllBytes(path) : null;
        }
        return new SettingsBackup(originals);
    }

    public void Restore()
    {
        foreach (var (path, original) in _originals)
            Restore(path, original);
    }

    private static void Restore(string path, byte[]? original)
    {
        try
        {
            if (original is null)
            {
                if (File.Exists(path)) File.Delete(path);
            }
            else
            {
                File.WriteAllBytes(path, original);
            }
        }
        catch { /* mejor esfuerzo: no tapar el resultado real de las pruebas por esto */ }
    }
}
