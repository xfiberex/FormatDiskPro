using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Controls;

namespace FormatDiskPro.UI;

/// <summary>
/// Diálogo de confirmación reforzada: requiere escribir la letra de la unidad para habilitar el botón.
/// </summary>
/// <remarks>
/// Lo comparten las dos operaciones irreversibles de la app —formatear y reinicializar—, así que el
/// título es un <b>parámetro obligatorio</b>, no un valor por defecto. Hasta `T6-01` lo fijaba el propio
/// constructor con <c>confirm.title</c>, de modo que reinicializar (que borra el disco físico entero)
/// se anunciaba como «Confirmar formato»: el cuerpo explicaba una cosa y el título prometía otra menos
/// grave. Con el título obligatorio, una tercera operación destructiva no puede heredar el nombre
/// equivocado por omisión: quien la añada tiene que decidirlo.
/// </remarks>
public sealed partial class ConfirmDialog : ContentDialog
{
    private readonly string _letter;

    /// <param name="driveLetter">Letra que hay que teclear para habilitar el botón primario.</param>
    /// <param name="title">Título del diálogo: debe nombrar la operación que se va a ejecutar.</param>
    /// <param name="summary">Detalle de lo que se va a destruir.</param>
    public ConfirmDialog(char driveLetter, string title, string summary)
    {
        InitializeComponent();

        _letter = char.ToUpper(driveLetter).ToString();

        Title              = title;
        PrimaryButtonText  = L.T("btn.start");
        CloseButtonText    = L.T("btn.cancel");
        DefaultButton      = ContentDialogButton.None;
        IsPrimaryButtonEnabled = false;

        SummaryText.Text = summary;
        PromptText.Text  = L.T("confirm.prompt", _letter);
        // El placeholder NO lleva la letra (T6-02). Lo hacía, y hacía dos daños: el campo se leía como si
        // ya estuviera relleno —una letra gris dentro de una caja vacía es indistinguible de una escrita—
        // y ponía la respuesta dentro del hueco donde hay que teclearla, que es el único punto de fricción
        // deliberada de toda la app. La letra se dice UNA vez, en PromptText, que es una instrucción; ahí
        // hay que leerla y transcribirla, y eso es justo lo que se pretende que cueste.
        // El "…" neutro del XAML se deja tal cual: este método ya no toca PlaceholderText.
        //
        // Y por eso hace falta la línea de abajo. Al quitar el placeholder salió a la luz lo que tapaba:
        // WinUI usa el PlaceholderText como NOMBRE ACCESIBLE del TextBox cuando no hay otro, así que el
        // campo se llamaba «I» — un lector de pantalla anunciaba la respuesta en voz alta— y sin
        // placeholder pasaría a llamarse «…», que no es mejor. El nombre se fija explícito: no depende de
        // lo que se pinte dentro, y PromptText (encima, con la instrucción completa) sigue siendo quien
        // dice QUÉ letra.
        AutomationProperties.SetName(InputBox, L.T("confirm.inputName"));

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
