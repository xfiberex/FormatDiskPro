namespace FormatDiskPro;

/// <summary>
/// Tamaño de una partición dentro de un <see cref="PartitionPlan"/>. Jerarquía cerrada: solo existen
/// <see cref="Exact"/> y <see cref="Remainder"/>.
/// </summary>
/// <remarks>
/// Sustituye al <c>long?</c> que <see cref="ReinitDrive"/> recibía antes, donde <c>null</c> significaba
/// «todo el disco». Ese tipo no distinguía «el resto» de «no lo sé todavía», y el significado vivía en un
/// comentario en vez de en el tipo. Aquí no hay forma de escribir un plan ambiguo.
/// </remarks>
public abstract record PartitionSize
{
    private PartitionSize() { }

    /// <summary>Tamaño exacto en bytes, tal cual se pasa a <c>New-Partition -Size</c>.</summary>
    public sealed record Exact(long Bytes) : PartitionSize;

    /// <summary>
    /// Todo el espacio libre que quede. Como mucho una por plan, y tiene que ser la última.
    ///
    /// <para>Al ejecutar se delega en <c>New-Partition -UseMaximumSize</c> en vez de calcular los bytes:
    /// calcularlos es pedir un error de alineación, y ese error se descubre con el disco ya borrado. Sí se
    /// calcula al <b>validar</b> (<see cref="PartitionPlan.EffectiveSizes"/>), porque para saber si un
    /// volumen FAT32 cabe en 32 GB hay que conocer su tamaño.</para>
    /// </summary>
    public sealed record Remainder : PartitionSize;
}

/// <summary>Una partición del plan: cuánto ocupa, con qué se formatea y cómo se llama.</summary>
/// <param name="Size">Tamaño exacto o «el resto».</param>
/// <param name="FileSystem">Sistema de archivos destino (uno de <see cref="PartitionPlan.SupportedFileSystems"/>).</param>
/// <param name="Label">Etiqueta de volumen; la cadena vacía es válida.</param>
public sealed record PartitionSpec(PartitionSize Size, string FileSystem, string Label);

/// <summary>Motivo por el que un <see cref="PartitionPlan"/> se rechaza. Es un valor, no un texto: lo que
/// se muestra al usuario se traduce en la capa de UI, y esto se puede comparar en una prueba.</summary>
public enum PlanProblem
{
    /// <summary>El plan es válido.</summary>
    None,
    /// <summary>No se pudo determinar el tamaño del disco: sin él no hay nada que validar.</summary>
    UnknownDiskSize,
    /// <summary>Un plan sin particiones dejaría el disco borrado y vacío.</summary>
    NoPartitions,
    /// <summary>MBR admite 4 particiones primarias como máximo.</summary>
    TooManyForMbr,
    /// <summary>MBR no puede direccionar un disco de más de 2 TB.</summary>
    MbrCannotAddressDisk,
    /// <summary>Sistema de archivos fuera de <see cref="PartitionPlan.SupportedFileSystems"/>.</summary>
    UnknownFileSystem,
    /// <summary>Etiqueta con caracteres prohibidos o más larga de lo que admite su sistema de archivos.</summary>
    InvalidLabel,
    /// <summary>Tamaño exacto de cero o negativo.</summary>
    NonPositiveSize,
    /// <summary>Más de una partición pide «el resto».</summary>
    MultipleRemainders,
    /// <summary>«El resto» no es la última partición del plan.</summary>
    RemainderIsNotLast,
    /// <summary>La suma de los tamaños, más el margen de alineación, no cabe en el disco.</summary>
    DoesNotFit,
    /// <summary>Una partición queda por debajo de <see cref="PartitionPlan.MinPartitionBytes"/>.</summary>
    PartitionTooSmall,
    /// <summary>Un volumen FAT32 supera el límite de Windows (32 GB).</summary>
    Fat32VolumeTooLarge,
    /// <summary>Un volumen FAT supera los 2 GB.</summary>
    FatVolumeTooLarge,
}

/// <summary>Resultado de validar un plan: el motivo y, cuando aplica, qué partición lo provoca.</summary>
/// <param name="Problem">Motivo del rechazo, o <see cref="PlanProblem.None"/>.</param>
/// <param name="PartitionIndex">Índice de la partición culpable, o <c>-1</c> si el problema es del plan
/// entero. Permite que la UI señale la fila concreta en vez de dar un error genérico.</param>
public sealed record PlanValidation(PlanProblem Problem, int PartitionIndex = -1)
{
    /// <summary>El plan se puede ejecutar.</summary>
    public bool Ok => Problem == PlanProblem.None;

    /// <summary>Plan válido, sin partición culpable.</summary>
    public static PlanValidation Valid { get; } = new(PlanProblem.None);
}

/// <summary>
/// El layout que se va a crear sobre un disco recién borrado, como dato puro y validable **antes** de
/// tocar nada.
/// </summary>
/// <remarks>
/// <para><b>Por qué existe.</b> El layout estaba implícito en un <c>long?</c>: «una partición de este
/// tamaño, o todo el disco si es <c>null</c>». Con una sola partición eso se sostenía; con dos deja de
/// sostenerse, y los errores de un layout mal calculado <b>se descubren con el disco ya borrado</b>. Esta
/// es la única parte del tier que se puede probar entera sin hardware, así que es donde tiene que vivir la
/// validación.</para>
///
/// <para><b>Esto no es un gestor de particiones.</b> No redimensiona, no fusiona y no mueve: el disco se
/// está borrando entero de todos modos (<c>Clear-Disk</c>) y lo único que se decide es cuántas particiones
/// se crean sobre el vacío. Si algún día hay que preservar datos, es que nos hemos salido del alcance.</para>
/// </remarks>
/// <param name="Style">Estilo de tabla de particiones, normalmente de <see cref="ReinitPlan.StyleFor"/>.</param>
/// <param name="Partitions">Particiones a crear, en el orden en que se crearán.</param>
public sealed record PartitionPlan(DiskPartitionStyle Style, IReadOnlyList<PartitionSpec> Partitions)
{
    /// <summary>
    /// Máximo de particiones primarias en MBR. No es una elección del proyecto: es el formato.
    ///
    /// <para>Y en la práctica es <b>el</b> tope, porque <see cref="ReinitPlan.StyleFor"/> elige MBR en todo
    /// disco de menos de 2 TB — es decir, en cualquier memoria USB. Esa elección es deliberada: MBR es lo
    /// que hace que el pendrive lo lea el BIOS de una placa base, un televisor o la radio de un coche, que
    /// es el caso de uso que originó todo esto.</para>
    /// </summary>
    public const int MaxMbrPrimaryPartitions = 4;

    /// <summary>
    /// Tamaño mínimo de una partición del plan: 64 MiB.
    ///
    /// <para>Queda por encima del volumen más pequeño que Windows formatea con cualquiera de los sistemas
    /// admitidos (FAT32 necesita ~33 MiB). Sin este suelo, una partición diminuta —típicamente «el resto»
    /// cuando apenas queda nada— pasaría la validación y reventaría en <c>Format-Volume</c>, otra vez con
    /// el disco ya borrado.</para>
    /// </summary>
    public const long MinPartitionBytes = 64L * 1024 * 1024;

    /// <summary>Límite de un volumen FAT (FAT16) en esta aplicación: 2 GB, el mismo umbral con el que el
    /// selector de sistema de archivos ofrece FAT.</summary>
    public const long FatMaxBytes = 2L * 1024 * 1024 * 1024;

    /// <summary>Sistemas de archivos que la aplicación sabe crear. Coincide con el selector de la UI.</summary>
    public static readonly string[] SupportedFileSystems = ["NTFS", "exFAT", "ReFS", "FAT32", "FAT"];

    /// <summary>
    /// Sistemas de archivos que se ofrecen para la <b>segunda</b> partición (el espacio sobrante de una
    /// FAT32 pequeña). Deliberadamente más corto que <see cref="SupportedFileSystems"/>.
    /// </summary>
    /// <remarks>
    /// <b>FAT32 queda fuera</b>: el sobrante de un pendrive grande supera los 32 GB que admite Windows, así
    /// que ofrecerlo sería ofrecer un fallo — uno que además llegaría con el disco ya borrado. <b>FAT</b>
    /// queda fuera por lo mismo, con un techo aún más bajo (2 GB). Y <b>ReFS</b> porque no es un sistema
    /// para memorias extraíbles. Quedan los dos que tienen sentido, con exFAT primero por ser el que mejor
    /// se lleva con este tipo de unidades y no tener el límite de 4 GB por archivo.
    /// </remarks>
    public static readonly string[] SecondPartitionFileSystems = ["exFAT", "NTFS"];

    /// <summary>Ajusta un sistema de archivos al conjunto de <see cref="SecondPartitionFileSystems"/>,
    /// cayendo al primero (exFAT) si no está. Protege de un <c>settings.json</c> editado a mano.</summary>
    /// <param name="fs">Sistema de archivos a normalizar.</param>
    public static string NormalizeSecondPartitionFileSystem(string? fs)
        => fs is not null && SecondPartitionFileSystems.Contains(fs, StringComparer.Ordinal)
            ? fs
            : SecondPartitionFileSystems[0];

    /// <summary>Plan de una sola partición que ocupa todo el disco: el comportamiento histórico de
    /// <em>Reinicializar unidad</em> sin la opción de FAT32 pequeña.</summary>
    /// <param name="style">Estilo de tabla de particiones.</param>
    /// <param name="fileSystem">Sistema de archivos destino.</param>
    /// <param name="label">Etiqueta de volumen.</param>
    public static PartitionPlan WholeDisk(DiskPartitionStyle style, string fileSystem, string label)
        => new(style, [new PartitionSpec(new PartitionSize.Remainder(), fileSystem, label)]);

    /// <summary>
    /// Tamaño real que tendrá cada partición sobre un disco de <paramref name="diskSizeBytes"/>, con «el
    /// resto» ya resuelto. Puede devolver un valor negativo si el plan no cabe — de eso se ocupa
    /// <see cref="Validate"/>, que es quien debe llamarse antes.
    /// </summary>
    /// <remarks>
    /// Se reserva <see cref="ReinitPlan.PartitionReserveBytes"/> <b>por partición</b>: cada una alinea su
    /// inicio, y la tabla (su copia final, en GPT) también ocupa. Es una reserva generosa a propósito —
    /// quedarse corto significa fallar con el disco borrado, y pasarse solo significa unos MiB sin usar.
    /// </remarks>
    /// <param name="diskSizeBytes">Tamaño total del disco físico.</param>
    public long[] EffectiveSizes(long diskSizeBytes)
    {
        long reserved   = ReinitPlan.PartitionReserveBytes * Partitions.Count;
        long fixedTotal = Partitions.Sum(p => p.Size is PartitionSize.Exact e ? e.Bytes : 0L);
        long remainder  = diskSizeBytes - reserved - fixedTotal;

        return [.. Partitions.Select(p => p.Size is PartitionSize.Exact e ? e.Bytes : remainder)];
    }

    /// <summary>
    /// Comprueba que el plan se puede ejecutar sobre un disco de <paramref name="diskSizeBytes"/>. Lógica
    /// pura: no lanza ningún proceso y no toca ningún disco.
    /// </summary>
    /// <remarks>
    /// El orden de las comprobaciones va de lo estructural (¿tiene sentido el plan?) a lo dimensional
    /// (¿cabe en este disco?), para que el motivo devuelto sea el más explicativo y no el primero que
    /// salte. Un plan sin particiones se rechaza por eso y no por «no cabe».
    /// </remarks>
    /// <param name="diskSizeBytes">Tamaño total del disco físico, en bytes.</param>
    public PlanValidation Validate(long diskSizeBytes)
    {
        if (Partitions.Count == 0) return new PlanValidation(PlanProblem.NoPartitions);

        if (Style == DiskPartitionStyle.Mbr && Partitions.Count > MaxMbrPrimaryPartitions)
            return new PlanValidation(PlanProblem.TooManyForMbr);

        for (int i = 0; i < Partitions.Count; i++)
        {
            PartitionSpec p = Partitions[i];

            if (!SupportedFileSystems.Contains(p.FileSystem, StringComparer.Ordinal))
                return new PlanValidation(PlanProblem.UnknownFileSystem, i);

            if (FormatLogic.ValidateLabel(p.Label, p.FileSystem) != FormatLogic.LabelValidation.Ok)
                return new PlanValidation(PlanProblem.InvalidLabel, i);

            if (p.Size is PartitionSize.Exact e && e.Bytes <= 0)
                return new PlanValidation(PlanProblem.NonPositiveSize, i);
        }

        // Las reglas de «el resto» se miran sobre el plan entero, no partición a partición. Dentro del
        // bucle, un plan con DOS «resto» se rechazaba por «no es la última» —cierto, pero no es el
        // problema—: el motivo devuelto acaba en un mensaje al usuario, y conviene que sea el que explica.
        int[] remainders = [.. Enumerable.Range(0, Partitions.Count)
            .Where(i => Partitions[i].Size is PartitionSize.Remainder)];

        if (remainders.Length > 1)
            return new PlanValidation(PlanProblem.MultipleRemainders, remainders[1]);

        if (remainders.Length == 1 && remainders[0] != Partitions.Count - 1)
            return new PlanValidation(PlanProblem.RemainderIsNotLast, remainders[0]);

        // Sin tamaño de disco no se pueden hacer las comprobaciones dimensionales. Eso NO es motivo para
        // rechazar un plan que no las necesita: una sola partición «el resto» la resuelve Windows con
        // -UseMaximumSize sin que nadie calcule nada. Exigir el tamaño siempre bloquearía justo el caso
        // para el que existe Reinicializar —un USB en RAW, donde Get-Disk puede no devolver nada— y sería
        // una regresión frente al comportamiento anterior a que el plan existiera.
        if (diskSizeBytes <= 0)
        {
            bool needsSize = Partitions.Count != 1 || Partitions[0].Size is PartitionSize.Exact;
            return needsSize ? new PlanValidation(PlanProblem.UnknownDiskSize) : PlanValidation.Valid;
        }

        if (Style == DiskPartitionStyle.Mbr && diskSizeBytes > ReinitPlan.MbrLimitBytes)
            return new PlanValidation(PlanProblem.MbrCannotAddressDisk);

        // Se mide sobre la parte de tamaño fijo: con un «resto» presente, la suma de los tamaños efectivos
        // da el disco entero por construcción y no diría nunca que no cabe.
        long reserved   = ReinitPlan.PartitionReserveBytes * Partitions.Count;
        long fixedTotal = Partitions.Sum(p => p.Size is PartitionSize.Exact e ? e.Bytes : 0L);
        if (fixedTotal + reserved > diskSizeBytes) return new PlanValidation(PlanProblem.DoesNotFit);

        long[] sizes = EffectiveSizes(diskSizeBytes);
        for (int i = 0; i < sizes.Length; i++)
        {
            if (sizes[i] < MinPartitionBytes) return new PlanValidation(PlanProblem.PartitionTooSmall, i);

            // El límite es del VOLUMEN, no del disco, y por eso hace falta el tamaño resuelto de «el resto»:
            // pedir el resto de un pendrive de 256 GB en FAT32 es un plan inválido que Windows rechazaría
            // a mitad de la operación.
            switch (Partitions[i].FileSystem)
            {
                case "FAT32" when sizes[i] > FormatLogic.Fat32MaxBytes:
                    return new PlanValidation(PlanProblem.Fat32VolumeTooLarge, i);
                case "FAT" when sizes[i] > FatMaxBytes:
                    return new PlanValidation(PlanProblem.FatVolumeTooLarge, i);
            }
        }

        return PlanValidation.Valid;
    }
}
