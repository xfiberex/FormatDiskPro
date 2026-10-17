// SPDX-License-Identifier: GPL-3.0-only
using Xunit;

namespace FormatDiskPro.Tests;

/// <summary>
/// Las guardas que deciden si una operación destructiva procede, ejercidas por el lado que importa: el
/// <b>rechazo</b> (`AUD-015`). Hasta aquí vivían en los manejadores de la ventana y ninguna prueba las
/// hacía decir que no; la única forma de comprobarlas era intentar formatear el disco de Windows.
/// </summary>
public sealed class DestructiveGuardTests
{
    // ── CanFormat ────────────────────────────────────────────────

    [Fact]
    public void CanFormat_NoDriveSelected_IsRejected()
        => Assert.Equal(GuardRejection.NoDrive, DestructiveGuard.CanFormat(null, isProtected: false, 'C'));

    [Fact]
    public void CanFormat_ProtectedDrive_IsRejected()
        => Assert.Equal(GuardRejection.Protected, DestructiveGuard.CanFormat('D', isProtected: true, 'C'));

    /// <summary>
    /// La unidad de Windows se rechaza <b>aunque no venga marcada como protegida</b>: la marca la pone la
    /// ventana al cargar la lista, y esta comprobación no puede depender de que la haya puesto bien.
    /// </summary>
    [Theory]
    [InlineData('C', 'C')]
    [InlineData('c', 'C')]
    [InlineData('C', 'c')]
    [InlineData('i', 'I')]   // la «I turca»: char.ToUpper('i') en tr-TR no da 'I' (`T1-01`)
    public void CanFormat_SystemDrive_IsRejectedWhateverTheCase(char letter, char systemLetter)
        => Assert.Equal(GuardRejection.SystemDrive, DestructiveGuard.CanFormat(letter, isProtected: false, systemLetter));

    [Fact]
    public void CanFormat_AnyOtherDrive_Proceeds()
        => Assert.Equal(GuardRejection.None, DestructiveGuard.CanFormat('G', isProtected: false, 'C'));

    // ── CanReinit ────────────────────────────────────────────────

    /// <summary>Reinicializar borra el disco entero: solo sobre extraíbles, que es su caso de uso.</summary>
    [Theory]
    [InlineData(DriveType.Fixed)]
    [InlineData(DriveType.Ram)]
    [InlineData(DriveType.Network)]
    [InlineData(DriveType.CDRom)]
    [InlineData(DriveType.Unknown)]   // lo que deja la ventana si no pudo leer el tipo
    [InlineData(DriveType.NoRootDirectory)]
    public void CanReinit_AnythingButRemovable_IsRejected(DriveType type)
        => Assert.Equal(GuardRejection.NotRemovable, DestructiveGuard.CanReinit(type, 'G', isProtected: false, 'C'));

    [Fact]
    public void CanReinit_ProtectedRemovable_IsRejected()
        => Assert.Equal(GuardRejection.Protected,
            DestructiveGuard.CanReinit(DriveType.Removable, 'G', isProtected: true, 'C'));

    /// <summary>Windows To Go y similares: una extraíble puede ser la unidad del sistema.</summary>
    [Fact]
    public void CanReinit_RemovableThatHoldsWindows_IsRejected()
        => Assert.Equal(GuardRejection.SystemDrive,
            DestructiveGuard.CanReinit(DriveType.Removable, 'c', isProtected: false, 'C'));

    [Fact]
    public void CanReinit_PlainRemovable_Proceeds()
        => Assert.Equal(GuardRejection.None,
            DestructiveGuard.CanReinit(DriveType.Removable, 'G', isProtected: false, 'C'));

    // ── CanClearDisk ─────────────────────────────────────────────

    /// <summary>
    /// El caso de `AUD-012`: si no se puede leer el disco de Windows, <b>no se borra nada</b>. La guarda
    /// anterior era <c>targetDisk is null || (sysDisk is not null &amp;&amp; targetDisk == sysDisk)</c>, que con
    /// <c>(3, null)</c> dejaba pasar: fallaba abierta justo cuando menos sabía.
    /// </summary>
    [Fact]
    public void CanClearDisk_SystemDiskUnknown_IsRejected()
        => Assert.Equal(GuardRejection.SystemDiskUnknown, DestructiveGuard.CanClearDisk(targetDisk: 3, systemDisk: null));

    [Fact]
    public void CanClearDisk_TargetDiskUnknown_IsRejected()
        => Assert.Equal(GuardRejection.TargetDiskUnknown, DestructiveGuard.CanClearDisk(targetDisk: null, systemDisk: 0));

    [Fact]
    public void CanClearDisk_BothUnknown_IsRejected()
        => Assert.NotEqual(GuardRejection.None, DestructiveGuard.CanClearDisk(targetDisk: null, systemDisk: null));

    /// <summary>Otra partición del disco de Windows: <c>Clear-Disk</c> se llevaría también el sistema.</summary>
    [Theory]
    [InlineData(0)]
    [InlineData(2)]
    public void CanClearDisk_SameDiskAsWindows_IsRejected(int disk)
        => Assert.Equal(GuardRejection.SameDiskAsSystem, DestructiveGuard.CanClearDisk(disk, disk));

    [Theory]
    [InlineData(1, 0)]
    [InlineData(0, 1)]   // Windows no siempre está en el disco 0
    public void CanClearDisk_DifferentKnownDisks_Proceeds(int targetDisk, int systemDisk)
        => Assert.Equal(GuardRejection.None, DestructiveGuard.CanClearDisk(targetDisk, systemDisk));

    /// <summary>
    /// Red contra el descuido que originó `AUD-012`: con cualquiera de los dos datos sin conocer, la
    /// respuesta <b>nunca</b> es «procede», se añadan los motivos que se añadan.
    /// </summary>
    [Theory]
    [InlineData(null, null)]
    [InlineData(null, 0)]
    [InlineData(0, null)]
    [InlineData(7, null)]
    public void CanClearDisk_NeverProceedsOnMissingFacts(int? targetDisk, int? systemDisk)
        => Assert.NotEqual(GuardRejection.None, DestructiveGuard.CanClearDisk(targetDisk, systemDisk));

    // ── El texto de cada rechazo ─────────────────────────────────

    public static TheoryData<GuardRejection> EveryRejection()
    {
        var data = new TheoryData<GuardRejection>();
        foreach (var r in Enum.GetValues<GuardRejection>())
            if (r != GuardRejection.None) data.Add(r);
        return data;
    }

    /// <summary>
    /// Todo rechazo llega a la pantalla con un texto que existe. <c>L.T</c> devuelve la propia clave cuando
    /// no la encuentra, así que una clave mal escrita enseñaría «reinit.diskUnknown» en un diálogo.
    /// </summary>
    [Theory]
    [MemberData(nameof(EveryRejection))]
    public void EveryRejection_HasATextInBothOperations(GuardRejection rejection)
    {
        var (title, body) = DestructiveGuard.DescribeFormat(rejection);
        string reinit = DestructiveGuard.DescribeReinit(rejection);

        Assert.NotEqual(title, L.T(title));
        Assert.NotEqual(body, L.T(body));
        Assert.NotEqual(reinit, L.T(reinit));
    }

    /// <summary>
    /// «No se pudo comprobar» no puede decirse con la frase de «comparte disco con Windows»: afirmaría
    /// algo que no se sabe. Era lo que ocurría con el disco de destino sin leer.
    /// </summary>
    [Theory]
    [InlineData(GuardRejection.TargetDiskUnknown)]
    [InlineData(GuardRejection.SystemDiskUnknown)]
    public void UnknownDisk_IsNotReportedAsSharingTheSystemDisk(GuardRejection rejection)
        => Assert.NotEqual(
            DestructiveGuard.DescribeReinit(GuardRejection.SameDiskAsSystem),
            DestructiveGuard.DescribeReinit(rejection));
}
