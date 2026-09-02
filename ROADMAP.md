# FormatDiskPro — Hoja de ruta

> **Qué hay aquí:** las características agrupadas por **tiers**, con dónde vive cada una en la arquitectura
> por capas (`Core` lógica pura testeable · `Services` efectos colaterales · `UI` WinUI 3 · `Localization`).
>
> **Qué NO hay aquí:** el detalle de cómo se resolvió cada cosa y por qué — eso vive en
> [`CONTEXT.md`](CONTEXT.md) (§4 *Decisiones* y el *Registro de cambios*).
>
> **Propósito del proyecto:** **formatear, diagnosticar y gestionar unidades en Windows**. Todo lo que hay
> aquí cabe dentro de eso; lo que no, está al final y está fuera **a propósito**.

> **Este archivo tiene dos partes y dos numeraciones distintas — no las mezcles:**
>
> | Parte | Qué es | IDs |
> |---|---|---|
> | **Parte 1** (abajo) | **Historial de producto**: las características entregadas, por tiers de entrega. Cerrada. | `#1`–`#45` |
> | **Parte 2** (al final) | **Backlog de remediación** de la auditoría técnica del **2026-08-13**. Cerrada. | `T0-01`–`T4-05` |
>
> Al final de la Parte 2 hay además seis tiers que **no** son parte de la auditoría (que sigue siendo de
> 40 tareas): **Tier 5 — Ocurrencias para features existentes** (`T5-01`–`T5-05`), ampliaciones de lo ya
> entregado, **Tier 6 — Refinado de UX/UI** (`T6-01`–`T6-15`), cerrado también, **Tier 7 — Consistencia
> y descubribilidad de la UI** (`T7-01`–`T7-09`) y **Tier 8 — Lo que solo se ve usando la app**
> (`T8-01`–`T8-06`), ambos cerrados el 2026-08-26, y **Tier 9 — Re-auditoría transversal con la app en
> marcha** (`T9-01`–`T9-20`), **abierto y cerrado el 2026-08-26**: 20 tareas (1 Alta · 9 Medias · 10 Bajas),
> **20/20 completadas**. Y **Tier 10 — Lo que solo aparece al publicar** (`T10-01`–`T10-02`), **abierto el
> 2026-08-26** y el único con trabajo pendiente: no nace de una revisión, sino del corte de la v1.25.0.
>
> **Los IDs no se reutilizan nunca**, tampoco los de tareas descartadas: viven en commits e issues.

## 🏁 Estado

> **Se abrió y se cerró el [Tier 11](#-tier-11--rendimiento-y-jerarquía-de-la-ventana-principal-abierto-2026-09-01)**
> el **2026-09-01**, **4/4**. No sale de un fallo sino de una petición de producto sobre la ventana
> principal, y las cuatro tareas atacan la misma raíz —**qué se ve y con qué peso**—: `T11-01`, el pie
> enseña disco, CPU y RAM mientras corre la operación (antes, en un borrado seguro de 40 minutos la única
> señal de vida era una barra quieta); `T11-02`, las tres herramientas que **no escriben nada** salen del
> menú a una barra de acciones; `T11-03`, la tarjeta de unidad se ordena por importancia en vez de
> repartir seis datos con el mismo peso; y `T11-04`, ese panel de rendimiento deja de ser un desplegable
> y pasa a una franja fija de tres columnas — al compactarlo desapareció el motivo de poder plegarlo.
>
> **Publicado en la v1.26.0** (2026-09-01). **Galería regenerada y suite de UI en verde** (con la USB conectada): las **12** capturas
> del README rehechas conduciendo la app real, y **34 pasan / 3 se omiten / 0 fallan** en 1 m 57 s. Las 3
> omitidas son de opt-in (`ALLOW_YANK` ×2, `ALLOW_DESTRUCTIVE`), no falta de hardware. Regenerar la
> galería es además lo que destapó que el arreglo de `T12-06` no hacía nada.
>
> **Queda una tarea abierta, y está bloqueada a propósito**: `T10-02`, en el
> **[Tier 10](#-tier-10--lo-que-solo-aparece-al-publicar-abierto-2026-08-26)**. El tier se abrió el
> **2026-08-26** al cortar la v1.25.0, cuando la puerta de cobertura abortó el corte con un informe vacío
> cuya causa **sigue sin conocerse**. `T10-01` (2026-08-27) no la busca: se ocupa de que la próxima vez
> queden **pruebas** —informe conservado y diagnósticos del recolector— y de que el mensaje deje de culpar
> al paquete equivocado. `T10-02` es la causa, y espera a que vuelva a ocurrir, porque no se puede
> investigar lo que no se reproduce. Todo lo demás es registro. El
> **[Tier 9](#️-tier-9--re-auditoría-transversal-con-la-app-en-marcha-abierto-2026-08-26)** se abrió y se
> cerró el **2026-08-26**, **20/20**, por una re-auditoría de las 12 áreas aplicables ejecutada **sobre la
> máquina**. Los Tiers 1–8 estaban cerrados desde antes —los dos últimos,
> **[7](#-tier-7--consistencia-y-descubribilidad-de-la-ui)** (9/9) y
> **[8](#-tier-8--lo-que-solo-se-ve-usando-la-app)** (6/6), ese mismo día—. Lo que queda fuera está fuera
> a propósito, y su porqué está en *[Decisiones cerradas](#-decisiones-cerradas-no-reabrir)*.
>
> **Lo que enseñó el Tier 9, en una línea:** la app estaba mejor que lo que la rodea. De sus 20 tareas,
> **ninguna** era un fallo de las operaciones de disco; la única **Alta** (`T9-01`) estaba en el corte de
> versión, que podía publicar un instalador sin correspondencia con el commit etiquetado, y las dos más
> reveladoras (`T9-04`/`T9-05`) estaban en la **propia herramienta de auditoría**, que perdía en silencio
> 4 de sus 26 capturas —incluida la del diálogo destructivo—.

**Parte 1 — funcionalidad: TERMINADA (2026-07-13).** Tiers 1–9 completados; no hay características
pendientes. Lo que queda fuera está **deliberadamente** fuera — incluidas las dos decisiones que definen el
producto y **no se van a reabrir**: la app corre **siempre elevada** y su ventana es de **tamaño fijo**.

**Parte 2 — calidad: CERRADA (2026-08-16).** Una auditoría técnica transversal (código, seguridad,
rendimiento, accesibilidad, i18n, arquitectura, QA, documentación, DevOps) encontró **37 puntos de mejora**
—luego 40—, ninguno de ellos una característica nueva. **39 completadas y 2 descartadas**: `T2-10` (CI, se
implementó y se revirtió: las pruebas de este proyecto son locales) y `T4-03` (firmar, contradecía la
decisión `#13`). Ver **[Parte 2](#parte-2--backlog-de-remediación-auditoría-2026-08-13)**.

**Tier 5 — ocurrencias: CERRADO (2026-08-16).** «Funcionalidad terminada» no significa «sin huecos»: usar
lo entregado revela dónde una característica se queda a medio camino. El hueco era real: *FAT32 pequeña*
dejaba el resto del disco **sin asignar**, y recuperarlo obligaba a salir a una herramienta de Windows.
**4 completadas** (`T5-01`, `T5-02`, `T5-03`, `T5-05`) y **1 descartada** por decisión de producto
(`T5-04`, N particiones). Viven en el **[Tier 5](#-tier-5--ocurrencias-para-features-existentes)**, aparte
de la auditoría y aparte del historial cerrado de la Parte 1.

**Tier 6 — refinado de UX/UI: CERRADO (2026-08-17), 15/15.** Una revisión enfocada solo en interfaz sobre
las capturas del corte de la v1.22.0 encontró **10 hallazgos** —**3 defectos** en los que la interfaz
afirma algo que no es cierto (el diálogo de *Reinicializar* titulado «Confirmar formato», el campo de
confirmación mostrando la letra que hay que teclear, y una velocidad de rotación cuyo valor es «SSD») y
**7 refinamientos**— más una tarea para **completar la propia revisión**, que no pudo ejecutarse contra la
app por falta de terminal elevada. Al ejecutarla (`T6-11`, galería completa en ambos temas) aparecieron
**tres hallazgos más** que la primera ronda no podía ver, así que el tier cerró en **15**. Ver
**[Tier 6](#-tier-6--refinado-de-uxui)**.

**Tier 7 — consistencia y descubribilidad: CERRADO (2026-08-26), 9/9.** Con el Tier 6 cerrado, una
revisión sobre el **código** de la UI —no sobre capturas— buscó lo que una galería no enseña: qué pasa al
pulsar y qué se ofrece para negarse después. **6 hallazgos, ninguno un defecto de corrección**: borrar un
preset no confirmaba mientras vaciar el historial sí (`T7-01`), *Herramientas* ofrece operaciones que
después rechaza en un diálogo (`T7-02`), el campo más esotérico era el único sin ayuda (`T7-03`), la app
tiene un solo atajo de teclado (`T7-04`), la búsqueda del historial no dice cuánto oculta (`T7-05`) y
quedan dos preguntas que solo se contestan con la app en marcha (`T7-06`). `T7-06` —la revisión **con la app en marcha**— se hizo el mismo día y, como `T6-11` en su
tier, **abrió lo que la lectura no podía ver**: `T7-07` (las filas del historial y de presets se
anunciaban con el volcado del record, y los filtros no decían qué filtraban) y `T7-08`, **una
comprobación de ojo**: si WinUI pinta el tooltip de un ítem de menú deshabilitado. La respuesta fue
**no** —no existe el `ShowOnDisabled` de WPF—, así que el motivo bajó al texto visible del ítem y `T7-02`
quedó completa. Y mirar ese menú arreglado abrió a su vez `T7-09`: el marco de foco salía **recortado** en
los seis diálogos. De paso desmintió la sospecha de partida: los `ListView` con `SelectionMode="None"`
**sí** se recorren y desplazan solo con teclado. Ver **[Tier 7](#-tier-7--consistencia-y-descubribilidad-de-la-ui)**.

| Tier | Tema | Versión |
|---|---|---|
| **1** | Quick wins (persistencia, ETA, borrado seguro, historial) | 1.4.0 |
| **2** | Diagnóstico y gestión (S.M.A.R.T., chkdsk, protección de escritura, reinicializar, benchmark) | 1.5.0–1.7.0 |
| **3** | Presets, 5 idiomas, aviso al terminar | 1.8.0 |
| **4** | Refinado de lo existente | 1.10.0 / 1.11.0 |
| **5** | Confianza, transparencia legal y sostenibilidad | 1.12.0 |
| **6** | Pulido UX/UI | 1.13.0 |
| **7** | Partición FAT32 pequeña al reinicializar | 1.14.0 |
| **8** | **Seguridad y confianza** | 1.15.0 |
| **9** | **Infraestructura y calidad** | 1.15.1 |

---

# Parte 1 — Historial de producto (cerrado)

## ✅ Tier 1 — Quick wins *(v1.4.0)*

| # | Característica | Dónde |
|---|----------------|-------|
| 1 | **Persistencia de configuración** (idioma, tema, última unidad) | `Services/AppSettings` → `%AppData%\FormatDiskPro\settings.json` |
| 2 | **ETA + velocidad (MB/s)** en operaciones largas | `Core/Throughput` |
| 3 | **Borrado seguro con progreso real** (sobrescritor propio; sustituye `cipher /w`) | `Services/SecureWipe` |
| 4 | **Visor de historial integrado** | `Core/HistoryEntry` + `UI/HistoryDialog` |

## ✅ Tier 2 — Diagnóstico y gestión *(v1.5.0 – v1.7.0)*

Refuerzan el corazón del proyecto. Todo vía cmdlets de **Storage** (no `diskpart`), coherente con `DiskService`.

| # | Característica | Dónde | Versión |
|---|----------------|-------|---------|
| 5 | **S.M.A.R.T. ampliado**: temperatura, horas, desgaste, RPM, errores, en diálogo dedicado. *"No disponible"* en unidades sin contadores (típico USB) | `Core/SmartInfo`, `DiskService.GetSmartAsync`, `UI/HealthDialog` | 1.5.0 |
| 6 | **chkdsk**: *Solo comprobar* (solo lectura) o *Comprobar y reparar* (`/f`). La reparación queda **bloqueada en el disco de sistema**, para no programar un reinicio | `Services/CheckDisk` | 1.6.0 |
| 7 | **Protección de escritura**: la detecta y ofrece quitarla al pulsar Iniciar, evitando el fallo críptico | `DiskService.IsDiskReadOnlyAsync`/`ClearReadOnlyAsync` | 1.6.0 |
| 8 | **Reinicializar unidad** (USB con particiones raras o RAW): limpia el disco y recrea una única partición usable. **Solo extraíbles**, con guardas reforzadas (disco físico ≠ Windows + escribir la letra) | `Core/ReinitPlan`, `Services/ReinitDrive` | 1.7.0 |
| 9 | **Benchmark** no destructivo, perfil CrystalDiskMark: secuencial Q8 + 4 KiB aleatorio, **sin caché del sistema**, mediana de 3 pasadas | `Core/Benchmark`, `Services/BenchmarkRunner` | 1.7.0 · refinado en 1.9.0 |

## ✅ Tier 3 — Presets, idiomas y avisos *(v1.8.0)*

| # | Característica | Dónde |
|---|----------------|-------|
| 10 | **Presets personalizados** del usuario, persistidos | `Core/Presets`, `UI/PresetsDialog` |
| 11 | **5 idiomas** (ES/EN/PT/FR/IT), con test de completitud: cada clave tiene sus 5 traducciones | `Localization` |
| 12 | **Aviso al terminar** (sonido + parpadeo), solo si la ventana no está en primer plano | `Services/Notifier` |
| ~~13~~ | ~~Paquete winget + firma del instalador~~ | ❌ **Descartado** — ver *Decisiones cerradas* |

## ✅ Tier 4 — Refinado de lo existente *(v1.10.0 / v1.11.0)*

> No añade capacidades: **pule y profundiza las que ya existen**.

| # | Característica | Refina |
|---|----------------|--------|
| 14 | **Pasadas de borrado seguro** configurables (1/3/7). *NIST 800-88: 1 basta en discos modernos* | #3 |
| 15 | **IOPS** junto a MB/s en el 4 KiB aleatorio | #9 |
| 16 | **Umbrales de color** en S.M.A.R.T. (verde/ámbar/rojo) **+ texto de estado** (no solo color) y botón *Actualizar* | #5 |
| 17 | **Autorefresco de unidades** al conectar/desconectar (`WM_DEVICECHANGE`, con debounce) | la gestión base |
| 18 | **Idioma automático** en el primer arranque (luego manda la elección del usuario) | #11 |
| 19 | **Búsqueda, filtros y exportación CSV** del historial | #4 |
| 20 | **Editar y reordenar** presets | #10 |
| 21 | **Changelog** en el aviso de actualización, antes de descargar | updates |
| 22 | **Accesibilidad transversal**: nombres accesibles, aceleradores de menú, F5 | la capa UI |

*(v1.10.1: fix de adaptación a **DPI/escalado** — ventana por DPI + diálogos con `MaxWidth`.)*

## ✅ Tier 5 — Confianza, legal y sostenibilidad *(v1.12.0)*

Capa de **distribución/confianza**: no añade funciones de disco.

| # | Característica |
|---|----------------|
| 23 | Relicencia de MIT a **GNU GPL v3.0**, con el texto **embebido en el `.exe`** (*Ayuda → Licencia*) |
| 24 | **Disclaimer** de uso destructivo / sin garantía |
| 25 | **Avisos de terceros** (atribuciones) |
| 26 | **Aviso de privacidad**: sin telemetría; única conexión = GitHub Releases |
| 27 | **Donaciones voluntarias** (PayPal). **Ninguna función se bloquea ni es de pago** |

## ✅ Tier 6 — Pulido UX/UI *(v1.13.0)*

> Como el Tier 4, no añade capacidades: refina **presentación y feedback** con patrones Fluent estándar.

| # | Característica |
|---|----------------|
| 28 | Aviso de unidad protegida como **InfoBar** (antes competía con el estado transitorio del footer) |
| 29 | `ConfirmDialog`: **foco inicial + Enter** para confirmar. Mantiene la fricción deliberada de escribir la letra, sin obligar a soltar el teclado |
| 30 | **Barra de capacidad** usado/libre en la tarjeta Unidad |
| 31 | **Iconos por tipo de unidad** en el selector |
| 32 | **Estado vacío** del selector de unidades |
| 33 | **Salud coloreada** en la tarjeta principal (reusa los umbrales de #16) |
| 34 | **Validación inline** de la etiqueta (el modal se mantiene como respaldo) |
| 35 | **Progreso en la barra de tareas** (`ITaskbarList3`), visible con la app minimizada |
| 36 | **Estado de error** en la barra de progreso al fallar o cancelar |

*(Incluye el fix de ancho de `LegalTextDialog`/`ConfirmDialog`, que desbordaban la ventana.)*

## ✅ Tier 7 — Partición FAT32 pequeña al reinicializar *(v1.14.0)*

> Windows **nunca** permite un volumen FAT32 mayor de 32 GB (restricción de la plataforma, no del proyecto).
> Por eso el selector oculta FAT32 en discos ≥ 32 GB — lo que dejaba sin salida el caso real: **flashear el
> BIOS/UEFI de una placa base** desde un USB grande, cuya utilidad solo lee FAT32.

| # | Característica | Dónde |
|---|----------------|-------|
| 37 | Opción de crear **solo una partición FAT32 pequeña** (1–32 GB, elegible) y dejar el resto sin asignar. Visible solo en extraíbles ≥ 32 GB, y **solo** vía *Reinicializar unidad* (el flujo de *Iniciar* no cambia) | `Core/ReinitPlan`, `Services/ReinitDrive`, `UI/MainWindow` |

> **No es un gestor de particiones** (sigue fuera de alcance): una sola partición, el resto sin asignar.
>
> **Ahí está el hueco que abre el [Tier 5](#-tier-5--ocurrencias-para-features-existentes)** (2026-08-15):
> «el resto sin asignar» deja un pendrive grande con solo 32 GB usables hasta que el usuario abre *Crear y
> formatear particiones* de Windows — justo la herramienta que esta app existe para no tener que abrir.
>
> **Fix de plataforma, hallado con hardware real:** `Clear-Disk` **no siempre deja el disco en RAW**. Afectaba
> a *toda* Reinicializar unidad, no solo a esta opción.

## ✅ Tier 8 — Seguridad y confianza *(v1.15.0)*

> Nace de comparar el proyecto con su hermano **WingetUSoft**, que resolvió estos puntos antes: el port viene
> con sus tests y con los tropiezos ya conocidos.

| # | Característica | Dónde |
|---|----------------|-------|
| 38 | **Verificar el instalador antes de ejecutarlo elevado**: firma Authenticode → si no, **SHA-256** contra el asset `*.exe.sha256`. Sin ninguna de las dos, **se borra y no se ejecuta**. *(Antes se lanzaba con permisos de administrador sin comprobar nada.)* | `Services/UpdateService`, `build-installer.ps1`, `release.ps1` |
| 39 | **Neutralizar fórmulas** en la exportación CSV. El escape RFC 4180 protege la *estructura* del CSV, no al programa que lo abre | `Core/HistoryEntry.CsvField` |
| 40 | **Contraste WCAG AA** de los colores de severidad, **medido por tests** (8 casos, mínimo 4.5:1): un color mal elegido **rompe el build** | `Core/SeverityPalette` |
| 44 | **Build reproducible**: versión **exacta** del Windows App SDK + publicación a `%TEMP%` (MAX_PATH) | `.csproj`, `build-installer.ps1` |

> **#44 no estaba planeado: apareció porque el instalador ya no compilaba.** Un paquete más nuevo del SDK
> (referenciado como `1.8.*`, flotante) añadió un archivo cuyo nombre hace que la ruta del publish pase de
> **MAX_PATH**, e Inno Setup abortaba sin decir cuál era.

## ✅ Tier 9 — Infraestructura y calidad *(v1.15.1)*

| # | Característica | Dónde |
|---|----------------|-------|
| 41 | **UI tests en el pipeline de release** (`-UiTests`): un corte **no sale si la app real falla**. Los tests con precondición ausente ahora **se OMITEN en vez de fallar** — omitido dice *"no tengo el hardware"*, fallido dice *"la app está rota"* | `tests/…/TestDriveFacts.cs`, `release.ps1` |
| 42 | **Instalador probado end-to-end**: instalación limpia + actualización in-place con el flujo silencioso real (cierra la app, actualiza y la relanza) | — |
| 45 | **La codificación del `.csproj` se corrompía en CADA release** (una capa de mojibake por versión, durante 14). El `.exe` publicado mostraba el nombre del autor destrozado en sus propiedades | `release.ps1` |

> **#45 salió inspeccionando el binario instalado**, no revisando código. Es el tipo de fallo que solo aparece
> **ejecutando** las cosas.

---

## 🚫 Deliberadamente fuera de alcance

Adoptar cualquiera de estos sería **cambiar el alcance del producto**:

- **Creador de USB booteable desde ISO** (territorio Rufus).
- **Gestor de particiones completo** (redimensionar / fusionar / mover).
- **Clonado / imagen / backup de discos.**

## 🚫 Decisiones cerradas (no reabrir)

- **La app corre SIEMPRE elevada (`requireAdministrator`) — firme (2026-07-13).** Se evaluó el modelo
  `asInvoker` + worker elevado por named pipe (el de WingetUSoft) y **se descartó**: FormatDiskPro formatea,
  borra y reinicializa discos, así que **casi todo lo que hace necesita administrador**. El "menor privilegio"
  sería nominal —pediría UAC igual, solo más tarde y más veces— a cambio de refactorizar **todos** los
  `Services`, que asumen proceso elevado. Pedirlo de entrada es coherente con lo que la herramienta es, y el
  manifiesto lo declara en vez de escalar por sorpresa. Consecuencia asumida: los UI tests y
  `tools/capture-screenshots.ps1` exigen terminal elevada, y ambos lo validan con un mensaje claro.
- **La ventana es de tamaño fijo (500×900) — firme (2026-07-13).** Es un **diálogo de tarea**, no un espacio de
  trabajo: no hay contenido que gane con más ancho (ni tablas, ni listas largas) y el layout de tarjetas ya
  cabe entero. Portar `WindowSizing`/`ContentScroller` de WingetUSoft resolvería un problema que **aquí no
  existe**.
- **#13 paquete winget + firma del instalador — descartado (2026-06-24).** GitHub Releases con
  auto-actualización integrada es la distribución del proyecto, y **no se firmará el instalador**: SmartScreen
  seguirá mostrando "editor desconocido". La firma sigue disponible como **opción** del pipeline, no como
  objetivo. El **#38 no lo contradice**: verifica el hash, no exige firmar — de hecho es *más* necesario
  precisamente **porque** no hay firma.
- **`T4-03` «firmar el instalador» — descartada (2026-08-16), y con ella se cierra la auditoría.** Es la
  misma decisión que `#13`, que la auditoría reabrió sin querer al listarla como tarea pendiente. Tenerla
  en el backlog **afirmaba algo falso**: que el proyecto debía firmar y aún no lo había hecho. Lo cierto es
  que decidió no firmar, y **construyó la verificación por SHA-256 precisamente por eso**.
  - **No queda trabajo escondido detrás.** El pipeline ya admite firmar (`-CertThumbprint` / `-CertFile` /
    `-CertPassword` / `-TimestampUrl` en `build-installer.ps1` y `release.ps1`), el `.sha256` se genera
    **después** de firmar porque firmar cambia el binario, y la ruta Authenticode quedó endurecida en
    `T1-08` (`WTD_REVOKE_WHOLECHAIN` + `WTD_CACHE_ONLY_URL_RETRIEVAL`). Falta un certificado, que es una
    **compra**, no ingeniería — y por eso no pertenece a un backlog técnico.
  - **El día que haya certificado, el trabajo no es «firmar»:** es poner `UpdateService.SignsItsInstallers`
    en `true` **y** fijar el publicador esperado. Lo primero sin lo segundo reabre el agujero que `T1-08`
    cerró. Esa condición **ya la vigila un test tripwire** que falla si se hace a medias, así que está
    mejor custodiada por el build que por una casilla sin marcar.
- **CI con GitHub Actions — descartado (2026-07-12).** Un runner hospedado **no puede** ejecutar los UI tests
  (necesitan sesión elevada y la USB física de pruebas), así que solo duplicaría los unitarios que
  `release.ps1` ya corre antes de cada corte, con menos cobertura. Misma decisión que en WingetUSoft.
  **Reafirmado y ampliado el 2026-08-15: NO habrá CI de ningún tipo, tampoco de solo unitarias.** La
  auditoría lo propuso (`T2-10`), se llegó a implementar y se revirtió: **el testing de este proyecto es
  local**. La prueba que vale aquí es la que ejerce la app real, y esa no cabe en un runner; un ✅ verde
  que solo cubre los unitarios afirma más de lo que prueba. La puerta de calidad es
  `release.ps1 -UiTests` desde terminal elevada.

---

# Parte 2 — Backlog de remediación (auditoría 2026-08-13)

> **Qué es esto.** Una revisión técnica transversal del repositorio completo (12 áreas) traducida a tareas
> ejecutables. **Ninguna añade funcionalidad**: todas corrigen, endurecen o miden lo que ya existe. El
> informe que las originó no se repite aquí — cada tarea es autocontenida.
>
> **Excepciones, y están marcadas como tales:** los **Tiers 5 y 6** del final no vienen de la auditoría.
> El **5** además añade funcionalidad; el **6** no —es refinado de interfaz— pero nace de una revisión
> posterior, no de aquel informe. Van aquí por continuidad de numeración, no porque formen parte de las 40.
>
> **Base de la revisión:** v1.15.2 · build 0 advertencias/0 errores · **289/289** unitarias verificadas en
> ejecución (224 ms) el 2026-08-13.

## Índice

| Tier | Tema | Tareas | Esfuerzo agregado |
|---|---|---:|---|
| **T0** | Crítico / bloqueante — la app puede morir en mitad de una operación | 2 | bajo |
| **T1** | Alta prioridad — guardas destructivas, barreras a11y, i18n rota, seguridad | 9 | bajo-medio |
| **T2** | Mejoras sustanciales — a11y, exactitud de medición, cobertura, CI, arquitectura | 13 | medio-alto |
| **T3** | Pulido — errores silenciosos, docs contradictorias, consistencia | 11 | bajo |
| **T4** | Futuro / opcional — fuera del alcance inmediato | 5 | — |
| **T5** | **Ocurrencias para features existentes** — ampliaciones nacidas de usar lo ya entregado | 5 | medio-alto |
| **T6** | **Refinado de UX/UI** — 3 defectos + 11 refinamientos + 1 de cobertura · cerrado | 15 | bajo-medio |
| **T7** | **Consistencia y descubribilidad de la UI** — revisión sobre el código, no sobre capturas · cerrado | 9 | bajo-medio |
| **T8** | **Lo que solo se ve usando la app** — incluye *Exportar CSV*, roto en toda versión publicada · cerrado | 6 | bajo-medio |
| **T9** | **Re-auditoría transversal con la app en marcha** — 1 Alta · 9 Medias · 10 Bajas · **20/20, cerrado** | 20 | bajo-medio |
| **T10** | **Lo que solo aparece al publicar** — nacido del corte de la v1.25.0 · `T10-01` hecha · `T10-02` **abierta, a la espera de que vuelva a ocurrir** | 2 | bajo |
| **T11** | **Rendimiento y jerarquía de la ventana principal** — el pie deja de ser solo una barra, las tres herramientas de solo lectura salen del menú y la tarjeta de unidad se ordena por importancia · **4/4, cerrado** | 4 | bajo |
| **T12** | **Lo que la ventana no dice** — un contraste por debajo de AA que el barrido no veía, el botón que no nombraba lo que destruye, las opciones bajo el pliegue, los presets lejos de lo que configuran, una barra de progreso en la que el éxito y el fallo eran el mismo color, y la retirada de la franja de rendimiento · **7/7, cerrado** | 7 | bajo |
| | **Total** | **108** | |

> **Esta tabla se quedó atrás dos tiers** (marcaba el T6 como «lo único abierto» y sumaba 60) hasta la
> re-auditoría del 2026-08-26. Al añadir un tier hay que tocarla: es el único sitio donde se ve el
> conjunto de un vistazo.

**Orden recomendado:** T0 → T1-01/02 (guardas destructivas) → T1-03/04 (a11y medible) → T1-05/06/07
(i18n) → T1-08/09 (updater) → T2 → T3.

**El T5 no forma parte de la auditoría** (esa cerró en 40 tareas, y el recuento de progreso sigue siendo
sobre esas 40): comparte numeración porque nace de lo mismo —mirar lo entregado y ver dónde se queda
corto—, pero **añade funcionalidad**, cosa que ninguna tarea `T0`–`T4` hace. Su orden interno es
**T5-01 → T5-02 → T5-03 → T5-05**, y `T5-04` solo si el uso lo pide.

---

## 🔴 Tier 0 — Crítico / bloqueante

> Un fallo de E/S previsible (USB que se desconecta, unidad falsificada que deja de responder, `chkdsk.exe`
> bloqueado por política) **termina el proceso sin aviso ni registro**. En Release no hay ninguna red de
> seguridad: el único `UnhandledException` del proyecto lo genera WinUI bajo `#if DEBUG`.

- [x] **[T0-01] Red de seguridad global para excepciones no controladas**
  - **Área:** Código / robustez
  - **Ubicación:** `src/FormatDiskPro/App.xaml.cs:9`
  - **Qué hacer:** suscribir `UnhandledException` en `App.OnLaunched`; registrar la excepción con
    `History.Log($"CRASH: {e.Exception}")`, marcar `e.Handled = true` y mostrar un diálogo de error en vez
    de morir. Es la red, no el arreglo (ese es `T0-02`).
  - **Criterio de aceptación:** forzar una excepción en un handler `async void` (p. ej. con un punto de
    interrupción condicional o una unidad extraída a mitad de *Verificar capacidad*) deja la app **viva**,
    con una entrada `CRASH:` en `history.log`.
  - **Verificado sobre hardware el 2026-08-14.** Al desactivar el `catch` de `T0-02` y desmontar la USB a
    mitad de *Verificar capacidad*, el historial recibe exactamente
    `2026-08-14 12:03:31⇥CRASH: System.IO.IOException…` y la app sigue viva. La red global funciona.
  - **Esfuerzo:** bajo
  - **Depende de:** ninguna

- [x] **[T0-02] Capturar excepciones en los handlers `async void` de operación**
  - **Área:** Código / robustez
  - **Ubicación:** `src/FormatDiskPro/UI/MainWindow.xaml.cs:1039` (`MnuVerify_Click`), `:1179`
    (`MnuCheck_Click`), `:1277` (`MnuReinit_Click`), `:1378` (`MnuBenchmark_Click`)
  - **Qué hacer:** los cuatro usan `try { … } finally { EndOperation(); }` **sin `catch`**. Añadir un
    `catch (Exception ex)` que replique el patrón ya correcto de `RunFormatAsync`
    (`MainWindow.xaml.cs:937`): `_lastOperationFailed = true`, `History.Log(...)`, `ShowInfoAsync(...)`.
    `CapacityVerifier.RunAsync` solo atrapa `OperationCanceledException`
    (`CapacityVerifier.cs:109`) y `CheckDisk.RunAsync` no atrapa nada, así que un `IOException` o un
    `Win32Exception` llega hasta el `async void`.
  - **Criterio de aceptación:** con la unidad de pruebas desconectada a mitad de cada una de las cuatro
    operaciones, la app muestra el error, escribe la línea de historial y vuelve a estado ocioso.
  - **Verificado sobre hardware el 2026-08-14 para *Verificar capacidad* y *Benchmark*** (`T2-13`), las dos
    que escriben en bucle. `CHKDSK` no entra por aquí —sale con código de error y se interpreta como
    `Failed`, sin excepción— y `REINIT` no se probó por ser destructivo.
  - **Esfuerzo:** bajo
  - **Depende de:** ninguna (complementaria de `T0-01`, no sustituible por ella)

---

## 🟠 Tier 1 — Alta prioridad

- [x] **[T1-01] `IsSystemDrive` debe comparar con cultura invariante**
  - **Área:** Código / seguridad de datos
  - **Ubicación:** `src/FormatDiskPro/UI/MainWindow.xaml.cs:2026-2030`
  - **Qué hacer:** `char.ToUpper(letter) == char.ToUpper(sys)` es sensible a la cultura. Con cultura turca
    (`tr-TR`), `char.ToUpper('i')` da `'İ'` y no `'I'`: la comparación fallaría y la guarda del disco de
    sistema **dejaría de proteger**. Usar `char.ToUpperInvariant` en ambos lados — como ya hace
    `ParseDriveLetter` (`:1783`) y `CheckDisk` (`CheckDisk.cs:50`).
  - **Criterio de aceptación:** test unitario que fija `CultureInfo.CurrentCulture = new("tr-TR")` y
    comprueba que la letra `I` se sigue detectando como disco de sistema.
  - **Esfuerzo:** bajo
  - **Depende de:** ninguna

- [x] **[T1-02] `format.com`: pasar `/Y` en vez de teclear "Y"/"S" por stdin**
  - **Área:** Código / i18n
  - **Ubicación:** `src/FormatDiskPro/UI/MainWindow.xaml.cs:1002-1004`,
    `src/FormatDiskPro/Core/FormatLogic.cs:58-63`
  - **Qué hacer:** el formato completo de NTFS/FAT32/FAT usa `format.com` y responde su confirmación
    escribiendo `"Y"` y `"S"` en la entrada estándar — las respuestas de un Windows **inglés y español**.
    En un Windows francés (`O`) o alemán (`J`) no coinciden y el proceso se queda esperando entrada, con
    `StandardInput` nunca cerrado. Añadir `/Y` a `BuildComArgumentList` y eliminar la escritura por stdin.
  - **Criterio de aceptación:** `BuildComArgumentList` incluye `/Y`; un formato completo termina sin
    escribir nada en stdin. Verificar en un Windows no ES/EN, o con `chcp`/idioma de sistema cambiado.
  - **Esfuerzo:** bajo
  - **Depende de:** ninguna
  - **Resuelta y REPRODUCIDA el 2026-08-14.** Ya no estaba «pendiente de verificación»: se montó un VHD de
    400 MB y se lanzó `format.com` **sin escribir nada** por la entrada estándar, que es lo que le ocurre a
    un Windows cuyo idioma no responde ni a `Y` ni a `S`:

    | | Resultado |
    |---|---|
    | **Con `/Y`** | Termina en **0.1 s**, exit 0, «Formato completado» |
    | **Sin `/Y`** | **Se cuelga** esperando una tecla que nunca llega |

    El `/Y` resuelve además el segundo prompt que esta nota dejaba abierto —el de la etiqueta de volumen—
    porque asume etiqueta vacía cuando no se pasa `/V:`. Ya no se escribe nada por stdin: se **cierra**,
    para que una build hipotética sin `/Y` falle en vez de colgarse indefinidamente.
  - **Segundo fallo de idioma en la misma ruta, que esta tarea NO mencionaba.** `ExtractPercent` reconocía
    `%`, `percent` y `por ciento` — el mismo par inglés/español que la respuesta por stdin. Como
    `format.com` escribe la **palabra** y no el símbolo, en un Windows francés o italiano la barra de
    progreso se quedaba clavada en 0 durante todo un formato completo sin que nada fallara. Añadidos
    `por cento` (pt), `per cento` (it), `pour cent` (fr) y `Prozent` (de).
  - **Residuo honesto:** las cuatro palabras nuevas son **traducciones, no observaciones** — no se ha visto
    la salida real de un `format.com` no español. No se pudo: un VHD montado se anuncia como
    aprovisionamiento fino y Windows rechaza el formato **completo** sobre él, que es el único que imprime
    porcentajes. El riesgo está acotado: si alguna no coincide, se degrada a barra parada con el formato
    correcto — nunca a un fallo. Hay test de esa degradación.
  - **Ojo al matiz:** el idioma que manda aquí es el de **Windows**, no el de la app. Se puede tener
    FormatDiskPro en español sobre un Windows alemán, así que esta lista es incompleta por naturaleza.

- [x] **[T1-03] Contraste WCAG AA en el historial: estado «Cancelado» (3.52:1)**
  - **Área:** Accesibilidad
  - **Ubicación:** `src/FormatDiskPro/UI/HistoryDialog.xaml.cs:164`,
    `src/FormatDiskPro/UI/HistoryDialog.xaml:34-35`
  - **Qué hacer:** el título de las filas canceladas usa `#868686` sobre el fondo de tarjeta claro
    `#FBFBFB` = **3.52:1**, por debajo del 4.5:1 que exige WCAG AA para texto normal (13 px SemiBold no
    califica como texto grande). El tema oscuro sí pasa (5.09:1). Oscurecer el gris claro hasta ≥ 4.5:1
    (p. ej. `#6E6E6E` ≈ 4.9:1).
  - **Criterio de aceptación:** el barrido de contraste de `T1-04` cubre este color y pasa en ambos temas.
  - **Esfuerzo:** bajo
  - **Depende de:** ninguna (pero se cierra de forma natural junto a `T1-04`)

- [x] **[T1-04] Unificar los colores de severidad en `SeverityPalette` y medirlos todos**
  - **Área:** Arquitectura / accesibilidad
  - **Ubicación:** `src/FormatDiskPro/UI/HistoryDialog.xaml.cs:160-166`,
    `src/FormatDiskPro/UI/MainWindow.xaml.cs:426-433`
  - **Qué hacer:** los RGB verde/ámbar/rojo están **duplicados a mano** en tres sitios
    (`SeverityPalette`, `HistoryDialog.ColorFor`, `MainWindow.ProtectedColor`/`DriveBrush`). Solo el
    primero lo mide `SeverityPaletteTests`, así que las copias **escapan del test que existe justamente
    para esto** (así se coló `T1-03`). Mover todos los valores a `Core/SeverityPalette` y extender
    `SeverityPaletteTests.EverySeverityColor_MeetsWcagAaContrast_AgainstItsCardBackground` para recorrerlos.
  - **Criterio de aceptación:** no queda ningún `Color.FromArgb` de severidad fuera de `Core/`; el test
    de contraste enumera todos los colores semánticos de la app y falla si se añade uno que no llega a AA.
  - **Esfuerzo:** medio
  - **Depende de:** ninguna

- [x] **[T1-05] Localizar las descripciones de sistema de archivos a PT/FR/IT**
  - **Área:** i18n
  - **Ubicación:** `src/FormatDiskPro/UI/MainWindow.xaml.cs:32-48` y `:555`
  - **Qué hacer:** `FsDescEs`/`FsDescEn` son los dos únicos idiomas; `UpdateFsDescription` hace
    `L.Current == AppLang.Es ? FsDescEs : FsDescEn`, así que **portugués, francés e italiano ven texto en
    inglés**. Mover las 5 descripciones a `Localization.Map` como claves `fs.desc.ntfs`, `fs.desc.exfat`,
    `fs.desc.refs`, `fs.desc.fat32`, `fs.desc.fat` con sus 5 traducciones, y borrar los dos diccionarios.
  - **Criterio de aceptación:** con la app en PT, FR o IT, la descripción bajo el selector está en ese
    idioma; `EveryEntry_HasFiveNonEmptyTranslations` cubre las nuevas claves.
  - **Esfuerzo:** bajo
  - **Depende de:** ninguna

- [x] **[T1-06] Localizar los nombres de los presets integrados**
  - **Área:** i18n
  - **Ubicación:** `src/FormatDiskPro/Core/Presets.cs:16-23`
  - **Qué hacer:** los cinco presets integrados llevan el nombre **en español fijo** («USB universal
    (Windows / macOS / Linux)», «Consola / TV / Cámara», …) y `BuildPresetsMenu`
    (`MainWindow.xaml.cs:658`) los pinta tal cual en los 5 idiomas. Guardar una **clave** de localización
    en `FormatPreset` (o un campo paralelo) y resolverla con `L.T` al construir el menú. Ojo: el nombre se
    usa también como identidad frente a los presets del usuario (`Presets.IsNameAvailable`), así que la
    comparación de duplicados debe seguir haciéndose contra el nombre **mostrado** en el idioma activo.
  - **Criterio de aceptación:** con la app en EN/PT/FR/IT los presets integrados aparecen traducidos, y
    `PresetsTests` sigue en verde (incluida la detección de nombres duplicados).
  - **Esfuerzo:** medio
  - **Depende de:** ninguna

- [x] **[T1-07] Test que impida que vuelva a haber texto de UI fuera de `Localization`**
  - **Área:** QA / i18n
  - **Ubicación:** `tests/FormatDiskPro.Tests/LocalizationTests.cs:24-32`
  - **Qué hacer:** `EveryEntry_HasFiveNonEmptyTranslations` solo recorre `L.Map`, así que daba **luz verde
    mientras `T1-05` y `T1-06` estaban rotos** — falsa confianza. Añadir un test que falle si aparecen
    tablas de cadenas de cara al usuario fuera de `Localization` (p. ej. reflexión sobre los campos
    `static readonly Dictionary<string,string>` de la capa `UI`, o comprobación de que los presets
    integrados devuelven una clave conocida de `L.Map`).
  - **Criterio de aceptación:** revertir `T1-05` o `T1-06` hace fallar la suite.
  - **Esfuerzo:** medio
  - **Depende de:** T1-05, T1-06

- [x] **[T1-08] Comprobar revocación al validar la firma Authenticode**
  - **Área:** Seguridad
  - **Ubicación:** `src/FormatDiskPro/Services/UpdateService.cs:279`
  - **Qué hacer:** `fdwRevocationChecks = WTD_REVOKE_NONE` acepta como válida una firma cuyo certificado
    haya sido **revocado**. Es la vía *preferente* de verificación (`VerifyInstallerAsync` devuelve sin
    mirar el hash si la firma pasa), así que conviene que sea la fuerte. Cambiar a
    `WTD_REVOKE_WHOLECHAIN` (`0x00000001`) con `WTD_CACHE_ONLY_URL_RETRIEVAL` para no depender de la red.
  - **Criterio de aceptación:** un instalador firmado con certificado revocado es rechazado y cae al
    camino del SHA-256. Sigue funcionando sin conexión.
  - **Esfuerzo:** bajo
  - **Depende de:** ninguna
  - **Resuelta el 2026-08-13, y NO como se planteó.** Al reafirmarse la decisión de no firmar (`#13`), la
    nota de abajo resultó estar equivocada en lo importante: `VerifyAuthenticodeSignature` comprueba la
    **validez** de una firma, no su **autoría**, y `VerifyInstallerAsync` devolvía **sin mirar el hash** si
    la firma pasaba. Como el proyecto no firma, esa rama solo podía activarse sobre un binario **que no
    produjimos nosotros**: era un modo de saltarse el SHA-256 con cualquier ejecutable firmado por
    cualquier CA de confianza, con `LaunchInstaller` ejecutando **como administrador** al otro lado. El
    hash pasa a ser obligatorio y el atajo queda tras `UpdateService.SignsItsInstallers` (`false`).
    Se aplicó **también** el contenido original (`WTD_REVOKE_WHOLECHAIN` + `WTD_CACHE_ONLY_URL_RETRIEVAL`),
    que sigue siendo lo correcto para el día que haya certificado.
  - **Al firmar algún día:** poner el flag en `true` **y** fijar el publicador esperado. Lo primero sin lo
    segundo reabre el agujero; hay un test tripwire que falla si se hace a medias.
  - **Nota original (conservada porque explica cómo se coló):** *hoy el proyecto no firma (decisión `#13`),
    así que esta rama solo se ejercita si algún día se firma. Es endurecimiento preventivo, no un agujero
    explotable hoy.* — La primera frase era cierta; la conclusión, no.

- [x] **[T1-09] No confiar en el nombre de asset de GitHub para construir la ruta de descarga**
  - **Área:** Seguridad
  - **Ubicación:** `src/FormatDiskPro/Services/UpdateService.cs:150-163`
  - **Qué hacer:** `PrepareDownloadPath` hace `Path.Combine(dir, release.AssetName)` con el nombre tal cual
    viene del JSON de GitHub. `Path.Combine` **descarta el primer argumento si el segundo es una ruta
    absoluta**, y no filtra `..`. Envolver con `Path.GetFileName(...)` y rechazar el resultado si queda
    vacío o si `Path.GetFullPath` se sale de `dir`.
  - **Criterio de aceptación:** test que pasa un `AssetName` de `C:\Windows\System32\x.exe` y otro de
    `..\..\x.exe` y comprueba que el archivo aterriza dentro de `%TEMP%\FormatDiskPro_update`.
  - **Esfuerzo:** bajo
  - **Depende de:** ninguna
  - **Nota:** **alcance honesto** — explotarlo exige controlar el release del repo, y quien pueda hacer eso
    ya controla el `.exe`. Pero la app corre **elevada**, así que el peor caso es escritura arbitraria como
    administrador: el coste del arreglo (una línea) no justifica dejarlo.

---

## 🟡 Tier 2 — Mejoras sustanciales

- [x] **[T2-01] Anunciar estado y progreso a los lectores de pantalla**
  - **Área:** Accesibilidad
  - **Ubicación:** `src/FormatDiskPro/UI/MainWindow.xaml:208-212`
  - **Qué hacer:** `StatusText` y `FormatProgress` cambian durante operaciones de minutos u horas sin
    ninguna notificación UIA: un usuario de lector de pantalla no sabe si el formateo avanza, falló o
    terminó. Marcar `StatusText` como región activa
    (`AutomationProperties.LiveSetting="Polite"`) y emitir un `RaiseNotificationEvent` en los hitos
    (inicio, fin, error, cancelación) en vez de en cada tick.
  - **Criterio de aceptación:** con Narrador activo, iniciar y terminar una operación se anuncia sin
    mover el foco.
  - **Esfuerzo:** medio
  - **Depende de:** ninguna
  - **Resuelta el 2026-08-15.** `StatusText` es región activa `Polite`, y `MainWindow.AnnounceStatus`
    emite un `RaiseNotificationEvent` en los **hitos**: inicio de las cinco operaciones y —en un solo
    sitio, `EndOperation`— fin, error o cancelación, con `ActionCompleted`/`ActionAborted` según cómo
    haya acabado.
  - **`Polite` y solo en los hitos, a propósito.** Una notificación por cada tick de porcentaje
    convertiría el lector de pantalla en ruido continuo durante una hora de formateo, que es *peor* que
    el silencio de partida. El avance queda en la región activa, que el usuario consulta cuando quiere;
    lo que se anuncia es lo que no puede perderse.
  - *Verificar capacidad* no fija estado inicial (lo pone el primer tick de progreso, que puede tardar):
    se anuncia el inicio **sin tocar** `StatusText`, para no pintar un texto que se sobrescribe enseguida.
  - **Medido sobre la app real**, no razonado: `AccessibilityTests.StatusText_IsAPoliteLiveRegion` lee el
    `LiveSetting` por UI Automation. Verificado por reversión: quitando el atributo del XAML **y
    recompilando**, la prueba falla. *(Sin recompilar no falla — los UI tests lanzan el `.exe` de `bin`,
    no el XAML del repo. Casi cuela como «verificado».)*

- [x] **[T2-02] Asociar el error de etiqueta con su campo**
  - **Área:** Accesibilidad
  - **Ubicación:** `src/FormatDiskPro/UI/MainWindow.xaml:143-148`
  - **Qué hacer:** `LabelErrorText` aparece bajo `VolumeLabelBox` sin relación programática: un lector de
    pantalla en el campo no lee el motivo del error. Añadir
    `AutomationProperties.DescribedBy` apuntando a `LabelErrorText` y marcarlo `LiveSetting="Assertive"`.
  - **Criterio de aceptación:** escribir una etiqueta inválida hace que Narrador lea el mensaje sin salir
    del campo.
  - **Esfuerzo:** bajo
  - **Depende de:** ninguna
  - **Resuelta el 2026-08-15.** `LabelErrorText` es región activa `Assertive` (bloquea la acción, no es
    información de fondo) y `VolumeLabelBox` lo referencia con `DescribedBy`. El vínculo se hace en
    code-behind: en WinUI `AutomationProperties.DescribedBy` es una **colección** y no admite
    `x:Reference` desde XAML. El evento `LiveRegionChanged` se emite solo cuando el mensaje **aparece o
    cambia**: se escribe letra a letra, y repetirlo en cada pulsación sería insoportable.
  - **Lo que enseñó la prueba al fallar:** un elemento `Collapsed` **no existe en el árbol de UI
    Automation**. Buscar `LabelErrorText` con la etiqueta válida no devuelve nada — hay que provocar el
    error primero. Que el vínculo solo exista mientras el mensaje se muestra es lo correcto, pero una
    prueba escrita sin saberlo habría fallado sin que nada estuviera mal.
  - **Medido sobre la app real:** `AccessibilityTests.InvalidLabel_ShowsAnAssertiveErrorLinkedToTheField`
    escribe una etiqueta inválida y comprueba por UIA el `LiveSetting` y que `DescribedBy` apunta de
    verdad al mensaje.

- [x] **[T2-03] Releer sin caché en la verificación de capacidad**
  - **Área:** Rendimiento / corrección funcional
  - **Ubicación:** `src/FormatDiskPro/Services/CapacityVerifier.cs:48-49` y `:78-79`
  - **Qué hacer:** la escritura usa `FileOptions.WriteThrough` pero la **relectura** usa E/S normal
    (`SequentialScan`), así que la caché de archivos de Windows puede servir los bloques desde RAM en vez
    del medio — justo lo que la prueba quiere descartar. En una USB falsa **pequeña** (menor que la RAM
    libre) el patrón podría releerse íntegro desde caché y dar un **falso OK**. Abrir el handle de lectura
    con `FILE_FLAG_NO_BUFFERING` (`(FileOptions)0x20000000`) y buffers alineados al sector, como ya hace
    `BenchmarkRunner` (`BenchmarkRunner.cs:36-37`, `AllocAligned`).
  - **Criterio de aceptación:** verificar una USB pequeña conocida-buena sigue dando OK; el contador de
    lecturas físicas (perfmon / Resource Monitor) se aproxima al volumen escrito.
  - **Esfuerzo:** medio
  - **Depende de:** ninguna
  - **Resuelta el 2026-08-15.** La relectura usa `FILE_FLAG_NO_BUFFERING` + `RandomAccess.ReadAsync` con
    buffer alineado (mismo patrón que `BenchmarkRunner.AllocAligned`). El objetivo se **redondea a la
    baja** al sector: así todos los tamaños de archivo —y por tanto todos los bloques, incluido el último
    de cada uno— quedan alineados, que es lo que exige la E/S sin caché. Se sacrifican <4 KB del margen de
    seguridad de 64 MB.
  - **Ya no es «razonado sobre el código»: el flag está demostrado activo.** Sustituyendo el buffer
    alineado por uno desplazado un byte, las pruebas fallan con `IOException: El parámetro no es
    correcto` — el error que Windows devuelve a la E/S **sin caché** ante un buffer desalineado. Con la
    caché de por medio, esa desalineación sería irrelevante y las pruebas pasarían: es precisamente lo que
    distingue una ruta de la otra.
  - **Probado también sobre la USB real** (`CapacityVerifierDriveTests`, 64 MB en `D:`, 7 s): un disco
    fijo y un medio extraíble no tienen por qué anunciar la misma geometría, y este modo no degrada
    —falla— si no cuadra. La prueba se **omite** salvo que se defina `FORMATDISKPRO_VERIFY_DRIVE=<letra>`.
  - **Lo que sigue sin poder medirse:** que un bloque concreto venga del medio y no de la RAM. No hay API
    que lo afirme; lo que hay es el flag, y ahora está probado que se aplica.

- [x] **[T2-04] Medir la cobertura de pruebas, no solo contarlas**
  - **Área:** QA
  - **Ubicación:** `tests/FormatDiskPro.Tests/FormatDiskPro.Tests.csproj:11-15`
  - **Qué hacer:** «289 pruebas» es un recuento, no una cobertura: hoy nadie sabe qué porcentaje de `Core`
    y de los helpers de `Services` se ejercita. Añadir `coverlet.collector`, generar el informe en
    `release.ps1` y fijar un umbral mínimo para `Core/`.
  - **Criterio de aceptación:** `dotnet test --collect:"XPlat Code Coverage"` produce informe; el corte
    falla si `Core/` baja del umbral acordado.
  - **Esfuerzo:** medio
  - **Depende de:** ninguna
  - **Resuelta el 2026-08-15.** `coverlet.collector` en el proyecto de pruebas (solo actúa si se pide el
    recolector: un `dotnet test` normal no cambia) y `release.ps1` mide en la misma pasada, imprime el
    dato y **aborta el corte** por debajo del umbral. Medida por primera vez: **97.1 % de línea en
    `Core/`** (367/378).
  - **Umbral en 90 %, por debajo de lo medido y a propósito.** Un listón pegado al valor actual obliga a
    escribir pruebas de relleno para que el corte no rompa por un método nuevo; lo que se quiere es un
    suelo que avise de una **regresión real**. Subirlo debe ser deliberado; que baje, un síntoma.
  - **Se mide SOLO `Core/`**, que es la capa que puede probarse entera sin hardware: ahí un hueco es una
    decisión, no una limitación. Medir `Services/` y `UI/` con la misma vara premiaría escribir pruebas
    fáciles de lo que no importa; su red son los UI tests.
  - **Verificado que la puerta cierra:** subiendo el umbral a 99 el corte aborta y lista las cinco clases
    con menos cobertura, para que el mensaje diga qué hacer y no solo que algo va mal.

- [x] **[T2-05] Pruebas de los caminos de error de las operaciones**
  - **Área:** QA
  - **Ubicación:** `tests/FormatDiskPro.Tests/`
  - **Qué hacer:** ninguna prueba cubre qué pasa cuando una operación **falla** (es la causa raíz de
    `T0-02`). Los `Services` son `static`, así que no se pueden sustituir; extraer al menos las rutas de
    error a helpers puros y probarlas, o introducir una interfaz mínima para inyectar el fallo.
  - **Criterio de aceptación:** existen pruebas que ejercitan el fallo de verificación, chkdsk y benchmark
    sin hardware.
  - **Esfuerzo:** alto
  - **Depende de:** T0-02
  - **Resuelta el 2026-08-13.** Se extrajo una costura interna en `CapacityVerifier.RunInAsync(dir, target,
    …, afterWriteAsync)` que permite **corromper lo escrito entre la fase de escritura y la de lectura** —
    es decir, reproducir una unidad falsificada sin tener una—. Antes, lo único que probaba la detección
    era un test de UI de 57 minutos sobre una USB **auténtica**, que por definición nunca la dispara.
    También se extrajo `Core/OperationFailure.LogLine` desde `MainWindow.ReportOperationErrorAsync`.
    **Encontró un defecto real:** ver `T3-11`.

- [x] **[T2-06] Emparejar el `.sha256` con el instalador que verifica**
  - **Área:** Seguridad
  - **Ubicación:** `src/FormatDiskPro/Services/UpdateService.cs:83-104`
  - **Qué hacer:** `ParseRelease` se queda con el **último** asset que termina en `.sha256`, sin comprobar
    que corresponda al `.exe` elegido. Con más de un asset el emparejamiento es arbitrario. Elegir el
    checksum cuyo nombre sea `<nombre-del-exe>.sha256`.
  - **Criterio de aceptación:** test con un release de varios assets que comprueba el emparejamiento.
  - **Esfuerzo:** bajo
  - **Depende de:** ninguna
  - **Resuelta el 2026-08-15.** El emparejamiento es ahora por nombre exacto (sin distinguir mayúsculas), y
    si no aparece el hash **del instalador elegido**, `ChecksumUrl` queda vacía y la actualización se
    rechaza por no verificable — el fallo seguro. `ParseRelease` pasa a `internal` para poder probarla con
    JSON real. **Verificado por reversión:** con la lógica vieja («el último `.sha256` que aparezca»), dos
    de las cinco pruebas nuevas fallan. *(El síntoma no habría sido un agujero de seguridad sino una
    actualización que se rechaza siempre a sí misma: comparar el instalador contra el hash de otro archivo
    nunca coincide.)*

- [x] **[T2-07] Acotar el tamaño de la descarga del checksum**
  - **Área:** Seguridad
  - **Ubicación:** `src/FormatDiskPro/Services/UpdateService.cs:236`
  - **Qué hacer:** `Http.GetStringAsync(checksumUrl, ct)` lee la respuesta entera en memoria sin límite.
    Leer como máximo unos cientos de bytes (un SHA-256 en hex ocupa 64).
  - **Criterio de aceptación:** una respuesta desmedida se rechaza en vez de materializarse.
  - **Esfuerzo:** bajo
  - **Depende de:** ninguna
  - **Resuelta el 2026-08-15.** `DownloadChecksumTextAsync` con tope de **512 bytes**, comprobando las dos
    cosas: el `Content-Length` declarado y lo que realmente llega (un servidor puede mentir en la cabecera
    o no enviarla). Nueva clave `update.checksumUnreadable` en los 5 idiomas: el motivo del rechazo es
    distinto del de un hash que no coincide, y el usuario lee ese texto. **La prueba discrimina porque el
    hash servido es el correcto** —va al principio del cuerpo, seguido de 64 KB de relleno—: lo que rechaza
    la respuesta es su tamaño, no la comparación. Verificado por reversión: con `GetStringAsync`, pasa.

- [x] **[T2-08] Adelgazar `MainWindow.xaml.cs` (2 070 líneas)**
  - **Área:** Arquitectura
  - **Ubicación:** `src/FormatDiskPro/UI/MainWindow.xaml.cs`
  - **Qué hacer:** concentra orquestación, lanzamiento de procesos (`RunFormatComAsync`,
    `RunFormatVolumeAsync`), tematizado, i18n, ciclo de vida de operación y subclassing Win32. Extraer al
    menos el lanzamiento de procesos de formato a `Services/` (donde ya viven sus hermanos) y el
    `WM_DEVICECHANGE` a su propio helper. **Sin cambiar comportamiento** — cada extracción, un commit.
  - **Criterio de aceptación:** ningún archivo de `UI/` supera ~800 líneas; las 289 unitarias y los UI
    tests siguen en verde.
  - **Esfuerzo:** alto
  - **Depende de:** T0-02 (para no mover código y cambiar su manejo de errores a la vez)
  - **Resuelta el 2026-08-15.** Dos extracciones **reales** —las que pedía la tarea— y luego una partición
    del resto:
    1. **`Services/FormatProcess`**: `RunFormatVolumeAsync`/`RunFormatComAsync` salen de la ventana y se
       ponen junto a sus hermanos (`CheckDisk`, `ReinitDrive`, `SecureWipe`). El proceso en marcha se
       entrega por *callback* en vez de guardarse en el servicio, y el progreso va por `IProgress<int>`:
       así el servicio no toca ni la barra ni el estado de la ventana.
    2. **`UI/DeviceChangeWatcher`**: los cuatro `DllImport`, el delegado que hay que mantener vivo y el
       *debounce* del `WM_DEVICECHANGE`, en su propia clase `IDisposable`.
    3. **El resto, repartido en `partial class`** por asunto: `.DriveInfo`, `.FormatOptions`,
       `.Operations`, `.HelpAndUpdates`, `.Preferences`.
  - **Resultado:** `MainWindow.xaml.cs` **2 107 → 753 líneas**; el mayor de `UI/` es ahora
    `MainWindow.Operations.cs` (509). Ninguno supera 800.
  - **Honestidad sobre el punto 3:** partir un archivo en `partial` **no reduce el acoplamiento** — sigue
    siendo la misma clase con el mismo estado compartido. Lo que arregla es lo que la tarea decía:
    encontrar algo en 2.000 líneas. El rediseño de verdad (inyección de dependencias, `Services` no
    estáticos) es `T4-02`, **resuelto el 2026-08-16** — y que siga siendo una clase grande con estado
    compartido tampoco lo arregló aquello: lo que arregló fue que sus fallos sean observables.
  - **Sin cambiar comportamiento, y comprobado como toca:** build 0/0, **389/389** unitarias y la suite de
    UI **24/27 con la USB conectada** — exactamente el mismo resultado que antes de tocar nada.

- [x] **[T2-09] Rotar `history.log`**
  - **Área:** Código
  - **Ubicación:** `src/FormatDiskPro/Services/History.cs:21-30`
  - **Qué hacer:** el historial solo crece; `HistoryDialog` lo parsea entero en memoria en cada apertura
    (`HistoryDialog.xaml.cs:64`). Rotar por tamaño (p. ej. 2 MB → `history.1.log`).
  - **Criterio de aceptación:** superado el umbral se rota y el visor sigue mostrando lo reciente.
  - **Esfuerzo:** bajo
  - **Depende de:** ninguna
  - **Resuelta el 2026-08-15.** Política pura en `Core/HistoryRotation` (umbral 2 MB, nombre de la
    generación anterior) y el movimiento de archivos en `History`. **Dos generaciones y se acabó**
    (`history.log` + `history.1.log`): el disco queda acotado a ~4 MB.
  - **El detalle que decide si esto es una mejora o un regalo envenenado:** el visor lee **las dos**
    generaciones, la vieja primero. Rotando solo el archivo activo, la entrada que provoca la rotación
    dejaría al usuario mirando un historial casi vacío justo después de una operación — y en un registro de
    auditoría eso se lee como *«se han perdido mis datos»*. Por lo mismo, *Borrar el historial* se lleva
    también la generación rotada: si no, limpiar dejaría 2 MB a la vista.
  - Se rota **antes** de escribir, no después, para que el archivo activo nunca quede por encima del
    umbral. Verificado por reversión: sin la rotación fallan dos de las seis pruebas nuevas.
  - **Efecto colateral en los UI tests:** `SettingsBackup` respalda ahora también `history.1.log`. Hacen
    falta 2 MB para que exista, pero el respaldo existe justamente para no dejar rastro en el `%AppData%`
    del usuario, y una excepción «que casi nunca pasa» es como se cuelan.

- [x] ~~**[T2-10] CI de solo unitarias en GitHub Actions**~~ — ❌ **DESCARTADA (2026-08-15)**
  - **Área:** DevOps
  - **Ubicación:** `.github/` (hoy solo contiene `FUNDING.yml`)
  - **Qué hacer:** la decisión de 2026-07-12 descartó CI porque un runner hospedado no puede correr los UI
    tests — cierto, pero eso deja también los **unitarios** sin ejecutarse en ningún PR externo: hoy solo
    corren en la máquina del mantenedor durante `release.ps1`. Un workflow de `dotnet build` +
    `dotnet test FormatDiskPro.slnx` (que **no** arrastra los UI tests, por diseño) cubre ese hueco sin
    reabrir nada.
  - **Criterio de aceptación:** un PR con una unitaria rota queda en rojo antes de la revisión.
  - **Esfuerzo:** bajo
  - **Depende de:** ninguna
  - **Descartada el 2026-08-15, por decisión del mantenedor: las pruebas de este proyecto se ejecutan
    SOLO en local.** Se llegó a implementar (`.github/workflows/tests.yml`, con los comandos verificados
    en local) y se **revirtió**. No es una tarea aplazada: no se va a hacer.
  - **El porqué, que además refuerza la decisión del 2026-07-12:** en este proyecto la prueba que vale es
    la que ejerce la app real, y esa **no cabe en un runner** (sesión elevada + USB física). Un CI que solo
    corre los unitarios pone un ✅ verde en el repositorio que dice bastante menos de lo que parece —
    justo el problema que `T2-12` acaba de arreglar en el otro extremo del proceso—. La verificación de
    verdad la da `release.ps1 -UiTests` desde una terminal elevada, y esa es la puerta que importa.
  - **Consecuencia asumida:** en un PR externo, los unitarios no se ejecutan hasta que el mantenedor los
    corre. El proyecto es de un solo mantenedor y el corte no sale sin pasar por `release.ps1`, así que la
    puerta sigue existiendo — está en local, no en GitHub.

- [x] **[T2-12] Que el corte diga cuánta cobertura de UI llevó realmente**
  - **Área:** DevOps / QA
  - **Ubicación:** `release.ps1:220-223`
  - **Qué hacer:** el 2026-08-13, al conectar por fin la USB de pruebas, apareció que
    `CheckDisk_ScanOnly_CompletesForTestDrive` llevaba **roto desde la v1.15.2** (el pase de UX apiló los
    botones del diálogo de chkdsk y eliminó el `PrimaryButton` que el test buscaba). Los cortes de
    **v1.15.2 y v1.16.0 salieron en verde con ese test roto**, porque sin la USB se omitía y «omitido» y
    «correcto» se distinguen mal en el resumen. El diseño de omitir en vez de fallar **es el correcto** —
    un corte no debe caer por falta de hardware — pero hoy no deja rastro de qué cobertura se sacrificó.
    Hacer que `release.ps1` cuente los omitidos, los liste y lo repita en el resumen final del corte
    (p. ej. «UI tests: 17/23 — 6 OMITIDOS por falta de la USB de pruebas»).
  - **Criterio de aceptación:** un corte sin la USB imprime, al final, cuántos tests se omitieron y por
    qué; con la USB conectada lo dice también, con el conteo a cero.
  - **Esfuerzo:** bajo
  - **Depende de:** ninguna
  - **Resuelta el 2026-08-15.** `release.ps1` pide ahora el **logger `trx`** y lo lee
    (`Get-TestRunSummary` + `Show-UiTestCoverage`): la salida de consola de `dotnet test` **no lista** los
    tests omitidos ni su motivo, el `.trx` sí. El resumen se imprime **dos veces** —al terminar las
    pruebas y en el bloque final del corte, que es el único que se lee cuando todo ha ido bien— y nombra
    cada test omitido **con el motivo que declara su atributo** («Requiere la USB de pruebas conectada…»,
    «Define FORMATDISKPRO_ALLOW_YANK=1…»). Sin `-UiTests` o con `-SkipTests` lo dice también, en vez de
    callar.
  - **Verificado sobre un `.trx` real**, no sobre uno inventado: la corrida no elevada de la suite produce
    los **8 omitidos** y el resumen los lista con sus motivos, acentos incluidos. *(El `.trx` se lee con
    `XmlDocument.Load` y no con `[xml](Get-Content -Raw)`: en PS 5.1 eso último lee con la página de
    códigos ANSI y destroza los motivos, que están en español. Es la misma trampa del `.csproj` del `#45`,
    en otro archivo.)*

- [x] **[T2-13] Ejercitar los `catch` de las operaciones quitando la unidad de verdad**
  *(añadida y resuelta el 2026-08-14)*
  - **Área:** QA
  - **Ubicación:** `tests/FormatDiskPro.UiTests/OperationErrorTests.cs`, `DriveYank.cs`
  - **Por qué:** `T2-05` dejó bajo test lo que los `catch` de `T0-02` **escriben**, no que lleguen a
    **ejecutarse**. Un `catch` que nadie ha visto ejecutarse no es una red, es una suposición.
  - **Qué se hizo:** `DriveYank.ForceDismount` desmonta el volumen a la fuerza a mitad de la operación
    (`FSCTL_DISMOUNT_VOLUME`), invalidando los handles abiertos de la app — el efecto exacto de quitarle
    la USB de las manos. Dos pruebas: *Verificar capacidad* y *Benchmark*.
  - **La aserción que hace que la prueba valga:** `Assert.DoesNotContain("CRASH:", …)`. Sin ella pasaría
    **igual con los `catch` borrados**, porque la red global de `T0-01` también deja la app viva y también
    muestra un diálogo: desde fuera las dos rutas son idénticas. Lo único que las distingue es qué línea
    aparece en el historial. **Verificado por reversión:** con el `catch` de `VERIFY` neutralizado, el
    historial recibe `CRASH: System.IO.IOException…` y la prueba falla nombrando lo que faltaba.
  - **`Set-Disk -IsOffline` no sirve** para esto: Windows lo rechaza sobre medios extraíbles
    (*«Removable media cannot be set to offline»*). Es una operación de discos fijos.
  - **Opt-in propio** (`FORMATDISKPRO_ALLOW_YANK=1`), separado del destructivo: no borra datos, pero hace
    desaparecer una unidad del sistema y no debe correr por sorpresa en un corte de release.
  - **Esfuerzo:** medio · **Depende de:** T0-02, T2-05

- [x] **[T2-11] `SECURITY.md`, `CONTRIBUTING.md` y plantillas de issue**
  - **Área:** Documentación / DevOps
  - **Ubicación:** `.github/`
  - **Qué hacer:** una herramienta GPLv3 que formatea discos, corre elevada y se auto-actualiza no publica
    canal de reporte de vulnerabilidades ni guía de contribución. Añadir `SECURITY.md` (cómo reportar y
    qué esperar), `CONTRIBUTING.md` (build, pruebas, la exigencia de terminal elevada) y plantillas de
    issue/PR.
  - **Criterio de aceptación:** los tres archivos existen y GitHub los muestra en la pestaña Security y al
    abrir un issue.
  - **Esfuerzo:** bajo
  - **Depende de:** ninguna
  - **Resuelta el 2026-08-15.** `.github/SECURITY.md`, `.github/CONTRIBUTING.md`, dos plantillas de issue
    (formularios YAML: error y sugerencia), `config.yml` con el enlace al reporte privado y
    `PULL_REQUEST_TEMPLATE.md`. El README enlaza los tres. YAML validado.
  - **`SECURITY.md` dice también lo que NO es una vulnerabilidad** —correr como administrador, el
    instalador sin firmar, el alcance honesto del SHA-256— para no recibir reportes de decisiones ya
    documentadas, y deja claro qué **sí** interesa. El canal es el reporte privado de GitHub: no se
    publica ninguna dirección de correo.
  - **`CONTRIBUTING.md` recoge lo que aquí falla si no lo sabes:** terminal elevada para los UI tests, las
    precondiciones que omiten en vez de fallar, los tests que vigilan la i18n y el contraste, y que **no
    se aceptan PRs con GitHub Actions** — el testing de este proyecto es local (`T2-10`).

---

## 🟢 Tier 3 — Pulido y mantenimiento

- [x] **[T3-01] La exportación CSV no puede fallar en silencio**
  - **Área:** UX / manejo de errores
  - **Ubicación:** `src/FormatDiskPro/UI/HistoryDialog.xaml.cs:105-112`
  - **Qué hacer:** `catch { }` se traga cualquier fallo de escritura: el usuario elige destino, no ve nada
    y **cree que exportó**. Mantener el «no romper el diálogo», pero informar del fallo.
  - **Criterio de aceptación:** exportar a una ruta sin permiso muestra un mensaje.
  - **Esfuerzo:** bajo · **Depende de:** ninguna
  - **Resuelta el 2026-08-15.** `InfoBar` de error **dentro** del propio diálogo (un `ContentDialog` no
    puede abrir otro), con el motivo real de la excepción, más una línea `EXPORT ERROR` en el historial.
    Se conserva el «no romper el diálogo» —sigue abierto y utilizable— pero sin callar: el fallo
    silencioso dejaba al usuario convencido de que tenía su CSV. Nueva clave `history.exportFailed`.

- [x] **[T3-02] Evitar el `sb.ToString()` por chunk en `ReinitDrive`**
  - **Área:** Rendimiento
  - **Ubicación:** `src/FormatDiskPro/Services/ReinitDrive.cs:99-106`
  - **Qué hacer:** rematerializa el búfer completo en cada lectura (O(n²)). Buscar los marcadores `STAGE:`
    sobre el fragmento nuevo más un solapamiento corto, como ya hace el `carry` de
    `CheckDisk.cs:70-84`.
  - **Criterio de aceptación:** mismo reporte de etapas, sin `ToString()` dentro del bucle.
  - **Esfuerzo:** bajo · **Depende de:** ninguna
  - **Resuelta el 2026-08-15.** Se busca sobre el fragmento nuevo más 24 caracteres de solapamiento
    (`STAGE:partition` son 15), como el `carry` de `CheckDisk`. El `StringBuilder` sigue acumulando la
    salida completa —hace falta al final para `ParseNewLetter`—, pero ya no se rematerializa en cada
    lectura.

- [x] **[T3-03] `LoadHealthAsync` no es un handler: no debe ser `async void`**
  - **Área:** Código
  - **Ubicación:** `src/FormatDiskPro/UI/MainWindow.xaml.cs:353`
  - **Qué hacer:** convertir a `async Task` y consumirlo explícitamente desde
    `DrivePicker_SelectionChanged`.
  - **Criterio de aceptación:** no quedan `async void` fuera de handlers de eventos.
  - **Esfuerzo:** bajo · **Depende de:** T0-02
  - **Resuelta el 2026-08-15.** Es `async Task` y se consume con un descarte explícito (`_ =`): no puede
    esperarse —el selector no se bloquea mientras PowerShell consulta el S.M.A.R.T.— pero ahora está
    escrito que es deliberado. Comprobado que **no queda ningún `async void` fuera de un manejador**.
  - **Con un matiz que la tarea no decía, y que importa:** al dejar de ser `async void`, una excepción ya
    no llega a la red global de `T0-01` — se quedaría en una `Task` que nadie observa, es decir, en
    silencio. Se añade un `catch` que pinta la salud como «no disponible» y registra `HEALTH ERROR`.
    Cambiar dónde se manejan los errores es parte del cambio, no un extra.

- [x] **[T3-04] Corregir la documentación de validación de `AppSettings`**
  - **Área:** Documentación
  - **Ubicación:** `src/FormatDiskPro/Services/AppSettings.cs:39-49` vs `:79-95`
  - **Qué hacer:** los `<summary>` dicen que `SecureWipePasses` y `SmallFat32SizeGb` «se validan **al
    cargar**», pero `Load()` solo deserializa; la normalización ocurre en la UI (`InitWipePasses`,
    `InitSmallFat32Size`). O normalizar en `Load()` (preferible) o corregir el texto.
  - **Criterio de aceptación:** documentación y comportamiento coinciden.
  - **Esfuerzo:** bajo · **Depende de:** ninguna
  - **Resuelta el 2026-08-15, por la vía preferible: normalizando en `Load()`.** Corregir el texto habría
    dejado el hueco abierto — un `settings.json` editado a mano (o escrito por una versión futura) entraba
    con 0 pasadas o una partición de 7 GB, y la UI lo tapaba eligiendo otra cosa mientras el objeto seguía
    llevando el valor imposible. +7 pruebas.

- [x] **[T3-05] «No hay colores hardcodeados» ya no es cierto**
  - **Área:** Documentación
  - **Ubicación:** `src/FormatDiskPro/UI/Theme/AppTheme.xaml:10`; `CONTEXT.md` §4 *Otros*
  - **Qué hacer:** el comentario afirma que no hay colores fijos, y `CONTEXT.md` reconoce **dos**
    excepciones deliberadas (`SeverityPalette` y la barra de capacidad). En realidad hay más:
    `MainWindow.ProtectedColor`/`DriveBrush`/`UpdateCaptionButtonColors` y `HistoryDialog.ColorFor`.
    Ajustar ambos textos tras `T1-04`, cuando la lista real sea la definitiva.
  - **Criterio de aceptación:** la documentación enumera exactamente las excepciones que quedan.
  - **Esfuerzo:** bajo · **Depende de:** T1-04

- [x] **[T3-06] `L.T(clave, args)` puede lanzar pese a prometer que no**
  - **Área:** Código
  - **Ubicación:** `src/FormatDiskPro/Localization/Localization.cs:61-64`
  - **Qué hacer:** `T(string)` es defensivo («nunca lanza»), pero la sobrecarga con `params` llama a
    `string.Format`, que lanza `FormatException` si una traducción trae un placeholder mal escrito.
    Envolver en `try/catch` y devolver la plantilla sin formatear.
  - **Criterio de aceptación:** test con una traducción de placeholder roto que no lanza.
  - **Esfuerzo:** bajo · **Depende de:** ninguna
  - **Resuelta el 2026-08-15.** `try/catch (FormatException)` que devuelve la plantilla **sin formatear**:
    sigue siendo legible y además delata el error. Un fallo de traducción debe verse como un texto raro,
    no como una app que se cae. Verificado por reversión: con el `string.Format` directo, la prueba nueva
    falla con `FormatException`.

- [x] **[T3-07] Formato de la apertura de `L.Map`**
  - **Área:** Estilo
  - **Ubicación:** `src/FormatDiskPro/Localization/Localization.cs:67-68`
  - **Qué hacer:** la primera entrada va pegada a la llave (`new()\n{        ["section.drive"]`), lo que
    descoloca el diccionario respecto al resto del archivo.
  - **Criterio de aceptación:** la primera clave se alinea con las demás.
  - **Esfuerzo:** bajo · **Depende de:** ninguna
  - **Resuelta el 2026-08-15.** La llave de apertura va en su línea y `section.drive` se alinea con el
    resto del diccionario.

- [x] **[T3-08] Sacar los iconos decorativos del árbol de automatización**
  - **Área:** Accesibilidad
  - **Ubicación:** `src/FormatDiskPro/UI/MainWindow.xaml:79,102,131,160`
  - **Qué hacer:** los `FontIcon` de encabezado son puramente decorativos y hoy los recorre el lector de
    pantalla. Marcarlos `AutomationProperties.AccessibilityView="Raw"`.
  - **Criterio de aceptación:** el recorrido con Narrador no lee glifos sin significado.
  - **Esfuerzo:** bajo · **Depende de:** ninguna
  - **Resuelta el 2026-08-15.** `AccessibilityView="Raw"` **en el estilo** `SectionIconStyle`, no en cada
    icono: así lo hereda cualquier icono de sección que se añada después: la corrección se queda puesta
    sola. El glifo del botón *Actualizar* se marca aparte (el botón ya tiene nombre accesible propio).

- [x] **[T3-09] No pasar la contraseña del certificado por línea de comandos**
  - **Área:** Seguridad / DevOps
  - **Ubicación:** `src/FormatDiskPro/installer/build-installer.ps1:39` y `:85`
  - **Qué hacer:** `-CertPassword` es `[string]` y se pasa a `signtool` como `/p <valor>`: queda visible en
    la línea de comandos del proceso (cualquier proceso local la ve) y en el historial de PowerShell. Usar
    `[SecureString]` o una variable de entorno.
  - **Criterio de aceptación:** firmar no expone la contraseña en la línea de comandos.
  - **Esfuerzo:** bajo · **Depende de:** ninguna
  - **Nota:** hoy no se firma (`#13`); aplica solo a quien use el flujo opcional.
  - **Resuelta el 2026-08-15, con un límite que conviene decir en voz alta.** `-CertPassword` pasa a
    `[SecureString]` en `build-installer.ps1` **y** en `release.ps1`, con alternativa por la variable
    `FORMATDISKPRO_CERT_PASSWORD`, y solo se descifra en el momento de construir los argumentos.
  - **Lo que esto NO arregla:** `signtool.exe` únicamente acepta la contraseña por `/p`, así que durante
    esa llamada sigue estando en **su** línea de comandos. Lo que se elimina es la exposición en el
    historial de PowerShell y en la línea de comandos de nuestros scripts. La única vía sin exposición
    alguna es importar el `.pfx` en el almacén y usar `-CertThumbprint`; queda documentado en la ayuda del
    script para quien algún día firme.

- [x] **[T3-10] Documentar (o cambiar) el RNG del borrado seguro**
  - **Área:** Seguridad / documentación
  - **Ubicación:** `src/FormatDiskPro/Services/SecureWipe.cs:85`
  - **Qué hacer:** la pasada «aleatoria» usa `System.Random`, un PRNG no criptográfico. Para **destruir**
    datos previos da igual el origen de la aleatoriedad, pero el nombre «borrado seguro» invita a suponer
    otra cosa. O usar `RandomNumberGenerator.Fill` (coste despreciable frente a la E/S) o decirlo en el
    `<remarks>`, que ya es honesto sobre la limitación de TRIM.
  - **Criterio de aceptación:** código y documentación coinciden sobre qué garantiza la pasada aleatoria.
  - **Esfuerzo:** bajo · **Depende de:** ninguna
  - **Resuelta el 2026-08-15 cambiando el código, no el texto:** `RandomNumberGenerator.Fill` en vez de
    `System.Random`. Para **destruir** lo anterior el origen de la aleatoriedad da igual —lo que borra es
    la sobrescritura—, pero el coste frente a la E/S es despreciable y sale más barato cumplir la
    expectativa que documentar por qué no se cumple. El `<remarks>` sigue siendo honesto sobre lo que de
    verdad limita esto: TRIM y el remapeo de celdas en SSD.

- [x] **[T3-11] El historial se corrompe al registrar texto multilínea** *(añadida y resuelta el 2026-08-13)*
  - **Área:** Código / robustez
  - **Ubicación:** `src/FormatDiskPro/Services/History.cs:21`, `src/FormatDiskPro/App.xaml.cs`
  - **Qué pasaba:** `history.log` es un formato de **una entrada por línea**
    (`marca de tiempo TAB mensaje`) y `HistoryEntry.Parse` lo lee así, pero `History.Log` escribía el texto
    recibido tal cual. Los caminos de error registran texto que no controlamos: `ex.Message` puede traer
    saltos de línea, y el registro de caídas de `T0-01` guarda `e.Exception` **completa, con su traza de
    pila**, que siempre es multilínea. Resultado: **una sola caída se convertía en decenas de entradas
    fantasma** sin marca de tiempo, categoría `Other` y resultado `Info` — es decir, el registro que uno
    consulta justo cuando algo ha ido mal quedaba inservible, y el fallo aparecía disfrazado de
    información. Lo introdujo `T0-01` esta misma mañana y no lo vio nadie porque **ningún test cubría el
    camino de error**: exactamente el hueco que `T2-05` existía para cerrar.
  - **Qué se hizo:** `HistoryEntry.SanitizeDetail` (pura) aplana los saltos a `⏎`; `History.Log` la aplica.
    No se recorta la longitud a propósito: en una entrada `CRASH:` la traza es justo lo que se quiere leer.
  - **Encontrada por:** `T2-05` · **Esfuerzo:** bajo

---

## 🔵 Tier 4 — Futuro / opcional *(fuera del alcance inmediato)*

- [x] **[T4-01] `CHANGELOG.md` en la raíz** — el registro vivía repartido entre `CONTEXT.md` y las notas
  de GitHub Releases; un `CHANGELOG.md` estándar (Keep a Changelog) es lo que espera quien llega al repo.
  *Área: Documentación · Esfuerzo: bajo · Depende de: ninguna*
  - **Resuelta el 2026-08-16.** [`CHANGELOG.md`](CHANGELOG.md) con las 28 versiones publicadas y una
    sección *Sin publicar*. Enlazado desde el README y desde `CONTEXT.md`.
  - **Con puerta en el corte, o habría nacido para envejecer.** `release.ps1` **aborta** si
    `CHANGELOG.md` no tiene ya la sección de la versión que se va a publicar, y el mensaje dice
    exactamente qué escribir. Es la misma forma que ya tienen aquí la cobertura mínima y el `.sha256`:
    un documento que se mantiene «por disciplina» se queda atrás, y entonces **afirma ser el registro
    del proyecto y miente** — peor que no tenerlo.
  - **Las fechas salen de los tags de git, no del recuerdo.** Al escribirlas a ojo desde el índice de
    versiones de `CONTEXT.md`, **18 de 28 estaban mal** (hasta 8 días de desviación). Se corrigieron con
    `git for-each-ref`. El detalle importa porque este archivo es justo el que alguien consultará para
    saber cuándo entró un cambio.

- [x] **[T4-02] Inyección de dependencias en `Services`** — todos eran `static`, lo que hacía imposible
  probar los caminos de error sin hardware (raíz de `T2-05`). Es un rediseño, no un arreglo.
  *Área: Arquitectura · Esfuerzo: alto · Depende de: T2-05*
  - **Resuelta el 2026-08-16.** Los once servicios pasan a clases con interfaz, y el grafo se construye
    en una **raíz de composición** (`Services/AppServices`) que `App` crea y pasa a `MainWindow`, que a
    su vez la pasa a los diálogos que la necesitan (`HealthDialog`, `HistoryDialog`, `AboutDialog`,
    `WhatsNewDialog`). **No es un localizador de servicios:** nadie le pide nada «desde dentro», así que
    las dependencias siguen siendo visibles en cada constructor — que es la mitad del valor de esto.
  - **La costura que de verdad desbloquea las pruebas es `IProcessRunner`.** Lo caro de probar no era la
    estática: era que cada servicio hacía `new Process(...)` él mismo. Con el lanzador inyectado, un
    doble reproduce en milisegundos lo que antes exigía la avería real — un `chkdsk` que devuelve 2, un
    `Clear-Disk` que falla a mitad, un `powershell.exe` bloqueado por directiva.
  - **La abstracción se queda en *arrancar* el proceso, no en «ejecutar y devolver la salida».** Cada
    servicio lee su salida distinto, y con matices que costaron hardware descubrir: el solapamiento de
    marcadores de `T3-02`, cerrar la entrada estándar de `T1-02`, esperar con `CancellationToken.None`
    para no perder el código de salida al cancelar. Unificar esos bucles habría sido **reescribirlos**,
    y este cambio no podía cambiar comportamiento. Los bucles quedan intactos; solo cambia de dónde sale
    el proceso.
  - **Lo que ganó el proyecto, en pruebas: 398 → 433 (+35), ninguna toca un disco.** Entre ellas, las
    del camino destructivo que **nunca** se habían podido escribir: reinicializar con `Clear-Disk`
    fallando a mitad, y —el peor caso— salir con código 0 pero **sin letra asignada**, o sea el disco
    borrado y sin volumen montable. Antes, comprobar eso significaba borrar un disco de verdad.
  - **Dos costuras artificiales desaparecen al llegar la real:** `History.LogTo/ReadLinesFrom/ClearAt`
    (`internal static` con la ruta como parámetro) vuelven a ser privadas, porque ahora la ruta se
    inyecta por constructor.
  - **`UpdateService` conserva sus miembros internos `static`, a propósito.** Ya se probaba entero
    —sus pruebas levantan un servidor HTTP local y ejercitan hash correcto, hash que no coincide,
    checksum ausente y respuesta desmedida—, así que instanciarlos habría sido reescribir la ruta de
    verificación que corre **elevada** sin ganar una sola prueba. Se instancia lo que consume la UI.
  - **Verificado por reversión, no solo por «pasa en verde»:** deshaciendo la guarda de la letra nueva
    falla `Reinit_ExitZeroButNoLetterAssigned_IsAFailure`; poniendo el solapamiento de marcadores a 0
    falla `Reinit_Success_ReportsEveryStageOnceAndInOrder` — que para eso entrega su salida partida en
    trozos de 6 caracteres, en vez de de una vez como haría un `StringReader`.
  - **Lo que esto NO es.** No cambia comportamiento: las 398 pruebas anteriores siguen en verde tal cual,
    y el build sigue en 0/0. Tampoco elimina el acoplamiento de `MainWindow`, que sigue siendo una clase
    grande con estado compartido (eso lo dijo ya `T2-08`). Lo que hace es que **fallar sea observable**.
  - **Corregido el 2026-08-16, y merece quedar escrito: dos de las pruebas nuevas eran intermitentes.**
    Recogían los reportes de `IProgress<T>` con un `Progress<T>` de la BCL, que **no entrega en el acto**:
    postea cada reporte al contexto de sincronización o, si no hay ninguno —el caso en una prueba—, al
    pool. La aserción competía con las entregas, así que **pasaban aisladas y fallaban al correr la suite
    entera**, que es la peor forma de fallar: parece un fallo del código bajo prueba. Además, acumular en
    un `List<T>` desde varios hilos del pool no es seguro. Se sustituye por un `IProgress<T>` síncrono
    (`SyncProgress<T>`). **El servicio no cambia**: en la app esa asincronía es la correcta, porque WinUI
    ordena los reportes por el contexto de la UI. Confirmado con **6 pasadas seguidas** de la suite
    completa, 433/433 cada una.

- [x] ~~**[T4-03] Firmar el instalador**~~ — ❌ **DESCARTADA (2026-08-16).** Ver
  *[Decisiones cerradas](#-decisiones-cerradas-no-reabrir)*.
  *Área: Seguridad · Esfuerzo: alto · Depende de: ninguna*
  - **No se descarta por coste, sino porque no era una tarea.** Entró en la auditoría como pendiente y
    en realidad **contradecía una decisión ya cerrada** (`#13`, 2026-06-24, reafirmada al resolver
    `T1-08`): este proyecto se distribuye por GitHub Releases sin firmar, y por eso existe la
    verificación por **SHA-256**. Tenerla abierta insinuaba que el proyecto «debía» firmar y aún no lo
    había hecho, cuando lo cierto es lo contrario.
  - **Nada queda sin hacer.** El pipeline **ya admite firmar**: `build-installer.ps1` y `release.ps1`
    aceptan `-CertThumbprint`/`-CertFile`/`-CertPassword`/`-TimestampUrl`, el `.sha256` se genera
    **después** de firmar (firmar cambia el binario) y `UpdateService` tiene lista la ruta Authenticode
    con `WTD_REVOKE_WHOLECHAIN` (`T1-08`). Lo único que falta es un certificado, que no es trabajo de
    ingeniería sino una compra.
  - **Y si algún día se compra, el trabajo no es «firmar».** Es poner
    `UpdateService.SignsItsInstallers` en `true` **y** fijar el publicador esperado, las dos cosas: lo
    primero sin lo segundo reabre el agujero que `T1-08` cerró —una firma válida de *cualquiera*
    saltándose el hash, con el instalador ejecutándose como administrador—. Hay un **test tripwire** que
    falla si se hace a medias, así que esa condición ya está vigilada por el build y no necesita vivir
    en el backlog.

- [x] **[T4-04] Más capturas en el README** — eran 3; el modo galería de `tools/capture-screenshots.ps1`
  ya producía 12 en claro y oscuro. *(Estaba listado como pulido opcional en la §6 de `CONTEXT.md`, sección que se reescribió al cerrarse todo.)*
  *Área: Documentación · Esfuerzo: bajo · Depende de: ninguna*
  - **Resuelta el 2026-08-16.** El README pasa de **3 a 12** capturas: ventana principal, S.M.A.R.T.,
    chkdsk, reinicializar, confirmación destructiva e historial, **cada una en los dos temas**. Se
    regeneraron todas —las anteriores eran de la v1.15.2— fotografiando el **publish self-contained**,
    que es lo que se distribuye.
  - **La galería sigue siendo un artefacto de revisión y sigue ignorada por git.** Lo que se versiona es
    el subconjunto elegido, copiado a `docs/screenshots/`. 415 KB en total: un README con 12 PNG no
    tiene por qué pesar.
  - **Encontró un defecto real en la herramienta, y ese es el valor que no estaba en la tarea.** Tres
    tomas (`reinit`, `confirm`, `checkdisk`) esperaban con un `Start-Sleep` fijo de 1,2 s en vez de
    esperar a un elemento, como sí hace el resto —y como decía el comentario que las encabeza—. Sobre
    una unidad **extraíble válida**, *Reinicializar* consulta antes el número de disco físico del
    objetivo y el de Windows (dos llamadas a PowerShell) para la guarda de «no es el disco del
    sistema»: la foto salía con la ventana principal **y sin diálogo**. Ahora esperan al `InputBox` de
    `ConfirmDialog` y al `CheckScanButton`.
  - **Y una trampa que solo se ve mirando las fotos:** capturar *Reinicializar* sin `-Drive <USB>` no
    falla — produce una imagen del mensaje «solo unidades extraíbles». Es la **guarda**, no la
    característica, y habría acabado en el README como si lo fuera. Queda avisado en el README junto al
    comando.
  - **De ahí que las 12 no salgan todas de la misma unidad:** *Reinicializar* y *chkdsk* van sobre la
    USB de pruebas, y el resto sobre un SSD interno, porque un USB **no expone** los contadores
    S.M.A.R.T. que hacen interesante esa pantalla. El README lo dice en vez de disimularlo.

- [x] **[T4-05] Renombrar el `Name` interno del formulario** — resto de la migración desde Windows Forms.
  *(Estaba listado como pulido opcional en la §6 de `CONTEXT.md`, sección que se reescribió al cerrarse todo.)*
  *Área: Limpieza · Esfuerzo: bajo · Depende de: ninguna*
  - **Resuelta el 2026-08-16.** Quedaban exactamente dos rastros, y ninguno era una propiedad `Name`:
    el método **`SetFormEnabled`** (ahora `SetControlsEnabled`, que además describe lo que hace: habilita
    controles, no un formulario) y un comentario *«same as MainForm»* sobre las tablas estáticas.
    Actualizadas también las cinco referencias en comentarios de los UI tests.

---

## 🟣 Tier 5 — Ocurrencias para features existentes

> **Este tier rompe la regla de la Parte 2 a propósito, y por eso va aparte.** Las 40 tareas de la
> auditoría no añaden funcionalidad: corrigen, endurecen o miden lo que ya existe. Estas **sí** añaden
> —son ampliaciones—, pero no son ideas nuevas: son **huecos que se ven al usar una característica ya
> entregada**. Se numeran `T5-xx` como continuación de la auditoría porque nacen del mismo sitio: mirar
> lo que ya está hecho y encontrar dónde se queda corto.
>
> **Origen (2026-08-15, uso real):** tras *Reinicializar unidad → FAT32 pequeña*, el disco queda con la
> partición pedida y **el resto sin asignar**. Para recuperar ese espacio hay que salir a *Crear y
> formatear particiones de disco duro* de Windows. La característica `#37` resuelve el caso del flasheo de
> BIOS y **deja al usuario con un pendrive de 256 GB del que solo puede usar 32**.

**Por qué esto no reabre el «gestor de particiones completo»** (que sigue fuera de alcance): ahí lo vetado
es **redimensionar, fusionar y mover** — operar sobre particiones **con datos**, que es lo que exige
recolocar bytes y puede destruirlos. Aquí el disco se está **borrando entero de todos modos** (`Clear-Disk`
ya lo hace hoy) y solo cambia **cuántas particiones se crean sobre el vacío**. Crear dos en vez de una en
un disco que ya vas a vaciar no es gestionar particiones; es terminar de crear el layout que la operación
ya está creando. La línea del alcance no se mueve: **si algún día hay que preservar datos, es que nos
hemos salido.**

---

- [x] **[T5-01] El plan de particiones, como dato puro** *(prerrequisito de todo el tier)* — **hecho (2026-08-16)**
  - **Área:** Arquitectura / `Core`
  - **Ubicación:** `src/FormatDiskPro/Core/ReinitPlan.cs`, `src/FormatDiskPro/Services/ReinitDrive.cs`
  - **Qué hacer:** hoy el layout está **implícito en un `long?`**: `partitionSizeBytes` significa «una
    partición de este tamaño, o todo el disco si es `null`». Sustituirlo por un plan explícito —una
    secuencia de particiones, cada una con tamaño (o «el resto»), sistema de archivos y etiqueta— más una
    función pura que lo **valide contra el tamaño real del disco** antes de tocar nada: que la suma quepa,
    que ninguna sea de 0, que como máximo una sea «el resto», que cada volumen FAT32 respete el límite de
    32 GB de Windows, y que el número de particiones sea legal para el estilo elegido (**MBR: 4 primarias
    como máximo**; GPT: sin problema práctico).
  - **Por qué primero:** es la única parte de esto que se puede probar **entera sin hardware**, y es donde
    viven los errores que de verdad duelen — un plan mal calculado se descubre **con el disco ya borrado**.
    `ReinitDrive` debe recibir un plan **ya validado** y limitarse a ejecutarlo.
  - **También en plural:** `ParseNewLetter` devuelve **una** letra; con varias particiones hay varias.
    Pasa a devolver la lista, conservando cuál es la primera (la que la UI selecciona al terminar).
  - **Criterio de aceptación:** un plan cuya suma excede el disco, o con dos «resto», o con una FAT32 de
    64 GB, o con 5 particiones en MBR, se **rechaza sin lanzar ningún proceso**. Cubierto por pruebas
    unitarias, sin USB.
  - *Esfuerzo: medio · Depende de: ninguna*

- [x] **[T5-02] Usar el espacio restante en vez de dejarlo sin asignar** *(el hueco real)* — **hecho (2026-08-16)**
  - **Área:** Funcionalidad / UI
  - **Ubicación:** `UI/MainWindow.FormatOptions.cs` (tarjeta de opciones), `Services/ReinitDrive`
  - **Qué hacer:** al marcar *FAT32 pequeña*, ofrecer **qué hacer con el resto**: dejarlo sin asignar
    (comportamiento actual, que se conserva porque a veces es lo querido) o **crear una segunda partición
    con todo el espacio sobrante**, con su sistema de archivos (NTFS/exFAT) y su etiqueta. Una sola
    partición extra, en la misma operación y bajo la misma confirmación destructiva que ya existe.
  - **Por qué es el arreglo y no una función nueva:** es el caso `#37` **terminado**. Hoy la opción que
    resuelve el flasheo de BIOS **inutiliza el resto del pendrive** hasta que el usuario sale a una
    herramienta de Windows que esta app existe para no tener que abrir.
  - **Criterio de aceptación:** en la USB de pruebas, *FAT32 pequeña (1 GB) + resto en exFAT* deja el disco
    **sin espacio sin asignar** y con dos volúmenes montados; el explorador muestra los dos. El camino
    «dejar sin asignar» sigue produciendo exactamente el resultado de hoy.
  - *Esfuerzo: medio · Depende de: T5-01*

- [x] **[T5-03] Qué queda cuando el plan falla a mitad** — **hecho (2026-08-16)**
  - **Área:** Robustez
  - **Ubicación:** `Services/ReinitDrive`, `UI/MainWindow.Operations.cs`
  - **Qué hacer:** con **una** partición, un fallo es binario: salió o no salió. Con varias hay un estado
    intermedio real —la 1 creada y formateada, la 2 no— y hoy el mensaje de error no sabría decirlo.
    Reportar **qué particiones existen y cuáles no** al fallar, y registrarlo en el historial.
  - **Regla:** **no revertir automáticamente.** El disco ya está borrado; «deshacer» solo puede significar
    borrar otra vez, y una limpieza automática tras un error es la clase de decisión que un usuario no
    pidió. Se informa y se deja elegir.
  - **Criterio de aceptación:** forzando el fallo de la segunda partición, la app queda viva, dice
    exactamente qué se creó, lo registra, y **no** borra lo que sí funcionó.
  - *Esfuerzo: bajo-medio · Depende de: T5-01, T5-02*

- [x] ~~**[T5-04] Varias particiones definidas por el usuario**~~ — ❌ **DESCARTADA (2026-08-16)**
  - **Qué era:** permitir **N particiones**, cada una con su tamaño, sistema de archivos y etiqueta, en
    lugar de las dos de `T5-02`. Tope duro: 4 en MBR, un tope razonable en GPT.
  - **Por qué se cierra, decidido con el usuario tras entregar `T5-02`:**
    1. **La ventana es de 500×900 y de tamaño fijo**, decisión firme del proyecto. Una tabla editable de
       filas variables no cabe ahí, así que N obligaría a un **diálogo aparte** — y eso deja de ser
       «terminar la operación que ya estás haciendo» para convertirse en otra pantalla del producto.
    2. **MBR solo admite 4 primarias**, y `ReinitPlan.StyleFor` elige MBR en todo disco de menos de 2 TB
       —es decir, en cualquier memoria USB— porque es lo que hace que el pendrive lo lea el BIOS de una
       placa base, un televisor o la radio de un coche. Así que «N» en la práctica es «3 o 4»: poco
       recorrido frente a lo que cuesta.
    3. **El caso que originó el tier lo cubren dos.** FAT32 para flashear + el resto aprovechable resuelve
       el pendrive de 256 GB del que solo se podían usar 32.
    4. Cada partición extra es otra forma de fallar a mitad, con el disco ya borrado.
  - **Lo que NO se pierde:** el motor y el validador de `T5-01` **admiten N desde el primer día**
    (`PartitionPlan` es una lista, y `Validate` ya comprueba el tope de 4 en MBR). Lo que se limitó es la
    **interfaz**. Si algún día el uso real pide más de dos, esto se reabre sin rehacer `Core/`.
  - *Estado: cerrada por decisión de producto, no por falta de tiempo.*

- [x] **[T5-05] Cobertura de UI del plan multi-partición** — **hecho (2026-08-16)**, junto a `T5-02`
  - **Área:** QA
  - **Ubicación:** `tests/FormatDiskPro.UiTests/DestructiveLifecycleTests.cs`
  - **Qué hacer:** el ciclo destructivo actual crea **una** partición. Extenderlo al plan de `T5-02` sobre
    la USB de pruebas, **manteniendo el opt-in** `FORMATDISKPRO_ALLOW_DESTRUCTIVE=1` (un corte de release
    no debe ejecutarlo) y dejando el disco en un estado conocido al terminar.
  - **Criterio de aceptación:** la prueba comprueba **los dos volúmenes**, no solo que la operación diga
    que fue bien. Con el tamaño de la segunda partición mal calculado a propósito, falla.
  - *Esfuerzo: medio · Depende de: T5-02*

> **Nota de plataforma que conviene tener escrita antes de empezar:** Windows solo monta **todas** las
> particiones de un medio marcado como extraíble desde Windows 10 1703. FormatDiskPro exige 19041 o
> superior, así que **aquí no es un problema** — pero un pendrive multipartición hecho con esta app puede
> mostrar **solo la primera** en un equipo más antiguo. Si `T5-02` entra, eso va dicho en la interfaz, no
> solo en este archivo.

---

## 🎨 Tier 6 — Refinado de UX/UI

> **Ojo con el nombre: hay otro «Tier 6» en este archivo.** El de la **Parte 1** («Pulido UX/UI», v1.13.0)
> es historial de producto cerrado y usa IDs `#28`–`#36`. Este es de la **Parte 2** y usa `T6-xx`. Se
> llaman parecido porque tratan de lo mismo; no son el mismo tier ni la misma numeración.
>
> **Como el Tier 5, este tampoco forma parte de la auditoría** (que cerró en 40 tareas y ahí se queda).
> A diferencia del Tier 5, **no añade funcionalidad**: todo lo de aquí es la interfaz diciendo algo
> impreciso, mostrando un dato en crudo o pintando dos veces lo mismo de forma distinta.

**Origen (2026-08-17):** revisión enfocada solo en UX/UI, sobre las capturas del corte de la v1.22.0
(`docs/screenshots/`, 2026-08-16) y contrastando cada hallazgo contra el código. **10 hallazgos: 3 son
defectos** —la interfaz afirma algo que no es cierto— **y 7 son refinamientos**.

**Lo que esta revisión NO cubrió, y hay que cubrir antes de darla por completa** (`T6-11`):

- El terminal no estaba elevado, así que **no se pudieron usar ni `tools/capture-screenshots.ps1` ni
  FlaUI**: los dos abortan por diseño contra una app `requireAdministrator`.
- Las capturas son **anteriores** al bloque de ocupación nuevo (barra de dos colores + rótulo), así que
  eso **no está revisado en ejecución** por nadie salvo a ojo del usuario.
- Quedan diálogos **sin fotografiar en esta ronda**: Presets, Acerca de, Novedades, Licencia y Terceros
  (los de `docs/screenshots/gallery/` son de julio y ya no representan la app).

---

### Defectos — la interfaz afirma algo que no es cierto

- [x] **[T6-01] El diálogo de *Reinicializar* se titula «Confirmar formato»** — **hecho (2026-08-17)**
  - **Área:** UI / seguridad de la acción destructiva
  - **Ubicación:** `src/FormatDiskPro/UI/ConfirmDialog.xaml.cs`, `src/FormatDiskPro/UI/MainWindow.Operations.cs`, `src/FormatDiskPro/Localization/Localization.cs`
  - **Qué hacer:** `ConfirmDialog` fija `Title = L.T("confirm.title")` en el constructor, así que las **dos**
    operaciones destructivas se anuncian como «Confirmar formato». El cuerpo del diálogo sí explica bien que
    se borra el disco entero. Pasar el título como parámetro y añadir una clave para la reinicialización.
  - **Por qué:** es la operación **más** destructiva de la app —borra el disco físico completo, todas sus
    particiones— presentándose con el nombre de otra menos grave. Quien lee solo el título confirma algo
    distinto de lo que va a ocurrir.
  - **Criterio de aceptación:** abrir *Reinicializar* muestra un título que nombra la reinicialización;
    abrir *Iniciar* sigue mostrando el de formato. Una prueba de UI comprueba los dos, no uno.
  - **Cómo se hizo:** el título pasa a ser **parámetro obligatorio** de `ConfirmDialog`, no un valor por
    defecto. Una tercera operación destructiva no puede heredar el nombre equivocado por omisión: quien la
    añada tiene que decidirlo. Nueva clave `confirm.titleReinit` en los cinco idiomas.
  - **Verificado — y por reversión, no solo en verde:** prueba unitaria (los dos títulos existen y difieren
    en **los cinco** idiomas, para que una traducción perezosa no rehaga el fallo en uno solo), 527/527. Y
    la de UI (`ConfirmDialogs_EachDestructiveOperationHasItsOwnTitle`) contra la app real: lee
    `'Confirmar formato'` y `'Confirmar reinicialización'`. **Devolviendo la llamada a `confirm.title`, la
    prueba falla** y el mensaje nombra el problema.
  - *Esfuerzo: bajo · Depende de: —*

- [x] **[T6-02] El campo de confirmación lleva la letra a adivinar como *placeholder*** — **hecho (2026-08-17)**
  - **Área:** UI / seguridad de la acción destructiva
  - **Ubicación:** `src/FormatDiskPro/UI/ConfirmDialog.xaml.cs`, `src/FormatDiskPro/UI/ConfirmDialog.xaml`
  - **Qué hacer:** el code-behind hace `InputBox.PlaceholderText = _letter`, pisando el `"…"` neutro que ya
    trae el XAML. Quitarlo.
  - **Por qué:** dos daños a la vez. **Parece relleno**: en las capturas el campo muestra una «G» gris y se
    lee como si ya estuviera escrita (el script de captura no teclea nada). Y **regala la respuesta** justo
    en el único punto donde el diseño añade fricción a propósito: escribir la letra es la barrera que separa
    «he entendido qué unidad voy a destruir» de «he pulsado dos veces».
  - **Criterio de aceptación:** el campo aparece visiblemente vacío y el botón primario sigue deshabilitado
    hasta que la letra tecleada coincide.
  - **Era peor de lo que decía esta tarea.** WinUI usa el `PlaceholderText` como **nombre accesible** del
    `TextBox` cuando no hay otro: el campo se llamaba `I`, o sea que un lector de pantalla **anunciaba la
    respuesta en voz alta**. Y quitar el placeholder sin más lo habría dejado llamándose «…». Se le da
    nombre propio (`confirm.inputName`, ×5 idiomas), que además no depende de lo que se pinte dentro.
  - **La primera prueba que escribí no valía, y lo dijo la reversión.** Buscaba un elemento del diálogo cuyo
    texto visible fuera justo la letra, y pasaba **igual con el fallo puesto**: WinUI no publica el
    placeholder como texto de ningún elemento. Se reescribió contra el `Name` del campo, que es donde el
    fallo existía.
  - **Verificado por reversión:** con `PlaceholderText = _letter` de vuelta, la prueba falla con
    `Name='I'`. 527/527 unitarias · 26/29 de UI (3 omitidas de opt-in).
  - *Esfuerzo: trivial · Depende de: —*

- [x] **[T6-03] «Velocidad de rotación: SSD»** — **hecho (2026-08-17)**
  - **Área:** UI / S.M.A.R.T.
  - **Ubicación:** `src/FormatDiskPro/UI/HealthDialog.xaml.cs`
  - **Qué hacer:** cuando las RPM son 0 se devuelve el literal `"SSD"` como *valor* de la fila «Velocidad de
    rotación». Ocultar la fila en ese caso (o mostrar «No aplicable», localizado).
  - **Por qué:** una velocidad cuyo valor es un tipo de medio es un error de categoría, y la fila
    inmediatamente anterior ya dice «Tipo de medio: SSD». Un disco de estado sólido no tiene eje: la
    respuesta correcta no es «SSD», es que la pregunta no aplica. De paso, `"SSD"` es texto de cara al
    usuario fuera de `Localization/`.
  - **Criterio de aceptación:** en un SSD la fila no aparece (o dice «No aplicable»); en un disco mecánico
    sigue mostrando las RPM.
  - **Cómo se hizo:** la decisión es lógica pura y vive en `Core` —`SmartInfo.HasSpindle`—, no en el
    diálogo, para poder medirla. `RPM = 0` es el disco diciendo explícitamente «no giro» y manda sobre el
    tipo de medio; sin RPM, decide el medio. **Sin ninguna señal devuelve `true`** («asume que gira») a
    propósito: esconder la fila por desconocimiento afirmaría que es de estado sólido sin saberlo. De paso
    desaparece el literal `"SSD"`, que era texto de cara al usuario fuera de `Localization/`.
  - **Verificado en la app real, por las dos caras** (capturas con `capture-screenshots.ps1 -Only health`):
    en **D:** (SATA SSD) la fila ya no aparece; en **I:** (USB que no informa de nada) **sigue apareciendo**
    como *No disponible*. +9 unitarias sobre `HasSpindle`.
  - *Esfuerzo: bajo · Depende de: —*

### Refinamientos

- [x] **[T6-04] Las horas de encendido, en escala humana** — **hecho (2026-08-17)**
  - **Área:** UI / S.M.A.R.T.
  - **Ubicación:** `src/FormatDiskPro/UI/HealthDialog.xaml.cs`, `src/FormatDiskPro/Localization/Localization.cs`
  - **Qué hacer:** hoy se pinta `{0} h` en crudo: «32147 h». Añadir separador de millares (según cultura) y
    el equivalente legible: `32.147 h (3 años y 8 meses)`.
  - **Por qué:** el dato existe para responder «¿cuánto ha vivido este disco?», y en horas nadie lo
    responde de cabeza.
  - **Cómo se hizo:** `SmartInfo.PowerOnEquivalent`, función pura en `Core`. Elige unidad por tramos
    (días / meses / años) con los cortes a **dos** unidades y no a una, para no decir «1,1 meses» pudiendo
    decir «33,5 días». **Siempre un decimal**, y por eso no hay que pluralizar: «1,0 años» concuerda en
    los cinco idiomas y «1 años» no.
  - **Verificado:** +11 unitarias y captura de la app — `32,161 h (≈ 3.7 años)` en el SSD de 3 años y medio.
  - *Esfuerzo: bajo · Depende de: —*

- [x] **[T6-05] El historial habla en lenguaje de log, no de usuario** — **hecho (2026-08-17)**
  - **Área:** UI / historial
  - **Ubicación:** `src/FormatDiskPro/UI/HistoryDialog.xaml.cs`, `src/FormatDiskPro/Core/HistoryEntry.cs`, `src/FormatDiskPro/UI/MainWindow.Operations.cs`
  - **Qué hacer:** el detalle de cada entrada es la línea de log tal cual:
    `REINIT I: -> G: fs=FAT32 style=MBR small-fat32=2147483648`. Darle una representación legible
    —incluido convertir los tamaños en bytes a `FormatBytes`— **sin perder** la línea original.
  - **Por qué:** es la mayor distancia entre lo que la app es y lo que enseña. `2147483648` son 2 GB, y
    quien abre *Historial de operaciones* no está depurando: está comprobando qué le hizo a un disco.
  - **Cuidado con no romper dos cosas al arreglarlo:** la exportación **CSV** y el fichero `history.log`
    que se puede abrir desde el propio diálogo son formatos con consumidores; lo que cambia es cómo se
    **muestra**. Y el parseo de una entrada por línea ya se rompió una vez (`T3-11`): lo que se añada no
    puede volver a partir entradas.
  - **Criterio de aceptación:** una prueba fija que la representación mostrada de una entrada de
    reinicialización no contiene el número de bytes en crudo, y otra que el CSV exportado sigue igual.
  - **Cómo se hizo:** `HistoryEntry.Humanize`, función **de presentación** en `Core`. Convierte el valor,
    no la línea (`small-fat32=2 GB`, no prosa), y sobre una **lista blanca de claves** —en la misma línea
    conviven `code=1` y `passes=3`, y convertir esos a «1 B» sería peor que no hacer nada. Se aplica solo
    al pintar la fila: `history.log` y el CSV siguen con el byte exacto, y las entradas **ya escritas** se
    ven bien sin migrar nada.
  - **Lo que casi se cuela:** el buscador filtra por `Detail` crudo. Sin tocarlo, teclear «2 GB» —lo que
    el usuario está viendo— no habría devuelto nada. `Matches` busca ahora en los dos.
  - **Verificado:** +11 unitarias (incluidas las dos que exige el criterio) y captura de la app: la lista
    muestra `small-fat32=2 GB` y `bytes=512 MB`, con `code=0` y `repair=False` intactos.
  - *Esfuerzo: medio · Depende de: —*

- [x] **[T6-06] «Etiqueta del volumen:» es el único encabezado con dos puntos** — **hecho (2026-08-17)**
  - **Área:** UI / i18n
  - **Ubicación:** `src/FormatDiskPro/Localization/Localization.cs` (`label.label`), `src/FormatDiskPro/UI/MainWindow.xaml`
  - **Qué hacer:** quitar los dos puntos en los cinco idiomas. «Sistema de archivos» y «Tamaño de unidad de
    asignación» —los otros dos encabezados de la misma tarjeta— no los llevan.
  - **Por qué:** son tres campos en fila; que uno puntúe distinto se ve.
  - **Verificado:** captura de la app — los tres encabezados sin dos puntos — y una prueba por idioma que
    falla si alguno vuelve a terminar en `:` (también cubre el francés, donde sería « :» con espacio).
  - *Esfuerzo: trivial · Depende de: —*

- [x] **[T6-07] Cada diálogo coloca sus botones de forma distinta** — **hecho (2026-08-17)**
  - **Área:** UI / consistencia
  - **Ubicación:** `src/FormatDiskPro/UI/HistoryDialog.xaml`, `src/FormatDiskPro/UI/MainWindow.Operations.cs` (diálogo de modo de chkdsk), `src/FormatDiskPro/UI/ConfirmDialog.xaml`, `src/FormatDiskPro/UI/HealthDialog.xaml`
  - **Qué hacer:** unificar el pie de los diálogos. Hoy *chkdsk* apila dos botones a todo el ancho y deja
    «Cancelar» alineado a la derecha; *Historial* pone tres a la izquierda y «Cerrar» a la derecha. Los
    anchos de diálogo también bailan bastante entre sí.
  - **Por qué:** son cuatro pantallas de la misma app y cada una enseña una gramática de botones distinta.
  - **Antes de tocar:** decidir **una** regla y escribirla (aquí o en `CONTEXT.md`), o el siguiente diálogo
    volverá a inventarse la suya.
  - **⚠️ El hallazgo estaba medio equivocado, y eso cambió la tarea.** Los botones apilados de *chkdsk*
    **no** son un descuido: con tres botones nativos en fila, WinUI truncaba «Comprobar y reparar» **sin
    puntos suspensivos** («Comprobar y repar»), y en PT/IT es peor. Estaba documentado en un comentario del
    propio código. Uniformarlos habría reintroducido un fallo real. Igual el *Historial*: sus tres botones
    NO cierran el diálogo, así que van en el contenido; abajo solo va lo que cierra.
  - **Lo que sí estaba mal era el ANCHO.** Siete diálogos con seis criterios: 360, 380, 400, 300–420,
    360–420 y uno **sin ninguno** (el de chkdsk, que se ajustaba a su texto y salía visiblemente más
    estrecho). Abrir dos seguidos hacía «saltar» la ventana. Ahora los fijan dos tokens compartidos,
    `DialogContentMinWidth`/`MaxWidth`, incluido el que se construye en código.
  - **Y la regla queda escrita** en `AppTheme.xaml`, junto a los tokens: qué va en los botones nativos (lo
    que cierra), qué va en el contenido (lo que no) y cuándo se apila (cuando el texto pueda truncarse),
    con el porqué de la excepción de chkdsk para que nadie la «arregle».
  - **Verificado:** captura de los diálogos — el de chkdsk ya tiene el mismo ancho que el resto.
  - *Esfuerzo: medio · Depende de: —*

- [x] **[T6-08] Los grupos deshabilitados se atenúan dos veces** — **hecho (2026-08-17)**
  - **Área:** UI / accesibilidad
  - **Ubicación:** `src/FormatDiskPro/UI/MainWindow.xaml`, `src/FormatDiskPro/UI/MainWindow.FormatOptions.cs`, `src/FormatDiskPro/UI/MainWindow.Preferences.cs`
  - **Qué hacer:** los sub-bloques (`WipePassesPanel`, `SmallFat32SizePanel`, `RestPanel`) llevan
    `Opacity="0.5"` en el panel **y** `IsEnabled="false"` en el control. Poner `IsEnabled` en el panel
    entero y quitar la opacidad.
  - **Por qué:** el ComboBox queda doblemente apagado, y la etiqueta —que ya usa el color terciario del
    tema— pierde contraste por una vía que el barrido de `SeverityPalette` **no mira**: la opacidad no es
    un color del inventario. El visual deshabilitado del tema sí está exento por norma y es el que Windows
    dibuja en el resto del sistema.
  - **Criterio de aceptación:** los tres bloques se ven igual entre sí deshabilitados, y ningún texto
    visible depende de un `Opacity` para atenuarse.
  - **Lo primero que intenté no compila:** en WinUI **un panel no se puede deshabilitar**. `IsEnabled` vive
    en `Control` y `Panel` deriva de `FrameworkElement`, así que `<StackPanel IsEnabled="False">` da
    `WMC0011` — al contrario que en WPF, donde `UIElement` sí lo tiene.
  - **Cómo quedó:** helper `SetSubOptionEnabled(on, labels, controls)`. El control se apaga con su propio
    `IsEnabled` (visual deshabilitado del tema, exento de contraste por serlo de verdad) y la etiqueta
    **aparte**, porque un `TextBlock` tampoco es un `Control` y se quedaría a pleno contraste, más viva que
    el desplegable de al lado: se le pone `TextFillColorDisabledBrush` y al reactivar `ClearValue`, para no
    dejarle un color clavado al cambiar de tema en caliente. `RestDetailPanel`, que antes no se atenúaba,
    entra en el mismo tratamiento.
  - **Verificado:** la app arranca (la búsqueda del pincel del tema ocurre en `SetControlsEnabled` al
    iniciar: si la clave no existiera, no arrancaría) y las pruebas de UI que ejercen el
    habilitar/deshabilitar siguen en verde.
  - *Esfuerzo: bajo · Depende de: —*

- [x] **[T6-09] «sólo NTFS»** — **hecho (2026-08-17)**
  - **Área:** i18n / redacción
  - **Ubicación:** `src/FormatDiskPro/Localization/Localization.cs` (`opt.compress`)
  - **Qué hacer:** «solo», sin tilde. La RAE la retiró en 2010.
  - **Cómo se hizo:** corregida, y con una prueba que **barre el diccionario entero** en vez de anclar esa
    cadena: es el tipo de detalle que vuelve solo al escribir texto nuevo, no al editar el existente.
  - *Esfuerzo: trivial · Depende de: —*

- [x] **[T6-10] *chkdsk* no explica en qué se diferencian sus dos opciones** — **hecho (2026-08-17)**
  - **Área:** UI / redacción
  - **Ubicación:** `src/FormatDiskPro/UI/MainWindow.Operations.cs`, `src/FormatDiskPro/Localization/Localization.cs`
  - **Qué hacer:** el diálogo ofrece «Solo comprobar» y «Comprobar y reparar» sin una línea que diga qué
    cambia. Añadir una descripción breve a cada opción: reparar exige acceso exclusivo al volumen (puede
    pedir desmontarlo o reiniciar) y puede tardar mucho más.
  - **Por qué:** es una elección que hoy se hace a ciegas, y la opción equivocada deja la unidad ocupada un
    buen rato. Que «Solo comprobar» sea ya el botón primario ayuda, pero no explica.
  - **Cómo se hizo:** cada opción pasa a ser un **«command link»** —título más una línea de explicación
    dentro del propio botón—, que es el patrón de Windows para elegir entre dos acciones que no se
    distinguen por el nombre. Encaja con que ya estuvieran apilados (ver `T6-07`).
  - **Detalle de accesibilidad:** al pasar el contenido de una cadena a un panel con dos textos, el nombre
    accesible dependía de cómo recorriera la automatización ese panel. Se fija explícito, y así el lector
    de pantalla lee también la explicación — que es justo lo que hace falta para elegir.
  - *Esfuerzo: bajo · Depende de: —*

- [x] **[T6-12] Los números siguen a Windows y el texto al idioma de la app** — **hecho (2026-08-17)**
  - **Área:** i18n / UI
  - **Ubicación:** `src/FormatDiskPro/Core/FormatLogic.cs` (`FormatBytes`), `src/FormatDiskPro/UI/HealthDialog.xaml.cs`, y cualquier otro sitio que formatee números
  - **Qué hacer:** decidir —y aplicar en todas partes— si los números se formatean con la cultura del
    **sistema** o con la del **idioma elegido en la app**, que son cosas distintas porque la app deja
    cambiar el idioma sin tocar Windows. Hoy nadie fija `CurrentCulture` a partir de `L.Current`.
  - **Cómo se ve:** con la interfaz en español sobre un Windows en inglés sale `32,161 h (≈ 3.7 años)` —
    separadores ingleses con palabras españolas— y lo mismo lleva haciendo `FormatBytes` desde siempre
    (`223.6 GB`). En español debería ser `32.161 h (≈ 3,7 años)`.
  - **Origen:** apareció al hacer `T6-04`, que puso un decimal justo al lado de una palabra traducida y lo
    hizo visible. **No es una regresión de `T6-04`**: es anterior y afecta a toda la app, y `T6-04` se
    implementó igual que el resto (`CurrentCulture`) para no dejar dos criterios conviviendo.
  - **Cuidado:** lo que se **guarda** (`history.log`, CSV, comandos de PowerShell) debe seguir en
    `InvariantCulture` — esto es solo para lo que se **muestra**. Cambiarlo a lo bruto es exactamente cómo
    reaparecería `T1-01` (la guarda de disco de sistema bajo cultura turca).
  - **Decidido:** los números que se muestran siguen al **idioma elegido en la app**. Es lo coherente con
    que la app deje cambiar de idioma sin tocar Windows: si el texto cambia, el número también.
  - **Hecho:** nueva `L.Culture` (es-ES/en-US/pt-BR/fr-FR/it-IT), que `L.Set` actualiza junto al idioma.
    `FormatLogic.FormatBytes` la usa por defecto y acepta una cultura explícita; `HealthDialog` pasa de
    `CurrentCulture` a `L.Culture`. `Throughput.FormatSpeed` y `HistoryEntry.Humanize` la heredan por
    delegar en `FormatBytes`.
  - **Lo que NO se hizo, a propósito:** `L.Culture` **no** se asigna a `CultureInfo.CurrentCulture`. La
    cultura del hilo gobierna además comparaciones y mayúsculas, que es por donde volvería `T1-01`; hay
    una prueba que fija tr-TR, cambia el idioma y comprueba que el hilo no se ha movido.
  - **Lo que delató el fallo:** cuatro pruebas de la suite afirmaban el separador **inglés** con la app
    arrancando en español. Pasaban porque `FormatBytes` leía la cultura del hilo y el fixture la ponía
    invariante — medían el separador de la prueba, no el de la app. Ahora la cultura se dice en cada una.
  - **De propina:** la fecha del historial (`yyyy-MM-dd HH:mm`) pasa a invariante explícita — el patrón
    ya era ISO fijo, pero `-` y `:` son *marcadores* de separador y los ponía Windows.
  - *Esfuerzo: medio · Depende de: —*

### Completar la propia revisión

- [x] **[T6-11] Rehacer la revisión con la app en ejecución** — **hecho (2026-08-17)**
  - **Área:** QA / UX
  - **Ubicación:** `tools/capture-screenshots.ps1`, `docs/screenshots/gallery/`
  - **Qué hacer:** desde **terminal elevada**, `.\tools\capture-screenshots.ps1 -Gallery` y revisar lo que
    la ronda del 2026-08-17 no pudo ver: el bloque de ocupación nuevo, y los diálogos de Presets, Acerca
    de, Novedades, Licencia y Terceros. Refrescar de paso `docs/screenshots/gallery/`, que es de julio.
  - **Por qué:** la revisión que abre este tier se hizo sobre fotos previas al último cambio de la tarjeta
    *Unidad*, y sin poder abrir la mitad de los diálogos. Sus 10 hallazgos son válidos —cada uno está
    contrastado contra el código—, pero **la cobertura no fue completa y conviene que eso quede dicho**.
  - **Hecho:** 26 tomas (13 pantallas × 2 temas) con la USB conectada, ninguna omitida.
    `docs/screenshots/gallery/` **está en `.gitignore`**, así que refrescarla no mete 26 binarios en el
    repo: es material de revisión local, no del proyecto.
  - **Confirmado en ejecución:** el bloque de ocupación en los dos temas (en claro, relleno `#5C5C5C` sobre
    pista `#E0E0E0`: se distinguen sin esfuerzo), `T6-01` («Confirmar reinicialización»), `T6-02` (el campo
    se ve vacío), `T6-06`, `T6-07` y `T6-10`.
  - **Y encontró tres cosas que la primera ronda no podía ver** — `T6-13`, `T6-14` y `T6-15`. Que ampliar la
    cobertura produjera hallazgos nuevos es la señal de que esta tarea hacía falta.
  - *Esfuerzo: bajo (pero requiere elevación y sesión de escritorio) · Depende de: —*

---

### Abiertas —y cerradas— por la revisión completa (`T6-11`)

- [x] **[T6-13] *Novedades* enseña Markdown en crudo y con los párrafos rotos** — **hecho (2026-08-17)**
  - **Área:** UI / `Core`
  - **Ubicación:** `src/FormatDiskPro/Core/ReleaseNotes.cs`
  - **Qué hacer:** dos cosas en el mismo conversor. **(1)** `ToPlainText` quita `**` y `__` pero **no la
    cursiva de un solo asterisco**, así que el diálogo muestra `*Reinicializar unidad → …*` con los
    asteriscos a la vista. **(2)** Conserva los saltos de línea del Markdown original, que viene ajustado a
    ~100 columnas; el diálogo ajusta encima y salen párrafos partidos a mitad de frase («…para actualizar
    el / BIOS de una placa base…»). Dentro de un párrafo, un salto simple debe pasar a espacio — que es lo
    que significa en Markdown.
  - **Por qué importa más de lo que parece:** es la **primera pantalla que se ve tras actualizar**.
  - **Cuidado:** quitar todos los `*` a lo bruto se llevaría también los de un texto legítimo, y desenvolver
    a lo bruto pegaría viñetas y encabezados. `ReleaseNotesTests` ya existe: ampliarlo.
  - **Hecho:** dos regex de énfasis simple que exigen marcador **pareado y pegado a un no-espacio** (el
    subrayado pide además que no haya letra alrededor, para no partir `nombres_asi`), y un desenvolvido
    que cierra bloque solo en línea en blanco, encabezado o salto forzado de Markdown (dos espacios al
    final) — así una viñeta no se pega a la siguiente pero su continuación ajustada sí se le une.
  - **+9 pruebas**, tres de ellas guardando el «cuidado»: `2 * 3 = 6`, `notas_de_version_final` y un
    asterisco suelto salen intactos. Verificado por reversión (5 rojas con el fallo puesto) y a ojo en
    la captura de *Novedades*.
  - *Esfuerzo: bajo · Depende de: —*

- [x] **[T6-14] Los textos legales no caben en el ancho del diálogo** — **hecho (2026-08-17)**
  - **Área:** UI
  - **Ubicación:** `src/FormatDiskPro/UI/LegalTextDialog.xaml`
  - **Qué hacer:** *Licencia* y *Avisos de terceros* se pintan en monoespaciada y su fuente viene ajustada a
    ~72-80 columnas, pero en el diálogo entran unas ~60: cada línea larga se parte, y hasta las líneas de
    guiones separadoras salen cortadas en dos. Elegir una de tres: bajar el tamaño de letra, dar a este
    diálogo un ancho propio (documentando por qué se sale del común que fijó `T6-07`), o permitir
    desplazamiento horizontal.
  - **Por qué:** es texto legal. Se lee mal, pero **no puede verse alterado**: la salida NO es reajustar el
    contenido.
  - **Hecho: las tres a la vez, porque ninguna basta sola.** Medidos los ficheros reales (`LICENSE` 78
    columnas como mucho, `THIRD-PARTY-NOTICES.txt` 81): `TextWrapping="NoWrap"` para no alterar la
    maquetación, un `LegalDialogContentWidth` propio de 430 —la única excepción declarada al ancho común
    de `T6-07`, documentada en `AppTheme.xaml` y en el propio diálogo—, cuerpo a 10 px (que es una medida,
    no un gusto: a 10 px de Consolas entran ~78 columnas en 430) y desplazamiento horizontal de red.
    Resultado: la GPL entera —674 líneas— se lee sin tocar la barra.
  - **El primer intento fue peor que el problema.** Con `NoWrap` a 11 px el texto salía **cortado** al
    llegar al borde, sin barra visible: cambié un ajuste feo por una truncación silenciosa. Lo cazó la
    captura, no el razonamiento — por eso `T6-11` existe.
  - **Y las 3 líneas que seguían sin caber eran nuestras**, no de nadie: `THIRD-PARTY-NOTICES.txt` es el
    documento de atribución del proyecto, así que se reajustó a 78 columnas. El texto MIT que cita y la
    GPL **no se tocaron** — eso era justo lo prohibido.
  - *Esfuerzo: bajo · Depende de: T6-07*

- [x] **[T6-15] Los resúmenes de confirmación llevan saltos de línea fijos** — **hecho (2026-08-17)**
  - **Área:** i18n / UI
  - **Ubicación:** `src/FormatDiskPro/Localization/Localization.cs` (`reinit.summary*`)
  - **Qué hacer:** esas cadenas traen `
` incrustados para maquetar y el `TextBlock` ajusta por su cuenta
    encima. Resultado: «…de la unidad I: (todas sus / particiones) / y se recreará…», partido donde no toca
    —y en un sitio distinto en cada idioma, porque la frase no mide lo mismo—. Dejar solo los saltos que
    separan **párrafos** y que del ajuste se encargue el control.
  - **Por qué:** es el texto que hay que leer **antes de borrar un disco entero**.
  - **Hecho:** fuera el salto de maquetación en las tres claves × 5 idiomas; quedan los que separan
    párrafos y los que abren un elemento de la lista numerada de `reinit.summaryTwoPartitions`, que sí
    son estructura. La prueba no ancla las cadenas: recorre **cada `
` de los 15 textos** y falla si no
    es una de esas dos cosas, así que también caza el que se cuele en una traducción futura.
  - *Esfuerzo: bajo · Depende de: —*

---

## 🧭 Tier 7 — Consistencia y descubribilidad de la UI

> **Ni auditoría ni Tier 6.** El [Tier 6](#-tier-6--refinado-de-uxui) cerró la clase de fallos «la
> interfaz **afirma algo que no es cierto**»: títulos equivocados, datos en crudo, un valor que mentía.
> Lo que abre este tier es de otra naturaleza y no se ve en una captura: **acciones destructivas que no
> se comportan igual entre sí**, opciones que se ofrecen para rechazarse después, y trabajo que la
> interfaz obliga a hacer a mano. Nada de esto es un defecto de corrección; todo es la app pidiendo al
> usuario más atención de la que su tarea merece.

**Origen (2026-08-25):** revisión de UX/UI sobre el código de `src/FormatDiskPro/UI/` con el Tier 6 ya
cerrado, buscando expresamente lo que una galería de capturas **no** enseña: qué pasa al pulsar, qué se
ofrece y luego se niega, y qué hay que repetir a mano.

- [x] **[T7-01] Borrar un preset no pide confirmación ni se puede deshacer** — **hecho (2026-08-25)**
  - **Área:** UI / consistencia de las acciones destructivas
  - **Ubicación:** `src/FormatDiskPro/UI/PresetsDialog.xaml`, `PresetsDialog.xaml.cs`, `Localization.cs`
  - **Qué hacer:** `DeleteBtn_Click` hacía `Remove` + `Persist()` en **un clic**, sin confirmar y sin
    deshacer, en una fila de cuatro botones de icono donde la papelera está pegada a «Editar». En el
    mismo producto, *Vaciar historial* —la otra acción destructiva sin deshacer— sí confirma. Reusar ahí
    el patrón que ya existe: un `Flyout` dentro del contenido, porque un `ContentDialog` no puede abrir
    otro.
  - **Por qué:** no es pérdida de datos del disco, pero sí de configuración que el usuario escribió a
    mano; y sobre todo, **dos acciones igual de irreversibles no pueden pedir cosas distintas**.
  - **Hecho:** flyout de confirmación con el **nombre del preset dentro** (`preset.deleteConfirm` × 5
    idiomas) — en una lista de papeleras idénticas, «¿Eliminar?» a secas no dice cuál se va a perder. El
    flyout se guarda al abrirse (`Opening`) porque su contenido vive en un `Popup` y desde el botón que
    confirma no se sube hasta él por el árbol visual. +2 unitarias (el marcador `{0}` en los cinco).
  - *Esfuerzo: bajo · Depende de: —*

- [x] **[T7-03] El campo más esotérico es el único sin ayuda** — **hecho (2026-08-25)**
  - **Área:** UI / prevención de errores
  - **Ubicación:** `src/FormatDiskPro/UI/MainWindow.xaml`, `MainWindow.Preferences.cs`, `Localization.cs`
  - **Qué hacer:** el sistema de archivos lleva descripción bajo su combo desde `T1-05` (`fs.desc.*`,
    cinco idiomas). *Tamaño de unidad de asignación* no llevaba ninguna, y es el que menos gente sabe
    elegir. Una pista con el mismo `HintTextStyle`, en el mismo sitio.
  - **Por qué:** la ayuda está donde no hace falta y falta donde sí. Quien no sabe qué es un clúster no
    tiene forma de averiguarlo desde la app.
  - **Hecho:** `alloc.hint` × 5 idiomas bajo `AllocUnitPicker`. **No nombra ninguna opción de la lista**:
    el combo se puebla con tamaños concretos (`4 KB`, `64 KB`) y el recomendado llega *preseleccionado*,
    así que no hay ningún elemento «Predeterminado» al que mandar al usuario — una prueba barre las cinco
    traducciones y falla si alguna lo inventa.
  - *Esfuerzo: bajo · Depende de: —*

- [x] **[T7-05] La búsqueda del historial no dice cuánto está ocultando** — **hecho (2026-08-25)**
  - **Área:** UI / historial
  - **Ubicación:** `src/FormatDiskPro/UI/HistoryDialog.xaml`, `HistoryDialog.xaml.cs`, `Localization.cs`
  - **Qué hacer:** el buscador era un `TextBox` sin botón de limpiar, con dos filtros más al lado y sin
    ningún recuento. Es fácil quedarse con una lista corta sin saber por qué.
  - **Por qué:** el estado vacío ya distingue *sin historial* de *sin coincidencias* (`T6-05`), que es la
    mitad difícil; lo que faltaba es el caso intermedio — hay resultados, pero no todos.
  - **Hecho:** `AutoSuggestBox` (trae el botón de limpiar; se le apaga la lista de sugerencias, que no
    tiene qué sugerir) con **nombre accesible propio**, no el placeholder — la lección de `T6-02` —, y un
    recuento «12 de 340» que se oculta con el historial vacío, porque ahí el estado vacío ya lo dice con
    palabras. Los dos números se formatean con `L.Culture` y llegan ya formateados a `L.T`: `string.Format`
    los pondría en la cultura de Windows y volvería a mezclar separadores ingleses con texto español
    (`T6-12`). +2 unitarias (los marcadores `{0}`/`{1}` en los cinco idiomas).
  - *Esfuerzo: bajo · Depende de: —*

### Hechas en la segunda tanda

- [x] **[T7-02] El menú *Herramientas* ofrecía lo que luego rechazaba** — **hecho (2026-08-25)**
  - **Área:** UI / prevención de errores
  - **Ubicación:** `src/FormatDiskPro/UI/MainWindow.DriveInfo.cs`, `MainWindow.xaml.cs`, `MainWindow.Preferences.cs`, `Localization.cs`
  - **Qué hacer:** `SetControlsEnabled` habilitaba `MnuTools` **en bloque**, así que la incompatibilidad se
    descubría **después** del clic, en un diálogo: reinicializar sobre una unidad no extraíble
    (`reinit.onlyRemovable`), expulsar un disco fijo, verificar capacidad o quitar la protección sobre una
    unidad protegida. Decidirlo por unidad y apagar el ítem **diciendo el motivo**.
  - **Por qué:** el diálogo llega tarde: el usuario ya eligió. Pero **las dos mitades van juntas o no va
    ninguna** — un ítem gris sin explicación es *peor* que el diálogo, porque deja al usuario sin saber qué
    hizo mal.
  - **Hecho:** `UpdateToolsMenuAvailability()` en un solo sitio, con las condiciones copiadas **una a una**
    de las guardas de `Operations.cs`, que **siguen ahí**: entre abrir el menú y pulsar, la unidad puede
    cambiar (`WM_DEVICECHANGE`). Se recalcula al cambiar de unidad, al terminar una operación y **al
    cambiar de idioma** (los motivos se escriben en el ítem, así que si no se reescriben se quedan en el
    idioma anterior). *Comprobar errores* y *Benchmark* no se apagan nunca con una unidad seleccionada:
    chkdsk en solo lectura sí corre sobre el disco de sistema —lo que no se ofrece allí es la reparación—
    y el benchmark no escribe fuera de su archivo temporal. +1 UI test sobre el disco de sistema (la única
    unidad que hay en cualquier máquina, así que no necesita la USB) que comprueba las dos listas **y** que
    ningún ítem apagado se queda sin `HelpText`.
  - **Ojo, lo que NO está verificado:** el motivo va en `ToolTipService` **y** en `HelpText`. El `HelpText`
    está comprobado por el test (es lo que lee un lector de pantalla); que WinUI **pinte** el tooltip sobre
    un `MenuFlyoutItem` deshabilitado no se ha podido confirmar — una sonda con FlaUI no detectó tooltip
    tampoco sobre un control habilitado que sí lo tiene, así que la sonda no vale como prueba de nada. Va a
    `T7-06`, que es la tarea de mirar la app con las manos.
  - *Esfuerzo: medio · Depende de: —*

- [x] **[T7-04] La app tenía un solo atajo de teclado** — **hecho (2026-08-25)**
  - **Área:** UI / eficiencia
  - **Ubicación:** `src/FormatDiskPro/UI/MainWindow.xaml`, `MainWindow.Preferences.cs`, `MainWindow.HelpAndUpdates.cs`, `HistoryDialog.xaml`
  - **Qué hacer:** `F5` (actualizar la lista) era el único `KeyboardAccelerator` de la app; los menús tienen
    `AccessKey` y ahí se acababa. Añadir atajos a lo que se usa a diario, **visibles** donde se aprenden.
  - **Por qué:** un atajo que no se anuncia no existe.
  - **Hecho:** `Ctrl+I` salud, `Ctrl+B` benchmark, `Ctrl+H` historial y `Ctrl+E` exportar CSV dentro del
    historial. **Solo diagnósticos que no escriben nada**: formatear, reinicializar, verificar capacidad y
    borrado seguro no llevan atajo a propósito — una combinación mal pulsada no puede ser el primer paso de
    algo que borra datos. No hizo falta `KeyboardAcceleratorTextOverride`: el `MenuFlyoutItem` pinta solo el
    texto del acelerador cuando hay uno de verdad (el *override* es para anunciar uno que no existe). El F5
    sí va escrito a mano en su tooltip, porque con un `ToolTip` explícito WinUI ya no añade el suyo.
  - **Y una consecuencia que había que atender:** `MnuHistory_Click` no comprobaba `_isBusy` —le bastaba con
    que el menú entero se deshabilitara durante una operación—, y `Ctrl+H` llega ahí **sin pasar por el
    menú**. Guarda explícita: un `ContentDialog` modal encima de un formateo tapa el progreso y el botón de
    cancelar.
  - **Verificado por reversión** sobre el `.exe` real: el UI test nuevo pulsa `Ctrl+H` **sin abrir el menú**
    y espera el diálogo; quitando el acelerador, falla. No era una obviedad — los `MenuFlyoutItem` de un
    `MenuBar` viven en un flyout que puede no haberse desplegado nunca.
  - *Esfuerzo: bajo · Depende de: —*

### Hecha con la app en marcha

- [x] **[T7-06] Rehacer la revisión con la app en ejecución** — **hecho (2026-08-25)**
  - **Área:** UI / QA
  - **Ubicación:** `tests/FormatDiskPro.UiTests/KeyboardAndNamingTests.cs`, `TestDriveFacts.cs`
  - **Qué hacer:** contestar lo que **no se resuelve leyendo código**: teclado en los `ListView` de
    historial y presets, foco inicial y orden de tabulación de los diálogos, y si el tooltip de un ítem
    de menú deshabilitado (`T7-02`) se ve.
  - **Por qué:** es la misma tarea que `T6-11`, y aquella abrió tres hallazgos que la revisión sobre
    código no podía ver. Que las cinco anteriores estén hechas cierra la mitad que se puede leer.
  - **Hecho:** se escribió una sonda que **no afirmaba nada** —solo recorría la app y contaba lo que
    veía— y con su salida delante se convirtió en cinco pruebas y se borró, que era la condición.
    Resultados:
    - **La sospecha de partida era falsa.** Con `SelectionMode="None"`, los `ListView` **sí** se
      recorren y se desplazan solo con teclado: en el historial, tres tabulaciones desde el buscador
      caen en una fila y ↓/AvPág mueven la lista (0 % → 30 %). No había nada que arreglar. Queda fijado
      por dos pruebas que **no dependen de ningún arreglo nuestro**: siguen verdes al revertirlo todo.
    - **Foco inicial correcto** en los dos diálogos (buscador; nombre del preset), y el flyout de
      borrado de `T7-01` **funciona con el teclado**: al abrirse el foco cae en el botón que confirma,
      y el tab se queda dentro del flyout.
    - **Y abrió `T7-07`**, con dos defectos que solo se ven con un lector de pantalla: las filas se
      anunciaban con el volcado del record y los dos filtros del historial no decían qué filtraban.
    - **El tooltip del ítem deshabilitado sigue sin respuesta**, y va aparte en `T7-08`: FlaUI no lo
      detecta **ni sobre un control habilitado que sí lo tiene** (se comprobó el ratón encima con
      `FromPoint`), así que la sonda no distingue «no hay tooltip» de «no sé verlo». Es una
      comprobación de ojo, de cinco segundos, y no se afirma nada mientras no se haga.
  - *Esfuerzo: medio · Depende de: T7-01 y T7-02*

### Abiertas por la revisión con la app en marcha (`T7-06`)

- [x] **[T7-07] Las filas se anunciaban con el volcado del record** — **hecho (2026-08-25)**
  - **Área:** UI / accesibilidad
  - **Ubicación:** `src/FormatDiskPro/UI/HistoryDialog.xaml(.cs)`, `PresetsDialog.xaml(.cs)`, `Localization.cs`
  - **Qué pasaba:** el `ListViewItem` de una fila del historial se anunciaba como
    `HistoryRow { Time = 2026-08-18 08:42, Title = …, Glyph = , Accent = Microsoft.UI.Xaml.Media.SolidColorBrush }`,
    y el de un preset como `FormatPreset { Name = …, AllocationUnit = 4096, …, NameKey =  }`: el
    `ToString()` del record, marca de clase y pincel incluidos. Pasa porque el contenido del ítem es un
    **objeto**, no texto. Además, los dos `ComboBox` de filtro del historial **no exponían nombre**: se
    anunciaban como «cuadro combinado» a secas, sin decir qué filtran — el buscador de al lado sí lo
    tenía desde `T7-05`.
  - **Por qué:** es exactamente lo que `T6-02` corrigió en el campo de confirmación (un lector de
    pantalla diciendo lo que no debía) y lo que `T2-01`/`T2-02` construyeron para las operaciones. La app
    se puede seguir a ciegas menos en sus dos listas.
  - **Hecho:** el nombre se pone en el **contenedor** vía `ContainerContentChanging` —dentro de la
    plantilla no cambia el del `ListViewItem`—: la fila del historial se anuncia «Formato · Correcto.
    2026-08-18 08:42. unidad=I: fs=exFAT» y la de un preset, con su nombre. Los dos filtros reciben
    `history.filter.catName`/`resName` × 5 idiomas. **Verificado por reversión**: quitando los tres
    arreglos, las tres pruebas de nombres fallan y las dos de teclado siguen verdes.
  - *Esfuerzo: bajo · Depende de: T7-06*

- [x] **[T7-08] El tooltip de un ítem de menú deshabilitado NO se ve** — **hecho (2026-08-26)**
  - **Área:** UI / prevención de errores
  - **Ubicación:** `src/FormatDiskPro/UI/MainWindow.DriveInfo.cs` (`SetMenuItemAvailability`),
    `src/FormatDiskPro/UI/MainWindow.Preferences.cs` (`ApplyLanguage`),
    `src/FormatDiskPro/Localization/Localization.cs`
  - **La respuesta, mirando la pantalla:** con el disco de sistema seleccionado y el ratón encima de
    *Reinicializar unidad…*, **no aparece nada**. WinUI no tiene el `ShowOnDisabled` de WPF: un control
    deshabilitado no recibe eventos de puntero, así que su tooltip no se pinta nunca. El motivo que
    `T7-02` escribió solo le llegaba a un lector de pantalla.
  - **Hecho:** el motivo, **en corto, pegado al texto visible del ítem** — «Reinicializar unidad…
    (unidad protegida)», «Expulsar unidad (solo extraíbles)». Tres claves nuevas
    (`menu.tagNoDrive`/`tagProtected`/`tagRemovable`) × 5 idiomas, junto a las `menu.why*` de `T7-02`,
    que **se quedan**: la etiqueta cabe en un menú, la frase completa dice el porqué y va donde ya iba
    (tooltip + `HelpText`).
  - **No fue la `InfoBar`**, que era lo que la tarea proponía: el flyout de *Herramientas* se abre
    justo encima de la fila donde vive `ProtectedBar`, así que el aviso habría salido **debajo del menú
    que lo motiva**. Y el motivo es por ítem, no por ventana: una barra tendría que resumir hasta tres
    razones distintas y perdería a cuál corresponde cada una. La etiqueta va donde está el problema.
  - **Efecto colateral que hubo que resolver:** el texto de esos siete ítems tenía **dos dueños**
    —`ApplyLanguage` y `UpdateToolsMenuAvailability`—, y con la etiqueta dentro eso se vuelve un error:
    según cuál escribiera el último, la etiqueta se perdía o se acumulaba. `ApplyLanguage` deja de
    escribirlos (ya llamaba a `UpdateToolsMenuAvailability` al final), y el texto se re-deriva **siempre**
    de la clave de localización, nunca del que el ítem trae puesto.
  - **Verificado por reversión:** quitando la etiqueta del `Text`, la prueba de `T7-02` —ampliada con la
    comprobación del texto visible— falla; con ella, verde. +6 unitarias sobre las etiquetas en los cinco
    idiomas (que sean cortas y entre paréntesis, y que la frase larga siga diciendo más que ellas).
  - *Esfuerzo: bajo (mirar) · Depende de: —*

### Abierta al mirar el resultado de `T7-08`

- [x] **[T7-09] El foco salía recortado en los diálogos** — **hecho (2026-08-26)**
  - **Área:** UI / accesibilidad
  - **Ubicación:** `src/FormatDiskPro/UI/Theme/AppTheme.xaml` y la raíz de los seis diálogos
  - **Qué pasaba:** al tabular hasta el primer filtro del *Historial*, su marco de foco salía **cortado
    por la izquierda**. WinUI dibuja ese marco **hacia fuera** de los límites del control (2 px de trazo
    primario + 1 px de secundario) y el `ContentDialog` envuelve su contenido en un `ScrollViewer` que
    recorta: cualquier control pegado al borde de la raíz pierde el lado que cae fuera.
  - **Hecho:** un recurso compartido, `DialogContentPadding` = 3 px —exactamente lo que el trazo
    necesita—, aplicado a la raíz del contenido de los seis diálogos. Va **dentro** de
    `MinWidth`/`MaxWidth`, así que no cambia el ancho de ninguno ni toca lo que fijó `T6-07`.
  - **No era solo el historial**, y por eso el arreglo no vive ahí: los seis diálogos ponían su raíz
    pegada al borde. Lo mismo le pasaba al buscador y a la fila de botones; solo se notó en el combo
    porque su marco es el más visible.
  - **La única excepción, declarada:** `LegalTextDialog`, cuyo ancho es el valor **medido** en `T6-14`
    para que quepan las 78 columnas de la GPL sin barra horizontal — un relleno le comería 6 px de esa
    cuenta y volvería a partir el texto legal. Su raíz es además un `ScrollViewer` a pantalla completa:
    no hay ningún control tabulable pegado a su borde.
  - **Por qué:** un foco a medio dibujar es exactamente el problema que el resto del Tier 7 vino a
    arreglar —la app sabiendo algo que no enseña—, y quien navega a teclado es quien más depende de él.
  - **Verificado por reversión:** quitando el relleno de un diálogo, la prueba que barre los seis falla
    y nombra el fichero. La prueba defiende **la convención, no los píxeles**: que el recorte ya no se ve
    es una comprobación de ojo, como la de `T7-08`.
  - *Esfuerzo: bajo · Depende de: T7-08*

---

## 🔍 Tier 8 — Lo que solo se ve usando la app

> **Ni Tier 6 ni Tier 7.** El [Tier 6](#-tier-6--refinado-de-uxui) miró **capturas** y el
> [Tier 7](#-tier-7--consistencia-y-descubribilidad-de-la-ui) miró **código**. Este tier salió de algo que
> ninguno de los dos podía dar: **una captura de la app en uso real**, con su historial lleno. Ahí se veían
> cuatro entradas seguidas que decían `EXPORT ERROR:` y nada más.
>
> Y a diferencia del Tier 7, aquí **sí hay defectos de corrección**. El primero es grande: una función del
> menú que **nunca funcionó en ninguna versión publicada**, y que ninguna prueba cubría.

**Origen (2026-08-26):** una captura del historial durante la revisión del Tier 7.

- [x] **[T8-01] *Exportar CSV* nunca funcionó en la app publicada** — **hecho (2026-08-26)**
  - **Área:** Corrección / historial
  - **Ubicación:** `src/FormatDiskPro/UI/SaveFileDialog.cs` (nuevo), `UI/HistoryDialog.xaml.cs`
  - **Qué pasaba:** `FileSavePicker.PickSaveFileAsync()` —el selector de archivos de WinRT— **rechaza a
    los procesos elevados**, y FormatDiskPro corre siempre elevada (`requireAdministrator`). Lanzaba
    `COMException 0x80004005` **en el acto**, sin llegar a mostrar ninguna ventana. El botón parecía no
    hacer nada.
  - **Medido, no supuesto:** una sonda de UI pulsó el botón contra el .exe real y enumeró las ventanas del
    proceso por `EnumWindows` — ninguna nueva, y la `InfoBar` con el HRESULT. La misma sonda, ya con el
    arreglo, ve aparecer la ventana `Exportar CSV` de clase `#32770`, la de los diálogos comunes de
    Windows.
  - **Hecho:** el diálogo «Guardar como» de Windows por COM (`IFileSaveDialog`), que no pasa por el
    intermediario que rechaza la elevación y es el mismo diálogo moderno del resto del sistema. La
    escritura pasa de `FileIO.WriteTextAsync` a `File.WriteAllTextAsync` con **UTF-8 con BOM**, que es lo
    que escribía antes: sin BOM, Excel destroza los acentos de los detalles.
  - **Por qué se nos escapó:** *no había ninguna prueba de la exportación*, ni unitaria ni de UI. La había
    de `HistoryEntry.ToCsv` —la parte pura— pero ninguna del camino que el usuario pulsa. Ahora sí.
  - *Esfuerzo: alto · Depende de: —*

- [x] **[T8-02] Los errores podían salir vacíos** — **hecho (2026-08-26)**
  - **Área:** Corrección / diagnóstico
  - **Ubicación:** `src/FormatDiskPro/Core/ErrorText.cs` (nuevo) y los once sitios que mostraban o
    registraban el mensaje de una excepción
  - **Qué pasaba:** las cuatro líneas `EXPORT ERROR:` del historial no estaban truncadas — la `Message` de
    esa excepción era **de verdad la cadena vacía**. Una excepción que cruza la frontera de WinRT lleva su
    texto en un `IRestrictedErrorInfo`, y cuando ese descriptor viene sin descripción, lo que llega a .NET
    es un mensaje en blanco. El `InfoBar` mostraba título sin cuerpo.
  - **Hecho:** `ErrorText.Describe(ex)` — el mensaje si lo hay (recortado), y si no, el tipo y el
    `HRESULT`. Es lógica pura, así que se prueba de verdad. Lo usan los once sitios que enseñan o
    registran un error, incluida la línea de historial de un formateo fallido (`OperationFailure`), que es
    la más importante del archivo.
  - **Y una prueba que barre las fuentes** y falla si vuelve a aparecer el mensaje en crudo fuera de
    `ErrorText`. No es estilo: cada uno de esos sitios podía escribir un error vacío, y el fallo estuvo ahí
    años sin que ninguna prueba lo notara porque el código «se leía bien».
  - **Fue lo que diagnosticó `T8-01`:** con el respaldo puesto, la app dijo `COMException (HRESULT
    0x80004005)` en pantalla, y eso señaló directamente al selector de archivos.
  - *Esfuerzo: medio · Depende de: —*

- [x] **[T8-03] Dos botones más que podían no hacer nada** — **hecho (2026-08-26)**
  - **Área:** Corrección / prevención de errores
  - **Ubicación:** `Services/History.cs`, `Services/UpdateService.cs`, `UI/HistoryDialog.xaml.cs`,
    `UI/AboutDialog.*`, `UI/WhatsNewDialog.*`, `UI/MainWindow.HelpAndUpdates.cs`
  - **Qué pasaba:** buscando más fallos de la familia de `T8-01` aparecieron dos `catch` vacíos.
    `History.Open()` —el botón *Abrir archivo* del historial— y `UpdateService.OpenUrl()` —*Apoyar el
    proyecto*, *GitHub*, *Ver en GitHub*— se tragaban cualquier fallo: sin editor asociado a `.log` o sin
    navegador, el botón no producía **ningún** efecto visible.
  - **Hecho:** `Open()` deja salir la excepción y el diálogo la cuenta en la `InfoBar` que ya tenía;
    `OpenUrl()` pasa a devolver `bool` y quien llama enseña la dirección para poder copiarla. *Ver en
    GitHub* sigue cerrando el diálogo cuando el navegador **sí** abre —que es lo que se espera— y solo se
    queda abierto cuando hay algo que contar.
  - **Por qué:** un botón que no hace nada es el peor fallo posible de una interfaz: no da al usuario
    ninguna forma de saber si el problema es suyo, del programa o del clic.
  - *Esfuerzo: bajo · Depende de: T8-02*

### Abiertas por la publicación de la v1.24.0

- [x] **[T8-04] El corte publicó unas notas que no contaban nada** — **hecho (2026-08-26)**
  - **Área:** Publicación / `release.ps1`
  - **Ubicación:** `release.ps1` (bloque *Notas del release*)
  - **Qué pasaba:** sin `-NotesFile`, el script generaba una **plantilla genérica** —«Instalador
    self-contained para Windows x64…» y poco más— y la publicaba como cuerpo del release. Así salió la
    v1.24.0: el corte terminó **en verde**, con sus 604 pruebas y su 98,1 % de cobertura, y el release no
    mencionaba ni uno solo de sus cambios. Es la peor clase de fallo de un script de publicación: no
    avisa.
  - **Hecho:** sin `-NotesFile`, las notas salen de **la sección del CHANGELOG de esa versión**, más el
    pie del instalador y el `.sha256`. Esa sección ya es **obligatoria** —el script aborta si falta—, así
    que está escrita y revisada: olvidarse las notas deja de ser posible. `-NotesFile` sigue mandando
    cuando se pasa, porque un registro por versión y unas notas de publicación pueden querer contar lo
    mismo de otra forma.
  - **Y el plan del `-DryRun` dice de dónde saldrán**, que es donde se habría visto antes de publicar.
  - *Esfuerzo: bajo · Depende de: —*

- [x] **[T8-05] *Novedades* enseñaba las almohadillas del encabezado** — **hecho (2026-08-26)**
  - **Área:** Corrección / novedades
  - **Ubicación:** `src/FormatDiskPro/Core/ReleaseNotes.cs`
  - **Qué pasaba:** la pantalla de *Novedades* de la v1.24.0 mostraba «`## FormatDiskPro v1.24.0`» con las
    almohadillas a la vista, pese a que `ToPlainText` quita los encabezados desde siempre. La culpa era de
    un carácter invisible: el cuerpo del release empezaba por una **marca de orden de bytes** (`U+FEFF`),
    que `Out-File -Encoding utf8` de PowerShell 5.1 escribe y que viaja intacta hasta la API de GitHub.
    Con ella delante, el `#` ya no estaba al principio de su línea y la expresión regular no lo veía.
  - **Lo traicionero:** `U+FEFF` **no es espacio en blanco** para .NET —es categoría `Cf`, no `Zs`—, así
    que ni `\s` ni `Trim()` lo tocan. Un texto que se ve idéntico se comporta distinto.
  - **Hecho:** se quita la marca —**todas**, no solo la primera— antes de cualquier otra cosa. Arreglado
    también el origen (`T8-04` escribe ya sin BOM), pero el conversor no puede fiarse de eso: el cuerpo de
    un release puede venir de cualquier sitio, incluido un editor que lo guarde con marca.
  - **Verificado por reversión:** quitando el reemplazo, caen las tres pruebas nuevas.
  - *Esfuerzo: bajo · Depende de: T8-04*

- [x] **[T8-06] La cobertura fallaba «al azar», y no era al azar** — **hecho (2026-08-26)**
  - **Área:** Publicación / `release.ps1`
  - **Ubicación:** `release.ps1` (bloque de pruebas)
  - **Qué pasaba:** el corte moría con «Se pidió cobertura y no se obtuvo informe. […] revisa que
    coverlet.collector siga referenciado», que apunta a un paquete que falta — y no faltaba nada. El
    informe estaba ahí y estaba **vacío**: 235 bytes con `<packages />`. Cuando la compilación no está al
    día, coverlet instrumenta los ensamblados que encuentra y **MSBuild los sobrescribe acto seguido** con
    los recién compilados, así que no queda nada instrumentado que medir.
  - **Y no era «al azar»:** pasa **siempre** que hay código sin compilar, que es exactamente la situación
    de cualquier corte real —se publica justo después de tocar código—. Medido: informe vacío 2 de 2 veces
    con cambios sin compilar, 1,6 MB 4 de 4 con la compilación al día. La primera vez se descartó como
    transitorio porque al repetir salió bien; lo que pasaba es que la primera corrida había dejado la
    compilación hecha.
  - **Hecho:** `dotnet build` de la solución **antes** de medir, y `dotnet test --no-build` después.
  - **Verificado bajo la condición que fallaba:** se tocó una fuente a propósito para desactualizar la
    compilación y el ensayo pasó con 98,1 %.
  - **Por qué importa más de lo que parece:** el mensaje culpaba al paquete equivocado. Un fallo de
    herramienta que señala mal se arregla dos veces: una buscando donde no es, y otra donde sí.
  - *Esfuerzo: bajo · Depende de: —*

---

## 🛠️ Tier 9 — Re-auditoría transversal con la app en marcha *(abierto 2026-08-26)*

> **CERRADO el 2026-08-26, 20/20** — el mismo día que se abrió. El título conserva la fecha de apertura
> porque cuatro enlaces de este archivo y de `CONTEXT.md` apuntan a este ancla.

> **De dónde sale.** Una re-auditoría de las 12 áreas aplicables (las 13 menos SEO; dentro del área 6,
> la responsividad web no aplica) ejecutada **sobre la máquina**, no solo leyendo: build, 607 unitarias con cobertura,
> los 37 UI tests con la USB conectada, la galería de capturas completa y medición de arranque y
> contraste. Con los Tiers 0–8 cerrados, **es una tanda temática, no un nivel de severidad**: la
> severidad va marcada en cada tarea y hay una **Alta** (`T9-01`).
>
> **Base de la revisión:** v1.24.1 · build **0 advertencias / 0 errores** · unitarias **606 pasan /
> 1 omitida / 0 fallan** (607) · `Core/` al **97,9 %** · UI **34 pasan / 3 omitidas (opt-in) / 0 fallan**
> de 37 con `Category!=Slow`, USB `utilidades` conectada · todo medido el 2026-08-26.
>
> **Lo que la revisión NO encontró**, y consta para no repetirla: ninguna ruta de inyección de comandos
> (las tres vías nuevas que se persiguieron —presets, plan de particiones y etiqueta— están cerradas por
> lista blanca y se verificaron una a una), ningún fallo de contraste (cuatro pares **medidos** sobre los
> PNG: 5,1 · 8,11 · 6,94 · 5,49 : 1, todos ≥ 4,5), ningún defecto de rendimiento (**578 ms** de media
> hasta ventana visible) y ninguna incorrección en la tipografía francesa. Ver *Áreas auditadas sin
> hallazgos*.

### Corte de versión (`release.ps1`)

- [x] **[T9-01] El corte no comprueba el árbol sucio, y publica lo que haya en él** · **Alta**
  - **Área:** DevOps
  - **Ubicación:** [release.ps1:338](release.ps1#L338) (solo mira `^??`) · [release.ps1:552](release.ps1#L552) (`git add -u`)
  - **Qué hacer:** la validación previa solo rechaza archivos **sin rastrear**; los archivos rastreados
    **modificados** no bloquean nada, y `git add -u` los barre luego al commit `release: vX.Y.Z`. Peor: el
    instalador se compila **desde el árbol de trabajo**, así que el binario publicado puede no
    corresponder al commit etiquetado. Añadir la comprobación de modificados (`git status --porcelain`
    sin filtrar a `??`) bajo la misma bandera `-AllowDirty`, que es lo que su propia ayuda ya promete
    («Permite continuar con cambios sin commitear en el árbol de trabajo»).
  - **Criterio de aceptación:** con un archivo rastreado modificado y sin `-AllowDirty`, el corte aborta
    nombrándolo; con `-AllowDirty`, avisa y continúa. `-DryRun` lo refleja en el plan.
  - **Esfuerzo:** bajo
  - **Depende de:** ninguna

- [x] **[T9-02] Al fallar el release, el script aconseja un reintento que él mismo rechaza** · Media
  - **Área:** DevOps
  - **Ubicación:** [release.ps1:607](release.ps1#L607) y [release.ps1:571](release.ps1#L571) frente a [release.ps1:331](release.ps1#L331)
  - **Qué hacer:** si `gh release create` falla, el mensaje dice «el tag ya está publicado; puedes
    reintentar el release»; pero al reintentar, la validación de la línea 331 aborta con «El tag ya existe
    localmente». El consejo y la guarda se contradicen. Detectar el caso «el tag ya existe **y** apunta a
    HEAD **y** no hay release publicado» y permitir retomar desde el paso 5, o bien decir en el mensaje
    los dos comandos exactos de borrado del tag (local y remoto) antes de reintentar.
  - **Criterio de aceptación:** tras un fallo simulado de `gh release create`, seguir literalmente lo que
    dice el mensaje deja el release publicado, sin pasos que el mensaje no nombre.
  - **Esfuerzo:** bajo
  - **Depende de:** ninguna

- [x] **[T9-03] `GH_TOKEN` sobrevive al corte en el entorno del proceso** · Baja
  - **Área:** Seguridad / DevOps
  - **Ubicación:** [release.ps1:601](release.ps1#L601)
  - **Qué hacer:** el token de la credencial cacheada se asigna a `$env:GH_TOKEN` y no se limpia nunca.
    Las variables `$env:` son del **proceso**, así que queda vivo en la consola tras terminar el script y
    lo hereda cualquier proceso lanzado después desde esa misma terminal. Limpiarlo en el `finally` que ya
    existe (junto al `Pop-Location`).
  - **Criterio de aceptación:** tras un corte que haya tenido que rellenar la credencial,
    `$env:GH_TOKEN` está vacía en la terminal que lo lanzó.
  - **Esfuerzo:** bajo
  - **Depende de:** ninguna

### Galería de capturas (`tools/capture-screenshots.ps1`)

- [x] **[T9-04] La unidad que elige por defecto hace imposibles 4 de las 26 tomas** · Media
  - **Área:** DevOps / QA
  - **Ubicación:** [tools/capture-screenshots.ps1:131-137](tools/capture-screenshots.ps1#L131)
  - **Qué hacer:** `Resolve-CaptureDrive` toma la **primera unidad no-sistema** —aquí `D:`, fija y de
    223,6 GB—. Sobre ella, *FAT32* no se oferta (Windows lo limita a 32 GB) y *Reinicializar* sale
    deshabilitado (solo extraíbles, `T7-02`), así que `main-fat32` y `reinit` **no pueden salir en ningún
    tema**. Preferir una unidad **extraíble** y, si la hay, una de ≤ 32 GB; o declarar la precondición por
    toma y elegir unidad por toma.
  - **Verificado:** con `-Drive I` (extraíble, 27,3 GB) las dos tomas salen a la primera. Es la
    configuración, no las tomas.
  - **Criterio de aceptación:** `-Gallery` sin argumentos produce las **26** tomas en una máquina con la
    USB de pruebas conectada.
  - **Esfuerzo:** bajo
  - **Depende de:** ninguna

- [x] **[T9-05] La galería confunde «omitida» con «correcta» — la lección de `T2-12`, sin aplicar aquí** · Media
  - **Área:** DevOps / QA
  - **Ubicación:** [tools/capture-screenshots.ps1:462](tools/capture-screenshots.ps1#L462) y `Capture-GalleryShot`
  - **Qué hacer:** una toma que falla emite un `Write-Warning` y la corrida sigue hasta «Galería
    completada», sin recuento. Quien la usa para auditar recibe 22 PNG y **no tiene forma de saber que
    faltan 4** salvo contándolos — y las dos que faltaban son la del diálogo **destructivo** y la de
    FAT32. Es exactamente lo que `T2-12` corrigió para las pruebas: llevar la cuenta de lo omitido y
    decirlo al final. Emitir un resumen «N tomas · M omitidas» y salir con código distinto de cero si
    alguna falló sin `-Only`.
  - **Criterio de aceptación:** una toma forzada a fallar deja un resumen que la nombra y un código de
    salida distinto de cero.
  - **Esfuerzo:** bajo
  - **Depende de:** T9-04

- [x] **[T9-06] El `-Exe` por defecto es el que la documentación desaconseja** · Baja
  - **Área:** DevOps
  - **Ubicación:** [tools/capture-screenshots.ps1:112](tools/capture-screenshots.ps1#L112)
  - **Qué hacer:** por defecto busca en `bin\Release`, que es justo el binario que `CONTEXT.md` §4
    documenta como el que **fotografía el diálogo de error de .NET** en vez de la app. La mitigación
    —publicar antes y pasar `-Exe`— vive solo en la prosa. Publicar desde el propio script, o detectar que
    la ventana encontrada no es la de la app y abortar con el motivo.
  - **Criterio de aceptación:** ejecutarlo sin argumentos tras un `dotnet build -c Release` plano no
    produce capturas del diálogo de error: o publica, o falla diciendo por qué.
  - **Esfuerzo:** medio
  - **Depende de:** ninguna

### Corrección

- [x] **[T9-07] El historial se fecha con la cultura de Windows, no en invariante** · Media
  - **Área:** Auditoría de código / i18n
  - **Ubicación:** [src/FormatDiskPro/Services/History.cs:100](src/FormatDiskPro/Services/History.cs#L100) · [src/FormatDiskPro/UI/HistoryDialog.xaml.cs:151](src/FormatDiskPro/UI/HistoryDialog.xaml.cs#L151)
  - **Qué hacer:** `$"{DateTime.Now:yyyy-MM-dd HH:mm:ss}"` usa `CurrentCulture`, y con ella el
    **calendario** de la cultura. En un Windows tailandés (`th-TH`, calendario budista) la entrada se
    escribe con el año **2569** en vez de 2026; en `ar-SA` (Umm al-Qura), con el año híjri. Pasar
    `CultureInfo.InvariantCulture` explícitamente en los dos sitios (el segundo es el nombre del CSV
    exportado).
  - **Por qué importa:** los **cuatro** sitios que leen, exportan o muestran esa fecha ya son invariantes
    a propósito (`HistoryEntry.Parse:115`, `ToCsv:167`, `HistoryDialog:179`) — son los dos que
    **escriben** los que se saltan la convención que el propio §4 de `CONTEXT.md` declara («lo que se
    guarda sigue pasando `InvariantCulture` de forma explícita»). Y no se rechaza: `TryParseExact` acepta
    «2569» como año, así que la entrada queda 543 años en el futuro y encabeza el orden.
  - **Verificado:** medido con `th-TH` — escribe `2569-08-26`. Es la misma familia que `T1-01` (cultura
    turca) y `T6-12`.
  - **Criterio de aceptación:** una prueba que fije `CurrentCulture` en `th-TH`, registre una entrada y
    exija que la línea escrita empiece por el año gregoriano.
  - **Esfuerzo:** bajo
  - **Depende de:** ninguna

- [x] **[T9-08] Un `settings.json` corrupto se sustituye en silencio y se sobrescribe** · Media
  - **Área:** Auditoría de código
  - **Ubicación:** [src/FormatDiskPro/Services/AppSettings.cs:116](src/FormatDiskPro/Services/AppSettings.cs#L116) y `Save` en [:135](src/FormatDiskPro/Services/AppSettings.cs#L135)
  - **Qué hacer:** ante un JSON ilegible, `Load` devuelve los valores por defecto sin decir nada; el
    siguiente `Save` **sobrescribe el archivo**, y con él los presets del usuario, que son el único dato
    que la app no puede reconstruir. Renombrar el archivo ilegible a `settings.corrupt.json` antes de
    seguir y registrarlo en el historial, que es donde se consulta después.
  - **Criterio de aceptación:** con un `settings.json` truncado a la mitad, la app arranca con los
    valores por defecto, el archivo original sigue existiendo con otro nombre y el historial lo registra.
  - **Esfuerzo:** bajo
  - **Depende de:** ninguna

- [x] **[T9-09] `BuildVolumeScript` es el único constructor de comandos sin la guarda de la convención** · Baja
  - **Área:** Seguridad (defensa en profundidad)
  - **Ubicación:** [src/FormatDiskPro/Core/FormatLogic.cs:23-34](src/FormatDiskPro/Core/FormatLogic.cs#L23)
  - **Qué hacer:** interpola `fs` en el script sin comprobarlo contra
    `PartitionPlan.SupportedFileSystems`, mientras `DiskService` valida `char.IsLetter` en sus **cinco**
    métodos y `ReinitDrive` **revalida el plan entero** antes de construir el suyo. Añadir la misma
    comprobación y devolver/lanzar si no está en la lista.
  - **NO es explotable hoy, y conviene que quede escrito por qué:** el valor sale siempre del `ComboBox`
    del XAML, y la otra vía plausible —un preset del `settings.json`, que vive en `%AppData%` y se puede
    escribir **sin elevación** mientras la app corre **elevada**— está cerrada porque `MnuPreset_Click`
    exige `FileSystemPicker.Items.IndexOf(preset.FileSystem) >= 0` y rechaza lo que no coincida. Se
    persiguió esa ruta entera y no llega. Esto es coherencia de convención, no un agujero abierto.
  - **Criterio de aceptación:** una prueba que pase un `fs` fuera de la lista y exija que no se produzca
    un script ejecutable.
  - **Esfuerzo:** bajo
  - **Depende de:** ninguna

### Interfaz e idioma

- [x] **[T9-10] El estado de salud se muestra en inglés crudo, teniendo la traducción hecha** · Media
  - **Área:** i18n / UX/UI / Accesibilidad
  - **Ubicación:** [src/FormatDiskPro/UI/MainWindow.DriveInfo.cs:161](src/FormatDiskPro/UI/MainWindow.DriveInfo.cs#L161) · [src/FormatDiskPro/UI/HealthDialog.xaml.cs:78](src/FormatDiskPro/UI/HealthDialog.xaml.cs#L78)
  - **Qué hacer:** ambos sitios pintan el `HealthStatus` crudo del proveedor de Storage —siempre en
    inglés— en vez de traducirlo. La tarjeta principal muestra **«Salud: Healthy»** en los cinco idiomas,
    y el diálogo S.M.A.R.T. muestra **«Healthy — Normal»**: el valor inglés y su traducción, uno al lado
    del otro. Usar `LevelLabel(SmartInfo.HealthLevel(...))`, y dejar el valor crudo solo cuando el nivel
    sea `Unknown` (que es cuando no hay nada que traducir).
  - **Lo llamativo es que la pieza ya existe:** `health.level.ok/warning/critical` están traducidas a los
    cinco idiomas desde hace tiempo y `SmartInfo.HealthLevel` ya clasifica el valor — hoy solo se usan
    para elegir el **color**. El comentario de `RenderHealth` dice «el texto ya transmite el estado; el
    color refuerza», y es precisamente lo que no ocurre para quien no lee inglés — ni para un lector de
    pantalla, que canta la palabra inglesa dentro de una frase en español.
  - **Anclado a:** `main-light.png` («Salud: Healthy») y `health-dark.png` («Estado de salud: Healthy — Normal»).
  - **Criterio de aceptación:** en los cinco idiomas, la fila de salud no contiene ninguna de las cadenas
    `Healthy`/`Warning`/`Unhealthy` cuando el nivel es conocido. Prueba que barra los cinco.
  - **Esfuerzo:** bajo
  - **Depende de:** ninguna

- [x] **[T9-11] Dos cadenas de estado usan `...` donde las otras 156 usan `…`** · Baja
  - **Área:** Ortografía y redacción
  - **Ubicación:** [Localization.cs:231](src/FormatDiskPro/Localization/Localization.cs#L231) (`status.formatting`) y [:236](src/FormatDiskPro/Localization/Localization.cs#L236) (`status.wiping`)
  - **Qué hacer:** sustituir los tres puntos por el carácter de puntos suspensivos en las 10 cadenas
    (2 claves × 5 idiomas). Sus **hermanas** de la misma barra de estado —`check.scanning`,
    `bench.preparing`— ya usan `…`, así que la incoherencia se ve en el mismo sitio de la pantalla y
    durante las dos operaciones más largas, que son las que más se miran.
  - **Criterio de aceptación:** un barrido del diccionario que falle si aparece `...` en cualquier
    cadena, al estilo del que dejó `T6-09` para «sólo».
  - **Esfuerzo:** bajo
  - **Depende de:** ninguna

### Pruebas y limpieza

- [x] **[T9-12] El arreglo de `T6-12` es el único sin tripwire** · Baja
  - **Área:** QA y testing
  - **Ubicación:** [src/FormatDiskPro/Localization/Localization.cs:100](src/FormatDiskPro/Localization/Localization.cs#L100)
  - **Qué hacer:** `L.T(clave, args)` llama a `string.Format(template, args)` **sin proveedor**, así que
    formatea con la cultura de Windows. Hoy no falla —verificado: ninguna plantilla lleva especificador
    (`{0:N0}`) y los ~45 puntos de llamada preformatean con `L.Culture` antes de entrar—, pero la
    convención la sostiene la disciplina de quien llama, no el código. Pasar `Culture` como proveedor en
    esa única línea la vuelve estructural.
  - **Por qué merece la pena:** este proyecto convierte sus arreglos en barridos que no se pueden
    esquivar —`T1-04` con el inventario de color, `T1-07` con las tablas de cadenas—. `T6-12` es el que
    se quedó sin el suyo, y su forma de volver es que alguien pase un `long` a `L.T`.
  - **Criterio de aceptación:** con la app en español y `CurrentCulture` en `en-US`, una clave con un
    número grande como argumento sale con separadores españoles.
  - **Esfuerzo:** bajo
  - **Depende de:** ninguna

- [x] **[T9-13] `DecodeArguments` es API pública que solo usa su propia prueba** · Baja
  - **Área:** Refactorización y limpieza
  - **Ubicación:** [src/FormatDiskPro/Core/FormatLogic.cs:46](src/FormatDiskPro/Core/FormatLogic.cs#L46)
  - **Qué hacer:** no tiene ni un consumidor de producción; su única referencia es la prueba de ida y
    vuelta de [FormatLogicTests.cs:111](tests/FormatDiskPro.Tests/FormatLogicTests.cs#L111). Además, una
    prueba de ida y vuelta contra un inverso escrito a medida **no puede fallar** si ambos lados comparten
    el error. Anclar la prueba al Base64 esperado y bajar el método a `internal`, o retirarlo.
  - **Criterio de aceptación:** la prueba de codificación afirma la cadena Base64 concreta; `Core` no
    expone métodos sin consumidor.
  - **Esfuerzo:** bajo
  - **Depende de:** ninguna

### Documentación

- [x] **[T9-14] La cabecera de `CONTEXT.md` se contradice con la fila de al lado** · Media
  - **Área:** Documentación
  - **Ubicación:** [CONTEXT.md:14](CONTEXT.md#L14) (fila *Estado*) y [CONTEXT.md:17](CONTEXT.md#L17) (fila *Pruebas*)
  - **Qué hacer:** la fila *Estado* dice «Abierto: **Tier 7** … (7/8, desde 2026-08-25)» mientras la fila
    *Hoja de ruta*, **dos líneas más abajo en la misma tabla**, dice «sin tareas abiertas (Tiers 7 y 8
    cerrados)» — igual que §3 y que el `ROADMAP.md`. Y la fila *Pruebas* dice «588 unitarias · 30 de UI ·
    26 pasan» cuando §3 dice 607 y 38. Medido hoy: **606 pasan / 1 omitida** (607) y **34 pasan / 3
    omitidas** de 37 con el filtro.
  - **Es la reincidencia de un fallo ya diagnosticado:** el propio §3 explica que la tabla de tiers
    «vivía aquí duplicada de la del `ROADMAP.md`, y se quedó desactualizada por serlo». La cabecera
    volvió a duplicar estado y recuentos, y volvió a envejecer — y es lo primero que lee quien retoma el
    proyecto.
  - **Criterio de aceptación:** la cabecera no repite ningún dato que viva en §3 o en el `ROADMAP.md`:
    o enlaza, o no lo dice.
  - **Esfuerzo:** bajo
  - **Depende de:** ninguna

- [x] **[T9-15] El árbol de arquitectura lista un archivo que no existe y omite uno que sí** · Media
  - **Área:** Documentación
  - **Ubicación:** [CONTEXT.md:33-60](CONTEXT.md#L33) (§2)
  - **Qué hacer:** el árbol de `Core/` lista **`SecureWipe.cs`**, que no existe ahí (el borrado seguro
    vive solo en `Services/`), y **omite `ErrorText.cs`**, que sí existe y lo introdujo `T8-02` — la pieza
    de la que dependen los once sitios que muestran o registran un error. El `README.md` ya lo tiene bien;
    es solo `CONTEXT.md` el que se quedó atrás.
  - **Criterio de aceptación:** el árbol de §2 coincide archivo a archivo con `ls src/FormatDiskPro/Core`.
  - **Esfuerzo:** bajo
  - **Depende de:** ninguna

- [x] **[T9-16] Comentario obsoleto: el `.csproj` ya no usa `1.8.*`** · Baja
  - **Área:** Documentación / limpieza
  - **Ubicación:** [src/FormatDiskPro/installer/build-installer.ps1:161](src/FormatDiskPro/installer/build-installer.ps1#L161)
  - **Qué hacer:** el comentario justifica publicar a `%TEMP%` diciendo «El `.csproj` referencia el SDK
    como `1.8.*`, así que el conjunto de archivos puede crecer solo». Ya no: se fijó a `1.8.260529003`
    exacta, y precisamente por el fallo que el comentario describe. Actualizarlo — el motivo de MAX_PATH
    sigue siendo válido, el argumento del comodín ya no.
  - **Criterio de aceptación:** ningún comentario del repo afirma que la versión del SDK sea flotante.
  - **Esfuerzo:** bajo
  - **Depende de:** ninguna

### Instalador y cumplimiento

- [x] **[T9-17] El instalador borra el directorio de destino entero, sea cual sea** · Media
  - **Área:** Seguridad / DevOps
  - **Ubicación:** [src/FormatDiskPro/installer/installer.iss:76](src/FormatDiskPro/installer/installer.iss#L76)
  - **Qué hacer:** `[InstallDelete] Type: filesandordirs; Name: "{app}\*"` vacía el directorio de
    instalación antes de copiar. El comentario lo justifica —«No hay datos de usuario en `{app}`»— y es
    cierto **para el directorio por defecto**; pero `DisableDirPage` no está fijado, así que en una
    instalación nueva el usuario puede elegir destino, y apuntar a una carpeta que ya use se lleva su
    contenido por delante. Fijar `DisableDirPage=yes` (coherente con que no haya nada que elegir aquí) o
    acotar el borrado a lo que el propio instalador pone.
  - **Criterio de aceptación:** instalar sobre un directorio con un archivo ajeno no lo borra, o el
    usuario no puede elegir ese directorio.
  - **Esfuerzo:** bajo
  - **Depende de:** ninguna

- [x] **[T9-18] La comprobación de actualizaciones es automática y no se puede desactivar** · Baja · *requiere revisión legal*
  - **Área:** Legal y cumplimiento / Privacidad
  - **Ubicación:** [src/FormatDiskPro/UI/MainWindow.xaml.cs:225](src/FormatDiskPro/UI/MainWindow.xaml.cs#L225) · texto en `about.privacy`
  - **Qué hacer:** cada arranque contacta con `api.github.com` sin preguntar y sin que exista preferencia
    para evitarlo. El aviso de privacidad es **exacto en el qué** («la única conexión a Internet es para
    comprobar y descargar actualizaciones desde GitHub Releases») pero no dice que sea **automática ni en
    cada arranque**, y contactar con un tercero transmite la IP del usuario. Añadir la preferencia
    (`Configuración` ya tiene dónde) y precisar el texto.
  - **Alcance honesto:** la app no recopila nada; lo que hay es una conexión saliente a un tercero sin
    opción de negarse. **Si eso exige base legal o mención explícita depende de la jurisdicción y no se
    dictamina aquí.** Además tiene un lado práctico: es una utilidad de disco que se usa en equipos
    recién montados y sin red.
  - **Criterio de aceptación:** existe una preferencia persistida que, desactivada, hace que no salga
    ninguna petición de red en el arranque; el texto de privacidad la describe.
  - **Esfuerzo:** bajo
  - **Depende de:** ninguna

- [x] **[T9-19] Los avisos de terceros son incoherentes con su propio criterio** · Baja
  - **Área:** Legal y cumplimiento
  - **Ubicación:** `THIRD-PARTY-NOTICES.txt`
  - **Qué hacer:** el archivo incluye **Inno Setup** con la etiqueta «solo para construir el instalador;
    no se redistribuye», pero omite xUnit (Apache-2.0), FlaUI (MIT) y coverlet (MIT), que están en esa
    misma categoría. O se listan las cuatro, o se declara que solo se listan los componentes
    redistribuidos y se retira Inno Setup. Lo redistribuido —.NET y Windows App SDK, ambos MIT— está bien
    cubierto y es compatible con GPLv3.
  - **Criterio de aceptación:** el archivo declara su criterio y lo cumple sin excepciones.
  - **Esfuerzo:** bajo
  - **Depende de:** ninguna

- [x] **[T9-20] Desinstalar deja el historial de operaciones en el disco** · Baja
  - **Área:** Legal y cumplimiento / UX
  - **Ubicación:** `src/FormatDiskPro/installer/installer.iss` (sin sección `[UninstallDelete]`)
  - **Qué hacer:** al desinstalar queda `%AppData%\FormatDiskPro` con `settings.json` y el historial
    —que es un registro fechado de qué unidades se formatearon— sin que nadie lo mencione. Preguntar en
    la desinstalación si se borran los datos de usuario, que es lo que hace el resto de aplicaciones que
    guardan algo fuera de `{app}`.
  - **Criterio de aceptación:** la desinstalación ofrece borrar los datos de usuario y respeta la
    respuesta.
  - **Esfuerzo:** bajo
  - **Depende de:** ninguna

---

## 🧪 Tier 10 — Lo que solo aparece al publicar *(abierto 2026-08-26)*

> **De dónde sale.** Del corte de la **v1.25.0**, no de una revisión. El primer intento abortó en la
> puerta de cobertura con el informe **vacío** —el síntoma de `T8-06`, ya cerrado— **con su arreglo
> puesto y funcionando**. El reintento salió limpio y la v1.25.0 se publicó. Queda una tarea, y lo
> honesto es decir que **no se conoce su causa**: `T8-06` cerró *una* de las causas del informe vacío,
> y hay al menos otra.
>
> **Base:** v1.25.0 · unitarias **623/623, 0 omitidas** (con `FORMATDISKPRO_VERIFY_DRIVE`) · `Core/` al
> **98,1 %** · UI **34/37**, los 3 omitidos opt-in · todo del propio corte del 2026-08-26.

- [x] **[T10-01] El informe de cobertura sale vacío de vez en cuando, y no se sabe por qué** — **hecho (2026-08-27)** · Media
  - **Área:** DevOps / publicación
  - **Ubicación:** [release.ps1:437-449](release.ps1#L437-L449) (compilar, medir y abortar) ·
    [release.ps1:156](release.ps1#L156) (`Get-CoreCoverage`, que devuelve `$null` sin distinguir casos)
  - **Qué pasó, medido:** al cortar la v1.25.0 el script murió con «Se pidió cobertura y no se obtuvo
    informe», con las **623 unitarias en verde** y un `coverage.cobertura.xml` de **235 bytes**
    (`<packages />`). Es exactamente el artefacto de `T8-06` — pero su arreglo estaba puesto y se
    ejecutó: `dotnet build` de la solución (18,5 s, recompiló) y `dotnet test --no-build` después.
  - **Lo que NO se consiguió:** reproducirlo. **Tres intentos, los tres con informe de 1,6 MB**: (1)
    `dotnet test --no-build` con la compilación al día; (2) la secuencia exacta del corte, con
    `FORMATDISKPRO_VERIFY_DRIVE=I` puesta como la tenía el corte; (3) **la condición que provocaba
    `T8-06`** —tocar una fuente para desactualizar la compilación— y aun así compilar antes. El
    reintento del corte completo también salió bien, al 98,1 %. Así que **no es la causa de `T8-06`**,
    y esa sigue cerrada: esto es otra distinta.
  - **Único dato diferencial, y es débil:** en la corrida que falló, la compilación tardó **18,5 s** y
    las pruebas **1 m 6 s**; en las que salieron bien, 8 s y 20 s. **Hipótesis sin verificar**, y hay
    que tratarla como tal: la corrida que falló fue la primera con la prueba de capacidad sobre unidad
    real activada, que hace E/S sin caché sobre la USB durante ~1 min. No se ha medido ninguna relación
    entre eso y la instrumentación, y bien podría no haberla.
  - **Qué hacer, y qué NO:** **no** poner un reintento automático de la medición. Un fallo intermitente
    que se reintenta solo deja de aparecer y no deja de existir, y esta puerta es la que decide si un
    corte sale. Lo que hace falta es que la **próxima vez que ocurra deje pruebas**:
    1. Distinguir en el mensaje **informe ausente** de **informe presente y vacío**. Hoy los dos caen en
       el mismo `$null` y en el mismo texto, que culpa a `coverlet.collector` — y en las dos ocasiones
       registradas el paquete estaba perfectamente referenciado. Es la lección de `T8-06` sin aprender
       del todo: *un fallo de herramienta que señala mal se arregla dos veces*.
    2. Cuando el informe salga vacío, **conservarlo** junto al log de la corrida en vez de dejarlo en un
       `%TEMP%` que la siguiente corrida borra, y decir dónde quedó.
    3. Añadir los diagnósticos del recolector (`--diag`) **solo en ese camino**, para no ralentizar los
       cortes que van bien.
  - **Criterio de aceptación:** provocar el caso a mano (un `coverage.cobertura.xml` vacío) y comprobar
    que el corte aborta con un mensaje que dice que el informe está **vacío**, no que falte el paquete,
    y que nombra la ruta donde lo ha dejado.
  - **Lo que este fallo hizo bien, y consta:** abortó **antes del bump**. Árbol limpio, sin commit, sin
    tag, `.csproj` intacto en `1.24.1`. La puerta falló hacia el lado seguro, que es la mitad del
    trabajo de una puerta.
  - **Hecho (2026-08-27):** `Get-CoreCoverage` ya no devuelve `$null` nunca: devuelve un `.Status` que
    separa **`missing`** (no hay informe — aquí sí cabe sospechar del paquete), **`unreadable`** (lo hay y
    no es XML), **`empty`** (lo hay, es válido y no declara ni una clase: el artefacto de 235 bytes) y
    **`nocore`** (mide clases, pero ninguna de `Core/` — eso es el filtro o el árbol, no el recolector).
    Cada uno aborta con su mensaje. En `empty`/`unreadable` el informe se copia a
    `%TEMP%\FormatDiskPro_cobertura_fallida\<versión>-<fecha>` —**fuera del repo**, porque un archivo
    nuevo dentro haría abortar el siguiente corte por árbol sucio (`T9-01`)— y se repite la medición **una
    vez** con `--diag`, cuyos logs se conservan igual.
  - **Y esa segunda pasada NO es un reintento:** el corte muere igual, gane o pierda. Lo único que cambia
    es el veredicto que imprime —«es intermitente» o «es reproducible aquí y ahora»— y el consejo que da.
    Un fallo intermitente al que se le pone un reintento deja de aparecer sin dejar de existir, y esta
    puerta es la que decide si un corte sale.
  - **Verificado, y en dos niveles:** las **cinco** clasificaciones, contra informes fabricados a mano
    (incluido el `<packages />` real de 235 bytes) — 5/5; y el **camino de fallo entero**, con una copia
    desechable del script apuntando a ese informe vacío: clasificó `empty`, conservó el informe, repitió
    con `--diag` (midió 98,1 %, veredicto «intermitente»), guardó **3,7 MB de diagnósticos** —incluido el
    log del *datacollector*, que es justo el que puede explicar por qué no se instrumentó nada— y abortó
    con `exit 1` sin nombrar a `coverlet.collector`.
  - **Esfuerzo:** bajo
  - **Depende de:** ninguna

- [ ] **[T10-02] La causa del informe vacío sigue sin conocerse** · Baja
  - **Área:** DevOps / publicación
  - **Ubicación:** [release.ps1:525-575](release.ps1#L525-L575) (el camino de fallo que ahora recoge las
    pruebas)
  - **Qué falta:** saber **por qué** pasa. `T8-06` cerró una causa (compilar antes de medir) y `T10-01` se
    ocupó de que la próxima vez queden pruebas, pero **ninguna de las dos explica** el fallo del corte de
    la v1.25.0, que ocurrió con el arreglo de `T8-06` puesto y ejecutado y no se reprodujo en tres
    intentos.
  - **Está bloqueada, y es deliberado:** no se puede investigar lo que no se puede reproducir. La tarea se
    queda abierta **a la espera de la próxima vez que ocurra**, que es cuando habrá logs del recolector en
    `%TEMP%\FormatDiskPro_cobertura_fallida` — y ese, no otro, es el momento de trabajarla.
  - **Lo único que se sabe, y es débil:** en la corrida que falló, la compilación tardó 18,5 s y las
    pruebas 1 m 6 s; en las que salieron bien, 8 s y 20 s. Fue también la primera con la prueba de
    capacidad sobre unidad real activada. **Hipótesis sin verificar**, anotada para contrastarla con los
    logs cuando los haya — no para darla por buena.
  - **Criterio de aceptación:** nombrar la causa con un log delante, o —si tras varias ocurrencias los
    diagnósticos no la señalan— cerrarla diciendo qué se descartó y con qué pruebas. Cerrarla sin
    ninguna de las dos cosas sería fingir que se resolvió.
  - **Esfuerzo:** desconocido (depende de lo que digan los logs)
  - **Depende de:** `T10-01` (hecha: es la que produce las pruebas)

---

## 🔤 Tier 12 — Lo que la ventana no dice *(abierto 2026-09-01)*

> **De dónde sale.** De una revisión de UI/UX pedida después de cerrar el Tier 11. El primer hallazgo no
> es una preferencia: es un **defecto medido**, y de la misma familia que el que documenta
> `SeverityPalette.All` — un color de texto por debajo de WCAG AA que el barrido de contraste no podía
> ver porque venía de un `ThemeResource` de Windows.
>
> **Base:** unitarias **667/667** (666 pasan · 1 se omite) · build 0/0.

- [x] **[T12-01] El texto terciario de Fluent no llega a AA, y el barrido no podía verlo** — **hecho (2026-09-01)** · Alta
  - **Área:** Accesibilidad / contraste
  - **Ubicación:** [Core/FluentTextPalette.cs](src/FormatDiskPro/Core/FluentTextPalette.cs) ·
    [Core/SeverityPalette.cs](src/FormatDiskPro/Core/SeverityPalette.cs) (`MutedText`) ·
    [UI/Theme/AppTheme.xaml](src/FormatDiskPro/UI/Theme/AppTheme.xaml) ·
    [tests/TextContrastTests.cs](tests/FormatDiskPro.Tests/TextContrastTests.cs)
  - **Medido, con la fórmula de la propia app** (`SeverityPalette.ContrastRatio`):

    | Token | Claro | Oscuro | AA 4,5:1 |
    |---|---:|---:|---|
    | `TextFillColorPrimary` | 16,65:1 | 14,16:1 | OK |
    | `TextFillColorSecondary` | 6,17:1 | 9,09:1 | OK |
    | **`TextFillColorTertiary`** | **3,29:1** | 5,09:1 | **por debajo** |

  - **Dónde estaba:** en `HintTextStyle`, `DriveMetaStyle` y `MetricCaptionStyle` — **18 controles** de la
    ventana principal. No es texto decorativo: son las pistas que explican qué sistema de archivos y qué
    tamaño de clúster elegir, la línea `NTFS · Disco fijo · NVMe · SSD`, «Libre: 181,6 GB» y las etiquetas
    de la franja de rendimiento.
  - **Por qué el barrido no lo veía, que es lo importante:** `SeverityPaletteTests` recorre
    `SeverityPalette.All()`, y la documentación de esa clase decía que sus colores eran «los únicos que no
    salen de un `ThemeResource` de Windows». Eso dejaba fuera de la medición a los que **sí** salen de
    uno. Es **el mismo fallo que ese inventario existe para evitar** —«así entró un gris de 3.52:1»—
    repitiéndose por el otro lado: que un color venga de Windows no lo hace correcto para cualquier uso.
    Fluent define el terciario para texto de apoyo, no para contenido; usarlo en una pista fue decisión
    nuestra, y por tanto su contraste también.
  - **Qué se hizo:**
    1. `SeverityPalette.MutedText` — tercer nivel de texto **elegido y medido**: `#6C6C6C` / `#9A9A9A`,
       **5,07:1** y **5,03:1**. Con margen (no el primero que pasa, por lo mismo que en `ForResult`) y por
       debajo del secundario de Fluent, que es lo que conserva el escalón de jerarquía. Subirlo a
       secundario habría sido más fácil y habría borrado el tercer nivel: si el paso más callado no puede
       leerse, es que estaba mal elegido, no que sobre.
    2. `FluentTextPalette` — los tokens de texto de Fluent que la app usa, con su valor real, para poder
       medirlos.
    3. `TextContrastTests` — **recorre el XAML** buscando `TextFillColor*Brush` y mide lo que hay puesto.
       Un token no declarado también falla: significa que la app usa un color que nadie ha medido.
  - **`HighContrast` no usa el gris:** en alto contraste manda el color del sistema, y sustituirlo por uno
    propio —por muy medido que esté— es justo lo que ese modo existe para impedir.
  - **La copia está anclada:** un `ResourceDictionary` no puede llamar a `Core`, así que los dos hex están
    duplicados en `AppTheme.xaml`; una prueba los lee de ahí y exige que coincidan con `SeverityPalette`.
    Sin ella, la duplicación sería exactamente el agujero que esta tarea cierra.
  - **Verificado en negativo:** se reintrodujo el terciario en un estilo y el barrido **falló**. Un test de
    contraste que no se ha visto fallar no se sabe si mide.
  - **Esfuerzo:** bajo
  - **Depende de:** ninguna

- [x] **[T12-02] El botón que puede destruir un disco no lo nombraba** — **hecho (2026-09-01)** · Alta
  - **Área:** UI / prevención de errores
  - **Ubicación:** [MainWindow.FormatOptions.cs](src/FormatDiskPro/UI/MainWindow.FormatOptions.cs) (`UpdateFooterSummary`)
  - **Qué pasaba:** `StartButton.Content = L.T("btn.start")` → **«Iniciar»**, siempre. El peor fallo posible
    de esta app es formatear la unidad equivocada, y el control que lo dispara era el **único sitio de la
    pantalla** donde el destino no aparecía.
  - **Qué se hizo:** **«Formatear H:»**, siguiendo a la selección. La confirmación reforzada —escribir la
    letra— sigue siendo la red; esto es la primera línea, y llega antes.
  - **Un solo dueño del texto**, por lo mismo que `UpdateToolsMenuAvailability` lo es del de los siete
    ítems del menú: depende del idioma *y* de la unidad, y dos dueños dejarían el nombre perdido o pegado
    dos veces según cuál corriera el último. `ApplyLanguage` deja de escribirlo.
  - **Esfuerzo:** bajo
  - **Depende de:** ninguna

- [x] **[T12-03] Se podía formatear sin haber visto nunca las opciones** — **hecho (2026-09-01)** · Media
  - **Área:** UI / densidad
  - **Ubicación:** [MainWindow.xaml](src/FormatDiskPro/UI/MainWindow.xaml) (`FormatSummaryText`) ·
    [MainWindow.FormatOptions.cs](src/FormatDiskPro/UI/MainWindow.FormatOptions.cs) (`CurrentFormatSummary`)
  - **Qué pasaba:** en una ventana de alto fijo, la tarjeta *Opciones de formato* —formato rápido,
    compresión, **borrado seguro**— queda entera bajo el pliegue, mientras el botón que la ejecuta está
    siempre visible en el pie.
  - **Qué se hizo:** el resumen —**`NTFS · 4 KB · rápido`**— en la columna del medio de la fila de
    botones, que estaba vacía. Sale de los propios controles, no de un estado paralelo: así no puede
    mentir sobre lo que la operación hará. El detalle completo (compresión incluida) va al tooltip, y el
    resumen se oculta durante la operación, cuando el pie lo manda `StatusText`.
  - **Reutiliza la cadena que ya existía** dentro de `MnuManagePresets_Click`, que construía este mismo
    texto para el diálogo de presets. Ahora hay un solo `CurrentFormatSummary` para los dos.
  - **Esfuerzo:** bajo
  - **Depende de:** ninguna

- [x] **[T12-04] Los presets estaban a tres menús de la tarjeta que configuran** — **hecho (2026-09-01)** · Media
  - **Área:** UI / descubribilidad
  - **Ubicación:** [MainWindow.xaml](src/FormatDiskPro/UI/MainWindow.xaml) (`PresetsButton`) ·
    [MainWindow.FormatOptions.cs](src/FormatDiskPro/UI/MainWindow.FormatOptions.cs) (`FillPresets`)
  - **Qué se hizo:** un `DropDownButton` en la cabecera de *Configuración de formato*, que es
    exactamente lo que un preset configura. El menú *Configuración → Presets* **se queda**: quitarlo
    rompería la ruta que la gente ya conoce y las pruebas de UI que la recorren.
  - **Un solo constructor para las dos listas** (`FillPresets`): son la misma lista, y construirlas por
    separado haría que un preset nuevo apareciera en una y no en la otra. Los `MenuFlyoutItem` no se
    pueden compartir entre dos flyouts —un elemento de XAML tiene un solo padre—, así que se crean dos
    juegos con la misma fábrica.
  - **Esfuerzo:** bajo
  - **Depende de:** ninguna

- [x] **[T12-05] En un equipo con acento rojo, el éxito y el fallo eran el mismo color** — **hecho (2026-09-01)** · Alta
  - **Área:** UI / señalización de estado
  - **Ubicación:** [MainWindow.xaml.cs](src/FormatDiskPro/UI/MainWindow.xaml.cs) (`ApplyProgressColor`)
  - **Cómo apareció:** en una captura del propio usuario. Un benchmark que **terminó bien** dejaba la
    barra llena y **roja** — exactamente igual que uno que falla.
  - **La causa:** `FormatProgress` nunca fijaba `Foreground`, así que usaba el **color de acento** que el
    usuario tiene en Windows; y `ShowError` pinta de rojo al fallar o cancelar. Con el acento en rojo, el
    único canal que distinguía éxito de fallo no distinguía nada.
  - **Es una decisión que el repo ya había tomado, sin aplicar aquí.** `CapacityBrush` lo dice con todas
    las letras: «una barra de capacidad no debe usar el color de ACENTO del sistema (lo que hace un
    `ProgressBar` por defecto): en un equipo con acento rojo se veía roja con el disco medio vacío y leía
    como alarma». La misma trampa, el mismo control, otro sitio.
  - **Qué se hizo:** `Foreground` explícito desde `SeverityPalette.For(SmartLevel.Ok)` — verde mientras va
    y al terminar bien, rojo de `ShowError` al fallar o cancelar. Los dos salen del inventario que el
    barrido de contraste mide, y ninguno depende ya de lo que el usuario tenga configurado.
  - **Verificado con la app en marcha, en los DOS estados**, que es lo que había que comprobar: fijar
    `Foreground` a mano podía haber ganado al estado de error del propio control y dejar el fallo sin
    rojo. Benchmark completo → **barra verde llena**; benchmark cancelado → **barra roja**.
  - **De paso, una corrección:** la documentación de `T11-01` decía que el benchmark alimenta la fila de
    Disco de la franja de rendimiento. **No lo hace, y no debe.** Su `IProgress` reporta porcentaje, no
    bytes, y una vez por ventana de medición —cada varios segundos—: un caudal derivado de ahí sería
    grueso, a saltos, y **contradiría** la mediana de MB/s que el propio benchmark calcula y enseña. Dos
    cifras distintas para lo mismo en la misma pantalla es justo lo que esa franja se diseñó para evitar.
    Las operaciones que sí informan de bytes son **dos**: verificación de capacidad y borrado seguro.
  - **Esfuerzo:** bajo
  - **Depende de:** ninguna

- [x] **[T12-06] El contenido se cortaba a media tarjeta sin decir que seguía** — **hecho (2026-09-01)** · Media
  - **Área:** UI / descubribilidad
  - **Ubicación:** [MainWindow.xaml.cs](src/FormatDiskPro/UI/MainWindow.xaml.cs) (`UpdateScrollAffordance`)
  - **Qué pasaba:** la ventana es de **tamaño fijo**, así que el contenido casi siempre desborda; y WinUI
    oculta la barra de desplazamiento hasta que alguien interactúa. Quedaba una tarjeta cortada por el
    borde inferior sin nada que avisara — y lo que quedaba debajo eran las **opciones de formato**.
  - **Qué se hizo:** un **galón** (`⌄`) centrado bajo el contenido, que aparece mientras quede algo por
    ver y desaparece al llegar al final. Ocupa **su propia fila**, no flota encima del contenido.
  - **Costó tres intentos, y los dos primeros constan para que no se reintenten:**
    1. **Un degradado en el borde inferior.** Sobre el material **Mica** no hay un color de fondo que
       igualar —el degradado tiene que acabar en algo opaco y la ventana no lo es—, así que se leyó como
       una **franja clara**; y al superponerse a un `TextBox` lo dejaba con aspecto de **deshabilitado**.
       Ese segundo problema lo tiene cualquier velo sobre el contenido, no solo este.
    2. **Forzar `VerticalScrollBarVisibility="Visible"`.** Se dio por bueno sin comprobarlo y **no hace
       nada**: con las barras auto-ocultas de Windows 11 (el valor por defecto), el `ScrollBar` se hace
       visible pero su estado de indicador lo sigue colapsando hasta que hay interacción. Se descubrió al
       **regenerar las capturas del README**: no salía el rail. Se confirmó ampliando ×6 la franja derecha
       de la captura — ahí no había barra ninguna.
  - **La lección, y es la de siempre en este repo:** un arreglo de UI que no se ha *visto* funcionar no se
    sabe si funciona. Los dos primeros intentos compilaban, corrían y no hacían nada de lo que decían.
  - **Por eso el galón ocupa su propia fila:** no tapa nada, no lava ningún control y no depende de
    ningún color de fondo. Cuesta 14 px, y solo cuando hay algo que desplazar. No oscila: al aparecer
    quita alto al `ScrollViewer`, lo que solo puede *aumentar* el desbordamiento.
  - **Y el disparador tampoco era el que parecía:** `SizeChanged` del `ScrollViewer` mide el **hueco**,
    que en una ventana de tamaño fijo no cambia nunca. Lo que crece es el **contenido**, así que el
    manejador va en el `StackPanel` de dentro.
  - **Esfuerzo:** bajo
  - **Depende de:** ninguna

- [x] **[T12-07] La franja de rendimiento no aportaba nada, y se retira** — **hecho (2026-09-01)** · Media
  - **Área:** UI / alcance de producto
  - **Qué se quitó:** `Core/SystemLoad.cs` (con `MovingAverage`), `Services/PerformanceMonitor.cs`,
    `UI/MainWindow.Performance.cs`, la franja del XAML, los tres estilos `Metric*`,
    `AppServices.Performance`, las 11 claves `perf.*` (× 5 idiomas) y sus **41 unitarias**. Es decir,
    `T11-01` y `T11-04` enteras. **626 pruebas** tras la retirada.
  - **Por qué, y el motivo de peso es que la justificación de partida era falsa.** `T11-01` se defendió
    con «en un borrado seguro de 40 minutos la única señal de vida era una barra de progreso». **No era
    cierto**: `TimerElapsed_Tick` ya escribía en el pie `03:41 · 42,3 MB/s · ETA 05:12`, y **para las
    mismas dos operaciones** que alimentaban la fila de Disco (verificación de capacidad y borrado
    seguro). La fila de Disco era un duplicado de la línea que tenía justo debajo — y peor, porque no
    llevaba ETA. La afirmación no se comprobó antes de construir.
  - **Las tres filas fallaban por motivos distintos, que es lo que impedía salvar un subconjunto:**
    Disco **duplicaba**; CPU y RAM del equipo **decoraban** —no son asunto de un formateador de discos, y
    eso estaba dicho en la propia conversación que la encargó antes de construirla—. Quitar CPU/RAM y
    dejar Disco conserva el duplicado; quitar Disco y dejar CPU/RAM conserva lo decorativo.
  - **Lo que costaba:** ~34 px permanentes en una ventana de **alto fijo** donde este mismo tier y el
    anterior llevaban peleando por espacio vertical (`T12-03`: las opciones quedaban bajo el pliegue), más
    un servicio Win32 con temporizador atado a la activación de la ventana, 41 pruebas y 55 cadenas
    traducidas. La fila de Disco enseñaba `–` en 3 de las 5 operaciones.
  - **Lo único que se pierde y no está en otro sitio** es el **pico** de velocidad. Se deja fuera a
    propósito: cabría en el cronómetro en una línea, pero nadie lo ha pedido y añadirlo «por si acaso» es
    cómo empezó esto.
  - **Lo que SÍ sobrevive de aquel trabajo**, porque se sostiene solo: el color de la barra de progreso
    (`T12-05`), que salió de mirar la franja funcionando; el galón de scroll (`T12-06`); y
    `SeverityPalette.MutedText` con su barrido (`T12-01`), que nació de medir los grises que la franja
    usaba y ahora protege a toda la ventana.
  - **La lección:** una petición de producto no exime de comprobar el problema que dice resolver. La
    franja se construyó bien —compacta, medida, accesible, probada— sobre un problema que no existía.
  - **Verificado:** build 0/0, **626/626**, y con las capturas regeneradas.
  - **Esfuerzo:** bajo
  - **Depende de:** revierte `T11-01` y `T11-04`

### Dos refinamientos que se propusieron y NO se hicieron

Los dos salieron de la misma revisión que abrió este tier, y al mirarlos de cerca no se sostuvieron.
Constan aquí para no volver a proponerlos sin argumento nuevo.

- **Encoger el aviso de unidad protegida** (el `InfoBar` que aparece sobre la tarjeta de unidad) para
  ganar alto, alegando que el mismo hecho ya lo dicen el `[Protegido]` del selector y su color rojo.
  **No.** Solo aparece cuando la unidad seleccionada está protegida, que es precisamente cuando el
  usuario necesita entender por qué no puede formatear. Redundar en el aviso más importante de la app no
  es despilfarro, y encogerlo para recuperar 40 px de una tarjeta que en ese estado está deshabilitada
  entera es cambiar seguridad por espacio que no hace falta.

- **Quitar o degradar el botón «Cerrar»** del pie, alegando que duplica la X de la barra de título.
  **No.** Durante una operación es «Cancelar» —la única forma de parar un borrado seguro de 40 minutos— y
  ahí se gana el sitio de sobra. En reposo ya es visualmente secundario frente al botón de acento, que es
  toda la jerarquía que necesita. La preocupación real era la memoria muscular hacia el botón de abajo a
  la derecha, y de eso se ocupa la confirmación reforzada, no la posición.

### La trampa de WinUI que encontró este tier

`IsChecked="True"` en el XAML de un `CheckBox` que además declara `Checked="…"` **dispara el manejador
durante el propio parseo**, cuando los controles declarados más abajo en el archivo aún no existen. El
manejador nuevo tocaba el pie de la ventana, y el `NullReferenceException` salió envuelto como
`XamlParseException: Failed to assign to property 'ToggleButton.IsChecked'` — un mensaje que señala al
atributo y no a la causa. La app arrancaba en negro y moría.

Lo encontró **su propio registro**: `App.UnhandledException` (`T0-01`) dejó la traza completa en
`history.log`, con archivo y línea. Sin esa red, el síntoma era un `0xc000027b` en `Microsoft.UI.Xaml.dll`.

La guarda es un campo `_uiBuilt`, puesto justo después de `InitializeComponent`. No confundir con
`_uiReady`, que marca el final del constructor entero y sirve para no persistir preferencias mientras se
restauran.

---

## 📈 Tier 11 — Rendimiento y jerarquía de la ventana principal *(abierto 2026-09-01)*

> **De dónde sale.** De una petición de producto —un panel de uso de recursos, al estilo del «Tu entorno»
> de otras herramientas— y de una carencia real: durante un borrado seguro de 40 minutos o una
> verificación de capacidad de una USB de 256 GB, **la única señal de vida era una barra de progreso**.
> Con ella quieta no hay forma de distinguir «va lento» de «se colgó».
>
> **Base:** v1.25.0 · unitarias **664/664** (663 pasan · 1 se omite) · build 0/0.

- [x] **[T11-01] El pie no dice a qué velocidad va la operación ni a costa de qué** — **hecho (2026-09-01)** · Media
  - **Área:** UI / UX
  - **Ubicación:** [src/FormatDiskPro/Core/SystemLoad.cs](src/FormatDiskPro/Core/SystemLoad.cs) ·
    [src/FormatDiskPro/Services/PerformanceMonitor.cs](src/FormatDiskPro/Services/PerformanceMonitor.cs) ·
    [src/FormatDiskPro/UI/MainWindow.Performance.cs](src/FormatDiskPro/UI/MainWindow.Performance.cs)
  - **Qué se hizo:** un panel plegable en el **pie**, encima de la barra de progreso, con tres métricas de
    la misma forma —etiqueta + valor, barra, pie de contexto—: **Disco** (caudal de la operación y su
    pico), **CPU** y **RAM** del equipo. Se despliega solo al empezar una operación y se queda desplegado
    al terminar, convertido en el resumen de lo que acaba de pasar.
  - **La decisión de diseño que lo sostiene, y por qué NO es una cuarta tarjeta:** la ventana es de
    **tamaño fijo** (500×900 DIP, decisión firme). Las tres tarjetas ya la llenan: una cuarta siempre
    visible empujaría *Opciones de formato* fuera de la vista para cobrar sitio permanente por un dato que
    solo tiene sentido mientras algo corre. En el pie está además **pegado a la barra de progreso y al
    cronómetro**, que describen la misma operación.
  - **Lo que se decidió NO enseñar, y es la mitad del trabajo:**
    - **CPU del proceso.** Sería casi 0 durante un formateo: el trabajo lo hacen `format.com`,
      `chkdsk.exe` y PowerShell en **procesos aparte**. Se enseña la del equipo, dicho así en el pie de la
      fila. El consumo propio de la app va al pie de RAM, que es donde no engaña.
    - **Un contador de disco del sistema.** Daría el tráfico de toda la máquina. El caudal sale de los
      **bytes que la propia operación reporta** —el mismo dato del que ya vivían la velocidad y el ETA del
      cronómetro—, así que mide lo que el usuario está esperando y no hay dos cifras distintas para lo
      mismo en la misma pantalla.
    - **Un máximo teórico para la barra de disco.** No existe: depende del medio, del bus y de la
      operación. La barra se escala contra el **pico de esta operación** («vas al 70 % de tu mejor
      momento»), que es la pregunta útil — ¿se está frenando?
  - **Trampas evitadas, y por qué constan:**
    - **`PerformanceCounter` no vale aquí.** Los nombres de categoría de PDH están **traducidos** (en un
      Windows en español es «Procesador», no «Processor») y esta app se instala en cinco idiomas: se
      habría caído en la mitad de las máquinas. Se usa `GetSystemTimes` + `GlobalMemoryStatusEx`.
    - **`GetSystemTimes` incluye el ocioso dentro del tiempo de núcleo.** Restarlo no es una corrección
      opcional, es la fórmula; hay una prueba que lo ancla.
    - **Las barras NO son `ProgressBar`**, por lo mismo que la de ocupación (`ProgressBarTrackHeight` fija
      la pista en 1 px mientras el relleno ocupa el `MinHeight`): pista = `Border`, relleno = su hijo.
    - **Ni un `Color.FromArgb` nuevo.** Los colores salen de `SeverityPalette`, que es el inventario que
      el barrido de contraste WCAG mide, y con los **mismos umbrales 80/90** que la barra de ocupación:
      dos barras iguales que cambiaran de color en umbrales distintos enseñarían que el color no importa.
    - **Sin región activa.** `StatusText` ya es la del pie; otra que se actualizara cada segundo con tres
      cifras haría inusable el lector de pantalla durante una operación de horas. Las barras van en
      `AccessibilityView="Raw"` y el valor queda en texto, al lado.
  - **Coste, acotado:** el temporizador corre con el panel desplegado **o** con una operación en curso, y
    en ningún otro momento. La app arranca elevada en toda sesión y muchas se pasan enteras sin formatear
    nada: un tick por segundo perpetuo sería coste sin beneficio.
  - **Verificado:** compilación 0/0, **+41 unitarias** sobre `SystemLoad` y `MovingAverage` (664/664), y
    **con la app en marcha**: panel plegado, desplegado, valores vivos y preferencia persistida entre
    arranques.
  - **Esfuerzo:** bajo
  - **Depende de:** ninguna

- [x] **[T11-02] Todo lo que no fuera formatear vivía escondido en un menú** — **hecho (2026-09-01)** · Media
  - **Área:** UI / descubribilidad
  - **Ubicación:** [MainWindow.xaml](src/FormatDiskPro/UI/MainWindow.xaml) (`QuickBar`) ·
    [MainWindow.DriveInfo.cs](src/FormatDiskPro/UI/MainWindow.DriveInfo.cs) (`ApplyQuickBarLanguage`,
    espejo en `UpdateToolsMenuAvailability`)
  - **Qué pasaba:** la salud S.M.A.R.T., el benchmark y el historial —tres de las funciones que más
    justifican instalar la app— **no existían para quien no abriera `Herramientas`**. La ventana ofrecía
    formatear y nada más.
  - **Qué se hizo:** una barra de tres botones bajo el menú, con icono y etiqueta corta.
  - **Por qué esas tres y no otras, que es toda la decisión:** son **exactamente** las que ya tenían atajo
    de teclado (Ctrl+I / Ctrl+B / Ctrl+H), y por el mismo criterio que se escribió al dárselo: son las
    únicas que **no escriben nada**. Formatear, reinicializar, verificar capacidad, quitar protección y
    borrado seguro **se quedan en el menú**: una operación que borra datos no debe estar a un clic, y su
    confirmación reforzada existe para que llegar ahí cueste. La barra no añade un criterio nuevo, aplica
    el que ya había en un segundo sitio.
  - **Y de paso esquiva `T7-08` sin repetir su solución:** WinUI **no pinta el tooltip de un control
    deshabilitado**, así que un botón de icono apagado sería mudo — el fallo que `T7-08` tuvo que arreglar
    metiendo el motivo en el texto del ítem. Estas tres solo se apagan cuando **no hay unidad**, y
    entonces el propio selector, dos centímetros más arriba, ya dice «No hay unidades — conecta un
    dispositivo». El historial no se apaga nunca.
  - **Los botones no declaran `KeyboardAccelerator`:** el acelerador vive en su ítem del menú y
    declararlo dos veces lo duplicaría. El atajo se anuncia en el tooltip, que además lleva la frase
    larga («Salud del disco (S.M.A.R.T.)… (Ctrl+I)») y es el nombre de automatización del botón: «Salud»
    a secas, fuera del contexto visual de la barra, no dice qué hace.
  - **El estado se ESPEJA, no se recalcula:** `BtnHealth.IsEnabled = MnuHealth.IsEnabled`. Dos
    condiciones para la misma acción acabarían discrepando y el usuario vería un botón vivo sobre un menú
    apagado.
  - **Verificado:** con la app en marcha, en español; +4 claves × 5 idiomas.
  - **Esfuerzo:** bajo
  - **Depende de:** ninguna

- [x] **[T11-03] La tarjeta de unidad daba el mismo peso a «Conexión: USB» que a «Salud: Crítico»** — **hecho (2026-09-01)** · Media
  - **Área:** UI / jerarquía visual
  - **Ubicación:** [MainWindow.xaml](src/FormatDiskPro/UI/MainWindow.xaml) (cabecera de la tarjeta) ·
    [MainWindow.DriveInfo.cs](src/FormatDiskPro/UI/MainWindow.DriveInfo.cs) (`SetInfo`, `ClearHealthColor`)
  - **Qué pasaba:** seis líneas «Etiqueta: valor» en una rejilla de 2×3, **todas del mismo tamaño y del
    mismo color**. Las dos preguntas que se hacen al seleccionar una unidad —cuánto cabe y si está sana—
    había que buscarlas entre las otras cuatro.
  - **Qué se hizo:** tres niveles. La **capacidad** como dato principal (20 px, semibold), la **salud** a
    su derecha con un punto de color, y sistema de archivos, tipo y conexión en una línea de contexto
    atenuada. El **espacio libre** baja bajo la barra de ocupación, que es de lo que habla — antes estaba
    a tres líneas del dato con el que se compara. Mismo contenido, mismos seis controles, reordenados.
  - **La parte que no se ve, y es la que importa:** los rótulos desaparecen de la **pantalla**, no de la
    **accesibilidad**. `SetInfo` pinta el valor y pone la frase entera en `AutomationProperties.Name`, en
    un solo sitio para que no puedan separarse: un lector de pantalla sigue leyendo «Total: 930,5 GB».
    La jerarquía visual no se paga con información.
  - **El punto de color no sustituye al texto** (WCAG 1.4.1): repite en color lo que la palabra ya dice, y
    va en `AccessibilityView="Raw"`. Con salud desconocida se pone **gris** en vez de ocultarse — un hueco
    donde había un círculo se lee como que el dato cambió de sitio. Su pincel es **el mismo objeto** que
    el del texto, no una segunda derivación del mismo color.
  - **Verificado:** con la app en marcha (`931,5 GB` · `● Normal` · `NTFS · Disco fijo · NVMe · SSD` ·
    `Libre: 181,6 GB`). Sin colores nuevos: los dos estilos usan `ThemeResource` de Fluent y el punto
    reusa `HealthDialog.LevelBrush` / `SeverityPalette`, ya medidos.
  - **Esfuerzo:** bajo
  - **Depende de:** ninguna

- [x] **[T11-04] El panel de rendimiento pedía un clic para enseñar tres números** — **hecho (2026-09-01)** · Media
  - **Área:** UI / densidad
  - **Ubicación:** [MainWindow.xaml](src/FormatDiskPro/UI/MainWindow.xaml) (`PerfStrip`) ·
    [MainWindow.Performance.cs](src/FormatDiskPro/UI/MainWindow.Performance.cs)
  - **De dónde sale:** de ver `T11-01` funcionando. El `Expander` estaba bien resuelto y aun así era la
    pieza equivocada.
  - **El razonamiento que estaba mal en `T11-01`:** se plegó por el **alto** —la ventana es de tamaño fijo
    y el panel desplegado ocupaba ~230 px—. Pero un desplegable cobra su propio precio: un clic para ver
    un dato que se consulta de un vistazo, y un encabezado que hay que llenar con un resumen para que
    plegado diga algo. Ese precio se paga **siempre**; el del alto solo se pagaba desplegado. El error fue
    aceptar el alto como dado en vez de atacarlo.
  - **Qué se hizo:** tres columnas —etiqueta + valor arriba, barra de 4 px debajo—. La franja entera cabe
    en **~34 px**, menos que el encabezado que el `Expander` ocupaba **plegado**. Sin el problema del alto
    no queda motivo para plegar, así que la franja es fija: siempre visible, sin clic, sin resumen que
    inventar. La preferencia `ShowPerformance` se elimina — ya no hay nada que recordar.
  - **Lo que se sacrifica, y adónde va:** el pie de cada métrica (el pico, los núcleos, el consumo de la
    propia app) ya no cabe en pantalla y pasa al **tooltip**, junto al valor. No se pierde: estos
    controles **nunca se deshabilitan**, así que el tooltip sí se muestra — que era justo el problema de
    los ítems de menú de `T7-08`. Ese mismo texto es el nombre de automatización de cada columna.
  - **Las columnas no son iguales, y es deliberado** (`1.15* / 0.7* / 1.15*`): «100 %» de CPU siempre es
    corto y «13,5 GB / 31,9 GB» de RAM siempre es largo. A tercios, la RAM saldría truncada con hueco
    sobrante a su izquierda.
  - **El muestreo cambia de disparador:** ya no puede depender de si el panel está abierto, porque siempre
    lo está. Ahora corre mientras la ventana está **al frente** o hay una operación en curso. Con la app
    detrás no hay nadie mirando; con una operación en curso hay que seguir midiendo aunque el usuario se
    haya ido, o el pico tendría agujeros.
  - **Verificado:** build 0/0, 664/664 y con la app en marcha — la franja ocupa una línea y el contenido
    de arriba recupera el alto que el panel desplegado le quitaba.
  - **Esfuerzo:** bajo
  - **Depende de:** `T11-01`

---

## 📋 Progreso

| Fecha | Tarea | Notas |
|---|---|---|
| 2026-09-01 | **T12-07** | **Se retira la franja de rendimiento entera** (`T11-01` + `T11-04`). El motivo de peso: su justificación de partida era **falsa** — se defendió con «la única señal de vida era una barra de progreso» y el cronómetro del pie **ya escribía velocidad y ETA**, para las mismas dos operaciones. La fila de Disco duplicaba la línea de debajo; CPU y RAM decoraban. Cada fila fallaba por un motivo distinto, así que no había subconjunto que salvar. Fuera ~34 px permanentes, un servicio Win32, 41 pruebas y 55 cadenas (**626 unitarias**). Sobrevive lo que se sostiene solo: el color de la barra de progreso, el galón de scroll y `MutedText`. Lección: una petición de producto no exime de comprobar el problema que dice resolver. |
| 2026-09-01 | **T12-05** y **T12-06** | **`T12-05` salió de una captura del usuario**: un benchmark que terminó BIEN dejaba la barra llena y roja, igual que uno fallido — `FormatProgress` usaba el color de **acento del sistema** y en ese equipo el acento es rojo, así que `ShowError` no distinguía nada. Es la decisión que `CapacityBrush` ya había tomado («no debe usar el color de ACENTO del sistema»), sin aplicar aquí. Ahora el verde de `SeverityPalette` significa que va bien y el rojo que no, en cualquier equipo; **verificado en los dos estados** con la app en marcha, porque fijar `Foreground` a mano podía haber ganado al estado de error del control. `T12-06`: la barra de desplazamiento se deja a la vista cuando hay algo que desplazar — el degradado que se probó primero se descartó **con la app delante** (sobre Mica no hay fondo opaco que igualar y se leía como una franja clara). Y una corrección: el benchmark **no** alimenta la fila de Disco y no debe — su progreso es por ventana y contradiría su propia mediana. |
| 2026-09-01 | **T12-01** a **T12-04** | **Se abre y se cierra el Tier 12**, de una revisión de UI/UX. El primero es un **defecto medido**: `TextFillColorTertiaryBrush` da **3,29:1** en claro —por debajo de AA— y pintaba 18 controles, entre ellos las pistas que explican qué clúster elegir. El barrido no podía verlo porque solo medía los colores propios, que es **el mismo fallo que ese inventario existe para evitar**: ahora `TextContrastTests` recorre el XAML y mide lo que hay puesto, y `SeverityPalette.MutedText` (5,07:1 / 5,03:1) conserva el tercer nivel de jerarquía en vez de borrarlo. **Verificado en negativo.** Los otros tres: el botón primario pasa de «Iniciar» a **«Formatear H:»** (era el único control capaz de destruir un disco sin nombrarlo), el pie resume **`NTFS · 4 KB · rápido`** porque las opciones quedan bajo el pliegue y el botón no, y los presets bajan a la tarjeta que configuran. +3 unitarias (667). |
| 2026-09-01 | **T11-04** | El panel de rendimiento deja de ser un `Expander` y pasa a una **franja fija de tres columnas** (~34 px, menos que el encabezado que el desplegable ocupaba plegado). El razonamiento de `T11-01` estaba mal: plegar se justificó por el alto, pero el clic y el resumen del encabezado se pagan **siempre** y el alto solo desplegado — la respuesta era compactar, no plegar. Los pies de cada métrica van al tooltip, que aquí **sí** se muestra porque estos controles nunca se deshabilitan (`T7-08`). Se elimina la preferencia `ShowPerformance` y el muestreo pasa a depender de si la ventana está al frente. |
| 2026-09-01 | **T11-03** | La tarjeta de unidad deja de repartir seis datos con el mismo peso: **capacidad** como dato principal, **salud** con punto de color a su derecha, y FS/tipo/conexión en una línea de contexto atenuada; el espacio libre baja bajo la barra de ocupación, junto al dato con el que se compara. Los rótulos se van de la pantalla pero **no de la accesibilidad**: `SetInfo` pone la frase entera en `AutomationProperties.Name`, en un solo sitio. El punto no sustituye al texto (1.4.1) y con salud desconocida se pone gris en vez de desaparecer. |
| 2026-09-01 | **T11-02** | Las tres herramientas que **no escriben nada** —salud, benchmark, historial— salen del menú a una barra de acciones bajo la barra de menús. No es un criterio nuevo: son exactamente las tres que ya tenían atajo, y por el mismo motivo. Todo lo destructivo se queda dentro del menú. Esquiva `T7-08` (WinUI no pinta el tooltip de un control deshabilitado) porque solo se apagan **sin unidad**, y entonces el selector ya lo explica. El estado se espeja del ítem del menú en vez de recalcularse. |
| 2026-09-01 | **T11-01** | El pie deja de ser solo una barra: panel plegable con **disco (caudal + pico), CPU y RAM**, desplegado solo mientras hay operación. Lo que no enseña está tan decidido como lo que enseña — nada de «CPU del proceso» (el trabajo lo hacen procesos hijos y diría 0), nada de contadores de disco del sistema (medirían toda la máquina), y la barra de caudal se escala contra **su propio pico** porque no hay un máximo teórico honesto. Win32 y no `PerformanceCounter`: los nombres de PDH están traducidos y la app se instala en cinco idiomas. Colores de `SeverityPalette` con los mismos umbrales 80/90 que la barra de ocupación. **+41 unitarias (664/664)**, verificado con la app en marcha. |
| 2026-08-27 | **T10-01** | El camino de fallo de la cobertura deja de mentir y empieza a dejar pruebas. `Get-CoreCoverage` separa **`missing` · `unreadable` · `empty` · `nocore`**, que antes eran el mismo `$null` y el mismo mensaje —el que culpa a `coverlet.collector`, referenciado y presente las dos veces que ha fallado—. En `empty`/`unreadable` conserva el informe **fuera del repo** (dentro haría abortar el siguiente corte por `T9-01`) y repite la medición una vez con `--diag`, **sin que eso desbloquee nada**: el corte muere igual y solo cambia el veredicto. **Verificado con las 5 clasificaciones (5/5) y con el camino entero ejercitado**: conservó el informe, guardó 3,7 MB de diagnósticos y abortó. Queda `T10-02`: la causa. |
| 2026-08-26 | — | **Se abre el [Tier 10](#-tier-10--lo-que-solo-aparece-al-publicar-abierto-2026-08-26)** con **1 tarea** (`T10-01`, Media), y no de una revisión sino del **corte de la v1.25.0**: el primer intento abortó con el informe de cobertura **vacío** —el artefacto de `T8-06`— **con el arreglo de `T8-06` puesto y ejecutado**. No se reprodujo en tres intentos, incluido el caso que provocaba `T8-06`. Se abre reconociendo que **la causa no se conoce**, y la tarea va de dejar pruebas la próxima vez, no de reintentar hasta que salga. El corte reintentado publicó la v1.25.0 al 98,1 %. |
| 2026-08-26 | **T9-20** | Desinstalar ofrece borrar `%AppData%\FormatDiskPro` —preferencias e historial—, que antes quedaba en disco sin que nadie lo mencionara. Se **pregunta** (por defecto No) y en modo silencioso se conserva. **Trampa encontrada al validar:** un comentario `{ … }` de Pascal se cierra con la **primera** llave que aparezca, así que escribir `{app}` dentro terminaba el comentario a mitad y el resto se compilaba como código («'BEGIN' expected»). Queda anotado en el propio `.iss`. |
| 2026-08-26 | **T9-19** | `THIRD-PARTY-NOTICES.txt` declara su criterio y lo cumple: **(A) redistribuido** (.NET y WinAppSDK, MIT, compatibles con GPLv3) frente a **(B) solo construcción/pruebas**, donde entran los que faltaban —xUnit (Apache-2.0), FlaUI y coverlet— junto a Inno Setup, que ya estaba. Al ampliarlo, cinco líneas se pasaron de **78 columnas** y el visor de la app las **trunca** sin barra visible (`T6-14`): ahora hay una prueba que barre los dos textos legales y falla si alguna se pasa. |
| 2026-08-26 | **T9-18** | La comprobación de actualizaciones al arrancar es **opcional**: nueva preferencia en *Configuración*, activada por defecto, persistida y traducida a los cinco idiomas. Es la única conexión a Internet de la app y salía en cada arranque sin preguntar. Desactivarla **no** deja la app sin actualizar: *Ayuda → Buscar actualizaciones…* sigue ahí, con su verificación por SHA-256. El texto de privacidad ahora dice que es automática y cómo apagarla. +1 unitaria. |
| 2026-08-26 | **T9-17** | `DisableDirPage=yes`: el instalador deja de ofrecer elegir carpeta. `[InstallDelete]` vacía `{app}` entero antes de copiar —necesario para actualizar entre versiones con distinto conjunto de archivos—, y con la página de destino visible eso alcanzaba a cualquier carpeta que se apuntara. Aquí no hay nada que elegir: los datos viven en `%AppData%`. **Verificado compilando el instalador con ISCC.** |
| 2026-08-26 | **T9-14** | La cabecera de `CONTEXT.md` deja de llevar estado. Llevaba versión, recuento de pruebas y tiers abiertos, todo duplicado de §3 y del ROADMAP, y llegó a **contradecir a la fila de al lado**. Ahora solo lleva lo que no caduca —qué es el proyecto y dónde está cada cosa— y enlaza a §3, que pasa a ser la única fuente. Es la segunda vez que la duplicación envejece en este archivo; queda escrito dentro para que no haya una tercera. |
| 2026-08-26 | **T9-13** | `DecodeArguments` baja a `internal` (no la usaba la app, solo su prueba) y la prueba de codificación **se ancla al Base64 concreto** de UTF-16LE, calculado aparte. Una ida y vuelta contra un inverso propio no falla si ambos lados comparten el error, y lo que ejecuta el script no es nuestro decodificador: es `powershell.exe -EncodedCommand`. |
| 2026-08-26 | **T9-09** | `BuildVolumeScript` y `BuildComArgumentList` validan letra y sistema de archivos contra la lista blanca, como ya hacían `DiskService` (cinco métodos) y `ReinitDrive` (revalidando el plan). **No cerraba un agujero abierto** —se persiguió la ruta desde `settings.json` y está cerrada por el `IndexOf` del selector—: lo que evita es que la seguridad de la ruta que formatea dependa de que todos los llamantes validen antes. +6 unitarias. |
| 2026-08-26 | **T9-08** | Un `settings.json` ilegible se **aparta** a `settings.corrupt.json` en vez de quedarse a que el primer `Save()` lo pise, que se llevaba por delante los presets del usuario —lo único que la app no sabe reconstruir—. La ventana lo registra en el historial; `AppSettings` no conoce el historial y no se le da esa dependencia por una línea. +3 unitarias. |
| 2026-08-26 | **T9-06** | La galería comprueba que la ventana que ha abierto **es la app**: si no aparece el selector de unidades, aborta explicando que lo más probable es que sea el diálogo «You must install or update .NET» y cómo publicar self-contained. Antes fotografiaba ese diálogo sin rechistar, y la mitigación vivía solo en la prosa de `CONTEXT.md`. |
| 2026-08-26 | **T9-05** | La galería lleva la cuenta: resumen final con guardadas / omitidas / fallidas y **código de salida distinto de cero** si no está completa. Antes una toma perdida solo emitía un aviso y la corrida terminaba diciendo «Galería completada». Es la lección de `T2-12` —distinguir «omitido» de «correcto»— aplicada por fin a las capturas. |
| 2026-08-26 | **T9-04** | Cada toma declara su precondición y se le busca una unidad que la cumpla, en vez de usar todas la primera unidad no-sistema. Con un disco fijo grande, `main-fat32` (FAT32 no se oferta por encima de 32 GB) y `reinit` (solo extraíbles, `T7-02`) **no podían existir**: la galería salía con 22 de 26 y sin decirlo, y entre las que faltaban estaba la del diálogo destructivo. **Verificado: 26/26 sin `-Drive`.** |
| 2026-08-26 | **T9-02** | Nuevo `-ResumeRelease` para retomar un corte que murió después de etiquetar: reutiliza tag e instalador y solo crea el GitHub Release. Antes el mensaje aconsejaba «reintenta» y la validación de arriba abortaba con «el tag ya existe»: el consejo y la guarda se contradecían justo cuando el corte está a medias. Los dos mensajes de fallo ahora nombran el flag exacto, y el `-DryRun` en ese modo enseña el plan **real**, no los pasos que se salta. |
| 2026-08-26 | **T9-01** | El corte solo miraba los archivos **sin rastrear**; los **modificados** entraban enteros al commit vía `git add -u`, y como el instalador se compila desde el árbol de trabajo, **el binario publicado podía no corresponder al commit etiquetado**. Ahora se comprueban los dos casos, ambos bajo `-AllowDirty` —que es lo que su propia ayuda ya prometía— y el `-DryRun` dice cuántos modificados hay ahora mismo. Verificado contra el estado real del repo: 4 modificados, 0 sin rastrear → aborta (antes pasaba en verde). |
| 2026-08-26 | **T9-07** | La marca de tiempo del historial se escribía con la cultura del hilo, y con ella su **calendario**: en `th-TH` salía el año **2569**. Como `Parse` lee en invariante, la entrada no se rechazaba —quedaba 543 años en el futuro, encabezando el visor—. `InvariantCulture` explícita en los **dos** sitios que escriben (la línea del log y el nombre del CSV), reusando la constante `HistoryEntry.TimeFormat` con la que se lee. **Verificado por reversión**: el test cae mostrando `2569-08-26`. +1 unitaria. |
| 2026-08-26 | **T9-10** | *Estado de salud* dejaba de ser el valor crudo de Windows: la tarjeta decía «Salud: **Healthy**» en los cinco idiomas y el diálogo S.M.A.R.T. «**Healthy — Normal**», el inglés junto a su traducción. Las claves `health.level.*` y `SmartInfo.HealthLevel` ya existían y solo se usaban para el **color**; ahora dan también el texto. Con `Unknown` se conserva lo que reporte el disco, que es cuando no hay nada que traducir. +1 unitaria sobre las dos mitades del contrato. |
| 2026-08-26 | **T9-12** | `L.T(clave, args)` pasa `Culture` como proveedor. **No cambia ni una cadena hoy** —ninguna plantilla lleva especificador y los ~45 puntos de llamada ya preformatean—: cambia **quién sostiene la regla**, que hasta ahora era la disciplina de quien llama. Es el barrido que a `T6-12` le faltaba, y el que impide que reaparezca pasando un `long`. **Verificado por reversión.** +1 unitaria. |
| 2026-08-26 | **T9-11** | `status.formatting` y `status.wiping` usaban `...` mientras las otras 156 cadenas —incluidas sus vecinas de la misma barra de estado— usaban `…`. Corregidas las 10 (2 claves × 5 idiomas) y **barrido** del diccionario entero que falla si reaparece, al estilo del de `T6-09`. +1 unitaria. |
| 2026-08-26 | **T9-03** | `$env:GH_TOKEN` se limpia en el `finally` del corte. Era del **proceso**, así que sobrevivía en la terminal y lo heredaba cualquier proceso abierto después. Solo se borra si lo puso el script: si venía del entorno del usuario, es suyo. |
| 2026-08-26 | **T9-15** | El árbol de `CONTEXT.md` §2 listaba `Core/SecureWipe.cs` —que no existe: el borrado seguro vive solo en `Services/`— y omitía `Core/ErrorText.cs`, la pieza de `T8-02` de la que dependen los once sitios que informan de un error. |
| 2026-08-26 | **T9-16** | Comentario de `build-installer.ps1` que justificaba publicar a `%TEMP%` con que «el `.csproj` referencia el SDK como `1.8.*`». Se fijó a versión exacta precisamente por ese fallo. El motivo de MAX_PATH sigue siendo válido y se conserva; el del comodín, no. |
| 2026-08-26 | — | **Re-auditoría transversal (12 áreas) con la app en marcha: se abre el [Tier 9](#️-tier-9--re-auditoría-transversal-con-la-app-en-marcha-abierto-2026-08-26)** con **20 tareas** (1 Alta, 9 Medias, 10 Bajas). Ejecutada, no solo leída: build, 607 unitarias con cobertura, 37 UI tests con la USB, galería completa de capturas, arranque y contraste medidos. **Nada de lo cerrado antes se encontró cerrado en falso.** Lo más grave (`T9-01`) no está en la app sino en el corte: no comprueba el árbol sucio, así que puede publicar un instalador que no corresponde al commit etiquetado. Lo más revelador (`T9-04`/`T9-05`) es que la propia herramienta de auditoría perdía en silencio 4 de sus 26 tomas —una de ellas, la del diálogo destructivo—. |
| 2026-08-26 | **T8-06** | El corte abortaba con «se pidió cobertura y no se obtuvo informe» **siempre que había código sin compilar** —o sea, en cualquier corte real—: coverlet instrumentaba y MSBuild sobrescribía, dejando un informe de 235 bytes. El mensaje culpaba a un paquete que no faltaba. Se compila antes de medir (`dotnet build` + `dotnet test --no-build`). **Verificado desactualizando la compilación a propósito.** |
| 2026-08-26 | **T8-05** | *Novedades* enseñaba «## FormatDiskPro v1.24.0» con las almohadillas: el cuerpo del release empezaba por una **marca de orden de bytes** (`U+FEFF`) y con ella delante el `#` no estaba al principio de su línea. `U+FEFF` **no es espacio en blanco** para .NET (categoría `Cf`), así que ni `\s` ni `Trim()` lo quitan. Se elimina antes de nada. **Verificado por reversión** + 3 unitarias. |
| 2026-08-26 | **T8-04** | El corte de la v1.24.0 salió **en verde** y publicó una **plantilla genérica** como notas: sin `-NotesFile`, el script no leía el CHANGELOG. Ahora las notas salen de la sección de esa versión —que el propio script ya exige que exista— y se escriben **sin BOM**, que es lo que causó `T8-05`. El `-DryRun` dice de dónde saldrán. |
| 2026-08-26 | **T8-03** | Dos `catch` vacíos más de la misma familia: *Abrir archivo* del historial y los enlaces a GitHub/donación no producían **ningún** efecto visible al fallar. `History.Open()` deja salir la excepción y el diálogo la cuenta; `OpenUrl()` devuelve `bool` y quien llama enseña la dirección. *Ver en GitHub* solo se queda abierto si el navegador NO abrió. |
| 2026-08-26 | **T8-02** | Los errores podían salir **vacíos**: el mensaje de una excepción venida de WinRT es la cadena vacía cuando su `IRestrictedErrorInfo` no trae descripción. `ErrorText.Describe(ex)` respalda con tipo + `HRESULT`, y lo usan los once sitios que enseñan o registran un error. Una prueba barre las fuentes y falla si vuelve a aparecer el mensaje en crudo. Fue lo que diagnosticó `T8-01`. +6 unitarias. |
| 2026-08-26 | **T8-01** | ***Exportar CSV* nunca funcionó en una versión publicada.** El `FileSavePicker` de WinRT rechaza a los procesos elevados y la app siempre lo es: `COMException 0x80004005` en el acto, sin abrir ninguna ventana. Sustituido por el diálogo «Guardar como» de Windows por COM (`IFileSaveDialog`). **Medido con una sonda de UI** contra el .exe real —antes, ninguna ventana nueva; después, la ventana `Exportar CSV` de clase `#32770`—, convertida luego en prueba de regresión. No había NINGUNA prueba de la exportación: por ahí viajó el fallo. |
| 2026-08-26 | **T7-09** | Al mirar el menú arreglado se vio otra cosa: el marco de foco salía **cortado** en los diálogos. WinUI lo dibuja hacia fuera del control y el `ContentDialog` recorta su contenido, así que la raíz pegada al borde se comía 3 px del marco — en los **seis** diálogos, no solo en el historial. Recurso compartido `DialogContentPadding` = 3 px, dentro de `MinWidth`/`MaxWidth` (no toca `T6-07`). `LegalTextDialog` queda fuera **a propósito**: su ancho es la medida de `T6-14`. **Verificado por reversión** + 2 pruebas que barren los diálogos. |
| 2026-08-26 | **T7-08** | La comprobación a ojo dio **no**: WinUI no pinta el tooltip de un control deshabilitado —no existe el `ShowOnDisabled` de WPF— y el motivo de `T7-02` solo le llegaba al lector de pantalla. El motivo pasa al **texto visible del ítem** en corto («(unidad protegida)», «(solo extraíbles)», «(sin unidad)») × 5 idiomas. No a la `InfoBar`: el flyout se abre justo encima de ella, y el motivo es por ítem, no por ventana. De paso, el texto de esos siete ítems pasa a tener **un solo dueño** (`UpdateToolsMenuAvailability`); con dos, la etiqueta se perdía o se duplicaba. **Verificado por reversión** + 6 unitarias. |
| 2026-08-25 | **T7-07** | Las filas de historial y presets dejan de anunciarse con el `ToString()` del record —«HistoryRow { …, Accent = Microsoft.UI.Xaml.Media.SolidColorBrush }»— y los dos filtros del historial dicen qué filtran. El nombre va en el **contenedor** (`ContainerContentChanging`): dentro de la plantilla no cambia el del `ListViewItem`. **Verificado por reversión**: quitando los arreglos caen las tres pruebas de nombres y siguen verdes las dos de teclado. |
| 2026-08-25 | **T7-06** | Revisión con la app en marcha. **Desmintió su propia sospecha**: `SelectionMode="None"` no impide recorrer ni desplazar los `ListView` con teclado (0 % → 30 % con ↓ y AvPág). Foco inicial correcto en ambos diálogos y flyout de borrado de `T7-01` usable con teclado. Abrió **`T7-07`** (hecha) y **`T7-08`** (el tooltip del ítem apagado, que FlaUI no puede medir: no lo ve ni sobre un control habilitado que sí lo tiene). La sonda que lo midió se convirtió en 5 pruebas y se borró. |
| 2026-08-25 | **T7-04** | Atajos para los tres diagnósticos que no escriben nada (`Ctrl+I` salud, `Ctrl+B` benchmark, `Ctrl+H` historial) y `Ctrl+E` para exportar dentro del historial. Formatear/reinicializar/verificar **no llevan atajo a propósito**. Sin `KeyboardAcceleratorTextOverride`: el `MenuFlyoutItem` pinta solo el texto cuando el acelerador existe de verdad. Sacó a la luz que `MnuHistory_Click` no comprobaba `_isBusy` —le bastaba con que el menú se deshabilitara— y `Ctrl+H` llega sin pasar por el menú. **Verificado por reversión** con un UI test que pulsa el atajo sin abrir el menú. |
| 2026-08-25 | **T7-02** | *Herramientas* se ajusta a la unidad: lo que no aplica sale apagado **y con el motivo escrito** (tooltip + `HelpText`), en vez de aceptarse y rechazarse en un diálogo. Las condiciones son las guardas de `Operations.cs` copiadas una a una, y esas guardas **se quedan**: entre abrir el menú y pulsar, la unidad puede cambiar. chkdsk y benchmark no se apagan —el primero corre en solo lectura sobre el disco de sistema—. +1 UI test sobre el disco de sistema, que comprueba también que ningún ítem apagado se queda sin `HelpText`. |
| 2026-08-25 | **T7-05** | El buscador del historial pasa a `AutoSuggestBox` (botón de limpiar de serie, sugerencias apagadas) con nombre accesible propio en vez del placeholder —`T6-02`— y recuento «12 de 340», oculto con el historial vacío porque ahí el estado vacío ya habla. Los números se formatean con `L.Culture` **antes** de entrar en `L.T`: `string.Format` usa la cultura de Windows y por ahí volvía `T6-12`. +2 unitarias. |
| 2026-08-25 | **T7-03** | Pista bajo *Tamaño de unidad de asignación*, el único campo esotérico sin ayuda mientras el sistema de archivos —que casi todo el mundo sabe elegir— tenía la suya desde `T1-05`. No nombra ninguna opción: el combo lleva tamaños concretos y el recomendado llega **preseleccionado**, no hay un elemento «Predeterminado»; una prueba barre los cinco idiomas y falla si alguna traducción lo inventa. |
| 2026-08-25 | **T7-01** | Borrar un preset ya no es un clic irreversible: flyout de confirmación con **el nombre dentro** (en una fila de papeleras idénticas, «¿Eliminar?» no dice cuál). Mismo patrón que *Vaciar historial*, que es lo que hacía la inconsistencia. El flyout se captura en `Opening` porque su contenido vive en un `Popup` y no se sube hasta él por el árbol visual. +2 unitarias sobre el marcador `{0}` en los cinco idiomas. |
| 2026-08-25 | — | Revisión de UX/UI sobre el **código** de la UI (no sobre capturas), con el Tier 6 ya cerrado: **Tier 7 abierto** con 6 tareas, ninguna un defecto de corrección. |
| 2026-08-17 | **T6-14** | Los textos legales caben: `NoWrap` + ancho propio (430, la única excepción declarada al común de `T6-07`) + cuerpo a 10 px, que es una medida —a 10 px de Consolas entran ~78 columnas, y `LICENSE` mide 78— no un gusto. El primer intento (11 px) **truncaba** el texto sin barra visible: peor que el fallo, y lo cazó la captura. Las 3 líneas que aún no cabían eran de nuestro propio fichero de atribución; el texto MIT y la GPL no se tocaron. |
| 2026-08-17 | **T6-15** | Fuera el salto de maquetación de los 15 resúmenes de reinicialización. La prueba recorre cada `
` de los tres textos en cinco idiomas en vez de anclar las cadenas: caza también el que se cuele en una traducción futura. |
| 2026-08-17 | **T6-13** | *Novedades* deja de enseñar `*asteriscos*` y de partir los párrafos a mitad de frase: énfasis simple pareado y desenvolvido por bloques (viñeta, encabezado y salto forzado cierran; la continuación ajustada de una viñeta se le une). +9 unitarias, tres de ellas guardando lo que NO debe tocarse. |
| 2026-08-17 | **T6-12** | Los números que se muestran siguen al idioma de la app, no a Windows: nueva `L.Culture`, que `FormatBytes` usa por defecto. **No** se asigna a `CurrentCulture` —por ahí volvería `T1-01`— y hay una prueba con tr-TR que lo fija. Lo delataron cuatro pruebas que afirmaban el separador inglés con la app en español. |
| 2026-08-17 | **T6-11** | Galería completa (26 tomas, 2 temas) con la app en ejecución. Confirma `T6-01`, `T6-02`, `T6-06`, `T6-07`, `T6-10` y la barra de ocupación en ambos temas — y **abre `T6-13`, `T6-14` y `T6-15`**, que la primera ronda no podía ver. |
| 2026-08-17 | **T6-10** | Las dos opciones de chkdsk pasan a ser «command link»: título más una línea que dice qué cambia (reparar exige uso exclusivo y tarda mucho más). Nombre accesible explícito, porque el contenido dejó de ser una cadena. |
| 2026-08-17 | **T6-09** | «solo» sin tilde, y un barrido del diccionario entero que falla si reaparece en cualquier cadena. |
| 2026-08-17 | **T6-08** | Fuera el `Opacity="0.5"` que doblaba la atenuación. En WinUI un panel **no** se puede deshabilitar (`IsEnabled` es de `Control`), así que el helper apaga control por control y atenúa la etiqueta con `TextFillColorDisabledBrush` — un `TextBlock` no tiene estado deshabilitado. |
| 2026-08-17 | **T6-07** | El hallazgo estaba medio equivocado: los botones apilados de chkdsk evitan un truncado real de WinUI en PT/IT y se quedan. Lo que sí estaba mal era el **ancho** (siete diálogos, seis criterios): ahora dos tokens compartidos. La regla de dónde va cada botón queda escrita en `AppTheme.xaml`. |
| 2026-08-17 | **T6-06** | «Etiqueta del volumen» sin dos puntos en los cinco idiomas, como los otros dos encabezados de la misma tarjeta. +3 pruebas que fallan si alguno vuelve a puntuar. |
| 2026-08-17 | **T6-05** | El historial muestra los tamaños legibles (`small-fat32=2 GB`) vía `HistoryEntry.Humanize`, función de **presentación**: `history.log` y el CSV conservan el byte exacto y las entradas ya escritas se ven bien sin migrar. Lista blanca de claves para no tocar `code=1`. `Matches` busca en crudo **y** en legible, o teclear lo que se ve no encontraría nada. +11 unitarias. |
| 2026-08-17 | **T6-04** | Horas de encendido con separador de millares y equivalencia: `32,161 h (≈ 3.7 años)`. Nueva `SmartInfo.PowerOnEquivalent` (tramos días/meses/años, un decimal siempre para no pluralizar en cinco idiomas). +11 unitarias. |
| 2026-08-17 | **T6-12** | **Abierta** ese día, no cerrada: al poner un decimal junto a una palabra traducida se hizo visible que los números siguen la cultura de Windows y el texto el idioma de la app. Es anterior y afecta a toda la app (`FormatBytes` incluido). |
| 2026-08-17 | **T6-03** | La fila del eje solo se pinta si hay eje: nueva función pura `SmartInfo.HasSpindle` en `Core` (RPM=0 manda; sin RPM decide el medio; sin señal asume que gira, para no afirmar «SSD» sin saberlo). Fuera el literal `"SSD"`. +9 unitarias y verificación visual en SSD y en USB. |
| 2026-08-17 | **T6-02** | Fuera el placeholder que era la letra a teclear. Al quitarlo se vio que WinUI lo usaba como **nombre accesible** del campo (un lector de pantalla cantaba la respuesta): se le da nombre propio, `confirm.inputName` ×5 idiomas. La primera prueba pasaba con el fallo puesto —no veía el placeholder— y se reescribió contra el `Name`. Verificada por reversión. |
| 2026-08-17 | **T6-01** | El título de `ConfirmDialog` pasa a ser parámetro **obligatorio**: reinicializar dejaba de anunciarse como «Confirmar formato». Nueva clave `confirm.titleReinit` ×5 idiomas. +1 unitaria (los títulos difieren en los cinco) y +1 de UI, **verificada por reversión** sobre la app real. |
| 2026-08-17 | — | Revisión de UX/UI sobre las capturas del corte 1.22.0: **Tier 6 abierto** con 11 tareas (3 defectos · 7 refinamientos · 1 para completar la propia revisión, que no pudo correr contra la app por falta de terminal elevada). |
| 2026-08-13 | — | Auditoría inicial: 37 tareas abiertas (T0: 2 · T1: 9 · T2: 11 · T3: 10 · T4: 5). |
| 2026-08-13 | **T0-01** | Handler `UnhandledException` en `App.OnLaunched`: registra `CRASH:`, marca `Handled` y avisa. Nueva clave `crash.body`. |
| 2026-08-13 | **T0-02** | `catch` en los cuatro handlers de operación, vía el helper compartido `MainWindow.ReportOperationErrorAsync`. |
| 2026-08-13 | **T1-01** | Nuevo `Core/DriveLetter` (invariante de cultura); `IsSystemDrive` lo usa. +8 pruebas, incluida la de cultura turca. |
| 2026-08-13 | **T1-03** | Gris de «Cancelado» en tema claro: `#868686` (3.52:1) → `#6E6E6E` (**4.93:1**). Tema oscuro sin cambios (ya cumplía). |
| 2026-08-13 | **T1-05** | Descripciones de sistema de archivos movidas a `Localization` como `fs.desc.*` con PT/FR/IT reales. +5 pruebas. |
| 2026-08-13 | **T1-09** | `UpdateService.SafeAssetFileName` sanea el nombre de asset antes de `Path.Combine`. +12 pruebas. |
| 2026-08-13 | **T1-04** | Inventario único `SeverityPalette.All()` + barrido de contraste sobre él; los 4 consumidores delegan. +7 pruebas. |
| 2026-08-13 | **T3-05** | Cerrada con T1-04: `AppTheme.xaml` y `CONTEXT.md` §4 ya enumeran las dos únicas excepciones reales de color. |
| 2026-08-13 | **T1-06** | `FormatPreset.NameKey` + `Presets.DisplayName`: los 5 integrados se traducen a EN/PT/FR/IT. Los duplicados se comparan contra el nombre mostrado. +1 prueba. |
| 2026-08-13 | **T1-07** | `LocalizationCoverageTests`: barrido del código fuente contra tablas de cadenas fuera de `Localization/`, más el anclaje de los presets a claves reales. +5 pruebas. |
| 2026-08-13 | **T1-08** | La firma Authenticode deja de eximir del SHA-256 mientras el proyecto no firme (era un bypass real, no endurecimiento preventivo). +`WTD_REVOKE_WHOLECHAIN`. +3 pruebas. |
| 2026-08-13 | **T2-05** | Costura `CapacityVerifier.RunInAsync`: por fin se prueba la detección de unidades falsificadas **sin** una unidad falsificada. +`Core/OperationFailure`. +29 pruebas. |
| 2026-08-13 | **T3-11** | *(hallada por T2-05)* `History.Log` aplana el texto multilínea: una caída ya no se parte en decenas de entradas fantasma. |
| 2026-08-14 | **T2-13** | Los `catch` de `T0-02` ejecutados **de verdad**: desmontaje forzado de la USB a mitad de *Verificar capacidad* y *Benchmark*. +2 pruebas de UI (23 → 25). |
| 2026-08-14 | **T1-02** | `format.com /Y` (cuelgue reproducido y arreglado) + `ExtractPercent` en 6 idiomas. **Tier 1 cerrado.** +10 pruebas. |
| 2026-08-15 | **T2-12** | `release.ps1` lee el `.trx` y dice cuántos UI tests se omitieron **y por qué**, al terminar y en el resumen final. |
| 2026-08-15 | **T2-06** | El `.sha256` se empareja por nombre con el instalador elegido; si no está el suyo, la actualización se rechaza. +5 pruebas. |
| 2026-08-15 | **T2-07** | Tope de 512 bytes al leer el checksum (cabecera **y** flujo real). Nueva clave `update.checksumUnreadable`. +1 prueba. |
| 2026-08-15 | **T2-09** | `Core/HistoryRotation` + rotación a `history.1.log` a los 2 MB. El visor lee las dos generaciones; *Borrar* se lleva ambas. +13 pruebas. |
| 2026-08-15 | **T3 (9)** | Pulido completo: CSV que ya no falla en silencio, `ReinitDrive` sin O(n²), `LoadHealthAsync` con su error propio, `AppSettings.Load` normaliza de verdad, `L.T` no lanza, iconos decorativos fuera del árbol UIA, contraseña de firma como `SecureString`, RNG criptográfico en el borrado. **Tier 3 cerrado.** +9 pruebas. |
| 2026-08-15 | **T2-08** | `Services/FormatProcess` + `UI/DeviceChangeWatcher` extraídos; el resto en `partial class`. `MainWindow.xaml.cs` 2107 → **753** líneas. **T2 cerrado.** |
| 2026-08-15 | **T2-04** | Cobertura medida (`coverlet`) y **exigida** en el corte: `Core/` al **97.1 %**, mínimo 90 %. |
| 2026-08-15 | **T2-11** | `SECURITY.md` (canal privado + lo que no es vulnerabilidad), `CONTRIBUTING.md`, plantillas de issue y de PR. Enlazados desde el README. |
| 2026-08-15 | **T2-03** | La relectura de *Verificar capacidad* deja de poder servirse de la caché del SO (`FILE_FLAG_NO_BUFFERING` + buffer alineado). +2 pruebas. |
| 2026-08-15 | **T2-01** | `StatusText` como región activa `Polite` + notificación UIA en los hitos (inicio/fin/error/cancelación), nunca por tick. +1 prueba de UI. |
| 2026-08-15 | **T2-02** | Error de etiqueta `Assertive` y vinculado al campo con `DescribedBy`. +1 prueba de UI. *(Un `Collapsed` no está en el árbol UIA.)* |
| 2026-08-15 | — | `NonSystemDriveFact`: las 4 pruebas de la tarjeta de opciones se **omiten** en una máquina de un solo disco, en vez de fallar. |
| 2026-08-16 | ~~**T4-03**~~ | ❌ **Descartada.** Contradecía la decisión `#13` (no se firma) desde que se escribió. El pipeline ya admite firmar; lo que falta es un certificado, que es una compra y no una tarea. Ver *Decisiones cerradas*. |
| 2026-08-16 | **T4-04** | README de 3 a 12 capturas (6 pantallas × 2 temas), regeneradas sobre el publish. Arreglados 3 disparos del script que esperaban por tiempo en vez de por elemento. |
| 2026-08-16 | **T4-02** | Inyección de dependencias: 11 servicios con interfaz + raíz de composición `AppServices` + costura `IProcessRunner`. Los caminos de error se prueban sin hardware. +35 pruebas (398 → 433). |
| 2026-08-16 | **T4-01** | `CHANGELOG.md` (Keep a Changelog) con las 28 versiones, fechas tomadas de los tags de git. `release.ps1` aborta si falta la sección de la versión a publicar. |
| 2026-08-16 | **T4-05** | `SetFormEnabled` → `SetControlsEnabled` y fuera el comentario «same as MainForm»: últimos rastros de Windows Forms. |
| 2026-08-16 | **T5-01** | `Core/PartitionPlan.cs`: el layout deja de ser un `long?` y pasa a ser un plan validable (13 motivos tipados, con índice de la partición culpable). `ParseNewLetters` en plural con índice de partición. `ReinitDrive` ejecuta N particiones y revalida el plan antes de `Clear-Disk`. La UI sigue mandando **una** partición: sin cambio de comportamiento. +40 pruebas (453 → 493). |
| 2026-08-16 | **T5-02** | El sobrante deja de morir sin asignar: fila *«El resto del disco»* en la tarjeta de opciones (dejarlo sin asignar —por defecto— o crear una segunda partición en exFAT/NTFS con su etiqueta). La FAT32 va **siempre primera**, y la UI dice por qué (`opt.restNote`). +16 pruebas (493 → 509). |
| 2026-08-16 | **T5-05** | `FullLifecycle` extendido con un cuarto paso: FAT32 de 1 GB + resto en exFAT. Comprueba el **disco físico** (número de particiones y espacio sin asignar), no el diálogo de éxito — que diría lo mismo si la segunda partición no se creara. Verificado: 2 particiones, **0 MB sin asignar**. |
| 2026-08-16 | **T5-03** | Los marcadores del script se emiten **según se alcanzan**, no agrupados al final: con `ErrorActionPreference='Stop'`, un fallo en la segunda partición abortaba antes de imprimir el de la primera, y «no se creó nada» era indistinguible de «la primera salió bien». Dos marcadores, `PART:` (creada) y `LETTER:` (creada **y** formateada), porque son dos estados distintos. **No se revierte** — hay una prueba que exige que no se lance ningún proceso de limpieza. +12 pruebas (509 → 521). |
| 2026-08-16 | ~~**T5-04**~~ | ❌ **Descartada.** N particiones no cabe en una ventana de tamaño fijo sin convertirse en otra pantalla, MBR limita a 4 de todos modos y el caso real lo cubren dos. El motor sigue admitiendo N: lo limitado es la interfaz. Ver *Tier 5*. |
| 2026-08-15 | ~~**T2-10**~~ | ❌ **Descartada.** Se implementó el workflow y se revirtió: el testing de este proyecto es **solo local**. Ver *Decisiones cerradas*. |

**Estado: AUDITORÍA CERRADA (2026-08-16).** 39/40 completadas · 2 descartadas (`T2-10` CI, `T4-03` firma)
· **0 abiertas** (T0: 0 · T1: 0 · T2: 0 · T3: 0 · **T4: 0**).

**Estado global (2026-08-26): 0 tareas abiertas.** El [Tier 9](#️-tier-9--re-auditoría-transversal-con-la-app-en-marcha-abierto-2026-08-26) se abrió y se cerró el mismo día, **20/20**.
Tiers 1–9 de producto y Tiers 0–8 de calidad: cerrados. La re-auditoría del 2026-08-26 **verificó en el
código** las tareas cerradas que tocaban sus áreas y **no encontró ninguna cerrada en falso**: `T8-01`
(*Exportar CSV* por `IFileSaveDialog`), `T8-02` (`ErrorText.Describe` en los once sitios), `T7-09`
(`DialogContentPadding`), `T6-12`/`T7-05` (números por `L.Culture`), `T1-04` (inventario de color, cuatro
pares vueltos a medir sobre los PNG) y `T1-01` (`DriveLetter` invariante) siguen en pie. Lo que sí apareció
es el **reverso** de dos de ellas: `T6-12` es el único arreglo sin barrido que lo vigile (`T9-12`), y la
convención de invariante que `T1-01` estableció no llegó a los dos sitios que **escriben** la fecha del
historial (`T9-07`).

Las dos descartadas no son deuda aparcada: **son decisiones tomadas**, y viven en
*[Decisiones cerradas](#-decisiones-cerradas-no-reabrir)* con su porqué. `T2-10` (CI) se llegó a
implementar y se revirtió; `T4-03` (firmar) contradecía la decisión `#13` desde el día en que se escribió.

El **[Tier 5](#-tier-5--ocurrencias-para-features-existentes)** —ampliaciones de features ya entregadas,
**aparte de las 40** porque añade funcionalidad y no remediación— quedó **cerrado el 2026-08-16**:
4 completadas y `T5-04` descartada.

**Tiers 0 y 1 cerrados**, y no solo razonados: los tres fallos que «necesitaban hardware o un Windows
extranjero para verificarse» acabaron reproducidos aquí (`T0-01`/`T0-02` con la USB desmontada a la fuerza,
`T1-02` con un VHD y sin escribir en stdin). `T3-11` se añadió **ya resuelta**: la encontró `T2-05` al
recorrer el camino de error de punta a punta. `T2-12` se añadió el 2026-08-13 al ejecutar por fin la suite
de UI completa sobre hardware real (23/23 en verde), que destapó un test roto desde la v1.15.2 y dos cortes
publicados sin notarlo — y se **cerró el 2026-08-15**: el corte ya no puede volver a llamar «verde» a una
cobertura que no ejerció.

Al cerrar el Tier 5 (v1.22.0): build Release **0 advertencias / 0 errores**, **521/521** unitarias
(eran 289 antes de la auditoría) y **28** de UI (eran 23).

> **`T1-04` cierra el patrón que la auditoría encontró tres veces.** El barrido ya no recorre una función
> concreta sino el **inventario** `SeverityPalette.All()`: añadir un color semántico es lo mismo que
> ponerlo bajo test, no hay forma de hacer una cosa sin la otra. Se verificó revirtiendo el gris a
> `#868686` — el test falla y nombra el color. Fuera de `Core` solo quedan los colores del caption de la
> barra de título, excluidos a propósito (cromo de ventana, superpuesto sobre Mica/Acrylic: no hay fondo
> fijo contra el que medir).
>
> **`T1-06`/`T1-07` lo cierran también en la i18n.** El problema no era que faltara un test, sino que
> `EveryEntry_HasFiveNonEmptyTranslations` cubría menos de lo que su nombre sugiere: comprobaba que **lo
> registrado** estuviera traducido, no que **lo mostrado** estuviera registrado. `LocalizationCoverageTests`
> ataca el otro lado — recorre el código fuente buscando tablas de cadenas fuera de `Localization/`, que es
> la forma exacta que tomó el fallo de `T1-05`. Verificado por reversión: quitarle la clave a un preset y
> reintroducir un `Dictionary<string,string>` en `MainWindow` hace fallar cuatro pruebas, cada una nombrando
> al culpable con fichero y línea. Incluye un test que comprueba que el propio patrón sigue reconociendo el
> diccionario real de `Localization.cs`: un barrido que ha dejado de detectar nada no se distingue de uno
> limpio.
>
> **`T1-08` es el aviso de que una nota de auditoría también puede estar mal.** La tarea se archivó como
> «endurecimiento preventivo, no explotable hoy» porque el proyecto no firma. Al reafirmarse esa decisión y
> volver a mirar el código, resultó ser justo al revés: **precisamente porque no firmamos**, una firma válida
> solo puede aparecer en un binario ajeno, y ese atajo saltaba la única verificación que existe antes de
> ejecutar como administrador. Verificado con `dotnet.exe` —firmado de verdad, no simulado—: con el atajo
> activo, un ejecutable de Microsoft se acepta como nuestro instalador.
>
> **`T2-05` valió por lo que encontró, no por lo que verificó.** Al recorrer el camino de error entero
> apareció `T3-11`: `T0-01` había empezado a registrar trazas de pila completas en un log de una entrada
> por línea, así que **cada caída partía el historial en decenas de entradas fantasma clasificadas como
> «información»**. Lo introdujo el arreglo de la mañana y pasó desapercibido justamente porque no había
> pruebas del camino de error. También quedó bajo test lo que da sentido a la verificación de capacidad —
> detectar una unidad falsificada—, que hasta hoy solo lo ejercitaba un test de UI de 57 minutos sobre una
> USB **auténtica**, es decir, sobre el único caso en el que la detección nunca se dispara.
>
> **Ya está verificado** (`T2-13`): desmontando la USB por la fuerza a mitad de *Verificar capacidad* y
> *Benchmark*, la app sobrevive, avisa, registra y vuelve a estado ocioso. Y la prueba **discrimina**: con
> el `catch` neutralizado, el historial recibe `CRASH:` en vez de `VERIFY ERROR` y falla. Eso valida a la
> vez la red global de `T0-01` y el `catch` de `T0-02`, que hasta hoy solo estaban razonados.

<!-- Al completar una tarea: marcar [x] arriba y añadir aquí una fila con la fecha absoluta y el commit. -->

### Áreas auditadas sin hallazgos

Constan aquí para que no se vuelvan a revisar sin motivo:

- **SEO** — **no aplica**: aplicación de escritorio, sin superficie web indexable.
- **Diseño responsivo** — **no aplica** en el sentido web. La ventana es de tamaño fijo por decisión firme,
  y la adaptación que sí importa (DPI/escalado por monitor, `PerMonitorV2`, `SizeAndCenterWindow`) está
  resuelta y acotada al área de trabajo.
- **Inyección de comandos** — **revisado, sin hallazgos**. Todo PowerShell va por `-EncodedCommand`
  (Base64 UTF-16LE), las letras de unidad se validan antes de interpolar, las etiquetas se escapan
  (`'`→`''`) y `format.com`/`chkdsk.exe` reciben `ArgumentList`. No se encontró ninguna ruta de inyección.
- **Secretos en el repositorio** — **revisado, sin hallazgos**. No hay credenciales; `.gitignore` excluye
  `*.pfx`/`*.p12`/`*.snk`/`*.cer` y el token de `gh` se toma de la credencial de git cacheada sin
  imprimirse.
- **Dependencias de terceros** — **revisado, sin hallazgos**. Superficie mínima y deliberada:
  `Microsoft.WindowsAppSDK` fijado a versión exacta, más xUnit y el SDK de pruebas. Sin paquetes
  transitivos de riesgo ni licencias incompatibles con GPLv3.

#### Añadido por la re-auditoría del 2026-08-26 (medido, no estimado)

- **Contraste de color** — **medido sobre los PNG de la galería, sin hallazgos.** Cuatro pares
  muestreados píxel a píxel: prompt rojo del diálogo destructivo `#C42B1C` sobre `#F3F3F3` = **5,1:1**;
  encabezado de tarjeta claro `#9E0912` sobre `#FBFBFB` = **8,11:1**; el mismo en oscuro `#FB9D8B` sobre
  `#2B2B2B` = **6,94:1**; botón primario blanco sobre `#D20E1E` = **5,49:1**. Los cuatro por encima del
  4,5:1 de AA para texto normal.
- **Rendimiento** — **medido, sin hallazgos.** Arranque hasta ventana visible: 669 / 532 / 533 ms
  (**media 578 ms**) sobre el publish self-contained. Working set ~180 MB, memoria privada ~120 MB
  —normales en WinUI 3 self-contained—. Publish: 214,6 MB en 509 archivos. Sin `Thread.Sleep` ni esperas
  artificiales en `src/`, y ningún `.Result`/`.Wait()` bloqueante.
- **Inyección de comandos (segunda pasada, rutas nuevas)** — **revisado, sin hallazgos.** Se persiguieron
  las tres vías que no existían en la revisión de 2026-08-13: **presets del `settings.json`** (cerrada:
  `MnuPreset_Click` exige `FileSystemPicker.Items.IndexOf(...) >= 0`), **plan de particiones** (cerrada:
  `PartitionPlan.Validate` exige lista blanca y `ReinitDrive` **revalida** antes de construir el script) y
  **etiqueta de volumen** (cerrada: `ValidateLabel` + escape `'`→`''` + `ArgumentList`). Importa porque
  `settings.json` vive en `%AppData%`, se escribe **sin elevación** y lo lee un proceso **elevado**: era
  la ruta con premio, y no llega. Queda solo la coherencia de convención de `T9-09`.
- **Tipografía francesa** — **revisado, sin hallazgos.** El espacio fino antes de la puntuación doble está
  aplicado de forma consistente: 48 cadenas con ` :` y 14 con ` ?`/` !`. Los `{0}:` sin espacio son
  designadores de unidad («D:»), no puntuación, y ahí la ausencia de espacio es correcta.
- **Fugas de proceso** — **revisado, sin hallazgos.** `FormatProcess` no libera el handle a propósito
  (lo entrega a quien llama); se comprobó que sus **dos** únicos llamantes lo guardan en `_activeProcess`
  y que `EndOperation` lo libera desde un `finally`. El resto de servicios usa `using`.
- **Plantillas de GitHub** — **revisado, sin hallazgos.** `SECURITY.md` (canal privado y política de
  versiones), `CONTRIBUTING.md`, `PULL_REQUEST_TEMPLATE.md` y las tres plantillas de issue están
  completas y son coherentes con que no haya CI.
