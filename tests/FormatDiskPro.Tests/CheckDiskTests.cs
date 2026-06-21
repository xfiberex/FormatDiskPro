using FormatDiskPro;
using Xunit;

namespace FormatDiskPro.Tests;

/// <summary>
/// Pruebas de la interpretación del código de salida de chkdsk (lógica pura). La ejecución real
/// del proceso (<see cref="CheckDisk.RunAsync"/>) es E/S y no se cubre con pruebas unitarias.
/// </summary>
public sealed class CheckDiskTests
{
    [Theory]
    [InlineData(0, false, CheckResult.Clean)]
    [InlineData(0, true,  CheckResult.Clean)]
    [InlineData(1, true,  CheckResult.Repaired)]
    [InlineData(1, false, CheckResult.Errors)]
    [InlineData(2, false, CheckResult.Errors)]
    [InlineData(2, true,  CheckResult.Errors)]
    [InlineData(3, false, CheckResult.Failed)]
    [InlineData(3, true,  CheckResult.Failed)]
    public void Interpret_MapsExitCodes(int code, bool repair, CheckResult expected)
        => Assert.Equal(expected, CheckDisk.Interpret(code, repair));
}
