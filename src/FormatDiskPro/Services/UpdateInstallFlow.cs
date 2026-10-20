// SPDX-License-Identifier: GPL-3.0-only
namespace FormatDiskPro;

/// <summary>Cómo terminó un intento de descargar e instalar una actualización.</summary>
public enum UpdateInstallOutcome
{
    /// <summary>El instalador se lanzó: la app tiene que cerrarse para que la reemplace.</summary>
    Launched,
    /// <summary>Se canceló la descarga.</summary>
    Cancelled,
    /// <summary>Falló la descarga, la verificación o el lanzamiento: la app sigue abierta.</summary>
    Failed,
}

/// <summary>
/// Descarga, verifica y lanza el instalador de una versión, y dice a la ventana cuándo el cierre que viene es
/// intencional.
/// </summary>
/// <remarks>
/// <para><b>Por qué está fuera de la ventana</b> (`AUD-030`). El fallo era de orden: el cierre se daba por
/// intencional antes de lanzar el instalador, y si lanzarlo fallaba nadie lo deshacía, así que
/// <c>EndOperation</c> salía sin rehabilitar los controles y la ventana se quedaba apagada. Dentro de
/// <c>MainWindow</c> no había forma de probarlo: una ventana WinUI no se crea en las unitarias, y la app real
/// no tiene, ni debe tener, una costura que cambie de dónde baja el instalador —corre elevada—. Aquí basta un
/// <see cref="IUpdateService"/> falso.</para>
///
/// <para>Sin <c>ConfigureAwait(false)</c> a propósito: las dos devoluciones de llamada tocan la interfaz, y se
/// ejecutan en el contexto de quien llama, que es el hilo de la ventana.</para>
/// </remarks>
public static class UpdateInstallFlow
{
    /// <summary>Descarga e intenta lanzar el instalador de <paramref name="release"/>.</summary>
    /// <param name="updates">El servicio de actualizaciones.</param>
    /// <param name="history">El historial, donde queda la descarga o el error.</param>
    /// <param name="release">La versión que se instala.</param>
    /// <param name="progress">El porcentaje descargado.</param>
    /// <param name="launching">Se llama justo antes de lanzar el instalador, ya verificado.</param>
    /// <param name="setClosingForUpdate">
    /// Recibe <see langword="true"/> antes de lanzar el instalador —desde ahí, cerrar la ventana es lo
    /// esperado aunque haya una operación en curso— y <see langword="false"/> si el intento acaba sin lanzarlo.
    /// </param>
    /// <param name="ct">El token para cancelar la descarga.</param>
    /// <returns>Cómo terminó y, si falló, la excepción.</returns>
    public static async Task<(UpdateInstallOutcome Outcome, Exception? Error)> RunAsync(
        IUpdateService updates, IHistory history, ReleaseInfo release, IProgress<int> progress,
        Action launching, Action<bool> setClosingForUpdate, CancellationToken ct)
    {
        try
        {
            // `using`: el instalador viene con su archivo retenido, y así sigue hasta que se ha lanzado
            // (`AUD-002`). Lo que arranca es lo que se verificó, no lo que haya en esa ruta después.
            using VerifiedInstaller installer = await updates.DownloadAsync(release, progress, ct);
            history.Log($"UPDATE DOWNLOADED {release.TagName}: {installer.Path}");
            launching();
            // Instalación silenciosa: el instalador cierra esta app, actualiza y la relanza. El cierre se da
            // por intencional ANTES de lanzarlo, para que AppWindow_Closing no lo cancele por la operación en
            // curso; así la app suelta el AppMutex y sus archivos y el instalador puede seguir.
            setClosingForUpdate(true);
            updates.LaunchInstaller(installer, silent: true);
            return (UpdateInstallOutcome.Launched, null);
        }
        catch (OperationCanceledException)
        {
            setClosingForUpdate(false);
            return (UpdateInstallOutcome.Cancelled, null);
        }
        catch (Exception ex)
        {
            // Si lo que falló fue lanzar el instalador, el cierre ya se había dado por intencional. La app no
            // se va a cerrar: se deshace, o la ventana se queda con todo apagado (`AUD-030`).
            setClosingForUpdate(false);
            history.Log($"UPDATE DOWNLOAD ERROR {release.TagName}: {ErrorText.Describe(ex)}");
            return (UpdateInstallOutcome.Failed, ex);
        }
    }
}
