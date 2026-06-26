namespace FormatDiskPro;

/// <summary>
/// Borrado seguro del espacio libre: sobrescribe el espacio libre de la unidad con uno o más
/// patrones para que los datos eliminados previamente no sean recuperables, y reporta
/// <b>progreso real</b> (porcentaje + bytes) — a diferencia de <c>cipher.exe /w</c>, que no
/// expone un porcentaje fiable.
/// </summary>
/// <remarks>
/// Limitación en SSD/NVMe con TRIM: sobrescribir el espacio libre (este método y también
/// <c>cipher /w</c>) no garantiza el borrado a nivel de celdas porque el controlador puede
/// remapear bloques; el borrado seguro real en SSD requiere <i>ATA Secure Erase</i>.
/// </remarks>
public static class SecureWipe
{
    private const int  BlockSize   = 8 * 1024 * 1024;          // 8 MB por escritura
    private const long MaxFileSize = 1L * 1024 * 1024 * 1024;  // 1 GB por archivo (seguro incluso en FAT32)

    /// <summary>Margen de seguridad: deja este espacio libre para no llenar la unidad por completo.</summary>
    public const long SafetyMarginBytes = 64L * 1024 * 1024;

    /// <summary>
    /// Números de pasadas que se ofrecen en la interfaz. NIST 800-88: <b>1</b> pasada basta en discos
    /// modernos; <b>3</b>/<b>7</b> existen para políticas concretas (no aportan en medios con remapeo).
    /// </summary>
    public static readonly int[] AllowedPasses = [1, 3, 7];

    /// <summary>
    /// Ajusta un número de pasadas a un valor admitido (<see cref="AllowedPasses"/>): lo devuelve si es
    /// válido y <c>1</c> en cualquier otro caso (valor persistido corrupto o fuera de rango). Lógica pura.
    /// </summary>
    /// <param name="passes">Número de pasadas a validar.</param>
    public static int NormalizePasses(int passes)
        => Array.IndexOf(AllowedPasses, passes) >= 0 ? passes : 1;

    /// <summary>
    /// Bytes que se sobrescribirán en total: <c>(espacio libre - margen) * pasadas</c> (nunca negativo).
    /// </summary>
    public static long PlannedBytes(long freeBytes, int passes)
        => Math.Max(0, freeBytes - SafetyMarginBytes) * Math.Max(1, passes);

    /// <summary>
    /// Patrón de una pasada: la <b>última</b> pasada es aleatoria; las anteriores alternan
    /// <c>0x00</c> (pares) y <c>0xFF</c> (impares). Con <c>passes = 1</c> la única pasada es aleatoria.
    /// </summary>
    public static (bool Random, byte Fill) PassPattern(int pass, int passes)
    {
        int n = Math.Max(1, passes);
        if (pass >= n - 1) return (true, 0);
        return (false, pass % 2 == 0 ? (byte)0x00 : (byte)0xFF);
    }

    /// <summary>
    /// Sobrescribe el espacio libre de la unidad <paramref name="letter"/> con
    /// <paramref name="passes"/> pasadas y elimina los archivos temporales al terminar
    /// (lo que vuelve a liberar el espacio).
    /// </summary>
    /// <param name="letter">Letra de la unidad.</param>
    /// <param name="passes">Número de pasadas (se fuerza a un mínimo de 1).</param>
    /// <param name="progress">Progreso reportado: (porcentaje 0-100, bytes escritos, bytes totales previstos).</param>
    /// <param name="ct">Token de cancelación.</param>
    /// <returns><c>0</c> si se completó; <c>-1</c> si la unidad no estaba lista.</returns>
    /// <exception cref="OperationCanceledException">Si se cancela mediante <paramref name="ct"/>.</exception>
    public static async Task<int> RunAsync(
        char letter, int passes,
        IProgress<(int percent, long bytesDone, long totalBytes)> progress,
        CancellationToken ct)
    {
        if (!char.IsLetter(letter)) return -1;
        if (passes < 1) passes = 1;

        string dir = $"{letter}:\\__fdp_wipe__";
        long bytesDone = 0;

        try
        {
            var drive = new DriveInfo(letter.ToString());
            if (!drive.IsReady) return -1;

            long perPass = Math.Max(0, drive.AvailableFreeSpace - SafetyMarginBytes);
            long total   = perPass * passes;
            Directory.CreateDirectory(dir);

            var buffer = new byte[BlockSize];
            var rng    = new Random();

            for (int pass = 0; pass < passes; pass++)
            {
                var (random, fill) = PassPattern(pass, passes);
                if (!random) Array.Fill(buffer, fill);   // patrón fijo: rellenar una vez

                long passWritten = 0;
                int fileNo = 0;
                while (passWritten < perPass)
                {
                    long fileTarget = Math.Min(MaxFileSize, perPass - passWritten);
                    string path = Path.Combine(dir, $"wipe_{pass}_{fileNo:D4}.bin");

                    await using (var fs = new FileStream(path, FileMode.Create, FileAccess.Write,
                                     FileShare.None, 1 << 20, FileOptions.WriteThrough))
                    {
                        long fileWritten = 0;
                        while (fileWritten < fileTarget)
                        {
                            ct.ThrowIfCancellationRequested();
                            int size = (int)Math.Min(BlockSize, fileTarget - fileWritten);
                            if (random) rng.NextBytes(buffer.AsSpan(0, size));   // patrón aleatorio: regenerar
                            await fs.WriteAsync(buffer.AsMemory(0, size), ct);

                            fileWritten += size;
                            passWritten += size;
                            bytesDone   += size;
                            int pct = total > 0 ? (int)(bytesDone * 100 / total) : 100;
                            progress.Report((pct, bytesDone, total));
                        }
                    }
                    fileNo++;
                }
            }

            return 0;
        }
        finally
        {
            try { if (Directory.Exists(dir)) Directory.Delete(dir, recursive: true); } catch { }
        }
    }
}
