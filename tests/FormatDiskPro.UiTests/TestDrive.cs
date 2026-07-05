namespace FormatDiskPro.UiTests;

/// <summary>
/// Localiza la unidad USB física dedicada a estas pruebas por ETIQUETA de volumen, no por letra:
/// "Reinicializar unidad" puede reasignar la letra, así que la letra no es un identificador estable
/// entre pasos. Las dos particiones ("utilidades" / "Bios Flash") están en el mismo disco físico y
/// autorizadas explícitamente por el usuario para pruebas destructivas (ambas vacías/desechables).
/// </summary>
public static class TestDrive
{
    public const string PrimaryLabel = "utilidades";
    public const string SecondaryLabel = "Bios Flash";

    /// <summary>
    /// Variable de entorno que debe valer "1" para permitir que se ejecuten pruebas que BORRAN datos
    /// reales (Formatear/Reinicializar hasta el final). Sin ella, esas pruebas fallan con un mensaje
    /// claro en vez de arriesgar cualquier unidad conectada por accidente.
    /// </summary>
    public const string DestructiveOptInVar = "FORMATDISKPRO_ALLOW_DESTRUCTIVE";

    public static char? FindLetter(string label)
    {
        foreach (var d in DriveInfo.GetDrives())
        {
            if (d.DriveType != DriveType.Removable) continue;
            try
            {
                if (d.IsReady && string.Equals(d.VolumeLabel, label, StringComparison.OrdinalIgnoreCase))
                    return d.Name[0];
            }
            catch { /* unidad retirada entre GetDrives() y la lectura de sus propiedades */ }
        }
        return null;
    }

    public static char RequireLetter(string label) =>
        FindLetter(label) ?? throw new InvalidOperationException(
            $"No se encontró conectada la unidad USB de pruebas (partición extraíble con etiqueta " +
            $"'{label}'). Conéctala antes de correr estas pruebas.");

    public static void RequireDestructiveOptIn()
    {
        if (Environment.GetEnvironmentVariable(DestructiveOptInVar) != "1")
            throw new InvalidOperationException(
                $"Esta prueba formatea/reinicializa la unidad USB de pruebas de verdad (BORRA su " +
                $"contenido). Solo corre si defines la variable de entorno {DestructiveOptInVar}=1 " +
                "antes de 'dotnet test'.");
    }
}
