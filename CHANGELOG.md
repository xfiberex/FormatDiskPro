# Changelog

Todos los cambios relevantes de **FormatDiskPro**, versión a versión.

El formato sigue [Keep a Changelog](https://keepachangelog.com/es-ES/1.1.0/) y el proyecto usa
[Versionado Semántico](https://semver.org/lang/es/).

> **Dónde está cada cosa.** Este archivo es el resumen por versión: **qué cambió**. El **por qué** —las
> decisiones, los fallos que las provocaron y lo que se aprendió— vive en
> [`CONTEXT.md`](CONTEXT.md) (§4 *Decisiones* y su *Registro de cambios*), y lo que queda por hacer, en
> [`ROADMAP.md`](ROADMAP.md). Las notas de cada publicación están además en
> [GitHub Releases](https://github.com/xfiberex/FormatDiskPro/releases).

---

## [Sin publicar]

Nada todavía.

---

## [1.21.0] — 2026-08-16

**Cierre de la auditoría del 2026-08-13** (39/40 completadas, 2 descartadas). Es un corte de
**mantenimiento**: no cambia nada de lo que la app hace ni de lo que el usuario ve — lo que cambia es
que sus fallos ahora se pueden probar, y que el repositorio cuenta lo que hace.

### Añadido

- **Inyección de dependencias en `Services`** (`T4-02`): los servicios dejan de ser clases `static` y se
  construyen en una raíz de composición (`AppServices`), que `App` pasa a `MainWindow` y esta a los
  diálogos. Los que lanzan procesos reciben un `IProcessRunner`, la costura que permite probar sus
  **caminos de error sin hardware**: un `chkdsk` que devuelve 2, un `Clear-Disk` que falla a mitad, un
  `powershell.exe` que ni arranca. **+35 pruebas** (398 → 433), ninguna de ellas tocando un disco.
- **Este `CHANGELOG.md`** (`T4-01`): hasta ahora el registro vivía repartido entre `CONTEXT.md` y las
  notas de GitHub Releases. `release.ps1` **aborta** si falta la sección de la versión que se publica.

### Cambiado

- **El README pasa de 3 a 12 capturas** (`T4-04`): ventana principal, S.M.A.R.T., chkdsk, reinicializar,
  confirmación destructiva e historial, cada una en tema claro **y** oscuro. Regeneradas sobre el publish
  self-contained; las anteriores eran de la v1.15.2.
- `MainWindow.SetFormEnabled` pasa a llamarse `SetControlsEnabled` (`T4-05`): último resto de nomenclatura
  de Windows Forms, migrado a WinUI 3 en la v1.2.0.

### Corregido

- `tools/capture-screenshots.ps1`: tres tomas de la galería (`reinit`, `confirm`, `checkdisk`) esperaban
  por **tiempo** en vez de por elemento, así que sobre una unidad extraíble válida fotografiaban la
  ventana principal **sin el diálogo** — *Reinicializar* hace antes dos consultas a PowerShell para la
  guarda del disco de sistema. Ahora esperan al control que deben retratar.
- **Dos de las pruebas nuevas eran intermitentes** y se arreglaron antes de que llegaran a molestar:
  recogían los reportes de `IProgress<T>` con `Progress<T>`, que los **entrega de forma asíncrona**, así
  que la aserción competía con ellos (pasaban aisladas, fallaban en la suite completa). Ahora usan un
  `IProgress<T>` síncrono. El servicio no cambia: en la app esa asincronía es la correcta.

### Retirado

- **`T4-03` «firmar el instalador», descartada.** Contradecía la decisión `#13` (no se firma), que es la
  razón de existir de la verificación por SHA-256. El pipeline **ya admite** firmar; lo que falta es un
  certificado, que es una compra y no una tarea de ingeniería. Con esto la auditoría del 2026-08-13 queda
  **cerrada**: 39/40 completadas y 2 descartadas.

---

## [1.20.0] — 2026-08-15

**Tier 3 de la auditoría, cerrado.** Nueve arreglos de pulido: ninguno cambia lo que la app hace; varios
cambian lo que la app **cuenta** cuando algo va mal.

### Corregido

- La **exportación CSV** del historial ya no falla en silencio: un `catch { }` se tragaba cualquier error
  de escritura y el usuario se quedaba creyendo que había exportado.
- La **salud ilegible** se muestra como «no disponible» en vez de dejar la tarjeta a medias.
- `AppSettings.Load` **normaliza de verdad** al cargar: un `settings.json` con 0 pasadas ya no entra vivo.
- Un **marcador mal escrito en una traducción** ya no tumba la pantalla que solo quería mostrar un texto.
- `LoadHealthAsync` deja de ser `async void` sin perder su manejo de errores.

### Cambiado

- El **borrado seguro** usa RNG criptográfico.
- Los **iconos decorativos** salen del árbol de accesibilidad, puesto en el estilo y no icono a icono.
- La contraseña del certificado de firma pasa a `SecureString` en los scripts.

## [1.19.0] — 2026-08-15

**Tier 2 de la auditoría, cerrado.**

### Corregido

- **Verificar capacidad ya no puede leer de la caché del sistema** (`FILE_FLAG_NO_BUFFERING` en la
  relectura): se acabaron los falsos OK en unidades falsificadas pequeñas — el peor resultado posible
  aquí, decirle a alguien que su unidad es auténtica cuando no lo es.

### Añadido

- **Cobertura de `Core/` medida (97,1 %) y exigida en el corte** (mínimo 90 %).
- `SECURITY.md`, `CONTRIBUTING.md`, plantillas de issue y de PR.

### Cambiado

- `MainWindow` repartido por asunto: **2 107 → 753 líneas**, sin cambiar comportamiento. Salen
  `Services/FormatProcess` y `UI/DeviceChangeWatcher`.

## [1.18.0] — 2026-08-15

### Añadido

- **Se puede seguir la app con un lector de pantalla**: `StatusText` es región activa y las operaciones
  anuncian sus **hitos** (inicio, fin, error, cancelación) — nunca cada tick de porcentaje.
- El error de etiqueta **se lee desde su propio campo** (`DescribedBy` + región activa asertiva).
- El corte de release **declara qué cobertura de UI no ejerció**, con el motivo de cada prueba omitida.

### Corregido

- El `.sha256` **se empareja por nombre** con el instalador que verifica; antes se tomaba el último que
  apareciera, y bastaba otro asset con checksum para rechazar la actualización buena.
- El checksum se lee **acotado a 512 bytes**.
- `history.log` **rota** a los 2 MB, y el visor lee las dos generaciones.

## [1.17.0] — 2026-08-14

### Corregido

- **El formato completo dejaba colgada la app en un Windows que no fuera ES/EN**: se respondía `Y`/`S` por
  la entrada estándar. Ahora se pasa `/Y` y no se escribe nada.
- La **barra de progreso del formato** entiende la palabra «porcentaje» en seis idiomas; antes se quedaba
  clavada en 0 durante todo un formato completo sin que nada fallara.
- El historial **deja de partirse** al registrar un error multilínea (una caída se convertía en decenas de
  entradas fantasma).

### Seguridad

- **Una firma Authenticode válida deja de eximir del SHA-256.** `WinVerifyTrust` responde a «¿lo firmó
  *alguien* de confianza?», no a «¿lo firmamos *nosotros*?», y el proyecto no firma: esa rama solo podía
  activarse sobre un binario ajeno, con el instalador ejecutándose **como administrador** al otro lado.

### Añadido

- Los **presets integrados** aparecen traducidos en los cinco idiomas.

## [1.16.0] — 2026-08-13

### Corregido

- **La app deja de cerrarse ante fallos de E/S**: red de seguridad global para excepciones no controladas
  y `catch` propio en los cuatro handlers de operación que no lo tenían.
- La **guarda del disco de sistema** compara letras con cultura invariante (con cultura turca dejaba de
  proteger).
- **Contraste WCAG AA** en el estado «Cancelado» del historial (estaba en 3,52:1).
- Las **descripciones de sistema de archivos** se ven en los cinco idiomas, no solo ES/EN.

## [1.15.2] — 2026-07-20

### Corregido

- Truncación del botón de chkdsk en PT/IT (los botones pasan a apilarse).
- La **barra de capacidad** deja de heredar el color de acento del sistema: en un equipo con acento rojo,
  un disco medio vacío se leía como alarma.

## [1.15.1] — 2026-07-13

### Añadido

- **UI tests en el pipeline de release** (`-UiTests`): un corte no sale si la app real falla. Las pruebas
  con precondición ausente **se omiten** en vez de fallar.
- Instalador probado **end-to-end** (instalación limpia + actualización in-place).

### Corregido

- Los **metadatos del `.exe` publicado** estaban corrompidos: el `.csproj` acumulaba una capa de mojibake
  por release, durante 14 versiones.

## [1.15.0] — 2026-07-12

### Seguridad

- **El instalador se verifica antes de ejecutarse elevado**: firma Authenticode o, en su defecto, SHA-256
  contra el asset `*.exe.sha256`. Sin ninguna de las dos, se borra y no se ejecuta.
- **Neutralización de fórmulas** en la exportación CSV del historial.

### Añadido

- **Contraste WCAG AA medido por tests** en los colores de severidad: un color mal elegido rompe el build.
- **Build reproducible**: versión exacta del Windows App SDK y publicación a `%TEMP%` (MAX_PATH).

## [1.14.1] — 2026-07-05

### Corregido

- Mantenimiento de las pruebas de UI.

## [1.14.0] — 2026-07-02

### Añadido

- **Partición FAT32 pequeña al reinicializar** discos grandes, con tamaño seleccionable (1–32 GB): resuelve
  el flasheo de BIOS/UEFI desde un USB grande, cuya utilidad solo lee FAT32.

### Corregido

- `Clear-Disk` no siempre deja el disco en RAW; afectaba a *toda* la operación de reinicializar.

## [1.13.0] — 2026-07-02

### Añadido

- Pulido UX/UI: aviso de unidad protegida como `InfoBar`, foco inicial y Enter en la confirmación, barra de
  capacidad usado/libre, iconos por tipo de unidad, estado vacío del selector, salud coloreada en la
  tarjeta principal, validación inline de la etiqueta, **progreso en la barra de tareas** y estado de error
  en la barra de progreso.

## [1.12.0] — 2026-07-01

### Añadido

- **Relicencia a GNU GPL v3.0**, con el texto embebido en el `.exe`.
- Disclaimer de uso destructivo, avisos de terceros y **aviso de privacidad** (sin telemetría).
- **Donaciones voluntarias**. Ninguna función se bloquea ni es de pago.

## [1.11.0] — 2026-06-27

### Añadido

- Umbrales de color **y texto de estado** en S.M.A.R.T., con botón *Actualizar*.
- **Historial filtrable y exportable a CSV**.
- **Editar y reordenar** presets.
- Accesibilidad transversal: nombres accesibles, aceleradores de menú, F5.
- **Autorefresco de unidades** al conectar o desconectar (`WM_DEVICECHANGE`, con debounce).

## [1.10.1] — 2026-06-26

### Corregido

- Adaptación a **DPI/escalado**: ventana dimensionada por DPI y diálogos con `MaxWidth`.

## [1.10.0] — 2026-06-26

### Añadido

- **IOPS** junto a los MB/s del 4 KiB aleatorio.
- **Pasadas de borrado seguro** configurables (1/3/7).
- **Idioma automático** en el primer arranque.
- **Changelog** en el aviso de actualización, antes de descargar.

## [1.9.1] — 2026-06-25

### Corregido

- Correcciones de una revisión de código.

## [1.9.0] — 2026-06-23

### Cambiado

- **Benchmark** refinado a perfil CrystalDiskMark: secuencial Q8 + 4 KiB aleatorio, sin caché del sistema,
  mediana de tres pasadas.

## [1.8.0] — 2026-06-22

### Añadido

- **Presets personalizados** persistidos.
- **Cinco idiomas** (ES/EN/PT/FR/IT), con test de completitud.
- **Aviso al terminar** (sonido + parpadeo), solo si la ventana no está en primer plano.

## [1.7.1] — 2026-06-22

### Corregido

- El diálogo de novedades no aparecía al actualizar desde una versión sin `LastVersionSeen`.

## [1.7.0] — 2026-06-22

### Añadido

- **Reinicializar unidad** (USB con particiones raras o RAW): limpia el disco y recrea una partición
  usable. Solo extraíbles, con guardas reforzadas.
- **Benchmark** no destructivo de lectura/escritura.
- Diálogo de **novedades** tras actualizar.

## [1.6.0] — 2026-06-21

### Añadido

- **chkdsk**: *Solo comprobar* (solo lectura) o *Comprobar y reparar* (`/f`); la reparación queda bloqueada
  en el disco de sistema.
- **Protección de escritura**: se detecta y se ofrece quitarla al pulsar Iniciar.

## [1.5.0] — 2026-06-21

### Añadido

- **S.M.A.R.T. ampliado**: temperatura, horas de encendido, desgaste, RPM y errores, en diálogo dedicado.

## [1.4.0] — 2026-06-21

### Añadido

- **Persistencia de configuración** (idioma, tema, última unidad).
- **ETA y velocidad (MB/s)** en operaciones largas.
- **Borrado seguro con progreso real** (sobrescritor propio; sustituye a `cipher /w`).
- **Visor de historial** integrado.

## [1.3.0] — 2026-06-21

### Cambiado

- Rediseño UI/UX: tarjetas y acento del sistema.

## [1.2.2] — 2026-06-20

### Corregido

- El cierre para auto-actualizar quedaba bloqueado por el estado «ocupado». **La auto-actualización
  silenciosa funciona desde aquí.**

## [1.2.1] — 2026-06-19

### Corregido

- **La 1.2.0 crasheaba al iniciar**: faltaba el `.pri` propio de la app en el publish.

## [1.2.0] — 2026-06-19

### Cambiado

- Migración de **Windows Forms a WinUI 3**.

> ⚠️ **Versión obsoleta y rota: no usar.** Corregida en la 1.2.1.

## [1.1.0] — 2026-06-18

### Añadido

- Arquitectura por capas, endurecimiento, pruebas, actualizaciones automáticas e instalador.

---

[Sin publicar]: https://github.com/xfiberex/FormatDiskPro/compare/v1.21.0...HEAD
[1.21.0]: https://github.com/xfiberex/FormatDiskPro/releases/tag/v1.21.0
[1.20.0]: https://github.com/xfiberex/FormatDiskPro/releases/tag/v1.20.0
[1.19.0]: https://github.com/xfiberex/FormatDiskPro/releases/tag/v1.19.0
[1.18.0]: https://github.com/xfiberex/FormatDiskPro/releases/tag/v1.18.0
[1.17.0]: https://github.com/xfiberex/FormatDiskPro/releases/tag/v1.17.0
[1.16.0]: https://github.com/xfiberex/FormatDiskPro/releases/tag/v1.16.0
[1.15.2]: https://github.com/xfiberex/FormatDiskPro/releases/tag/v1.15.2
[1.15.1]: https://github.com/xfiberex/FormatDiskPro/releases/tag/v1.15.1
[1.15.0]: https://github.com/xfiberex/FormatDiskPro/releases/tag/v1.15.0
[1.14.1]: https://github.com/xfiberex/FormatDiskPro/releases/tag/v1.14.1
[1.14.0]: https://github.com/xfiberex/FormatDiskPro/releases/tag/v1.14.0
[1.13.0]: https://github.com/xfiberex/FormatDiskPro/releases/tag/v1.13.0
[1.12.0]: https://github.com/xfiberex/FormatDiskPro/releases/tag/v1.12.0
[1.11.0]: https://github.com/xfiberex/FormatDiskPro/releases/tag/v1.11.0
[1.10.1]: https://github.com/xfiberex/FormatDiskPro/releases/tag/v1.10.1
[1.10.0]: https://github.com/xfiberex/FormatDiskPro/releases/tag/v1.10.0
[1.9.1]:  https://github.com/xfiberex/FormatDiskPro/releases/tag/v1.9.1
[1.9.0]:  https://github.com/xfiberex/FormatDiskPro/releases/tag/v1.9.0
[1.8.0]:  https://github.com/xfiberex/FormatDiskPro/releases/tag/v1.8.0
[1.7.1]:  https://github.com/xfiberex/FormatDiskPro/releases/tag/v1.7.1
[1.7.0]:  https://github.com/xfiberex/FormatDiskPro/releases/tag/v1.7.0
[1.6.0]:  https://github.com/xfiberex/FormatDiskPro/releases/tag/v1.6.0
[1.5.0]:  https://github.com/xfiberex/FormatDiskPro/releases/tag/v1.5.0
[1.4.0]:  https://github.com/xfiberex/FormatDiskPro/releases/tag/v1.4.0
[1.3.0]:  https://github.com/xfiberex/FormatDiskPro/releases/tag/v1.3.0
[1.2.2]:  https://github.com/xfiberex/FormatDiskPro/releases/tag/v1.2.2
[1.2.1]:  https://github.com/xfiberex/FormatDiskPro/releases/tag/1.2.1
[1.2.0]:  https://github.com/xfiberex/FormatDiskPro/releases/tag/v1.2.0
[1.1.0]:  https://github.com/xfiberex/FormatDiskPro/releases/tag/v1.1.0
