// SPDX-License-Identifier: GPL-3.0-only
using Xunit;

namespace FormatDiskPro.Tests;

/// <summary>
/// Lo que deja un intento de instalar una actualización (`AUD-030`): si el instalador no llega a lanzarse, el
/// cierre deja de ser intencional y la ventana vuelve a habilitar sus controles.
/// </summary>
/// <remarks>
/// Es el criterio de la tarea —«con un <see cref="IUpdateService"/> falso cuyo <c>LaunchInstaller</c> lanza,
/// los controles vuelven a estar activos»— medido en lo que decide si vuelven: <c>EndOperation</c> rehabilita
/// los controles salvo con el cierre por actualización marcado, y ese indicador es lo que fija
/// <see cref="UpdateInstallFlow"/>. La ventana no se puede crear en una prueba unitaria.
/// </remarks>
public sealed class UpdateInstallFlowTests : IDisposable
{
    private readonly string _dir = Path.Combine(Path.GetTempPath(), $"fdp_flow_{Guid.NewGuid():N}");
    private static readonly ReleaseInfo Release =
        new("v9.9.9", "", "https://example.invalid", "https://example.invalid/setup.exe", "FormatDiskPro-9.9.9-setup.exe", 0);

    public UpdateInstallFlowTests() => Directory.CreateDirectory(_dir);

    public void Dispose()
    {
        try { Directory.Delete(_dir, recursive: true); } catch { /* mejor esfuerzo */ }
    }

    private sealed class FakeUpdates(string dir) : IUpdateService
    {
        public Exception? DownloadFailure { get; init; }
        public Exception? LaunchFailure   { get; init; }
        public bool Launched { get; private set; }

        public Task<VerifiedInstaller> DownloadAsync(
            ReleaseInfo release, IProgress<int>? progress, CancellationToken ct, string? destinationPath = null)
        {
            ct.ThrowIfCancellationRequested();
            if (DownloadFailure is not null) throw DownloadFailure;
            string path = Path.Combine(dir, "FormatDiskPro-9.9.9-setup.exe");
            File.WriteAllBytes(path, [0x4D, 0x5A]);
            return Task.FromResult(new VerifiedInstaller(path, new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read)));
        }

        public void LaunchInstaller(VerifiedInstaller installer, bool silent = false)
        {
            if (LaunchFailure is not null) throw LaunchFailure;
            Launched = true;
        }

        public Task<ReleaseInfo?> GetLatestAsync(CancellationToken ct = default) => Task.FromResult<ReleaseInfo?>(null);
        public Task<ReleaseInfo?> CheckForUpdateAsync(CancellationToken ct = default) => Task.FromResult<ReleaseInfo?>(null);
        public bool OpenUrl(string url) => false;
    }

    private sealed class FakeHistory : IHistory
    {
        public List<string> Lines { get; } = [];
        public void Log(string line) => Lines.Add(line);
        public IReadOnlyList<string> ReadLines() => Lines;
        public void Clear() => Lines.Clear();
        public void Open() { }
    }

    private static async Task<(UpdateInstallOutcome Outcome, Exception? Error, List<bool> Closing, FakeHistory History)> Run(
        IUpdateService updates, CancellationToken ct = default)
    {
        var closing = new List<bool>();
        var history = new FakeHistory();
        var (outcome, error) = await UpdateInstallFlow.RunAsync(
            updates, history, Release, new Progress<int>(), () => { }, closing.Add, ct);
        return (outcome, error, closing, history);
    }

    /// <summary>
    /// El caso de la tarea. Antes, el cierre se marcaba como intencional y nadie lo desmarcaba: la ventana se
    /// quedaba con todos los controles apagados y solo se podía cerrar.
    /// </summary>
    [Fact]
    public async Task LaunchFails_ClosingIsNoLongerIntentional()
    {
        var updates = new FakeUpdates(_dir) { LaunchFailure = new System.ComponentModel.Win32Exception(1223, "El usuario canceló") };

        var (outcome, error, closing, history) = await Run(updates);

        Assert.Equal(UpdateInstallOutcome.Failed, outcome);
        Assert.IsType<System.ComponentModel.Win32Exception>(error);
        Assert.Equal([true, false], closing);   // se marcó para lanzar, y se deshizo al fallar
        Assert.False(updates.Launched);
        Assert.Contains(history.Lines, l => l.StartsWith("UPDATE DOWNLOAD ERROR v9.9.9:", StringComparison.Ordinal));
    }

    [Fact]
    public async Task Launched_LeavesTheClosingMarkedAsIntentional()
    {
        var updates = new FakeUpdates(_dir);

        var (outcome, error, closing, history) = await Run(updates);

        Assert.Equal(UpdateInstallOutcome.Launched, outcome);
        Assert.Null(error);
        Assert.Equal([true], closing);
        Assert.True(updates.Launched);
        Assert.Contains(history.Lines, l => l.StartsWith("UPDATE DOWNLOADED v9.9.9:", StringComparison.Ordinal));
    }

    [Fact]
    public async Task DownloadFails_NeverMarksTheClosing()
    {
        var updates = new FakeUpdates(_dir) { DownloadFailure = new HttpRequestException("sin red") };

        var (outcome, _, closing, _) = await Run(updates);

        Assert.Equal(UpdateInstallOutcome.Failed, outcome);
        Assert.DoesNotContain(true, closing);
        Assert.False(updates.Launched);
    }

    [Fact]
    public async Task Cancelled_NeverMarksTheClosing()
    {
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        var (outcome, _, closing, _) = await Run(new FakeUpdates(_dir), cts.Token);

        Assert.Equal(UpdateInstallOutcome.Cancelled, outcome);
        Assert.DoesNotContain(true, closing);
    }
}
