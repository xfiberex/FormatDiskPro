using FlaUI.Core.AutomationElements;
using FlaUI.Core.Definitions;
using FlaUI.Core.Tools;

namespace FormatDiskPro.UiTests;

/// <summary>
/// Acciones reutilizables sobre la ventana principal, en base a los AutomationId (= x:Name del
/// XAML de <c>MainWindow.xaml</c>) ya verificados contra el .exe real.
/// </summary>
public static class MainWindowActions
{
    public static AutomationElement Require(Window window, string automationId) =>
        window.FindFirstDescendant(cf => cf.ByAutomationId(automationId))
            ?? throw new InvalidOperationException($"No se encontró el control '{automationId}'.");

    /// <summary>
    /// Espera a que un control se habilite. Tras cerrar el ContentDialog de resultado de una operación
    /// (Formatear/Reinicializar), <c>EndOperation()</c> — que rehabilita los controles vía
    /// <c>SetFormEnabled(true)</c> — corre en el <c>finally</c> DESPUÉS de que el <c>await</c> de
    /// <c>ShowInfoAsync</c> se resuelva; hay un margen (dispatcher) entre "el diálogo ya no está en el
    /// árbol" (lo que <see cref="DialogHelper.WaitForNoDialog"/> confirma) y "el finally ya se ejecutó"
    /// — confirmado contra hardware real con <c>SmallFat32Check</c> tras Reinicializar.
    /// </summary>
    public static void WaitUntilEnabled(Window window, string automationId, TimeSpan? timeout = null)
    {
        var result = Retry.WhileFalse(
            () => { try { return Require(window, automationId).IsEnabled; } catch { return false; } },
            timeout: timeout ?? TimeSpan.FromSeconds(5),
            interval: TimeSpan.FromMilliseconds(200),
            ignoreException: true);

        if (!result.Success)
            throw new InvalidOperationException($"El control '{automationId}' no se habilitó a tiempo.");
    }

    public static ComboBox DrivePicker(Window window) => Require(window, "DrivePicker").AsComboBox();

    /// <summary>
    /// Texto visible de un ítem del DrivePicker. El ComboBox está enlazado a <c>DriveViewModel</c> vía
    /// un DataTemplate (icono + TextBlock con <c>DisplayText</c>): la propiedad <c>Text</c>/<c>Name</c>
    /// del propio ComboBoxItem no refleja ese contenido — devuelve el <c>ToString()</c> por defecto
    /// del objeto enlazado ("FormatDiskPro.UI.DriveViewModel"), confirmado contra hardware real. Hay
    /// que leer el TextBlock descendiente, igual que el patrón Text de <see cref="DialogHelper.ReadText"/>.
    /// </summary>
    private static string GetItemDisplayText(AutomationElement item) =>
        item.FindFirstDescendant(cf => cf.ByControlType(ControlType.Text))?.Name ?? item.Name;

    /// <summary>
    /// Extrae la letra de unidad de un DisplayText. La unidad de sistema/protegida se muestra como
    /// "[Protegido] C:" (prefijo confirmado contra hardware real) — asumir que la letra está en la
    /// posición 0 falla justo para ese caso (lee '[' en vez de 'C'). Se busca el primer "X:" real.
    /// </summary>
    private static char ExtractDriveLetter(string displayText)
    {
        for (int i = 0; i < displayText.Length - 1; i++)
            if (char.IsLetter(displayText[i]) && displayText[i + 1] == ':')
                return char.ToUpperInvariant(displayText[i]);
        throw new InvalidOperationException($"No se pudo extraer una letra de unidad de '{displayText}'.");
    }

    /// <summary>Diagnóstico: texto visible de cada ítem del DrivePicker, en el orden que reporta FlaUI.</summary>
    public static string DumpDriveItems(Window window)
    {
        var combo = DrivePicker(window);
        combo.Expand();
        Thread.Sleep(150);
        try
        {
            return string.Join(", ", combo.Items.Select((it, idx) => $"[{idx}]='{GetItemDisplayText(it)}'"));
        }
        finally
        {
            combo.Collapse();
        }
    }

    /// <summary>Letra (mayúscula) de la unidad actualmente seleccionada en el picker, o null si no hay selección.</summary>
    public static char? GetSelectedDriveLetter(Window window)
    {
        var selected = DrivePicker(window).SelectedItem;
        if (selected is null) return null;
        string text = GetItemDisplayText(selected);
        return string.IsNullOrEmpty(text) ? null : ExtractDriveLetter(text);
    }

    /// <summary>
    /// Selecciona en el picker la unidad cuya letra coincide; lanza con diagnóstico si no está en la
    /// lista. Un Formatear/Reinicializar real (o cualquier evento de dispositivo que dispare
    /// <c>HookDeviceNotifications</c>) hace que <c>LoadDrives()</c> vacíe y repueble el ComboBox de
    /// forma asíncrona — confirmado contra hardware real: una lectura única de <c>Items</c> justo tras
    /// cerrar el diálogo de resultado puede atrapar la ventana momentánea en la que está vacío. Se
    /// reintenta leyendo <c>Items</c> con el desplegable ya abierto (WinUI lo repuebla en vivo al estar
    /// enlazado a la misma ObservableCollection) en vez de una única comprobación.
    /// </summary>
    public static bool SelectDriveByLetter(Window window, char letter)
    {
        char wanted = char.ToUpperInvariant(letter);
        var combo = DrivePicker(window);
        combo.Expand();
        try
        {
            string lastSeen = "(ninguno)";
            var result = Retry.WhileNull(() =>
            {
                var items = combo.Items;
                lastSeen = items.Length == 0 ? "(ninguno)" : string.Join(", ", items.Select(i => $"'{GetItemDisplayText(i)}'"));
                return items.FirstOrDefault(i => ExtractDriveLetter(GetItemDisplayText(i)) == wanted);
            }, timeout: TimeSpan.FromSeconds(10), interval: TimeSpan.FromMilliseconds(300), ignoreException: true);

            if (result.Result is null)
                throw new InvalidOperationException(
                    $"El DrivePicker no ofrece ninguna unidad '{wanted}:'. Ítems vistos: {lastSeen}");

            result.Result.Select();
            return true;
        }
        finally
        {
            combo.Collapse();
        }
    }

    /// <summary>
    /// Selecciona la primera unidad del picker que NO sea la unidad de sistema (protegida: casi todos
    /// los controles de formato se deshabilitan sobre ella, per <c>MainWindow.SetFormEnabled</c>).
    /// Para tests que solo necesitan "alguna unidad formateable", sin importar cuál.
    /// </summary>
    public static char SelectAnyNonSystemDrive(Window window)
    {
        char systemLetter = char.ToUpperInvariant(Path.GetPathRoot(Environment.SystemDirectory)![0]);
        var combo = DrivePicker(window);
        combo.Expand();
        try
        {
            string lastSeen = "(ninguno)";
            var result = Retry.WhileNull(() =>
            {
                var items = combo.Items;
                lastSeen = items.Length == 0 ? "(ninguno)" : string.Join(", ", items.Select(i => $"'{GetItemDisplayText(i)}'"));
                return items.FirstOrDefault(i => ExtractDriveLetter(GetItemDisplayText(i)) != systemLetter);
            }, timeout: TimeSpan.FromSeconds(10), interval: TimeSpan.FromMilliseconds(300), ignoreException: true);

            if (result.Result is null)
                throw new InvalidOperationException(
                    $"No hay ninguna unidad no protegida disponible para estas pruebas de opciones de " +
                    $"formato. Ítems vistos: {lastSeen}");

            char letter = ExtractDriveLetter(GetItemDisplayText(result.Result));
            result.Result.Select();
            return letter;
        }
        finally
        {
            combo.Collapse();
        }
    }

    public static void SelectComboText(Window window, string automationId, string text) =>
        Require(window, automationId).AsComboBox().Select(text);

    public static CheckBox CheckBox(Window window, string automationId) => Require(window, automationId).AsCheckBox();

    public static void SetChecked(Window window, string automationId, bool value)
    {
        var box = CheckBox(window, automationId);
        if ((box.ToggleState == FlaUI.Core.Definitions.ToggleState.On) != value)
            box.Toggle();
    }

    public static Button Button(Window window, string automationId) => Require(window, automationId).AsButton();

    public static TextBox TextBox(Window window, string automationId) => Require(window, automationId).AsTextBox();

    /// <summary>
    /// Abre un ítem de menú siguiendo una ruta de AutomationIds (p. ej. "MnuConfig", "MnuLang",
    /// "MnuLangEn"). Usa los patrones UIA (ExpandCollapse para abrir un submenú, Invoke para el ítem
    /// hoja final) en vez de un clic de ratón simulado por coordenadas: un <c>.Click()</c> depende de
    /// la posición real del cursor/foco de la ventana en pantalla, que resultó frágil en la práctica
    /// tras el primer clic de la sesión (el segundo nivel de menú dejaba de encontrarse).
    /// </summary>
    public static void ClickMenuPath(Window window, params string[] automationIds)
    {
        for (int i = 0; i < automationIds.Length; i++)
        {
            string id = automationIds[i];
            bool isLast = i == automationIds.Length - 1;

            var result = Retry.WhileNull(
                () => window.FindFirstDescendant(cf => cf.ByAutomationId(id)),
                timeout: TimeSpan.FromSeconds(10),
                interval: TimeSpan.FromMilliseconds(200),
                ignoreException: true);

            if (!result.Success || result.Result is null)
                throw new InvalidOperationException($"No se encontró el ítem de menú '{id}'.");

            var element = result.Result;

            if (!isLast && element.Patterns.ExpandCollapse.IsSupported)
                element.Patterns.ExpandCollapse.Pattern.Expand();
            else if (element.Patterns.Invoke.IsSupported)
                element.Patterns.Invoke.Pattern.Invoke();
            // Los ToggleMenuFlyoutItem (idioma/tema) no exponen Invoke: su automation peer expone
            // Toggle, que dispara el mismo evento Click subyacente en el que engancha MainWindow.
            else if (element.Patterns.Toggle.IsSupported)
                element.Patterns.Toggle.Pattern.Toggle();
            else if (element.Patterns.ExpandCollapse.IsSupported)
                element.Patterns.ExpandCollapse.Pattern.Expand();
            else
                element.Click();

            // Pequeño respiro tras cada paso: la animación de apertura/cierre del flyout necesita un
            // instante para asentarse antes de que el siguiente paso lea el árbol de automatización.
            Thread.Sleep(150);
        }
    }
}
