# FormatDiskPro

![Release](https://img.shields.io/github/v/release/xfiberex/FormatDiskPro?label=versión&color=blue)
![.NET](https://img.shields.io/badge/.NET-10-512BD4)
![Plataforma](https://img.shields.io/badge/Windows-10%20%7C%2011-0078D6)
![Licencia](https://img.shields.io/github/license/xfiberex/FormatDiskPro?label=licencia&color=green)

Herramienta de formateo y **gestión de unidades** para Windows con soporte para **5 sistemas de archivos**, diagnóstico **S.M.A.R.T. avanzado**, verificación de capacidad real, comprobación de errores (chkdsk), detección de protección de escritura, actualizaciones automáticas y protección del disco de sistema.

Inspirada en el diálogo nativo de Windows "Formatear unidad", pero ampliada hasta convertirse en una utilidad seria de gestión y diagnóstico de memorias USB y discos, con una **interfaz moderna basada en tarjetas** (WinUI 3 / Fluent Design 2).

## Capturas

Cada pantalla, en los **dos temas**. El tema sigue al de Windows en tiempo real (o se fuerza a
claro/oscuro desde *Configuración*), y el color de acento es el que tengas puesto en el sistema — en
estas capturas, rojo.

### Ventana principal

| Claro | Oscuro |
|:---:|:---:|
| ![Ventana principal en tema claro](docs/screenshots/main-light.png) | ![Ventana principal en tema oscuro](docs/screenshots/main-dark.png) |

Tarjetas de **unidad**, **configuración de formato** y **opciones**, con la barra de capacidad
usado/libre y la salud coloreada. La descripción bajo el selector cambia con el sistema de archivos.

### Salud del disco (S.M.A.R.T.)

| Claro | Oscuro |
|:---:|:---:|
| ![Salud S.M.A.R.T. en tema claro](docs/screenshots/health-light.png) | ![Salud S.M.A.R.T. en tema oscuro](docs/screenshots/health-dark.png) |

**Colorea cada métrica por rango** (verde / ámbar / rojo) y añade su estado **en texto**, para no
depender solo del color. Los contadores que la unidad no expone salen como *No disponible* en vez de
como un cero engañoso — es lo habitual en USB.

### Comprobar errores (chkdsk) · Reinicializar unidad

| Comprobar errores | Reinicializar unidad |
|:---:|:---:|
| ![Diálogo de chkdsk en tema claro](docs/screenshots/checkdisk-light.png) | ![Diálogo de reinicializar en tema oscuro](docs/screenshots/reinit-dark.png) |
| ![Diálogo de chkdsk en tema oscuro](docs/screenshots/checkdisk-dark.png) | ![Diálogo de reinicializar en tema claro](docs/screenshots/reinit-light.png) |

*Solo comprobar* es de solo lectura; *Comprobar y reparar* usa `/f` y queda **bloqueado en el disco de
sistema**. *Reinicializar unidad* solo se ofrece en **extraíbles**, y avisa de que borra el disco físico
entero, no solo la partición seleccionada.

### Confirmación destructiva · Historial

| Confirmar formato | Historial de operaciones |
|:---:|:---:|
| ![Diálogo de confirmación en tema claro](docs/screenshots/confirm-light.png) | ![Historial en tema claro](docs/screenshots/history-light.png) |
| ![Diálogo de confirmación en tema oscuro](docs/screenshots/confirm-dark.png) | ![Historial en tema oscuro](docs/screenshots/history-dark.png) |

Antes de destruir nada hay que **escribir la letra de la unidad**: fricción deliberada, y el botón
*Iniciar* no se habilita hasta entonces. Todo lo que hace la app queda en el **historial**, con búsqueda,
filtros por categoría y resultado, y exportación a CSV.

> Las capturas de *Reinicializar* y *chkdsk* se tomaron sobre la **USB de pruebas** y el resto sobre un
> **SSD interno**: reinicializar solo existe en unidades extraíbles, y un USB no expone los contadores
> S.M.A.R.T. que hacen interesante esa pantalla.
>
> Se generan conduciendo la app real por UI Automation con
> [`tools/capture-screenshots.ps1`](tools/capture-screenshots.ps1), sobre el **publish self-contained**
> que se distribuye. No se editan a mano.

## Características

### Formateo
- **5 sistemas de archivos**: NTFS, exFAT, ReFS, FAT32 y FAT
- **Sugerencia automática** según tipo y tamaño de unidad
- **Descripción contextual** de cada sistema de archivos
- **Formato rápido o completo**, con **progreso real en %** (formato completo de NTFS/FAT/FAT32)
- **Compresión NTFS** opcional
- **Borrado seguro con progreso real**: sobrescribe el espacio libre con un patrón (sobrescritor propio) mostrando **% real, velocidad (MB/s) y tiempo restante (ETA)**; **pasadas configurables (1 / 3 / 7)**, 1 por defecto (NIST 800-88: basta en discos modernos)
- **Presets** de un clic (USB universal, consola/TV, datos Windows, comprimido, borrado seguro), más **presets personalizados**: guarda tu configuración actual con un nombre, y **edítalos** (renombrar / actualizar a la config actual), **reordénalos** o elimínalos desde *Presets → Gestionar presets…*

### Seguridad
- **Protección del disco de sistema**: la unidad de Windows se marca como `[Protegido]` con todos los controles de formato deshabilitados
- **Doble guardia** del disco de sistema (al listar las unidades y de nuevo al iniciar el formateo)
- **Confirmación reforzada**: hay que escribir la letra de la unidad para confirmar el formateo
- **Validación de etiqueta de volumen** antes de la operación destructiva
- **Revalidación de disponibilidad** de la unidad al iniciar (detecta USBs extraídos)
- **Detección de protección de escritura**: si la unidad está en *solo lectura*, lo detecta al pulsar Iniciar y ofrece quitar la protección antes de formatear (evita el fallo críptico); también disponible como herramienta manual
- **Reinicializar unidad**: para USB con particiones raras o RAW, limpia el disco y recrea una única partición primaria formateada y usable. **Solo unidades extraíbles**, con guardas reforzadas (bloqueo del disco de sistema, verificación de que el disco físico no es el de Windows y confirmación escribiendo la letra); en cualquier unidad extraíble permite opcionalmente crear solo una pequeña partición FAT32 y dejar el resto sin asignar (por ejemplo, para actualizar el BIOS/UEFI de una placa base, ya que Windows nunca permite un volumen FAT32 mayor de 32 GB). El selector ofrece 1/2/4/8/16/32 GB, **filtrado a los que caben de verdad en el disco físico**. El resto puede **dejarse sin asignar** (por defecto) o **aprovecharse en una segunda partición** con su propio sistema de archivos (exFAT o NTFS) y etiqueta, sin salir de la aplicación; la FAT32 se crea siempre primera, porque los equipos anteriores a Windows 10 1703 y muchos aparatos (televisores, radios de coche, BIOS) solo leen la primera partición de un medio extraíble. Si la operación falla a mitad, informa de qué particiones llegaron a crearse y cuáles quedaron utilizables, y **no revierte nada** (el disco ya está borrado: deshacer solo podría ser borrarlo otra vez)

### Diagnóstico
- **Panel de información**: tamaño, espacio libre, FS actual y tipo, con una **barra de ocupación** cuyo color indica lo llena que está la unidad (neutro con espacio de sobra, **ámbar** al llenarse ≥80 % y **rojo** casi llena ≥90 %), no el color de acento del sistema
- **Salud S.M.A.R.T. avanzada**: estado de salud, conexión (USB/SATA/NVMe) y tipo de medio (SSD/HDD) en el panel, más un **diálogo de detalle** con temperatura, horas de encendido, desgaste de SSD, RPM y errores de lectura/escritura (`Get-StorageReliabilityCounter`). Temperatura, desgaste y errores se **colorean por rango** (verde/ámbar/rojo) con texto de estado (Normal/Atención/Crítico) y un botón **Actualizar**
- **Verificación de capacidad real**: detecta memorias USB falsificadas escribiendo y releyendo un patrón
- **Comprobación de errores (chkdsk)**: *Solo comprobar* (solo lectura, universal) o *Comprobar y reparar* (`/f`), con progreso en vivo y resultado claro
- **Benchmark de lectura/escritura**: mide la velocidad real (MB/s) **secuencial** (cola Q8) y **4 KiB aleatorio** (con **IOPS** junto a los MB/s, estilo CrystalDiskMark) con un archivo temporal de ~512 MB **sin caché del sistema**, tomando la mediana de varias pasadas; **no destructivo** y disponible en cualquier unidad

### Experiencia
- **Interfaz moderna basada en tarjetas** (WinUI 3 / Fluent): secciones con encabezado e icono, barra de acción inferior y un **color de acento que sigue el de Windows** (sistema de diseño inspirado en Win11Debloat), adaptándose a tema claro u oscuro
- **Interfaz multilingüe** Español · Inglés · Português · Français · Italiano (conmutable en caliente); **detecta el idioma del sistema en el primer arranque** (luego manda tu elección)
- **Tema automático / claro / oscuro**: sigue el tema del sistema Windows en tiempo real; opción de forzar claro u oscuro desde el menú
- **Recuerda tus preferencias** (idioma, tema, última unidad, presets, aviso y pasadas de borrado seguro) entre sesiones (`%AppData%\FormatDiskPro\settings.json`)
- **Expulsión segura** de unidades removibles
- **Visor de historial integrado** dentro de la app (con **búsqueda y filtros** por categoría/resultado, y **exportación a CSV**), además del registro de auditoría en `%AppData%\FormatDiskPro\history.log`
- **Lista de unidades autorefrescada**: se actualiza sola al **conectar o desconectar** una unidad (además del botón Refrescar / F5)
- **Tiempo transcurrido, velocidad y ETA** en operaciones largas, con **cancelación segura** de cualquier operación
- **Aviso al terminar**: sonido + parpadeo de la barra de tareas al completar operaciones largas (solo si la ventana no está en primer plano), para poder alejarte del PC; se activa/desactiva en *Configuración → Avisar al terminar*
- **Actualizaciones integradas**: comprueba GitHub Releases al inicio y bajo demanda; el aviso *"Actualización disponible"* muestra el **changelog** de la nueva versión antes de descargar e instalar
- **Diálogo de novedades**: tras actualizar, muestra automáticamente (una sola vez) las novedades de la nueva versión —las mismas notas publicadas en GitHub Releases—; también disponible en cualquier momento desde *Ayuda → Novedades…*
- **Icono propio** de aplicación

> 📋 Consulta la **[hoja de ruta](ROADMAP.md)** para ver las características implementadas y las próximas (organizadas por *tiers*),
> y el **[changelog](CHANGELOG.md)** para lo que trajo cada versión.

## Requisitos

| Requisito | Versión mínima |
|-----------|----------------|
| Windows | 10 / 11 (x64) |
| .NET | 10.0 — *solo para compilar desde código; el instalador lo incluye* |
| Privilegios | Administrador (UAC requerido) |

## Instalación

Descarga el instalador más reciente desde la página de **[Releases](https://github.com/xfiberex/FormatDiskPro/releases)** (`FormatDiskPro-x.y.z-setup.exe`) y ejecútalo. El instalador es *self-contained*: **no requiere instalar .NET** por separado.

### Actualizaciones

La aplicación comprueba si hay una versión más reciente en GitHub Releases al iniciarse y mediante **Ayuda → Buscar actualizaciones…**. Si hay una nueva versión, ofrece descargar e instalar el nuevo instalador automáticamente (actualización silenciosa con relanzado desde la 1.2.2 en adelante).

> **Modelo de confianza (desde la v1.15.0).** El instalador se ejecuta **con permisos de administrador**, así que antes de lanzarlo la app comprueba que es el que publicó el proyecto:
>
> 1. Si lleva una **firma Authenticode** válida y de confianza para Windows, se acepta (es la garantía más fuerte, porque la avala una CA).
> 2. Si no —hoy los instaladores se publican **sin firmar**, ver más abajo—, se calcula su **SHA-256** y se compara con el que se publica como asset del release (`FormatDiskPro-x.y.z-setup.exe.sha256`).
>
> Si no supera ninguna de las dos, **el instalador se borra y no se ejecuta nada**.
>
> **Alcance honesto:** el instalador y su hash salen del mismo release, así que esto detecta un archivo **corrupto o manipulado en tránsito**, pero no protegería frente a un compromiso de la cuenta de GitHub (quien pudiera sustituir el `.exe` podría sustituir también el hash). Es el compromiso habitual de un proyecto sin certificado, y es exactamente la garantía que sustituye a la firma. La firma Authenticode —que además eliminaría el aviso de SmartScreen— sigue disponible como **opción** del flujo de publicación (ver [Construcción](#construcción)), pero no se aplica a los binarios publicados por decisión del proyecto.

## Construcción

```bash
dotnet build -c Release
```

El ejecutable queda en `src\FormatDiskPro\bin\Release\net10.0-windows10.0.19041.0\win-x64\FormatDiskPro.exe`.

> Para **ejecutarlo** directamente puede hacer falta el *publish self-contained* (`build-installer.ps1`
> o `dotnet publish … --self-contained true`): `dotnet build` produce un apphost *framework-dependent*
> que, en máquinas sin el runtime .NET 10 de escritorio, avisa «You must install or update .NET». El
> instalador distribuido no tiene este problema (es self-contained).

### Generar el instalador

Requiere [Inno Setup 6](https://jrsoftware.org/isinfo.php) (`winget install JRSoftware.InnoSetup`):

```powershell
src\FormatDiskPro\installer\build-installer.ps1
```

Publica la app *self-contained* (win-x64) y compila el instalador en `src\FormatDiskPro\installer\Output\`, junto con su **`.sha256`** (el hash con el que la app verifica la descarga al auto-actualizarse). El instalador limpia la instalación previa antes de copiar y, en una actualización in-place, cierra y relanza la app automáticamente.

> La publicación intermedia va a `%TEMP%\FormatDiskPro-publish`, no dentro del repo: Inno Setup no maneja rutas de más de 260 caracteres, y los nombres de archivo del Windows App SDK *self-contained* se pasan del límite en cuanto el repositorio no cuelga de una carpeta corta.

**Firma de código (opcional, recomendada):** sin firma, SmartScreen muestra "editor desconocido". Si tienes un certificado, fírmalo pasando la huella o un `.pfx`:

```powershell
# Certificado del almacén de Windows (por huella SHA-1):
src\FormatDiskPro\installer\build-installer.ps1 -CertThumbprint A1B2C3...
# O un archivo .pfx:
src\FormatDiskPro\installer\build-installer.ps1 -CertFile cert.pfx -CertPassword ****
```

Firma el ejecutable publicado y el instalador (sellado de tiempo RFC3161). Requiere `signtool.exe` (Windows SDK).

¿Sin certificado? El script `installer\new-selfsigned-cert.ps1` genera uno **autofirmado** de prueba y muestra su huella:

```powershell
src\FormatDiskPro\installer\new-selfsigned-cert.ps1          # crea el cert y muestra el thumbprint
src\FormatDiskPro\installer\new-selfsigned-cert.ps1 -Trust   # (como admin) además lo hace de confianza en este equipo
```

> ⚠️ Un certificado autofirmado **no** elimina los avisos de SmartScreen para usuarios finales (su cadena no es de confianza). Sirve para validar el pipeline o para entornos controlados. Para distribución pública usa un certificado **OV/EV** de una CA reconocida.

### Publicar una versión

El script `release.ps1` (raíz del repo) corta una versión completa en un paso: valida, ejecuta las pruebas, actualiza `<Version>`, compila el instalador, hace commit + tag, lo sube y crea el **GitHub Release** con el instalador y su **`.sha256`** adjuntos.

> ⚠️ El asset `.sha256` es **obligatorio** mientras se publique sin firmar: es con lo que la app verifica la descarga antes de ejecutarla como administrador. `release.ps1` aborta si no lo encuentra.
>
> 📝 El corte también **aborta si [`CHANGELOG.md`](CHANGELOG.md) no tiene la sección de la versión** que se va a publicar. Mueve antes lo que haya bajo *Sin publicar* a su propia sección: un changelog que se queda atrás afirma ser el registro del proyecto y miente.

```powershell
.\release.ps1 -Version 1.7.0           # release completo
.\release.ps1 -Version 1.7.0 -DryRun   # muestra el plan sin modificar nada
.\release.ps1 -Version 1.7.0 -CertThumbprint A1B2C3...   # firmando el instalador
```

Flags: `-DryRun`, `-SkipTests`, `-AllowDirty`, `-NotesFile <archivo.md>`, y los de firma (`-CertThumbprint` / `-CertFile` / `-CertPassword` / `-TimestampUrl`, reenviados a `build-installer.ps1`). Los usuarios con una versión anterior recibirán el aviso de actualización automáticamente.

### Regenerar las capturas del README

Las capturas de arriba **no se hacen a mano**: las genera un script que conduce la app real por UI
Automation (fija tema, idioma y unidad, abre el diálogo S.M.A.R.T. y fotografía la ventana).

```powershell
.\tools\capture-screenshots.ps1                          # claro + oscuro + S.M.A.R.T.
.\tools\capture-screenshots.ps1 -Theme dark -Language en -Drive H
.\tools\capture-screenshots.ps1 -Gallery                 # modo galería: cada diálogo/estado en ambos temas
.\tools\capture-screenshots.ps1 -Gallery -Only checkdisk,confirm   # solo unas tomas concretas
```

El **modo galería** (`-Gallery`) fotografía cada diálogo y estado clave (Confirmar, Historial, Presets,
chkdsk, Reinicializar, S.M.A.R.T., Acerca de…) en claro **y** oscuro para revisar la UX/UI de un vistazo;
guarda en `docs/screenshots/gallery/` (ignorada por git) sin tocar las capturas del README. **De ahí salen
las 12 del README**: se revisan en la galería y se copian a `docs/screenshots/` las que valen.

> **Dos avisos que cuestan una tanda de capturas si no los sabes.** *Reinicializar unidad* exige pasar
> `-Drive <USB>`: sobre un disco fijo la toma sale con el mensaje de «solo unidades extraíbles», que es
> la guarda y no la característica. Y conviene fotografiar el **publish self-contained**
> (`-Exe <publish>\FormatDiskPro.exe`), no el `dotnet build` — ver *Decisiones* en `CONTEXT.md`.

Requiere **terminal elevada** (la app es `requireAdministrator`) y una sesión de escritorio sin nada
encima de la ventana. Respalda y restaura tu `settings.json` real, así que no altera tu configuración.

> Fotografía el **publish self-contained** (lo que se distribuye), no el `dotnet build`: en algunas máquinas
> el apphost *framework-dependent* de un build plano no arranca. Publica con `build-installer.ps1` (o
> `dotnet publish … --self-contained true`) y pásalo con `-Exe <publish>\FormatDiskPro.exe`.

### Pruebas

```bash
dotnet test                                   # unitarias (xUnit)
```

Los **UI tests** (FlaUI/UIA3) conducen la app real y **no están en la solución**: se lanzan aparte, desde
una **terminal elevada**.

```powershell
dotnet test tests\FormatDiskPro.UiTests --filter "Category!=Slow"
```

Los que necesitan la USB física de pruebas **se omiten solos** si no está conectada (omitido ≠ fallido), y
los que van más allá piden su propia autorización explícita: `FORMATDISKPRO_ALLOW_DESTRUCTIVE=1` para el que
borra datos de verdad, y `FORMATDISKPRO_ALLOW_YANK=1` para los que **desmontan la unidad a la fuerza a mitad
de una operación** — no borran nada, pero comprueban que la app sobrevive a perder el disco de las manos en
lugar de cerrarse sin avisar. Para incluirlos en un corte de versión:
`.\release.ps1 -Version X.Y.Z -UiTests`.

Las pruebas unitarias (xUnit) cubren la lógica pura aislada en `Core` y los helpers testeables de `Services`: construcción de comandos de formato, blindaje anti-inyección, parseo de progreso, longitud de etiqueta, consistencia de presets, comparación de versiones, persistencia de configuración, cálculo de velocidad/ETA, patrón y número de pasadas del borrado seguro, parseo del historial (más filtro y exportación CSV, con neutralización de fórmulas) y del detalle S.M.A.R.T. (más umbrales de severidad), **verificación del instalador descargado** (SHA-256 contra un servidor HTTP local, rechazo del hash que no coincide y del release sin hash), **contraste WCAG de todos los colores semánticos** en ambos temas (el barrido recorre el inventario `SeverityPalette.All()`, compone el alfa sobre el fondo y exige 4.5:1 al texto y 3:1 a los objetos gráficos), **comparación de letras de unidad invariante de cultura** (la guarda que impide formatear el disco de sistema, verificada bajo cultura turca), **saneado del nombre de asset** antes de componer la ruta de descarga del instalador, interpretación del código de salida de chkdsk, elección de estilo de partición (MBR/GPT) y parseo de la reinicialización, planificación/velocidad/IOPS del benchmark, conversión de las notas de versión (Markdown → texto plano), validación y renombrado de nombres de presets personalizados, clasificación de eventos de cambio de dispositivo, **los caminos de error de las operaciones largas** (detección de unidad falsificada reproducida sin unidad falsificada: bloque corrompido, lectura corta, cancelación con limpieza; y que lo que registra un fallo se vuelva a leer como **una** entrada de historial aunque la excepción traiga una traza de pila multilínea), completitud de las traducciones (5 idiomas, incluidas las descripciones de sistema de archivos y los nombres de los presets integrados) **y un barrido del propio código fuente que falla si aparece una tabla de cadenas fuera de `Localization/`** — la forma que tomó el último texto que se quedó sin traducir, y mapeo de códigos de idioma y de cultura del sistema, y la decisión de aviso al terminar.

## Uso

1. Ejecutar como **Administrador** (el manifiesto UAC lo solicita automáticamente)
2. Seleccionar la unidad a formatear en el desplegable (cualquiera **salvo la del sistema**, que aparece protegida)
3. Elegir sistema de archivos, tamaño de cluster y etiqueta (o aplicar un **Preset** desde el menú *Configuración*)
4. Pulsar **Iniciar**, escribir la letra de la unidad para confirmar y aceptar

> **Nota de seguridad**: el disco del sistema (donde reside Windows) aparece marcado como `[Protegido]` y todos sus controles de formato quedan deshabilitados. El resto de unidades — removibles, discos de datos fijos y discos RAM — pueden formatearse. Antes de iniciar se exige confirmar escribiendo la letra de la unidad.

### Menú

| Menú | Opciones |
|------|----------|
| **Herramientas** | Verificar capacidad real · Salud del disco (S.M.A.R.T.) · Comprobar errores (chkdsk) · Benchmark rápido · Quitar protección de escritura · Reinicializar unidad · Expulsar unidad · Ver historial |
| **Configuración** | Idioma (ES/EN/PT/FR/IT) · Tema (Automático/Claro/Oscuro) · Presets (con Gestionar presets…) · Avisar al terminar |
| **Ayuda** | Buscar actualizaciones · Novedades · Licencia · Avisos de terceros · Acerca de (con disclaimer, privacidad y *Apoyar el proyecto*) |

## Sistemas de archivos disponibles

| FS | Recomendado para | Límite de archivo |
|----|-----------------|-------------------|
| NTFS | Discos internos Windows | Sin límite práctico |
| exFAT | USB > 32 GB | Sin límite práctico |
| ReFS | Almacenamiento crítico | Sin límite práctico |
| FAT32 | USB ≤ 32 GB, consolas | 4 GB |
| FAT | Unidades < 2 GB | 2 GB |

> En cualquier unidad extraíble, *Reinicializar unidad* permite crear una pequeña partición FAT32
> (1/2/4/8/16/32 GB, elegible) y, con el resto del disco, **dejarlo sin asignar o crear una segunda
> partición** en exFAT/NTFS — ver arriba.

## Arquitectura

Separación por capas (lógica pura aislada de los efectos colaterales y de la UI):

```
src/FormatDiskPro/
├─ Core/            Lógica pura y testeable
│  ├─ FormatLogic.cs        Construcción de comandos, parseo de progreso, formato de bytes
│  ├─ Throughput.cs         Velocidad y tiempo restante (ETA) de operaciones largas
│  ├─ SmartInfo.cs          Modelo + parseo del detalle S.M.A.R.T. + umbrales de severidad
│  ├─ SeverityPalette.cs    Inventario de colores semánticos por tema (contraste WCAG medido por tests)
│  ├─ DriveLetter.cs        Comparación de letras de unidad invariante de cultura
│  ├─ HistoryEntry.cs       Parseo del historial + filtro y exportación a CSV (anti CSV injection)
│  ├─ ReinitPlan.cs         Estilo MBR/GPT por tamaño + parseo de la nueva letra
│  ├─ Benchmark.cs          Tamaño de prueba, velocidad e IOPS
│  ├─ ReleaseNotes.cs       Notas de versión (Markdown) → texto plano
│  ├─ DeviceChange.cs       Interpretación de WM_DEVICECHANGE (autorefresco de unidades)
│  ├─ LegalText.cs          Lectura de la licencia GPLv3 y avisos de terceros embebidos
│  ├─ UpdateChecker.cs      Comparación de versiones para actualizaciones
│  ├─ AppInfo.cs            Versión, coordenadas del repositorio y enlace de donación
│  ├─ Presets.cs            Configuraciones predefinidas (nombre traducido) + validación/renombrado
│  └─ OperationFailure.cs   Línea de historial de una operación fallida
├─ Services/        Efectos colaterales (procesos / disco / red)
│  ├─ DiskService.cs        S.M.A.R.T., nº de disco, protección de escritura y expulsión (PowerShell)
│  ├─ SecureWipe.cs         Borrado seguro del espacio libre (sobrescritor propio, con progreso)
│  ├─ CheckDisk.cs          Comprobación / reparación del sistema de archivos (chkdsk)
│  ├─ ReinitDrive.cs        Reinicializar disco extraíble (clean + partición + formato)
│  ├─ BenchmarkRunner.cs    Benchmark de lectura/escritura (no destructivo)
│  ├─ CapacityVerifier.cs   Verificación de capacidad real
│  ├─ AppSettings.cs        Preferencias persistentes (settings.json: idioma/tema/unidad/presets/aviso)
│  ├─ Notifier.cs           Aviso al terminar (sonido + parpadeo de barra de tareas, Win32)
│  ├─ UpdateService.cs      GitHub Releases: consulta, descarga, VERIFICACIÓN (firma/SHA-256) e instalación
│  └─ History.cs            Registro de auditoría
├─ UI/              WinUI 3 (Windows App SDK)
│  ├─ MainWindow.xaml / .cs        Ventana principal y orquestación
│  ├─ ConfirmDialog.xaml / .cs     ContentDialog — confirmación reforzada
│  ├─ HealthDialog.xaml / .cs      Diálogo de detalle S.M.A.R.T.
│  ├─ HistoryDialog.xaml / .cs     Visor de historial integrado
│  ├─ WhatsNewDialog.xaml / .cs    Novedades de la versión (tras actualizar / manual)
│  ├─ PresetsDialog.xaml / .cs     Gestionar presets propios (guardar / editar / reordenar / eliminar)
│  ├─ AboutDialog.xaml / .cs       Acerca de: descripción, disclaimer, privacidad, donación
│  ├─ LegalTextDialog.xaml / .cs   Visor de licencia GPLv3 / avisos de terceros
│  ├─ Theme/AppTheme.xaml          Tokens de diseño (tarjetas, encabezados, footer)
│  └─ DriveViewModel.cs            Modelo de binding para el ComboBox de unidades
├─ Localization/    Cadenas ES/EN/PT/FR/IT centralizadas (arreglo por idioma)
├─ installer/       Inno Setup (installer.iss + build-installer.ps1 → Output/)
└─ Program.cs       Punto de entrada

tests/FormatDiskPro.Tests/     Pruebas xUnit sobre la lógica de Core y los helpers de Services
tests/FormatDiskPro.UiTests/  Pruebas de UI con FlaUI/UIA3 sobre la app real (fuera de la solución)
tools/capture-screenshots.ps1 Regenera las capturas del README conduciendo la app por UI Automation
docs/screenshots/             Capturas del README (generadas, no editadas a mano)
ROADMAP.md                    Hoja de ruta de características (tiers)
CHANGELOG.md                  Qué cambió en cada versión (Keep a Changelog)
release.ps1                   Corte de versión en un paso (build + tag + GitHub Release)
```

## Stack

- C# 13 / .NET 10
- **WinUI 3** (Windows App SDK 1.8, unpackaged) — Mica, Fluent Design 2, `ExtendsContentIntoTitleBar`, sistema de tarjetas inspirado en Win11Debloat
- `Format-Volume` / `format.com` (formateo) · sobrescritor propio (borrado seguro y benchmark) · `chkdsk` (comprobación/reparación) · `Clear-Disk` / `Initialize-Disk` / `New-Partition` (reinicializar) · `Get-PhysicalDisk` / `Get-StorageReliabilityCounter` (S.M.A.R.T.) · `Set-Disk` (protección de escritura)
- Comandos PowerShell vía `-EncodedCommand` (Base64 UTF-16LE) para evitar inyección
- UAC: `requireAdministrator` en `app.manifest`

## Licencia

Software libre distribuido bajo la **[GNU General Public License v3.0](LICENSE)** (GPLv3): puedes usarlo,
estudiarlo, modificarlo y redistribuirlo, **siempre que los derivados conserven la misma licencia y su código
fuente abierto**. Se ofrece **SIN NINGUNA GARANTÍA** (ver el aviso del programa). Las atribuciones de
componentes de terceros están en [THIRD-PARTY-NOTICES.txt](THIRD-PARTY-NOTICES.txt). La licencia y los avisos
también se pueden consultar dentro de la app en *Ayuda → Licencia* y *Ayuda → Avisos de terceros*.

> ⚠️ **Aviso de uso:** FormatDiskPro formatea y borra unidades de forma **irreversible**. Comprueba siempre la
> unidad seleccionada antes de iniciar; el autor no se hace responsable de pérdidas de datos.

## Contribuir y reportar

- **Errores y sugerencias:** por los [issues](https://github.com/xfiberex/FormatDiskPro/issues), con las
  plantillas que piden lo que hace falta para reproducirlos (versión, unidad, pasos).
- **Vulnerabilidades:** **no** las publiques como issue — sigue [SECURITY.md](.github/SECURITY.md), que
  usa el reporte privado de GitHub.
- **Código:** [CONTRIBUTING.md](.github/CONTRIBUTING.md) explica cómo compilar, cómo correr las pruebas
  (las de UI exigen **terminal elevada**) y qué se espera de un PR.

## Apoyar el proyecto

FormatDiskPro es gratuito y de código abierto. Si te resulta útil, puedes **apoyar su desarrollo con una
donación voluntaria** (PayPal) desde *Ayuda → Apoyar el proyecto* dentro de la app. Las donaciones son
totalmente opcionales: **ninguna función está limitada ni de pago**.

## Privacidad

La aplicación **no recopila datos personales ni telemetría**. La única conexión a Internet es para comprobar y
descargar actualizaciones desde GitHub Releases (HTTPS).
