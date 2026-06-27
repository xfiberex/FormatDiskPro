using FormatDiskPro;
using Xunit;

namespace FormatDiskPro.Tests;

/// <summary>
/// Pruebas del helper puro de <c>WM_DEVICECHANGE</c>: solo la llegada y la retirada completa de
/// dispositivos justifican recargar la lista de unidades; el resto de subtipos se ignoran.
/// </summary>
public sealed class DeviceChangeTests
{
    [Fact]
    public void IsArrivalOrRemoval_TrueForArrivalAndRemoveComplete()
    {
        Assert.True(DeviceChange.IsArrivalOrRemoval(DeviceChange.DbtDeviceArrival));
        Assert.True(DeviceChange.IsArrivalOrRemoval(DeviceChange.DbtDeviceRemoveComplete));
    }

    [Theory]
    [InlineData(0x0007UL)]   // DBT_DEVNODES_CHANGED (ruidoso)
    [InlineData(0x8001UL)]   // DBT_DEVICEQUERYREMOVE
    [InlineData(0x0000UL)]
    public void IsArrivalOrRemoval_FalseForOtherSubtypes(ulong wParam)
        => Assert.False(DeviceChange.IsArrivalOrRemoval((nuint)wParam));
}
