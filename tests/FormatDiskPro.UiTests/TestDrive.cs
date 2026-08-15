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

    /// <summary>
    /// Variable de entorno que debe valer "1" para permitir las pruebas que <b>desmontan</b> la USB a
    /// mitad de una operación (`Set-Disk -IsOffline`) para comprobar que la app sobrevive. No borran
    /// datos —por eso no reusan <see cref="DestructiveOptInVar"/>— pero sí hacen desaparecer la unidad
    /// del sistema durante unos segundos, así que no deben correr por sorpresa en un corte de release.
    /// </summary>
    public const string YankOptInVar = "FORMATDISKPRO_ALLOW_YANK";

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

    /// <summary>
    /// ¿Hay alguna unidad montada que NO sea la de sistema? Las pruebas de la tarjeta de opciones no
    /// tocan ninguna unidad, pero necesitan que haya una seleccionable: sobre la de sistema
    /// (<c>[Protegido] C:</c>) <c>SetFormEnabled</c> deshabilita casi todos esos controles.
    ///
    /// <para>Es una precondición de <b>máquina</b>, no de la USB de pruebas: un equipo con un solo disco
    /// no la cumple. Antes eso salía en <b>rojo</b> —cuatro fallos que no eran fallos— y este proyecto ya
    /// decidió que precondición ausente se OMITE (ver <see cref="TestDriveFactAttribute"/>).</para>
    /// </summary>
    public static bool HasNonSystemDrive()
    {
        char system = char.ToUpperInvariant(Path.GetPathRoot(Environment.SystemDirectory)![0]);
        foreach (var d in DriveInfo.GetDrives())
        {
            try
            {
                if (d.IsReady && char.ToUpperInvariant(d.Name[0]) != system) return true;
            }
            catch { /* unidad retirada entre GetDrives() y la lectura de sus propiedades */ }
        }
        return false;
    }

    public static void RequireDestructiveOptIn()
    {
        if (Environment.GetEnvironmentVariable(DestructiveOptInVar) != "1")
            throw new InvalidOperationException(
                $"Esta prueba formatea/reinicializa la unidad USB de pruebas de verdad (BORRA su " +
                $"contenido). Solo corre si defines la variable de entorno {DestructiveOptInVar}=1 " +
                "antes de 'dotnet test'.");
    }
}
