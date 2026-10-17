// SPDX-License-Identifier: GPL-3.0-only
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

    /// <summary>
    /// Disco físico y partición que hay detrás de la letra; <c>null</c> si no se puede determinar. Es lo
    /// que se compara antes y después de confirmar una operación destructiva (`AUD-013`).
    /// </summary>
    Task<DriveIdentity?> GetIdentityAsync(char letter);

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
/// <param name="runner">Lanzador de procesos.</param>
/// <param name="isMounted">
/// ¿Sigue montado el volumen de esta letra? Por omisión se pregunta a Windows; las pruebas lo sustituyen
/// para no depender de qué haya conectado.
/// </param>
/// <param name="ejectWait">Cuánto se espera a que una unidad expulsada desaparezca. Por omisión, 8 s.</param>
/// <param name="queryTimeout">
/// Cuánto se espera a cada PowerShell antes de matarlo y dar el dato por desconocido. Por omisión,
/// <see cref="DefaultQueryTimeout"/>; las pruebas lo acortan.
/// </param>
public sealed class DiskService(
    IProcessRunner runner, Func<char, bool>? isMounted = null, TimeSpan? ejectWait = null,
    TimeSpan? queryTimeout = null) : IDiskService
{
    /// <summary>
    /// Plazo de cada consulta (`AUD-010`). Antes no había ninguno: una unidad averiada que deja colgado
    /// <c>Get-Disk</c>, que es justo el tipo de unidad para el que existe esta herramienta, dejaba
    /// esperando para siempre a «Formatear» y a «Reinicializar». La ventana seguía respondiendo, pero el
    /// botón pulsado no hacía nada. En una unidad sana estas consultas tardan uno o dos segundos.
    /// </summary>
    public static readonly TimeSpan DefaultQueryTimeout = TimeSpan.FromSeconds(15);

    private readonly Func<char, bool> _isMounted = isMounted ?? IsVolumeMounted;
    private readonly TimeSpan _ejectWait = ejectWait ?? TimeSpan.FromSeconds(8);
    private readonly TimeSpan _queryTimeout = queryTimeout ?? DefaultQueryTimeout;

    private static bool IsVolumeMounted(char letter)
    {
        try { return new DriveInfo(letter.ToString()).IsReady; }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or ArgumentException)
        {
            return false;
        }
    }

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

    /// <inheritdoc/>
    /// <remarks>
    /// Una sola consulta para los diez datos: tienen que ser de un mismo instante. El orden de los campos
    /// es el que espera <see cref="DriveIdentity.Parse"/>, con el identificador único al final.
    /// </remarks>
    public async Task<DriveIdentity?> GetIdentityAsync(char letter)
    {
        if (!char.IsLetter(letter)) return null;

        string script =
            "$ErrorActionPreference='Stop';" +
            $"$p = Get-Partition -DriveLetter {letter}; $d = $p | Get-Disk;" +
            "\"$($d.Number)|$($d.Size)|$($d.BusType)|$($d.IsSystem)|$($d.IsBoot)|" +
            "$($p.PartitionNumber)|$($p.Offset)|$($p.Size)|$($d.SerialNumber)|$($d.UniqueId)\"";

        return DriveIdentity.Parse(await RunCapturedAsync(script));
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

        if (await RunAsync(script) != 0) return false;

        // El verbo «Eject» del shell no dice si funcionó: PowerShell termina con 0 también cuando
        // Windows se niega porque hay un archivo abierto (`AUD-008`). Lo que cuenta es si el volumen
        // desaparece, y cuando funciona lo hace en un par de segundos. Si pasado el plazo sigue ahí,
        // no se expulsó: decir que sí invita a tirar del cable con datos a medio escribir.
        var deadline = DateTime.UtcNow + _ejectWait;
        while (_isMounted(letter))
        {
            if (DateTime.UtcNow >= deadline) return false;
            await Task.Delay(250);
        }
        return true;
    }

    // ── Internos ──────────────────────────────────────────────────

    /// <remarks>
    /// La salida de error <b>no se redirige</b> (`AUD-010`). Antes se redirigía y nadie la leía: con
    /// <c>-EncodedCommand</c>, PowerShell escribe cada error como CLIXML (1,7 KB por error), y en cuanto
    /// llenaba la tubería se quedaba bloqueado escribiendo, sin terminar nunca.
    /// </remarks>
    private static ProcessSpec BuildSpec(string script, bool capture)
    {
        byte[] bytes = Encoding.Unicode.GetBytes(script);
        string encoded = Convert.ToBase64String(bytes);
        return new ProcessSpec
        {
            FileName               = SystemTools.PowerShell,
            Arguments              = $"-NonInteractive -NoProfile -EncodedCommand {encoded}",
            RedirectStandardOutput = capture,
            RedirectStandardError  = false,
        };
    }

    /// <summary>
    /// Espera a que termine <paramref name="proc"/> como mucho <see cref="_queryTimeout"/>. Si se pasa, lo
    /// mata y devuelve <c>false</c>: quien llama da el dato por desconocido.
    /// </summary>
    private async Task<bool> WaitWithinTimeoutAsync(IProcessHandle proc)
    {
        using var cts = new CancellationTokenSource(_queryTimeout);
        try
        {
            await proc.WaitForExitAsync(cts.Token);
            return true;
        }
        catch (OperationCanceledException)
        {
            try { proc.Kill(entireProcessTree: true); } catch { /* pudo terminar justo ahora */ }
            return false;
        }
    }

    private async Task<int> RunAsync(string script)
    {
        try
        {
            using var proc = runner.Start(BuildSpec(script, capture: false));
            return await WaitWithinTimeoutAsync(proc) ? proc.ExitCode : -1;
        }
        catch { return -1; }
    }

    private async Task<string> RunCapturedAsync(string script)
    {
        try
        {
            using var proc = runner.Start(BuildSpec(script, capture: true));
            var outTask = proc.StandardOutput.ReadToEndAsync();
            if (!await WaitWithinTimeoutAsync(proc)) return "";
            return await outTask;
        }
        catch { return ""; }
    }
}
