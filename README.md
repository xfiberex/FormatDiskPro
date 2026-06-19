# FormatDiskPro

![Release](https://img.shields.io/github/v/release/xfiberex/FormatDiskPro?label=versión&color=blue)
![.NET](https://img.shields.io/badge/.NET-10-512BD4)
![Plataforma](https://img.shields.io/badge/Windows-10%20%7C%2011-0078D6)
![Licencia](https://img.shields.io/github/license/xfiberex/FormatDiskPro?label=licencia&color=green)

Herramienta de formateo y **gestión de unidades** para Windows con soporte para **5 sistemas de archivos**, diagnóstico S.M.A.R.T., verificación de capacidad real, actualizaciones automáticas y protección del disco de sistema.

Inspirada en el diálogo nativo de Windows "Formatear unidad", pero ampliada hasta convertirse en una utilidad seria de gestión de memorias USB.

## Características

### Formateo
- **5 sistemas de archivos**: NTFS, exFAT, ReFS, FAT32 y FAT
- **Sugerencia automática** según tipo y tamaño de unidad
- **Descripción contextual** de cada sistema de archivos
- **Formato rápido o completo**, con **progreso real en %** (formato completo de NTFS/FAT/FAT32)
- **Compresión NTFS** opcional
- **Borrado seguro**: sobrescribe el espacio libre tras formatear (`cipher /w`)
- **Presets** de un clic (USB universal, consola/TV, datos Windows, comprimido, borrado seguro)

### Seguridad
- **Protección del disco de sistema**: la unidad de Windows se marca como `[Protegido]` con todos los controles de formato deshabilitados
- **Doble guardia** del disco de sistema (al listar las unidades y de nuevo al iniciar el formateo)
- **Confirmación reforzada**: hay que escribir la letra de la unidad para confirmar el formateo
- **Validación de etiqueta de volumen** antes de la operación destructiva
- **Revalidación de disponibilidad** de la unidad al iniciar (detecta USBs extraídos)

### Diagnóstico
- **Panel de información**: tamaño, espacio libre, FS actual y tipo
- **Salud S.M.A.R.T.** y tipo de conexión/medio (`Get-PhysicalDisk`: USB/SATA/NVMe · SSD/HDD)
- **Verificación de capacidad real**: detecta memorias USB falsificadas escribiendo y releyendo un patrón

### Experiencia
- **Interfaz bilingüe** Español / Inglés (conmutable en caliente)
- **Tema automático / claro / oscuro**: sigue el tema del sistema Windows en tiempo real; opción de forzar claro u oscuro desde el menú
- **Expulsión segura** de unidades removibles
- **Historial de operaciones** auditado en `%AppData%\FormatDiskPro\history.log`
- **Timer de tiempo transcurrido** y **cancelación segura** de cualquier operación
- **Actualizaciones integradas**: comprueba GitHub Releases al inicio y bajo demanda; descarga e instala la nueva versión
- **Icono propio** de aplicación

## Requisitos

| Requisito | Versión mínima |
|-----------|----------------|
| Windows | 10 / 11 (x64) |
| .NET | 10.0 — *solo para compilar desde código; el instalador lo incluye* |
| Privilegios | Administrador (UAC requerido) |

## Instalación

Descarga el instalador más reciente desde la página de **[Releases](https://github.com/xfiberex/FormatDiskPro/releases)** (`FormatDiskPro-x.y.z-setup.exe`) y ejecútalo. El instalador es *self-contained*: **no requiere instalar .NET** por separado.

### Actualizaciones

La aplicación comprueba si hay una versión más reciente en GitHub Releases al iniciarse y mediante **Ayuda → Buscar actualizaciones…**. Si hay una nueva versión, ofrece descargar e instalar el nuevo instalador automáticamente.

## Construcción

```bash
dotnet build -c Release
```

El ejecutable queda en `src\FormatDiskPro\bin\Release\net10.0-windows\FormatDiskPro.exe`.

### Generar el instalador

Requiere [Inno Setup 6](https://jrsoftware.org/isinfo.php) (`winget install JRSoftware.InnoSetup`):

```powershell
src\FormatDiskPro\installer\build-installer.ps1
```

Publica la app *self-contained* (win-x64) y compila el instalador en `src\FormatDiskPro\installer\Output\`.

### Publicar una versión

El script `release.ps1` (raíz del repo) corta una versión completa en un paso: valida, ejecuta las pruebas, actualiza `<Version>`, compila el instalador, hace commit + tag, lo sube y crea el **GitHub Release** con el instalador adjunto.

```powershell
.\release.ps1 -Version 1.2.0           # release completo
.\release.ps1 -Version 1.2.0 -DryRun   # muestra el plan sin modificar nada
```

Flags: `-DryRun`, `-SkipTests`, `-AllowDirty`, `-NotesFile <archivo.md>`. Los usuarios con una versión anterior recibirán el aviso de actualización automáticamente.

### Pruebas

```bash
dotnet test
```

Las pruebas unitarias (xUnit) cubren la lógica pura aislada en `Core`: construcción de comandos de formato, blindaje anti-inyección, parseo de progreso, longitud de etiqueta, consistencia de presets y comparación de versiones de actualización.

## Uso

1. Ejecutar como **Administrador** (el manifiesto UAC lo solicita automáticamente)
2. Seleccionar la unidad a formatear en el desplegable (cualquiera **salvo la del sistema**, que aparece protegida)
3. Elegir sistema de archivos, tamaño de cluster y etiqueta (o aplicar un **Preset** desde el menú *Configuración*)
4. Pulsar **Iniciar**, escribir la letra de la unidad para confirmar y aceptar

> **Nota de seguridad**: el disco del sistema (donde reside Windows) aparece marcado como `[Protegido]` y todos sus controles de formato quedan deshabilitados. El resto de unidades — removibles, discos de datos fijos y discos RAM — pueden formatearse. Antes de iniciar se exige confirmar escribiendo la letra de la unidad.

### Menú

| Menú | Opciones |
|------|----------|
| **Herramientas** | Verificar capacidad real · Expulsar unidad · Ver historial |
| **Configuración** | Idioma (ES/EN) · Tema (Automático/Claro/Oscuro) · Presets |
| **Ayuda** | Buscar actualizaciones · Acerca de |

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
│  ├─ UpdateChecker.cs      Comparación de versiones para actualizaciones
│  ├─ AppInfo.cs            Versión en ejecución y coordenadas del repositorio
│  └─ Presets.cs            Configuraciones predefinidas
├─ Services/        Efectos colaterales (procesos / disco / red)
│  ├─ DiskService.cs        S.M.A.R.T., expulsión y borrado seguro (PowerShell)
│  ├─ CapacityVerifier.cs   Verificación de capacidad real
│  ├─ UpdateService.cs      GitHub Releases: consulta, descarga e instalación
│  └─ History.cs            Registro de auditoría
├─ UI/              WinUI 3 (Windows App SDK)
│  ├─ MainWindow.xaml / MainWindow.xaml.cs   Ventana principal y orquestación
│  ├─ ConfirmDialog.xaml / .xaml.cs          ContentDialog — confirmación reforzada
│  └─ DriveViewModel.cs                      Modelo de binding para el ComboBox de unidades
├─ Localization/    Cadenas ES/EN centralizadas
├─ installer/       Inno Setup (installer.iss + build-installer.ps1 → Output/)
└─ Program.cs       Punto de entrada

tests/FormatDiskPro.Tests/   Pruebas xUnit sobre la lógica de Core
release.ps1                  Corte de versión en un paso (build + tag + GitHub Release)
```

## Stack

- C# 13 / .NET 10
- **WinUI 3** (Windows App SDK 1.8, unpackaged) — Mica, Fluent Design 2, `ExtendsContentIntoTitleBar`
- `Format-Volume` / `format.com` (formateo) · `cipher` (borrado) · `Get-PhysicalDisk` (S.M.A.R.T.)
- Comandos PowerShell vía `-EncodedCommand` (Base64 UTF-16LE) para evitar inyección
- UAC: `requireAdministrator` en `app.manifest`

## Licencia

Distribuido bajo licencia **MIT**. Consulta el archivo [LICENSE](LICENSE) para más detalles.
