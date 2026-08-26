using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using System.Linq;

namespace FormatDiskPro;

/// <summary>
/// Lógica pura de construcción de comandos de formato y parseo de progreso.
/// Sin estado ni dependencias de UI: es directamente testeable en aislamiento.
/// </summary>
public static partial class FormatLogic
{
    /// <summary>Límite real de Windows para crear un volumen FAT32 (32 GiB): ni <c>Format-Volume</c> ni
    /// <c>format.com</c> permiten uno mayor. Usado para ocultar FAT32 del selector en discos grandes y
    /// como umbral de la opción de partición FAT32 pequeña en Reinicializar unidad.</summary>
    public const long Fat32MaxBytes = 32L * 1024 * 1024 * 1024;

    /// <summary>
    /// Construye el script PowerShell <c>Format-Volume</c> (sin codificar).
    /// La etiqueta se escapa para una cadena entre comillas simples de PowerShell.
    /// </summary>
    /// <remarks>
    /// <para><b>Valida sus dos entradas interpoladas antes de componer nada</b> (`T9-09`). La letra y el
    /// sistema de archivos se meten en el script <b>sin comillas</b>, así que son las dos posiciones
    /// donde un valor inesperado dejaría de ser un dato para pasar a ser sintaxis. Es la misma guarda que
    /// <see cref="DiskService"/> aplica en sus cinco métodos y que <see cref="ReinitDrive"/> aplica
    /// revalidando el plan entero; esta era la única función que construía un comando sin ella.</para>
    ///
    /// <para><b>Alcance honesto, como en <see cref="UpdateService.SafeAssetFileName"/>:</b> hoy no cierra
    /// ningún agujero abierto. El sistema de archivos sale siempre del <c>ComboBox</c> del XAML, y la vía
    /// por la que podría llegar algo de fuera —un preset del <c>settings.json</c>, que vive en
    /// <c>%AppData%</c>, se escribe <b>sin elevación</b> y lo lee un proceso <b>elevado</b>— está cerrada
    /// aparte: aplicar un preset exige que su sistema de archivos coincida con un ítem del selector. Lo
    /// que esto evita es que la seguridad de la ruta que formatea dependa de que <b>todos</b> los
    /// llamantes, presentes y futuros, se acuerden de validar antes.</para>
    /// </remarks>
    /// <exception cref="ArgumentException">
    /// Si la letra no lo es, o si el sistema de archivos no está en
    /// <see cref="PartitionPlan.SupportedFileSystems"/>.
    /// </exception>
    public static string BuildVolumeScript(
        char driveLetter, string fs, long allocBytes,
        string label, bool quickFormat, bool compress)
    {
        if (!char.IsLetter(driveLetter))
            throw new ArgumentException($"Letra de unidad no válida: '{driveLetter}'.", nameof(driveLetter));

        if (!PartitionPlan.SupportedFileSystems.Contains(fs, StringComparer.Ordinal))
            throw new ArgumentException($"Sistema de archivos no admitido: '{fs}'.", nameof(fs));

        var ps = new StringBuilder("Format-Volume");
        ps.Append($" -DriveLetter {driveLetter}");
        ps.Append($" -FileSystem {fs}");
        ps.Append($" -AllocationUnitSize {allocBytes}");
        if (!string.IsNullOrEmpty(label)) ps.Append($" -NewFileSystemLabel '{label.Replace("'", "''")}'");
        if (!quickFormat)                 ps.Append(" -Full");
        if (compress && fs == "NTFS")     ps.Append(" -Compress");
        ps.Append(" -Confirm:$false -Force");
        return ps.ToString();
    }

    /// <summary>Codifica un script como argumentos <c>-EncodedCommand</c> (Base64 UTF-16LE) de powershell.exe.</summary>
    public static string EncodeArguments(string script)
    {
        byte[] encoded = Encoding.Unicode.GetBytes(script);
        return $"-NonInteractive -NoProfile -EncodedCommand {Convert.ToBase64String(encoded)}";
    }

    /// <summary>
    /// Decodifica los argumentos producidos por <see cref="EncodeArguments"/> de vuelta al script original.
    /// </summary>
    /// <remarks>
    /// <b><c>internal</c>, no <c>public</c>: no lo usa la app, solo las pruebas</b> (`T9-13`). Se conserva
    /// porque hace legible lo que se afirma sobre un Base64 —«el script que se ejecuta es este»— en las
    /// pruebas que comprueban el escapado de la etiqueta. Lo que NO puede hacer es sostener sola la prueba
    /// de <see cref="EncodeArguments"/>: una ida y vuelta contra un inverso escrito a medida no falla si
    /// ambos lados comparten el mismo error, así que esa prueba ancla además el Base64 concreto.
    /// </remarks>
    internal static string DecodeArguments(string arguments)
    {
        const string marker = "-EncodedCommand ";
        int i = arguments.IndexOf(marker, StringComparison.Ordinal);
        if (i < 0) return "";
        string b64 = arguments[(i + marker.Length)..].Trim();
        return Encoding.Unicode.GetString(Convert.FromBase64String(b64));
    }

    /// <summary>
    /// Argumentos para <c>format.com</c> como lista (cada elemento se escapa de forma independiente
    /// por el runtime, evitando inyección a través de la etiqueta).
    ///
    /// <para><b><c>/Y</c> no es opcional.</b> Sin él, <c>format.com</c> hace DOS preguntas por consola:
    /// la confirmación («¿Continuar con el formato (S/N)?») y, si no se pasa <c>/V:</c>, la etiqueta de
    /// volumen. Antes se respondía escribiendo <c>"Y"</c> y <c>"S"</c> en la entrada estándar — las
    /// respuestas de un Windows <b>inglés y español</b>. En uno francés (<c>O</c>) o alemán (<c>J</c>) no
    /// coinciden y el proceso <b>se queda esperando entrada para siempre</b>, con el formato a medias.
    /// <c>/Y</c> suprime las dos preguntas y asume etiqueta vacía cuando no se especifica, así que no
    /// queda nada que dependa del idioma. También fuerza el desmontaje del volumen si hace falta, que es
    /// lo que uno quiere de una herramienta de formateo.</para>
    /// </summary>
    /// <exception cref="ArgumentException">
    /// Si la letra no lo es, o si el sistema de archivos no está en
    /// <see cref="PartitionPlan.SupportedFileSystems"/>. Aquí el escape por argumento del runtime ya
    /// impide la inyección; se valida igualmente para que las dos rutas de formateo acepten exactamente
    /// lo mismo y no haya una más permisiva que la otra (`T9-09`).
    /// </exception>
    public static IReadOnlyList<string> BuildComArgumentList(char driveLetter, string fs, long allocBytes, string label)
    {
        if (!char.IsLetter(driveLetter))
            throw new ArgumentException($"Letra de unidad no válida: '{driveLetter}'.", nameof(driveLetter));

        if (!PartitionPlan.SupportedFileSystems.Contains(fs, StringComparer.Ordinal))
            throw new ArgumentException($"Sistema de archivos no admitido: '{fs}'.", nameof(fs));

        var args = new List<string> { $"{driveLetter}:", $"/FS:{fs}", $"/A:{allocBytes}", "/Y" };
        if (!string.IsNullOrEmpty(label)) args.Add($"/V:{label}");
        return args;
    }

    /// <summary>Longitud máxima de etiqueta de volumen permitida por sistema de archivos.</summary>
    public static int MaxLabelLength(string fs) => fs switch
    {
        "NTFS" or "ReFS"            => 32,
        "FAT32" or "FAT" or "exFAT" => 11,
        _                           => 32,
    };

    /// <summary>Caracteres no permitidos en una etiqueta de volumen de Windows.</summary>
    public static readonly char[] InvalidLabelChars = ['\\', '/', ':', '*', '?', '"', '<', '>', '|'];

    /// <summary>Motivo por el que una etiqueta de volumen no es válida (o <see cref="Ok"/>).</summary>
    public enum LabelValidation { Ok, InvalidChars, TooLong }

    /// <summary>
    /// Valida una etiqueta de volumen para el sistema de archivos dado: caracteres permitidos
    /// (<see cref="InvalidLabelChars"/>) y longitud máxima por FS (<see cref="MaxLabelLength"/>).
    /// Una etiqueta vacía siempre es válida. Lógica pura, compartida por el hint en vivo del
    /// <c>TextBox</c> y la validación al enviar (formatear / reinicializar).
    /// </summary>
    public static LabelValidation ValidateLabel(string label, string fs)
    {
        if (string.IsNullOrEmpty(label)) return LabelValidation.Ok;
        if (label.Any(c => InvalidLabelChars.Contains(c))) return LabelValidation.InvalidChars;
        if (label.Length > MaxLabelLength(fs)) return LabelValidation.TooLong;
        return LabelValidation.Ok;
    }

    /// <summary>
    /// Extrae el último porcentaje (0-100) de un fragmento de salida de <c>format.com</c>; -1 si no hay.
    ///
    /// <para><b>Ojo con el idioma.</b> <c>format.com</c> no escribe el símbolo <c>%</c> sino la palabra
    /// («1 por ciento completado»), así que esto depende del idioma de <b>Windows</b> — que NO es el
    /// idioma de la app: alguien puede tener FormatDiskPro en español sobre un Windows alemán. El patrón
    /// cubría solo inglés y español, el mismo par que la respuesta <c>"Y"/"S"</c> que había en
    /// <c>RunFormatComAsync</c>; en un Windows francés o italiano la barra de progreso se quedaba clavada
    /// en 0 durante todo un formato completo, sin que nada fallara.</para>
    ///
    /// <para><b>Esta lista es incompleta por naturaleza</b> y no hay forma de completarla: Windows habla
    /// muchos más idiomas de los que se pueden enumerar aquí. En el peor caso se degrada a lo de antes —
    /// barra parada, formato correcto—, nunca a un fallo. Si aparece otro idioma, se añade aquí y a
    /// <c>FormatLogicTests.ExtractPercent_UnderstandsEachLanguage</c>.</para>
    /// </summary>
    public static int ExtractPercent(string chunk)
    {
        var matches = PercentRegex().Matches(chunk);
        if (matches.Count == 0) return -1;
        // Se lee de la salida de una herramienta, no de un humano: invariante siempre.
        return int.TryParse(matches[^1].Groups[1].Value, NumberStyles.Integer,
                            CultureInfo.InvariantCulture, out int v) ? v : -1;
    }

    /// <summary>
    /// Formatea una cantidad de bytes en una cadena legible (B, KB, MB, GB, TB), con un decimal como
    /// máximo y sin el <c>,0</c> en valores enteros ("2 GB", "1,5 KB" en español; "1.5 KB" en inglés).
    ///
    /// <para>`T6-12`: el separador decimal lo pone <see cref="L.Culture"/> —el idioma elegido en la app—,
    /// no la cultura de Windows, que es de donde venía «223.6 GB» junto a texto en español. Es una función
    /// de <b>presentación</b>: nada de lo que se guarda pasa por aquí.</para>
    /// </summary>
    /// <param name="bytes">Cantidad de bytes.</param>
    /// <param name="culture">Cultura de formato; por omisión, la del idioma activo de la app.</param>
    public static string FormatBytes(long bytes, IFormatProvider? culture = null)
    {
        string[] u = ["B", "KB", "MB", "GB", "TB"];
        double v = bytes; int i = 0;
        while (v >= 1024 && i < u.Length - 1) { v /= 1024; i++; }
        return string.Format(culture ?? L.Culture, "{0:0.#} {1}", v, u[i]);
    }

    // es: "por ciento" · pt: "por cento" · it: "per cento" · fr: "pour cent" · de: "Prozent".
    // `percent` va antes que `per\s*cento` a propósito: son prefijos distintos y no se solapan (el
    // italiano lleva espacio), pero el orden deja claro cuál es cuál.
    [GeneratedRegex(@"(\d{1,3})\s*(?:%|percent|por\s*ciento|por\s*cento|per\s*cento|pour\s*cent|prozent)",
        RegexOptions.IgnoreCase)]
    private static partial Regex PercentRegex();
}
