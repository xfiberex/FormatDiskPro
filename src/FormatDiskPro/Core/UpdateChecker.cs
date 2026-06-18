namespace FormatDiskPro;

/// <summary>
/// Lógica pura de comparación de versiones para actualizaciones. Sin red ni estado:
/// directamente testeable. La parte de E/S (GitHub, descarga) vive en <c>UpdateService</c>.
/// </summary>
public static class UpdateChecker
{
    /// <summary>
    /// Convierte una etiqueta de versión ("v1.2.3", "1.2.3", "1.2") en <see cref="Version"/>.
    /// Tolera el prefijo "v"/"V" y los sufijos de prelanzamiento ("-beta", "+meta").
    /// </summary>
    public static bool TryParseTag(string? tag, out Version version)
    {
        version = new Version(0, 0, 0);
        if (string.IsNullOrWhiteSpace(tag)) return false;

        string s = tag.Trim();
        if (s.Length > 0 && (s[0] == 'v' || s[0] == 'V')) s = s[1..];

        // descartar sufijos de prelanzamiento / metadatos de build
        int cut = s.IndexOfAny(['-', '+', ' ']);
        if (cut >= 0) s = s[..cut];

        if (s.Length == 0) return false;

        // Version requiere al menos Major.Minor; normalizamos "1" → "1.0"
        if (!s.Contains('.')) s += ".0";

        return Version.TryParse(s, out var parsed) && (version = Normalize(parsed)) is not null;
    }

    /// <summary>True si <paramref name="latestTag"/> representa una versión estrictamente mayor que <paramref name="current"/>.</summary>
    public static bool IsNewer(string? latestTag, Version current)
        => TryParseTag(latestTag, out var latest) && latest > Normalize(current);

    /// <summary>Iguala los componentes no definidos (-1) a 0 para comparar de forma estable.</summary>
    private static Version Normalize(Version v)
        => new(Math.Max(0, v.Major), Math.Max(0, v.Minor), Math.Max(0, v.Build), Math.Max(0, v.Revision));
}
