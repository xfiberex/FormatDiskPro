using System.Diagnostics;
using System.Text;

namespace FormatDiskPro;

/// <summary>Resultado interpretado de una comprobación con chkdsk.</summary>
public enum CheckResult { Clean, Repaired, Errors, Failed }

/// <summary>
/// Comprobación/reparación del sistema de archivos con <c>chkdsk</c>. El modo solo-lectura
/// (sin modificadores) es seguro y universal (NTFS/FAT/exFAT); la reparación usa <c>/f</c>
/// y requiere bloqueo exclusivo del volumen.
/// </summary>
public static class CheckDisk
{
    /// <summary>
    /// Interpreta el código de salida de chkdsk según si se pidió reparar. Lógica pura y testeable.
    /// </summary>
    public static CheckResult Interpret(int exitCode, bool repair) => exitCode switch
    {
        0 => CheckResult.Clean,
        1 => repair ? CheckResult.Repaired : CheckResult.Errors,
        2 => CheckResult.Errors,
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
    public static async Task<(int code, string output)> RunAsync(
        char letter, bool repair, IProgress<int> progress, CancellationToken ct)
    {
        if (!char.IsLetter(letter)) return (-1, "");

        var psi = new ProcessStartInfo
        {
            FileName               = "chkdsk.exe",
            UseShellExecute        = false,
            RedirectStandardInput  = true,
            RedirectStandardOutput = true,
            RedirectStandardError  = true,
            CreateNoWindow         = true,
        };
        psi.ArgumentList.Add($"{char.ToUpperInvariant(letter)}:");
        if (repair) psi.ArgumentList.Add("/f");

        using var proc = new Process { StartInfo = psi };
        proc.Start();
        using var reg = ct.Register(() => { try { proc.Kill(entireProcessTree: true); } catch { } });

        // Si chkdsk no puede bloquear el volumen, pregunta si programar la comprobación en el próximo
        // reinicio: respondemos "N" para declinar y que el proceso no se quede esperando entrada.
        try
        {
            await proc.StandardInput.WriteLineAsync("N");
            await proc.StandardInput.FlushAsync(ct);
        }
        catch { }

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
