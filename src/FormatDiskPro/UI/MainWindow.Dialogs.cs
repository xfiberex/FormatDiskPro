// SPDX-License-Identifier: GPL-3.0-only
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml.Controls;

namespace FormatDiskPro.UI;

/// <summary>
/// Diálogos: la compuerta de un solo <see cref="ContentDialog"/> a la vez, los avisos y confirmaciones, y el cierre de la ventana.
///
/// <para>Parte de <see cref="MainWindow"/>: es la MISMA clase, repartida en archivos por asunto (T2-08,
/// `AUD-042`). No es un rediseño y no cambia comportamiento.</para>
/// </summary>
public sealed partial class MainWindow
{
    // ── Dialog helpers ────────────────────────────────────────────

    /// <summary>
    /// Diálogo abierto ahora mismo, o <c>null</c>. Es la compuerta de <see cref="ShowOneAsync"/>.
    /// </summary>
    private ContentDialog? _openDialog;

    /// <summary>
    /// Abre un <see cref="ContentDialog"/> <b>solo si no hay otro abierto</b>; si lo hay, no lo abre y
    /// devuelve <see cref="ContentDialogResult.None"/>.
    /// </summary>
    /// <remarks>
    /// <para><b>Por qué</b> (`T13-19`). WinUI admite un único <c>ContentDialog</c> a la vez: el segundo
    /// lanza <c>COMException 0x80000019</c>. Y estos flujos son <c>async void</c>, así que esa excepción
    /// no la recoge nadie: termina en la red global de <see cref="App"/>, que deja la app viva pero
    /// escribe un <c>CRASH</c> en el historial. Pasó de verdad, en mitad de una tanda de capturas.</para>
    ///
    /// <para><b>No es una carrera teórica:</b> con el ratón no se llega —el modal tapa la ventana—, pero
    /// por UI Automation sí, y sobre todo cuando un diálogo se abre <b>solo</b> (novedades al estrenar
    /// versión, aviso de actualización) justo mientras alguien pulsa un botón.</para>
    ///
    /// <para><b>Por qué descartar y no encolar.</b> El segundo diálogo siempre responde a una acción que
    /// el usuario puede repetir en cuanto cierre el primero, y encolarlo le haría aparecer un diálogo
    /// «solo» un rato después, sin saber a qué contesta. Se deja rastro en el historial para que no sea
    /// un silencio: ahí está la diferencia con tragarse el error.</para>
    ///
    /// <para><c>None</c> es lo que ya devuelve un diálogo cerrado sin elegir nada, así que quien confirma
    /// con <c>== ContentDialogResult.Primary</c> entiende el rechazo sin cambiar una línea.</para>
    /// </remarks>
    private async Task<ContentDialogResult> ShowOneAsync(ContentDialog dialog)
    {
        // La única excepción a «descartar el segundo» (`AUD-028`): la pregunta «¿Cancelar?» abierta cuando la
        // operación ya ha terminado. Lo que llega entonces es su resultado —éxito, error, «se crearon 1 de 2
        // particiones»—, que no responde a nada que el usuario pueda repetir, y la pregunta ya no tiene objeto:
        // cancelar algo que terminó no hace nada (CloseButton_Click lo comprueba al volver). Se cierra la
        // pregunta y se enseña el resultado, como hace ConfirmDialog cuando cambia la lista de unidades.
        if (_openDialog is not null && ReferenceEquals(_openDialog, _cancelQuestion))
            await DismissCancelQuestionAsync(_cancelQuestion);

        if (_openDialog is not null)
        {
            _services.History.Log($"DIALOG SKIPPED: '{dialog.Title}' (ya había otro abierto: '{_openDialog.Title}')");
            return ContentDialogResult.None;
        }

        _openDialog = dialog;
        // La compuerta se abre en cuanto el diálogo EMPIEZA a cerrarse, y no al volver de ShowAsync.
        // Entre las dos cosas hay un salto por la cola del despachador, y ahí cabe de sobra otra acción
        // del usuario: cerrar un diálogo y pulsar Ctrl+H acto seguido llegaba con la compuerta todavía
        // cerrada, y el historial no se abría. Medido en la suite de UI, donde fallaron tres pruebas por
        // esto. WinUI, por su parte, sí admite abrir el siguiente una vez empezado el cierre — lo que no
        // admite es que haya dos abiertos a la vez, que es lo que esto evita.
        //
        // Solo se libera si el diálogo abierto sigue siendo ESTE: cuando la pregunta «¿Cancelar?» se cierra
        // para dar paso al resultado, su ShowAsync vuelve con el resultado ya en pantalla, y liberar sin mirar
        // dejaría la compuerta abierta con un diálogo visible.
        void ReleaseGate(ContentDialog sender, ContentDialogClosingEventArgs args)
        {
            if (ReferenceEquals(_openDialog, dialog)) _openDialog = null;
        }
        dialog.Closing += ReleaseGate;
        try     { return await dialog.ShowAsync(); }
        finally
        {
            dialog.Closing -= ReleaseGate;
            if (ReferenceEquals(_openDialog, dialog)) _openDialog = null;
        }
    }

    /// <summary>La pregunta «¿Cancelar?» de la operación en curso, mientras está abierta (`AUD-028`).</summary>
    private ContentDialog? _cancelQuestion;

    /// <summary>
    /// Cierra la pregunta «¿Cancelar?» y espera a que empiece a cerrarse, que es cuando WinUI ya admite
    /// abrir el siguiente diálogo. Con un tope: si el evento no llega, se sigue igual.
    /// </summary>
    private async Task DismissCancelQuestionAsync(ContentDialog question)
    {
        var closing = new TaskCompletionSource();
        void OnClosing(ContentDialog s, ContentDialogClosingEventArgs a) => closing.TrySetResult();
        question.Closing += OnClosing;
        try
        {
            question.Hide();
            await Task.WhenAny(closing.Task, Task.Delay(TimeSpan.FromSeconds(2)));
        }
        finally { question.Closing -= OnClosing; }
        if (ReferenceEquals(_openDialog, question)) _openDialog = null;
        _services.History.Log("DIALOG CLOSED: la pregunta de cancelar, porque la operación terminó antes de contestarla");
    }

    private Task ShowInfoAsync(string title, string message) =>
        ShowDialogAsync(title, message, null, null, L.T("btn.close"));

    private async Task<bool> ShowConfirmAsync(string title, string message, bool defaultNo = false)
    {
        var dlg = new ContentDialog
        {
            Title = title,
            Content = message,
            PrimaryButtonText = L.T("confirm.yes"),
            CloseButtonText = L.T("confirm.no"),
            DefaultButton = defaultNo ? ContentDialogButton.Close : ContentDialogButton.Primary,
            XamlRoot = Content.XamlRoot,
            RequestedTheme = CurrentTheme,
        };
        return await ShowOneAsync(dlg) == ContentDialogResult.Primary;
    }

    private async Task ShowDialogAsync(string title, string message,
        string? primary, string? secondary, string close)
    {
        var dlg = new ContentDialog
        {
            Title = title,
            Content = message,
            CloseButtonText = close,
            XamlRoot = Content.XamlRoot,
            RequestedTheme = CurrentTheme,
        };
        if (primary is not null)   dlg.PrimaryButtonText   = primary;
        if (secondary is not null) dlg.SecondaryButtonText = secondary;
        await ShowOneAsync(dlg);
    }

    // ── Window closing ────────────────────────────────────────────

    private async void AppWindow_Closing(AppWindow sender, AppWindowClosingEventArgs args)
    {
        // Auto-actualización en curso: dejamos cerrar para que el instalador pueda reemplazar la app.
        if (_closingForUpdate) { RemoveDeviceHook(); return; }
        if (!_isBusy) { RemoveDeviceHook(); return; }
        args.Cancel = true;
        await ShowInfoAsync(L.T("closing.title"), L.T(_cancelAvailable ? "closing.body" : "closing.bodyNoCancel"));
    }
}
