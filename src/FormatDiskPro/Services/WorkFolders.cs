// SPDX-License-Identifier: GPL-3.0-only
namespace FormatDiskPro;

/// <summary>
/// Carpetas de trabajo que la app crea en la raíz de una unidad: el borrado seguro, la verificación de
/// capacidad y el benchmark escriben en ellas y las borran al terminar (`AUD-038`).
/// </summary>
/// <remarks>
/// <para>Las borra el <c>finally</c> de cada servicio, también si la operación se cancela o falla. Lo que
/// ese <c>finally</c> no cubre es que el proceso muera (cierre forzado, apagón, la USB que se va a mitad) o
/// que el borrado falle (un antivirus con el archivo abierto): entonces la carpeta se queda, con hasta todo
/// el espacio libre de la unidad dentro, y nada lo decía.</para>
/// <para>Por eso la ventana las busca al seleccionar una unidad y al terminar cada operación, y ofrece
/// borrarlas. Los nombres viven aquí, en un solo sitio, para que buscar y crear no puedan divergir.</para>
/// </remarks>
public static class WorkFolders
{
    /// <summary>Carpeta del borrado seguro.</summary>
    public const string Wipe = "__fdp_wipe__";

    /// <summary>Carpeta de la verificación de capacidad.</summary>
    public const string Verify = "__fdp_verify__";

    /// <summary>Carpeta del benchmark.</summary>
    public const string Bench = "__fdp_bench__";

    /// <summary>Todas las carpetas de trabajo.</summary>
    public static readonly IReadOnlyList<string> All = [Wipe, Verify, Bench];

    /// <summary>Ruta de la carpeta <paramref name="name"/> en la raíz de la unidad <paramref name="letter"/>.</summary>
    public static string On(char letter, string name) => $"{letter}:\\{name}";

    /// <summary>
    /// Carpetas de trabajo que existen bajo <paramref name="root"/> (la raíz de una unidad, o una carpeta en
    /// las pruebas). Una unidad que no está lista no tiene ninguna.
    /// </summary>
    public static IReadOnlyList<string> FindLeftovers(string root)
    {
        try { return [.. All.Select(n => Path.Combine(root, n)).Where(Directory.Exists)]; }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException) { return []; }
    }

    /// <summary>Borra las carpetas indicadas y devuelve las que no se pudieron borrar.</summary>
    public static IReadOnlyList<string> Delete(IEnumerable<string> dirs)
    {
        var failed = new List<string>();
        foreach (string dir in dirs)
        {
            try { if (Directory.Exists(dir)) Directory.Delete(dir, recursive: true); }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException) { failed.Add(dir); }
        }
        return failed;
    }
}
