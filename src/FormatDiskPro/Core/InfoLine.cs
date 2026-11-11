// SPDX-License-Identifier: GPL-3.0-only
namespace FormatDiskPro;

/// <summary>
/// Une los datos de una línea de contexto («SSD SATA · 223,6 GB · Disco fijo») <b>saltándose los que no
/// hay</b>.
/// </summary>
/// <remarks>
/// <para><b>Por qué existe</b> (`AUD-072`, `AUD-075`). Un USB no informa de su conexión ni de su salud, y la
/// línea de la tarjeta decía «FAT32 · USB / Extraíble · –»: un guion suelto se lee como un dato que no
/// terminó de cargar, no como uno que no existe. Las confirmaciones necesitan la misma línea con el modelo,
/// que también puede faltar. Un solo sitio decide qué cuenta como «no hay» para que las dos no discrepen.</para>
/// <para>Cuenta como vacío: <see langword="null"/>, una cadena en blanco y el marcador de dato desconocido
/// que la interfaz usaba hasta ahora («–», en <see cref="Placeholder"/>).</para>
/// </remarks>
public static class InfoLine
{
    /// <summary>Separador entre datos, el mismo que usa la tarjeta de unidad.</summary>
    public const string Separator = " · ";

    /// <summary>Marcador de «sin dato» que no debe aparecer suelto en una línea.</summary>
    public const string Placeholder = "–";

    /// <summary>Une con <see cref="Separator"/> los datos que tienen valor, en el orden dado.</summary>
    /// <param name="parts">Datos ya formateados; los vacíos o <see cref="Placeholder"/> se omiten.</param>
    /// <returns>La línea, o cadena vacía si ningún dato tiene valor.</returns>
    public static string Join(params string?[] parts) =>
        string.Join(Separator, parts.Where(HasValue).Select(p => p!.Trim()));

    /// <summary>Si un dato tiene algo que enseñar.</summary>
    /// <param name="part">Dato ya formateado.</param>
    public static bool HasValue(string? part) =>
        !string.IsNullOrWhiteSpace(part) && part.Trim() != Placeholder;
}
