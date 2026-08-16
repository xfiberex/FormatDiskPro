using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
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
    // ── SafeAssetFileName: la ruta de descarga no puede salirse de su carpeta ──────────────

    /// <summary>
    /// El instalador se descarga a <c>%TEMP%\FormatDiskPro_update</c> y luego se ejecuta ELEVADO, así que
    /// el nombre de archivo no puede arrastrar componentes de ruta. <c>Path.Combine</c> descarta su primer
    /// argumento ante una ruta absoluta, de modo que un nombre de asset manipulado escribiría donde
    /// quisiera con permisos de administrador.
    /// </summary>
    [Theory]
    [InlineData(@"C:\Windows\System32\evil.exe", "evil.exe")]        // ruta absoluta: Path.Combine la respetaría
    [InlineData(@"..\..\evil.exe",               "evil.exe")]        // traversal relativo
    [InlineData("../../evil.exe",                "evil.exe")]        // idem con separador POSIX
    [InlineData(@"sub\dir\evil.exe",             "evil.exe")]        // subcarpeta que no existe
    [InlineData("FormatDiskPro-1.2.3-setup.exe", "FormatDiskPro-1.2.3-setup.exe")]   // caso normal: intacto
    public void SafeAssetFileName_StripsPathComponents(string assetName, string expected)
        => Assert.Equal(expected, UpdateService.SafeAssetFileName(assetName, "1.2.3"));

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("..")]
    [InlineData(@"a\..")]      // GetFileName deja "..", que no es un nombre utilizable
    public void SafeAssetFileName_UnusableName_FallsBackToVersionedName(string? assetName)
        => Assert.Equal("FormatDiskPro-1.2.3-setup.exe", UpdateService.SafeAssetFileName(assetName, "1.2.3"));

    /// <summary>El resultado, combinado con la carpeta de descarga, nunca sale de ella.</summary>
    [Theory]
    [InlineData(@"C:\Windows\System32\evil.exe")]
    [InlineData(@"..\..\..\evil.exe")]
    public void SafeAssetFileName_CombinedPath_StaysInsideDownloadFolder(string assetName)
    {
        string dir = Path.Combine(Path.GetTempPath(), "FormatDiskPro_update");
        string combined = Path.GetFullPath(Path.Combine(dir, UpdateService.SafeAssetFileName(assetName, "1.2.3")));

        Assert.StartsWith(Path.GetFullPath(dir) + Path.DirectorySeparatorChar, combined, StringComparison.OrdinalIgnoreCase);
    }

    // ── ParseRelease: el checksum que se usa es el DEL instalador elegido ──────────────────

    private static ReleaseInfo Parse(string releaseJson)
    {
        using var doc = JsonDocument.Parse(releaseJson);
        return UpdateService.ParseRelease(doc.RootElement);
    }

    /// <summary>JSON de un release de GitHub con los assets indicados (solo los campos que se leen).</summary>
    private static string ReleaseJson(params string[] assetNames)
    {
        string assets = string.Join(",", assetNames.Select(n =>
            $$"""{"name":"{{n}}","browser_download_url":"https://example.invalid/{{n}}","size":123}"""));
        return $$"""
            {"tag_name":"v9.9.9","body":"notas","html_url":"https://example.invalid/r","assets":[{{assets}}]}
            """;
    }

    /// <summary>
    /// Con varios assets, el <c>.sha256</c> se empareja <b>por nombre</b> con el instalador elegido. Antes se
    /// guardaba el último que apareciera en el JSON, así que bastaba con que el release llevara otro archivo
    /// con checksum para acabar verificando el instalador contra el hash de otra cosa.
    /// </summary>
    [Fact]
    public void ParseRelease_MultipleChecksums_PairsTheOneOfTheChosenInstaller()
    {
        var info = Parse(ReleaseJson(
            "FormatDiskPro-9.9.9-portable.exe",
            "FormatDiskPro-9.9.9-portable.exe.sha256",
            "FormatDiskPro-9.9.9-setup.exe",
            "FormatDiskPro-9.9.9-setup.exe.sha256"));

        Assert.Equal("FormatDiskPro-9.9.9-setup.exe", info.AssetName);
        Assert.Equal("https://example.invalid/FormatDiskPro-9.9.9-setup.exe.sha256", info.ChecksumUrl);
    }

    /// <summary>El orden en el JSON no decide nada: el instalador manda, venga donde venga su hash.</summary>
    [Fact]
    public void ParseRelease_ChecksumBeforeItsInstaller_IsStillPaired()
    {
        var info = Parse(ReleaseJson(
            "FormatDiskPro-9.9.9-setup.exe.sha256",
            "FormatDiskPro-9.9.9-setup.exe",
            "otro.exe.sha256"));

        Assert.Equal("https://example.invalid/FormatDiskPro-9.9.9-setup.exe.sha256", info.ChecksumUrl);
    }

    /// <summary>
    /// Un checksum que NO es el del instalador elegido no vale como verificación: mejor rechazar la
    /// actualización por no verificable que compararla con el hash de otro archivo (que nunca coincidiría).
    /// </summary>
    [Fact]
    public void ParseRelease_ChecksumOfAnotherAsset_LeavesTheReleaseUnverifiable()
    {
        var info = Parse(ReleaseJson(
            "FormatDiskPro-9.9.9-setup.exe",
            "FormatDiskPro-9.9.9-portable.exe.sha256"));

        Assert.Equal("FormatDiskPro-9.9.9-setup.exe", info.AssetName);
        Assert.Equal("", info.ChecksumUrl);
    }

    /// <summary>El caso normal —lo que sube <c>release.ps1</c>: el instalador y su hash— sigue emparejando.</summary>
    [Fact]
    public void ParseRelease_InstallerAndItsChecksum_IsTheHappyPath()
    {
        var info = Parse(ReleaseJson("FormatDiskPro-9.9.9-setup.exe", "FormatDiskPro-9.9.9-setup.exe.sha256"));

        Assert.Equal("v9.9.9", info.TagName);
        Assert.Equal("https://example.invalid/FormatDiskPro-9.9.9-setup.exe", info.AssetUrl);
        Assert.Equal("https://example.invalid/FormatDiskPro-9.9.9-setup.exe.sha256", info.ChecksumUrl);
    }

    /// <summary>Los releases anteriores a la v1.15.0 no publican hash: quedan sin verificación posible.</summary>
    [Fact]
    public void ParseRelease_NoChecksumAsset_LeavesTheReleaseUnverifiable()
        => Assert.Equal("", Parse(ReleaseJson("FormatDiskPro-9.9.9-setup.exe")).ChecksumUrl);

    // ── Verificación por SHA-256 ──────────────────────────────────────────────────────────

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

    /// <summary>
    /// Tripwire. <c>VerifyAuthenticodeSignature</c> responde por la VALIDEZ de una firma, no por su
    /// AUTORÍA: le vale cualquier certificado en el que Windows confíe. Mientras el proyecto no firme
    /// (#13), dejar que una firma válida exima del SHA-256 convierte cualquier ejecutable firmado por
    /// cualquier CA en un modo de saltarse la única verificación que hay — y al otro lado está
    /// <c>LaunchInstaller</c> ejecutando como administrador.
    ///
    /// Si esta prueba falla es porque alguien puso el flag en <c>true</c>. Eso solo es correcto si además
    /// se fija el publicador esperado (comparar el sujeto del certificado con el del proyecto). Con lo
    /// primero sin lo segundo, se reabre el agujero.
    /// </summary>
    [Fact]
    public void SignsItsInstallers_StaysFalse_WhileTheProjectHasNoCertificate()
        => Assert.False(UpdateService.SignsItsInstallers,
            "Si el proyecto ya firma, fija también el publicador esperado antes de dar la firma por buena.");

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
            string path = await new UpdateService().DownloadAsync(
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
                new UpdateService().DownloadAsync(Release(server), null, CancellationToken.None, destination));

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
    /// El asset <c>.sha256</c> se lee con un tope de tamaño: la URL sale del JSON del release, así que no
    /// puede decidir cuánta memoria se materializa. Lo que hace discriminante a esta prueba es que el hash
    /// servido <b>es el correcto</b> —está al principio del cuerpo— y aun así se rechaza: lo que falla es el
    /// tamaño de la respuesta, no la comparación. Con el <c>GetStringAsync</c> anterior esto pasaba.
    /// </summary>
    [Fact]
    public async Task DownloadAsync_OversizedChecksumResponse_IsRejectedBeforeComparing()
    {
        byte[] installer = [1, 2, 3, 4];
        string hash = Convert.ToHexString(SHA256.HashData(installer));

        // Hash válido + relleno hasta muy por encima del tope de lectura.
        byte[] bloated = Encoding.UTF8.GetBytes(hash + new string(' ', 64 * 1024));

        using var server = new LocalHttpServer(new Dictionary<string, byte[]>
        {
            ["/setup.exe"] = installer,
            ["/setup.exe.sha256"] = bloated
        });

        string destination = ScratchInstallerPath();
        try
        {
            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                new UpdateService().DownloadAsync(Release(server), null, CancellationToken.None, destination));

            Assert.False(File.Exists(destination));
        }
        finally
        {
            if (File.Exists(destination)) File.Delete(destination);
        }
    }

    /// <summary>
    /// Un binario con firma Authenticode <b>embebida</b> y válida, garantizado presente porque estas
    /// pruebas corren sobre él: <c>dotnet.exe</c>. (Los binarios de Windows como <c>explorer.exe</c> no
    /// valen: su firma es de <i>catálogo</i>, y <c>WinVerifyTrust</c> con <c>WTD_CHOICE_FILE</c> no la ve.)
    /// </summary>
    private static string SignedBinaryPath()
    {
        // .../dotnet/shared/Microsoft.NETCore.App/10.0.x/  →  tres niveles arriba está la raíz de dotnet.
        var dir = new DirectoryInfo(System.Runtime.InteropServices.RuntimeEnvironment.GetRuntimeDirectory());
        string root = dir.Parent?.Parent?.Parent?.FullName
            ?? throw new InvalidOperationException($"Ruta de runtime inesperada: {dir.FullName}");

        string exe = Path.Combine(root, "dotnet.exe");
        Assert.True(File.Exists(exe), $"No se encontró {exe}, pero estas pruebas se están ejecutando con él.");
        return exe;
    }

    /// <summary>
    /// La comprobación de revocación (<c>WTD_REVOKE_WHOLECHAIN</c>) NO debe romper una firma legítima.
    /// Es el riesgo real del cambio: pedir revocación sin <c>WTD_CACHE_ONLY_URL_RETRIEVAL</c> haría que la
    /// validación dependiera de poder contactar con la CA, y una máquina sin red rechazaría firmas buenas.
    /// Esta prueba corre sobre un binario firmado de verdad, así que ejercita la ruta completa de
    /// <c>WinVerifyTrust</c>, no una simulación.
    /// </summary>
    [Fact]
    public void VerifyAuthenticodeSignature_GenuinelySignedFile_IsAccepted()
        => Assert.True(UpdateService.VerifyAuthenticodeSignature(SignedBinaryPath()),
            "Una firma Authenticode válida se está rechazando: revisa los flags de revocación.");

    /// <summary>
    /// El núcleo de la decisión: un instalador <b>firmado y válido</b> pero sin hash publicado se rechaza
    /// igual. Hasta la v1.16.0 la firma era un atajo que devolvía sin mirar el hash — y como el proyecto no
    /// firma (#13), ese atajo solo podía activarse sobre un binario que no produjimos nosotros.
    ///
    /// Nótese que el contenido servido es un ejecutable de Microsoft legítimamente firmado: exactamente el
    /// material con el que se construiría el ataque, y aun así no pasa.
    /// </summary>
    [Fact]
    public async Task DownloadAsync_SignedButNoChecksum_IsStillRejected()
    {
        byte[] signed = await File.ReadAllBytesAsync(SignedBinaryPath());

        using var server = new LocalHttpServer(new Dictionary<string, byte[]>
        {
            ["/setup.exe"] = signed
        });

        string destination = ScratchInstallerPath();
        try
        {
            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                new UpdateService().DownloadAsync(
                    Release(server, withChecksum: false), null, CancellationToken.None, destination));

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
                new UpdateService().DownloadAsync(
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
