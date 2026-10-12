// Micro-medición sin disco: cuánto procesador cuesta por bloque de 8 MB el trabajo que los servicios
// hacen entre dos esperas de E/S (y que, sin ConfigureAwait(false), se ejecuta en el hilo de UI).
// Copia literal de CapacityVerifier.FillPattern (CapacityVerifier.cs:206-220).
using System.Diagnostics;
using System.Security.Cryptography;

const int Block = 8 * 1024 * 1024;
var a = new byte[Block];
var b = new byte[Block];

static void FillPattern(byte[] buffer, int blockIndex, int length)
{
    unchecked
    {
        ulong seed = (ulong)blockIndex * 0x9E3779B97F4A7C15UL + 0xD1B54A32D192ED03UL;
        for (int i = 0; i < length; i += 8)
        {
            ulong v = seed + (ulong)i;
            v ^= v >> 30; v *= 0xBF58476D1CE4E5B9UL;
            int n = Math.Min(8, length - i);
            for (int k = 0; k < n; k++)
                buffer[i + k] = (byte)(v >> (k * 8));
        }
    }
}

static double Median(Func<int, int> body, int reps)
{
    var t = new double[reps];
    for (int i = 0; i < reps; i++)
    {
        var sw = Stopwatch.StartNew();
        body(i);
        t[i] = sw.Elapsed.TotalMilliseconds;
    }
    Array.Sort(t);
    return t[reps / 2];
}

// Calentamiento (JIT de nivel 1)
for (int i = 0; i < 40; i++) { FillPattern(a, i, Block); FillPattern(b, i, Block); a.AsSpan().SequenceEqual(b); RandomNumberGenerator.Fill(a); }

double fill = Median(i => { FillPattern(a, i, Block); return 0; }, 31);
double rng  = Median(i => { RandomNumberGenerator.Fill(a.AsSpan()); return 0; }, 31);
FillPattern(a, 7, Block); FillPattern(b, 7, Block);
double cmp  = Median(i => a.AsSpan().SequenceEqual(b) ? 1 : 0, 31);

Console.WriteLine($".NET {Environment.Version} · {Environment.ProcessorCount} núcleos lógicos · bloque de 8 MB, mediana de 31");
Console.WriteLine($"FillPattern (escritura y lectura de Verificar capacidad): {fill:F2} ms");
Console.WriteLine($"RandomNumberGenerator.Fill (pasada aleatoria del borrado seguro): {rng:F2} ms");
Console.WriteLine($"SequenceEqual (comparación en la lectura):                 {cmp:F2} ms");
Console.WriteLine($"Verificar, fase de lectura = FillPattern + SequenceEqual:  {fill + cmp:F2} ms por bloque");
foreach (int mbps in new[] { 30, 150, 500, 2000 })
{
    double blocks = mbps / 8.0;
    Console.WriteLine($"  a {mbps,4} MB/s -> {blocks,6:F1} bloques/s · hilo de UI ocupado: verificar(lectura) {Math.Min(100, blocks * (fill + cmp) / 10):F0} % · borrado {Math.Min(100, blocks * rng / 10):F0} %");
}
