namespace FormatDiskPro;

/// <summary>
/// Verifica la capacidad real de una unidad escribiendo un patrón determinista en el
/// espacio libre y releyéndolo. Detecta unidades falsificadas (que mienten sobre su tamaño).
/// </summary>
public static class CapacityVerifier
{
    private const int BlockSize    = 8 * 1024 * 1024;  // 8 MB por bloque
    private const long SafetyMargin = 64L * 1024 * 1024; // dejar 64 MB libres

    public sealed record VerifyResult(bool Ok, long WrittenBytes, string FailureDetail);

    public enum Phase { Writing, Reading }

    public static async Task<VerifyResult> RunAsync(
        char letter,
        IProgress<(Phase phase, int percent, long bytes)> progress,
        CancellationToken ct)
    {
        string dir = $"{letter}:\\__fdp_verify__";
        var files = new List<(string Path, int Index, int Length)>();
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
            int index = 0;
            while (totalWritten < target)
            {
                ct.ThrowIfCancellationRequested();
                int size = (int)Math.Min(BlockSize, target - totalWritten);
                FillPattern(buffer, index, size);

                string path = Path.Combine(dir, $"blk_{index:D6}.bin");
                await using (var fs = new FileStream(path, FileMode.Create, FileAccess.Write,
                                 FileShare.None, 1 << 20, FileOptions.WriteThrough))
                {
                    await fs.WriteAsync(buffer.AsMemory(0, size), ct);
                }

                files.Add((path, index, size));
                totalWritten += size;
                index++;
                int pct = target > 0 ? (int)(totalWritten * 100 / target) : 100;
                progress.Report((Phase.Writing, pct, totalWritten));
            }

            // ── Fase de lectura/verificación ──────────────────────
            var readBuf  = new byte[BlockSize];
            var expected = new byte[BlockSize];
            long verified = 0;
            foreach (var f in files)
            {
                ct.ThrowIfCancellationRequested();
                FillPattern(expected, f.Index, f.Length);

                await using (var fs = new FileStream(f.Path, FileMode.Open, FileAccess.Read,
                                 FileShare.None, 1 << 20, FileOptions.SequentialScan))
                {
                    int total = 0, read;
                    while (total < f.Length &&
                           (read = await fs.ReadAsync(readBuf.AsMemory(total, f.Length - total), ct)) > 0)
                        total += read;

                    if (total != f.Length)
                        return new VerifyResult(false, verified, $"short-read@{f.Index}");

                    if (!readBuf.AsSpan(0, f.Length).SequenceEqual(expected.AsSpan(0, f.Length)))
                        return new VerifyResult(false, verified, $"mismatch@{f.Index}");
                }

                verified += f.Length;
                int pct = totalWritten > 0 ? (int)(verified * 100 / totalWritten) : 100;
                progress.Report((Phase.Reading, pct, verified));
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
