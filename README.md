# FormatDiskPro

Herramienta de formateo de discos para Windows con soporte para **5 sistemas de archivos** y protección de discos fijos.

Inspirada en el diálogo nativo de Windows "Formatear unidad", pero con funciones extendidas de seguridad, información de unidad en tiempo real y sugerencia automática de sistema de archivos.

## Características

- **5 sistemas de archivos**: NTFS, exFAT, ReFS, FAT32 y FAT
- **Sugerencia automática** según tipo y tamaño de unidad
- **Descripción contextual** de cada sistema de archivos
- **Panel de información** de la unidad seleccionada (tamaño, espacio libre, FS actual, tipo)
- **Protección de discos fijos**: controles deshabilitados con aviso visual, imposible formatear discos internos desde la UI
- **Doble guardia del disco de sistema**: bloqueo adicional por letra de unidad aunque se intente saltarse la protección visual
- **Validación de etiqueta de volumen** antes de la operación destructiva
- **Formato completo o rápido**
- **Compresión NTFS** opcional
- **Timer de tiempo transcurrido** durante el formateo
- **Cancelación segura** del proceso en curso
- **Revalidación de disponibilidad** de la unidad al iniciar (detecta USBs extraídos)

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
3. Elegir sistema de archivos, tamaño de cluster y etiqueta
4. Pulsar **Iniciar** y confirmar en el diálogo de advertencia

> **Nota de seguridad**: los discos fijos aparecen marcados como `[Protegido]` y todos sus controles de formato quedan deshabilitados. Solo es posible formatear unidades removibles y discos RAM.

## Sistemas de archivos disponibles

| FS | Recomendado para | Límite de archivo |
|----|-----------------|-------------------|
| NTFS | Discos internos Windows | Sin límite práctico |
| exFAT | USB > 32 GB | Sin límite práctico |
| ReFS | Almacenamiento crítico | Sin límite práctico |
| FAT32 | USB ≤ 32 GB, consolas | 4 GB |
| FAT | Unidades < 2 GB | 2 GB |

## Stack

- C# 13 / .NET 10
- Windows Forms (`net10.0-windows`)
- `Format-Volume` (PowerShell) vía `-EncodedCommand` (Base64 UTF-16LE)
- UAC: `requireAdministrator` en `app.manifest`
