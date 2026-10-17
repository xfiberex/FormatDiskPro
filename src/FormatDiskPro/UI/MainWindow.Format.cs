// SPDX-License-Identifier: GPL-3.0-only
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
namespace FormatDiskPro.UI;

/// <summary>
/// Formatear: las comprobaciones y la confirmación de <c>Formatear</c>, la validación de la etiqueta y el formato con su borrado seguro.
///
/// <para>Parte de <see cref="MainWindow"/>: es la MISMA clase, repartida en archivos por asunto (T2-08,
/// `AUD-042`). No es un rediseño y no cambia comportamiento.</para>
/// </summary>
public sealed partial class MainWindow
{
    // ── Format ────────────────────────────────────────────────────

    private async void StartButton_Click(object sender, RoutedEventArgs e)
    {
        // La decisión de si esta unidad se puede formatear es de Core (`AUD-015`): aquí solo se reúnen los
        // hechos y se enseña el motivo.
        var driveItem = DrivePicker.SelectedItem as DriveViewModel;
        var rejection = DestructiveGuard.CanFormat(
            driveItem?.Letter, _isDriveProtected || driveItem?.IsProtected == true, SystemLetter);
        if (rejection != GuardRejection.None || driveItem is null)
        {
            var (titleKey, bodyKey) = DestructiveGuard.DescribeFormat(rejection);
            await ShowInfoAsync(L.T(titleKey), L.T(bodyKey));
            return;
        }
        if (FileSystemPicker.SelectedItem is null || AllocUnitPicker.SelectedIndex < 0)
        {
            await ShowInfoAsync(L.T("msg.warning"), L.T("msg.selectFsAlloc"));
            return;
        }

        string fs       = FileSystemPicker.SelectedItem.ToString()!;
        long allocBytes = GetSelectedAllocBytes();
        string label    = VolumeLabelBox.Text.Trim();
        bool quick      = QuickFormatCheck.IsChecked == true;
        bool compress   = CompressCheck.IsChecked    == true;
        bool secure     = SecureWipeCheck.IsChecked  == true;

        bool driveReady;
        try { driveReady = driveItem.Info.IsReady; } catch { driveReady = false; }
        // «No lista» no es «no está». Un volumen sin sistema de archivos —el que deja un formato completo
        // cancelado— no está listo, y es justo el que hay que poder formatear (`AUD-001`): rechazarlo aquí
        // dejaba la unidad sin salida dentro de la app. Solo se rechaza si detrás de la letra ya no hay
        // ninguna partición (unidad retirada, lector sin tarjeta).
        if (!driveReady && await _services.Disk.IsDiskReadOnlyAsync(driveItem.Letter) is null)
        {
            await ShowInfoAsync(L.T("msg.goneTitle"), L.T("msg.goneBody", driveItem.Letter));
            LoadDrives();
            return;
        }

        if (!await ValidateLabelAsync(label, fs, focusOnError: true))
            return;

        // Qué hay detrás de la letra AHORA, antes de preguntar nada. Se lanza ya y se recoge justo antes de
        // abrir la confirmación: corre a la vez que la consulta de solo lectura, así que no añade espera.
        Task<DriveIdentity?> identityTask = _services.Disk.GetIdentityAsync(driveItem.Letter);

        // Protección de escritura: si el disco está en solo lectura, el formateo fallaría con un error
        // poco claro. Lo detectamos y ofrecemos quitarla antes de continuar.
        if (await _services.Disk.IsDiskReadOnlyAsync(driveItem.Letter) == true)
        {
            if (!await ShowConfirmAsync(L.T("unlock.confirmTitle"), L.T("unlock.confirmBody", driveItem.Letter)))
                return;
            if (!await _services.Disk.ClearReadOnlyAsync(driveItem.Letter))
            {
                await ShowInfoAsync(L.T("unlock.confirmTitle"), L.T("unlock.failed", driveItem.Letter));
                return;
            }
        }

        int securePasses = SelectedWipePasses();
        // La opción de FAT32 pequeña solo aplica a Reinicializar unidad: si está marcada, avisar aquí
        // para que nadie formatee creyendo que obtendrá la partición pequeña.
        bool smallFat32Ignored = SmallFat32Check.Visibility == Visibility.Visible && SmallFat32Check.IsChecked == true;
        string mode = (quick ? L.T("fmt.quick") : L.T("fmt.full")) +
            (secure ? $" + {L.T("confirm.secure")}" + (securePasses > 1 ? $" ×{securePasses}" : "") : "");
        (string, string)[] details =
        [
            (L.T("confirm.drive"),   driveItem.DisplayText),
            (L.T("confirm.fs"),      fs),
            (L.T("confirm.cluster"), SelectedAllocLabel()),
            (L.T("confirm.label"),   string.IsNullOrEmpty(label) ? L.T("confirm.nolabel") : label),
            (L.T("confirm.mode"),    mode),
        ];

        DriveIdentity? confirmed = await identityTask;

        var dlg = new ConfirmDialog(driveItem.Letter, L.T("confirm.title"),
                                    L.T("btn.start.drive", $"{driveItem.Letter}:"), L.T("confirm.warning"),
                                    details, smallFat32Ignored ? L.T("confirm.smallFat32Ignored") : null)
            { XamlRoot = Content.XamlRoot, RequestedTheme = CurrentTheme };
        if (await ShowOneAsync(dlg) != ContentDialogResult.Primary) return;

        // Entre abrir la confirmación y aceptarla pasa lo que el usuario tarde en teclear la letra, y una
        // letra no es una identidad: se retira un pendrive, se conecta otro disco y Windows le da la
        // misma. Se comprueba que detrás sigue estando lo que se confirmó (`AUD-013`).
        var targetCheck = DestructiveGuard.StillTheConfirmedTarget(
            confirmed, await _services.Disk.GetIdentityAsync(driveItem.Letter));
        if (targetCheck != GuardRejection.None)
        {
            _services.History.Log($"FORMAT REJECTED {driveItem.Letter}: {targetCheck}");
            LoadDrives();
            var (titleKey, bodyKey) = DestructiveGuard.DescribeFormat(targetCheck);
            await ShowInfoAsync(L.T(titleKey), L.T(bodyKey, driveItem.Letter));
            return;
        }

        await RunFormatAsync(driveItem.Letter, fs, allocBytes, label, quick, compress, secure, securePasses);
    }

    /// <summary>
    /// Valida la etiqueta de volumen para el sistema de archivos dado: caracteres permitidos y longitud
    /// máxima por FS. Muestra el diálogo correspondiente y devuelve <c>false</c> si no es válida; una
    /// etiqueta vacía siempre es válida. Compartido por formatear (Iniciar) y reinicializar.
    /// </summary>
    private async Task<bool> ValidateLabelAsync(string label, string fs, bool focusOnError)
    {
        switch (FormatLogic.ValidateLabel(label, fs))
        {
            case FormatLogic.LabelValidation.InvalidChars:
                await ShowInfoAsync(L.T("msg.invalidTitle"), L.T("msg.invalidLabel"));
                if (focusOnError) VolumeLabelBox.Focus(FocusState.Programmatic);
                return false;
            case FormatLogic.LabelValidation.TooLong:
                await ShowInfoAsync(L.T("msg.labelLongTitle"), L.T("msg.labelLong", FileSystems.MaxLabelLength(fs), fs));
                if (focusOnError) VolumeLabelBox.Focus(FocusState.Programmatic);
                return false;
            default:
                return true;
        }
    }

    // Hint en vivo bajo la etiqueta (mientras se escribe / al cambiar de FS): el modal de
    // ValidateLabelAsync queda como respaldo al enviar (Iniciar / Reinicializar).
    private void UpdateLabelHint()
    {
        string fs = FileSystemPicker.SelectedItem?.ToString() ?? "NTFS";
        var result = FormatLogic.ValidateLabel(VolumeLabelBox.Text, fs);
        string previous = LabelErrorText.Text;
        LabelErrorText.Text = result switch
        {
            FormatLogic.LabelValidation.InvalidChars => L.T("msg.invalidLabel"),
            FormatLogic.LabelValidation.TooLong       => L.T("msg.labelLong", FileSystems.MaxLabelLength(fs), fs),
            _                                          => "",
        };
        LabelErrorText.Visibility = result == FormatLogic.LabelValidation.Ok ? Visibility.Collapsed : Visibility.Visible;

        // El LiveSetting="Assertive" del XAML solo dice CÓMO leerlo; el evento es lo que dispara la
        // lectura. Solo al aparecer o cambiar el mensaje: se escribe letra a letra, y repetir el mismo
        // error en cada pulsación sería insoportable con un lector de pantalla.
        if (LabelErrorText.Text.Length > 0 && LabelErrorText.Text != previous)
            LiveRegion.Announce(LabelErrorText);
    }

    private void VolumeLabelBox_TextChanged(object sender, TextChangedEventArgs e) => UpdateLabelHint();

    private async Task RunFormatAsync(
        char driveLetter, string fs, long allocBytes,
        string label, bool quickFormat, bool compress, bool secureWipe, int securePasses)
    {
        BeginOperation();
        bool useFormatCom = FormatLogic.UsesFormatCom(fs, quickFormat);
        // Por Format-Volume no hay cancelación que ofrecer: se dice desde el primer momento, en vez de
        // aceptar un «Cancelar» que dejaría el formato corriendo con la app ya en «Cancelado» (`AUD-001`).
        SetCancelAvailable(useFormatCom);

        StatusText.ClearValue(TextBlock.ForegroundProperty);
        SetStatusAndAnnounce(L.T(useFormatCom ? "status.formatting" : "status.formattingNoCancel",
            driveLetter, quickFormat ? L.T("fmt.quick") : L.T("fmt.full")));
        FormatProgress.Value   = 0;
        FormatProgress.IsIndeterminate = !useFormatCom;

        try
        {
            int code; string output;
            if (useFormatCom)
            {
                var percent = new Progress<int>(p => FormatProgress.Value = p);
                (code, output) = await _services.Format.RunComAsync(
                    driveLetter, fs, allocBytes, label, compress, percent, p => _activeProcess = p, _cts!.Token);
            }
            else
            {
                var (c, so, se) = await _services.Format.RunVolumeAsync(
                    driveLetter, fs, allocBytes, label, quickFormat, compress, p => _activeProcess = p, _cts!.Token);
                code = c;
                output = string.IsNullOrWhiteSpace(se) ? so : se;
            }

            FormatProgress.IsIndeterminate = false;

            if (_cancelRequested)
            {
                FormatProgress.Value = 0;
                // Un formato completo cancelado deja el volumen SIN sistema de archivos. Se dice, y se
                // recarga la lista para que la tarjeta no siga enseñando la unidad de antes (`AUD-001`).
                bool unformatted = !IsDriveReady(driveLetter);
                _services.History.Log($"FORMAT CANCELLED {driveLetter}: {fs}{(unformatted ? " unformatted" : "")}");
                LoadDrives();
                StatusText.Text = unformatted ? L.T("status.cancelledUnformatted", driveLetter) : L.T("status.cancelled");
                return;
            }

            if (code == 0)
            {
                FormatProgress.Value = 100;

                int wipeCode = FormatHistory.WipeCompleted;
                if (secureWipe)
                {
                    SetCancelAvailable(true);   // el borrado seguro sí se detiene al cancelar
                    StatusText.ClearValue(TextBlock.ForegroundProperty);
                    StatusText.Text = L.T("status.wiping");
                    FormatProgress.IsIndeterminate = false;
                    FormatProgress.Value = 0;

                    var wipeProgress = new Progress<(int percent, long bytesDone, long totalBytes)>(p =>
                    {
                        FormatProgress.Value = Math.Clamp(p.percent, 0, 100);
                        _opBytesDone  = p.bytesDone;
                        _opTotalBytes = p.totalBytes;
                        StatusText.Text = L.T("status.wiping.progress", FormatBytes(p.bytesDone));
                    });

                    // Si el borrado no llega a completarse, el formato SÍ se hizo, y el historial tiene que
                    // decirlo antes que lo del borrado (`AUD-036`): antes solo quedaba «WIPE CANCELLED» o, si
                    // el borrado lanzaba, un «FORMAT ERROR» de un formato que había salido bien.
                    void LogFormatWithoutWipe() => _services.History.Log(FormatHistory.Success(
                        driveLetter, fs, allocBytes, quickFormat, compress,
                        wipeRequested: false, FormatHistory.WipeCompleted, securePasses, label));

                    try
                    {
                        wipeCode = await _services.Wipe.RunAsync(driveLetter, securePasses, wipeProgress, _cts!.Token);
                    }
                    catch (OperationCanceledException)
                    {
                        _opTotalBytes = 0;
                        FormatProgress.Value = 0;
                        StatusText.Text = L.T("status.cancelled");
                        LogFormatWithoutWipe();
                        _services.History.Log($"WIPE CANCELLED {driveLetter}:");
                        return;
                    }
                    catch (Exception ex)
                    {
                        _opTotalBytes = 0;
                        LogFormatWithoutWipe();
                        await ReportOperationErrorAsync("WIPE", driveLetter, ex);
                        return;
                    }

                    _opTotalBytes = 0;   // detener velocidad/ETA: el resto del flujo no maneja bytes
                    FormatProgress.Value = 100;
                    if (_cancelRequested)
                    {
                        StatusText.Text = L.T("status.cancelled");
                        LogFormatWithoutWipe();
                        _services.History.Log($"WIPE CANCELLED {driveLetter}:");
                        return;
                    }
                }

                _services.History.Log(FormatHistory.Success(
                    driveLetter, fs, allocBytes, quickFormat, compress, secureWipe, wipeCode, securePasses, label));

                // El borrado seguro se pidió y no se hizo (`AUD-009`): el formato está bien, pero quien
                // lo pidió para que no se pudieran recuperar los datos tiene que saber que siguen ahí.
                // Antes se registraba `wipe=True` y se anunciaba un éxito sin más.
                if (secureWipe && !FormatHistory.WipeDone(secureWipe, wipeCode))
                {
                    FormatProgress.Value = 0;
                    _lastOperationFailed = true;
                    _services.History.Log(FormatHistory.WipeFailed(driveLetter, wipeCode));
                    LoadDrives();
                    StatusText.Text = L.T("status.error");
                    await ShowInfoAsync(L.T("msg.warning"), L.T("wipe.notDone", driveLetter, fs));
                    return;
                }

                StatusText.Text = L.T("status.success");
                string successBody = L.T("success.body", driveLetter, fs);
                // Con borrado seguro, el resultado dice lo que se sobrescribió y lo que no (`AUD-039`).
                if (secureWipe)
                    successBody += " " + L.T("success.wipeNote", securePasses, FormatBytes(FreeSpaceFill.SafetyMarginBytes));
                await ShowInfoAsync(L.T("success.title"), successBody);
                LoadDrives();
            }
            else
            {
                FormatProgress.Value = 0;
                _lastOperationFailed = true;
                StatusText.Text = L.T("status.error");
                _services.History.Log($"FORMAT FAIL {driveLetter}: fs={fs} code={code}");
                await ShowInfoAsync(L.T("error.formatTitle"), L.T("error.formatBody", driveLetter, output.Trim()));
            }
        }
        catch (OperationCanceledException)
        {
            FormatProgress.IsIndeterminate = false;
            FormatProgress.Value = 0;
            StatusText.Text = L.T("status.cancelled");
            _services.History.Log($"FORMAT CANCELLED {driveLetter}: {fs}{(IsDriveReady(driveLetter) ? "" : " unformatted")}");
        }
        catch (Exception ex)
        {
            FormatProgress.IsIndeterminate = false;
            FormatProgress.Value = 0;
            _lastOperationFailed = true;
            StatusText.Text = _cancelRequested ? L.T("status.cancelled") : L.T("status.unexpected");
            _services.History.Log($"FORMAT ERROR {driveLetter}: {ErrorText.Describe(ex)}");
            if (!_cancelRequested)
                await ShowInfoAsync(L.T("msg.error"), $"{L.T("status.unexpected")}\n{ErrorText.Describe(ex)}");
        }
        finally
        {
            EndOperation();
        }
    }
}
