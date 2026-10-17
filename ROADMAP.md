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

**Última actualización:** 2026-10-16 · **Publicado:** v1.30.1 · **17 tareas abiertas**, ninguna añade
funcionalidad.

| Tier | Severidad | Abiertas |
|---|---|---:|
| **1** | Alto | 0 |
| **2** | Medio | 1 |
| **3** | Bajo | 15 |
| — | Bloqueada a propósito (`T10-02`) | 1 |
| | **Total** | **17** |

De la auditoría del 2026-10-11 (71 hallazgos) quedan **16** y se han cerrado **55**
(`AUD-001`, `AUD-002`, `AUD-003`, `AUD-004`, `AUD-005`, `AUD-006`, `AUD-007`, `AUD-008`, `AUD-009`, `AUD-010`, `AUD-011`, `AUD-012`, `AUD-013`, `AUD-014`, `AUD-015`, `AUD-016`, `AUD-017`, `AUD-018`, `AUD-019`, `AUD-020`, `AUD-021`, `AUD-022`, `AUD-023`, `AUD-025`, `AUD-026`, `AUD-027`, `AUD-029`, `AUD-031`, `AUD-033`, `AUD-034`, `AUD-037`, `AUD-040`, `AUD-044`, `AUD-045`, `AUD-046`, `AUD-047`, `AUD-050`, `AUD-051`, `AUD-052`, `AUD-053`, `AUD-054`, `AUD-055`, `AUD-056`, `AUD-057`, `AUD-058`, `AUD-059`, `AUD-060`, `AUD-061`, `AUD-062`, `AUD-063`, `AUD-064`, `AUD-065`, `AUD-066`, `AUD-068`, `AUD-069`). Por área:

| Área | Alto | Medio | Bajo | Total |
|---|---:|---:|---:|---:|
| Seguridad | – | – | – | 0 |
| Código | – | – | 2 | 2 |
| QA | – | – | – | 0 |
| Documentación | – | – | – | 0 |
| DevOps | – | – | – | 0 |
| Legal | – | 1 | 3 | 4 |
| Rendimiento | – | – | 1 | 1 |
| UI/UX | – | – | 1 | 1 |
| Arquitectura y resiliencia | – | – | 3 | 3 |
| Refactorización | – | – | 3 | 3 |
| Redacción | – | – | 2 | 2 |
| **Total** | **–** | **1** | **15** | **16** |

**Esfuerzo:** 11 de esfuerzo bajo (hasta 2 horas) y 5 de esfuerzo medio (hasta 1 día); ninguna de
esfuerzo alto.

**Orden recomendado** (el porqué, en la sección 4 del informe): las de resiliencia de las operaciones
(`AUD-036`, `AUD-038`, `AUD-039`, con `AUD-035`), que piden pruebas de UI en terminal elevada. Las de licencias
(`AUD-024`, y después `AUD-071`) y `AUD-067` necesitan antes una decisión del autor; el identificador ya
está decidido (`GPL-3.0-only`).

> En cada tarea, **Depende de** solo aparece si depende de otra, y **Antecedente** solo si reabre o
> completa una tarea anterior. Todas salen de la auditoría del 2026-10-11.

## 🟡 Tier 2 — Medio

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
  - **Avance (2026-10-15):** el arreglo está hecho: en el `catch` genérico de `DownloadAndRunUpdateAsync`, `_closingForUpdate` vuelve a `false` antes de que `EndOperation` decida si rehabilita los controles. **Para cerrarla falta** la prueba que pide el criterio: un `IUpdateService` falso cuyo `LaunchInstaller` lance, inyectado en la app real. No existe aún la forma de inyectarlo en una prueba de UI, y la sesión que hizo el cambio no podía ejecutar la app.

### Rendimiento

- [ ] **AUD-032 · El trabajo por bloque y las continuaciones de los servicios se ejecutan en el hilo de la interfaz**
  - **Área:** Rendimiento · **Severidad:** Bajo · **Esfuerzo:** Bajo
  - **Ubicación:** `src/Services/CapacityVerifier.cs:110-122`, `:143-172` · `src/Services/SecureWipe.cs:118-134` · `src/Services/BenchmarkRunner.cs:211-226` · llamadas: `src/UI/MainWindow.Operations.cs:65`, `:557`, `src/UI/MainWindow.xaml.cs:641` · `src/Program.cs:24-25`
  - **Qué hacer:** `ConfigureAwait(false)` en los `await` de `Services/`, o envolver la llamada en `Task.Run`; `Progress<T>` ya devuelve los informes al hilo de UI.
  - **Criterio de aceptación:** una prueba que ejecuta `RunAsync` bajo un `SynchronizationContext` contador registra 0 `Post` durante las fases de E/S.

### UI/UX

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

### Legal

- [ ] **AUD-067 · La app conecta con GitHub en el primer arranque tras actualizar aunque la comprobación automática esté desactivada**
  - **Área:** Legal · **Severidad:** Bajo · **Esfuerzo:** Bajo · *requiere revisión legal*
  - **Ubicación:** `src/UI/MainWindow.xaml.cs:280-286` · `src/UI/MainWindow.HelpAndUpdates.cs:84-107` · `src/Localization/Localization.cs:477` · `README.md:198` · `.github/SECURITY.md:53`
  - **Qué hacer:** con la preferencia desactivada, mostrar *Novedades* sin red o no mostrarlo solo; y precisar el texto (actualizaciones y notas de versión).
  - **Criterio de aceptación:** con la preferencia desactivada y una versión nueva, el arranque no emite ninguna petición.
  - **Antecedente:** ya conocido (`T9-18`), cierre incompleto: su criterio era que, desactivada, «no salga ninguna petición de red en el arranque»

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
  - **Depende de:** AUD-024 (decidir antes el permiso adicional). El identificador ya está decidido: `GPL-3.0-only` (`AUD-069`, 2026-10-16)

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

**Una tarea abierta roza una decisión, y no la toca:**

- **`AUD-028`** no discute que haya un solo `ContentDialog` a la vez (`T13-19`): señala que el diálogo
  de **resultado** de una operación no es «una acción que el usuario puede repetir», que era el motivo
  para descartar en vez de encolar.

Lo que una auditoría propuso y se descartó tras comprobarlo está en *Falsos positivos verificados* de
[`CONTEXT.md`](CONTEXT.md#8-falsos-positivos-verificados); las áreas revisadas sin hallazgos, al final del
[historial](docs/roadmap-historico.md#áreas-auditadas-sin-hallazgos).
