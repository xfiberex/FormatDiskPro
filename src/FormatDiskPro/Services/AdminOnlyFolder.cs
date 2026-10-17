// SPDX-License-Identifier: GPL-3.0-only
using System.Security.AccessControl;
using System.Security.Principal;

namespace FormatDiskPro;

/// <summary>
/// Prepara una carpeta en la que <b>solo escriben administradores</b>, y se niega a usar una que no
/// pueda garantizarlo (`AUD-002`).
/// </summary>
/// <remarks>
/// <para><b>Para qué.</b> El instalador de una actualización se descarga, se verifica y se ejecuta
/// elevado. Si eso ocurre en una carpeta donde escribe la cuenta sin elevar —como era
/// <c>%Temp%\FormatDiskPro_update</c>—, verificar no sirve de mucho: el archivo se puede cambiar
/// después, o la carpeta entera puede ser una unión hacia otro sitio.</para>
///
/// <para><b>Por qué no basta con crearla.</b> Bajo <c>%ProgramData%</c> cualquier cuenta puede crear
/// carpetas, y quien crea una es su dueño: puede haberla dejado preparada antes de que la app llegue.
/// Por eso cada nivel se <b>comprueba</b> aunque ya exista —que no sea un punto de reanálisis y que su
/// propietario sea Administradores o SYSTEM— y solo entonces se le impone la lista de acceso. Una
/// carpeta de otro dueño no se «arregla»: se rechaza, porque su dueño puede volver a cambiarle los
/// permisos cuando quiera.</para>
/// </remarks>
public static class AdminOnlyFolder
{
    private static readonly SecurityIdentifier Administrators = new(WellKnownSidType.BuiltinAdministratorsSid, null);
    private static readonly SecurityIdentifier LocalSystem    = new(WellKnownSidType.LocalSystemSid, null);

    /// <summary>
    /// Crea —o comprueba y vuelve a cerrar— cada nivel de <paramref name="levels"/> bajo
    /// <paramref name="root"/>, y devuelve la ruta del último.
    /// </summary>
    /// <param name="root">Carpeta de partida, que se da por buena (<c>%ProgramData%</c>).</param>
    /// <param name="levels">Subcarpetas, de fuera adentro. Todas quedan cerradas a administradores.</param>
    /// <returns>La ruta de la carpeta más interna.</returns>
    /// <exception cref="InvalidOperationException">
    /// Algún nivel es un punto de reanálisis o pertenece a otra cuenta. El mensaje dice cuál y por qué.
    /// </exception>
    public static string Prepare(string root, params string[] levels)
    {
        string path = root;
        foreach (string level in levels)
        {
            path = Path.Combine(path, level);
            var dir = new DirectoryInfo(path);

            // Si alguien la crea entre la pregunta y la creación, Create no hace nada y no avisa. No
            // importa: lo que decide es la comprobación de después, que se hace siempre.
            if (!dir.Exists) dir.Create(AdminOnlyAcl(withOwner: true));

            ThrowIfNotOurs(path);
            dir.SetAccessControl(AdminOnlyAcl(withOwner: false));
        }
        return path;
    }

    private static void ThrowIfNotOurs(string path)
    {
        if (StateFolder.IsReparsePoint(path))
            throw new InvalidOperationException(L.T("update.folderRedirected", path));

        var owner = new DirectoryInfo(path)
            .GetAccessControl(AccessControlSections.Owner)
            .GetOwner(typeof(SecurityIdentifier)) as SecurityIdentifier;

        if (owner is null || !(owner.Equals(Administrators) || owner.Equals(LocalSystem)))
            throw new InvalidOperationException(L.T("update.folderOwner", path));
    }

    /// <summary>
    /// Control total para Administradores y SYSTEM, heredable, y <b>nada más</b>: la lista va protegida
    /// para que no herede los permisos de <c>%ProgramData%</c>, que dejan crear archivos a cualquiera.
    /// </summary>
    private static DirectorySecurity AdminOnlyAcl(bool withOwner)
    {
        const InheritanceFlags inherit = InheritanceFlags.ContainerInherit | InheritanceFlags.ObjectInherit;

        var acl = new DirectorySecurity();
        if (withOwner) acl.SetOwner(Administrators);
        acl.SetAccessRuleProtection(isProtected: true, preserveInheritance: false);
        acl.AddAccessRule(new FileSystemAccessRule(
            Administrators, FileSystemRights.FullControl, inherit, PropagationFlags.None, AccessControlType.Allow));
        acl.AddAccessRule(new FileSystemAccessRule(
            LocalSystem, FileSystemRights.FullControl, inherit, PropagationFlags.None, AccessControlType.Allow));
        return acl;
    }
}
