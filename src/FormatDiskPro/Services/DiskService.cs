using System.Globalization;
using System.Text;

namespace FormatDiskPro;

/// <summary>
/// Consultas y cambios de estado sobre unidades: salud S.M.A.R.T., disco físico, protección de
/// escritura y expulsión.
/// </summary>
public interface IDiskService
{
    /// <summary>Obtiene salud S.M.A.R.T., tipo de bus y tipo de medio del disco físico de la unidad.</summary>
    Task<DiskService.HealthInfo?> GetHealthAsync(char letter);

    /// <summary>Obtiene el detalle S.M.A.R.T. extendido del disco físico de la unidad.</summary>
    Task<SmartInfo?> GetSmartAsync(char letter);

    /// <summary>Indica si el disco físico está en solo lectura; <c>null</c> si no se puede determinar.</summary>
    Task<bool?> IsDiskReadOnlyAsync(char letter);

    /// <summary>Número de disco físico de la unidad; <c>null</c> si no se puede determinar.</summary>
    Task<int?> GetDiskNumberAsync(char letter);

    /// <summary>Tamaño en bytes del disco físico de la unidad; <c>null</c> si no se puede determinar.</summary>
    Task<long?> GetDiskSizeAsync(char letter);

    /// <summary>Quita la protección de escritura del disco físico. <c>true</c> si lo logra.</summary>
    Task<bool> ClearReadOnlyAsync(char letter);

    /// <summary>Expulsa una unidad removible usando el shell de Windows.</summary>
    Task<bool> EjectAsync(char letter);
}

/// <summary>
/// Operaciones sobre unidades vía PowerShell: salud S.M.A.R.T., expulsión y borrado seguro.
/// Todos los comandos se envían como -EncodedCommand (Base64 UTF-16LE) para evitar inyección.
/// </summary>
/// <remarks>
/// Recibe el <see cref="IProcessRunner"/> por constructor (`T4-02`): así los caminos de error
/// —salida vacía, texto que no se puede interpretar, PowerShell que no arranca— se prueban sin
/// disco de por medio. El parseo de la salida sigue viviendo en <c>Core/</c>.
/// </remarks>
public sealed class DiskService(IProcessRunner runner) : IDiskService
{
    public sealed record HealthInfo(string Health, string Bus, string Media);

    public async Task<HealthInfo?> GetHealthAsync(char letter)
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

    /// <inheritdoc/>
    /// <remarks>
    /// Los contadores de fiabilidad pueden no estar disponibles (p. ej. USB) → quedan nulos.
    /// </remarks>
    public async Task<SmartInfo?> GetSmartAsync(char letter)
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

    public async Task<bool?> IsDiskReadOnlyAsync(char letter)
    {
        if (!char.IsLetter(letter)) return null;

        string script =
            "$ErrorActionPreference='Stop';" +
            $"(Get-Partition -DriveLetter {letter} | Get-Disk).IsReadOnly";

        string output = (await RunCapturedAsync(script)).Trim();
        if (output.Equals("True", StringComparison.OrdinalIgnoreCase)) return true;
        if (output.Equals("False", StringComparison.OrdinalIgnoreCase)) return false;
        return null;
    }

    /// <inheritdoc/>
    /// <remarks>Guarda crítica: se usa para no reinicializar el disco del sistema.</remarks>
    public async Task<int?> GetDiskNumberAsync(char letter)
    {
        if (!char.IsLetter(letter)) return null;

        string script =
            "$ErrorActionPreference='Stop';" +
            $"(Get-Partition -DriveLetter {letter} | Get-Disk).Number";

        string output = (await RunCapturedAsync(script)).Trim();
        return int.TryParse(output, out int number) ? number : null;
    }

    /// <inheritdoc/>
    /// <remarks>
    /// Es el tope de la partición FAT32 pequeña. Va aparte de <see cref="GetHealthAsync"/> a propósito:
    /// lo que hace falta es el tamaño del DISCO, y <see cref="DriveInfo.TotalSize"/> mide el volumen —ver
    /// <see cref="ReinitPlan.SmallFat32SizesFor"/>.
    /// </remarks>
    public async Task<long?> GetDiskSizeAsync(char letter)
    {
        if (!char.IsLetter(letter)) return null;

        string script =
            "$ErrorActionPreference='Stop';" +
            $"(Get-Partition -DriveLetter {letter} | Get-Disk).Size";

        string output = (await RunCapturedAsync(script)).Trim();
        return long.TryParse(output, NumberStyles.Integer, CultureInfo.InvariantCulture, out long size) && size > 0
            ? size
            : null;
    }

    public async Task<bool> ClearReadOnlyAsync(char letter)
    {
        if (!char.IsLetter(letter)) return false;

        string script =
            "$ErrorActionPreference='Stop';" +
            $"Get-Partition -DriveLetter {letter} | Get-Disk | Set-Disk -IsReadOnly $false";

        return await RunAsync(script) == 0;
    }

    public async Task<bool> EjectAsync(char letter)
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

    private static ProcessSpec BuildSpec(string script, bool capture)
    {
        byte[] bytes = Encoding.Unicode.GetBytes(script);
        string encoded = Convert.ToBase64String(bytes);
        return new ProcessSpec
        {
            FileName               = "powershell.exe",
            Arguments              = $"-NonInteractive -NoProfile -EncodedCommand {encoded}",
            RedirectStandardOutput = capture,
            RedirectStandardError  = capture,
        };
    }

    private async Task<int> RunAsync(string script)
    {
        try
        {
            using var proc = runner.Start(BuildSpec(script, capture: false));
            await proc.WaitForExitAsync();
            return proc.ExitCode;
        }
        catch { return -1; }
    }

    private async Task<string> RunCapturedAsync(string script)
    {
        try
        {
            using var proc = runner.Start(BuildSpec(script, capture: true));
            var outTask = proc.StandardOutput.ReadToEndAsync();
            await proc.WaitForExitAsync();
            return await outTask;
        }
        catch { return ""; }
    }
}
