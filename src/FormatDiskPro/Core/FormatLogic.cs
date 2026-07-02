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
    public static string BuildVolumeScript(
        char driveLetter, string fs, long allocBytes,
        string label, bool quickFormat, bool compress)
    {
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

    /// <summary>Decodifica los argumentos producidos por <see cref="EncodeArguments"/> de vuelta al script original.</summary>
    public static string DecodeArguments(string arguments)
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
    /// </summary>
    public static IReadOnlyList<string> BuildComArgumentList(char driveLetter, string fs, long allocBytes, string label)
    {
        var args = new List<string> { $"{driveLetter}:", $"/FS:{fs}", $"/A:{allocBytes}" };
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

    /// <summary>Extrae el último porcentaje (0-100) de un fragmento de salida de <c>format.com</c>; -1 si no hay.</summary>
    public static int ExtractPercent(string chunk)
    {
        var matches = PercentRegex().Matches(chunk);
        if (matches.Count == 0) return -1;
        return int.TryParse(matches[^1].Groups[1].Value, out int v) ? v : -1;
    }

    /// <summary>Formatea una cantidad de bytes en una cadena legible (B, KB, MB, GB, TB), con un decimal
    /// como máximo y sin el ".0" en valores enteros ("2 GB", "1.5 KB").</summary>
    public static string FormatBytes(long bytes)
    {
        string[] u = ["B", "KB", "MB", "GB", "TB"];
        double v = bytes; int i = 0;
        while (v >= 1024 && i < u.Length - 1) { v /= 1024; i++; }
        return $"{v:0.#} {u[i]}";
    }

    [GeneratedRegex(@"(\d{1,3})\s*(?:%|percent|por\s*ciento)", RegexOptions.IgnoreCase)]
    private static partial Regex PercentRegex();
}
