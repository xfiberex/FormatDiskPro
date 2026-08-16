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
> | **Parte 2** (al final) | **Backlog de remediación** de la auditoría técnica del **2026-08-13**. Abierta. | `T0-01`–`T4-05` |
>
> Al final de la Parte 2 hay además un **Tier 5 — Ocurrencias para features existentes** (`T5-01`–`T5-05`):
> ampliaciones de lo ya entregado, **no** parte de la auditoría (que sigue siendo de 40 tareas).

## 🏁 Estado

**Parte 1 — funcionalidad: TERMINADA (2026-07-13).** Tiers 1–9 completados; no hay características
pendientes. Lo que queda fuera está **deliberadamente** fuera — incluidas las dos decisiones que definen el
producto y **no se van a reabrir**: la app corre **siempre elevada** y su ventana es de **tamaño fijo**.

**Parte 2 — calidad: ABIERTA (2026-08-13), 38/40 al 2026-08-16.** Una auditoría técnica transversal
(código, seguridad, rendimiento, accesibilidad, i18n, arquitectura, QA, documentación, DevOps) encontró
**37 puntos de mejora**, ninguno de ellos una característica nueva. Que la funcionalidad esté cerrada no
cierra la calidad: ver **[Parte 2](#parte-2--backlog-de-remediación-auditoría-2026-08-13)**. Las **dos que
quedan** dependen de algo ajeno al código: un certificado de firma (`T4-03`) y una tanda de capturas
regeneradas (`T4-04`).

**Tier 5 — ocurrencias: CERRADO (2026-08-16).** «Funcionalidad terminada» no significa «sin huecos»: usar
lo entregado revela dónde una característica se queda a medio camino. El hueco era real: *FAT32 pequeña*
dejaba el resto del disco **sin asignar**, y recuperarlo obligaba a salir a una herramienta de Windows.
**4 completadas** (`T5-01`, `T5-02`, `T5-03`, `T5-05`) y **1 descartada** por decisión de producto
(`T5-04`, N particiones). Viven en el **[Tier 5](#-tier-5--ocurrencias-para-features-existentes)**, aparte
de la auditoría y aparte del historial cerrado de la Parte 1.

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
> **Única excepción, y está marcada como tal:** el **Tier 5** del final no viene de la auditoría y sí añade
> funcionalidad. Va aquí por continuidad de numeración, no porque forme parte de las 40.
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
| | **Total** | **45** | |

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

- [ ] ~~**[T2-10] CI de solo unitarias en GitHub Actions**~~ — ❌ **DESCARTADA (2026-08-15)**
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
  ya producía 12 en claro y oscuro. *(Estaba listado como pulido opcional en `CONTEXT.md` §6.)*
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
  *(Estaba listado como pulido opcional en `CONTEXT.md` §6.)*
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

## 📋 Progreso

| Fecha | Tarea | Notas |
|---|---|---|
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

Las dos descartadas no son deuda aparcada: **son decisiones tomadas**, y viven en
*[Decisiones cerradas](#-decisiones-cerradas-no-reabrir)* con su porqué. `T2-10` (CI) se llegó a
implementar y se revirtió; `T4-03` (firmar) contradecía la decisión `#13` desde el día en que se escribió.

El **[Tier 5](#-tier-5--ocurrencias-para-features-existentes)** también quedó **cerrado el 2026-08-16**
(4 completadas + `T5-04` descartada). **No** formaba parte de la auditoría: añade funcionalidad, que es
justo lo que ninguna tarea `T0`–`T4` hace.
**Aparte de las 40**, el **Tier 5** (5 tareas, abierto desde el 2026-08-15) recoge ampliaciones de features
ya entregadas; no cuenta en este progreso porque no es remediación.
**Tiers 0 y 1 cerrados**, y no solo razonados: los tres fallos que «necesitaban hardware o un Windows
extranjero para verificarse» acabaron reproducidos aquí (`T0-01`/`T0-02` con la USB desmontada a la fuerza,
`T1-02` con un VHD y sin escribir en stdin). `T3-11` se añadió
**ya resuelta**: la encontró `T2-05` al recorrer el camino de error de punta a punta.
`T2-12` se añadió el 2026-08-13 al ejecutar por fin la suite de UI completa sobre hardware real
(23/23 en verde), que destapó un test roto desde la v1.15.2 y dos cortes publicados sin notarlo — y se
**cerró el 2026-08-15**: el corte ya no puede volver a llamar «verde» a una cobertura que no ejerció.
Build Release **0 advertencias / 0 errores**; suite **388/388** unitarias (eran 289; +99 nuevas) y **27**
de UI (eran 23).

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
