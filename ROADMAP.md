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

## 🏁 Estado

**Parte 1 — funcionalidad: TERMINADA (2026-07-13).** Tiers 1–9 completados; no hay características
pendientes. Lo que queda fuera está **deliberadamente** fuera — incluidas las dos decisiones que definen el
producto y **no se van a reabrir**: la app corre **siempre elevada** y su ventana es de **tamaño fijo**.

**Parte 2 — calidad: ABIERTA (2026-08-13).** Una auditoría técnica transversal (código, seguridad,
rendimiento, accesibilidad, i18n, arquitectura, QA, documentación, DevOps) encontró **37 puntos de mejora**,
ninguno de ellos una característica nueva. Que la funcionalidad esté cerrada no cierra la calidad: ver
**[Parte 2](#parte-2--backlog-de-remediación-auditoría-2026-08-13)**.

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
- **CI con GitHub Actions — descartado (2026-07-12).** Un runner hospedado **no puede** ejecutar los UI tests
  (necesitan sesión elevada y la USB física de pruebas), así que solo duplicaría los unitarios que
  `release.ps1` ya corre antes de cada corte, con menos cobertura. Misma decisión que en WingetUSoft.
  *(Matizado por la auditoría en `T2-10`: el argumento vale para los UI tests, pero deja los **unitarios**
  sin ejecutar en ningún PR externo. Se propone CI **solo de unitarios**, sin reabrir lo de los UI tests.)*

---

# Parte 2 — Backlog de remediación (auditoría 2026-08-13)

> **Qué es esto.** Una revisión técnica transversal del repositorio completo (12 áreas) traducida a tareas
> ejecutables. **Ninguna añade funcionalidad**: todas corrigen, endurecen o miden lo que ya existe. El
> informe que las originó no se repite aquí — cada tarea es autocontenida.
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
| | **Total** | **40** | |

**Orden recomendado:** T0 → T1-01/02 (guardas destructivas) → T1-03/04 (a11y medible) → T1-05/06/07
(i18n) → T1-08/09 (updater) → T2 → T3.

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
  - **Verificado sobre hardware el 2026-08-13.** Al desactivar el `catch` de `T0-02` y desmontar la USB a
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
  - **Verificado sobre hardware el 2026-08-13 para *Verificar capacidad* y *Benchmark*** (`T2-13`), las dos
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

- [ ] **[T1-02] `format.com`: pasar `/Y` en vez de teclear "Y"/"S" por stdin**
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
  - **Nota:** *pendiente de verificación* — el fallo se dedujo del código, no se reprodujo. Revisar de paso
    el prompt de etiqueta de volumen que `format.com` hace en discos **fijos** (no extraíbles).

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

- [ ] **[T2-01] Anunciar estado y progreso a los lectores de pantalla**
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

- [ ] **[T2-02] Asociar el error de etiqueta con su campo**
  - **Área:** Accesibilidad
  - **Ubicación:** `src/FormatDiskPro/UI/MainWindow.xaml:143-148`
  - **Qué hacer:** `LabelErrorText` aparece bajo `VolumeLabelBox` sin relación programática: un lector de
    pantalla en el campo no lee el motivo del error. Añadir
    `AutomationProperties.DescribedBy` apuntando a `LabelErrorText` y marcarlo `LiveSetting="Assertive"`.
  - **Criterio de aceptación:** escribir una etiqueta inválida hace que Narrador lea el mensaje sin salir
    del campo.
  - **Esfuerzo:** bajo
  - **Depende de:** ninguna

- [ ] **[T2-03] Releer sin caché en la verificación de capacidad**
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
  - **Nota:** *pendiente de verificación* — razonado sobre el código, no medido. En unidades grandes el
    conjunto excede la caché y la detección sigue funcionando; el riesgo se concentra en volúmenes chicos.

- [ ] **[T2-04] Medir la cobertura de pruebas, no solo contarlas**
  - **Área:** QA
  - **Ubicación:** `tests/FormatDiskPro.Tests/FormatDiskPro.Tests.csproj:11-15`
  - **Qué hacer:** «289 pruebas» es un recuento, no una cobertura: hoy nadie sabe qué porcentaje de `Core`
    y de los helpers de `Services` se ejercita. Añadir `coverlet.collector`, generar el informe en
    `release.ps1` y fijar un umbral mínimo para `Core/`.
  - **Criterio de aceptación:** `dotnet test --collect:"XPlat Code Coverage"` produce informe; el corte
    falla si `Core/` baja del umbral acordado.
  - **Esfuerzo:** medio
  - **Depende de:** ninguna

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

- [ ] **[T2-06] Emparejar el `.sha256` con el instalador que verifica**
  - **Área:** Seguridad
  - **Ubicación:** `src/FormatDiskPro/Services/UpdateService.cs:83-104`
  - **Qué hacer:** `ParseRelease` se queda con el **último** asset que termina en `.sha256`, sin comprobar
    que corresponda al `.exe` elegido. Con más de un asset el emparejamiento es arbitrario. Elegir el
    checksum cuyo nombre sea `<nombre-del-exe>.sha256`.
  - **Criterio de aceptación:** test con un release de varios assets que comprueba el emparejamiento.
  - **Esfuerzo:** bajo
  - **Depende de:** ninguna

- [ ] **[T2-07] Acotar el tamaño de la descarga del checksum**
  - **Área:** Seguridad
  - **Ubicación:** `src/FormatDiskPro/Services/UpdateService.cs:236`
  - **Qué hacer:** `Http.GetStringAsync(checksumUrl, ct)` lee la respuesta entera en memoria sin límite.
    Leer como máximo unos cientos de bytes (un SHA-256 en hex ocupa 64).
  - **Criterio de aceptación:** una respuesta desmedida se rechaza en vez de materializarse.
  - **Esfuerzo:** bajo
  - **Depende de:** ninguna

- [ ] **[T2-08] Adelgazar `MainWindow.xaml.cs` (2 070 líneas)**
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

- [ ] **[T2-09] Rotar `history.log`**
  - **Área:** Código
  - **Ubicación:** `src/FormatDiskPro/Services/History.cs:21-30`
  - **Qué hacer:** el historial solo crece; `HistoryDialog` lo parsea entero en memoria en cada apertura
    (`HistoryDialog.xaml.cs:64`). Rotar por tamaño (p. ej. 2 MB → `history.1.log`).
  - **Criterio de aceptación:** superado el umbral se rota y el visor sigue mostrando lo reciente.
  - **Esfuerzo:** bajo
  - **Depende de:** ninguna

- [ ] **[T2-10] CI de solo unitarias en GitHub Actions**
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

- [ ] **[T2-12] Que el corte diga cuánta cobertura de UI llevó realmente**
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

- [x] **[T2-13] Ejercitar los `catch` de las operaciones quitando la unidad de verdad**
  *(añadida y resuelta el 2026-08-13)*
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

- [ ] **[T2-11] `SECURITY.md`, `CONTRIBUTING.md` y plantillas de issue**
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

---

## 🟢 Tier 3 — Pulido y mantenimiento

- [ ] **[T3-01] La exportación CSV no puede fallar en silencio**
  - **Área:** UX / manejo de errores
  - **Ubicación:** `src/FormatDiskPro/UI/HistoryDialog.xaml.cs:105-112`
  - **Qué hacer:** `catch { }` se traga cualquier fallo de escritura: el usuario elige destino, no ve nada
    y **cree que exportó**. Mantener el «no romper el diálogo», pero informar del fallo.
  - **Criterio de aceptación:** exportar a una ruta sin permiso muestra un mensaje.
  - **Esfuerzo:** bajo · **Depende de:** ninguna

- [ ] **[T3-02] Evitar el `sb.ToString()` por chunk en `ReinitDrive`**
  - **Área:** Rendimiento
  - **Ubicación:** `src/FormatDiskPro/Services/ReinitDrive.cs:99-106`
  - **Qué hacer:** rematerializa el búfer completo en cada lectura (O(n²)). Buscar los marcadores `STAGE:`
    sobre el fragmento nuevo más un solapamiento corto, como ya hace el `carry` de
    `CheckDisk.cs:70-84`.
  - **Criterio de aceptación:** mismo reporte de etapas, sin `ToString()` dentro del bucle.
  - **Esfuerzo:** bajo · **Depende de:** ninguna

- [ ] **[T3-03] `LoadHealthAsync` no es un handler: no debe ser `async void`**
  - **Área:** Código
  - **Ubicación:** `src/FormatDiskPro/UI/MainWindow.xaml.cs:353`
  - **Qué hacer:** convertir a `async Task` y consumirlo explícitamente desde
    `DrivePicker_SelectionChanged`.
  - **Criterio de aceptación:** no quedan `async void` fuera de handlers de eventos.
  - **Esfuerzo:** bajo · **Depende de:** T0-02

- [ ] **[T3-04] Corregir la documentación de validación de `AppSettings`**
  - **Área:** Documentación
  - **Ubicación:** `src/FormatDiskPro/Services/AppSettings.cs:39-49` vs `:79-95`
  - **Qué hacer:** los `<summary>` dicen que `SecureWipePasses` y `SmallFat32SizeGb` «se validan **al
    cargar**», pero `Load()` solo deserializa; la normalización ocurre en la UI (`InitWipePasses`,
    `InitSmallFat32Size`). O normalizar en `Load()` (preferible) o corregir el texto.
  - **Criterio de aceptación:** documentación y comportamiento coinciden.
  - **Esfuerzo:** bajo · **Depende de:** ninguna

- [x] **[T3-05] «No hay colores hardcodeados» ya no es cierto**
  - **Área:** Documentación
  - **Ubicación:** `src/FormatDiskPro/UI/Theme/AppTheme.xaml:10`; `CONTEXT.md` §4 *Otros*
  - **Qué hacer:** el comentario afirma que no hay colores fijos, y `CONTEXT.md` reconoce **dos**
    excepciones deliberadas (`SeverityPalette` y la barra de capacidad). En realidad hay más:
    `MainWindow.ProtectedColor`/`DriveBrush`/`UpdateCaptionButtonColors` y `HistoryDialog.ColorFor`.
    Ajustar ambos textos tras `T1-04`, cuando la lista real sea la definitiva.
  - **Criterio de aceptación:** la documentación enumera exactamente las excepciones que quedan.
  - **Esfuerzo:** bajo · **Depende de:** T1-04

- [ ] **[T3-06] `L.T(clave, args)` puede lanzar pese a prometer que no**
  - **Área:** Código
  - **Ubicación:** `src/FormatDiskPro/Localization/Localization.cs:61-64`
  - **Qué hacer:** `T(string)` es defensivo («nunca lanza»), pero la sobrecarga con `params` llama a
    `string.Format`, que lanza `FormatException` si una traducción trae un placeholder mal escrito.
    Envolver en `try/catch` y devolver la plantilla sin formatear.
  - **Criterio de aceptación:** test con una traducción de placeholder roto que no lanza.
  - **Esfuerzo:** bajo · **Depende de:** ninguna

- [ ] **[T3-07] Formato de la apertura de `L.Map`**
  - **Área:** Estilo
  - **Ubicación:** `src/FormatDiskPro/Localization/Localization.cs:67-68`
  - **Qué hacer:** la primera entrada va pegada a la llave (`new()\n{        ["section.drive"]`), lo que
    descoloca el diccionario respecto al resto del archivo.
  - **Criterio de aceptación:** la primera clave se alinea con las demás.
  - **Esfuerzo:** bajo · **Depende de:** ninguna

- [ ] **[T3-08] Sacar los iconos decorativos del árbol de automatización**
  - **Área:** Accesibilidad
  - **Ubicación:** `src/FormatDiskPro/UI/MainWindow.xaml:79,102,131,160`
  - **Qué hacer:** los `FontIcon` de encabezado son puramente decorativos y hoy los recorre el lector de
    pantalla. Marcarlos `AutomationProperties.AccessibilityView="Raw"`.
  - **Criterio de aceptación:** el recorrido con Narrador no lee glifos sin significado.
  - **Esfuerzo:** bajo · **Depende de:** ninguna

- [ ] **[T3-09] No pasar la contraseña del certificado por línea de comandos**
  - **Área:** Seguridad / DevOps
  - **Ubicación:** `src/FormatDiskPro/installer/build-installer.ps1:39` y `:85`
  - **Qué hacer:** `-CertPassword` es `[string]` y se pasa a `signtool` como `/p <valor>`: queda visible en
    la línea de comandos del proceso (cualquier proceso local la ve) y en el historial de PowerShell. Usar
    `[SecureString]` o una variable de entorno.
  - **Criterio de aceptación:** firmar no expone la contraseña en la línea de comandos.
  - **Esfuerzo:** bajo · **Depende de:** ninguna
  - **Nota:** hoy no se firma (`#13`); aplica solo a quien use el flujo opcional.

- [ ] **[T3-10] Documentar (o cambiar) el RNG del borrado seguro**
  - **Área:** Seguridad / documentación
  - **Ubicación:** `src/FormatDiskPro/Services/SecureWipe.cs:85`
  - **Qué hacer:** la pasada «aleatoria» usa `System.Random`, un PRNG no criptográfico. Para **destruir**
    datos previos da igual el origen de la aleatoriedad, pero el nombre «borrado seguro» invita a suponer
    otra cosa. O usar `RandomNumberGenerator.Fill` (coste despreciable frente a la E/S) o decirlo en el
    `<remarks>`, que ya es honesto sobre la limitación de TRIM.
  - **Criterio de aceptación:** código y documentación coinciden sobre qué garantiza la pasada aleatoria.
  - **Esfuerzo:** bajo · **Depende de:** ninguna

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

- [ ] **[T4-01] `CHANGELOG.md` en la raíz** — hoy el registro vive repartido entre `CONTEXT.md` y las notas
  de GitHub Releases; un `CHANGELOG.md` estándar (Keep a Changelog) es lo que espera quien llega al repo.
  *Área: Documentación · Esfuerzo: bajo · Depende de: ninguna*

- [ ] **[T4-02] Inyección de dependencias en `Services`** — todos son `static`, lo que hace imposible
  probar los caminos de error sin hardware (raíz de `T2-05`). Es un rediseño, no un arreglo.
  *Área: Arquitectura · Esfuerzo: alto · Depende de: T2-05*

- [ ] **[T4-03] Firmar el instalador** — reabriría la decisión `#13`, cerrada el 2026-06-24. Solo tiene
  sentido si aparece presupuesto para un certificado; haría a `T1-08` relevante en producción.
  *Área: Seguridad · Esfuerzo: alto · Depende de: ninguna*

- [ ] **[T4-04] Más capturas en el README** — hoy 3; el modo galería de `tools/capture-screenshots.ps1` ya
  produce 12 en claro y oscuro. *(Ya listado como pulido opcional en `CONTEXT.md` §6.)*
  *Área: Documentación · Esfuerzo: bajo · Depende de: ninguna*

- [ ] **[T4-05] Renombrar el `Name` interno del formulario** — resto de la migración desde Windows Forms.
  *(Ya listado como pulido opcional en `CONTEXT.md` §6.)*
  *Área: Limpieza · Esfuerzo: bajo · Depende de: ninguna*

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
| 2026-08-13 | **T2-13** | Los `catch` de `T0-02` ejecutados **de verdad**: desmontaje forzado de la USB a mitad de *Verificar capacidad* y *Benchmark*. +2 pruebas de UI (23 → 25). |

**Estado:** 15/40 completadas · **25 abiertas** (T0: 0 · T1: **1** · T2: 11 · T3: 9 · T4: 5).
Del Tier 1 solo queda `T1-02`, que necesita un Windows no ES/EN para verificarse. `T3-11` se añadió
**ya resuelta**: la encontró `T2-05` al recorrer el camino de error de punta a punta.
`T2-12` se añadió el 2026-08-13 al ejecutar por fin la suite de UI completa sobre hardware real
(23/23 en verde), que destapó un test roto desde la v1.15.2 y dos cortes publicados sin notarlo.
Build Release **0 advertencias / 0 errores**; suite **359/359** (eran 289; +70 pruebas nuevas).

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
