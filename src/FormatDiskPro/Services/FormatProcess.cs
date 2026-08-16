using System.Text;

namespace FormatDiskPro;

/// <summary>Lanza los procesos que formatean: <c>Format-Volume</c> y <c>format.com</c>.</summary>
public interface IFormatProcess
{
    /// <inheritdoc cref="FormatProcess.RunVolumeAsync"/>
    Task<(int code, string stdout, string stderr)> RunVolumeAsync(
        char driveLetter, string fs, long allocBytes, string label,
        bool quickFormat, bool compress, Action<IProcessHandle>? started, CancellationToken ct);

    /// <inheritdoc cref="FormatProcess.RunComAsync"/>
    Task<(int code, string output)> RunComAsync(
        char driveLetter, string fs, long allocBytes, string label,
        IProgress<int>? percent, Action<IProcessHandle>? started, CancellationToken ct);
}

/// <summary>
/// Lanza los procesos que formatean: <c>Format-Volume</c> (vía PowerShell) y <c>format.com</c>.
///
/// <para>Vive en <c>Services/</c> con sus hermanos —<see cref="CheckDisk"/>, <see cref="ReinitDrive"/>,
/// <see cref="SecureWipe"/>— y no en la ventana: lanzar procesos y leer su salida no es
/// responsabilidad de la UI, y mientras estuvo ahí era el único flujo de formateo sin un sitio propio
/// donde mirarlo. Los <b>comandos</b> siguen construyéndose en <see cref="FormatLogic"/> (lógica pura,
/// bajo test); aquí solo se ejecutan.</para>
///
/// <para>El proceso en marcha se entrega por <paramref name="started"/> en vez de guardarse aquí: quien
/// llama necesita poder matarlo al cancelar, y un estado estático compartido en un servicio que puede
/// invocarse desde varios sitios es justo lo que no conviene.</para>
/// </summary>
public sealed class FormatProcess(IProcessRunner runner) : IFormatProcess
{
    /// <summary>
    /// Formatea con <c>Format-Volume</c> (PowerShell). Devuelve el código de salida y las dos salidas por
    /// separado: el error, cuando lo hay, es más informativo que la salida estándar.
    /// </summary>
    /// <param name="driveLetter">Letra de la unidad.</param>
    /// <param name="fs">Sistema de archivos (NTFS, exFAT, ReFS, FAT32, FAT).</param>
    /// <param name="allocBytes">Tamaño de unidad de asignación, en bytes.</param>
    /// <param name="label">Etiqueta del volumen.</param>
    /// <param name="quickFormat">Formato rápido.</param>
    /// <param name="compress">Compresión NTFS.</param>
    /// <param name="started">Recibe el proceso recién arrancado (para poder cancelarlo).</param>
    /// <param name="ct">Cancelación: mata el árbol de procesos.</param>
    public async Task<(int code, string stdout, string stderr)> RunVolumeAsync(
        char driveLetter, string fs, long allocBytes, string label,
        bool quickFormat, bool compress, Action<IProcessHandle>? started, CancellationToken ct)
    {
        string script = FormatLogic.BuildVolumeScript(driveLetter, fs, allocBytes, label, quickFormat, compress);
        string args   = FormatLogic.EncodeArguments(script);

        // Sin `using` a propósito: el proceso se entrega a quien llama, que es quien lo cancela y quien lo
        // libera al cerrar la operación. Liberarlo aquí dejaría su referencia inservible del otro lado.
        var proc = runner.Start(new ProcessSpec
        {
            FileName  = "powershell.exe",
            Arguments = args,
        });
        started?.Invoke(proc);
        using var reg = ct.Register(() => { try { proc.Kill(entireProcessTree: true); } catch { } });

        var outTask = proc.StandardOutput.ReadToEndAsync();
        var errTask = proc.StandardError.ReadToEndAsync();
        await proc.WaitForExitAsync(CancellationToken.None);

        return (proc.ExitCode, await outTask, await errTask);
    }

    /// <summary>
    /// Formatea con <c>format.com</c>, que es el único camino que informa del <b>progreso real</b> de un
    /// formato completo (<c>Format-Volume</c> no lo hace).
    /// </summary>
    /// <param name="percent">Porcentaje 0-100 según lo va imprimiendo <c>format.com</c>.</param>
    /// <remarks>
    /// No se escribe nada por la entrada estándar: <c>/Y</c> (ver
    /// <see cref="FormatLogic.BuildComArgumentList"/>) suprime las dos preguntas. Responder <c>"Y"</c>/
    /// <c>"S"</c> solo acertaba en un Windows inglés o español; en uno francés o alemán el proceso se
    /// quedaba esperando una tecla con el formato a medias. La entrada se <b>cierra</b> para que una build
    /// hipotética que sí pregunte falle rápido en vez de colgarse indefinidamente.
    /// </remarks>
    public async Task<(int code, string output)> RunComAsync(
        char driveLetter, string fs, long allocBytes, string label,
        IProgress<int>? percent, Action<IProcessHandle>? started, CancellationToken ct)
    {
        string formatExe = Path.Combine(Environment.SystemDirectory, "format.com");

        // Sin `using` a propósito: el proceso se entrega a quien llama, que es quien lo cancela y quien lo
        // libera al cerrar la operación. Liberarlo aquí dejaría su referencia inservible del otro lado.
        var proc = runner.Start(new ProcessSpec
        {
            FileName              = formatExe,
            ArgumentList          = FormatLogic.BuildComArgumentList(driveLetter, fs, allocBytes, label),
            RedirectStandardInput = true,
        });
        started?.Invoke(proc);
        using var reg = ct.Register(() => { try { proc.Kill(entireProcessTree: true); } catch { } });

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

            // El porcentaje puede quedar partido entre dos lecturas: se rastrea con un solapamiento corto.
            string scan = carry + chunk;
            int pct = FormatLogic.ExtractPercent(scan);
            if (pct >= 0 && pct != lastPct)
            {
                lastPct = pct;
                percent?.Report(Math.Clamp(pct, 0, 100));
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
