using FormatDiskPro;
using Xunit;

namespace FormatDiskPro.Tests;

/// <summary>
/// Verifica el comportamiento defensivo del proveedor de localización <see cref="L"/>.
/// </summary>
public sealed class LocalizationTests
{
    [Fact]
    public void T_UnknownKey_ReturnsKeyItself()
        => Assert.Equal("clave.inexistente", L.T("clave.inexistente"));

    [Fact]
    public void T_KnownKey_ReturnsLocalizedText()
        => Assert.False(string.IsNullOrWhiteSpace(L.T("btn.start")));

    [Fact]
    public void T_WithArguments_FormatsPlaceholders()
        => Assert.Contains("G", L.T("success.body", 'G', "NTFS"));
}
