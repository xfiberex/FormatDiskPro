// SPDX-License-Identifier: GPL-3.0-only
namespace FormatDiskPro;

/// <summary>
/// Acceso a los textos legales <b>embebidos</b> en el ejecutable (licencia GPLv3 y avisos de terceros),
/// para mostrarlos dentro de la app sin depender de archivos sueltos junto al .exe. Defensivo: ante
/// cualquier problema devuelve cadena vacía.
/// </summary>
public static class LegalText
{
    /// <summary>Texto completo de la licencia (GNU GPL v3.0).</summary>
    public static string License() => EmbeddedResources.Read("FormatDiskPro.LICENSE.txt");

    /// <summary>Avisos y atribuciones de componentes de terceros.</summary>
    public static string ThirdParty() => EmbeddedResources.Read("FormatDiskPro.THIRD-PARTY-NOTICES.txt");
}
