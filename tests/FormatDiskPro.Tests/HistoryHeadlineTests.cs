// SPDX-License-Identifier: GPL-3.0-only
using Xunit;

namespace FormatDiskPro.Tests;

/// <summary>
/// El titular de cada fila del historial (`AUD-073`): la interfaz deja de enseñar «UPDATE DOWNLOADED
/// v1.30.2: …» y enseña de qué trata, qué pasó y sobre qué, con el detalle técnico aparte.
/// </summary>
public sealed class HistoryHeadlineTests
{
    private static HistoryHeadline Of(string message) => HistoryEntry.Parse("2026-11-10 10:00:00\t" + message)!.Headline();

    [Fact]
    public void ADownloadedUpdate_SaysWhatHappened_ToWhichVersion_AndKeepsThePathApart()
    {
        HistoryHeadline h = Of(@"UPDATE DOWNLOADED v1.30.2: C:\ProgramData\FormatDiskPro\update\setup.exe");
        Assert.Equal("history.cat.update", h.NounKey);
        Assert.Equal("history.act.downloaded", h.ActionKey);
        Assert.Equal("1.30.2", h.Subject);
        Assert.Equal(@"C:\ProgramData\FormatDiskPro\update\setup.exe", h.Detail);
    }

    [Fact]
    public void AFormat_NamesTheDrive_AndShowsTheSizesReadable()
    {
        HistoryHeadline h = Of("FORMAT OK G: fs=NTFS alloc=4096 quick=True");
        Assert.Equal("history.cat.format", h.NounKey);
        Assert.Null(h.ActionKey);                    // «OK» es el resultado, no una acción
        Assert.Equal("G:", h.Subject);
        Assert.Equal($"fs=NTFS alloc={FormatLogic.FormatBytes(4096)} quick=True", h.Detail);
    }

    [Fact]
    public void ALineWithNothingAfterTheDrive_HasAnEmptyDetail()
    {
        HistoryHeadline h = Of("EJECT g:");
        Assert.Equal("history.cat.eject", h.NounKey);
        Assert.Equal("G:", h.Subject);
        Assert.Equal("", h.Detail);
    }

    [Theory]
    [InlineData("CRASH: System.Exception: boom", "history.noun.crash", null, "System.Exception: boom")]
    [InlineData("SETTINGS READ ERROR: no se pudo leer", "history.noun.settings", "history.act.read", "no se pudo leer")]
    [InlineData("REINIT REJECTED G: TargetChanged", "history.cat.reinit", "history.act.rejected", "TargetChanged")]
    [InlineData("WORKFOLDERS DELETE FAIL G: acceso denegado", "history.noun.workfolders", "history.act.delete", "acceso denegado")]
    public void TheAppsOwnEntries_GetTheirOwnNoun_AndTheirAction(string message, string noun, string? action, string detail)
    {
        HistoryHeadline h = Of(message);
        Assert.Equal(noun, h.NounKey);
        Assert.Equal(action, h.ActionKey);
        Assert.Equal(detail, h.Detail);
    }

    [Fact]
    public void AnUnknownWordInTheHeader_KeepsTheWholeLine_RatherThanLoseIt()
    {
        HistoryHeadline h = Of("FORMAT RESUMED G: algo");
        Assert.Equal("history.cat.format", h.NounKey);
        Assert.Null(h.ActionKey);
        Assert.Null(h.Subject);
        Assert.Equal("FORMAT RESUMED G: algo", h.Detail);
    }

    [Fact]
    public void ALineFromAnotherProgram_IsAnOperation_WithTheWholeLineAsDetail()
    {
        HistoryHeadline h = Of("NUEVA línea escrita a mano");
        Assert.Equal("history.cat.other", h.NounKey);
        Assert.Equal("NUEVA línea escrita a mano", h.Detail);
    }

    /// <summary>Una palabra clave nueva no puede llegar a la lista sin su sustantivo traducido.</summary>
    [Fact]
    public void EveryKeyword_HasANoun_AndEveryKeyIsTranslated()
    {
        var keys = new List<string>();
        foreach (string keyword in HistoryEntry.CategoryByPrefix.Keys)
        {
            string? noun = HistoryEntry.NounKeyFor(keyword);
            Assert.True(noun is not null, $"«{keyword}» no tiene sustantivo en NounKeyFor.");
            keys.Add(noun!);
        }
        foreach (string word in HistoryEntry.ActionWords)
        {
            string? action = HistoryEntry.ActionKeyFor(word);
            Assert.True(action is not null, $"«{word}» está en ActionWords y no en ActionKeyFor.");
            keys.Add(action!);
        }

        foreach (string key in keys)
        {
            Assert.True(L.Map.ContainsKey(key), $"Falta la clave '{key}'.");
            Assert.All(L.Map[key], t => Assert.False(string.IsNullOrWhiteSpace(t), $"'{key}' vacía en algún idioma."));
        }
    }
}
