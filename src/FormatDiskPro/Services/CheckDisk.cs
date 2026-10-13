using System.Text;

namespace FormatDiskPro;

/// <summary>Resultado interpretado de una comprobación con chkdsk.</summary>
public enum CheckResult { Clean, Repaired, Errors, Failed }

/// <summary>Comprobación/reparación del sistema de archivos con <c>chkdsk</c>.</summary>
public interface ICheckDisk
{
    /// <inheritdoc cref="CheckDisk.RunAsync"/>
    Task<(int code, string output)> RunAsync(
        char letter, bool repair, IProgress<int> progress, CancellationToken ct);
}

/// <summary>
/// Comprobación/reparación del sistema de archivos con <c>chkdsk</c>. El modo solo-lectura
/// (sin modificadores) es seguro y universal (NTFS/FAT/exFAT); la reparación usa <c>/f</c>
/// y requiere bloqueo exclusivo del volumen.
/// </summary>
public sealed class CheckDisk(IProcessRunner runner) : ICheckDisk
{
    /// <summary>
    /// Interpreta el código de salida de chkdsk según si se pidió reparar. Lógica pura y testeable.
    /// Sigue siendo <c>static</c>: no tiene dependencias que inyectar.
    /// </summary>
    /// <param name="exitCode">Código de salida de <c>chkdsk</c>.</param>
    /// <param name="repair">Si se lanzó con <c>/f</c>.</param>
    /// <param name="scanRan">
    /// Si llegó a examinar el volumen (<see cref="CheckDiskOutput.ScanRan"/>). Solo decide el código 3,
    /// que <c>chkdsk</c> usa para «no pude comprobar» <b>y</b> para «hay errores y no los corregí porque
    /// no se pidió /f» (`AUD-007`). En solo lectura, si examinó y terminó en 3, son errores: decir «no se
    /// pudo comprobar (¿está en uso?)» era dar por sana —o por ocupada— una unidad dañada.
    /// </param>
    public static CheckResult Interpret(int exitCode, bool repair, bool scanRan) => exitCode switch
    {
        0 => CheckResult.Clean,
        1 => repair ? CheckResult.Repaired : CheckResult.Errors,
        2 => CheckResult.Errors,
        3 when !repair && scanRan => CheckResult.Errors,
        _ => CheckResult.Failed,
    };

    /// <summary>
    /// Ejecuta chkdsk sobre la unidad. <paramref name="repair"/> añade <c>/f</c> (reparar).
    /// Reporta el progreso (0-100) parseado de la salida y devuelve el código y la salida combinada.
    /// </summary>
    /// <param name="letter">Letra de la unidad.</param>
    /// <param name="repair"><c>true</c> para reparar (<c>/f</c>); <c>false</c> para solo comprobar.</param>
    /// <param name="progress">Progreso 0-100 (parseado de la salida de chkdsk).</param>
    /// <param name="ct">Token de cancelación (mata el proceso al cancelar).</param>
    /// <returns>Código de salida de chkdsk y la salida combinada (stdout + stderr).</returns>
    public async Task<(int code, string output)> RunAsync(
        char letter, bool repair, IProgress<int> progress, CancellationToken ct)
    {
        if (!char.IsLetter(letter)) return (-1, "");

        var args = new List<string> { $"{char.ToUpperInvariant(letter)}:" };
        if (repair) args.Add("/f");

        using var proc = runner.Start(new ProcessSpec
        {
            FileName              = SystemTools.ChkDsk,
            ArgumentList          = args,
            RedirectStandardInput = true,
            OutputEncoding        = SystemTools.AnsiEncoding,   // la salida se enseña: ver AnsiEncoding
        });
        using var reg = ct.Register(() => { try { proc.Kill(entireProcessTree: true); } catch { } });

        // Si chkdsk no puede bloquear el volumen, pregunta: primero si forzar el desmontaje y después
        // si programar la comprobación para el próximo reinicio. Se responde «N» a la primera y **se
        // cierra la entrada** (`AUD-006`). Una sola «N» con la entrada abierta dejaba la segunda
        // pregunta sin respuesta y a chkdsk esperándola para siempre, con la app en «Comprobando…»;
        // con la entrada cerrada sale por sí mismo, con código 3. No se contestan las dos con «N»
        // porque la letra de «no» depende del idioma de Windows y el fin de entrada, no.
        try
        {
            await proc.StandardInput.WriteLineAsync("N");
            await proc.StandardInput.FlushAsync(ct);
        }
        catch { }
        try { proc.StandardInput.Close(); } catch { }

        var errTask = proc.StandardError.ReadToEndAsync();
        var sb      = new StringBuilder();
        var buffer  = new char[512];
        int read, lastPct = -1;
        string carry = "";
        var reader = proc.StandardOutput;
        while ((read = await reader.ReadAsync(buffer.AsMemory(0, buffer.Length))) > 0)
        {
            string chunk = new(buffer, 0, read);
            sb.Append(chunk);

            string scan = carry + chunk;
            int pct = FormatLogic.ExtractPercent(scan);
            if (pct >= 0 && pct != lastPct)
            {
                lastPct = pct;
                progress.Report(Math.Clamp(pct, 0, 100));
            }
            carry = scan.Length > 16 ? scan[^16..] : scan;
        }

        string err = await errTask;
        await proc.WaitForExitAsync(CancellationToken.None);

        string output = sb.ToString();
        if (!string.IsNullOrWhiteSpace(err)) output += "\n" + err;
        return (proc.ExitCode, output);
    }
}
