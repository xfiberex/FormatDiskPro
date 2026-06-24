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

### 9. Benchmark rápido de lectura/escritura — ✅ implementado (v1.7.0; refinado tras v1.8.0)
Perfil estilo CrystalDiskMark, **no destructivo** (archivo temporal de ~512 MB), que mide cuatro cifras:
**secuencial** (bloque 1 MiB, cola **Q8**) y **4 KiB aleatorio** (Q1), lectura y escritura. Toda la E/S es
**sin caché del sistema** (`FILE_FLAG_NO_BUFFERING` vía `RandomAccess` + buffer alineado al sector); la fase
secuencial mantiene varias operaciones en vuelo (overlapped I/O con `Task.WhenAll`) para no infravalorar
NVMe/SSD. Cada fase se mide por **ventanas de tiempo** (se adapta a unidades rápidas y lentas) y se toma la
**mediana** de 3 pasadas (descarta el arranque en frío y los picos).
- Dónde: `Core/Benchmark.cs` (tamaño alineado a bloque, velocidad, mediana y desplazamiento aleatorio,
  puro testeable; `BenchmarkResult` con `Sequential`/`Random4K`), `Services/BenchmarkRunner.cs` (motor de
  E/S sin caché con cola profunda) + `MnuBenchmark` en `MainWindow`.
- Permitido en **cualquier unidad lista** (incluido el disco interno); valida espacio libre (~576 MB).

---

## ✅ Tier 3 — Pulido / distribución (completado; #13 descartado)

### 10. Presets personalizados del usuario — ✅ implementado (v1.8.0)
Guardar la configuración de formato actual como preset propio y eliminarlos, persistidos en
`settings.json`. Aparecen en *Configuración → Presets* junto a los integrados; se gestionan desde
*Presets → Gestionar presets…*.
- Dónde: `Core/Presets` (`NormalizeName`/`IsNameAvailable`, puros), `AppSettings.UserPresets`,
  `UI/PresetsDialog.xaml` + reconstrucción de `BuildPresetsMenu`.

### 11. Más idiomas — ✅ implementado (v1.8.0)
`Localization.cs` refactorizado de tupla `(Es, En)` a arreglo por idioma; añadidos **Portugués, Francés
e Italiano** (ES/EN/PT/FR/IT). Selección en *Configuración → Idioma*, persistida.
- Dónde: `Localization.cs` (`AppLang` ampliado, `FromCode`/`ToCode`, ~250 claves × 5 idiomas) + menú.
- Prueba de completitud: cada clave tiene 5 traducciones no vacías.

### 12. Aviso al terminar — ✅ implementado (v1.8.0)
Sonido + parpadeo de la barra de tareas al completar operaciones largas (≥ 10 s), solo si la ventana no
está en primer plano. Interruptor *Configuración → Avisar al terminar* (por defecto activado).
- Dónde: `Services/Notifier.cs` (`ShouldNotify` puro + Win32 `FlashWindowEx`/`MessageBeep`),
  `AppSettings.NotifyOnFinish`, llamada en `EndOperation`.

### 13. Confianza y distribución — ❌ descartado (2026-06-24)
Se evaluó publicar un **paquete winget** (con la **firma del instalador** como prerequisito práctico para
pasar limpia la validación) para instalar/actualizar vía `winget`. **Descartado por decisión del usuario:**
la distribución por **GitHub Releases** con **auto-actualización integrada** se considera suficiente y fiable
para el público objetivo, y **no se firmará el instalador**. El soporte de firma sigue disponible de forma
**opcional** en `build-installer.ps1`/`release.ps1` para quien lo necesite, pero deja de ser un objetivo del
proyecto. → **Tier 3 cerrado.**

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
**#8 (reinicializar unidad)** y **#9 (benchmark)** en v1.7.0 → **Tier 2 completado**;
**#10 (presets personalizados)**, **#11 (más idiomas: PT/FR/IT)** y **#12 (aviso al terminar)** en v1.8.0.
El **#13 (winget + firma)** queda **descartado** (2026-06-24): GitHub Releases + auto-actualización se
considera distribución suficiente. → **Tier 3 cerrado** (solo resta lo deliberadamente fuera de alcance).
