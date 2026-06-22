using System.Collections.ObjectModel;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace FormatDiskPro.UI;

/// <summary>
/// Diálogo para gestionar presets propios: guardar la configuración de formato actual con un nombre y
/// eliminar presets guardados. Edita y persiste <see cref="AppSettings.UserPresets"/>; la ventana
/// principal reconstruye el menú de presets al cerrarse.
/// </summary>
public sealed partial class PresetsDialog : ContentDialog
{
    private readonly FormatPreset _current;   // configuración actual a guardar (sin nombre)
    private readonly AppSettings  _settings;
    private readonly ObservableCollection<FormatPreset> _userPresets;

    /// <summary>Crea el diálogo con la configuración actual, su resumen legible y la configuración persistida.</summary>
    /// <param name="current">Configuración de formato actual (el nombre se toma del cuadro de texto).</param>
    /// <param name="currentSummary">Resumen legible de la configuración actual (p. ej. "NTFS · 4 KB · rápido").</param>
    /// <param name="settings">Preferencias persistidas a editar.</param>
    public PresetsDialog(FormatPreset current, string currentSummary, AppSettings settings)
    {
        InitializeComponent();
        _current  = current;
        _settings = settings;
        _userPresets = [.. settings.UserPresets];

        Title           = L.T("preset.manage");
        CloseButtonText = L.T("btn.close");
        SaveHeader.Text = L.T("preset.saveHeader");
        CurrentText.Text= L.T("preset.currentIs", currentSummary);
        NameBox.Header  = L.T("preset.nameLabel");
        NameBox.PlaceholderText = L.T("preset.namePlaceholder");
        SaveBtn.Content = L.T("preset.saveBtn");
        ListHeader.Text = L.T("preset.yourPresets");
        EmptyText.Text  = L.T("preset.empty");

        PresetsList.ItemsSource = _userPresets;
        UpdateEmptyState();
    }

    private IEnumerable<string> ExistingNames() =>
        Presets.All.Select(p => p.Name).Concat(_userPresets.Select(p => p.Name));

    private void SaveBtn_Click(object sender, RoutedEventArgs e)
    {
        string name = Presets.NormalizeName(NameBox.Text);
        if (!Presets.IsNameAvailable(name, ExistingNames()))
        {
            ErrorText.Text = L.T("preset.dupName");
            ErrorText.Visibility = Visibility.Visible;
            return;
        }

        ErrorText.Visibility = Visibility.Collapsed;
        var preset = _current with { Name = name };
        _userPresets.Add(preset);
        _settings.UserPresets.Add(preset);
        _settings.Save();

        NameBox.Text = "";
        UpdateEmptyState();
    }

    private void DeleteBtn_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not FrameworkElement { Tag: FormatPreset preset }) return;
        _userPresets.Remove(preset);
        _settings.UserPresets.Remove(preset);   // record → igualdad por valor
        _settings.Save();
        UpdateEmptyState();
    }

    private void UpdateEmptyState()
    {
        bool empty = _userPresets.Count == 0;
        EmptyText.Visibility   = empty ? Visibility.Visible : Visibility.Collapsed;
        PresetsList.Visibility = empty ? Visibility.Collapsed : Visibility.Visible;
    }
}
