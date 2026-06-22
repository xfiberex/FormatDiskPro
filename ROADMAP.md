# FormatDiskPro — Hoja de ruta de características

> Mejoras agrupadas por **tiers**, manteniéndose siempre dentro del propósito del proyecto:
> **formatear, diagnosticar y gestionar unidades en Windows**. Cada item indica una idea de
> dónde viviría en la arquitectura por capas (`Core` lógica pura testeable · `Services` efectos
> colaterales · `UI` WinUI 3 · `Localization`). El detalle del estado vive en [`CONTEXT.md`](CONTEXT.md).

---

## ✅ Tier 1 — Quick wins (implementado en v1.4.0)

| # | Característica | Dónde |
|---|----------------|-------|
| 1 | **Persistencia de configuración** (idioma, tema, última unidad) | `Services/AppSettings.cs` → `%AppData%\FormatDiskPro\settings.json` |
| 2 | **ETA + velocidad (MB/s)** en operaciones largas | `Core/Throughput.cs` + timer en `MainWindow` |
| 3 | **Borrado seguro con progreso real** (sobrescritor propio, reemplaza `cipher /w`) | `Services/SecureWipe.cs` |
| 4 | **Visor de historial integrado** (lista filtrable, abrir/vaciar) | `Core/HistoryEntry.cs` + `UI/HistoryDialog.xaml` |

---

## ✅ Tier 2 — Diagnóstico y gestión (completado)

Refuerzan el corazón "diagnóstico/gestión" del proyecto.

### 5. S.M.A.R.T. ampliado — ✅ implementado (publicado en v1.5.0)
Detalle del disco físico además de Salud/Bus/Media: **temperatura, horas de encendido, desgaste de SSD,
RPM y errores de lectura/escritura** (`Get-StorageReliabilityCounter`), en un **diálogo dedicado**
abierto desde *Herramientas → Salud del disco (S.M.A.R.T.)…*.
- Dónde: `Core/SmartInfo.cs` (modelo + parser testeable), `DiskService.GetSmartAsync`, `UI/HealthDialog.xaml`.
- Consulta bajo demanda (no recarga el panel inline). Fallback "No disponible" para unidades sin contadores (USB).

### 6. Verificar / reparar sistema de archivos (chkdsk) — ✅ implementado (publicado en v1.6.0)
Herramienta *Herramientas → Comprobar errores (chkdsk)…* con modo **Solo comprobar** (read-only, universal)
o **Comprobar y reparar** (`/f`), progreso parseado en el footer y diálogo de resultado.
- Dónde: `Services/CheckDisk.cs` (`RunAsync` + `Interpret` puro testeable) + `MnuCheck` en `MainWindow`.
- Reparación bloqueada en el disco de sistema (evita programar reinicio).

### 7. Detección y quita de protección de escritura — ✅ implementado (publicado en v1.6.0)
Detecta unidad *read-only* y ofrece quitarla **automáticamente al pulsar Iniciar** (evita el fallo críptico)
y desde *Herramientas → Quitar protección de escritura…*. Usa cmdlets de Storage (`Set-Disk -IsReadOnly`),
no diskpart.
- Dónde: `DiskService.IsDiskReadOnlyAsync`/`ClearReadOnlyAsync` + flujo en `MainWindow`.

### 8. Reinicializar unidad — ✅ implementado (publicado en v1.7.0)
Para USB con particiones raras o RAW: limpia el disco y recrea una **única partición primaria**
formateada, dejando la unidad usable. Usa cmdlets de Storage (`Clear-Disk`/`Initialize-Disk`/
`New-Partition`/`Format-Volume`), no diskpart, coherente con el resto de `DiskService`.
- Dónde: `Core/ReinitPlan.cs` (estilo MBR/GPT + parseo, puro testeable), `Services/ReinitDrive.cs`
  (streaming de etapas), `DiskService.GetDiskNumberAsync` + `MnuReinit` en `MainWindow`.
- **Solo unidades extraíbles** + guardas reforzadas: bloqueo del disco de sistema/protegido,
  comparación de **número de disco físico** objetivo ≠ Windows, y confirmación escribiendo la letra
  (`ConfirmDialog`) advirtiendo que se borra **todo el disco físico**.

### 9. Benchmark rápido de lectura/escritura — ✅ implementado (publicado en v1.7.0)
Test secuencial corto (MB/s de lectura y escritura), **no destructivo** (escribe/relee un archivo
temporal de ~256 MB), reutilizando la mecánica de E/S por bloques de `CapacityVerifier`/`SecureWipe`.
- Dónde: `Core/Benchmark.cs` (tamaño de prueba + velocidad, puro testeable),
  `Services/BenchmarkRunner.cs` + `MnuBenchmark` en `MainWindow`.
- Permitido en **cualquier unidad lista** (incluido el disco interno); valida espacio libre.

---

## ⏳ Tier 3 — Pulido / distribución (pendiente)

### 10. Presets personalizados del usuario
Crear/guardar/eliminar presets propios, persistidos junto a `settings.json`. Hoy `Core/Presets` es fijo.
- Dónde: extender `Core/Presets` + `Services/AppSettings.cs` + pequeña UI.
- Esfuerzo: pequeño-medio.

### 11. Más idiomas
`Localization.cs` ya está preparado (diccionario ES/EN). Añadir p. ej. PT/FR/IT es casi mecánico.
- Esfuerzo: pequeño (por idioma).

### 12. Aviso al terminar
Beep + parpadeo de la barra de tareas al completar operaciones largas, para poder alejarse del PC.
- Dónde: `MainWindow` (al final de las operaciones largas).
- Esfuerzo: pequeño-medio.

### 13. Confianza y distribución
No son features de código, pero de alto valor:
- **Firma Authenticode** con certificado OV/EV (quita SmartScreen — el build ya lo soporta vía
  `build-installer.ps1`/`release.ps1`).
- Paquete **winget** para instalación/actualización sencilla.
- **GitHub Actions** que ejecute `release.ps1` (CI/CD).

---

## 🚫 Deliberadamente fuera de alcance

Se excluyen a propósito para no desviar el producto de su propósito. Adoptar cualquiera de ellos sería
una **decisión de cambiar el alcance**:

- **Creador de USB booteable desde ISO** (territorio Rufus) — gran expansión.
- **Gestor de particiones completo** (redimensionar/fusionar/mover).
- **Clonado / imagen / backup de discos.**

---

## Sugerencia de priorización

**#5 (S.M.A.R.T.)** en v1.5.0; **#6 (chkdsk)** y **#7 (protección de escritura)** en v1.6.0;
**#8 (reinicializar unidad)** y **#9 (benchmark)** en v1.7.0 → **Tier 2 completado**.
Siguiente: el **Tier 3** (presets personalizados, más idiomas, aviso al terminar, firma/winget/CI).
