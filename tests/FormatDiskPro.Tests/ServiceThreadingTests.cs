// SPDX-License-Identifier: GPL-3.0-only
using Xunit;

namespace FormatDiskPro.Tests;

/// <summary>
/// Los motores de E/S no trabajan en el hilo de quien los llama (`AUD-032`).
/// </summary>
/// <remarks>
/// <para>Desde la ventana, el llamador es el hilo de la interfaz. Antes, cada <c>await</c> de los motores
/// volvía a él, y el trabajo de cada bloque —generar 8 MB aleatorios, comparar lo releído, miles de E/S de
/// 4 KiB en el benchmark— se hacía allí: la ventana no respondía y UI Automation agotaba su tiempo.</para>
/// <para>La prueba instala un contexto que <b>cuenta</b> cada <c>Post</c> (cada vuelta al hilo del
/// llamador) y lee la cuenta desde el progreso, que se informa en mitad de la E/S. El progreso de la prueba
/// es síncrono a propósito: el <see cref="Progress{T}"/> de la ventana sí hace <c>Post</c>, y eso es lo
/// correcto, pero no es lo que se mide aquí.</para>
/// </remarks>
public sealed class ServiceThreadingTests
{
    /// <summary>Contexto que cuenta las veces que alguien intenta volver a él, y ejecuta en el pool.</summary>
    private sealed class CountingContext : SynchronizationContext
    {
        private int _posts;
        public int Posts => Volatile.Read(ref _posts);

        public override void Post(SendOrPostCallback d, object? state)
        {
            Interlocked.Increment(ref _posts);
            ThreadPool.QueueUserWorkItem(_ => d(state));
        }

        public override void Send(SendOrPostCallback d, object? state)
        {
            Interlocked.Increment(ref _posts);
            d(state);
        }
    }

    /// <summary>Progreso que se ejecuta en el mismo hilo que informa: no pasa por ningún contexto.</summary>
    private sealed class InlineProgress<T>(Action<T> report) : IProgress<T>
    {
        public void Report(T value) => report(value);
    }

    private static string TempDir() =>
        Path.Combine(Path.GetTempPath(), "fdp-threading-" + Guid.NewGuid().ToString("N"));

    /// <summary>
    /// Arranca <paramref name="run"/> con <see cref="CountingContext"/> como contexto actual —como si lo
    /// llamara el hilo de la interfaz— y espera a que termine.
    /// </summary>
    private static async Task RunUnderCountingContext(CountingContext ctx, Func<Task> run)
    {
        SynchronizationContext? previous = SynchronizationContext.Current;
        SynchronizationContext.SetSynchronizationContext(ctx);
        Task task;
        try     { task = run(); }
        finally { SynchronizationContext.SetSynchronizationContext(previous); }
        await task;
    }

    [Fact]
    public async Task SecureWipe_DoesItsBlockWorkOffTheCallersThread()
    {
        string dir = TempDir();
        var seen = new List<int>();
        var ctx = new CountingContext();
        try
        {
            await RunUnderCountingContext(ctx, () =>
                SecureWipe.RunInAsync(dir, perPass: 24L * 1024 * 1024, passes: 2,
                    new InlineProgress<(int, long, long)>(_ => { lock (seen) seen.Add(ctx.Posts); }),
                    CancellationToken.None, maxFileSize: 8L * 1024 * 1024));

            Assert.NotEmpty(seen);
            Assert.All(seen, posts => Assert.Equal(0, posts));
        }
        finally
        {
            if (Directory.Exists(dir)) Directory.Delete(dir, recursive: true);
        }
    }

    [Fact]
    public async Task CapacityVerifier_DoesItsBlockWorkOffTheCallersThread()
    {
        string dir = TempDir();
        var seen = new List<int>();
        var ctx = new CountingContext();
        try
        {
            Task<CapacityVerifier.VerifyResult> run = null!;
            await RunUnderCountingContext(ctx, () => run = CapacityVerifier.RunInAsync(dir, target: 24L * 1024 * 1024,
                new InlineProgress<(CapacityVerifier.Phase, int, long)>(_ => { lock (seen) seen.Add(ctx.Posts); }),
                CancellationToken.None));
            CapacityVerifier.VerifyResult result = await run;

            Assert.True(result.Ok, result.FailureDetail);
            Assert.NotEmpty(seen);
            Assert.All(seen, posts => Assert.Equal(0, posts));
        }
        finally
        {
            if (Directory.Exists(dir)) Directory.Delete(dir, recursive: true);
        }
    }
}
