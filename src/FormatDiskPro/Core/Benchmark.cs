namespace FormatDiskPro;

/// <summary>Resultado de un benchmark de lectura/escritura: velocidades en bytes/segundo y tamaño probado.</summary>
public sealed record BenchmarkResult(double WriteBytesPerSec, double ReadBytesPerSec, long TestBytes);

/// <summary>
/// Lógica pura y testeable del benchmark rápido: planificación del tamaño de prueba y cálculo de
/// velocidad. La E/S real (escribir y releer un archivo temporal) vive en <see cref="BenchmarkRunner"/>.
/// </summary>
public static class Benchmark
{
    /// <summary>Tamaño objetivo de la prueba: 256 MB (suficiente para una medición rápida y representativa).</summary>
    public const long TargetTestBytes = 256L * 1024 * 1024;

    /// <summary>Margen de seguridad: deja este espacio libre para no llenar la unidad (igual que el borrado seguro).</summary>
    public const long SafetyMarginBytes = 64L * 1024 * 1024;

    /// <summary>
    /// Bytes a escribir en la prueba: el objetivo (256 MB) acotado por el espacio libre menos el
    /// margen de seguridad. Devuelve <c>0</c> si no hay espacio suficiente. Lógica pura.
    /// </summary>
    /// <param name="freeBytes">Espacio libre de la unidad en bytes.</param>
    public static long PlanTestBytes(long freeBytes)
        => Math.Min(TargetTestBytes, Math.Max(0, freeBytes - SafetyMarginBytes));

    /// <summary>
    /// Calcula la velocidad en bytes por segundo. Devuelve <c>0</c> si el tiempo no es positivo
    /// (evita divisiones por cero). Lógica pura.
    /// </summary>
    /// <param name="bytes">Bytes transferidos.</param>
    /// <param name="elapsed">Tiempo empleado.</param>
    public static double BytesPerSec(long bytes, TimeSpan elapsed)
        => elapsed.TotalSeconds <= 0 ? 0 : bytes / elapsed.TotalSeconds;
}
