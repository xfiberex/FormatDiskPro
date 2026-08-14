using FlaUI.Core.AutomationElements;

namespace FormatDiskPro.UiTests;

/// <summary>
/// El criterio de aceptación de `T0-01` y `T0-02`, por fin ejercitado: **la unidad desaparece a mitad de
/// la operación**.
///
/// Hasta ahora, de esos <c>catch</c> estaba bajo test lo que <i>escriben</i> (`T2-05`,
/// <c>OperationFailure.LogLine</c>), no que lleguen a <i>ejecutarse</i>. Un <c>catch</c> que nadie ha
/// visto ejecutarse no es una red: es una suposición.
///
/// <para><b>Qué se comprueba, y por qué cada cosa.</b></para>
/// <list type="number">
/// <item>La app <b>sigue viva</b> — que es exactamente lo que no ocurría antes de `T0-01`.</item>
/// <item>Muestra un <b>diálogo de error</b> en vez de morir en silencio.</item>
/// <item>Deja la línea en <c>history.log</c>, y <b>en una sola entrada</b> pese a que el mensaje de la
///       excepción pueda traer saltos de línea (`T3-11`).</item>
/// <item>El historial <b>NO</b> contiene <c>CRASH:</c>. Esto es lo que distingue "el <c>catch</c> del
///       handler hizo su trabajo" de "se escapó hasta la red global de `T0-01`". Sin esta comprobación,
///       la prueba pasaría igual con los <c>catch</c> borrados, y no probaría lo que dice probar.</item>
/// <item>La app vuelve a <b>estado ocioso</b>: el botón Iniciar se rehabilita.</item>
/// </list>
///
/// Se salta salvo <c>FORMATDISKPRO_ALLOW_YANK=1</c>. Ver <see cref="DriveYank"/> para por qué se desmonta
/// por software en vez de tirar del cable.
/// </summary>
[Collection(AppCollection.Name)]
public sealed class OperationErrorTests(AppFixture fixture)
{
    private Window Window => fixture.MainWindow;

    /// <summary>Margen para que la operación arranque y esté escribiendo de verdad antes del desmontaje.</summary>
    private static readonly TimeSpan WriteWarmUp = TimeSpan.FromSeconds(8);

    /// <summary>Tras perder la unidad, el fallo debe aflorar en la UI en un tiempo razonable.</summary>
    private static readonly TimeSpan FailureTimeout = TimeSpan.FromMinutes(2);

    private static string HistoryPath => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "FormatDiskPro", "history.log");

    private static string ReadHistory() =>
        File.Exists(HistoryPath) ? File.ReadAllText(HistoryPath) : "";

    [YankFact]
    public void VerifyCapacity_DriveDisappears_AppSurvivesAndReportsTheError()
        => AssertSurvivesLosingTheDrive("MnuVerify", "VERIFY ERROR");

    [YankFact]
    public void Benchmark_DriveDisappears_AppSurvivesAndReportsTheError()
        => AssertSurvivesLosingTheDrive("MnuBenchmark", "BENCH ERROR");

    /// <summary>
    /// Arranca la operación <paramref name="menuId"/> sobre la USB, desmonta el disco a media faena y
    /// exige que la app lo cuente en vez de morirse.
    /// </summary>
    private void AssertSurvivesLosingTheDrive(string menuId, string expectedLogPrefix)
    {
        char letter = TestDrive.RequireLetter(TestDrive.PrimaryLabel);
        Assert.True(MainWindowActions.SelectDriveByLetter(Window, letter),
            $"La unidad de pruebas ({letter}:) no aparece en el selector.");

        string historyBefore = ReadHistory();
        bool dismounted = false;

        try
        {
            MainWindowActions.ClickMenuPath(Window, "MnuTools", menuId);

            var confirm = DialogHelper.WaitForDialog(fixture);
            DialogHelper.PrimaryButton(confirm).Invoke();
            DialogHelper.WaitForNoDialog(fixture);

            Thread.Sleep(WriteWarmUp);          // que esté escribiendo de verdad, no arrancando

            DriveYank.ForceDismount(letter);
            dismounted = true;

            // 1 y 2: sigue viva y lo cuenta. Si la app hubiera muerto, aquí no habría ningún diálogo.
            var error = DialogHelper.WaitForDialog(fixture, FailureTimeout);
            string shown = DialogHelper.ReadText(error);
            DialogHelper.SafeCloseAnyDialog(fixture);

            Assert.False(string.IsNullOrWhiteSpace(shown), "El diálogo de error no muestra ningún texto.");

            // 3: la línea de historial, y en UNA sola entrada.
            string added = ReadHistory()[historyBefore.Length..];
            Assert.Contains(expectedLogPrefix, added);

            string[] errorLines = [.. added.Split('\n', StringSplitOptions.RemoveEmptyEntries)
                .Where(l => l.Contains(expectedLogPrefix, StringComparison.Ordinal))];
            Assert.Single(errorLines);

            // 4: lo atrapó el handler, NO la red global. Sin esto la prueba pasaría con los catch borrados.
            Assert.DoesNotContain("CRASH:", added);

            // 5: vuelve a estado ocioso. Se miran DrivePicker y MnuTools, no StartButton: este último es
            // `enabled && !_isDriveProtected` (SetFormEnabled), y al desaparecer la USB el selector puede
            // caer en C:, que está protegida — quedaría deshabilitado con la app perfectamente ociosa.
            MainWindowActions.WaitUntilEnabled(Window, "DrivePicker", TimeSpan.FromSeconds(15));
            MainWindowActions.WaitUntilEnabled(Window, "MnuTools", TimeSpan.FromSeconds(15));
        }
        finally
        {
            // Nunca dejar la unidad desmontada ni la app ocupada: la primera versión de esta prueba abortó
            // con el benchmark todavía corriendo y la SIGUIENTE falló con un DrivePicker vacío, que es un
            // síntoma que no se parece en nada a su causa.
            DialogHelper.SafeCloseAnyDialog(fixture);
            ReturnToIdle();

            if (dismounted)
                DriveYank.WaitForRemount(TestDrive.PrimaryLabel, TimeSpan.FromSeconds(60));
        }
    }

    /// <summary>
    /// Devuelve la app a estado ocioso pase lo que pase. Si hay una operación en curso, el botón de cierre
    /// del pie actúa de <i>Cancelar</i> (<c>EndOperation</c> lo devuelve a "Cerrar" al terminar).
    /// </summary>
    private void ReturnToIdle()
    {
        try
        {
            if (MainWindowActions.Require(Window, "MnuTools").IsEnabled) return;

            MainWindowActions.Require(Window, "CloseButton").AsButton().Invoke();
            DialogHelper.SafeCloseAnyDialog(fixture);
            MainWindowActions.WaitUntilEnabled(Window, "MnuTools", TimeSpan.FromSeconds(60));
        }
        catch
        {
            // Mejor esfuerzo: no tapar el resultado real de la prueba con un fallo de la limpieza.
        }
    }
}
