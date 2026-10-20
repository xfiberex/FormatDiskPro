// SPDX-License-Identifier: GPL-3.0-only
namespace FormatDiskPro;

/// <summary>
/// Lectura de los textos que viajan embebidos en el ejecutable: licencia, avisos de terceros y CHANGELOG.
/// </summary>
internal static class EmbeddedResources
{
    /// <summary>Lee un recurso de texto del ensamblado de la app.</summary>
    /// <param name="name">El nombre lógico del recurso (el <c>LogicalName</c> del <c>.csproj</c>).</param>
    /// <returns>El texto, o una cadena vacía ante cualquier problema: quien lo enseña tiene su propio mensaje.</returns>
    public static string Read(string name)
    {
        try
        {
            using var s = typeof(EmbeddedResources).Assembly.GetManifestResourceStream(name);
            if (s is null) return "";
            using var r = new StreamReader(s);
            return r.ReadToEnd();
        }
        catch
        {
            return "";
        }
    }
}
