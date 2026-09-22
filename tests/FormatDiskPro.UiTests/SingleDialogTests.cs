using FlaUI.Core.AutomationElements;

namespace FormatDiskPro.UiTests;

/// <summary>
/// WinUI solo admite <b>un</b> <c>ContentDialog</c> abierto a la vez: abrir el segundo lanza
/// <c>COMException 0x80000019</c>. Estas pruebas comprueban que la ventana no lo intenta nunca
/// (`T13-19`).
/// </summary>
/// <remarks>
/// <para><b>De dónde sale.</b> Del historial real del mantenedor: el 2026-09-18, en mitad de una tanda de
/// capturas, quedó un <c>CRASH: COMException (0x80000019) … Only a single ContentDialog can be open at any
/// time</c> con la pila en <c>ContentDialog.ShowAsync</c> ← <c>StartButton_Click</c>. La app <b>no</b> se
/// cierra —la red global de <c>App</c> lo atrapa—, así que el único rastro es esa línea del historial: por
/// eso es lo que se comprueba aquí y no «la ventana sigue viva», que también lo estaba antes.</para>
///
/// <para><b>Con el ratón no se llega</b>, porque el diálogo modal tapa la ventana. Por UI Automation sí, y
/// también si otro diálogo se abre por su cuenta (actualización, novedades) justo mientras el usuario
/// pulsa — que es lo que le pasó al mantenedor.</para>
/// </remarks>
[Collection(AppCollection.Name)]
public sealed class SingleDialogTests
{
    private readonly AppFixture _fixture;
    private Window Window => _fixture.MainWindow;

    public SingleDialogTests(AppFixture fixture)
    {
        _fixture = fixture;
        MainWindowActions.SelectAnyNonSystemDrive(Window);
    }

    private static string HistoryPath => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "FormatDiskPro", "history.log");

    private static string ReadHistory() =>
        File.Exists(HistoryPath) ? File.ReadAllText(HistoryPath) : "";

    [NonSystemDriveFact]
    public void StartButtonTwice_DoesNotOpenASecondDialog()
    {
        int crashesBefore = CountCrashes(ReadHistory());

        var start = MainWindowActions.Button(Window, "StartButton");
        try
        {
            // Dos invocaciones seguidas SIN esperar: la primera abre la confirmación y la segunda llega
            // con ella abierta. Es lo que un clic de ratón no puede hacer y la automatización sí.
            start.Patterns.Invoke.Pattern.Invoke();
            start.Patterns.Invoke.Pattern.Invoke();
            Thread.Sleep(2500);

            // Un solo diálogo en el árbol, y la ventana responde.
            var dialog = DialogHelper.WaitForDialog(_fixture, TimeSpan.FromSeconds(10));
            Assert.NotNull(dialog);
        }
        finally
        {
            DialogHelper.SafeCloseAnyDialog(_fixture);
        }

        Assert.Equal(crashesBefore, CountCrashes(ReadHistory()));
    }

    /// <summary>Líneas <c>CRASH</c> del historial: es el rastro que deja la red global de <c>App</c>.</summary>
    private static int CountCrashes(string history) =>
        history.Split('\n').Count(l => l.Contains("CRASH", StringComparison.Ordinal));
}
