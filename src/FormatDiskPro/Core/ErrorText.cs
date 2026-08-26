namespace FormatDiskPro;

/// <summary>
/// Convierte una excepción en texto para un humano — el usuario en un diálogo, o quien luego lea el
/// historial— <b>garantizando que nunca sale vacío</b>.
///
/// <para><b>Por qué existe.</b> Cuatro líneas del historial de la máquina de desarrollo decían
/// <c>EXPORT ERROR:</c> y nada más. No era un fallo del registro: la <c>Message</c> de esa excepción era
/// de verdad la cadena vacía. Una excepción que cruza la frontera de WinRT lleva su texto en un
/// <c>IRestrictedErrorInfo</c>, y cuando ese descriptor viene sin descripción —cosa habitual en los
/// fallos de COM— lo que llega a .NET es un mensaje en blanco. El resultado eran un <c>InfoBar</c> con
/// título y sin cuerpo, y un historial que registraba que algo falló sin decir qué: casi tan inútil
/// como el <c>catch</c> vacío que aquello vino a sustituir.</para>
///
/// <para>El respaldo es el tipo y el <c>HRESULT</c>. No es bonito y no es para el usuario medio, pero es
/// lo único que queda cuando la plataforma no da texto, y es exactamente lo que hace falta para
/// diagnosticarlo — de hecho fue así, con <c>COMException (HRESULT 0x80004005)</c> en pantalla, como se
/// identificó que el selector de archivos de WinRT no funciona en un proceso elevado.</para>
/// </summary>
public static class ErrorText
{
    /// <summary>
    /// Texto de una excepción, o su tipo y <c>HRESULT</c> si no trae mensaje.
    /// </summary>
    /// <param name="ex">La excepción que se va a contar.</param>
    /// <returns>Una cadena no vacía, siempre.</returns>
    public static string Describe(Exception ex) =>
        string.IsNullOrWhiteSpace(ex.Message)
            ? $"{ex.GetType().Name} (HRESULT 0x{ex.HResult:X8})"
            : ex.Message.Trim();
}
