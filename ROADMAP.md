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

## ✅ Tier 4 — Refinado de características existentes (completado)

> A diferencia de los tiers anteriores (features **nuevas**), el Tier 4 **no añade capacidades nuevas**:
> **pule y profundiza las que ya existen**, sin salir del propósito del proyecto. Cada item refina una
> feature ya publicada e indica dónde vive en la arquitectura por capas. La numeración continúa la global
> (#14…). Los **quick wins #14/#15/#18/#21** se publicaron en **v1.10.0**; **#16/#17/#19/#20/#22** en **v1.11.0**
> → **Tier 4 completado.**

### 14. Pasadas de borrado seguro configurables — ✅ implementado (v1.10.0) · refina #3
Selector **1 / 3 / 7** del borrado seguro en *Opciones de formato* (antes fijo a 1), activo solo cuando se
marca *Borrado seguro*; la elección se persiste y se muestra en la confirmación (`×N` si > 1). La lógica pura
ya soportaba N pasadas (alterna `0x00`/`0xFF` + última aleatoria).
- Dónde: `Core/SecureWipe` (`AllowedPasses`/`NormalizePasses` puros, además de `PassPattern`/`PlannedBytes`),
  selector en `UI/MainWindow` (`WipePassesPicker`) + `AppSettings.SecureWipePasses`.
- Nota NIST 800-88: 1 pasada basta en discos modernos; el resto es para políticas específicas.

### 15. IOPS en el benchmark — ✅ implementado (v1.10.0) · refina #9
Muestra **IOPS** junto a MB/s en las cifras de 4 KiB aleatorio del resultado (como CrystalDiskMark). Cálculo
puro (`bytes/s ÷ 4096`) y presentación en el diálogo de resultado.
- Dónde: `Core/Benchmark` (`Iops` + `Random4KBlockBytes`, puros testeables) + `UI/MainWindow` (diálogo) + clave `bench.resultBody`.

### 16. S.M.A.R.T. con umbrales de color y refresco — ✅ implementado (v1.11.0) · refina #5
Colorea temperatura / desgaste / errores según rangos (verde / ámbar / rojo) con **texto de estado** anexo
(no solo color, por accesibilidad) y añade un botón **Actualizar** al diálogo (re-consulta sin cerrar).
- Dónde: `Core/SmartInfo` (`SmartLevel` + `TemperatureLevel`/`WearLevel`/`ErrorLevel`, puros testeables) + `UI/HealthDialog`.

### 17. Refresco automático de unidades — ✅ implementado (v1.11.0) · refina la gestión base
Actualiza la lista al **insertar/extraer** una unidad (escucha de `WM_DEVICECHANGE` por subclassing de la
ventana), en vez de depender solo del botón Refrescar, con _debounce_ para no recargar en ráfaga.
- Dónde: `Core/DeviceChange` (constantes + `IsArrivalOrRemoval`, puro) + subclass Win32 + `LoadDrives` en `UI/MainWindow`.

### 18. Idioma automático en el primer arranque — ✅ implementado (v1.10.0) · refina #11
Detecta el idioma del sistema (`CultureInfo.CurrentUICulture`) la **primera** vez (sin `settings.json`) y lo
mapea a ES/EN/PT/FR/IT con fallback a ES. A partir de ahí, la elección manual del usuario manda y se persiste.
- Dónde: `Localization` (`FromCulture` puro testeable) + semilla en `MainWindow` (gated por `AppSettings.LoadedFromFile`).

### 19. Búsqueda / filtro y exportación del historial — ✅ implementado (v1.11.0) · refina #4
Caja de búsqueda + filtros por **categoría** y **resultado** en `HistoryDialog`, y **exportar a CSV** (lo
filtrado) con selector de archivo.
- Dónde: `Core/HistoryEntry` (`Matches` + `ToCsv`, puros testeables) + `UI/HistoryDialog` (filtros + `FileSavePicker`).

### 20. Editar y reordenar presets — ✅ implementado (v1.11.0) · refina #10
Además de añadir/eliminar, permite **editar** (renombrar y, opcionalmente, actualizar la config asociada a la
actual) y **reordenar** (subir/bajar). La persistencia refleja el orden mostrado.
- Dónde: `Core/Presets` (`IsRenameAvailable` puro) + `UI/PresetsDialog` + `AppSettings.UserPresets`.

### 21. Notas de versión en el aviso de actualización — ✅ implementado (v1.10.0) · refina updates
El diálogo *"Actualización disponible"* muestra el **changelog** (cuerpo del release, ya incluido en
`ReleaseInfo.Notes` que devuelve `CheckForUpdateAsync`, convertido con `ReleaseNotes.ToPlainText`) en un panel
con scroll, antes de descargar. Botones *Descargar e instalar* / *Más tarde*.
- Dónde: `UI/MainWindow.ShowUpdateAvailableAsync` reutilizando `Core/ReleaseNotes`; claves `update.availBody`/`changelog`/`download`/`later`.

### 22. Pulido de accesibilidad transversal — ✅ implementado (v1.11.0) · refina la capa UI
`AutomationProperties.Name` + tooltip en los botones de icono (acciones de preset y selector de pasadas),
**aceleradores de teclado** en el menú (Alt + primera letra del título localizado) y **F5** para refrescar
unidades. (El `MaxLength` dinámico de la etiqueta por FS ya se hizo en 1.9.1.)
- Dónde: `UI/*` (`AutomationProperties`, `KeyboardAccelerator`, `AccessKey`) + claves en `Localization`.

---

## ✅ Tier 5 — Confianza, transparencia legal y sostenibilidad (v1.12.0)

> Capa de **distribución/confianza** (no añade funciones de disco): licencia adecuada visible, avisos legales
> y soporte por **donación voluntaria** (nunca obligatoria, no bloquea ninguna función). Numeración global (#23…).

### 23. Relicenciar a GPLv3 + licencia visible in-app — ✅ implementado (v1.12.0)
El proyecto pasa de **MIT** a **GNU GPL v3.0** (software libre con copyleft: los derivados siguen abiertos). El
texto oficial vive en `LICENSE` (embebido en el ejecutable) y se consulta desde *Ayuda → Licencia*.
- Dónde: `LICENSE` (texto GPLv3) embebido vía `.csproj`; `Core/LegalText.License()` + `UI/LegalTextDialog`.

### 24. Disclaimer de uso destructivo / sin garantía — ✅ implementado (v1.12.0)
Aviso claro en *Ayuda → Acerca de*: el formateo/borrado es **irreversible** y el software se ofrece **SIN
GARANTÍA**; comprobar siempre la unidad antes de iniciar.
- Dónde: `UI/AboutDialog` + claves `about.disclaimer*`.

### 25. Avisos de terceros (atribuciones) — ✅ implementado (v1.12.0)
Lista de componentes de terceros y sus licencias (.NET, Windows App SDK/WinUI 3, Segoe Fluent Icons, Inno Setup)
en *Ayuda → Avisos de terceros*.
- Dónde: `THIRD-PARTY-NOTICES.txt` embebido; `Core/LegalText.ThirdParty()` + `UI/LegalTextDialog`.

### 26. Aviso de privacidad — ✅ implementado (v1.12.0)
Declaración en *Acerca de*: sin telemetría ni recopilación de datos; única conexión = GitHub Releases (HTTPS).
- Dónde: `UI/AboutDialog` + clave `about.privacy`.

### 27. Donaciones (opcional, no intrusivo) — ✅ implementado (v1.12.0)
Botón **Apoyar el proyecto** (PayPal) en *Acerca de* y `.github/FUNDING.yml` para el botón «Sponsor» del repo.
Voluntario; **ninguna función se bloquea ni es de pago**.
- Dónde: `AppInfo.DonateUrl` + `UI/AboutDialog`; `.github/FUNDING.yml`.

---

## ✅ Tier 6 — Pulido UX/UI (completado)

> Como el Tier 4, **no añade capacidades nuevas**: refina la **presentación y el feedback** de la UI
> existente con patrones Fluent/WinUI estándar, sin tocar la lógica de formateo ni los servicios.
> Numeración global (#28…). Orden sugerido: quick wins (#28–#32) → trabajo medio (#33–#36).

### 28. Aviso de unidad protegida como InfoBar — ✅ implementado (v1.13.0) · refina la capa UI
El aviso «Disco fijo protegido…» pasa del `StatusText` del footer (texto rojo que compite con el estado
transitorio de las operaciones) a un **`InfoBar` Fluent** (`Severity=Warning`) sobre las tarjetas: icono,
color y semántica de accesibilidad de serie; el footer queda solo para el estado de operaciones.
- Dónde: `UI/MainWindow` (`ProtectedBar` en `ApplyProtection`/`ApplyLanguage`); clave `protected.status`
  sin el prefijo «⚠» (el icono lo aporta el `InfoBar`).

### 29. Confirmación reforzada: foco inicial + Enter para confirmar — ✅ implementado (v1.13.0) · refina el flujo de confirmación (#formato/#8)
Al abrir `ConfirmDialog` el foco va directo a la caja de la letra; cuando la letra escrita coincide,
**Enter** equivale a *Iniciar* (`DefaultButton` pasa a `Primary` dinámicamente). Mantiene la fricción
deliberada (hay que escribir la letra) pero sin obligar a soltar el teclado para pulsar el botón.
- Dónde: `UI/ConfirmDialog`.

### 30. Barra de capacidad en la tarjeta Unidad — ✅ implementado (v1.13.0) · refina la gestión base
**Barra fina usado/libre** bajo los datos de la unidad (elemento estándar de las herramientas de discos):
el porcentaje ocupado se percibe de un vistazo sin leer las cifras de Total/Libre.
- Dónde: `UI/MainWindow` (`CapacityBar` en `UpdateInfo`/`ClearInfo`); clave `info.used` (nombre accesible + tooltip).

### 31. Iconos por tipo de unidad en el selector — ✅ implementado (v1.13.0)
`FontIcon` por `DriveType` (USB / disco fijo / RAM) en el `DataTemplate` del `ComboBox` de unidades:
distinguir unidades de un vistazo sin leer la letra. El icono sigue el color del texto (rojo si protegida).
- Dónde: `UI/DriveViewModel` (`Glyph` por tipo) + template en `MainWindow.xaml`.

### 32. Estado vacío del selector de unidades — ✅ implementado (v1.13.0)
`PlaceholderText` localizado («No hay unidades — conecta un dispositivo») cuando no hay unidades
elegibles; antes el selector quedaba vacío y la tarjeta con guiones, sin explicación.
- Dónde: `UI/MainWindow` (`ApplyLanguage`) + clave `drive.none` en `Localization`.

### 33. Salud coloreada en la tarjeta Unidad — ✅ implementado (v1.13.0) · refina #16
Reusa los umbrales de `SmartLevel` para colorear la línea «Salud:» de la tarjeta principal
(verde/ámbar/rojo), como el diálogo S.M.A.R.T.: aviso temprano sin abrirlo. El estado del diálogo
también pasa a niveles (`HealthLevel`): gana el texto de estado localizado y un estado no reportado
deja de pintarse en rojo.
- Dónde: `Core/SmartInfo.HealthLevel` (puro testeable) + `UI/MainWindow.RenderHealth` +
  `UI/HealthDialog.LevelBrush` (ahora estático compartido).

### 34. Validación inline de la etiqueta — ✅ implementado (v1.13.0) · refina la validación compartida (1.9.1)
Hint de error bajo el `TextBox` mientras se teclean caracteres inválidos o al cambiar de FS (antes solo
un diálogo modal al pulsar Iniciar; el `MaxLength` dinámico ya era inline desde 1.9.1). El modal se
mantiene como respaldo al enviar.
- Dónde: `Core/FormatLogic.ValidateLabel` (+ `LabelValidation`/`InvalidLabelChars`, puros testeables,
  compartidos por el hint y `ValidateLabelAsync`) + `UI/MainWindow` (`LabelErrorText`,
  `VolumeLabelBox_TextChanged`, `UpdateLabelHint`).

### 35. Progreso en la barra de tareas de Windows — ✅ implementado (v1.13.0) · refina #12
`ITaskbarList3.SetProgressValue`/`SetProgressState` durante operaciones largas: el progreso (o el estado
indeterminado) se ve en el icono de la barra de tareas con la app minimizada, a la misma cadencia de 1 s
del cronómetro (complementa el aviso al terminar).
- Dónde: `Services/TaskbarProgress` (helper COM/Win32 propio, no-op si el shell no lo soporta) +
  `UI/MainWindow` (`TimerElapsed_Tick` espeja `FormatProgress`; `EndOperation` limpia el icono).

### 36. Estado de error en la barra de progreso — ✅ implementado (v1.13.0)
`ProgressBar.ShowError` (rojo Fluent) al fallar o cancelar una operación, hasta el siguiente inicio;
antes la barra simplemente se vaciaba y el único feedback era el diálogo modal.
- Dónde: `UI/MainWindow` (`_lastOperationFailed` + `EndOperation` combina con `_cancelRequested`;
  caso especial del benchmark, que detecta el fallo tras `EndOperation`).

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
considera distribución suficiente. → **Tier 3 cerrado.**

**Tier 4 (refinado) — completado:**
1. **Quick wins** — ✅ **v1.10.0**: **#15 IOPS**, **#21 changelog en el aviso**, **#14 pasadas configurables**, **#18 idioma automático**.
2. **Trabajo medio** — ✅ **v1.11.0**: **#16 umbrales S.M.A.R.T.**, **#22 accesibilidad**, **#19 historial (filtro/CSV)**, **#20 editar/reordenar presets**.
3. **Integración con el sistema** — ✅ **v1.11.0**: **#17 refresco automático de unidades** (`WM_DEVICECHANGE`).

→ **Tier 4 cerrado.** El **Tier 5** (confianza/legal/donaciones, #23–#27) va en **v1.12.0**.

**Tier 6 (pulido UX/UI) — completado:**
1. **Quick wins** — ✅ **#28 InfoBar de unidad protegida**, **#29 ConfirmDialog (foco + Enter)**,
   **#30 barra de capacidad**, **#31 iconos por tipo**, **#32 estado vacío**.
2. **Trabajo medio** — ✅ **#33 salud coloreada**, **#34 validación inline de etiqueta**,
   **#35 progreso en la barra de tareas**, **#36 error en la barra de progreso**.

Todo publicado en **v1.13.0**.

→ **Tier 6 cerrado.** Solo queda lo deliberadamente fuera de alcance.

Todos respetan la regla de oro (lógica pura testeable en `Core`) y el propósito del proyecto; ninguno entra en
el territorio "fuera de alcance".
