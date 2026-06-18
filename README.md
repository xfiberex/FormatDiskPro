# FormatDiskPro

Herramienta de formateo y **gestión de unidades** para Windows con soporte para **5 sistemas de archivos**, diagnóstico S.M.A.R.T., verificación de capacidad real y protección de discos fijos.

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
- **Protección de discos fijos**: marcados como `[Protegido]`, con todos los controles deshabilitados
- **Doble guardia del disco de sistema** por letra de unidad
- **Confirmación reforzada**: hay que escribir la letra de la unidad para confirmar el formateo
- **Validación de etiqueta de volumen** antes de la operación destructiva
- **Revalidación de disponibilidad** de la unidad al iniciar (detecta USBs extraídos)

### Diagnóstico
- **Panel de información**: tamaño, espacio libre, FS actual y tipo
- **Salud S.M.A.R.T.** y tipo de conexión/medio (`Get-PhysicalDisk`: USB/SATA/NVMe · SSD/HDD)
- **Verificación de capacidad real**: detecta memorias USB falsificadas escribiendo y releyendo un patrón

### Experiencia
- **Interfaz bilingüe** Español / Inglés (conmutable en caliente)
- **Tema claro / oscuro** (Windows 11, `Application.SetColorMode`)
- **Expulsión segura** de unidades removibles
- **Historial de operaciones** auditado en `%AppData%\FormatDiskPro\history.log`
- **Timer de tiempo transcurrido** y **cancelación segura** de cualquier operación
- **Icono propio** de aplicación

## Requisitos

| Requisito | Versión mínima |
|-----------|----------------|
| Windows | 10 / 11 |
| .NET | 10.0 |
| Privilegios | Administrador (UAC requerido) |

## Construcción

```bash
dotnet build -c Release
```

El ejecutable queda en `bin\Release\net10.0-windows\FormatDiskPro.exe`.

## Uso

1. Ejecutar como **Administrador** (el manifiesto UAC lo solicita automáticamente)
2. Seleccionar una unidad **removible** en el desplegable
3. Elegir sistema de archivos, tamaño de cluster y etiqueta (o aplicar un **Preset** desde el menú *Configuración*)
4. Pulsar **Iniciar**, escribir la letra de la unidad para confirmar y aceptar

> **Nota de seguridad**: los discos fijos aparecen marcados como `[Protegido]` y todos sus controles de formato quedan deshabilitados. Solo es posible formatear unidades removibles y discos RAM.

### Menú

| Menú | Opciones |
|------|----------|
| **Herramientas** | Verificar capacidad real · Expulsar unidad · Ver historial |
| **Configuración** | Idioma (ES/EN) · Tema (Claro/Oscuro) · Presets |
| **Ayuda** | Acerca de |

## Sistemas de archivos disponibles

| FS | Recomendado para | Límite de archivo |
|----|-----------------|-------------------|
| NTFS | Discos internos Windows | Sin límite práctico |
| exFAT | USB > 32 GB | Sin límite práctico |
| ReFS | Almacenamiento crítico | Sin límite práctico |
| FAT32 | USB ≤ 32 GB, consolas | 4 GB |
| FAT | Unidades < 2 GB | 2 GB |

## Arquitectura

| Archivo | Responsabilidad |
|---------|-----------------|
| `Form1.cs` / `Form1.Designer.cs` | UI principal y orquestación |
| `Localization.cs` | Cadenas ES/EN centralizadas |
| `DiskService.cs` | S.M.A.R.T., expulsión y borrado seguro (PowerShell) |
| `CapacityVerifier.cs` | Verificación de capacidad real |
| `ConfirmFormatDialog.cs` | Confirmación reforzada |
| `Presets.cs` | Configuraciones predefinidas |
| `History.cs` | Registro de auditoría |

## Stack

- C# 13 / .NET 10
- Windows Forms (`net10.0-windows`)
- `Format-Volume` / `format.com` (formateo) · `cipher` (borrado) · `Get-PhysicalDisk` (S.M.A.R.T.)
- Comandos PowerShell vía `-EncodedCommand` (Base64 UTF-16LE) para evitar inyección
- UAC: `requireAdministrator` en `app.manifest`
