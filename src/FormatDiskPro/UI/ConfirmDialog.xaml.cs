using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Windows.UI;

namespace FormatDiskPro.UI;

/// <summary>
/// Diálogo de confirmación reforzada: requiere escribir la letra de la unidad para habilitar el botón.
/// </summary>
public sealed partial class ConfirmDialog : ContentDialog
{
    private readonly string _letter;

    public ConfirmDialog(char driveLetter, string summary)
    {
        InitializeComponent();

        _letter = char.ToUpper(driveLetter).ToString();

        Title              = L.T("confirm.title");
        PrimaryButtonText  = L.T("btn.start");
        CloseButtonText    = L.T("btn.cancel");
        DefaultButton      = ContentDialogButton.None;
        IsPrimaryButtonEnabled = false;

        SummaryText.Text = summary;
        PromptText.Text  = L.T("confirm.prompt", _letter);
        PromptText.Foreground = new SolidColorBrush(Color.FromArgb(255, 192, 0, 0));
        InputBox.PlaceholderText = _letter;

        InputBox.TextChanged += (_, _) =>
            IsPrimaryButtonEnabled = InputBox.Text.Trim().ToUpperInvariant() == _letter;
    }
}
