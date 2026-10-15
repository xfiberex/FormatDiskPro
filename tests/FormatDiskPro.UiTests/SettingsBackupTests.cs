namespace FormatDiskPro.UiTests;

/// <summary>
/// El respaldo de ajustes sobrevive a que el proceso de pruebas muera (`AUD-040`). No lanza la app: va
/// sobre carpetas temporales, y «matar el proceso» es no llamar a <see cref="SettingsBackup.Restore"/>,
/// que es exactamente lo que deja de ocurrir cuando la corrida muere.
/// </summary>
public sealed class SettingsBackupTests : IDisposable
{
    private readonly string _root = Directory.CreateTempSubdirectory("fdp_backup_").FullName;

    private string AppDir    => Path.Combine(_root, "app");
    private string BackupDir => Path.Combine(_root, "respaldo");
    private string AppFile(string name) => Path.Combine(AppDir, name);

    public SettingsBackupTests()
    {
        Directory.CreateDirectory(AppDir);
        File.WriteAllText(AppFile("settings.json"), "{\"Language\":\"Es\"}");
        File.WriteAllText(AppFile("history.log"), "entrada real del usuario\n");
        // history.1.log NO existe: restaurar tiene que borrarlo si las pruebas lo crean.
    }

    public void Dispose()
    {
        try { Directory.Delete(_root, recursive: true); } catch { }
    }

    /// <summary>Lo que dejaría una corrida de pruebas: otro idioma, entradas de prueba y un archivo nuevo.</summary>
    private void PolluteLikeATestRun()
    {
        File.WriteAllText(AppFile("settings.json"), "{\"Language\":\"Fr\"}");
        File.AppendAllText(AppFile("history.log"), "FORMAT de prueba\n");
        File.WriteAllText(AppFile("history.1.log"), "rotado por las pruebas\n");
    }

    private void AssertOriginals()
    {
        Assert.Equal("{\"Language\":\"Es\"}", File.ReadAllText(AppFile("settings.json")));
        Assert.Equal("entrada real del usuario\n", File.ReadAllText(AppFile("history.log")));
        Assert.False(File.Exists(AppFile("history.1.log")), "history.1.log no existía y debería haberse borrado.");
    }

    [Fact]
    public void Restore_PutsTheOriginalsBack_AndRemovesTheDiskCopy()
    {
        var backup = SettingsBackup.Capture(AppDir, BackupDir);
        Assert.True(File.Exists(Path.Combine(BackupDir, SettingsBackup.CompleteMarker)),
            "Capturar tiene que dejar una copia completa en disco.");

        PolluteLikeATestRun();
        backup.Restore();

        AssertOriginals();
        Assert.False(Directory.Exists(BackupDir), "Una copia ya restaurada no debe quedarse en disco.");
    }

    /// <summary>
    /// El criterio de aceptación: la corrida muere sin restaurar, y la siguiente devuelve los archivos
    /// originales <b>antes</b> de capturar. Sin la copia en disco, la segunda captura respaldaría el estado
    /// que dejó la primera, y al terminar «restauraría» los ajustes de las pruebas.
    /// </summary>
    [Fact]
    public void AfterARunThatDied_TheNextCaptureRestoresTheOriginals()
    {
        SettingsBackup.Capture(AppDir, BackupDir);   // corrida 1
        PolluteLikeATestRun();                         // ...y muere sin llamar a Restore

        var second = SettingsBackup.Capture(AppDir, BackupDir);   // corrida 2

        AssertOriginals();
        PolluteLikeATestRun();
        second.Restore();
        AssertOriginals();
        Assert.False(Directory.Exists(BackupDir));
    }

    /// <summary>
    /// Una copia sin marcador de completa es de una captura que murió a medias, cuando las pruebas aún no
    /// habían tocado nada: no se restaura, porque podría pisar los archivos buenos con una copia parcial.
    /// </summary>
    [Fact]
    public void AnIncompleteDiskCopy_IsIgnored()
    {
        Directory.CreateDirectory(BackupDir);
        File.WriteAllText(Path.Combine(BackupDir, "settings.json"), "{\"Language\":\"De\"}");

        var backup = SettingsBackup.Capture(AppDir, BackupDir);

        AssertOriginals();
        backup.Restore();
        AssertOriginals();
    }
}
