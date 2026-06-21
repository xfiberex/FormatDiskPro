namespace FormatDiskPro;

public enum AppLang { Es, En }

/// <summary>
/// Proveedor de cadenas localizadas (ES/EN). Uso: L.T("clave") o L.T("clave", arg0, ...).
/// </summary>
public static class L
{
    public static AppLang Current { get; private set; } = AppLang.Es;

    /// <summary>Se dispara cuando cambia el idioma para que la UI se refresque.</summary>
    public static event Action? Changed;

    public static void Set(AppLang lang)
    {
        if (Current == lang) return;
        Current = lang;
        Changed?.Invoke();
    }

    public static string T(string key)
    {
        if (Map.TryGetValue(key, out var pair))
            return Current == AppLang.Es ? pair.Es : pair.En;
        return key; // defensivo: nunca lanza
    }

    public static string T(string key, params object[] args) => string.Format(T(key), args);

    private static readonly Dictionary<string, (string Es, string En)> Map = new()
    {
        ["app.subtitle"]     = ("Seleccione una unidad para formatear", "Select a drive to format"),
        ["drive.label"]      = ("Unidad:", "Drive:"),
        ["section.drive"]    = ("Unidad", "Drive"),
        ["section.format"]   = ("Configuración de formato", "Format settings"),
        ["fs.label"]         = ("Sistema de archivos", "File system"),
        ["alloc.label"]      = ("Tamaño de unidad de asignación", "Allocation unit size"),
        ["label.label"]      = ("Etiqueta del volumen:", "Volume label:"),
        ["options.group"]    = ("Opciones de formato", "Format options"),
        ["opt.quick"]        = ("Formato rápido", "Quick format"),
        ["opt.compress"]     = ("Habilitar compresión (sólo NTFS)", "Enable compression (NTFS only)"),
        ["opt.secure"]       = ("Borrado seguro (sobrescribir espacio libre)", "Secure erase (overwrite free space)"),
        ["btn.restore"]      = ("Restaurar valores predeterminados", "Restore defaults"),
        ["btn.start"]        = ("Iniciar", "Start"),
        ["btn.close"]        = ("Cerrar", "Close"),
        ["btn.cancel"]       = ("Cancelar", "Cancel"),
        ["tip.refresh"]      = ("Actualizar lista de unidades", "Refresh drive list"),

        ["info.total"]       = ("Total: {0}", "Total: {0}"),
        ["info.free"]        = ("Libre: {0}", "Free: {0}"),
        ["info.fs"]          = ("Sistema actual: {0}", "Current FS: {0}"),
        ["info.type"]        = ("Tipo: {0}", "Type: {0}"),
        ["info.health"]      = ("Salud: {0}", "Health: {0}"),
        ["info.bus"]         = ("Conexión: {0}", "Bus: {0}"),
        ["info.dash"]        = ("–", "–"),
        ["info.loading"]     = ("consultando…", "querying…"),

        ["menu.tools"]       = ("Herramientas", "Tools"),
        ["menu.verify"]      = ("Verificar capacidad real…", "Verify real capacity…"),
        ["menu.eject"]       = ("Expulsar unidad", "Eject drive"),
        ["menu.history"]     = ("Ver historial", "View history"),
        ["menu.config"]      = ("Configuración", "Settings"),
        ["menu.lang"]        = ("Idioma", "Language"),
        ["menu.lang.es"]     = ("Español", "Spanish"),
        ["menu.lang.en"]     = ("Inglés", "English"),
        ["menu.theme"]       = ("Tema", "Theme"),
        ["menu.theme.auto"]  = ("Automático", "Automatic"),
        ["menu.theme.light"] = ("Claro", "Light"),
        ["menu.theme.dark"]  = ("Oscuro", "Dark"),
        ["menu.presets"]     = ("Presets", "Presets"),
        ["menu.help"]        = ("Ayuda", "Help"),
        ["menu.about"]       = ("Acerca de…", "About…"),
        ["menu.updates"]     = ("Buscar actualizaciones…", "Check for updates…"),

        ["type.fixed"]       = ("Disco fijo", "Fixed disk"),
        ["type.removable"]   = ("USB / Removible", "USB / Removable"),
        ["type.ram"]         = ("Disco RAM", "RAM disk"),
        ["type.network"]     = ("Red", "Network"),
        ["type.cdrom"]       = ("CD/DVD", "CD/DVD"),
        ["type.unknown"]     = ("Desconocido", "Unknown"),

        ["protected.tag"]    = ("[Protegido] ", "[Protected] "),
        ["protected.status"] = ("⚠  Disco fijo protegido — el formateo está deshabilitado.",
                                "⚠  Fixed disk protected — formatting is disabled."),

        ["fmt.quick"]        = ("rápido", "quick"),
        ["fmt.full"]         = ("completo", "full"),
        ["status.formatting"]= ("Formateando {0}: ({1})...", "Formatting {0}: ({1})..."),
        ["status.cancelled"] = ("Operación cancelada.", "Operation cancelled."),
        ["status.success"]   = ("Formato completado con éxito.", "Format completed successfully."),
        ["status.error"]     = ("Error durante el formato.", "Error during format."),
        ["status.unexpected"]= ("Error inesperado.", "Unexpected error."),
        ["status.wiping"]    = ("Borrado seguro (sobrescribiendo espacio libre)...", "Secure erase (overwriting free space)..."),
        ["status.verifying"] = ("Verificando capacidad...", "Verifying capacity..."),
        ["status.ejected"]   = ("Unidad expulsada.", "Drive ejected."),

        ["msg.warning"]      = ("Advertencia", "Warning"),
        ["msg.error"]        = ("Error", "Error"),
        ["msg.info"]         = ("Información", "Information"),
        ["msg.selectDrive"]  = ("Seleccione una unidad.", "Select a drive."),
        ["msg.selectFsAlloc"]= ("Seleccione el sistema de archivos y el tamaño de unidad.",
                                "Select the file system and allocation unit size."),
        ["msg.systemTitle"]  = ("Operación no permitida", "Operation not allowed"),
        ["msg.systemBody"]   = ("No se puede formatear la unidad que contiene Windows.",
                                "Cannot format the drive that contains Windows."),
        ["msg.protTitle"]    = ("Disco protegido", "Protected disk"),
        ["msg.protBody"]     = ("Este es un disco fijo protegido. La operación no está permitida.",
                                "This is a protected fixed disk. The operation is not allowed."),
        ["msg.invalidLabel"] = ("La etiqueta contiene caracteres no válidos:\n\\ / : * ? \" < > |",
                                "The label contains invalid characters:\n\\ / : * ? \" < > |"),
        ["msg.invalidTitle"] = ("Etiqueta inválida", "Invalid label"),
        ["msg.labelLongTitle"]= ("Etiqueta demasiado larga", "Label too long"),
        ["msg.labelLong"]    = ("La etiqueta supera el máximo de {0} caracteres para {1}.",
                                "The label exceeds the maximum of {0} characters for {1}."),
        ["msg.goneTitle"]    = ("Unidad no disponible", "Drive unavailable"),
        ["msg.goneBody"]     = ("La unidad {0}: ya no está disponible. Actualice la lista.",
                                "Drive {0}: is no longer available. Refresh the list."),

        ["confirm.title"]    = ("Confirmar formato", "Confirm format"),
        ["confirm.warning"]  = ("ADVERTENCIA: Se destruirán TODOS los datos en:",
                                "WARNING: ALL data will be destroyed on:"),
        ["confirm.drive"]    = ("Unidad", "Drive"),
        ["confirm.fs"]       = ("Sistema", "File system"),
        ["confirm.cluster"]  = ("Cluster", "Cluster"),
        ["confirm.label"]    = ("Etiqueta", "Label"),
        ["confirm.nolabel"]  = ("(sin etiqueta)", "(no label)"),
        ["confirm.mode"]     = ("Tipo", "Mode"),
        ["confirm.secure"]   = ("Borrado seguro", "Secure erase"),
        ["confirm.yes"]      = ("Sí", "Yes"),
        ["confirm.no"]       = ("No", "No"),
        ["confirm.prompt"]   = ("Para confirmar, escriba la letra de la unidad ({0}):",
                                "To confirm, type the drive letter ({0}):"),

        ["success.title"]    = ("Éxito", "Success"),
        ["success.body"]     = ("La unidad {0}: se formateó correctamente con {1}.",
                                "Drive {0}: was formatted successfully with {1}."),
        ["error.formatTitle"]= ("Error de formato", "Format error"),
        ["error.formatBody"] = ("Error al formatear la unidad {0}:\n\n{1}",
                                "Error formatting drive {0}:\n\n{1}"),
        ["cancel.title"]     = ("Cancelar operación", "Cancel operation"),
        ["cancel.body"]      = ("¿Cancelar la operación en curso?\n\nNota: la unidad puede quedar en un estado no utilizable.",
                                "Cancel the operation in progress?\n\nNote: the drive may be left in an unusable state."),
        ["closing.title"]    = ("Operación en progreso", "Operation in progress"),
        ["closing.body"]     = ("Utilice el botón Cancelar para detener la operación.",
                                "Use the Cancel button to stop the operation."),

        ["eject.fail"]       = ("No se pudo expulsar la unidad. Asegúrese de que no esté en uso.",
                                "Could not eject the drive. Make sure it is not in use."),
        ["eject.fixed"]      = ("Solo se pueden expulsar unidades removibles.",
                                "Only removable drives can be ejected."),

        ["verify.title"]     = ("Verificar capacidad real", "Verify real capacity"),
        ["verify.warn"]      = ("Esta prueba escribirá datos en el espacio libre de {0}: para detectar capacidad falsa.\n\nPuede tardar varios minutos. ¿Continuar?",
                                "This test will write data to the free space of {0}: to detect fake capacity.\n\nIt may take several minutes. Continue?"),
        ["verify.writing"]   = ("Verificando (escribiendo): {0}", "Verifying (writing): {0}"),
        ["verify.reading"]   = ("Verificando (leyendo): {0}", "Verifying (reading): {0}"),
        ["verify.okTitle"]   = ("Capacidad verificada", "Capacity verified"),
        ["verify.okBody"]    = ("La unidad {0}: es auténtica.\nDatos verificados: {1}.",
                                "Drive {0}: is genuine.\nData verified: {1}."),
        ["verify.failTitle"] = ("¡Capacidad falsa detectada!", "Fake capacity detected!"),
        ["verify.failBody"]  = ("La unidad {0}: falló la verificación: los datos no coinciden tras {1}.\n\nProbablemente sea una unidad falsificada.",
                                "Drive {0}: failed verification: data mismatch after {1}.\n\nIt is likely a counterfeit drive."),

        ["about.title"]      = ("Acerca de FormatDiskPro", "About FormatDiskPro"),
        ["about.body"]       = ("FormatDiskPro v{0}\n\nHerramienta de formateo y gestión de unidades para Windows.\nNTFS · exFAT · ReFS · FAT32 · FAT\n\n.NET 10 · WinUI 3",
                                "FormatDiskPro v{0}\n\nDisk format and management tool for Windows.\nNTFS · exFAT · ReFS · FAT32 · FAT\n\n.NET 10 · WinUI 3"),

        ["preset.title"]     = ("Preset aplicado", "Preset applied"),
        ["preset.body"]      = ("Configuración «{0}» aplicada.", "Preset \"{0}\" applied."),
        ["preset.na"]        = ("El preset «{0}» no es compatible con esta unidad.",
                                "Preset \"{0}\" is not compatible with this drive."),

        ["update.checking"]  = ("Buscando actualizaciones…", "Checking for updates…"),
        ["update.uptodate"]  = ("Ya tienes la última versión ({0}).", "You already have the latest version ({0})."),
        ["update.availTitle"]= ("Actualización disponible", "Update available"),
        ["update.available"] = ("Hay una nueva versión disponible: {0}\n(versión actual: {1})\n\n¿Descargar e instalar ahora?",
                                "A new version is available: {0}\n(current version: {1})\n\nDownload and install now?"),
        ["update.downloading"]=("Descargando actualización… {0}%", "Downloading update… {0}%"),
        ["update.launching"] = ("Iniciando el instalador…", "Launching installer…"),
        ["update.noasset"]   = ("La versión {0} no incluye un instalador descargable. Se abrirá la página de la versión.",
                                "Release {0} has no downloadable installer. Opening the release page."),
        ["update.error"]     = ("No se pudo completar la operación de actualización:\n{0}",
                                "The update operation could not be completed:\n{0}"),
    };
}
