namespace FormatDiskPro;

public enum AppLang { Es, En, Pt, Fr, It }

/// <summary>
/// Proveedor de cadenas localizadas (ES/EN/PT/FR/IT). Uso: L.T("clave") o L.T("clave", arg0, ...).
/// Cada entrada del diccionario es un arreglo indexado por <see cref="AppLang"/> (orden Es, En, Pt, Fr, It).
/// </summary>
public static class L
{
    public static AppLang Current { get; private set; } = AppLang.Es;

    public static void Set(AppLang lang)
    {
        if (Current == lang) return;
        Current = lang;
    }

    /// <summary>
    /// Idioma a partir del nombre de una cultura .NET (p. ej. <c>"es-ES"</c>, <c>"pt-BR"</c>, <c>"fr"</c>):
    /// toma la parte de idioma de dos letras (antes de <c>-</c>/<c>_</c>) y la mapea con <see cref="FromCode"/>.
    /// Desconocido o vacío → Es. Lógica pura; se usa para sembrar el idioma en el primer arranque.
    /// </summary>
    /// <param name="cultureName">Nombre de la cultura, p. ej. <see cref="System.Globalization.CultureInfo.Name"/>.</param>
    public static AppLang FromCulture(string? cultureName)
    {
        if (string.IsNullOrWhiteSpace(cultureName)) return AppLang.Es;
        string lang = cultureName.Trim();
        int sep = lang.IndexOfAny(['-', '_']);
        if (sep > 0) lang = lang[..sep];
        return FromCode(lang);
    }

    /// <summary>Convierte un código ISO (<c>"es"/"en"/"pt"/"fr"/"it"</c>) al idioma; desconocido → Es.</summary>
    public static AppLang FromCode(string? code) => code?.Trim().ToLowerInvariant() switch
    {
        "en" => AppLang.En,
        "pt" => AppLang.Pt,
        "fr" => AppLang.Fr,
        "it" => AppLang.It,
        _    => AppLang.Es,
    };

    /// <summary>Código ISO del idioma (<c>"es"/"en"/"pt"/"fr"/"it"</c>).</summary>
    public static string ToCode(AppLang lang) => lang switch
    {
        AppLang.En => "en",
        AppLang.Pt => "pt",
        AppLang.Fr => "fr",
        AppLang.It => "it",
        _          => "es",
    };

    public static string T(string key)
    {
        if (Map.TryGetValue(key, out var arr))
        {
            int i = (int)Current;
            return i >= 0 && i < arr.Length && !string.IsNullOrEmpty(arr[i]) ? arr[i] : arr[0];
        }
        return key; // defensivo: nunca lanza
    }

    public static string T(string key, params object[] args) => string.Format(T(key), args);

    /// <summary>Diccionario de traducciones. Orden de cada arreglo: <c>[Es, En, Pt, Fr, It]</c>.</summary>
    internal static readonly Dictionary<string, string[]> Map = new()
    {        ["section.drive"]    = ["Unidad", "Drive", "Unidade", "Lecteur", "Unità"],
        ["section.format"]   = ["Configuración de formato", "Format settings", "Configurações de formatação", "Paramètres de formatage", "Impostazioni di formattazione"],
        ["fs.label"]         = ["Sistema de archivos", "File system", "Sistema de arquivos", "Système de fichiers", "File system"],
        ["alloc.label"]      = ["Tamaño de unidad de asignación", "Allocation unit size", "Tamanho da unidade de alocação", "Taille d'unité d'allocation", "Dimensione unità di allocazione"],
        ["label.label"]      = ["Etiqueta del volumen:", "Volume label:", "Rótulo do volume:", "Nom de volume :", "Etichetta del volume:"],
        ["options.group"]    = ["Opciones de formato", "Format options", "Opções de formatação", "Options de formatage", "Opzioni di formattazione"],
        ["opt.quick"]        = ["Formato rápido", "Quick format", "Formatação rápida", "Formatage rapide", "Formattazione rapida"],
        ["opt.compress"]     = ["Habilitar compresión (sólo NTFS)", "Enable compression (NTFS only)", "Ativar compactação (apenas NTFS)", "Activer la compression (NTFS uniquement)", "Abilita compressione (solo NTFS)"],
        ["opt.secure"]       = ["Borrado seguro (sobrescribir espacio libre)", "Secure erase (overwrite free space)", "Apagamento seguro (sobrescrever espaço livre)", "Effacement sécurisé (écraser l'espace libre)", "Cancellazione sicura (sovrascrivi spazio libero)"],
        ["opt.passes"]       = ["Pasadas:", "Passes:", "Passagens:", "Passes :", "Passaggi:"],
        ["opt.smallFat32"]     = ["Crear solo una partición FAT32 pequeña y dejar el resto sin asignar", "Create only a small FAT32 partition and leave the rest unallocated", "Criar apenas uma partição FAT32 pequena e deixar o resto não alocado", "Créer uniquement une petite partition FAT32 et laisser le reste non alloué", "Crea solo una piccola partizione FAT32 e lascia il resto non allocato"],
        ["opt.smallFat32Size"] = ["Tamaño:", "Size:", "Tamanho:", "Taille :", "Dimensione:"],
        ["opt.smallFat32Go"]   = ["Reinicializar unidad ahora…", "Reinitialize drive now…", "Reinicializar unidade agora…", "Réinitialiser le lecteur maintenant…", "Reinizializza unità adesso…"],
        ["opt.smallFat32Hint"] = ["Windows no permite crear volúmenes FAT32 de más de {0}. Marca esta opción y usa Herramientas → Reinicializar unidad… (no el botón Iniciar) para crear una partición FAT32 de {1} —útil, por ejemplo, para actualizar el BIOS/UEFI de una placa base—, dejando el resto del disco sin asignar; el sistema de archivos del selector se ignora. Recuerda: FAT32 no admite archivos de más de 4 GB.", "Windows won't create FAT32 volumes larger than {0}. Check this box and use Tools → Reinitialize drive… (not the Start button) to create a {1} FAT32 partition —handy, for example, to flash a motherboard's BIOS/UEFI—, leaving the rest of the disk unallocated; the file system selector is ignored. Remember: FAT32 doesn't support files larger than 4 GB.", "O Windows não permite criar volumes FAT32 maiores que {0}. Marque esta opção e use Ferramentas → Reinicializar unidade… (não o botão Iniciar) para criar uma partição FAT32 de {1} —útil, por exemplo, para atualizar a BIOS/UEFI de uma placa-mãe—, deixando o restante do disco não alocado; o sistema de arquivos do seletor é ignorado. Lembre-se: o FAT32 não suporta arquivos maiores que 4 GB.", "Windows ne permet pas de créer des volumes FAT32 de plus de {0}. Cochez cette option et utilisez Outils → Réinitialiser le lecteur… (pas le bouton Démarrer) pour créer une partition FAT32 de {1} —utile, par exemple, pour flasher le BIOS/UEFI d'une carte mère—, laissant le reste du disque non alloué ; le système de fichiers du sélecteur est ignoré. Rappel : FAT32 ne prend pas en charge les fichiers de plus de 4 Go.", "Windows non consente di creare volumi FAT32 più grandi di {0}. Seleziona questa opzione e usa Strumenti → Reinizializza unità… (non il pulsante Avvia) per creare una partizione FAT32 da {1} —utile, ad esempio, per aggiornare il BIOS/UEFI di una scheda madre—, lasciando non allocato il resto del disco; il file system del selettore viene ignorato. Ricorda: FAT32 non supporta file più grandi di 4 GB."],
        ["btn.restore"]      = ["Restaurar valores predeterminados", "Restore defaults", "Restaurar padrões", "Restaurer les valeurs par défaut", "Ripristina predefiniti"],
        ["btn.start"]        = ["Iniciar", "Start", "Iniciar", "Démarrer", "Avvia"],
        ["btn.close"]        = ["Cerrar", "Close", "Fechar", "Fermer", "Chiudi"],
        ["btn.cancel"]       = ["Cancelar", "Cancel", "Cancelar", "Annuler", "Annulla"],
        ["tip.refresh"]      = ["Actualizar lista de unidades", "Refresh drive list", "Atualizar lista de unidades", "Actualiser la liste des lecteurs", "Aggiorna elenco unità"],
        ["drive.none"]       = ["No hay unidades — conecta un dispositivo", "No drives — connect a device", "Sem unidades — conecte um dispositivo", "Aucun lecteur — connectez un périphérique", "Nessuna unità — collega un dispositivo"],

        ["info.total"]       = ["Total: {0}", "Total: {0}", "Total: {0}", "Total : {0}", "Totale: {0}"],
        ["info.free"]        = ["Libre: {0}", "Free: {0}", "Livre: {0}", "Libre : {0}", "Libero: {0}"],
        ["info.fs"]          = ["Sistema actual: {0}", "Current FS: {0}", "Sistema atual: {0}", "Système actuel : {0}", "Sistema attuale: {0}"],
        ["info.type"]        = ["Tipo: {0}", "Type: {0}", "Tipo: {0}", "Type : {0}", "Tipo: {0}"],
        ["info.health"]      = ["Salud: {0}", "Health: {0}", "Saúde: {0}", "Santé : {0}", "Stato: {0}"],
        ["info.bus"]         = ["Conexión: {0}", "Bus: {0}", "Conexão: {0}", "Connexion : {0}", "Connessione: {0}"],
        ["info.used"]        = ["Espacio utilizado", "Used space", "Espaço utilizado", "Espace utilisé", "Spazio utilizzato"],
        ["info.dash"]        = ["–", "–", "–", "–", "–"],
        ["info.loading"]     = ["consultando…", "querying…", "consultando…", "interrogation…", "interrogazione…"],

        ["menu.tools"]       = ["Herramientas", "Tools", "Ferramentas", "Outils", "Strumenti"],
        ["menu.verify"]      = ["Verificar capacidad real…", "Verify real capacity…", "Verificar capacidade real…", "Vérifier la capacité réelle…", "Verifica capacità reale…"],
        ["menu.health"]      = ["Salud del disco (S.M.A.R.T.)…", "Disk health (S.M.A.R.T.)…", "Saúde do disco (S.M.A.R.T.)…", "Santé du disque (S.M.A.R.T.)…", "Stato del disco (S.M.A.R.T.)…"],
        ["menu.check"]       = ["Comprobar errores (chkdsk)…", "Check for errors (chkdsk)…", "Verificar erros (chkdsk)…", "Vérifier les erreurs (chkdsk)…", "Controlla errori (chkdsk)…"],
        ["menu.benchmark"]   = ["Benchmark rápido (lectura/escritura)…", "Quick benchmark (read/write)…", "Benchmark rápido (leitura/escrita)…", "Benchmark rapide (lecture/écriture)…", "Benchmark rapido (lettura/scrittura)…"],
        ["menu.unlock"]      = ["Quitar protección de escritura…", "Remove write protection…", "Remover proteção contra gravação…", "Supprimer la protection en écriture…", "Rimuovi protezione da scrittura…"],
        ["menu.reinit"]      = ["Reinicializar unidad…", "Reinitialize drive…", "Reinicializar unidade…", "Réinitialiser le lecteur…", "Reinizializza unità…"],
        ["menu.eject"]       = ["Expulsar unidad", "Eject drive", "Ejetar unidade", "Éjecter le lecteur", "Espelli unità"],
        ["menu.history"]     = ["Ver historial", "View history", "Ver histórico", "Voir l'historique", "Visualizza cronologia"],
        ["menu.config"]      = ["Configuración", "Settings", "Configurações", "Paramètres", "Impostazioni"],
        ["menu.lang"]        = ["Idioma", "Language", "Idioma", "Langue", "Lingua"],
        ["menu.lang.es"]     = ["Español", "Spanish", "Espanhol", "Espagnol", "Spagnolo"],
        ["menu.lang.en"]     = ["Inglés", "English", "Inglês", "Anglais", "Inglese"],
        ["menu.lang.pt"]     = ["Portugués", "Portuguese", "Português", "Portugais", "Portoghese"],
        ["menu.lang.fr"]     = ["Francés", "French", "Francês", "Français", "Francese"],
        ["menu.lang.it"]     = ["Italiano", "Italian", "Italiano", "Italien", "Italiano"],
        ["menu.theme"]       = ["Tema", "Theme", "Tema", "Thème", "Tema"],
        ["menu.theme.auto"]  = ["Automático", "Automatic", "Automático", "Automatique", "Automatico"],
        ["menu.theme.light"] = ["Claro", "Light", "Claro", "Clair", "Chiaro"],
        ["menu.theme.dark"]  = ["Oscuro", "Dark", "Escuro", "Sombre", "Scuro"],
        ["menu.presets"]     = ["Presets", "Presets", "Predefinições", "Préréglages", "Preset"],
        ["menu.managePresets"]= ["Gestionar presets…", "Manage presets…", "Gerenciar predefinições…", "Gérer les préréglages…", "Gestisci preset…"],
        ["menu.notify"]      = ["Avisar al terminar", "Notify when finished", "Avisar ao terminar", "Avertir à la fin", "Avvisa al termine"],
        ["menu.help"]        = ["Ayuda", "Help", "Ajuda", "Aide", "Aiuto"],
        ["menu.about"]       = ["Acerca de…", "About…", "Sobre…", "À propos…", "Informazioni…"],
        ["menu.updates"]     = ["Buscar actualizaciones…", "Check for updates…", "Procurar atualizações…", "Rechercher des mises à jour…", "Cerca aggiornamenti…"],
        ["menu.whatsnew"]    = ["Novedades…", "What's new…", "Novidades…", "Nouveautés…", "Novità…"],

        ["type.fixed"]       = ["Disco fijo", "Fixed disk", "Disco fixo", "Disque fixe", "Disco fisso"],
        ["type.removable"]   = ["USB / Removible", "USB / Removable", "USB / Removível", "USB / Amovible", "USB / Rimovibile"],
        ["type.ram"]         = ["Disco RAM", "RAM disk", "Disco RAM", "Disque RAM", "Disco RAM"],
        ["type.network"]     = ["Red", "Network", "Rede", "Réseau", "Rete"],
        ["type.cdrom"]       = ["CD/DVD", "CD/DVD", "CD/DVD", "CD/DVD", "CD/DVD"],
        ["type.unknown"]     = ["Desconocido", "Unknown", "Desconhecido", "Inconnu", "Sconosciuto"],

        ["protected.tag"]    = ["[Protegido] ", "[Protected] ", "[Protegido] ", "[Protégé] ", "[Protetto] "],
        ["protected.status"] = ["Disco fijo protegido — el formateo está deshabilitado.", "Fixed disk protected — formatting is disabled.", "Disco fixo protegido — a formatação está desativada.", "Disque fixe protégé — le formatage est désactivé.", "Disco fisso protetto — la formattazione è disabilitata."],

        ["fmt.quick"]        = ["rápido", "quick", "rápido", "rapide", "rapida"],
        ["fmt.full"]         = ["completo", "full", "completo", "complet", "completa"],
        ["status.formatting"]= ["Formateando {0}: ({1})...", "Formatting {0}: ({1})...", "Formatando {0}: ({1})...", "Formatage de {0}: ({1})...", "Formattazione di {0}: ({1})..."],
        ["status.cancelled"] = ["Operación cancelada.", "Operation cancelled.", "Operação cancelada.", "Opération annulée.", "Operazione annullata."],
        ["status.success"]   = ["Formato completado con éxito.", "Format completed successfully.", "Formatação concluída com sucesso.", "Formatage terminé avec succès.", "Formattazione completata con successo."],
        ["status.error"]     = ["Error durante el formato.", "Error during format.", "Erro durante a formatação.", "Erreur pendant le formatage.", "Errore durante la formattazione."],
        ["status.unexpected"]= ["Error inesperado.", "Unexpected error.", "Erro inesperado.", "Erreur inattendue.", "Errore imprevisto."],
        ["status.wiping"]    = ["Borrado seguro (sobrescribiendo espacio libre)...", "Secure erase (overwriting free space)...", "Apagamento seguro (sobrescrevendo espaço livre)...", "Effacement sécurisé (écrasement de l'espace libre)...", "Cancellazione sicura (sovrascrittura spazio libero)..."],
        ["status.wiping.progress"] = ["Borrado seguro: {0}", "Secure erase: {0}", "Apagamento seguro: {0}", "Effacement sécurisé : {0}", "Cancellazione sicura: {0}"],
        ["status.ejected"]   =["Unidad expulsada.", "Drive ejected.", "Unidade ejetada.", "Lecteur éjecté.", "Unità espulsa."],

        ["history.title"]        = ["Historial de operaciones", "Operation history", "Histórico de operações", "Historique des opérations", "Cronologia operazioni"],
        ["history.empty"]        = ["Sin operaciones registradas.", "No operations recorded.", "Nenhuma operação registrada.", "Aucune opération enregistrée.", "Nessuna operazione registrata."],
        ["history.noMatch"]      = ["Ninguna operación coincide con el filtro.", "No operations match the filter.", "Nenhuma operação corresponde ao filtro.", "Aucune opération ne correspond au filtre.", "Nessuna operazione corrisponde al filtro."],
        ["history.search"]       = ["Buscar…", "Search…", "Pesquisar…", "Rechercher…", "Cerca…"],
        ["history.filter.allCat"]= ["Todas las categorías", "All categories", "Todas as categorias", "Toutes les catégories", "Tutte le categorie"],
        ["history.filter.allRes"]= ["Todos los resultados", "All results", "Todos os resultados", "Tous les résultats", "Tutti i risultati"],
        ["history.export"]       = ["Exportar CSV", "Export CSV", "Exportar CSV", "Exporter CSV", "Esporta CSV"],
        ["history.open"]         = ["Abrir archivo", "Open file", "Abrir arquivo", "Ouvrir le fichier", "Apri file"],
        ["history.clear"]        = ["Vaciar historial", "Clear history", "Limpar histórico", "Effacer l'historique", "Cancella cronologia"],
        ["history.clearConfirm"] = ["¿Vaciar el historial?", "Clear the history?", "Limpar o histórico?", "Effacer l'historique ?", "Cancellare la cronologia?"],
        ["history.cat.format"]   = ["Formato", "Format", "Formatação", "Formatage", "Formattazione"],
        ["history.cat.wipe"]     = ["Borrado seguro", "Secure erase", "Apagamento seguro", "Effacement sécurisé", "Cancellazione sicura"],
        ["history.cat.verify"]   = ["Verificación", "Verification", "Verificação", "Vérification", "Verifica"],
        ["history.cat.eject"]    = ["Expulsión", "Eject", "Ejeção", "Éjection", "Espulsione"],
        ["history.cat.update"]   = ["Actualización", "Update", "Atualização", "Mise à jour", "Aggiornamento"],
        ["history.cat.other"]    = ["Operación", "Operation", "Operação", "Opération", "Operazione"],
        ["history.res.ok"]        = ["Correcto", "Success", "Sucesso", "Réussi", "Riuscito"],
        ["history.res.fail"]      = ["Fallo", "Failed", "Falha", "Échec", "Non riuscito"],
        ["history.res.error"]     = ["Error", "Error", "Erro", "Erreur", "Errore"],
        ["history.res.cancelled"] = ["Cancelado", "Cancelled", "Cancelado", "Annulé", "Annullato"],
        ["history.res.info"]      = ["Info", "Info", "Info", "Info", "Info"],

        ["health.title"]    = ["Salud del disco (S.M.A.R.T.)", "Disk health (S.M.A.R.T.)", "Saúde do disco (S.M.A.R.T.)", "Santé du disque (S.M.A.R.T.)", "Stato del disco (S.M.A.R.T.)"],
        ["health.querying"] = ["Consultando…", "Querying…", "Consultando…", "Interrogation…", "Interrogazione…"],
        ["health.refresh"]  = ["Actualizar", "Refresh", "Atualizar", "Actualiser", "Aggiorna"],
        ["health.level.ok"]       = ["Normal", "Normal", "Normal", "Normal", "Normale"],
        ["health.level.warning"]  = ["Atención", "Caution", "Atenção", "Attention", "Attenzione"],
        ["health.level.critical"] = ["Crítico", "Critical", "Crítico", "Critique", "Critico"],
        ["health.na"]       = ["No disponible", "Not available", "Indisponível", "Non disponible", "Non disponibile"],
        ["health.note"]     = ["Algunos valores no están disponibles en todas las unidades (p. ej. USB).", "Some values aren't available on all drives (e.g. USB).", "Alguns valores não estão disponíveis em todas as unidades (ex.: USB).", "Certaines valeurs ne sont pas disponibles sur tous les lecteurs (p. ex. USB).", "Alcuni valori non sono disponibili su tutte le unità (es. USB)."],
        ["health.drive"]    = ["Unidad", "Drive", "Unidade", "Lecteur", "Unità"],
        ["health.status"]   = ["Estado de salud", "Health status", "Estado de saúde", "État de santé", "Stato di salute"],
        ["health.bus"]      = ["Conexión", "Bus", "Conexão", "Connexion", "Connessione"],
        ["health.media"]    = ["Tipo de medio", "Media type", "Tipo de mídia", "Type de support", "Tipo di supporto"],
        ["health.spindle"]  = ["Velocidad", "Spindle speed", "Velocidade", "Vitesse de rotation", "Velocità"],
        ["health.temp"]     = ["Temperatura", "Temperature", "Temperatura", "Température", "Temperatura"],
        ["health.hours"]    = ["Horas de encendido", "Power-on hours", "Horas ligado", "Heures de fonctionnement", "Ore di accensione"],
        ["health.wear"]     = ["Desgaste (SSD)", "Wear (SSD)", "Desgaste (SSD)", "Usure (SSD)", "Usura (SSD)"],
        ["health.readErr"]  = ["Errores de lectura", "Read errors", "Erros de leitura", "Erreurs de lecture", "Errori di lettura"],
        ["health.writeErr"] = ["Errores de escritura", "Write errors", "Erros de escrita", "Erreurs d'écriture", "Errori di scrittura"],
        ["health.unit.temp"]    = ["{0} °C", "{0} °C", "{0} °C", "{0} °C", "{0} °C"],
        ["health.unit.hours"]   = ["{0} h", "{0} h", "{0} h", "{0} h", "{0} h"],
        ["health.unit.percent"] = ["{0} %", "{0} %", "{0} %", "{0} %", "{0} %"],
        ["health.unit.rpm"]     = ["{0} RPM", "{0} RPM", "{0} RPM", "{0} RPM", "{0} RPM"],

        ["unlock.confirmTitle"] = ["Protección de escritura", "Write protection", "Proteção contra gravação", "Protection en écriture", "Protezione da scrittura"],
        ["unlock.confirmBody"]  = ["La unidad {0}: está protegida contra escritura.\n\n¿Quitar la protección ahora?", "Drive {0}: is write-protected.\n\nRemove the protection now?", "A unidade {0}: está protegida contra gravação.\n\nRemover a proteção agora?", "Le lecteur {0}: est protégé en écriture.\n\nSupprimer la protection maintenant ?", "L'unità {0}: è protetta da scrittura.\n\nRimuovere la protezione ora?"],
        ["unlock.notProtected"] = ["La unidad {0}: no está protegida contra escritura.", "Drive {0}: is not write-protected.", "A unidade {0}: não está protegida contra gravação.", "Le lecteur {0}: n'est pas protégé en écriture.", "L'unità {0}: non è protetta da scrittura."],
        ["unlock.cleared"]      = ["Protección de escritura quitada de {0}:.", "Write protection removed from {0}:.", "Proteção contra gravação removida de {0}:.", "Protection en écriture supprimée de {0}:.", "Protezione da scrittura rimossa da {0}:."],
        ["unlock.failed"]       = ["No se pudo quitar la protección de escritura de {0}:.", "Could not remove write protection from {0}:.", "Não foi possível remover a proteção contra gravação de {0}:.", "Impossible de supprimer la protection en écriture de {0}:.", "Impossibile rimuovere la protezione da scrittura da {0}:."],
        ["unlock.blockedSystem"]= ["No se puede modificar la protección del disco de sistema.", "Cannot change protection of the system disk.", "Não é possível alterar a proteção do disco do sistema.", "Impossible de modifier la protection du disque système.", "Impossibile modificare la protezione del disco di sistema."],

        ["check.modeTitle"]      = ["Comprobar errores", "Check for errors", "Verificar erros", "Vérifier les erreurs", "Controlla errori"],
        ["check.modeBody"]       = ["¿Cómo quieres comprobar la unidad {0}:?", "How do you want to check drive {0}:?", "Como deseja verificar a unidade {0}:?", "Comment vérifier le lecteur {0}: ?", "Come vuoi controllare l'unità {0}:?"],
        ["check.scanOnly"]       = ["Solo comprobar", "Check only", "Apenas verificar", "Vérifier seulement", "Solo controlla"],
        ["check.repair"]         = ["Comprobar y reparar", "Check and repair", "Verificar e reparar", "Vérifier et réparer", "Controlla e ripara"],
        ["check.scanning"]       = ["Comprobando {0}:…", "Checking {0}:…", "Verificando {0}:…", "Vérification de {0}:…", "Controllo di {0}:…"],
        ["check.repairing"]      = ["Comprobando y reparando {0}:…", "Checking and repairing {0}:…", "Verificando e reparando {0}:…", "Vérification et réparation de {0}:…", "Controllo e riparazione di {0}:…"],
        ["check.resultClean"]    = ["La unidad {0}: no tiene errores.", "Drive {0}: has no errors.", "A unidade {0}: não tem erros.", "Le lecteur {0}: ne contient aucune erreur.", "L'unità {0}: non presenta errori."],
        ["check.resultRepaired"] = ["Se repararon errores en la unidad {0}:.", "Errors were repaired on drive {0}:.", "Erros foram reparados na unidade {0}:.", "Des erreurs ont été réparées sur le lecteur {0}:.", "Sono stati riparati errori sull'unità {0}:."],
        ["check.resultErrors"]   = ["Se encontraron errores en {0}:. Usa «Comprobar y reparar».", "Errors were found on {0}:. Use \"Check and repair\".", "Foram encontrados erros em {0}:. Use «Verificar e reparar».", "Des erreurs ont été trouvées sur {0}:. Utilisez « Vérifier et réparer ».", "Sono stati trovati errori su {0}:. Usa «Controlla e ripara»."],
        ["check.resultFailed"]   = ["No se pudo comprobar la unidad {0}: (¿está en uso?).", "Could not check drive {0}: (is it in use?).", "Não foi possível verificar a unidade {0}: (está em uso?).", "Impossible de vérifier le lecteur {0}: (est-il en cours d'utilisation ?).", "Impossibile controllare l'unità {0}: (è in uso?)."],

        ["reinit.title"]         = ["Reinicializar unidad", "Reinitialize drive", "Reinicializar unidade", "Réinitialiser le lecteur", "Reinizializza unità"],
        ["reinit.onlyRemovable"] = ["Solo se pueden reinicializar unidades extraíbles (USB).", "Only removable drives (USB) can be reinitialized.", "Apenas unidades removíveis (USB) podem ser reinicializadas.", "Seuls les lecteurs amovibles (USB) peuvent être réinitialisés.", "Solo le unità rimovibili (USB) possono essere reinizializzate."],
        ["reinit.blockedSystem"] = ["No se puede reinicializar el disco del sistema.", "The system disk cannot be reinitialized.", "O disco do sistema não pode ser reinicializado.", "Le disque système ne peut pas être réinitialisé.", "Il disco di sistema non può essere reinizializzato."],
        ["reinit.sameDisk"]      = ["La unidad comparte disco físico con Windows: no se puede reinicializar.", "The drive shares its physical disk with Windows: it cannot be reinitialized.", "A unidade compartilha o disco físico com o Windows: não pode ser reinicializada.", "Le lecteur partage son disque physique avec Windows : il ne peut pas être réinitialisé.", "L'unità condivide il disco fisico con Windows: non può essere reinizializzata."],
        ["reinit.summary"]       = ["Se borrará TODO el disco físico de la unidad {0}: (todas sus particiones)\ny se recreará una única partición {1} formateada en {2}.\n\nEsta acción NO se puede deshacer.", "The ENTIRE physical disk of drive {0}: will be erased (all its partitions)\nand a single {1} partition formatted as {2} will be recreated.\n\nThis action CANNOT be undone.", "TODO o disco físico da unidade {0}: será apagado (todas as suas partições)\ne será recriada uma única partição {1} formatada em {2}.\n\nEsta ação NÃO pode ser desfeita.", "TOUT le disque physique du lecteur {0}: sera effacé (toutes ses partitions)\net une seule partition {1} formatée en {2} sera recréée.\n\nCette action est IRRÉVERSIBLE.", "L'INTERO disco fisico dell'unità {0}: verrà cancellato (tutte le sue partizioni)\ne verrà ricreata un'unica partizione {1} formattata in {2}.\n\nQuesta azione NON può essere annullata."],
        ["reinit.stage.clean"]     = ["Reinicializando {0}: — limpiando disco…", "Reinitializing {0}: — cleaning disk…", "Reinicializando {0}: — limpando disco…", "Réinitialisation de {0}: — nettoyage du disque…", "Reinizializzazione di {0}: — pulizia disco…"],
        ["reinit.stage.init"]      = ["Reinicializando {0}: — inicializando disco…", "Reinitializing {0}: — initializing disk…", "Reinicializando {0}: — inicializando disco…", "Réinitialisation de {0}: — initialisation du disque…", "Reinizializzazione di {0}: — inizializzazione disco…"],
        ["reinit.stage.partition"] = ["Reinicializando {0}: — creando partición…", "Reinitializing {0}: — creating partition…", "Reinicializando {0}: — criando partição…", "Réinitialisation de {0}: — création de la partition…", "Reinizializzazione di {0}: — creazione partizione…"],
        ["reinit.stage.format"]    = ["Reinicializando {0}: — formateando…", "Reinitializing {0}: — formatting…", "Reinicializando {0}: — formatando…", "Réinitialisation de {0}: — formatage…", "Reinizializzazione di {0}: — formattazione…"],
        ["reinit.doneTitle"]     = ["Unidad reinicializada", "Drive reinitialized", "Unidade reinicializada", "Lecteur réinitialisé", "Unità reinizializzata"],
        ["reinit.doneBody"]      = ["La unidad se reinicializó correctamente y ahora está disponible como {0}:.", "The drive was reinitialized successfully and is now available as {0}:.", "A unidade foi reinicializada com sucesso e agora está disponível como {0}:.", "Le lecteur a été réinitialisé avec succès et est maintenant disponible en tant que {0}:.", "L'unità è stata reinizializzata correttamente ed è ora disponibile come {0}:."],
        ["reinit.failed"]        = ["No se pudo reinicializar la unidad.", "Could not reinitialize the drive.", "Não foi possível reinicializar a unidade.", "Impossible de réinitialiser le lecteur.", "Impossibile reinizializzare l'unità."],
        ["reinit.summaryFat32Small"] = ["Se borrará TODO el disco físico de la unidad {0}: (todas sus particiones)\ny se creará una única partición FAT32 de {1}; el resto del disco quedará SIN ASIGNAR.\n\nEsta acción NO se puede deshacer.", "The ENTIRE physical disk of drive {0}: will be erased (all its partitions)\nand a single {1} FAT32 partition will be created; the rest of the disk will be left UNALLOCATED.\n\nThis action CANNOT be undone.", "TODO o disco físico da unidade {0}: será apagado (todas as suas partições)\ne será criada uma única partição FAT32 de {1}; o restante do disco ficará NÃO ALOCADO.\n\nEsta ação NÃO pode ser desfeita.", "TOUT le disque physique du lecteur {0}: sera effacé (toutes ses partitions)\net une seule partition FAT32 de {1} sera créée ; le reste du disque restera NON ALLOUÉ.\n\nCette action est IRRÉVERSIBLE.", "L'INTERO disco fisico dell'unità {0}: verrà cancellato (tutte le sue partizioni)\ne verrà creata un'unica partizione FAT32 da {1}; il resto del disco rimarrà NON ALLOCATO.\n\nQuesta azione NON può essere annullata."],
        ["reinit.doneBodyFat32Small"] = ["La unidad se reinicializó correctamente: ahora tiene una partición FAT32 de {1} disponible como {0}:. El resto del disco quedó sin asignar (puedes usarlo más adelante desde Administración de discos de Windows).", "The drive was reinitialized successfully: it now has a {1} FAT32 partition available as {0}:. The rest of the disk was left unallocated (you can use it later from Windows Disk Management).", "A unidade foi reinicializada com sucesso: agora tem uma partição FAT32 de {1} disponível como {0}:. O restante do disco ficou não alocado (você pode usá-lo depois pelo Gerenciamento de Disco do Windows).", "Le lecteur a été réinitialisé avec succès : il dispose maintenant d'une partition FAT32 de {1} disponible en tant que {0}:. Le reste du disque est resté non alloué (vous pouvez l'utiliser plus tard depuis la Gestion des disques de Windows).", "L'unità è stata reinizializzata correttamente: ora ha una partizione FAT32 da {1} disponibile come {0}:. Il resto del disco è rimasto non allocato (puoi usarlo in seguito da Gestione disco di Windows)."],

        ["bench.confirmTitle"]   = ["Benchmark rápido", "Quick benchmark", "Benchmark rápido", "Benchmark rapide", "Benchmark rapido"],
        ["bench.confirmBody"]    = ["Se medirá la velocidad de {0}: con un archivo temporal de unos 512 MB: secuencial (cola Q8) y 4 KiB aleatorio, lectura y escritura, sin caché. Tarda unos segundos.\n\nLa operación no es destructiva. ¿Continuar?", "Speed of {0}: will be measured with a temporary file of about 512 MB: sequential (queue Q8) and random 4 KiB, read and write, cache-bypassed. It takes a few seconds.\n\nThe operation is non-destructive. Continue?", "A velocidade de {0}: será medida com um arquivo temporário de cerca de 512 MB: sequencial (fila Q8) e 4 KiB aleatório, leitura e escrita, sem cache. Leva alguns segundos.\n\nA operação não é destrutiva. Continuar?", "La vitesse de {0}: sera mesurée avec un fichier temporaire d'environ 512 Mo : séquentiel (file Q8) et 4 Kio aléatoire, lecture et écriture, sans cache. Cela prend quelques secondes.\n\nL'opération n'est pas destructive. Continuer ?", "La velocità di {0}: verrà misurata con un file temporaneo di circa 512 MB: sequenziale (coda Q8) e 4 KiB casuale, lettura e scrittura, senza cache. Richiede alcuni secondi.\n\nL'operazione non è distruttiva. Continuare?"],
        ["bench.preparing"]      = ["Benchmark de {0}: — preparando…", "Benchmark of {0}: — preparing…", "Benchmark de {0}: — preparando…", "Benchmark de {0}: — préparation…", "Benchmark di {0}: — preparazione…"],
        ["bench.seqWrite"]       = ["Benchmark de {0}: — secuencial (escritura)…", "Benchmark of {0}: — sequential (write)…", "Benchmark de {0}: — sequencial (escrita)…", "Benchmark de {0}: — séquentiel (écriture)…", "Benchmark di {0}: — sequenziale (scrittura)…"],
        ["bench.seqRead"]        = ["Benchmark de {0}: — secuencial (lectura)…", "Benchmark of {0}: — sequential (read)…", "Benchmark de {0}: — sequencial (leitura)…", "Benchmark de {0}: — séquentiel (lecture)…", "Benchmark di {0}: — sequenziale (lettura)…"],
        ["bench.rndWrite"]       = ["Benchmark de {0}: — 4K aleatorio (escritura)…", "Benchmark of {0}: — random 4K (write)…", "Benchmark de {0}: — 4K aleatório (escrita)…", "Benchmark de {0}: — 4K aléatoire (écriture)…", "Benchmark di {0}: — 4K casuale (scrittura)…"],
        ["bench.rndRead"]        = ["Benchmark de {0}: — 4K aleatorio (lectura)…", "Benchmark of {0}: — random 4K (read)…", "Benchmark de {0}: — 4K aleatório (leitura)…", "Benchmark de {0}: — 4K aléatoire (lecture)…", "Benchmark di {0}: — 4K casuale (lettura)…"],
        ["bench.resultTitle"]    = ["Resultado del benchmark", "Benchmark result", "Resultado do benchmark", "Résultat du benchmark", "Risultato del benchmark"],
        ["bench.resultBody"]     = ["Unidad {0}:\n\n  Secuencial (Q8, 1 MiB)\n    Escritura:  {1}\n    Lectura:    {2}\n\n  4K aleatorio (Q1)\n    Escritura:  {3}  ({5})\n    Lectura:    {4}  ({6})", "Drive {0}:\n\n  Sequential (Q8, 1 MiB)\n    Write:  {1}\n    Read:   {2}\n\n  Random 4K (Q1)\n    Write:  {3}  ({5})\n    Read:   {4}  ({6})", "Unidade {0}:\n\n  Sequencial (Q8, 1 MiB)\n    Escrita:  {1}\n    Leitura:  {2}\n\n  4K aleatório (Q1)\n    Escrita:  {3}  ({5})\n    Leitura:  {4}  ({6})", "Lecteur {0}:\n\n  Séquentiel (Q8, 1 Mio)\n    Écriture :  {1}\n    Lecture :   {2}\n\n  4 Kio aléatoire (Q1)\n    Écriture :  {3}  ({5})\n    Lecture :   {4}  ({6})", "Unità {0}:\n\n  Sequenziale (Q8, 1 MiB)\n    Scrittura:  {1}\n    Lettura:    {2}\n\n  4K casuale (Q1)\n    Scrittura:  {3}  ({5})\n    Lettura:    {4}  ({6})"],
        ["bench.noSpace"]        = ["No hay espacio libre suficiente en {0}: para el benchmark (se necesitan ~576 MB).", "Not enough free space on {0}: for the benchmark (~576 MB needed).", "Não há espaço livre suficiente em {0}: para o benchmark (são necessários ~576 MB).", "Espace libre insuffisant sur {0}: pour le benchmark (~576 Mo nécessaires).", "Spazio libero insufficiente su {0}: per il benchmark (servono ~576 MB)."],
        ["bench.note"]           = ["Sin caché del sistema; secuencial con cola Q8 y 4K aleatorio Q1, mediana de 3 pasadas.", "System cache bypassed; sequential at queue depth Q8 and random 4K at Q1, median of 3 passes.", "Sem cache do sistema; sequencial com fila Q8 e 4K aleatório Q1, mediana de 3 passagens.", "Sans cache système ; séquentiel en file Q8 et 4K aléatoire Q1, médiane de 3 passes.", "Senza cache di sistema; sequenziale a coda Q8 e 4K casuale Q1, mediana di 3 passaggi."],
        ["bench.failed"]         = ["No se pudo completar el benchmark de {0}:.", "Could not complete the benchmark of {0}:.", "Não foi possível concluir o benchmark de {0}:.", "Impossible de terminer le benchmark de {0}:.", "Impossibile completare il benchmark di {0}:."],

        ["msg.warning"]      = ["Advertencia", "Warning", "Aviso", "Avertissement", "Avviso"],
        ["msg.error"]        = ["Error", "Error", "Erro", "Erreur", "Errore"],
        ["msg.selectDrive"]  =["Seleccione una unidad.", "Select a drive.", "Selecione uma unidade.", "Sélectionnez un lecteur.", "Seleziona un'unità."],
        ["msg.selectFsAlloc"]= ["Seleccione el sistema de archivos y el tamaño de unidad.", "Select the file system and allocation unit size.", "Selecione o sistema de arquivos e o tamanho da unidade de alocação.", "Sélectionnez le système de fichiers et la taille d'unité d'allocation.", "Seleziona il file system e la dimensione dell'unità di allocazione."],
        ["msg.systemTitle"]  = ["Operación no permitida", "Operation not allowed", "Operação não permitida", "Opération non autorisée", "Operazione non consentita"],
        ["msg.systemBody"]   = ["No se puede formatear la unidad que contiene Windows.", "Cannot format the drive that contains Windows.", "Não é possível formatar a unidade que contém o Windows.", "Impossible de formater le lecteur qui contient Windows.", "Impossibile formattare l'unità che contiene Windows."],
        ["msg.protTitle"]    = ["Disco protegido", "Protected disk", "Disco protegido", "Disque protégé", "Disco protetto"],
        ["msg.protBody"]     = ["Este es un disco fijo protegido. La operación no está permitida.", "This is a protected fixed disk. The operation is not allowed.", "Este é um disco fixo protegido. A operação não é permitida.", "Il s'agit d'un disque fixe protégé. L'opération n'est pas autorisée.", "Questo è un disco fisso protetto. L'operazione non è consentita."],
        ["msg.invalidLabel"] = ["La etiqueta contiene caracteres no válidos:\n\\ / : * ? \" < > |", "The label contains invalid characters:\n\\ / : * ? \" < > |", "O rótulo contém caracteres inválidos:\n\\ / : * ? \" < > |", "Le nom de volume contient des caractères non valides :\n\\ / : * ? \" < > |", "L'etichetta contiene caratteri non validi:\n\\ / : * ? \" < > |"],
        ["msg.invalidTitle"] = ["Etiqueta inválida", "Invalid label", "Rótulo inválido", "Nom de volume non valide", "Etichetta non valida"],
        ["msg.labelLongTitle"]= ["Etiqueta demasiado larga", "Label too long", "Rótulo muito longo", "Nom de volume trop long", "Etichetta troppo lunga"],
        ["msg.labelLong"]    = ["La etiqueta supera el máximo de {0} caracteres para {1}.", "The label exceeds the maximum of {0} characters for {1}.", "O rótulo excede o máximo de {0} caracteres para {1}.", "Le nom de volume dépasse le maximum de {0} caractères pour {1}.", "L'etichetta supera il massimo di {0} caratteri per {1}."],
        ["msg.goneTitle"]    = ["Unidad no disponible", "Drive unavailable", "Unidade indisponível", "Lecteur indisponible", "Unità non disponibile"],
        ["msg.goneBody"]     = ["La unidad {0}: ya no está disponible. Actualice la lista.", "Drive {0}: is no longer available. Refresh the list.", "A unidade {0}: não está mais disponível. Atualize a lista.", "Le lecteur {0}: n'est plus disponible. Actualisez la liste.", "L'unità {0}: non è più disponibile. Aggiorna l'elenco."],

        ["confirm.title"]    = ["Confirmar formato", "Confirm format", "Confirmar formatação", "Confirmer le formatage", "Conferma formattazione"],
        ["confirm.warning"]  = ["ADVERTENCIA: Se destruirán TODOS los datos en:", "WARNING: ALL data will be destroyed on:", "AVISO: TODOS os dados serão destruídos em:", "AVERTISSEMENT : TOUTES les données seront détruites sur :", "AVVISO: TUTTI i dati verranno distrutti su:"],
        ["confirm.drive"]    = ["Unidad", "Drive", "Unidade", "Lecteur", "Unità"],
        ["confirm.fs"]       = ["Sistema", "File system", "Sistema", "Système", "Sistema"],
        ["confirm.cluster"]  = ["Cluster", "Cluster", "Cluster", "Cluster", "Cluster"],
        ["confirm.label"]    = ["Etiqueta", "Label", "Rótulo", "Nom", "Etichetta"],
        ["confirm.nolabel"]  = ["(sin etiqueta)", "(no label)", "(sem rótulo)", "(sans nom)", "(senza etichetta)"],
        ["confirm.mode"]     = ["Tipo", "Mode", "Tipo", "Mode", "Tipo"],
        ["confirm.secure"]   = ["Borrado seguro", "Secure erase", "Apagamento seguro", "Effacement sécurisé", "Cancellazione sicura"],
        ["confirm.smallFat32Ignored"] = ["Nota: la opción de partición FAT32 pequeña NO aplica aquí (se formatea toda la unidad). Para crearla, usa Herramientas → Reinicializar unidad…", "Note: the small FAT32 partition option does NOT apply here (the whole drive is formatted). To create it, use Tools → Reinitialize drive…", "Nota: a opção de partição FAT32 pequena NÃO se aplica aqui (a unidade inteira é formatada). Para criá-la, use Ferramentas → Reinicializar unidade…", "Remarque : l'option de petite partition FAT32 ne s'applique PAS ici (tout le lecteur est formaté). Pour la créer, utilisez Outils → Réinitialiser le lecteur…", "Nota: l'opzione della piccola partizione FAT32 NON si applica qui (viene formattata l'intera unità). Per crearla, usa Strumenti → Reinizializza unità…"],
        ["confirm.yes"]      = ["Sí", "Yes", "Sim", "Oui", "Sì"],
        ["confirm.no"]       = ["No", "No", "Não", "Non", "No"],
        ["confirm.prompt"]   = ["Para confirmar, escriba la letra de la unidad ({0}):", "To confirm, type the drive letter ({0}):", "Para confirmar, digite a letra da unidade ({0}):", "Pour confirmer, saisissez la lettre du lecteur ({0}) :", "Per confermare, digita la lettera dell'unità ({0}):"],

        ["success.title"]    = ["Éxito", "Success", "Sucesso", "Succès", "Operazione riuscita"],
        ["success.body"]     = ["La unidad {0}: se formateó correctamente con {1}.", "Drive {0}: was formatted successfully with {1}.", "A unidade {0}: foi formatada com sucesso com {1}.", "Le lecteur {0}: a été formaté avec succès en {1}.", "L'unità {0}: è stata formattata correttamente con {1}."],
        ["error.formatTitle"]= ["Error de formato", "Format error", "Erro de formatação", "Erreur de formatage", "Errore di formattazione"],
        ["error.formatBody"] = ["Error al formatear la unidad {0}:\n\n{1}", "Error formatting drive {0}:\n\n{1}", "Erro ao formatar a unidade {0}:\n\n{1}", "Erreur lors du formatage du lecteur {0}:\n\n{1}", "Errore durante la formattazione dell'unità {0}:\n\n{1}"],
        ["cancel.title"]     = ["Cancelar operación", "Cancel operation", "Cancelar operação", "Annuler l'opération", "Annulla operazione"],
        ["cancel.body"]      = ["¿Cancelar la operación en curso?\n\nNota: la unidad puede quedar en un estado no utilizable.", "Cancel the operation in progress?\n\nNote: the drive may be left in an unusable state.", "Cancelar a operação em andamento?\n\nNota: a unidade pode ficar em um estado inutilizável.", "Annuler l'opération en cours ?\n\nRemarque : le lecteur peut rester dans un état inutilisable.", "Annullare l'operazione in corso?\n\nNota: l'unità potrebbe rimanere in uno stato inutilizzabile."],
        ["closing.title"]    = ["Operación en progreso", "Operation in progress", "Operação em andamento", "Opération en cours", "Operazione in corso"],
        ["closing.body"]     = ["Utilice el botón Cancelar para detener la operación.", "Use the Cancel button to stop the operation.", "Use o botão Cancelar para parar a operação.", "Utilisez le bouton Annuler pour arrêter l'opération.", "Usa il pulsante Annulla per interrompere l'operazione."],

        ["eject.fail"]       = ["No se pudo expulsar la unidad. Asegúrese de que no esté en uso.", "Could not eject the drive. Make sure it is not in use.", "Não foi possível ejetar a unidade. Verifique se não está em uso.", "Impossible d'éjecter le lecteur. Assurez-vous qu'il n'est pas en cours d'utilisation.", "Impossibile espellere l'unità. Assicurati che non sia in uso."],
        ["eject.fixed"]      = ["Solo se pueden expulsar unidades removibles.", "Only removable drives can be ejected.", "Apenas unidades removíveis podem ser ejetadas.", "Seuls les lecteurs amovibles peuvent être éjectés.", "Solo le unità rimovibili possono essere espulse."],

        ["verify.title"]     = ["Verificar capacidad real", "Verify real capacity", "Verificar capacidade real", "Vérifier la capacité réelle", "Verifica capacità reale"],
        ["verify.warn"]      = ["Esta prueba escribirá datos en el espacio libre de {0}: para detectar capacidad falsa.\n\nPuede tardar varios minutos. ¿Continuar?", "This test will write data to the free space of {0}: to detect fake capacity.\n\nIt may take several minutes. Continue?", "Este teste gravará dados no espaço livre de {0}: para detectar capacidade falsa.\n\nPode levar vários minutos. Continuar?", "Ce test écrira des données dans l'espace libre de {0}: pour détecter une fausse capacité.\n\nCela peut prendre plusieurs minutes. Continuer ?", "Questo test scriverà dati nello spazio libero di {0}: per rilevare capacità falsa.\n\nPuò richiedere alcuni minuti. Continuare?"],
        ["verify.writing"]   = ["Verificando (escribiendo): {0}", "Verifying (writing): {0}", "Verificando (gravando): {0}", "Vérification (écriture) : {0}", "Verifica (scrittura): {0}"],
        ["verify.reading"]   = ["Verificando (leyendo): {0}", "Verifying (reading): {0}", "Verificando (lendo): {0}", "Vérification (lecture) : {0}", "Verifica (lettura): {0}"],
        ["verify.okTitle"]   = ["Capacidad verificada", "Capacity verified", "Capacidade verificada", "Capacité vérifiée", "Capacità verificata"],
        ["verify.okBody"]    = ["La unidad {0}: es auténtica.\nDatos verificados: {1}.", "Drive {0}: is genuine.\nData verified: {1}.", "A unidade {0}: é autêntica.\nDados verificados: {1}.", "Le lecteur {0}: est authentique.\nDonnées vérifiées : {1}.", "L'unità {0}: è autentica.\nDati verificati: {1}."],
        ["verify.failTitle"] = ["¡Capacidad falsa detectada!", "Fake capacity detected!", "Capacidade falsa detectada!", "Fausse capacité détectée !", "Capacità falsa rilevata!"],
        ["verify.failBody"]  = ["La unidad {0}: falló la verificación: los datos no coinciden tras {1}.\n\nProbablemente sea una unidad falsificada.", "Drive {0}: failed verification: data mismatch after {1}.\n\nIt is likely a counterfeit drive.", "A unidade {0}: falhou na verificação: os dados não coincidem após {1}.\n\nProvavelmente é uma unidade falsificada.", "Le lecteur {0}: a échoué à la vérification : données incohérentes après {1}.\n\nIl s'agit probablement d'un lecteur contrefait.", "L'unità {0}: ha fallito la verifica: dati non corrispondenti dopo {1}.\n\nProbabilmente è un'unità contraffatta."],

        ["about.title"]      = ["Acerca de FormatDiskPro", "About FormatDiskPro", "Sobre o FormatDiskPro", "À propos de FormatDiskPro", "Informazioni su FormatDiskPro"],
        ["about.body"]       = ["FormatDiskPro v{0}\n\nHerramienta de formateo y gestión de unidades para Windows.\nNTFS · exFAT · ReFS · FAT32 · FAT\n\n.NET 10 · WinUI 3", "FormatDiskPro v{0}\n\nDisk format and management tool for Windows.\nNTFS · exFAT · ReFS · FAT32 · FAT\n\n.NET 10 · WinUI 3", "FormatDiskPro v{0}\n\nFerramenta de formatação e gerenciamento de unidades para Windows.\nNTFS · exFAT · ReFS · FAT32 · FAT\n\n.NET 10 · WinUI 3", "FormatDiskPro v{0}\n\nOutil de formatage et de gestion de lecteurs pour Windows.\nNTFS · exFAT · ReFS · FAT32 · FAT\n\n.NET 10 · WinUI 3", "FormatDiskPro v{0}\n\nStrumento di formattazione e gestione unità per Windows.\nNTFS · exFAT · ReFS · FAT32 · FAT\n\n.NET 10 · WinUI 3"],
        ["about.version"]    = ["Versión {0}", "Version {0}", "Versão {0}", "Version {0}", "Versione {0}"],
        ["about.desc"]       = ["Herramienta de formateo y gestión de unidades para Windows (NTFS · exFAT · ReFS · FAT32 · FAT). .NET 10 · WinUI 3.", "Disk format and management tool for Windows (NTFS · exFAT · ReFS · FAT32 · FAT). .NET 10 · WinUI 3.", "Ferramenta de formatação e gestão de unidades para Windows (NTFS · exFAT · ReFS · FAT32 · FAT). .NET 10 · WinUI 3.", "Outil de formatage et de gestion de lecteurs pour Windows (NTFS · exFAT · ReFS · FAT32 · FAT). .NET 10 · WinUI 3.", "Strumento di formattazione e gestione unità per Windows (NTFS · exFAT · ReFS · FAT32 · FAT). .NET 10 · WinUI 3."],
        ["about.copyright"]  = ["© 2026 Ricky Angel Jiménez Bueno · Software libre bajo licencia GNU GPL v3.0.", "© 2026 Ricky Angel Jiménez Bueno · Free software under the GNU GPL v3.0 license.", "© 2026 Ricky Angel Jiménez Bueno · Software livre sob a licença GNU GPL v3.0.", "© 2026 Ricky Angel Jiménez Bueno · Logiciel libre sous licence GNU GPL v3.0.", "© 2026 Ricky Angel Jiménez Bueno · Software libero con licenza GNU GPL v3.0."],
        ["about.disclaimerHeader"]= ["Aviso", "Disclaimer", "Aviso", "Avertissement", "Avviso"],
        ["about.disclaimer"] = ["Este programa formatea y borra unidades de forma irreversible. Se proporciona SIN NINGUNA GARANTÍA; úsalo bajo tu propia responsabilidad. Comprueba siempre la unidad seleccionada antes de iniciar.", "This program formats and erases drives irreversibly. It is provided WITHOUT ANY WARRANTY; use it at your own risk. Always double-check the selected drive before starting.", "Este programa formata e apaga unidades de forma irreversível. É fornecido SEM QUALQUER GARANTIA; use por sua conta e risco. Verifique sempre a unidade selecionada antes de iniciar.", "Ce programme formate et efface des lecteurs de façon irréversible. Il est fourni SANS AUCUNE GARANTIE ; utilisez-le à vos propres risques. Vérifiez toujours le lecteur sélectionné avant de démarrer.", "Questo programma formatta ed elimina unità in modo irreversibile. È fornito SENZA ALCUNA GARANZIA; usalo a tuo rischio. Verifica sempre l'unità selezionata prima di iniziare."],
        ["about.privacyHeader"]= ["Privacidad", "Privacy", "Privacidade", "Confidentialité", "Privacy"],
        ["about.privacy"]    = ["No recopila datos personales ni telemetría. La única conexión a Internet es para comprobar y descargar actualizaciones desde GitHub Releases (HTTPS).", "It collects no personal data or telemetry. The only Internet connection is to check for and download updates from GitHub Releases (HTTPS).", "Não coleta dados pessoais nem telemetria. A única conexão à Internet é para verificar e baixar atualizações do GitHub Releases (HTTPS).", "Aucune donnée personnelle ni télémétrie n'est collectée. La seule connexion Internet sert à vérifier et télécharger les mises à jour depuis GitHub Releases (HTTPS).", "Non raccoglie dati personali né telemetria. L'unica connessione a Internet serve a verificare e scaricare aggiornamenti da GitHub Releases (HTTPS)."],
        ["about.github"]     = ["Ver en GitHub", "View on GitHub", "Ver no GitHub", "Voir sur GitHub", "Visualizza su GitHub"],
        ["about.donate"]     = ["Apoyar el proyecto", "Support the project", "Apoiar o projeto", "Soutenir le projet", "Sostieni il progetto"],
        ["menu.license"]     = ["Licencia", "License", "Licença", "Licence", "Licenza"],
        ["menu.thirdParty"]  = ["Avisos de terceros", "Third-party notices", "Avisos de terceiros", "Avis de tiers", "Note di terze parti"],
        ["legal.unavailable"]= ["Texto no disponible.", "Text not available.", "Texto indisponível.", "Texte indisponible.", "Testo non disponibile."],
        ["preset.body"]      = ["Configuración «{0}» aplicada.", "Preset \"{0}\" applied.", "Predefinição «{0}» aplicada.", "Préréglage « {0} » appliqué.", "Preset «{0}» applicato."],
        ["preset.na"]        = ["El preset «{0}» no es compatible con esta unidad.", "Preset \"{0}\" is not compatible with this drive.", "A predefinição «{0}» não é compatível com esta unidade.", "Le préréglage « {0} » n'est pas compatible avec ce lecteur.", "Il preset «{0}» non è compatibile con questa unità."],
        ["preset.manage"]    = ["Gestionar presets", "Manage presets", "Gerenciar predefinições", "Gérer les préréglages", "Gestisci preset"],
        ["preset.saveHeader"]= ["Guardar configuración actual", "Save current settings", "Salvar configuração atual", "Enregistrer les paramètres actuels", "Salva impostazioni correnti"],
        ["preset.currentIs"] = ["Actual: {0}", "Current: {0}", "Atual: {0}", "Actuel : {0}", "Attuale: {0}"],
        ["preset.nameLabel"] = ["Nombre del preset", "Preset name", "Nome da predefinição", "Nom du préréglage", "Nome del preset"],
        ["preset.namePlaceholder"]= ["Mi preset", "My preset", "Minha predefinição", "Mon préréglage", "Il mio preset"],
        ["preset.saveBtn"]   = ["Guardar", "Save", "Salvar", "Enregistrer", "Salva"],
        ["preset.yourPresets"]= ["Tus presets", "Your presets", "Suas predefinições", "Vos préréglages", "I tuoi preset"],
        ["preset.empty"]     = ["Aún no has guardado presets.", "You haven't saved any presets yet.", "Você ainda não salvou predefinições.", "Vous n'avez pas encore enregistré de préréglages.", "Non hai ancora salvato preset."],
        ["preset.dupName"]   =["Ya existe un preset con ese nombre.", "A preset with that name already exists.", "Já existe uma predefinição com esse nome.", "Un préréglage portant ce nom existe déjà.", "Esiste già un preset con quel nome."],
        ["preset.editHeader"]= ["Editar preset", "Edit preset", "Editar predefinição", "Modifier le préréglage", "Modifica preset"],
        ["preset.updateBtn"] = ["Actualizar", "Update", "Atualizar", "Mettre à jour", "Aggiorna"],
        ["preset.cancelEdit"]= ["Cancelar", "Cancel", "Cancelar", "Annuler", "Annulla"],
        ["preset.updateConfig"]= ["Actualizar a la configuración actual ({0})", "Update to current settings ({0})", "Atualizar para a configuração atual ({0})", "Mettre à jour avec les paramètres actuels ({0})", "Aggiorna alle impostazioni correnti ({0})"],
        ["preset.moveUp"]    = ["Subir", "Move up", "Subir", "Monter", "Sposta su"],
        ["preset.moveDown"]  = ["Bajar", "Move down", "Descer", "Descendre", "Sposta giù"],
        ["preset.edit"]      = ["Editar", "Edit", "Editar", "Modifier", "Modifica"],
        ["preset.delete"]    = ["Eliminar", "Delete", "Excluir", "Supprimer", "Elimina"],

        ["update.checking"]  = ["Buscando actualizaciones…", "Checking for updates…", "Procurando atualizações…", "Recherche de mises à jour…", "Ricerca aggiornamenti…"],
        ["update.uptodate"]  = ["Ya tienes la última versión ({0}).", "You already have the latest version ({0}).", "Você já tem a versão mais recente ({0}).", "Vous avez déjà la dernière version ({0}).", "Hai già l'ultima versione ({0})."],
        ["update.availTitle"]= ["Actualización disponible", "Update available", "Atualização disponível", "Mise à jour disponible", "Aggiornamento disponibile"],
        ["update.availBody"] = ["Nueva versión disponible: {0}\nVersión actual: {1}", "New version available: {0}\nCurrent version: {1}", "Nova versão disponível: {0}\nVersão atual: {1}", "Nouvelle version disponible : {0}\nVersion actuelle : {1}", "Nuova versione disponibile: {0}\nVersione attuale: {1}"],
        ["update.changelog"] = ["Novedades:", "What's new:", "Novidades:", "Nouveautés :", "Novità:"],
        ["update.download"]  = ["Descargar e instalar", "Download and install", "Baixar e instalar", "Télécharger et installer", "Scarica e installa"],
        ["update.later"]     = ["Más tarde", "Later", "Mais tarde", "Plus tard", "Più tardi"],
        ["update.downloading"]=["Descargando actualización… {0}%", "Downloading update… {0}%", "Baixando atualização… {0}%", "Téléchargement de la mise à jour… {0}%", "Download aggiornamento… {0}%"],
        ["update.launching"] = ["Iniciando el instalador…", "Launching installer…", "Iniciando o instalador…", "Lancement de l'installateur…", "Avvio del programma di installazione…"],
        ["update.noasset"]   = ["La versión {0} no incluye un instalador descargable. Se abrirá la página de la versión.", "Release {0} has no downloadable installer. Opening the release page.", "A versão {0} não inclui um instalador para download. A página da versão será aberta.", "La version {0} ne comprend pas d'installateur téléchargeable. La page de la version va s'ouvrir.", "La versione {0} non include un programma di installazione scaricabile. Verrà aperta la pagina della versione."],
        ["update.error"]     = ["No se pudo completar la operación de actualización:\n{0}", "The update operation could not be completed:\n{0}", "Não foi possível concluir a operação de atualização:\n{0}", "Impossible de terminer l'opération de mise à jour :\n{0}", "Impossibile completare l'operazione di aggiornamento:\n{0}"],

        ["whatsnew.title"]      = ["Novedades de FormatDiskPro", "What's new in FormatDiskPro", "Novidades do FormatDiskPro", "Nouveautés de FormatDiskPro", "Novità di FormatDiskPro"],
        ["whatsnew.version"]    = ["Versión {0}", "Version {0}", "Versão {0}", "Version {0}", "Versione {0}"],
        ["whatsnew.viewOnGitHub"]= ["Ver en GitHub", "View on GitHub", "Ver no GitHub", "Voir sur GitHub", "Vedi su GitHub"],
        ["whatsnew.empty"]      = ["No se pudieron cargar las novedades. Puedes verlas en GitHub.", "Could not load the release notes. You can view them on GitHub.", "Não foi possível carregar as novidades. Você pode vê-las no GitHub.", "Impossible de charger les nouveautés. Vous pouvez les consulter sur GitHub.", "Impossibile caricare le novità. Puoi vederle su GitHub."],
    };
}
