using FormatDiskPro;
using Xunit;

namespace FormatDiskPro.Tests;

/// <summary>
/// Verifica que los textos legales embebidos (licencia GPLv3 y avisos de terceros) se cargan desde
/// los recursos del ensamblado para poder mostrarse dentro de la app.
/// </summary>
public sealed class LegalTextTests
{
    [Fact]
    public void License_IsGplV3()
    {
        string text = LegalText.License();
        Assert.False(string.IsNullOrWhiteSpace(text));
        Assert.Contains("GNU GENERAL PUBLIC LICENSE", text);
        Assert.Contains("Version 3", text);
    }

    [Fact]
    public void ThirdParty_ListsComponentsAndLicenses()
    {
        string text = LegalText.ThirdParty();
        Assert.False(string.IsNullOrWhiteSpace(text));
        Assert.Contains("Windows App SDK", text);
        Assert.Contains("MIT", text);
    }
}
