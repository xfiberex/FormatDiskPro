namespace FormatDiskPro;

/// <summary>
/// Verifica la capacidad real de una unidad escribiendo un patrón determinista en el
/// espacio libre y releyéndolo. Detecta unidades falsificadas (que mienten sobre su tamaño).
/// </summary>
public static class CapacityVerifier
{
    private const int  BlockSize    = 8 * 1024 * 1024;          // 8 MB: unidad del patrón anti-aliasing
    private const long MaxFileSize  = 1L * 1024 * 1024 * 1024;  // 1 GB por archivo (seguro incluso en FAT32)
    private const long SafetyMargin = 64L * 1024 * 1024;        // dejar 64 MB libres

    public sealed record VerifyResult(bool Ok, long WrittenBytes, string FailureDetail);

    public enum Phase { Writing, Reading }

    public static async Task<VerifyResult> RunAsync(
        char letter,
        IProgress<(Phase phase, int percent, long bytes)> progress,
        CancellationToken ct)
    {
        string dir = $"{letter}:\\__fdp_verify__";
        // Cada archivo agrupa varios bloques de 8 MB; guardamos el índice de bloque global inicial
        // para regenerar el patrón exacto durante la verificación (la detección de aliasing se preserva).
        var files = new List<(string Path, int StartBlock, long Length)>();
        long totalWritten = 0;

        try
        {
            var drive = new DriveInfo(letter.ToString());
            if (!drive.IsReady)
                return new VerifyResult(false, 0, "unit-not-ready");

            long target = Math.Max(0, drive.AvailableFreeSpace - SafetyMargin);
            Directory.CreateDirectory(dir);

            // ── Fase de escritura ─────────────────────────────────
            var buffer = new byte[BlockSize];
            int blockIndex = 0;
            int fileNo = 0;
            while (totalWritten < target)
            {
                long fileTarget = Math.Min(MaxFileSize, target - totalWritten);
                string path = Path.Combine(dir, $"vol_{fileNo:D4}.bin");
                int startBlock = blockIndex;
                long fileWritten = 0;

                await using (var fs = new FileStream(path, FileMode.Create, FileAccess.Write,
                                 FileShare.None, 1 << 20, FileOptions.WriteThrough))
                {
                    while (fileWritten < fileTarget)
                    {
                        ct.ThrowIfCancellationRequested();
                        int size = (int)Math.Min(BlockSize, fileTarget - fileWritten);
                        FillPattern(buffer, blockIndex, size);
                        await fs.WriteAsync(buffer.AsMemory(0, size), ct);

                        fileWritten  += size;
                        totalWritten += size;
                        blockIndex++;
                        int pct = target > 0 ? (int)(totalWritten * 100 / target) : 100;
                        progress.Report((Phase.Writing, pct, totalWritten));
                    }
                }

                files.Add((path, startBlock, fileWritten));
                fileNo++;
            }

            // ── Fase de lectura/verificación ──────────────────────
            var readBuf  = new byte[BlockSize];
            var expected = new byte[BlockSize];
            long verified = 0;
            foreach (var f in files)
            {
                ct.ThrowIfCancellationRequested();

                await using var fs = new FileStream(f.Path, FileMode.Open, FileAccess.Read,
                                 FileShare.None, 1 << 20, FileOptions.SequentialScan);
                long fileRead = 0;
                int blk = f.StartBlock;
                while (fileRead < f.Length)
                {
                    ct.ThrowIfCancellationRequested();
                    int size = (int)Math.Min(BlockSize, f.Length - fileRead);
                    FillPattern(expected, blk, size);

                    int total = 0, read;
                    while (total < size &&
                           (read = await fs.ReadAsync(readBuf.AsMemory(total, size - total), ct)) > 0)
                        total += read;

                    if (total != size)
                        return new VerifyResult(false, verified, $"short-read@{blk}");

                    if (!readBuf.AsSpan(0, size).SequenceEqual(expected.AsSpan(0, size)))
                        return new VerifyResult(false, verified, $"mismatch@{blk}");

                    verified += size;
                    fileRead += size;
                    blk++;
                    int pct = totalWritten > 0 ? (int)(verified * 100 / totalWritten) : 100;
                    progress.Report((Phase.Reading, pct, verified));
                }
            }

            return new VerifyResult(true, totalWritten, "");
        }
        catch (OperationCanceledException)
        {
            return new VerifyResult(false, totalWritten, "cancelled");
        }
        finally
        {
            try { if (Directory.Exists(dir)) Directory.Delete(dir, recursive: true); } catch { }
        }
    }

    /// <summary>
    /// Patrón dependiente del índice del bloque: si una unidad falsa reescribe direcciones
    /// (aliasing), releerá el patrón de OTRO bloque y la comparación fallará.
    /// </summary>
    private static void FillPattern(byte[] buffer, int blockIndex, int length)
    {
        unchecked
        {
            ulong seed = (ulong)blockIndex * 0x9E3779B97F4A7C15UL + 0xD1B54A32D192ED03UL;
            for (int i = 0; i < length; i += 8)
            {
                ulong v = seed + (ulong)i;
                v ^= v >> 30; v *= 0xBF58476D1CE4E5B9UL;
                int n = Math.Min(8, length - i);
                for (int b = 0; b < n; b++)
                    buffer[i + b] = (byte)(v >> (b * 8));
            }
        }
    }
}
