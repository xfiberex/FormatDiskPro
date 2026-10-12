# Áreas 03 (Rendimiento), 07 (Arquitectura, datos y resiliencia) y 08 (QA) · revisión del coordinador

> Estas tres áreas las revisó el coordinador en la sesión principal (los subagentes que las tenían
> asignadas se cortaron sin escribir nada). El texto definitivo de cada hallazgo está en `INFORME.md`;
> aquí queda **qué se miró, con qué y qué salió**, para la próxima re-auditoría.

## Qué se leyó entero

`UI/MainWindow.xaml.cs`, `UI/MainWindow.Operations.cs`, `UI/MainWindow.HelpAndUpdates.cs`,
`UI/DeviceChangeWatcher.cs`, `UI/DriveViewModel.cs`, `App.xaml.cs`, `Program.cs`, y de `Services/`:
`UpdateService`, `SecureWipe`, `CapacityVerifier`, `BenchmarkRunner`, `ReinitDrive`, `FormatProcess`,
`CheckDisk`, `DiskService`, `ProcessRunner`, `AppSettings`, `History`. De `Core/`: `FormatLogic`.
**Por fragmentos** (lo citado por cada hallazgo): `MainWindow.DriveInfo.cs`, `MainWindow.xaml`,
`Core/HistoryEntry.cs`, `PresetsDialog.xaml.cs`, `HistoryDialog.xaml.cs`, `Core/Presets.cs`.
**No leídos enteros** (muestreo declarado): `MainWindow.FormatOptions.cs`, `MainWindow.Preferences.cs`,
`HealthDialog`, `SaveFileDialog.cs`, `Core/PartitionPlan.cs`, `Core/ReinitPlan.cs`, `Core/SmartInfo.cs`,
`Localization.cs` (este sí lo leyó entero el subagente de redacción), y las pruebas (se leyeron
`TestDrive.cs`, `TestDriveFacts.cs`, `DestructiveLifecycleTests.cs`, `DriveYank.cs`, `AppFixture.cs`,
`SettingsBackup.cs` y los nombres de todos los métodos de prueba).

## Área 03 · Rendimiento

| Qué | Cómo | Resultado |
|---|---|---|
| Arranque hasta ventana | 5 arranques del publish self-contained, máquina en reposo | 586 / 513 / 502 / 515 / 516 ms (media templada **512 ms**) |
| Ventana utilizable (selector en el árbol de UIA) | ídem | media **642 ms** |
| Memoria en reposo | ídem, a los 6 s | working set **185 MB**, privada **121 MB** |
| Tamaño del publish | recuento | **228,7 MB en 519 archivos** (el 2026-08-26: 214,6 MB en 509; el SDK 2.x trae más) |
| Trabajo por bloque en el hilo de UI | micro-medición sin disco (`trabajo/scripts/bloque.cs`, .NET 10.0.12) | `FillPattern` 4,12 ms · `RandomNumberGenerator.Fill` 1,26 ms · `SequenceEqual` 0,36 ms por bloque de 8 MB |

Sin regresión de arranque frente a la medición del 2026-08-26 (578 ms, ~180 MB). Un hallazgo Bajo (el
trabajo por bloque y las continuaciones de los servicios caen en el hilo de UI: irrelevante en USB,
28 % del hilo a 500 MB/s, saturado hacia 1,8 GB/s). No medido: el efecto sobre las IOPS de 4 KiB del
benchmark en unidades rápidas (no se ejecutó el benchmark sobre discos del usuario).

## Área 07 · Arquitectura, datos y resiliencia

Lo que se persiguió y dónde acabó:

| Pregunta | Respuesta | Evidencia |
|---|---|---|
| ¿Cancelar detiene de verdad la operación? | **No** en la vía `Format-Volume` (confirmado); sí en `format.com` (deja la unidad sin formato); sí en chkdsk y en las tres de E/S propia | `evidencias/cancelar-format-volume.txt` |
| ¿Se revalida la unidad entre confirmar y ejecutar? | **No** (estática) | `MainWindow.xaml.cs:443-517`, `Operations.cs:330-354`, `ReinitDrive.cs:84` |
| ¿La guarda del disco de Windows falla cerrada? | **No** si no se puede leer el disco del sistema | `Operations.cs:347-354` |
| ¿Escritura atómica de `settings.json` / `history.log`? | No (`File.WriteAllText`, `AppendAllText`); un JSON truncado se aparta a `.corrupt.json` (`T9-08` sigue resuelta) | `AppSettings.cs:217-229` |
| ¿Instancia única? | No | `Program.cs:18` |
| ¿Limpieza de temporales si el proceso muere? | No hay barrido al arrancar | `SecureWipe.cs:144`, `CapacityVerifier.cs:184`, `BenchmarkRunner.cs:147` |
| ¿Timeouts en procesos y red? | Red: 30 s hasta cabeceras + cancelación. Procesos de `DiskService`: ninguno | `DiskService.cs:173-194` |
| ¿Límites de capa? | `Core` sin WinUI/`Process`/`HttpClient` (cobertura 97,7 %); servicios con interfaz, compuestos en `AppServices` | lectura |
| ¿Extracción de la unidad a mitad? | Cubierta por las dos pruebas `*_DriveDisappears_*` (3/3 pasan con opt-in) | `evidencias/ui-destructivas.trx` |
| ¿Cierre de la ventana durante una operación? | Cancelado con aviso (`AppWindow_Closing`); `WM_CLOSE` y `taskkill` en reposo cierran bien | `trabajo/scripts/cierre-wmclose.ps1` |

## Área 08 · QA y testing

- **Unitarias:** 690 (689 pasan, 1 omitida por opt-in `FORMATDISKPRO_VERIFY_DRIVE`), 1 s.
- **UI:** 43 (40 pasan, 3 omitidas por opt-in) en 1 m 45 s; las 3 de opt-in pasan (2 m 15 s) y dejan los discos idénticos.
- **Cobertura real por carpeta** (coverlet, `trabajo/scripts/cov.js`): `Core` **97,7 %** (516/528; el corte exige 90 %) · `Localization` 100 % · `Services` **65,4 %** (539/824) · `UI` 0 % en unitarias (1.988 líneas; la cubren las de UI, que no miden cobertura).
- **Servicios por debajo del 30 %:** `SecureWipe` 15 % (6/41), `BenchmarkRunner` 7 %, `Notifier` 7 %, `ProcessRunner` 21 %, `TaskbarProgress` 0 %, `AppServices` 0 %.
- **Guardas destructivas:** `DriveLetter.Same` tiene prueba (incluida cultura turca) y hay una de UI para el menú Herramientas sobre el disco de sistema; **ninguna** ejerce el rechazo de formatear sobre el disco de sistema ni los tres filtros de Reinicializar.
- **Selección de la unidad de pruebas:** por etiqueta (`utilidades`) y solo entre `DriveType.Removable`; las destructivas exigen además la variable de opt-in con valor exacto `1`. No puede elegir un disco fijo.
- **Respaldo de ajustes:** `SettingsBackup` vive solo en memoria; si el proceso de pruebas muere, no hay de dónde restaurar.
- **Esperas fijas:** 18 `Thread.Sleep` en las pruebas de UI (9 en `KeyboardAndNamingTests`); no se observó ninguna prueba intermitente en dos corridas.
- **`T10-02`** (informe de cobertura vacío): no se reprodujo en esta corrida (el informe salió completo); sigue abierta.

## Hipótesis que no llegaron a hallazgo

- `TaskbarProgress` no llama a `ITaskbarList3.HrInit` antes de usar la interfaz (`Services/TaskbarProgress.cs:55-58`): la documentación lo exige; no se observó ningún fallo.
- Decodificación de la salida de `chkdsk`/`format.com` (página OEM) como UTF-8 por defecto en .NET 10: los acentos saldrían rotos en el diálogo de error; no se comprobó desde la app.
- `Clear-Disk` sobre el disco de arranque: es posible que Windows lo rechace por su cuenta (no comprobable sin riesgo).
- IOPS de 4 KiB del benchmark infravaloradas en unidades rápidas por el salto al despachador en cada operación.
