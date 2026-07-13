# FormatDiskPro — Hoja de ruta

> **Qué hay aquí:** las características agrupadas por **tiers**, con dónde vive cada una en la arquitectura
> por capas (`Core` lógica pura testeable · `Services` efectos colaterales · `UI` WinUI 3 · `Localization`).
>
> **Qué NO hay aquí:** el detalle de cómo se resolvió cada cosa y por qué — eso vive en
> [`CONTEXT.md`](CONTEXT.md) (§4 *Decisiones* y el *Registro de cambios*).
>
> **Propósito del proyecto:** **formatear, diagnosticar y gestionar unidades en Windows**. Todo lo que hay
> aquí cabe dentro de eso; lo que no, está al final y está fuera **a propósito**.

## 🏁 Estado: proyecto TERMINADO (2026-07-13)

**Tiers 1–9 completados. No hay ninguna tier abierta ni trabajo pendiente.** Lo que queda fuera está
**deliberadamente** fuera — incluidas las dos decisiones que definen el producto y **no se van a reabrir**:
la app corre **siempre elevada** y su ventana es de **tamaño fijo**.

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
