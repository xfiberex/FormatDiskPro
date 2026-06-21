using FormatDiskPro;
using Xunit;

namespace FormatDiskPro.Tests;

/// <summary>
/// Verifica la persistencia de preferencias: round-trip de guardado/carga y el
/// comportamiento defensivo ante archivos ausentes o JSON corrupto.
/// </summary>
public sealed class AppSettingsTests : IDisposable
{
    private readonly string _dir;
    private readonly string _path;

    public AppSettingsTests()
    {
        _dir  = Path.Combine(Path.GetTempPath(), "fdp_tests_" + Guid.NewGuid().ToString("N"));
        _path = Path.Combine(_dir, "settings.json");
    }

    public void Dispose()
    {
        try { if (Directory.Exists(_dir)) Directory.Delete(_dir, recursive: true); } catch { }
    }

    [Fact]
    public void Load_MissingFile_ReturnsDefaults()
    {
        var s = AppSettings.Load(_path);
        Assert.Equal("es", s.Language);
        Assert.Equal("auto", s.Theme);
        Assert.Null(s.LastDriveLetter);
    }

    [Fact]
    public void SaveThenLoad_RoundTripsValues()
    {
        new AppSettings { Language = "en", Theme = "dark", LastDriveLetter = "G" }.Save(_path);

        var loaded = AppSettings.Load(_path);
        Assert.Equal("en", loaded.Language);
        Assert.Equal("dark", loaded.Theme);
        Assert.Equal("G", loaded.LastDriveLetter);
    }

    [Fact]
    public void Save_CreatesMissingDirectory()
    {
        new AppSettings().Save(_path);
        Assert.True(File.Exists(_path));
    }

    [Fact]
    public void Load_CorruptJson_ReturnsDefaults()
    {
        Directory.CreateDirectory(_dir);
        File.WriteAllText(_path, "{ esto no es json válido ");

        var s = AppSettings.Load(_path);
        Assert.Equal("es", s.Language);
        Assert.Equal("auto", s.Theme);
    }
}
