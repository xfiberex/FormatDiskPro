namespace FormatDiskPro;

/// <summary>
/// Constantes y lógica pura para interpretar el mensaje Win32 <c>WM_DEVICECHANGE</c>, usado para
/// refrescar automáticamente la lista de unidades al conectar/desconectar dispositivos. El enganche
/// real del mensaje (subclassing de la ventana) vive en la capa UI.
/// </summary>
public static class DeviceChange
{
    /// <summary>Mensaje de Windows que notifica cambios de hardware/dispositivos.</summary>
    public const uint WmDeviceChange = 0x0219;

    /// <summary>wParam: un dispositivo (p. ej. un volumen USB) se ha conectado y está disponible.</summary>
    public const nuint DbtDeviceArrival = 0x8000;

    /// <summary>wParam: un dispositivo se ha retirado por completo.</summary>
    public const nuint DbtDeviceRemoveComplete = 0x8004;

    /// <summary>
    /// ¿El <paramref name="wParam"/> de <c>WM_DEVICECHANGE</c> indica una conexión o desconexión
    /// completa de dispositivo (y por tanto conviene recargar las unidades)? Ignora el resto de
    /// subtipos (consultas, cambios pendientes, etc.). Lógica pura.
    /// </summary>
    public static bool IsArrivalOrRemoval(nuint wParam) =>
        wParam == DbtDeviceArrival || wParam == DbtDeviceRemoveComplete;
}
