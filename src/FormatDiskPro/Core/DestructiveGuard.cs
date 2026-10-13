namespace FormatDiskPro;

/// <summary>Por qué una operación destructiva no procede (o <see cref="None"/> si procede).</summary>
public enum GuardRejection
{
    /// <summary>Nada lo impide.</summary>
    None,
    /// <summary>No hay ninguna unidad seleccionada.</summary>
    NoDrive,
    /// <summary>La unidad está marcada como protegida.</summary>
    Protected,
    /// <summary>Es la unidad que contiene Windows.</summary>
    SystemDrive,
    /// <summary>La operación solo admite unidades extraíbles, y esta no lo es.</summary>
    NotRemovable,
    /// <summary>No se pudo averiguar en qué disco físico está la unidad a borrar.</summary>
    TargetDiskUnknown,
    /// <summary>No se pudo averiguar en qué disco físico está Windows.</summary>
    SystemDiskUnknown,
    /// <summary>La unidad está en el mismo disco físico que Windows.</summary>
    SameDiskAsSystem,
}

/// <summary>
/// Las decisiones que separan una operación destructiva de una pérdida de datos que nadie pidió:
/// <b>¿se puede formatear esta unidad? ¿se puede borrar este disco?</b>
/// </summary>
/// <remarks>
/// <para><b>Por qué aquí y no en la ventana</b> (`AUD-015`). Estas guardas vivían repartidas por los
/// manejadores de <c>MainWindow</c>, mezcladas con los diálogos que enseñan, y ninguna prueba ejercía su
/// rechazo: la única forma de comprobarlas era intentar formatear el disco de Windows. Son, sin embargo,
/// una tabla de decisiones sobre hechos —una letra, un tipo de unidad, dos números de disco—, sin
/// efectos: aquí se prueban enteras sin hardware y sin ventana, como <see cref="PartitionPlan.Validate"/>.
/// La ventana reúne los hechos y traduce el motivo a un diálogo; no decide.</para>
///
/// <para><b>Ante la duda, no.</b> Lo que no se pudo averiguar se trata como un rechazo con motivo propio,
/// nunca como «no hay inconveniente». Hasta `AUD-012` la guarda del disco de Windows hacía lo contrario
/// con uno de sus dos datos: si no se podía leer el disco del sistema, dejaba pasar.</para>
/// </remarks>
public static class DestructiveGuard
{
    /// <summary>¿Es <paramref name="letter"/> la unidad que contiene Windows?</summary>
    /// <param name="letter">Letra de la unidad.</param>
    /// <param name="systemLetter">Letra de la unidad de Windows.</param>
    /// <remarks>
    /// Va por <see cref="DriveLetter.Same"/>, invariante de cultura: con <c>char.ToUpper</c> y cultura
    /// turca, una <c>i</c> minúscula no se reconocería como la unidad <c>I:</c> (`T1-01`).
    /// </remarks>
    public static bool IsSystemDrive(char letter, char systemLetter) => DriveLetter.Same(letter, systemLetter);

    /// <summary>¿Se puede formatear la unidad seleccionada?</summary>
    /// <param name="letter">Letra de la unidad seleccionada, o <c>null</c> si no hay ninguna.</param>
    /// <param name="isProtected">Si la unidad está marcada como protegida.</param>
    /// <param name="systemLetter">Letra de la unidad de Windows.</param>
    public static GuardRejection CanFormat(char? letter, bool isProtected, char systemLetter)
    {
        if (letter is not char l)             return GuardRejection.NoDrive;
        if (isProtected)                      return GuardRejection.Protected;
        if (IsSystemDrive(l, systemLetter))   return GuardRejection.SystemDrive;
        return GuardRejection.None;
    }

    /// <summary>
    /// ¿Es esta unidad candidata a <i>Reinicializar</i>? Primera mitad de la guarda: lo que se sabe sin
    /// consultar nada. La segunda es <see cref="CanClearDisk"/>.
    /// </summary>
    /// <param name="driveType">Tipo de la unidad.</param>
    /// <param name="letter">Letra de la unidad.</param>
    /// <param name="isProtected">Si la unidad está marcada como protegida.</param>
    /// <param name="systemLetter">Letra de la unidad de Windows.</param>
    public static GuardRejection CanReinit(DriveType driveType, char letter, bool isProtected, char systemLetter)
    {
        if (driveType != DriveType.Removable)     return GuardRejection.NotRemovable;
        if (isProtected)                          return GuardRejection.Protected;
        if (IsSystemDrive(letter, systemLetter))  return GuardRejection.SystemDrive;
        return GuardRejection.None;
    }

    /// <summary>
    /// ¿Se puede borrar <b>entero</b> el disco físico de la unidad? <i>Reinicializar</i> hace
    /// <c>Clear-Disk</c>, que se lleva todas las particiones del disco y no solo la seleccionada, así que
    /// ese disco no puede ser el de Windows.
    /// </summary>
    /// <param name="targetDisk">Número del disco a borrar, o <c>null</c> si no se pudo leer.</param>
    /// <param name="systemDisk">Número del disco de Windows, o <c>null</c> si no se pudo leer.</param>
    /// <remarks>
    /// Los <b>dos</b> números tienen que conocerse. Con el del sistema a <c>null</c> no hay con qué
    /// comparar, y «no sé si es el disco de Windows» no es «no lo es» (`AUD-012`).
    /// </remarks>
    public static GuardRejection CanClearDisk(int? targetDisk, int? systemDisk)
    {
        if (targetDisk is null)       return GuardRejection.TargetDiskUnknown;
        if (systemDisk is null)       return GuardRejection.SystemDiskUnknown;
        if (targetDisk == systemDisk) return GuardRejection.SameDiskAsSystem;
        return GuardRejection.None;
    }

    /// <summary>
    /// Claves de localización —título y cuerpo— del aviso con el que <i>Formatear</i> explica un rechazo.
    /// </summary>
    /// <remarks>
    /// Un motivo que no tenga texto propio cae en el más severo, «no se puede formatear la unidad que
    /// contiene Windows»: si algún día se añade un motivo y se olvida aquí, el resultado es un aviso de
    /// más, nunca una operación de más.
    /// </remarks>
    public static (string TitleKey, string BodyKey) DescribeFormat(GuardRejection rejection) => rejection switch
    {
        GuardRejection.NoDrive   => ("msg.warning", "msg.selectDrive"),
        GuardRejection.Protected => ("msg.protTitle", "msg.protBody"),
        _                        => ("msg.systemTitle", "msg.systemBody"),
    };

    /// <summary>Clave de localización del aviso con el que <i>Reinicializar</i> explica un rechazo.</summary>
    public static string DescribeReinit(GuardRejection rejection) => rejection switch
    {
        GuardRejection.NotRemovable      => "reinit.onlyRemovable",
        GuardRejection.SameDiskAsSystem  => "reinit.sameDisk",
        GuardRejection.TargetDiskUnknown or GuardRejection.SystemDiskUnknown => "reinit.diskUnknown",
        _                                => "reinit.blockedSystem",
    };
}
