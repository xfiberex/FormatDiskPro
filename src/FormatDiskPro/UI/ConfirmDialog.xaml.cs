using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

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
        InputBox.PlaceholderText = _letter;

        InputBox.TextChanged += (_, _) =>
        {
            bool match = InputBox.Text.Trim().ToUpperInvariant() == _letter;
            IsPrimaryButtonEnabled = match;
            // Enter confirma solo cuando la letra coincide: se mantiene la fricción deliberada
            // (escribir la letra) sin obligar a soltar el teclado para pulsar el botón.
            DefaultButton = match ? ContentDialogButton.Primary : ContentDialogButton.None;
        };

        Opened += (_, _) => InputBox.Focus(FocusState.Programmatic);
    }
}
