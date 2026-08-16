using System.ComponentModel;
using System.Text;
using Xunit;

namespace FormatDiskPro.Tests;

/// <summary>
/// Lo que hacen los servicios cuando el proceso que lanzan <b>falla</b>: devuelve un código de error,
/// no imprime lo que se espera, o ni siquiera arranca.
///
/// <para><b>Por qué estas pruebas no existían.</b> Los servicios eran clases <c>static</c> que hacían
/// <c>new Process(...)</c> ellas mismas, así que reproducir cualquiera de esos fallos exigía provocarlo
/// de verdad: desconectar la USB a mitad, bloquear <c>chkdsk.exe</c> por política, o —en el caso de
/// <see cref="ReinitDrive"/>— <b>borrar un disco entero</b> y que la operación fallase después. Eso es lo
/// que la auditoría anotó como raíz de <c>T2-05</c> y lo que resuelve la inyección de
/// <see cref="IProcessRunner"/> (`T4-02`): aquí no se lanza ni un proceso.</para>
/// </summary>
public sealed class ServiceErrorPathTests
{
    /// <summary>Devuelve el script PowerShell que se envió por <c>-EncodedCommand</c>.</summary>
    private static string DecodeScript(ProcessSpec spec)
    {
        const string marker = "-EncodedCommand ";
        string args = spec.Arguments ?? "";
        int i = args.IndexOf(marker, StringComparison.Ordinal);
        Assert.True(i >= 0, "El comando debe ir codificado, nunca concatenado en la línea de órdenes.");
        return Encoding.Unicode.GetString(Convert.FromBase64String(args[(i + marker.Length)..]));
    }

    // ── DiskService ───────────────────────────────────────────────

    /// <summary>
    /// La guarda que impide reinicializar el disco de Windows compara números de disco. Si la consulta
    /// no devuelve nada utilizable, tiene que quedarse en <c>null</c>: la UI lo trata como "no se puede
    /// determinar" y <b>bloquea</b> la operación. Devolver un número inventado sería catastrófico.
    /// </summary>
    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("Get-Partition : No se encontró ninguna partición")]
    [InlineData("2, 3")]
    public async Task GetDiskNumber_UnparseableOutput_IsNullSoTheSystemDiskGuardBlocks(string output)
    {
        var disk = new DiskService(FakeProcessRunner.Returning(output));

        Assert.Null(await disk.GetDiskNumberAsync('G'));
    }

    [Fact]
    public async Task GetDiskNumber_PowerShellDoesNotStart_IsNullInsteadOfThrowing()
    {
        var disk = new DiskService(FakeProcessRunner.Throwing(new Win32Exception(2, "No se encuentra el archivo")));

        Assert.Null(await disk.GetDiskNumberAsync('G'));
    }

    /// <summary>
    /// La protección de escritura es ternaria a propósito: sí, no y <b>no se sabe</b>. Una salida que no
    /// es ni <c>True</c> ni <c>False</c> tiene que caer en el tercero — la UI solo ofrece quitarla cuando
    /// la respuesta es un <c>true</c> explícito.
    /// </summary>
    [Theory]
    [InlineData("Verdadero")]
    [InlineData("1")]
    [InlineData("")]
    public async Task IsDiskReadOnly_UnexpectedOutput_IsNullAndNeverTrue(string output)
    {
        var disk = new DiskService(FakeProcessRunner.Returning(output));

        Assert.Null(await disk.IsDiskReadOnlyAsync('G'));
    }

    [Theory]
    [InlineData("True",  true)]
    [InlineData("false", false)]
    [InlineData(" TRUE\r\n", true)]
    public async Task IsDiskReadOnly_ReadsTheBooleanRegardlessOfCasingOrPadding(string output, bool expected)
    {
        var disk = new DiskService(FakeProcessRunner.Returning(output));

        Assert.Equal(expected, await disk.IsDiskReadOnlyAsync('G'));
    }

    /// <summary>
    /// El tamaño del disco es el tope de la partición FAT32 pequeña. Si la consulta no devuelve algo
    /// utilizable tiene que quedarse en <c>null</c>: la UI cae entonces al tamaño del volumen, que siempre
    /// es menor o igual. Un número inventado —o uno leído con la coma decimal del idioma del sistema—
    /// ofrecería tamaños que no caben y <c>New-Partition</c> fallaría con el disco ya borrado.
    /// </summary>
    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("Get-Disk : No se encontró el disco")]
    [InlineData("0")]                        // un disco de 0 bytes no es un tamaño, es un fallo
    [InlineData("-1")]
    [InlineData("15.500.000.000")]           // separador de miles: no es un long
    [InlineData("1,5E+10")]                  // notación científica con coma decimal
    public async Task GetDiskSize_UnparseableOutput_IsNullSoTheCeilingStaysConservative(string output)
    {
        var disk = new DiskService(FakeProcessRunner.Returning(output));

        Assert.Null(await disk.GetDiskSizeAsync('G'));
    }

    [Fact]
    public async Task GetDiskSize_ReadsTheByteCount()
    {
        var disk = new DiskService(FakeProcessRunner.Returning("15500000000\r\n"));

        Assert.Equal(15_500_000_000L, await disk.GetDiskSizeAsync('G'));
    }

    [Fact]
    public async Task GetDiskSize_PowerShellDoesNotStart_IsNullInsteadOfThrowing()
    {
        var disk = new DiskService(FakeProcessRunner.Throwing(new Win32Exception(2, "No se encuentra el archivo")));

        Assert.Null(await disk.GetDiskSizeAsync('G'));
    }

    [Fact]
    public async Task GetDiskSize_NonLetter_NeverLaunchesAProcess()
    {
        var disk = new DiskService(FakeProcessRunner.Forbidden());

        Assert.Null(await disk.GetDiskSizeAsync('1'));
    }

    [Fact]
    public async Task ClearReadOnly_NonZeroExitCode_ReportsFailure()
    {
        var disk = new DiskService(FakeProcessRunner.Returning("", exitCode: 1));

        Assert.False(await disk.ClearReadOnlyAsync('G'));
    }

    [Fact]
    public async Task Eject_ProcessThatDoesNotStart_ReportsFailureInsteadOfThrowing()
    {
        var disk = new DiskService(FakeProcessRunner.Throwing(new Win32Exception(5, "Acceso denegado")));

        Assert.False(await disk.EjectAsync('G'));
    }

    /// <summary>Sin el separador esperado no hay salud que leer: nulo, no una tarjeta con basura.</summary>
    [Theory]
    [InlineData("")]
    [InlineData("Get-PhysicalDisk : el disco no responde")]
    public async Task GetHealth_OutputWithoutTheExpectedShape_IsNull(string output)
    {
        var disk = new DiskService(FakeProcessRunner.Returning(output));

        Assert.Null(await disk.GetHealthAsync('G'));
    }

    /// <summary>Una letra que no es letra no llega a componer un comando: no se lanza nada.</summary>
    [Fact]
    public async Task DiskService_NonLetter_NeverLaunchesAProcess()
    {
        var runner = FakeProcessRunner.Forbidden();
        var disk = new DiskService(runner);

        Assert.Null(await disk.GetSmartAsync('1'));
        Assert.Null(await disk.GetDiskNumberAsync('\\'));
        Assert.Null(await disk.IsDiskReadOnlyAsync(' '));
        Assert.False(await disk.ClearReadOnlyAsync('7'));
        Assert.False(await disk.EjectAsync('-'));
        Assert.Empty(runner.Started);
    }

    // ── CheckDisk ─────────────────────────────────────────────────

    /// <summary>
    /// <c>CheckDisk.RunAsync</c> <b>no atrapa nada</b>, y eso es deliberado: la excepción tiene que llegar
    /// al <c>catch</c> del handler para que el usuario vea el error (`T0-02`). Esta prueba fija ese
    /// contrato — si algún día se añadiera aquí un <c>catch</c> silencioso, el fallo se volvería invisible.
    /// </summary>
    [Fact]
    public async Task CheckDisk_ChkdskBlockedByPolicy_PropagatesSoTheUiCanReportIt()
    {
        var check = new CheckDisk(FakeProcessRunner.Throwing(new Win32Exception(1260, "Bloqueado por directiva")));

        await Assert.ThrowsAsync<Win32Exception>(() => check.RunAsync(
            'G', repair: false, new Progress<int>(), CancellationToken.None));
    }

    [Fact]
    public async Task CheckDisk_ExitCodeTwo_IsReportedAsErrorsAndNotAsClean()
    {
        var check = new CheckDisk(FakeProcessRunner.Returning("Windows encontró problemas", exitCode: 2));

        var (code, output) = await check.RunAsync('G', repair: false, new Progress<int>(), CancellationToken.None);

        Assert.Equal(2, code);
        Assert.Contains("problemas", output, StringComparison.Ordinal);
        Assert.Equal(CheckResult.Errors, CheckDisk.Interpret(code, repair: false));
    }

    /// <summary>El error de chkdsk va por la salida de error y también tiene que llegar al usuario.</summary>
    [Fact]
    public async Task CheckDisk_StderrIsAppendedToTheOutput()
    {
        var check = new CheckDisk(FakeProcessRunner.Returning("", exitCode: 3, stderr: "No se puede bloquear la unidad"));

        var (_, output) = await check.RunAsync('G', repair: false, new Progress<int>(), CancellationToken.None);

        Assert.Contains("No se puede bloquear la unidad", output, StringComparison.Ordinal);
    }

    /// <summary>
    /// <c>/f</c> es lo que separa comprobar de <b>modificar</b> el sistema de archivos. Que solo aparezca
    /// al pedir reparación se comprueba ahora sobre los argumentos reales, no leyendo el código.
    /// </summary>
    [Theory]
    [InlineData(false, false)]
    [InlineData(true,  true)]
    public async Task CheckDisk_RepairFlagIsPassedOnlyWhenRepairing(bool repair, bool expectsF)
    {
        var runner = FakeProcessRunner.Returning("", exitCode: 0);
        var check = new CheckDisk(runner);

        await check.RunAsync('g', repair, new Progress<int>(), CancellationToken.None);

        var spec = Assert.Single(runner.Started);
        Assert.Equal("chkdsk.exe", spec.FileName);
        Assert.Equal("G:", spec.ArgumentList[0]);   // siempre en mayúscula invariante
        Assert.Equal(expectsF, spec.ArgumentList.Contains("/f"));
    }

    [Fact]
    public async Task CheckDisk_Cancellation_KillsTheProcessTree()
    {
        var runner = FakeProcessRunner.Returning("", exitCode: 0);
        var check = new CheckDisk(runner);
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        await check.RunAsync('G', repair: false, new Progress<int>(), cts.Token);

        Assert.True(Assert.Single(runner.Handles).WasKilled);
    }

    // ── ReinitDrive (el destructivo) ──────────────────────────────

    /// <summary>Disco de referencia para los planes de estas pruebas: 64 GiB, holgado para cualquiera.</summary>
    private const long Disk64Gb = 64L * 1024 * 1024 * 1024;

    /// <summary>
    /// El fallo que <b>solo</b> se podía ver con el disco ya borrado: <c>Clear-Disk</c> o
    /// <c>Format-Volume</c> revientan a mitad. Tiene que salir como fallo con el motivo real, no como
    /// éxito ni como excepción.
    /// </summary>
    [Fact]
    public async Task Reinit_PowerShellFails_ReportsFailureWithTheRealReason()
    {
        var runner = FakeProcessRunner.Returning(
            "STAGE:clean\nSTAGE:init\n", exitCode: 1, stderr: "Clear-Disk : Access is denied.");
        var reinit = new ReinitDrive(runner);

        var r = await reinit.RunAsync('G', PartitionPlan.WholeDisk(DiskPartitionStyle.Gpt, "NTFS", ""),
            Disk64Gb, new Progress<string>(), CancellationToken.None);

        Assert.False(r.Ok);
        Assert.Null(r.NewLetter);
        Assert.Contains("Access is denied", r.Detail, StringComparison.Ordinal);
    }

    /// <summary>
    /// Salida limpia y código 0, pero <b>sin letra asignada</b>: el disco quedó borrado y sin volumen
    /// montable. Es un fallo, y darlo por bueno dejaría al usuario buscando una unidad que no existe.
    /// </summary>
    [Fact]
    public async Task Reinit_ExitZeroButNoLetterAssigned_IsAFailure()
    {
        var reinit = new ReinitDrive(FakeProcessRunner.Returning(
            "STAGE:clean\nSTAGE:init\nSTAGE:partition\nSTAGE:format\nLETTER:\n", exitCode: 0));

        var r = await reinit.RunAsync('G', PartitionPlan.WholeDisk(DiskPartitionStyle.Gpt, "NTFS", ""),
            Disk64Gb, new Progress<string>(), CancellationToken.None);

        Assert.False(r.Ok);
        Assert.Null(r.NewLetter);
        Assert.Empty(r.Letters);
    }

    /// <summary>
    /// Las cuatro etapas llegan a la UI, una vez cada una y en orden — con la salida entregada en trozos
    /// de 6 caracteres, que parten <c>STAGE:partition</c> por la mitad. Es justo el caso para el que
    /// existe el solapamiento de <c>T3-02</c>: sin él, una etapa partida entre dos lecturas se perdería
    /// y el usuario vería el estado congelado mientras el disco se está borrando.
    /// </summary>
    [Fact]
    public async Task Reinit_Success_ReportsEveryStageOnceAndInOrder()
    {
        var stages = new List<string>();
        var reinit = new ReinitDrive(FakeProcessRunner.Returning(
            "STAGE:clean\nSTAGE:init\nSTAGE:partition\nSTAGE:format\nLETTER:H\n",
            exitCode: 0, chunkSize: 6));

        var r = await reinit.RunAsync('G', PartitionPlan.WholeDisk(DiskPartitionStyle.Gpt, "exFAT", "DATOS"),
            Disk64Gb, new SyncProgress<string>(stages.Add), CancellationToken.None);

        Assert.True(r.Ok);
        Assert.Equal('H', r.NewLetter);
        Assert.Equal(["clean", "init", "partition", "format"], stages);
    }

    /// <summary>Un proceso que ni arranca no puede escapar como excepción: sale como fallo con detalle.</summary>
    [Fact]
    public async Task Reinit_PowerShellDoesNotStart_IsAFailureAndNotAnException()
    {
        var reinit = new ReinitDrive(FakeProcessRunner.Throwing(new Win32Exception(2, "No se encuentra powershell.exe")));

        var r = await reinit.RunAsync('G', PartitionPlan.WholeDisk(DiskPartitionStyle.Mbr, "NTFS", ""),
            Disk64Gb, new Progress<string>(), CancellationToken.None);

        Assert.False(r.Ok);
        Assert.Contains("powershell.exe", r.Detail, StringComparison.Ordinal);
    }

    /// <summary>
    /// Las guardas de entrada se resuelven antes de lanzar nada: no se toca el disco.
    ///
    /// <para>Desde <c>T5-01</c> el grueso de esas guardas vive en <see cref="PartitionPlan.Validate"/> y se
    /// prueba sin servicio de por medio; aquí lo que se comprueba es que <see cref="ReinitDrive"/>
    /// <b>revalide</b> en vez de fiarse de quien llama. Es la última línea antes de <c>Clear-Disk</c>.</para>
    /// </summary>
    [Theory]
    [InlineData('1',  "NTFS",   null)]    // letra que no es letra
    [InlineData('G',  "NT;FS",  null)]    // sistema de archivos inventado
    [InlineData('G',  "NTFS",   0L)]      // tamaño cero
    [InlineData('G',  "NTFS",   -1L)]     // tamaño negativo
    [InlineData('G',  "FAT32",  Disk64Gb)]  // FAT32 de 64 GB: Windows no lo formatearía
    public async Task Reinit_InvalidRequest_IsRejectedBeforeLaunchingAnything(
        char letter, string fs, long? sizeBytes)
    {
        var runner = FakeProcessRunner.Forbidden();
        var reinit = new ReinitDrive(runner);

        PartitionPlan plan = sizeBytes is long bytes
            ? new PartitionPlan(DiskPartitionStyle.Gpt, [new PartitionSpec(new PartitionSize.Exact(bytes), fs, "")])
            : PartitionPlan.WholeDisk(DiskPartitionStyle.Gpt, fs, "");

        var r = await reinit.RunAsync(letter, plan, Disk64Gb, new Progress<string>(), CancellationToken.None);

        Assert.False(r.Ok);
        Assert.Empty(runner.Started);
    }

    /// <summary>
    /// El tamaño de la partición FAT32 pequeña tiene que llegar al comando: si se perdiera, se crearía
    /// una partición de todo el disco — exactamente lo contrario de lo que se pidió, y ya borrado.
    /// </summary>
    [Fact]
    public async Task Reinit_SmallFat32_SendsTheExactPartitionSizeAndNotUseMaximumSize()
    {
        var runner = FakeProcessRunner.Returning("LETTER:H", exitCode: 0);
        var reinit = new ReinitDrive(runner);

        var plan = new PartitionPlan(DiskPartitionStyle.Mbr,
            [new PartitionSpec(new PartitionSize.Exact(2_147_483_648L), "FAT32", "BIOS")]);

        await reinit.RunAsync('G', plan, Disk64Gb, new Progress<string>(), CancellationToken.None);

        string script = DecodeScript(Assert.Single(runner.Started));
        Assert.Contains("-Size 2147483648", script, StringComparison.Ordinal);
        Assert.DoesNotContain("-UseMaximumSize", script, StringComparison.Ordinal);
    }

    /// <summary>La etiqueta va dentro de un literal de PowerShell: la comilla simple se duplica.</summary>
    [Fact]
    public async Task Reinit_LabelWithSingleQuote_IsEscapedInTheScript()
    {
        var runner = FakeProcessRunner.Returning("LETTER:H", exitCode: 0);
        var reinit = new ReinitDrive(runner);

        await reinit.RunAsync('G', PartitionPlan.WholeDisk(DiskPartitionStyle.Gpt, "NTFS", "D'ANGELO"),
            Disk64Gb, new Progress<string>(), CancellationToken.None);

        Assert.Contains("'D''ANGELO'", DecodeScript(Assert.Single(runner.Started)), StringComparison.Ordinal);
    }

    /// <summary>
    /// Un plan de dos particiones produce las dos: tamaño exacto la primera, <c>-UseMaximumSize</c> la
    /// segunda, cada una con su sistema de archivos y su etiqueta. Es el motor de <c>T5-02</c>, ejercitado
    /// aquí <b>sin tocar ningún disco</b> — que es exactamente para lo que el plan se separó del ejecutor.
    /// </summary>
    [Fact]
    public async Task Reinit_TwoPartitionPlan_CreatesBothAndDelegatesTheRemainderToWindows()
    {
        var runner = FakeProcessRunner.Returning("LETTER:0:H\nLETTER:1:I\n", exitCode: 0);
        var reinit = new ReinitDrive(runner);

        var plan = new PartitionPlan(DiskPartitionStyle.Mbr, [
            new PartitionSpec(new PartitionSize.Exact(1024L * 1024 * 1024), "FAT32", "BIOS"),
            new PartitionSpec(new PartitionSize.Remainder(), "exFAT", "DATOS"),
        ]);

        var r = await reinit.RunAsync('G', plan, Disk64Gb, new Progress<string>(), CancellationToken.None);

        string script = DecodeScript(Assert.Single(runner.Started));
        Assert.Contains("-Size 1073741824", script, StringComparison.Ordinal);
        Assert.Contains("-UseMaximumSize", script, StringComparison.Ordinal);
        Assert.Contains("-FileSystem FAT32 -NewFileSystemLabel 'BIOS'", script, StringComparison.Ordinal);
        Assert.Contains("-FileSystem exFAT -NewFileSystemLabel 'DATOS'", script, StringComparison.Ordinal);

        Assert.True(r.Ok);
        Assert.Equal(['H', 'I'], r.Letters);
        Assert.Equal('H', r.NewLetter);   // la primera partición es la que la UI selecciona
    }

    /// <summary>
    /// La primera partición se crea y la segunda no: código 0 y una sola letra. Darlo por bueno dejaría al
    /// usuario creyendo que tiene dos volúmenes cuando solo tiene uno, con el disco ya borrado.
    /// </summary>
    [Fact]
    public async Task Reinit_TwoPartitionPlan_OnlyOneLetterComesBack_IsAFailure()
    {
        var reinit = new ReinitDrive(FakeProcessRunner.Returning("PART:0:1\nLETTER:0:H\n", exitCode: 0));

        var r = await reinit.RunAsync('G', TwoPartitionPlan(), Disk64Gb, new Progress<string>(), CancellationToken.None);

        Assert.False(r.Ok);
        Assert.Equal(['H'], r.Letters);   // se conserva lo que SÍ se creó: lo necesita `T5-03`
    }

    private static PartitionPlan TwoPartitionPlan() => new(DiskPartitionStyle.Mbr, [
        new PartitionSpec(new PartitionSize.Exact(1024L * 1024 * 1024), "FAT32", "BIOS"),
        new PartitionSpec(new PartitionSize.Remainder(), "exFAT", "DATOS"),
    ]);

    // ── Fallo a mitad del plan (`T5-03`) ──────────────────────────

    /// <summary>
    /// El estado intermedio real: la partición 1 creada y formateada, la 2 <b>creada pero sin formatear</b>.
    /// Son dos cifras distintas y las dos hacen falta — decir «no se creó ninguna» sería falso, y decir
    /// «se crearon dos» ocultaría que una no se puede usar.
    /// </summary>
    [Fact]
    public async Task Reinit_SecondPartitionFailsToFormat_ReportsCreatedAndUsableSeparately()
    {
        var reinit = new ReinitDrive(FakeProcessRunner.Returning(
            "STAGE:partition\nPART:0:1\nSTAGE:format\nLETTER:0:H\nPART:1:2\n",
            exitCode: 1, stderr: "Format-Volume : El parámetro no es correcto."));

        var r = await reinit.RunAsync('G', TwoPartitionPlan(), Disk64Gb, new Progress<string>(), CancellationToken.None);

        Assert.False(r.Ok);
        Assert.Equal(2, r.PartitionsCreated);   // las dos existen en la tabla de particiones
        Assert.Equal(['H'], r.Letters);         // pero solo una es utilizable
        Assert.Contains("no es correcto", r.Detail, StringComparison.Ordinal);
    }

    /// <summary>
    /// Falla <c>Clear-Disk</c> antes de crear nada: cero y cero. Con los marcadores agrupados al final del
    /// script —como estaban antes de `T5-03`— esta salida era indistinguible de la anterior, porque en
    /// ambas se emitían cero letras.
    /// </summary>
    [Fact]
    public async Task Reinit_FailsBeforeCreatingAnything_ReportsNothingCreated()
    {
        var reinit = new ReinitDrive(FakeProcessRunner.Returning(
            "STAGE:clean\n", exitCode: 1, stderr: "Clear-Disk : Access is denied."));

        var r = await reinit.RunAsync('G', TwoPartitionPlan(), Disk64Gb, new Progress<string>(), CancellationToken.None);

        Assert.False(r.Ok);
        Assert.Equal(0, r.PartitionsCreated);
        Assert.Empty(r.Letters);
    }

    /// <summary>
    /// <b>No se revierte nada al fallar.</b> El disco ya está borrado, así que «deshacer» solo podría
    /// significar borrarlo otra vez — una decisión que el usuario no pidió. Se comprueba de la única forma
    /// que no admite interpretación: <b>no se lanza un segundo proceso</b>.
    /// </summary>
    [Fact]
    public async Task Reinit_WhenItFailsHalfway_NeverLaunchesACleanupProcess()
    {
        var runner = FakeProcessRunner.Returning(
            "PART:0:1\nLETTER:0:H\nPART:1:2\n", exitCode: 1, stderr: "Format-Volume : fallo");
        var reinit = new ReinitDrive(runner);

        var r = await reinit.RunAsync('G', TwoPartitionPlan(), Disk64Gb, new Progress<string>(), CancellationToken.None);

        Assert.False(r.Ok);
        Assert.Single(runner.Started);   // uno solo: el de la operación. Ninguna limpieza automática.
    }

    /// <summary>
    /// Los marcadores se emiten <b>según se alcanzan</b>, no agrupados al final. Es lo que hace observable
    /// el fallo parcial: con los ecos al final, un fallo en la segunda partición abortaba el script antes
    /// de imprimir el de la primera.
    /// </summary>
    [Fact]
    public async Task Reinit_EmitsEachPartitionMarkerBeforeMovingOnToTheNext()
    {
        var runner = FakeProcessRunner.Returning("PART:0:1\nLETTER:0:H\nPART:1:2\nLETTER:1:I\n", exitCode: 0);
        var reinit = new ReinitDrive(runner);

        await reinit.RunAsync('G', TwoPartitionPlan(), Disk64Gb, new Progress<string>(), CancellationToken.None);

        string script = DecodeScript(Assert.Single(runner.Started));
        int firstLetter  = script.IndexOf("'LETTER:0:", StringComparison.Ordinal);
        int secondCreate = script.IndexOf("$p1 = New-Partition", StringComparison.Ordinal);

        Assert.True(firstLetter >= 0 && secondCreate >= 0, "El script no tiene la forma esperada.");
        Assert.True(firstLetter < secondCreate,
            "La letra de la primera partición debe emitirse ANTES de crear la segunda; si no, un fallo en " +
            "la segunda se lleva por delante el informe de la primera.");
    }

    // ── FormatProcess ─────────────────────────────────────────────

    /// <summary>
    /// El progreso del formato completo se saca de lo que imprime <c>format.com</c>. Aquí se comprueba
    /// sobre la salida, sin formatear nada — el porcentaje llega y el proceso se entrega a quien llama
    /// (que es quien tiene que poder cancelarlo).
    ///
    /// <para>La salida se entrega <b>en trozos de 7 caracteres</b>, que parten cada "N percent" por la
    /// mitad: así lo que se prueba es el rastreo con solapamiento, no una lectura única e irreal.</para>
    /// </summary>
    [Fact]
    public async Task FormatCom_ReportsProgressAndHandsTheProcessToTheCaller()
    {
        var percents = new List<int>();
        IProcessHandle? handed = null;
        var format = new FormatProcess(FakeProcessRunner.Returning(
            "10 percent completed.\n55 percent completed.\n100 percent completed.\n",
            exitCode: 0, chunkSize: 7));

        var (code, _) = await format.RunComAsync(
            'G', "NTFS", 4096, "", new SyncProgress<int>(percents.Add), p => handed = p, CancellationToken.None);

        Assert.Equal(0, code);
        Assert.NotNull(handed);
        Assert.Equal([10, 55, 100], percents);
    }

    /// <summary>
    /// <c>/Y</c> es lo que impide que el formato completo se quede colgado esperando una tecla en un
    /// Windows que no responde ni a <c>Y</c> ni a <c>S</c> (`T1-02`). Se comprueba en los argumentos que
    /// de verdad se pasan al proceso.
    /// </summary>
    [Fact]
    public async Task FormatCom_AlwaysPassesTheUnattendedFlag()
    {
        var runner = FakeProcessRunner.Returning("", exitCode: 0);
        var format = new FormatProcess(runner);

        await format.RunComAsync('G', "NTFS", 4096, "", null, null, CancellationToken.None);

        Assert.Contains("/Y", Assert.Single(runner.Started).ArgumentList);
    }

    /// <summary>
    /// Un formato con <c>Format-Volume</c> que falla informa por la salida de <b>error</b>, y ese es el
    /// texto que la ventana muestra: se devuelve por separado justamente para poder preferirlo.
    /// </summary>
    [Fact]
    public async Task FormatVolume_FailureKeepsStdoutAndStderrSeparate()
    {
        var format = new FormatProcess(FakeProcessRunner.Returning(
            "", exitCode: 1, stderr: "Format-Volume : El acceso al dispositivo está denegado."));

        var (code, stdout, stderr) = await format.RunVolumeAsync(
            'G', "NTFS", 4096, "", quickFormat: true, compress: false, null, CancellationToken.None);

        Assert.Equal(1, code);
        Assert.Empty(stdout);
        Assert.Contains("acceso al dispositivo", stderr, StringComparison.Ordinal);
    }
}
