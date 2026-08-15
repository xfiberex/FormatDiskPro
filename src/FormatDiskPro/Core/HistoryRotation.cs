namespace FormatDiskPro;

/// <summary>
/// Política de rotación de <c>history.log</c>. Lógica pura: decide <b>cuándo</b> rotar y <b>cómo se llama</b>
/// el archivo rotado; mover archivos es cosa de <see cref="History"/>.
///
/// <para><b>Por qué hace falta.</b> El historial solo crecía, y el visor lo interpreta <b>entero</b> en
/// memoria cada vez que se abre. Con una entrada por operación eso tarda años en notarse, pero desde
/// <c>T0-01</c> también se registran las caídas <b>con su traza de pila completa</b>: un solo `CRASH:`
/// ocupa lo que cientos de operaciones normales.</para>
///
/// <para><b>Dos generaciones y se acabó</b> (<c>history.log</c> + <c>history.1.log</c>): el consumo en disco
/// queda acotado a ~4 MB y el visor sigue mostrando lo reciente, porque lee las dos. Al rotar por tercera
/// vez, la generación más vieja se pierde — es un registro de auditoría local, no un archivo permanente, y
/// quien necesite conservarlo tiene la exportación a CSV.</para>
/// </summary>
public static class HistoryRotation
{
    /// <summary>
    /// Tamaño a partir del cual se rota. 2 MB son decenas de miles de entradas normales: se llega antes
    /// acumulando trazas de pila que operando.
    /// </summary>
    public const long MaxBytes = 2 * 1024 * 1024;

    /// <summary>¿Toca rotar un historial de este tamaño?</summary>
    /// <param name="currentBytes">Tamaño actual del archivo, en bytes.</param>
    public static bool ShouldRotate(long currentBytes) => currentBytes >= MaxBytes;

    /// <summary>
    /// Nombre de la generación anterior: <c>history.log</c> → <c>history.1.log</c>. El <c>.log</c> se
    /// conserva al final a propósito, para que siga abriéndose con el mismo programa que el actual.
    /// </summary>
    /// <param name="logPath">Ruta del historial actual.</param>
    public static string PreviousPath(string logPath)
    {
        string dir  = Path.GetDirectoryName(logPath) ?? "";
        string name = Path.GetFileNameWithoutExtension(logPath);
        string ext  = Path.GetExtension(logPath);           // incluye el punto, o "" si no hay extensión
        return Path.Combine(dir, $"{name}.1{ext}");
    }
}
