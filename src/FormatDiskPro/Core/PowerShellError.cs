// SPDX-License-Identifier: GPL-3.0-only
using System.Globalization;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;

namespace FormatDiskPro;

/// <summary>
/// Convierte lo que PowerShell escribe por su salida de error en el <b>mensaje</b> que hay dentro
/// (`AUD-011`).
/// </summary>
/// <remarks>
/// <para><b>Qué llega.</b> Lanzado con <c>-EncodedCommand</c> y la salida redirigida, PowerShell no
/// escribe el error como texto: lo serializa en CLIXML. El diálogo de «Error al formatear» enseñaba eso
/// tal cual —una línea <c>#&lt; CLIXML</c> y un bloque de XML con registros de progreso, nodos
/// <c>&lt;S S="Error"&gt;</c> y saltos de línea escritos como <c>_x000D__x000A_</c>—, con el motivo
/// real enterrado dentro.</para>
///
/// <para><b>Qué sale.</b> El primer párrafo del error: lo que dice el cmdlet. Se descarta lo que viene
/// después —la línea del script, los <c>+ ~~~~</c>, <c>CategoryInfo</c>, <c>FullyQualifiedErrorId</c>—,
/// que describe el script interno de la app y a quien lo lee no le dice nada. El corte no depende del
/// idioma de Windows: se hace en la primera línea que empieza por <c>+</c>.</para>
///
/// <para>Defensivo: si el texto no es CLIXML, o no se reconoce nada dentro, se devuelve el original
/// recortado. Un mensaje feo es mejor que uno vacío.</para>
/// </remarks>
public static partial class PowerShellError
{
    [GeneratedRegex("<S S=\"Error\">(.*?)</S>", RegexOptions.Singleline | RegexOptions.CultureInvariant)]
    private static partial Regex ErrorNodeRegex();

    [GeneratedRegex("_x([0-9A-Fa-f]{4})_", RegexOptions.CultureInvariant)]
    private static partial Regex EscapeRegex();

    /// <summary>«En línea: 1 Carácter: 1», «At line:1 char:1»: dos números tras dos puntos, en cualquier idioma.</summary>
    [GeneratedRegex(@"^\D*:\s*\d+\D+:\s*\d+\s*$", RegexOptions.CultureInvariant)]
    private static partial Regex PositionLineRegex();

    [GeneratedRegex(@"\s+", RegexOptions.CultureInvariant)]
    private static partial Regex WhitespaceRegex();

    /// <summary>Extrae el mensaje legible de la salida de error de PowerShell.</summary>
    /// <param name="stderr">Lo que el proceso escribió por su salida de error; puede ser <c>null</c>.</param>
    /// <returns>El mensaje en una sola línea, o el texto original recortado si no era CLIXML.</returns>
    public static string Clean(string? stderr)
    {
        string raw = (stderr ?? "").Trim();
        if (!raw.Contains("CLIXML", StringComparison.Ordinal)) return raw;

        var text = new StringBuilder();
        foreach (Match node in ErrorNodeRegex().Matches(raw))
            text.Append(Unescape(node.Groups[1].Value));
        if (text.Length == 0) return raw;

        // PowerShell parte el mensaje al ancho de la consola, así que las líneas del primer párrafo se
        // vuelven a unir. El párrafo termina donde empieza la descripción del script.
        var message = new List<string>();
        foreach (string line in text.ToString().Split('\n'))
        {
            string trimmed = line.Trim();
            if (trimmed.StartsWith('+')) break;
            if (trimmed.Length > 0) message.Add(trimmed);
        }
        if (message.Count > 1 && PositionLineRegex().IsMatch(message[^1]))
            message.RemoveAt(message.Count - 1);

        string result = WhitespaceRegex().Replace(string.Join(' ', message), " ").Trim();
        return result.Length > 0 ? result : raw;
    }

    /// <summary>Deshace los dos escapes de CLIXML: <c>_xHHHH_</c> (UTF-16) y las entidades de XML.</summary>
    private static string Unescape(string value)
    {
        string unescaped = EscapeRegex().Replace(value, m =>
            ((char)int.Parse(m.Groups[1].Value, NumberStyles.HexNumber, CultureInfo.InvariantCulture)).ToString());
        return WebUtility.HtmlDecode(unescaped);
    }
}
