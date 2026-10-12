# Área 10 · Ortografía y redacción · hallazgos provisionales

> **VERSIÓN PARCIAL (punto de control 1).** Lo que sigue está confirmado en estática. Faltan por cerrar:
> clasificación nuevo/conocido de dos hallazgos, `CHANGELOG.md`, `docs/release-notes-*.md` y el muestreo de
> `CONTEXT.md`/`ROADMAP.md`. Se sustituirá por la versión completa.

Revisado: `src/FormatDiskPro/Localization/Localization.cs` entero (326 claves × 5 idiomas, leído y además
analizado con un script propio: marcadores, puntuación final, espacios, comillas, signo %, raya), los diez
`*.xaml` de `src/FormatDiskPro/` y todo el code-behind de `UI/` buscando literales que no pasan por `L.T`,
`Core/LegalText.cs`, `installer/installer.iss`, `README.md`, `.github/*.md` y las tres plantillas de issue,
`THIRD-PARTY-NOTICES.txt`.

Herramientas ejecutadas: Node v24.20.0 con dos scripts propios de solo lectura (en el scratchpad de la sesión).
Resultado: 326 entradas, las 326 con 5 valores; **0** marcadores `{n}` que falten o sobren respecto al
español; **0** llaves sueltas; **0** `...` ASCII; **0** cadenas con espacio inicial o final involuntario.

## Hallazgos (provisional, sin ordenar todavía)

### P-10-01 · La pista de «FAT32 pequeña» manda no usar «el botón Iniciar», que ya no existe
**Área:** Redacción · **Severidad:** Bajo · **Confianza:** Alta · **Verificación:** estática · **Esfuerzo:** Bajo
**Ubicación:** `src/FormatDiskPro/Localization/Localization.cs:147` (`opt.smallFat32Hint`) y `:151` (`opt.smallFat32HintSmall`), 5 idiomas cada una = 10 cadenas · `.github/ISSUE_TEMPLATE/bug_report.yml:69` («3. Pulsar Iniciar»)
**Estado:** nuevo (resto de `T12-02`: renombró el botón el 2026-09-01 y no tocó los textos que lo nombran)
**Problema:** «usa Herramientas → Reinicializar unidad… (no el botón Iniciar)» / «(not the Start button)» / «(não o botão Iniciar)» / «(pas le bouton Démarrer)» / «(non il pulsante Avvia)». El botón primario se llama «Formatear X:» desde `T12-02` (`MainWindow.FormatOptions.cs:501-503`, claves `btn.start` = «Formatear», `btn.start.drive` = «Formatear {0}»).
**Solución propuesta:** nombrar el botón como se ve: «(no el botón Formatear)» en los cinco idiomas; corregir el placeholder de la plantilla.

### P-10-02 · El aviso de privacidad nombra un menú que no existe con ese nombre en EN, PT, FR e IT
**Área:** Redacción · **Severidad:** Bajo · **Confianza:** Alta · **Verificación:** estática · **Esfuerzo:** Bajo
**Ubicación:** `Localization.cs:477` (`about.privacy`) frente a `:232` (`menu.config`)
**Estado:** nuevo (el texto se reescribió en `T9-18`)
**Problema:** EN «turn it off in **Configuration** →» (el menú es «Settings»); PT «em **Configuração** →» («Configurações»); FR «dans **Configuration** →» («Paramètres»); IT «in **Configurazione** →» («Impostazioni»). En ES coincide.

### P-10-03 · `reinit.plan.invalidLabel` dice que exFAT admite 32 caracteres; el código valida 11
**Área:** Redacción · **Severidad:** Bajo · **Confianza:** Alta · **Verificación:** estática · **Esfuerzo:** Bajo
**Ubicación:** `Localization.cs:439` (5 idiomas) frente a `src/FormatDiskPro/Core/FormatLogic.cs:122-127`
**Estado:** nuevo (entró con `T13-18`)
**Problema:** «FAT32 y FAT admiten 11 caracteres; NTFS, exFAT y ReFS, 32». `MaxLabelLength` devuelve 11 para `"FAT32" or "FAT" or "exFAT"`.

### P-10-04 · Las IOPS del benchmark se formatean con la cultura de Windows, no con la del idioma de la app
**Ubicación:** `src/FormatDiskPro/UI/MainWindow.Operations.cs:623` (`:N0` en una cadena interpolada)
**Estado:** `T6-12` cerrada en falso para este punto (pendiente de redactar)

### P-10-05 · El español mezcla tú y usted (6 cadenas de usted frente al resto en tú)
**Ubicación:** `Localization.cs:395`, `:396`, `:406`, `:446`, `:455`, `:457`

### P-10-06 · Terminología no uniforme dentro del mismo idioma (ES, PT, FR, IT)
ES removible/extraíble; ES «disco de sistema»/«del sistema»; ES Cluster/clúster; ES `preset.body` «Configuración»; ES `msg.selectFsAlloc` «tamaño de unidad»; PT `fmt.presets` «Presets» frente a «Predefinições»; FR «étiquette de volume» frente a «nom de volume»; IT «Vedi»/«Visualizza su GitHub».

### P-10-07 · Convenciones tipográficas sin fijar: signo %, comillas inglesas, raya española en otros idiomas, variante del inglés

### P-10-08 · Texto visible que no pasa por `Localization`: «ETA», unidades (KB/MB/GB), nombre del CSV, mensajes de excepción en español

### P-10-09 · En francés conviven «Go/Mo/Kio» (texto fijo) y «GB/MB/KB» (valores calculados)

### P-10-10 · Puntos suspensivos desiguales en el menú Ayuda (`menu.license`, `menu.thirdParty`)

## Hipótesis por verificar
- Espacio antes de `: ? ! »` en francés: es U+0020, no U+00A0/U+202F (no reabre «Tipografía francesa»: el espacio está; lo que falta comprobar es si un salto de línea deja el signo huérfano).
- Comillas «» en portugués de Brasil.
- IT `about.disclaimer` «formatta ed elimina unità».
- `fs.desc.refs` «Requiere Windows Pro o superior».

## Pendiente de verificar en ejecución
(por completar)

## Puntos fuertes
(por completar)

## Para otras áreas
- Área 9: clave muerta `about.body` (`Localization.cs:470`), sin ninguna referencia en `src/`.
- Área 6: los nombres de idioma del menú se traducen al idioma activo (`MainWindow.Preferences.cs:168-172`) en vez de mostrarse cada uno en el suyo.
