using System.Text.RegularExpressions;
using Xunit;

namespace FormatDiskPro.Tests;

/// <summary>
/// Un solo <c>ContentDialog</c> a la vez: todo el que abre la ventana pasa por <c>ShowOneAsync</c>
/// (`T13-19`).
/// </summary>
/// <remarks>
/// <para>WinUI lanza <c>COMException 0x80000019</c> al abrir el segundo, y como estos flujos son
/// <c>async void</c> esa excepción no la recoge nadie: acaba en la red global de <c>App</c>, que deja la
/// app viva y escribe un <c>CRASH</c> en el historial. Ocurrió de verdad el 2026-09-18.</para>
/// <para>La compuerta solo sirve si <b>nadie la rodea</b>, y un <c>ShowAsync</c> suelto no rompe nada al
/// escribirlo: rompe el día que dos diálogos coinciden. Por eso esto es una prueba y no un comentario.</para>
/// </remarks>
public class SingleDialogGateTests
{
    private static readonly Regex DirectShow = new(@"\.ShowAsync\(\)", RegexOptions.Compiled);
    private static readonly Regex LineComment = new(@"//[^
]*", RegexOptions.Compiled);

    private static string RepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            if (File.Exists(Path.Combine(dir.FullName, "FormatDiskPro.slnx"))) return dir.FullName;
            dir = dir.Parent;
        }
        throw new InvalidOperationException("No se encontró la raíz del repo.");
    }

    [Fact]
    public void EveryDialog_GoesThroughTheGate()
    {
        string ui = Path.Combine(RepoRoot(), "src", "FormatDiskPro", "UI");
        var offenders = new List<string>();

        foreach (string file in Directory.EnumerateFiles(ui, "*.cs", SearchOption.AllDirectories))
        {
            if (file.EndsWith(".g.cs", StringComparison.Ordinal) || file.EndsWith(".g.i.cs", StringComparison.Ordinal))
                continue;

            string[] lines = LineComment.Replace(File.ReadAllText(file), "").Split('\n');
            for (int i = 0; i < lines.Length; i++)
            {
                if (!DirectShow.IsMatch(lines[i])) continue;
                // La única llamada directa permitida es la que vive DENTRO de la compuerta.
                if (lines[i].Contains("return await dialog.ShowAsync();", StringComparison.Ordinal)) continue;
                offenders.Add($"{Path.GetFileName(file)}:{i + 1}");
            }
        }

        Assert.True(offenders.Count == 0,
            "Estos diálogos se abren sin pasar por ShowOneAsync, así que pueden ser el segundo (`T13-19`): "
            + string.Join(" · ", offenders));
    }
}
