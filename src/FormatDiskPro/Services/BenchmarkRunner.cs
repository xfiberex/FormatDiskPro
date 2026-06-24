using System.Diagnostics;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;

namespace FormatDiskPro;

/// <summary>Fase del benchmark en curso (para la etiqueta de progreso).</summary>
public enum BenchPhase { Preparing, SeqWrite, SeqRead, RndWrite, RndRead }

/// <summary>
/// Benchmark de lectura/escritura inspirado en CrystalDiskMark: mide la velocidad <b>secuencial</b>
/// (bloque de 1 MiB, cola Q8) y de <b>4 KiB aleatorio</b> (cola Q1) escribiendo y releyendo un archivo
/// temporal. <b>No destructivo</b> — el archivo se elimina al terminar.
/// </summary>
/// <remarks>
/// <para>Toda la E/S usa <c>FILE_FLAG_NO_BUFFERING</c> para <b>omitir la caché del sistema</b>, de modo que
/// las cifras reflejen el medio y no la RAM. La fase secuencial mantiene varias operaciones en vuelo
/// (overlapped I/O vía <see cref="Task.WhenAll(System.Collections.Generic.IEnumerable{Task})"/>) para que
/// las unidades NVMe/SSD alcancen su velocidad real, que a cola 1 quedaría infravalorada.</para>
/// <para>Cada fase se mide por <b>ventanas de tiempo</b> (se adapta a unidades rápidas y lentas) y se toma la
/// <see cref="Benchmark.Median"/> de <see cref="Benchmark.Passes"/> ventanas para descartar el arranque en
/// frío y los picos transitorios. La E/S sin caché exige buffers y desplazamientos alineados al sector: los
/// buffers se fijan con <see cref="GCHandle"/> y se toma un sub-rango alineado.</para>
/// </remarks>
public static class BenchmarkRunner
{
    private const int SeqBlock      = 1024 * 1024;   // 1 MiB por operación secuencial (estilo SEQ1M)
    private const int RndBlock      = 4096;          // 4 KiB por operación aleatoria (estilo RND4K)
    private const int SeqQueueDepth = 8;             // operaciones secuenciales en vuelo (Q8)
    private const int Alignment     = 4096;          // alineación de sector exigida por la E/S sin caché
    private const int PrepShare     = 15;            // % del progreso reservado al relleno inicial del archivo

    private static readonly TimeSpan WindowDuration = TimeSpan.FromSeconds(1.5);  // duración de cada ventana de medición

    // FILE_FLAG_NO_BUFFERING (no expuesto en FileOptions) + E/S asíncrona real para la cola profunda.
    private const FileOptions NoBuffering = (FileOptions)0x2000_0000;
    private const FileOptions IoOptions   = NoBuffering | FileOptions.Asynchronous;

    /// <summary>
    /// Ejecuta el benchmark sobre la unidad <paramref name="letter"/>. Devuelve <c>null</c> si la unidad
    /// no está lista o no hay espacio suficiente para la prueba.
    /// </summary>
    /// <param name="letter">Letra de la unidad.</param>
    /// <param name="progress">Progreso: fase actual y porcentaje 0-100 global de la operación.</param>
    /// <param name="ct">Token de cancelación.</param>
    /// <returns>Velocidades secuenciales y de 4 KiB aleatorio y tamaño probado, o <c>null</c> si no se pudo medir.</returns>
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

            long testBytes = Benchmark.PlanTestBytes(drive.AvailableFreeSpace, SeqBlock);
            if (testBytes <= 0) return null;

            // En unidades muy pequeñas, no usar más cola que bloques disponibles (evita E/S solapadas al mismo offset).
            int seqQd = (int)Math.Clamp(testBytes / SeqBlock, 1, SeqQueueDepth);

            Directory.CreateDirectory(dir);

            byte[] seqRaw, rndRaw;
            var seqPin = default(GCHandle);
            var rndPin = default(GCHandle);
            try
            {
                Memory<byte>[] seqBuffers = AllocAligned(seqQd, SeqBlock, out seqRaw, out seqPin);
                Memory<byte>[] rndBuffers = AllocAligned(1, RndBlock, out rndRaw, out rndPin);
                foreach (var b in seqBuffers) Random.Shared.NextBytes(b.Span);   // datos no triviales (no favorecer compresión/dedup)
                Random.Shared.NextBytes(rndBuffers[0].Span);
                Memory<byte> rndBuffer = rndBuffers[0];
                var rng = Random.Shared;

                int windowsDone = 0;
                const int totalWindows = 4 * Benchmark.Passes;
                int WindowPercent() => PrepShare + windowsDone * (100 - PrepShare) / totalWindows;

                // ── Preparación: escribir el archivo completo una vez (deja datos válidos para leer) ──
                using (var h = File.OpenHandle(path, FileMode.Create, FileAccess.Write, FileShare.None,
                           IoOptions, preallocationSize: testBytes))
                {
                    await FillFileAsync(h, testBytes, seqQd, seqBuffers, progress, ct);
                }

                // Mide una fase (Passes ventanas) y devuelve la mediana de MB/s, avanzando el progreso por ventana.
                async Task<double> MeasureSeqAsync(bool write, BenchPhase phase)
                {
                    var speeds = new double[Benchmark.Passes];
                    using var h = File.OpenHandle(path, FileMode.Open,
                        write ? FileAccess.Write : FileAccess.Read, FileShare.None, IoOptions);
                    for (int p = 0; p < Benchmark.Passes; p++)
                    {
                        speeds[p] = await SeqWindowAsync(h, write, testBytes, seqQd, seqBuffers, ct);
                        windowsDone++;
                        progress.Report((phase, WindowPercent()));
                    }
                    return Benchmark.Median(speeds);
                }

                async Task<double> MeasureRndAsync(bool write, BenchPhase phase)
                {
                    var speeds = new double[Benchmark.Passes];
                    using var h = File.OpenHandle(path, FileMode.Open,
                        write ? FileAccess.Write : FileAccess.Read, FileShare.None, IoOptions);
                    for (int p = 0; p < Benchmark.Passes; p++)
                    {
                        speeds[p] = await RndWindowAsync(h, write, testBytes, rndBuffer, rng, ct);
                        windowsDone++;
                        progress.Report((phase, WindowPercent()));
                    }
                    return Benchmark.Median(speeds);
                }

                double seqWrite = await MeasureSeqAsync(write: true,  BenchPhase.SeqWrite);
                double seqRead  = await MeasureSeqAsync(write: false, BenchPhase.SeqRead);
                double rndWrite = await MeasureRndAsync(write: true,  BenchPhase.RndWrite);
                double rndRead  = await MeasureRndAsync(write: false, BenchPhase.RndRead);

                return new BenchmarkResult(
                    new BenchmarkScore(seqRead, seqWrite),
                    new BenchmarkScore(rndRead, rndWrite),
                    testBytes);
            }
            finally
            {
                if (seqPin.IsAllocated) seqPin.Free();
                if (rndPin.IsAllocated) rndPin.Free();
            }
        }
        finally
        {
            try { if (Directory.Exists(dir)) Directory.Delete(dir, recursive: true); } catch { }
        }
    }

    /// <summary>
    /// Reserva un array fijado (POH no garantiza alineación) y devuelve <paramref name="count"/> sub-buffers de
    /// <paramref name="blockSize"/> bytes alineados al sector. El llamador debe liberar <paramref name="pin"/>.
    /// </summary>
    private static Memory<byte>[] AllocAligned(int count, int blockSize, out byte[] raw, out GCHandle pin)
    {
        raw = new byte[count * blockSize + Alignment];
        pin = GCHandle.Alloc(raw, GCHandleType.Pinned);
        long addr = pin.AddrOfPinnedObject().ToInt64();
        int pad = (int)((Alignment - (addr & (Alignment - 1))) & (Alignment - 1));
        var buffers = new Memory<byte>[count];
        for (int i = 0; i < count; i++)
            buffers[i] = raw.AsMemory(pad + i * blockSize, blockSize);
        return buffers;
    }

    /// <summary>Escribe el archivo completo una vez, en lotes de cola <paramref name="qd"/>, informando del progreso.</summary>
    private static async Task FillFileAsync(SafeFileHandle h, long testBytes, int qd, Memory<byte>[] buffers,
        IProgress<(BenchPhase, int)> progress, CancellationToken ct)
    {
        long off = 0;
        var tasks = new Task[qd];
        while (off < testBytes)
        {
            int n = 0;
            for (; n < qd && off < testBytes; n++, off += SeqBlock)
                tasks[n] = RandomAccess.WriteAsync(h, buffers[n], off, ct).AsTask();
            await Task.WhenAll(n == qd ? tasks : tasks[..n]);
            ct.ThrowIfCancellationRequested();
            progress.Report((BenchPhase.Preparing, (int)((double)off / testBytes * PrepShare)));
        }
    }

    /// <summary>Una ventana secuencial (Q<paramref name="qd"/>, 1 MiB) recorriendo el archivo en bucle; devuelve MB/s.</summary>
    private static async Task<double> SeqWindowAsync(SafeFileHandle h, bool write, long testBytes, int qd,
        Memory<byte>[] buffers, CancellationToken ct)
    {
        long off = 0, bytes = 0;
        var tasks = new Task[qd];
        var sw = Stopwatch.StartNew();
        while (sw.Elapsed < WindowDuration)
        {
            for (int i = 0; i < qd; i++)
            {
                if (off >= testBytes) off = 0;
                long o = off;
                off += SeqBlock;
                tasks[i] = write
                    ? RandomAccess.WriteAsync(h, buffers[i], o, ct).AsTask()
                    : RandomAccess.ReadAsync(h, buffers[i], o, ct).AsTask();
            }
            await Task.WhenAll(tasks);
            bytes += (long)qd * SeqBlock;
            ct.ThrowIfCancellationRequested();
        }
        sw.Stop();
        return Benchmark.BytesPerSec(bytes, sw.Elapsed);
    }

    /// <summary>Una ventana de 4 KiB aleatorio (Q1) en desplazamientos alineados al azar; devuelve MB/s.</summary>
    private static async Task<double> RndWindowAsync(SafeFileHandle h, bool write, long testBytes,
        Memory<byte> buffer, Random rng, CancellationToken ct)
    {
        long bytes = 0;
        var sw = Stopwatch.StartNew();
        while (sw.Elapsed < WindowDuration)
        {
            long o = Benchmark.RandomAlignedOffset(testBytes, RndBlock, rng);
            if (write) await RandomAccess.WriteAsync(h, buffer, o, ct);
            else       await RandomAccess.ReadAsync(h, buffer, o, ct);
            bytes += RndBlock;
            ct.ThrowIfCancellationRequested();
        }
        sw.Stop();
        return Benchmark.BytesPerSec(bytes, sw.Elapsed);
    }
}
