using System.Globalization;
using FormatDiskPro;
using Xunit;

namespace FormatDiskPro.Tests;

/// <summary>
/// La comparación de letras de unidad decide si una unidad es el disco de sistema, es decir, si se puede
/// formatear. Debe dar el mismo resultado en cualquier idioma de Windows.
/// </summary>
public sealed class DriveLetterTests
{
    [Theory]
    [InlineData('c', 'C')]
    [InlineData('C', 'c')]
    [InlineData('C', 'C')]
    public void Same_IgnoresCase(char a, char b)
        => Assert.True(DriveLetter.Same(a, b));

    [Theory]
    [InlineData('C', 'D')]
    [InlineData('c', 'd')]
    public void Same_DifferentLetters_IsFalse(char a, char b)
        => Assert.False(DriveLetter.Same(a, b));

    /// <summary>
    /// El caso que motiva <see cref="DriveLetter"/>: en cultura turca <c>char.ToUpper('i')</c> devuelve
    /// <c>'İ'</c> (U+0130, I con punto), que NO es <c>'I'</c>. Con la comparación sensible a la cultura
    /// que había antes, la unidad <c>I:</c> dejaba de reconocerse como disco de sistema en un Windows
    /// turco y la guarda contra formatearlo no se activaba.
    /// </summary>
    [Fact]
    public void Same_TurkishCulture_StillMatchesDottedI()
    {
        var previous = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = new CultureInfo("tr-TR");

            // Comprobamos primero que la cultura hace lo que decimos: si un día .NET cambiara este
            // comportamiento, este test dejaría de estar probando lo que cree probar.
            Assert.NotEqual(char.ToUpper('i'), char.ToUpper('I'));

            Assert.True(DriveLetter.Same('i', 'I'));
            Assert.True(DriveLetter.Same('I', 'i'));
        }
        finally
        {
            CultureInfo.CurrentCulture = previous;
        }
    }

    [Theory]
    [InlineData('c', 'C')]
    [InlineData('C', 'C')]
    public void Normalize_ReturnsUppercase(char input, char expected)
        => Assert.Equal(expected, DriveLetter.Normalize(input));
}
