using System.Text;

namespace FormatDiskPro;

/// <summary>
/// Lo que se saca de la salida de <c>chkdsk</c> además de su código (`AUD-007`).
/// </summary>
public static class CheckDiskOutput
{
    /// <summary>
    /// ¿Llegó <c>chkdsk</c> a examinar el volumen? Lo delata que informara de algún porcentaje.
    /// </summary>
    /// <remarks>
    /// Es lo que permite leer el código 3, que <c>chkdsk</c> usa para dos cosas opuestas: «no pude
    /// comprobar» y «encontré errores y no los corregí porque no se pidió». Cuando no puede ni empezar
    /// —volumen en uso, sistema de archivos que no reconoce— no hay porcentajes; cuando examina, sí.
    /// No se mira el texto del resultado, que está en el idioma de Windows.
    /// </remarks>
    public static bool ScanRan(string? output) => FormatLogic.ExtractPercent(output ?? "") >= 0;

    /// <summary>
    /// Deja la salida como se vería en una consola, para poder enseñarla.
    /// </summary>
    /// <remarks>
    /// <c>chkdsk</c> pinta el progreso reescribiendo la misma línea con retornos de carro: capturado,
    /// eso son decenas de «33 por ciento completado» pegados. De cada línea se conserva lo último que
    /// se escribió en ella, se quitan los espacios de relleno y se juntan las líneas en blanco seguidas.
    /// </remarks>
    public static string Clean(string? output)
    {
        var result = new StringBuilder();
        bool previousBlank = true;   // también se descartan las líneas en blanco del principio

        foreach (string rawLine in (output ?? "").Replace("\r\n", "\n").Split('\n'))
        {
            string[] rewrites = rawLine.Split('\r', StringSplitOptions.RemoveEmptyEntries);
            string line = rewrites.Length > 0 ? rewrites[^1].TrimEnd() : "";

            bool blank = line.Length == 0;
            if (blank && previousBlank) continue;
            result.AppendLine(line);
            previousBlank = blank;
        }

        return result.ToString().TrimEnd();
    }
}
