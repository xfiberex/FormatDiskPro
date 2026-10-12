# Área 01 · Código · hallazgos provisionales

> **BORRADOR EN CURSO (punto de control).** Primera versión guardada para no perder trabajo. Leído entero
> hasta ahora: `Program.cs`, `App.xaml(.cs)`, `.csproj`, `app.manifest`, los 20 archivos de `Core/`, los 14
> de `Services/`, las 6 parciales de `MainWindow`, `MainWindow.xaml`, `DeviceChangeWatcher`, `DriveViewModel`,
> `HighContrast`, `SaveFileDialog`, `ConfirmDialog`, `HealthDialog`, `HistoryDialog`, `PresetsDialog`.
> **Pendiente:** `Localization.cs`, diálogos restantes y XAML, pruebas, scripts, instalador, CI; contraste de
> cada hallazgo con `ROADMAP.md`/`CONTEXT.md` (el campo *Estado* de abajo es provisional) y herramientas.

## Hallazgos (candidatos, severidad y estado aún por cerrar)

### P-01-01 · El borrado seguro puede no ejecutarse y aun así registrarse y anunciarse como hecho
**Área:** Código · **Severidad:** Medio · **Confianza:** Alta · **Verificación:** estática · **Esfuerzo:** Bajo
**Ubicación:** `src/FormatDiskPro/UI/MainWindow.xaml.cs:641` · `src/FormatDiskPro/Services/SecureWipe.cs:85,94`
**Estado:** por contrastar
**Problema:** `SecureWipe.RunAsync` devuelve `-1` si la unidad no está lista (`if (!drive.IsReady) return -1;`), pero el llamante descarta el valor: `await _services.Wipe.RunAsync(driveLetter, securePasses, wipeProgress, _cts!.Token);`. El flujo sigue hasta `FORMAT OK … wipe=True passes=N` y el diálogo de éxito.
**Impacto:** quien pidió «Borrado seguro» recibe confirmación y registro de auditoría de un borrado que no se hizo; los datos previos siguen recuperables.
**Solución propuesta:** tratar `!= 0` como fallo (estado de error, `WIPE FAIL` en el historial, diálogo propio) o hacer que el servicio lance.
**Criterio de aceptación:** con un `ISecureWipe` falso que devuelve `-1`, la operación termina en error y el historial no contiene `wipe=True`.

### P-01-02 · Los servicios reanudan en el hilo de UI: el benchmark mide también el salto al despachador
**Área:** Código · **Severidad:** Medio · **Confianza:** Media · **Verificación:** estática (magnitud: ejecución) · **Esfuerzo:** Bajo
**Ubicación:** `Services/BenchmarkRunner.cs:211-226` (y 185-208), `Services/CapacityVerifier.cs:110-122,143-172`, `Services/SecureWipe.cs:118-134`; llamadas sin `Task.Run` en `UI/MainWindow.Operations.cs:65,557` y `UI/MainWindow.xaml.cs:641`
**Estado:** por contrastar
**Problema:** ningún `await` de `Services/` lleva `ConfigureAwait(false)` y la UI los invoca directamente desde el hilo de UI, con `DispatcherQueueSynchronizationContext` instalado (`Program.cs:24-25`). Cada continuación vuelve al hilo de UI: una por cada E/S de 4 KiB en `RndWindowAsync`, y el relleno/comparación de bloques de 8 MB (`FillPattern`, `RandomNumberGenerator.Fill`) se ejecuta en el hilo de UI.
**Impacto:** el cronómetro del 4 KiB Q1 incluye la latencia del despachador (cifra a la baja en unidades rápidas); la verificación y el borrado cargan el hilo de UI por bloque.
**Solución propuesta:** `ConfigureAwait(false)` en los `await` de `Services/` o envolver la llamada en `Task.Run`; `Progress<T>` ya devuelve los informes al hilo de UI.
**Criterio de aceptación:** una prueba que ejecuta `RunAsync` bajo un `SynchronizationContext` contador registra 0 `Post` durante las ventanas de medición.

### P-01-03 · La guarda «no reinicializar el disco de Windows» falla abierta si no se puede leer el disco del sistema
**Área:** Código · **Severidad:** Medio · **Confianza:** Alta · **Verificación:** estática · **Esfuerzo:** Bajo
**Ubicación:** `UI/MainWindow.Operations.cs:348-354`
**Estado:** por contrastar
**Problema:** `if (targetDisk is null || (sysDisk is not null && targetDisk == sysDisk))`: con `sysDisk == null` la comparación se salta y el flujo continúa hacia `Clear-Disk`.
**Solución propuesta:** rechazar también cuando `sysDisk is null`.

### P-01-04 · Una excepción durante el arranque deja un proceso elevado sin ventana
**Área:** Código · **Severidad:** Medio · **Confianza:** Alta (estática) · **Verificación:** estática; reproducible en ejecución · **Esfuerzo:** Bajo
**Ubicación:** `App.xaml.cs:26,45,53`; disparador: `Services/AppSettings.cs:137` + `UI/MainWindow.FormatOptions.cs:444`
**Estado:** por contrastar (T0-01, T9-08)
**Problema:** `OnUnhandledException` marca `e.Handled = true` siempre. Si lanza el constructor de `MainWindow`, `App.MainWindow` queda `null`, no hay diálogo y el proceso sigue vivo sin interfaz. Disparador concreto: `settings.json` válido con `"UserPresets": null` → `_settings.UserPresets.Count` lanza `NullReferenceException` en `BuildPresetsMenu`.

### P-01-05 · Reinicializar cancelada o fallida: sin registro de la cancelación y con la lista de unidades desactualizada
**Ubicación:** `UI/MainWindow.Operations.cs:455-460,477-500`, `UI/MainWindow.xaml.cs:249`, `Services/ReinitDrive.cs:137-138`
**Estado:** por contrastar (T5-03)

### P-01-06 · «Expulsar» da la expulsión por buena sin comprobarla
**Ubicación:** `Services/DiskService.cs:145-156`, `UI/MainWindow.Operations.cs:120-127`

### P-01-07 · chkdsk: el código 3 se presenta como «fallo» también en modo solo lectura, y la salida se descarta
**Ubicación:** `Services/CheckDisk.cs:27-33`, `UI/MainWindow.Operations.cs:283`

### P-01-08 · chkdsk: se escribe una sola «N» y la entrada estándar no se cierra
**Ubicación:** `Services/CheckDisk.cs:62-67` (compárese con `Services/FormatProcess.cs:99`)

### P-01-09 · `ShowOneAsync` descarta también los diálogos de resultado
**Ubicación:** `UI/MainWindow.xaml.cs:318-337,406,883,672-673`
**Estado:** por contrastar (T13-19)

### P-01-10 · `DiskService`: stderr redirigido que nadie lee, sin timeout ni cancelación
**Ubicación:** `Services/DiskService.cs:160-194`

### P-01-11 · `AppSettings.Load` devuelve valores por defecto ante un error de E/S y el arranque los guarda encima
**Ubicación:** `Services/AppSettings.cs:162-165`, `UI/MainWindow.HelpAndUpdates.cs:93-94`
**Estado:** por contrastar (T9-08)

### P-01-12 · Un cambio de dispositivo ajeno restablece etiqueta, tamaño de clúster y la casilla de FAT32 pequeña
**Ubicación:** `UI/MainWindow.xaml.cs:416,434`, `UI/MainWindow.DriveInfo.cs:37-49,61`, `UI/MainWindow.FormatOptions.cs:99`

### P-01-13 · La etiqueta de la unidad anterior se queda en el campo al seleccionar una unidad no lista
**Ubicación:** `UI/MainWindow.DriveInfo.cs:61-62`

### P-01-14 · Si falla el lanzamiento del instalador, la interfaz queda deshabilitada
**Ubicación:** `UI/MainWindow.HelpAndUpdates.cs:241-243`, `UI/MainWindow.xaml.cs:861`

### P-01-15 · El resultado del historial se deduce de palabras sueltas en todo el mensaje, incluida la etiqueta del usuario
**Ubicación:** `Core/HistoryEntry.cs:297-317`, `UI/MainWindow.xaml.cs:663`

### P-01-16 · Huecos del registro de auditoría (formateo + borrado cancelado/fallido, cancelaciones, detalle del fallo)
**Ubicación:** `UI/MainWindow.xaml.cs:643-659,672,682-688`, `UI/MainWindow.Operations.cs:67-72,285-290,455-460`

### P-01-17 · Convenciones propias no aplicadas en todos los sitios (cultura invariante, `DriveLetter`)
**Ubicación:** `UI/ConfirmDialog.xaml.cs:43`, `Services/DiskService.cs:111`, `Core/ReinitPlan.cs:152,203`

### P-01-18 · La documentación XML no se valida al compilar: cabeceras `<para>` sin cerrar y `<summary>` duplicado
**Ubicación:** `UI/MainWindow.Operations.cs:24-27` (y las otras cuatro parciales), `UI/MainWindow.xaml.cs:777-801`

### P-01-19 · `TaskbarProgress` no llama a `HrInit`
**Ubicación:** `Services/TaskbarProgress.cs:52-58`

## Hipótesis por verificar
- Codificación de la salida de `chkdsk`/`format.com`/PowerShell (OEM frente a ANSI) y stderr en CLIXML con `-EncodedCommand`.
- `Initialize-Disk`: la tolerancia compara el mensaje en inglés (`Services/ReinitDrive.cs:93`).

## Pendiente de verificar en ejecución
(por completar)

## Puntos fuertes
(por completar)

## Para otras áreas
(por completar)
