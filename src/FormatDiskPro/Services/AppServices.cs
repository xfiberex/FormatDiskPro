namespace FormatDiskPro;

/// <summary>
/// Raíz de composición: el único sitio donde se decide <b>qué implementación</b> de cada servicio usa
/// la app. Todo lo demás recibe sus dependencias por constructor y no sabe construirlas.
/// </summary>
/// <remarks>
/// <para><b>Por qué existe</b> (`T4-02`). Hasta la v1.20.0 los servicios eran clases <c>static</c>: la
/// UI los invocaba por nombre y no había forma de sustituirlos, así que probar qué hace la app cuando
/// una operación <b>falla</b> exigía provocar el fallo real —una USB desconectada a mitad, un
/// <c>chkdsk</c> bloqueado por política, un disco falsificado—. Esa es la raíz que la auditoría anotó en
/// <c>T2-05</c> y la que esta clase cierra: con las dependencias inyectadas, un doble de prueba
/// reproduce el fallo en milisegundos y sin tocar un disco.</para>
///
/// <para><b>Un contenedor a mano y no un framework de DI.</b> Son doce servicios, sin ciclos de vida ni
/// ámbitos: un <c>ServiceCollection</c> añadiría una dependencia y una indirección para resolver algo
/// que aquí cabe en una pantalla. Si algún día hay ámbitos o resolución dinámica, este es el archivo
/// que se sustituye.</para>
///
/// <para><b>No es un localizador de servicios.</b> Nadie pide servicios a esta clase «desde dentro»:
/// <see cref="App"/> construye la instancia y la pasa a <c>MainWindow</c>, que a su vez la pasa a los
/// diálogos que la necesitan. Las dependencias siguen siendo visibles en cada constructor, que es la
/// mitad del valor de haberlas inyectado.</para>
/// </remarks>
public sealed class AppServices
{
    /// <summary>Construye el grafo real de la app.</summary>
    /// <param name="runner">
    /// Lanzador de procesos compartido por los servicios que invocan a PowerShell, <c>chkdsk.exe</c> o
    /// <c>format.com</c>. Es el punto único que un doble de prueba sustituye para simular sus fallos.
    /// </param>
    public AppServices(IProcessRunner? runner = null)
    {
        ProcessRunner = runner ?? new SystemProcessRunner();

        Disk      = new DiskService(ProcessRunner);
        CheckDisk = new CheckDisk(ProcessRunner);
        Reinit    = new ReinitDrive(ProcessRunner);
        Format    = new FormatProcess(ProcessRunner);
        Verifier  = new CapacityVerifier();
        Benchmark = new BenchmarkRunner();
        Wipe      = new SecureWipe();
        History   = new History();
        Notifier  = new Notifier();
        Taskbar   = new TaskbarProgress();
        Updates   = new UpdateService();
    }

    // Deliberadamente NO hay un constructor "para pruebas" que acepte los once servicios sueltos: su
    // único consumidor posible sería MainWindow, y una ventana WinUI no se instancia en las unitarias.
    // Las pruebas construyen el servicio que les interesa (`new CheckDisk(runnerFalso)`) y se saltan
    // este grafo. Un constructor sin llamantes que promete servir para algo es peor que no tenerlo.

    public IProcessRunner     ProcessRunner { get; }
    public IDiskService       Disk      { get; }
    public ICheckDisk         CheckDisk { get; }
    public IReinitDrive       Reinit    { get; }
    public IFormatProcess     Format    { get; }
    public ICapacityVerifier  Verifier  { get; }
    public IBenchmarkRunner   Benchmark { get; }
    public ISecureWipe        Wipe      { get; }
    public IHistory           History   { get; }
    public INotifier          Notifier  { get; }
    public ITaskbarProgress   Taskbar   { get; }
    public IUpdateService     Updates   { get; }
}
