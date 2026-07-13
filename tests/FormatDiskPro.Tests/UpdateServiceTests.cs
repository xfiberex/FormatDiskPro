using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using Xunit;

namespace FormatDiskPro.Tests;

/// <summary>
/// Cubre la verificación con la que la auto-actualización decide si el instalador descargado es el que
/// publicó el proyecto, ANTES de ejecutarlo como administrador (<c>UpdateService.VerifyInstallerAsync</c>).
///
/// Mientras los instaladores se publiquen sin firmar —firmar está descartado (#13)— el hash es la única
/// verificación que hay: se compara, sin distinguir mayúsculas, contra el asset <c>*.exe.sha256</c> del
/// release, que genera <c>installer/build-installer.ps1</c> con <c>Get-FileHash -Algorithm SHA256</c>. Si
/// el formato de esa salida cambiara —minúsculas, guiones, Base64— la comparación fallaría siempre y la
/// app rechazaría su propio instalador, así que el formato se fija aquí.
/// </summary>
public sealed class UpdateServiceTests
{
    [Fact]
    public async Task ComputeSha256Async_MatchesKnownHash_AsUppercaseHexWithoutSeparators()
    {
        // SHA-256 de "abc" (vector de prueba estándar del NIST).
        const string expected = "BA7816BF8F01CFEA414140DE5DAE2223B00361A396177A9CB410FF61F20015AD";

        string path = Path.Combine(Path.GetTempPath(), $"fdp_sha_{Guid.NewGuid():N}.bin");
        try
        {
            await File.WriteAllTextAsync(path, "abc");

            // Mismo formato que produce Get-FileHash: hexadecimal en mayúsculas y sin guiones.
            Assert.Equal(expected, await UpdateService.ComputeSha256Async(path));
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public async Task ComputeSha256Async_DifferentContent_ProducesDifferentHash()
    {
        // El sentido entero de la verificación: un instalador manipulado, aunque sea en un byte, no pasa.
        string original = Path.Combine(Path.GetTempPath(), $"fdp_sha_{Guid.NewGuid():N}.bin");
        string tampered = Path.Combine(Path.GetTempPath(), $"fdp_sha_{Guid.NewGuid():N}.bin");
        try
        {
            await File.WriteAllBytesAsync(original, [1, 2, 3, 4]);
            await File.WriteAllBytesAsync(tampered, [1, 2, 3, 5]);

            Assert.NotEqual(
                await UpdateService.ComputeSha256Async(original),
                await UpdateService.ComputeSha256Async(tampered));
        }
        finally
        {
            File.Delete(original);
            File.Delete(tampered);
        }
    }

    /// <summary>Un archivo cualquiera no está firmado: la verificación NO puede darlo por bueno.</summary>
    [Fact]
    public async Task VerifyAuthenticodeSignature_UnsignedFile_IsRejected()
    {
        string path = Path.Combine(Path.GetTempPath(), $"fdp_unsigned_{Guid.NewGuid():N}.exe");
        try
        {
            await File.WriteAllBytesAsync(path, [1, 2, 3, 4]);
            Assert.False(UpdateService.VerifyAuthenticodeSignature(path));
        }
        finally
        {
            File.Delete(path);
        }
    }

    // Ruta propia por prueba: la de producción es fija y compartida, y escribir ahí podría borrar un
    // instalador real que el usuario tuviera a medio descargar.
    private static string ScratchInstallerPath() =>
        Path.Combine(Path.GetTempPath(), $"fdp_setup_{Guid.NewGuid():N}.exe");

    private static ReleaseInfo Release(LocalHttpServer server, bool withChecksum = true) =>
        new("v9.9.9", "9.9.9", "", "https://example.invalid",
            server.UrlFor("/setup.exe"), "FormatDiskPro-9.9.9-setup.exe", 0,
            withChecksum ? server.UrlFor("/setup.exe.sha256") : "");

    /// <summary>
    /// La descarga debe cerrar su FileStream (FileShare.None) ANTES de verificar. Si no, la verificación
    /// no puede ni abrir el archivo —"lo está usando otro proceso", siendo el proceso ella misma— y la
    /// auto-actualización falla SIEMPRE. Es la regresión que sufrió WingetUSoft (v1.4.1) con este código.
    /// </summary>
    [Fact]
    public async Task DownloadAsync_ClosesFileBeforeVerifying_SoTheChecksumCanBeRead()
    {
        // Más grande que el buffer de 1 MB de la descarga, para dar varias vueltas al bucle de lectura.
        byte[] installer = RandomNumberGenerator.GetBytes((1 << 20) + 4096);
        string hash = Convert.ToHexString(SHA256.HashData(installer));

        using var server = new LocalHttpServer(new Dictionary<string, byte[]>
        {
            ["/setup.exe"] = installer,
            // Mismo formato que escribe build-installer.ps1: "<hash> *<archivo>".
            ["/setup.exe.sha256"] = Encoding.UTF8.GetBytes($"{hash} *FormatDiskPro-9.9.9-setup.exe")
        });

        string destination = ScratchInstallerPath();
        try
        {
            string path = await UpdateService.DownloadAsync(
                Release(server), progress: null, CancellationToken.None, destination);

            Assert.Equal(destination, path);
            Assert.Equal(installer, await File.ReadAllBytesAsync(path));
        }
        finally
        {
            if (File.Exists(destination)) File.Delete(destination);
        }
    }

    [Fact]
    public async Task DownloadAsync_ChecksumMismatch_ThrowsAndDeletesTheInstaller()
    {
        byte[] installer = [1, 2, 3, 4];
        string wrongHash = Convert.ToHexString(SHA256.HashData([9, 9, 9, 9]));

        using var server = new LocalHttpServer(new Dictionary<string, byte[]>
        {
            ["/setup.exe"] = installer,
            ["/setup.exe.sha256"] = Encoding.UTF8.GetBytes(wrongHash)
        });

        string destination = ScratchInstallerPath();
        try
        {
            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                UpdateService.DownloadAsync(Release(server), null, CancellationToken.None, destination));

            // Un instalador que no se pudo verificar no puede quedarse en disco esperando a que alguien
            // lo ejecute como administrador.
            Assert.False(File.Exists(destination));
        }
        finally
        {
            if (File.Exists(destination)) File.Delete(destination);
        }
    }

    /// <summary>
    /// Sin firma y sin hash publicado no hay nada con lo que verificar: no se ejecuta. Es el caso de los
    /// releases anteriores a la v1.15.0, que no subían el asset .sha256.
    /// </summary>
    [Fact]
    public async Task DownloadAsync_NoChecksumAsset_ThrowsAndDeletesTheInstaller()
    {
        using var server = new LocalHttpServer(new Dictionary<string, byte[]>
        {
            ["/setup.exe"] = [1, 2, 3, 4]
        });

        string destination = ScratchInstallerPath();
        try
        {
            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                UpdateService.DownloadAsync(
                    Release(server, withChecksum: false), null, CancellationToken.None, destination));

            Assert.False(File.Exists(destination));
        }
        finally
        {
            if (File.Exists(destination)) File.Delete(destination);
        }
    }

    /// <summary>
    /// Servidor HTTP mínimo sobre <see cref="TcpListener"/> para servir el instalador y su hash desde
    /// localhost. No se usa <c>HttpListener</c> porque en Windows exige reservar la URL como administrador.
    /// </summary>
    private sealed class LocalHttpServer : IDisposable
    {
        private readonly TcpListener _listener;
        private readonly Dictionary<string, byte[]> _routes;
        private readonly CancellationTokenSource _cts = new();

        public LocalHttpServer(Dictionary<string, byte[]> routes)
        {
            _routes = routes;
            _listener = new TcpListener(IPAddress.Loopback, 0);
            _listener.Start();
            _ = Task.Run(AcceptLoopAsync);
        }

        public string UrlFor(string path) =>
            $"http://127.0.0.1:{((IPEndPoint)_listener.LocalEndpoint).Port}{path}";

        private async Task AcceptLoopAsync()
        {
            try
            {
                while (!_cts.IsCancellationRequested)
                {
                    using TcpClient client = await _listener.AcceptTcpClientAsync(_cts.Token);
                    await using NetworkStream stream = client.GetStream();

                    string path = ParsePath(await ReadRequestHeadAsync(stream, _cts.Token));
                    bool found = _routes.TryGetValue(path, out byte[]? body);
                    body ??= [];

                    string head =
                        $"HTTP/1.1 {(found ? "200 OK" : "404 Not Found")}\r\n" +
                        "Content-Type: application/octet-stream\r\n" +
                        $"Content-Length: {body.Length}\r\n" +
                        "Connection: close\r\n\r\n";

                    await stream.WriteAsync(Encoding.ASCII.GetBytes(head), _cts.Token);
                    await stream.WriteAsync(body, _cts.Token);
                    await stream.FlushAsync(_cts.Token);
                    client.Client.Shutdown(SocketShutdown.Send);   // cierre limpio: sin RST en el cliente
                }
            }
            catch (OperationCanceledException) { }
            catch (ObjectDisposedException) { }
            catch (SocketException) { }
        }

        // Los GET no traen cuerpo: la petición termina en la línea en blanco.
        private static async Task<string> ReadRequestHeadAsync(NetworkStream stream, CancellationToken ct)
        {
            var head = new StringBuilder();
            byte[] one = new byte[1];
            while (!head.ToString().EndsWith("\r\n\r\n", StringComparison.Ordinal))
            {
                if (await stream.ReadAsync(one, ct) == 0) break;
                head.Append((char)one[0]);
            }
            return head.ToString();
        }

        private static string ParsePath(string requestHead)
        {
            string[] parts = requestHead.Split('\n')[0].Split(' ');   // "GET /setup.exe HTTP/1.1"
            return parts.Length > 1 ? parts[1] : "/";
        }

        public void Dispose()
        {
            _cts.Cancel();
            _listener.Dispose();
            _cts.Dispose();
        }
    }
}
