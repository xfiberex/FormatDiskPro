namespace FormatDiskPro;

/// <summary>
/// Qué direcciones puede abrir la app en el navegador (`AUD-004`).
/// </summary>
/// <remarks>
/// <para><b>Por qué hace falta.</b> «Abrir una URL» se implementa pidiéndole al shell que abra una
/// cadena, y el shell abre <b>cualquier cosa</b>: <c>C:\x.exe</c> lo ejecuta, <c>file:///…</c> también.
/// Una de las direcciones que se abren —la página del release— no la escribe el proyecto: llega en el
/// JSON de GitHub. Mientras lo que se le pase al shell no se compruebe, lo que separa «ver las
/// novedades» de «ejecutar un archivo» es que ese campo traiga siempre lo que se espera.</para>
///
/// <para><b>Lista cerrada y no «cualquier https».</b> La app enlaza a dos sitios y no tiene por qué
/// abrir un tercero. Añadir un enlace nuevo obliga a pasar por aquí, que es la idea.</para>
/// </remarks>
public static class LinkPolicy
{
    /// <summary>Los únicos sitios a los que la app enlaza: el repositorio y la página de donaciones.</summary>
    private static readonly string[] AllowedHosts = ["github.com", "www.paypal.me"];

    /// <summary>
    /// Comprueba una dirección y la devuelve en su forma canónica, que es la que hay que abrir.
    /// </summary>
    /// <param name="url">La dirección tal como llega.</param>
    /// <param name="safe">
    /// La dirección normalizada por <see cref="Uri"/> (con sus caracteres especiales escapados), o cadena
    /// vacía si se rechaza.
    /// </param>
    /// <returns>
    /// <see langword="true"/> solo si es <c>https</c>, absoluta, de un sitio de la lista, sin usuario ni
    /// contraseña incrustados y en el puerto habitual.
    /// </returns>
    public static bool TryGetSafeUrl(string? url, out string safe)
    {
        safe = "";
        if (string.IsNullOrWhiteSpace(url)) return false;
        if (!Uri.TryCreate(url.Trim(), UriKind.Absolute, out Uri? uri)) return false;

        if (uri.Scheme != Uri.UriSchemeHttps) return false;
        if (!uri.IsDefaultPort) return false;
        // «https://github.com@otro.sitio/» es una dirección de otro.sitio que a simple vista parece de
        // GitHub. El host ya la descartaría; se rechaza aparte para que no dependa de eso.
        if (uri.UserInfo.Length > 0) return false;
        if (!AllowedHosts.Contains(uri.IdnHost, StringComparer.OrdinalIgnoreCase)) return false;

        safe = uri.AbsoluteUri;
        return true;
    }
}
