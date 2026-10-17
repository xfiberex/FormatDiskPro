// SPDX-License-Identifier: GPL-3.0-only
using Xunit;

namespace FormatDiskPro.Tests;

/// <summary>
/// Qué hay detrás de una letra, y cuándo deja de ser lo mismo (`AUD-013`). Una operación destructiva se
/// confirma sobre una letra y se ejecuta un rato después; estas pruebas fijan qué cuenta como «sigue
/// siendo lo que se confirmó».
/// </summary>
public sealed class DriveIdentityTests
{
    /// <summary>Línea real de la USB de pruebas, tal como la escribe la consulta (2026-10-13).</summary>
    private const string UsbLine =
        @"1|31457280000|USB|False|False|1|1048576|1073741824|0758951061857004219|USBSTOR\DISK&VEN_VENDORCO&PROD_PRODUCTCODE&REV_2.00\0758951061857004219&0:RAJB-Laptop";

    /// <summary>Línea real del disco de Windows del mismo equipo.</summary>
    private const string SystemLine =
        "0|256060514304|NVMe|True|True|3|122683392|254991663104|0025_38E1_31C8_823C.|eui.002538E131C8823C";

    private static DriveIdentity Usb => DriveIdentity.Parse(UsbLine)!;

    // ── Parse ────────────────────────────────────────────────────

    [Fact]
    public void Parse_RealUsbLine_ReadsEveryField()
    {
        DriveIdentity id = Assert.IsType<DriveIdentity>(DriveIdentity.Parse(UsbLine));

        Assert.Equal(1, id.DiskNumber);
        Assert.Equal(31_457_280_000, id.DiskSize);
        Assert.Equal("USB", id.BusType);
        Assert.False(id.IsSystem);
        Assert.False(id.IsBoot);
        Assert.Equal(1, id.PartitionNumber);
        Assert.Equal(1_048_576, id.PartitionOffset);
        Assert.Equal(1_073_741_824, id.PartitionSize);
        Assert.Equal("0758951061857004219", id.DiskSerial);
        Assert.Equal(@"USBSTOR\DISK&VEN_VENDORCO&PROD_PRODUCTCODE&REV_2.00\0758951061857004219&0:RAJB-Laptop", id.DiskUniqueId);
    }

    [Fact]
    public void Parse_RealSystemDiskLine_KnowsItIsSystemAndBoot()
    {
        DriveIdentity id = Assert.IsType<DriveIdentity>(DriveIdentity.Parse(SystemLine));

        Assert.True(id.IsSystem);
        Assert.True(id.IsBoot);
        Assert.Equal("NVMe", id.BusType);
    }

    /// <summary>PowerShell termina la línea con un salto, y a veces la precede de otro.</summary>
    [Fact]
    public void Parse_ToleratesSurroundingWhitespace()
        => Assert.True(Usb.SameAs(DriveIdentity.Parse("\r\n" + UsbLine + "\r\n")));

    /// <summary>
    /// El identificador único es de formato libre: si llevara una barra, tiene que quedarse entera dentro
    /// de él y no descolocar los nueve campos anteriores.
    /// </summary>
    [Fact]
    public void Parse_PipeInsideTheUniqueId_StaysInTheUniqueId()
    {
        DriveIdentity id = Assert.IsType<DriveIdentity>(
            DriveIdentity.Parse("2|1000|USB|False|False|1|0|900|SER|raro|con|barras"));

        Assert.Equal("SER", id.DiskSerial);
        Assert.Equal("raro|con|barras", id.DiskUniqueId);
    }

    /// <summary>
    /// Lo que no es una identidad completa no es una identidad: nada de rellenar con ceros. Una salida
    /// vacía es lo que devuelve la consulta cuando la letra no tiene partición detrás.
    /// </summary>
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   \r\n")]
    [InlineData("1|31457280000|USB|False|False|1|1048576")]                    // faltan campos
    [InlineData("x|31457280000|USB|False|False|1|1048576|1073741824|S|U")]     // número de disco
    [InlineData("1|0|USB|False|False|1|1048576|1073741824|S|U")]               // disco de tamaño cero
    [InlineData("1|-5|USB|False|False|1|1048576|1073741824|S|U")]              // tamaño negativo
    [InlineData("1|31457280000|USB|quizá|False|1|1048576|1073741824|S|U")]     // booleano que no lo es
    [InlineData("1|31457280000|USB|False||1|1048576|1073741824|S|U")]          // booleano vacío
    [InlineData("1|31457280000|USB|False|False|uno|1048576|1073741824|S|U")]   // número de partición
    [InlineData("Get-Partition : No se encontraron objetos MSFT_Partition")]   // un error, no una línea
    public void Parse_AnythingElse_IsNull(string? line)
        => Assert.Null(DriveIdentity.Parse(line));

    // ── SameAs ───────────────────────────────────────────────────

    [Fact]
    public void SameAs_TwoReadingsOfTheSameDrive_Match()
        => Assert.True(Usb.SameAs(DriveIdentity.Parse(UsbLine)));

    [Fact]
    public void SameAs_Null_IsFalse()
        => Assert.False(Usb.SameAs(null));

    /// <summary>Windows no garantiza la caja de estos textos entre dos consultas; el contenido, sí.</summary>
    [Fact]
    public void SameAs_IgnoresCaseInTexts()
        => Assert.True(Usb.SameAs(Usb with
        {
            BusType      = "usb",
            DiskSerial   = Usb.DiskSerial.ToLowerInvariant(),
            DiskUniqueId = Usb.DiskUniqueId.ToLowerInvariant(),
        }));

    public static TheoryData<string, DriveIdentity> OneFieldDifferent() => new()
    {
        { "otro número de disco",          Usb with { DiskNumber = 2 } },
        { "otro tamaño de disco",          Usb with { DiskSize = Usb.DiskSize + 512 } },
        { "otro bus",                      Usb with { BusType = "SATA" } },
        { "pasa a ser de sistema",         Usb with { IsSystem = true } },
        { "pasa a ser de arranque",        Usb with { IsBoot = true } },
        { "otra partición",                Usb with { PartitionNumber = 2 } },
        { "la partición empieza en otro sitio", Usb with { PartitionOffset = 2_097_152 } },
        { "la partición mide otra cosa",   Usb with { PartitionSize = Usb.PartitionSize - 4096 } },
        { "otro número de serie",          Usb with { DiskSerial = "0758951061857004220" } },
        { "otro identificador único",      Usb with { DiskUniqueId = Usb.DiskUniqueId + "X" } },
    };

    /// <summary>
    /// Basta <b>un</b> dato distinto. Es estricto a propósito: el caso que importa es el de dos pendrives
    /// del mismo modelo —mismo bus, mismo tamaño, la misma letra—, que solo se distinguen por el número
    /// de serie; y el contrario, el mismo disco al que le han cambiado las particiones.
    /// </summary>
    [Theory]
    [MemberData(nameof(OneFieldDifferent))]
    public void SameAs_AnySingleDifference_IsNotTheSame(string what, DriveIdentity other)
    {
        Assert.False(Usb.SameAs(other), what);
        Assert.False(other.SameAs(Usb), what);
    }

    [Fact]
    public void SameAs_TheSystemDisk_IsNotTheUsb()
        => Assert.False(Usb.SameAs(DriveIdentity.Parse(SystemLine)));

    // ── La decisión: ¿sigue siendo lo que se confirmó? ───────────

    [Fact]
    public void StillTheConfirmedTarget_SameDrive_Proceeds()
        => Assert.Equal(GuardRejection.None,
            DestructiveGuard.StillTheConfirmedTarget(Usb, DriveIdentity.Parse(UsbLine)));

    [Theory]
    [MemberData(nameof(OneFieldDifferent))]
    public void StillTheConfirmedTarget_AnythingDifferent_IsRejected(string what, DriveIdentity now)
        => Assert.True(GuardRejection.TargetChanged == DestructiveGuard.StillTheConfirmedTarget(Usb, now), what);

    /// <summary>Se confirmó una unidad y ya no hay nada que leer detrás de la letra: se retiró.</summary>
    [Fact]
    public void StillTheConfirmedTarget_DriveGone_IsRejected()
        => Assert.Equal(GuardRejection.TargetChanged, DestructiveGuard.StillTheConfirmedTarget(Usb, null));

    /// <summary>Al confirmar no había nada que leer y ahora sí: ha aparecido otra cosa en esa letra.</summary>
    [Fact]
    public void StillTheConfirmedTarget_SomethingAppeared_IsRejected()
        => Assert.Equal(GuardRejection.TargetChanged, DestructiveGuard.StillTheConfirmedTarget(null, Usb));

    /// <summary>
    /// Sin identidad ni antes ni después —un disco RAM, una unidad virtual sin partición que consultar—
    /// se sigue: son volúmenes que la app formatea desde siempre. Queda fijado para que sea una decisión
    /// y no un descuido; <i>Reinicializar</i> no llega aquí sin conocer el disco.
    /// </summary>
    [Fact]
    public void StillTheConfirmedTarget_NeverReadable_Proceeds()
        => Assert.Equal(GuardRejection.None, DestructiveGuard.StillTheConfirmedTarget(null, null));
}
