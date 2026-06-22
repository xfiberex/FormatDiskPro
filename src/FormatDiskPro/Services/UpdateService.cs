using System.Diagnostics;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;

namespace FormatDiskPro;

/// <summary>
/// Información de una versión publicada en GitHub Releases.
/// </summary>
public sealed record ReleaseInfo(
    string TagName,
    string Version,
    string Notes,
    string HtmlUrl,
    string? AssetUrl,
    string? AssetName,
    long AssetSize);

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
        string? url = null, name = null;
        long size = 0;
        if (root.TryGetProperty("assets", out var assets) && assets.ValueKind == JsonValueKind.Array)
        {
            JsonElement? best = null;
            foreach (var a in assets.EnumerateArray())
            {
                string an = a.TryGetProperty("name", out var n) ? n.GetString() ?? "" : "";
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

        return new ReleaseInfo(tag, tag, notes, html, url, name, size);
    }

    /// <summary>Devuelve la última versión solo si es más reciente que la instalada; null en caso contrario.</summary>
    public static async Task<ReleaseInfo?> CheckForUpdateAsync(CancellationToken ct = default)
    {
        var latest = await GetLatestAsync(ct);
        if (latest is null) return null;
        return UpdateChecker.IsNewer(latest.TagName, AppInfo.Version) ? latest : null;
    }

    /// <summary>
    /// Descarga el instalador de una versión a la carpeta temporal e informa el progreso (0-100).
    /// Devuelve la ruta del archivo descargado.
    /// </summary>
    public static async Task<string> DownloadAsync(
        ReleaseInfo release, IProgress<int>? progress, CancellationToken ct)
    {
        if (string.IsNullOrEmpty(release.AssetUrl))
            throw new InvalidOperationException("La versión no incluye un instalador descargable.");

        string fileName = string.IsNullOrEmpty(release.AssetName)
            ? $"FormatDiskPro-{release.Version}-setup.exe"
            : release.AssetName!;
        string dir  = Path.Combine(Path.GetTempPath(), "FormatDiskPro_update");
        Directory.CreateDirectory(dir);

        // Limpia descargas previas para no acumular instaladores viejos en %Temp%.
        try { foreach (var old in Directory.GetFiles(dir)) File.Delete(old); }
        catch { /* archivo en uso u otro problema: no es crítico */ }

        string path = Path.Combine(dir, fileName);

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
        return path;
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
