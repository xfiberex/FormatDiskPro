using FormatDiskPro;
using Xunit;

namespace FormatDiskPro.Tests;

/// <summary>
/// Pruebas del borrado seguro: los helpers puros (bytes planificados, patrón por pasada) y el motor de
/// escritura (<see cref="SecureWipe.RunInAsync"/>) sobre una carpeta temporal del disco local, sin tocar
/// ninguna unidad (`AUD-016`). Se mira lo que queda escrito en cada pasada, no solo que no lance.
/// </summary>
public sealed class SecureWipeTests
{
    [Fact]
    public void PlannedBytes_SubtractsSafetyMargin_TimesPasses()
    {
        long free = SecureWipe.SafetyMarginBytes + 1000;
        Assert.Equal(1000, SecureWipe.PlannedBytes(free, 1));
        Assert.Equal(3000, SecureWipe.PlannedBytes(free, 3));
    }

    [Fact]
    public void PlannedBytes_FreeBelowMargin_IsZero()
        => Assert.Equal(0, SecureWipe.PlannedBytes(0, 1));

    [Fact]
    public void PlannedBytes_ZeroPasses_TreatedAsOne()
        => Assert.Equal(500, SecureWipe.PlannedBytes(SecureWipe.SafetyMarginBytes + 500, 0));

    [Fact]
    public void PassPattern_SinglePass_IsRandom()
        => Assert.True(SecureWipe.PassPattern(0, 1).Random);

    [Fact]
    public void PassPattern_ThreePasses_ZeroThenOnesThenRandom()
    {
        Assert.Equal((false, (byte)0x00), SecureWipe.PassPattern(0, 3));
        Assert.Equal((false, (byte)0xFF), SecureWipe.PassPattern(1, 3));
        Assert.True(SecureWipe.PassPattern(2, 3).Random);
    }

    [Fact]
    public void PassPattern_LastPassAlwaysRandom()
        => Assert.True(SecureWipe.PassPattern(1, 2).Random);

    [Theory]
    [InlineData(1, 1)]
    [InlineData(3, 3)]
    [InlineData(7, 7)]
    public void NormalizePasses_AllowedValuesUnchanged(int passes, int expected)
        => Assert.Equal(expected, SecureWipe.NormalizePasses(passes));

    [Theory]
    [InlineData(0)]
    [InlineData(2)]
    [InlineData(-5)]
    [InlineData(100)]
    public void NormalizePasses_InvalidValuesFallBackToOne(int passes)
        => Assert.Equal(1, SecureWipe.NormalizePasses(passes));

    [Fact]
    public void AllowedPasses_AreExactlyOneThreeSeven()
        => Assert.Equal(new[] { 1, 3, 7 }, SecureWipe.AllowedPasses);

    // ── El motor de escritura (`AUD-016`) ──────────────────────────────────────────

    private const long Block = 8L * 1024 * 1024;   // SecureWipe.BlockSize

    /// <summary>Directorio de trabajo propio por prueba, en el disco local (no toca ninguna unidad real).</summary>
    private static string ScratchDir() =>
        Path.Combine(Path.GetTempPath(), $"fdp_wipe_{Guid.NewGuid():N}");

    /// <summary>
    /// Progreso <b>síncrono</b>: <see cref="Progress{T}"/> entrega los avisos más tarde y en otro hilo, y
    /// aquí hace falta saber exactamente cuántos hubo y en qué orden, y poder cancelar en el primero.
    /// </summary>
    private sealed class Recorder(Action<(int percent, long bytesDone, long totalBytes)>? onReport = null)
        : IProgress<(int percent, long bytesDone, long totalBytes)>
    {
        public List<(int percent, long bytesDone, long totalBytes)> Reports { get; } = [];

        public void Report((int percent, long bytesDone, long totalBytes) value)
        {
            Reports.Add(value);
            onReport?.Invoke(value);
        }
    }

    /// <summary>Lo que hay escrito en <paramref name="dir"/>, con los archivos en orden de nombre.</summary>
    private static byte[] ReadAll(string dir)
        => Directory.GetFiles(dir).Order(StringComparer.Ordinal).SelectMany(f => File.ReadAllBytes(f)).ToArray();

    /// <summary>
    /// Datos aleatorios de varios megabytes contienen los 256 valores de byte; un patrón fijo, uno solo.
    /// Se exige casi todos para que la prueba no dependa de la suerte: que falte alguno en 8 MB de datos
    /// aleatorios es tan improbable que no va a ocurrir.
    /// </summary>
    private static void AssertLooksRandom(byte[] data, string what)
    {
        int distinct = data.Distinct().Count();
        Assert.True(distinct >= 250,
            $"{what} debería ser aleatorio, pero solo contiene {distinct} valores de byte distintos.");
    }

    private static void AssertAllBytes(byte[] data, byte expected, string what)
    {
        int at = Array.FindIndex(data, b => b != expected);
        Assert.True(at < 0, $"{what} debería ser todo 0x{expected:X2}, pero el byte {at} vale 0x{(at < 0 ? 0 : data[at]):X2}.");
    }

    /// <summary>
    /// El caso por defecto (una pasada): lo que queda en el espacio libre es aleatorio, ocupa justo lo
    /// pedido —un tamaño que no es múltiplo del bloque, como el espacio libre de verdad— y el directorio
    /// se borra al terminar.
    /// </summary>
    [Fact]
    public async Task RunInAsync_SinglePass_WritesRandomData_AndCleansUp()
    {
        string dir = ScratchDir();
        long perPass = Block + 4321;
        byte[]? written = null;

        await SecureWipe.RunInAsync(dir, perPass, 1, new Recorder(), CancellationToken.None,
            afterPassAsync: _ => { written = ReadAll(dir); return Task.CompletedTask; });

        Assert.NotNull(written);
        Assert.Equal(perPass, written.LongLength);
        AssertLooksRandom(written, "La única pasada");
        Assert.False(Directory.Exists(dir), "El directorio de trabajo debe borrarse al terminar.");
    }

    /// <summary>
    /// Con tres pasadas, lo que hay en disco tras cada una es el patrón que le toca: 0x00, 0xFF y, la
    /// última, aleatorio. Es la prueba que falla si la última pasada deja de ser aleatoria, que es la que
    /// decide qué queda en la unidad. Los archivos se limitan a un bloque para que el reparto en varios
    /// archivos también se ejercite.
    /// </summary>
    [Fact]
    public async Task RunInAsync_ThreePasses_LeavesZerosThenOnesThenRandom()
    {
        string dir = ScratchDir();
        long perPass = 2 * Block + 1000;   // tres archivos: dos llenos y uno corto
        var afterEach = new List<byte[]>();

        await SecureWipe.RunInAsync(dir, perPass, 3, new Recorder(), CancellationToken.None,
            afterPassAsync: _ => { afterEach.Add(ReadAll(dir)); return Task.CompletedTask; },
            maxFileSize: Block);

        Assert.Equal(3, afterEach.Count);
        Assert.All(afterEach, data => Assert.Equal(perPass, data.LongLength));
        AssertAllBytes(afterEach[0], 0x00, "La pasada 0");
        AssertAllBytes(afterEach[1], 0xFF, "La pasada 1");
        AssertLooksRandom(afterEach[2], "La última pasada");
        Assert.False(Directory.Exists(dir));
    }

    /// <summary>
    /// Cada pasada sobrescribe <b>los mismos archivos</b>: lo que hay en el directorio nunca pasa de lo
    /// que cabe en el espacio libre. Antes cada pasada creaba archivos nuevos sin borrar los anteriores,
    /// y en una unidad real la segunda se quedaba sin espacio: con 3 y 7 pasadas, el borrado no terminaba.
    /// </summary>
    [Fact]
    public async Task RunInAsync_EveryPass_OverwritesTheSameFiles()
    {
        string dir = ScratchDir();
        long perPass = 2 * Block + 1000;
        var snapshots = new List<(string[] Names, long Bytes)>();

        await SecureWipe.RunInAsync(dir, perPass, 3, new Recorder(), CancellationToken.None,
            afterPassAsync: _ =>
            {
                var files = Directory.GetFiles(dir).Select(f => new FileInfo(f)).ToArray();
                snapshots.Add((files.Select(f => f.Name).Order(StringComparer.Ordinal).ToArray(),
                               files.Sum(f => f.Length)));
                return Task.CompletedTask;
            },
            maxFileSize: Block);

        Assert.Equal(3, snapshots.Count);
        Assert.All(snapshots, s => Assert.Equal(perPass, s.Bytes));
        Assert.All(snapshots, s => Assert.Equal(snapshots[0].Names, s.Names));
    }

    /// <summary>
    /// El progreso cuenta todas las pasadas: el total previsto es <c>bytes por pasada × pasadas</c>, crece
    /// sin retroceder y termina en el 100 % con todo escrito.
    /// </summary>
    [Fact]
    public async Task RunInAsync_Progress_CoversAllPasses_AndEndsAtHundred()
    {
        long perPass = Block + 1;
        var progress = new Recorder();

        await SecureWipe.RunInAsync(ScratchDir(), perPass, 3, progress, CancellationToken.None);

        long total = 3 * perPass;
        Assert.Equal(6, progress.Reports.Count);   // dos escrituras por pasada: un bloque y un byte
        Assert.All(progress.Reports, r => Assert.Equal(total, r.totalBytes));
        Assert.Equal(progress.Reports.Select(r => r.bytesDone).Order(), progress.Reports.Select(r => r.bytesDone));
        Assert.Equal((100, total, total), progress.Reports[^1]);
    }

    /// <summary>
    /// Cancelar a mitad lanza <see cref="OperationCanceledException"/> (es el contrato de
    /// <see cref="ISecureWipe.RunAsync"/>), no escribe más bloques y <b>borra igual</b> lo escrito: si no,
    /// cancelar dejaría la unidad del usuario llena.
    /// </summary>
    [Fact]
    public async Task RunInAsync_Cancelled_StopsAndCleansUp()
    {
        string dir = ScratchDir();
        using var cts = new CancellationTokenSource();
        var cancelOnFirstReport = new Recorder(_ => cts.Cancel());

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => SecureWipe.RunInAsync(dir, 4 * Block, 1, cancelOnFirstReport, cts.Token));

        Assert.Single(cancelOnFirstReport.Reports);
        Assert.False(Directory.Exists(dir), "Cancelar debe limpiar: si no, deja la unidad llena.");
    }

    /// <summary>
    /// Sin espacio por encima del margen no hay nada que escribir: termina sin avisos de progreso, sin
    /// lanzar y sin dejar el directorio.
    /// </summary>
    [Fact]
    public async Task RunInAsync_NothingToWrite_ReturnsWithoutReports()
    {
        string dir = ScratchDir();
        var progress = new Recorder();

        await SecureWipe.RunInAsync(dir, 0, 3, progress, CancellationToken.None);

        Assert.Empty(progress.Reports);
        Assert.False(Directory.Exists(dir));
    }

    /// <summary>Un número de pasadas no válido cuenta como una, y esa única pasada es aleatoria.</summary>
    [Fact]
    public async Task RunInAsync_ZeroPasses_RunsOneRandomPass()
    {
        string dir = ScratchDir();
        var passesSeen = new List<int>();
        byte[]? written = null;

        await SecureWipe.RunInAsync(dir, Block, 0, new Recorder(), CancellationToken.None,
            afterPassAsync: pass => { passesSeen.Add(pass); written = ReadAll(dir); return Task.CompletedTask; });

        Assert.Equal(new[] { 0 }, passesSeen);
        Assert.NotNull(written);
        AssertLooksRandom(written, "La única pasada");
    }

    /// <summary>
    /// Una unidad que no está lista se rechaza con <c>-1</c> antes de escribir nada, y sin lanzar: es una
    /// USB retirada entre el formato y el borrado. La app trata ese valor como «el borrado no se hizo»
    /// (`AUD-009`).
    /// </summary>
    [Fact]
    public async Task RunAsync_DriveNotReady_ReturnsMinusOne()
    {
        var progress = new Recorder();

        int code = await new SecureWipe().RunAsync(TestPaths.UnusedDriveLetter(), 1, progress, CancellationToken.None);

        Assert.Equal(-1, code);
        Assert.Empty(progress.Reports);
    }

    [Fact]
    public async Task RunAsync_NotALetter_ReturnsMinusOne()
        => Assert.Equal(-1, await new SecureWipe().RunAsync('1', 1, new Recorder(), CancellationToken.None));
}
