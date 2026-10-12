# Notas del coordinador · verificación propia y áreas 3, 7 y 8

> Desde las 22:07 del 2026-10-11 la revisión estática la termina el coordinador en la sesión principal:
> reanudar los siete subagentes agotó tres veces el límite de sesión. Los archivos `01`…`13` de esta
> carpeta son lo que los subagentes dejaron escrito (12 y 13 completos; 1, 2, 5, 6, 9, 10 y 11 parciales;
> 3, 7 y 8 sin empezar). Aquí queda lo que el coordinador verificó leyendo el código y lo que añadió.

## Código leído entero por el coordinador

`UI/MainWindow.xaml.cs`, `UI/MainWindow.Operations.cs`, `UI/MainWindow.HelpAndUpdates.cs`,
`Services/UpdateService.cs`, `SecureWipe.cs`, `CapacityVerifier.cs`, `ReinitDrive.cs`, `FormatProcess.cs`,
`ProcessRunner.cs`, `AppSettings.cs`, `DiskService.cs`, `History.cs`, `CheckDisk.cs`, `BenchmarkRunner.cs`,
`Core/FormatLogic.cs`, `App.xaml.cs`, `Program.cs`, `UI/DeviceChangeWatcher.cs`.

## Candidatos de los subagentes VERIFICADOS en el código (línea a línea)

| Prov. | Qué | Verificado en | Nota |
|---|---|---|---|
| P-02-01 | Instalador verificado y luego ejecutado por ruta en `%TEMP%\FormatDiskPro_update` | `UpdateService.cs:217-227`, `:202-214`, `:399-404`, `:523-528`; `MainWindow.HelpAndUpdates.cs:235-243` | Cierto. Ningún handle se mantiene entre el hash y `Process.Start`. `Directory.GetFiles(dir)`+`File.Delete` sigue uniones. Candidato Alto → verificador |
| P-02-02 | `OpenUrl` e `History.Open` con `ShellExecute` desde proceso elevado; `OpenUrl` sin validar esquema | `UpdateService.cs:541-549`, `History.cs:77-86`, `MainWindow.HelpAndUpdates.cs:163` | Cierto |
| P-02-04 | `powershell.exe` / `chkdsk.exe` por nombre | `DiskService.cs:166`, `ReinitDrive.cs:101`, `FormatProcess.cs:57`, `CheckDisk.cs:54`; contraste `FormatProcess.cs:86` | Cierto |
| P-01-01 | Resultado de `Wipe.RunAsync` descartado | `MainWindow.xaml.cs:641`; `SecureWipe.cs:85,94` devuelve `-1` | Cierto |
| P-01-03 | Guarda de Reinicializar falla abierta con `sysDisk == null` | `MainWindow.Operations.cs:347-354` | Cierto |
| P-01-04 | `e.Handled = true` siempre; si lanza el constructor de `MainWindow` no hay ventana ni diálogo | `App.xaml.cs:26,45,53`; `AppSettings.cs:137` no normaliza `UserPresets: null` | Cierto en estática → reproducir |
| P-01-05 | Reinicializar cancelada: sin `History.Log` ni `LoadDrives()` | `MainWindow.Operations.cs:455-460` | Cierto |
| P-01-06 | Expulsar: `code == 0` de PowerShell ≠ expulsada | `DiskService.cs:145-156`, `Operations.cs:120-127` | Cierto |
| P-01-07 | chkdsk: código 3 en solo lectura → `Failed`; salida descartada | `CheckDisk.cs:27-33`, `Operations.cs:283` | Cierto. Según la tabla de códigos de Microsoft, 3 incluye «errores no corregidos porque no se indicó /f» |
| P-01-08 | chkdsk: una sola «N» y stdin sin cerrar | `CheckDisk.cs:62-67` | Cierto → reproducir con un handle abierto en la USB |
| P-01-09 | `ShowOneAsync` descarta también diálogos de resultado | `MainWindow.xaml.cs:318-337`, `:664`, `:673`, `:879-889` | Cierto: basta el diálogo «¿Cancelar?» abierto cuando la operación termina |
| P-01-10 | `DiskService`: stderr redirigido sin leer, sin timeout ni cancelación | `DiskService.cs:160-194` | Cierto |
| P-01-11 | `AppSettings.Load` → valores por defecto ante error de E/S y `Save()` en el arranque | `AppSettings.cs:162-165`, `MainWindow.HelpAndUpdates.cs:93-94` | Cierto; escritura no atómica (`File.WriteAllText`, `:223`) |
| P-01-14 | Si falla `LaunchInstaller`, `_closingForUpdate` queda en `true` y `EndOperation` no rehabilita | `MainWindow.HelpAndUpdates.cs:241-243`, `MainWindow.xaml.cs:861` | Cierto |
| P-01-16 | Huecos de auditoría: formato hecho + borrado cancelado solo deja `WIPE CANCELLED`; excepción en el borrado → `FORMAT ERROR` | `MainWindow.xaml.cs:643-659`, `:676-691` | Cierto |
| P-01-02 | Sin `ConfigureAwait(false)` en `Services/`; continuaciones y relleno de bloques en el hilo de UI | `BenchmarkRunner.cs:211-226`, `CapacityVerifier.cs:110-122,143-172`, `SecureWipe.cs:118-134` | Cierto en estática; magnitud por medir |
| P-13-02 | Petición a GitHub en el primer arranque tras actualizar aunque la preferencia esté desactivada | `MainWindow.xaml.cs:280` (antes del `if` de `:285`), `MainWindow.HelpAndUpdates.cs:84-107` | Cierto |

## Hallazgos nuevos del coordinador (áreas 7 y 3), pendientes de redactar en formato final

- **C-07-A · No se revalida la identidad de la unidad entre la confirmación y la operación.** Formatear: `StartButton_Click` captura `driveItem` antes del diálogo (`MainWindow.xaml.cs:443`, `:511-517`) y `RunFormatAsync` usa solo la letra. Reinicializar: los tres filtros (extraíble, no sistema, disco ≠ disco de Windows) se evalúan antes del diálogo (`Operations.cs:330-354`) y el script vuelve a resolver el disco **por letra** (`ReinitDrive.cs:84`). Con el diálogo abierto `_isBusy` es `false`, así que `LoadDrives()` sí refresca la lista (`:249`) pero el diálogo y la operación siguen con la letra capturada. Reproducible intercambiando letras en la USB de pruebas.
- **C-07-B · Cancelar un formato lanzado con `Format-Volume` mata PowerShell, no el formato.** `FormatProcess.cs:61` (`proc.Kill(entireProcessTree: true)`): el trabajo lo hace el proveedor de almacenamiento (WMI), fuera del árbol de procesos. Por comprobar en ejecución con un formato completo exFAT en la USB.
- **C-07-C · El borrado seguro deja sin sobrescribir 64 MB** (`SecureWipe.cs:36`, `:96`) y no lo dice. Por contrastar con los textos de la interfaz y con ROADMAP/CONTEXT.
- **C-07-D · Sin instancia única.** `Program.cs:18` crea el mutex con `initiallyOwned: false` y no mira si ya existía: dos instancias elevadas pueden operar sobre la misma unidad y pisarse `settings.json`.
- **C-07-E · Restos de `__fdp_wipe__` / `__fdp_verify__` / `__fdp_bench__`** si el proceso muere o el borrado del `finally` falla (`SecureWipe.cs:144`, `CapacityVerifier.cs:184`, `BenchmarkRunner.cs:147`): nada los limpia en el arranque siguiente ni los menciona.
- **C-03-A · Arranque medido en reposo:** 512 ms hasta la ventana, 642 ms utilizable, WS 185 MB, privada 121 MB. Sin regresión frente al 2026-08-26.

## Ejecución pendiente (orden)

1. `settings.json` con `"UserPresets": null` → ¿proceso elevado sin ventana? (P-01-04)
2. chkdsk `/f` con un handle abierto en la USB → ¿segunda pregunta y cuelgue? (P-01-08)
3. Formato completo exFAT de I: cancelado a los pocos segundos → ¿sigue formateando? (C-07-B)
4. Intercambio de letras con el diálogo de confirmación abierto (C-07-A)
5. Cierre por `WM_CLOSE`: a qué ventana va y por qué no cierra
6. Volcado del árbol de UIA de la ventana principal (P-05-01) y orden de tabulación
7. gitleaks portable · `release.ps1 -DryRun`
8. **Lo último:** expulsar la USB desde la app (P-01-06); requiere volver a enchufarla
