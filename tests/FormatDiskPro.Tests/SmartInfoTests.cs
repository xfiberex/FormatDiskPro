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
}
