using Xunit;

namespace FormatDiskPro.Tests;

/// <summary>Utilidades compartidas por las pruebas de caminos de error.</summary>
internal static class TestPaths
{
    /// <summary>
    /// Una letra de unidad que <b>no existe</b> en esta máquina, para ejercitar los caminos de "unidad no
    /// disponible" sin depender de hardware. Se busca de atrás hacia delante (Z, Y, X…) porque las letras
    /// altas rara vez están asignadas; nunca devuelve A ni B (disqueteras) ni C.
    ///
    /// Si todas estuvieran ocupadas, falla con un mensaje claro en vez de devolver una letra real: probar
    /// "unidad no lista" contra una unidad que sí lo está no probaría nada.
    /// </summary>
    public static char UnusedDriveLetter()
    {
        var used = DriveInfo.GetDrives()
            .Select(d => char.ToUpperInvariant(d.Name[0]))
            .ToHashSet();

        for (char c = 'Z'; c >= 'D'; c--)
            if (!used.Contains(c)) return c;

        Assert.Fail("No queda ninguna letra de unidad libre entre D y Z en esta máquina.");
        return '\0';   // inalcanzable: Assert.Fail lanza
    }
}
