using System.Text.RegularExpressions;
using Xunit;

namespace FormatDiskPro.Tests;

/// <summary>
/// `T7-09`: el rectángulo de foco de WinUI se dibuja <b>hacia fuera</b> de los límites del control, y el
/// <c>ContentDialog</c> envuelve su contenido en un <c>ScrollViewer</c> que recorta. Un control pegado al
/// borde de la raíz pierde por tanto el lado del foco que cae fuera — así se vio, al tabular hasta el
/// primer filtro del historial: el marco salía cortado por la izquierda.
///
/// <para>El arreglo es un relleno de 3 px en la raíz del contenido, y lo que esta prueba defiende no es el
/// número sino que sea <b>uno solo para todos</b>. El ancho común (<c>T6-07</c>) ya enseñó adónde lleva lo
/// contrario: seis criterios distintos y un diálogo que saltaba al abrir el siguiente. Un diálogo nuevo que
/// se olvide del relleno recorta el foco exactamente igual, y nadie lo va a ver hasta que alguien tabule.</para>
/// </summary>
public sealed class DialogLayoutTests
{
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
    /// La única excepción, y es declarada: <c>LegalTextDialog</c> fija su ancho al valor EXACTO que
    /// `T6-14` midió para que quepan las 78 columnas de la GPL sin barra horizontal. Un relleno le
    /// comería 6 px de esa cuenta y volvería a partir el texto legal, que es justo lo que esa tarea
    /// arregló. Su raíz es además un <c>ScrollViewer</c> a pantalla completa del diálogo: no hay ningún
    /// control tabulable pegado a su borde al que recortarle el foco.
    /// </summary>
    private const string Exempt = "LegalTextDialog.xaml";

    private static List<string> DialogFiles()
        => [.. Directory.EnumerateFiles(Path.Combine(RepoRoot(), "src", "FormatDiskPro", "UI"), "*Dialog.xaml")
              .Where(f => Path.GetFileName(f) != Exempt)];

    [Fact]
    public void EveryDialogRoot_UsesTheSharedFocusSafePadding()
    {
        var files = DialogFiles();

        // Un barrido que no encuentra ficheros pasaría siempre: eso es lo que hay que evitar aquí.
        Assert.True(files.Count >= 5, $"Solo se encontraron {files.Count} diálogos: el barrido no mira donde debe.");

        foreach (string file in files)
        {
            string xaml = File.ReadAllText(file);
            Assert.True(xaml.Contains("Padding=\"{StaticResource DialogContentPadding}\"", StringComparison.Ordinal),
                $"{Path.GetFileName(file)} no aplica DialogContentPadding en la raíz de su contenido: el " +
                 "foco del primer control pegado al borde saldrá recortado. Si de verdad es una excepción, " +
                 "va nombrada en DialogLayoutTests.Exempt con su porqué, como LegalTextDialog.");
        }
    }

    /// <summary>
    /// Y el relleno tiene que seguir siendo el del recurso compartido: 3 px, que es lo que el trazo del
    /// foco necesita (2 px primarios + 1 px secundario). Menos vuelve a recortar; más deja de ser una
    /// medida y pasa a ser un margen estético, que es como se pierde el porqué.
    /// </summary>
    [Fact]
    public void TheSharedPadding_IsStillTheThreePixelsTheFocusRingNeeds()
    {
        string theme = File.ReadAllText(
            Path.Combine(RepoRoot(), "src", "FormatDiskPro", "UI", "Theme", "AppTheme.xaml"));

        var match = Regex.Match(theme, @"<Thickness x:Key=""DialogContentPadding"">([^<]+)</Thickness>");
        Assert.True(match.Success, "Falta el recurso DialogContentPadding en AppTheme.xaml.");
        Assert.Equal("3", match.Groups[1].Value.Trim());
    }
}
