using System.Diagnostics;
using System.Text;

namespace FormatDiskPro;

/// <summary>
/// Reinicializa una unidad extraíble: limpia el disco físico y recrea una única partición primaria
/// formateada (para USB con particiones raras, RAW o esquemas corruptos). Usa cmdlets de Storage
/// (<c>Clear-Disk</c>/<c>Initialize-Disk</c>/<c>New-Partition</c>/<c>Format-Volume</c>) vía
/// <c>-EncodedCommand</c> (Base64 UTF-16LE) para evitar inyección, igual que <see cref="DiskService"/>.
/// </summary>
/// <remarks>
/// <b>Destructivo a nivel de disco físico:</b> <c>Clear-Disk</c> borra <i>todas</i> las particiones del
/// disco, no solo la unidad seleccionada. La capa de UI aplica las guardas (solo extraíbles, no el disco
/// del sistema, y disco físico distinto al de Windows) antes de invocar este servicio.
/// </remarks>
public static class ReinitDrive
{
    /// <summary>
    /// Limpia y recrea la partición de la unidad <paramref name="letter"/>, formateándola con
    /// <paramref name="fileSystem"/> y <paramref name="label"/>. Reporta la etapa en curso
    /// (<c>clean</c>/<c>init</c>/<c>partition</c>/<c>format</c>) y devuelve la nueva letra asignada.
    /// </summary>
    /// <param name="letter">Letra de la unidad a reinicializar.</param>
    /// <param name="style">Estilo de partición (MBR o GPT).</param>
    /// <param name="fileSystem">Sistema de archivos destino (NTFS, exFAT, FAT32, FAT, ReFS).</param>
    /// <param name="label">Etiqueta de volumen (puede ser vacía).</param>
    /// <param name="stage">Etapa en curso, como token sin traducir.</param>
    /// <param name="ct">Token de cancelación (mata el proceso al cancelar).</param>
    /// <returns>Resultado con éxito, nueva letra y detalle de error si lo hubo.</returns>
    public static async Task<ReinitResult> RunAsync(
        char letter, DiskPartitionStyle style, string fileSystem, string label,
        IProgress<string> stage, CancellationToken ct)
    {
        if (!char.IsLetter(letter)) return new ReinitResult(false, null, "invalid-letter");

        // El sistema de archivos procede de un selector cerrado; validamos por si acaso (solo letras/dígitos).
        if (string.IsNullOrEmpty(fileSystem) || !fileSystem.All(char.IsLetterOrDigit))
            return new ReinitResult(false, null, "invalid-fs");

        string safeLabel = label.Replace("'", "''");   // literal de PowerShell con comillas simples
        string styleName = style.ToPowerShell();

        string script =
            "$ErrorActionPreference='Stop';" +
            $"$d = (Get-Partition -DriveLetter {letter} | Get-Disk);" +
            "'STAGE:clean';" +
            "Clear-Disk -Number $d.Number -RemoveData -RemoveOEM -Confirm:$false;" +
            "'STAGE:init';" +
            $"Initialize-Disk -Number $d.Number -PartitionStyle {styleName};" +
            "'STAGE:partition';" +
            "$p = New-Partition -DiskNumber $d.Number -UseMaximumSize -AssignDriveLetter;" +
            "'STAGE:format';" +
            $"Format-Volume -Partition $p -FileSystem {fileSystem} -NewFileSystemLabel '{safeLabel}' -Confirm:$false | Out-Null;" +
            // Re-consultamos la letra asignada (el objeto recién creado puede no reflejarla aún).
            "$l = (Get-Partition -DiskNumber $d.Number | Where-Object { $_.DriveLetter } | Select-Object -First 1).DriveLetter;" +
            "'LETTER:' + $l";

        byte[] bytes   = Encoding.Unicode.GetBytes(script);
        string encoded = Convert.ToBase64String(bytes);

        var psi = new ProcessStartInfo
        {
            FileName               = "powershell.exe",
            Arguments              = $"-NonInteractive -NoProfile -EncodedCommand {encoded}",
            UseShellExecute        = false,
            RedirectStandardOutput = true,
            RedirectStandardError  = true,
            CreateNoWindow         = true,
        };

        try
        {
            using var proc = new Process { StartInfo = psi };
            proc.Start();
            using var reg = ct.Register(() => { try { proc.Kill(entireProcessTree: true); } catch { } });

            var errTask = proc.StandardError.ReadToEndAsync(CancellationToken.None);
            var sb      = new StringBuilder();
            var buffer  = new char[512];
            string[] stages = ["clean", "init", "partition", "format"];
            var reported = new HashSet<string>();
            var reader  = proc.StandardOutput;
            int read;
            while ((read = await reader.ReadAsync(buffer.AsMemory(0, buffer.Length))) > 0)
            {
                sb.Append(buffer, 0, read);
                string text = sb.ToString();
                foreach (string s in stages)
                    if (text.Contains($"STAGE:{s}") && reported.Add(s))
                        stage.Report(s);
            }

            string err = await errTask;
            await proc.WaitForExitAsync(CancellationToken.None);

            if (ct.IsCancellationRequested)
                return new ReinitResult(false, null, "cancelled");

            string output = sb.ToString();
            char? newLetter = ReinitPlan.ParseNewLetter(output);
            bool ok = proc.ExitCode == 0 && newLetter is not null;
            string detail = ok ? "" : (string.IsNullOrWhiteSpace(err) ? $"exit={proc.ExitCode}" : err.Trim());
            return new ReinitResult(ok, newLetter, detail);
        }
        catch (Exception ex)
        {
            return new ReinitResult(false, null, ex.Message);
        }
    }
}
