using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using System.Collections.ObjectModel;
using System.Linq;
using Windows.Storage;
using Windows.Storage.Pickers;
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
    private readonly ObservableCollection<HistoryRow> _rows = new();
    private IReadOnlyList<HistoryEntry> _all = [];   // más recientes primero
    private bool _ready;

    public HistoryDialog(bool dark, IntPtr hwnd)
    {
        InitializeComponent();
        _dark = dark;
        _hwnd = hwnd;

        Title                      = L.T("history.title");
        CloseButtonText            = L.T("btn.close");
        SearchBox.PlaceholderText  = L.T("history.search");
        OpenFileButton.Content     = L.T("history.open");
        ExportButton.Content       = L.T("history.export");
        ClearButton.Content        = L.T("history.clear");
        ClearConfirmText.Text      = L.T("history.clearConfirm");
        ClearConfirmButton.Content = L.T("history.clear");
        EmptyText.Text             = L.T("history.empty");

        EntriesList.ItemsSource = _rows;
        PopulateFilters();
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
        var entries = HistoryEntry.ParseAll(History.ReadLines());
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
    }

    private void Search_Changed(object sender, TextChangedEventArgs e) { if (_ready) ApplyFilter(); }
    private void Filter_Changed(object sender, SelectionChangedEventArgs e) { if (_ready) ApplyFilter(); }

    private async void Export_Click(object sender, RoutedEventArgs e)
    {
        var picker = new FileSavePicker
        {
            SuggestedStartLocation = PickerLocationId.DocumentsLibrary,
            SuggestedFileName = $"FormatDiskPro-historial-{DateTime.Now:yyyyMMdd-HHmmss}",
        };
        picker.FileTypeChoices.Add("CSV", new List<string> { ".csv" });
        WinRT.Interop.InitializeWithWindow.Initialize(picker, _hwnd);

        try
        {
            var file = await picker.PickSaveFileAsync();
            if (file is null) return;
            await FileIO.WriteTextAsync(file, HistoryEntry.ToCsv(FilteredEntries()));
        }
        catch { /* exportar nunca debe romper el diálogo */ }
    }

    private HistoryRow ToRow(HistoryEntry e)
    {
        string time  = e.Time == DateTime.MinValue ? "" : e.Time.ToString("yyyy-MM-dd HH:mm");
        string title = $"{CategoryText(e.Category)} · {ResultText(e.Result)}";
        return new HistoryRow(time, title, e.Detail, GlyphFor(e.Result),
                              new SolidColorBrush(ColorFor(e.Result, _dark)));
    }

    private void OpenFile_Click(object sender, RoutedEventArgs e) => History.Open();

    private void ClearConfirm_Click(object sender, RoutedEventArgs e)
    {
        ClearFlyout.Hide();
        History.Clear();
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

    // Color semántico por resultado, según el tema efectivo.
    private static Color ColorFor(HistoryResult r, bool dark) => r switch
    {
        HistoryResult.Ok                          => dark ? Color.FromArgb(255, 0x6C, 0xCB, 0x5F) : Color.FromArgb(255, 0x0F, 0x7B, 0x0F),
        HistoryResult.Fail or HistoryResult.Error => dark ? Color.FromArgb(255, 0xFF, 0x99, 0xA4) : Color.FromArgb(255, 0xC4, 0x2B, 0x1C),
        HistoryResult.Cancelled                   => dark ? Color.FromArgb(255, 0x9B, 0x9B, 0x9B) : Color.FromArgb(255, 0x86, 0x86, 0x86),
        _                                         => dark ? Color.FromArgb(255, 0xFF, 0xFF, 0xFF) : Color.FromArgb(255, 0x19, 0x19, 0x19),
    };
}
