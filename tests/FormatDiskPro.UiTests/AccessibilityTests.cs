// SPDX-License-Identifier: GPL-3.0-only
using FlaUI.Core.AutomationElements;
using FlaUI.Core.Definitions;
using FlaUI.Core.Tools;

namespace FormatDiskPro.UiTests;

/// <summary>
/// Accesibilidad de la ventana principal, medida sobre la app REAL a través de UI Automation — que es la
/// misma superficie por la que la lee un lector de pantalla.
///
/// <para><b>Por qué estas pruebas y no una inspección a ojo:</b> lo que se afirma aquí (una región activa,
/// un error vinculado a su campo) no se ve en una captura ni se nota usando la app con ratón. Se declara
/// en el XAML/code-behind y, si alguien lo quita en un refactor, no se rompe nada visible — se rompe en
/// silencio, justo para los usuarios que menos van a reportarlo.</para>
///
/// <para>No necesitan la USB de pruebas: solo la ventana principal recién abierta.</para>
/// </summary>
[Collection(AppCollection.Name)]
public sealed class AccessibilityTests(AppFixture fixture)
{
    private Window Window => fixture.MainWindow;

    /// <summary>
    /// <c>StatusText</c> cambia durante operaciones de minutos u horas sin que nada mueva el foco. Como
    /// región activa <b>Polite</b>, un lector de pantalla puede seguir el progreso sin interrumpir; los
    /// hitos (inicio/fin/error/cancelación) se anuncian además con <c>RaiseNotificationEvent</c>, que no
    /// deja rastro consultable en el árbol UIA y por eso no se comprueba aquí.
    /// </summary>
    [Fact]
    public void StatusText_IsAPoliteLiveRegion()
    {
        // Desde `T13-10` el pie en reposo no ocupa sitio: la fila de estado aparece cuando hay algo que
        // decir. Hay que provocar un mensaje primero — igual que la prueba del error de etiqueta tiene
        // que provocar el error—, porque un elemento Collapsed no está en el árbol de automatización.
        // Aplicar un preset es lo más inocuo que escribe estado: solo mueve los controles de formato.
        MainWindowActions.ClickMenuPath(Window, "MnuConfig", "MnuPresets", "preset.builtin.windowsData");

        var status = Retry.WhileNull(
            () => Window.FindFirstDescendant(cf => cf.ByAutomationId("StatusText")),
            timeout: TimeSpan.FromSeconds(5),
            interval: TimeSpan.FromMilliseconds(200),
            ignoreException: true).Result
            ?? throw new InvalidOperationException(
                "Aplicar un preset no hizo aparecer StatusText: el pie no está diciendo nada.");

        Assert.True(status.FrameworkAutomationElement.LiveSetting.TryGetValue(out LiveSetting live),
            "StatusText no expone LiveSetting: la barra de estado ha dejado de ser una región activa.");
        Assert.Equal(LiveSetting.Polite, live);
    }

    /// <summary>
    /// El mensaje de error de la etiqueta es <b>Assertive</b> —no es información de fondo, es lo que
    /// impide continuar— y va vinculado al campo por <c>DescribedBy</c>, que es lo que hace que se lea
    /// <b>sin salir del cuadro de texto</b>. Antes aparecía debajo, sin ninguna relación programática:
    /// desde el campo no había forma de saber por qué no dejaba seguir.
    ///
    /// <para><b>Hay que provocar el error primero</b>, y esto lo descubrió esta misma prueba al fallar:
    /// un elemento <c>Collapsed</c> <b>no está en el árbol de UI Automation</b>, así que
    /// <c>LabelErrorText</c> sencillamente no existe mientras la etiqueta es válida. Que el vínculo solo
    /// tenga efecto mientras el error se muestra es justo lo que se quiere, pero conviene tenerlo escrito:
    /// una prueba que buscara el control sin provocar el error fallaría sin que nada estuviera mal.</para>
    ///
    /// <para><c>*</c> no es un carácter válido en ninguno de los cinco sistemas de archivos.</para>
    /// </summary>
    [Fact]
    public void InvalidLabel_ShowsAnAssertiveErrorLinkedToTheField()
    {
        var box = MainWindowActions.TextBox(Window, "VolumeLabelBox");
        string original = box.Text;

        try
        {
            box.Text = "no*valido";

            // TextChanged -> UpdateLabelHint pasa por el dispatcher, y el elemento aparece en el árbol UIA
            // al hacerse visible: se espera a que exista en vez de leerlo una sola vez.
            var error = Retry.WhileNull(
                () => Window.FindFirstDescendant(cf => cf.ByAutomationId("LabelErrorText")),
                timeout: TimeSpan.FromSeconds(5),
                interval: TimeSpan.FromMilliseconds(200),
                ignoreException: true).Result
                ?? throw new InvalidOperationException(
                    "Una etiqueta inválida no hizo aparecer LabelErrorText: sin mensaje no hay nada que anunciar.");

            Assert.False(string.IsNullOrWhiteSpace(error.Name),
                "LabelErrorText apareció vacío: DescribedBy apuntaría a un texto sin contenido.");

            Assert.True(error.FrameworkAutomationElement.LiveSetting.TryGetValue(out LiveSetting live),
                "LabelErrorText no expone LiveSetting: el error de etiqueta ha dejado de ser una región activa.");
            Assert.Equal(LiveSetting.Assertive, live);

            Assert.True(box.FrameworkAutomationElement.DescribedBy.TryGetValue(out AutomationElement[]? describedBy),
                "VolumeLabelBox no expone DescribedBy: el error volvería a ser invisible desde el campo.");
            Assert.NotNull(describedBy);
            Assert.Contains(describedBy, e => e.AutomationId == "LabelErrorText");
        }
        finally
        {
            box.Text = original;
        }
    }

    /// <summary>
    /// `T13-08`: las pistas de los dos combos de formato tampoco llegaban al lector de pantalla. Están
    /// debajo del control y en gris, sin relación programática: desde el combo no había forma de saber
    /// que existen. La de <c>AllocUnitPicker</c> es la que `T7-03` añadió por ser «el único campo
    /// esotérico sin ayuda», y hasta ahora solo ayudaba a quien ve.
    ///
    /// <para>Se comprueba también que el texto no está vacío: un <c>DescribedBy</c> que apunta a un
    /// control sin contenido es un vínculo que no dice nada, y pasaría igual.</para>
    /// </summary>
    [Theory]
    [InlineData("FileSystemPicker", "FsDescText")]
    [InlineData("AllocUnitPicker",  "AllocHintText")]
    public void FieldHints_AreLinkedToTheirControl(string controlId, string hintId)
    {
        var control = MainWindowActions.Require(Window, controlId);
        var hint = MainWindowActions.Require(Window, hintId);

        Assert.False(string.IsNullOrWhiteSpace(hint.Name),
            $"{hintId} está vacío: el vínculo apuntaría a un texto que no dice nada.");

        Assert.True(control.FrameworkAutomationElement.DescribedBy.TryGetValue(out AutomationElement[]? describedBy),
            $"{controlId} no expone DescribedBy: su pista vuelve a existir solo para quien la ve.");
        Assert.NotNull(describedBy);
        Assert.Contains(describedBy, e => e.AutomationId == hintId);
    }

    // ── AUD-003: el selector de unidad ────────────────────────────

    /// <summary>
    /// El control desde el que se elige <b>qué se va a formatear</b> tiene nombre, y cada unidad se
    /// anuncia con el suyo.
    /// </summary>
    /// <remarks>
    /// Era el único desplegable sin nombre accesible, y todas sus opciones se anunciaban con el nombre
    /// del tipo («FormatDiskPro.UI.DriveViewModel»): con un lector de pantalla, el pendrive y el disco
    /// de Windows sonaban igual. Se compara el nombre de cada opción con el texto que <b>se ve</b> en
    /// ella, que es lo que tiene que decir.
    /// </remarks>
    [Fact]
    public void DrivePicker_HasANameAndEveryDriveIsAnnouncedByItsOwn()
    {
        var picker = MainWindowActions.DrivePicker(Window);
        Assert.False(string.IsNullOrWhiteSpace(picker.Name),
            "DrivePicker no tiene nombre accesible: se anuncia como «cuadro combinado» a secas.");

        picker.Expand();
        Thread.Sleep(200);
        try
        {
            var items = picker.Items;
            Assert.NotEmpty(items);
            Assert.All(items, item =>
            {
                string shown = item.FindFirstDescendant(cf => cf.ByControlType(ControlType.Text))?.Name ?? "";
                Assert.False(string.IsNullOrWhiteSpace(shown), "Una unidad no muestra ningún texto.");
                Assert.Equal(shown, item.Name);
                Assert.Matches("[A-Z]:", item.Name);
            });
            Assert.Equal(items.Length, items.Select(i => i.Name).Distinct().Count());
        }
        finally
        {
            picker.Collapse();
        }
    }

    /// <summary>
    /// Ningún control con el que se pueda interactuar en la ventana principal está sin nombre. Es el
    /// volcado de UI Automation de la auditoría convertido en prueba: lo que encuentre aquí es lo que
    /// un lector de pantalla anunciaría como «botón» o «cuadro combinado», sin más.
    /// </summary>
    [Fact]
    public void NoInteractiveControlInTheMainWindow_IsNameless()
    {
        ControlType[] interactive =
        [
            ControlType.Button, ControlType.ComboBox, ControlType.CheckBox, ControlType.Edit,
            ControlType.MenuItem, ControlType.RadioButton, ControlType.Slider, ControlType.Hyperlink,
            ControlType.SplitButton, ControlType.ListItem, ControlType.TabItem,
        ];

        var nameless = Window.FindAllDescendants()
            .Where(e => interactive.Contains(e.ControlType))
            // Los botones de la barra de título los pone Windows, no la app, y se anuncian por su cuenta.
            .Where(e => e.Properties.FrameworkId.ValueOrDefault == "XAML")
            .Where(e => string.IsNullOrWhiteSpace(e.Name))
            .Select(e => $"{e.ControlType} '{e.AutomationId}'")
            .ToList();

        Assert.True(nameless.Count == 0, "Controles sin nombre accesible: " + string.Join(", ", nameless));
    }

    // ── AUD-014: las regiones activas avisan ──────────────────────

    /// <summary>
    /// Ejecuta <paramref name="act"/> escuchando los avisos de región activa bajo
    /// <paramref name="scope"/>, y devuelve los AutomationId de los elementos que avisaron.
    /// </summary>
    /// <remarks>
    /// Es lo que un lector de pantalla escucha. <c>LiveSetting</c> en el árbol solo dice que la región
    /// <i>está declarada</i>; que hable depende de este evento, y eso no se ve inspeccionando nada.
    /// </remarks>
    private List<string> LiveRegionAnnouncementsDuring(AutomationElement scope, Action act)
    {
        var announced = new List<string>();
        using var handler = scope.RegisterAutomationEvent(
            fixture.Automation.EventLibrary.Element.LiveRegionChangedEvent,
            TreeScope.Subtree,
            (element, _) =>
            {
                string id;
                try { id = element.AutomationId; } catch { id = "?"; }
                lock (announced) announced.Add(id);
            });

        act();

        Retry.WhileFalse(
            () => { lock (announced) return announced.Count > 0; },
            timeout: TimeSpan.FromSeconds(5), interval: TimeSpan.FromMilliseconds(100));
        lock (announced) return [.. announced];
    }

    /// <summary>
    /// Filtrar el historial <b>dice</b> cuántas entradas quedan. El recuento estaba declarado como región
    /// activa y nunca avisaba: quien filtraba sin ver la lista no sabía si quedaban tres o ninguna.
    /// </summary>
    [Fact]
    public void FilteringTheHistory_AnnouncesTheNewCount()
    {
        // Con el historial vacío el recuento no se muestra. Se garantiza una entrada escribiéndola
        // donde la app la lee; SettingsBackup devuelve el archivo a su estado al terminar la suite.
        string log = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "FormatDiskPro", "history.log");
        File.AppendAllText(log, $"{DateTime.Now:yyyy-MM-dd HH:mm:ss}\tEJECT Z:{Environment.NewLine}");

        MainWindowActions.ClickMenuPath(Window, "MnuTools", "MnuHistory");
        var dialog = DialogHelper.WaitForDialogContaining(fixture, "ExportButton");
        try
        {
            var count = DialogHelper.WaitForChild(dialog, "CountText");
            Assert.True(count.FrameworkAutomationElement.LiveSetting.TryGetValue(out LiveSetting live));
            Assert.Equal(LiveSetting.Polite, live);
            string before = count.Name;

            var search = DialogHelper.WaitForChild(dialog, "SearchBox")
                .FindFirstDescendant(cf => cf.ByControlType(ControlType.Edit))
                ?? throw new InvalidOperationException("El cuadro de búsqueda no tiene campo de texto.");

            var announced = LiveRegionAnnouncementsDuring(dialog,
                () => search.AsTextBox().Text = "esto-no-esta-en-ningun-historial");

            Assert.Contains("CountText", announced);
            Assert.NotEqual(before, DialogHelper.WaitForChild(dialog, "CountText").Name);
        }
        finally
        {
            DialogHelper.SafeCloseAnyDialog(fixture);
        }
    }

    /// <summary>
    /// Guardar un preset con un nombre que ya existe <b>dice</b> por qué no se guarda. El mensaje
    /// aparecía en rojo bajo el campo y nada más: para un lector de pantalla, pulsar «Guardar» no hacía
    /// nada.
    /// </summary>
    [NonSystemDriveFact]
    public void RejectingADuplicatePresetName_AnnouncesTheError()
    {
        MainWindowActions.SelectAnyNonSystemDrive(Window);
        MainWindowActions.ClickMenuPath(Window, "MnuConfig", "MnuPresets");
        var manage = Retry.WhileNull(
            () => Window.FindAllDescendants(cf => cf.ByControlType(ControlType.MenuItem))
                .FirstOrDefault(mi => mi.Name == "Gestionar presets…"),
            timeout: TimeSpan.FromSeconds(5), interval: TimeSpan.FromMilliseconds(200),
            ignoreException: true).Result
            ?? throw new InvalidOperationException("No se encontró «Gestionar presets…».");
        manage.Patterns.Invoke.Pattern.Invoke();

        var dialog = DialogHelper.WaitForDialogContaining(fixture, "SaveHeader");
        try
        {
            string name = "UITEST-" + Guid.NewGuid().ToString("N")[..8];
            var nameBox = DialogHelper.WaitForChild(dialog, "NameBox").AsTextBox();
            var save = DialogHelper.WaitForChild(dialog, "SaveBtn").AsButton();

            nameBox.Text = name;
            save.Invoke();                       // la primera vez se guarda
            Thread.Sleep(300);
            nameBox.Text = name;

            var announced = LiveRegionAnnouncementsDuring(dialog, save.Invoke);   // la segunda, se rechaza

            Assert.Contains("ErrorText", announced);

            var error = DialogHelper.WaitForChild(dialog, "ErrorText");
            Assert.False(string.IsNullOrWhiteSpace(error.Name));
            Assert.True(error.FrameworkAutomationElement.LiveSetting.TryGetValue(out LiveSetting live));
            Assert.Equal(LiveSetting.Assertive, live);
            Assert.True(nameBox.FrameworkAutomationElement.DescribedBy.TryGetValue(out AutomationElement[]? describedBy));
            Assert.Contains(describedBy!, e => e.AutomationId == "ErrorText");
        }
        finally
        {
            DialogHelper.SafeCloseAnyDialog(fixture);
        }
    }
}
