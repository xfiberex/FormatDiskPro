using Xunit;

namespace FormatDiskPro.Tests;

/// <summary>
/// La política de rotación (pura): cuándo rotar y cómo se llama la generación anterior.
/// El comportamiento sobre archivos reales lo cubre <see cref="HistoryTests"/>.
/// </summary>
public sealed class HistoryRotationTests
{
    [Theory]
    [InlineData(0, false)]
    [InlineData(1024, false)]
    [InlineData(HistoryRotation.MaxBytes - 1, false)]
    [InlineData(HistoryRotation.MaxBytes, true)]        // el umbral rota: >= , no >
    [InlineData(HistoryRotation.MaxBytes * 4, true)]
    public void ShouldRotate_DecidesByThreshold(long bytes, bool expected)
        => Assert.Equal(expected, HistoryRotation.ShouldRotate(bytes));

    /// <summary>
    /// El <c>.log</c> se conserva al final para que la generación anterior siga abriéndose con el mismo
    /// programa que el historial actual (<c>history.log1</c> no lo haría).
    /// </summary>
    [Theory]
    [InlineData(@"C:\a\b\history.log", @"C:\a\b\history.1.log")]
    [InlineData(@"C:\a\b\history",     @"C:\a\b\history.1")]      // sin extensión
    public void PreviousPath_InsertsGenerationBeforeTheExtension(string path, string expected)
        => Assert.Equal(expected, HistoryRotation.PreviousPath(path));
}
