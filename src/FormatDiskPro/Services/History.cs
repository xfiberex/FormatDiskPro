using System.Diagnostics;

namespace FormatDiskPro;

/// <summary>
/// Registro de auditoría de operaciones en %AppData%\FormatDiskPro\history.log
///
/// <para>El archivo <b>rota</b> al superar <see cref="HistoryRotation.MaxBytes"/> (ver
/// <see cref="HistoryRotation"/>): la generación anterior queda en <c>history.1.log</c> y el visor lee las
/// dos, así que rotar no vacía lo que el usuario ve.</para>
/// </summary>
public static class History
{
    private static readonly string Header = $"# FormatDiskPro — historial de operaciones{Environment.NewLine}";

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
    public static void Log(string line) => LogTo(FilePath, line);

    /// <summary>Lee las líneas del historial (vacío si no existe). Defensivo: nunca lanza.</summary>
    public static IReadOnlyList<string> ReadLines() => ReadLinesFrom(FilePath);

    /// <summary>Vacía el historial dejándolo con la cabecera. Defensivo: nunca lanza.</summary>
    public static void Clear() => ClearAt(FilePath);

    /// <summary>Abre el archivo de historial en el editor predeterminado (lo crea si no existe).</summary>
    public static void Open()
    {
        try
        {
            string path = FilePath;
            if (!File.Exists(path))
            {
                Directory.CreateDirectory(Path.GetDirectoryName(path)!);
                File.WriteAllText(path, Header);
            }
            Process.Start(new ProcessStartInfo(path) { UseShellExecute = true });
        }
        catch { /* ignorar */ }
    }

    // ── Costuras internas: las mismas operaciones sobre una ruta dada ──────────────────────────
    // La app siempre usa FilePath (%AppData%), pero las pruebas necesitan un historial propio: escribir en
    // el real las haría depender del equipo donde corren y, peor, destruiría el del usuario al probar la
    // rotación o el borrado.

    /// <inheritdoc cref="Log"/>
    internal static void LogTo(string path, string line)
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            RotateIfNeeded(path);
            string safe = HistoryEntry.SanitizeDetail(line);
            File.AppendAllText(path, $"{DateTime.Now:yyyy-MM-dd HH:mm:ss}\t{safe}{Environment.NewLine}");
        }
        catch { /* el log nunca debe romper la operación */ }
    }

    /// <summary>
    /// Lee el historial <b>y la generación rotada</b>, la vieja primero, para que el visor siga mostrando
    /// contexto justo después de una rotación en vez de quedarse casi vacío. Defensivo: nunca lanza.
    /// </summary>
    internal static IReadOnlyList<string> ReadLinesFrom(string path)
    {
        try
        {
            var lines = new List<string>();
            string previous = HistoryRotation.PreviousPath(path);
            if (File.Exists(previous)) lines.AddRange(File.ReadAllLines(previous));
            if (File.Exists(path))     lines.AddRange(File.ReadAllLines(path));
            return lines;
        }
        catch { return []; }
    }

    /// <summary>
    /// Vacía el historial. Borra <b>también</b> la generación rotada: si no, «borrar el historial» dejaría
    /// a la vista los 2 MB anteriores, que es justo lo contrario de lo que el usuario pidió.
    /// </summary>
    internal static void ClearAt(string path)
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            string previous = HistoryRotation.PreviousPath(path);
            if (File.Exists(previous)) File.Delete(previous);
            File.WriteAllText(path, Header);
        }
        catch { /* ignorar */ }
    }

    /// <summary>
    /// Rota <b>antes</b> de escribir, no después: así el archivo activo nunca pasa del umbral, en vez de
    /// quedarse por encima hasta la siguiente entrada. El <c>history.1.log</c> anterior se sobrescribe —dos
    /// generaciones es el tope, ver <see cref="HistoryRotation"/>—.
    /// </summary>
    private static void RotateIfNeeded(string path)
    {
        var info = new FileInfo(path);
        if (!info.Exists || !HistoryRotation.ShouldRotate(info.Length)) return;

        File.Move(path, HistoryRotation.PreviousPath(path), overwrite: true);
        File.WriteAllText(path, Header);
    }
}
