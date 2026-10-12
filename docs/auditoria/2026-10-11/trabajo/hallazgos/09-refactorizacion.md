# Área 09 · Refactorización y limpieza · hallazgos provisionales

> **BORRADOR EN CURSO (punto de control).** Primera versión guardada para no perder trabajo. Falta leer
> `Localization.cs`, diálogos restantes y XAML, pruebas, scripts, instalador y CI; contrastar cada hallazgo
> con `ROADMAP.md`/`CONTEXT.md` (el *Estado* es provisional) y ejecutar herramientas.

## Hallazgos (candidatos)

### P-09-01 · Bloque de 16 `using` copiado en las seis parciales de `MainWindow`; no hay formateador ni analizadores de estilo configurados
**Ubicación:** `UI/MainWindow.Operations.cs:1-17`, `.DriveInfo.cs:1-16`, `.FormatOptions.cs:1-16`, `.HelpAndUpdates.cs:1-16`, `.Preferences.cs:1-16`, `.xaml.cs:1-16`; raíz del repo sin `.editorconfig`

### P-09-02 · La lista de sistemas de archivos y sus límites viven en varios sitios
**Ubicación:** `Core/PartitionPlan.cs:124,127`, `UI/MainWindow.xaml.cs:24-46,586`, `UI/MainWindow.FormatOptions.cs:39-43`, `Core/FormatLogic.cs:122-127`

### P-09-03 · Tres copias de «codificar un script para `-EncodedCommand`»
**Ubicación:** `Core/FormatLogic.cs:64-68`, `Services/DiskService.cs:160-171`, `Services/ReinitDrive.cs:96-103`

### P-09-04 · Constantes y utilidades de E/S sin caché duplicadas entre `BenchmarkRunner`, `CapacityVerifier` y `SecureWipe`
**Ubicación:** `Services/BenchmarkRunner.cs:38,44,155-165`, `Services/CapacityVerifier.cs:30-36,193-200`, `Services/SecureWipe.cs:32-36`, `Core/Benchmark.cs:28`

### P-09-05 · Bucle de lectura con `carry` duplicado en `CheckDisk` y `FormatProcess`
**Ubicación:** `Services/CheckDisk.cs:69-95`, `Services/FormatProcess.cs:101-128`
**Estado:** decisión documentada en `Services/ProcessRunner.cs:51-55` (por contrastar)

### P-09-06 · Miembros sin uso real o redundantes
**Ubicación:** `Services/UpdateService.cs:20-28,173` (`ReleaseInfo.Version` == `TagName`), `UI/MainWindow.xaml.cs:121` (parámetro por omisión sin llamantes)

### P-09-07 · Comentarios y documentación desfasados
**Ubicación:** `Core/ReinitPlan.cs:32-33`, `Services/ReinitDrive.cs:14`, `Services/DiskService.cs:35`, `Core/SeverityPalette.cs:115`, `UI/MainWindow.xaml.cs:118-119`

### P-09-08 · `MainWindow.xaml.cs` vuelve a superar el tope de 800 líneas que el propio proyecto se fijó
**Ubicación:** `UI/MainWindow.xaml.cs` (986 líneas) frente a `CONTEXT.md:80`

## Hipótesis por verificar
(por completar)

## Cosas a comprobar en ejecución
(por completar)

## Puntos fuertes
(por completar)
