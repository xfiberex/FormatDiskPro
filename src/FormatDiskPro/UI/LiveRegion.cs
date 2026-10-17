// SPDX-License-Identifier: GPL-3.0-only
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation.Peers;

namespace FormatDiskPro.UI;

/// <summary>
/// El aviso que hace que un lector de pantalla <b>lea</b> una región activa cuando cambia (`AUD-014`).
/// </summary>
/// <remarks>
/// <para><b>Declarar la región no basta.</b> <c>AutomationProperties.LiveSetting</c> en el XAML solo dice
/// <i>cómo</i> leer el texto —interrumpiendo o esperando turno—. Lo que dispara la lectura es el evento
/// <c>LiveRegionChanged</c>, y WinUI no lo lanza solo al cambiar <c>Text</c>: hay que lanzarlo. Una
/// región declarada cuyo código nunca llama aquí se ve correcta en el XAML y en una inspección del
/// árbol, y es muda.</para>
///
/// <para>Vivía como método privado de <c>MainWindow</c>; los diálogos que declaraban regiones activas
/// no tenían con qué avisar, y no avisaban.</para>
/// </remarks>
internal static class LiveRegion
{
    /// <summary>
    /// Avisa de que el contenido de <paramref name="element"/> ha cambiado. Defensivo: la accesibilidad
    /// no puede tumbar la interfaz que describe.
    /// </summary>
    /// <param name="element">
    /// Un elemento <b>visible</b> con <c>LiveSetting</c> declarado. Uno colapsado no está en el árbol de
    /// automatización y no tiene a quién avisar: hay que mostrarlo antes de llamar.
    /// </param>
    public static void Announce(UIElement element)
    {
        try
        {
            var peer = FrameworkElementAutomationPeer.FromElement(element)
                    ?? FrameworkElementAutomationPeer.CreatePeerForElement(element);
            peer?.RaiseAutomationEvent(AutomationEvents.LiveRegionChanged);
        }
        catch { /* ignorar */ }
    }
}
