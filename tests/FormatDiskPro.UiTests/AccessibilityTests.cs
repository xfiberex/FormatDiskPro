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
}
