// SPDX-License-Identifier: GPL-3.0-only
using System.Diagnostics;
using Xunit;

namespace FormatDiskPro.Tests;

/// <summary>
/// Qué puede abrir la app en el navegador, y cómo lo abre (`AUD-004`).
///
/// <para>«Abrir una URL» es pedirle al shell que abra una cadena, y el shell abre cualquier cosa: una
/// ruta la ejecuta. Una de las direcciones que se abren llega en el JSON de GitHub, así que lo que se
/// fija aquí es que <b>nada que no sea un enlace del proyecto</b> llegue al shell.</para>
/// </summary>
public sealed class LinkPolicyTests
{
    /// <summary>Los tres casos que nombra el criterio de aceptación, y sus parientes.</summary>
    [Theory]
    [InlineData("file:///C:/x.exe")]
    [InlineData(@"C:\x.exe")]
    [InlineData("http://github.com/xfiberex/FormatDiskPro")]
    [InlineData(@"\\servidor\recurso\x.exe")]
    [InlineData("x.exe")]
    [InlineData("ms-settings:windowsupdate")]
    [InlineData("javascript:alert(1)")]
    [InlineData("https://ejemplo.invalid/")]
    [InlineData("https://github.com.ejemplo.invalid/")]       // el host solo EMPIEZA por github.com
    [InlineData("https://github.com@ejemplo.invalid/")]       // github.com es aquí el usuario, no el sitio
    [InlineData("https://usuario:clave@github.com/")]
    [InlineData("https://github.com:8443/")]
    [InlineData("https://api.github.com/repos")]              // la API no es una página que enseñar
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void AnythingThatIsNotOneOfTheProjectLinks_IsRejected(string? url)
    {
        Assert.False(LinkPolicy.TryGetSafeUrl(url, out string safe));
        Assert.Equal("", safe);
    }

    /// <summary>
    /// Todas las direcciones que la app enlaza hoy pasan. Si se añade una a <see cref="AppInfo"/> y no
    /// a la lista de <see cref="LinkPolicy"/>, el botón dejaría de hacer nada: esta prueba es el aviso.
    /// </summary>
    [Fact]
    public void EveryLinkTheAppOffers_IsAccepted()
    {
        string[] links =
        [
            AppInfo.RepoUrl,
            AppInfo.ReleasesPageUrl,
            AppInfo.DonateUrl,
            AppInfo.ReleasesPageUrl + "/tag/v1.26.0",   // la forma de `html_url` en el JSON de un release
        ];

        Assert.All(links, link =>
        {
            Assert.True(LinkPolicy.TryGetSafeUrl(link, out string safe), link);
            Assert.Equal(link, safe);
        });
    }

    /// <summary>Lo que se entrega es la forma canónica: sin espacios alrededor y con el host en minúsculas.</summary>
    [Fact]
    public void TheAcceptedUrl_IsTheNormalisedOne()
    {
        Assert.True(LinkPolicy.TryGetSafeUrl("  HTTPS://GitHub.com/xfiberex/FormatDiskPro  ", out string safe));
        Assert.Equal("https://github.com/xfiberex/FormatDiskPro", safe);
    }

    /// <summary>El servicio aplica la política: una dirección rechazada no llega a lanzar nada.</summary>
    [Theory]
    [InlineData("file:///C:/x.exe")]
    [InlineData(@"C:\x.exe")]
    [InlineData("http://github.com/")]
    public void OpenUrl_RejectedAddress_ReportsFailureWithoutOpeningAnything(string url)
        => Assert.False(new UpdateService().OpenUrl(url));

    /// <summary>
    /// La apertura va por el <c>explorer.exe</c> de Windows, por ruta absoluta y <b>sin</b>
    /// <c>UseShellExecute</c>: es lo que hace que el navegador lo abra el shell del escritorio, con el
    /// token normal de la sesión, y no este proceso, que está elevado.
    /// </summary>
    [Theory]
    [InlineData("https://github.com/xfiberex/FormatDiskPro")]
    [InlineData(@"C:\Users\Nombre Apellido\AppData\Roaming\FormatDiskPro\history.log")]
    public void DesktopShell_HandsTheTargetToExplorer_ByAbsolutePath(string target)
    {
        ProcessStartInfo psi = DesktopShell.StartInfoFor(target);

        Assert.Equal(SystemTools.Explorer, psi.FileName);
        Assert.True(Path.IsPathFullyQualified(psi.FileName));
        Assert.True(File.Exists(psi.FileName));
        Assert.False(psi.UseShellExecute);
        // Un único argumento, entero: es el runtime quien lo entrecomilla si lleva espacios.
        Assert.Equal([target], psi.ArgumentList);
        Assert.Equal("", psi.Arguments);
    }
}
