namespace FormatDiskPro;

/// <summary>
/// Cómo se registra el fallo de una operación en el historial. Vivía dentro de
/// <c>MainWindow.ReportOperationErrorAsync</c>, donde no había forma de probarlo: los <c>catch</c> que lo
/// invocan (`T0-02`) solo se ejercitan cuando algo se rompe de verdad en mitad de una operación sobre
/// hardware. Aquí es lógica pura y se puede comprobar que lo escrito **se vuelve a leer bien**.
/// </summary>
public static class OperationFailure
{
    /// <summary>
    /// Línea de historial para una operación que ha fallado, en el formato que
    /// <see cref="HistoryEntry.Parse"/> sabe clasificar: <c>OPERACIÓN ERROR L: mensaje</c>.
    /// </summary>
    /// <param name="operation">Palabra clave de la operación en mayúsculas (<c>VERIFY</c>, <c>CHECK</c>…).</param>
    /// <param name="letter">Letra de la unidad; se normaliza con <see cref="DriveLetter"/>.</param>
    /// <param name="ex">La excepción que abortó la operación.</param>
    public static string LogLine(string operation, char letter, Exception ex) =>
        HistoryEntry.SanitizeDetail($"{operation} ERROR {DriveLetter.Normalize(letter)}: {ex.Message}");
}
