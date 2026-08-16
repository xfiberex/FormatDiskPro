using System.Diagnostics;

namespace FormatDiskPro;

/// <summary>
/// Descripción de un proceso a lanzar. Es un DTO: no decide nada, solo traslada a
/// <see cref="IProcessRunner"/> lo que hoy se escribía a mano en cada <see cref="ProcessStartInfo"/>.
/// </summary>
/// <remarks>
/// Los indicadores de redirección se declaran <b>uno por uno</b> en vez de redirigirlo todo siempre:
/// un proceso cuya salida se redirige pero no se lee puede bloquearse al llenarse la tubería, así que
/// cada servicio pide exactamente lo que va a consumir — igual que antes de existir esta costura.
/// </remarks>
public sealed class ProcessSpec
{
    /// <summary>Ejecutable a lanzar (p. ej. <c>powershell.exe</c>, <c>chkdsk.exe</c>).</summary>
    public required string FileName { get; init; }

    /// <summary>Línea de argumentos completa. Excluyente con <see cref="ArgumentList"/>.</summary>
    public string? Arguments { get; init; }

    /// <summary>Argumentos sueltos, escapados de uno en uno por el runtime.</summary>
    public IReadOnlyList<string> ArgumentList { get; init; } = [];

    public bool RedirectStandardInput  { get; init; }
    public bool RedirectStandardOutput { get; init; } = true;
    public bool RedirectStandardError  { get; init; } = true;
}

/// <summary>
/// Un proceso ya arrancado. Refleja la parte de <see cref="Process"/> que estos servicios usan, para
/// que el bucle de lectura de cada uno siga siendo <b>exactamente</b> el mismo.
/// </summary>
public interface IProcessHandle : IDisposable
{
    TextReader StandardOutput { get; }
    TextReader StandardError  { get; }
    TextWriter StandardInput  { get; }
    int ExitCode { get; }
    Task WaitForExitAsync(CancellationToken ct = default);
    void Kill(bool entireProcessTree);
}

/// <summary>
/// Lanza procesos. <b>Es la costura que hace testeables los caminos de error de los servicios</b>
/// (`T4-02`): hasta ahora todos hacían <c>new Process(...)</c> directamente, así que reproducir un
/// <c>chkdsk</c> que devuelve 2, un <c>Clear-Disk</c> que falla o un <c>powershell.exe</c> que ni
/// siquiera arranca exigía el hardware —o la avería— de verdad.
/// </summary>
/// <remarks>
/// La abstracción se queda deliberadamente en <b>arrancar</b> el proceso, no en «ejecutar y devolver
/// la salida». Cada servicio lee su salida de una forma distinta y con matices que costaron hardware
/// real descubrir (el <c>carry</c> del solapamiento de marcadores, cerrar la entrada estándar,
/// esperar con <see cref="CancellationToken.None"/> para no perder el código de salida al cancelar).
/// Unificar esos bucles aquí sería reescribirlos, y este cambio no puede cambiar comportamiento.
/// </remarks>
public interface IProcessRunner
{
    /// <summary>Arranca el proceso descrito. Quien llama es dueño del handle y debe liberarlo.</summary>
    IProcessHandle Start(ProcessSpec spec);
}

/// <summary>Implementación real: <see cref="Process"/> del sistema.</summary>
public sealed class SystemProcessRunner : IProcessRunner
{
    public IProcessHandle Start(ProcessSpec spec)
    {
        var psi = new ProcessStartInfo
        {
            FileName               = spec.FileName,
            UseShellExecute        = false,
            RedirectStandardInput  = spec.RedirectStandardInput,
            RedirectStandardOutput = spec.RedirectStandardOutput,
            RedirectStandardError  = spec.RedirectStandardError,
            CreateNoWindow         = true,
        };
        if (spec.Arguments is not null) psi.Arguments = spec.Arguments;
        foreach (string a in spec.ArgumentList) psi.ArgumentList.Add(a);

        var proc = new Process { StartInfo = psi };
        proc.Start();
        return new SystemProcessHandle(proc);
    }

    private sealed class SystemProcessHandle(Process proc) : IProcessHandle
    {
        public TextReader StandardOutput => proc.StandardOutput;
        public TextReader StandardError  => proc.StandardError;
        public TextWriter StandardInput  => proc.StandardInput;
        public int ExitCode => proc.ExitCode;

        public Task WaitForExitAsync(CancellationToken ct = default) => proc.WaitForExitAsync(ct);
        public void Kill(bool entireProcessTree) => proc.Kill(entireProcessTree);
        public void Dispose() => proc.Dispose();
    }
}
