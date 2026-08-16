namespace FormatDiskPro.Tests;

/// <summary>
/// Doble de <see cref="IProcessRunner"/>: devuelve una salida y un código de salida preparados, o
/// revienta al arrancar, <b>sin lanzar ningún proceso</b>.
///
/// <para>Es lo que hace posible probar los caminos de error de los servicios sin el hardware ni la avería
/// de verdad (`T4-02`, raíz de <c>T2-05</c>): un <c>chkdsk</c> que devuelve 2, un <c>Clear-Disk</c> que
/// falla a mitad, un <c>powershell.exe</c> que no arranca porque lo bloquea una política. Antes, cada uno
/// de esos casos exigía provocarlo de verdad — y algunos son destructivos.</para>
/// </summary>
internal sealed class FakeProcessRunner(Func<ProcessSpec, IProcessHandle> factory) : IProcessRunner
{
    /// <summary>Los procesos que se pidió arrancar, en orden. Permite afirmar qué se invocó y con qué.</summary>
    public List<ProcessSpec> Started { get; } = [];

    /// <summary>Todos los handles entregados, para comprobar si se los mató al cancelar.</summary>
    public List<FakeProcessHandle> Handles { get; } = [];

    public IProcessHandle Start(ProcessSpec spec)
    {
        Started.Add(spec);
        var handle = factory(spec);
        if (handle is FakeProcessHandle f) Handles.Add(f);
        return handle;
    }

    /// <summary>Un proceso que escribe <paramref name="stdout"/> y termina con <paramref name="exitCode"/>.</summary>
    /// <param name="chunkSize">
    /// Caracteres por lectura. Por omisión la salida llega entera de una vez; un valor pequeño simula que
    /// el proceso la va escribiendo poco a poco, que es lo que ocurre de verdad y lo que obliga a los
    /// servicios a rastrear marcadores <b>partidos entre dos lecturas</b>.
    /// </param>
    public static FakeProcessRunner Returning(
        string stdout, int exitCode = 0, string stderr = "", int chunkSize = int.MaxValue)
        => new(_ => new FakeProcessHandle(stdout, stderr, exitCode, chunkSize));

    /// <summary>
    /// Un proceso que <b>ni siquiera arranca</b>: es lo que ocurre cuando el ejecutable no está o una
    /// política lo bloquea, y <see cref="System.Diagnostics.Process.Start()"/> lanza <c>Win32Exception</c>.
    /// </summary>
    public static FakeProcessRunner Throwing(Exception ex) => new(_ => throw ex);

    /// <summary>Un runner que falla la prueba si alguien intenta lanzar algo con él.</summary>
    public static FakeProcessRunner Forbidden()
        => new(spec => throw new InvalidOperationException(
            $"No debía lanzarse ningún proceso, y se intentó lanzar '{spec.FileName}'."));
}

/// <summary>Proceso simulado: salida fija, código de salida fijo y registro de si se lo mató.</summary>
internal sealed class FakeProcessHandle(string stdout, string stderr, int exitCode, int chunkSize = int.MaxValue)
    : IProcessHandle
{
    private readonly ChunkedReader _out = new(stdout, chunkSize);
    private readonly StringReader _err = new(stderr);

    public TextReader StandardOutput => _out;
    public TextReader StandardError  => _err;
    public TextWriter StandardInput  => TextWriter.Null;
    public int ExitCode => exitCode;

    /// <summary><c>true</c> si se pidió matar el proceso (cancelación).</summary>
    public bool WasKilled { get; private set; }

    /// <summary><c>true</c> si quien lo arrancó lo liberó.</summary>
    public bool WasDisposed { get; private set; }

    public Task WaitForExitAsync(CancellationToken ct = default) => Task.CompletedTask;
    public void Kill(bool entireProcessTree) => WasKilled = true;
    public void Dispose() => WasDisposed = true;

    /// <summary>
    /// Lector que entrega como mucho <c>chunkSize</c> caracteres por lectura. Un
    /// <see cref="StringReader"/> devuelve la cadena entera de una vez, así que con él los servicios
    /// verían la salida completa en la primera pasada y <b>nunca</b> ejercitarían el rastreo de un
    /// marcador partido entre dos lecturas — que es el caso real y el que costó un fallo O(n²).
    /// </summary>
    private sealed class ChunkedReader(string text, int chunkSize) : TextReader
    {
        private int _pos;

        public override int Read(char[] buffer, int index, int count)
        {
            int n = Math.Min(Math.Min(count, chunkSize), text.Length - _pos);
            if (n <= 0) return 0;
            text.CopyTo(_pos, buffer, index, n);
            _pos += n;
            return n;
        }

        public override int Peek() => _pos < text.Length ? text[_pos] : -1;
    }
}
