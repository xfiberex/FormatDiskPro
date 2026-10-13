using Xunit;

namespace FormatDiskPro.Tests;

/// <summary>
/// La app no escribe, mueve ni borra a través de una carpeta de estado que resulte ser un enlace
/// (`AUD-025`).
///
/// <para><c>%AppData%\FormatDiskPro</c> pertenece a la cuenta sin elevar, y la app opera en ella
/// elevada. Si esa cuenta sustituye la carpeta por una unión, es un administrador quien crea, pisa o
/// borra archivos allí donde apunte. Estas pruebas montan la unión de verdad y miran <b>el otro
/// lado</b>: lo que cuenta no es lo que devuelven los métodos, sino que en el destino no cambie nada.</para>
/// </summary>
public sealed class StateFolderTests
{
    private const string Untouched = "esto no es de la app y no se toca";

    /// <summary>Una unión <c>estado</c> → <c>destino</c>, con el destino ya creado.</summary>
    private static (string State, string Target) Redirect(ScratchFolder scratch)
    {
        string target = scratch.CreateFolder("destino");
        string state = scratch.PathOf("estado");
        scratch.CreateJunction(state, target);
        return (state, target);
    }

    [Fact]
    public void History_ThroughAJunction_CreatesNothingOnTheOtherSide()
    {
        using var scratch = new ScratchFolder();
        var (state, target) = Redirect(scratch);
        var history = new History(Path.Combine(state, "history.log"));

        history.Log("FORMAT OK G:");
        history.Clear();

        Assert.Empty(Directory.GetFileSystemEntries(target));
        Assert.Empty(history.ReadLines());
    }

    /// <summary>
    /// El caso que hace daño: al otro lado <b>ya hay</b> archivos con esos nombres. «Borrar el
    /// historial» borra <c>history.1.log</c> y reescribe <c>history.log</c>; a través de la unión, eso
    /// era un administrador borrando y pisando archivos ajenos.
    /// </summary>
    [Fact]
    public void History_ThroughAJunction_LeavesExistingFilesAsTheyWere()
    {
        using var scratch = new ScratchFolder();
        var (state, target) = Redirect(scratch);
        string log = Path.Combine(target, "history.log");
        string rotated = Path.Combine(target, "history.1.log");
        File.WriteAllText(log, Untouched);
        File.WriteAllText(rotated, Untouched);
        var history = new History(Path.Combine(state, "history.log"));

        history.Log("FORMAT OK G:");
        history.Clear();

        Assert.Equal(Untouched, File.ReadAllText(log));
        Assert.Equal(Untouched, File.ReadAllText(rotated));
        // Y tampoco se lee: lo que hay ahí no es el historial de la app.
        Assert.Empty(history.ReadLines());
    }

    /// <summary>
    /// «Abrir archivo» crea el historial si no existe y luego lo abre. A través de una unión no hace ni
    /// lo uno ni lo otro, y lo dice con una excepción —que es como este método cuenta sus fallos—.
    /// </summary>
    [Fact]
    public void History_Open_ThroughAJunction_RefusesInsteadOfCreatingTheFile()
    {
        using var scratch = new ScratchFolder();
        var (state, target) = Redirect(scratch);
        var history = new History(Path.Combine(state, "history.log"));

        var ex = Assert.Throws<InvalidOperationException>(history.Open);

        Assert.Contains(state, ex.Message, StringComparison.Ordinal);
        Assert.Empty(Directory.GetFileSystemEntries(target));
    }

    [Fact]
    public void Settings_Save_ThroughAJunction_DoesNotOverwriteWhatIsThere()
    {
        using var scratch = new ScratchFolder();
        var (state, target) = Redirect(scratch);
        string json = Path.Combine(target, "settings.json");
        File.WriteAllText(json, Untouched);

        new AppSettings { Language = "en" }.Save(Path.Combine(state, "settings.json"));

        Assert.Equal(Untouched, File.ReadAllText(json));
    }

    /// <summary>
    /// Un <c>settings.json</c> ilegible se <b>aparta</b> a <c>.corrupt.json</c> (`T9-08`), y apartar es
    /// mover un archivo. A través de una unión no se mueve nada: se arranca con los valores por defecto
    /// y el archivo se queda donde estaba, con su nombre.
    /// </summary>
    [Fact]
    public void Settings_Load_ThroughAJunction_NeitherReadsNorMovesTheFile()
    {
        using var scratch = new ScratchFolder();
        var (state, target) = Redirect(scratch);
        string json = Path.Combine(target, "settings.json");
        File.WriteAllText(json, "{ esto no es JSON");

        AppSettings loaded = AppSettings.Load(Path.Combine(state, "settings.json"));

        Assert.False(loaded.LoadedFromFile);
        Assert.Null(loaded.PreservedUnreadablePath);
        Assert.Equal(["settings.json"], Directory.GetFiles(target).Select(Path.GetFileName));
    }

    /// <summary>Lo que se lee a través de la unión tampoco se usa, aunque sea un JSON válido.</summary>
    [Fact]
    public void Settings_Load_ThroughAJunction_IgnoresAValidFileToo()
    {
        using var scratch = new ScratchFolder();
        var (state, target) = Redirect(scratch);
        File.WriteAllText(Path.Combine(target, "settings.json"), """{ "Language": "fr" }""");

        AppSettings loaded = AppSettings.Load(Path.Combine(state, "settings.json"));

        Assert.Equal(new AppSettings().Language, loaded.Language);
    }

    // ── La comprobación ───────────────────────────────────────────

    [Fact]
    public void IsRedirected_IsTrueForAFileInsideAJunction()
    {
        using var scratch = new ScratchFolder();
        var (state, _) = Redirect(scratch);

        Assert.True(StateFolder.IsRedirected(Path.Combine(state, "history.log")));
    }

    /// <summary>
    /// El caso normal, que es el de todo el mundo: una carpeta corriente —exista o no todavía, con el
    /// archivo o sin él— no se toma por un enlace. Si esto fallara la app dejaría de guardar nada.
    /// </summary>
    [Fact]
    public void IsRedirected_IsFalseForAnOrdinaryFolder_WhetherOrNotItExistsYet()
    {
        using var scratch = new ScratchFolder();
        string existing = scratch.CreateFolder("normal");
        File.WriteAllText(Path.Combine(existing, "history.log"), "x");

        Assert.False(StateFolder.IsRedirected(Path.Combine(existing, "history.log")));
        Assert.False(StateFolder.IsRedirected(Path.Combine(existing, "todavia-no.log")));
        Assert.False(StateFolder.IsRedirected(scratch.PathOf("no-existe", "history.log")));
    }

    /// <summary>Basta con que uno de los archivos consultados esté redirigido.</summary>
    [Fact]
    public void IsRedirected_ChecksEveryFileItIsGiven()
    {
        using var scratch = new ScratchFolder();
        var (state, _) = Redirect(scratch);
        string normal = scratch.CreateFolder("normal");

        Assert.True(StateFolder.IsRedirected(
            Path.Combine(normal, "history.log"), Path.Combine(state, "settings.json")));
    }

    /// <summary>La carpeta de estado sigue siendo la de siempre: no se ha movido de sitio.</summary>
    [Fact]
    public void DefaultPaths_StayUnderAppData()
    {
        string appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);

        Assert.Equal(Path.Combine(appData, "FormatDiskPro"), StateFolder.DefaultDirectory);
        Assert.Equal(Path.Combine(appData, "FormatDiskPro", "history.log"), History.DefaultPath);
        Assert.Equal(Path.Combine(appData, "FormatDiskPro", "settings.json"), AppSettings.DefaultPath);
    }
}
