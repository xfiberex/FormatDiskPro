using FormatDiskPro;
using Xunit;

namespace FormatDiskPro.Tests;

/// <summary>
/// Pruebas del parseo de la línea de salud S.M.A.R.T.: caso completo, caso USB sin contadores
/// de fiabilidad, valores no numéricos y líneas inválidas.
/// </summary>
public sealed class SmartInfoTests
{
    [Fact]
    public void Parse_FullLine_ReadsAllFields()
    {
        var s = SmartInfo.Parse("Healthy|NVMe|SSD|0|35|1200|2|0|1");
        Assert.NotNull(s);
        Assert.Equal("Healthy", s!.Health);
        Assert.Equal("NVMe", s.Bus);
        Assert.Equal("SSD", s.Media);
        Assert.Equal(0u, s.SpindleSpeedRpm);
        Assert.Equal(35, s.TemperatureC);
        Assert.Equal(1200L, s.PowerOnHours);
        Assert.Equal(2, s.WearPercent);
        Assert.Equal(0L, s.ReadErrors);
        Assert.Equal(1L, s.WriteErrors);
    }

    [Fact]
    public void Parse_UsbWithoutReliabilityCounters_LeavesNumericNull()
    {
        // El disco existe (Health/Bus/Media) pero Get-StorageReliabilityCounter no devolvió nada.
        var s = SmartInfo.Parse(string.Join("|", "Healthy", "USB", "Unspecified", "", "", "", "", "", ""));
        Assert.NotNull(s);
        Assert.Equal("USB", s!.Bus);
        Assert.Null(s.SpindleSpeedRpm);
        Assert.Null(s.TemperatureC);
        Assert.Null(s.PowerOnHours);
        Assert.Null(s.WearPercent);
        Assert.Null(s.ReadErrors);
        Assert.Null(s.WriteErrors);
    }

    /// <summary>
    /// `T6-03`: la fila «Velocidad de rotación» mostraba el literal «SSD» —una velocidad cuyo valor era un
    /// tipo de medio— con «Tipo de medio: SSD» justo encima. Ahora la fila solo se pinta si hay eje, y
    /// quién lo decide es esta función pura.
    /// </summary>
    [Theory]
    // RPM = 0 es el disco diciendo explícitamente «no giro»: manda sobre el tipo de medio, incluso si
    // este viniera mal informado.
    [InlineData("Healthy|NVMe|SSD|0|||||",           false)]
    [InlineData("Healthy|SATA|HDD|0|||||",           false)]
    // Con RPM reales, gira — aunque el medio venga sin especificar.
    [InlineData("Healthy|SATA|HDD|7200|||||",        true)]
    [InlineData("Healthy|SATA|Unspecified|5400|||||", true)]
    // Sin RPM, decide el medio.
    [InlineData("Healthy|USB|SSD||||||",             false)]
    [InlineData("Healthy|USB|Unspecified||||||",     true)]
    [InlineData("Healthy|USB|HDD||||||",             true)]
    public void HasSpindle_DecidesWhetherTheRowMakesSense(string line, bool expected)
        => Assert.Equal(expected, SmartInfo.HasSpindle(SmartInfo.Parse(line)));

    /// <summary>
    /// Sin información no se esconde la fila: esconderla afirmaría que es de estado sólido, y eso no se
    /// sabe. La interfaz la muestra como «no disponible», que es lo que de verdad ocurre.
    /// </summary>
    [Fact]
    public void HasSpindle_WithoutAnySignal_AssumesItSpins()
        => Assert.True(SmartInfo.HasSpindle(SmartInfo.Parse("?|?|?||||||")));

    /// <summary>Sin disco no hay fila que pintar: no debe lanzar.</summary>
    [Fact]
    public void HasSpindle_WithoutDisk_IsFalse()
        => Assert.False(SmartInfo.HasSpindle(null));

    /// <summary>
    /// `T6-04`: «32161 h» no responde a la pregunta que hace esa fila («¿cuánto ha vivido este disco?»).
    /// La unidad se elige por tramos para que el número tenga magnitud útil, con los cortes a DOS
    /// unidades y no a una: con 33 días se dice «33,5 días», no «1,1 meses».
    /// </summary>
    [Theory]
    [InlineData(null,   SmartInfo.PowerOnUnit.None,   0.0)]   // el disco no lo reporta
    [InlineData(0L,     SmartInfo.PowerOnUnit.None,   0.0)]
    [InlineData(23L,    SmartInfo.PowerOnUnit.None,   0.0)]   // menos de un día: las horas ya se leen
    [InlineData(24L,    SmartInfo.PowerOnUnit.Days,   1.0)]
    [InlineData(804L,   SmartInfo.PowerOnUnit.Days,   33.5)]  // 33,5 días, no «1,1 meses»
    [InlineData(1461L,  SmartInfo.PowerOnUnit.Months, 2.0)]   // dos meses exactos: cambia de tramo
    [InlineData(8766L,  SmartInfo.PowerOnUnit.Months, 12.0)]  // un año se sigue diciendo en meses
    [InlineData(17532L, SmartInfo.PowerOnUnit.Years,  2.0)]   // dos años exactos: cambia de tramo
    [InlineData(32161L, SmartInfo.PowerOnUnit.Years,  3.7)]   // el disco real que destapó el fallo
    public void PowerOnEquivalent_PicksAUnitWithUsefulMagnitude(
        long? hours, SmartInfo.PowerOnUnit unit, double value)
    {
        var span = SmartInfo.PowerOnEquivalent(hours);
        Assert.Equal(unit, span.Unit);
        Assert.Equal(value, span.Value, precision: 1);
    }

    /// <summary>
    /// Un contador corrupto no puede acabar en «≈ -0,4 años». Se trata como «nada que traducir», igual
    /// que un valor demasiado pequeño: la fila enseñará las horas crudas, que es la verdad.
    /// </summary>
    [Fact]
    public void PowerOnEquivalent_NegativeHours_HaveNoEquivalent()
        => Assert.Equal(SmartInfo.PowerOnUnit.None, SmartInfo.PowerOnEquivalent(-3000).Unit);

    /// <summary>
    /// Siempre un decimal, y de ahí que no haya que pluralizar: «1,0 años» concuerda en los cinco
    /// idiomas, «1 años» no. Si alguien "simplifica" a entero, esto lo caza.
    /// </summary>
    [Fact]
    public void PowerOnEquivalent_KeepsOneDecimal()
        => Assert.Equal(3.7, SmartInfo.PowerOnEquivalent(32161).Value, precision: 1);

    [Fact]
    public void Parse_NonNumericValues_BecomeNull()
    {
        var s = SmartInfo.Parse("Healthy|SATA|HDD|7200|abc|xyz|||");
        Assert.NotNull(s);
        Assert.Equal(7200u, s!.SpindleSpeedRpm);
        Assert.Null(s.TemperatureC);
        Assert.Null(s.PowerOnHours);
    }

    [Fact]
    public void Parse_MissingTrailingFields_ParsesWhatExists()
    {
        var s = SmartInfo.Parse("Healthy|SATA|HDD");
        Assert.NotNull(s);
        Assert.Equal("HDD", s!.Media);
        Assert.Null(s.TemperatureC);
        Assert.Null(s.WriteErrors);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("sin separadores")]
    public void Parse_InvalidLine_ReturnsNull(string line)
        => Assert.Null(SmartInfo.Parse(line));

    [Fact]
    public void Parse_EmptyHealth_FallsBackToQuestionMark()
    {
        var s = SmartInfo.Parse("|SATA|HDD|7200|||||");
        Assert.NotNull(s);
        Assert.Equal("?", s!.Health);
    }

    [Theory]
    [InlineData(null, SmartLevel.Unknown)]
    [InlineData(30, SmartLevel.Ok)]
    [InlineData(50, SmartLevel.Ok)]
    [InlineData(55, SmartLevel.Warning)]
    [InlineData(60, SmartLevel.Warning)]
    [InlineData(75, SmartLevel.Critical)]
    public void TemperatureLevel_ClassifiesByRange(int? c, SmartLevel expected)
        => Assert.Equal(expected, SmartInfo.TemperatureLevel(c));

    [Theory]
    [InlineData(null, SmartLevel.Unknown)]
    [InlineData(0, SmartLevel.Ok)]
    [InlineData(69, SmartLevel.Ok)]
    [InlineData(70, SmartLevel.Warning)]
    [InlineData(89, SmartLevel.Warning)]
    [InlineData(90, SmartLevel.Critical)]
    [InlineData(100, SmartLevel.Critical)]
    public void WearLevel_ClassifiesByRange(int? w, SmartLevel expected)
        => Assert.Equal(expected, SmartInfo.WearLevel(w));

    [Theory]
    [InlineData(null, SmartLevel.Unknown)]
    [InlineData(0L, SmartLevel.Ok)]
    [InlineData(1L, SmartLevel.Warning)]
    [InlineData(99L, SmartLevel.Warning)]
    [InlineData(100L, SmartLevel.Critical)]
    public void ErrorLevel_ClassifiesByRange(long? e, SmartLevel expected)
        => Assert.Equal(expected, SmartInfo.ErrorLevel(e));

    [Theory]
    [InlineData("Healthy", SmartLevel.Ok)]
    [InlineData("healthy", SmartLevel.Ok)]
    [InlineData(" Healthy ", SmartLevel.Ok)]
    [InlineData("Warning", SmartLevel.Warning)]
    [InlineData("Unhealthy", SmartLevel.Critical)]
    [InlineData("?", SmartLevel.Unknown)]
    [InlineData("", SmartLevel.Unknown)]
    [InlineData(null, SmartLevel.Unknown)]
    public void HealthLevel_ClassifiesReportedStatus(string? health, SmartLevel expected)
        => Assert.Equal(expected, SmartInfo.HealthLevel(health));
}
