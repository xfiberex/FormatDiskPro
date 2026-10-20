// SPDX-License-Identifier: GPL-3.0-only
using Xunit;

namespace FormatDiskPro.Tests;

/// <summary>
/// El historial se escribe en un formato técnico que no depende del idioma de la app (`AUD-049`).
/// </summary>
/// <remarks>
/// Desde que <see cref="FormatLogic.FormatBytes"/> traduce las unidades, escribir el registro con él haría que
/// la misma medición quedara como «9,2 Mo/s» o «9.2 MB/s» según el idioma que hubiera puesto. El registro se
/// pega en issues y lo lee el visor: tiene que ser el mismo en cualquier idioma.
/// </remarks>
[Collection(LanguageCollection.Name)]
public sealed class LogFormatTests
{
    private static readonly BenchmarkResult Result = new(
        new BenchmarkScore(ReadBytesPerSec: 22.2 * 1024 * 1024, WriteBytesPerSec: 9.2 * 1024 * 1024),
        new BenchmarkScore(ReadBytesPerSec: 2.8 * 1024 * 1024, WriteBytesPerSec: 25.6 * 1024),
        TestBytes: 536870912);

    [Theory]
    [InlineData(AppLang.Es)]
    [InlineData(AppLang.Fr)]
    [InlineData(AppLang.En)]
    public void BenchLogLine_IsTheSameInEveryLanguage(AppLang lang)
    {
        var prev = L.Current;
        try
        {
            L.Set(lang);
            Assert.Equal(
                "BENCH D: seq w=9.2 MB/s r=22.2 MB/s · rnd4k w=25.6 KB/s (6 IOPS) r=2.8 MB/s (717 IOPS) bytes=536870912",
                Benchmark.LogLine('D', Result));
        }
        finally { L.Set(prev); }
    }

    [Fact]
    public void FormatBytesForLog_IgnoresTheAppLanguage()
    {
        var prev = L.Current;
        try
        {
            L.Set(AppLang.Fr);
            Assert.Equal("1,5 Ko", FormatLogic.FormatBytes(1536));        // lo que se enseña
            Assert.Equal("1.5 KB", FormatLogic.FormatBytesForLog(1536));  // lo que se escribe
        }
        finally { L.Set(prev); }
    }
}
