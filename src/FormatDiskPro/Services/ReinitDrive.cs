using System.Text;

namespace FormatDiskPro;

/// <summary>Reinicializa una unidad extraíble: limpia el disco físico y crea sobre él un plan de particiones.</summary>
public interface IReinitDrive
{
    /// <inheritdoc cref="ReinitDrive.RunAsync"/>
    Task<ReinitResult> RunAsync(
        char letter, PartitionPlan plan, long diskSizeBytes, IProgress<string> stage, CancellationToken ct);
}

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
public sealed class ReinitDrive(IProcessRunner runner) : IReinitDrive
{
    /// <summary>
    /// Limpia el disco de la unidad <paramref name="letter"/> y crea sobre él las particiones de
    /// <paramref name="plan"/>, formateando cada una. Reporta la etapa en curso
    /// (<c>clean</c>/<c>init</c>/<c>partition</c>/<c>format</c>) y devuelve las letras asignadas.
    /// </summary>
    /// <remarks>
    /// El plan se <b>revalida aquí</b> (<see cref="PartitionPlan.Validate"/>) aunque la UI ya lo haya
    /// hecho. No es desconfianza gratuita: este método es la última línea antes de <c>Clear-Disk</c>, y un
    /// plan inválido que llegue hasta aquí se descubriría con el disco ya borrado.
    /// </remarks>
    /// <param name="letter">Letra de la unidad a reinicializar.</param>
    /// <param name="plan">Plan de particiones, ya validado por quien llama.</param>
    /// <param name="diskSizeBytes">Tamaño del disco físico, para revalidar el plan.</param>
    /// <param name="stage">Etapa en curso, como token sin traducir.</param>
    /// <param name="ct">Token de cancelación (mata el proceso al cancelar).</param>
    /// <returns>Resultado con éxito, letras asignadas y detalle de error si lo hubo.</returns>
    public async Task<ReinitResult> RunAsync(
        char letter, PartitionPlan plan, long diskSizeBytes, IProgress<string> stage, CancellationToken ct)
    {
        if (!char.IsLetter(letter)) return new ReinitResult(false, null, "invalid-letter");

        PlanValidation check = plan.Validate(diskSizeBytes);
        if (!check.Ok) return new ReinitResult(false, null, $"invalid-plan:{check.Problem}:{check.PartitionIndex}");

        string styleName = plan.Style.ToPowerShell();

        // Cada partición se crea y se formatea antes de pasar a la siguiente, y guarda su objeto en $p<i>
        // para poder releer su letra por número de partición al final. Releerlas por "la primera que tenga
        // letra" —lo que hacía la versión de una sola partición— deja de valer en cuanto hay dos.
        // Cada partición emite sus dos marcadores EN CUANTO los alcanza, no al final del script (`T5-03`).
        // Con `ErrorActionPreference='Stop'`, agrupar los ecos al final significaba que un fallo en la
        // segunda partición abortaba antes de emitir ninguno: la app no podía distinguir "no se creó nada"
        // de "la primera salió bien y la segunda no", que es justo lo que hay que poder contar cuando el
        // disco ya está borrado.
        //
        // Son dos marcadores y no uno porque son dos estados distintos: `PART:i:` significa creada (existe
        // en la tabla de particiones) y `LETTER:i:X` significa además formateada y utilizable. Una partición
        // creada cuyo formato falla se queda entre los dos, y decir "no se creó ninguna" sería falso.
        var body = new StringBuilder();
        for (int i = 0; i < plan.Partitions.Count; i++)
        {
            PartitionSpec p = plan.Partitions[i];
            string safeLabel = p.Label.Replace("'", "''");   // literal de PowerShell con comillas simples
            string size = p.Size is PartitionSize.Exact e ? $"-Size {e.Bytes}" : "-UseMaximumSize";

            if (i == 0) body.Append("'STAGE:partition';");
            body.Append($"$p{i} = New-Partition -DiskNumber $d.Number {size} -AssignDriveLetter;");
            body.Append($"'PART:{i}:' + $p{i}.PartitionNumber;");
            if (i == 0) body.Append("'STAGE:format';");
            body.Append($"Format-Volume -Partition $p{i} -FileSystem {p.FileSystem} -NewFileSystemLabel '{safeLabel}' -Confirm:$false | Out-Null;");

            // Se re-consulta por número de partición: el objeto recién creado puede no reflejar la letra
            // todavía, y Windows las asigna en el orden que quiere.
            body.Append($"'LETTER:{i}:' + (Get-Partition -DiskNumber $d.Number -PartitionNumber $p{i}.PartitionNumber).DriveLetter;");
        }

        string script =
            "$ErrorActionPreference='Stop';" +
            $"$d = (Get-Partition -DriveLetter {letter} | Get-Disk);" +
            "'STAGE:clean';" +
            "$d = Clear-Disk -Number $d.Number -RemoveData -RemoveOEM -Confirm:$false -PassThru;" +
            "'STAGE:init';" +
            // Clear-Disk no siempre deja el disco en RAW (comprobado con hardware real: en algunos
            // USB/controladoras sigue reportándose "inicializado" con su estilo previo tras limpiarlo).
            // Initialize-Disk falla entonces con "The disk has already been initialized" aunque el disco
            // ya esté vacío y listo para particionar; se tolera ese error concreto y se continúa con el
            // estilo que el disco ya tenga (cualquier otro error sí se propaga).
            $"try {{ $d = Initialize-Disk -Number $d.Number -PartitionStyle {styleName} -PassThru -ErrorAction Stop }} catch {{ if ($_.Exception.Message -notmatch 'already been initialized') {{ throw }} }};" +
            body.ToString();

        byte[] bytes   = Encoding.Unicode.GetBytes(script);
        string encoded = Convert.ToBase64String(bytes);

        var spec = new ProcessSpec
        {
            FileName  = "powershell.exe",
            Arguments = $"-NonInteractive -NoProfile -EncodedCommand {encoded}",
        };

        try
        {
            using var proc = runner.Start(spec);
            using var reg = ct.Register(() => { try { proc.Kill(entireProcessTree: true); } catch { } });

            var errTask = proc.StandardError.ReadToEndAsync(CancellationToken.None);
            var sb      = new StringBuilder();
            var buffer  = new char[512];
            string[] stages = ["clean", "init", "partition", "format"];
            var reported = new HashSet<string>();
            var reader  = proc.StandardOutput;
            int read;
            // Se busca sobre el fragmento NUEVO más un solapamiento corto, no sobre el búfer entero: con
            // `sb.ToString()` dentro del bucle, cada lectura rematerializaba toda la salida acumulada y el
            // coste crecía O(n²). El solapamiento cubre un marcador partido entre dos lecturas — el motivo
            // por el que se miraba el texto completo—, que es lo mismo que hace el `carry` de CheckDisk.
            string carry = "";
            const int MarkerOverlap = 24;   // "STAGE:partition" son 15 caracteres; 24 deja margen
            while ((read = await reader.ReadAsync(buffer.AsMemory(0, buffer.Length))) > 0)
            {
                sb.Append(buffer, 0, read);

                string scan = carry + new string(buffer, 0, read);
                foreach (string s in stages)
                    if (scan.Contains($"STAGE:{s}") && reported.Add(s))
                        stage.Report(s);
                carry = scan.Length > MarkerOverlap ? scan[^MarkerOverlap..] : scan;
            }

            string err = await errTask;
            await proc.WaitForExitAsync(CancellationToken.None);

            if (ct.IsCancellationRequested)
                return new ReinitResult(false, null, "cancelled");

            string output = sb.ToString();
            IReadOnlyList<char> letters = ReinitPlan.ParseNewLetters(output);
            int created = ReinitPlan.CountCreatedPartitions(output);

            // Se exige una letra POR partición: con un plan de dos, que solo la primera se creara y
            // formateara es exactamente el fallo parcial que no debe darse por bueno.
            bool ok = proc.ExitCode == 0 && letters.Count == plan.Partitions.Count;
            string detail = ok ? "" : (string.IsNullOrWhiteSpace(err) ? $"exit={proc.ExitCode}" : err.Trim());

            // No se limpia nada al fallar (`T5-03`): el disco ya está borrado, así que "deshacer" solo
            // podría significar borrarlo otra vez. Se informa de lo que quedó y decide el usuario.
            return new ReinitResult(ok, letters.Count > 0 ? letters[0] : null, detail)
            {
                Letters           = letters,
                PartitionsCreated = created,
            };
        }
        catch (Exception ex)
        {
            return new ReinitResult(false, null, ex.Message);
        }
    }
}
