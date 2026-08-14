using System.ComponentModel;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;

namespace FormatDiskPro.UiTests;

/// <summary>
/// Hace <b>desaparecer</b> la unidad de pruebas a mitad de una operación, y la devuelve después.
///
/// Es la única forma de comprobar que los <c>catch</c> de los handlers <c>async void</c> (`T0-02`) llegan
/// a ejecutarse de verdad: hasta ahora estaba bajo test lo que <i>escriben</i>, no que se <i>disparen</i>.
///
/// <para><b>Por qué NO se usa <c>Set-Disk -IsOffline</c>.</b> Es lo primero que se intentó y Windows lo
/// rechaza de plano sobre este hardware: <i>«Removable media cannot be set to offline»</i>. Poner un disco
/// offline es una operación de discos fijos; una USB no la admite.</para>
///
/// <para><b>Por qué <c>FSCTL_DISMOUNT_VOLUME</c>.</b> Actúa un nivel más abajo, sobre el <b>volumen</b>, y
/// es lo que hace Windows por dentro al "quitar hardware de forma segura". A diferencia de
/// <c>FSCTL_LOCK_VOLUME</c> —que <b>falla</b> si alguien tiene archivos abiertos, justo el caso aquí— este
/// fuerza el desmontaje e <b>invalida los handles ya abiertos</b>. Eso es exactamente lo que le pasa a la
/// app cuando le quitan la USB de las manos: sus escrituras en curso empiezan a fallar. Y es reversible
/// solo: Windows remonta el volumen al primer acceso, sin desconectar ni reconectar nada.</para>
///
/// <para><b>Por qué no tirar del cable.</b> Sería el escenario literal, pero no es repetible ni
/// desatendido, y un tirón en mitad de una escritura es la forma más sucia de desmontar: sube el riesgo
/// de dejar el volumen necesitando <c>chkdsk</c>. El desmontaje forzado le da a Windows la oportunidad de
/// dejar el sistema de archivos consistente.</para>
/// </summary>
internal static class DriveYank
{
    /// <summary>
    /// Desmonta el volumen a la fuerza: los handles que la app tenga abiertos quedan inválidos y sus
    /// siguientes escrituras fallan, como si hubieran extraído la unidad.
    /// </summary>
    public static void ForceDismount(char letter)
    {
        // "\\.\G:" — el volumen, no el directorio raíz. Sin la barra final: con ella se abre el directorio.
        using SafeFileHandle handle = NativeMethods.CreateFile(
            $@"\\.\{char.ToUpperInvariant(letter)}:",
            NativeMethods.GENERIC_READ | NativeMethods.GENERIC_WRITE,
            NativeMethods.FILE_SHARE_READ | NativeMethods.FILE_SHARE_WRITE,
            IntPtr.Zero,
            NativeMethods.OPEN_EXISTING,
            0,
            IntPtr.Zero);

        if (handle.IsInvalid)
            throw new Win32Exception(Marshal.GetLastWin32Error(),
                $"No se pudo abrir el volumen {letter}: para desmontarlo (¿terminal sin elevar?).");

        if (!NativeMethods.DeviceIoControl(
                handle, NativeMethods.FSCTL_DISMOUNT_VOLUME,
                IntPtr.Zero, 0, IntPtr.Zero, 0, out _, IntPtr.Zero))
        {
            throw new Win32Exception(Marshal.GetLastWin32Error(),
                $"FSCTL_DISMOUNT_VOLUME falló sobre {letter}:.");
        }
    }

    /// <summary>
    /// Espera a que el volumen vuelva a estar accesible. No hay que "montarlo": Windows lo remonta solo
    /// en cuanto algo lo toca, y consultarlo aquí es justamente ese toque.
    /// </summary>
    public static void WaitForRemount(string expectedLabel, TimeSpan timeout)
    {
        var deadline = DateTime.UtcNow + timeout;
        while (DateTime.UtcNow < deadline)
        {
            if (TestDrive.FindLetter(expectedLabel) is not null) return;
            Thread.Sleep(500);
        }

        throw new InvalidOperationException(
            $"La partición '{expectedLabel}' no volvió tras el desmontaje en {timeout.TotalSeconds:0} s. " +
            "Compruébalo en el Explorador antes de seguir.");
    }

    private static class NativeMethods
    {
        internal const uint GENERIC_READ  = 0x80000000;
        internal const uint GENERIC_WRITE = 0x40000000;
        internal const uint FILE_SHARE_READ  = 0x00000001;
        internal const uint FILE_SHARE_WRITE = 0x00000002;
        internal const uint OPEN_EXISTING = 3;
        internal const uint FSCTL_DISMOUNT_VOLUME = 0x00090020;

        [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        internal static extern SafeFileHandle CreateFile(
            string lpFileName, uint dwDesiredAccess, uint dwShareMode, IntPtr lpSecurityAttributes,
            uint dwCreationDisposition, uint dwFlagsAndAttributes, IntPtr hTemplateFile);

        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool DeviceIoControl(
            SafeFileHandle hDevice, uint dwIoControlCode,
            IntPtr lpInBuffer, uint nInBufferSize, IntPtr lpOutBuffer, uint nOutBufferSize,
            out uint lpBytesReturned, IntPtr lpOverlapped);
    }
}
