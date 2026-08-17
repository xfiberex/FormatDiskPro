using System.Text;
using System.Text.RegularExpressions;

namespace FormatDiskPro;

/// <summary>
/// Conversión de las notas de versión (Markdown de GitHub Releases) a texto plano legible para
/// mostrarlas en el diálogo de novedades. Lógica pura y testeable (sin red ni UI).
/// </summary>
public static partial class ReleaseNotes
{
    [GeneratedRegex(@"^\s{0,3}#{1,6}\s*")]              private static partial Regex HeadingRegex();
    [GeneratedRegex(@"^(\s*)[-*+]\s+")]                 private static partial Regex BulletRegex();
    [GeneratedRegex(@"\[([^\]]+)\]\([^)]*\)")]          private static partial Regex LinkRegex();
    [GeneratedRegex(@"\n{3,}")]                         private static partial Regex BlankLinesRegex();

    // Énfasis de un solo marcador (*cursiva*, _cursiva_). Las negritas (`**`/`__`) ya se han quitado
    // antes por reemplazo directo, así que aquí solo quedan los sueltos. Exigir que el marcador abra y
    // cierre pegado a un carácter no-espacio es lo que evita comerse un asterisco suelto de un texto
    // legítimo; el subrayado pide además que no haya letra alrededor, para no partir nombres_asi.
    [GeneratedRegex(@"\*(?=\S)([^*\n]+?)(?<=\S)\*")]            private static partial Regex ItalicAsteriskRegex();
    [GeneratedRegex(@"(?<![\w_])_(?=\S)([^_\n]+?)(?<=\S)_(?![\w_])")] private static partial Regex ItalicUnderscoreRegex();

    /// <summary>
    /// Convierte Markdown a texto plano: quita marcadores de encabezado (<c>#</c>), normaliza viñetas
    /// (<c>-</c>/<c>*</c>/<c>+</c> → <c>•</c>), elimina negritas y cursivas (<c>**</c>, <c>__</c>,
    /// <c>*</c>, <c>_</c>) y comillas de código, reduce enlaces <c>[texto](url)</c> a su texto,
    /// reúne en una sola línea los saltos internos de un párrafo y colapsa líneas en blanco repetidas.
    /// Devuelve cadena vacía si la entrada es nula o en blanco.
    /// </summary>
    /// <param name="markdown">Cuerpo Markdown de la versión (campo <c>body</c> del release).</param>
    public static string ToPlainText(string? markdown)
    {
        if (string.IsNullOrWhiteSpace(markdown)) return "";

        string normalized = markdown.Replace("\r\n", "\n").Replace("\r", "\n");
        var sb = new StringBuilder();

        // ¿Puede la línea siguiente unirse a la que acabamos de escribir? Un salto simple dentro de un
        // mismo bloque significa un espacio en Markdown: la fuente viene ajustada a ~100 columnas y el
        // TextBlock del diálogo ajusta por su cuenta, así que respetar esos saltos parte los párrafos
        // dos veces, a mitad de frase. Cierran bloque la línea en blanco, el encabezado (que ocupa una
        // línea y solo una) y el salto forzado de Markdown (dos espacios al final).
        bool blockIsOpen = false;

        foreach (string raw in normalized.Split('\n'))
        {
            bool isBlank   = raw.AsSpan().Trim().IsEmpty;
            bool isHeading = HeadingRegex().IsMatch(raw);
            bool isBullet  = BulletRegex().IsMatch(raw);
            bool hardBreak = raw.EndsWith("  ", StringComparison.Ordinal);

            string line = HeadingRegex().Replace(raw, "");
            line = BulletRegex().Replace(line, "$1• ");
            line = LinkRegex().Replace(line, "$1");
            line = line.Replace("**", "").Replace("__", "").Replace("`", "");
            line = ItalicAsteriskRegex().Replace(line, "$1");
            line = ItalicUnderscoreRegex().Replace(line, "$1");
            line = line.TrimEnd();

            // Una viñeta abre bloque propio; sus continuaciones sí se le unen.
            if (blockIsOpen && !isBlank && !isHeading && !isBullet)
                sb.Append(' ').Append(line.TrimStart());
            else
            {
                if (sb.Length > 0) sb.Append('\n');
                sb.Append(line);
            }

            blockIsOpen = !isBlank && !isHeading && !hardBreak;
        }

        // Colapsar 3+ saltos consecutivos a un máximo de dos (una línea en blanco) y recortar extremos.
        return BlankLinesRegex().Replace(sb.ToString(), "\n\n").Trim();
    }
}
