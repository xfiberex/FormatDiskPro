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

        Title               = L.T("health.title");
        SecondaryButtonText = L.T("health.refresh");
        CloseButtonText     = L.T("btn.close");
        StatusText.Text     = L.T("health.querying");
        NoteText.Text       = L.T("health.note");
        NoteText.Visibility = Visibility.Collapsed;

        Opened += OnOpened;
        SecondaryButtonClick += OnRefresh;
    }

    private async void OnOpened(ContentDialog sender, ContentDialogOpenedEventArgs args)
    {
        Opened -= OnOpened;
        await QueryAndPopulateAsync();
    }

    /// <summary>Re-consulta los contadores S.M.A.R.T. sin cerrar el diálogo (botón Actualizar).</summary>
    private async void OnRefresh(ContentDialog sender, ContentDialogButtonClickEventArgs args)
    {
        var deferral = args.GetDeferral();
        args.Cancel = true;   // mantener el diálogo abierto
        try { await QueryAndPopulateAsync(); }
        finally { deferral.Complete(); }
    }

    private async Task QueryAndPopulateAsync()
    {
        StatusText.Visibility = Visibility.Visible;
        StatusText.Text = L.T("health.querying");
        RowsPanel.Children.Clear();
        NoteText.Visibility = Visibility.Collapsed;
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

        AddMetricRow(L.T("health.status"), Show(info.Health), SmartInfo.HealthLevel(info.Health));
        AddRow(L.T("health.bus"),     Show(info.Bus));
        AddRow(L.T("health.media"),   Show(info.Media));
        AddRow(L.T("health.spindle"), SpindleText(info));
        AddMetricRow(L.T("health.temp"),
            info.TemperatureC is int t ? L.T("health.unit.temp", t) : L.T("health.na"),
            SmartInfo.TemperatureLevel(info.TemperatureC));
        AddRow(L.T("health.hours"),   info.PowerOnHours is long h ? L.T("health.unit.hours", h) : L.T("health.na"));
        AddMetricRow(L.T("health.wear"),
            info.WearPercent is int w ? L.T("health.unit.percent", w) : L.T("health.na"),
            SmartInfo.WearLevel(info.WearPercent));
        AddMetricRow(L.T("health.readErr"),
            info.ReadErrors?.ToString() ?? L.T("health.na"), SmartInfo.ErrorLevel(info.ReadErrors));
        AddMetricRow(L.T("health.writeErr"),
            info.WriteErrors?.ToString() ?? L.T("health.na"), SmartInfo.ErrorLevel(info.WriteErrors));

        NoteText.Visibility = Visibility.Visible;
    }

    /// <summary>
    /// Fila de una métrica con umbral: añade un texto de estado (no solo color, por accesibilidad)
    /// y colorea el valor según el nivel. Para <see cref="SmartLevel.Unknown"/> no añade ni color ni texto.
    /// </summary>
    private void AddMetricRow(string label, string baseValue, SmartLevel level)
    {
        if (level == SmartLevel.Unknown)
        {
            AddRow(label, baseValue);
            return;
        }
        AddRow(label, $"{baseValue} — {LevelLabel(level)}", LevelBrush(level, _dark));
    }

    private static string LevelLabel(SmartLevel level) => level switch
    {
        SmartLevel.Ok       => L.T("health.level.ok"),
        SmartLevel.Warning  => L.T("health.level.warning"),
        SmartLevel.Critical => L.T("health.level.critical"),
        _                   => "",
    };

    /// <summary>
    /// Pincel Fluent (verde/ámbar/rojo) para un nivel S.M.A.R.T. según el tema efectivo.
    /// Compartido con la línea «Salud:» de la tarjeta principal (<c>MainWindow.RenderHealth</c>).
    /// </summary>
    internal static Brush LevelBrush(SmartLevel level, bool dark)
    {
        Color c = level switch
        {
            SmartLevel.Ok       => dark ? Color.FromArgb(255, 0x6C, 0xCB, 0x5F) : Color.FromArgb(255, 0x0F, 0x7B, 0x0F),
            SmartLevel.Warning  => dark ? Color.FromArgb(255, 0xFC, 0xC8, 0x4A) : Color.FromArgb(255, 0x9D, 0x5D, 0x00),
            SmartLevel.Critical => dark ? Color.FromArgb(255, 0xFF, 0x99, 0xA4) : Color.FromArgb(255, 0xC4, 0x2B, 0x1C),
            _                   => dark ? Color.FromArgb(255, 0xFF, 0xFF, 0xFF) : Color.FromArgb(255, 0x00, 0x00, 0x00),
        };
        return new SolidColorBrush(c);
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
}
