using System.Diagnostics;
using System.Security.Principal;
using Xunit;

namespace FormatDiskPro.Tests;

/// <summary>
/// Una prueba que necesita correr <b>elevada</b>; si no lo está, se omite diciendo por qué.
///
/// <para>Solo la usan las que hacen dueño de una carpeta al grupo Administradores, que Windows no
/// permite a un proceso sin elevar. En CI y en el corte se ejecutan: los dos corren elevados.</para>
/// </summary>
public sealed class ElevatedFactAttribute : FactAttribute
{
    public ElevatedFactAttribute()
    {
        using var identity = WindowsIdentity.GetCurrent();
        if (!new WindowsPrincipal(identity).IsInRole(WindowsBuiltInRole.Administrator))
            Skip = "Requiere una terminal elevada: hace dueño de una carpeta al grupo Administradores.";
    }
}

/// <summary>Una carpeta temporal por prueba, que se borra al terminar.</summary>
internal sealed class ScratchFolder : IDisposable
{
    private readonly List<string> _junctions = [];

    public string Root { get; } = Directory.CreateTempSubdirectory("fdp_folder_").FullName;

    public string PathOf(params string[] parts) => Path.Combine([Root, .. parts]);

    /// <summary>Crea una subcarpeta normal y devuelve su ruta.</summary>
    public string CreateFolder(params string[] parts)
        => Directory.CreateDirectory(PathOf(parts)).FullName;

    /// <summary>
    /// Crea una <b>unión</b> (<c>mklink /J</c>) de <paramref name="link"/> hacia
    /// <paramref name="target"/>. Es el enlace que cualquier cuenta puede crear sin privilegios, y por
    /// tanto el que importa: los enlaces simbólicos exigen ser administrador o el modo de desarrollador.
    /// </summary>
    public void CreateJunction(string link, string target)
    {
        var psi = new ProcessStartInfo(Path.Combine(Environment.SystemDirectory, "cmd.exe"))
        {
            UseShellExecute        = false,
            CreateNoWindow         = true,
            RedirectStandardOutput = true,
            RedirectStandardError  = true,
        };
        foreach (string a in (string[])["/c", "mklink", "/J", link, target]) psi.ArgumentList.Add(a);

        using var cmd = Process.Start(psi)!;
        string error = cmd.StandardError.ReadToEnd();
        cmd.StandardOutput.ReadToEnd();
        cmd.WaitForExit();

        Assert.True(cmd.ExitCode == 0, $"mklink /J falló ({cmd.ExitCode}): {error}");
        Assert.True((File.GetAttributes(link) & FileAttributes.ReparsePoint) != 0);
        _junctions.Add(link);
    }

    public void Dispose()
    {
        // Las uniones primero y sin recursión: se quita el enlace, no lo que hay al otro lado.
        foreach (string j in _junctions)
        {
            try { Directory.Delete(j); } catch (IOException) { } catch (UnauthorizedAccessException) { }
        }
        try { Directory.Delete(Root, recursive: true); }
        catch (IOException) { }
        catch (UnauthorizedAccessException) { }
    }
}
