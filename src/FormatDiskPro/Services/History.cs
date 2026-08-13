using System.Diagnostics;

namespace FormatDiskPro;

/// <summary>
/// Registro de auditoría de operaciones en %AppData%\FormatDiskPro\history.log
/// </summary>
public static class History
{
    public static string FilePath
    {
        get
        {
            string dir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "FormatDiskPro");
            return Path.Combine(dir, "history.log");
        }
    }

    /// <summary>
    /// Añade una entrada al historial. El mensaje se aplana con
    /// <see cref="HistoryEntry.SanitizeDetail"/>: este archivo es de una entrada por línea, y los caminos
    /// de error escriben texto que no controlamos (mensajes de excepción, trazas de pila completas).
    /// Defensivo: nunca lanza — el registro no puede romper la operación que está registrando.
    /// </summary>
    public static void Log(string line)
    {
        try
        {
            string path = FilePath;
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            string safe = HistoryEntry.SanitizeDetail(line);
            File.AppendAllText(path, $"{DateTime.Now:yyyy-MM-dd HH:mm:ss}\t{safe}{Environment.NewLine}");
        }
        catch { /* el log nunca debe romper la operación */ }
    }

    /// <summary>Lee las líneas del historial (vacío si no existe). Defensivo: nunca lanza.</summary>
    public static IReadOnlyList<string> ReadLines()
    {
        try
        {
            string path = FilePath;
            return File.Exists(path) ? File.ReadAllLines(path) : [];
        }
        catch { return []; }
    }

    /// <summary>Vacía el historial dejándolo con la cabecera. Defensivo: nunca lanza.</summary>
    public static void Clear()
    {
        try
        {
            string path = FilePath;
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            File.WriteAllText(path, $"# FormatDiskPro — historial de operaciones{Environment.NewLine}");
        }
        catch { /* ignorar */ }
    }

    /// <summary>Abre el archivo de historial en el editor predeterminado (lo crea si no existe).</summary>
    public static void Open()
    {
        try
        {
            string path = FilePath;
            if (!File.Exists(path))
            {
                Directory.CreateDirectory(Path.GetDirectoryName(path)!);
                File.WriteAllText(path, $"# FormatDiskPro — historial de operaciones{Environment.NewLine}");
            }
            Process.Start(new ProcessStartInfo(path) { UseShellExecute = true });
        }
        catch { /* ignorar */ }
    }
}
