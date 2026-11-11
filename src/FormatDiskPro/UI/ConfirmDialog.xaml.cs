// SPDX-License-Identifier: GPL-3.0-only
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
///
/// <para><b>El verbo del botón, por lo mismo</b> (`T13-03`). `T6-01` hizo obligatorio el título y dejó
/// el botón en <c>btn.start</c>: reinicializar se confirmaba con «Formatear», y en cuanto la letra
/// coincide <c>Enter</c> pulsa ese botón. Ahora también es un parámetro obligatorio, y los llamantes
/// nombran la unidad, como el botón principal de la ventana (`T12-02`).</para>
/// </remarks>
public sealed partial class ConfirmDialog : ContentDialog
{
    private readonly string _letter;

    /// <param name="driveLetter">Letra que hay que teclear para habilitar el botón primario.</param>
    /// <param name="title">Título del diálogo: debe nombrar la operación que se va a ejecutar.</param>
    /// <param name="primaryButtonText">
    /// Texto del botón que ejecuta la operación: su verbo y la unidad, p. ej. «Reinicializar F:».
    /// </param>
    /// <param name="content">Qué se pierde, sobre qué unidad y qué se aplica (`AUD-072`).</param>
    public ConfirmDialog(char driveLetter, string title, string primaryButtonText, ConfirmContent content)
    {
        InitializeComponent();

        _letter = char.ToUpper(driveLetter).ToString();

        Title              = title;
        PrimaryButtonText  = primaryButtonText;
        CloseButtonText    = L.T("btn.cancel");
        DefaultButton      = ContentDialogButton.None;
        IsPrimaryButtonEnabled = false;

        WarningBar.Title   = content.Warning;
        WarningBar.Message = L.T("confirm.irreversible");

        TargetNameText.Text = content.TargetName;
        TargetDetailText.Text = content.TargetDetail;
        TargetDetailText.Visibility = string.IsNullOrEmpty(content.TargetDetail) ? Visibility.Collapsed : Visibility.Visible;
        AtRiskText.Text = content.AtRisk;

        PlanTitleText.Text = content.PlanTitle;
        if (!string.IsNullOrEmpty(content.Plan))
        {
            SummaryText.Text = content.Plan;
            SummaryText.Visibility = Visibility.Visible;
        }
        AddDetails(content.PlanDetails ?? []);
        if (!string.IsNullOrEmpty(content.Note))
        {
            NoteText.Text = content.Note;
            NoteText.Visibility = Visibility.Visible;
        }
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

    private void AddDetails(IReadOnlyList<(string Label, string Value)> details)
    {
        if (details.Count == 0) return;

        var labelStyle = (Style)Resources["ConfirmRowLabelStyle"];
        var valueStyle = (Style)Resources["ConfirmRowValueStyle"];
        for (int row = 0; row < details.Count; row++)
        {
            DetailsGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            var label = new TextBlock { Text = details[row].Label, Style = labelStyle };
            var value = new TextBlock { Text = details[row].Value, Style = valueStyle };
            Grid.SetRow(label, row);
            Grid.SetRow(value, row);
            Grid.SetColumn(value, 1);
            DetailsGrid.Children.Add(label);
            DetailsGrid.Children.Add(value);
        }
        DetailsGrid.Visibility = Visibility.Visible;
    }
}

/// <summary>
/// Lo que enseña una confirmación reforzada, en el orden en que se lee (`AUD-072`).
/// </summary>
/// <param name="Warning">Qué se pierde, en una frase: título del aviso de error.</param>
/// <param name="TargetName">La unidad como la nombra el selector, p. ej. «D: (Datos)».</param>
/// <param name="TargetDetail">
/// Línea de contexto del objetivo (modelo, tamaño, conexión, tipo), unida con <see cref="InfoLine.Join"/>;
/// vacía si no se sabe nada más.
/// </param>
/// <param name="AtRisk">
/// Lo que contiene y se va a perder: los datos de un volumen o las unidades de un disco. Nunca vacío: si no
/// se pudo leer, lo dice.
/// </param>
/// <param name="PlanTitle">Rótulo de lo que se va a aplicar.</param>
/// <param name="Plan">Lo que se va a crear, en prosa (opcional).</param>
/// <param name="PlanDetails">
/// Filas (etiqueta, valor) de lo que se aplica, en dos columnas. Van aparte, y no alineadas con espacios,
/// porque la longitud de las etiquetas cambia con el idioma (`T13-04`).
/// </param>
/// <param name="Note">Aclaración opcional al pie.</param>
public sealed record ConfirmContent(
    string Warning, string TargetName, string TargetDetail, string AtRisk, string PlanTitle,
    string? Plan = null, IReadOnlyList<(string Label, string Value)>? PlanDetails = null, string? Note = null);
