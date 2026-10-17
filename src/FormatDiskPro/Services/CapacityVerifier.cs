// SPDX-License-Identifier: GPL-3.0-only

namespace FormatDiskPro;

/// <summary>
/// Verifica la capacidad real de una unidad escribiendo un patrón determinista en el
/// espacio libre y releyéndolo. Detecta unidades falsificadas (que mienten sobre su tamaño).
/// </summary>
/// <remarks>
/// <para><b>La relectura NO pasa por la caché de archivos de Windows</b> (<c>FILE_FLAG_NO_BUFFERING</c>,
/// igual que <see cref="BenchmarkRunner"/>). Es el punto entero de la prueba: si el sistema puede servir
/// los bloques desde RAM, se estaría verificando la caché en vez del medio. Con E/S normal, una USB falsa
/// **pequeña** —menor que la RAM libre— podía releerse íntegra desde caché y dar un **falso OK**, que es
/// el peor resultado posible aquí: decirle a alguien que su unidad es auténtica cuando no lo es.</para>
/// <para>La E/S sin caché exige que <b>buffer, desplazamiento y longitud</b> estén alineados al sector; de
/// ahí <see cref="UnbufferedIo.Alignment"/>, el búfer de <see cref="AlignedBuffers"/> y el redondeo del objetivo.</para>
/// </remarks>
public interface ICapacityVerifier
{
    /// <inheritdoc cref="CapacityVerifier.RunAsync"/>
    Task<CapacityVerifier.VerifyResult> RunAsync(
        char letter,
        IProgress<(CapacityVerifier.Phase phase, int percent, long bytes)> progress,
        CancellationToken ct);
}

/// <inheritdoc cref="ICapacityVerifier"/>
public sealed class CapacityVerifier : ICapacityVerifier
{
    private const int  BlockSize   = FreeSpaceFill.BlockSize;    // también la unidad del patrón anti-aliasing
    private const long MaxFileSize = FreeSpaceFill.MaxFileSize;
    private const int  Alignment   = UnbufferedIo.Alignment;

    public sealed record VerifyResult(bool Ok, long WrittenBytes, string FailureDetail);

    public enum Phase { Writing, Reading }

    /// <summary>
    /// Verifica la capacidad real de la unidad <paramref name="letter"/>: escribe el patrón en su espacio
    /// libre (menos el margen de seguridad) y lo relee sin pasar por la caché del sistema.
    /// </summary>
    public async Task<VerifyResult> RunAsync(
        char letter,
        IProgress<(Phase phase, int percent, long bytes)> progress,
        CancellationToken ct)
    {
        // Fuera del try de RunInAsync a propósito: si esto lanza (letra inválida, unidad retirada), el
        // directorio de trabajo aún no existe, así que no hay nada que limpiar. Comportamiento idéntico
        // al de antes de extraer la costura.
        var drive = new DriveInfo(letter.ToString());
        if (!drive.IsReady)
            return new VerifyResult(false, 0, "unit-not-ready");

        long target = Math.Max(0, drive.AvailableFreeSpace - FreeSpaceFill.SafetyMarginBytes);
        return await RunInAsync(WorkFolders.On(letter, WorkFolders.Verify), target, progress, ct);
    }

    /// <summary>
    /// El motor de la verificación, sin la unidad: escribe <paramref name="target"/> bytes en
    /// <paramref name="dir"/> y los relee. Existe como método aparte para poder **probar la detección de
    /// unidades falsificadas sin una unidad falsificada** — que es justo lo que ninguna prueba cubría.
    /// </summary>
    /// <param name="dir">La carpeta de trabajo donde se escriben los archivos; se borra al terminar.</param>
    /// <param name="target">Los bytes que se escriben y se releen, múltiplo de la alineación de sector.</param>
    /// <param name="progress">La fase, el porcentaje y los bytes llevados.</param>
    /// <param name="ct">El token para cancelar.</param>
    /// <param name="afterWriteAsync">
    /// Solo para pruebas: se ejecuta entre la fase de escritura y la de lectura. Permite corromper lo
    /// escrito para comprobar que la relectura lo detecta, que es exactamente lo que hace una unidad que
    /// miente sobre su tamaño.
    /// </param>
    /// <remarks>
    /// Se ejecuta en el pool de hilos (`AUD-032`): llamado desde la ventana, cada <c>await</c> volvía al hilo
    /// de la interfaz y el trabajo de cada bloque —generar y comparar 8 MB— se hacía allí, con la ventana sin
    /// responder. El progreso sigue llegando a la interfaz por <see cref="Progress{T}"/>.
    /// </remarks>
    internal static Task<VerifyResult> RunInAsync(
        string dir,
        long target,
        IProgress<(Phase phase, int percent, long bytes)> progress,
        CancellationToken ct,
        Func<Task>? afterWriteAsync = null)
        => Task.Run(() => RunInCoreAsync(dir, target, progress, ct, afterWriteAsync));

    private static async Task<VerifyResult> RunInCoreAsync(
        string dir,
        long target,
        IProgress<(Phase phase, int percent, long bytes)> progress,
        CancellationToken ct,
        Func<Task>? afterWriteAsync)
    {
        // Cada archivo agrupa varios bloques de 8 MB; guardamos el índice de bloque global inicial
        // para regenerar el patrón exacto durante la verificación (la detección de aliasing se preserva).
        var files = new List<(string Path, int StartBlock, long Length)>();
        long totalWritten = 0;

        // Redondeo a la baja al tamaño de sector: la relectura sin caché exige longitudes y
        // desplazamientos alineados, y con esto TODOS los tamaños de archivo (y por tanto todos los
        // bloques, incluido el último de cada uno) lo están. Se sacrifican menos de 4 KB del margen de
        // seguridad de 64 MB.
        target -= target % Alignment;

        AlignedBuffers? readAligned = null;

        try
        {
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

            if (afterWriteAsync is not null) await afterWriteAsync();

            // ── Fase de lectura/verificación (SIN caché del sistema) ──
            readAligned = new AlignedBuffers(1, BlockSize);
            Memory<byte> readBuf = readAligned.Blocks[0];
            var expected = new byte[BlockSize];
            long verified = 0;
            foreach (var f in files)
            {
                ct.ThrowIfCancellationRequested();

                using var handle = File.OpenHandle(f.Path, FileMode.Open, FileAccess.Read,
                                 FileShare.None, UnbufferedIo.NoBuffering | FileOptions.Asynchronous);
                long fileRead = 0;
                int blk = f.StartBlock;
                while (fileRead < f.Length)
                {
                    ct.ThrowIfCancellationRequested();
                    int size = (int)Math.Min(BlockSize, f.Length - fileRead);
                    FillPattern(expected, blk, size);

                    // Sin caché, una lectura parcial solo se puede reanudar desde un desplazamiento
                    // alineado: si llegara una que no lo está (no debería), se trata como lectura corta
                    // en vez de reintentar con una petición que la API rechazaría.
                    int total = 0;
                    while (total < size && total % Alignment == 0)
                    {
                        int read = await RandomAccess.ReadAsync(
                            handle, readBuf.Slice(total, size - total), fileRead + total, ct);
                        if (read == 0) break;
                        total += read;
                    }

                    if (total != size)
                        return new VerifyResult(false, verified, $"short-read@{blk}");

                    if (!readBuf.Span[..size].SequenceEqual(expected.AsSpan(0, size)))
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
            readAligned?.Dispose();
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
