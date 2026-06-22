using System.Reflection;

namespace FormatDiskPro;

/// <summary>
/// Metadatos de la aplicación: versión en ejecución y coordenadas del repositorio
/// usado para las actualizaciones (GitHub Releases).
/// </summary>
public static class AppInfo
{
    public const string GitHubOwner = "xfiberex";
    public const string GitHubRepo  = "FormatDiskPro";

    /// <summary>URL de la API de la última versión publicada (no borrador / no prelanzamiento).</summary>
    public static string LatestReleaseApiUrl =>
        $"https://api.github.com/repos/{GitHubOwner}/{GitHubRepo}/releases/latest";

    /// <summary>URL de la API de la versión publicada con un tag concreto (p. ej. <c>v1.7.0</c>).</summary>
    public static string ReleaseByTagApiUrl(string tag) =>
        $"https://api.github.com/repos/{GitHubOwner}/{GitHubRepo}/releases/tags/{Uri.EscapeDataString(tag)}";

    /// <summary>Página de versiones del proyecto.</summary>
    public static string ReleasesPageUrl =>
        $"https://github.com/{GitHubOwner}/{GitHubRepo}/releases";

    /// <summary>Versión del ensamblado en ejecución.</summary>
    public static Version Version =>
        Assembly.GetExecutingAssembly().GetName().Version ?? new Version(0, 0, 0);

    /// <summary>Versión legible (Major.Minor.Build), p. ej. "1.1.0".</summary>
    public static string VersionString
    {
        get
        {
            var v = Version;
            return $"{v.Major}.{v.Minor}.{v.Build}";
        }
    }
}
