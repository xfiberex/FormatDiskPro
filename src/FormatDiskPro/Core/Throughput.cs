namespace FormatDiskPro;

/// <summary>
/// Utilidades de rendimiento para operaciones largas: estimación de tiempo restante (ETA)
/// y formateo de velocidad. Lógica pura y testeable (sin estado ni efectos colaterales).
/// </summary>
public static class Throughput
{
    /// <summary>
    /// Estima el tiempo restante a partir de los bytes pendientes y la velocidad actual.
    /// </summary>
    /// <param name="remainingBytes">Bytes que faltan por procesar.</param>
    /// <param name="bytesPerSec">Velocidad actual en bytes por segundo.</param>
    /// <returns>El tiempo restante estimado, o <c>null</c> si no puede calcularse.</returns>
    public static TimeSpan? Eta(long remainingBytes, double bytesPerSec)
    {
        if (bytesPerSec <= 0 || remainingBytes < 0) return null;
        return TimeSpan.FromSeconds(remainingBytes / bytesPerSec);
    }

    /// <summary>
    /// Formatea una velocidad en bytes por segundo de forma legible, p. ej. <c>"45.2 MB/s"</c>.
    /// Devuelve cadena vacía si la velocidad no es positiva.
    /// </summary>
    public static string FormatSpeed(double bytesPerSec)
        => bytesPerSec <= 0 ? "" : $"{FormatLogic.FormatBytes((long)bytesPerSec)}/s";

    /// <summary>
    /// Formatea un ETA como <c>"mm:ss"</c> (o <c>"h:mm:ss"</c> si supera la hora).
    /// Devuelve cadena vacía si el ETA es desconocido.
    /// </summary>
    public static string FormatEta(TimeSpan? eta)
    {
        if (eta is not TimeSpan t) return "";
        return t.TotalHours >= 1
            ? $"{(int)t.TotalHours}:{t.Minutes:00}:{t.Seconds:00}"
            : $"{t.Minutes:00}:{t.Seconds:00}";
    }
}
