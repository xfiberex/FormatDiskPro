using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using FormatDiskPro;
using Xunit;

namespace FormatDiskPro.Tests;

/// <summary>
/// <see cref="ErrorText"/> promete una cosa: <b>lo que devuelve nunca está vacío</b>. Es lo que faltaba
/// cuando cuatro líneas del historial quedaron en <c>EXPORT ERROR:</c> y un <c>InfoBar</c> mostró un
/// título sin cuerpo — una excepción venida de WinRT puede traer la <c>Message</c> en blanco.
/// </summary>
public sealed class ErrorTextTests
{
    [Fact]
    public void Describe_KeepsARealMessage()
        => Assert.Equal("No hay espacio en el disco.",
                        ErrorText.Describe(new IOException("No hay espacio en el disco.")));

    /// <summary>
    /// El mensaje de una excepción es texto que no controlamos y suele venir con un salto al final; en
    /// una <c>InfoBar</c> eso es una línea en blanco bajo el texto.
    /// </summary>
    [Fact]
    public void Describe_TrimsTheEdges()
        => Assert.Equal("Acceso denegado.", ErrorText.Describe(new IOException("  Acceso denegado.\r\n")));

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("\r\n")]
    public void Describe_FallsBackToTypeAndHResult_WhenThereIsNoMessage(string message)
    {
        // 0x80004005 (E_FAIL) es el que devolvía de verdad el selector de archivos de WinRT en proceso
        // elevado, que es el caso que hizo falta diagnosticar.
        var ex = new COMException(message, unchecked((int)0x80004005));

        string text = ErrorText.Describe(ex);

        Assert.False(string.IsNullOrWhiteSpace(text));
        Assert.Contains("COMException", text, StringComparison.Ordinal);
        Assert.Contains("0x80004005", text, StringComparison.Ordinal);
    }

    /// <summary>Raíz del repo, localizada subiendo hasta encontrar <c>FormatDiskPro.slnx</c>.</summary>
    private static string RepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            if (File.Exists(Path.Combine(dir.FullName, "FormatDiskPro.slnx"))) return dir.FullName;
            dir = dir.Parent;
        }
        throw new InvalidOperationException(
            $"No se encontró la raíz del repo (FormatDiskPro.slnx) subiendo desde {AppContext.BaseDirectory}.");
    }

    /// <summary>
    /// Y nadie vuelve a usar <c>ex.Message</c> en crudo. No es una regla de estilo: cada uno de esos sitios
    /// puede escribir un error <b>vacío</b> en el historial o en un diálogo, y ese fallo estuvo años ahí
    /// sin que ninguna prueba lo notara porque el código «se leía bien».
    ///
    /// <para>El único sitio legítimo es <see cref="ErrorText"/>, que es quien decide el respaldo. Si
    /// algún día hace falta otro —un formato de archivo, algo que no lee un humano—, la respuesta
    /// correcta es añadir aquí una excepción nombrada, no borrar la prueba.</para>
    /// </summary>
    [Fact]
    public void NoRawExceptionMessages_OutsideErrorText()
    {
        string src = Path.Combine(RepoRoot(), "src", "FormatDiskPro");
        var files = Directory.EnumerateFiles(src, "*.cs", SearchOption.AllDirectories)
            .Where(f => !f.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.Ordinal)
                     && !f.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}", StringComparison.Ordinal)
                     && Path.GetFileName(f) != "ErrorText.cs")
            .ToList();

        // Un barrido que no encuentra ficheros pasaría siempre: eso es lo que hay que evitar aquí.
        Assert.True(files.Count >= 15, $"Solo se encontraron {files.Count} fuentes: el barrido no mira donde debe.");

        // Solo código: en los comentarios se NOMBRA a ex.Message al explicar por qué no se usa.
        var raw = new Regex(@"^(?!\s*(//|///|\*)).*\bex\.Message\b", RegexOptions.Compiled);

        var offenders = new List<string>();
        foreach (string file in files)
        {
            var lines = File.ReadAllLines(file);
            for (int i = 0; i < lines.Length; i++)
                if (raw.IsMatch(lines[i]))
                    offenders.Add($"{Path.GetFileName(file)}:{i + 1}: {lines[i].Trim()}");
        }

        Assert.True(offenders.Count == 0,
            "Estos sitios usan ex.Message en crudo y pueden acabar escribiendo un error vacío; " +
            $"pasa por ErrorText.Describe(ex):{Environment.NewLine}{string.Join(Environment.NewLine, offenders)}");
    }
}
