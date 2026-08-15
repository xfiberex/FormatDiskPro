using FormatDiskPro;
using Xunit;

namespace FormatDiskPro.Tests;

/// <summary>
/// Caminos de <b>fallo</b> de la verificación de capacidad (`T2-05`).
///
/// Lo que hace valiosa a esta función —detectar una unidad que miente sobre su tamaño— no lo probaba
/// nada: la prueba de UI ejercita el camino feliz sobre una USB real y tarda casi una hora, y una USB
/// falsificada no es algo que se tenga a mano. Aquí se reproduce el fallo **sin unidad**: se corrompe lo
/// escrito entre la fase de escritura y la de lectura, que es exactamente lo que le pasa a los datos en
/// una unidad falsificada cuando el firmware reescribe direcciones (aliasing).
/// </summary>
public sealed class CapacityVerifierTests
{
    private const long Block = 8L * 1024 * 1024;   // CapacityVerifier.BlockSize

    private static readonly Progress<(CapacityVerifier.Phase, int, long)> Ignore = new();

    /// <summary>Directorio de trabajo propio por prueba, en el disco local (no toca ninguna unidad real).</summary>
    private static string ScratchDir() =>
        Path.Combine(Path.GetTempPath(), $"fdp_verify_{Guid.NewGuid():N}");

    [Fact]
    public async Task RunInAsync_IntactData_Succeeds()
    {
        string dir = ScratchDir();
        var result = await CapacityVerifier.RunInAsync(dir, Block, Ignore, CancellationToken.None);

        Assert.True(result.Ok, $"Debería pasar con datos intactos, pero devolvió '{result.FailureDetail}'.");
        Assert.Equal(Block, result.WrittenBytes);
        Assert.Equal("", result.FailureDetail);
        Assert.False(Directory.Exists(dir), "El directorio de trabajo debe borrarse al terminar.");
    }

    /// <summary>
    /// La relectura va <b>sin caché del sistema</b> (`T2-03`), y eso impone alineación de sector en
    /// buffer, desplazamiento y longitud. El espacio libre de una unidad real no es múltiplo de nada, así
    /// que el objetivo se redondea a la baja: aquí se pide un tamaño deliberadamente feo y se exige que
    /// funcione igual y que lo escrito quede alineado.
    ///
    /// <para>Nótese que el resto de pruebas de esta clase <b>también</b> ejercitan esa ruta: si la
    /// alineación estuviera mal, Windows rechazaría la lectura con un error de parámetro en vez de
    /// devolver datos.</para>
    /// </summary>
    [Fact]
    public async Task RunInAsync_UnalignedTarget_IsRoundedDownAndStillVerifies()
    {
        string dir = ScratchDir();

        var result = await CapacityVerifier.RunInAsync(dir, Block + 1234, Ignore, CancellationToken.None);

        Assert.True(result.Ok, $"Debería verificar igual, pero devolvió '{result.FailureDetail}'.");
        Assert.Equal(0, result.WrittenBytes % 4096);
        Assert.Equal(Block, result.WrittenBytes);   // 1234 < 4096: el redondeo se come el resto
    }

    /// <summary>
    /// El caso que justifica la función entera: un bloque que al releerse no contiene lo que se escribió.
    /// Se corrompe **un solo byte** dentro del bloque 1, y se exige que el fallo lo señale por su índice
    /// —no basta con que falle, tiene que decir dónde—.
    /// </summary>
    [Fact]
    public async Task RunInAsync_CorruptedBlock_IsDetectedAndNamed()
    {
        string dir = ScratchDir();
        long target = 3 * Block;

        var result = await CapacityVerifier.RunInAsync(dir, target, Ignore, CancellationToken.None,
            afterWriteAsync: async () =>
            {
                string file = Directory.GetFiles(dir, "vol_*.bin").Single();
                await using var fs = new FileStream(file, FileMode.Open, FileAccess.ReadWrite);
                fs.Position = Block + 100;                       // dentro del bloque 1
                int original = fs.ReadByte();
                fs.Position = Block + 100;
                fs.WriteByte((byte)(original ^ 0xFF));           // un bit distinto basta
            });

        Assert.False(result.Ok, "Un bloque corrupto NO puede darse por bueno: es una unidad falsificada.");
        Assert.Equal("mismatch@1", result.FailureDetail);
        Assert.False(Directory.Exists(dir));
    }

    /// <summary>
    /// La otra forma de mentir: la unidad acepta la escritura pero luego no devuelve los datos. Se trunca
    /// el archivo tras escribirlo, de modo que la relectura se queda corta.
    /// </summary>
    [Fact]
    public async Task RunInAsync_TruncatedData_IsDetectedAsShortRead()
    {
        string dir = ScratchDir();
        long target = 3 * Block;

        var result = await CapacityVerifier.RunInAsync(dir, target, Ignore, CancellationToken.None,
            afterWriteAsync: () =>
            {
                string file = Directory.GetFiles(dir, "vol_*.bin").Single();
                using var fs = new FileStream(file, FileMode.Open, FileAccess.Write);
                fs.SetLength(Block);                             // se pierden los bloques 1 y 2
                return Task.CompletedTask;
            });

        Assert.False(result.Ok);
        Assert.Equal("short-read@1", result.FailureDetail);
        Assert.False(Directory.Exists(dir));
    }

    /// <summary>
    /// Cancelar no es fallar: devuelve "cancelled" —no lanza— y **borra igual** lo escrito. Si la limpieza
    /// no ocurriera, cancelar dejaría decenas de GB ocupados en la unidad del usuario.
    /// </summary>
    [Fact]
    public async Task RunInAsync_Cancelled_ReportsCancelledAndCleansUp()
    {
        string dir = ScratchDir();
        using var cts = new CancellationTokenSource();

        var cancelOnFirstReport = new Progress<(CapacityVerifier.Phase, int, long)>(_ => cts.Cancel());

        var result = await CapacityVerifier.RunInAsync(dir, 4 * Block, cancelOnFirstReport, cts.Token);

        Assert.False(result.Ok);
        Assert.Equal("cancelled", result.FailureDetail);
        Assert.False(Directory.Exists(dir), "Cancelar debe limpiar: si no, deja la unidad llena.");
    }

    /// <summary>
    /// Una unidad que no está lista se rechaza <b>antes</b> de escribir nada, y sin lanzar: es el caso de
    /// una USB desconectada entre que se abre el menú y se confirma el diálogo.
    /// </summary>
    [Fact]
    public async Task RunAsync_DriveNotReady_ReturnsUnitNotReady()
    {
        var result = await CapacityVerifier.RunAsync(TestPaths.UnusedDriveLetter(), Ignore, CancellationToken.None);

        Assert.False(result.Ok);
        Assert.Equal("unit-not-ready", result.FailureDetail);
        Assert.Equal(0, result.WrittenBytes);
    }
}
