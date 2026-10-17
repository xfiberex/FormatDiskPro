// SPDX-License-Identifier: GPL-3.0-only
using Xunit;

namespace FormatDiskPro.Tests;

/// <summary>
/// El proyecto es <c>GPL-3.0-only</c>, y lo dice igual en todos los sitios (`AUD-069`).
///
/// <para>Hasta el 2026-10-16 se declaraba «GPLv3» sin precisar si era «solo» o «o posterior», y las fuentes no
/// llevaban aviso. El autor decidió «solo la versión 3». Estas pruebas evitan que una fuente nueva llegue sin
/// su cabecera, o que alguno de los textos visibles vuelva a decir otra cosa.</para>
/// </summary>
public sealed class LicenseHeaderTests
{
    private const string Spdx = "SPDX-License-Identifier: GPL-3.0-only";

    /// <summary>Raíz del repo, localizada subiendo hasta encontrar <c>FormatDiskPro.slnx</c>.</summary>
    private static string RepoRoot()
    {
        for (var dir = new DirectoryInfo(AppContext.BaseDirectory); dir is not null; dir = dir.Parent)
            if (File.Exists(Path.Combine(dir.FullName, "FormatDiskPro.slnx"))) return dir.FullName;
        Assert.Fail($"No se encontró la raíz del repo (FormatDiskPro.slnx) subiendo desde {AppContext.BaseDirectory}.");
        return "";
    }

    /// <summary>Archivos de un patrón bajo <paramref name="relDir"/>, sin <c>bin</c> ni <c>obj</c>.</summary>
    private static IEnumerable<string> SourceFiles(string root, string relDir, string pattern, bool recurse = true)
    {
        string dir = Path.Combine(root, relDir);
        if (!Directory.Exists(dir)) return [];
        char sep = Path.DirectorySeparatorChar;
        return Directory.EnumerateFiles(dir, pattern, recurse ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly)
            .Where(f => !f.Contains($"{sep}bin{sep}") && !f.Contains($"{sep}obj{sep}"));
    }

    /// <summary>Primera línea del archivo, sin BOM.</summary>
    private static string FirstLine(string path)
    {
        using var reader = new StreamReader(path, detectEncodingFromByteOrderMarks: true);
        return reader.ReadLine() ?? "";
    }

    [Fact]
    public void EverySourceFile_StartsWithTheSpdxHeader()
    {
        string root = RepoRoot();
        var files = SourceFiles(root, "src", "*.cs")
            .Concat(SourceFiles(root, "tests", "*.cs"))
            .Select(f => (Path: f, Expected: "// " + Spdx))
            .Concat(SourceFiles(root, "src", "*.ps1")
                .Concat(SourceFiles(root, "tools", "*.ps1"))
                .Concat(SourceFiles(root, "", "*.ps1", recurse: false))
                .Select(f => (Path: f, Expected: "# " + Spdx)))
            .ToList();

        Assert.True(files.Count > 100, $"Solo se encontraron {files.Count} fuentes: ¿ha cambiado la estructura del repo?");

        var missing = files
            .Where(f => FirstLine(f.Path) != f.Expected)
            .Select(f => "  " + Path.GetRelativePath(root, f.Path))
            .ToList();

        Assert.True(missing.Count == 0,
            $"{missing.Count} fuente(s) no empiezan por la cabecera de licencia (`AUD-069`). Añade como primera línea " +
            $"«// {Spdx}» (C#) o «# {Spdx}» (PowerShell):\n" + string.Join("\n", missing));
    }

    [Fact]
    public void AboutDialog_DeclaresGpl30Only_InEveryLanguage()
    {
        string[] translations = L.Map["about.copyright"];
        Assert.All(translations, t => Assert.Contains("GPL-3.0-only", t));
    }

    [Theory]
    [InlineData("README.md")]
    [InlineData("THIRD-PARTY-NOTICES.txt")]
    [InlineData("src/FormatDiskPro/FormatDiskPro.csproj")]
    public void ProjectTexts_DeclareGpl30Only(string relPath)
    {
        string text = File.ReadAllText(Path.Combine(RepoRoot(), relPath));
        Assert.Contains("GPL-3.0-only", text);
        Assert.DoesNotContain("or-later", text);
    }
}
