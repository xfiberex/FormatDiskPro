using System.Diagnostics;
using System.Text;

namespace FormatDiskPro;

/// <summary>
/// Operaciones sobre unidades vía PowerShell: salud S.M.A.R.T., expulsión y borrado seguro.
/// Todos los comandos se envían como -EncodedCommand (Base64 UTF-16LE) para evitar inyección.
/// </summary>
public static class DiskService
{
    public sealed record HealthInfo(string Health, string Bus, string Media);

    /// <summary>Obtiene salud S.M.A.R.T., tipo de bus y tipo de medio del disco físico de la unidad.</summary>
    public static async Task<HealthInfo?> GetHealthAsync(char letter)
    {
        if (!char.IsLetter(letter)) return null;

        string script =
            $"$ErrorActionPreference='Stop';" +
            $"$p = Get-Partition -DriveLetter {letter};" +
            "$d = $p | Get-Disk | Get-PhysicalDisk | Select-Object -First 1;" +
            "\"$($d.HealthStatus)|$($d.BusType)|$($d.MediaType)\"";

        string output = await RunCapturedAsync(script);
        string line = output.Trim();
        if (string.IsNullOrEmpty(line) || !line.Contains('|')) return null;

        string[] parts = line.Split('|');
        return new HealthInfo(
            parts.Length > 0 ? parts[0].Trim() : "?",
            parts.Length > 1 ? parts[1].Trim() : "?",
            parts.Length > 2 ? parts[2].Trim() : "?");
    }

    /// <summary>
    /// Obtiene el detalle S.M.A.R.T. extendido del disco físico de la unidad (salud, bus, medio,
    /// RPM, temperatura, horas de encendido, desgaste de SSD y errores de lectura/escritura).
    /// Los contadores de fiabilidad pueden no estar disponibles (p. ej. USB) → quedan nulos.
    /// </summary>
    public static async Task<SmartInfo?> GetSmartAsync(char letter)
    {
        if (!char.IsLetter(letter)) return null;

        string script =
            "$ErrorActionPreference='Stop';" +
            $"$d = (Get-Partition -DriveLetter {letter} | Get-Disk | Get-PhysicalDisk | Select-Object -First 1);" +
            "$r = $d | Get-StorageReliabilityCounter -ErrorAction SilentlyContinue;" +
            "\"$($d.HealthStatus)|$($d.BusType)|$($d.MediaType)|$($d.SpindleSpeed)|$($r.Temperature)|$($r.PowerOnHours)|$($r.Wear)|$($r.ReadErrorsTotal)|$($r.WriteErrorsTotal)\"";

        string output = await RunCapturedAsync(script);
        return SmartInfo.Parse(output);
    }

    /// <summary>Expulsa una unidad removible usando el shell de Windows.</summary>
    public static async Task<bool> EjectAsync(char letter)
    {
        if (!char.IsLetter(letter)) return false;

        string script =
            "$sh = New-Object -ComObject Shell.Application;" +
            $"$item = $sh.Namespace(17).ParseName('{letter}:');" +
            "if ($item) { $item.InvokeVerb('Eject') }";

        int code = await RunAsync(script);
        return code == 0;
    }

    // ── Internos ──────────────────────────────────────────────────

    private static ProcessStartInfo BuildPsi(string script, bool capture)
    {
        byte[] bytes = Encoding.Unicode.GetBytes(script);
        string encoded = Convert.ToBase64String(bytes);
        return new ProcessStartInfo
        {
            FileName               = "powershell.exe",
            Arguments              = $"-NonInteractive -NoProfile -EncodedCommand {encoded}",
            UseShellExecute        = false,
            RedirectStandardOutput = capture,
            RedirectStandardError  = capture,
            CreateNoWindow         = true,
        };
    }

    private static async Task<int> RunAsync(string script)
    {
        try
        {
            using var proc = new Process { StartInfo = BuildPsi(script, capture: false) };
            proc.Start();
            await proc.WaitForExitAsync();
            return proc.ExitCode;
        }
        catch { return -1; }
    }

    private static async Task<string> RunCapturedAsync(string script)
    {
        try
        {
            using var proc = new Process { StartInfo = BuildPsi(script, capture: true) };
            proc.Start();
            var outTask = proc.StandardOutput.ReadToEndAsync();
            await proc.WaitForExitAsync();
            return await outTask;
        }
        catch { return ""; }
    }
}
