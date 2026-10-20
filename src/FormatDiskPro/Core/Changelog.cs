// SPDX-License-Identifier: GPL-3.0-only
using System.Text.RegularExpressions;

namespace FormatDiskPro;

/// <summary>
/// Las notas de una versión, leídas del <c>CHANGELOG.md</c> que viaja embebido en el ejecutable.
/// </summary>
/// <remarks>
/// <para><b>Por qué embebido</b> (`AUD-067`). <i>Novedades</i> pedía las notas a la API de GitHub, y lo hacía
/// en el primer arranque tras actualizar aunque la persona hubiera desactivado la comprobación automática: su
/// IP salía una vez por versión, justo lo que el texto de privacidad decía que no pasaba. Las notas de cada
/// release salen de este mismo archivo —<c>release.ps1</c> las toma de él—, así que no hay nada que descargar:
/// la app lleva las suyas.</para>
///
/// <para>La sección se corta igual que en <c>Get-ChangelogSection</c> de <c>release.ps1</c>: del encabezado
/// <c>## [X.Y.Z]</c> al siguiente <c>## </c>.</para>
/// </remarks>
public static partial class Changelog
{
    /// <summary>Nombre del recurso con el CHANGELOG dentro del ensamblado.</summary>
    public const string ResourceName = "FormatDiskPro.CHANGELOG.md";

    /// <summary>Notas de <paramref name="version"/> en el CHANGELOG embebido.</summary>
    /// <param name="version">La versión, sin la <c>v</c> (p. ej. <c>1.30.3</c>).</param>
    /// <returns>El Markdown de su sección, o una cadena vacía si no está.</returns>
    public static string ForVersion(string version) => Section(EmbeddedResources.Read(ResourceName), version);

    /// <summary>La sección de <paramref name="version"/> dentro de un CHANGELOG.</summary>
    /// <param name="changelog">El texto completo del CHANGELOG.</param>
    /// <param name="version">La versión, sin la <c>v</c>.</param>
    /// <returns>Las líneas entre su encabezado y el siguiente, sin espacios en los extremos; vacío si no está.</returns>
    public static string Section(string? changelog, string version)
    {
        if (string.IsNullOrEmpty(changelog) || string.IsNullOrWhiteSpace(version)) return "";

        string[] lines = changelog.Split('\n');
        var header = new Regex(@"^##\s*\[" + Regex.Escape(version.Trim()) + @"\]");
        int start = Array.FindIndex(lines, l => header.IsMatch(l));
        if (start < 0) return "";

        int end = Array.FindIndex(lines, start + 1, l => NextSection().IsMatch(l));
        if (end < 0) end = lines.Length;
        return string.Join("\n", lines[(start + 1)..end].Select(l => l.TrimEnd('\r'))).Trim();
    }

    [GeneratedRegex(@"^##\s")]
    private static partial Regex NextSection();
}
