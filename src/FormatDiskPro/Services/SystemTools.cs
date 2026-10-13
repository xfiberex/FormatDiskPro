using System.Runtime.InteropServices;
using System.Text;

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

    /// <summary>
    /// La página de códigos ANSI de Windows, que es en la que <c>chkdsk</c> escribe cuando su salida
    /// está redirigida y no tiene consola (`AUD-007`).
    /// </summary>
    /// <remarks>
    /// Medido el 2026-10-13 leyendo los bytes: en un Windows en español «está» llega como 0xE1, que es
    /// la 1252 y no la 850 de la consola. Mientras la salida solo se usaba para buscar porcentajes daba
    /// igual; para enseñarla, leída con otra página salen «estß» y «comprobaci¾n».
    /// </remarks>
    public static Encoding AnsiEncoding { get; } = ResolveAnsiEncoding();

    private static Encoding ResolveAnsiEncoding()
    {
        try
        {
            int codePage = (int)GetACP();
            if (codePage == Encoding.UTF8.CodePage) return Encoding.UTF8;
            // Las páginas de códigos de Windows no vienen registradas en .NET; el proveedor se
            // consulta directamente, sin registrarlo para todo el proceso.
            return CodePagesEncodingProvider.Instance.GetEncoding(codePage) ?? Encoding.Latin1;
        }
        catch (Exception ex) when (ex is ArgumentException or NotSupportedException)
        {
            return Encoding.Latin1;
        }
    }

    [DllImport("kernel32.dll")]
    private static extern uint GetACP();
}
