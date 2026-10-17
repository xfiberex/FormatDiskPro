// SPDX-License-Identifier: GPL-3.0-only
using System.Security.AccessControl;
using System.Security.Principal;
using Xunit;

namespace FormatDiskPro.Tests;

/// <summary>
/// La carpeta donde se descarga el instalador de una actualización solo la escriben administradores,
/// y una que no pueda garantizarlo se rechaza (`AUD-002`).
///
/// <para>El instalador se ejecuta elevado. Mientras se descargaba a <c>%Temp%</c> —que pertenece a la
/// cuenta sin elevar—, esa cuenta podía cambiar el archivo, o la carpeta entera, después de la
/// verificación. Aquí se fija lo que hace de la carpeta nueva un sitio distinto: quién es su dueño,
/// quién puede escribir en ella, y qué pasa cuando ya existía y no es de fiar.</para>
/// </summary>
public sealed class AdminOnlyFolderTests
{
    private static readonly SecurityIdentifier Administrators = new(WellKnownSidType.BuiltinAdministratorsSid, null);
    private static readonly SecurityIdentifier LocalSystem    = new(WellKnownSidType.LocalSystemSid, null);
    private static readonly SecurityIdentifier Everyone       = new(WellKnownSidType.WorldSid, null);

    private static SecurityIdentifier OwnerOf(string dir)
        => (SecurityIdentifier)new DirectoryInfo(dir).GetAccessControl().GetOwner(typeof(SecurityIdentifier))!;

    /// <summary>Todas las entradas de la lista de acceso, las propias y las heredadas.</summary>
    private static List<FileSystemAccessRule> RulesOf(FileSystemSecurity security)
        => [.. security.GetAccessRules(true, true, typeof(SecurityIdentifier)).Cast<FileSystemAccessRule>()];

    private static void AssertOnlyAdministratorsAndSystem(FileSystemSecurity security)
    {
        var rules = RulesOf(security);
        Assert.NotEmpty(rules);
        Assert.All(rules, r =>
        {
            Assert.Equal(AccessControlType.Allow, r.AccessControlType);
            Assert.True(r.IdentityReference.Equals(Administrators) || r.IdentityReference.Equals(LocalSystem),
                $"Tiene acceso una cuenta que no debería: {r.IdentityReference}");
        });
    }

    /// <summary>
    /// El criterio de aceptación: la carpeta no concede <b>nada</b> a la cuenta sin elevar. Se mira lo
    /// mismo que enseñaría <c>icacls</c> —dueño, herencia cortada y la lista entera— en cada nivel, y en
    /// un archivo creado dentro, que es donde acabará el instalador.
    /// </summary>
    [ElevatedFact]
    public void Prepare_CreatesEveryLevel_OwnedByAndOpenOnlyToAdministrators()
    {
        using var scratch = new ScratchFolder();

        string inner = AdminOnlyFolder.Prepare(scratch.Root, "FormatDiskPro", "update");

        Assert.Equal(scratch.PathOf("FormatDiskPro", "update"), inner);
        foreach (string level in (string[])[scratch.PathOf("FormatDiskPro"), inner])
        {
            var acl = new DirectoryInfo(level).GetAccessControl();
            Assert.Equal(Administrators, OwnerOf(level));
            Assert.True(acl.AreAccessRulesProtected, $"{level} sigue heredando permisos de su carpeta madre.");
            AssertOnlyAdministratorsAndSystem(acl);
        }

        string file = Path.Combine(inner, "FormatDiskPro-9.9.9-setup.exe");
        File.WriteAllBytes(file, [1, 2, 3]);
        AssertOnlyAdministratorsAndSystem(new FileInfo(file).GetAccessControl());
    }

    /// <summary>
    /// Una carpeta que ya existía, es de Administradores, pero dejaba escribir a cualquiera —la herencia
    /// de <c>%ProgramData%</c> hace justo eso—, <b>se cierra</b>. Ser de Administradores basta para
    /// fiarse de ella; no para dejarla como estaba.
    /// </summary>
    [ElevatedFact]
    public void Prepare_ExistingFolderOpenToEveryone_IsClosedAgain()
    {
        using var scratch = new ScratchFolder();
        var dir = new DirectoryInfo(scratch.CreateFolder("update"));
        var open = dir.GetAccessControl();
        open.SetOwner(Administrators);
        open.AddAccessRule(new FileSystemAccessRule(
            Everyone, FileSystemRights.FullControl,
            InheritanceFlags.ContainerInherit | InheritanceFlags.ObjectInherit,
            PropagationFlags.None, AccessControlType.Allow));
        dir.SetAccessControl(open);
        Assert.Contains(RulesOf(dir.GetAccessControl()), r => r.IdentityReference.Equals(Everyone));

        AdminOnlyFolder.Prepare(scratch.Root, "update");

        var closed = new DirectoryInfo(dir.FullName).GetAccessControl();
        Assert.True(closed.AreAccessRulesProtected);
        AssertOnlyAdministratorsAndSystem(closed);
    }

    /// <summary>
    /// Bajo <c>%ProgramData%</c> cualquiera puede crear una carpeta, y quien la crea es su dueño. Una
    /// carpeta así <b>no se arregla, se rechaza</b>: su dueño puede cambiarle los permisos otra vez
    /// cuando quiera, digamos lo que digamos nosotros ahora.
    /// </summary>
    [Fact]
    public void Prepare_FolderOwnedBySomeoneElse_IsRejectedAndSaysWhich()
    {
        using var scratch = new ScratchFolder();
        var dir = new DirectoryInfo(scratch.CreateFolder("update"));
        using var me = WindowsIdentity.GetCurrent();
        var mine = new DirectorySecurity();
        mine.SetOwner(me.User!);
        dir.SetAccessControl(mine);

        var ex = Assert.Throws<InvalidOperationException>(() => AdminOnlyFolder.Prepare(scratch.Root, "update"));

        Assert.Contains(dir.FullName, ex.Message, StringComparison.Ordinal);
        // No se le tocaron los permisos: rechazar no es «arreglar a medias».
        Assert.False(new DirectoryInfo(dir.FullName).GetAccessControl().AreAccessRulesProtected);
    }

    /// <summary>
    /// Una unión en el sitio de la carpeta llevaría la descarga —y la ejecución— a donde apunte. Se
    /// rechaza antes de crear nada dentro, y en el destino no aparece nada.
    /// </summary>
    [Fact]
    public void Prepare_FolderThatIsAJunction_IsRejectedBeforeCreatingAnythingBehindIt()
    {
        using var scratch = new ScratchFolder();
        string target = scratch.CreateFolder("destino");
        string link = scratch.PathOf("FormatDiskPro");
        scratch.CreateJunction(link, target);

        var ex = Assert.Throws<InvalidOperationException>(
            () => AdminOnlyFolder.Prepare(scratch.Root, "FormatDiskPro", "update"));

        Assert.Contains(link, ex.Message, StringComparison.Ordinal);
        Assert.Empty(Directory.GetFileSystemEntries(target));
    }

    /// <summary>La comprobación se hace en <b>cada</b> nivel, no solo en el primero.</summary>
    [ElevatedFact]
    public void Prepare_InnerLevelThatIsAJunction_IsRejectedToo()
    {
        using var scratch = new ScratchFolder();
        string outer = AdminOnlyFolder.Prepare(scratch.Root, "FormatDiskPro");
        string target = scratch.CreateFolder("destino");
        scratch.CreateJunction(Path.Combine(outer, "update"), target);

        Assert.Throws<InvalidOperationException>(
            () => AdminOnlyFolder.Prepare(scratch.Root, "FormatDiskPro", "update"));
    }

    /// <summary>Llamarla dos veces da lo mismo: la segunda actualización usa la carpeta de la primera.</summary>
    [ElevatedFact]
    public void Prepare_IsRepeatable_AndKeepsWhatIsInside()
    {
        using var scratch = new ScratchFolder();
        string first = AdminOnlyFolder.Prepare(scratch.Root, "FormatDiskPro", "update");
        string file = Path.Combine(first, "FormatDiskPro-9.9.9-setup.exe");
        File.WriteAllBytes(file, [1]);

        string second = AdminOnlyFolder.Prepare(scratch.Root, "FormatDiskPro", "update");

        Assert.Equal(first, second);
        Assert.True(File.Exists(file));
    }
}
