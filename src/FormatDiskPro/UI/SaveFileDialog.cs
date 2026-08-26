using System.Runtime.InteropServices;

namespace FormatDiskPro.UI;

/// <summary>
/// Diálogo «Guardar como» de Windows, por COM (<c>IFileSaveDialog</c>, el <i>Common Item Dialog</i> de
/// Vista en adelante).
///
/// <para><b>Por qué no se usa <c>Windows.Storage.Pickers.FileSavePicker</c>.</b> Porque en esta app no
/// funciona, y no funcionaba desde el primer día. FormatDiskPro se ejecuta <b>siempre elevada</b>
/// (<c>requireAdministrator</c> en el manifiesto), y el selector de WinRT delega en un intermediario que
/// rechaza a los procesos elevados: <c>PickSaveFileAsync</c> lanza <c>COMException 0x80004005</c>
/// <b>en el acto</b>, sin llegar a mostrar ninguna ventana. Medido contra el .exe real —una sonda de UI
/// pulsó <i>Exportar CSV</i> y no apareció ningún HWND nuevo—, y antes de eso escrito cuatro veces en el
/// historial de la máquina de desarrollo como <c>EXPORT ERROR:</c> a secas, porque la <c>Message</c> de
/// esa excepción viene <b>vacía</b>.</para>
///
/// <para>El diálogo COM no pasa por ese intermediario: lo crea el propio proceso, así que la elevación le
/// da igual. Es además el mismo diálogo moderno que usa el resto de Windows —no el
/// <c>GetSaveFileName</c> de los noventa—, con sus accesos rápidos y su barra de navegación.</para>
///
/// <para>De la interfaz se declaran los métodos <b>en orden de vtable</b> hasta el último que se usa: en
/// COM el orden ES el contrato, así que ninguno puede saltarse ni reordenarse, y los que quedan por
/// encima del último necesario simplemente no se declaran.</para>
/// </summary>
internal static class SaveFileDialog
{
    private const uint FOS_OVERWRITEPROMPT  = 0x00000002;   // pregunta antes de pisar un archivo
    private const uint FOS_FORCEFILESYSTEM  = 0x00000040;   // solo rutas reales (nada de bibliotecas virtuales)
    private const uint FOS_PATHMUSTEXIST    = 0x00000800;
    private const uint FOS_NOREADONLYRETURN = 0x00008000;

    /// <summary>El usuario cerró el diálogo sin guardar. No es un error: es la respuesta «no».</summary>
    private const int ERROR_CANCELLED_HR = unchecked((int)0x800704C7);

    private const int SIGDN_FILESYSPATH = unchecked((int)0x80058000);

    [ComImport, Guid("C0B4E2F3-BA21-4773-8DBA-335EC946EB8B")]
    private class FileSaveDialogRcw { }

    [ComImport, Guid("43826D1E-E718-42EE-BC55-A1E261C37BFE"),
     InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    private interface IShellItem
    {
        void BindToHandler(IntPtr pbc, ref Guid bhid, ref Guid riid, out IntPtr ppv);
        void GetParent(out IShellItem ppsi);
        void GetDisplayName(int sigdnName, [MarshalAs(UnmanagedType.LPWStr)] out string ppszName);
        void GetAttributes(uint sfgaoMask, out uint psfgaoAttribs);
        void Compare(IShellItem psi, uint hint, out int piOrder);
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct FilterSpec
    {
        [MarshalAs(UnmanagedType.LPWStr)] public string Name;
        [MarshalAs(UnmanagedType.LPWStr)] public string Spec;
    }

    // IID de IFileSaveDialog. La vtable empieza por IModalWindow e IFileDialog, así que declarar solo
    // esos métodos —en su orden— es correcto: lo que IFileSaveDialog añade va DETRÁS y no se usa.
    [ComImport, Guid("84BCCD23-5FDE-4CDB-AEA4-AF64B83D78AB"),
     InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    private interface IFileSaveDialog
    {
        [PreserveSig] int Show(IntPtr parent);                                     // IModalWindow
        void SetFileTypes(uint cFileTypes, [MarshalAs(UnmanagedType.LPArray)] FilterSpec[] rgFilterSpec);
        void SetFileTypeIndex(uint iFileType);
        void GetFileTypeIndex(out uint piFileType);
        void Advise(IntPtr pfde, out uint pdwCookie);
        void Unadvise(uint dwCookie);
        void SetOptions(uint fos);
        void GetOptions(out uint pfos);
        void SetDefaultFolder(IShellItem psi);
        void SetFolder(IShellItem psi);
        void GetFolder(out IShellItem ppsi);
        void GetCurrentSelection(out IShellItem ppsi);
        void SetFileName([MarshalAs(UnmanagedType.LPWStr)] string pszName);
        void GetFileName([MarshalAs(UnmanagedType.LPWStr)] out string pszName);
        void SetTitle([MarshalAs(UnmanagedType.LPWStr)] string pszTitle);
        void SetOkButtonLabel([MarshalAs(UnmanagedType.LPWStr)] string pszText);
        void SetFileNameLabel([MarshalAs(UnmanagedType.LPWStr)] string pszLabel);
        void GetResult(out IShellItem ppsi);
        void AddPlace(IShellItem psi, int fdap);
        void SetDefaultExtension([MarshalAs(UnmanagedType.LPWStr)] string pszDefaultExtension);
    }

    [DllImport("shell32.dll", CharSet = CharSet.Unicode, PreserveSig = false)]
    private static extern void SHCreateItemFromParsingName(
        [MarshalAs(UnmanagedType.LPWStr)] string pszPath, IntPtr pbc, ref Guid riid,
        [MarshalAs(UnmanagedType.Interface)] out IShellItem ppv);

    /// <summary>
    /// Pide al usuario dónde guardar y devuelve la ruta elegida, o <c>null</c> si cancela.
    /// </summary>
    /// <param name="owner">Ventana propietaria: la hace modal y evita que quede detrás.</param>
    /// <param name="title">Título del diálogo, ya localizado.</param>
    /// <param name="suggestedName">Nombre propuesto, sin extensión.</param>
    /// <param name="filterName">Nombre visible del tipo de archivo (p. ej. «CSV»), ya localizado.</param>
    /// <param name="extension">Extensión con punto, p. ej. <c>.csv</c>.</param>
    /// <param name="initialFolder">Carpeta inicial; se ignora si no existe.</param>
    /// <returns>Ruta completa elegida, o <c>null</c> si el usuario canceló.</returns>
    /// <exception cref="COMException">Si el diálogo falla por algo que no sea la cancelación.</exception>
    public static string? Show(IntPtr owner, string title, string suggestedName,
                               string filterName, string extension, string? initialFolder = null)
    {
        var dialog = (IFileSaveDialog)new FileSaveDialogRcw();
        try
        {
            dialog.SetTitle(title);
            dialog.SetFileTypes(1, [new FilterSpec { Name = filterName, Spec = "*" + extension }]);
            dialog.SetFileTypeIndex(1);            // 1, no 0: el índice de tipo de archivo es 1-based
            dialog.SetDefaultExtension(extension.TrimStart('.'));
            dialog.SetFileName(suggestedName + extension);
            dialog.SetOptions(FOS_OVERWRITEPROMPT | FOS_FORCEFILESYSTEM | FOS_PATHMUSTEXIST | FOS_NOREADONLYRETURN);

            // La carpeta inicial es una comodidad, no un requisito: si no existe o el shell la rechaza,
            // el diálogo abre donde Windows recuerde y el usuario navega. Fallar por esto sería absurdo.
            if (!string.IsNullOrEmpty(initialFolder) && Directory.Exists(initialFolder))
            {
                try
                {
                    var iid = typeof(IShellItem).GUID;
                    SHCreateItemFromParsingName(initialFolder, IntPtr.Zero, ref iid, out var folder);
                    dialog.SetFolder(folder);
                }
                catch (COMException) { }
            }

            int hr = dialog.Show(owner);
            if (hr == ERROR_CANCELLED_HR) return null;
            if (hr < 0) throw new COMException("IFileSaveDialog::Show falló (HRESULT 0x" + hr.ToString("X8") + ").", hr);

            dialog.GetResult(out var item);
            item.GetDisplayName(SIGDN_FILESYSPATH, out string path);
            return path;
        }
        finally
        {
            // El RCW mantiene viva la referencia COM: sin esto el diálogo se libera cuando al recolector
            // le parece bien, y hasta entonces el objeto sigue vivo en el apartamento.
            Marshal.FinalReleaseComObject(dialog);
        }
    }
}
