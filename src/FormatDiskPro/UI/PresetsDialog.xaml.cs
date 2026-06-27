using System.Collections.ObjectModel;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
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
    private readonly string _currentSummary;
    private readonly AppSettings  _settings;
    private readonly ObservableCollection<FormatPreset> _userPresets;
    private int _editIndex = -1;              // -1 = modo añadir; si no, índice editado en _userPresets

    /// <summary>Crea el diálogo con la configuración actual, su resumen legible y la configuración persistida.</summary>
    /// <param name="current">Configuración de formato actual (el nombre se toma del cuadro de texto).</param>
    /// <param name="currentSummary">Resumen legible de la configuración actual (p. ej. "NTFS · 4 KB · rápido").</param>
    /// <param name="settings">Preferencias persistidas a editar.</param>
    public PresetsDialog(FormatPreset current, string currentSummary, AppSettings settings)
    {
        InitializeComponent();
        _current  = current;
        _currentSummary = currentSummary;
        _settings = settings;
        _userPresets = [.. settings.UserPresets];

        Title           = L.T("preset.manage");
        CloseButtonText = L.T("btn.close");
        SaveHeader.Text = L.T("preset.saveHeader");
        CurrentText.Text= L.T("preset.currentIs", currentSummary);
        NameBox.Header  = L.T("preset.nameLabel");
        NameBox.PlaceholderText = L.T("preset.namePlaceholder");
        SaveBtn.Content = L.T("preset.saveBtn");
        CancelEditBtn.Content = L.T("preset.cancelEdit");
        UpdateConfigCheck.Content = L.T("preset.updateConfig", currentSummary);
        ListHeader.Text = L.T("preset.yourPresets");
        EmptyText.Text  = L.T("preset.empty");

        PresetsList.ItemsSource = _userPresets;
        UpdateEmptyState();
    }

    private IEnumerable<string> ExistingNames() =>
        Presets.All.Select(p => p.Name).Concat(_userPresets.Select(p => p.Name));

    /// <summary>Fija el nombre accesible y el tooltip (localizados) de cada botón de icono de fila.</summary>
    private void IconBtn_Loaded(object sender, RoutedEventArgs e)
    {
        if (sender is not Button b) return;
        string label = L.T(b.Name switch
        {
            "MoveUpBtn"     => "preset.moveUp",
            "MoveDownBtn"   => "preset.moveDown",
            "EditPresetBtn" => "preset.edit",
            _               => "preset.delete",
        });
        AutomationProperties.SetName(b, label);
        ToolTipService.SetToolTip(b, label);
    }

    private void SaveBtn_Click(object sender, RoutedEventArgs e)
    {
        string name = Presets.NormalizeName(NameBox.Text);

        if (_editIndex >= 0)   // editar preset existente
        {
            FormatPreset original = _userPresets[_editIndex];
            if (!Presets.IsRenameAvailable(name, original.Name, ExistingNames()))
            {
                ShowError();
                return;
            }
            FormatPreset baseConfig = UpdateConfigCheck.IsChecked == true ? _current : original;
            _userPresets[_editIndex] = baseConfig with { Name = name };
            Persist();
            ExitEditMode();
            return;
        }

        // añadir preset nuevo
        if (!Presets.IsNameAvailable(name, ExistingNames()))
        {
            ShowError();
            return;
        }
        ErrorText.Visibility = Visibility.Collapsed;
        _userPresets.Add(_current with { Name = name });
        Persist();
        NameBox.Text = "";
        UpdateEmptyState();
    }

    private void EditBtn_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not FrameworkElement { Tag: FormatPreset preset }) return;
        int idx = _userPresets.IndexOf(preset);
        if (idx < 0) return;

        _editIndex = idx;
        NameBox.Text = preset.Name;
        UpdateConfigCheck.IsChecked = false;
        UpdateConfigCheck.Visibility = Visibility.Visible;
        CancelEditBtn.Visibility = Visibility.Visible;
        SaveHeader.Text = L.T("preset.editHeader");
        SaveBtn.Content = L.T("preset.updateBtn");
        ErrorText.Visibility = Visibility.Collapsed;
        NameBox.Focus(FocusState.Programmatic);
    }

    private void CancelEdit_Click(object sender, RoutedEventArgs e) => ExitEditMode();

    private void ExitEditMode()
    {
        _editIndex = -1;
        NameBox.Text = "";
        UpdateConfigCheck.Visibility = Visibility.Collapsed;
        UpdateConfigCheck.IsChecked = false;
        CancelEditBtn.Visibility = Visibility.Collapsed;
        SaveHeader.Text = L.T("preset.saveHeader");
        SaveBtn.Content = L.T("preset.saveBtn");
        ErrorText.Visibility = Visibility.Collapsed;
    }

    private void MoveUp_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not FrameworkElement { Tag: FormatPreset preset }) return;
        ExitEditMode();
        int i = _userPresets.IndexOf(preset);
        if (i > 0) { _userPresets.Move(i, i - 1); Persist(); }
    }

    private void MoveDown_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not FrameworkElement { Tag: FormatPreset preset }) return;
        ExitEditMode();
        int i = _userPresets.IndexOf(preset);
        if (i >= 0 && i < _userPresets.Count - 1) { _userPresets.Move(i, i + 1); Persist(); }
    }

    private void DeleteBtn_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not FrameworkElement { Tag: FormatPreset preset }) return;
        ExitEditMode();
        _userPresets.Remove(preset);
        Persist();
        UpdateEmptyState();
    }

    /// <summary>Sincroniza el orden/contenido persistido con la lista mostrada y guarda.</summary>
    private void Persist()
    {
        _settings.UserPresets.Clear();
        _settings.UserPresets.AddRange(_userPresets);
        _settings.Save();
    }

    private void ShowError()
    {
        ErrorText.Text = L.T("preset.dupName");
        ErrorText.Visibility = Visibility.Visible;
    }

    private void UpdateEmptyState()
    {
        bool empty = _userPresets.Count == 0;
        EmptyText.Visibility   = empty ? Visibility.Visible : Visibility.Collapsed;
        PresetsList.Visibility = empty ? Visibility.Collapsed : Visibility.Visible;
    }
}
