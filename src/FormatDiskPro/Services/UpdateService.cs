using System.Diagnostics;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text.Json;

namespace FormatDiskPro;

/// <summary>
/// Información de una versión publicada en GitHub Releases.
/// </summary>
/// <param name="ChecksumUrl">
/// URL del asset <c>*.sha256</c> con el que se verifica el instalador antes de ejecutarlo
/// (ver <c>UpdateService.VerifyInstallerAsync</c>). Vacía si el release no lo publica: los releases
/// anteriores a la v1.15.0 no lo llevan.
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

/// <summary>
/// Soporte de actualizaciones vía GitHub Releases: consulta la última versión,
/// descarga el instalador y lo ejecuta. La comparación de versiones vive en <see cref="UpdateChecker"/>.
/// </summary>
public static class UpdateService
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
    public static Task<ReleaseInfo?> GetLatestAsync(CancellationToken ct = default)
        => GetFromUrlAsync(AppInfo.LatestReleaseApiUrl, ct);

    /// <summary>
    /// Obtiene la versión publicada con el tag indicado (p. ej. <c>v1.7.0</c>), o null si no existe.
    /// Se usa para mostrar las novedades de la versión instalada tras una actualización.
    /// </summary>
    public static Task<ReleaseInfo?> GetReleaseByTagAsync(string tag, CancellationToken ct = default)
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

    /// <summary>Convierte el JSON de un release de GitHub en un <see cref="ReleaseInfo"/>.</summary>
    private static ReleaseInfo ParseRelease(JsonElement root)
    {
        string tag   = root.TryGetProperty("tag_name", out var t) ? t.GetString() ?? "" : "";
        string notes = root.TryGetProperty("body", out var b) ? b.GetString() ?? "" : "";
        string html  = root.TryGetProperty("html_url", out var h) ? h.GetString() ?? AppInfo.ReleasesPageUrl : AppInfo.ReleasesPageUrl;

        // Elegir el asset instalador: preferir el .exe que contenga "setup".
        // El checksum (*.sha256) no compite con él: no termina en .exe, así que el bucle lo ignora.
        string? url = null, name = null;
        string checksumUrl = "";
        long size = 0;
        if (root.TryGetProperty("assets", out var assets) && assets.ValueKind == JsonValueKind.Array)
        {
            JsonElement? best = null;
            foreach (var a in assets.EnumerateArray())
            {
                string an = a.TryGetProperty("name", out var n) ? n.GetString() ?? "" : "";
                if (an.EndsWith(".sha256", StringComparison.OrdinalIgnoreCase))
                {
                    checksumUrl = a.TryGetProperty("browser_download_url", out var cu) ? cu.GetString() ?? "" : "";
                    continue;
                }
                if (!an.EndsWith(".exe", StringComparison.OrdinalIgnoreCase)) continue;
                if (best is null || an.Contains("setup", StringComparison.OrdinalIgnoreCase))
                    best = a;
            }
            if (best is { } asset)
            {
                name = asset.TryGetProperty("name", out var n) ? n.GetString() : null;
                url  = asset.TryGetProperty("browser_download_url", out var u) ? u.GetString() : null;
                size = asset.TryGetProperty("size", out var s) && s.TryGetInt64(out long sv) ? sv : 0;
            }
        }

        return new ReleaseInfo(tag, tag, notes, html, url, name, size, checksumUrl);
    }

    /// <summary>Devuelve la última versión solo si es más reciente que la instalada; null en caso contrario.</summary>
    public static async Task<ReleaseInfo?> CheckForUpdateAsync(CancellationToken ct = default)
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
    public static async Task<string> DownloadAsync(
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
        string fileName = string.IsNullOrEmpty(release.AssetName)
            ? $"FormatDiskPro-{release.Version}-setup.exe"
            : release.AssetName!;
        string dir = Path.Combine(Path.GetTempPath(), "FormatDiskPro_update");
        Directory.CreateDirectory(dir);

        // Limpia descargas previas para no acumular instaladores viejos en %Temp%.
        try { foreach (var old in Directory.GetFiles(dir)) File.Delete(old); }
        catch { /* archivo en uso u otro problema: no es crítico */ }

        return Path.Combine(dir, fileName);
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
    /// Se aceptan dos pruebas, en este orden:
    ///
    /// 1. <b>Firma Authenticode válida</b>: la más fuerte, porque la avala una CA en la que confía Windows.
    ///    Es la vía preferente y la única que haría falta el día que el proyecto tenga certificado.
    /// 2. <b>SHA-256 publicado como asset del release</b> (<c>*.exe.sha256</c>: lo genera
    ///    <c>installer/build-installer.ps1</c> y lo sube <c>release.ps1</c>). Hoy los instaladores se
    ///    publican SIN firmar —firmar está descartado (#13)—, así que sin este segundo camino la
    ///    auto-actualización estaría muerta: rechazaría siempre su propio instalador.
    ///
    /// Alcance honesto del hash: el instalador y su <c>.sha256</c> salen del mismo release, así que esto
    /// detecta corrupción y manipulación <b>en tránsito</b>, pero NO protege frente a un compromiso de la
    /// cuenta de GitHub (quien pudiera sustituir el .exe podría sustituir también el hash). Es el
    /// compromiso habitual de un proyecto sin certificado, y es exactamente la garantía que sustituye a
    /// la firma.
    ///
    /// Sin firma válida y sin <c>.sha256</c> no se ejecuta nada: el instalador se borra. Esto rechaza
    /// también los releases anteriores a la v1.15.0 (no publicaban el hash), lo cual es correcto: solo
    /// importa hacia adelante, porque nunca se ofrece actualizar a una versión más vieja que la instalada.
    /// </summary>
    private static async Task VerifyInstallerAsync(string filePath, string? checksumUrl, CancellationToken ct)
    {
        if (VerifyAuthenticodeSignature(filePath))
            return;

        if (string.IsNullOrWhiteSpace(checksumUrl))
            throw new InvalidOperationException(L.T("update.unverifiable"));

        string published = await Http.GetStringAsync(checksumUrl, ct);

        // Admite tanto "<hash>" a secas como el formato de sha256sum: "<hash> *FormatDiskPro-X.Y.Z-setup.exe".
        string expected = published.Trim().Split((char[])[' ', '\t', '\r', '\n'], 2)[0];
        string actual = await ComputeSha256Async(filePath, ct);

        if (!string.Equals(expected, actual, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException(L.T("update.checksumMismatch"));
    }

    /// <summary>SHA-256 del archivo, en hexadecimal y mayúsculas (el mismo formato que <c>Get-FileHash</c>).</summary>
    internal static async Task<string> ComputeSha256Async(string filePath, CancellationToken ct = default)
    {
        await using FileStream stream = File.OpenRead(filePath);
        byte[] hash = await SHA256.HashDataAsync(stream, ct);
        return Convert.ToHexString(hash);
    }

    /// <summary>
    /// ¿El archivo lleva una firma Authenticode válida y de confianza para Windows?
    /// Devuelve false si no está firmado, si la firma está caducada o si su cadena no es de confianza.
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
                fdwRevocationChecks = NativeMethods.WTD_REVOKE_NONE,
                dwUnionChoice = NativeMethods.WTD_CHOICE_FILE,
                pUnion = fileInfoPtr,
                dwStateAction = NativeMethods.WTD_STATEACTION_IGNORE,
                hWVTStateData = IntPtr.Zero,
                pwszURLReference = null,
                dwProvFlags = NativeMethods.WTD_SAFER_FLAG,
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
        internal const uint WTD_REVOKE_NONE = 0;
        internal const uint WTD_CHOICE_FILE = 1;
        internal const uint WTD_STATEACTION_IGNORE = 0;
        internal const uint WTD_SAFER_FLAG = 0x100;

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
    public static void LaunchInstaller(string installerPath, bool silent = false)
    {
        var psi = new ProcessStartInfo(installerPath) { UseShellExecute = true };
        if (silent) psi.Arguments = "/VERYSILENT /NORESTART /AUTOUPDATE=1";
        Process.Start(psi);
    }

    /// <summary>Abre una URL en el navegador predeterminado.</summary>
    public static void OpenUrl(string url)
    {
        try { Process.Start(new ProcessStartInfo(url) { UseShellExecute = true }); } catch { }
    }
}
