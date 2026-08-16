using Xunit;

namespace FormatDiskPro.Tests;

/// <summary>
/// El historial sobre archivos de verdad: que rote al pasar del umbral, que rotar <b>no vacíe lo que el
/// visor muestra</b> y que borrarlo se lleve también la generación anterior.
///
/// <para>Todo se hace sobre un historial propio en <c>%TEMP%</c>, nunca sobre el de <c>%AppData%</c>: probar
/// la rotación y el borrado ahí destruiría el historial real de quien ejecute las pruebas.</para>
/// </summary>
public sealed class HistoryTests : IDisposable
{
    private readonly string _dir;
    private readonly string _log;
    // La ruta se inyecta por constructor (T4-02): antes hacía falta una costura `internal static`
    // con la ruta como parámetro para no escribir en el %AppData% real.
    private readonly History _history;

    public HistoryTests()
    {
        _dir = Path.Combine(Path.GetTempPath(), $"fdp_history_{Guid.NewGuid():N}");
        Directory.CreateDirectory(_dir);
        _log = Path.Combine(_dir, "history.log");
        _history = new History(_log);
    }

    public void Dispose()
    {
        try { Directory.Delete(_dir, recursive: true); } catch { /* mejor esfuerzo */ }
    }

    private string Previous => HistoryRotation.PreviousPath(_log);

    /// <summary>Deja el historial justo en el umbral, con una entrada reconocible al final.</summary>
    private void FillToThreshold(string marker)
    {
        string filler = new string('x', 200) + Environment.NewLine;
        using var w = new StreamWriter(_log, append: false);
        while (w.BaseStream.Length + filler.Length < HistoryRotation.MaxBytes)
            w.Write(filler);
        w.Write($"2026-08-15 10:00:00\t{marker}{Environment.NewLine}");
        w.Flush();
        // El bucle deja el archivo un poco por debajo; la entrada final lo empuja al umbral.
        while (w.BaseStream.Length < HistoryRotation.MaxBytes) w.Write('x');
    }

    [Fact]
    public void LogTo_BelowThreshold_DoesNotRotate()
    {
        _history.Log("FORMAT OK G:");
        _history.Log("VERIFY OK G:");

        Assert.False(File.Exists(Previous));
        Assert.Equal(2, HistoryEntry.ParseAll(_history.ReadLines()).Count);
    }

    [Fact]
    public void LogTo_OverThreshold_RotatesAndKeepsTheActiveFileSmall()
    {
        FillToThreshold("VIEJA");

        _history.Log("NUEVA");

        Assert.True(File.Exists(Previous), "La generación anterior debería estar en history.1.log.");
        Assert.True(new FileInfo(_log).Length < HistoryRotation.MaxBytes,
            "Se rota ANTES de escribir: el archivo activo no debe quedar por encima del umbral.");
        Assert.Contains("NUEVA", File.ReadAllText(_log), StringComparison.Ordinal);
        Assert.Contains("VIEJA", File.ReadAllText(Previous), StringComparison.Ordinal);
    }

    /// <summary>
    /// Lo que hace que la rotación sea aceptable: el visor lee las dos generaciones, así que rotar no
    /// convierte el historial en una pantalla casi vacía justo después de la entrada que lo provocó.
    /// </summary>
    [Fact]
    public void ReadLinesFrom_AfterRotation_StillShowsTheOlderEntries()
    {
        FillToThreshold("VIEJA");
        _history.Log("NUEVA");

        var entries = HistoryEntry.ParseAll(_history.ReadLines());

        Assert.Contains(entries, e => e.Detail.Contains("VIEJA", StringComparison.Ordinal));
        Assert.Contains(entries, e => e.Detail.Contains("NUEVA", StringComparison.Ordinal));

        // La vieja primero: el visor invierte el orden para mostrar lo más reciente arriba.
        int vieja = entries.ToList().FindIndex(e => e.Detail.Contains("VIEJA", StringComparison.Ordinal));
        int nueva = entries.ToList().FindIndex(e => e.Detail.Contains("NUEVA", StringComparison.Ordinal));
        Assert.True(vieja < nueva, "Las entradas rotadas deben leerse antes que las actuales.");
    }

    /// <summary>Dos generaciones es el tope: la tercera rotación se lleva por delante la más vieja.</summary>
    [Fact]
    public void LogTo_RotatingTwice_KeepsOnlyTwoGenerations()
    {
        FillToThreshold("PRIMERA");
        _history.Log("SEGUNDA");
        FillToThreshold("TERCERA");
        _history.Log("CUARTA");

        Assert.Equal(2, Directory.GetFiles(_dir).Length);
        Assert.DoesNotContain("PRIMERA", File.ReadAllText(Previous), StringComparison.Ordinal);
        Assert.Contains("TERCERA", File.ReadAllText(Previous), StringComparison.Ordinal);
    }

    /// <summary>
    /// «Borrar el historial» tiene que borrarlo entero. Si la generación rotada sobreviviera, el visor
    /// seguiría mostrando 2 MB de entradas justo después de que el usuario pidiera limpiarlo.
    /// </summary>
    [Fact]
    public void ClearAt_AlsoRemovesTheRotatedGeneration()
    {
        FillToThreshold("VIEJA");
        _history.Log("NUEVA");

        _history.Clear();

        Assert.False(File.Exists(Previous));
        Assert.Empty(HistoryEntry.ParseAll(_history.ReadLines()));
    }

    /// <summary>El registro es defensivo: una ruta imposible no puede tumbar la operación que registra.</summary>
    [Fact]
    public void LogTo_UnusablePath_DoesNotThrow()
    {
        string impossible = Path.Combine(_dir, "no\0valido", "history.log");

        var broken = new History(impossible);

        broken.Log("FORMAT OK G:");           // no debe lanzar
        Assert.Empty(broken.ReadLines());      // ni al leerlo
    }
}
