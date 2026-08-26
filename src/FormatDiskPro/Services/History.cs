using System.Diagnostics;
using System.Globalization;

namespace FormatDiskPro;

/// <summary>Registro de auditoría de operaciones.</summary>
public interface IHistory
{
    /// <inheritdoc cref="History.Log"/>
    void Log(string line);

    /// <inheritdoc cref="History.ReadLines"/>
    IReadOnlyList<string> ReadLines();

    /// <inheritdoc cref="History.Clear"/>
    void Clear();

    /// <inheritdoc cref="History.Open"/>
    void Open();
}

/// <summary>
/// Registro de auditoría de operaciones en %AppData%\FormatDiskPro\history.log
///
/// <para>El archivo <b>rota</b> al superar <see cref="HistoryRotation.MaxBytes"/> (ver
/// <see cref="HistoryRotation"/>): la generación anterior queda en <c>history.1.log</c> y el visor lee las
/// dos, así que rotar no vacía lo que el usuario ve.</para>
/// </summary>
/// <param name="path">
/// Archivo de historial. La app usa siempre <see cref="History.DefaultPath"/> (%AppData%); las pruebas
/// pasan el suyo, porque escribir en el real las haría depender del equipo donde corren y —peor—
/// destruiría el del usuario al probar la rotación o el borrado.
/// </param>
public sealed class History(string? path = null) : IHistory
{
    private static readonly string Header = $"# FormatDiskPro — historial de operaciones{Environment.NewLine}";

    /// <summary>Ruta del historial de la app: <c>%AppData%\FormatDiskPro\history.log</c>.</summary>
    public static string DefaultPath
    {
        get
        {
            string dir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "FormatDiskPro");
            return Path.Combine(dir, "history.log");
        }
    }

    /// <summary>Archivo sobre el que opera esta instancia.</summary>
    public string FilePath { get; } = path ?? DefaultPath;

    /// <summary>
    /// Añade una entrada al historial. El mensaje se aplana con
    /// <see cref="HistoryEntry.SanitizeDetail"/>: este archivo es de una entrada por línea, y los caminos
    /// de error escriben texto que no controlamos (mensajes de excepción, trazas de pila completas).
    /// Defensivo: nunca lanza — el registro no puede romper la operación que está registrando.
    /// </summary>
    public void Log(string line) => LogTo(FilePath, line);

    /// <summary>Lee las líneas del historial (vacío si no existe). Defensivo: nunca lanza.</summary>
    public IReadOnlyList<string> ReadLines() => ReadLinesFrom(FilePath);

    /// <summary>Vacía el historial dejándolo con la cabecera. Defensivo: nunca lanza.</summary>
    public void Clear() => ClearAt(FilePath);

    /// <summary>
    /// Abre el archivo de historial en el editor predeterminado (lo crea si no existe).
    ///
    /// <para><b>Deja salir la excepción a propósito.</b> Antes la atrapaba y no hacía nada, así que
    /// pulsar <i>Abrir archivo</i> sin un editor asociado a <c>.log</c> —o con el archivo bloqueado— no
    /// producía absolutamente ningún efecto: ni ventana, ni aviso, ni rastro. Es el mismo fallo silencioso
    /// que ya se corrigió en la exportación del CSV, y quien puede contarlo es la UI, que tiene dónde
    /// escribirlo; un servicio no.</para>
    /// </summary>
    /// <exception cref="Exception">Lo que falle al crear el archivo o al pedirle al shell que lo abra.</exception>
    public void Open()
    {
        string path = FilePath;
        if (!File.Exists(path))
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            File.WriteAllText(path, Header);
        }
        Process.Start(new ProcessStartInfo(path) { UseShellExecute = true });
    }

    // ── Implementación sobre una ruta dada ────────────────────────────────────────────────────
    // Antes eran costuras `internal static` con la ruta como parámetro, la única forma de probar esto
    // sin escribir en el %AppData% real. Con la ruta inyectada por constructor (`T4-02`) esa costura
    // sobra: las pruebas construyen su propia instancia y estos métodos vuelven a ser detalle interno.

    /// <inheritdoc cref="Log"/>
    private static void LogTo(string path, string line)
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            RotateIfNeeded(path);
            string safe = HistoryEntry.SanitizeDetail(line);
            // InvariantCulture explícita, y la MISMA constante con la que HistoryEntry.Parse lo lee
            // (`T9-07`). Sin proveedor, el formato usa el calendario de la cultura del hilo: en un
            // Windows tailandés esta línea se escribía con el año budista (2569 en vez de 2026), y Parse
            // la aceptaba como gregoriana. El historial es el registro de auditoría de operaciones
            // destructivas: su fecha no puede depender del idioma del sistema.
            string stamp = DateTime.Now.ToString(HistoryEntry.TimeFormat, CultureInfo.InvariantCulture);
            File.AppendAllText(path, $"{stamp}\t{safe}{Environment.NewLine}");
        }
        catch { /* el log nunca debe romper la operación */ }
    }

    /// <summary>
    /// Lee el historial <b>y la generación rotada</b>, la vieja primero, para que el visor siga mostrando
    /// contexto justo después de una rotación en vez de quedarse casi vacío. Defensivo: nunca lanza.
    /// </summary>
    private static IReadOnlyList<string> ReadLinesFrom(string path)
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
    private static void ClearAt(string path)
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
