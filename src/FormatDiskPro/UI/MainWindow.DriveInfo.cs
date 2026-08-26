using Microsoft.UI.Composition.SystemBackdrops;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using Windows.Graphics;
using Windows.UI;
using Windows.UI.ViewManagement;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Text;

namespace FormatDiskPro.UI;

/// <summary>
/// Unidad seleccionada: salud S.M.A.R.T., protección de escritura, ocupación e información de la unidad.
///
/// <para>Parte de <see cref="MainWindow"/>: es la MISMA clase, repartida en archivos por
/// asunto (T2-08). No es un rediseño y no cambia comportamiento — el archivo único pasaba de
/// 2.000 líneas y encontrar algo en él era el problema.
/// </summary>
public sealed partial class MainWindow
{
    // ── Drive selection ───────────────────────────────────────────

    private void DrivePicker_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        // El tamaño del disco anterior no vale para la unidad nueva: se descarta ANTES de repintar las
        // opciones, para que UpdateSmallFat32Option no ofrezca tamaños del disco que ya no está.
        _selectedDiskSizeBytes = null;

        if (DrivePicker.SelectedItem is not DriveViewModel item)
        {
            _isDriveProtected = false;
            _lastHealth = null;
            _diskSizeLetter = '\0';
            ClearInfo();
            SmallFat32Check.Visibility     = Visibility.Collapsed;
            SmallFat32Check.IsChecked      = false;
            SmallFat32SizePanel.Visibility = Visibility.Collapsed;
            UpdateSmallFat32Hint();
            UpdateToolsMenuAvailability();
            return;
        }

        _isDriveProtected = item.IsProtected;
        _diskSizeLetter   = item.Letter;   // se fija aquí, no dentro de la Task, para que la guarda de
                                           // obsolescencia funcione aunque el usuario cambie de unidad ya
        if (_settings.LastDriveLetter != item.Letter.ToString())
        {
            _settings.LastDriveLetter = item.Letter.ToString();
            _settings.Save();
        }
        UpdateInfo(item.Info);
        UpdateFileSystemOptions(item.Info);
        try { if (item.Info.IsReady) VolumeLabelBox.Text = item.Info.VolumeLabel; }
        catch { VolumeLabelBox.Text = ""; }

        ApplyProtection();

        // Se consumen explícitamente: no se esperan (el selector no puede bloquearse mientras PowerShell
        // consulta el S.M.A.R.T.), pero el descarte deja escrito que es deliberado. Ver LoadHealthAsync.
        // Van en paralelo y no encadenadas: son dos consultas independientes y una no debe esperar a la otra.
        _ = LoadHealthAsync(item);
        _ = LoadDiskSizeAsync(item);
    }

    /// <summary>
    /// Carga el tamaño del disco físico de la unidad seleccionada y con él ajusta los tamaños que ofrece la
    /// partición FAT32 pequeña.
    ///
    /// <para>Va aparte de <see cref="LoadHealthAsync"/> porque el dato que hace falta es el del DISCO:
    /// <see cref="DriveInfo.TotalSize"/> mide el volumen, y usarlo como tope dejaría la función en un
    /// trinquete que solo baja — crear una partición de 2 GB en un pendrive de 16 impediría volver a 8.</para>
    ///
    /// <para>Si la consulta falla (unidad RAW, disco desconectado a mitad) no se toca nada: se conserva el
    /// tope provisional del volumen, que siempre es menor o igual. Se ofrece de menos, nunca de más.</para>
    /// </summary>
    private async Task LoadDiskSizeAsync(DriveViewModel item)
    {
        char letter = item.Letter;

        long? size;
        try
        {
            size = await _services.Disk.GetDiskSizeAsync(letter);
        }
        catch (Exception ex)
        {
            // Igual que en LoadHealthAsync: al no ser `async void`, la excepción moriría en una Task que
            // nadie observa. Se cuenta en el historial y la opción se queda con el tope conservador.
            _services.History.Log($"DISKSIZE ERROR {letter}: {ErrorText.Describe(ex)}");
            return;
        }

        if (_diskSizeLetter != letter || size is null) return;

        _selectedDiskSizeBytes = size;
        RefreshSmallFat32Option();
    }

    /// <summary>
    /// Carga la salud S.M.A.R.T. de la unidad seleccionada y la pinta.
    ///
    /// <para><c>async Task</c> y no <c>async void</c>: esto <b>no es un manejador de eventos</b>. En un
    /// <c>async void</c> una excepción no puede capturarse desde quien llama y acaba en la red global de
    /// <c>App.UnhandledException</c> (`T0-01`) — que existe para lo imprevisto, no como forma habitual de
    /// enterarse de que algo falló.</para>
    ///
    /// <para>La comparación con <c>_healthLetter</c> descarta la respuesta si el usuario ya cambió de
    /// unidad: la consulta tarda, y sin eso la salud de la unidad anterior pisaría a la nueva.</para>
    /// </summary>
    private async Task LoadHealthAsync(DriveViewModel item)
    {
        char letter = item.Letter;
        _healthLetter = letter;
        _lastHealth = null;
        InfoHealthText.Text = L.T("info.health", L.T("info.loading"));
        InfoBusText.Text    = L.T("info.bus", L.T("info.loading"));

        DiskService.HealthInfo? info;
        try
        {
            info = await _services.Disk.GetHealthAsync(letter);
        }
        catch (Exception ex)
        {
            // Al dejar de ser `async void`, una excepción aquí ya no llega a la red global: quedaría en
            // una Task que nadie observa, es decir, en silencio. Se atrapa y se cuenta — la salud pasa a
            // "no disponible", que es exactamente lo que el usuario necesita saber.
            if (_healthLetter == letter) RenderHealth(null);
            _services.History.Log($"HEALTH ERROR {letter}: {ErrorText.Describe(ex)}");
            return;
        }

        if (_healthLetter != letter) return;

        _lastHealth = info;
        RenderHealth(info);
    }

    private void RenderHealth(DiskService.HealthInfo? h)
    {
        if (h is null)
        {
            InfoHealthText.Text = L.T("info.health", L.T("info.dash"));
            InfoBusText.Text    = L.T("info.bus", L.T("info.dash"));
            InfoHealthText.ClearValue(TextBlock.ForegroundProperty);
            return;
        }
        // Un USB puede no reportar salud/bus/medio: mostrar el guion en vez de dejar el valor vacío
        // (línea "Salud:" sin nada) o un separador "·" huérfano en "Conexión:".
        string dash = L.T("info.dash");
        bool hasBus = !string.IsNullOrWhiteSpace(h.Bus), hasMedia = !string.IsNullOrWhiteSpace(h.Media);
        string conn = hasBus && hasMedia ? $"{h.Bus} · {h.Media}" : hasBus ? h.Bus : hasMedia ? h.Media : dash;
        InfoHealthText.Text = L.T("info.health", string.IsNullOrWhiteSpace(h.Health) ? dash : h.Health);
        InfoBusText.Text    = L.T("info.bus", conn);

        // Umbrales de #16: colorear el estado reportado (el texto ya transmite el estado; el color refuerza).
        var level = SmartInfo.HealthLevel(h.Health);
        if (level == SmartLevel.Unknown)
            InfoHealthText.ClearValue(TextBlock.ForegroundProperty);
        else
            InfoHealthText.Foreground = HealthDialog.LevelBrush(level, _darkMode);
    }

    /// <summary>
    /// Ajusta el menú <i>Herramientas</i> a la unidad seleccionada (<c>T7-02</c>): lo que esa unidad no
    /// admite se ve apagado en vez de aceptarse y rechazarse después en un diálogo.
    ///
    /// <para><b>El motivo va escrito en cada ítem</b>, y por partida doble: una etiqueta corta en el
    /// TEXTO visible —<c>T7-08</c>— y la frase completa en el tooltip y el <c>HelpText</c> de
    /// automatización. Un ítem gris y mudo es peor que el diálogo que sustituye: deja al usuario sin
    /// saber qué hizo mal. Las condiciones son EXACTAMENTE las guardas de cada handler en
    /// <c>MainWindow.Operations.cs</c>, que <b>siguen ahí</b>: el estado de la unidad puede cambiar
    /// entre que se abre el menú y se pulsa (para eso está <c>WM_DEVICECHANGE</c>), así que esto es la
    /// primera línea y aquellas son la red.</para>
    ///
    /// <para><i>Comprobar errores</i> y <i>Benchmark</i> no se apagan nunca con una unidad seleccionada:
    /// chkdsk en modo solo lectura sí corre sobre el disco de sistema —lo que no se ofrece allí es la
    /// reparación— y el benchmark no escribe fuera de su propio archivo temporal.</para>
    ///
    /// <para>Este método es el ÚNICO dueño del texto de estos siete ítems: <c>ApplyLanguage</c> ya no
    /// se los escribe, porque el texto depende del idioma <i>y</i> de la unidad, y dos dueños dejarían
    /// la etiqueta del motivo pegada —o perdida— según cuál escribiera el último.</para>
    /// </summary>
    private void UpdateToolsMenuAvailability()
    {
        var item = DrivePicker.SelectedItem as DriveViewModel;
        bool hasDrive  = item is not null;
        bool blocked   = item is null || item.IsProtected || IsSystemDrive(item.Letter);
        bool removable = item?.Info.DriveType == DriveType.Removable;

        // El motivo más específico primero: sin unidad no hay nada que explicar sobre protecciones.
        var noDrive     = (L.T("menu.whyNoDrive"),   L.T("menu.tagNoDrive"));
        var protectedWhy = (L.T("menu.whyProtected"), L.T("menu.tagProtected"));
        var removableWhy = (L.T("menu.whyRemovable"), L.T("menu.tagRemovable"));

        SetMenuItemAvailability(MnuVerify, "menu.verify", hasDrive && !blocked,
                                !hasDrive ? noDrive : protectedWhy);
        SetMenuItemAvailability(MnuHealth,    "menu.health",    hasDrive, noDrive);
        SetMenuItemAvailability(MnuCheck,     "menu.check",     hasDrive, noDrive);
        SetMenuItemAvailability(MnuBenchmark, "menu.benchmark", hasDrive, noDrive);
        SetMenuItemAvailability(MnuUnlock, "menu.unlock", hasDrive && !blocked,
                                !hasDrive ? noDrive : protectedWhy);
        SetMenuItemAvailability(MnuReinit, "menu.reinit", hasDrive && removable && !blocked,
                                !hasDrive ? noDrive : !removable ? removableWhy : protectedWhy);
        SetMenuItemAvailability(MnuEject,  "menu.eject",  hasDrive && removable,
                                !hasDrive ? noDrive : removableWhy);
    }

    /// <summary>
    /// Habilita o apaga un ítem del menú, poniéndole el motivo cuando queda apagado y quitándoselo
    /// cuando vuelve.
    ///
    /// <para>El motivo va en tres sitios y no es redundancia: la <b>etiqueta corta</b> se pega al texto
    /// visible porque WinUI <b>no muestra el tooltip de un control deshabilitado</b> —no existe el
    /// <c>ShowOnDisabled</c> de WPF—, así que sin ella quien mira la pantalla no recibe nada; el
    /// <b>tooltip</b> se conserva por si el ítem se recorre con el ratón desde un control vecino; y el
    /// <c>HelpText</c> lleva la frase completa, que es lo que lee un lector de pantalla, porque este
    /// anuncia el ítem como no disponible pero no sabría decir por qué.</para>
    ///
    /// <para>El texto se re-deriva SIEMPRE de <paramref name="labelKey"/>, nunca del que el ítem trae
    /// puesto: leerlo acumularía la etiqueta cada vez que se repinta el menú.</para>
    /// </summary>
    /// <param name="menuItem">Ítem del menú a ajustar.</param>
    /// <param name="labelKey">Clave de localización del texto base del ítem, sin etiqueta de motivo.</param>
    /// <param name="enabled">Si la unidad seleccionada admite la operación.</param>
    /// <param name="why">Frase completa del motivo y su etiqueta corta, ambas ya localizadas.</param>
    private static void SetMenuItemAvailability(
        MenuFlyoutItem menuItem, string labelKey, bool enabled, (string Reason, string Tag) why)
    {
        menuItem.IsEnabled = enabled;

        if (enabled)
        {
            menuItem.Text = L.T(labelKey);
            ToolTipService.SetToolTip(menuItem, null);
            AutomationProperties.SetHelpText(menuItem, "");
        }
        else
        {
            menuItem.Text = $"{L.T(labelKey)}  {why.Tag}";
            ToolTipService.SetToolTip(menuItem, why.Reason);
            AutomationProperties.SetHelpText(menuItem, why.Reason);
        }
    }

    private void ApplyProtection()
    {
        if (_isDriveProtected)
        {
            FileSystemPicker.IsEnabled  = false;
            AllocUnitPicker.IsEnabled   = false;
            VolumeLabelBox.IsEnabled    = false;
            RestoreButton.IsEnabled     = false;
            StartButton.IsEnabled       = false;
            QuickFormatCheck.IsEnabled  = false;
            CompressCheck.IsEnabled     = false;
            SecureWipeCheck.IsEnabled   = false;
            ProtectedBar.Message = L.T("protected.status");
            ProtectedBar.IsOpen  = true;
        }
        else
        {
            FileSystemPicker.IsEnabled  = true;
            AllocUnitPicker.IsEnabled   = true;
            VolumeLabelBox.IsEnabled    = true;
            RestoreButton.IsEnabled     = true;
            StartButton.IsEnabled       = true;
            QuickFormatCheck.IsEnabled  = true;
            SecureWipeCheck.IsEnabled   = true;
            CompressCheck.IsEnabled     = FileSystemPicker.SelectedItem?.ToString() == "NTFS";
            ProtectedBar.IsOpen = false;
            StatusText.ClearValue(TextBlock.ForegroundProperty);
            StatusText.Text = "";
        }
        UpdateWipePassesEnabled();
        UpdateToolsMenuAvailability();
    }

    // Una unidad protegida es la misma señal que una salud crítica, así que lleva el mismo color: se
    // reusa SeverityPalette en vez de repetir el RGB. Repetirlo es exactamente como entró un fallo de
    // contraste sin romper el build (ver el comentario de SeverityPalette.All).
    private Color ProtectedColor() => SeverityPalette.For(SmartLevel.Critical, _darkMode);

    private SolidColorBrush DriveBrush(bool isProtected) =>
        new(isProtected ? ProtectedColor() : SeverityPalette.Text(_darkMode));

    private void UpdateInfo(DriveInfo drive)
    {
        try
        {
            if (!drive.IsReady) { ClearInfo(); return; }
            long total = drive.TotalSize, free = drive.AvailableFreeSpace;
            InfoTotalText.Text = L.T("info.total", FormatBytes(total));
            InfoFreeText.Text  = L.T("info.free", FormatBytes(free));
            InfoFsText.Text    = L.T("info.fs", drive.DriveFormat);
            InfoTypeText.Text  = L.T("info.type", DriveTypeName(drive.DriveType));
            RenderCapacity(total, free);
        }
        catch { ClearInfo(); }
    }

    /// <summary>
    /// Pinta la barra de ocupación: el relleno usado a la izquierda y el espacio libre como pista.
    /// </summary>
    /// <remarks>
    /// El reparto se hace con las columnas ESTRELLA del <c>Grid</c>, no con un ancho en píxeles: así la
    /// proporción sobrevive a cualquier redimensión sin volver a calcular nada. Los dos colores se
    /// reasignan en cada llamada porque dependen del tema efectivo, y <c>ApplyThemeMode</c> vuelve a pasar
    /// por aquí al cambiar de tema.
    /// </remarks>
    /// <param name="total">Tamaño total del volumen, en bytes.</param>
    /// <param name="free">Espacio libre disponible, en bytes.</param>
    private void RenderCapacity(long total, long free)
    {
        long used = Math.Max(0, total - free);
        double usedPct = total > 0 ? Math.Clamp(used * 100.0 / total, 0, 100) : 0;

        CapacityColumns.ColumnDefinitions[0].Width = new GridLength(usedPct, GridUnitType.Star);
        CapacityColumns.ColumnDefinitions[1].Width = new GridLength(100 - usedPct, GridUnitType.Star);
        CapacityUsedFill.Background = CapacityBrush(usedPct);
        CapacityBar.Background      = new SolidColorBrush(SeverityPalette.TrackFill(_darkMode));
        CapacityText.Text           = L.T("info.usedOf", FormatBytes(used), FormatBytes(total));
        CapacityPanel.Visibility    = Visibility.Visible;

        // El nombre accesible lleva el dato, no solo la etiqueta: un Border no expone valor de rango como
        // hacía el ProgressBar, así que «Espacio utilizado» a secas no le diría nada a un lector de
        // pantalla. Se fija aquí y no en ApplyLanguage porque ese también termina llamando a UpdateInfo.
        // El rótulo visible queda en Raw: dice lo mismo en bytes y leerlo dos veces sobra.
        string name = L.T("info.used", (int)Math.Round(usedPct));
        AutomationProperties.SetName(CapacityBar, name);
        ToolTipService.SetToolTip(CapacityBar, name);
    }

    /// <summary>
    /// Pincel de la barra de OCUPACIÓN: neutro cuando hay espacio de sobra, ámbar al llenarse (≥80 %),
    /// rojo casi llena (≥90 %). Una barra de capacidad no debe usar el color de ACENTO del sistema
    /// (lo que hace un <c>ProgressBar</c> por defecto): en un equipo con acento rojo se veía roja con el
    /// disco medio vacío y leía como alarma. El color codifica cuánto queda, no la marca. Ámbar/rojo
    /// reusan <see cref="SeverityPalette"/> (theme-aware, contraste medido por tests); el neutro sigue el
    /// tema efectivo (<c>_darkMode</c>), y se re-deriva al cambiar de tema porque <see cref="UpdateInfo"/>
    /// vuelve a correr (ver <c>ApplyThemeMode</c>).
    /// </summary>
    private Brush CapacityBrush(double usedPct)
    {
        Color c = usedPct >= 90 ? SeverityPalette.For(SmartLevel.Critical, _darkMode)
                : usedPct >= 80 ? SeverityPalette.For(SmartLevel.Warning, _darkMode)
                : SeverityPalette.NeutralFill(_darkMode);
        return new SolidColorBrush(c);
    }

    private void ClearInfo()
    {
        InfoTotalText.Text  = L.T("info.total", L.T("info.dash"));
        InfoFreeText.Text   = L.T("info.free", L.T("info.dash"));
        InfoFsText.Text     = L.T("info.fs", L.T("info.dash"));
        InfoTypeText.Text   = L.T("info.type", L.T("info.dash"));
        InfoHealthText.Text = L.T("info.health", L.T("info.dash"));
        InfoBusText.Text    = L.T("info.bus", L.T("info.dash"));
        InfoHealthText.ClearValue(TextBlock.ForegroundProperty);
        CapacityPanel.Visibility = Visibility.Collapsed;
    }
}
