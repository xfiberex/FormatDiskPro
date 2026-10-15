using Xunit;

namespace FormatDiskPro.Tests;

/// <summary>
/// Las consultas de <see cref="DiskService"/> no esperan para siempre a un PowerShell colgado (`AUD-010`).
/// Con un proceso que no termina nunca, cada método devuelve su valor de «no se sabe» dentro del plazo y
/// mata el proceso, en vez de dejar esperando a «Formatear» o a «Reinicializar».
/// </summary>
public sealed class DiskServiceTimeoutTests
{
    /// <summary>Plazo de las pruebas: corto para que la suite no lo note, y de sobra para un proceso falso.</summary>
    private static readonly TimeSpan QueryTimeout = TimeSpan.FromMilliseconds(200);

    /// <summary>
    /// Margen para dar la prueba por fallida en vez de colgar la suite. Es lo que ocurría antes: sin plazo,
    /// la espera no terminaba.
    /// </summary>
    private static readonly TimeSpan Deadline = TimeSpan.FromSeconds(10);

    private static (DiskService Disk, FakeProcessRunner Runner) Hanging()
    {
        var runner = FakeProcessRunner.Hanging();
        return (new DiskService(runner, isMounted: _ => true, queryTimeout: QueryTimeout), runner);
    }

    private static async Task<T> WithinDeadline<T>(Task<T> query)
    {
        var finished = await Task.WhenAny(query, Task.Delay(Deadline));
        Assert.True(finished == query, $"La consulta seguía esperando pasados {Deadline.TotalSeconds} s: no tiene plazo.");
        return await query;
    }

    private static void AssertKilled(FakeProcessRunner runner)
    {
        Assert.NotEmpty(runner.Handles);
        Assert.All(runner.Handles, h => Assert.True(h.WasKilled, "El PowerShell colgado tiene que matarse, no abandonarse."));
    }

    public static TheoryData<string> Queries() => new()
    {
        nameof(DiskService.GetHealthAsync),
        nameof(DiskService.GetSmartAsync),
        nameof(DiskService.IsDiskReadOnlyAsync),
        nameof(DiskService.GetDiskNumberAsync),
        nameof(DiskService.GetDiskSizeAsync),
        nameof(DiskService.GetIdentityAsync),
    };

    /// <summary>Las seis consultas que devuelven un dato dan <c>null</c>, que es el «no se sabe» de todas.</summary>
    [Theory]
    [MemberData(nameof(Queries))]
    public async Task AHungQuery_ReturnsUnknown_WithinTheTimeout(string query)
    {
        var (disk, runner) = Hanging();

        object? result = query switch
        {
            nameof(DiskService.GetHealthAsync)      => await WithinDeadline(disk.GetHealthAsync('G')),
            nameof(DiskService.GetSmartAsync)       => await WithinDeadline(disk.GetSmartAsync('G')),
            nameof(DiskService.IsDiskReadOnlyAsync) => await WithinDeadline(disk.IsDiskReadOnlyAsync('G')),
            nameof(DiskService.GetDiskNumberAsync)  => await WithinDeadline(disk.GetDiskNumberAsync('G')),
            nameof(DiskService.GetDiskSizeAsync)    => await WithinDeadline(disk.GetDiskSizeAsync('G')),
            nameof(DiskService.GetIdentityAsync)    => await WithinDeadline(disk.GetIdentityAsync('G')),
            _ => throw new ArgumentOutOfRangeException(nameof(query), query, null),
        };

        Assert.Null(result);
        AssertKilled(runner);
    }

    /// <summary>Los dos cambios de estado dan <c>false</c>: no se puede afirmar que se hicieron.</summary>
    [Fact]
    public async Task AHungClearReadOnly_ReportsFailure_WithinTheTimeout()
    {
        var (disk, runner) = Hanging();

        Assert.False(await WithinDeadline(disk.ClearReadOnlyAsync('G')));
        AssertKilled(runner);
    }

    [Fact]
    public async Task AHungEject_ReportsFailure_WithinTheTimeout()
    {
        var (disk, runner) = Hanging();

        Assert.False(await WithinDeadline(disk.EjectAsync('G')));
        AssertKilled(runner);
    }

    /// <summary>
    /// La salida de error ya no se redirige: nadie la leía, y PowerShell se quedaba bloqueado al llenar la
    /// tubería con sus errores en CLIXML.
    /// </summary>
    [Fact]
    public async Task NoQuery_RedirectsTheErrorOutput()
    {
        var runner = FakeProcessRunner.Returning("");
        var disk = new DiskService(runner, isMounted: _ => false);

        await disk.GetHealthAsync('G');
        await disk.IsDiskReadOnlyAsync('G');
        await disk.GetIdentityAsync('G');
        await disk.ClearReadOnlyAsync('G');

        Assert.NotEmpty(runner.Started);
        Assert.All(runner.Started, spec => Assert.False(spec.RedirectStandardError,
            "Redirigir la salida de error sin leerla puede bloquear el proceso."));
    }
}
