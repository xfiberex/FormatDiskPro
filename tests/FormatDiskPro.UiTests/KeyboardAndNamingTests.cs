using FlaUI.Core.AutomationElements;
using FlaUI.Core.Definitions;
using FlaUI.Core.Input;
using FlaUI.Core.WindowsAPI;

namespace FormatDiskPro.UiTests;

/// <summary>
/// Lo que `T7-06` fue a averiguar y ahora queda fijado: que las listas se puedan recorrer y desplazar
/// <b>solo con el teclado</b>, y que lo que anuncian tenga sentido para quien no las ve.
///
/// <para>Salieron de una sonda que no afirmaba nada, solo contaba lo que veía. Dos de sus hallazgos eran
/// defectos —las filas se anunciaban con el <c>ToString()</c> del record («HistoryRow { Time = …,
/// Accent = Microsoft.UI.Xaml.Media.SolidColorBrush }») y los dos filtros del historial no decían qué
/// filtraban—, y estas pruebas son lo que impide que vuelvan.</para>
///
/// <para><b>Verificadas por reversión</b>, y el resultado reparte: quitando los arreglos, las tres de
/// <i>nombres</i> fallan y las dos de <i>teclado</i> siguen en verde. Es lo correcto —esas dos no prueban
/// ningún arreglo nuestro: fijan que WinUI deja recorrer y desplazar una lista con
/// <c>SelectionMode="None"</c>, que era la sospecha de `T7-06` y resultó infundada—.</para>
/// </summary>
[Collection(AppCollection.Name)]
public sealed class KeyboardAndNamingTests(AppFixture fixture)
{
    private Window Window => fixture.MainWindow;

    private Window OpenHistory()
    {
        Window.Focus();
        Keyboard.TypeSimultaneously(VirtualKeyShort.CONTROL, VirtualKeyShort.KEY_H);
        var dialog = DialogHelper.WaitForDialog(fixture);
        Thread.Sleep(400);
        return dialog;
    }

    private AutomationElement? Focused()
    {
        try { return fixture.Automation.FocusedElement(); } catch { return null; }
    }

    /// <summary>
    /// El foco arranca en el buscador y, tabulando, se llega a la lista: tres pasos (los dos filtros y
    /// la fila). Con `SelectionMode="None"` cabía esperar que los ítems no fueran alcanzables — lo son.
    /// </summary>
    [HistoryFilledFact]
    public void HistoryList_IsReachableWithTheKeyboardAlone()
    {
        OpenHistory();
        try
        {
            Assert.Equal("TextBox", Focused()?.ClassName);   // el AutoSuggestBox de T7-05

            for (int i = 0; i < 3; i++) { Keyboard.Press(VirtualKeyShort.TAB); Thread.Sleep(250); }

            var focused = Focused();
            Assert.NotNull(focused);
            Assert.Equal(ControlType.ListItem, focused!.ControlType);
        }
        finally
        {
            DialogHelper.SafeCloseAnyDialog(fixture);
        }
    }

    /// <summary>Y desde ahí, las flechas y AvPág la desplazan de verdad: sin ratón se ve todo.</summary>
    [HistoryFilledFact]
    public void HistoryList_ScrollsWithArrowsAndPageDown()
    {
        var dialog = OpenHistory();
        try
        {
            var list = DialogHelper.WaitForChild(dialog, "EntriesList");
            var scroll = list.Patterns.Scroll;
            Assert.True(scroll.IsSupported, "La lista del historial no expone ScrollPattern.");

            if (!scroll.Pattern.VerticallyScrollable)
                return;   // caben todas: no hay desplazamiento que comprobar

            double before = scroll.Pattern.VerticalScrollPercent;
            for (int i = 0; i < 3; i++) { Keyboard.Press(VirtualKeyShort.TAB); Thread.Sleep(250); }
            for (int i = 0; i < 6; i++) { Keyboard.Press(VirtualKeyShort.DOWN); Thread.Sleep(120); }
            Keyboard.Press(VirtualKeyShort.NEXT);
            Thread.Sleep(400);

            Assert.True(scroll.Pattern.VerticalScrollPercent > before,
                $"La lista no se desplazó con el teclado (sigue en {scroll.Pattern.VerticalScrollPercent} %).");
        }
        finally
        {
            DialogHelper.SafeCloseAnyDialog(fixture);
        }
    }

    /// <summary>
    /// Una fila del historial se anuncia por lo que dice, no por el volcado del record. El fallo real
    /// incluía hasta el pincel del acento: «Glyph = , Accent = Microsoft.UI.Xaml.Media.SolidColorBrush».
    /// </summary>
    [HistoryFilledFact]
    public void HistoryRows_AreNotAnnouncedAsRecordDumps()
    {
        var dialog = OpenHistory();
        try
        {
            var list = DialogHelper.WaitForChild(dialog, "EntriesList");
            var first = list.FindAllChildren().FirstOrDefault();
            Assert.NotNull(first);

            string name = first!.Name ?? "";
            Assert.False(string.IsNullOrWhiteSpace(name), "La fila del historial no tiene nombre accesible.");
            Assert.DoesNotContain("HistoryRow", name, StringComparison.Ordinal);
            Assert.DoesNotContain("SolidColorBrush", name, StringComparison.Ordinal);
        }
        finally
        {
            DialogHelper.SafeCloseAnyDialog(fixture);
        }
    }

    /// <summary>Los dos filtros dicen qué filtran: sin nombre se anunciaban como «cuadro combinado».</summary>
    [Fact]
    public void HistoryFilters_SayWhatTheyFilter()
    {
        var dialog = OpenHistory();
        try
        {
            foreach (string id in (string[])["CategoryFilter", "ResultFilter"])
            {
                var combo = DialogHelper.WaitForChild(dialog, id);
                Assert.False(string.IsNullOrWhiteSpace(combo.Name), $"'{id}' no expone nombre accesible.");
            }
        }
        finally
        {
            DialogHelper.SafeCloseAnyDialog(fixture);
        }
    }

    /// <summary>
    /// La fila de un preset se llama como el preset, y el flyout de borrado de `T7-01` funciona con el
    /// teclado: al abrirlo el foco cae en el botón que confirma y la pregunta nombra lo que se va a
    /// perder. Crea su propio preset y lo borra al terminar — <c>AppFixture</c> restaura
    /// <c>settings.json</c> igualmente.
    /// </summary>
    [Fact]
    public void PresetRow_IsNamedAfterThePreset_AndItsDeleteFlyoutIsUsableWithTheKeyboard()
    {
        const string name = "T706 prueba";
        try
        {
            MainWindowActions.ClickMenuPath(Window, "MnuConfig", "MnuPresets");
            var manage = FlaUI.Core.Tools.Retry.WhileNull(
                () => Window.FindAllDescendants(cf => cf.ByControlType(ControlType.MenuItem))
                        .FirstOrDefault(mi => mi.Name == "Gestionar presets…"),
                timeout: TimeSpan.FromSeconds(5), interval: TimeSpan.FromMilliseconds(200), ignoreException: true);
            Assert.NotNull(manage.Result);
            manage.Result!.Patterns.Invoke.Pattern.Invoke();

            var dialog = DialogHelper.WaitForDialogContaining(fixture, "SaveHeader");
            DialogHelper.WaitForChild(dialog, "NameBox").AsTextBox().Text = name;
            Thread.Sleep(200);
            DialogHelper.WaitForChild(dialog, "SaveBtn").Patterns.Invoke.Pattern.Invoke();
            Thread.Sleep(600);

            var row = DialogHelper.WaitForChild(dialog, "PresetsList").FindAllChildren().FirstOrDefault();
            Assert.NotNull(row);
            Assert.Equal(name, row!.Name);

            DialogHelper.WaitForChild(dialog, "DeletePresetBtn").Patterns.Invoke.Pattern.Invoke();
            Thread.Sleep(700);

            var question = Window.FindFirstDescendant(cf => cf.ByAutomationId("DeleteConfirmText"));
            Assert.NotNull(question);
            Assert.Contains(name, question!.Name, StringComparison.Ordinal);

            Assert.Equal("DeleteConfirmBtn", Focused()?.AutomationId);

            Window.FindFirstDescendant(cf => cf.ByAutomationId("DeleteConfirmBtn"))!
                  .Patterns.Invoke.Pattern.Invoke();
            Thread.Sleep(600);

            Assert.Null(DialogHelper.WaitForDialogContaining(fixture, "SaveHeader")
                        .FindFirstDescendant(cf => cf.ByAutomationId("PresetsList")));
        }
        finally
        {
            DialogHelper.SafeCloseAnyDialog(fixture);
        }
    }
}
