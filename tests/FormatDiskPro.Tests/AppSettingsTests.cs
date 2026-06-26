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

    [Fact]
    public void LoadedFromFile_FalseForDefaultsAndNewInstance()
    {
        Assert.False(new AppSettings().LoadedFromFile);
        Assert.False(AppSettings.Load(_path).LoadedFromFile);   // archivo ausente → instalación nueva
    }

    [Fact]
    public void LoadedFromFile_TrueWhenLoadedFromExistingFile()
    {
        new AppSettings { Language = "en" }.Save(_path);
        Assert.True(AppSettings.Load(_path).LoadedFromFile);    // existía configuración → uso previo
    }

    [Fact]
    public void LoadedFromFile_IsNotPersisted()
    {
        // No debe serializarse a JSON (es estado en tiempo de ejecución, no una preferencia).
        new AppSettings().Save(_path);
        Assert.DoesNotContain("LoadedFromFile", File.ReadAllText(_path));
    }

    [Fact]
    public void UserPresets_RoundTrip()
    {
        var settings = new AppSettings();
        settings.UserPresets.Add(new FormatPreset("Mi NTFS", "NTFS", 4096, true, false, false));
        settings.UserPresets.Add(new FormatPreset("USB", "exFAT", 131072, true, false, true));
        settings.Save(_path);

        var loaded = AppSettings.Load(_path);
        Assert.Equal(2, loaded.UserPresets.Count);
        Assert.Equal("Mi NTFS", loaded.UserPresets[0].Name);
        Assert.Equal("exFAT", loaded.UserPresets[1].FileSystem);
        Assert.True(loaded.UserPresets[1].SecureWipe);
    }

    [Fact]
    public void NotifyOnFinish_DefaultsTrue_AndRoundTrips()
    {
        Assert.True(new AppSettings().NotifyOnFinish);

        new AppSettings { NotifyOnFinish = false }.Save(_path);
        Assert.False(AppSettings.Load(_path).NotifyOnFinish);
    }

    [Fact]
    public void SecureWipePasses_DefaultsToOne_AndRoundTrips()
    {
        Assert.Equal(1, new AppSettings().SecureWipePasses);

        new AppSettings { SecureWipePasses = 7 }.Save(_path);
        Assert.Equal(7, AppSettings.Load(_path).SecureWipePasses);
    }
}
