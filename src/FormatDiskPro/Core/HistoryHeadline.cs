// SPDX-License-Identifier: GPL-3.0-only
using System.Text.RegularExpressions;

namespace FormatDiskPro;

/// <summary>
/// El titular de una entrada del historial, listo para traducir: de qué trata, qué pasó y sobre qué unidad o
/// versión, más el detalle técnico que queda después (`AUD-073`).
/// </summary>
/// <remarks>
/// <para><b>Por qué existe.</b> La lista enseñaba la línea del log tal cual —«UPDATE DOWNLOADED v1.30.2:
/// C:\ProgramData\…»—, en inglés y mayúsculas dentro de una interfaz en español. Al historial se va después de
/// que algo salga mal, a saber qué le pasó a un disco, y lo primero que se leía era un código.</para>
/// <para><b>Qué NO cambia.</b> <c>history.log</c> y el CSV siguen en crudo: tienen consumidores (soporte,
/// issues, hojas de cálculo) y las líneas ya escritas se releen igual. Esto es presentación, como
/// <see cref="HistoryEntry.Humanize"/>, y se calcula de la palabra clave, así que no hay migración.</para>
/// <para>Devuelve <b>claves</b>, no texto: la traducción es de la interfaz (el reparto de `PlanRejection`).
/// Si la cabecera trae una palabra que no conoce, no se arriesga a perderla: el detalle es la línea entera.</para>
/// </remarks>
/// <param name="NounKey">Clave de lo que trata la entrada («Formato», «Configuración», «Cierre inesperado»).</param>
/// <param name="ActionKey">Clave de lo que pasó si la cabecera lo dice («Descarga completada»), o <see langword="null"/>.</param>
/// <param name="Subject">Unidad («G:») o versión («1.30.2») de la que trata, o <see langword="null"/>.</param>
/// <param name="Detail">Lo que va detrás de la cabecera, con los tamaños legibles; vacío si no hay nada.</param>
public sealed record HistoryHeadline(string NounKey, string? ActionKey, string? Subject, string Detail);

public sealed partial record HistoryEntry
{
    /// <summary>
    /// Clave del sustantivo de cada palabra clave de <see cref="CategoryByPrefix"/>. Las operaciones usan el
    /// nombre de su categoría; las de la aplicación y las consultas, uno propio, porque «Aplicación · Error»
    /// no dice si falló el historial, la configuración o la app entera.
    /// </summary>
    /// <remarks>
    /// <c>HistoryHeadlineTests</c> exige que cada palabra de <see cref="CategoryByPrefix"/> tenga el suyo. Es un
    /// <c>switch</c> y no un diccionario para no tomar la forma que vigila <c>NoStringTablesOutsideLocalization</c>:
    /// aquí solo hay claves, el texto está en <c>Localization.cs</c>.
    /// </remarks>
    /// <param name="keyword">Palabra clave con la que empieza la línea.</param>
    /// <returns>La clave, o <see langword="null"/> si la palabra no es de la app.</returns>
    public static string? NounKeyFor(string keyword) => keyword switch
    {
        "FORMAT"        => "history.cat.format",
        "WIPE"          => "history.cat.wipe",
        "REINIT"        => "history.cat.reinit",
        "CHKDSK"        => "history.cat.chkdsk",
        "VERIFY"        => "history.cat.verify",
        "BENCH"         => "history.cat.benchmark",
        "HEALTH"        => "history.cat.health",
        "DISKSIZE"      => "history.noun.disksize",
        "UNLOCK"        => "history.cat.unlock",
        "EJECT"         => "history.cat.eject",
        "UPDATE"        => "history.cat.update",
        "WHATSNEW"      => "history.noun.whatsnew",
        "SETTINGS"      => "history.noun.settings",
        "HISTORY"       => "history.noun.history",
        "EXPORT"        => "history.noun.export",
        "CRASH"         => "history.noun.crash",
        "DIALOG"        => "history.noun.dialog",
        "WORKFOLDERS"   => "history.noun.workfolders",
        _ => null,
    };

    /// <summary>
    /// Palabras de la cabecera que dicen qué pasó, y su clave. Las de resultado (<c>OK</c>, <c>FAIL</c>,
    /// <c>ERROR</c>, <c>CANCELLED</c>) no están: el resultado ya lo lleva la fila, con su color.
    /// </summary>
    /// <param name="word">Palabra de la cabecera.</param>
    /// <returns>La clave, o <see langword="null"/> si no es una de las conocidas.</returns>
    public static string? ActionKeyFor(string word) => word switch
    {
        "DOWNLOADED"    => "history.act.downloaded",
        "DOWNLOAD"      => "history.act.download",
        "CHECK"         => "history.act.check",
        "REJECTED"      => "history.act.rejected",
        "FOUND"         => "history.act.found",
        "DELETED"       => "history.act.deleted",
        "DELETE"        => "history.act.delete",
        "SKIPPED"       => "history.act.skipped",
        "OPEN"          => "history.act.open",
        "READ"          => "history.act.read",
        "UNREADABLE"    => "history.act.unreadable",
        "CLOSED"        => "history.act.closed",
        _ => null,
    };

    /// <summary>Todas las palabras de <see cref="ActionKeyFor"/>, para poder recorrerlas en las pruebas.</summary>
    public static readonly IReadOnlyList<string> ActionWords =
        ["DOWNLOADED", "DOWNLOAD", "CHECK", "REJECTED", "FOUND", "DELETED", "DELETE", "SKIPPED", "OPEN", "READ", "UNREADABLE", "CLOSED"];

    private static readonly HashSet<string> ResultWords = new(StringComparer.Ordinal) { "OK", "FAIL", "ERROR", "CANCELLED" };

    /// <summary>El titular de esta entrada (`AUD-073`). Lógica pura.</summary>
    public HistoryHeadline Headline()
    {
        string message = Detail;
        string keyword = Keyword(message);
        if (NounKeyFor(keyword) is not string noun)
            return new HistoryHeadline("history.cat.other", null, null, Humanize(message));

        int colon = message.IndexOf(':');
        string head = colon < 0 ? message : message[..colon];
        string rest = colon < 0 ? "" : message[(colon + 1)..].Trim();

        string? action = null, subject = null;
        foreach (string word in head.Split(' ', StringSplitOptions.RemoveEmptyEntries).Skip(1))
        {
            if (ResultWords.Contains(word)) continue;
            if (word.Length == 1 && char.IsAsciiLetter(word[0])) { subject = $"{char.ToUpperInvariant(word[0])}:"; continue; }
            if (VersionRegex().IsMatch(word)) { subject = word.TrimStart('v', 'V'); continue; }
            if (action is null && ActionKeyFor(word) is string a) { action = a; continue; }

            // Una palabra que no conoce: mejor la línea entera que un titular que se la calle.
            return new HistoryHeadline(noun, null, null, Humanize(message));
        }
        return new HistoryHeadline(noun, action, subject, Humanize(rest));
    }

    // «v1.30.2», «1.30.2-beta»: lo que deja la etiqueta de una versión en la cabecera de UPDATE.
    [GeneratedRegex(@"^[vV]?\d+(\.\d+)+([-+][0-9A-Za-z.\-]+)?$")]
    private static partial Regex VersionRegex();
}
