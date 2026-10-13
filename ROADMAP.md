# FormatDiskPro — Hoja de ruta

> **Qué hay aquí:** solo lo **abierto** —las tareas pendientes, por severidad— y lo que está fuera de
> alcance **a propósito**.
>
> **Qué NO hay aquí:** lo ya hecho. Cada tarea cerrada, con cómo se resolvió, está en
> [`docs/roadmap-historico.md`](docs/roadmap-historico.md); el porqué de las decisiones, en
> [`CONTEXT.md`](CONTEXT.md) §4; el registro razonado de cada cambio, en
> [`docs/contexto-historico.md`](docs/contexto-historico.md); y qué trajo cada versión, en
> [`CHANGELOG.md`](CHANGELOG.md).
>
> **Propósito del proyecto:** **formatear, diagnosticar y gestionar unidades en Windows**. Todo lo que hay
> aquí cabe dentro de eso; lo que no, está al final y está fuera **a propósito**.

## Cómo se mantiene

1. **Al completar una tarea, se mueve**: su bloque sale de aquí y entra en
   [`docs/roadmap-historico.md`](docs/roadmap-historico.md#auditoría-2026-10-11--tareas-completadas-aud-nnn),
   marcado `[x]`, con una línea **Hecho (fecha)** y su fila en la tabla de allí. Aquí solo se actualizan
   las cifras de *Estado*. Este archivo no guarda tareas cerradas.
2. **Los IDs no se reutilizan nunca**, tampoco los de tareas descartadas: viven en commits e issues. Hay
   tres numeraciones y solo la última tiene tareas abiertas: `#1`–`#45` (producto), `T0-01`–`T13-20`
   (auditoría del 2026-08-13 y los tiers que la siguieron) y `AUD-001`–`AUD-071` (auditoría del 2026-10-11).
3. **Una tarea nueva** toma el siguiente ID libre y va al tier de su severidad: Tier 1 = Alto, Tier 2 =
   Medio, Tier 3 = Bajo. Si cambia de severidad cambia de sección, no de ID.
4. **Rutas.** `src/…` abrevia `src/FormatDiskPro/…`. Las evidencias (`evidencias/…`, `trabajo/…`) están en
   `docs/auditoria/2026-10-11/`, junto al
   [informe](docs/auditoria/2026-10-11/INFORME.md) que da el detalle de cada hallazgo `AUD`.

## 🏁 Estado

**Última actualización:** 2026-10-13 · **Publicado:** v1.29.0 · **53 tareas abiertas**, ninguna añade
funcionalidad.

| Tier | Severidad | Abiertas |
|---|---|---:|
| **1** | Alto | 0 |
| **2** | Medio | 9 |
| **3** | Bajo | 43 |
| — | Bloqueada a propósito (`T10-02`) | 1 |
| | **Total** | **53** |

De la auditoría del 2026-10-11 (71 hallazgos) quedan **52** y se han cerrado **19**
(`AUD-001`, `AUD-002`, `AUD-003`, `AUD-004`, `AUD-006`, `AUD-007`, `AUD-008`, `AUD-009`, `AUD-011`, `AUD-012`, `AUD-013`, `AUD-014`, `AUD-015`, `AUD-018`, `AUD-019`, `AUD-025`, `AUD-026`, `AUD-027`, `AUD-029`). Por área:

| Área | Alto | Medio | Bajo | Total |
|---|---:|---:|---:|---:|
| Seguridad | – | 1 | – | 1 |
| Código | – | 1 | 3 | 4 |
| QA | – | 1 | 1 | 2 |
| Documentación | – | 1 | 2 | 3 |
| DevOps | – | 4 | 15 | 19 |
| Legal | – | 1 | 5 | 6 |
| Rendimiento | – | – | 1 | 1 |
| UI/UX | – | – | 3 | 3 |
| Arquitectura y resiliencia | – | – | 4 | 4 |
| Refactorización | – | – | 3 | 3 |
| Redacción | – | – | 6 | 6 |
| **Total** | **–** | **9** | **43** | **52** |

**Esfuerzo:** 42 de esfuerzo bajo (hasta 2 horas) y 10 de esfuerzo medio (hasta 1 día); ninguna de
esfuerzo alto.

**Orden recomendado** (el porqué, en la sección 4 del informe): los arreglos de un solo sitio que quedan (`AUD-005` y
`AUD-066`, dos interruptores en GitHub; `AUD-044` a `AUD-047`, textos) → antes del próximo corte, `AUD-020`, `AUD-021` y `AUD-052` a
`AUD-056` → `AUD-017`. Las de licencias (`AUD-024`, `AUD-069`, `AUD-071`) necesitan antes una decisión
del autor.

> En cada tarea, **Depende de** solo aparece si depende de otra, y **Antecedente** solo si reabre o
> completa una tarea anterior. Todas salen de la auditoría del 2026-10-11.

## 🟡 Tier 2 — Medio

- [ ] **AUD-005 · El canal privado de vulnerabilidades que anuncia `SECURITY.md` está desactivado en GitHub**
  - **Área:** Seguridad · **Severidad:** Medio · **Esfuerzo:** Bajo
  - **Ubicación:** `.github/SECURITY.md:20-22` · configuración del repositorio
  - **Qué hacer:** activar *Private vulnerability reporting* en *Settings → Code security*. De paso, activar las alertas de Dependabot (ver AUD-066).
  - **Criterio de aceptación:** la API devuelve `enabled: true` y el enlace de `SECURITY.md` abre el formulario desde una cuenta sin permisos.

- [ ] **AUD-010 · Las consultas a PowerShell de `DiskService` no tienen límite de tiempo ni cancelación, y redirigen una salida de error que nadie lee**
  - **Área:** Código · **Severidad:** Medio · **Esfuerzo:** Bajo
  - **Ubicación:** `src/Services/DiskService.cs:160-194`
  - **Qué hacer:** un plazo por consulta (10 a 15 s) con `CancellationTokenSource` que mate el proceso, y leer o no redirigir `stderr`. Que el fallo por plazo llegue a la interfaz como «no se pudo consultar la unidad».
  - **Criterio de aceptación:** con un `IProcessRunner` falso que no termina, cada método de `DiskService` devuelve su valor de «no se sabe» dentro del plazo.

- [ ] **AUD-016 · El borrado seguro no tiene ninguna prueba automatizada**
  - **Área:** QA · **Severidad:** Medio · **Esfuerzo:** Medio
  - **Ubicación:** `src/Services/SecureWipe.cs:80-146` · `tests/FormatDiskPro.Tests/SecureWipeTests.cs:9` · `tests/FormatDiskPro.UiTests/DestructiveLifecycleTests.cs:233`
  - **Qué hacer:** la misma costura que `CapacityVerifier`: un `RunInAsync(dir, bytes, passes, …)` probado sobre una carpeta temporal (contenido de la última pasada, borrado del directorio, cancelación a mitad), y activar el borrado en una variante del ciclo destructivo de UI.
  - **Criterio de aceptación:** `SecureWipe.cs` supera el 90 % de cobertura y existe una prueba que falla si la última pasada no es aleatoria.

- [ ] **AUD-017 · El requisito de sistema documentado contradice al instalador, y varios archivos siguen citando el Windows App SDK 1.8**
  - **Área:** Documentación · **Severidad:** Medio · **Esfuerzo:** Bajo
  - **Ubicación:** `README.md:94`, `:190` · `.github/CONTRIBUTING.md:13` · `src/installer/installer.iss:11`, `:25`, `:66-67` · `src/FormatDiskPro.csproj:5`, `:43`
  - **Qué hacer:** decidir el mínimo (de hecho, a 2026-10-11, es 19041), declararlo en el `.csproj` (`SupportedOSPlatformVersion`) y alinear README, CONTRIBUTING, `MinVersion` y los tres comentarios.
  - **Criterio de aceptación:** una búsqueda de «1809», «19041» y «1.8» en el repo devuelve un único valor coherente.

- [ ] **AUD-020 · El corte no comprueba en qué rama está ni si va por detrás de `origin`**
  - **Área:** DevOps · **Severidad:** Medio · **Esfuerzo:** Bajo
  - **Ubicación:** `release.ps1:421-426`, `:745-749`, `:805`
  - **Qué hacer:** al principio de las validaciones, `git fetch origin $branch`, abortar si `git rev-list --count HEAD..origin/$branch` es mayor que 0 y avisar si la rama no es `master`. En `-ResumeRelease`, comprobar que el tag está contenido en `origin/$branch`.
  - **Criterio de aceptación:** con un commit de más en `origin/master`, `release.ps1 -DryRun` aborta antes de las pruebas diciendo cuántos commits faltan.

- [ ] **AUD-021 · `-ResumeRelease` publica el instalador que haya en la carpeta de salida sin comprobar que es el del tag**
  - **Área:** DevOps · **Severidad:** Medio · **Esfuerzo:** Bajo
  - **Ubicación:** `release.ps1:473-474`, `:731-742`, `:771-781`, `:847`
  - **Qué hacer:** antes de `gh release create`, recalcular `Get-FileHash` y abortar si no coincide con el `.sha256`; anotar el hash en el mensaje del tag y exigir en `-ResumeRelease` que el instalador presente sea ese.
  - **Criterio de aceptación:** con el `.exe` sustituido tras etiquetar, `-ResumeRelease` aborta nombrando la discrepancia.

- [ ] **AUD-022 · La compilación solo es reproducible para el Windows App SDK**
  - **Área:** DevOps · **Severidad:** Medio · **Esfuerzo:** Medio
  - **Ubicación:** raíz del repo (no existen `global.json`, `nuget.config` ni `packages.lock.json`) · `.github/workflows/ci.yml:49`, `:62-64` · `src/installer/build-installer.ps1:183-190`
  - **Qué hacer:** `global.json` con `rollForward: latestPatch`; `RestorePackagesWithLockFile` con `--locked-mode` en CI y en el corte; `nuget.config` con solo nuget.org; y que el corte deje escrita la versión del runtime empaquetado.
  - **Criterio de aceptación:** dos compilaciones del mismo commit usan el mismo SDK y los mismos paquetes, y cada release declara el runtime que contiene.

- [ ] **AUD-023 · La configuración de agentes de IA está versionada en un repositorio público sin fijar ni vigilar**
  - **Área:** DevOps · **Severidad:** Medio · **Esfuerzo:** Bajo
  - **Ubicación:** `.mcp.json:3-10` · `.claude/settings.json:2-13` · `.claude/CLAUDE.md` · `.agents/skills/*/SKILL.md` · `skills-lock.json` · `.github/CONTRIBUTING.md:47-48` · `.github/PULL_REQUEST_TEMPLATE.md:28`
  - **Qué hacer:** añadir esas rutas a la lista de «revisar con cuidado» de la guía y de la plantilla; un paso de CI que compare cada `SKILL.md` con su hash bloqueado; documentar qué es `codegraph` y de dónde se instala.
  - **Criterio de aceptación:** un PR que toque esas rutas queda señalado y el CI detecta un `SKILL.md` que no corresponde a su hash.

- [ ] **AUD-024 · Los avisos de terceros declaran licencias que no son las de lo que se redistribuye**
  - **Área:** Legal · **Severidad:** Medio · **Esfuerzo:** Medio · *requiere revisión legal*
  - **Ubicación:** `THIRD-PARTY-NOTICES.txt:23`, `:30-40`, `:50-54` · `src/installer/installer.iss:95`
  - **Qué hacer:** citar en cada entrada la licencia que acompaña al artefacto redistribuido, mover Inno Setup al grupo de lo redistribuido, incluir o enlazar los avisos del runtime y del SDK, y decidir con asesoramiento si se añade el permiso adicional.
  - **Criterio de aceptación:** cada binario de terceros del instalador queda cubierto por una entrada con su licencia real, y el proyecto declara por escrito cómo encaja con su GPLv3.
  - **Antecedente:** ya conocido como «revisado, sin hallazgos» (`T9-19` y *Áreas auditadas sin hallazgos* del historial); se reabre por un hecho nuevo: el salto al SDK 2.x (`T13-20`) y la licencia real del paquete

## 🟢 Tier 3 — Bajo

### Código

- [ ] **AUD-028 · El diálogo con el resultado de una operación se descarta si hay otro abierto**
  - **Área:** Código · **Severidad:** Bajo · **Esfuerzo:** Bajo
  - **Ubicación:** `src/UI/MainWindow.xaml.cs:318-337`, `:664`, `:673`, `:879-889` · `src/UI/MainWindow.Operations.cs:475`, `:496-499`
  - **Qué hacer:** para los diálogos de resultado, esperar a que se cierre el abierto en vez de descartar; o cerrar el de «¿Cancelar?» cuando la operación termina.
  - **Criterio de aceptación:** una prueba de UI abre «¿Cancelar?» durante un benchmark, espera a que termine y comprueba que el resultado se muestra al cerrar la pregunta.

- [ ] **AUD-030 · Si falla el lanzamiento del instalador, la interfaz se queda deshabilitada**
  - **Área:** Código · **Severidad:** Bajo · **Esfuerzo:** Bajo
  - **Ubicación:** `src/UI/MainWindow.HelpAndUpdates.cs:241-243`, `:250-261` · `src/UI/MainWindow.xaml.cs:861`
  - **Qué hacer:** poner el indicador después de que `LaunchInstaller` haya devuelto, o devolverlo a `false` en el `catch`.
  - **Criterio de aceptación:** con un `IUpdateService` falso cuyo `LaunchInstaller` lanza, los controles vuelven a estar activos.

- [ ] **AUD-031 · El resultado que muestra el historial se deduce de palabras sueltas del mensaje, incluida la etiqueta del usuario**
  - **Área:** Código · **Severidad:** Bajo · **Esfuerzo:** Bajo
  - **Ubicación:** `src/Core/HistoryEntry.cs:297-317` · `src/UI/MainWindow.xaml.cs:663`
  - **Qué hacer:** decidir el resultado solo con el prefijo de la línea (la operación y la palabra que la sigue), no con todo el mensaje.
  - **Criterio de aceptación:** `FORMAT OK G: … label='A FAIL B'` se clasifica como correcto en una prueba.

### Rendimiento

- [ ] **AUD-032 · El trabajo por bloque y las continuaciones de los servicios se ejecutan en el hilo de la interfaz**
  - **Área:** Rendimiento · **Severidad:** Bajo · **Esfuerzo:** Bajo
  - **Ubicación:** `src/Services/CapacityVerifier.cs:110-122`, `:143-172` · `src/Services/SecureWipe.cs:118-134` · `src/Services/BenchmarkRunner.cs:211-226` · llamadas: `src/UI/MainWindow.Operations.cs:65`, `:557`, `src/UI/MainWindow.xaml.cs:641` · `src/Program.cs:24-25`
  - **Qué hacer:** `ConfigureAwait(false)` en los `await` de `Services/`, o envolver la llamada en `Task.Run`; `Progress<T>` ya devuelve los informes al hilo de UI.
  - **Criterio de aceptación:** una prueba que ejecuta `RunAsync` bajo un `SynchronizationContext` contador registra 0 `Post` durante las fases de E/S.

### UI/UX

- [ ] **AUD-033 · Durante una operación, los tres botones de la barra rápida siguen activos y no hacen nada**
  - **Área:** UI/UX · **Severidad:** Bajo · **Esfuerzo:** Bajo
  - **Ubicación:** `src/UI/MainWindow.xaml.cs:942-968` · `src/UI/MainWindow.xaml:116-132` · `src/UI/MainWindow.Operations.cs:136`, `:525` · `src/UI/MainWindow.HelpAndUpdates.cs:34`
  - **Qué hacer:** deshabilitarlos en `SetControlsEnabled`, como los menús que duplican.
  - **Criterio de aceptación:** durante una operación, los tres botones tienen `IsEnabled = false`.

- [ ] **AUD-034 · Guardar un preset sin nombre responde «Ya existe un preset con ese nombre»**
  - **Área:** UI/UX · **Severidad:** Bajo · **Esfuerzo:** Bajo
  - **Ubicación:** `src/UI/PresetsDialog.xaml.cs:89-93`, `:199-203` · `src/Core/Presets.cs:62-67`
  - **Qué hacer:** distinguir los tres motivos (vacío, demasiado largo, duplicado) con su texto.
  - **Criterio de aceptación:** cada motivo de rechazo muestra un texto distinto, en los cinco idiomas.

- [ ] **AUD-035 · La confirmación de «Reinicializar» no identifica el disco físico ni las otras unidades que se van a perder**
  - **Área:** UI/UX · **Severidad:** Bajo · **Esfuerzo:** Bajo
  - **Ubicación:** `src/Localization/Localization.cs:368` · `src/UI/MainWindow.Operations.cs:432-441` · evidencia: `evidencias/galeria-pt/reinit-light.png`
  - **Qué hacer:** añadir al resumen el modelo y el tamaño del disco y la lista de unidades que desaparecerán («Disco de 29,3 GB · se perderán G: e I:»).
  - **Criterio de aceptación:** sobre un disco con dos particiones, la confirmación nombra las dos letras y el tamaño del disco.

### Arquitectura y resiliencia

- [ ] **AUD-036 · Una operación cancelada o a medias no deja un registro fiel ni actualiza la lista de unidades**
  - **Área:** Arquitectura y resiliencia · **Severidad:** Bajo · **Esfuerzo:** Bajo
  - **Ubicación:** `src/UI/MainWindow.Operations.cs:67-72`, `:285-290`, `:455-460` · `src/UI/MainWindow.xaml.cs:249`, `:612-618`, `:643-659`, `:676-691`
  - **Qué hacer:** registrar siempre el desenlace (`REINIT CANCELLED`, `FORMAT OK` antes del borrado, `WIPE ERROR`) y llamar a `LoadDrives()` en `EndOperation`.
  - **Criterio de aceptación:** cada desenlace de cada operación deja exactamente una línea que lo nombra, y la lista se recarga al terminar.
  - **Antecedente:** `AUD-001` (hecha) ya recarga la lista y dice cómo queda la unidad al cancelar un **formato**; faltan las demás operaciones y los registros

- [ ] **AUD-037 · Los ajustes se escriben sin atomicidad, se degradan a valores por defecto ante un error de lectura y no hay instancia única**
  - **Área:** Arquitectura y resiliencia · **Severidad:** Bajo · **Esfuerzo:** Medio
  - **Ubicación:** `src/Services/AppSettings.cs:162-165`, `:217-229` · `src/UI/MainWindow.HelpAndUpdates.cs:93-94` · `src/Program.cs:13-19`
  - **Qué hacer:** escribir a un temporal y sustituir con `File.Replace`; no guardar en el arranque si la carga falló por E/S; y, si ya hay una instancia, activar su ventana y salir.
  - **Criterio de aceptación:** una prueba simula un fallo de E/S en la lectura y comprueba que el archivo original no se sobrescribe; lanzar la app dos veces deja una sola ventana.

- [ ] **AUD-038 · Si el proceso muere, los archivos temporales de borrado, verificación y benchmark se quedan en la unidad**
  - **Área:** Arquitectura y resiliencia · **Severidad:** Bajo · **Esfuerzo:** Bajo
  - **Ubicación:** `src/Services/SecureWipe.cs:88`, `:144` · `src/Services/CapacityVerifier.cs:59`, `:184` · `src/Services/BenchmarkRunner.cs:61`, `:147`
  - **Qué hacer:** al seleccionar una unidad, detectar esas carpetas y ofrecer borrarlas; y avisar si la limpieza del `finally` falla.
  - **Criterio de aceptación:** con una carpeta `__fdp_verify__` en la unidad, la app lo dice y la borra al aceptarlo.

- [ ] **AUD-039 · El borrado seguro deja sin sobrescribir los últimos 64 MB y no lo dice**
  - **Área:** Arquitectura y resiliencia · **Severidad:** Bajo · **Esfuerzo:** Bajo
  - **Ubicación:** `src/Services/SecureWipe.cs:36`, `:96` · `src/Localization/Localization.cs:142`
  - **Qué hacer:** reducir el margen al mínimo que el sistema de archivos necesite cuando se viene de formatear, o declararlo en la pista de la opción y en el resultado.
  - **Criterio de aceptación:** el margen es coherente con lo que la interfaz promete, y una prueba fija el valor.

### QA

- [ ] **AUD-040 · El respaldo de ajustes de las pruebas de UI no sobrevive a que el proceso de pruebas muera**
  - **Área:** QA · **Severidad:** Bajo · **Esfuerzo:** Bajo
  - **Ubicación:** `tests/FormatDiskPro.UiTests/SettingsBackup.cs:10-33` · `tests/FormatDiskPro.UiTests/AppFixture.cs:27`, `:47-53`
  - **Qué hacer:** escribir el respaldo también a disco y, al empezar la corrida siguiente, restaurarlo si existe uno sin consumir.
  - **Criterio de aceptación:** tras matar el proceso de pruebas a mitad, la corrida siguiente restaura los archivos originales.

### Refactorización

- [ ] **AUD-041 · La misma lógica vive copiada en varios sitios**
  - **Área:** Refactorización · **Severidad:** Bajo · **Esfuerzo:** Medio
  - **Ubicación:** sistemas de archivos y límites: `src/Core/PartitionPlan.cs:124`, `:127`, `src/UI/MainWindow.xaml.cs:24-46`, `:586`, `src/Core/FormatLogic.cs:122-127` · codificación de scripts: `src/Core/FormatLogic.cs:64-68`, `src/Services/DiskService.cs:160-171`, `src/Services/ReinitDrive.cs:96-103` · E/S sin caché: `src/Services/BenchmarkRunner.cs:38-45`, `:155-165`, `src/Services/CapacityVerifier.cs:30-36`, `:193-200`, `src/Services/SecureWipe.cs:32-36`
  - **Qué hacer:** un único catálogo de sistemas de archivos en `Core/`, un único `PowerShellCommand.Encode`, y un ayudante compartido de búfer alineado.
  - **Criterio de aceptación:** cada dato y cada ayudante tiene una sola definición.

- [ ] **AUD-042 · `MainWindow.xaml.cs` vuelve a superar el tope que el propio proyecto se fijó, y no hay formateador ni analizadores configurados**
  - **Área:** Refactorización · **Severidad:** Bajo · **Esfuerzo:** Medio
  - **Ubicación:** `src/UI/MainWindow.xaml.cs` (986 líneas) frente al tope de 800 líneas por parcial (`CONTEXT.md` §2) · bloque de `using` repetido en las seis parciales (p. ej. `src/UI/MainWindow.Operations.cs:1-17`) · raíz del repo (sin `.editorconfig`)
  - **Qué hacer:** separar los ayudantes de diálogo y el ciclo de vida de la operación en sus propias parciales; `global using` para los comunes; `.editorconfig` con `IDE0005` como advertencia.
  - **Criterio de aceptación:** ninguna parcial pasa de 800 líneas y la compilación señala los `using` sin uso.
  - **Antecedente:** regresión del objetivo de `T2-08`, que lo dejó en 753 líneas

- [ ] **AUD-043 · Miembros y textos sin uso, comentarios desfasados y documentación XML que nadie valida**
  - **Área:** Refactorización · **Severidad:** Bajo · **Esfuerzo:** Bajo
  - **Ubicación:** `src/Localization/Localization.cs:470` (`about.body`, sin referencias) · `src/Services/UpdateService.cs:20-28`, `:173` (`Version` siempre igual a `TagName`) · `src/Services/ReinitDrive.cs:14` y `src/Services/DiskService.cs:35` (resúmenes que describen lo que la clase hacía antes) · `src/UI/MainWindow.Operations.cs:24-27` y `src/UI/MainWindow.HelpAndUpdates.cs:23-26` (`<para>` sin cerrar) · `src/UI/MainWindow.xaml.cs:777-801` (dos `<summary>` en el mismo método)
  - **Qué hacer:** borrar lo que no se usa, corregir los resúmenes y activar `GenerateDocumentationFile` con CS1570 como advertencia.
  - **Criterio de aceptación:** la compilación con la documentación activada no da avisos de XML mal formado.

### Redacción

- [ ] **AUD-044 · Dos pistas de la interfaz mandan no usar «el botón Iniciar», que ya no existe**
  - **Área:** Redacción · **Severidad:** Bajo · **Esfuerzo:** Bajo
  - **Ubicación:** `src/Localization/Localization.cs:147`, `:151` (cinco idiomas) · `.github/ISSUE_TEMPLATE/bug_report.yml:69`
  - **Qué hacer:** nombrar el botón como se ve («no el botón Formatear») en los cinco idiomas y en la plantilla.
  - **Criterio de aceptación:** ninguna cadena ni plantilla menciona un botón «Iniciar».

- [ ] **AUD-045 · El aviso de privacidad remite a un menú que no se llama así en inglés, portugués, francés ni italiano**
  - **Área:** Redacción · **Severidad:** Bajo · **Esfuerzo:** Bajo
  - **Ubicación:** `src/Localization/Localization.cs:477` frente a `:232`
  - **Qué hacer:** usar en cada idioma el nombre real del menú; mejor aún, componer el texto con la propia clave `menu.config`.
  - **Criterio de aceptación:** una prueba comprueba que `about.privacy` contiene el valor de `menu.config` en cada idioma.

- [ ] **AUD-046 · Un mensaje de error dice que exFAT admite etiquetas de 32 caracteres; el código valida 11**
  - **Área:** Redacción · **Severidad:** Bajo · **Esfuerzo:** Bajo
  - **Ubicación:** `src/Localization/Localization.cs:439` (cinco idiomas) frente a `src/Core/FormatLogic.cs:122-127`
  - **Qué hacer:** corregir el texto y generarlo a partir de `MaxLabelLength`.
  - **Criterio de aceptación:** una prueba compara las cifras del texto con `MaxLabelLength` para los cinco sistemas.

- [ ] **AUD-047 · Las IOPS del benchmark se formatean con la cultura de Windows y no con la del idioma de la app**
  - **Área:** Redacción · **Severidad:** Bajo · **Esfuerzo:** Bajo
  - **Ubicación:** `src/UI/MainWindow.Operations.cs:622-623`
  - **Qué hacer:** `string.Create(L.Culture, $"…")` o `ToString("N0", L.Culture)`.
  - **Criterio de aceptación:** una prueba fija el separador de miles de las IOPS para cada idioma.
  - **Antecedente:** ya conocido (`T6-12`), cierre incompleto: «los números pasan a seguir al idioma de la app» no llegó a este punto

- [ ] **AUD-048 · Tratamiento, terminología y tipografía no son uniformes dentro de cada idioma**
  - **Área:** Redacción · **Severidad:** Bajo · **Esfuerzo:** Medio
  - **Ubicación:** tratamiento de usted en `src/Localization/Localization.cs:395`, `:396`, `:406`, `:446`, `:455`, `:457` frente al tuteo del resto · terminología y puntuación: lista completa en `trabajo/hallazgos/10-redaccion.md`
  - **Qué hacer:** un glosario breve por idioma en `CONTRIBUTING.md` y una pasada de unificación.
  - **Criterio de aceptación:** el glosario existe y una búsqueda de cada término descartado no devuelve resultados.

- [ ] **AUD-049 · Hay texto visible que no pasa por la localización, y en francés conviven dos sistemas de unidades**
  - **Área:** Redacción · **Severidad:** Bajo · **Esfuerzo:** Medio
  - **Ubicación:** `src/UI/MainWindow.xaml.cs:912` («ETA») · `src/Core/FormatLogic.cs:185` (B, KB, MB, GB, TB fijos) · detalle del historial tal como se escribe en el registro · evidencias: `evidencias/galeria-fr-656/main-secure-light.png`, `evidencias/galeria-es/history-dark.png`
  - **Qué hacer:** pasar «ETA» y las unidades por `L.T` (o decidir que las unidades no se traducen y cambiar el texto fijo francés a «4 GB»).
  - **Criterio de aceptación:** en la galería en francés no conviven «Go» y «GB».

### Documentación

- [ ] **AUD-050 · La plantilla de sugerencias sigue diciendo que el CI está fuera de alcance a propósito**
  - **Área:** Documentación · **Severidad:** Bajo · **Esfuerzo:** Bajo
  - **Ubicación:** `.github/ISSUE_TEMPLATE/feature_request.yml:8-11`
  - **Qué hacer:** quitar «CI» de la lista. (La nota del historial que decía lo mismo ya está corregida.)
  - **Criterio de aceptación:** ninguna plantilla presenta el CI como fuera de alcance.

- [ ] **AUD-051 · El README promete velocidad y tiempo restante «en toda operación larga»; solo las dan dos**
  - **Área:** Documentación · **Severidad:** Bajo · **Esfuerzo:** Bajo
  - **Ubicación:** `README.md:80` frente a `src/UI/MainWindow.xaml.cs:901-916`
  - **Qué hacer:** ajustar la frase («tiempo y cancelación en toda operación larga; velocidad y tiempo restante en las que escriben el espacio libre»).
  - **Criterio de aceptación:** el README describe lo que cada operación muestra.

### DevOps

- [ ] **AUD-052 · El corte prueba una compilación de depuración y sin el listón de cero advertencias**
  - **Área:** DevOps · **Severidad:** Bajo · **Esfuerzo:** Bajo
  - **Ubicación:** `release.ps1:515`, `:519` · `src/installer/build-installer.ps1:183-190` · `.github/workflows/ci.yml:74-84`
  - **Qué hacer:** `dotnet build -c Release -warnaserror` y `dotnet test -c Release --no-build` en el corte.
  - **Criterio de aceptación:** una advertencia introducida a propósito aborta `release.ps1 -DryRun`.

- [ ] **AUD-053 · Las herramientas de los últimos pasos se validan después de mutar, y la simulación no las comprueba**
  - **Área:** DevOps · **Severidad:** Bajo · **Esfuerzo:** Bajo
  - **Ubicación:** `release.ps1:689-728`, `:753-770`, `:814-844` · `src/installer/build-installer.ps1:129`, `:139`, `:177`
  - **Qué hacer:** una comprobación previa compartida con `-DryRun` (Inno Setup, `gh` y su sesión, certificado si se firma) y restaurar el `.csproj` si falla el instalador.
  - **Criterio de aceptación:** sin `gh` en el `PATH`, `release.ps1 -DryRun` aborta nombrándolo.

- [ ] **AUD-054 · La guarda de «sección del CHANGELOG vacía» no salta cuando dos encabezados de versión van seguidos**
  - **Área:** DevOps · **Severidad:** Bajo · **Esfuerzo:** Bajo
  - **Ubicación:** `release.ps1:653-662`
  - **Qué hacer:** exigir `$end -gt $start` y que el cuerpo contenga alguna línea que no sea encabezado.
  - **Criterio de aceptación:** con dos encabezados seguidos, el corte aborta.

- [ ] **AUD-055 · El corte no dice qué pruebas unitarias se omitieron**
  - **Área:** DevOps · **Severidad:** Bajo · **Esfuerzo:** Bajo
  - **Ubicación:** `release.ps1:519-521`, `:292-299`, `:324-328`
  - **Qué hacer:** `--logger trx` también en las unitarias, y tratar como fallo un `.trx` ausente o sin resultados.
  - **Criterio de aceptación:** una prueba omitida aparece nombrada en el resumen del corte.

- [ ] **AUD-056 · El corte solo rechaza una de las tres variables que hacen tocar hardware**
  - **Área:** DevOps · **Severidad:** Bajo · **Esfuerzo:** Bajo
  - **Ubicación:** `release.ps1:605-607` · `tests/FormatDiskPro.UiTests/TestDriveFacts.cs` · `tests/FormatDiskPro.Tests/CapacityVerifierDriveTests.cs:16-23`
  - **Qué hacer:** rechazar o anunciar las tres antes de la primera ejecución de pruebas.
  - **Criterio de aceptación:** con cualquiera de las tres definida, `release.ps1 -DryRun` aborta o lo declara.

- [ ] **AUD-057 · Los paquetes de prueba no los actualiza nadie y ya divergen entre los dos proyectos**
  - **Área:** DevOps · **Severidad:** Bajo · **Esfuerzo:** Bajo
  - **Ubicación:** `.github/dependabot.yml` · `tests/FormatDiskPro.Tests/FormatDiskPro.Tests.csproj:12-17` · `tests/FormatDiskPro.UiTests/FormatDiskPro.UiTests.csproj:12-16`
  - **Qué hacer:** añadir `nuget` a Dependabot con `ignore` para `Microsoft.WindowsAppSDK*`, y unificar versiones con `Directory.Packages.props`.
  - **Criterio de aceptación:** los dos proyectos de prueba referencian las mismas versiones y algo propone sus actualizaciones.

- [ ] **AUD-058 · El CI no ejercita lo que solo falla al publicar**
  - **Área:** DevOps · **Severidad:** Bajo · **Esfuerzo:** Medio
  - **Ubicación:** `.github/workflows/ci.yml:66-84` · raíz del repo (sin `.editorconfig` ni `.gitattributes`)
  - **Qué hacer:** un paso de `dotnet publish --self-contained` que compruebe el `.pri` y la longitud de las rutas, y otro con `shell: powershell` que analice los cuatro `.ps1` y verifique el BOM.
  - **Criterio de aceptación:** un PR que quite el BOM de `release.ps1` o rompa `CopyAppPriToPublish` queda en rojo.

- [ ] **AUD-059 · El script de capturas falla o se degrada cuando lo lanza otro proceso**
  - **Área:** DevOps · **Severidad:** Bajo · **Esfuerzo:** Bajo
  - **Ubicación:** `tools/capture-screenshots.ps1:56-58`, `:588-591`, `:406`, `:418`, `:595`
  - **Qué hacer:** separar `$Only` por comas al entrar, validar cada nombre contra la lista de tomas y fijar `[Console]::OutputEncoding` a UTF-8.
  - **Criterio de aceptación:** las dos formas de invocación producen las mismas tomas y un nombre inexistente aborta nombrándolo.

- [ ] **AUD-060 · `new-selfsigned-cert.ps1` sigue recibiendo la contraseña como texto en la línea de comandos**
  - **Área:** DevOps · **Severidad:** Bajo · **Esfuerzo:** Bajo
  - **Ubicación:** `src/installer/new-selfsigned-cert.ps1:34`, `:41`, `:66`
  - **Qué hacer:** `[SecureString]$Password`, pidiéndola con `Read-Host -AsSecureString` si falta.
  - **Criterio de aceptación:** ningún script del repo declara una contraseña como `[string]`.

- [ ] **AUD-061 · El corte no activa el modo estricto y deja dos redirecciones de error sin proteger**
  - **Área:** DevOps · **Severidad:** Bajo · **Esfuerzo:** Bajo
  - **Ubicación:** `release.ps1:95`, `:418`, `:426` · `src/installer/build-installer.ps1:52`
  - **Qué hacer:** `Set-StrictMode -Version Latest` y un ayudante para las llamadas de solo lectura que comprueban `$LASTEXITCODE`.
  - **Criterio de aceptación:** sin red, `release.ps1 -DryRun` aborta con un mensaje propio.

- [ ] **AUD-062 · El instalador sigue pudiendo vaciar una carpeta ajena si el destino se da por línea de comandos**
  - **Área:** DevOps · **Severidad:** Bajo · **Esfuerzo:** Bajo
  - **Ubicación:** `src/installer/installer.iss:54`, `:82-91`
  - **Qué hacer:** en `[Code]`, negarse si `{app}` existe, no está vacía y no contiene `FormatDiskPro.exe`.
  - **Criterio de aceptación:** con `/DIR=` a una carpeta ajena no vacía, el instalador se niega.
  - **Antecedente:** ya conocido (`T9-17`), cierre incompleto

- [ ] **AUD-063 · No queda registro de lo que cada instalador empaqueta**
  - **Área:** DevOps · **Severidad:** Bajo · **Esfuerzo:** Medio
  - **Ubicación:** `release.ps1:847` · `src/installer/build-installer.ps1:183-227`
  - **Qué hacer:** adjuntar un SBOM (CycloneDX o SPDX) como tercer asset o, como mínimo, una línea en las notas con SDK, runtime y Windows App SDK.
  - **Criterio de aceptación:** cada release declara las versiones que contiene.
  - **Depende de:** AUD-022

- [ ] **AUD-064 · El comando de cobertura que recomienda la guía deja archivos que hacen abortar el corte siguiente**
  - **Área:** DevOps · **Severidad:** Bajo · **Esfuerzo:** Bajo
  - **Ubicación:** `.github/CONTRIBUTING.md:24` · `.gitignore` · `release.ps1:484-487`
  - **Qué hacer:** añadir `TestResults/` y `*.trx` a `.gitignore`.
  - **Criterio de aceptación:** tras ejecutar el comando de la guía, `git status` no lista nada nuevo.

- [ ] **AUD-065 · `build-installer.ps1 -Version` etiqueta el instalador pero no el binario, y los valores por defecto de `installer.iss` están obsoletos**
  - **Área:** DevOps · **Severidad:** Bajo · **Esfuerzo:** Bajo
  - **Ubicación:** `src/installer/build-installer.ps1:183-190`, `:204` · `src/installer/installer.iss:20-28`
  - **Qué hacer:** pasar `-p:Version` al publish o abortar si difiere; quitar los valores por defecto y fallar con `#error`.
  - **Criterio de aceptación:** no se puede producir un instalador cuya versión difiera de la de su ejecutable.

- [ ] **AUD-066 · La rama principal no está protegida y las alertas de dependencias están desactivadas**
  - **Área:** DevOps · **Severidad:** Bajo · **Esfuerzo:** Bajo
  - **Ubicación:** configuración del repositorio `xfiberex/FormatDiskPro`
  - **Qué hacer:** una regla que impida forzar y borrar `master` y exija el check de CI en los PR; activar las alertas de Dependabot; revisar el PR #2.
  - **Criterio de aceptación:** la API devuelve la protección y las alertas activas.

### Legal

- [ ] **AUD-067 · La app conecta con GitHub en el primer arranque tras actualizar aunque la comprobación automática esté desactivada**
  - **Área:** Legal · **Severidad:** Bajo · **Esfuerzo:** Bajo · *requiere revisión legal*
  - **Ubicación:** `src/UI/MainWindow.xaml.cs:280-286` · `src/UI/MainWindow.HelpAndUpdates.cs:84-107` · `src/Localization/Localization.cs:477` · `README.md:198` · `.github/SECURITY.md:53`
  - **Qué hacer:** con la preferencia desactivada, mostrar *Novedades* sin red o no mostrarlo solo; y precisar el texto (actualizaciones y notas de versión).
  - **Criterio de aceptación:** con la preferencia desactivada y una versión nueva, el arranque no emite ninguna petición.
  - **Antecedente:** ya conocido (`T9-18`), cierre incompleto: su criterio era que, desactivada, «no salga ninguna petición de red en el arranque»

- [ ] **AUD-068 · El repo redistribuye nueve *skills* de un tercero sin su licencia ni su aviso de copyright**
  - **Área:** Legal · **Severidad:** Bajo · **Esfuerzo:** Bajo
  - **Ubicación:** `.agents/skills/*/SKILL.md` · `skills-lock.json` · `THIRD-PARTY-NOTICES.txt`
  - **Qué hacer:** añadir la licencia y el copyright del origen junto a los archivos y una línea en los avisos; o dejar de versionarlos.
  - **Criterio de aceptación:** cada archivo de terceros del repo tiene identificada su licencia.

- [ ] **AUD-069 · La licencia se declara como «GPLv3» sin precisar si es «solo» o «o posterior»**
  - **Área:** Legal · **Severidad:** Bajo · **Esfuerzo:** Bajo
  - **Ubicación:** `README.md:196` · `src/Localization/Localization.cs:473` · `src/FormatDiskPro.csproj:16-18`
  - **Qué hacer:** declarar `GPL-3.0-only` o `GPL-3.0-or-later` en los tres sitios y una cabecera SPDX de una línea por fuente.
  - **Criterio de aceptación:** el mismo identificador aparece en README, *Acerca de* y fuentes.

- [ ] **AUD-070 · Al pedir el historial se avisa de «letras y etiquetas», pero también contiene rutas con el nombre de usuario**
  - **Área:** Legal · **Severidad:** Bajo · **Esfuerzo:** Bajo
  - **Ubicación:** `.github/ISSUE_TEMPLATE/bug_report.yml:76-80` · `.github/SECURITY.md:32-33` · `src/UI/MainWindow.HelpAndUpdates.cs:236` · `src/UI/MainWindow.xaml.cs:278` · `src/App.xaml.cs:47` · evidencia: `evidencias/galeria-es/history-dark.png`
  - **Qué hacer:** ampliar el aviso, o registrar solo el nombre del archivo.
  - **Criterio de aceptación:** el aviso enumera lo que el historial puede contener, o el historial deja de contener rutas del perfil.

- [ ] **AUD-071 · La guía de contribución no dice bajo qué licencia entran las aportaciones**
  - **Área:** Legal · **Severidad:** Bajo · **Esfuerzo:** Bajo · *requiere revisión legal*
  - **Ubicación:** `.github/CONTRIBUTING.md` · `.github/PULL_REQUEST_TEMPLATE.md`
  - **Qué hacer:** una sección «Licencia de las contribuciones» y valorar un DCO (`Signed-off-by`).
  - **Criterio de aceptación:** la guía y la plantilla dicen bajo qué términos se acepta una contribución.
  - **Depende de:** AUD-069 y AUD-024 (decidir antes el identificador de licencia y el permiso adicional)

---

## ⏸️ Bloqueada a propósito

- [ ] **T10-02 · La causa del informe de cobertura vacío sigue sin conocerse**
  - **Área:** DevOps · **Severidad:** Bajo · **Esfuerzo:** desconocido (depende de lo que digan los logs)
  - **Ubicación:** `release.ps1` (`Get-CoreCoverage` y el camino de fallo que conserva el informe)
  - **Qué falta:** saber **por qué**, al cortar la v1.25.0, la puerta de cobertura abortó con un informe vacío (`<packages />`, 235 bytes) y las unitarias en verde. Ocurrió con el arreglo de `T8-06` puesto y no se reprodujo en tres intentos.
  - **Por qué espera:** no se puede investigar lo que no se reproduce. Se trabaja **la próxima vez que ocurra**, con los diagnósticos que `T10-01` deja en `%TEMP%\FormatDiskPro_cobertura_fallida`. Única pista, sin verificar: en la corrida que falló la compilación tardó 18,5 s y las pruebas 1 m 6 s (8 s y 20 s en las buenas), y fue la primera con la prueba de capacidad sobre unidad real.
  - **Criterio de aceptación:** nombrar la causa con un log delante o, si tras varias ocurrencias los diagnósticos no la señalan, cerrarla diciendo qué se descartó y con qué pruebas.
  - **Antecedente:** `T8-06` y `T10-01`, en el Tier 10 del [historial](docs/roadmap-historico.md)

---

## 🚫 Deliberadamente fuera de alcance

Adoptar cualquiera de estos sería **cambiar el alcance del producto**:

- **Creador de USB booteable desde ISO** (territorio Rufus).
- **Gestor de particiones completo** (redimensionar / fusionar / mover).
- **Clonado / imagen / backup de discos.**

## 🚫 Decisiones cerradas (no reabrir)

El porqué completo de cada una está en [`CONTEXT.md`](CONTEXT.md#4-decisiones-y-convenciones-clave) §4;
aquí, lo justo para no proponerlas otra vez.

- **La app corre SIEMPRE elevada** (`requireAdministrator`) — firme desde el 2026-07-13. El modelo
  `asInvoker` + worker elevado se evaluó y se descartó: casi todo lo que hace la app necesita
  administrador, así que el menor privilegio sería nominal. Por eso las pruebas de UI y
  `tools/capture-screenshots.ps1` exigen terminal elevada.
- **La ventana es de tamaño fijo (500×900)** — firme desde el 2026-07-13. Es un diálogo de tarea, no un
  espacio de trabajo: ningún contenido gana con más ancho.
- **No se firma el instalador ni se publica en winget** (`#13`, 2026-06-24; reafirmado al descartar
  `T4-03` el 2026-08-16). La distribución es GitHub Releases con auto-actualización, y por eso la
  verificación por SHA-256 (`#38`) es obligatoria. Falta un certificado, que es una compra y no ingeniería. El
  día que lo haya, el trabajo es poner `UpdateService.SignsItsInstallers` en `true` **y** fijar el
  publicador esperado; un test falla si se hace a medias.
- **Las pruebas de UI son locales; el CI cubre compilación y unitarias** (`T2-10`: descartado el
  2026-08-15, revisado el 2026-09-18 al hacerse público el repositorio). Un runner hospedado no puede
  ejecutar la app elevada contra la USB de pruebas, así que el CI solo **compila** las pruebas de UI. La
  puerta de publicación sigue siendo `release.ps1 -UiTests` desde una terminal elevada.
- ***Reinicializar* no es un gestor de particiones** (`T5-04`, 2026-08-16). Crea un layout sobre un disco
  que se borra entero; no redimensiona, fusiona ni mueve datos, y la interfaz se queda en dos particiones.

**Tres tareas abiertas rozan una decisión, y no la tocan:**

- **`AUD-028`** no discute que haya un solo `ContentDialog` a la vez (`T13-19`): señala que el diálogo
  de **resultado** de una operación no es «una acción que el usuario puede repetir», que era el motivo
  para descartar en vez de encolar.
- **`AUD-057`** no discute que el Windows App SDK se suba a mano: señala que excluir NuGet entero de
  Dependabot deja sin vigilar también los paquetes de prueba.
- **`AUD-058`** no pide ejecutar las pruebas de UI en CI: pide que el CI ejercite el publish y los
  scripts, que es donde han estado los tres fallos de publicación documentados.

Lo que una auditoría propuso y se descartó tras comprobarlo está en *Falsos positivos verificados* de
[`CONTEXT.md`](CONTEXT.md#8-falsos-positivos-verificados); las áreas revisadas sin hallazgos, al final del
[historial](docs/roadmap-historico.md#áreas-auditadas-sin-hallazgos).
