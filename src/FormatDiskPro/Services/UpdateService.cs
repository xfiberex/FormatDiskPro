using System.Diagnostics;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace FormatDiskPro;

/// <summary>
/// Información de una versión publicada en GitHub Releases.
/// </summary>
/// <param name="ChecksumUrl">
/// URL del asset <c>*.sha256</c> con el que se verifica el instalador antes de ejecutarlo
/// (ver <c>UpdateService.VerifyInstallerAsync</c>). Vacía si el release no publica el checksum
/// <b>del instalador elegido</b>: los releases anteriores a la v1.15.0 no lo llevan, y un
/// <c>.sha256</c> que no le corresponda no cuenta (ver <c>UpdateService.ParseRelease</c>).
/// </param>
public sealed record ReleaseInfo(
    string TagName,
    string Version,
    string Notes,
    string HtmlUrl,
    string? AssetUrl,
    string? AssetName,
    long AssetSize,
    string ChecksumUrl = "");

/// <summary>Actualizaciones vía GitHub Releases: consulta, descarga verificada e instalación.</summary>
public interface IUpdateService
{
    /// <inheritdoc cref="UpdateService.GetLatestAsync"/>
    Task<ReleaseInfo?> GetLatestAsync(CancellationToken ct = default);

    /// <inheritdoc cref="UpdateService.GetReleaseByTagAsync"/>
    Task<ReleaseInfo?> GetReleaseByTagAsync(string tag, CancellationToken ct = default);

    /// <inheritdoc cref="UpdateService.CheckForUpdateAsync"/>
    Task<ReleaseInfo?> CheckForUpdateAsync(CancellationToken ct = default);

    /// <inheritdoc cref="UpdateService.DownloadAsync"/>
    Task<string> DownloadAsync(
        ReleaseInfo release, IProgress<int>? progress, CancellationToken ct, string? destinationPath = null);

    /// <inheritdoc cref="UpdateService.LaunchInstaller"/>
    void LaunchInstaller(string installerPath, bool silent = false);

    /// <inheritdoc cref="UpdateService.OpenUrl"/>
    bool OpenUrl(string url);
}

/// <summary>
/// Soporte de actualizaciones vía GitHub Releases: consulta la última versión,
/// descarga el instalador y lo ejecuta. La comparación de versiones vive en <see cref="UpdateChecker"/>.
/// </summary>
/// <remarks>
/// <b>Lo que aquí NO se instancia, y por qué.</b> `T4-02` convirtió los servicios en objetos inyectables
/// para poder probar sus caminos de error sin hardware. Este ya se probaba entero —sus pruebas levantan
/// un servidor HTTP local y ejercitan hash correcto, hash que no coincide, checksum ausente y respuesta
/// desmedida—, así que sus miembros internos (<c>ParseRelease</c>, <c>ComputeSha256Async</c>,
/// <c>VerifyInstallerAsync</c>…) siguen siendo <c>static</c>: tocarlos sería reescribir la ruta de
/// verificación que se ejecuta <b>elevada</b>, sin ganar una sola prueba. Lo que se instancia es la
/// superficie que consume la UI, para que dependa de <see cref="IUpdateService"/> y no de un tipo estático.
/// </remarks>
public sealed class UpdateService : IUpdateService
{
    private static readonly HttpClient Http = CreateClient();

    private static HttpClient CreateClient()
    {
        var c = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };
        // GitHub exige User-Agent en todas las peticiones a su API.
        c.DefaultRequestHeaders.UserAgent.Add(
            new ProductInfoHeaderValue(AppInfo.GitHubRepo, AppInfo.VersionString));
        c.DefaultRequestHeaders.Accept.Add(
            new MediaTypeWithQualityHeaderValue("application/vnd.github+json"));
        c.DefaultRequestHeaders.Add("X-GitHub-Api-Version", "2022-11-28");
        return c;
    }

    /// <summary>Obtiene la última versión publicada, o null si no se pudo determinar.</summary>
    public Task<ReleaseInfo?> GetLatestAsync(CancellationToken ct = default)
        => GetFromUrlAsync(AppInfo.LatestReleaseApiUrl, ct);

    /// <summary>
    /// Obtiene la versión publicada con el tag indicado (p. ej. <c>v1.7.0</c>), o null si no existe.
    /// Se usa para mostrar las novedades de la versión instalada tras una actualización.
    /// </summary>
    public Task<ReleaseInfo?> GetReleaseByTagAsync(string tag, CancellationToken ct = default)
        => string.IsNullOrWhiteSpace(tag)
            ? Task.FromResult<ReleaseInfo?>(null)
            : GetFromUrlAsync(AppInfo.ReleaseByTagApiUrl(tag), ct);

    private static async Task<ReleaseInfo?> GetFromUrlAsync(string url, CancellationToken ct)
    {
        using var resp = await Http.GetAsync(url, ct);
        if (!resp.IsSuccessStatusCode) return null;

        await using var stream = await resp.Content.ReadAsStreamAsync(ct);
        using var doc = await JsonDocument.ParseAsync(stream, cancellationToken: ct);
        return ParseRelease(doc.RootElement);
    }

    /// <summary>
    /// Convierte el JSON de un release de GitHub en un <see cref="ReleaseInfo"/>.
    ///
    /// <para><b>El checksum se empareja por nombre con el instalador elegido</b>, no se toma «el
    /// <c>.sha256</c> que haya». Antes se guardaba el último asset terminado en <c>.sha256</c> que
    /// apareciera en el JSON: con más de un asset —un instalador ARM64, un portable, un adjunto de un
    /// colaborador— el emparejamiento era arbitrario, y verificar un instalador contra el hash de otro
    /// archivo solo puede terminar de una forma: rechazando la actualización real. Se busca exactamente
    /// <c>&lt;nombre-del-exe&gt;.sha256</c>, que es lo que genera <c>build-installer.ps1</c> y sube
    /// <c>release.ps1</c>.</para>
    ///
    /// <para>Si no aparece, <c>ChecksumUrl</c> queda vacía y la actualización se rechaza por no
    /// verificable — que es el fallo seguro: nunca ejecutar sin comprobar.</para>
    /// </summary>
    internal static ReleaseInfo ParseRelease(JsonElement root)
    {
        string tag   = root.TryGetProperty("tag_name", out var t) ? t.GetString() ?? "" : "";
        string notes = root.TryGetProperty("body", out var b) ? b.GetString() ?? "" : "";
        string html  = root.TryGetProperty("html_url", out var h) ? h.GetString() ?? AppInfo.ReleasesPageUrl : AppInfo.ReleasesPageUrl;

        string? url = null, name = null;
        string checksumUrl = "";
        long size = 0;
        if (root.TryGetProperty("assets", out var assets) && assets.ValueKind == JsonValueKind.Array)
        {
            // Los checksums se recogen con su nombre para poder buscar después el del instalador elegido;
            // el instalador es el primer .exe que contenga "setup", o el primer .exe si ninguno lo hace.
            // (Una lista y no un diccionario de cadena a cadena: son tres assets, y esa forma la reserva
            // el proyecto para las tablas de texto localizable — ver LocalizationCoverageTests.)
            var checksums = new List<(string Name, string Url)>();
            JsonElement? best = null;
            bool bestIsSetup = false;

            foreach (var a in assets.EnumerateArray())
            {
                string an = a.TryGetProperty("name", out var n) ? n.GetString() ?? "" : "";
                if (an.EndsWith(".sha256", StringComparison.OrdinalIgnoreCase))
                {
                    string cu = a.TryGetProperty("browser_download_url", out var c) ? c.GetString() ?? "" : "";
                    if (cu.Length > 0) checksums.Add((an, cu));
                    continue;
                }
                if (!an.EndsWith(".exe", StringComparison.OrdinalIgnoreCase)) continue;

                bool isSetup = an.Contains("setup", StringComparison.OrdinalIgnoreCase);
                if (best is null || (isSetup && !bestIsSetup))
                {
                    best = a;
                    bestIsSetup = isSetup;
                }
            }

            if (best is { } asset)
            {
                name = asset.TryGetProperty("name", out var n) ? n.GetString() : null;
                url  = asset.TryGetProperty("browser_download_url", out var u) ? u.GetString() : null;
                size = asset.TryGetProperty("size", out var s) && s.TryGetInt64(out long sv) ? sv : 0;

                if (!string.IsNullOrEmpty(name))
                {
                    string expected = name + ".sha256";
                    checksumUrl = checksums
                        .FirstOrDefault(c => string.Equals(c.Name, expected, StringComparison.OrdinalIgnoreCase))
                        .Url ?? "";
                }
            }
        }

        return new ReleaseInfo(tag, tag, notes, html, url, name, size, checksumUrl);
    }

    /// <summary>Devuelve la última versión solo si es más reciente que la instalada; null en caso contrario.</summary>
    public async Task<ReleaseInfo?> CheckForUpdateAsync(CancellationToken ct = default)
    {
        var latest = await GetLatestAsync(ct);
        if (latest is null) return null;
        return UpdateChecker.IsNewer(latest.TagName, AppInfo.Version) ? latest : null;
    }

    /// <summary>
    /// Descarga el instalador de una versión a la carpeta temporal, informa el progreso (0-100) y lo
    /// <b>verifica</b> antes de devolverlo (ver <see cref="VerifyInstallerAsync"/>). Devuelve la ruta del
    /// archivo descargado; si no se puede verificar, lo borra y lanza.
    /// </summary>
    /// <param name="destinationPath">Solo para pruebas: si es null se usa la carpeta temporal habitual.</param>
    public async Task<string> DownloadAsync(
        ReleaseInfo release, IProgress<int>? progress, CancellationToken ct, string? destinationPath = null)
    {
        if (string.IsNullOrEmpty(release.AssetUrl))
            throw new InvalidOperationException("La versión no incluye un instalador descargable.");

        string path = destinationPath ?? PrepareDownloadPath(release);

        // La descarga va en su propio método a propósito: así su FileStream (abierto con FileShare.None)
        // queda cerrado ANTES de verificar. Con el handle todavía vivo, tanto la firma como el hash
        // fallarían al abrir el archivo con "lo está usando otro proceso" —el proceso somos nosotros— y
        // la actualización se rechazaría siempre a sí misma.
        await DownloadToFileAsync(release, path, progress, ct);

        try
        {
            await VerifyInstallerAsync(path, release.ChecksumUrl, ct);
        }
        catch
        {
            TryDeleteRejectedInstaller(path);
            throw;
        }

        return path;
    }

    private static string PrepareDownloadPath(ReleaseInfo release)
    {
        string dir = Path.Combine(Path.GetTempPath(), "FormatDiskPro_update");
        Directory.CreateDirectory(dir);

        // Limpia descargas previas para no acumular instaladores viejos en %Temp%.
        try { foreach (var old in Directory.GetFiles(dir)) File.Delete(old); }
        catch { /* archivo en uso u otro problema: no es crítico */ }

        return Path.Combine(dir, SafeAssetFileName(release.AssetName, release.Version));
    }

    /// <summary>
    /// Nombre de archivo seguro a partir del nombre de asset que publica GitHub, para componerlo con la
    /// carpeta de descarga.
    ///
    /// <para><b>Por qué no se usa tal cual:</b> <see cref="Path.Combine(string, string)"/> <b>descarta el
    /// primer argumento</b> si el segundo es una ruta absoluta (<c>Path.Combine(@"C:\a", @"C:\b")</c>
    /// devuelve <c>C:\b</c>), y tampoco resuelve <c>..</c>. Como la app corre elevada y el archivo se
    /// ejecuta después, un nombre manipulado podría escribir en cualquier sitio como administrador.</para>
    ///
    /// <para><b>Alcance honesto:</b> el nombre viene del JSON del propio repositorio, así que explotarlo
    /// exige controlar el release — y quien pueda hacer eso ya controla el <c>.exe</c> que se descarga.
    /// Esto no cierra un agujero abierto: evita que la seguridad del flujo dependa de que GitHub sanee
    /// los nombres de asset, que es una suposición que nadie verificó.</para>
    /// </summary>
    /// <param name="assetName">Nombre del asset tal como llega del API de GitHub.</param>
    /// <param name="version">Versión del release, para el nombre de reserva.</param>
    /// <returns>Un nombre de archivo sin componentes de ruta.</returns>
    internal static string SafeAssetFileName(string? assetName, string version)
    {
        string fallback = $"FormatDiskPro-{version}-setup.exe";
        if (string.IsNullOrWhiteSpace(assetName)) return fallback;

        // GetFileName se queda con lo que hay tras el último separador y descarta la raíz: convierte
        // "C:\Windows\System32\x.exe" y "..\..\x.exe" en "x.exe".
        string name;
        try { name = Path.GetFileName(assetName.Trim()); }
        catch (ArgumentException) { return fallback; }   // caracteres no válidos en la ruta

        // "..", "." o una cadena vacía no son nombres de archivo utilizables (p. ej. assetName = "a\..").
        if (name.Length == 0 || name == "." || name == "..") return fallback;
        if (name.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0) return fallback;

        return name;
    }

    private static async Task DownloadToFileAsync(
        ReleaseInfo release, string path, IProgress<int>? progress, CancellationToken ct)
    {
        // ResponseHeadersRead: el cuerpo se lee en streaming, fuera del Timeout de 30 s del HttpClient
        // (que solo cubre hasta las cabeceras). Es lo que hace viable un instalador de ~60 MB en
        // conexiones lentas; no cambiar a ResponseContentRead. La cancelación la lleva el token.
        using var resp = await Http.GetAsync(release.AssetUrl, HttpCompletionOption.ResponseHeadersRead, ct);
        resp.EnsureSuccessStatusCode();

        long total = resp.Content.Headers.ContentLength ?? release.AssetSize;
        await using var src = await resp.Content.ReadAsStreamAsync(ct);
        await using var dst = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.None, 1 << 20, useAsync: true);

        var buffer = new byte[1 << 20];
        long read = 0;
        int n, lastPct = -1;
        while ((n = await src.ReadAsync(buffer, ct)) > 0)
        {
            await dst.WriteAsync(buffer.AsMemory(0, n), ct);
            read += n;
            if (total > 0)
            {
                int pct = (int)(read * 100 / total);
                if (pct != lastPct) { lastPct = pct; progress?.Report(Math.Clamp(pct, 0, 100)); }
            }
        }
        progress?.Report(100);
    }

    /// <summary>
    /// Borra el instalador que no pasó la verificación. Si el borrado falla se ignora: el error que
    /// importa es el que lo rechazó, y el próximo intento sobrescribe el archivo (FileMode.Create).
    /// </summary>
    private static void TryDeleteRejectedInstaller(string path)
    {
        try { File.Delete(path); }
        catch (IOException) { }
        catch (UnauthorizedAccessException) { }
    }

    /// <summary>
    /// Comprueba que el instalador recién descargado es el que publicó el proyecto, <b>antes</b> de
    /// ejecutarlo con permisos de administrador (que es lo que hace <see cref="LaunchInstaller"/>).
    ///
    /// La prueba exigida es el <b>SHA-256 publicado como asset del release</b> (<c>*.exe.sha256</c>: lo
    /// genera <c>installer/build-installer.ps1</c> y lo sube <c>release.ps1</c>). Sin él no se ejecuta
    /// nada: el instalador se borra. Esto rechaza también los releases anteriores a la v1.15.0 (no
    /// publicaban el hash), lo cual es correcto: solo importa hacia adelante, porque nunca se ofrece
    /// actualizar a una versión más vieja que la instalada.
    ///
    /// <para><b>Por qué la firma Authenticode NO sirve hoy de atajo.</b> Hasta la v1.16.0 una firma válida
    /// hacía devolver sin mirar el hash. Pero <see cref="VerifyAuthenticodeSignature"/> responde a «¿lo
    /// firmó <i>alguien</i> en quien Windows confía?», no a «¿lo firmamos <i>nosotros</i>?» — no hay
    /// publicador que fijar, porque firmar está descartado (#13) y esa decisión se ha reafirmado. Mientras
    /// el proyecto no firme, esa rama <b>solo puede activarse sobre un binario que nosotros no
    /// produjimos</b>: convertía cualquier ejecutable firmado por cualquier CA de confianza en un modo de
    /// saltarse el hash, y lo que hay al otro lado es <see cref="LaunchInstaller"/> ejecutando con
    /// permisos de administrador. El atajo queda bajo <see cref="SignsItsInstallers"/>, en <c>false</c>:
    /// el día que haya certificado hay que poner el flag <b>y</b> fijar el publicador esperado, no solo lo
    /// primero.</para>
    ///
    /// Alcance honesto del hash: el instalador y su <c>.sha256</c> salen del mismo release, así que esto
    /// detecta corrupción y manipulación <b>en tránsito</b>, pero NO protege frente a un compromiso de la
    /// cuenta de GitHub (quien pudiera sustituir el .exe podría sustituir también el hash). Es el
    /// compromiso habitual de un proyecto sin certificado, y es exactamente la garantía que sustituye a
    /// la firma.
    /// </summary>
    private static async Task VerifyInstallerAsync(string filePath, string? checksumUrl, CancellationToken ct)
    {
        if (SignsItsInstallers)
        {
            if (VerifyAuthenticodeSignature(filePath))
                return;
        }

        if (string.IsNullOrWhiteSpace(checksumUrl))
            throw new InvalidOperationException(L.T("update.unverifiable"));

        string published = await DownloadChecksumTextAsync(checksumUrl, ct);

        // Admite tanto "<hash>" a secas como el formato de sha256sum: "<hash> *FormatDiskPro-X.Y.Z-setup.exe".
        string expected = published.Trim().Split((char[])[' ', '\t', '\r', '\n'], 2)[0];
        string actual = await ComputeSha256Async(filePath, ct);

        if (!string.Equals(expected, actual, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException(L.T("update.checksumMismatch"));
    }

    /// <summary>
    /// Tamaño máximo que se acepta de un asset <c>.sha256</c>. Su contenido real son 64 caracteres de hash
    /// más, como mucho, el nombre del archivo: <c>build-installer.ps1</c> escribe
    /// <c>"&lt;hash&gt; *FormatDiskPro-X.Y.Z-setup.exe"</c>, unos 110 bytes. 512 deja margen de sobra para
    /// un nombre más largo o un salto de línea, y sigue estando lejos de cualquier cosa que merezca
    /// materializarse en memoria.
    /// </summary>
    private const int MaxChecksumBytes = 512;

    /// <summary>
    /// Descarga el texto del asset <c>.sha256</c> con un <b>tope de tamaño</b>.
    ///
    /// <para>Antes era un <c>GetStringAsync</c>, que lee la respuesta entera en memoria sin límite: la URL
    /// sale del JSON del release, así que basta que apunte a otra cosa —por error o por manipulación— para
    /// que la app se trague un archivo arbitrario buscando en él un hash de 64 caracteres. No es un agujero
    /// de ejecución (lo que se hace con el texto es comparar), pero no hay ninguna razón para leer más de
    /// lo que un checksum puede ocupar.</para>
    ///
    /// <para>Se comprueban las dos cosas: la longitud declarada en la cabecera, cuando viene, y lo que
    /// realmente llega — un servidor puede mentir en <c>Content-Length</c> o no enviarlo.</para>
    /// </summary>
    private static async Task<string> DownloadChecksumTextAsync(string checksumUrl, CancellationToken ct)
    {
        using var resp = await Http.GetAsync(checksumUrl, HttpCompletionOption.ResponseHeadersRead, ct);
        resp.EnsureSuccessStatusCode();

        if (resp.Content.Headers.ContentLength is > MaxChecksumBytes)
            throw new InvalidOperationException(L.T("update.checksumUnreadable"));

        await using var stream = await resp.Content.ReadAsStreamAsync(ct);

        // Un byte más que el tope: si el hueco extra se llena, la respuesta se pasó de largo.
        var buffer = new byte[MaxChecksumBytes + 1];
        int read = 0, n;
        while (read < buffer.Length &&
               (n = await stream.ReadAsync(buffer.AsMemory(read, buffer.Length - read), ct)) > 0)
        {
            read += n;
        }

        if (read > MaxChecksumBytes)
            throw new InvalidOperationException(L.T("update.checksumUnreadable"));

        return Encoding.UTF8.GetString(buffer, 0, read);
    }

    /// <summary>SHA-256 del archivo, en hexadecimal y mayúsculas (el mismo formato que <c>Get-FileHash</c>).</summary>
    internal static async Task<string> ComputeSha256Async(string filePath, CancellationToken ct = default)
    {
        await using FileStream stream = File.OpenRead(filePath);
        byte[] hash = await SHA256.HashDataAsync(stream, ct);
        return Convert.ToHexString(hash);
    }

    /// <summary>
    /// ¿Firma el proyecto sus propios instaladores? <b>No</b> (decisión #13, reafirmada el 2026-08-13).
    /// Mientras sea <c>false</c>, una firma Authenticode válida no exime de comprobar el SHA-256 — ver el
    /// porqué en <see cref="VerifyInstallerAsync"/>. Ponerlo en <c>true</c> sin fijar además el publicador
    /// esperado reabriría exactamente el agujero que este flag cierra.
    /// </summary>
    /// <remarks>
    /// <c>static readonly</c> y no <c>const</c> a propósito: como constante, el compilador pliega el
    /// <c>if</c> y marca la rama con CS0162 (código inaccesible), y este proyecto compila a 0 advertencias.
    /// </remarks>
    internal static readonly bool SignsItsInstallers = false;

    /// <summary>
    /// ¿El archivo lleva una firma Authenticode válida y de confianza para Windows? Ojo: responde por la
    /// <b>validez</b> de la firma, no por su <b>autoría</b> — cualquier certificado de confianza vale.
    /// Devuelve false si no está firmado, si la firma está caducada, si su cadena no es de confianza o si
    /// el certificado ha sido <b>revocado</b>.
    /// </summary>
    internal static bool VerifyAuthenticodeSignature(string filePath)
    {
        var fileInfo = new NativeMethods.WINTRUST_FILE_INFO
        {
            cbStruct = (uint)Marshal.SizeOf<NativeMethods.WINTRUST_FILE_INFO>(),
            pcwszFilePath = filePath,
            hFile = IntPtr.Zero,
            pgKnownSubject = IntPtr.Zero
        };

        nint fileInfoPtr = Marshal.AllocHGlobal(Marshal.SizeOf<NativeMethods.WINTRUST_FILE_INFO>());
        try
        {
            Marshal.StructureToPtr(fileInfo, fileInfoPtr, false);

            var trustData = new NativeMethods.WINTRUST_DATA
            {
                cbStruct = (uint)Marshal.SizeOf<NativeMethods.WINTRUST_DATA>(),
                pPolicyCallbackData = IntPtr.Zero,
                pSIPClientData = IntPtr.Zero,
                dwUIChoice = NativeMethods.WTD_UI_NONE,
                // WHOLECHAIN: una firma cuyo certificado haya sido revocado deja de contar como válida.
                // CACHE_ONLY_URL_RETRIEVAL (en dwProvFlags) evita que esto dependa de la red: usa las CRL
                // ya cacheadas por Windows en vez de bloquearse contactando con la CA.
                fdwRevocationChecks = NativeMethods.WTD_REVOKE_WHOLECHAIN,
                dwUnionChoice = NativeMethods.WTD_CHOICE_FILE,
                pUnion = fileInfoPtr,
                dwStateAction = NativeMethods.WTD_STATEACTION_IGNORE,
                hWVTStateData = IntPtr.Zero,
                pwszURLReference = null,
                dwProvFlags = NativeMethods.WTD_SAFER_FLAG | NativeMethods.WTD_CACHE_ONLY_URL_RETRIEVAL,
                dwUIContext = 0
            };

            nint trustDataPtr = Marshal.AllocHGlobal(Marshal.SizeOf<NativeMethods.WINTRUST_DATA>());
            try
            {
                Marshal.StructureToPtr(trustData, trustDataPtr, false);
                var actionId = new Guid("00AAC56B-CD44-11D0-8CC2-00C04FC295EE");   // WINTRUST_ACTION_GENERIC_VERIFY_V2
                return NativeMethods.WinVerifyTrust(IntPtr.Zero, ref actionId, trustDataPtr) == 0;
            }
            finally
            {
                Marshal.FreeHGlobal(trustDataPtr);
            }
        }
        finally
        {
            Marshal.FreeHGlobal(fileInfoPtr);
        }
    }

    private static class NativeMethods
    {
        internal const uint WTD_UI_NONE = 2;
        internal const uint WTD_REVOKE_WHOLECHAIN = 1;
        internal const uint WTD_CHOICE_FILE = 1;
        internal const uint WTD_STATEACTION_IGNORE = 0;
        internal const uint WTD_SAFER_FLAG = 0x100;
        internal const uint WTD_CACHE_ONLY_URL_RETRIEVAL = 0x1000;

        [DllImport("wintrust.dll", ExactSpelling = true, SetLastError = false, CharSet = CharSet.Unicode)]
        internal static extern uint WinVerifyTrust(IntPtr hwnd, ref Guid pgActionID, IntPtr pWVTData);

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        internal struct WINTRUST_FILE_INFO
        {
            public uint cbStruct;
            [MarshalAs(UnmanagedType.LPWStr)] public string? pcwszFilePath;
            public IntPtr hFile;
            public IntPtr pgKnownSubject;
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        internal struct WINTRUST_DATA
        {
            public uint cbStruct;
            public IntPtr pPolicyCallbackData;
            public IntPtr pSIPClientData;
            public uint dwUIChoice;
            public uint fdwRevocationChecks;
            public uint dwUnionChoice;
            public IntPtr pUnion;
            public uint dwStateAction;
            public IntPtr hWVTStateData;
            [MarshalAs(UnmanagedType.LPWStr)] public string? pwszURLReference;
            public uint dwProvFlags;
            public uint dwUIContext;
        }
    }

    /// <summary>
    /// Ejecuta el instalador descargado (pedirá elevación UAC).
    /// </summary>
    /// <param name="installerPath">Ruta del instalador (.exe) descargado.</param>
    /// <param name="silent">
    /// Si es <see langword="true"/>, lo ejecuta en modo silencioso (`/VERYSILENT`) y le indica
    /// que relance la app al terminar (`/AUTOUPDATE=1`), para una actualización sin interrupción.
    /// </param>
    public void LaunchInstaller(string installerPath, bool silent = false)
    {
        var psi = new ProcessStartInfo(installerPath) { UseShellExecute = true };
        if (silent) psi.Arguments = "/VERYSILENT /NORESTART /AUTOUPDATE=1";
        Process.Start(psi);
    }

    /// <summary>
    /// Abre una URL en el navegador predeterminado.
    /// </summary>
    /// <param name="url">Dirección a abrir.</param>
    /// <returns><see langword="true"/> si el shell aceptó abrirla.</returns>
    /// <remarks>
    /// Sigue sin lanzar —un enlace roto no puede tumbar un diálogo—, pero ahora <b>contesta</b>: antes
    /// devolvía <c>void</c> y se tragaba el fallo, así que pulsar «Apoyar el proyecto» sin navegador
    /// asociado no hacía nada en absoluto y el usuario se quedaba pulsando. Quien llama decide cómo
    /// contarlo; aquí solo se sabe si salió o no.
    /// </remarks>
    public bool OpenUrl(string url)
    {
        try
        {
            Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
            return true;
        }
        catch { return false; }
    }
}
