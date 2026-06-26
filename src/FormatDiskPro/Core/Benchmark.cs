namespace FormatDiskPro;

/// <summary>Velocidades de lectura y escritura de una prueba, en bytes por segundo.</summary>
/// <param name="ReadBytesPerSec">Velocidad de lectura (bytes/segundo).</param>
/// <param name="WriteBytesPerSec">Velocidad de escritura (bytes/segundo).</param>
public sealed record BenchmarkScore(double ReadBytesPerSec, double WriteBytesPerSec);

/// <summary>
/// Resultado de un benchmark: velocidades <b>secuenciales</b> (bloque grande, cola profunda) y de
/// <b>4 KiB aleatorio</b> (cola 1), más el tamaño del archivo de prueba en bytes.
/// </summary>
/// <param name="Sequential">Velocidades secuenciales (Q8, bloque de 1 MiB).</param>
/// <param name="Random4K">Velocidades de 4 KiB aleatorio (Q1).</param>
/// <param name="TestBytes">Tamaño del archivo temporal usado para la prueba.</param>
public sealed record BenchmarkResult(BenchmarkScore Sequential, BenchmarkScore Random4K, long TestBytes);

/// <summary>
/// Lógica pura y testeable del benchmark: planificación del tamaño de prueba, cálculo de velocidad,
/// mediana de pasadas y selección de desplazamientos aleatorios alineados. La E/S real (sin caché, con
/// cola profunda y patrón aleatorio) vive en <see cref="BenchmarkRunner"/>.
/// </summary>
public static class Benchmark
{
    /// <summary>Tamaño objetivo del archivo de prueba: 512 MiB (equilibra unidades rápidas y lentas y da recorrido al patrón aleatorio).</summary>
    public const long TargetTestBytes = 512L * 1024 * 1024;

    /// <summary>Margen de seguridad: deja este espacio libre para no llenar la unidad (igual que el borrado seguro).</summary>
    public const long SafetyMarginBytes = 64L * 1024 * 1024;

    /// <summary>Pasadas (ventanas cronometradas) por fase; el resultado es la <b>mediana</b>, que descarta el arranque en frío y los picos transitorios.</summary>
    public const int Passes = 3;

    /// <summary>Tamaño de bloque del perfil <b>4 KiB aleatorio</b> (4096 B), base del cálculo de IOPS.</summary>
    public const int Random4KBlockBytes = 4096;

    /// <summary>
    /// Tamaño del archivo de prueba: el objetivo (512 MiB) acotado por el espacio libre menos el margen
    /// de seguridad y <b>truncado a un múltiplo de <paramref name="blockSize"/></b> (la E/S sin caché exige
    /// tamaños y desplazamientos alineados al sector). Devuelve <c>0</c> si no cabe ni un bloque completo.
    /// Lógica pura.
    /// </summary>
    /// <param name="freeBytes">Espacio libre de la unidad en bytes.</param>
    /// <param name="blockSize">Tamaño de bloque de E/S, múltiplo del tamaño de sector.</param>
    public static long PlanTestBytes(long freeBytes, long blockSize)
    {
        if (blockSize <= 0) return 0;
        long usable = Math.Min(TargetTestBytes, freeBytes - SafetyMarginBytes);
        return usable < blockSize ? 0 : usable - (usable % blockSize);
    }

    /// <summary>
    /// Calcula la velocidad en bytes por segundo. Devuelve <c>0</c> si el tiempo no es positivo
    /// (evita divisiones por cero). Lógica pura.
    /// </summary>
    /// <param name="bytes">Bytes transferidos.</param>
    /// <param name="elapsed">Tiempo empleado.</param>
    public static double BytesPerSec(long bytes, TimeSpan elapsed)
        => elapsed.TotalSeconds <= 0 ? 0 : bytes / elapsed.TotalSeconds;

    /// <summary>
    /// Operaciones de E/S por segundo (IOPS) de un flujo de bloques de <paramref name="blockBytes"/> bytes:
    /// <c>bytes/s ÷ tamaño de bloque</c>. Equivalente a la cifra de IOPS que muestra CrystalDiskMark junto a los
    /// MB/s del 4 KiB aleatorio. Devuelve <c>0</c> si el tamaño de bloque no es positivo. Lógica pura.
    /// </summary>
    /// <param name="bytesPerSec">Velocidad en bytes por segundo.</param>
    /// <param name="blockBytes">Tamaño de cada operación en bytes (p. ej. 4096 para 4 KiB).</param>
    public static double Iops(double bytesPerSec, int blockBytes)
        => blockBytes <= 0 ? 0 : bytesPerSec / blockBytes;

    /// <summary>
    /// Mediana de un conjunto de velocidades; <c>0</c> si está vacío. Para un número par de valores
    /// devuelve la media de los dos centrales. No modifica la entrada. Lógica pura.
    /// </summary>
    /// <param name="values">Velocidades de cada pasada.</param>
    public static double Median(ReadOnlySpan<double> values)
    {
        if (values.IsEmpty) return 0;
        Span<double> sorted = values.Length <= 16 ? stackalloc double[values.Length] : new double[values.Length];
        values.CopyTo(sorted);
        sorted.Sort();
        int mid = sorted.Length / 2;
        return (sorted.Length & 1) == 1 ? sorted[mid] : (sorted[mid - 1] + sorted[mid]) / 2;
    }

    /// <summary>
    /// Devuelve un desplazamiento aleatorio <b>alineado a <paramref name="blockSize"/></b> dentro de
    /// <c>[0, lengthBytes − blockSize]</c>, apto para una E/S sin caché de un bloque. Devuelve <c>0</c>
    /// si no cabe ni un bloque. Lógica pura (determinista para un <paramref name="rng"/> sembrado).
    /// </summary>
    /// <param name="lengthBytes">Tamaño total del archivo en bytes.</param>
    /// <param name="blockSize">Tamaño de bloque (alineación), múltiplo del tamaño de sector.</param>
    /// <param name="rng">Generador aleatorio.</param>
    public static long RandomAlignedOffset(long lengthBytes, int blockSize, Random rng)
    {
        if (blockSize <= 0 || lengthBytes < blockSize) return 0;
        long blocks = lengthBytes / blockSize;
        return rng.NextInt64(blocks) * blockSize;
    }
}
