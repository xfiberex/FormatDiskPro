// SPDX-License-Identifier: GPL-3.0-only
using System.Diagnostics;
using FlaUI.Core.AutomationElements;

namespace FormatDiskPro.UiTests;

/// <summary>
/// Lanzar la app con otra ya abierta deja una sola ventana (`AUD-037`). Antes se abría una segunda
/// instancia elevada, que podía operar a la vez sobre la misma unidad.
/// </summary>
[Collection(AppCollection.Name)]
public sealed class SingleInstanceTests(AppFixture fixture)
{
    private Window Window => fixture.MainWindow;

    [Fact]
    public void LaunchingASecondInstance_LeavesOnlyTheFirstWindow()
    {
        using var second = Process.Start(new ProcessStartInfo(AppFixture.ResolveExePath()) { UseShellExecute = false })
            ?? throw new InvalidOperationException("No se pudo lanzar la segunda instancia.");
        try
        {
            // Tiene hasta el plazo de espera de SingleInstance (5 s) más el arranque; 20 s es de sobra.
            bool exited = second.WaitForExit(TimeSpan.FromSeconds(20));
            Assert.True(exited, "La segunda instancia sigue abierta: no cedió el paso a la primera.");

            // La primera sigue ahí y responde.
            Assert.False(string.IsNullOrEmpty(Window.Title));
            MainWindowActions.WaitUntilEnabled(Window, "DrivePicker", TimeSpan.FromSeconds(10));

            int windows = Process.GetProcessesByName(Path.GetFileNameWithoutExtension(AppFixture.ResolveExePath()))
                .Count(p => { using (p) return !p.HasExited && p.MainWindowHandle != IntPtr.Zero; });
            Assert.Equal(1, windows);
        }
        finally
        {
            if (!second.HasExited) second.Kill(entireProcessTree: true);
        }
    }
}
