namespace FormatDiskPro;

/// <summary>
/// Convierte el rechazo de un <see cref="PartitionPlan"/> en <b>qué decirle a quien lo pidió</b>: la clave
/// del texto y los valores que van dentro (límites y tamaños, ya formateados).
/// </summary>
/// <remarks>
/// <para><b>Por qué existe</b> (`T13-18`). <c>Reinicializar</c> validaba el plan y, si no pasaba, enseñaba
/// siempre la misma frase: «La distribución de particiones pedida no es válida para este disco». El motivo
/// exacto iba al historial y no a la pantalla, así que quien lo leía no sabía qué cambiar. El caso que lo
/// destapó es corriente: con un volumen de 28,5 GB elegido, el selector sugiere <b>FAT32</b> —decide por el
/// tamaño del volumen—, pero <c>Reinicializar</c> trabaja sobre el <b>disco entero</b>, y 59,8 GB en FAT32
/// no es un plan válido. La salida —exFAT, NTFS o la FAT32 pequeña— estaba a un clic y no se decía.</para>
///
/// <para><b>Por qué aquí y no en la UI.</b> Esto es una tabla de decisiones sobre un valor, sin efectos:
/// se prueba entera sin hardware y sin ventana. La UI solo traduce la clave y la muestra.</para>
///
/// <para><b>Qué NO lleva texto propio.</b> Cinco motivos no los puede producir este formulario —no hay
/// forma de pedir cero particiones, ni dos «restos», ni un sistema de archivos que no esté en el selector—.
/// Si alguno apareciera sería un fallo de la aplicación, no algo que el usuario pueda arreglar cambiando
/// una opción, y escribirle una instrucción sería mentirle: se quedan con el texto genérico. Cuáles son
/// está escrito en <see cref="FromTheForm"/>, y una prueba comprueba que los demás sí dicen qué cambiar.</para>
/// </remarks>
public static class PlanRejection
{
    /// <summary>Texto genérico de toda la vida: «no es válida para este disco, no se ha modificado nada».</summary>
    public const string GenericKey = "reinit.invalidPlan";

    /// <summary>
    /// Los motivos que el formulario de <c>Reinicializar</c> <b>sí</b> puede provocar, y que por tanto
    /// tienen que decir qué cambiar. Es la lista que recorre la prueba.
    /// </summary>
    public static readonly IReadOnlyList<PlanProblem> FromTheForm =
    [
        PlanProblem.UnknownDiskSize,      // un USB en RAW puede no dar su tamaño
        PlanProblem.MbrCannotAddressDisk, // un disco de más de 2 TB
        PlanProblem.InvalidLabel,         // la escribe el usuario
        PlanProblem.DoesNotFit,           // FAT32 pequeña más grande que el disco
        PlanProblem.PartitionTooSmall,    // el sobrante, cuando la FAT32 pequeña casi llena el disco
        PlanProblem.Fat32VolumeTooLarge,  // el caso que destapó todo esto
        PlanProblem.FatVolumeTooLarge,
    ];

    /// <summary>
    /// Clave del mensaje y sus argumentos para un plan rechazado.
    /// </summary>
    /// <param name="validation">Lo que devolvió <see cref="PartitionPlan.Validate"/>.</param>
    /// <param name="plan">El plan rechazado; de él sale el tamaño del volumen culpable.</param>
    /// <param name="diskSizeBytes">Tamaño del disco, o <c>0</c> si no se pudo leer.</param>
    /// <returns>La clave de <c>Localization</c> y los valores que van en sus <c>{0}</c>.</returns>
    public static (string Key, object[] Args) Describe(PlanValidation validation, PartitionPlan plan, long diskSizeBytes)
    {
        long culprit = CulpritBytes(validation, plan, diskSizeBytes);

        return validation.Problem switch
        {
            PlanProblem.UnknownDiskSize      => ("reinit.plan.unknownSize", []),
            PlanProblem.MbrCannotAddressDisk => ("reinit.plan.mbrTooBig",
                                                 [FormatLogic.FormatBytes(ReinitPlan.MbrLimitBytes),
                                                  FormatLogic.FormatBytes(diskSizeBytes)]),
            PlanProblem.InvalidLabel         => ("reinit.plan.invalidLabel", []),
            PlanProblem.DoesNotFit           => ("reinit.plan.doesNotFit",
                                                 [FormatLogic.FormatBytes(diskSizeBytes)]),
            PlanProblem.PartitionTooSmall    => ("reinit.plan.tooSmall",
                                                 [FormatLogic.FormatBytes(PartitionPlan.MinPartitionBytes)]),
            PlanProblem.Fat32VolumeTooLarge  => ("reinit.plan.fat32TooLarge",
                                                 [FormatLogic.FormatBytes(FormatLogic.Fat32MaxBytes),
                                                  FormatLogic.FormatBytes(culprit)]),
            PlanProblem.FatVolumeTooLarge    => ("reinit.plan.fatTooLarge",
                                                 [FormatLogic.FormatBytes(PartitionPlan.FatMaxBytes),
                                                  FormatLogic.FormatBytes(culprit)]),

            // Los cinco que el formulario no puede producir, y el caso imposible (`None`).
            _ => (GenericKey, []),
        };
    }

    /// <summary>
    /// Tamaño del volumen que provocó el rechazo, o <c>0</c> si el problema no señala a ninguno. Se toma de
    /// <see cref="PartitionPlan.EffectiveSizes"/> —el mismo cálculo que validó el plan— y no de una cuenta
    /// aparte que pudiera discrepar de él.
    /// </summary>
    private static long CulpritBytes(PlanValidation validation, PartitionPlan plan, long diskSizeBytes)
    {
        if (validation.PartitionIndex < 0 || diskSizeBytes <= 0) return 0;

        long[] sizes = plan.EffectiveSizes(diskSizeBytes);
        return validation.PartitionIndex < sizes.Length ? sizes[validation.PartitionIndex] : 0;
    }
}
