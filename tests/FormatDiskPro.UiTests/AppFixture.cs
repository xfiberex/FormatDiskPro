using System.Security.Principal;
using FlaUI.Core;
using FlaUI.Core.AutomationElements;
using FlaUI.UIA3;

namespace FormatDiskPro.UiTests;

public sealed class AppFixture : IDisposable
{
    public Application App { get; }
    public UIA3Automation Automation { get; }
    public Window MainWindow { get; }

    private readonly SettingsBackup _settingsBackup;

    public AppFixture()
    {
        // FormatDiskPro.exe pide requireAdministrator (app.manifest). Si este proceso de
        // pruebas no está también elevado, UIPI bloquea en silencio los mensajes/inputs de
        // UI Automation contra la ventana elevada: falla como timeout, no como error claro.
        EnsureElevated();

        // La app es unpackaged: no tiene almacenamiento aislado por prueba. settings.json/history.log
        // viven en %AppData%\FormatDiskPro, el MISMO sitio que usa la instalación real del usuario.
        // Sin este backup, cambiar idioma/tema/unidad durante las pruebas dejaría esos cambios
        // filtrados en su app de verdad.
        _settingsBackup = SettingsBackup.Capture();

        var exePath = ResolveExePath();
        App = Application.Launch(exePath);
        Automation = new UIA3Automation();
        MainWindow = App.GetMainWindow(Automation, TimeSpan.FromSeconds(20))
            ?? throw new InvalidOperationException("FormatDiskPro no abrió su ventana principal a tiempo.");

        // Al primer arranque de una versión, MainWindow.OnFirstActivated dispara por su cuenta
        // MaybeShowWhatsNewAsync() y CheckForUpdatesAsync(manual: false) — pueden abrir un
        // ContentDialog (Novedades / Actualización disponible) en cualquier momento de los primeros
        // segundos, sin relación con lo que esté haciendo un test. WinUI solo permite un
        // ContentDialog abierto a la vez: si esto colisiona con el diálogo que abre un test, la app
        // intenta abrir un segundo dentro de un manejador async void sin captura y el proceso muere
        // (visto en la práctica: un solo diálogo sin cerrar aquí tumbó toda la suite). Se descarta
        // aquí, antes de que arranque ningún test.
        DialogHelper.DismissStartupDialogs(this, TimeSpan.FromSeconds(8));
    }

    public void Dispose()
    {
        try { App.Close(); } catch { /* pudo cerrarse ya dentro de un test */ }
        Automation.Dispose();
        App.Dispose();
        _settingsBackup.Restore();
    }

    private static void EnsureElevated()
    {
        using var identity = WindowsIdentity.GetCurrent();
        var principal = new WindowsPrincipal(identity);
        if (!principal.IsInRole(WindowsBuiltInRole.Administrator))
        {
            throw new InvalidOperationException(
                "Estas pruebas de UI requieren una terminal elevada: FormatDiskPro.exe exige " +
                "administrador (requireAdministrator) y un proceso de pruebas no elevado no puede " +
                "automatizar su ventana. Vuelve a ejecutar 'dotnet test' desde PowerShell/terminal " +
                "como administrador.");
        }
    }

    private static string ResolveExePath()
    {
        var overridePath = Environment.GetEnvironmentVariable("FORMATDISKPRO_EXE");
        if (!string.IsNullOrWhiteSpace(overridePath))
        {
            if (!File.Exists(overridePath))
                throw new FileNotFoundException($"FORMATDISKPRO_EXE apunta a una ruta inexistente: {overridePath}");
            return overridePath;
        }

        var repoRoot = FindRepoRoot(AppContext.BaseDirectory)
            ?? throw new InvalidOperationException(
                $"No se encontró la raíz del repo (FormatDiskPro.slnx) subiendo desde {AppContext.BaseDirectory}.");

        var binRoot = Path.Combine(repoRoot, "src", "FormatDiskPro", "bin");
        var candidates = Directory.Exists(binRoot)
            ? Directory.GetFiles(binRoot, "FormatDiskPro.exe", SearchOption.AllDirectories)
            : [];

        if (candidates.Length == 0)
        {
            throw new FileNotFoundException(
                "No se encontró FormatDiskPro.exe compilado. Ejecuta 'dotnet build -c Release' sobre " +
                "src/FormatDiskPro antes de correr estas pruebas, o define FORMATDISKPRO_EXE con la ruta " +
                "al ejecutable.");
        }

        return candidates.OrderByDescending(File.GetLastWriteTimeUtc).First();
    }

    private static string? FindRepoRoot(string startDir)
    {
        var dir = new DirectoryInfo(startDir);
        while (dir is not null)
        {
            if (File.Exists(Path.Combine(dir.FullName, "FormatDiskPro.slnx")))
                return dir.FullName;
            dir = dir.Parent;
        }
        return null;
    }
}

[CollectionDefinition(Name)]
public sealed class AppCollection : ICollectionFixture<AppFixture>
{
    public const string Name = "FormatDiskPro app";
}
