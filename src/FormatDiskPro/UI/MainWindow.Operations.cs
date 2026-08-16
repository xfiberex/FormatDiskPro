using Microsoft.UI.Composition.SystemBackdrops;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using Windows.Graphics;
using Windows.UI;
using Windows.UI.ViewManagement;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Text;

namespace FormatDiskPro.UI;

/// <summary>
/// Operaciones del menú Herramientas: verificar capacidad, expulsar, salud, quitar protección, chkdsk, reinicializar y benchmark.
///
/// <para>Parte de <see cref="MainWindow"/>: es la MISMA clase, repartida en archivos por
/// asunto (T2-08). No es un rediseño y no cambia comportamiento — el archivo único pasaba de
/// 2.000 líneas y encontrar algo en él era el problema.
/// </summary>
public sealed partial class MainWindow
{
    // ── Capacity verification ─────────────────────────────────────

    private async void MnuVerify_Click(object sender, RoutedEventArgs e)
    {
        if (_isBusy || DrivePicker.SelectedItem is not DriveViewModel item) return;

        if (item.IsProtected || IsSystemDrive(item.Letter))
        {
            await ShowInfoAsync(L.T("msg.protTitle"), L.T("msg.protBody"));
            return;
        }

        if (!await ShowConfirmAsync(L.T("verify.title"), L.T("verify.warn", item.Letter), defaultNo: true))
            return;

        BeginOperation();
        FormatProgress.IsIndeterminate = false;
        FormatProgress.Value = 0;
        // Esta operación no fija un estado inicial: el primer tick de progreso lo hace, y puede tardar.
        // Se anuncia el inicio sin tocar StatusText, para no pintar un texto que se sobrescribe enseguida.
        AnnounceStatus(L.T("verify.title"));

        var progress = new Progress<(CapacityVerifier.Phase phase, int percent, long bytes)>(p =>
        {
            FormatProgress.Value = Math.Clamp(p.percent, 0, 100);
            _opBytesDone  = p.bytes;
            _opTotalBytes = p.percent > 0 ? p.bytes * 100L / p.percent : 0;
            StatusText.ClearValue(TextBlock.ForegroundProperty);
            StatusText.Text = p.phase == CapacityVerifier.Phase.Writing
                ? L.T("verify.writing", FormatBytes(p.bytes))
                : L.T("verify.reading", FormatBytes(p.bytes));
        });

        try
        {
            var result = await _services.Verifier.RunAsync(item.Letter, progress, _cts!.Token);

            if (_cancelRequested || result.FailureDetail == "cancelled")
            {
                FormatProgress.Value = 0;
                StatusText.Text = L.T("status.cancelled");
                return;
            }

            if (result.Ok)
            {
                FormatProgress.Value = 100;
                StatusText.Text = L.T("verify.okTitle");
                _services.History.Log($"VERIFY OK {item.Letter}: written={result.WrittenBytes}");
                await ShowInfoAsync(L.T("verify.okTitle"), L.T("verify.okBody", item.Letter, FormatBytes(result.WrittenBytes)));
            }
            else
            {
                FormatProgress.Value = 0;
                _lastOperationFailed = true;
                StatusText.Foreground = new SolidColorBrush(ProtectedColor());
                StatusText.Text = L.T("verify.failTitle");
                _services.History.Log($"VERIFY FAIL {item.Letter}: {result.FailureDetail} ok-until={result.WrittenBytes}");
                await ShowInfoAsync(L.T("verify.failTitle"), L.T("verify.failBody", item.Letter, FormatBytes(result.WrittenBytes)));
            }
        }
        catch (OperationCanceledException)
        {
            FormatProgress.Value = 0;
            StatusText.Text = L.T("status.cancelled");
        }
        catch (Exception ex)
        {
            // Una unidad falsificada o que se desconecta a mitad lanza IOException aquí: es el
            // escenario que esta herramienta existe para provocar, no un caso remoto.
            await ReportOperationErrorAsync("VERIFY", item.Letter, ex);
        }
        finally
        {
            EndOperation();
        }
    }

    // ── Eject ─────────────────────────────────────────────────────

    private async void MnuEject_Click(object sender, RoutedEventArgs e)
    {
        if (_isBusy || DrivePicker.SelectedItem is not DriveViewModel item) return;

        if (item.Info.DriveType != DriveType.Removable)
        {
            await ShowInfoAsync(L.T("msg.warning"), L.T("eject.fixed"));
            return;
        }

        bool ok = await _services.Disk.EjectAsync(item.Letter);
        if (ok)
        {
            StatusText.ClearValue(TextBlock.ForegroundProperty);
            StatusText.Text = L.T("status.ejected");
            _services.History.Log($"EJECT {item.Letter}:");
            LoadDrives();
        }
        else
        {
            await ShowInfoAsync(L.T("msg.warning"), L.T("eject.fail"));
        }
    }

    private async void MnuHealth_Click(object sender, RoutedEventArgs e)
    {
        if (_isBusy) return;
        if (DrivePicker.SelectedItem is not DriveViewModel item)
        {
            await ShowInfoAsync(L.T("msg.warning"), L.T("msg.selectDrive"));
            return;
        }
        var dlg = new HealthDialog(_darkMode, item.Letter, item.DisplayText, _services.Disk)
        {
            XamlRoot = Content.XamlRoot,
            RequestedTheme = CurrentTheme,
        };
        await dlg.ShowAsync();
    }

    // ── Write protection (#7) ─────────────────────────────────────

    private async void MnuUnlock_Click(object sender, RoutedEventArgs e)
    {
        if (_isBusy || DrivePicker.SelectedItem is not DriveViewModel item) return;

        if (item.IsProtected || IsSystemDrive(item.Letter))
        {
            await ShowInfoAsync(L.T("unlock.confirmTitle"), L.T("unlock.blockedSystem"));
            return;
        }

        if (await _services.Disk.IsDiskReadOnlyAsync(item.Letter) != true)
        {
            await ShowInfoAsync(L.T("unlock.confirmTitle"), L.T("unlock.notProtected", item.Letter));
            return;
        }

        if (!await ShowConfirmAsync(L.T("unlock.confirmTitle"), L.T("unlock.confirmBody", item.Letter)))
            return;

        if (await _services.Disk.ClearReadOnlyAsync(item.Letter))
        {
            StatusText.ClearValue(TextBlock.ForegroundProperty);
            StatusText.Text = L.T("unlock.cleared", item.Letter);
            _services.History.Log($"UNLOCK {item.Letter}:");
            LoadDrives();
        }
        else
        {
            await ShowInfoAsync(L.T("unlock.confirmTitle"), L.T("unlock.failed", item.Letter));
        }
    }

    // ── chkdsk (#6) ───────────────────────────────────────────────

    private async void MnuCheck_Click(object sender, RoutedEventArgs e)
    {
        if (_isBusy || DrivePicker.SelectedItem is not DriveViewModel item) return;

        bool protectedDrive = item.IsProtected || IsSystemDrive(item.Letter);

        // Modo: Solo comprobar (read-only, por defecto) / Comprobar y reparar / Cancelar.
        // La reparación (/f) no se ofrece en el disco de sistema (programaría un reinicio).
        //
        // Las dos acciones van como botones APILADOS a todo el ancho dentro del Content, no como
        // Primary/Secondary del ContentDialog: con tres botones en una fila, "Comprobar y reparar"
        // (y aún más su traducción PT/IT) no cabía y WinUI lo truncaba sin puntos suspensivos
        // ("Comprobar y repar"). Apilados nunca truncan, en ningún idioma.
        var modeDlg = new ContentDialog
        {
            Title          = L.T("check.modeTitle"),
            CloseButtonText = L.T("btn.cancel"),
            XamlRoot       = Content.XamlRoot,
            RequestedTheme = CurrentTheme,
        };

        bool? repairChoice = null;   // null = cancelar · false = solo comprobar · true = comprobar y reparar

        var scanButton = new Button
        {
            Content             = L.T("check.scanOnly"),
            HorizontalAlignment = HorizontalAlignment.Stretch,
            Style               = (Style)Application.Current.Resources["AccentButtonStyle"],
        };
        // AutomationId explícito: al crearse en código no hay x:Name del que WinUI lo derive, y los UI
        // tests localizan por AutomationId. Sin esto quedan fuera de su alcance — que es justo lo que
        // pasó al apilar estos botones en la v1.15.2: el test seguía buscando el 'PrimaryButton' que
        // dejó de existir, y nadie se enteró porque solo corre con la USB de pruebas conectada.
        AutomationProperties.SetAutomationId(scanButton, "CheckScanButton");
        scanButton.Click += (_, _) => { repairChoice = false; modeDlg.Hide(); };
        // Enfocar el botón por defecto preserva "Enter = Solo comprobar" (antes lo daba DefaultButton).
        scanButton.Loaded += (_, _) => scanButton.Focus(FocusState.Programmatic);

        var panel = new StackPanel { Spacing = 8 };
        panel.Children.Add(new TextBlock { Text = L.T("check.modeBody", item.Letter), TextWrapping = TextWrapping.Wrap });
        panel.Children.Add(scanButton);
        if (!protectedDrive)
        {
            var repairButton = new Button
            {
                Content             = L.T("check.repair"),
                HorizontalAlignment = HorizontalAlignment.Stretch,
            };
            AutomationProperties.SetAutomationId(repairButton, "CheckRepairButton");
            repairButton.Click += (_, _) => { repairChoice = true; modeDlg.Hide(); };
            panel.Children.Add(repairButton);
        }
        modeDlg.Content = panel;

        await modeDlg.ShowAsync();
        if (repairChoice is null) return;   // Cancelar (botón Cerrar, Esc o clic fuera)
        bool repair = repairChoice.Value;

        BeginOperation();
        FormatProgress.IsIndeterminate = false;
        FormatProgress.Value = 0;
        StatusText.ClearValue(TextBlock.ForegroundProperty);
        SetStatusAndAnnounce(repair ? L.T("check.repairing", item.Letter) : L.T("check.scanning", item.Letter));

        var progress = new Progress<int>(p => FormatProgress.Value = Math.Clamp(p, 0, 100));

        try
        {
            var (code, _) = await _services.CheckDisk.RunAsync(item.Letter, repair, progress, _cts!.Token);

            if (_cancelRequested)
            {
                FormatProgress.Value = 0;
                StatusText.Text = L.T("status.cancelled");
                return;
            }

            FormatProgress.Value = 100;
            CheckResult res = CheckDisk.Interpret(code, repair);
            _services.History.Log($"CHKDSK {item.Letter}: repair={repair} code={code} result={res}");

            string msg = res switch
            {
                CheckResult.Clean    => L.T("check.resultClean", item.Letter),
                CheckResult.Repaired => L.T("check.resultRepaired", item.Letter),
                CheckResult.Errors   => L.T("check.resultErrors", item.Letter),
                _                    => L.T("check.resultFailed", item.Letter),
            };
            StatusText.Text = msg;
            await ShowInfoAsync(L.T("check.modeTitle"), msg);
        }
        catch (OperationCanceledException)
        {
            FormatProgress.Value = 0;
            StatusText.Text = L.T("status.cancelled");
        }
        catch (Exception ex)
        {
            // CheckDisk.RunAsync no atrapa nada: Process.Start puede lanzar Win32Exception si
            // chkdsk.exe no está disponible o lo bloquea una política.
            await ReportOperationErrorAsync("CHKDSK", item.Letter, ex);
        }
        finally
        {
            EndOperation();
        }
    }

    // ── Reinicializar unidad (#8) ─────────────────────────────────

    private async void MnuReinit_Click(object sender, RoutedEventArgs e)
    {
        if (_isBusy || DrivePicker.SelectedItem is not DriveViewModel item) return;

        // Solo unidades extraíbles (USB): es el caso de uso y minimiza el riesgo.
        DriveType type;
        try { type = item.Info.DriveType; } catch { type = DriveType.Unknown; }
        if (type != DriveType.Removable)
        {
            await ShowInfoAsync(L.T("reinit.title"), L.T("reinit.onlyRemovable"));
            return;
        }

        // Nunca el disco del sistema ni una unidad protegida.
        if (item.IsProtected || IsSystemDrive(item.Letter))
        {
            await ShowInfoAsync(L.T("reinit.title"), L.T("reinit.blockedSystem"));
            return;
        }

        // Guarda crítica: el disco físico objetivo no puede ser el mismo que el de Windows
        // (Clear-Disk borra TODO el disco, no solo la partición seleccionada).
        char sysLetter  = Path.GetPathRoot(Environment.SystemDirectory)![0];
        int? targetDisk = await _services.Disk.GetDiskNumberAsync(item.Letter);
        int? sysDisk    = await _services.Disk.GetDiskNumberAsync(sysLetter);
        if (targetDisk is null || (sysDisk is not null && targetDisk == sysDisk))
        {
            await ShowInfoAsync(L.T("reinit.title"), L.T("reinit.sameDisk"));
            return;
        }

        // Configuración de formato tomada del formulario. La opción de FAT32 pequeña fuerza el FS y el
        // tamaño de partición, ignorando el selector — debe resolverse ANTES de validar la etiqueta (FAT32
        // limita a 11 caracteres, no los 32 del FS que estuviera elegido en el picker).
        bool smallFat32 = SmallFat32Check.Visibility == Visibility.Visible && SmallFat32Check.IsChecked == true;
        string fs = smallFat32 ? "FAT32" : (FileSystemPicker.SelectedItem?.ToString() ?? "NTFS");
        long? partitionSizeBytes = smallFat32 ? ReinitPlan.SmallFat32PartitionBytes(SelectedSmallFat32SizeGb()) : null;
        string label = VolumeLabelBox.Text.Trim();
        if (!await ValidateLabelAsync(label, fs, focusOnError: false))
            return;

        DiskPartitionStyle style;
        try { style = ReinitPlan.StyleFor(item.Info.TotalSize); } catch { style = DiskPartitionStyle.Mbr; }

        // Confirmación reforzada: escribir la letra de la unidad (reutiliza ConfirmDialog).
        string summary = smallFat32
            ? L.T("reinit.summaryFat32Small", item.Letter, FormatLogic.FormatBytes(partitionSizeBytes!.Value))
            : L.T("reinit.summary", item.Letter, style.ToPowerShell(), fs);
        var confirm = new ConfirmDialog(item.Letter, summary) { XamlRoot = Content.XamlRoot, RequestedTheme = CurrentTheme };
        if (await confirm.ShowAsync() != ContentDialogResult.Primary) return;

        BeginOperation();
        FormatProgress.IsIndeterminate = true;
        StatusText.ClearValue(TextBlock.ForegroundProperty);
        SetStatusAndAnnounce(L.T("reinit.stage.clean", item.Letter));

        var stage = new Progress<string>(s => StatusText.Text = L.T($"reinit.stage.{s}", item.Letter));

        try
        {
            var r = await _services.Reinit.RunAsync(item.Letter, style, fs, label, partitionSizeBytes, stage, _cts!.Token);
            FormatProgress.IsIndeterminate = false;

            if (_cancelRequested || r.Detail == "cancelled")
            {
                FormatProgress.Value = 0;
                StatusText.Text = L.T("status.cancelled");
                return;
            }

            if (r.Ok && r.NewLetter is char newLetter)
            {
                FormatProgress.Value = 100;
                _services.History.Log($"REINIT {item.Letter}: -> {newLetter}: fs={fs} style={style.ToPowerShell()}{(smallFat32 ? $" small-fat32={partitionSizeBytes}" : "")}");
                _pendingInitialLetter = newLetter;
                DrivePicker.SelectedIndex = -1;   // fuerza que LoadDrives use la nueva letra
                LoadDrives();
                string doneBody = smallFat32
                    ? L.T("reinit.doneBodyFat32Small", newLetter, FormatLogic.FormatBytes(partitionSizeBytes!.Value))
                    : L.T("reinit.doneBody", newLetter);
                await ShowInfoAsync(L.T("reinit.doneTitle"), doneBody);
            }
            else
            {
                FormatProgress.Value = 0;
                _lastOperationFailed = true;
                _services.History.Log($"REINIT FAIL {item.Letter}: {r.Detail}");
                StatusText.Foreground = new SolidColorBrush(ProtectedColor());
                StatusText.Text = L.T("reinit.failed");
                await ShowInfoAsync(L.T("reinit.title"), L.T("reinit.failed"));
            }
        }
        catch (OperationCanceledException)
        {
            FormatProgress.Value = 0;
            StatusText.Text = L.T("status.cancelled");
        }
        catch (Exception ex)
        {
            // ReinitDrive.RunAsync ya devuelve el fallo como resultado en vez de lanzarlo, así que
            // aquí solo llegaría un fallo de la propia UI. Se cubre igual: el disco ya está borrado
            // a estas alturas y morir sin decir nada sería el peor momento para hacerlo.
            await ReportOperationErrorAsync("REINIT", item.Letter, ex);
        }
        finally
        {
            FormatProgress.IsIndeterminate = false;
            EndOperation();
        }
    }

    // ── Benchmark rápido (#9) ──────────────────────────────────────

    private async void MnuBenchmark_Click(object sender, RoutedEventArgs e)
    {
        if (_isBusy || DrivePicker.SelectedItem is not DriveViewModel item) return;

        if (!await ShowConfirmAsync(L.T("bench.confirmTitle"), L.T("bench.confirmBody", item.Letter)))
            return;

        BeginOperation();
        FormatProgress.IsIndeterminate = false;
        FormatProgress.Value = 0;
        StatusText.ClearValue(TextBlock.ForegroundProperty);
        SetStatusAndAnnounce(L.T("bench.preparing", item.Letter));

        // Tras terminar, ignora cualquier callback de progreso aún en cola (no debe pisar el estado final).
        bool benchRunning = true;
        var progress = new Progress<(BenchPhase phase, int percent)>(p =>
        {
            if (!benchRunning) return;
            FormatProgress.Value = Math.Clamp(p.percent, 0, 100);
            StatusText.Text = L.T(p.phase switch
            {
                BenchPhase.SeqWrite => "bench.seqWrite",
                BenchPhase.SeqRead  => "bench.seqRead",
                BenchPhase.RndWrite => "bench.rndWrite",
                BenchPhase.RndRead  => "bench.rndRead",
                _                   => "bench.preparing",
            }, item.Letter);
        });

        BenchmarkResult? res = null;
        bool cancelled = false;
        Exception? failure = null;
        try
        {
            res = await _services.Benchmark.RunAsync(item.Letter, progress, _cts!.Token);
        }
        catch (OperationCanceledException)
        {
            cancelled = true;   // se trata abajo como cancelación
        }
        catch (Exception ex)
        {
            // Igual que la cancelación: se guarda y se trata DESPUÉS del finally, para no mostrar un
            // modal con la operación todavía abierta. File.OpenHandle lanza si la unidad no admite
            // E/S sin búfer (algunos medios virtuales o de red) o si se queda sin espacio.
            failure = ex;
        }
        finally
        {
            // Cierra la operación (para el cronómetro, repone los botones, avisa si procede) ANTES de mostrar
            // ningún diálogo modal, para que el pie de página no quede en estado "ocupado" tras el resultado.
            benchRunning = false;
            EndOperation();
        }

        if (cancelled || _cancelRequested)
        {
            FormatProgress.Value = 0;
            StatusText.Text = L.T("status.cancelled");
            return;
        }

        if (failure is not null)
        {
            // ShowError directo: EndOperation() ya corrió en el finally, así que _lastOperationFailed
            // no llegaría a la barra (mismo motivo que el caso `res is null` de abajo).
            FormatProgress.ShowError = true;
            await ReportOperationErrorAsync("BENCH", item.Letter, failure);
            return;
        }

        if (res is null)
        {
            // Se detecta tras EndOperation() (arriba, en el finally): no puede pasar por
            // _lastOperationFailed, se fija ShowError directamente.
            FormatProgress.Value = 0;
            FormatProgress.ShowError = true;
            StatusText.Text = L.T("bench.failed", item.Letter);
            await ShowInfoAsync(L.T("bench.resultTitle"), L.T("bench.noSpace", item.Letter));
            return;
        }

        FormatProgress.Value = 100;
        string seqW = Throughput.FormatSpeed(res.Sequential.WriteBytesPerSec);
        string seqR = Throughput.FormatSpeed(res.Sequential.ReadBytesPerSec);
        string rndW = Throughput.FormatSpeed(res.Random4K.WriteBytesPerSec);
        string rndR = Throughput.FormatSpeed(res.Random4K.ReadBytesPerSec);
        // IOPS junto a los MB/s del 4 KiB aleatorio (como CrystalDiskMark): bytes/s ÷ 4096, redondeado.
        string rndWIops = FormatIops(res.Random4K.WriteBytesPerSec);
        string rndRIops = FormatIops(res.Random4K.ReadBytesPerSec);
        _services.History.Log($"BENCH {item.Letter}: seq w={seqW} r={seqR} · rnd4k w={rndW} ({rndWIops}) r={rndR} ({rndRIops}) bytes={res.TestBytes}");
        StatusText.Text = L.T("bench.resultTitle");
        await ShowDialogAsync(
            L.T("bench.resultTitle"),
            L.T("bench.resultBody", item.Letter, seqW, seqR, rndW, rndR, rndWIops, rndRIops) + "\n\n" + L.T("bench.note"),
            null, null, L.T("btn.close"));
    }

    /// <summary>Formatea las IOPS de una velocidad de 4 KiB aleatorio como entero con sufijo "IOPS".</summary>
    private static string FormatIops(double bytesPerSec) =>
        $"{Math.Round(Benchmark.Iops(bytesPerSec, Benchmark.Random4KBlockBytes)):N0} IOPS";
}
