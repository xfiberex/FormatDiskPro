// SPDX-License-Identifier: GPL-3.0-only
namespace FormatDiskPro;

/// <summary>
/// Qué hay en el disco físico de una unidad: su modelo y las letras de todas sus particiones. Es lo que la
/// confirmación de «Reinicializar» enseña antes de borrarlo entero (`AUD-035`).
/// </summary>
/// <remarks>
/// <para>Hasta ahora la confirmación hablaba solo de la letra seleccionada, y <c>Clear-Disk</c> borra el disco
/// físico: en un pendrive con dos particiones se perdía también la otra unidad sin que nada la nombrara.</para>
/// <para>Es un dato para la persona, no una guarda: lo que protege de borrar otro disco es
/// <see cref="DriveIdentity"/>, que el script vuelve a comprobar. Por eso va en una consulta aparte y no
/// amplía la identidad.</para>
/// </remarks>
/// <param name="Model">Nombre del disco tal como lo da Windows (<c>FriendlyName</c>); puede estar vacío.</param>
/// <param name="Letters">Letras de las particiones del disco, en mayúscula, sin repetir y en orden.</param>
public sealed record DiskContents(string Model, IReadOnlyList<char> Letters)
{
    /// <summary>
    /// Lee la línea <c>modelo|letras</c> que escribe la consulta de PowerShell (p. ej.
    /// <c>SanDisk Ultra|GI</c>). Devuelve <see langword="null"/> si no tiene esa forma.
    /// </summary>
    /// <remarks>Se parte por el ÚLTIMO <c>|</c>: el modelo lo escribe el fabricante y no se puede descartar que lo lleve.</remarks>
    public static DiskContents? Parse(string? line)
    {
        if (string.IsNullOrWhiteSpace(line)) return null;
        string trimmed = line.Trim();
        int bar = trimmed.LastIndexOf('|');
        if (bar < 0) return null;

        string model = trimmed[..bar].Trim();
        char[] letters = [.. trimmed[(bar + 1)..]
            .Where(char.IsAsciiLetter)
            .Select(char.ToUpperInvariant)
            .Distinct()
            .Order()];
        return new DiskContents(model, letters);
    }

    /// <summary>Las letras como se leen en pantalla: <c>G:, I:</c>.</summary>
    public static string FormatLetters(IEnumerable<char> letters) =>
        string.Join(", ", letters.Select(c => $"{char.ToUpperInvariant(c)}:"));
}
