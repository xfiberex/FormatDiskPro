# FormatDiskPro

![Release](https://img.shields.io/github/v/release/xfiberex/FormatDiskPro?label=versión&color=blue)
![.NET](https://img.shields.io/badge/.NET-10-512BD4)
![Plataforma](https://img.shields.io/badge/Windows-10%20%7C%2011-0078D6)
![Licencia](https://img.shields.io/github/license/xfiberex/FormatDiskPro?label=licencia&color=green)

Herramienta de formateo y **gestión de unidades** para Windows con soporte para **5 sistemas de archivos**, diagnóstico **S.M.A.R.T. avanzado**, verificación de capacidad real, comprobación de errores (chkdsk), detección de protección de escritura, actualizaciones automáticas y protección del disco de sistema.

Inspirada en el diálogo nativo de Windows "Formatear unidad", pero ampliada hasta convertirse en una utilidad seria de gestión y diagnóstico de memorias USB y discos, con una **interfaz moderna basada en tarjetas** (WinUI 3 / Fluent Design 2).

## Características

### Formateo
- **5 sistemas de archivos**: NTFS, exFAT, ReFS, FAT32 y FAT
- **Sugerencia automática** según tipo y tamaño de unidad
- **Descripción contextual** de cada sistema de archivos
- **Formato rápido o completo**, con **progreso real en %** (formato completo de NTFS/FAT/FAT32)
- **Compresión NTFS** opcional
- **Borrado seguro con progreso real**: sobrescribe el espacio libre con un patrón (sobrescritor propio) mostrando **% real, velocidad (MB/s) y tiempo restante (ETA)**; 1 pasada por defecto
- **Presets** de un clic (USB universal, consola/TV, datos Windows, comprimido, borrado seguro), más **presets personalizados**: guarda tu configuración actual con un nombre y elimínalos desde *Presets → Gestionar presets…*

### Seguridad
- **Protección del disco de sistema**: la unidad de Windows se marca como `[Protegido]` con todos los controles de formato deshabilitados
- **Doble guardia** del disco de sistema (al listar las unidades y de nuevo al iniciar el formateo)
- **Confirmación reforzada**: hay que escribir la letra de la unidad para confirmar el formateo
- **Validación de etiqueta de volumen** antes de la operación destructiva
- **Revalidación de disponibilidad** de la unidad al iniciar (detecta USBs extraídos)
- **Detección de protección de escritura**: si la unidad está en *solo lectura*, lo detecta al pulsar Iniciar y ofrece quitar la protección antes de formatear (evita el fallo críptico); también disponible como herramienta manual
- **Reinicializar unidad**: para USB con particiones raras o RAW, limpia el disco y recrea una única partición primaria formateada y usable. **Solo unidades extraíbles**, con guardas reforzadas (bloqueo del disco de sistema, verificación de que el disco físico no es el de Windows y confirmación escribiendo la letra)

### Diagnóstico
- **Panel de información**: tamaño, espacio libre, FS actual y tipo
- **Salud S.M.A.R.T. avanzada**: estado de salud, conexión (USB/SATA/NVMe) y tipo de medio (SSD/HDD) en el panel, más un **diálogo de detalle** con temperatura, horas de encendido, desgaste de SSD, RPM y errores de lectura/escritura (`Get-StorageReliabilityCounter`)
- **Verificación de capacidad real**: detecta memorias USB falsificadas escribiendo y releyendo un patrón
- **Comprobación de errores (chkdsk)**: *Solo comprobar* (solo lectura, universal) o *Comprobar y reparar* (`/f`), con progreso en vivo y resultado claro
- **Benchmark de lectura/escritura**: mide la velocidad real (MB/s) **secuencial** (cola Q8) y **4 KiB aleatorio** con un archivo temporal de ~512 MB **sin caché del sistema**, tomando la mediana de varias pasadas; **no destructivo** y disponible en cualquier unidad

### Experiencia
- **Interfaz moderna basada en tarjetas** (WinUI 3 / Fluent): secciones con encabezado e icono, barra de acción inferior y un **color de acento que sigue el de Windows** (sistema de diseño inspirado en Win11Debloat), adaptándose a tema claro u oscuro
- **Interfaz multilingüe** Español · Inglés · Português · Français · Italiano (conmutable en caliente)
- **Tema automático / claro / oscuro**: sigue el tema del sistema Windows en tiempo real; opción de forzar claro u oscuro desde el menú
- **Recuerda tus preferencias** (idioma, tema, última unidad, presets y aviso) entre sesiones (`%AppData%\FormatDiskPro\settings.json`)
- **Expulsión segura** de unidades removibles
- **Visor de historial integrado** dentro de la app, además del registro de auditoría en `%AppData%\FormatDiskPro\history.log`
- **Tiempo transcurrido, velocidad y ETA** en operaciones largas, con **cancelación segura** de cualquier operación
- **Aviso al terminar**: sonido + parpadeo de la barra de tareas al completar operaciones largas (solo si la ventana no está en primer plano), para poder alejarte del PC; se activa/desactiva en *Configuración → Avisar al terminar*
- **Actualizaciones integradas**: comprueba GitHub Releases al inicio y bajo demanda; descarga e instala la nueva versión
- **Diálogo de novedades**: tras actualizar, muestra automáticamente (una sola vez) las novedades de la nueva versión —las mismas notas publicadas en GitHub Releases—; también disponible en cualquier momento desde *Ayuda → Novedades…*
- **Icono propio** de aplicación

> 📋 Consulta la **[hoja de ruta](ROADMAP.md)** para ver las características implementadas y las próximas (organizadas por *tiers*).

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

> **Modelo de confianza:** la descarga se realiza por **HTTPS** desde GitHub Releases (lo que protege frente a manipulación en tránsito), pero el instalador **no se verifica con firma ni hash** antes de ejecutarse con elevación. En la práctica esto implica confiar en la integridad de la cuenta y los releases del proyecto en GitHub. La firma Authenticode (que también eliminaría los avisos de SmartScreen) está disponible de forma **opcional** en el flujo de publicación —ver [Construcción](#construcción)—, pero no se aplica a los binarios publicados por decisión del proyecto.

## Construcción

```bash
dotnet build -c Release
```

El ejecutable queda en `src\FormatDiskPro\bin\Release\net10.0-windows10.0.19041.0\win-x64\FormatDiskPro.exe`.

### Generar el instalador

Requiere [Inno Setup 6](https://jrsoftware.org/isinfo.php) (`winget install JRSoftware.InnoSetup`):

```powershell
src\FormatDiskPro\installer\build-installer.ps1
```

Publica la app *self-contained* (win-x64) y compila el instalador en `src\FormatDiskPro\installer\Output\`. El instalador limpia la instalación previa antes de copiar y, en una actualización in-place, cierra y relanza la app automáticamente.

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

El script `release.ps1` (raíz del repo) corta una versión completa en un paso: valida, ejecuta las pruebas, actualiza `<Version>`, compila el instalador, hace commit + tag, lo sube y crea el **GitHub Release** con el instalador adjunto.

```powershell
.\release.ps1 -Version 1.7.0           # release completo
.\release.ps1 -Version 1.7.0 -DryRun   # muestra el plan sin modificar nada
.\release.ps1 -Version 1.7.0 -CertThumbprint A1B2C3...   # firmando el instalador
```

Flags: `-DryRun`, `-SkipTests`, `-AllowDirty`, `-NotesFile <archivo.md>`, y los de firma (`-CertThumbprint` / `-CertFile` / `-CertPassword` / `-TimestampUrl`, reenviados a `build-installer.ps1`). Los usuarios con una versión anterior recibirán el aviso de actualización automáticamente.

### Pruebas

```bash
dotnet test
```

Las pruebas unitarias (xUnit) cubren la lógica pura aislada en `Core` y los helpers testeables de `Services`: construcción de comandos de formato, blindaje anti-inyección, parseo de progreso, longitud de etiqueta, consistencia de presets, comparación de versiones, persistencia de configuración, cálculo de velocidad/ETA, patrón del borrado seguro, parseo del historial y del detalle S.M.A.R.T., interpretación del código de salida de chkdsk, elección de estilo de partición (MBR/GPT) y parseo de la reinicialización, planificación/velocidad del benchmark, conversión de las notas de versión (Markdown → texto plano), validación de nombres de presets personalizados, completitud de las traducciones (5 idiomas) y mapeo de códigos de idioma, y la decisión de aviso al terminar.

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
| **Ayuda** | Buscar actualizaciones · Novedades · Acerca de |

## Sistemas de archivos disponibles

| FS | Recomendado para | Límite de archivo |
|----|-----------------|-------------------|
| NTFS | Discos internos Windows | Sin límite práctico |
| exFAT | USB > 32 GB | Sin límite práctico |
| ReFS | Almacenamiento crítico | Sin límite práctico |
| FAT32 | USB ≤ 32 GB, consolas | 4 GB |
| FAT | Unidades < 2 GB | 2 GB |

## Arquitectura

Separación por capas (lógica pura aislada de los efectos colaterales y de la UI):

```
src/FormatDiskPro/
├─ Core/            Lógica pura y testeable
│  ├─ FormatLogic.cs        Construcción de comandos, parseo de progreso, formato de bytes
│  ├─ Throughput.cs         Velocidad y tiempo restante (ETA) de operaciones largas
│  ├─ SmartInfo.cs          Modelo + parseo del detalle S.M.A.R.T.
│  ├─ HistoryEntry.cs       Parseo del historial de operaciones
│  ├─ ReinitPlan.cs         Estilo MBR/GPT por tamaño + parseo de la nueva letra
│  ├─ Benchmark.cs          Tamaño de prueba y cálculo de velocidad
│  ├─ ReleaseNotes.cs       Notas de versión (Markdown) → texto plano
│  ├─ UpdateChecker.cs      Comparación de versiones para actualizaciones
│  ├─ AppInfo.cs            Versión en ejecución y coordenadas del repositorio
│  └─ Presets.cs            Configuraciones predefinidas
├─ Services/        Efectos colaterales (procesos / disco / red)
│  ├─ DiskService.cs        S.M.A.R.T., nº de disco, protección de escritura y expulsión (PowerShell)
│  ├─ SecureWipe.cs         Borrado seguro del espacio libre (sobrescritor propio, con progreso)
│  ├─ CheckDisk.cs          Comprobación / reparación del sistema de archivos (chkdsk)
│  ├─ ReinitDrive.cs        Reinicializar disco extraíble (clean + partición + formato)
│  ├─ BenchmarkRunner.cs    Benchmark de lectura/escritura (no destructivo)
│  ├─ CapacityVerifier.cs   Verificación de capacidad real
│  ├─ AppSettings.cs        Preferencias persistentes (settings.json: idioma/tema/unidad/presets/aviso)
│  ├─ Notifier.cs           Aviso al terminar (sonido + parpadeo de barra de tareas, Win32)
│  ├─ UpdateService.cs      GitHub Releases: consulta, descarga e instalación
│  └─ History.cs            Registro de auditoría
├─ UI/              WinUI 3 (Windows App SDK)
│  ├─ MainWindow.xaml / .cs        Ventana principal y orquestación
│  ├─ ConfirmDialog.xaml / .cs     ContentDialog — confirmación reforzada
│  ├─ HealthDialog.xaml / .cs      Diálogo de detalle S.M.A.R.T.
│  ├─ HistoryDialog.xaml / .cs     Visor de historial integrado
│  ├─ WhatsNewDialog.xaml / .cs    Novedades de la versión (tras actualizar / manual)
│  ├─ PresetsDialog.xaml / .cs     Gestionar presets propios (guardar / eliminar)
│  ├─ Theme/AppTheme.xaml          Tokens de diseño (tarjetas, encabezados, footer)
│  └─ DriveViewModel.cs            Modelo de binding para el ComboBox de unidades
├─ Localization/    Cadenas ES/EN/PT/FR/IT centralizadas (arreglo por idioma)
├─ installer/       Inno Setup (installer.iss + build-installer.ps1 → Output/)
└─ Program.cs       Punto de entrada

tests/FormatDiskPro.Tests/   Pruebas xUnit sobre la lógica de Core y los helpers de Services
ROADMAP.md                   Hoja de ruta de características (tiers)
release.ps1                  Corte de versión en un paso (build + tag + GitHub Release)
```

## Stack

- C# 13 / .NET 10
- **WinUI 3** (Windows App SDK 1.8, unpackaged) — Mica, Fluent Design 2, `ExtendsContentIntoTitleBar`, sistema de tarjetas inspirado en Win11Debloat
- `Format-Volume` / `format.com` (formateo) · sobrescritor propio (borrado seguro y benchmark) · `chkdsk` (comprobación/reparación) · `Clear-Disk` / `Initialize-Disk` / `New-Partition` (reinicializar) · `Get-PhysicalDisk` / `Get-StorageReliabilityCounter` (S.M.A.R.T.) · `Set-Disk` (protección de escritura)
- Comandos PowerShell vía `-EncodedCommand` (Base64 UTF-16LE) para evitar inyección
- UAC: `requireAdministrator` en `app.manifest`

## Licencia

Distribuido bajo licencia **MIT**. Consulta el archivo [LICENSE](LICENSE) para más detalles.
