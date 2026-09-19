using System.Runtime.InteropServices;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;

namespace FormatDiskPro.UI;

/// <summary>
/// ¿Está Windows en un tema de <b>alto contraste</b>? Y, si lo está, los pinceles del sistema con los que
/// hay que pintar en vez de con los propios.
/// </summary>
/// <remarks>
/// <para><b>Qué cubre Windows y qué no</b> (`T13-09`, comprobado con el tema *Blanco* en marcha). WinUI
/// aplica por su cuenta el <i>high contrast adjustment</i> al <b>texto</b>: en alto contraste, un
/// <c>Foreground</c> propio se sustituye por el color del tema, y por eso el historial sale entero del
/// mismo gris aunque cada fila tenga su pincel. Lo que <b>no</b> toca es lo que no es texto: el relleno de
/// la barra de ocupación, el punto de salud y la barra de progreso seguían pintados con los colores de la
/// app —la barra de 212 GB usados salía en el ámbar propio, y su parte libre, en un gris que casi no se
/// distinguía del fondo del tema—.</para>
///
/// <para><b>Por qué se cede el color y no se elige otro.</b> Quien activa un tema de contraste está
/// pidiendo <b>esa</b> paleta, no una mejor: su fondo, su texto y su resaltado. El significado no se
/// pierde, porque en esta app el color nunca va solo — la salud lleva su palabra («Normal», «Atención»,
/// «Crítico»), la ocupación lleva sus cifras y el resultado de una operación, su texto de estado.</para>
///
/// <para><b>Por qué Win32 y no <c>AccessibilitySettings</c>.</b> Esa clase de WinRT nace atada a un
/// <c>CoreWindow</c>, que una app de escritorio no tiene. <c>SystemParametersInfo</c> responde siempre y
/// sin depender del modelo de aplicación.</para>
/// </remarks>
internal static class HighContrast
{
    private const uint SpiGetHighContrast = 0x0042;
    private const uint HcfHighContrastOn  = 0x00000001;

    [StructLayout(LayoutKind.Sequential)]
    private struct HighContrastInfo
    {
        public uint Size;
        public uint Flags;
        public IntPtr DefaultScheme;
    }

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool SystemParametersInfoW(uint action, uint param, ref HighContrastInfo info, uint winIni);

    /// <summary>
    /// <c>true</c> si hay un tema de contraste activo. Se consulta cada vez: el usuario puede cambiarlo
    /// con la app abierta (Alt izq + Mayús izq + Impr Pant), y un valor cacheado dejaría la ventana
    /// pintada con la paleta anterior.
    /// </summary>
    public static bool IsActive
    {
        get
        {
            var info = new HighContrastInfo { Size = (uint)Marshal.SizeOf<HighContrastInfo>() };
            // Si la consulta falla, se responde que NO: el camino normal pinta los colores medidos de
            // SeverityPalette, que es lo correcto en un tema que no es de contraste.
            return SystemParametersInfoW(SpiGetHighContrast, info.Size, ref info, 0)
                && (info.Flags & HcfHighContrastOn) != 0;
        }
    }

    /// <summary>
    /// Pincel del tema activo por su clave de recurso del sistema (<c>SystemColorWindowTextColorBrush</c>
    /// y compañía), o <c>null</c> si esa clave no existiera.
    /// </summary>
    /// <remarks>
    /// Aquí <b>sí</b> vale <c>Application.Current.Resources</c>, al contrario que en el caso de
    /// `T13-17`: en alto contraste Windows manda sobre el tema forzado desde *Configuración*, así que
    /// los dos —el de la aplicación y el del elemento— resuelven al mismo diccionario.
    /// </remarks>
    public static Brush? SystemBrush(string resourceKey)
        => Application.Current.Resources.TryGetValue(resourceKey, out object? value) ? value as Brush : null;

    /// <summary>Color del texto y de los trazos del tema de contraste.</summary>
    public static Brush? Ink => SystemBrush("SystemColorWindowTextColorBrush");

    /// <summary>Color de resaltado del tema: lo que el propio tema usa para «esto está seleccionado».</summary>
    public static Brush? Highlight => SystemBrush("SystemColorHighlightColorBrush");

    /// <summary>Fondo de ventana del tema.</summary>
    public static Brush? Surface => SystemBrush("SystemColorWindowColorBrush");
}
