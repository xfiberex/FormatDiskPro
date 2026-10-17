// SPDX-License-Identifier: GPL-3.0-only
using System.Text;

namespace FormatDiskPro;

/// <summary>
/// Los argumentos con los que la aplicación lanza <c>powershell.exe</c>: el script va en
/// <c>-EncodedCommand</c> (Base64 UTF-16LE), así que nada de lo que lleve se interpreta como argumento.
/// </summary>
/// <remarks>
/// Una sola definición (`AUD-041`): antes la misma línea estaba escrita en <see cref="FormatLogic"/>, en
/// <see cref="DiskService"/> y en <see cref="ReinitDrive"/>, y cambiar una opción de arranque —añadir
/// <c>-NoLogo</c>, quitar el perfil— exigía acordarse de las tres.
/// </remarks>
public static class PowerShellCommand
{
    /// <summary>Codifica un script como argumentos <c>-EncodedCommand</c> de powershell.exe.</summary>
    /// <param name="script">El script, sin codificar.</param>
    /// <returns>La línea de argumentos, con <c>-NonInteractive -NoProfile</c> delante.</returns>
    public static string Encode(string script)
    {
        byte[] encoded = Encoding.Unicode.GetBytes(script);
        return $"-NonInteractive -NoProfile -EncodedCommand {Convert.ToBase64String(encoded)}";
    }

    /// <summary>
    /// Decodifica los argumentos producidos por <see cref="Encode"/> de vuelta al script original.
    /// </summary>
    /// <param name="arguments">La línea de argumentos que devolvió <see cref="Encode"/>.</param>
    /// <returns>El script, o una cadena vacía si no hay <c>-EncodedCommand</c>.</returns>
    /// <remarks>
    /// <b><c>internal</c>, no <c>public</c>: no lo usa la app, solo las pruebas</b> (`T9-13`). Se conserva
    /// porque hace legible lo que se afirma sobre un Base64 —«el script que se ejecuta es este»— en las
    /// pruebas que comprueban el escapado de la etiqueta. Lo que NO puede hacer es sostener sola la prueba
    /// de <see cref="Encode"/>: una ida y vuelta contra un inverso escrito a medida no falla si ambos lados
    /// comparten el mismo error, así que esa prueba ancla además el Base64 concreto.
    /// </remarks>
    internal static string Decode(string arguments)
    {
        const string marker = "-EncodedCommand ";
        int i = arguments.IndexOf(marker, StringComparison.Ordinal);
        if (i < 0) return "";
        string b64 = arguments[(i + marker.Length)..].Trim();
        return Encoding.Unicode.GetString(Convert.FromBase64String(b64));
    }
}
