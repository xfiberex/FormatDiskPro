using System.Diagnostics;

namespace FormatDiskPro;

/// <summary>Fase del benchmark en curso.</summary>
public enum BenchPhase { Writing, Reading }

/// <summary>
/// Benchmark rápido de lectura/escritura secuencial: escribe un archivo temporal en el espacio libre
/// y lo relee, midiendo MB/s. <b>No destructivo</b> — el archivo se elimina al terminar. Reutiliza la
/// mecánica de E/S por bloques de <see cref="SecureWipe"/>/<see cref="CapacityVerifier"/>.
/// </summary>
/// <remarks>
/// La caché del sistema operativo puede inflar la velocidad de lectura; las cifras son orientativas.
/// La escritura usa <see cref="FileOptions.WriteThrough"/> para reflejar mejor la velocidad real del medio.
/// </remarks>
public static class BenchmarkRunner
{
    private const int BlockSize = 8 * 1024 * 1024;   // 8 MB por bloque (igual que SecureWipe/CapacityVerifier)

    /// <summary>
    /// Ejecuta el benchmark sobre la unidad <paramref name="letter"/>. Devuelve <c>null</c> si la unidad
    /// no está lista o no hay espacio suficiente para la prueba.
    /// </summary>
    /// <param name="letter">Letra de la unidad.</param>
    /// <param name="progress">Progreso: fase (escritura/lectura) y porcentaje 0-100.</param>
    /// <param name="ct">Token de cancelación.</param>
    /// <returns>Velocidades de escritura/lectura y tamaño probado, o <c>null</c> si no se pudo medir.</returns>
    /// <exception cref="OperationCanceledException">Si se cancela mediante <paramref name="ct"/>.</exception>
    public static async Task<BenchmarkResult?> RunAsync(
        char letter, IProgress<(BenchPhase phase, int percent)> progress, CancellationToken ct)
    {
        if (!char.IsLetter(letter)) return null;

        string dir  = $"{letter}:\\__fdp_bench__";
        string path = Path.Combine(dir, "bench.bin");

        try
        {
            var drive = new DriveInfo(letter.ToString());
            if (!drive.IsReady) return null;

            long testBytes = Benchmark.PlanTestBytes(drive.AvailableFreeSpace);
            if (testBytes <= 0) return null;

            Directory.CreateDirectory(dir);
            var buffer = new byte[BlockSize];
            new Random().NextBytes(buffer);   // datos no triviales para no favorecer compresión/dedup

            // ── Escritura ─────────────────────────────────────────
            var sw = Stopwatch.StartNew();
            long written = 0;
            await using (var fs = new FileStream(path, FileMode.Create, FileAccess.Write,
                             FileShare.None, 1 << 20, FileOptions.WriteThrough))
            {
                while (written < testBytes)
                {
                    ct.ThrowIfCancellationRequested();
                    int size = (int)Math.Min(BlockSize, testBytes - written);
                    await fs.WriteAsync(buffer.AsMemory(0, size), ct);
                    written += size;
                    progress.Report((BenchPhase.Writing, (int)(written * 100 / testBytes)));
                }
            }
            sw.Stop();
            double writeSpeed = Benchmark.BytesPerSec(written, sw.Elapsed);

            // ── Lectura ───────────────────────────────────────────
            sw.Restart();
            long readTotal = 0;
            await using (var fs = new FileStream(path, FileMode.Open, FileAccess.Read,
                             FileShare.None, 1 << 20, FileOptions.SequentialScan))
            {
                int read;
                while ((read = await fs.ReadAsync(buffer.AsMemory(0, BlockSize), ct)) > 0)
                {
                    readTotal += read;
                    progress.Report((BenchPhase.Reading, (int)Math.Min(100, readTotal * 100 / testBytes)));
                }
            }
            sw.Stop();
            double readSpeed = Benchmark.BytesPerSec(readTotal, sw.Elapsed);

            return new BenchmarkResult(writeSpeed, readSpeed, testBytes);
        }
        finally
        {
            try { if (Directory.Exists(dir)) Directory.Delete(dir, recursive: true); } catch { }
        }
    }
}
