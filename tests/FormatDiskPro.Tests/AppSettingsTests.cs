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

    /// <summary>
    /// La documentación decía que estos dos valores «se validan al cargar» y no era cierto: la
    /// normalización solo ocurría en la UI, al construir sus ComboBox, así que un <c>settings.json</c>
    /// editado a mano entraba en el objeto con un valor imposible. Ahora <see cref="AppSettings.Load"/> lo
    /// hace de verdad — el documento y el comportamiento ya coinciden.
    /// </summary>
    [Theory]
    [InlineData(0)]      // menos de una pasada no es una pasada
    [InlineData(-3)]
    [InlineData(2)]      // solo se admiten 1, 3 y 7
    [InlineData(99)]
    public void Load_InvalidSecureWipePasses_IsNormalized(int stored)
    {
        Directory.CreateDirectory(_dir);
        File.WriteAllText(_path, $$"""{"SecureWipePasses": {{stored}}}""");

        Assert.Equal(1, AppSettings.Load(_path).SecureWipePasses);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(7)]      // no está entre los tamaños ofrecidos
    [InlineData(64)]     // por encima del máximo que Windows admite en FAT32
    public void Load_InvalidSmallFat32Size_IsNormalized(int stored)
    {
        Directory.CreateDirectory(_dir);
        File.WriteAllText(_path, $$"""{"SmallFat32SizeGb": {{stored}}}""");

        int loaded = AppSettings.Load(_path).SmallFat32SizeGb;
        Assert.Contains(loaded, ReinitPlan.AllowedSmallFat32SizesGb);
    }

    /// <summary>Un sistema de archivos imposible para el sobrante (FAT32 en un disco grande reventaría al
    /// formatear, con el disco ya borrado) no puede sobrevivir a la carga.</summary>
    [Theory]
    [InlineData("FAT32")]
    [InlineData("ReFS")]
    [InlineData("ext4")]
    public void Load_InvalidSecondPartitionFileSystem_IsNormalized(string stored)
    {
        Directory.CreateDirectory(_dir);
        File.WriteAllText(_path, $$"""{"SecondPartitionFileSystem": "{{stored}}"}""");

        Assert.Equal("exFAT", AppSettings.Load(_path).SecondPartitionFileSystem);
    }

    [Fact]
    public void Load_ValidSecondPartitionFileSystem_IsKept()
    {
        Directory.CreateDirectory(_dir);
        File.WriteAllText(_path, """{"SecondPartitionFileSystem": "NTFS"}""");

        Assert.Equal("NTFS", AppSettings.Load(_path).SecondPartitionFileSystem);
    }

    /// <summary>Por defecto el sobrante se sigue dejando sin asignar: `T5-02` amplía lo que se puede
    /// hacer, no cambia lo que ocurre si no tocas nada.</summary>
    [Fact]
    public void Defaults_LeaveTheRestUnallocated()
    {
        var fresh = new AppSettings();

        Assert.False(fresh.CreateSecondPartition);
        Assert.Equal("exFAT", fresh.SecondPartitionFileSystem);
    }

    /// <summary>
    /// Un <c>settings.json</c> ilegible se <b>aparta</b>, no se pierde (`T9-08`).
    ///
    /// <para><b>El fallo que fija:</b> cargar era defensivo —ante un JSON roto se arrancaba con los
    /// valores por defecto, que es lo correcto— pero el archivo se quedaba donde estaba, y las
    /// preferencias se guardan al salir. El primer <see cref="AppSettings.Save"/> lo sobrescribía y con
    /// él se iban los <b>presets del usuario</b>, que son el único dato que la app no sabe reconstruir.
    /// Un fallo de lectura no puede destruir el dato que no se pudo leer.</para>
    ///
    /// <para>Se comprueba lo que importa: que la app arranque, que el contenido original siga existiendo
    /// con otro nombre, y que <see cref="AppSettings.PreservedUnreadablePath"/> lo diga — es lo que la
    /// ventana principal usa para dejar constancia en el historial.</para>
    /// </summary>
    [Fact]
    public void Load_UnreadableFile_IsPreservedInsteadOfOverwritten()
    {
        Directory.CreateDirectory(_dir);
        const string original = """{"UserPresets": [{"Name": "El mío", "Fi""";   // truncado a la mitad
        File.WriteAllText(_path, original);

        var settings = AppSettings.Load(_path);

        // Arranca, y con los valores por defecto.
        Assert.False(settings.LoadedFromFile);
        Assert.Empty(settings.UserPresets);

        // El archivo ilegible ya no ocupa la ruta que el próximo Save() pisaría...
        Assert.False(File.Exists(_path));

        // ...sino que sigue existiendo, íntegro, con su nombre nuevo.
        string preserved = AppSettings.UnreadablePathFor(_path);
        Assert.Equal(preserved, settings.PreservedUnreadablePath);
        Assert.True(File.Exists(preserved));
        Assert.Equal(original, File.ReadAllText(preserved));

        // Y guardar encima ya no puede llevárselo por delante.
        settings.Save(_path);
        Assert.Equal(original, File.ReadAllText(preserved));
    }

    /// <summary>
    /// La comprobación de actualizaciones al arrancar viene <b>activada</b> y se puede apagar (`T9-18`).
    ///
    /// <para>El valor por defecto importa: es la única conexión a Internet de la app, y desactivarla de
    /// serie dejaría sin actualizaciones —ni avisos de seguridad— a quien nunca abra el menú. Lo que hacía
    /// falta no era apagarla, sino que se <b>pudiera</b> apagar; antes no había forma.</para>
    ///
    /// <para>Se comprueba también que persista: una preferencia de privacidad que se olvida al reiniciar
    /// no es una preferencia.</para>
    /// </summary>
    [Fact]
    public void CheckUpdatesOnStartup_DefaultsToOn_AndPersistsWhenTurnedOff()
    {
        Assert.True(new AppSettings().CheckUpdatesOnStartup);

        Directory.CreateDirectory(_dir);
        new AppSettings { CheckUpdatesOnStartup = false }.Save(_path);

        Assert.False(AppSettings.Load(_path).CheckUpdatesOnStartup);
    }

    /// <summary>Un archivo legible no se aparta: la ruta de rescate solo se activa cuando hace falta.</summary>
    [Fact]
    public void Load_ValidFile_IsNotPreserved()
    {
        Directory.CreateDirectory(_dir);
        File.WriteAllText(_path, """{"Language": "fr"}""");

        var settings = AppSettings.Load(_path);

        Assert.Null(settings.PreservedUnreadablePath);
        Assert.True(File.Exists(_path));
        Assert.False(File.Exists(AppSettings.UnreadablePathFor(_path)));
    }
}
