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
/// Tarjeta de configuración de formato: sistema de archivos, unidad de asignación, etiqueta, partición FAT32 pequeña (#37) y presets.
///
/// <para>Parte de <see cref="MainWindow"/>: es la MISMA clase, repartida en archivos por
/// asunto (T2-08). No es un rediseño y no cambia comportamiento — el archivo único pasaba de
/// 2.000 líneas y encontrar algo en él era el problema.
/// </summary>
public sealed partial class MainWindow
{
    // ── File system ───────────────────────────────────────────────

    private void UpdateFileSystemOptions(DriveInfo drive)
    {
        string? previous = FileSystemPicker.SelectedItem?.ToString();
        FileSystemPicker.Items.Clear();

        long bytes = 0;
        try { bytes = drive.IsReady ? drive.TotalSize : 0; } catch { }

        FileSystemPicker.Items.Add("NTFS");
        FileSystemPicker.Items.Add("exFAT");
        FileSystemPicker.Items.Add("ReFS");
        if (bytes == 0 || bytes < FormatLogic.Fat32MaxBytes) FileSystemPicker.Items.Add("FAT32");
        if (bytes == 0 || bytes < 2L * 1024 * 1024 * 1024)  FileSystemPicker.Items.Add("FAT");

        int idx = previous is not null ? FileSystemPicker.Items.IndexOf(previous) : -1;
        if (idx >= 0) FileSystemPicker.SelectedIndex = idx;
        else          SuggestFileSystem(drive, bytes);

        UpdateSmallFat32Option(drive, bytes);
    }

    private void SuggestFileSystem(DriveInfo drive, long totalBytes)
    {
        string suggested = drive.DriveType == DriveType.Removable
            ? (totalBytes > FormatLogic.Fat32MaxBytes ? "exFAT" : "FAT32")
            : "NTFS";
        int idx = FileSystemPicker.Items.IndexOf(suggested);
        FileSystemPicker.SelectedIndex = idx >= 0 ? idx : 0;
    }

    private void FileSystemPicker_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        UpdateAllocationUnits();
        UpdateFsDescription();
        UpdateCompressionOption();
        UpdateLabelMaxLength();
        // Un cambio de FS puede volver inválida la etiqueta ya escrita (p. ej. NTFS→FAT32 acorta el máximo).
        UpdateLabelHint();
    }

    // Ajusta el máximo de caracteres de la etiqueta al límite del FS seleccionado (FAT/FAT32/exFAT: 11;
    // NTFS/ReFS: 32), para dar feedback inmediato en vez de fallar solo al pulsar Iniciar.
    private void UpdateLabelMaxLength()
    {
        string? fs = FileSystemPicker.SelectedItem?.ToString();
        VolumeLabelBox.MaxLength = fs is not null ? FormatLogic.MaxLabelLength(fs) : 32;
    }

    private void UpdateAllocationUnits()
    {
        string? fs = FileSystemPicker.SelectedItem?.ToString();
        if (fs is null || !FsDefaults.TryGetValue(fs, out var cfg)) return;

        AllocUnitPicker.Items.Clear();
        _allocBytes.Clear();

        foreach (long size in cfg.Sizes)
        {
            string label = size >= 1024 * 1024 ? $"{size / (1024 * 1024)} MB"
                         : size >= 1024 ? $"{size / 1024} KB" : $"{size} bytes";
            AllocUnitPicker.Items.Add(label);
            _allocBytes.Add(size);
        }

        int defIdx = Array.IndexOf(cfg.Sizes, cfg.Default);
        AllocUnitPicker.SelectedIndex = defIdx >= 0 ? defIdx : 0;
    }

    private void UpdateFsDescription()
    {
        string? fs = FileSystemPicker.SelectedItem?.ToString();
        string key = fs is not null ? FsDescriptionKey(fs) : "";
        FsDescText.Text = key.Length > 0 ? L.T(key) : "";
    }

    private void UpdateCompressionOption()
    {
        bool isNtfs = FileSystemPicker.SelectedItem?.ToString() == "NTFS";
        CompressCheck.IsEnabled = isNtfs && !_isDriveProtected;
        if (!isNtfs) CompressCheck.IsChecked = false;
    }

    // ── Reinicializar: partición FAT32 pequeña (#37) ──────────────

    /// <summary>
    /// Muestra la opción en cualquier unidad extraíble donde quepa al menos el menor de los tamaños
    /// ofrecidos, y deja en el selector solo los que caben de verdad.
    ///
    /// <para>Antes la condición era <c>≥ 32 GB</c>, porque la función se pensó como rodeo al límite de
    /// Windows (no crea volúmenes FAT32 mayores de 32 GB) y en discos menores FAT32 ya está en el selector.
    /// Pero lo que hace por debajo —crear una partición de N y dejar el resto sin asignar— sirve igual en un
    /// pendrive de 16 GB, y ocultarla ahí dejaba fuera un caso de uso legítimo.</para>
    ///
    /// <para>Solo surte efecto vía "Reinicializar unidad…"; el flujo normal de Iniciar la ignora.</para>
    /// </summary>
    /// <param name="drive">Unidad seleccionada.</param>
    /// <param name="volumeBytes">Tamaño del VOLUMEN. Se usa solo como tope provisional mientras
    /// <see cref="LoadDiskSizeAsync"/> consulta el del disco físico: siempre es menor o igual, así que
    /// mientras tanto se ofrece de menos, nunca de más.</param>
    private void UpdateSmallFat32Option(DriveInfo drive, long volumeBytes)
    {
        DriveType type = DriveType.Unknown;
        try { type = drive.DriveType; } catch { }

        long ceiling = _selectedDiskSizeBytes ?? volumeBytes;
        int[] sizes  = type == DriveType.Removable ? ReinitPlan.SmallFat32SizesFor(ceiling) : [];

        bool qualifies = sizes.Length > 0;
        SmallFat32Check.Visibility     = qualifies ? Visibility.Visible : Visibility.Collapsed;
        SmallFat32SizePanel.Visibility = qualifies ? Visibility.Visible : Visibility.Collapsed;
        RestPanel.Visibility           = qualifies ? Visibility.Visible : Visibility.Collapsed;
        if (!qualifies) SmallFat32Check.IsChecked = false;
        PopulateSmallFat32Sizes(sizes, ceiling);
        UpdateSmallFat32SizeEnabled();
        UpdateSmallFat32Hint();
    }

    /// <summary>Recalcula la opción para la unidad ya seleccionada. Lo usa
    /// <see cref="LoadDiskSizeAsync"/> cuando llega el tamaño real del disco, que es el dato bueno.</summary>
    private void RefreshSmallFat32Option()
    {
        if (DrivePicker.SelectedItem is not DriveViewModel item) return;

        long bytes = 0;
        try { bytes = item.Info.IsReady ? item.Info.TotalSize : 0; } catch { }
        UpdateSmallFat32Option(item.Info, bytes);
    }

    /// <summary>
    /// Rellena el selector con los tamaños que caben, preseleccionando el preferido del usuario (o el mayor
    /// disponible). La reposición es un no-op si la lista no cambió, para no parpadear cuando llega el
    /// tamaño del disco y coincide con el provisional.
    /// </summary>
    private void PopulateSmallFat32Sizes(int[] sizes, long ceilingBytes)
    {
        _smallFat32Ceiling = ceilingBytes;
        if (_smallFat32Sizes.SequenceEqual(sizes)) return;

        // La selección que se hace aquí es programática: no debe persistirse como elección del usuario.
        // Si hoy hay un pendrive de 8 GB conectado, guardar "8" borraría la preferencia de 32 del usuario.
        _repopulatingSizes = true;
        try
        {
            _smallFat32Sizes.Clear();
            _smallFat32Sizes.AddRange(sizes);

            SmallFat32SizePicker.Items.Clear();
            foreach (int gb in sizes) SmallFat32SizePicker.Items.Add($"{gb} GB");

            int? pick = ReinitPlan.PickSmallFat32Size(_settings.SmallFat32SizeGb, _smallFat32Sizes);
            SmallFat32SizePicker.SelectedIndex = pick is int gbPick ? _smallFat32Sizes.IndexOf(gbPick) : -1;
        }
        finally { _repopulatingSizes = false; }
    }

    private void SmallFat32Check_Toggled(object sender, RoutedEventArgs e)
    {
        UpdateSmallFat32SizeEnabled();
        UpdateSmallFat32Hint();
    }

    /// <summary>
    /// Tamaño en GB elegido en la UI; <c>0</c> si no hay selección válida.
    ///
    /// <para>Devuelve 0 y no 32 (lo que hacía cuando la lista era fija en el XAML) a propósito: ahora la
    /// lista depende del disco, y caer al máximo podría pedir una partición que no cabe. Quien llama trata
    /// el 0 como "sin partición pequeña".</para>
    /// </summary>
    private int SelectedSmallFat32SizeGb()
    {
        int idx = SmallFat32SizePicker.SelectedIndex;
        return idx >= 0 && idx < _smallFat32Sizes.Count ? _smallFat32Sizes[idx] : 0;
    }

    /// <summary>El selector de tamaño y el enlace de reinicializar solo están activos cuando la casilla
    /// de FAT32 pequeña está visible, habilitada y marcada.</summary>
    private void UpdateSmallFat32SizeEnabled()
    {
        bool on = SmallFat32Check.Visibility == Visibility.Visible && SmallFat32Check.IsEnabled
               && SmallFat32Check.IsChecked == true && _smallFat32Sizes.Count > 0;
        SetSubOptionEnabled(on, [SmallFat32SizeLbl], SmallFat32SizePicker);
        SmallFat32GoButton.IsEnabled = on;
        SetSubOptionEnabled(on, [RestLbl], RestPicker);
        UpdateRestOption();
    }

    /// <summary>
    /// Habilita o deshabilita un sub-bloque de opciones —sus controles y sus etiquetas— como una unidad.
    /// </summary>
    /// <remarks>
    /// <para><b>Por qué existe</b> (`T6-08`). Antes cada bloque se atenuaba DOS veces: <c>Opacity="0.5"</c>
    /// sobre el panel entero y <c>IsEnabled="false"</c> sobre el control, así que el desplegable quedaba
    /// doblemente apagado. Ahora solo lo apaga su propio <c>IsEnabled</c>, con el visual deshabilitado del
    /// tema — el mismo que dibuja Windows en el resto del sistema, y que está exento del requisito de
    /// contraste por ser deshabilitado de verdad y no un texto tenue.</para>
    ///
    /// <para><b>Se reciben los controles uno a uno, y no el panel que los contiene, porque en WinUI un
    /// panel no se puede deshabilitar:</b> <c>IsEnabled</c> vive en <c>Control</c>, y <c>Panel</c> deriva
    /// de <c>FrameworkElement</c>. Un <c>&lt;StackPanel IsEnabled="False"&gt;</c> no compila
    /// (<c>WMC0011</c>) — al contrario que en WPF, donde <c>UIElement</c> sí lo tiene. Es la primera cosa
    /// que uno intenta al arreglar esto.</para>
    ///
    /// <para><b>La etiqueta hay que atenuarla aparte</b>, por el mismo motivo: un <c>TextBlock</c> tampoco
    /// es un <c>Control</c>, así que no tiene estado visual deshabilitado y se quedaría a pleno contraste,
    /// más viva que el desplegable de al lado. Se le pone <c>TextFillColorDisabledBrush</c>, que es el
    /// token del tema para esto, y al reactivarla se hace <c>ClearValue</c> para devolverla al color de su
    /// estilo en vez de fijarle otro a mano (que se quedaría clavado al cambiar de tema en caliente).</para>
    /// </remarks>
    /// <param name="on">Si el sub-bloque debe quedar activo.</param>
    /// <param name="labels">Etiquetas del bloque, que no se atenúan solas.</param>
    /// <param name="controls">Controles del bloque.</param>
    private static void SetSubOptionEnabled(bool on, TextBlock[] labels, params Control[] controls)
    {
        foreach (Control control in controls) control.IsEnabled = on;

        foreach (TextBlock label in labels)
        {
            if (on) label.ClearValue(TextBlock.ForegroundProperty);
            else    label.Foreground = (Brush)Application.Current.Resources["TextFillColorDisabledBrush"];
        }
    }

    // ── Reinicializar: qué hacer con el espacio sobrante (`T5-02`) ──

    /// <summary>¿Se pidió una segunda partición con el espacio que sobra?</summary>
    /// <remarks>Se pregunta por el índice y no por el texto: el texto está traducido a cinco idiomas.</remarks>
    private bool CreateSecondPartitionRequested()
        => RestPanel.Visibility == Visibility.Visible && RestPicker.IsEnabled && RestPicker.SelectedIndex == 1;

    /// <summary>Sistema de archivos elegido para la segunda partición, normalizado al conjunto permitido.</summary>
    private string SelectedRestFileSystem()
        => PartitionPlan.NormalizeSecondPartitionFileSystem(RestFsPicker.SelectedItem?.ToString());

    /// <summary>Rellena los dos selectores del sobrante desde las preferencias persistidas. Se llama al
    /// aplicar el idioma, porque el primero de ellos lleva texto traducido.</summary>
    private void InitRestPickers()
    {
        _repopulatingSizes = true;   // reutiliza la guarda: esta selección tampoco es del usuario
        try
        {
            RestPicker.Items.Clear();
            RestPicker.Items.Add(L.T("opt.restUnallocated"));
            RestPicker.Items.Add(L.T("opt.restSecond"));
            RestPicker.SelectedIndex = _settings.CreateSecondPartition ? 1 : 0;

            RestFsPicker.Items.Clear();
            foreach (string fs in PartitionPlan.SecondPartitionFileSystems) RestFsPicker.Items.Add(fs);
            int idx = Array.IndexOf(PartitionPlan.SecondPartitionFileSystems, _settings.SecondPartitionFileSystem);
            RestFsPicker.SelectedIndex = idx >= 0 ? idx : 0;
        }
        finally { _repopulatingSizes = false; }

        UpdateRestOption();
    }

    private void RestPicker_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_uiReady && !_repopulatingSizes)
        {
            _settings.CreateSecondPartition = RestPicker.SelectedIndex == 1;
            _settings.Save();
        }
        UpdateRestOption();
    }

    private void RestFsPicker_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (!_uiReady || _repopulatingSizes) return;
        _settings.SecondPartitionFileSystem = SelectedRestFileSystem();
        _settings.Save();
        // El máximo de la etiqueta depende del FS: exFAT admite 11 caracteres y NTFS 32.
        UpdateRestOption();
    }

    /// <summary>Muestra el formato y la etiqueta del sobrante solo cuando se pidió la segunda partición, y
    /// ajusta el máximo de la etiqueta al sistema de archivos elegido.</summary>
    private void UpdateRestOption()
    {
        bool second = CreateSecondPartitionRequested();
        RestDetailPanel.Visibility = second ? Visibility.Visible : Visibility.Collapsed;
        RestNoteText.Visibility    = second ? Visibility.Visible : Visibility.Collapsed;
        if (!second) return;

        RestNoteText.Text     = L.T("opt.restNote");
        RestLabelBox.MaxLength = FormatLogic.MaxLabelLength(SelectedRestFileSystem());
    }

    private void SmallFat32SizePicker_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (!_uiReady || _repopulatingSizes) return;   // selección programática, no elección del usuario
        int gb = SelectedSmallFat32SizeGb();
        if (gb <= 0) return;
        _settings.SmallFat32SizeGb = gb;
        _settings.Save();
        UpdateSmallFat32Hint();
    }

    private void UpdateSmallFat32Hint()
    {
        int gb = SelectedSmallFat32SizeGb();
        bool show = SmallFat32Check.Visibility == Visibility.Visible && SmallFat32Check.IsChecked == true && gb > 0;
        SmallFat32HintText.Visibility = show ? Visibility.Visible : Visibility.Collapsed;
        SmallFat32GoButton.Visibility = show ? Visibility.Visible : Visibility.Collapsed;
        if (!show) return;

        string size = FormatLogic.FormatBytes(ReinitPlan.SmallFat32PartitionBytes(gb));

        // En discos que no llegan a 32 GB, FAT32 ya está en el selector: mencionar el límite de Windows
        // ahí no explica nada. Lo que aporta la opción en esos discos es dejar espacio sin asignar.
        SmallFat32HintText.Text = _smallFat32Ceiling >= FormatLogic.Fat32MaxBytes
            ? L.T("opt.smallFat32Hint", FormatLogic.FormatBytes(FormatLogic.Fat32MaxBytes), size)
            : L.T("opt.smallFat32HintSmall", size);
    }

    private void RestoreButton_Click(object sender, RoutedEventArgs e)
    {
        if (DrivePicker.SelectedItem is DriveViewModel item)
        {
            UpdateFileSystemOptions(item.Info);
            try { if (item.Info.IsReady) VolumeLabelBox.Text = item.Info.VolumeLabel; }
            catch { VolumeLabelBox.Text = ""; }
        }
        QuickFormatCheck.IsChecked = true;
        CompressCheck.IsChecked    = false;
        SecureWipeCheck.IsChecked  = false;
        SmallFat32Check.IsChecked  = false;
        RestPicker.SelectedIndex   = 0;   // "dejarlo sin asignar": el valor por defecto de la opción
        RestLabelBox.Text          = "";
        UpdateSmallFat32Hint();
    }

    // ── Presets ───────────────────────────────────────────────────

    /// <summary>
    /// Rellena las DOS listas de presets: la del menú <i>Configuración</i> y la del botón que vive en la
    /// tarjeta que configuran (`T12-04`).
    /// </summary>
    /// <remarks>
    /// Un solo constructor para los dos, y a propósito: son la misma lista: si se construyeran por
    /// separado, un preset nuevo aparecería en uno y no en el otro hasta que alguien se acordara. Los
    /// <c>MenuFlyoutItem</c> no se pueden compartir entre dos <c>Flyout</c> —un elemento de XAML tiene un
    /// solo padre—, así que se crean dos juegos con la misma fábrica.
    /// </remarks>
    private void BuildPresetsMenu()
    {
        FillPresets(MnuPresets.Items);
        FillPresets(PresetsFlyout.Items);
    }

    /// <summary>Vuelca los presets integrados, los del usuario y <i>Gestionar…</i> en una lista.</summary>
    /// <param name="items">Lista de un <c>MenuFlyout</c> o de un <c>MenuFlyoutSubItem</c>.</param>
    private void FillPresets(IList<MenuFlyoutItemBase> items)
    {
        items.Clear();

        foreach (var preset in Presets.All)
            items.Add(MakePresetItem(preset));

        if (_settings.UserPresets.Count > 0)
        {
            items.Add(new MenuFlyoutSeparator());
            foreach (var preset in _settings.UserPresets)
                items.Add(MakePresetItem(preset));
        }

        items.Add(new MenuFlyoutSeparator());
        var manage = new MenuFlyoutItem { Text = L.T("menu.managePresets") };
        manage.Click += MnuManagePresets_Click;
        items.Add(manage);
    }

    /// <summary>
    /// Lo que se va a aplicar al pulsar el botón primario: sistema de archivos, tamaño de clúster y modo.
    /// </summary>
    /// <remarks>
    /// <para>Sale de los controles, no de un estado paralelo: es la única forma de que el resumen no
    /// pueda mentir sobre lo que la operación hará.</para>
    ///
    /// <para>La versión corta es la que cabe en el pie junto a los botones; la <paramref name="full"/>
    /// añade la compresión y es la del tooltip y la del diálogo de presets. Se separan porque el ancho
    /// disponible en el pie son ~200 px y «compresión» solo aplica a NTFS: gastarlos siempre en un dato
    /// que la mayoría de las veces no está activo dejaría fuera al que sí importa.</para>
    /// </remarks>
    /// <param name="full">Incluir la compresión.</param>
    private string CurrentFormatSummary(bool full = false)
    {
        string fs   = FileSystemPicker.SelectedItem?.ToString() ?? "";
        string mode = QuickFormatCheck.IsChecked == true ? L.T("fmt.quick") : L.T("fmt.full");

        if (full && CompressCheck.IsChecked == true) mode += " + " + L.T("fmt.compress");
        if (SecureWipeCheck.IsChecked == true)       mode += " + " + L.T("confirm.secure");

        return $"{fs} · {AllocUnitPicker.SelectedItem} · {mode}";
    }

    /// <summary>
    /// Escribe el pie: el texto del botón primario y el resumen de lo que va a aplicar.
    /// </summary>
    /// <remarks>
    /// <para><b>El botón nombra la unidad</b> (`T12-02`). «Iniciar» no decía ni qué empieza ni sobre qué,
    /// y el peor fallo posible de esta app es formatear la unidad equivocada: era el único control de la
    /// pantalla que podía destruir un disco sin nombrarlo. La confirmación reforzada sigue siendo la red;
    /// esto es la primera línea.</para>
    ///
    /// <para><b>Es el ÚNICO dueño de ese texto</b>, por lo mismo que <c>UpdateToolsMenuAvailability</c> lo
    /// es del de los siete ítems del menú: depende del idioma <i>y</i> de la unidad, y dos dueños dejarían
    /// el nombre de la unidad perdido o duplicado según cuál corriera el último.</para>
    ///
    /// <para>El resumen se oculta con una operación en curso: ahí el pie lo manda <c>StatusText</c>, que
    /// además es su región activa, y dos textos compitiendo por la misma fila sobra uno.</para>
    /// </remarks>
    private void UpdateFooterSummary()
    {
        var drive = DrivePicker.SelectedItem as DriveViewModel;

        StartButton.Content = drive is null
            ? L.T("btn.start")
            : L.T("btn.start.drive", $"{drive.Letter}:");

        if (_isBusy || drive is null || FileSystemPicker.SelectedItem is null)
        {
            FormatSummaryText.Text = "";
            ToolTipService.SetToolTip(FormatSummaryText, null);
            return;
        }

        FormatSummaryText.Text = CurrentFormatSummary();
        ToolTipService.SetToolTip(FormatSummaryText, L.T("fmt.summaryTip", CurrentFormatSummary(full: true)));
    }

    /// <summary>
    /// Repinta el pie cuando cambia una opción de formato que el resumen enseña.
    /// </summary>
    /// <remarks>
    /// La guarda de <c>_uiBuilt</c> no es defensiva por si acaso: <c>IsChecked="True"</c> en el XAML
    /// dispara esto <b>durante</b> <c>InitializeComponent</c>, cuando los controles del pie aún no
    /// existen. Ver el comentario de ese campo.
    /// </remarks>
    private void FormatOption_Changed(object sender, RoutedEventArgs e)
    {
        if (_uiBuilt) UpdateFooterSummary();
    }

    /// <inheritdoc cref="FormatOption_Changed(object, RoutedEventArgs)"/>
    private void FormatOption_Changed(object sender, SelectionChangedEventArgs e)
    {
        if (_uiBuilt) UpdateFooterSummary();
    }

    private MenuFlyoutItem MakePresetItem(FormatPreset preset)
    {
        var item = new MenuFlyoutItem { Text = Presets.DisplayName(preset), Tag = preset };
        item.Click += MnuPreset_Click;
        return item;
    }

    private async void MnuManagePresets_Click(object sender, RoutedEventArgs e)
    {
        if (_isBusy) return;

        string fs       = FileSystemPicker.SelectedItem?.ToString() ?? "NTFS";
        long allocBytes = GetSelectedAllocBytes();
        bool quick      = QuickFormatCheck.IsChecked == true;
        bool compress   = CompressCheck.IsChecked    == true;
        bool secure     = SecureWipeCheck.IsChecked  == true;
        var current     = new FormatPreset("", fs, allocBytes, quick, compress, secure);

        var dlg = new PresetsDialog(current, CurrentFormatSummary(full: true), _settings) { XamlRoot = Content.XamlRoot, RequestedTheme = CurrentTheme };
        await dlg.ShowAsync();
        BuildPresetsMenu();
    }

    private async void MnuPreset_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not MenuFlyoutItem { Tag: FormatPreset preset }) return;
        if (DrivePicker.SelectedItem is not DriveViewModel || _isDriveProtected || _isBusy) return;

        int idx = FileSystemPicker.Items.IndexOf(preset.FileSystem);
        if (idx < 0)
        {
            await ShowInfoAsync(L.T("msg.warning"), L.T("preset.na", Presets.DisplayName(preset)));
            return;
        }

        FileSystemPicker.SelectedIndex = idx;
        for (int i = 0; i < _allocBytes.Count; i++)
            if (_allocBytes[i] == preset.AllocationUnit) { AllocUnitPicker.SelectedIndex = i; break; }

        QuickFormatCheck.IsChecked = preset.QuickFormat;
        CompressCheck.IsChecked    = preset.Compress && preset.FileSystem == "NTFS";
        SecureWipeCheck.IsChecked  = preset.SecureWipe;

        StatusText.ClearValue(TextBlock.ForegroundProperty);
        StatusText.Text = L.T("preset.body", Presets.DisplayName(preset));
    }
}
