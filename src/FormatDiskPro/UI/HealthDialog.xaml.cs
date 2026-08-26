using Microsoft.UI.Text;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;

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
    private readonly IDiskService _disk;

    public HealthDialog(bool dark, char letter, string driveLabel, IDiskService disk)
    {
        InitializeComponent();
        _dark = dark;
        _disk = disk;
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
        var info = await _disk.GetSmartAsync(_letter);
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

        AddHealthStatusRow(info.Health);
        AddRow(L.T("health.bus"),     Show(info.Bus));
        AddRow(L.T("health.media"),   Show(info.Media));
        // La fila del eje solo aparece si hay eje (T6-03). En un SSD no es un dato que falte: es una
        // pregunta que no aplica, y la fila de encima ya dice «Tipo de medio: SSD». Antes se rellenaba con
        // el literal "SSD" —una velocidad cuyo valor era un tipo de medio, y texto sin traducir fuera de
        // Localization/—. Si NO se sabe si gira, la fila sí se muestra, como «no disponible»: esconderla
        // por desconocimiento sería afirmar que es de estado sólido sin saberlo.
        if (SmartInfo.HasSpindle(info))
            AddRow(L.T("health.spindle"),
                info.SpindleSpeedRpm is uint rpm ? L.T("health.unit.rpm", rpm) : L.T("health.na"));
        AddMetricRow(L.T("health.temp"),
            info.TemperatureC is int t ? L.T("health.unit.temp", t) : L.T("health.na"),
            SmartInfo.TemperatureLevel(info.TemperatureC));
        AddRow(L.T("health.hours"),   PowerOnHoursText(info.PowerOnHours));
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

    /// <summary>
    /// Fila «Estado de salud», con el estado <b>traducido</b> (`T9-10`).
    ///
    /// <para>Antes pasaba por <see cref="AddMetricRow"/> con el valor <b>crudo</b> del proveedor de
    /// Storage, que es una enumeración <b>siempre en inglés</b> (<c>Healthy</c>/<c>Warning</c>/
    /// <c>Unhealthy</c>). El resultado era la fila «Estado de salud: <c>Healthy — Normal</c>»: el valor
    /// inglés y su traducción, uno al lado del otro, en el único diálogo de la app que por lo demás está
    /// entero en el idioma elegido. Y quien usa un lector de pantalla oía la palabra inglesa dentro de
    /// una frase en español.</para>
    ///
    /// <para>Lo que hacía falta ya existía: <see cref="SmartInfo.HealthLevel"/> clasifica el valor y las
    /// claves <c>health.level.*</c> están traducidas a los cinco idiomas desde `#16` — solo se usaban
    /// para elegir el <b>color</b>. Aquí se usan también para el texto, que es lo que se lee.</para>
    ///
    /// <para>Con <see cref="SmartLevel.Unknown"/> se conserva el valor crudo: no hay nada que traducir,
    /// y esconder lo que el disco reportó sería peor que enseñarlo en inglés.</para>
    /// </summary>
    /// <param name="rawHealth">Estado tal como lo reporta el disco físico.</param>
    private void AddHealthStatusRow(string rawHealth)
    {
        SmartLevel level = SmartInfo.HealthLevel(rawHealth);

        if (level == SmartLevel.Unknown)
        {
            AddRow(L.T("health.status"), Show(rawHealth));
            return;
        }

        AddRow(L.T("health.status"), LevelLabel(level), LevelBrush(level, _dark));
    }

    /// <summary>
    /// Nombre traducido de un nivel S.M.A.R.T. <c>internal</c> porque la tarjeta de la ventana principal
    /// necesita el mismo texto para su línea «Salud:» (`T9-10`), igual que ya compartía
    /// <see cref="LevelBrush"/> para el color.
    /// </summary>
    /// <param name="level">Nivel a nombrar.</param>
    internal static string LevelLabel(SmartLevel level) => level switch
    {
        SmartLevel.Ok       => L.T("health.level.ok"),
        SmartLevel.Warning  => L.T("health.level.warning"),
        SmartLevel.Critical => L.T("health.level.critical"),
        _                   => "",
    };

    /// <summary>
    /// Pincel Fluent (verde/ámbar/rojo) para un nivel S.M.A.R.T. según el tema efectivo.
    /// Compartido con la línea «Salud:» de la tarjeta principal (<c>MainWindow.RenderHealth</c>).
    /// El RGB vive en <see cref="SeverityPalette"/>, donde los tests miden su contraste (WCAG AA).
    /// </summary>
    internal static Brush LevelBrush(SmartLevel level, bool dark) =>
        new SolidColorBrush(SeverityPalette.For(level, dark));

    /// <summary>
    /// Horas de encendido con separador de millares y, si aporta, su equivalencia legible:
    /// <c>32.161 h (≈ 3,7 años)</c>. El reparto en tramos es lógica pura y vive en
    /// <see cref="SmartInfo.PowerOnEquivalent"/>; aquí solo se le pone idioma y formato de número.
    /// </summary>
    private static string PowerOnHoursText(long? hours)
    {
        if (hours is not long h) return L.T("health.na");

        string exact = h.ToString("N0", L.Culture);
        var span = SmartInfo.PowerOnEquivalent(h);
        if (span.Unit == SmartInfo.PowerOnUnit.None) return L.T("health.unit.hours", exact);

        string key = span.Unit switch
        {
            SmartInfo.PowerOnUnit.Days   => "health.span.days",
            SmartInfo.PowerOnUnit.Months => "health.span.months",
            _                            => "health.span.years",
        };
        return L.T("health.unit.hoursWith", exact, L.T(key, span.Value.ToString("0.0", L.Culture)));
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
