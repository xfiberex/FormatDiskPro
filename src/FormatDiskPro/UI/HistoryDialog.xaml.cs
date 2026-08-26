using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Text;
using Windows.UI;

namespace FormatDiskPro.UI;

/// <summary>Fila de presentación de una entrada del historial (capa UI, para enlazar con x:Bind).</summary>
public sealed record HistoryRow(string Time, string Title, string Detail, string Glyph, Brush Accent);

/// <summary>
/// Visor integrado del historial de operaciones: lista las entradas (más recientes primero),
/// con acceso a abrir el archivo crudo y a vaciar el historial (con confirmación en flyout).
/// </summary>
public sealed partial class HistoryDialog : ContentDialog
{
    private readonly bool _dark;
    private readonly IntPtr _hwnd;
    private readonly IHistory _history;
    private readonly ObservableCollection<HistoryRow> _rows = new();
    private IReadOnlyList<HistoryEntry> _all = [];   // más recientes primero
    private bool _ready;

    public HistoryDialog(bool dark, IntPtr hwnd, IHistory history)
    {
        InitializeComponent();
        _dark = dark;
        _hwnd = hwnd;
        _history = history;

        Title                      = L.T("history.title");
        CloseButtonText            = L.T("btn.close");
        SearchBox.PlaceholderText  = L.T("history.search");
        // Nombre propio, no el placeholder: WinUI lo usa como nombre accesible cuando no hay otro, y
        // apoyarse en eso fue justo lo que falló en T6-02.
        AutomationProperties.SetName(SearchBox, L.T("history.search"));
        OpenFileButton.Content     = L.T("history.open");
        ExportButton.Content       = L.T("history.export");
        ClearButton.Content        = L.T("history.clear");
        ClearConfirmText.Text      = L.T("history.clearConfirm");
        ClearConfirmButton.Content = L.T("history.clear");
        EmptyText.Text             = L.T("history.empty");

        EntriesList.ItemsSource = _rows;
        PopulateFilters();
        // Los dos filtros no tenían Header ni nombre: se anunciaban como «cuadro combinado» a secas,
        // sin decir qué filtran (T7-07). El buscador de al lado sí lo tenía desde T7-05.
        AutomationProperties.SetName(CategoryFilter, L.T("history.filter.catName"));
        AutomationProperties.SetName(ResultFilter,   L.T("history.filter.resName"));
        _ready = true;
        LoadEntries();
    }

    private void PopulateFilters()
    {
        CategoryFilter.Items.Add(new ComboBoxItem { Content = L.T("history.filter.allCat"), Tag = null });
        foreach (HistoryCategory c in Enum.GetValues<HistoryCategory>())
            CategoryFilter.Items.Add(new ComboBoxItem { Content = CategoryText(c), Tag = c });
        CategoryFilter.SelectedIndex = 0;

        ResultFilter.Items.Add(new ComboBoxItem { Content = L.T("history.filter.allRes"), Tag = null });
        foreach (HistoryResult r in Enum.GetValues<HistoryResult>())
            ResultFilter.Items.Add(new ComboBoxItem { Content = ResultText(r), Tag = r });
        ResultFilter.SelectedIndex = 0;
    }

    private void LoadEntries()
    {
        var entries = HistoryEntry.ParseAll(_history.ReadLines());
        _all = entries.Reverse().ToList();   // más recientes primero
        ApplyFilter();
    }

    private HistoryCategory? SelectedCategory()
        => (CategoryFilter.SelectedItem as ComboBoxItem)?.Tag is HistoryCategory c ? c : null;

    private HistoryResult? SelectedResult()
        => (ResultFilter.SelectedItem as ComboBoxItem)?.Tag is HistoryResult r ? r : null;

    private IEnumerable<HistoryEntry> FilteredEntries()
        => _all.Where(e => e.Matches(SearchBox.Text, SelectedCategory(), SelectedResult()));

    private void ApplyFilter()
    {
        _rows.Clear();
        foreach (var e in FilteredEntries())
            _rows.Add(ToRow(e));

        bool any = _rows.Count > 0;
        EntriesList.Visibility = any ? Visibility.Visible : Visibility.Collapsed;
        EmptyText.Visibility   = any ? Visibility.Collapsed : Visibility.Visible;
        EmptyText.Text         = _all.Count == 0 ? L.T("history.empty") : L.T("history.noMatch");
        ClearButton.IsEnabled  = _all.Count > 0;
        ExportButton.IsEnabled = any;

        // «12 de 340» mientras haya historial; con el historial vacío el recuento sobra, porque el
        // estado vacío ya lo dice con palabras. Los números se formatean con L.Culture (T6-12): van
        // dentro de una frase traducida, y string.Format los pondría en la cultura de Windows.
        CountText.Visibility = _all.Count > 0 ? Visibility.Visible : Visibility.Collapsed;
        CountText.Text = _all.Count > 0
            ? L.T("history.count", _rows.Count.ToString(L.Culture), _all.Count.ToString(L.Culture))
            : "";
    }

    private void Search_Changed(AutoSuggestBox sender, AutoSuggestBoxTextChangedEventArgs e)
    {
        if (_ready) ApplyFilter();
    }

    /// <summary>
    /// Nombra cada fila para quien no la ve (<c>T7-07</c>): «Formato · Correcto. 2026-08-18 08:42.
    /// unidad=I: fs=exFAT». Sin esto, el <c>ListViewItem</c> se anuncia con el <c>ToString()</c> del
    /// record —marca de clase y todo, incluido el <c>SolidColorBrush</c> del acento—, porque su
    /// contenido es un objeto y no una cadena. Se ve tabulando hasta la lista con un lector de pantalla.
    ///
    /// <para>Va en el <b>contenedor</b> y no en la plantilla: el nombre que se anuncia es el del
    /// <c>ListViewItem</c>, y ponerlo dentro no lo cambia.</para>
    /// </summary>
    private void EntriesList_ContainerContentChanging(ListViewBase sender, ContainerContentChangingEventArgs args)
    {
        if (args.InRecycleQueue || args.Item is not HistoryRow row) return;
        AutomationProperties.SetName(args.ItemContainer, $"{row.Title}. {row.Time}. {row.Detail}");
    }

    private void Filter_Changed(object sender, SelectionChangedEventArgs e) { if (_ready) ApplyFilter(); }

    /// <summary>
    /// Exporta a CSV lo que los filtros dejan ver, preguntando antes dónde guardarlo.
    ///
    /// <para>El diálogo es el de Windows por COM (<see cref="SaveFileDialog"/>) y <b>no</b> el
    /// <c>FileSavePicker</c> de WinRT, que en esta app —elevada siempre— fallaba en el acto sin llegar a
    /// abrirse. El porqué completo está en <see cref="SaveFileDialog"/>.</para>
    ///
    /// <para>Sigue siendo <c>async</c> por la escritura: el CSV de un historial rotado son cientos de
    /// líneas y el diálogo no debe congelarse mientras se guardan. El que ya no es asíncrono es el
    /// diálogo del sistema, que es modal por naturaleza y bombea su propio bucle de mensajes.</para>
    /// </summary>
    private async void Export_Click(object sender, RoutedEventArgs e)
    {
        ExportErrorBar.IsOpen = false;
        try
        {
            string? path = SaveFileDialog.Show(
                _hwnd,
                L.T("history.export"),
                $"FormatDiskPro-historial-{DateTime.Now:yyyyMMdd-HHmmss}",
                L.T("history.exportType"),
                ".csv",
                Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments));
            if (path is null) return;

            // UTF-8 CON BOM, como escribía FileIO.WriteTextAsync: sin él, Excel abre el CSV en la página
            // de códigos del sistema y destroza los acentos de los detalles ("Formato rápido").
            await File.WriteAllTextAsync(path, HistoryEntry.ToCsv(FilteredEntries()), new UTF8Encoding(true));
        }
        catch (Exception ex)
        {
            // Se mantiene el "no romper el diálogo" —el historial sigue abierto y utilizable— pero SIN
            // callar: un fallo silencioso aquí deja al usuario convencido de que tiene su CSV.
            string detail = ErrorText.Describe(ex);
            ExportErrorBar.Title   = L.T("history.exportFailed");
            ExportErrorBar.Message = detail;
            ExportErrorBar.IsOpen  = true;
            _history.Log($"EXPORT ERROR: {detail}");
        }
    }

    private HistoryRow ToRow(HistoryEntry e)
    {
        // El patrón ya es ISO fijo, pero '-' y ':' son marcadores de separador: sin cultura explícita los
        // pone Windows y en algunas sale "2026.08.17 13.42" dentro de un patrón que pedía guiones (T6-12).
        string time  = e.Time == DateTime.MinValue
            ? ""
            : e.Time.ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture);
        string title = $"{CategoryText(e.Category)} · {ResultText(e.Result)}";
        // Solo la fila que se ve lleva los tamaños en legible (T6-05). El CSV (HistoryEntry.ToCsv) y el
        // propio history.log siguen exportando e.Detail con el byte exacto: son formatos con consumidores.
        return new HistoryRow(time, title, HistoryEntry.Humanize(e.Detail), GlyphFor(e.Result),
                              new SolidColorBrush(ColorFor(e.Result, _dark)));
    }

    /// <summary>
    /// Abre <c>history.log</c> en el editor asociado, y <b>dice si no puede</b>.
    ///
    /// <para><c>History.Open</c> se tragaba la excepción, así que sin un editor asociado a <c>.log</c>
    /// este botón no producía ningún efecto visible: ni ventana, ni aviso. Se reusa la misma
    /// <c>InfoBar</c> del error de exportación — el diálogo ya tiene dónde contar sus fallos, y dos
    /// barras compitiendo por el mismo hueco sería peor.</para>
    /// </summary>
    private void OpenFile_Click(object sender, RoutedEventArgs e)
    {
        ExportErrorBar.IsOpen = false;
        try { _history.Open(); }
        catch (Exception ex)
        {
            ExportErrorBar.Title   = L.T("history.openFailed");
            ExportErrorBar.Message = ErrorText.Describe(ex);
            ExportErrorBar.IsOpen  = true;
            _history.Log($"HISTORY OPEN ERROR: {ErrorText.Describe(ex)}");
        }
    }

    private void ClearConfirm_Click(object sender, RoutedEventArgs e)
    {
        ClearFlyout.Hide();
        _history.Clear();
        LoadEntries();
    }

    private static string CategoryText(HistoryCategory c) => c switch
    {
        HistoryCategory.Format     => L.T("history.cat.format"),
        HistoryCategory.SecureWipe => L.T("history.cat.wipe"),
        HistoryCategory.Verify     => L.T("history.cat.verify"),
        HistoryCategory.Eject      => L.T("history.cat.eject"),
        HistoryCategory.Update     => L.T("history.cat.update"),
        _                          => L.T("history.cat.other"),
    };

    private static string ResultText(HistoryResult r) => r switch
    {
        HistoryResult.Ok        => L.T("history.res.ok"),
        HistoryResult.Fail      => L.T("history.res.fail"),
        HistoryResult.Error     => L.T("history.res.error"),
        HistoryResult.Cancelled => L.T("history.res.cancelled"),
        _                       => L.T("history.res.info"),
    };

    // Glifos de Segoe Fluent Icons por resultado (code points; sin escapes ni caracteres no-ASCII).
    private static string GlyphFor(HistoryResult r) => char.ConvertFromUtf32(r switch
    {
        HistoryResult.Ok                          => 0xE73E,   // CheckMark
        HistoryResult.Fail or HistoryResult.Error => 0xE783,   // ErrorBadge
        HistoryResult.Cancelled                   => 0xE711,   // Cancel
        _                                         => 0xE946,   // Info
    });

    // El RGB vive en Core/SeverityPalette, donde los tests miden su contraste (WCAG AA). Estos colores
    // tiñen el TÍTULO de cada fila (ver HistoryDialog.xaml), no solo el glifo: son texto normal, así que
    // les aplica el 4.5:1, no el 3:1 de los objetos gráficos.
    private static Color ColorFor(HistoryResult r, bool dark) => SeverityPalette.ForResult(r, dark);
}
