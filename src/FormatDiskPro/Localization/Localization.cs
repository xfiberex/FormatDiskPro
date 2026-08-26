using System.Globalization;

namespace FormatDiskPro;

public enum AppLang { Es, En, Pt, Fr, It }

/// <summary>
/// Proveedor de cadenas localizadas (ES/EN/PT/FR/IT). Uso: L.T("clave") o L.T("clave", arg0, ...).
/// Cada entrada del diccionario es un arreglo indexado por <see cref="AppLang"/> (orden Es, En, Pt, Fr, It).
/// </summary>
public static class L
{
    public static AppLang Current { get; private set; } = AppLang.Es;

    /// <summary>
    /// Cultura con la que se formatean los números <b>que se muestran</b> (separadores de millares y
    /// decimal). Sigue al idioma elegido en la app, no al de Windows: son cosas distintas porque la app
    /// deja cambiar el idioma sin tocar el sistema, y hasta `T6-12` salían mezclados —«32,161 h (≈ 3.7
    /// años)», separadores ingleses con palabras españolas—.
    ///
    /// <para><b>Esto NO es <see cref="CultureInfo.CurrentCulture"/>, y a propósito.</b> Asignar la cultura
    /// del hilo cambiaría también comparaciones y mayúsculas, que es exactamente por donde volvería
    /// `T1-01` (la guarda de disco de sistema fallando bajo cultura turca). Aquí solo se formatea, y
    /// solo para pantalla: lo que se <b>guarda</b> —<c>history.log</c>, el CSV, los comandos de
    /// PowerShell— sigue pasando <see cref="CultureInfo.InvariantCulture"/> de forma explícita.</para>
    /// </summary>
    public static CultureInfo Culture { get; private set; } = CultureFor(AppLang.Es);

    public static void Set(AppLang lang)
    {
        if (Current == lang) return;
        Current = lang;
        Culture = CultureFor(lang);
    }

    /// <summary>Cultura de referencia de cada idioma soportado. Pura; no toca el hilo.</summary>
    private static CultureInfo CultureFor(AppLang lang) => CultureInfo.GetCultureInfo(lang switch
    {
        AppLang.En => "en-US",
        AppLang.Pt => "pt-BR",
        AppLang.Fr => "fr-FR",
        AppLang.It => "it-IT",
        _          => "es-ES",
    });

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

    /// <summary>
    /// Traducción con formato. Como <see cref="T(string)"/>, <b>nunca lanza</b>: si una traducción trae un
    /// marcador mal escrito (<c>{0</c>, <c>{2}</c> cuando solo hay un argumento…), <c>string.Format</c>
    /// lanzaría <see cref="FormatException"/> y tumbaría la pantalla que solo quería mostrar un texto.
    ///
    /// <para>Un error de traducción debe verse como un texto raro, no como una app que se cae: ante un
    /// fallo de formato se devuelve la plantilla <b>sin formatear</b>, que sigue siendo legible y además
    /// delata el error. Cinco idiomas por clave es sitio de sobra para una llave descolocada.</para>
    /// </summary>
    public static string T(string key, params object[] args)
    {
        string template = T(key);
        try { return string.Format(template, args); }
        catch (FormatException) { return template; }
    }

    /// <summary>Diccionario de traducciones. Orden de cada arreglo: <c>[Es, En, Pt, Fr, It]</c>.</summary>
    internal static readonly Dictionary<string, string[]> Map = new()
    {
        ["section.drive"]    = ["Unidad", "Drive", "Unidade", "Lecteur", "Unità"],
        ["section.format"]   = ["Configuración de formato", "Format settings", "Configurações de formatação", "Paramètres de formatage", "Impostazioni di formattazione"],
        ["fs.label"]         = ["Sistema de archivos", "File system", "Sistema de arquivos", "Système de fichiers", "File system"],

        // Descripción bajo el selector de sistema de archivos. Vivían como dos diccionarios ES/EN dentro
        // de MainWindow, así que PT/FR/IT veían inglés; el test de completitud no las alcanzaba porque
        // solo recorre este Map. Aquí sí quedan cubiertas.
        ["fs.desc.ntfs"]     = ["Ideal para discos internos Windows. Soporta archivos grandes, permisos y cifrado BitLocker.", "Ideal for internal Windows disks. Supports large files, permissions and BitLocker encryption.", "Ideal para discos internos do Windows. Suporta arquivos grandes, permissões e criptografia BitLocker.", "Idéal pour les disques internes Windows. Prend en charge les fichiers volumineux, les permissions et le chiffrement BitLocker.", "Ideale per dischi interni Windows. Supporta file di grandi dimensioni, permessi e crittografia BitLocker."],
        ["fs.desc.exfat"]    = ["Recomendado para memorias USB grandes (> 32 GB). Compatible con Windows, macOS y Linux sin límite de tamaño de archivo.", "Recommended for large USB drives (> 32 GB). Works on Windows, macOS and Linux with no file-size limit.", "Recomendado para pen drives grandes (> 32 GB). Compatível com Windows, macOS e Linux sem limite de tamanho de arquivo.", "Recommandé pour les grandes clés USB (> 32 Go). Compatible Windows, macOS et Linux, sans limite de taille de fichier.", "Consigliato per unità USB grandi (> 32 GB). Compatibile con Windows, macOS e Linux senza limiti di dimensione dei file."],
        ["fs.desc.refs"]     = ["Sistema resiliente a errores. Óptimo para almacenamiento de datos críticos. Requiere Windows Pro o superior.", "Error-resilient file system. Optimal for critical data storage. Requires Windows Pro or higher.", "Sistema resiliente a erros. Ideal para armazenamento de dados críticos. Requer Windows Pro ou superior.", "Système résilient aux erreurs. Optimal pour le stockage de données critiques. Nécessite Windows Pro ou supérieur.", "File system resiliente agli errori. Ottimale per l'archiviazione di dati critici. Richiede Windows Pro o superiore."],
        ["fs.desc.fat32"]    = ["Alta compatibilidad con dispositivos y consolas. Límite máximo de 4 GB por archivo.", "High compatibility with devices and consoles. Maximum 4 GB per file.", "Alta compatibilidade com dispositivos e consoles. Limite máximo de 4 GB por arquivo.", "Grande compatibilité avec les appareils et les consoles. Limite de 4 Go par fichier.", "Elevata compatibilità con dispositivi e console. Limite massimo di 4 GB per file."],
        ["fs.desc.fat"]      = ["Sistema heredado para unidades muy pequeñas (< 2 GB). Compatibilidad máxima con hardware antiguo.", "Legacy system for very small drives (< 2 GB). Maximum compatibility with old hardware.", "Sistema legado para unidades muito pequenas (< 2 GB). Compatibilidade máxima com hardware antigo.", "Système hérité pour les très petits lecteurs (< 2 Go). Compatibilité maximale avec le matériel ancien.", "Sistema legacy per unità molto piccole (< 2 GB). Massima compatibilità con hardware datato."],
        ["alloc.label"]      = ["Tamaño de unidad de asignación", "Allocation unit size", "Tamanho da unidade de alocação", "Taille d'unité d'allocation", "Dimensione unità di allocazione"],
        // Pista bajo el selector de unidad de asignación (T7-03). No nombra ninguna opción de la lista:
        // el combo se puebla con tamaños ("4 KB", "64 KB"), no con un elemento «Predeterminado» — lo que
        // hay es un valor PRESELECCIONADO por sistema de archivos (ver UpdateAllocationUnits).
        ["alloc.hint"]       = ["El valor preseleccionado es el recomendado para este sistema de archivos. Un clúster grande favorece los archivos grandes; uno pequeño desperdicia menos espacio con muchos archivos pequeños.", "The preselected value is the recommended one for this file system. A large cluster favours large files; a small one wastes less space with many small files.", "O valor pré-selecionado é o recomendado para este sistema de arquivos. Um cluster grande favorece arquivos grandes; um pequeno desperdiça menos espaço com muitos arquivos pequenos.", "La valeur présélectionnée est celle recommandée pour ce système de fichiers. Un grand cluster favorise les fichiers volumineux ; un petit gaspille moins d'espace avec de nombreux petits fichiers.", "Il valore preselezionato è quello consigliato per questo file system. Un cluster grande favorisce i file di grandi dimensioni; uno piccolo spreca meno spazio con molti file piccoli."],
        // Sin dos puntos (T6-06): es un `Header` de campo, como `fs.label` y `alloc.label` justo encima.
        // Era el único de los tres que puntuaba, y se notaba al verlos en fila.
        ["label.label"]      = ["Etiqueta del volumen", "Volume label", "Rótulo do volume", "Nom de volume", "Etichetta del volume"],
        ["options.group"]    = ["Opciones de formato", "Format options", "Opções de formatação", "Options de formatage", "Opzioni di formattazione"],
        ["opt.quick"]        = ["Formato rápido", "Quick format", "Formatação rápida", "Formatage rapide", "Formattazione rapida"],
        ["opt.compress"]     = ["Habilitar compresión (solo NTFS)", "Enable compression (NTFS only)", "Ativar compactação (apenas NTFS)", "Activer la compression (NTFS uniquement)", "Abilita compressione (solo NTFS)"],
        ["opt.secure"]       = ["Borrado seguro (sobrescribir espacio libre)", "Secure erase (overwrite free space)", "Apagamento seguro (sobrescrever espaço livre)", "Effacement sécurisé (écraser l'espace libre)", "Cancellazione sicura (sovrascrivi spazio libero)"],
        ["opt.passes"]       = ["Pasadas:", "Passes:", "Passagens:", "Passes :", "Passaggi:"],
        ["opt.smallFat32"]     = ["Crear solo una partición FAT32 pequeña y dejar el resto sin asignar", "Create only a small FAT32 partition and leave the rest unallocated", "Criar apenas uma partição FAT32 pequena e deixar o resto não alocado", "Créer uniquement une petite partition FAT32 et laisser le reste non alloué", "Crea solo una piccola partizione FAT32 e lascia il resto non allocato"],
        ["opt.smallFat32Size"] = ["Tamaño:", "Size:", "Tamanho:", "Taille :", "Dimensione:"],
        ["opt.smallFat32Go"]   = ["Reinicializar unidad ahora…", "Reinitialize drive now…", "Reinicializar unidade agora…", "Réinitialiser le lecteur maintenant…", "Reinizializza unità adesso…"],
        ["opt.smallFat32Hint"] = ["Windows no permite crear volúmenes FAT32 de más de {0}. Marca esta opción y usa Herramientas → Reinicializar unidad… (no el botón Iniciar) para crear una partición FAT32 de {1} —útil, por ejemplo, para actualizar el BIOS/UEFI de una placa base—, dejando el resto del disco sin asignar; el sistema de archivos del selector se ignora. Recuerda: FAT32 no admite archivos de más de 4 GB.", "Windows won't create FAT32 volumes larger than {0}. Check this box and use Tools → Reinitialize drive… (not the Start button) to create a {1} FAT32 partition —handy, for example, to flash a motherboard's BIOS/UEFI—, leaving the rest of the disk unallocated; the file system selector is ignored. Remember: FAT32 doesn't support files larger than 4 GB.", "O Windows não permite criar volumes FAT32 maiores que {0}. Marque esta opção e use Ferramentas → Reinicializar unidade… (não o botão Iniciar) para criar uma partição FAT32 de {1} —útil, por exemplo, para atualizar a BIOS/UEFI de uma placa-mãe—, deixando o restante do disco não alocado; o sistema de arquivos do seletor é ignorado. Lembre-se: o FAT32 não suporta arquivos maiores que 4 GB.", "Windows ne permet pas de créer des volumes FAT32 de plus de {0}. Cochez cette option et utilisez Outils → Réinitialiser le lecteur… (pas le bouton Démarrer) pour créer une partition FAT32 de {1} —utile, par exemple, pour flasher le BIOS/UEFI d'une carte mère—, laissant le reste du disque non alloué ; le système de fichiers du sélecteur est ignoré. Rappel : FAT32 ne prend pas en charge les fichiers de plus de 4 Go.", "Windows non consente di creare volumi FAT32 più grandi di {0}. Seleziona questa opzione e usa Strumenti → Reinizializza unità… (non il pulsante Avvia) per creare una partizione FAT32 da {1} —utile, ad esempio, per aggiornare il BIOS/UEFI di una scheda madre—, lasciando non allocato il resto del disco; il file system del selettore viene ignorato. Ricorda: FAT32 non supporta file più grandi di 4 GB."],
        // Variante para discos que NO llegan a 32 GB: ahí FAT32 ya está disponible en el selector, así que
        // hablar del límite de Windows (el {0} de opt.smallFat32Hint) solo confundiría. Lo que aporta la
        // opción en esos discos es dejar espacio sin asignar, y eso es lo que cuenta este texto.
        ["opt.smallFat32HintSmall"] = ["Crea una partición FAT32 de {0} y deja el resto del disco sin asignar —útil, por ejemplo, para actualizar el BIOS/UEFI de una placa base—. Usa Herramientas → Reinicializar unidad… (no el botón Iniciar); el sistema de archivos del selector se ignora. Recuerda: FAT32 no admite archivos de más de 4 GB.", "Creates a {0} FAT32 partition and leaves the rest of the disk unallocated —handy, for example, to flash a motherboard's BIOS/UEFI—. Use Tools → Reinitialize drive… (not the Start button); the file system selector is ignored. Remember: FAT32 doesn't support files larger than 4 GB.", "Cria uma partição FAT32 de {0} e deixa o restante do disco não alocado —útil, por exemplo, para atualizar a BIOS/UEFI de uma placa-mãe—. Use Ferramentas → Reinicializar unidade… (não o botão Iniciar); o sistema de arquivos do seletor é ignorado. Lembre-se: o FAT32 não suporta arquivos maiores que 4 GB.", "Crée une partition FAT32 de {0} et laisse le reste du disque non alloué —utile, par exemple, pour flasher le BIOS/UEFI d'une carte mère—. Utilisez Outils → Réinitialiser le lecteur… (pas le bouton Démarrer) ; le système de fichiers du sélecteur est ignoré. Rappel : FAT32 ne prend pas en charge les fichiers de plus de 4 Go.", "Crea una partizione FAT32 da {0} e lascia non allocato il resto del disco —utile, ad esempio, per aggiornare il BIOS/UEFI di una scheda madre—. Usa Strumenti → Reinizializza unità… (non il pulsante Avvia); il file system del selettore viene ignorato. Ricorda: FAT32 non supporta file più grandi di 4 GB."],
        // `T5-02`: qué hacer con el espacio sobrante en vez de dejarlo muerto.
        ["opt.rest"]            = ["El resto del disco:", "The rest of the disk:", "O restante do disco:", "Le reste du disque :", "Il resto del disco:"],
        ["opt.restUnallocated"] = ["Dejarlo sin asignar", "Leave it unallocated", "Deixar não alocado", "Le laisser non alloué", "Lasciarlo non allocato"],
        ["opt.restSecond"]      = ["Crear una segunda partición", "Create a second partition", "Criar uma segunda partição", "Créer une seconde partition", "Creare una seconda partizione"],
        ["opt.restFs"]          = ["Formato:", "Format:", "Formato:", "Format :", "Formato:"],
        ["opt.restLabel"]       = ["Etiqueta:", "Label:", "Rótulo:", "Nom :", "Etichetta:"],
        // La nota de plataforma va aquí y no solo en la documentación: quien crea un USB de dos particiones
        // necesita saber que un equipo viejo o un aparato empotrado puede ver solo la primera.
        ["opt.restNote"]        = ["La partición FAT32 se crea primero a propósito: Windows 10 (1703) y posteriores muestran las dos, pero equipos más antiguos y muchos aparatos (televisores, radios de coche, BIOS de placas base) solo leen la primera.", "The FAT32 partition is created first on purpose: Windows 10 (1703) and later show both, but older machines and many devices (TVs, car stereos, motherboard BIOS) only read the first one.", "A partição FAT32 é criada primeiro de propósito: o Windows 10 (1703) e posteriores mostram as duas, mas máquinas mais antigas e muitos aparelhos (TVs, rádios de carro, BIOS de placas-mãe) leem apenas a primeira.", "La partition FAT32 est créée en premier volontairement : Windows 10 (1703) et ultérieurs affichent les deux, mais les machines plus anciennes et de nombreux appareils (téléviseurs, autoradios, BIOS de cartes mères) ne lisent que la première.", "La partizione FAT32 viene creata per prima di proposito: Windows 10 (1703) e successivi mostrano entrambe, ma i computer più vecchi e molti apparecchi (TV, autoradio, BIOS di schede madri) leggono solo la prima."],
        ["btn.restore"]      = ["Restaurar valores predeterminados", "Restore defaults", "Restaurar padrões", "Restaurer les valeurs par défaut", "Ripristina predefiniti"],
        ["btn.start"]        = ["Iniciar", "Start", "Iniciar", "Démarrer", "Avvia"],
        ["btn.close"]        = ["Cerrar", "Close", "Fechar", "Fermer", "Chiudi"],
        ["btn.cancel"]       = ["Cancelar", "Cancel", "Cancelar", "Annuler", "Annulla"],
        // Motivos por los que un ítem de *Herramientas* queda apagado (T7-02). Van en el tooltip y en el
        // HelpText de automatización: un ítem gris que no dice por qué es peor que el diálogo al que
        // sustituye. Empiezan por «No disponible» porque el ítem ya se ve apagado — la frase completa,
        // que es lo que lee un lector de pantalla, tiene que decir las dos cosas.
        ["menu.whyNoDrive"]   = ["No disponible: no hay ninguna unidad seleccionada.", "Not available: no drive is selected.", "Indisponível: nenhuma unidade selecionada.", "Indisponible : aucun lecteur sélectionné.", "Non disponibile: nessuna unità selezionata."],
        ["menu.whyProtected"] = ["No disponible: la unidad está protegida o es el disco del sistema.", "Not available: the drive is protected or is the system disk.", "Indisponível: a unidade está protegida ou é o disco do sistema.", "Indisponible : le lecteur est protégé ou c'est le disque système.", "Non disponibile: l'unità è protetta o è il disco di sistema."],
        ["menu.whyRemovable"] = ["No disponible: solo para unidades extraíbles.", "Not available: removable drives only.", "Indisponível: apenas para unidades removíveis.", "Indisponible : uniquement pour les lecteurs amovibles.", "Non disponibile: solo per unità rimovibili."],
        // Y la MISMA razón, en corto, pegada al texto del ítem (T7-08). WinUI no muestra el tooltip de un
        // control deshabilitado —no hay `ShowOnDisabled` como en WPF—, así que el motivo de arriba solo le
        // llegaba al lector de pantalla: quien mira la pantalla veía un ítem gris y mudo. Estas etiquetas
        // se pintan siempre, sin necesidad de apuntar con el ratón. Van entre paréntesis y en minúscula
        // porque son un apéndice del nombre del ítem, no una frase aparte.
        ["menu.tagNoDrive"]   = ["(sin unidad)", "(no drive)", "(sem unidade)", "(aucun lecteur)", "(nessuna unità)"],
        ["menu.tagProtected"] = ["(unidad protegida)", "(drive protected)", "(unidade protegida)", "(lecteur protégé)", "(unità protetta)"],
        ["menu.tagRemovable"] = ["(solo extraíbles)", "(removable only)", "(apenas removíveis)", "(amovibles seulement)", "(solo rimovibili)"],
        // Cuando el shell no abre el navegador, el enlace se enseña para que se pueda copiar a mano. La
        // dirección va dentro del texto: decir "no se pudo abrir" y ocultar adónde iba no sirve de nada.
        ["link.failed"]      = ["No se pudo abrir el navegador. La dirección es: {0}", "The browser could not be opened. The address is: {0}", "Não foi possível abrir o navegador. O endereço é: {0}", "Impossible d'ouvrir le navigateur. L'adresse est : {0}", "Impossibile aprire il browser. L'indirizzo è: {0}"],
        ["history.openFailed"] = ["No se pudo abrir el archivo del historial", "The history file could not be opened", "Não foi possível abrir o arquivo do histórico", "Impossible d'ouvrir le fichier d'historique", "Impossibile aprire il file della cronologia"],
        ["tip.refresh"]      = ["Actualizar lista de unidades", "Refresh drive list", "Atualizar lista de unidades", "Actualiser la liste des lecteurs", "Aggiorna elenco unità"],
        ["drive.none"]       = ["No hay unidades — conecta un dispositivo", "No drives — connect a device", "Sem unidades — conecte um dispositivo", "Aucun lecteur — connectez un périphérique", "Nessuna unità — collega un dispositivo"],

        ["info.total"]       = ["Total: {0}", "Total: {0}", "Total: {0}", "Total : {0}", "Totale: {0}"],
        ["info.free"]        = ["Libre: {0}", "Free: {0}", "Livre: {0}", "Libre : {0}", "Libero: {0}"],
        ["info.fs"]          = ["Sistema actual: {0}", "Current FS: {0}", "Sistema atual: {0}", "Système actuel : {0}", "Sistema attuale: {0}"],
        ["info.type"]        = ["Tipo: {0}", "Type: {0}", "Tipo: {0}", "Type : {0}", "Tipo: {0}"],
        ["info.health"]      = ["Salud: {0}", "Health: {0}", "Saúde: {0}", "Santé : {0}", "Stato: {0}"],
        ["info.bus"]         = ["Conexión: {0}", "Bus: {0}", "Conexão: {0}", "Connexion : {0}", "Connessione: {0}"],
        ["info.used"]        = ["Espacio utilizado: {0} %", "Used space: {0}%", "Espaço utilizado: {0} %", "Espace utilisé : {0} %", "Spazio utilizzato: {0} %"],
        // Bloque de la barra de ocupación: etiqueta a la izquierda, cuánto se usa de cuánto a la derecha.
        ["info.capacity"]    = ["Ocupación", "Usage", "Ocupação", "Utilisation", "Utilizzo"],
        ["info.usedOf"]      = ["Usado {0} / {1}", "Used {0} / {1}", "Usado {0} / {1}", "Utilisé {0} / {1}", "Usato {0} / {1}"],
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
        // Recuento de lo que se está viendo tras buscar/filtrar (T7-05): «12 de 340». Los dos números
        // llegan YA formateados con L.Culture desde el diálogo — string.Format usaría la cultura de
        // Windows y volvería a mezclar separadores ingleses con texto en español (T6-12).
        ["history.count"]        = ["{0} de {1}", "{0} of {1}", "{0} de {1}", "{0} sur {1}", "{0} di {1}"],
        ["history.filter.catName"]= ["Filtrar por categoría", "Filter by category", "Filtrar por categoria", "Filtrer par catégorie", "Filtra per categoria"],
        ["history.filter.resName"]= ["Filtrar por resultado", "Filter by result", "Filtrar por resultado", "Filtrer par résultat", "Filtra per risultato"],
        ["history.filter.allCat"]= ["Todas las categorías", "All categories", "Todas as categorias", "Toutes les catégories", "Tutte le categorie"],
        ["history.filter.allRes"]= ["Todos los resultados", "All results", "Todos os resultados", "Tous les résultats", "Tutti i risultati"],
        ["history.export"]       = ["Exportar CSV", "Export CSV", "Exportar CSV", "Exporter CSV", "Esporta CSV"],
        ["history.open"]         = ["Abrir archivo", "Open file", "Abrir arquivo", "Ouvrir le fichier", "Apri file"],
        ["history.clear"]        = ["Vaciar historial", "Clear history", "Limpar histórico", "Effacer l'historique", "Cancella cronologia"],
        // Nombre del tipo de archivo en el diálogo «Guardar como». Lleva la extensión dentro porque es
        // lo que hace Windows en su propio selector: «CSV (*.csv)», no «CSV» a secas.
        ["history.exportType"]   = ["CSV (*.csv)", "CSV (*.csv)", "CSV (*.csv)", "CSV (*.csv)", "CSV (*.csv)"],
        ["history.exportFailed"] = ["No se pudo exportar el CSV", "The CSV could not be exported", "Não foi possível exportar o CSV", "Impossible d'exporter le CSV", "Impossibile esportare il CSV"],
        ["history.clearConfirm"] =["¿Vaciar el historial?", "Clear the history?", "Limpar o histórico?", "Effacer l'historique ?", "Cancellare la cronologia?"],
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
        ["health.spindle"]  = ["Velocidad de rotación", "Spindle speed", "Velocidade de rotação", "Vitesse de rotation", "Velocità di rotazione"],
        ["health.temp"]     = ["Temperatura", "Temperature", "Temperatura", "Température", "Temperatura"],
        ["health.hours"]    = ["Horas de encendido", "Power-on hours", "Horas ligado", "Heures de fonctionnement", "Ore di accensione"],
        ["health.wear"]     = ["Desgaste (SSD)", "Wear (SSD)", "Desgaste (SSD)", "Usure (SSD)", "Usura (SSD)"],
        ["health.readErr"]  = ["Errores de lectura", "Read errors", "Erros de leitura", "Erreurs de lecture", "Errori di lettura"],
        ["health.writeErr"] = ["Errores de escritura", "Write errors", "Erros de escrita", "Erreurs d'écriture", "Errori di scrittura"],
        ["health.unit.temp"]    = ["{0} °C", "{0} °C", "{0} °C", "{0} °C", "{0} °C"],
        ["health.unit.hours"]   = ["{0} h", "{0} h", "{0} h", "{0} h", "{0} h"],
        // Horas de encendido con su equivalencia legible (T6-04). Siempre con un decimal: «1,0 años»
        // concuerda en los cinco idiomas y «1 años» no, así que no hay que pluralizar nada.
        ["health.unit.hoursWith"] = ["{0} h ({1})", "{0} h ({1})", "{0} h ({1})", "{0} h ({1})", "{0} h ({1})"],
        ["health.span.days"]    = ["≈ {0} días", "≈ {0} days", "≈ {0} dias", "≈ {0} jours", "≈ {0} giorni"],
        ["health.span.months"]  = ["≈ {0} meses", "≈ {0} months", "≈ {0} meses", "≈ {0} mois", "≈ {0} mesi"],
        ["health.span.years"]   = ["≈ {0} años", "≈ {0} years", "≈ {0} anos", "≈ {0} ans", "≈ {0} anni"],
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
        // Qué distingue a una de otra (T6-10). Sin esto se elegía a ciegas, y la opción equivocada deja la
        // unidad ocupada un buen rato.
        ["check.scanOnlyDesc"]   = ["Solo informa: no cambia nada y puedes seguir usando la unidad.", "Reports problems only: nothing is changed and the drive stays usable.", "Apenas informa: não altera nada e a unidade continua utilizável.", "Signale uniquement : rien n'est modifié et le lecteur reste utilisable.", "Solo segnala: non modifica nulla e l'unità resta utilizzabile."],
        ["check.repairDesc"]     = ["Corrige lo que encuentre. Necesita uso exclusivo de la unidad y puede tardar mucho más.", "Fixes what it finds. Needs exclusive use of the drive and can take much longer.", "Corrige o que encontrar. Precisa de uso exclusivo da unidade e pode demorar muito mais.", "Corrige ce qu'il trouve. Nécessite un accès exclusif au lecteur et peut être bien plus long.", "Corregge ciò che trova. Richiede l'uso esclusivo dell'unità e può richiedere molto più tempo."],
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
        ["reinit.summary"]       = ["Se borrará TODO el disco físico de la unidad {0}: (todas sus particiones) y se recreará una única partición {1} formateada en {2}.\n\nEsta acción NO se puede deshacer.", "The ENTIRE physical disk of drive {0}: will be erased (all its partitions) and a single {1} partition formatted as {2} will be recreated.\n\nThis action CANNOT be undone.", "TODO o disco físico da unidade {0}: será apagado (todas as suas partições) e será recriada uma única partição {1} formatada em {2}.\n\nEsta ação NÃO pode ser desfeita.", "TOUT le disque physique du lecteur {0}: sera effacé (toutes ses partitions) et une seule partition {1} formatée en {2} sera recréée.\n\nCette action est IRRÉVERSIBLE.", "L'INTERO disco fisico dell'unità {0}: verrà cancellato (tutte le sue partizioni) e verrà ricreata un'unica partizione {1} formattata in {2}.\n\nQuesta azione NON può essere annullata."],
        ["reinit.stage.clean"]     = ["Reinicializando {0}: — limpiando disco…", "Reinitializing {0}: — cleaning disk…", "Reinicializando {0}: — limpando disco…", "Réinitialisation de {0}: — nettoyage du disque…", "Reinizializzazione di {0}: — pulizia disco…"],
        ["reinit.stage.init"]      = ["Reinicializando {0}: — inicializando disco…", "Reinitializing {0}: — initializing disk…", "Reinicializando {0}: — inicializando disco…", "Réinitialisation de {0}: — initialisation du disque…", "Reinizializzazione di {0}: — inizializzazione disco…"],
        ["reinit.stage.partition"] = ["Reinicializando {0}: — creando partición…", "Reinitializing {0}: — creating partition…", "Reinicializando {0}: — criando partição…", "Réinitialisation de {0}: — création de la partition…", "Reinizializzazione di {0}: — creazione partizione…"],
        ["reinit.stage.format"]    = ["Reinicializando {0}: — formateando…", "Reinitializing {0}: — formatting…", "Reinicializando {0}: — formatando…", "Réinitialisation de {0}: — formatage…", "Reinizializzazione di {0}: — formattazione…"],
        ["reinit.doneTitle"]     = ["Unidad reinicializada", "Drive reinitialized", "Unidade reinicializada", "Lecteur réinitialisé", "Unità reinizializzata"],
        ["reinit.doneBody"]      = ["La unidad se reinicializó correctamente y ahora está disponible como {0}:.", "The drive was reinitialized successfully and is now available as {0}:.", "A unidade foi reinicializada com sucesso e agora está disponível como {0}:.", "Le lecteur a été réinitialisé avec succès et est maintenant disponible en tant que {0}:.", "L'unità è stata reinizializzata correttamente ed è ora disponibile come {0}:."],
        ["reinit.failed"]        = ["No se pudo reinicializar la unidad.", "Could not reinitialize the drive.", "Não foi possível reinicializar a unidade.", "Impossible de réinitialiser le lecteur.", "Impossibile reinizializzare l'unità."],
        ["reinit.summaryFat32Small"] = ["Se borrará TODO el disco físico de la unidad {0}: (todas sus particiones) y se creará una única partición FAT32 de {1}; el resto del disco quedará SIN ASIGNAR.\n\nEsta acción NO se puede deshacer.", "The ENTIRE physical disk of drive {0}: will be erased (all its partitions) and a single {1} FAT32 partition will be created; the rest of the disk will be left UNALLOCATED.\n\nThis action CANNOT be undone.", "TODO o disco físico da unidade {0}: será apagado (todas as suas partições) e será criada uma única partição FAT32 de {1}; o restante do disco ficará NÃO ALOCADO.\n\nEsta ação NÃO pode ser desfeita.", "TOUT le disque physique du lecteur {0}: sera effacé (toutes ses partitions) et une seule partition FAT32 de {1} sera créée ; le reste du disque restera NON ALLOUÉ.\n\nCette action est IRRÉVERSIBLE.", "L'INTERO disco fisico dell'unità {0}: verrà cancellato (tutte le sue partizioni) e verrà creata un'unica partizione FAT32 da {1}; il resto del disco rimarrà NON ALLOCATO.\n\nQuesta azione NON può essere annullata."],
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
        ["crash.body"]       = ["Se produjo un error inesperado y la operación se detuvo. La aplicación sigue abierta y el detalle quedó registrado en el historial.\n\n{0}", "An unexpected error occurred and the operation stopped. The application is still open and the details were written to the history.\n\n{0}", "Ocorreu um erro inesperado e a operação foi interrompida. O aplicativo continua aberto e os detalhes foram registrados no histórico.\n\n{0}", "Une erreur inattendue s'est produite et l'opération s'est arrêtée. L'application reste ouverte et le détail a été enregistré dans l'historique.\n\n{0}", "Si è verificato un errore imprevisto e l'operazione si è interrotta. L'applicazione resta aperta e il dettaglio è stato registrato nella cronologia.\n\n{0}"],
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

        // Dos títulos, no uno: ConfirmDialog lo comparten formatear y reinicializar, y hasta la revisión
        // de UX/UI del 2026-08-17 las dos se anunciaban como «Confirmar formato» (T6-01). Reinicializar
        // borra el disco físico ENTERO: no puede presentarse con el nombre de la operación menos grave.
        ["confirm.title"]      = ["Confirmar formato", "Confirm format", "Confirmar formatação", "Confirmer le formatage", "Conferma formattazione"],
        ["confirm.titleReinit"] = ["Confirmar reinicialización", "Confirm reinitialization", "Confirmar reinicialização", "Confirmer la réinitialisation", "Conferma reinizializzazione"],
        ["confirm.warning"]  = ["ADVERTENCIA: Se destruirán TODOS los datos en:", "WARNING: ALL data will be destroyed on:", "AVISO: TODOS os dados serão destruídos em:", "AVERTISSEMENT : TOUTES les données seront détruites sur :", "AVVISO: TUTTI i dati verranno distrutti su:"],
        // Nombre accesible del campo donde se teclea la letra. Explícito a propósito (T6-02): sin él, WinUI
        // usa el PlaceholderText como nombre, y el placeholder era la propia letra a adivinar.
        ["confirm.inputName"] = ["Letra de la unidad", "Drive letter", "Letra da unidade", "Lettre du lecteur", "Lettera dell'unità"],
        ["confirm.drive"]    = ["Unidad", "Drive", "Unidade", "Lecteur", "Unità"],
        ["confirm.fs"]       = ["Sistema", "File system", "Sistema", "Système", "Sistema"],
        ["confirm.cluster"]  = ["Cluster", "Cluster", "Cluster", "Cluster", "Cluster"],
        ["confirm.label"]    = ["Etiqueta", "Label", "Rótulo", "Nom", "Etichetta"],
        ["confirm.nolabel"]  = ["(sin etiqueta)", "(no label)", "(sem rótulo)", "(sans nom)", "(senza etichetta)"],
        ["confirm.mode"]     = ["Tipo", "Mode", "Tipo", "Mode", "Tipo"],
        ["confirm.secure"]   = ["Borrado seguro", "Secure erase", "Apagamento seguro", "Effacement sécurisé", "Cancellazione sicura"],
        // `T5-03`: el fallo a mitad de un plan de varias particiones. El disco ya está borrado, así que lo
        // único útil que se puede decir es qué quedó en él — y dejar claro que la app no ha tocado nada más.
        ["reinit.failedPartial"] = ["No se pudo completar la reinicialización.\n\nEl disco YA estaba borrado cuando falló, así que no ha quedado como estaba. Se crearon {0} de {1} particiones y quedaron utilizables: {2}.\n\nNo se ha borrado nada más: revisa la unidad y vuelve a reinicializarla cuando quieras.", "The reinitialization could not be completed.\n\nThe disk had ALREADY been erased when it failed, so it is not as it was. {0} of {1} partitions were created and these are usable: {2}.\n\nNothing else was erased: check the drive and reinitialize it again whenever you want.", "Não foi possível concluir a reinicialização.\n\nO disco JÁ estava apagado quando falhou, então não ficou como estava. Foram criadas {0} de {1} partições e ficaram utilizáveis: {2}.\n\nNada mais foi apagado: verifique a unidade e reinicialize-a quando quiser.", "La réinitialisation n'a pas pu être terminée.\n\nLe disque était DÉJÀ effacé au moment de l'échec, il n'est donc pas dans son état d'origine. {0} partitions sur {1} ont été créées et celles-ci sont utilisables : {2}.\n\nRien d'autre n'a été effacé : vérifiez le lecteur et réinitialisez-le quand vous le souhaitez.", "Non è stato possibile completare la reinizializzazione.\n\nIl disco era GIÀ stato cancellato quando è fallita, quindi non è come prima. Sono state create {0} partizioni su {1} e queste sono utilizzabili: {2}.\n\nNon è stato cancellato nient'altro: controlla l'unità e reinizializzala quando vuoi."],
        ["reinit.noneUsable"]    = ["ninguna", "none", "nenhuma", "aucune", "nessuna"],
        ["reinit.summaryTwoPartitions"] = ["Se borrará TODO el disco físico de la unidad {0}: (todas sus particiones) y se crearán DOS particiones:\n  1) FAT32 de {1}\n  2) {2} con el resto del disco ({3})\n\nEsta acción NO se puede deshacer.", "The ENTIRE physical disk of drive {0}: will be erased (all its partitions) and TWO partitions will be created:\n  1) {1} FAT32\n  2) {2} with the rest of the disk ({3})\n\nThis action CANNOT be undone.", "TODO o disco físico da unidade {0}: será apagado (todas as suas partições) e serão criadas DUAS partições:\n  1) FAT32 de {1}\n  2) {2} com o restante do disco ({3})\n\nEsta ação NÃO pode ser desfeita.", "TOUT le disque physique du lecteur {0}: sera effacé (toutes ses partitions) et DEUX partitions seront créées :\n  1) FAT32 de {1}\n  2) {2} avec le reste du disque ({3})\n\nCette action est IRRÉVERSIBLE.", "L'INTERO disco fisico dell'unità {0}: verrà cancellato (tutte le sue partizioni) e verranno create DUE partizioni:\n  1) FAT32 da {1}\n  2) {2} con il resto del disco ({3})\n\nQuesta azione NON può essere annullata."],
        ["reinit.doneBodyTwoPartitions"] = ["La unidad se reinicializó correctamente: ahora tiene una partición FAT32 de {1} en {0}: y una segunda partición de {3} en {2}:, sin dejar espacio sin asignar.", "The drive was reinitialized successfully: it now has a {1} FAT32 partition on {0}: and a second {3} partition on {2}:, with no unallocated space left.", "A unidade foi reinicializada com sucesso: agora tem uma partição FAT32 de {1} em {0}: e uma segunda partição de {3} em {2}:, sem deixar espaço não alocado.", "Le lecteur a été réinitialisé avec succès : il dispose maintenant d'une partition FAT32 de {1} sur {0}: et d'une seconde partition de {3} sur {2}:, sans espace non alloué.", "L'unità è stata reinizializzata correttamente: ora ha una partizione FAT32 da {1} su {0}: e una seconda partizione da {3} su {2}:, senza spazio non allocato."],
        ["reinit.invalidPlan"]   = ["La distribución de particiones pedida no es válida para este disco. No se ha modificado nada.", "The requested partition layout isn't valid for this disk. Nothing was changed.", "O layout de partições solicitado não é válido para este disco. Nada foi alterado.", "La disposition de partitions demandée n'est pas valide pour ce disque. Rien n'a été modifié.", "Il layout di partizioni richiesto non è valido per questo disco. Non è stato modificato nulla."],
        ["reinit.sizeTooBig"]    = ["La partición de {0} no cabe en este disco ({1}). Elige un tamaño menor.", "A {0} partition doesn't fit on this disk ({1}). Choose a smaller size.", "Uma partição de {0} não cabe neste disco ({1}). Escolha um tamanho menor.", "Une partition de {0} ne tient pas sur ce disque ({1}). Choisissez une taille inférieure.", "Una partizione da {0} non entra in questo disco ({1}). Scegli una dimensione minore."],
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
        // Nombres de los cinco presets integrados (`Presets.All`). Vivían fijos en español dentro de
        // Core y el menú los pintaba tal cual en los 5 idiomas; el test de completitud no los alcanzaba
        // porque solo recorre este Map. Ver `Presets.DisplayName`.
        ["preset.builtin.usb"]        = ["USB universal (Windows / macOS / Linux)", "Universal USB (Windows / macOS / Linux)", "USB universal (Windows / macOS / Linux)", "Clé USB universelle (Windows / macOS / Linux)", "USB universale (Windows / macOS / Linux)"],
        ["preset.builtin.console"]    = ["Consola / TV / Cámara", "Console / TV / Camera", "Console / TV / Câmera", "Console / TV / Appareil photo", "Console / TV / Fotocamera"],
        ["preset.builtin.windowsData"]= ["Disco de datos Windows", "Windows data disk", "Disco de dados Windows", "Disque de données Windows", "Disco dati Windows"],
        ["preset.builtin.compressed"] = ["Almacenamiento comprimido (NTFS)", "Compressed storage (NTFS)", "Armazenamento comprimido (NTFS)", "Stockage compressé (NTFS)", "Archiviazione compressa (NTFS)"],
        ["preset.builtin.secureWipe"] = ["Borrado seguro + NTFS", "Secure erase + NTFS", "Apagamento seguro + NTFS", "Effacement sécurisé + NTFS", "Cancellazione sicura + NTFS"],

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
        // Confirmación de borrado de un preset (T7-01). Lleva el nombre dentro: en una lista de botones
        // de papelera idénticos, «¿Eliminar?» a secas no dice CUÁL se está a punto de perder.
        ["preset.deleteConfirm"] = ["¿Eliminar «{0}»?", "Delete “{0}”?", "Excluir «{0}»?", "Supprimer « {0} » ?", "Eliminare «{0}»?"],

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
        // Motivos por los que se RECHAZA un instalador ya descargado (UpdateService.VerifyInstallerAsync).
        // Llegan al usuario dentro de update.error, así que explican qué pasó y qué puede hacer.
        ["update.unverifiable"]   = ["El instalador descargado no se pudo verificar: esta versión no publica su hash SHA-256, así que no hay forma de comprobar que sea el que publicó el proyecto. Se ha descartado por seguridad. Descarga la actualización manualmente desde la página del proyecto en GitHub.", "The downloaded installer could not be verified: this release does not publish its SHA-256 hash, so there is no way to confirm it is the one the project published. It was discarded for safety. Download the update manually from the project page on GitHub.", "Não foi possível verificar o instalador baixado: esta versão não publica o seu hash SHA-256, portanto não há como confirmar que seja o que o projeto publicou. Foi descartado por segurança. Baixe a atualização manualmente na página do projeto no GitHub.", "L'installateur téléchargé n'a pas pu être vérifié : cette version ne publie pas son empreinte SHA-256, il est donc impossible de confirmer qu'il s'agit bien de celui publié par le projet. Il a été supprimé par sécurité. Téléchargez la mise à jour manuellement depuis la page du projet sur GitHub.", "Non è stato possibile verificare il programma di installazione scaricato: questa versione non pubblica il suo hash SHA-256, quindi non c'è modo di confermare che sia quello pubblicato dal progetto. È stato scartato per sicurezza. Scarica l'aggiornamento manualmente dalla pagina del progetto su GitHub."],
        ["update.checksumMismatch"] = ["El instalador descargado no coincide con el hash SHA-256 publicado, así que no es el que publicó el proyecto: puede estar dañado o haber sido manipulado. Se ha descartado y no se ha ejecutado nada.", "The downloaded installer does not match the published SHA-256 hash, so it is not the one the project published: it may be corrupted or tampered with. It was discarded and nothing was run.", "O instalador baixado não corresponde ao hash SHA-256 publicado, portanto não é o que o projeto publicou: pode estar danificado ou ter sido adulterado. Foi descartado e nada foi executado.", "L'installateur téléchargé ne correspond pas à l'empreinte SHA-256 publiée : ce n'est pas celui publié par le projet, il peut être corrompu ou altéré. Il a été supprimé et rien n'a été exécuté.", "Il programma di installazione scaricato non corrisponde all'hash SHA-256 pubblicato, quindi non è quello pubblicato dal progetto: potrebbe essere danneggiato o manomesso. È stato scartato e non è stato eseguito nulla."],

        ["update.checksumUnreadable"] = ["No se pudo leer el hash SHA-256 publicado: el servidor devolvió algo que no es un checksum. El instalador se ha descartado sin ejecutarlo. Descarga la actualización manualmente desde la página del proyecto en GitHub.", "The published SHA-256 hash could not be read: the server returned something that is not a checksum. The installer was discarded without running it. Download the update manually from the project page on GitHub.", "Não foi possível ler o hash SHA-256 publicado: o servidor devolveu algo que não é um checksum. O instalador foi descartado sem ser executado. Baixe a atualização manualmente na página do projeto no GitHub.", "Impossible de lire l'empreinte SHA-256 publiée : le serveur a renvoyé autre chose qu'une somme de contrôle. L'installateur a été supprimé sans être exécuté. Téléchargez la mise à jour manuellement depuis la page du projet sur GitHub.", "Non è stato possibile leggere l'hash SHA-256 pubblicato: il server ha restituito qualcosa che non è un checksum. Il programma di installazione è stato scartato senza eseguirlo. Scarica l'aggiornamento manualmente dalla pagina del progetto su GitHub."],

        ["whatsnew.title"]      = ["Novedades de FormatDiskPro", "What's new in FormatDiskPro", "Novidades do FormatDiskPro", "Nouveautés de FormatDiskPro", "Novità di FormatDiskPro"],
        ["whatsnew.version"]    = ["Versión {0}", "Version {0}", "Versão {0}", "Version {0}", "Versione {0}"],
        ["whatsnew.viewOnGitHub"]= ["Ver en GitHub", "View on GitHub", "Ver no GitHub", "Voir sur GitHub", "Vedi su GitHub"],
        ["whatsnew.empty"]      = ["No se pudieron cargar las novedades. Puedes verlas en GitHub.", "Could not load the release notes. You can view them on GitHub.", "Não foi possível carregar as novidades. Você pode vê-las no GitHub.", "Impossible de charger les nouveautés. Vous pouvez les consulter sur GitHub.", "Impossibile caricare le novità. Puoi vederle su GitHub."],
    };
}
