using FormatDiskPro;
using Xunit;

namespace FormatDiskPro.Tests;

/// <summary>
/// Las tres operaciones largas ante una unidad que no está: **degradan, no revientan** (`T2-05`).
///
/// Es el caso real de desconectar la USB entre que se abre el menú y se confirma el diálogo, y hasta ahora
/// solo se podía comprobar con hardware. Ninguna de estas pruebas toca una unidad real: usan una letra
/// libre de la máquina.
/// </summary>
public sealed class OperationGuardTests
{
    private static readonly char Missing = TestPaths.UnusedDriveLetter();

    [Fact]
    public async Task Benchmark_MissingDrive_ReturnsNullWithoutThrowing()
    {
        var result = await BenchmarkRunner.RunAsync(
            Missing, new Progress<(BenchPhase, int)>(), CancellationToken.None);

        Assert.Null(result);
    }

    /// <summary>
    /// Una "letra" que no es letra no puede llegar a componer una ruta ni a lanzar un proceso: es la
    /// primera guarda de las tres operaciones.
    /// </summary>
    [Theory]
    [InlineData('1')]
    [InlineData('\\')]
    [InlineData(' ')]
    public async Task Benchmark_NonLetter_IsRejectedBeforeTouchingTheDisk(char notALetter)
        => Assert.Null(await BenchmarkRunner.RunAsync(
            notALetter, new Progress<(BenchPhase, int)>(), CancellationToken.None));

    [Theory]
    [InlineData('1')]
    [InlineData('\\')]
    [InlineData(' ')]
    public async Task CheckDisk_NonLetter_ReturnsFailureCodeWithoutLaunchingChkdsk(char notALetter)
    {
        var (code, output) = await CheckDisk.RunAsync(
            notALetter, repair: false, new Progress<int>(), CancellationToken.None);

        Assert.Equal(-1, code);
        Assert.Equal("", output);

        // Y ese código se traduce en un fallo visible, no en un "limpio" silencioso.
        Assert.Equal(CheckResult.Failed, CheckDisk.Interpret(code, repair: false));
    }

    /// <summary>
    /// chkdsk devuelve códigos que no están documentados uno a uno; cualquiera que no sea 0/1/2 tiene que
    /// caer en <see cref="CheckResult.Failed"/>, nunca en "limpio".
    /// </summary>
    [Theory]
    [InlineData(3)]
    [InlineData(-1)]
    [InlineData(255)]
    public void CheckDisk_UnknownExitCode_IsAlwaysFailed(int code)
    {
        Assert.Equal(CheckResult.Failed, CheckDisk.Interpret(code, repair: false));
        Assert.Equal(CheckResult.Failed, CheckDisk.Interpret(code, repair: true));
    }
}
