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

## ⏳ Tier 2 — Diagnóstico y gestión (pendiente)

Refuerzan el corazón "diagnóstico/gestión" del proyecto.

### 5. S.M.A.R.T. ampliado — ✅ implementado (publicado en v1.5.0)
Detalle del disco físico además de Salud/Bus/Media: **temperatura, horas de encendido, desgaste de SSD,
RPM y errores de lectura/escritura** (`Get-StorageReliabilityCounter`), en un **diálogo dedicado**
abierto desde *Herramientas → Salud del disco (S.M.A.R.T.)…*.
- Dónde: `Core/SmartInfo.cs` (modelo + parser testeable), `DiskService.GetSmartAsync`, `UI/HealthDialog.xaml`.
- Consulta bajo demanda (no recarga el panel inline). Fallback "No disponible" para unidades sin contadores (USB).

### 6. Verificar / reparar sistema de archivos (chkdsk)
Herramienta "Comprobar errores": `chkdsk /scan` (solo lectura) y opción `/f` (reparar), con parseo de
salida y progreso. Muy demandado para USBs con problemas.
- Dónde: nuevo `Services/CheckDisk.cs` + entrada en el menú Herramientas + progreso en el footer.
- Esfuerzo: medio.

### 7. Detección y quita de protección de escritura
Detectar unidad *read-only* **antes** de formatear (causa típica de fallo) y ofrecer quitarla
(`diskpart attributes disk clear readonly`). Mensajes claros en vez de un error críptico.
- Dónde: `Services/DiskService.cs` (consulta de atributos) + flujo en `MainWindow`.
- Esfuerzo: medio. Riesgo: bajo-medio (diskpart).

### 8. Reinicializar unidad (diskpart clean)
Para USB con particiones raras o RAW: `diskpart clean` + crear partición primaria + formatear, dejando
la unidad como una sola partición usable.
- Dónde: nuevo `Services/ReinitDrive.cs` + flujo en `MainWindow`.
- Esfuerzo: medio-alto. **Riesgo alto** → exige las mismas guardas que ya existen
  (`IsSystemDrive`, confirmación reforzada escribiendo la letra).

### 9. Benchmark rápido de lectura/escritura
Test secuencial corto (MB/s de lectura y escritura), reutilizando la mecánica de E/S por bloques de
`CapacityVerifier`/`SecureWipe`.
- Dónde: nuevo `Services/Benchmark.cs` + diálogo de resultados.
- Esfuerzo: medio.

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

**#5 (S.M.A.R.T. ampliado)** ya está publicado (v1.5.0). Siguiente recomendado:
**#7 (protección de escritura)** y **#6 (chkdsk)**, ambos de bajo-medio riesgo. Dejar
**#8 (reinicializar unidad)** para cuando se quiera asumir su mayor riesgo, con guardas reforzadas.
