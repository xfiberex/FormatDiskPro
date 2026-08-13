namespace FormatDiskPro;

/// <summary>
/// Comparación y normalización de letras de unidad. Lógica pura y testeable.
/// </summary>
/// <remarks>
/// <para>Existe para que la comparación sea <b>invariante de cultura</b> en un único sitio. Con
/// <c>char.ToUpper</c> (sensible a la cultura) la comparación depende del idioma del sistema: en cultura
/// turca (<c>tr-TR</c>) <c>ToUpper('i')</c> devuelve <c>'İ'</c> (I con punto), que no es igual a
/// <c>'I'</c>. Una guarda escrita así <b>dejaría de reconocer la unidad <c>I:</c> como disco de
/// sistema</b> — y esa guarda es lo único que separa un formateo de una pérdida total de datos.</para>
/// <para>No es una hipótesis de laboratorio: es el clásico "problema de la I turca", la razón por la que
/// .NET recomienda las variantes <c>Invariant</c> para comparaciones que no son texto de cara al
/// usuario. Una letra de unidad es un identificador, no texto legible.</para>
/// </remarks>
public static class DriveLetter
{
    /// <summary>Letra en mayúscula, sin depender de la cultura activa.</summary>
    /// <param name="letter">Letra de unidad.</param>
    public static char Normalize(char letter) => char.ToUpperInvariant(letter);

    /// <summary>
    /// ¿Designan <paramref name="a"/> y <paramref name="b"/> la misma unidad? Comparación sin
    /// distinción de mayúsculas e <b>invariante de cultura</b>.
    /// </summary>
    /// <param name="a">Primera letra de unidad.</param>
    /// <param name="b">Segunda letra de unidad.</param>
    public static bool Same(char a, char b) => Normalize(a) == Normalize(b);
}
