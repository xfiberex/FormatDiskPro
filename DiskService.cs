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

    /// <summary>Sobrescribe el espacio libre de la unidad (cipher /w) para borrado seguro de datos.</summary>
    public static async Task<int> SecureWipeAsync(char letter, CancellationToken ct)
    {
        if (!char.IsLetter(letter)) return -1;

        var psi = new ProcessStartInfo
        {
            FileName               = "cipher.exe",
            Arguments              = $"/w:{letter}:\\",
            UseShellExecute        = false,
            RedirectStandardOutput = true,
            RedirectStandardError  = true,
            CreateNoWindow         = true,
        };

        using var proc = new Process { StartInfo = psi };
        proc.Start();
        using var reg = ct.Register(() => { try { proc.Kill(true); } catch { } });
        await proc.WaitForExitAsync(CancellationToken.None);
        return proc.ExitCode;
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
