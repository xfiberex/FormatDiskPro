// SPDX-License-Identifier: GPL-3.0-only
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;

namespace FormatDiskPro.UI;

/// <summary>
/// Ciclo de vida de una operación: empezar, terminar, cancelar, el cronómetro, el estado del pie y lo que se anuncia al lector de pantalla.
///
/// <para>Parte de <see cref="MainWindow"/>: es la MISMA clase, repartida en archivos por asunto (T2-08,
/// `AUD-042`). No es un rediseño y no cambia comportamiento.</para>
/// </summary>
public sealed partial class MainWindow
{
    // ── Operation lifecycle ───────────────────────────────────────

    /// <summary>
    /// Anuncia un hito de la operación a los lectores de pantalla (UIA <c>Notification</c>).
    ///
    /// <para><b>Por qué hace falta.</b> <c>StatusText</c> y <c>FormatProgress</c> cambian durante
    /// operaciones que duran minutos u horas, pero **nada mueve el foco**: sin esto, quien usa un lector
    /// de pantalla no se entera de si el formateo avanza, ha fallado o ha terminado. El
    /// <c>LiveSetting="Polite"</c> del XAML hace que Narrador pueda leer el texto cuando cambia; la
    /// notificación es lo que garantiza que los **hitos** se anuncian aunque el usuario esté en otra
    /// parte de la ventana.</para>
    ///
    /// <para><b>Solo en los hitos</b> (inicio, fin, error, cancelación), nunca en cada tick de progreso:
    /// una notificación por porcentaje convertiría el lector de pantalla en ruido continuo durante una
    /// hora — que es peor que el silencio de partida. Por eso el progreso se deja al <i>live region</i>,
    /// que el usuario consulta cuando quiere.</para>
    ///
    /// <para><c>MostRecent</c>: si llegan dos hitos seguidos, se lee el último, no la cola entera.</para>
    /// </summary>
    /// <param name="text">Texto a anunciar; si es null, se usa el de <c>StatusText</c>.</param>
    /// <param name="kind">Tipo de hito, para que el lector pueda darle el tono que corresponda.</param>
    private void AnnounceStatus(string? text = null, AutomationNotificationKind kind = AutomationNotificationKind.Other)
    {
        string message = (text ?? StatusText.Text ?? "").Trim();
        if (message.Length == 0) return;

        try
        {
            var peer = FrameworkElementAutomationPeer.FromElement(StatusText)
                    ?? FrameworkElementAutomationPeer.CreatePeerForElement(StatusText);
            peer?.RaiseNotificationEvent(
                kind, AutomationNotificationProcessing.MostRecent, message, "FormatDiskProStatus");
        }
        catch { /* accesibilidad: nunca puede tumbar la operación que está describiendo */ }
    }

    /// <summary>
    /// Deja la UI en estado de error tras una excepción no esperada de una operación, la registra y
    /// avisa al usuario. Comparte el tratamiento de <see cref="RunFormatAsync"/> con los flujos que
    /// no lo tenían (verificar capacidad, chkdsk, reinicializar, benchmark).
    ///
    /// <para>Existe porque esos cuatro son <c>async void</c>: sin un <c>catch</c> propio, un
    /// <see cref="IOException"/> —una USB que se desconecta a mitad, justo lo que la verificación de
    /// capacidad provoca a propósito— escapaba del handler y terminaba el proceso. El handler global
    /// de <see cref="App"/> es la red de último recurso; esto es el mensaje con contexto.</para>
    ///
    /// <para>Una cancelación NO pasa por aquí: cada flujo la trata antes, y no es un fallo.</para>
    /// </summary>
    /// <param name="operation">Etiqueta de la operación para el historial (p. ej. <c>"VERIFY"</c>).</param>
    /// <param name="letter">Letra de la unidad implicada.</param>
    /// <param name="ex">Excepción capturada.</param>
    private async Task ReportOperationErrorAsync(string operation, char letter, Exception ex)
    {
        FormatProgress.IsIndeterminate = false;
        FormatProgress.Value = 0;
        _lastOperationFailed = true;
        StatusText.Text = L.T("status.unexpected");
        _services.History.Log(OperationFailure.LogLine(operation, letter, ex));
        await ShowInfoAsync(L.T("msg.error"), $"{L.T("status.unexpected")}\n{ErrorText.Describe(ex)}");
    }

    /// <summary>
    /// Fija el color de la barra de progreso a partir de la paleta medida, en vez de dejar el del
    /// ACENTO del sistema (`T12-05`).
    /// </summary>
    /// <remarks>
    /// <para><b>Por qué.</b> Un <c>ProgressBar</c> de Fluent se pinta con el color de acento que el
    /// usuario haya elegido en Windows, y <c>ShowError</c> lo pone en rojo al fallar o cancelar. En un
    /// equipo con el <b>acento rojo</b> —que los hay, y en uno se vio— las dos cosas son el mismo color:
    /// un benchmark que terminó bien dejaba la barra llena y roja, exactamente igual que uno que falló.
    /// El único canal que distinguía éxito de fallo no distinguía nada.</para>
    ///
    /// <para>Es la misma decisión que ya estaba tomada para la barra de ocupación —«no debe usar el color
    /// de ACENTO del sistema: en un equipo con acento rojo se veía roja con el disco medio vacío y leía
    /// como alarma»—, sin aplicar aquí. Ahora el verde de <see cref="SmartLevel.Ok"/> significa que va
    /// bien y el rojo de <c>ShowError</c> que no, siempre, en cualquier equipo; y los dos salen de
    /// <see cref="SeverityPalette"/>, que es lo que el barrido de contraste mide.</para>
    ///
    /// <para>Se llama al arrancar y en cada cambio de tema, porque el color depende del tema efectivo y
    /// no del estado de la operación.</para>
    ///
    /// <para><b>En alto contraste no se fija nada</b> (`T13-09`): la barra se queda con los colores del tema.
    /// Su estado ya lo dicen el texto del pie y el <c>ShowError</c> del propio control, que en contraste se
    /// pinta con la paleta del sistema.</para>
    /// </remarks>
    private void ApplyProgressColor()
    {
        if (HighContrast.IsActive) FormatProgress.ClearValue(ProgressBar.ForegroundProperty);
        else FormatProgress.Foreground = new SolidColorBrush(SeverityPalette.For(SmartLevel.Ok, _darkMode));
    }

    /// <summary>Fija el estado visible y lo anuncia: es el hito de <b>inicio</b> de una operación.</summary>
    private void SetStatusAndAnnounce(string text)
    {
        StatusText.Text = text;
        AnnounceStatus(text);
    }

    /// <summary>
    /// La fila de estado ocupa sitio solo cuando dice algo (`T13-10`).
    /// </summary>
    /// <remarks>
    /// El cronómetro vive en la misma fila, y sin texto de estado no tiene nada que acompañar: durante
    /// una operación siempre hay estado, porque <see cref="BeginOperation"/> lo escribe antes de arrancar.
    /// </remarks>
    private void UpdateStatusRowVisibility()
        => StatusRow.Visibility = string.IsNullOrEmpty(StatusText.Text) ? Visibility.Collapsed : Visibility.Visible;

    private void BeginOperation()
    {
        // Antes de nada, y antes de anunciar: un elemento Collapsed no está en el árbol de automatización,
        // así que la barra tiene que existir ya cuando el lector de pantalla vaya a mirarla.
        FormatProgress.Visibility = Visibility.Visible;
        StatusRow.Visibility = Visibility.Visible;

        _isBusy = true;
        // SetControlsEnabled(false) NO pasa por UpdateToolsMenuAvailability, así que el resumen del pie
        // se quedaría puesto compitiendo con StatusText. Al terminar lo repinta EndOperation, que sí
        // vuelve a habilitar los controles.
        UpdateFooterSummary();
        _cancelRequested = false;
        _lastOperationFailed = false;
        FormatProgress.ShowError = false;
        _cts = new CancellationTokenSource();
        SetCancelAvailable(true);
        SetControlsEnabled(false);
        StatusText.ClearValue(TextBlock.ForegroundProperty);
        ElapsedText.Text = "00:00";
        CloseButton.Content = L.T("btn.cancel");
        _opStart = DateTime.Now;
        _opBytesDone = _opTotalBytes = _speedLastBytes = 0;
        _speedLastTime = _opStart;
        _elapsedTimer.Start();
    }

    /// <summary>
    /// Habilita o deshabilita «Cancelar» según si el paso en curso se puede detener de verdad.
    /// </summary>
    private void SetCancelAvailable(bool available)
    {
        _cancelAvailable = available;
        CloseButton.IsEnabled = available;
    }

    private static bool IsDriveReady(char letter)
    {
        try { return new DriveInfo(letter.ToString()).IsReady; }
        catch { return false; }
    }

    private void EndOperation()
    {
        _elapsedTimer.Stop();
        _isBusy = false;
        SetCancelAvailable(true);
        _activeProcess?.Dispose();
        _activeProcess = null;
        _cts?.Dispose();
        _cts = null;
        _services.Taskbar.Clear(WinRT.Interop.WindowNative.GetWindowHandle(this));
        // Si nos estamos cerrando para actualizar, la ventana ya se va: no tocar la UI.
        if (_closingForUpdate) return;
        SetControlsEnabled(true);
        CloseButton.Content = L.T("btn.close");
        // Barra en rojo (Fluent ShowError) al fallar o cancelar, hasta el próximo BeginOperation.
        FormatProgress.ShowError = _cancelRequested || _lastOperationFailed;

        // La lista se recarga al terminar CUALQUIER operación, y no solo las que cambian el volumen
        // (`AUD-036`): una cancelada a mitad, un benchmark o un borrado dejan otro espacio libre, y una
        // reinicialización interrumpida, otras particiones. Va antes del anuncio, que lee StatusText, y no lo
        // pisa: la unidad sigue siendo la misma, así que la recarga conserva la selección y las opciones.
        // Recargar vuelve a pasar por OnDriveSelected, que busca las carpetas de trabajo que un `finally`
        // no pudiera borrar (`AUD-038`).
        // ApplyProtection vacía la fila de estado y su color al seleccionar: se guardan los dos y se reponen,
        // porque son el resultado que la operación acaba de dejar.
        string statusBeforeReload = StatusText.Text;
        object colorBeforeReload  = StatusText.ReadLocalValue(TextBlock.ForegroundProperty);
        LoadDrives();
        StatusText.Text = statusBeforeReload;
        if (colorBeforeReload is Brush brush) StatusText.Foreground = brush;

        // Hito de FIN, en un solo sitio: aquí pasan las cinco operaciones, terminen bien, mal o
        // canceladas. Se anuncia el estado que la operación acaba de dejar en StatusText.
        AnnounceStatus(kind: FormatProgress.ShowError
            ? AutomationNotificationKind.ActionAborted
            : AutomationNotificationKind.ActionCompleted);

        // Aviso al terminar operaciones largas: sonido + parpadeo de la barra (solo si el usuario
        // no está mirando la ventana). No aplica a operaciones cortas ni canceladas.
        if (Notifier.ShouldNotify(DateTime.Now - _opStart, _settings.NotifyOnFinish, _cancelRequested, OperationNotifyThreshold))
            _services.Notifier.OperationFinished(WinRT.Interop.WindowNative.GetWindowHandle(this));
    }

    private async void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        if (!_isBusy) { App.MainWindow?.Close(); return; }
        if (!_cancelAvailable) return;

        var question = new ContentDialog
        {
            Title = L.T("cancel.title"),
            Content = L.T("cancel.body"),
            PrimaryButtonText = L.T("confirm.yes"),
            CloseButtonText = L.T("confirm.no"),
            DefaultButton = ContentDialogButton.Close,
            XamlRoot = Content.XamlRoot,
            RequestedTheme = CurrentTheme,
        };
        // Se recuerda mientras está abierta: si la operación termina antes de contestar, su resultado la
        // cierra en vez de descartarse (`AUD-028`, ver ShowOneAsync).
        _cancelQuestion = question;
        bool cancel;
        try     { cancel = await ShowOneAsync(question) == ContentDialogResult.Primary; }
        finally { _cancelQuestion = null; }

        if (cancel)
        {
            // La pregunta se queda abierta lo que el usuario tarde: si entretanto la operación terminó o
            // pasó a un paso que no se cancela, ya no hay nada que cancelar.
            if (!_isBusy || !_cancelAvailable) return;
            _cancelRequested = true;
            try { _cts?.Cancel(); } catch { }
            try { _activeProcess?.Kill(entireProcessTree: true); } catch { }
        }
    }

    // ── Timer ─────────────────────────────────────────────────────

    private void TimerElapsed_Tick(object? sender, object e)
    {
        var now = DateTime.Now;
        string text = (now - _opStart).ToString(@"mm\:ss");

        // Operaciones con bytes (verificación / borrado seguro): añadir velocidad y ETA.
        // Velocidad instantánea por ventana deslizante (delta de bytes entre ticks de 1 s),
        // robusta frente a operaciones por fases (un delta negativo simplemente omite ese tick).
        if (_opTotalBytes > 0)
        {
            double dt = (now - _speedLastTime).TotalSeconds;
            long db = _opBytesDone - _speedLastBytes;
            if (dt > 0 && db > 0)
            {
                double speed = db / dt;
                var eta = Throughput.Eta(Math.Max(0, _opTotalBytes - _opBytesDone), speed);
                string spd = Throughput.FormatSpeed(speed);
                string etaStr = Throughput.FormatEta(eta);
                if (spd.Length > 0)    text += $"  ·  {spd}";
                if (etaStr.Length > 0) text += $"  ·  ETA {etaStr}";
            }
            _speedLastBytes = _opBytesDone;
            _speedLastTime  = now;
        }

        ElapsedText.Text = text;

        // Espeja el estado de FormatProgress en el icono de la barra de tareas (visible con la app
        // minimizada), a la misma cadencia de 1 s del cronómetro — complementa el aviso al terminar.
        var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(this);
        if (FormatProgress.IsIndeterminate) _services.Taskbar.SetIndeterminate(hwnd);
        else                                _services.Taskbar.SetValue(hwnd, (int)FormatProgress.Value);
    }

    /// <summary>
    /// Habilita o deshabilita los controles de la ventana según si hay una operación en curso. Los de
    /// formato quedan además sujetos a la protección de la unidad: sobre una unidad protegida siguen
    /// deshabilitados aunque no haya nada corriendo.
    /// </summary>
    private void SetControlsEnabled(bool enabled)
    {
        bool canFormat = enabled && !_isDriveProtected;

        MnuTools.IsEnabled   = enabled;
        MnuConfig.IsEnabled  = enabled;
        MnuHelp.IsEnabled    = enabled;
        // La barra rápida duplica tres ítems de esos menús, y se apaga con ellos (`AUD-033`). Antes seguía
        // con aspecto de activa durante toda la operación, y sus manejadores salían sin decir nada. Al
        // volver, UpdateToolsMenuAvailability devuelve Salud y Benchmark al estado de su ítem.
        BtnHealth.IsEnabled    = enabled;
        BtnBenchmark.IsEnabled = enabled;
        BtnHistory.IsEnabled   = enabled;
        DrivePicker.IsEnabled   = enabled;
        RefreshButton.IsEnabled = enabled;
        FileSystemPicker.IsEnabled  = canFormat;
        AllocUnitPicker.IsEnabled   = canFormat;
        VolumeLabelBox.IsEnabled    = canFormat;
        PresetsButton.IsEnabled     = canFormat;
        StartButton.IsEnabled       = canFormat;
        QuickFormatCheck.IsEnabled  = canFormat;
        SecureWipeCheck.IsEnabled   = canFormat;
        CompressCheck.IsEnabled     = canFormat && FileSystemPicker.SelectedItem?.ToString() == "NTFS";
        SmallFat32Check.IsEnabled   = canFormat;
        // El detalle del sobrante se apaga como bloque, igual que los otros tres (T6-08): sus dos
        // etiquetas no se atenúan solas por estar dentro de un panel deshabilitado.
        SetSubOptionEnabled(canFormat, [RestFsLbl, RestLabelLbl], RestFsPicker, RestLabelBox);
        UpdateWipePassesEnabled();
        UpdateSmallFat32SizeEnabled();   // gobierna también RestPicker (ver UpdateSmallFat32SizeEnabled)
        // El menú vuelve habilitado tras una operación, pero cada ítem manda sobre sí mismo: lo que la
        // unidad seleccionada no admite tiene que seguir apagado (T7-02).
        if (enabled) UpdateToolsMenuAvailability();
    }
}
