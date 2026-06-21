using Microsoft.UI.Text;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Windows.UI;

namespace FormatDiskPro.UI;

/// <summary>
/// Diálogo de salud del disco (S.M.A.R.T. ampliado): consulta bajo demanda los contadores de
/// fiabilidad del disco físico de la unidad seleccionada y muestra el detalle con fallback
/// "No disponible" para los valores que la unidad no expone (típico en USB).
/// </summary>
public sealed partial class HealthDialog : ContentDialog
{
    private readonly bool _dark;
    private readonly char _letter;
    private readonly string _driveLabel;

    public HealthDialog(bool dark, char letter, string driveLabel)
    {
        InitializeComponent();
        _dark = dark;
        _letter = letter;
        _driveLabel = driveLabel;

        Title           = L.T("health.title");
        CloseButtonText = L.T("btn.close");
        StatusText.Text = L.T("health.querying");
        NoteText.Text   = L.T("health.note");
        NoteText.Visibility = Visibility.Collapsed;

        Opened += OnOpened;
    }

    private async void OnOpened(ContentDialog sender, ContentDialogOpenedEventArgs args)
    {
        Opened -= OnOpened;
        var info = await DiskService.GetSmartAsync(_letter);
        Populate(info);
    }

    private void Populate(SmartInfo? info)
    {
        StatusText.Visibility = Visibility.Collapsed;
        RowsPanel.Children.Clear();

        AddRow(L.T("health.drive"), _driveLabel);

        if (info is null)
        {
            AddRow(L.T("health.status"), L.T("health.na"));
            NoteText.Visibility = Visibility.Visible;
            return;
        }

        AddRow(L.T("health.status"),  Show(info.Health), HealthBrush(info.Health));
        AddRow(L.T("health.bus"),     Show(info.Bus));
        AddRow(L.T("health.media"),   Show(info.Media));
        AddRow(L.T("health.spindle"), SpindleText(info));
        AddRow(L.T("health.temp"),    info.TemperatureC is int t ? L.T("health.unit.temp", t)    : L.T("health.na"));
        AddRow(L.T("health.hours"),   info.PowerOnHours is long h ? L.T("health.unit.hours", h)   : L.T("health.na"));
        AddRow(L.T("health.wear"),    info.WearPercent is int w  ? L.T("health.unit.percent", w)  : L.T("health.na"));
        AddRow(L.T("health.readErr"),  info.ReadErrors?.ToString()  ?? L.T("health.na"));
        AddRow(L.T("health.writeErr"), info.WriteErrors?.ToString() ?? L.T("health.na"));

        NoteText.Visibility = Visibility.Visible;
    }

    private string SpindleText(SmartInfo info)
    {
        if (info.SpindleSpeedRpm is uint rpm)
            return rpm == 0 ? "SSD" : L.T("health.unit.rpm", rpm);
        return info.Media.Contains("SSD", StringComparison.OrdinalIgnoreCase) ? "SSD" : L.T("health.na");
    }

    private static string Show(string v) => string.IsNullOrEmpty(v) || v == "?" ? "—" : v;

    private void AddRow(string label, string value, Brush? valueBrush = null)
    {
        var grid = new Grid { ColumnSpacing = 16 };
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(150) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

        var lbl = new TextBlock
        {
            Text = label, FontSize = 13, Opacity = 0.7, TextWrapping = TextWrapping.Wrap,
        };
        var val = new TextBlock
        {
            Text = value, FontSize = 13, FontWeight = FontWeights.SemiBold, TextWrapping = TextWrapping.Wrap,
            HorizontalAlignment = HorizontalAlignment.Right, TextAlignment = TextAlignment.Right,
        };
        if (valueBrush is not null) val.Foreground = valueBrush;

        Grid.SetColumn(lbl, 0);
        Grid.SetColumn(val, 1);
        grid.Children.Add(lbl);
        grid.Children.Add(val);
        RowsPanel.Children.Add(grid);
    }

    private Brush HealthBrush(string health)
    {
        bool healthy = health.Equals("Healthy", StringComparison.OrdinalIgnoreCase);
        Color c = healthy
            ? (_dark ? Color.FromArgb(255, 0x6C, 0xCB, 0x5F) : Color.FromArgb(255, 0x0F, 0x7B, 0x0F))
            : (_dark ? Color.FromArgb(255, 0xFF, 0x99, 0xA4) : Color.FromArgb(255, 0xC4, 0x2B, 0x1C));
        return new SolidColorBrush(c);
    }
}
