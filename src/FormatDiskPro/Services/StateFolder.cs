// SPDX-License-Identifier: GPL-3.0-only
namespace FormatDiskPro;

/// <summary>
/// La carpeta donde la app guarda su estado —historial y preferencias—, y la comprobación de que sigue
/// siendo una carpeta de verdad (`AUD-025`).
/// </summary>
/// <remarks>
/// <para><b>El problema.</b> <c>%AppData%\FormatDiskPro</c> pertenece a la cuenta <b>sin elevar</b>, y la
/// app escribe, mueve y borra en ella <b>elevada</b>. Quien controle esa cuenta puede sustituir la
/// carpeta por una unión hacia otro sitio, y entonces es un administrador el que crea
/// <c>history.log</c>, sobrescribe <c>settings.json</c> o borra <c>history.1.log</c> allí donde la unión
/// apunte.</para>
///
/// <para><b>Lo que se hace.</b> Antes de tocar nada se mira si la carpeta o el archivo son puntos de
/// reanálisis (uniones, enlaces simbólicos), y si lo son <b>no se opera</b>: ni se lee, ni se escribe,
/// ni se aparta nada. La app funciona con los valores por defecto y lo dice al arrancar.</para>
///
/// <para><b>Alcance honesto.</b> Es una comprobación seguida de un uso, no una operación atómica:
/// quien sustituya la carpeta entre lo uno y lo otro la esquiva. Cerrar eso del todo exige llevar el
/// estado a una carpeta que solo escriban administradores, que cambia dónde viven las preferencias de
/// cada persona y cómo las borra el desinstalador; se dejó fuera a propósito. Lo que queda cerrado es
/// el caso que no necesita ganar ninguna carrera: dejar la unión puesta y esperar.</para>
/// </remarks>
public static class StateFolder
{
    /// <summary>Carpeta de estado de la app: <c>%AppData%\FormatDiskPro</c>.</summary>
    public static string DefaultDirectory { get; } = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "FormatDiskPro");

    /// <summary>
    /// ¿Lleva alguno de estos archivos, o la carpeta que los contiene, a otro sitio?
    /// </summary>
    /// <param name="files">
    /// Archivos de estado sobre los que se va a operar. Se comprueba la carpeta de cada uno y el archivo
    /// mismo si existe.
    /// </param>
    /// <returns>
    /// <see langword="true"/> si alguno es un punto de reanálisis <b>o no se pudo averiguar</b>: ante la
    /// duda no se escribe.
    /// </returns>
    public static bool IsRedirected(params string[] files)
    {
        foreach (string file in files)
        {
            if (IsReparsePoint(Path.GetDirectoryName(file)) || IsReparsePoint(file))
                return true;
        }
        return false;
    }

    /// <summary>¿Es <paramref name="path"/> una unión o un enlace simbólico? Lo que no existe, no lo es.</summary>
    internal static bool IsReparsePoint(string? path)
    {
        if (string.IsNullOrEmpty(path)) return false;
        try
        {
            // GetAttributes no sigue el enlace: devuelve los atributos del enlace mismo, que es
            // justo lo que hay que mirar.
            return (File.GetAttributes(path) & FileAttributes.ReparsePoint) != 0;
        }
        catch (FileNotFoundException) { return false; }
        catch (DirectoryNotFoundException) { return false; }
        catch (IOException) { return true; }
        catch (UnauthorizedAccessException) { return true; }
    }
}
