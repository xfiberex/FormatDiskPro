using System.Diagnostics;

namespace FormatDiskPro;

/// <summary>
/// Abre un enlace o un archivo <b>sin elevación</b>, a través del shell del escritorio (`AUD-004`).
/// </summary>
/// <remarks>
/// <para><b>El problema.</b> La app corre elevada, y un proceso hijo hereda el token de quien lo lanza.
/// Abrir un enlace con <c>UseShellExecute</c> desde aquí arrancaba el navegador —o el editor asociado a
/// <c>.log</c>— <b>como administrador</b>: un programa que procesa contenido de Internet, con todos los
/// privilegios, porque alguien pulsó «GitHub».</para>
///
/// <para><b>La salida.</b> El <c>explorer.exe</c> que pinta el escritorio ya corre con el token normal
/// de la sesión. Lanzar <c>explorer.exe "&lt;destino&gt;"</c> no abre el destino en el proceso nuevo: se
/// lo entrega al que ya está en marcha, que es quien lo abre, y lo abre sin elevar. Comprobado en
/// hardware el 2026-10-13: desde un proceso elevado, el navegador resultante no lo está.</para>
///
/// <para><b>Lo que se pierde.</b> Ese <c>explorer.exe</c> intermedio termina siempre con código 1 y no
/// dice si el destino llegó a abrirse. Aquí solo se sabe si arrancó; cuando no hay con qué abrir el
/// destino, quien lo cuenta es Windows con su propio diálogo.</para>
/// </remarks>
public static class DesktopShell
{
    /// <summary>Cómo se lanza la apertura de <paramref name="target"/>. Separado para poder probarlo.</summary>
    /// <param name="target">Una URL ya validada o la ruta absoluta de un archivo.</param>
    internal static ProcessStartInfo StartInfoFor(string target)
    {
        var psi = new ProcessStartInfo(SystemTools.Explorer) { UseShellExecute = false };
        // ArgumentList y no Arguments: el runtime entrecomilla el destino si lleva espacios, que es el
        // caso de cualquier ruta bajo «C:\Users\Nombre Apellido».
        psi.ArgumentList.Add(target);
        return psi;
    }

    /// <summary>Pide al shell del escritorio que abra <paramref name="target"/>.</summary>
    /// <param name="target">Una URL ya validada o la ruta absoluta de un archivo.</param>
    /// <exception cref="Exception">Lo que falle al arrancar <c>explorer.exe</c>.</exception>
    public static void Open(string target)
    {
        using Process? _ = Process.Start(StartInfoFor(target));
    }
}
