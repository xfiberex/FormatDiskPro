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

    public static void Log(string line)
    {
        try
        {
            string path = FilePath;
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            File.AppendAllText(path, $"{DateTime.Now:yyyy-MM-dd HH:mm:ss}\t{line}{Environment.NewLine}");
        }
        catch { /* el log nunca debe romper la operación */ }
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
