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

### Cambiado

- **La barra de ocupación pinta ahora el espacio libre, no solo el usado**, y pasa a tener bloque propio
  dentro de la tarjeta *Unidad*: separador, línea `Ocupación` — `Usado 780,9 GB / 930,5 GB` y la barra
  debajo, de 6 px y con las esquinas rectas en vez de la píldora anterior. Antes el hueco era la pista de
  1 px de un `ProgressBar`: en una unidad recién formateada (0 % usada) no se veía nada, y el espacio
  usado no aparecía en cifras por ningún lado. La barra de progreso de las operaciones no cambia.
- El relleno «usado» del **tema claro** se oscurece de `#8A8A8A` a `#5C5C5C`. Con el espacio libre pintado
  al lado, la frontera usado/libre se quedaba en 2.62:1, por debajo del 3:1 que WCAG exige a un objeto
  gráfico; ahora es 5.07:1.
- El nombre accesible de la barra incluye el porcentaje («Espacio utilizado: 43 %») en los cinco idiomas.
- **El diálogo de *Comprobar errores* explica sus dos opciones** (`T6-10`). Antes eran dos botones con el
  nombre a secas: ahora cada uno lleva debajo qué hace y qué cuesta — *Solo comprobar* no cambia nada y
  deja la unidad utilizable; *Comprobar y reparar* corrige, pero necesita uso exclusivo de la unidad y
  puede tardar mucho más.
- «sólo» → «solo» en la casilla de compresión (`T6-09`): la RAE retiró esa tilde en 2010.
- **Todos los diálogos tienen ya el mismo ancho** (`T6-07`). Había seis criterios distintos y el de
  *Comprobar errores* no tenía ninguno, así que salía más estrecho: abrir dos seguidos hacía saltar la
  ventana.
- **«Etiqueta del volumen» pierde los dos puntos** (`T6-06`), como los otros dos campos de su tarjeta.
- **Las opciones desactivadas se ven mejor** (`T6-08`): se atenuaban dos veces, así que los desplegables
  quedaban más apagados de la cuenta. Ahora usan el aspecto deshabilitado del propio tema de Windows, y
  sus etiquetas lo acompañan.
- **Las horas de encendido se leen** (`T6-04`). Antes: `32161 h`. Ahora: `32.161 h (≈ 3,7 años)`, con
  separador de millares y la equivalencia en días, meses o años según lo que dé una cifra útil.
- **El historial muestra los tamaños en unidades, no en bytes** (`T6-05`): `small-fat32=2 GB` en vez de
  `small-fat32=2147483648`. Solo cambia lo que se ve, así que las entradas ya registradas también se leen
  mejor; `history.log` y el CSV exportado conservan el número exacto. El buscador encuentra las dos formas.
- **Los números siguen ahora al idioma de la app, no al de Windows** (`T6-12`). Con la interfaz en español
  sobre un Windows en inglés salía `223.6 GB` y `32,161 h (≈ 3.7 años)`: separadores ingleses pegados a
  palabras españolas. Cambiar el idioma en la app cambia también cómo se escriben las cifras. Lo que se
  **guarda** —`history.log`, el CSV, los comandos— sigue en formato invariante y no depende del idioma.
- **La *Licencia* y los *Avisos de terceros* se leen enteros** (`T6-14`). Vienen preformateados a ~80
  columnas de anchura fija y el diálogo los ajustaba a unas 60: cada línea larga se partía en dos, y hasta
  las líneas de guiones separadoras salían cortadas. Ahora conservan su maquetación original.

### Corregido

- **La pantalla de *Novedades* enseñaba los asteriscos del Markdown** y partía los párrafos a mitad de
  frase (`T6-13`). Se quitaban las negritas pero no las cursivas de un solo asterisco, y se respetaban los
  saltos de línea del texto original —ajustado a 100 columnas— que el diálogo volvía a ajustar por su
  cuenta. Es la primera pantalla que se ve tras actualizar.
- **Los resúmenes de *Reinicializar unidad* se partían donde no toca** (`T6-15`): «…de la unidad I: (todas
  sus / particiones) / y se recreará…», y en un sitio distinto en cada idioma. Llevaban saltos de línea
  puestos a mano que peleaban con el ajuste del propio control. Es el texto que hay que leer antes de
  borrar un disco entero.

- **«Velocidad de rotación: SSD»** en *Salud del disco* (`T6-03`). Una velocidad cuyo valor era un tipo de
  medio, con «Tipo de medio: SSD» en la fila de encima. En un disco de estado sólido la fila ya no aparece
  —no es un dato que falte, es una pregunta que no aplica— y en un disco mecánico sigue mostrando sus RPM.
  Si no se sabe si gira, la fila se mantiene como «No disponible»: esconderla sería dar por hecho que es
  SSD.
- **El campo de confirmación mostraba la letra que hay que teclear** (`T6-02`). Iba como texto de
  marcador, así que el campo se leía como si ya estuviera relleno y regalaba la respuesta justo donde la
  app pone su única fricción deliberada. Además, WinUI usaba ese marcador como **nombre accesible** del
  campo: un lector de pantalla anunciaba la letra en voz alta. Ahora el campo aparece vacío y tiene su
  propio nombre («Letra de la unidad»), en los cinco idiomas.
- **El diálogo de confirmación de *Reinicializar unidad* se titulaba «Confirmar formato»** (`T6-01`). Las
  dos operaciones irreversibles comparten el mismo diálogo y compartían también el título, fijado dentro
  de él. El cuerpo sí explicaba que se borra el disco físico entero, pero quien leía solo el título estaba
  confirmando algo distinto —y menos grave— de lo que iba a ocurrir. Ahora cada operación pone el suyo.
- El barrido de contraste componía el alfa de un color sobre el fondo de la tarjeta aunque el color se
  pintara encima de otra cosa. Hoy ningún color de la app está en ese caso, pero daba una cifra que no
  correspondía a lo que se ve, que es justo lo que ese barrido existe para evitar.

---

## [1.22.0] — 2026-08-16

**El Tier 5 completo.** *Reinicializar unidad → FAT32 pequeña* dejaba el resto del disco sin asignar, y
recuperarlo obligaba a salir a *Crear y formatear particiones* de Windows — la herramienta que esta
aplicación existe para no tener que abrir. Ya no: el sobrante se puede aprovechar en la misma operación.
Y de camino, la propia opción aparece por fin en las unidades pequeñas, donde llevaba escondida desde
la 1.14.0.

### Añadido

- **El espacio sobrante ya no se queda muerto** (`T5-02`). Al crear una partición FAT32 pequeña, la
  tarjeta de opciones ofrece ahora qué hacer con el resto del disco: **dejarlo sin asignar** (lo de
  siempre, y sigue siendo el valor por defecto) o **crear una segunda partición** que lo ocupe entero, con
  su sistema de archivos (exFAT o NTFS) y su etiqueta. Todo en la misma operación y bajo la misma
  confirmación destructiva. Hasta ahora, la opción que resuelve el flasheo de una BIOS dejaba un pendrive
  de 256 GB del que solo se podían usar 32 hasta salir a una herramienta de Windows — que es justo lo que
  esta aplicación existe para no tener que abrir.
- La **partición FAT32 se crea siempre primera**, y la interfaz explica por qué: Windows 10 (1703) y
  posteriores muestran las dos, pero equipos más antiguos y muchos aparatos (televisores, radios de coche,
  BIOS de placas base) solo leen la primera — y es la que interesa que vean.
- FAT32 y FAT **no se ofrecen** para la segunda partición: el sobrante de un pendrive grande supera sus
  límites (32 GB y 2 GB), así que ofrecerlos sería ofrecer un fallo que llegaría con el disco ya borrado.
- **Si la operación se rompe a mitad, ahora dice qué quedó en el disco** (`T5-03`). Con dos particiones
  existe un estado intermedio real —la primera creada y formateada, la segunda no— y hasta ahora el mensaje
  de error no sabía distinguirlo de «no se hizo nada». El aviso cuenta cuántas particiones se crearon,
  cuáles quedaron utilizables, y deja claro que el disco **ya estaba borrado** cuando falló. Queda también
  en el historial.
- **No se revierte nada automáticamente**, y es deliberado: el disco ya está borrado, así que «deshacer»
  solo podría significar borrarlo otra vez — y esa no es una decisión que nadie haya pedido. Se informa y
  decides tú.

- **El plan de particiones, como dato puro** (`T5-01`, prerrequisito del resto del Tier 5). El layout que
  *Reinicializar unidad* crea estaba implícito en un `long?` («una partición de este tamaño, o todo el disco
  si es nulo»). Ahora es un `PartitionPlan` explícito —una secuencia de particiones, cada una con tamaño (o
  «el resto»), sistema de archivos y etiqueta— con una función pura que lo valida contra el tamaño real del
  disco **antes de tocar nada**: que la suma quepa con su margen de alineación, que ninguna sea de cero, que
  como mucho una sea «el resto» y vaya al final, que cada volumen FAT32 respete el límite de 32 GB de
  Windows, que las etiquetas valgan para su sistema de archivos, y que el número de particiones sea legal
  para el estilo (**MBR: 4 primarias**). Trece motivos de rechazo, cada uno un valor y no un texto, con el
  índice de la partición culpable.
- `ReinitDrive` **ejecuta N particiones** y revalida el plan justo antes de `Clear-Disk`. La interfaz sigue
  enviando **una sola**, así que la aplicación se comporta exactamente igual que antes; lo que falta para
  aprovechar el espacio sobrante (`T5-02`) es la interfaz, no el motor.
- Las letras se leen en plural (`LETTER:<índice>:<X>`): Windows las asigna en el orden que quiere, así que
  sin el índice la app no podría saber cuál es la partición que debe seleccionar al terminar.

### Corregido

- **La partición FAT32 pequeña se ocultaba en unidades de menos de 32 GB.** La opción se pensó como rodeo
  al límite de Windows (no crea volúmenes FAT32 mayores de 32 GB) y por eso solo aparecía en unidades
  extraíbles de ese tamaño o más. Pero lo que hace por debajo —crear una partición de N GB y dejar el resto
  del disco sin asignar— sirve igual en un pendrive de 16 GB, donde era imposible llegar a ella. Ahora
  aparece en **cualquier unidad extraíble** donde quepa al menos el menor de los tamaños.
- **El selector de tamaños no comprobaba si el tamaño cabía.** Ofrecía siempre 1/2/4/8/16/32 GB. En un
  pendrive de 16 GB nominales (~14,9 GiB reales) elegir 16 o 32 habría hecho fallar `New-Partition`
  **con el disco ya borrado**. Ahora se filtra por el tamaño real del disco físico, con un margen de 16 MiB
  para la alineación de la partición y los metadatos de la tabla.
- **El tope se medía sobre el volumen, no sobre el disco.** `DriveInfo.TotalSize` mide la partición actual,
  así que usar la función una vez (16 GB → partición de 2 GB) dejaba el tope clavado en 2 GB y la convertía
  en un trinquete que solo bajaba. Se consulta el tamaño del disco físico (`DiskService.GetDiskSizeAsync`,
  en paralelo con el S.M.A.R.T. al seleccionar unidad); mientras llega se usa el del volumen, que siempre es
  menor o igual — se ofrece de menos, nunca de más.
- **El estilo de partición (MBR/GPT) se elegía con el tamaño del volumen.** El límite de 2 TB es de MBR y se
  aplica al disco; ahora `ReinitPlan.StyleFor` recibe el dato del disco.
- **Segunda comprobación antes de borrar.** El tamaño elegido se vuelve a validar contra el disco real justo
  antes de `Clear-Disk`: el selector se pobló al seleccionar la unidad y el disco puede haber cambiado.

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
[1.22.0]: https://github.com/xfiberex/FormatDiskPro/releases/tag/v1.22.0
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
