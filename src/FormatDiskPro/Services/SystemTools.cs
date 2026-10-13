namespace FormatDiskPro;

/// <summary>
/// Rutas <b>absolutas</b> de las herramientas de Windows que lanza la app. Es el único sitio donde se
/// decide dónde está cada una (`AUD-026`).
/// </summary>
/// <remarks>
/// <para><b>Por qué no basta el nombre.</b> <c>Process.Start("powershell.exe")</c> deja que Windows lo
/// busque: primero en la carpeta de la aplicación, después en el directorio actual, y al final en el
/// <c>PATH</c>. La app corre <b>elevada</b>, así que cualquier <c>powershell.exe</c> que aparezca antes
/// que el de verdad en ese orden se ejecuta como administrador. Con la ruta completa no hay búsqueda
/// que ganar.</para>
///
/// <para><b>Alcance honesto.</b> Los dos primeros sitios de esa búsqueda son carpetas donde hoy solo
/// escribe un administrador (la de instalación) o que la app no cambia (el directorio actual), y el
/// <c>PATH</c> del sistema va por delante del del usuario. No había un camino abierto: había una
/// suposición —que nadie llegara a escribir ahí— sosteniendo la seguridad de todo lo que se lanza.</para>
/// </remarks>
public static class SystemTools
{
    /// <summary>Windows PowerShell 5.1, el que viene con el sistema.</summary>
    public static string PowerShell { get; } =
        Path.Combine(Environment.SystemDirectory, "WindowsPowerShell", "v1.0", "powershell.exe");

    /// <summary><c>chkdsk.exe</c>.</summary>
    public static string ChkDsk { get; } = Path.Combine(Environment.SystemDirectory, "chkdsk.exe");

    /// <summary><c>format.com</c>.</summary>
    public static string FormatCom { get; } = Path.Combine(Environment.SystemDirectory, "format.com");

    /// <summary>
    /// <c>explorer.exe</c>. Vive en la carpeta de Windows, no en <c>System32</c>.
    /// </summary>
    public static string Explorer { get; } =
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows), "explorer.exe");
}
