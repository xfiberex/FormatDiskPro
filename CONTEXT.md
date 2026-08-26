# Contexto del proyecto — FormatDiskPro

> **Qué es este archivo.** El contexto **vivo** del proyecto: qué es, cómo está montado, **qué se decidió y
> por qué**, y qué se aprendió por el camino. Sirve para retomarlo tras meses sin tocarlo, o desde otro
> equipo, sin repetir errores ya cometidos.
>
> El **§4 (Decisiones)** es la parte que más importa: casi todas nacieron de un fallo real. Léelo antes de
> "mejorar" algo que parezca raro.

| | |
|---|---|
| **Repositorio** | https://github.com/xfiberex/FormatDiskPro |
| **Versión publicada** | **1.24.1** (2026-08-26) |
| **Estado** | Producto (Tiers 1–9), auditoría de calidad, Tier 5 «Ocurrencias» y **Tier 6 — refinado de UX/UI** (15/15): **cerrados**. Abierto: **Tier 7 — consistencia y descubribilidad de la UI** (7/8, desde 2026-08-25) |
| **Stack** | C# 13 · .NET 10 · **WinUI 3** (Windows App SDK **1.8.260529003**, unpackaged, `net10.0-windows10.0.19041.0`) · xUnit · FlaUI/UIA3 · Inno Setup 6 |
| **Licencia** | GPLv3 · avisos de terceros · donaciones opcionales (PayPal) |
| **Pruebas** | **588** unitarias (`Core/` al 97,9 %) · **30** de UI sobre la app real — **26 pasan / 3 se omiten** (solo los opt-in) con la USB conectada, verificado el 2026-08-17 |
| **Hoja de ruta** | [`ROADMAP.md`](ROADMAP.md) — **sin tareas abiertas** (Tiers 7 y 8 cerrados el 2026-08-26, 9/9 y 5/5) · [`CHANGELOG.md`](CHANGELOG.md) — qué trajo cada versión |
| **Última actualización** | 2026-08-26 (**Tiers 7 y 8 cerrados**, publicados en la **v1.24.0** y la **v1.24.1**. El 7 acabó en el foco recortado de los diálogos (`T7-09`); el 8 salió de una captura del historial en uso y encontró que ***Exportar CSV* nunca funcionó en ninguna versión publicada** (`T8-01`), y luego que el propio corte publicaba unas notas vacías (`T8-04`)) |

---

## 1. Qué es

Utilidad de **formateo, diagnóstico y gestión de unidades** para Windows: 5 sistemas de archivos
(NTFS/exFAT/ReFS/FAT32/FAT), **S.M.A.R.T. avanzado**, verificación de capacidad real (detecta USB
falsificados), chkdsk, benchmark, borrado seguro, reinicializar unidad, protección de escritura, presets,
5 idiomas, tema claro/oscuro, historial exportable y **auto-actualización verificada** vía GitHub Releases.

**Corre siempre elevada** (`requireAdministrator`) y **su ventana es de tamaño fijo**: las dos son decisiones
firmes, no limitaciones. Ver §4.

## 2. Arquitectura (separación por capas)

```
src/FormatDiskPro/
├─ Core/            Lógica PURA y testeable (sin UI, sin procesos, sin red)
│  ├─ FormatLogic.cs      Comandos de formato, parseo de %, formato de bytes, validación de etiqueta
│  ├─ SmartInfo.cs        Modelo + parseo del detalle S.M.A.R.T. + umbrales de severidad (SmartLevel)
│  ├─ SeverityPalette.cs  INVENTARIO de colores semánticos por tema — contraste WCAG medido sobre All()
│  ├─ HistoryEntry.cs     Parseo del historial + filtro + exportación CSV (anti CSV injection)
│  ├─ ReinitPlan.cs       Estilo MBR/GPT por tamaño, tamaños de FAT32 pequeña que caben, parseo de letras
│  ├─ PartitionPlan.cs    EL LAYOUT COMO DATO: particiones + validación tipada ANTES de borrar nada (T5-01)
│  ├─ Benchmark.cs        Tamaño de prueba, velocidad, IOPS, mediana
│  ├─ SecureWipe.cs*      Patrón y nº de pasadas del borrado seguro (*la parte pura)
│  ├─ Presets.cs          Presets integrados (nombre traducido vía NameKey) + validación de los del usuario
│  ├─ Throughput.cs       Velocidad y ETA de operaciones largas
│  ├─ DeviceChange.cs     Interpretación de WM_DEVICECHANGE (autorefresco de unidades)
│  ├─ ReleaseNotes.cs     Notas de versión (Markdown) → texto plano
│  ├─ LegalText.cs        Licencia GPLv3 y avisos de terceros embebidos en el .exe
│  ├─ UpdateChecker.cs    Comparación de versiones (IsNewer)
│  ├─ DriveLetter.cs      Comparación de letras de unidad invariante de cultura (guarda del disco de sistema)
│  ├─ OperationFailure.cs Línea de historial de una operación fallida (camino de error de T0-02)
│  ├─ HistoryRotation.cs  Política de rotación del historial (umbral y nombre de la generación anterior)
│  └─ AppInfo.cs          Versión, coordenadas del repo, enlace de donación
├─ Services/        Efectos colaterales (procesos / disco / red) — clases con interfaz, inyectadas
│  ├─ AppServices.cs       RAÍZ DE COMPOSICIÓN: el único sitio que decide qué implementación usa la app
│  ├─ ProcessRunner.cs     IProcessRunner: la costura que hace testeables los caminos de error (T4-02)
│  ├─ DiskService.cs       S.M.A.R.T., nº de disco, protección de escritura, expulsión (PowerShell)
│  ├─ SecureWipe.cs        Sobrescritor propio del espacio libre, con progreso
│  ├─ CheckDisk.cs         chkdsk (comprobar / reparar) con streaming de progreso
│  ├─ ReinitDrive.cs       Reinicializar disco extraíble: clean + ejecutar un PartitionPlan ya validado
│  ├─ BenchmarkRunner.cs   Motor de E/S sin caché (FILE_FLAG_NO_BUFFERING), no destructivo
│  ├─ CapacityVerifier.cs  Verificación de capacidad real
│  ├─ AppSettings.cs       Preferencias (%AppData%\FormatDiskPro\settings.json)
│  ├─ Notifier.cs          Aviso al terminar (sonido + parpadeo de barra de tareas, Win32)
│  ├─ TaskbarProgress.cs   Progreso en el icono de la barra de tareas (ITaskbarList3)
│  ├─ FormatProcess.cs     Lanza Format-Volume (PowerShell) y format.com, con progreso real
│  ├─ UpdateService.cs     GitHub Releases: consulta, descarga, VERIFICACIÓN (firma/SHA-256), instalación
│  └─ History.cs           Auditoría (%AppData%\FormatDiskPro\history.log, rotado a history.1.log)
├─ UI/              WinUI 3 (Windows App SDK)
│  ├─ MainWindow          Ventana principal, repartida en partial class por asunto (ninguna >800 líneas):
│  │                      .xaml.cs (ciclo de vida, unidades, formato) · .DriveInfo · .FormatOptions
│  │                      .Operations (menú Herramientas) · .HelpAndUpdates · .Preferences
│  ├─ DeviceChangeWatcher Subclassing Win32 de WM_DEVICECHANGE (autorefresco de unidades)
│  ├─ ConfirmDialog       Confirmación reforzada (escribir la letra de la unidad)
│  ├─ HealthDialog        Detalle S.M.A.R.T. (colores por umbral + texto de estado)
│  ├─ HistoryDialog       Visor de historial (búsqueda, filtros, exportar CSV)
│  ├─ PresetsDialog       Gestionar presets propios (guardar / editar / reordenar / eliminar)
│  ├─ WhatsNewDialog      Novedades tras actualizar
│  ├─ AboutDialog         Acerca de: disclaimer, privacidad, donación
│  ├─ LegalTextDialog     Visor de licencia / avisos de terceros
│  ├─ Theme/AppTheme.xaml Tokens de diseño (tarjetas, encabezados, footer)
│  └─ DriveViewModel.cs   Binding del ComboBox de unidades (icono por tipo)
├─ Localization/    Localization.cs — 5 idiomas (ES/EN/PT/FR/IT), L.T("clave")
├─ installer/       installer.iss (Inno Setup) + build-installer.ps1 → Output/ (gitignored)
└─ Program.cs       Punto de entrada

tests/FormatDiskPro.Tests/    563 pruebas xUnit sobre Core y los Services (con IProcessRunner falso)
tests/FormatDiskPro.UiTests/  30 pruebas FlaUI/UIA3 sobre el .exe real — FUERA de la solución (ver abajo)
tools/capture-screenshots.ps1 Regenera docs/screenshots/ conduciendo la app por UI Automation
CHANGELOG.md                  Qué cambió en cada versión (Keep a Changelog); el corte exige su sección
release.ps1                   Corte de versión en un paso (tests + instalador + tag + GitHub Release)
FormatDiskPro.slnx            Solución: app + Tests. UiTests NO está incluido, a propósito.
```

**Regla de oro:** la lógica testeable vive en `Core`, **sin dependencias de WinUI, `Process` ni `HttpClient`**.
La UI y los servicios la consumen. Namespace único `FormatDiskPro`.

**Segunda regla, desde `T4-02` (2026-08-16):** los `Services` son **clases con interfaz**, no estáticas, y
nadie las construye salvo la raíz de composición `AppServices`. `App` la crea y se la pasa a `MainWindow`,
que se la pasa a los diálogos. **No es un localizador de servicios** —nadie le pide nada «desde dentro»—,
así que cada constructor sigue declarando de qué depende.

### Pruebas de UI (`FormatDiskPro.UiTests`) — lo que hay que saber

Lanzan el **`.exe` real** y lo automatizan por UI Automation, localizando controles por `AutomationId` (en
WinUI, el `x:Name` del XAML se expone como tal sin configuración extra).

- **Fuera de `FormatDiskPro.slnx` a propósito:** si estuvieran dentro, el `dotnet test` de los unitarios los
  arrastraría siempre, y necesitan condiciones que no toda máquina tiene. Se lanzan por ruta, o con
  `release.ps1 -UiTests`.
- **Exigen terminal ELEVADA.** UIPI bloquea que un proceso no elevado automatice la ventana de uno que sí lo
  está, y la app es `requireAdministrator`. `AppFixture` lo comprueba y falla con un mensaje claro en vez de
  colgarse.
- **Precondición ausente ≠ fallo.** Los 6 tests que necesitan la **USB de pruebas** (partición extraíble
  etiquetada `utilidades`) **se OMITEN** si no está conectada, vía `[TestDriveFact]`; el que borra datos de
  verdad se omite salvo `FORMATDISKPRO_ALLOW_DESTRUCTIVE=1`, vía `[DestructiveFact]`. Antes **fallaban**, y por
  eso no se podían meter en el pipeline.
- **Un `ContentDialog` convive con MÁS DE UN `ControlType.Window`** en el árbol: WinUI deja un proxy de Popup
  vacío junto al diálogo real. `DialogHelper.FindDialog` se queda con el que tiene contenido (más hijos).
- **`AppFixture` respalda y restaura `settings.json` + `history.log`** de `%AppData%` en cada corrida: la app
  es unpackaged, así que sin eso las pruebas contaminarían la instalación real del usuario.
- **NUNCA dos corridas en paralelo** contra la misma app/unidad: cada una lanza su instancia elevada del `.exe`
  y compiten por el mismo `DrivePicker` y el mismo `settings.json`. Produce fallos imposibles de diagnosticar.
- `VerifyCapacity_CompletesForTestDrive` lleva `[Trait("Category","Slow")]`: escribe y relee casi todo el
  espacio libre (es lo que hace falta para detectar capacidad falsificada) y puede superar los 30 min. Fuera
  del filtro por defecto.
- **Para iterar, lanza SIEMPRE con el filtro** — es lo mismo que hace `release.ps1`:

  ```
  dotnet test tests\FormatDiskPro.UiTests\FormatDiskPro.UiTests.csproj --no-build --filter "Category!=Slow"
  ```

  Sin él, `dotnet test` a secas arrastra `VerifyCapacity`, cuyo *timeout* es de **3 horas** y que sobre una
  partición de decenas de GB los consume de verdad. Con el filtro: **1 m 53 s** (2026-08-17, USB de dos
  particiones). El «`dotnet test` a secas» no es la forma neutra de correr esta suite: es la forma lenta.
- **Recuerda compilar el proyecto de UI aparte** (`dotnet build tests\FormatDiskPro.UiTests\...csproj`): no
  está en el `.slnx`, así que `--no-build` tras un `dotnet build FormatDiskPro.slnx` ejecuta el **DLL
  anterior**. Ver §4.

## 3. Estado actual

| | |
|---|---|
| Build | 0 advertencias / 0 errores |
| Unitarias | **607 / 607** (433 + 20 del arreglo de *FAT32 pequeña* + 40 `T5-01` + 16 `T5-02` + 12 `T5-03` + 5 de la barra de ocupación + 1 de `T6-01` + 9 de `T6-03` + 11 de `T6-04` + 11 de `T6-05` + 3 de `T6-06` + 1 de `T6-09` + 9 de `T6-13` + 3 de `T6-15` + 7 de `T6-12` + 3 de `T7-01`/`T7-03`/`T7-05` + 6 de `T7-08` + 2 de `T7-09` + 7 de `T8-02` + 1 de `T8-03` + 3 de `T8-05`) · se ejecutan **en local**, nunca en CI (ver §4) |
| UI tests | **38** en total (+1 de `T6-01`, +1 de `T6-02`, +1 de `T7-04`, +1 de `T7-02`, +5 de `T7-06`/`T7-07`, +1 de `T8-01`, −2 las dos sondas borradas) · con la USB (`utilidades`) y `--filter "Category!=Slow"`: **26 pasan / 3 se omiten / 0 fallan** en **1 m 47 s** (2026-08-17, antes del Tier 7) · las 3 omitidas son de opt-in (2 `ALLOW_YANK` + 1 `ALLOW_DESTRUCTIVE`), no falta de hardware · **sin** la USB: 19 pasan / 10 se omiten (con alguna unidad no-sistema conectada; el 2026-08-26, sin ninguna y ya con el Tier 7 y el Tier 8, fueron **27 pasan / 11 se omiten / 0 fallan** en 16 s — los cuatro `[NonSystemDriveFact]` de `FormatOptionsUiTests` también se omiten) · el corte usa ese mismo filtro y **dice qué dejó fuera** |
| Instalador | Verificado por SHA-256 (hash emparejado con su instalador) y probado **end-to-end** (limpia + in-place) |
| Publicado | **v1.24.1** (2026-08-26) · `master` sin trabajo pendiente de publicar |
| Auditoría | 2026-08-13 — **CERRADA el 2026-08-16**: 39/40 completadas + 2 descartadas (`T2-10` CI, `T4-03` firma) · **0 abiertas** ([`ROADMAP.md`](ROADMAP.md) Parte 2) |
| Ocurrencias | **Tier 5 CERRADO (2026-08-16)**: `T5-01`, `T5-02`, `T5-03` y `T5-05` completadas · `T5-04` (N particiones) **descartada** por decisión de producto — el motor admite N, lo limitado es la interfaz |
| Tareas abiertas | **Ninguna.** El **Tier 8** cerró el **2026-08-26**, 5/5: salió de una captura del historial en uso —cuatro `EXPORT ERROR:` sin nada detrás— y encontró que ***Exportar CSV* nunca funcionó en ninguna versión publicada** (`T8-01`), que los errores podían salir vacíos (`T8-02`) y que otros dos botones podían no hacer nada (`T8-03`). El **Tier 7** cerró el mismo día, 9/9: `T7-08` era la comprobación a ojo que FlaUI no podía medir, y dio **no** —WinUI no pinta el tooltip de un control deshabilitado—, así que el motivo de `T7-02` bajó al texto visible del ítem — y mirar ese menú arreglado abrió `T7-09`, el marco de foco recortado en los seis diálogos. Antes, la revisión con la app en marcha (`T7-06`) desmintió la sospecha de partida —los `ListView` sí se recorren con teclado— y abrió `T7-07`. El **Tier 6** cerró el 2026-08-17, 15/15. Producto, auditoría y Tier 5: cerrados |

> **La tabla de tiers completados vivía aquí duplicada** de la del [`ROADMAP.md`](ROADMAP.md#-estado), y se
> quedó desactualizada por serlo. Se mantiene solo allí: los nueve tiers de producto (1.4.0 → 1.15.1), la
> auditoría de calidad y el Tier 5 de ocurrencias, cada uno con la versión en la que entró.

## 4. Decisiones y convenciones clave

> Casi todas nacieron de un fallo real. Antes de cambiar algo que parezca arbitrario, lee su porqué.

### Producto (no reabrir)

- **La app corre SIEMPRE elevada** (`requireAdministrator` en `app.manifest`) — **decisión firme (2026-07-13)**.
  Se evaluó el modelo `asInvoker` + worker elevado por named pipe (el de WingetUSoft) y **se descartó**: esta
  app formatea, borra y reinicializa discos, así que **casi todo lo que hace necesita administrador**. El
  "menor privilegio" sería nominal (pediría UAC igual, solo más tarde y más veces) a cambio de refactorizar
  **todos** los `Services`, que asumen proceso elevado. Consecuencia asumida: los UI tests y
  `tools/capture-screenshots.ps1` **exigen terminal elevada**, y ambos lo validan con un mensaje claro.
- **La ventana es de tamaño FIJO (500×900)** — **decisión firme (2026-07-13)**. Es un **diálogo de tarea**, no
  un espacio de trabajo: ningún contenido gana con más ancho y el layout de tarjetas ya cabe entero. No portar
  `WindowSizing`/`ContentScroller` de WingetUSoft: allí la ventana lista paquetes en una tabla y lo
  necesitaba; aquí resolvería un problema que no existe.
- **El testing de este proyecto es LOCAL: no hay CI, ni GitHub Actions, ni workflows — firme (2026-08-15).**
  La auditoría propuso un CI de solo unitarias (`T2-10`); se implementó, se revirtió y la tarea queda
  **descartada**. El motivo no es el coste: en esta app la prueba que vale es la que **ejerce el binario
  real** contra hardware real (elevación + USB de pruebas), y eso **no cabe en un runner hospedado**. Un ✅
  verde que solo cubre los unitarios afirma más de lo que prueba — exactamente el problema que `T2-12`
  acaba de corregir en el otro extremo del proceso. La puerta de calidad es
  **`release.ps1 -UiTests` desde una terminal elevada**, y esa puerta ya existe. Consecuencia asumida: un
  PR externo no ejecuta nada hasta que el mantenedor lo corre en su máquina.
- **Protección de unidades:** SOLO se protege el **disco de sistema** (`IsSystemDrive()`). El resto
  —removibles, discos de datos fijos, RAM— **sí** se pueden formatear.
- **No se firma el instalador** (#13, 2026-06-24; **reafirmado el 2026-08-16 al descartar `T4-03`**):
  SmartScreen dirá "editor desconocido". La firma sigue disponible como **opción** del pipeline
  (`-CertThumbprint`/`-CertFile`/`-CertPassword`/`-TimestampUrl`). Es lo que hace **necesaria** la
  verificación por SHA-256.
  La auditoría había reabierto esto sin querer, listando «firmar el instalador» como tarea pendiente:
  eso **afirmaba que el proyecto debía firmar y aún no lo había hecho**, cuando lo cierto es que decidió
  no firmar y construyó la verificación por hash justamente por eso. Lo que falta es un **certificado**,
  que es una compra y no ingeniería, así que no pertenece a un backlog técnico.
  **El día que lo haya, el trabajo no es «firmar»**: es poner `UpdateService.SignsItsInstallers` en `true`
  **y** fijar el publicador esperado — lo primero sin lo segundo reabre el agujero de `T1-08`—, y esa
  condición ya la vigila un test tripwire que falla si se hace a medias.

### Seguridad

- **PowerShell vía `-EncodedCommand`** (Base64 UTF-16LE), nunca por concatenación. Validar
  `char.IsLetter(letter)` antes de interpolar. Etiqueta escapada (`'`→`''`) en `Format-Volume`; para
  `format.com`, `ArgumentList` (escape por argumento).
- **Verificación del instalador (desde v1.15.0, #38) — NO ROMPER.** El instalador se ejecuta **elevado**, así
  que antes se comprueba que es el del proyecto mediante el **SHA-256** contra el asset `*.exe.sha256`.
  Sin él, **se borra y no se ejecuta**. Consecuencias operativas:
  - **La firma Authenticode NO es un atajo mientras el proyecto no firme (`T1-08`, 2026-08-13).** Hasta la
    v1.16.0, una firma válida devolvía sin mirar el hash. Pero `WinVerifyTrust` responde a «¿lo firmó
    *alguien* de confianza?», no a «¿lo firmamos *nosotros*?», y no hay publicador que fijar porque no se
    firma (#13). Es decir: esa rama **solo podía activarse sobre un binario que no produjimos**, y convertía
    cualquier ejecutable firmado por cualquier CA en un modo de saltarse el hash — con `LaunchInstaller`
    ejecutando como administrador al otro lado. Queda tras `UpdateService.SignsItsInstallers` (`false`).
    **El día que haya certificado hay que poner el flag *y* fijar el publicador esperado**: lo primero sin
    lo segundo reabre el agujero, y hay un test tripwire que falla si se hace a medias.
  - **El `.sha256` se empareja por NOMBRE con el instalador elegido** (`T2-06`, 2026-08-15): se busca
    exactamente `<nombre-del-exe>.sha256`. Antes se usaba el último asset terminado en `.sha256` que
    apareciera en el JSON, así que bastaba con que el release llevara otro archivo con checksum —un
    portable, un adjunto— para verificar el instalador contra el hash de otra cosa y rechazar la
    actualización buena. Si el hash del instalador elegido no está, `ChecksumUrl` queda vacía y la
    actualización se rechaza: **el fallo seguro es no ejecutar**.
  - **El checksum se lee con tope de 512 bytes** (`T2-07`, 2026-08-15), comprobando el `Content-Length`
    declarado **y** lo que realmente llega. La URL sale del JSON del release: no puede decidir cuánta
    memoria se materializa. Motivo propio para el usuario: `update.checksumUnreadable`.
  - **Todo release debe subir su `.sha256`** o la auto-actualización lo rechazará. `build-installer.ps1` lo
    genera (**después** de firmar, si se firma: firmar cambia el binario) y `release.ps1` lo sube como segundo
    asset y **aborta si falta**.
  - **La descarga vive en su propio método** (`DownloadToFileAsync`) **a propósito**: su `FileStream` es
    `FileShare.None` y debe cerrarse **antes** de verificar. Si se fusiona con la verificación, esta no podrá
    ni abrir el archivo ("lo está usando otro proceso" — el proceso es la propia app) y la actualización
    fallará **siempre**. Hay test que lo caza.
  - **`HttpCompletionOption.ResponseHeadersRead` no es decorativo:** deja el cuerpo fuera del `Timeout` de 30 s
    del `HttpClient`. Con `ResponseContentRead`, un instalador de ~60 MB fallaría en toda conexión por debajo
    de 2 MB/s.
  - **Alcance honesto:** el `.exe` y su hash salen del mismo release → detecta corrupción y manipulación **en
    tránsito**, no un compromiso de la cuenta de GitHub.
- **Exportación CSV:** además del escape RFC 4180, se **neutralizan las fórmulas** (`=`/`+`/`-`/`@` → prefijo
  `'`). Escapar comillas protege la *estructura* del CSV, no al programa que lo abre.

### Build y publicación

- **Publicación self-contained** (`WindowsAppSDKSelfContained=true`): el usuario final no instala .NET.
- **`Microsoft.WindowsAppSDK` con versión EXACTA (`1.8.260529003`) — no volver a `1.8.*`.** Con comodín, NuGet
  resuelve el paquete más nuevo y **el conjunto de archivos publicados cambia solo**, sin tocar el repo. Así
  apareció, de un día para otro, el archivo que rompió el build (ver MAX_PATH). Subir de versión debe ser
  **deliberado y probado**, no un efecto colateral de la fecha en que se compile.
- **El instalador se publica a `%TEMP%`, no dentro del repo — MAX_PATH.** Inno Setup no usa las APIs de rutas
  largas, y el publish del Windows App SDK trae nombres de hasta 76 caracteres
  (`WindowsAppSdk.AppxDeploymentExtensions.Desktop-EventLog-Instrumentation.dll`). Sumados a la ruta del repo
  pasan de 260 en cuanto el checkout no cuelga de una carpeta corta, e ISCC aborta con «El sistema no puede
  encontrar la ruta especificada» **sin decir cuál**.
- **Instalador (Inno Setup):** `AppId = {CEC07916-C9B5-4EA8-9102-3273384395AD}` — **no cambiar nunca**
  (permite la actualización in-place). `PrivilegesRequired=admin`, `CloseApplications=yes`.
- **Versionado:** fuente única en el `<Version>` del `.csproj`. El updater lo compara con el `tag_name` del
  último release.
- **Workaround del PRI:** `dotnet publish` de una app WinUI 3 *unpackaged* **no copia el `.pri` propio** de la
  app; sin él, WinUI no resuelve el XAML y la app **crashea al iniciar** (fue el bug de la 1.2.0). El target
  `CopyAppPriToPublish` del `.csproj` lo copia a mano. No quitarlo.

- **`FormatDiskPro.slnx` NO incluye `tests/FormatDiskPro.UiTests`, y eso es deliberado.** `release.ps1`
  hace `dotnet test $solution --collect:"XPlat Code Coverage"`; meter ahí las pruebas de UI las ejecutaría
  dentro del paso de cobertura de cada corte, que es justo lo que no debe pasar (necesitan hardware,
  terminal elevada y opt-in). **Consecuencia, y es una trampa real:** `dotnet build FormatDiskPro.slnx`
  **no compila las pruebas de UI**, así que un error de compilación en ellas no aparece. Combinado con
  `dotnet test --no-build`, se ejecuta la **DLL vieja** y el resultado sale en verde sin haber probado lo
  nuevo — ocurrió el 2026-08-16 con `T5-05`. Al tocar ese proyecto: `dotnet build
  tests/FormatDiskPro.UiTests/FormatDiskPro.UiTests.csproj` **explícitamente**, o `dotnet test` sobre su
  `.csproj` **sin** `--no-build`.

### Trampas de PowerShell 5.1 (las tres nacieron de un fallo real)

- **Los scripts van con BOM UTF-8.** Sin él, los acentos rompen el parser.
- **El `.csproj` también va con BOM, y NO se lee con `Get-Content -Raw`.** Sin BOM, PS 5.1 lo lee con la página
  de códigos ANSI: los bytes UTF-8 de `é` se vuelven `Ã©` y, al reescribirlo, la corrupción **queda grabada**.
  Como el bump de versión ocurre en **cada** release, el daño se acumulaba capa sobre capa: tras 14 versiones
  el nombre del autor de `<Authors>`/`<Copyright>` estaba destrozado **en las propiedades del `.exe`
  publicado**. `release.ps1` usa `[System.IO.File]::ReadAllText` (detecta el BOM) y reescribe
  **conservándolo**. Cualquier script que toque el `.csproj` debe hacer lo mismo.
- **git + salida capturada = trampa.** git escribe por stderr en su operación **normal** (el resumen del
  `push`, los avisos de CRLF), sin que nada falle. Si la salida del script **se captura** (`| Tee-Object`,
  `2>&1 |`, un wrapper), PS 5.1 convierte cada línea de stderr de un exe nativo en `NativeCommandError` y, con
  `$ErrorActionPreference = "Stop"`, **aborta aunque git devuelva 0**. En un `push` eso deja el release **a
  medias**: rama subida, sin tag ni GitHub Release. Por eso los git que mutan estado van por **`Invoke-Git`**,
  que baja la preferencia mientras corre git y decide por `$LASTEXITCODE`.

### Otros

- **`gh` (GitHub CLI):** si no está autenticado, los scripts reutilizan la credencial de git cacheada
  (`git credential fill` → `GH_TOKEN`), solo en local, sin imprimir el token.
- **Framework de pruebas: xUnit.** Hay skills de mstest/nunit/tunit en `.agents/skills/`, pero **no se usan**.
- **Los colores SEMÁNTICOS viven todos en `Core/SeverityPalette`, y están medidos** (desde `T1-04`,
  2026-08-13). Salud S.M.A.R.T., resultado del historial, ocupación, unidad protegida y texto primario: uno
  por tema, en un solo sitio. `SeverityPaletteTests` recorre **`SeverityPalette.All()`** —el inventario, no
  una función concreta— y exige a cada color su umbral WCAG (4.5:1 texto, 3:1 objeto gráfico), componiendo
  antes el alfa sobre el fondo. **Añadir un color al inventario es ponerlo bajo test: no hay forma de hacer
  una cosa sin la otra.** Antes el barrido solo recorría `For(SmartLevel)` mientras los mismos RGB estaban
  copiados en `HistoryDialog` y `MainWindow`, y por ahí entró un gris de 3.52:1 sin romper el build.
  **Única excepción fuera de `Core`:** `MainWindow.UpdateCaptionButtonColors` (cromo de ventana, superpuesto
  sobre Mica/Acrylic: no hay fondo fijo contra el que medir).
- **Un color se mide contra lo que hay que distinguirlo, no siempre contra el fondo** (desde 2026-08-16).
  `PaletteColor.Against` permite declarar un color **adyacente** como referencia. Lo usa la pista de la
  barra de ocupación: contra la tarjeta no llega al 3:1 *a propósito* —es un hueco, no una segunda barra—,
  y lo que el usuario tiene que separar ahí es *usado* de *libre*. Entra al inventario con una entrada por
  cada relleno con el que puede compartir barra. Si alguien le quita el `Against` «para medirla como las
  demás», el barrido la suspende y la reacción natural sería oscurecerla hasta que compita con el relleno:
  hay un test que fija que esas entradas declaren su vecino.
- **La barra de capacidad NO usa el color de acento** (desde el pase de UX del 2026-07-20). Un `ProgressBar`
  por defecto hereda el acento del sistema; en un equipo con **acento rojo** la barra de ocupación se veía
  roja con el disco medio vacío y leía como *alarma*. Ahora codifica ocupación, no marca: neutro <80 %,
  ámbar ≥80 %, rojo ≥90 % (`MainWindow.CapacityBrush`, con `SeverityPalette.NeutralFill` para el neutro).
  Desde 2026-08-16 **tampoco es un `ProgressBar`**: su plantilla fija la pista en 1 px, así que usado y
  libre no podían tener el mismo grosor. Es un `Border` (libre) con un `Border` hijo (usado) y columnas
  estrella — ver el registro de cambios de esa fecha.
- **Capturas: fotografía el PUBLISH self-contained, no el `dotnet build`.** En esta máquina el apphost de un
  `dotnet build -c Release` (runtime .NET *framework-dependent*) **no arranca**: muestra "You must install or
  update .NET". `tools/capture-screenshots.ps1` prefiere `bin\Release`, así que tras un build plano capturaba
  el **diálogo de error** en vez de la app. Publica primero como `build-installer.ps1`
  (`dotnet publish -r win-x64 --self-contained true` a `%TEMP%\FormatDiskPro-publish`) y pásalo con
  `-Exe <publish>\FormatDiskPro.exe`. Es además la foto correcta: el publish es lo que se distribuye.

## 5. Tareas comunes

| Tarea | Comando |
|-------|---------|
| Compilar | `dotnet build -c Release` |
| Pruebas unitarias | `dotnet test` |
| UI tests (app real, **terminal elevada**) | `dotnet test tests\FormatDiskPro.UiTests --filter "Category!=Slow"` |
| Regenerar capturas del README (**terminal elevada**) | `.\tools\capture-screenshots.ps1` |
| Generar instalador | `src\FormatDiskPro\installer\build-installer.ps1` |
| **Publicar versión** | `.\release.ps1 -Version X.Y.Z -UiTests` (`-DryRun` para simular) |

`release.ps1` hace: validar → tests (unitarias + UI si `-UiTests`) → bump `<Version>` → build del instalador →
commit + tag `vX.Y.Z` → push → `gh release create` con el instalador **y su `.sha256`**.

> **Corte recomendado:** `.\release.ps1 -Version X.Y.Z -UiTests` desde una **terminal elevada**. Con el flag,
> el release **no sale si la app real falla**. `release.ps1` **aborta** si encuentra
> `FORMATDISKPRO_ALLOW_DESTRUCTIVE=1` activa: un corte jamás debe formatear una unidad.
>
> Solo hace `git add -u`, así que **los archivos nuevos hay que `git add`earlos antes**.
>
> **Y aborta si [`CHANGELOG.md`](CHANGELOG.md) no tiene la sección de la versión** que se va a publicar
> (`T4-01`): mueve antes lo que haya bajo *Sin publicar* a `## [X.Y.Z] — fecha` y añade su enlace abajo.

## 6. Qué queda fuera, y por qué

**No queda ninguna tarea abierta** —el estado vivo está en §3, esta sección es solo el alcance—. Los tres frentes del proyecto están cerrados:
producto (Tiers 1–9, 2026-07-13), auditoría de calidad (2026-08-16) y Tier 5 «Ocurrencias» (2026-08-16).

Lo que falta, falta **a propósito**. Las decisiones y su porqué viven en §4 y en *Decisiones cerradas* del
[`ROADMAP.md`](ROADMAP.md); en resumen: la app corre **siempre elevada** (`asInvoker` descartado), la
ventana es de **tamaño fijo**, no se **firma** el instalador (`#13`/`T4-03` — de ahí que el `.sha256` sea
obligatorio), no hay **CI** (`T2-10`: las pruebas son locales) y *Reinicializar* **no es un gestor de
particiones** (`T5-04`: crea layouts sobre un disco que se está borrando entero; nunca redimensiona, fusiona
ni mueve datos).

## 7. Cómo mantener este documento

1. Tras un cambio relevante, añadir una entrada en el **Registro de cambios** (fecha absoluta).
2. Actualizar el **Estado actual** (§3) y, si cambia una convención o decisión, el **§4**.
3. Commitearlo **junto con el cambio**, para que el contexto viaje con el código.

---

## Registro de cambios

### Índice de versiones

> El índice por versión vive ahora también, y con más detalle, en [`CHANGELOG.md`](CHANGELOG.md)
> (`T4-01`). Esta tabla se conserva porque es la entrada al registro **razonado** de abajo: allí está el
> *qué*, aquí empieza el *por qué*.

| Versión | Qué trajo |
|---|---|
| **1.24.1** | **La v1.24.0 se publicó bien y se contó mal.** Su página de descarga salió con un texto genérico —`release.ps1` no leía el CHANGELOG sin `-NotesFile`— y la pantalla de *Novedades* enseñaba las almohadillas del Markdown, porque el cuerpo del release empezaba por una marca de orden de bytes que dejaba el `#` sin estar al principio de su línea. Arreglado en los dos sitios: el script toma las notas de la sección obligatoria del CHANGELOG y las escribe sin BOM, y el conversor quita esa marca venga de donde venga. |
| **1.24.0** | **Tiers 7 y 8.** *Exportar CSV* **nunca funcionó en una versión publicada**: el selector de archivos de WinRT rechaza a los procesos elevados y la app siempre lo es, así que fallaba sin abrir ninguna ventana — y lo tapaba un segundo fallo, el mensaje de error llegaba vacío. Detrás cayeron dos `catch` vacíos más (*Abrir archivo* y los enlaces), el motivo de un ítem de menú apagado —que solo recibía un lector de pantalla, porque WinUI no pinta el tooltip de un control deshabilitado— y el marco de foco recortado en los seis diálogos. +19 pruebas (585 → 604). |
| **1.23.0** | **Tier 6 cerrado: refinado de UX/UI.** Tres cosas que la interfaz afirmaba y no eran ciertas —«Reinicializar unidad» confirmándose bajo el título «Confirmar formato», el campo de confirmación regalando la letra a teclear (y cantándola un lector de pantalla), y una velocidad de rotación cuyo valor era «SSD»— más los sitios donde el dato salía en crudo: bytes en el historial, horas de encendido sin equivalencia, Markdown a la vista en *Novedades* y textos legales que no cabían. Los números pasan a seguir al idioma de la app, no al de Windows. +64 pruebas (521 → 585). |
| **1.22.0** | **Tier 5 completo.** El espacio sobrante de *FAT32 pequeña* deja de morir sin asignar: se puede crear una segunda partición (exFAT/NTFS) en la misma operación, con la FAT32 siempre primera. La opción aparece por fin en unidades de menos de 32 GB, donde llevaba escondida desde la 1.14.0 — y con ella se arreglaron un selector que ofrecía tamaños que no caben y un tope medido sobre el volumen en vez del disco. El fallo a mitad ya informa de qué particiones quedaron, sin revertir nada. +88 pruebas (433 → 521). |
| **1.21.0** | **Auditoría cerrada.** Corte de **mantenimiento**: la app se comporta igual que la 1.20.0. `Services` inyectables con raíz de composición y costura `IProcessRunner` (+35 pruebas de caminos de error, ninguna toca un disco), `CHANGELOG.md` con puerta en el corte, README con 12 capturas, y fuera el último resto de Windows Forms. `T4-03` (firmar) descartada: contradecía `#13`. |
| **1.20.0** | **Tier 3 cerrado.** Pulido de lo que fallaba en silencio: la exportación CSV del historial informa del error real, la salud ilegible se muestra como «no disponible», el borrado seguro usa RNG criptográfico, las preferencias se normalizan al cargarlas, los iconos decorativos salen del árbol de accesibilidad y un marcador mal escrito en una traducción ya no tumba una pantalla. |
| **1.19.0** | **Tier 2 cerrado.** *Verificar capacidad* deja de poder leer de la caché del sistema (se acabaron los falsos OK en unidades pequeñas). Cobertura de `Core/` medida (97 %) y exigida en el corte; `MainWindow` repartido (2107 → 753 líneas) sin cambiar comportamiento; `SECURITY.md` y `CONTRIBUTING.md`. |
| **1.18.0** | Tercera tanda de la auditoría: las operaciones se pueden seguir con un lector de pantalla (región activa + notificación en los hitos), el error de etiqueta se lee desde su campo, el `.sha256` se empareja con su instalador y se lee acotado, `history.log` rota. El corte declara qué cobertura de UI no ejerció. |
| **1.17.0** | Segunda tanda de la auditoría: el formato completo dejaba colgada la app en un Windows que no fuera ES/EN; barra de progreso del formato en 6 idiomas; presets integrados traducidos; el historial deja de partirse al registrar un error multilínea; la firma Authenticode deja de eximir del SHA-256. Tiers 0 y 1 cerrados (16/40). |
| **1.16.0** | Robustez y accesibilidad: la app deja de cerrarse ante fallos de E/S, guarda de disco de sistema invariante de cultura, contraste AA en el historial, descripciones de FS en los 5 idiomas. Primera tanda de la auditoría (8/37). |
| **1.15.2** | Pase de refinamiento UX/UI: fix truncación del botón chkdsk, barra de capacidad semántica, etiqueta S.M.A.R.T. más clara. Modo galería de capturas. |
| **1.15.1** | Tier 9: UI tests en el pipeline, instalador probado end-to-end, build reproducible. Fix: metadatos del `.exe` corrompidos. Capturas en el README. |
| **1.15.0** | Tier 8 (seguridad): verificación del instalador (SHA-256), anti CSV injection, contraste WCAG AA. |
| **1.14.1** | Mantenimiento de las pruebas de UI. |
| **1.14.0** | Tier 7 (#37): partición FAT32 pequeña al reinicializar discos grandes, tamaño seleccionable. |
| **1.13.0** | Tier 6: pulido UX/UI (#28–#36). |
| **1.12.0** | Tier 5: relicencia a **GPLv3**, legal in-app, privacidad, donaciones (#23–#27). |
| **1.11.0** | Tier 4 (trabajo medio): umbrales S.M.A.R.T., historial filtrable/CSV, editar presets, a11y, autorefresco. |
| **1.10.1** | Fix de adaptación a DPI/escalado. |
| **1.10.0** | Tier 4 (quick wins): IOPS, pasadas configurables, idioma automático, changelog en el aviso. |
| **1.9.1** | Mantenimiento: correcciones de una revisión de código. |
| **1.9.0** | Benchmark refinado a perfil CrystalDiskMark (SEQ Q8 + RND4K, sin caché, mediana). |
| **1.8.0** | Tier 3: presets personalizados, 5 idiomas (PT/FR/IT), aviso al terminar. |
| **1.7.1** | Fix: el diálogo de novedades no aparecía al actualizar desde una versión sin `LastVersionSeen`. |
| **1.7.0** | Tier 2 completado: reinicializar unidad (#8) + benchmark (#9) + diálogo de novedades. |
| **1.6.0** | chkdsk (#6) + protección de escritura (#7). |
| **1.5.0** | S.M.A.R.T. ampliado (#5). |
| **1.4.0** | Tier 1: persistencia, ETA/velocidad, borrado seguro con progreso, historial. |
| **1.3.0** | Rediseño UI/UX inspirado en Win11Debloat (tarjetas + acento del sistema). |
| **1.2.2** | Fix: el cierre para auto-actualizar quedaba bloqueado por `_isBusy`. **La auto-actualización silenciosa funciona desde aquí.** |
| **1.2.1** | Fix crítico: la 1.2.0 crasheaba al iniciar (faltaba el `.pri` en el publish). |
| **1.2.0** | Migración de Windows Forms a **WinUI 3**. *(Obsoleta/rota: no usar.)* |
| **1.1.0** | Arquitectura por capas, hardening, tests, actualizaciones e instalador. |

---

### 2026-08-26 — `T8-04` y `T8-05`: el corte salió en verde y publicó unas notas vacías

**La v1.24.0 se publicó bien y contó mal.** El corte terminó impecable —604 pruebas, 98,1 % de cobertura,
34 UI tests sobre la app real, instalador y `.sha256` subidos— y el cuerpo del release era una **plantilla
genérica**: «Instalador self-contained para Windows x64…» y nada de lo que traía la versión. Se lanzó
`release.ps1` sin `-NotesFile`, y sin ese parámetro el script no leía el CHANGELOG: se inventaba un texto.

Es la peor forma de fallo para un script de publicación, porque **no avisa**. La sección del CHANGELOG es
obligatoria —el script aborta si falta— así que estaba escrita, revisada y a mano; simplemente no se usaba.
Ahora es de ahí de donde salen las notas cuando no se pasa `-NotesFile`, que sigue mandando cuando se pasa:
un registro por versión y unas notas de publicación pueden querer contar lo mismo de otra forma. Y el plan
del `-DryRun` dice de dónde van a salir, que es justo donde se habría visto antes de publicar.

**`T8-05` — y la plantilla arrastraba un carácter invisible.** La pantalla de *Novedades* mostraba
«`## FormatDiskPro v1.24.0`» con las almohadillas a la vista, aunque `ToPlainText` quita encabezados desde
siempre. El cuerpo del release empezaba por una **marca de orden de bytes** (`U+FEFF`): `Out-File
-Encoding utf8` de PowerShell 5.1 la escribe, y viaja intacta hasta la API de GitHub. Con ella delante, el
`#` ya no estaba al principio de su línea y la expresión regular no lo veía.

Lo traicionero es que **`U+FEFF` no es espacio en blanco para .NET** —categoría `Cf`, no `Zs`—, así que ni
`\s` ni `Trim()` lo tocan: hay que nombrarlo. Se quita antes de cualquier otra cosa, y se quitan todas y no
solo la primera, porque al pegar notas de varias fuentes puede aparecer a mitad del texto. El origen queda
arreglado en `T8-04` (ya escribe sin BOM), pero el conversor no puede fiarse de eso: el cuerpo de un
release puede venir de cualquier editor.

**Verificado por reversión:** quitando el reemplazo caen las tres pruebas nuevas, y con él vuelven a verde.
El release de la v1.24.0 se reeditó con las notas de verdad, así que la pantalla de *Novedades* de la
versión ya publicada también se ve bien: el texto lo pide a la API cada vez, no lo lleva dentro.

**Lo que este par deja escrito:** el Tier 8 salió de mirar la app en uso, y estos dos salieron de mirar
**el resultado de publicarla**. Un corte en verde dice que el código está bien, no que lo publicado esté
completo.

### 2026-08-26 — Tier 8: lo que solo se ve usando la app (`T8-01` a `T8-03`)

**Una captura encontró lo que dos revisiones enteras no.** El Tier 6 miró capturas de la galería y el
Tier 7 miró el código; este tier salió de una **captura de la app en uso real**, con el historial lleno,
enviada de pasada para enseñar otra cosa. En ella había cuatro entradas seguidas que decían
`EXPORT ERROR:` y nada más.

**`T8-01` — *Exportar CSV* nunca funcionó en ninguna versión publicada.** El
`FileSavePicker` de WinRT delega en un intermediario que **rechaza a los procesos elevados**, y
FormatDiskPro corre siempre elevada (`requireAdministrator`, decisión cerrada). `PickSaveFileAsync`
lanzaba `COMException 0x80004005` **en el acto**, sin llegar a mostrar ninguna ventana: para el usuario,
un botón que no hacía nada. Los tres primeros fallos del historial están separados por **3 y 4 segundos**
—demasiado poco para elegir un nombre de archivo—, que fue la primera señal de que el diálogo ni se abría.

**Medido, no supuesto.** Una sonda de UI pulsó el botón contra el .exe real y enumeró las ventanas del
proceso: ninguna nueva. Con el arreglo, la misma sonda ve aparecer la ventana `Exportar CSV` de clase
`#32770` —la de los diálogos comunes de Windows—. Las ventanas se enumeran por `EnumWindows` y **no** por
UI Automation: un modal del sistema bloquea el hilo de UI de la app y toda consulta UIA caduca con
«Operation timed out», que es un síntoma del bloqueo y no una medida. Tomarlo por resultado habría sido el
error que `T6-11` existe para no repetir. La sonda se convirtió en prueba de regresión y se borró.

**El arreglo es el diálogo de Windows por COM** (`IFileSaveDialog`), que lo crea el propio proceso y por
tanto la elevación le da igual — y es el mismo diálogo moderno del resto del sistema, no el
`GetSaveFileName` de los noventa. De la interfaz se declaran los métodos **en orden de vtable** hasta el
último que se usa: en COM el orden ES el contrato. La escritura pasa a `File.WriteAllTextAsync` con
**UTF-8 con BOM**, que es lo que hacía `FileIO.WriteTextAsync`: sin BOM, Excel abre el CSV en la página de
códigos del sistema y destroza los acentos.

**Por qué viajó en todas las versiones:** no había **ninguna** prueba de la exportación. La había de
`HistoryEntry.ToCsv` —la parte pura, con su escape RFC 4180 y su defensa contra fórmulas— pero ninguna del
camino que el usuario pulsa. La lección no es «faltaba un test», es que la cobertura estaba donde era
fácil ponerla.

**`T8-02` — y el error que lo tapaba.** Las cuatro líneas del historial no estaban truncadas: la `Message`
de esa excepción era **de verdad la cadena vacía**. Una excepción que cruza la frontera de WinRT lleva su
texto en un `IRestrictedErrorInfo`, y cuando ese descriptor viene sin descripción —lo habitual en los
fallos de COM— lo que llega a .NET es un mensaje en blanco. El `InfoBar` mostraba título sin cuerpo y el
historial registraba que algo falló sin decir qué. `ErrorText.Describe(ex)` respalda con el tipo y el
`HRESULT`, y **fue lo que diagnosticó `T8-01`**: con el respaldo puesto, la app dijo
`COMException (HRESULT 0x80004005)` en pantalla y eso señaló directamente al selector.

Lo usan los once sitios que enseñan o registran un error, incluida la línea de un formateo fallido
(`OperationFailure`), que es la más importante del archivo. Y una prueba **barre las fuentes** y falla si
vuelve a aparecer el mensaje en crudo fuera de `ErrorText`: no es una regla de estilo, cada uno de esos
sitios podía escribir un error vacío.

**`T8-03` — dos botones más que podían no hacer nada.** Buscando la misma familia de fallo aparecieron dos
`catch` vacíos: `History.Open()` (*Abrir archivo*) y `UpdateService.OpenUrl()` (*Apoyar el proyecto*,
*GitHub*, *Ver en GitHub*). Sin editor asociado a `.log` o sin navegador, el botón no producía **ningún**
efecto visible. Ahora `Open()` deja salir la excepción y el diálogo la cuenta en la `InfoBar` que ya
tenía, y `OpenUrl()` devuelve `bool` para que quien llama enseñe la dirección. Detalle que casi se cuela:
*Ver en GitHub* debe **seguir cerrando** el diálogo cuando el navegador sí abre —es lo que se espera de
ese botón—, así que solo se queda abierto cuando hay algo que contar.

**Lo que este tier deja escrito:** las tres revisiones de UI han encontrado cosas distintas porque miraron
fuentes distintas —capturas de galería, código, y la app en uso—. La tercera es la única que encontró un
defecto de corrección, y encontró el mayor de los tres.

### 2026-08-26 — `T7-08` y `T7-09`: se cierra el Tier 7 (9/9)

**La respuesta era «no», y la propuesta de la tarea también.** Con el disco de sistema seleccionado y el
ratón sobre *Reinicializar unidad…*, **no aparece nada**: WinUI no tiene el `ShowOnDisabled` de WPF, y un
control deshabilitado no recibe eventos de puntero, así que su tooltip no se pinta jamás. El motivo que
`T7-02` había escrito llegaba solo por `HelpText`, es decir, **solo a un lector de pantalla**. Un ítem gris
y mudo para quien mira era exactamente lo que esa tarea se prohibió a sí misma.

**Pero la `InfoBar` que la tarea proponía era el sitio equivocado, y mirarlo lo dejó claro.** El flyout de
*Herramientas* se despliega **justo encima** de la fila donde vive `ProtectedBar`: el aviso habría quedado
tapado por el propio menú que lo motiva. Y hay un problema anterior al de la geometría: el motivo es **por
ítem** —protegida, no extraíble, sin unidad—, mientras que una barra es **por ventana**, y con tres ítems
apagados por razones distintas tendría que resumirlas sin poder decir a cuál corresponde cada una.

**Dónde fue, entonces: al texto del propio ítem.** «Reinicializar unidad… (unidad protegida)», «Expulsar
unidad (solo extraíbles)». Tres claves nuevas (`menu.tagNoDrive`, `menu.tagProtected`,
`menu.tagRemovable`) × 5 idiomas, cortas y entre paréntesis porque son un apéndice del nombre, no una
frase aparte. Las `menu.why*` de `T7-02` **se quedan tal cual**: la etiqueta cabe en un menú, la frase
completa dice el porqué, y cada una va donde sirve —la primera a la vista, la segunda al `HelpText`—. El
tooltip se conserva aunque hoy no se pinte: no cuesta nada y deja de ser deuda el día que la plataforma lo
muestre.

**El efecto colateral fue el hallazgo de verdad.** El texto de esos siete ítems tenía **dos dueños**:
`ApplyLanguage` lo escribía al cambiar de idioma y `UpdateToolsMenuAvailability` al cambiar de unidad.
Mientras el texto era constante, la duplicación era inocua; con la etiqueta dentro se vuelve un fallo
—según cuál corriera el último, la etiqueta se perdía o se acumulaba en cada repintado—. `ApplyLanguage`
deja de escribirlos (ya llamaba a `UpdateToolsMenuAvailability` al final, por los motivos de `T7-02`), y
el texto se re-deriva **siempre** de la clave de localización, nunca del que el ítem trae puesto.

**Verificado por reversión:** quitando la etiqueta del `Text`, la prueba de `T7-02` —ampliada con una
comprobación del texto **visible**, no solo del `HelpText`— falla; con ella, verde. La aserción es
independiente del idioma (el texto de un ítem apagado termina en `)`), porque el idioma activo en la
máquina de pruebas es el que el usuario tenga guardado. +6 unitarias sobre las etiquetas en los cinco
idiomas.

**`T7-09` — y mirar el resultado abrió otra cosa.** El mismo repaso con la app delante enseñó que, al
tabular hasta el primer filtro del *Historial*, su **marco de foco salía cortado por la izquierda**. La
causa no está en ese combo: WinUI dibuja el marco de foco **hacia fuera** de los límites del control —2 px
de trazo primario más 1 px de secundario— y el `ContentDialog` envuelve su contenido en un `ScrollViewer`
que recorta. Cualquier control pegado al borde de la raíz pierde el lado que cae fuera, y **los seis
diálogos** ponían su raíz pegada al borde. Al buscador y a la fila de botones les pasaba igual; solo se
notó en el combo porque su marco es el más visible.

Por eso el arreglo no vive en el historial sino en un recurso compartido —`DialogContentPadding` = 3 px,
que es exactamente lo que el trazo necesita—, aplicado a la raíz de los seis. Va **dentro** de
`MinWidth`/`MaxWidth`, así que no mueve el ancho de nadie ni toca lo que fijó `T6-07`. Esa tarea es
justamente el precedente: un criterio por diálogo acabó en seis criterios distintos y en una ventana que
saltaba al abrir el siguiente.

**La excepción va declarada, no borrada.** `LegalTextDialog` se queda sin relleno porque su ancho es el
valor **medido** en `T6-14` para que quepan las 78 columnas de la GPL sin barra horizontal: 6 px menos
volverían a partir el texto legal. Y no lo necesita — su raíz es un `ScrollViewer` que ocupa el diálogo
entero, sin ningún control tabulable pegado al borde. El nombre y el porqué están en la propia prueba, que
es donde hay que ir a discutirlos si algún día cambian.

**Qué prueba la prueba, y qué no.** Barre los `*Dialog.xaml` y falla nombrando el que se olvide del
relleno —verificado por reversión—, pero lo que defiende es **la convención, no los píxeles**: que el
recorte ya no se vea es una comprobación de ojo, igual que la que abrió `T7-08`. Un diálogo nuevo que se
salte el relleno recorta el foco exactamente igual, y nadie lo notaría hasta que alguien tabulara.

### 2026-08-25 — Tier 7: siete de ocho (`T7-01` a `T7-07`)

**Por qué hay otro tier de UI con el Tier 6 recién cerrado.** El Tier 6 nació de las **capturas** y cerró
la clase «la interfaz afirma algo que no es cierto». Esta revisión partió del **código** de `UI/` y buscó
lo que una galería no puede enseñar: qué ocurre **al pulsar**, qué se ofrece para negarse después, y qué
obliga a hacer a mano. Los seis hallazgos son de esa naturaleza —consistencia, prevención de errores y
eficiencia—, **ninguno es un defecto de corrección**. Que la segunda revisión encuentre cosas distintas a
la primera no es que la primera fuera mala: es que miraba otra superficie, igual que `T6-11` encontró tres
cosas que la ronda sobre capturas no podía ver.

**`T7-01` — el hallazgo era una comparación, no un juicio.** Borrar un preset hacía `Remove` + `Persist()`
en un clic, sin confirmar ni deshacer, con la papelera pegada a «Editar» en una fila de cuatro iconos.
Aisladamente se puede defender (no es dato del disco). Lo que no se puede defender es que en el **mismo
producto** *Vaciar historial* sí confirme: dos acciones igual de irreversibles pidiendo cosas distintas.
Por eso el arreglo no inventa un patrón — reusa el `Flyout` dentro del contenido que ya existía allí,
que a su vez existe porque **un `ContentDialog` no puede abrir otro**.

Dos detalles que salieron al hacerlo: la pregunta lleva **el nombre del preset dentro** (en una lista de
papeleras idénticas, «¿Eliminar?» no dice cuál se pierde), y el flyout hay que **capturarlo en `Opening`**
para poder cerrarlo: su contenido vive en un `Popup`, así que desde el botón que confirma no se sube hasta
él por el árbol visual. Solo puede haber uno abierto a la vez, de modo que un campo basta.

**`T7-03` — la ayuda estaba donde no hacía falta.** El sistema de archivos tiene descripción bajo su combo
desde `T1-05`; *Tamaño de unidad de asignación* —el campo que menos gente sabe elegir— no tenía ninguna. Al
redactarla apareció la trampa: el borrador decía «Predeterminado sirve para casi todo», pero **no existe
ninguna opción llamada así**: `UpdateAllocationUnits` puebla el combo con tamaños concretos (`4 KB`,
`64 KB`) y el recomendado llega *preseleccionado*. Habría sido justo el fallo del Tier 6 —la interfaz
afirmando algo que no es cierto— en una cadena nueva. La prueba barre los cinco idiomas y falla si alguna
traducción futura vuelve a inventarse la opción.

**`T7-05` — el caso intermedio.** El estado vacío del historial ya distinguía *sin historial* de *sin
coincidencias*; lo que no había forma de saber es cuándo **hay** resultados pero no todos. Ahora el
buscador es un `AutoSuggestBox` (por el botón de limpiar; sugerencias apagadas, no hay corpus que sugerir)
con **nombre accesible propio** —apoyarse en el placeholder como nombre es lo que falló en `T6-02`— y
debajo un «12 de 340» que se oculta con el historial vacío, porque ahí el estado vacío ya lo dice con
palabras. Los dos números se formatean con `L.Culture` **antes** de entrar en `L.T`: `string.Format` usa
`CurrentCulture`, es decir la de Windows, y por ahí volvía `T6-12`.

**`T7-04` — el atajo que había que probar, no razonar.** `Ctrl+I` (salud), `Ctrl+B` (benchmark), `Ctrl+H`
(historial) y `Ctrl+E` (exportar, dentro del historial). Formatear, reinicializar, verificar capacidad y
borrado seguro **no llevan atajo a propósito**: una combinación mal pulsada no puede ser el primer paso de
algo que borra datos, y la confirmación reforzada existe justamente para que llegar ahí cueste.

Lo que no era obvio: los `MenuFlyoutItem` de un `MenuBar` viven en un flyout que puede no haberse
desplegado **nunca**, así que un `KeyboardAccelerator` puesto ahí bien podía no registrarse. Hay un UI test
que pulsa `Ctrl+H` **sin abrir el menú** contra el `.exe` real, y está **verificado por reversión**:
quitando el acelerador, falla. Tampoco hizo falta `KeyboardAcceleratorTextOverride` —el ítem pinta solo el
texto cuando el acelerador existe de verdad; el *override* es para anunciar uno que no existe—, y el F5 sí
va escrito a mano en su tooltip porque con un `ToolTip` explícito WinUI ya no añade el suyo.

Y una consecuencia que hubo que atender: `MnuHistory_Click` **no comprobaba `_isBusy`**. No hacía falta
mientras el único camino fuera el menú, que se deshabilita entero durante una operación; con un atajo
global sí, y un `ContentDialog` modal encima de un formateo tapa el progreso y el botón de cancelar.
**Añadir un atajo no es solo añadir una tecla: es añadir un camino nuevo a un handler.**

**`T7-02` — apagar sin decir por qué habría sido peor.** *Herramientas* se ajusta ahora a la unidad
seleccionada, con las condiciones copiadas **una a una** de las guardas de `Operations.cs` — que **siguen
ahí**: entre abrir el menú y pulsar, la unidad puede cambiar (`WM_DEVICECHANGE` existe para eso). El menú
es la primera línea; aquellas son la red. *Comprobar errores* y *Benchmark* no se apagan nunca con una
unidad seleccionada: chkdsk en solo lectura sí corre sobre el disco de sistema —lo que no se ofrece allí es
la reparación— y el benchmark no escribe fuera de su archivo temporal.

El motivo va en el ítem (tooltip **y** `HelpText`) y se reescribe al cambiar de idioma, o se quedaría en el
anterior. **Lo que no está verificado, y va a `T7-06`:** el `HelpText` sí lo comprueba un test —es lo que
lee un lector de pantalla—, pero que WinUI **pinte** el tooltip sobre un `MenuFlyoutItem` deshabilitado no
se ha podido confirmar. Una sonda con FlaUI no encontró tooltip ninguno… tampoco sobre un control
habilitado que sí lo tiene, así que la sonda no prueba nada. Se deja escrito como pregunta abierta en vez
de como hecho, que es la diferencia entre `T6-11` y la ronda que la precedió.

**`T7-06` — la revisión con la app en marcha desmintió su propia sospecha.** La tarea salió de suponer que
un `ListView` con `SelectionMode="None"` no se podría recorrer con el teclado. **Se puede**: tres
tabulaciones desde el buscador caen en una fila del historial y ↓/AvPág la desplazan (0 % → 30 %). El foco
inicial es el correcto en los dos diálogos y el flyout de borrado de `T7-01` funciona a teclado —el foco
cae en el botón que confirma y el tab se queda dentro—. **No había nada que arreglar en lo que la tarea iba
a arreglar**, y eso también es un resultado: hay dos pruebas que lo fijan y que **siguen verdes al revertir
todos los arreglos de este tier**, porque no prueban código nuestro sino una promesa de la plataforma.

El método importa más que el resultado: se escribió una **sonda que no afirmaba nada** —recorría la app e
imprimía lo que veía— y solo con su salida delante se escribieron los asserts. Escribir primero los asserts
habría fijado la suposición falsa. La sonda se borró al convertirse en las cinco pruebas, que era su
condición de existencia: una prueba que no puede fallar es ruido verde.

**`T7-07` — y encontró lo que la lectura no podía ver.** Las filas de las dos listas se anunciaban con el
`ToString()` del record: «HistoryRow { Time = …, Glyph = , Accent = Microsoft.UI.Xaml.Media.SolidColorBrush }»
y «FormatPreset { Name = …, AllocationUnit = 4096, … }». Pasa porque el contenido del ítem es un **objeto**,
no texto. Y los dos `ComboBox` de filtro del historial no exponían nombre: «cuadro combinado», sin decir
qué filtran —el buscador de al lado sí lo tenía desde `T7-05`—.

El arreglo va en el **contenedor** (`ContainerContentChanging`), no en la plantilla: el nombre que se
anuncia es el del `ListViewItem`, y ponerlo dentro no lo toca. **Verificado por reversión, y la reversión
reparte**: sin los arreglos caen las tres pruebas de nombres y siguen verdes las dos de teclado. Que una
prueba siga verde al revertir es sospechoso salvo que se sepa **por qué** —aquí, porque mide la plataforma—.

**`T7-08` — lo único que queda, y no es código.** ¿Pinta WinUI el tooltip de un `MenuFlyoutItem`
deshabilitado? De eso depende que `T7-02` cumpla su propia condición. **FlaUI no puede contestarlo**: no
detecta el tooltip ni sobre un control habilitado que sí lo tiene, con el ratón encima confirmado por
`FromPoint`. Un cero ahí no distingue «no hay tooltip» de «no sé verlo», así que **no se afirma nada**: la
tarea es poner el ratón encima y mirar. Si no aparece, el motivo se lleva a la `InfoBar` de la ventana,
que ya hace eso mismo para la unidad protegida.

### 2026-08-17 — `T6-12` a `T6-15`: se cierra el Tier 6 (15/15)

**`T6-13` — *Novedades* enseñaba el Markdown.** Dos fallos en el mismo conversor: se quitaban `**` y `__`
pero no la cursiva de un asterisco, y se respetaban los saltos de línea del original —ajustado a ~100
columnas— que el diálogo volvía a ajustar por su cuenta, partiendo las frases dos veces. El «cuidado»
anotado en la tarea era real y marcó el diseño: las regex de énfasis exigen marcador **pareado y pegado a
un no-espacio** (y el subrayado, que no haya letra alrededor), así que `2 * 3 = 6` y `notas_de_version`
salen intactos; el desenvolvido va por bloques, de modo que una viñeta no se pega a la siguiente pero su
continuación ajustada sí se le une. Tres de las nueve pruebas nuevas guardan justo eso: lo que **no** debe
tocarse.

**`T6-14` — el primer intento fue peor que el problema.** Con `NoWrap` y el cuerpo a 11 px el texto legal
dejaba de partirse… y pasaba a salir **cortado** en el borde, sin barra visible. Cambié un ajuste feo por
una truncación silenciosa, y no lo vi razonando: lo vio la captura. La solución fue medir en vez de elegir
entre las tres opciones que proponía la tarea — `LICENSE` mide 78 columnas como mucho, y a 10 px de
Consolas entran ~78 en 430 px, que es todo lo que da una ventana de 500. Con eso la GPL entera, 674 líneas,
se lee sin tocar la barra horizontal. Es la **única excepción declarada** al ancho común de `T6-07`, y está
documentada en los dos sitios donde alguien podría intentar «arreglarla».

Las tres líneas que aún no cabían resultaron ser **nuestras**: `THIRD-PARTY-NOTICES.txt` es el documento de
atribución del proyecto, no texto ajeno, así que se reajustó a 78 columnas. El texto MIT que cita y la GPL
no se tocaron — eso era exactamente lo prohibido.

**`T6-15` — la prueba no ancla las cadenas.** Los resúmenes de reinicialización traían saltos puestos a mano
que peleaban con el ajuste del control, y quedaban partidos en un sitio distinto en cada idioma. Se podía
arreglar y anclar el resultado; en vez de eso, la prueba recorre **cada `\n` de los tres textos en los cinco
idiomas** y falla si no separa párrafos ni abre un elemento de la lista numerada. Así caza también el que se
cuele en una traducción futura, que es cuando esto vuelve.

**`T6-12` — el fallo estaba dentro de la propia suite.** Decidido: los números que se **muestran** siguen al
idioma elegido en la app. Es lo coherente con dejar cambiar de idioma sin tocar Windows — si cambia el
texto, cambia el número. Nueva `L.Culture`, que `L.Set` actualiza junto al idioma y que `FormatBytes` usa
por defecto (aceptando además una cultura explícita, que es lo que hace testeable la escalera de unidades
sin mezclarla con el separador).

Lo que **no** se hizo, y es la mitad del trabajo: `L.Culture` **no** se asigna a `CultureInfo.CurrentCulture`.
La cultura del hilo gobierna también comparaciones y mayúsculas, que es por donde volvería `T1-01` (la
guarda de disco de sistema fallando bajo cultura turca). Hay una prueba que fija `tr-TR`, cambia el idioma
y comprueba que el hilo no se ha movido.

Y el detalle que lo delató: al aplicarlo se pusieron rojas **cuatro pruebas existentes** que afirmaban el
separador **inglés** con la app arrancando en español. Pasaban porque `FormatBytes` leía la cultura del
hilo y el fixture la fijaba invariante — medían el separador de la prueba, no el de la app. Estaban verdes
describiendo algo que el usuario nunca veía. Ahora cada una dice en qué idioma habla.

---

### 2026-08-17 — `T6-11`: la revisión completa encuentra lo que la incompleta no podía

26 tomas, 13 pantallas × 2 temas, con la app corriendo y la USB conectada. Confirmó en ejecución `T6-01`,
`T6-02`, `T6-06`, `T6-07` y `T6-10`, y la barra de ocupación en los dos temas —en claro, relleno `#5C5C5C`
sobre pista `#E0E0E0`: se distinguen sin esfuerzo, que era lo que el cálculo prometía y nadie había visto—.

**Y abrió tres tareas que la primera ronda no podía ver**, todas en pantallas que entonces no se pudieron
abrir: `T6-13` (*Novedades* muestra los asteriscos de la cursiva de Markdown y parte los párrafos por donde
venían ajustados en el original), `T6-14` (los textos legales vienen a ~75 columnas y en el diálogo entran
~60, así que hasta las líneas separadoras salen cortadas) y `T6-15` (los resúmenes de confirmación llevan
`\n` incrustados y el control vuelve a ajustar encima).

**Esto es el argumento de la tarea, no una nota al pie.** Una revisión que solo mira lo que puede mirar
produce una lista que parece completa. Anotar el hueco (`T6-11`) y luego cerrarlo fue lo que convirtió
«diez hallazgos» en trece — y los tres nuevos están en sitios que importan: la primera pantalla tras
actualizar, el texto legal y lo que hay que leer antes de borrar un disco.

**Y siguió pagando después.** Al implementar `T6-14` el primer intento truncaba el texto legal en silencio
—peor que el ajuste feo que venía a arreglar— y no lo detectó ningún razonamiento: lo detectó volver a
fotografiar el diálogo. La captura no es la verificación de esta tarea; es la verificación de todas.

**Detalle de intendencia:** `docs/screenshots/gallery/` está en `.gitignore`, así que refrescarla no mete
26 binarios en el repo. Es material de revisión local; las tres capturas del README viven aparte y esas sí
se versionan.

---

### 2026-08-17 — `T6-06`, `T6-07` y `T6-08`: dos trampas de WinUI y un hallazgo medio equivocado

**`T6-07` es el aviso de que una revisión también puede estar mal.** Anoté que «cada diálogo coloca sus
botones distinto» como si fuera descuido. Los botones apilados de *chkdsk* **no** lo son: con tres botones
nativos en fila, WinUI truncaba «Comprobar y reparar» **sin puntos suspensivos** («Comprobar y repar»), y
en PT/IT es peor. Estaba explicado en un comentario del propio código, que no leí antes de escribir el
hallazgo. Uniformarlos habría reintroducido un fallo real. Lo mismo el *Historial*: sus tres botones **no
cierran** el diálogo, así que van en el contenido; abajo solo va lo que cierra.

Lo que **sí** estaba mal era el **ancho**: siete diálogos con seis criterios (360, 380, 400, 300–420,
360–420, y el de chkdsk **sin ninguno**, ajustándose a su texto). Abrir dos seguidos hacía «saltar» la
ventana. Ahora los fijan dos tokens compartidos. Y la regla que la app ya seguía sin estar escrita —qué va
en los botones nativos, qué en el contenido, cuándo se apila— queda escrita en `AppTheme.xaml`, **con el
porqué de la excepción de chkdsk**, para que el siguiente que la mire no la «arregle».

**`T6-08`: en WinUI un panel no se puede deshabilitar.** `IsEnabled` vive en `Control` y `Panel` deriva de
`FrameworkElement`, así que `<StackPanel IsEnabled="False">` no compila (`WMC0011`) — al contrario que en
WPF, donde `UIElement` sí lo tiene. Fue lo primero que intenté.

Y su corolario, que es el que de verdad hay que recordar: **un `TextBlock` tampoco es un `Control`**, así
que no tiene estado visual deshabilitado. Apagar solo los desplegables dejaría las etiquetas a pleno
contraste, más vivas que el control al que acompañan. Se atenúan aparte con `TextFillColorDisabledBrush`
—el token del tema, no un alfa inventado— y al reactivarlas con `ClearValue`, para no dejarles un color
clavado que sobreviva a un cambio de tema en caliente.

---

### 2026-08-17 — `T6-04` y `T6-05`: el mismo principio en dos sitios

Las dos tareas son la misma idea: **la app guarda datos de máquina y los enseñaba sin traducir**.
«32161 h» y `small-fat32=2147483648` son correctos y no responden a la pregunta que el usuario tenía al
abrir esa pantalla.

**`T6-04` — un decimal siempre, y así no hay que pluralizar.** «1,0 años» concuerda en los cinco idiomas;
«1 años» no. Evitar la concordancia singular/plural en cinco traducciones vale más que ahorrar un decimal.
Los cortes de tramo están a **dos** unidades, no a una: con 33 días se dice «≈ 33,5 días», no «≈ 1,1 meses».

**`T6-05` — transforma lo que se MUESTRA, nunca lo que se guarda.** Era tentador arreglarlo en las llamadas
a `History.Log`, y habría sido peor por dos motivos: `history.log` y el CSV tienen consumidores y el byte
exacto es justo lo que sirve al depurar; y las entradas **ya escritas** seguirían ilegibles. Como función
de presentación, el historial de hace un mes también se arregla.

**Lista blanca de claves, no heurístico.** En la misma línea conviven `code=1`, `passes=3` y `quick=True`.
Un «convierte los números que parezcan grandes» acabaría diciendo «1 B» donde pone `code=1`. Añadir un
tamaño nuevo obliga a tocar la lista, que es justo la decisión que conviene tomar a conciencia.

**Lo que casi se cuela.** El buscador del historial filtra por el detalle crudo. Con la lista mostrando
«2 GB» y el fichero guardando «2147483648», teclear lo que estás viendo no habría devuelto nada — un
buscador que no encuentra lo que hay en pantalla es peor que no tener buscador. `Matches` busca ahora en
los dos.

**Y una tarea nueva, `T6-12`.** Poner un decimal al lado de una palabra traducida hizo visible algo
anterior: los números se formatean con la cultura de **Windows** y el texto con el idioma de **la app**,
que son cosas distintas porque la app deja cambiar el idioma sin tocar Windows. Sale `32,161 h (≈ 3.7
años)`. No es una regresión de `T6-04`: `FormatBytes` lleva haciéndolo desde siempre (`223.6 GB`), y
`T6-04` se implementó con el mismo criterio para no dejar dos conviviendo. Arreglarlo es un cambio
transversal y va aparte.

---

### 2026-08-17 — `T6-03`: «no lo sé» y «no aplica» no son lo mismo

La fila decía «Velocidad de rotación: **SSD**» —una velocidad cuyo valor es un tipo de medio— con «Tipo de
medio: SSD» justo encima. En un disco de estado sólido no es que el dato falte: es que la pregunta no
existe.

**Lo interesante fue el caso de en medio.** Hay tres estados, no dos:

| Señal | Qué significa | Qué se pinta |
|---|---|---|
| `RPM = 0` | el disco dice «no giro» | fila **oculta** |
| `RPM > 0` | gira a esa velocidad | fila con las RPM |
| sin RPM, medio = SSD | no hay contador, pero se declara SSD | fila **oculta** |
| sin RPM, medio desconocido | **no se sabe** | fila con *No disponible* |

El último es el que obliga a pensar. Esconder la fila ahí sería **afirmar que es de estado sólido sin
saberlo**, que es exactamente el tipo de mentira que esta tarea venía a quitar. Por eso `HasSpindle`
devuelve `true` cuando no hay ninguna señal: «asume que gira» no es una suposición sobre el hardware, es
la instrucción de mostrar la fila como desconocida en vez de decidir por el usuario.

**Dónde vive.** En `Core/SmartInfo`, no en el diálogo: es una decisión y aquí las decisiones se miden.
De paso se va el literal `"SSD"` del code-behind, que era texto de cara al usuario fuera de
`Localization/` — justo el patrón que `LocalizationCoverageTests` persigue.

**Verificado por las dos caras, en la app real** (`capture-screenshots.ps1 -Only health`): en **D:** (SATA
SSD) la fila ya no aparece; en **I:** (USB que no informa de nada) **sigue apareciendo** como *No
disponible*. Una sola de las dos capturas no habría distinguido «lo arreglé» de «la borré para todos».

---

### 2026-08-17 — `T6-02`: el placeholder que regalaba la respuesta, y lo que tapaba

El campo de confirmación llevaba como `PlaceholderText` la propia letra a teclear: parecía relleno, y
ponía la respuesta dentro del hueco donde hay que transcribirla — el único punto de fricción deliberada de
la app. Quitar esa línea es el arreglo entero… salvo por lo que se descubre al quitarla.

**WinUI usa el `PlaceholderText` como NOMBRE ACCESIBLE del `TextBox` cuando no hay otro.** El campo se
llamaba `I`: un lector de pantalla **anunciaba la respuesta en voz alta**. El fallo era peor de lo que la
revisión había anotado, y la mitad grave era la que no se veía en la captura. Y borrar el placeholder sin
más lo habría dejado llamándose «…», que no es mejor. Se le da nombre explícito
(`confirm.inputName` ×5), que además no depende de lo que se pinte dentro.

**La primera prueba que escribí no valía, y lo dijo la reversión.** Buscaba «un elemento del diálogo cuyo
texto visible sea exactamente la letra», razonando que así daba igual cómo expusiera WinUI el placeholder.
Pasó en verde — y **volvió a pasar con el fallo reintroducido**: WinUI no lo publica como texto de ningún
elemento. Un diagnóstico volcando las propiedades del `TextBox` señaló el sitio real (`Name`), y la prueba
se reescribió contra él. Ahí sí falla al revertir.

**La lección no es sobre placeholders.** Es que «lo escribí, pasa en verde» no dice nada sobre si la prueba
mira donde cree mirar. Esta pasó dos veces por motivos opuestos, y solo la reversión distinguió una de la
otra. Es el mismo patrón que ya había mordido en `T1-04` y en `T1-07`.

---

### 2026-08-17 — `T6-01`: el título del diálogo destructivo deja de ser un valor por defecto

Reinicializar se anunciaba como «Confirmar formato». Lo arreglado no es la cadena: es **de dónde salía**.
`ConfirmDialog` fijaba `Title = L.T("confirm.title")` **en su constructor**, así que las dos operaciones
irreversibles no podían tener títulos distintos aunque quisieran. Ahora el título es un **parámetro
obligatorio**, no opcional con valor por defecto: una tercera operación destructiva no puede heredar el
nombre equivocado por omisión, porque no compila sin decidirlo. Nueva clave `confirm.titleReinit` ×5.

**La prueba unitaria recorre los cinco idiomas**, no solo el español: el fallo era una cadena compartida,
y la forma natural de reintroducirlo es traducir uno nuevo copiando el de al lado. Exige que los dos
títulos existan y **difieran** en cada idioma.

**La prueba de UI no se ancla a un texto fijo.** `FormatDiskPro.UiTests` conduce el `.exe` como caja negra
—no referencia el ensamblado de la app— así que comparar contra «Confirmar reinicialización» rompería la
suite con la app en inglés. Lo que exige es lo que define el fallo: que cada operación tenga **su propio**
título y ninguno esté vacío. Hizo falta un `DialogHelper.ReadTitle`: la plantilla de WinUI pinta el título
en un `ContentControl x:Name="Title"`, con caída al `Name` del diálogo.

**Verificada por reversión, no solo en verde.** Contra la app real lee `'Confirmar formato'` y
`'Confirmar reinicialización'`; devolviendo la llamada a `confirm.title` **falla**, y el mensaje dice cuál
es el problema en vez de «esperaba X, obtuve Y». Una prueba que solo se ha visto en verde no demuestra que
mire lo que dice mirar.

**Dato de intendencia:** las etiquetas de la USB de pruebas se habían quedado en `UTIL`/`TEST` y
`TestDrive` busca `utilidades`/`Bios Flash` — las pruebas se **omitían**, que es lo correcto (precondición
ausente no es fallo), pero se leen como «pasadas» en el resumen si no se mira el desglose. Se reetiquetaron.
FAT32 guarda su etiqueta en mayúsculas (`BIOS FLASH`); `FindLetter` compara con `OrdinalIgnoreCase`, así
que da igual.

---

### 2026-08-17 — Revisión de UX/UI: se abre el Tier 6

Revisión enfocada **solo** en interfaz, sobre las capturas del corte de la v1.22.0 y contrastando cada
hallazgo contra el código. Resultado: **[Tier 6](ROADMAP.md#-tier-6--refinado-de-uxui)**, 11 tareas.

**Los tres que no son cuestión de gusto** —la interfaz afirma algo que no es cierto—:

1. **`ConfirmDialog` titula «Confirmar formato» también al reinicializar.** El título está fijado en el
   constructor, así que la operación **más** destructiva de la app (borra el disco físico entero) se
   anuncia con el nombre de otra menos grave. El cuerpo sí lo explica bien; el título no.
2. **El campo de confirmación lleva la letra como *placeholder***. Parece relleno —en las capturas se ve
   una «G» gris sin que nadie haya tecleado— y regala la respuesta justo donde el diseño puso fricción a
   propósito.
3. **«Velocidad de rotación: SSD»**. Una velocidad cuyo valor es un tipo de medio, con la fila de encima
   ya diciendo «Tipo de medio: SSD».

**El de más recorrido** es `T6-05`: el historial muestra la línea de log en crudo
(`REINIT I: -> G: fs=FAT32 style=MBR small-fat32=2147483648`). Eso son bytes sin convertir en una pantalla
que la gente abre para comprobar qué le hizo a un disco. Al arreglarlo hay dos cosas que no se pueden
romper: el CSV y `history.log` tienen consumidores, y el parseo por líneas ya se partió una vez (`T3-11`).

**Lo que esta revisión NO vio, y está anotado como `T6-11`.** El terminal no estaba elevado, así que ni
`capture-screenshots.ps1` ni FlaUI pudieron correr —los dos abortan por diseño contra una app
`requireAdministrator`—. Las capturas usadas son **anteriores** al bloque de ocupación nuevo, y quedaron
sin fotografiar Presets, Acerca de, Novedades, Licencia y Terceros. Los 10 hallazgos son válidos porque
cada uno está verificado en el código, pero **la cobertura no fue completa**, y eso vale la pena tenerlo
escrito en vez de dar el tier por exhaustivo.

**Nota de nombres:** ya existía un «Tier 6 — Pulido UX/UI» en la Parte 1 del ROADMAP (v1.13.0, IDs
`#28`–`#36`). Este es de la Parte 2 y usa `T6-xx`. Se parecen porque tratan de lo mismo; no son el mismo
tier. Está advertido en las dos cabeceras.

---

### 2026-08-16 — La barra de ocupación pasa a tener dos colores (y deja de ser un `ProgressBar`)

Hasta aquí el espacio **libre** no se pintaba: era el hueco del `ProgressBar`, una línea de 1 px del color
de pista del sistema. En una unidad recién formateada —0 % usado— la barra no mostraba absolutamente nada,
que es justo el estado en el que el usuario acaba de mirarla.

**Por qué no bastaba con engordar el `ProgressBar`.** Su plantilla (WinUI 1.8) pinta la pista con
`Height="{ThemeResource ProgressBarTrackHeight}"` = **1**, mientras el relleno ocupa el `MinHeight` del
control. Aunque se suba el `MinHeight`, usado y libre **no pueden tener el mismo grosor** sin sobrescribir
dos recursos internos de la plantilla, uno de ellos a través de un `Binding` con `StaticResource` que se
resuelve al aplicar el template. Se cambió por un `Border` (pista = libre) con un `Border` hijo (relleno =
usado) y columnas estrella para el reparto: dos colores, un grosor, y sitio para más segmentos el día que
la barra tenga que representar varias particiones.

**Lo que se rompía al añadir el segundo color.** El relleno neutro del tema claro era `#8A8A8A`, elegido
cuando su vecino era el fondo de la tarjeta. Con la pista pintada al lado, la frontera usado/libre se
quedaba en **2.62:1**: por debajo del 3:1 de WCAG 1.4.11, y esa frontera es *toda* la información de la
barra. Se oscureció a `#5C5C5C` → **5.07:1**. El tema oscuro no se tocó: allí el relleno ya era el claro de
los dos.

**El barrido de contraste tuvo que aprender a medir contra un vecino.** La pista **no debe** llegar al 3:1
contra la tarjeta —es un hueco, no una segunda barra—, así que meterla en `All()` tal cual la habría
suspendido, y la reacción natural habría sido oscurecerla hasta que compitiera con el relleno: el umbral
correcto aplicado al par equivocado. `PaletteColor` gana un `Against` opcional, y la pista entra al
inventario con **una entrada por cada relleno con el que puede compartir barra** (neutro, ámbar, rojo × 2
temas). Medidos: 5.07 / 3.98 / 4.29 en claro, 3.39 / 5.70 / 4.37 en oscuro. Un test fija que esas entradas
declaren su `Against`, para que nadie se lo quite «para que se mida como las demás».

**Accesibilidad:** un `Border` no expone valor de rango como hacía el `ProgressBar`, así que el nombre
accesible dejó de ser suficiente como etiqueta suelta. `info.used` pasa a llevar el dato
(«Espacio utilizado: 43 %») y se fija en `RenderCapacity`, no en `ApplyLanguage` — ese ya termina llamando
a `UpdateInfo`, y la barra está oculta mientras no haya unidad.

**Segunda pasada, el mismo día:** esquinas rectas en vez de píldora y el dato de ocupación **fuera** de la
barra, en su propio bloque: separador, línea `Ocupación` … `Usado 780,9 GB / 930,5 GB` (etiqueta izquierda,
dato derecha) y la barra debajo, a **6 px**.

- **Por qué 6 px y no 10.** La barra es el único bloque de color saturado de la tarjeta; el resto es
  tipografía gris con un título de acento. A 10 px, con la unidad al 84 % —ámbar—, pesaba más que el
  título de sección y desencajaba. Es un dato, no un control: la mitad de superficie de color basta.
- **Por qué el dato lleva etiqueta.** Suelto y alineado a la derecha era el único texto así de toda la
  tarjeta (las otras seis entradas son «Etiqueta: valor» en dos columnas a la izquierda), y se leía como
  huérfano. Emparejado con `Ocupación` a la izquierda reproduce la estructura de la referencia y encaja en
  la rejilla.

- **El rótulo por dentro se probó y se descartó.** Anclado a la derecha, el texto cae sobre el espacio
  libre casi siempre… salvo con la unidad casi llena, que es justo cuando hay que leerlo: ahí queda sobre
  el relleno ámbar o rojo. Se resolvió dándole fondo propio del color de la pista, que funcionaba pero
  dejaba una muesca gris dentro de la barra al llenarse. Fuera no hay problema que resolver: es texto de
  la tarjeta, con el contraste de cualquier otro. **Si alguien vuelve a intentar meterlo dentro, este es
  el caso que hay que resolver primero**, y medir anchos en tiempo de ejecución no basta: queda el tramo
  en que el texto pisa los dos segmentos.
- **Fallo encontrado mientras el rótulo estaba dentro, y que se queda arreglado:**
  `ContrastAgainstReference` componía el alfa sobre el fondo de la tarjeta aunque el color se pintara
  encima de otra cosa. Con colores opacos da igual, pero el color de texto claro lleva alfa (`#E4000000`):
  la cifra no correspondía a lo que se ve, que es el fallo exacto que ese barrido existe para cazar. Ahora
  se compone sobre la referencia, con un test propio — no depende de que haya hoy un color translúcido
  sobre un vecino.

---

### 2026-08-16 — Corte de la **v1.22.0** (Tier 5 completo)

`release.ps1 -Version 1.22.0 -UiTests -NotesFile docs\release-notes-1.22.0.md` desde terminal elevada, tras
un `-DryRun` previo.

**El dry run hizo su trabajo**: abortó porque `docs/release-notes-1.22.0.md` estaba sin rastrear. Es la
guarda de archivos nuevos, y avisó **antes** de tocar nada — que es exactamente para lo que está.

**Puertas superadas:** CHANGELOG con su sección · **521/521** unitarias · `Core/` al **97,9 %**
(463/473, mínimo 90) · UI **24/27** con 3 omitidas, todas de opt-in.

**Verificado contra el release real, no contra lo que dijo el script:**

- Hash local, contenido del `.sha256` y **el digest que GitHub calculó sobre el asset subido** coinciden:
  `3000c3de…0f17`.
- Asset del checksum con el nombre exacto `FormatDiskPro-1.22.0-setup.exe.sha256`, **96 bytes** (bajo el
  tope de 512 que impone `T2-07`).
- `.csproj` tras el bump: **BOM UTF-8 presente** y acentos intactos (`Ricky Angel Jiménez Bueno`) — la
  corrupción acumulativa de `Get-Content -Raw` no volvió.
- Metadatos del `.exe`: `1.22.0`, empresa y copyright correctos.
- Instalador de 58,8 MB (61 684 481 bytes).

**Dato que se corrige aquí:** el corte ejecuta **27** de las 28 pruebas de UI, no las 28. `release.ps1`
pasa `--filter "Category!=Slow"`. No es un fallo, pero la cifra del corte y la de una corrida manual no son
comparables, y conviene no confundirlas.

---

### 2026-08-16 — `T5-03`: qué queda cuando el plan falla a mitad (y el Tier 5 se cierra)

**El fallo estaba en el sitio menos evidente: dónde se imprimían los marcadores.** Iban todos agrupados al
final del script. Con `ErrorActionPreference='Stop'`, un fallo en la segunda partición abortaba **antes de
emitir el de la primera**, así que «no se creó nada» y «la primera salió bien y la segunda no» producían
exactamente la misma salida: cero letras. Ninguna cantidad de código en la UI podía distinguirlas.

Ahora cada partición emite lo suyo **en cuanto lo alcanza**, y son **dos** marcadores porque son dos estados:

- `PART:i:` — creada, existe en la tabla de particiones.
- `LETTER:i:X` — además formateada y utilizable.

Una partición cuyo `Format-Volume` falla se queda **entre las dos cifras**, y decir «no se creó ninguna»
sería falso. De ahí `ReinitResult.PartitionsCreated` junto a `Letters`.

**Decisiones:**

- **No se revierte.** El disco ya está borrado: «deshacer» solo podría significar borrarlo otra vez, y esa
  no es una decisión que el usuario haya pedido. Hay una prueba que lo exige de la única forma que no admite
  interpretación — que **no se lance un segundo proceso** tras el fallo.
- El aviso dice explícitamente que **el disco ya estaba borrado cuando falló**. Un «no se pudo
  reinicializar» a secas deja creer que no pasó nada, que es lo contrario de la verdad.
- El mensaje detallado solo aparece con planes de **más de una** partición: con una, el fallo es binario y
  el mensaje de siempre ya lo cuenta.

**El camino de fallo parcial NO está verificado sobre hardware**, y no es un descuido: forzar que
`Format-Volume` falle en la segunda partición de un USB real no es reproducible a voluntad. Se cubre con
`FakeProcessRunner`, que es exactamente para lo que existe la costura de `T4-02`.

**El fallo de proceso de esta sesión, que sí enseñó algo.** La primera ejecución en hardware falló, pero
**la app se comportó bien**: el `settings.json` del usuario tenía `CreateSecondPartition: true` de haber
probado la función, y la app recordó esa preferencia. La equivocada era la prueba, que daba por supuesto el
valor de fábrica de un ajuste **persistido** — es decir, pasaba o fallaba según lo que hubiera hecho antes
el usuario de esa máquina. Los pasos 2 y 3 fijan ahora su estado explícitamente. De rebote, ese fallo
confirmó `T5-02` por una vía que nadie había planeado: creó las dos particiones con NTFS en el resto.

**Con esto el Tier 5 queda cerrado**: `T5-01`, `T5-02`, `T5-03` y `T5-05` completadas, `T5-04` (N
particiones) **descartada** por decisión de producto — el motor admite N, lo que se limitó es la interfaz.
**521/521 unitarias** (+12).

---

### 2026-08-16 — `T5-02` y `T5-05`: el sobrante deja de morir sin asignar

El hueco que abrió el Tier 5, cerrado. Al crear una FAT32 pequeña, la tarjeta ofrece qué hacer con el resto:
dejarlo sin asignar (por defecto, sin cambio para quien no toque nada) o **una segunda partición** que lo
ocupe entero. Sin diálogo nuevo: dos filas más en la tarjeta, la misma operación y la misma confirmación.

**Alcance acordado con el usuario (2026-08-16): dos particiones, no N.** `T5-04` sigue cerrado. Las razones,
por si se reabre: la ventana es de 500×900 y de tamaño fijo, así que una tabla de filas variables obliga a
un diálogo aparte; MBR solo admite 4 primarias y MBR es lo que se elige en todo USB de menos de 2 TB, así
que «N» en la práctica es «3 o 4»; y el caso que originó el tier lo cubren dos. El **motor admite N desde
`T5-01`** — lo que se limitó es la interfaz.

**Decisiones:**

- **La FAT32 va siempre primera**, y no es cosmético: los equipos anteriores a Windows 10 1703 y muchos
  aparatos empotrados solo leen la primera partición de un medio extraíble, y la FAT32 es justo la que
  interesa que vean. La nota de plataforma vive en la **interfaz** (`opt.restNote`), no solo aquí.
- **FAT32 y FAT no se ofrecen para el sobrante**: el resto de un pendrive grande supera sus límites (32 GB
  y 2 GB), así que ofrecerlos sería ofrecer un fallo con el disco ya borrado. ReFS tampoco, por no ser un
  sistema para medios extraíbles. Quedan exFAT (primero) y NTFS.
- **`RestPicker` se selecciona por índice en las pruebas**, no por texto: sus ítems están traducidos a cinco
  idiomas y buscarlos por su cadena ataría la prueba al idioma activo.

**`T5-05` comprueba el disco físico, no el diálogo.** Si la segunda partición no se creara, el diálogo de
éxito diría exactamente lo mismo. `AssertDiskLayout` cuenta particiones y mide el espacio libre.

**Verificado sobre hardware** (USB de pruebas, 29,3 GiB): 2 particiones y **0 MB sin asignar** — partición 1
en `G:` de 1 GB en FAT32, partición 2 en `I:` de 28,296 GB en exFAT. Windows asignó **G e I, no G y H**: la
confirmación en vivo de por qué `T5-01` tuvo que emitir `LETTER:<índice>:`. Sin el índice, la app habría
tomado la partición de 28 GB por la FAT32.

**El fallo de la sesión, que merece quedar escrito.** La primera ejecución de `T5-05` salió **en verde sin
haber probado nada**: `dotnet build FormatDiskPro.slnx` no compila el proyecto de pruebas de UI (no está en
la solución, a propósito — ver §4), así que con `--no-build` se ejecutó la DLL anterior. El código nuevo ni
siquiera compilaba. Lo destapó comprobar el disco en vez de fiarse del verde, que es la misma disciplina que
la propia prueba aplica.

**509/509 unitarias** (+16).

---

### 2026-08-16 — `T5-01`: el layout deja de ser un `long?`

Primera tarea del Tier 5, y la única que se puede probar entera sin hardware. **No cambia nada de lo que la
app hace**: la UI sigue mandando una sola partición.

**El problema.** El layout vivía en `long? partitionSizeBytes`: «una partición de este tamaño, o todo el
disco si es `null`». Con una partición se sostenía. Ese tipo no distingue «el resto» de «todavía no lo sé»,
y el significado estaba en un comentario en vez de en el tipo.

**Qué hay ahora.** `Core/PartitionPlan.cs`: `PartitionSize` (jerarquía cerrada `Exact` | `Remainder`),
`PartitionSpec`, `PartitionPlan` y `Validate(diskSizeBytes)` → `PlanValidation(PlanProblem, PartitionIndex)`.
Trece motivos, **valores y no cadenas** —se traducen en la UI y se comparan en una prueba— y con el índice
de la partición culpable para que `T5-02`/`T5-04` puedan señalar la fila.

**Decisiones que merecen quedar escritas:**

- **«El resto» se delega a `-UseMaximumSize` al ejecutar, pero se calcula al validar.** Parece
  contradictorio y no lo es: calcular los bytes para crearlos es pedir un error de alineación, pero para
  saber si un FAT32 cabe en 32 GB hay que conocer su tamaño, y el de «el resto» solo se sabe con el del
  disco. De ahí `EffectiveSizes`. Pedir el resto de un pendrive de 256 GB en FAT32 se rechaza **antes** de
  borrar nada.
- **Un tamaño de disco desconocido no invalida un plan que no lo necesita.** La primera versión rechazaba
  todo plan sin tamaño, y eso habría roto *Reinicializar* en unidades **RAW** — justo el caso para el que la
  función existe, y donde `Get-Disk` puede no devolver nada. Una sola partición «el resto» no necesita el
  dato; cualquier otra cosa sí.
- **Las reglas de «el resto» se miran sobre el plan entero, no partición a partición.** Dentro del bucle, un
  plan con dos «resto» se rechazaba por «no es la última»: cierto, pero no es el problema. El motivo acaba
  en un mensaje al usuario y conviene que sea el que explica. Lo destaparon dos pruebas.
- **`ReinitDrive` revalida** aunque la UI ya lo haya hecho. Es la última línea antes de `Clear-Disk`.
- **`ReinitResult.Letters` va aparte de `NewLetter`** y no lo sustituye: son dos preguntas distintas. La UI
  necesita saber **cuál seleccionar**; `T5-03` necesitará saber **qué llegó a crearse**. Con una lista vacía
  y `Ok` en falso no se distinguiría «no se creó nada» de «se creó algo y falló después».
- **Se exige una letra por partición** para dar la operación por buena. Con un plan de dos, que solo salga
  la primera es exactamente el fallo parcial que no debe pasar por éxito.
- **`MinPartitionBytes` = 64 MiB.** Queda por encima del volumen más pequeño que Windows formatea con
  cualquiera de los sistemas admitidos (FAT32 necesita ~33 MiB). Sin ese suelo, «el resto» cuando apenas
  queda nada pasaría la validación y reventaría en `Format-Volume`, otra vez con el disco borrado.
- **El margen se reserva por partición**, no una vez por disco: cada una alinea su inicio. Es generoso a
  propósito — quedarse corto es fallar con el disco borrado; pasarse son unos MiB sin usar.

**493/493 unitarias** (+40) · `Core/` al **97,8 %** (445/455) · build 0/0.

**Verificado sobre hardware** (USB `utilidades`, disco 6): `DestructiveLifecycleTests` **3/3**, y el disco
queda **idéntico** al resultado anterior a `T5-01` — partición de 1 GB en FAT32 sobre MBR y 28,3 GB sin
asignar—, que es justo lo que se buscaba de un refactor. Confirma las tres cosas que las unitarias no
alcanzan: el camino `-Size` del bucle nuevo, que `LETTER:0:` se emite y se parsea (si no, `Ok` sería falso
al exigirse una letra por partición), y que releer por `-PartitionNumber $p<i>.PartitionNumber` funciona en
un USB real. Esa relectura sustituyó a «la primera partición que tenga letra», que deja de valer con dos, y
era el único cambio del script sin red.

> **Las pruebas de UI no dejan rastro en el historial**: `SettingsBackup` restaura `history.log` al
> terminar. Buscar ahí lo que hizo la suite no sirve — el estado del disco sí.

---

### 2026-08-16 — *FAT32 pequeña*: la función estaba escondida justo donde más falta hacía

Reportado probando la app con un pendrive: **en unidades de menos de 32 GB la sección no aparecía**. Una
sola condición en `MainWindow.FormatOptions.cs`:

```csharp
bool qualifies = type == DriveType.Removable && bytes >= FormatLogic.Fat32MaxBytes;
```

**Por qué estaba así, y por qué estaba mal.** La función (#37, Tier 7) se concibió como *rodeo al límite de
Windows*: como no deja crear volúmenes FAT32 de más de 32 GB, en un USB grande se recorta la partición para
que FAT32 sea legal. Con esa lectura, ocultarla en discos menores tenía sentido — allí FAT32 ya está en el
selector. Lo que la condición no vio es que el **mecanismo** que hay debajo (`New-Partition -Size N` y dejar
el resto sin asignar) es útil por sí mismo, independientemente de FAT32. Es el fallo clásico de codificar
la *motivación* de una función como su *precondición*.

**Tres cosas más salieron al tirar del hilo**, todas de la misma raíz:

1. **El selector no comprobaba si el tamaño cabía.** Ofrecía siempre 1/2/4/8/16/32 GB, fijos en el XAML. Un
   pendrive de "16 GB" son ~14,9 GiB reales: elegir 16 pedía una partición imposible y `New-Partition`
   habría fallado **con el disco ya borrado**. No se notaba solo porque el `≥ 32 GB` lo tapaba — el bug
   llevaba ahí desde la 1.14.0, latente detrás de la condición equivocada.
2. **El tope se medía sobre el volumen.** `DriveInfo.TotalSize` es la partición actual, no el disco. Como
   `Clear-Disk` borra el disco entero, el tope correcto es el del disco. Con el del volumen, usar la
   función una vez (16 GB → partición de 2 GB) dejaba el tope en 2 GB: un **trinquete que solo baja**. Es
   exactamente el problema que la revisión del `T5-01` había anticipado, aquí ya en producción.
3. **`StyleFor` recibía también el tamaño del volumen.** El límite de 2 TB es de MBR y se aplica al disco.

**Qué se hizo.** `Core`: `SmallFat32SizesFor(diskSizeBytes)` filtra los tamaños que caben —con
`PartitionReserveBytes` (16 MiB) de margen para la alineación de la partición y la copia de la tabla GPT al
final del disco— y `PickSmallFat32Size` elige la preselección. `Services`: `DiskService.GetDiskSizeAsync`,
hermano de `GetDiskNumberAsync`. `UI`: la sección aparece en cualquier extraíble donde quepa el menor de los
tamaños, el selector se pobla por código, y `LoadDiskSizeAsync` corre **en paralelo** con `LoadHealthAsync`
al seleccionar unidad.

**Decisiones que merecen quedar escritas:**

- **Mientras la consulta del disco está en vuelo se usa el tamaño del volumen**, que siempre es menor o
  igual. Se ofrece de menos, nunca de más: el error posible es una opción que falta un instante, no una que
  destruye un disco. Lo mismo si la consulta falla (unidad RAW): no se toca nada.
- **`SelectedSmallFat32SizeGb()` devuelve `0` y no `32`** cuando no hay selección válida. Con la lista fija
  del XAML caer al máximo era inocuo; con una lista que depende del disco, pediría una partición que no cabe.
- **La preselección programática no se persiste** (`_repopulatingSizes`). Si hoy hay conectado un pendrive
  de 8 GB, guardar «8» borraría la preferencia de 32 GB del usuario para el siguiente disco.
- **Se revalida el tamaño contra el disco justo antes de `Clear-Disk`.** El selector se pobló al seleccionar
  la unidad; entre eso y pulsar el botón el disco puede haber cambiado. Pasarse de tamaño falla en el peor
  momento posible: con el disco ya borrado.
- **El sistema de archivos sigue forzado a FAT32** (decisión explícita del usuario, 2026-08-16). Que la
  partición pequeña use el sistema de archivos del selector es más útil y más coherente, pero es un paso
  hacia el `T5` y arrastra renombrar la opción en los cinco idiomas. Queda anotado, no hecho.

Nueva clave `opt.smallFat32HintSmall` (×5 idiomas): en discos que no llegan a 32 GB, hablar del límite de
Windows no explica nada — lo que aporta la opción allí es dejar espacio sin asignar. Y `reinit.sizeTooBig`
para la revalidación. **453/453 unitarias** (+20).

**Verificado sobre hardware real** (2026-08-16, USB `utilidades`, disco 6, 29,3 GiB — justo por debajo del
umbral antiguo, así que antes la sección no aparecía):

- Suite de UI sin opt-in: **25 pasan / 3 se omiten / 0 fallan** de 28 (59 min).
- `FullLifecycle_FormatThenReinit_OnDedicatedTestUsb` con `ALLOW_DESTRUCTIVE=1`: **3/3** (1 min 5 s).
  Este paso 3 **se omitía siempre** hasta ahora: la USB no llegaba a 32 GB y la casilla nunca era visible.
- Estado final del disco comprobado con `Get-Partition`, no solo por el verde de la prueba: partición 1 de
  **1 GB en FAT32** (`XINT13`) sobre MBR, y **28,3 GB sin asignar**. El estilo MBR es el correcto para 29,3 GiB.

Ese estado final es además la demostración del arreglo del tope: el volumen queda en 0,996 GB pero el disco
sigue teniendo 29,3 GiB, así que el selector debe seguir ofreciendo hasta 16 GB. Con el tope medido sobre el
volumen —lo que hacía antes— la sección habría desaparecido por completo.

---

### 2026-08-16 — Corte de la **v1.21.0** (auditoría cerrada)

`release.ps1 -Version 1.21.0 -UiTests -NotesFile docs
elease-notes-1.21.0.md` desde terminal elevada y
con la USB conectada, precedido de un `-DryRun` completo — el primer corte que pasa por la **puerta del
`CHANGELOG`** de `T4-01`, y convenía verla funcionar antes de tocar git. Cobertura de `Core/` **97.4 %**
(368/378, mínimo 90), **433/433** unitarias y **24/27** de UI, con los 3 omitidos siendo exactamente los
opt-in. Instalador 58.8 MB.

Verificado contra el release ya publicado, como en los cortes anteriores: el `digest` que **GitHub**
calculó del instalador subido (`25fb7d78…`) coincide con el hash local y con el contenido del asset, que
se llama exactamente `FormatDiskPro-1.21.0-setup.exe.sha256` —el nombre que busca el emparejamiento de
`T2-06`— y ocupa **96 bytes**, holgadamente bajo el tope de 512 de `T2-07`. El `.csproj` conserva los
acentos de `<Authors>`/`<Copyright>` tras el bump y mantiene su BOM (`#45`), y el `.exe` publicado los
muestra bien.

> **Es un corte de mantenimiento, y las notas lo dicen por delante.** La app se comporta *exactamente*
> igual que la 1.20.0: sin funciones nuevas, sin cambios de interfaz, sin correcciones de algo que
> estuviera fallando al usuario. Prometer otra cosa en un release que solo mueve tripas gasta la
> atención de quien lo lea la próxima vez, y esa atención es lo que hace que se lean los avisos que sí
> importan. Hay precedente en el proyecto (la v1.14.1 fue mantenimiento de pruebas).

---

### 2026-08-16 — `T4-04` y `T4-03`: la auditoría queda cerrada

Con estas dos, la Parte 2 del ROADMAP se cierra: **39/40 completadas y 2 descartadas**, 0 abiertas. Lo
único que queda vivo en el repositorio es el Tier 5, que no es remediación sino ampliación.

**`T4-03` — descartada, y el motivo no es el dinero.** Es que **nunca fue una tarea**: «firmar el
instalador» contradice la decisión `#13` (2026-06-24), que ya había decidido *no* firmar y que es la razón
de existir de la verificación por SHA-256. Tenerla abierta en el backlog afirmaba algo falso —que el
proyecto debía firmar y aún no lo había hecho— cuando lo cierto es lo contrario.

> **Y no esconde trabajo pendiente.** El pipeline ya admite firmar, el `.sha256` se genera *después* de
> firmar (firmar cambia el binario) y la ruta Authenticode quedó endurecida en `T1-08`. Falta un
> certificado, que es una **compra**. El día que aparezca, lo que hay que hacer no es «firmar»: es poner
> `SignsItsInstallers` en `true` **y** fijar el publicador esperado, y esa condición ya la vigila un test
> tripwire que falla si se hace a medias. Está mejor custodiada por el build que por una casilla.

**`T4-04` — el README pasa de 3 a 12 capturas**, seis pantallas en los dos temas: principal, S.M.A.R.T.,
chkdsk, reinicializar, confirmación destructiva e historial. Se regeneraron todas (las anteriores eran de
la v1.15.2) fotografiando el **publish self-contained**, que es lo que se distribuye. La galería completa
sigue siendo un artefacto de revisión y sigue ignorada por git; lo que se versiona son las 12 elegidas.

> **La tarea decía «copiar capturas» y lo que apareció fue un defecto en la herramienta.** Tres tomas
> —`reinit`, `confirm`, `checkdisk`— esperaban con un `Start-Sleep` fijo de 1,2 s en lugar de esperar a un
> elemento, pese a que el comentario que las encabeza afirma que todas «esperan a un elemento estable».
> Sobre una unidad **extraíble válida**, *Reinicializar* consulta antes el número de disco físico del
> objetivo y el de Windows —dos llamadas a PowerShell— para la guarda de «no es el disco del sistema»: la
> foto salía con la ventana principal **y sin ningún diálogo**. Ahora esperan al `InputBox` de
> `ConfirmDialog` y al `CheckScanButton`.

**La trampa que solo se ve mirando las fotos, no ejecutando el script:** capturar *Reinicializar* sin
`-Drive <USB>` **no falla** — produce una imagen impecable del mensaje «solo unidades extraíbles». Eso es
la **guarda**, no la característica, y habría acabado en el README anunciando lo que la app *no* hace. Es
la misma lección que `T2-12` en otro sitio: un proceso que termina en verde no está diciendo que el
resultado sea bueno. Queda avisado en el README, junto al comando.

Por eso las 12 no salen todas de la misma unidad: *Reinicializar* y *chkdsk* van sobre la USB de pruebas y
el resto sobre un SSD interno, porque **un USB no expone los contadores S.M.A.R.T.** que hacen interesante
esa pantalla. El README lo explica en vez de disimularlo.

---

### 2026-08-16 — `T4-02`, `T4-01` y `T4-05`: que fallar sea observable

El Tier 4 era «futuro / opcional» por definición, pero dos de sus tareas eran deuda de verdad. Auditoría
**38/40**: solo quedan las dos que no dependen del código —`T4-03` necesita un certificado de firma y
`T4-04`, una tanda de capturas regeneradas—.

**`T4-02` — los `Services` dejan de ser estáticos.** Los once pasan a clases con interfaz, y el grafo se
construye en una **raíz de composición** (`Services/AppServices`) que `App` crea y pasa a `MainWindow`, y
esta a los diálogos que la necesitan. **No es un localizador de servicios:** nadie le pide nada «desde
dentro», así que cada constructor sigue declarando de qué depende — que es la mitad del valor de esto.

> **Lo caro de probar no era la estática: era `new Process(...)`.** La tarea decía «todos son `static`, lo
> que hace imposible probar los caminos de error sin hardware», y la parte operativa de esa frase es la
> segunda. Cada servicio construía su propio proceso, así que reproducir un `chkdsk` que devuelve 2, un
> `Clear-Disk` que revienta a mitad o un `powershell.exe` bloqueado por directiva exigía **provocar la
> avería de verdad** — y en el caso de *Reinicializar unidad*, borrar un disco. La costura que lo cambia
> todo es `IProcessRunner`.

**La abstracción se queda en *arrancar* el proceso, no en «ejecutar y devolver la salida».** Es la decisión
que más importa aquí. Cada servicio lee su salida de forma distinta y con matices que costaron hardware
descubrir: el solapamiento de marcadores (`T3-02`), cerrar la entrada estándar (`T1-02`), esperar con
`CancellationToken.None` para no perder el código de salida al cancelar. Unificar esos bucles en un
«runner que lo hace todo» habría sido **reescribirlos**, y este cambio no podía cambiar comportamiento.
Los bucles quedan intactos; solo cambia de dónde sale el proceso.

**Resultado en pruebas: 398 → 433, y ninguna toca un disco.** Entre las 35 nuevas están las del camino
destructivo que nunca se habían podido escribir: reinicializar con `Clear-Disk` fallando, y —el peor caso—
salir con **código 0 pero sin letra asignada**, o sea el disco borrado y sin volumen montable. También se
fija por prueba que `CheckDisk.RunAsync` **no atrapa nada** (deliberado: la excepción tiene que llegar al
`catch` del handler, `T0-02`), que `/f` solo se pasa al reparar y que `/Y` siempre se pasa a `format.com`.

> **Verificado por reversión, no por «pasa en verde».** Deshaciendo la guarda de la letra nueva falla
> `Reinit_ExitZeroButNoLetterAssigned_IsAFailure`; poniendo el solapamiento de marcadores a 0 falla
> `Reinit_Success_ReportsEveryStageOnceAndInOrder`. Esa segunda solo funciona porque el doble entrega la
> salida **partida en trozos de 6 caracteres**: con un `StringReader` normal llegaría entera de una vez y
> la prueba pasaría igual con el solapamiento roto. Es la misma lección de siempre aquí — una prueba que
> no puede fallar no prueba nada.

**Dos costuras artificiales desaparecen al llegar la real.** `History.LogTo`/`ReadLinesFrom`/`ClearAt` eran
`internal static` con la ruta como parámetro, la única forma que había de no escribir en el `%AppData%` del
usuario al probar. Con la ruta inyectada por constructor vuelven a ser privadas.

**`UpdateService` conserva sus miembros internos `static`, y es una excepción razonada.** Ya se probaba
entero —sus pruebas levantan un servidor HTTP local y ejercitan hash correcto, hash que no coincide,
checksum ausente y respuesta desmedida—, así que instanciarlos habría significado **reescribir la ruta de
verificación que corre elevada** sin ganar una sola prueba. Se instancia lo que consume la UI.

**`T4-01` — `CHANGELOG.md`, con puerta en el corte.** Las 28 versiones publicadas más una sección *Sin
publicar*. Lo que lo hace sostenible no es el archivo: `release.ps1` **aborta si no existe ya la sección de
la versión que se va a publicar**, con un mensaje que dice exactamente qué escribir. Misma forma que la
cobertura mínima y el `.sha256`, y por el mismo motivo — un changelog que se queda atrás **afirma ser el
registro del proyecto y miente**, que es peor que no tenerlo.

> **Las fechas salen de `git for-each-ref`, no del recuerdo.** Escritas a ojo desde el índice de versiones
> de este mismo archivo, **18 de las 28 estaban mal**, alguna con ocho días de desviación. El índice de
> arriba nunca las tuvo: solo dice qué trajo cada versión, no cuándo salió. Escribir un documento cuyo
> propósito es responder «¿cuándo entró esto?» a base de memoria era garantizar que respondiera mal.

**`T4-05` — el último resto de Windows Forms.** No era una propiedad `Name`: eran `SetFormEnabled` (ahora
`SetControlsEnabled`, que además describe lo que hace) y un comentario *«same as MainForm»*.

Build 0/0, **433/433** unitarias. La suite de UI no se ha vuelto a ejecutar tras estos cambios: el refactor
no toca XAML ni `AutomationId`, pero **eso es un razonamiento, no una medida** — el corte con `-UiTests`
sigue siendo la puerta que lo comprueba.

---

### 2026-08-15 — **Tier 5 «Ocurrencias para features existentes»** abierto en el ROADMAP

Nace de usar la app, no de auditarla: tras *Reinicializar unidad → FAT32 pequeña*, el disco queda con la
partición pedida y **el resto sin asignar**, así que un pendrive de 256 GB se queda con 32 usables hasta
que el usuario abre *Crear y formatear particiones* de Windows — **la herramienta que esta app existe para
no tener que abrir**. La característica `#37` resolvía el flasheo de BIOS y dejaba a medias el disco.

Cinco tareas (`T5-01`…`T5-05`), **fuera del recuento de la auditoría**: las 40 de la Parte 2 no añaden
funcionalidad y estas sí. Va documentado como excepción explícita en los dos sitios donde se afirma lo
contrario, para que el ROADMAP no se contradiga consigo mismo.

Tres decisiones que quedan tomadas antes de escribir código:

- **No reabre el «gestor de particiones completo»**, que sigue fuera de alcance. Lo vetado ahí es
  **redimensionar, fusionar y mover** — operar sobre particiones **con datos**. Aquí `Clear-Disk` ya borra
  el disco entero de todos modos y solo cambia **cuántas particiones se crean sobre el vacío**. El criterio
  para saber si nos hemos salido queda escrito: **si hay que preservar datos, nos salimos.**
- **El plan de particiones se convierte en dato puro (`T5-01`) antes que nada.** Hoy el layout es un
  `long?` con dos significados («este tamaño» / «todo el disco»). Es la única parte probable **sin
  hardware**, y es donde el error duele: un plan mal calculado se descubre **con el disco ya borrado**.
- **Un fallo a mitad no se revierte solo (`T5-03`).** Con varias particiones existe un estado intermedio
  real; «deshacer» solo podría significar borrar otra vez, y esa no es una decisión que tomar por el
  usuario. Se informa de qué se creó y qué no, y se deja elegir.

`T5-04` (N particiones a gusto del usuario) queda **condicionada y desaconsejada como punto de partida**:
el motor lo admitiría, pero la interfaz no — la ventana es un diálogo de tarea de tamaño fijo por decisión
firme, y una tabla editable de filas variables no cabe ahí sin convertirla en otra cosa. Dos particiones
cubren el caso que originó esto; N es una hipótesis. Si entra, va en diálogo aparte.

---

### 2026-08-15 — Corte de la **v1.20.0** (Tier 3 completo)

`release.ps1 -Version 1.20.0 -UiTests -NotesFile docs\release-notes-1.20.0.md` desde terminal elevada con
la USB conectada. Cobertura de `Core/` **97.1 %** (367/378 líneas, mínimo 90 %), **398/398** unitarias y
**24/27** de UI — los 3 omitidos son los opt-in por variable de entorno, ninguno por falta de hardware,
y el resumen final lo dice con esas palabras desde el arreglo del corte anterior. Instalador 58.8 MB.

Verificado contra el release ya publicado: el asset `.sha256` se llama exactamente
`FormatDiskPro-1.20.0-setup.exe.sha256` y ocupa 96 bytes, y el `digest` que GitHub calculó del instalador
subido (`c547889e…`) coincide con el hash local — o sea, el hash que la app usará para verificar
corresponde al binario realmente publicado, no solo al que se compiló aquí. El `.csproj` conserva los
acentos de `<Authors>`/`<Copyright>` tras el bump.

---

### 2026-08-15 — Tier 3 completo: nueve arreglos de pulido, tres con matiz

Ninguno cambia lo que la app hace; varios cambian lo que la app **cuenta** cuando algo va mal.

- **`T3-01` — la exportación CSV ya no falla en silencio.** Un `catch { }` se tragaba cualquier error de
  escritura: el usuario elegía destino, no veía nada y se quedaba **creyendo que había exportado**. Ahora
  el diálogo muestra un `InfoBar` con el motivo real y se registra `EXPORT ERROR`. Va dentro del propio
  diálogo porque WinUI no permite abrir un `ContentDialog` sobre otro.
- **`T3-03` — `LoadHealthAsync` pasa a `async Task`** con descarte explícito. **El matiz que la tarea no
  decía:** al dejar de ser `async void`, una excepción ya no llega a la red global de `T0-01` — se
  quedaría en una `Task` que nadie observa, o sea, en silencio. Se añade un `catch` que pinta «no
  disponible» y registra `HEALTH ERROR`. Cambiar el tipo de retorno **es** cambiar dónde se manejan los
  errores; no verlo habría convertido un arreglo en una regresión callada.
- **`T3-04` — `AppSettings.Load` normaliza de verdad.** La documentación decía «se valida al cargar» y no
  era cierto: lo hacía la UI al construir sus ComboBox. Se arregla por la vía preferible —normalizar en
  `Load()`—, no reescribiendo el texto: así un `settings.json` con 0 pasadas deja de entrar vivo.
- **`T3-06` — `L.T(clave, args)` ya no lanza.** Un marcador mal escrito en una traducción tumbaba la
  pantalla que solo quería mostrar un texto; ahora devuelve la plantilla sin formatear, que además delata
  el fallo. Verificado por reversión.
- **`T3-08` — iconos decorativos fuera del árbol de automatización**, puesto **en el estilo** y no en cada
  icono: cualquier icono de sección futuro lo hereda, así que la corrección se queda puesta sola.
- **`T3-10` — el borrado seguro usa RNG criptográfico.** Para destruir datos da igual el origen de la
  aleatoriedad, pero «borrado seguro» invita a suponer otra cosa y el coste frente a la E/S es
  despreciable: sale más barato cumplir la expectativa que documentar por qué no se cumple.
- **`T3-02`** (sin `ToString()` por chunk en `ReinitDrive`, con solapamiento como el `carry` de
  `CheckDisk`), **`T3-07`** (formato del diccionario) y **`T3-09`**, abajo.

> **`T3-09` merece decirse entero.** La contraseña del certificado pasa a `SecureString` en los dos
> scripts, con alternativa por variable de entorno, y solo se descifra al construir los argumentos. **Lo
> que no arregla:** `signtool.exe` solo la acepta por `/p`, así que durante esa llamada sigue en **su**
> línea de comandos. Se elimina la exposición en el historial de PowerShell y en nuestros scripts, no la
> de signtool. La única vía sin exposición es el `.pfx` en el almacén con `-CertThumbprint`, y así queda
> documentado para el día que se firme.

Build 0/0, **398/398** unitarias (+9) y UI **24/27** con la USB. Auditoría **35/40** + 1 descartada:
**Tiers 0–3 cerrados**, solo queda el Tier 4 (fuera del alcance inmediato por definición).

---

### 2026-08-15 — Corte de la **v1.19.0** (Tier 2 completo)

`release.ps1 -Version 1.19.0 -UiTests` desde terminal elevada con la USB conectada. Primer corte que pasa
por la **puerta de cobertura** de `T2-04`: 97.1 % de `Core/` sobre un mínimo de 90 %, impreso al ejecutar
las pruebas y repetido en el resumen final. 389/389 unitarias, **24/27** de UI (los 3 omitidos, los
opt-in). Instalador 58.8 MB.

Verificado contra el release ya publicado, como en la 1.18.0: el `.sha256` se llama exactamente
`FormatDiskPro-1.19.0-setup.exe.sha256`, coincide con el instalador y ocupa 96 bytes; el `.csproj`
conserva los acentos tras el bump.

---

### 2026-08-15 — `T2-08`: `MainWindow` de 2.107 a 753 líneas (Tier 2 cerrado)

Dos extracciones **reales**, las que pedía la tarea, y luego una partición del resto:

1. **`Services/FormatProcess`** — `RunFormatVolumeAsync`/`RunFormatComAsync` salen de la ventana y se
   ponen junto a sus hermanos (`CheckDisk`, `ReinitDrive`, `SecureWipe`). Lanzar procesos y leer su salida
   no era responsabilidad de la UI, y era el único flujo de formateo sin un sitio propio donde mirarlo. El
   proceso en marcha se entrega por *callback* en vez de guardarse en el servicio —quien llama es quien lo
   cancela— y el progreso va por `IProgress<int>`, así que el servicio no toca ni la barra ni el estado.
2. **`UI/DeviceChangeWatcher`** — los cuatro `DllImport`, el delegado que hay que mantener vivo para que
   no lo recoja el GC y el *debounce* del `WM_DEVICECHANGE`, en su propia clase `IDisposable`.
3. **El resto, en `partial class` por asunto**: `.DriveInfo`, `.FormatOptions`, `.Operations`,
   `.HelpAndUpdates`, `.Preferences`.

**Resultado:** `MainWindow.xaml.cs` **2 107 → 753** líneas; el mayor de `UI/` es ahora
`MainWindow.Operations.cs` (509). Ninguno pasa de 800.

> **Lo que este cambio NO es.** Partir un archivo en `partial` no reduce el acoplamiento: sigue siendo la
> misma clase con el mismo estado compartido. Arregla exactamente lo que la tarea decía —encontrar algo
> en 2.000 líneas— y nada más. El rediseño de verdad (inyección de dependencias, `Services` no estáticos)
> es `T4-02`, **cerrado el 2026-08-16** — y que `MainWindow` siga siendo una clase grande con estado
> compartido tampoco lo arregló aquello: lo que arregló fue que los fallos de los servicios sean observables.

Verificado como toca para un refactor que promete no cambiar comportamiento: build 0/0, 389/389
unitarias y la suite de UI **24/27 con la USB conectada** — el mismo resultado, tras cada paso.

> **Trampa de PS 5.1, otra vez y en un sitio nuevo:** el script auxiliar del reparto se guardó **sin BOM**
> y PowerShell lo leyó con la página de códigos ANSI, rompiendo el parser en la primera tilde. Es la misma
> historia del `.csproj` (`#45`) y del `.trx` (`T2-12`). **Regla, ya sin excepciones: todo archivo de texto
> con acentos que vaya a leer PS 5.1 necesita BOM.**

Con esto **el Tier 2 queda cerrado**: auditoría 26/40 + 1 descartada, quedan T3 (9) y T4 (5).

---

### 2026-08-15 — `T2-04` y `T2-11`: cobertura medida y exigida, y los documentos que faltaban

**`T2-04`.** «390 pruebas» era un recuento, no una medida. Ahora `release.ps1` recoge cobertura en la
misma pasada de las unitarias y **aborta el corte** si `Core/` baja del mínimo. Primera medición:
**97.1 % de línea** (367/378).

Dos decisiones que explican el número:

- **El umbral es 90 %, deliberadamente por debajo de lo medido.** Un listón pegado al valor actual obliga
  a escribir pruebas de relleno para que el corte no rompa al añadir un método; lo que se quiere es un
  suelo que avise de una **regresión real**.
- **Solo se mide `Core/`.** Es la capa que puede probarse entera sin hardware, así que ahí un hueco es una
  decisión y no una limitación. Aplicar la misma vara a `Services/` y `UI/` premiaría escribir pruebas
  fáciles de lo que no importa — su red son los UI tests.

Verificado que la puerta cierra: con el umbral a 99 el corte aborta **y lista las cinco clases con menos
cobertura**, para que el mensaje diga qué hacer, no solo que algo va mal.

**`T2-11`.** Una herramienta GPLv3 que formatea discos, corre elevada y se auto-actualiza no publicaba
canal de reporte de vulnerabilidades ni guía de contribución. Ya hay `.github/SECURITY.md`,
`.github/CONTRIBUTING.md`, plantillas de issue (formularios YAML) y de PR, enlazadas desde el README.

> **`SECURITY.md` dice también lo que NO es una vulnerabilidad** —correr como administrador, el instalador
> sin firmar, el alcance *en tránsito* del SHA-256—, porque son decisiones documentadas y recibir reportes
> de ellas gasta el tiempo de todos. El canal es el reporte privado de GitHub: **no se publica ninguna
> dirección de correo**. `CONTRIBUTING.md` recoge lo que aquí falla si no lo sabes de antemano: terminal
> elevada, precondiciones que omiten en vez de fallar, los tests que vigilan la i18n y el contraste, y que
> **no se aceptan PRs con GitHub Actions**.

Auditoría **25/40** · T2: 1 abierta (`T2-08`).

---

### 2026-08-15 — `T2-03`: la verificación de capacidad ya no puede leer de la caché

*Verificar capacidad* escribía con `WriteThrough` pero **releía con E/S normal**, así que la caché de
archivos de Windows podía servir los bloques desde RAM — justo lo que la prueba existe para descartar. En
una USB falsa **pequeña** (menor que la RAM libre) eso podía dar un **falso OK**: el peor resultado
posible aquí, decirle a alguien que su unidad es auténtica cuando no lo es.

La relectura usa ahora `FILE_FLAG_NO_BUFFERING` con `RandomAccess.ReadAsync` y buffer alineado (el mismo
patrón que ya tenía `BenchmarkRunner`). El objetivo se redondea a la baja al sector, y con eso todos los
tamaños de archivo —y todos los bloques, incluido el último— quedan alineados, que es lo que ese modo
exige. Se sacrifican menos de 4 KB del margen de seguridad de 64 MB.

> **Cómo se demuestra que el flag está realmente activo**, que era la parte que la tarea dejaba como
> «razonado, no medido»: sustituyendo el buffer alineado por uno desplazado **un byte**, las pruebas
> fallan con `IOException: El parámetro no es correcto` — el error que Windows devuelve a la E/S sin
> caché ante un buffer desalineado. **Con la caché de por medio esa desalineación sería irrelevante y las
> pruebas pasarían.** Ese contraste es la prueba: no hay API que diga «este bloque vino del medio», pero
> sí una que solo se comporta así cuando no hay caché.

Probado además sobre la **USB real** (64 MB en `D:`, 7 s): un disco fijo y un medio extraíble no tienen
por qué anunciar la misma geometría, y este modo no degrada —falla— si no cuadra. Esa prueba se **omite**
salvo que se defina `FORMATDISKPRO_VERIFY_DRIVE=<letra>`, para que las unitarias sigan corriendo en
cualquier máquina.

Build 0/0, **390/390** (389 + la de unidad real). Auditoría **23/40** · T2: 3 abiertas.

---

### 2026-08-15 — Corte de la **v1.18.0**, y la verificación del actualizador comprobada en producción

Corte con `release.ps1 -Version 1.18.0 -UiTests` desde terminal elevada y con la USB conectada: 388/388
unitarias, **24/27** de UI (los 3 omitidos son los opt-in, que un corte nunca debe ejecutar) y el resumen
del corte lo dijo por escrito — que es lo que `T2-12` existía para arreglar.

**Lo nuevo se comprobó contra el release ya publicado, no solo en pruebas:** el asset del hash se llama
`FormatDiskPro-1.18.0-setup.exe.sha256` —el nombre exacto que ahora busca el emparejamiento de `T2-06`—,
coincide con el instalador subido y ocupa **96 bytes**, holgadamente por debajo del tope de 512 de
`T2-07`. Y el `.csproj` conserva los acentos de `<Authors>`/`<Copyright>` tras el bump, que es el fallo
del `#45` y conviene seguir mirando en cada corte.

**Detalle de proceso, por honestidad:** el retoque final de `release.ps1` (el consejo del resumen de UI)
no llegó a tener commit propio —falló el `git commit` que lo intentaba— y acabó dentro del commit
`release: v1.18.0`, que `release.ps1` crea con `git add -u`. Sin consecuencias, pero explica por qué ese
commit toca `release.ps1` además del `.csproj`.

---

### 2026-08-15 — `T2-01`/`T2-02`: la app se puede seguir sin verla

Las operaciones de esta app duran minutos u horas y **nada mueve el foco** mientras avanzan: con un lector
de pantalla no había forma de saber si el formateo progresaba, había fallado o había terminado. Y el error
de la etiqueta aparecía debajo del campo sin ninguna relación programática con él, así que desde el propio
cuadro de texto no se podía averiguar por qué no dejaba continuar.

- **`StatusText` es región activa `Polite`** y `MainWindow.AnnounceStatus` emite una notificación UIA en
  los **hitos**: inicio de las cinco operaciones y —en un solo sitio, `EndOperation`— fin, error o
  cancelación, con `ActionCompleted`/`ActionAborted` según cómo haya terminado.
- **`LabelErrorText` es `Assertive`** y `VolumeLabelBox` lo referencia con `DescribedBy`. El vínculo se
  hace en code-behind: en WinUI esa propiedad es una **colección** y no admite `x:Reference` desde XAML.

> **La decisión que importa aquí es qué NO se anuncia.** Una notificación por cada tick de porcentaje
> convertiría el lector de pantalla en ruido continuo durante una hora de formateo — peor que el silencio
> del que se partía. El avance queda en la región activa, que se consulta cuando se quiere; se anuncia
> solo lo que no puede perderse. Por lo mismo, el error de etiqueta se anuncia al **aparecer o cambiar**,
> no en cada pulsación de tecla.

**Dos cosas las enseñó la prueba al fallar, no el razonamiento:**

1. **Un elemento `Collapsed` no existe en el árbol de UI Automation.** Buscar `LabelErrorText` con la
   etiqueta válida no devuelve nada: hay que provocar el error primero. Que el vínculo solo exista
   mientras el mensaje se muestra es lo correcto, pero una prueba escrita sin saberlo habría fallado sin
   que nada estuviera mal.
2. **Los UI tests lanzan el `.exe` de `bin`, no el XAML del repo.** Al verificar por reversión, quitar el
   `LiveSetting` del XAML **sin recompilar** deja la prueba en verde. Casi cuela como «verificado»: la
   reversión solo prueba algo si se reconstruye el binario que la prueba va a ejecutar.

**Y un arreglo de la suite que este equipo destapó:** las cuatro pruebas de la tarjeta de opciones
necesitan alguna unidad que no sea la de sistema, y en una máquina de un solo disco **fallaban** con un
error que habla del hardware, no de la app. Ahora hay `NonSystemDriveFact` y se **omiten**, que es la
regla que este proyecto ya se dio con `TestDriveFact`.

Suite de UI **27** (era 25). Sin USB ni segundo disco: 15 pasan / 12 se omiten / 0 fallan. **Con la USB
`utilidades` conectada (2026-08-15): 24 pasan / 3 se omiten / 0 fallan**, y los 3 omitidos son
exactamente los opt-in (2 de *yank* + 1 destructivo) que un corte de release nunca debe ejecutar.
Build 0/0, unitarias 388/388. Auditoría **22/40** · T2: 4 abiertas.

---

### 2026-08-15 — `T2-09`: el historial deja de crecer sin fin (y `T2-10` queda descartada)

**`T2-09` — rotación con dos generaciones.** `history.log` solo crecía y el visor lo interpreta **entero**
en memoria cada vez que se abre. Con una entrada por operación eso tarda años en notarse; con las trazas
de pila completas que registra `T0-01` desde agosto, no tanto. Ahora rota a los 2 MB: la política vive
pura en `Core/HistoryRotation` y el movimiento de archivos en `History`.

Lo que decide si esto es una mejora o un regalo envenenado no es el umbral, es qué ve el usuario después:

> El visor lee **las dos** generaciones, la vieja primero. Rotando solo el archivo activo, la entrada que
> provoca la rotación dejaría a alguien mirando un historial casi vacío justo después de una operación —
> y en un registro de auditoría eso no se lee como «ha rotado», se lee como **«he perdido mis datos»**.

Por lo mismo, *Borrar el historial* se lleva también `history.1.log`: si no, limpiar dejaría 2 MB a la
vista. Y se rota **antes** de escribir, no después, para que el archivo activo nunca quede por encima del
umbral. Verificado por reversión: sin la rotación fallan dos de las seis pruebas del comportamiento.

`SettingsBackup` (UI tests) respalda ahora también `history.1.log`. Hacen falta 2 MB para que aparezca,
pero ese respaldo existe justamente para no dejar rastro en el `%AppData%` real, y una excepción «que casi
nunca pasa» es como se cuelan las cosas.

**`T2-10` — CI de solo unitarias: implementada y revertida el mismo día.** Se llegó a escribir el workflow
(`windows-latest`, build sin advertencias + `dotnet test` sobre la solución, con los comandos verificados
en local) y se **descartó por decisión del mantenedor**: el testing de este proyecto es **local**, sin
GitHub Actions ni workflows de ningún tipo. Queda como **decisión cerrada** en §4, no como tarea aplazada.

El argumento que la cierra es el mismo que hace especial a este repositorio: la prueba que vale aquí es la
que **ejerce el binario real** contra hardware real —elevación y USB de pruebas—, y eso no cabe en un
runner. Un ✅ verde que solo cubre los unitarios afirma más de lo que prueba, que es literalmente el
problema que `T2-12` acababa de corregir en el otro extremo del proceso. La puerta de calidad es
`release.ps1 -UiTests` desde terminal elevada, y ya existe.

Build 0/0, **388/388** (375 → +13). Auditoría **20/40** + 1 descartada · T2: 6 abiertas.

---

### 2026-08-15 — `T2-12`, `T2-06`, `T2-07`: que el corte no pueda mentir, y el hash correcto

Tres tareas del Tier 2, todas en el mismo punto del proyecto: **lo que se verifica antes de ejecutar como
administrador**, y **lo que un corte de release afirma haber probado**.

**`T2-12` — el corte ya dice qué cobertura NO llevó.** `release.ps1` pide el logger `trx` y lo lee: la
salida de consola de `dotnet test` no lista los tests omitidos ni su motivo, el `.trx` sí. El resumen sale
**dos veces** —al terminar las pruebas y en el bloque final, que es el único que se mira cuando todo ha
ido bien— con el nombre de cada omitido y el motivo que declara su propio atributo:

```
[!] UI tests: 17/25 — 8 OMITIDOS por precondición ausente:
      - …CheckDisk_ScanOnly_CompletesForTestDrive: Requiere la USB de pruebas conectada (…'utilidades').
      - …Benchmark_DriveDisappears_…: Define FORMATDISKPRO_ALLOW_YANK=1 antes de 'dotnet test'…
```

Omitir en vez de fallar **sigue siendo lo correcto** —un corte no debe caer por falta de hardware—; lo que
faltaba era el rastro. Sin él, los cortes de la v1.15.2 y la v1.16.0 salieron «en verde» con
`CheckDisk_ScanOnly_CompletesForTestDrive` roto y omitido. Verificado sobre un `.trx` **real** (la corrida
no elevada produce los 8 omitidos), no sobre uno inventado.

> **Y la trampa de PS 5.1 volvió a aparecer, en otro archivo.** El `.trx` se lee con `XmlDocument.Load`,
> no con `[xml](Get-Content -Raw)`: eso último lee con la página de códigos ANSI y destroza los acentos de
> los motivos, que están en español. Es exactamente el fallo del `.csproj` (`#45`) en otro sitio.

**`T2-06` — el hash que se comprueba es el del instalador que se va a ejecutar.** `ParseRelease` se
quedaba con el **último** asset terminado en `.sha256`; ahora busca `<nombre-del-exe>.sha256`. Si no está,
`ChecksumUrl` queda vacía y la actualización se rechaza por no verificable. El síntoma del defecto no era
un agujero sino lo contrario —una actualización legítima rechazada siempre, porque el hash de otro archivo
nunca coincide—, pero la corrección es la misma. Verificado por reversión: con la lógica vieja, dos de las
cinco pruebas nuevas fallan.

**`T2-07` — leer un checksum no puede costar memoria arbitraria.** `GetStringAsync` leía la respuesta
entera; ahora hay un tope de 512 bytes (un hash en hex ocupa 64, y `build-installer.ps1` escribe unos 110
con el nombre). Se comprueban las dos cosas, la cabecera `Content-Length` y el flujo real, porque un
servidor puede mentir en la primera o no enviarla. **Lo que hace discriminante a la prueba** es que el hash
servido es el **correcto** —va al principio, seguido de 64 KB de relleno—: lo que rechaza la respuesta es
su tamaño, no la comparación. Con `GetStringAsync`, pasaba.

**Detalle que dice algo del proyecto:** el barrido de `T1-07` **paró este cambio dos veces**. La primera,
por un `Dictionary<string,string>` legítimo (nombre de asset → URL, sin texto de usuario); la segunda, por
mencionarlo en un **comentario**. Se resolvió sin abrir excepciones en la red: una lista de tuplas, que
para tres assets es igual de buena. Un barrido que se pasa de celoso se corrige cambiando el código, no
debilitando la red.

Build 0/0, **375/375** (369 → +6). Auditoría **19/40** · T2: 8 abiertas.

---

### 2026-08-14 — `T1-02`: el formato completo se colgaba en medio Windows, y no era el único idioma

`format.com` hace dos preguntas por consola, y se les respondía escribiendo `"Y"` y `"S"` en la entrada
estándar — las teclas de un Windows **inglés y español**. En uno francés (`O`) o alemán (`J`) ninguna
coincide y **el proceso se queda esperando entrada con el formato a medias**.

**Reproducido, no deducido.** La tarea llevaba la nota «pendiente de verificación: necesita un Windows no
ES/EN». No hacía falta: basta con **no escribir nada**, que es lo que efectivamente ocurre cuando la tecla
no coincide. Sobre un VHD de 400 MB:

| | Resultado |
|---|---|
| Con `/Y` | Termina en **0.1 s**, exit 0, «Formato completado» |
| Sin `/Y` | **Se cuelga** esperando una tecla que nunca llega |

`/Y` suprime las **dos** preguntas —también la de la etiqueta de volumen, que la nota original dejaba como
duda abierta, porque asume etiqueta vacía si no se pasa `/V:`— y fuerza el desmontaje si hace falta. Ya no
se escribe nada por stdin: se **cierra**, para que una build hipotética sin `/Y` falle rápido en vez de
colgarse indefinidamente.

**Y había un segundo fallo de idioma en la misma ruta que la tarea no mencionaba.** `ExtractPercent`
reconocía `%`, `percent` y `por ciento`: el mismo par inglés/español, en el mismo archivo. Como
`format.com` escribe la **palabra** y no el símbolo, en un Windows francés o italiano la barra de progreso
se quedaba clavada en 0 durante todo un formato completo **sin que nada fallara**. Añadidos `por cento`
(pt), `per cento` (it), `pour cent` (fr) y `Prozent` (de).

> **El matiz que hace esto distinto de `T1-05`/`T1-06`:** aquí el idioma que manda es el de **Windows**, no
> el de la app. Alguien puede tener FormatDiskPro en español sobre un Windows alemán. Por eso esta lista es
> incompleta por naturaleza y no puede dejar de serlo — lo que importa es **cómo se degrada**: sin
> coincidencia, barra parada y formato correcto, nunca un fallo. Hay test de esa degradación.

**Residuo honesto:** las cuatro palabras nuevas son traducciones, **no observaciones**. No se pudo ver la
salida real de un `format.com` no español: un VHD montado se anuncia como aprovisionamiento fino y Windows
rechaza el formato **completo** sobre él, que es el único que imprime porcentajes.

**Y el barrido de `T1-07` no habría cazado esto**: busca `Dictionary<string,string>` y esto es un
`[GeneratedRegex]`. La red tiene un agujero con la forma de lo que no se imaginó — que es, otra vez, el
patrón de siempre.

Build 0/0, **369/369** (359 → +10). Auditoría **16/40**: **Tiers 0 y 1 cerrados**.

---

### 2026-08-14 — `T2-13`: los `catch` ejecutados de verdad, quitando la unidad

`T2-05` dejó bajo test lo que los `catch` de `T0-02` **escriben**; faltaba lo otro: que lleguen a
**ejecutarse**. `DriveYank.ForceDismount` desmonta el volumen a la fuerza a mitad de la operación e
invalida los handles abiertos de la app — el efecto exacto de que le quiten la USB de las manos. Dos
pruebas: *Verificar capacidad* y *Benchmark*.

**Lo que hace que la prueba valga algo es una sola línea:**

```csharp
Assert.DoesNotContain("CRASH:", added);
```

Sin ella, la prueba pasaría **igual con los `catch` borrados**: la red global de `T0-01` también deja la
app viva y también muestra un diálogo, así que desde fuera las dos rutas son indistinguibles. Lo único
que las separa es qué línea aparece en el historial. Verificado por reversión: con el `catch` de `VERIFY`
neutralizado, el historial recibe `2026-08-14 12:03:31⇥CRASH: System.IO.IOException…` y la prueba falla
diciendo qué faltaba. Ese mismo experimento valida **las dos** redes a la vez, que hasta hoy solo estaban
razonadas.

**`Set-Disk -IsOffline` no sirve para desmontar una USB** — Windows lo rechaza de plano: *«Removable media
cannot be set to offline»*. Poner un disco offline es una operación de discos fijos. Lo que sí vale es
`FSCTL_DISMOUNT_VOLUME` sobre el volumen: es lo que hace Windows al "quitar hardware de forma segura" y,
a diferencia de `FSCTL_LOCK_VOLUME`, **no falla cuando hay archivos abiertos** — los invalida, que es
justo lo que hace falta aquí. Se remonta solo al primer acceso, sin tocar el cable.

**Trampa aprendida (costó una corrida entera):** una prueba que aborta a mitad deja la app **ocupada**, y
la siguiente falla con un `DrivePicker` vacío — un síntoma que no se parece en nada a su causa. El
`finally` ahora devuelve la app a estado ocioso además de remontar la unidad.

**Otra:** no vale esperar a que `StartButton` se rehabilite para dar la app por ociosa. En
`SetFormEnabled` es `enabled && !_isDriveProtected`, y al desaparecer la USB el selector puede caer en
`C:`, que está protegida: quedaría deshabilitado con la app perfectamente ociosa. Se miran `DrivePicker`
y `MnuTools`.

Opt-in propio `FORMATDISKPRO_ALLOW_YANK=1`, separado del destructivo: no borra datos, pero hace
desaparecer una unidad del sistema.

Suite de UI **25/25** con hardware (era 23). Auditoría **15/40**.

---

### 2026-08-13 — `T2-05`: los caminos de error, y el defecto que escondían

Ninguna prueba cubría qué pasa cuando una operación **falla** — que era la causa raíz de `T0-02`. Dos
costuras nuevas, y un hallazgo que justifica la tarea entera.

**`CapacityVerifier.RunInAsync(dir, target, …, afterWriteAsync)`.** El motor de la verificación, sin la
unidad. El parámetro `afterWriteAsync` corre entre la fase de escritura y la de lectura, y permite
**corromper lo escrito**: es decir, reproducir una unidad falsificada sin tener una. Hasta hoy, lo único
que ejercitaba la detección era un test de UI de 57 minutos sobre una USB **auténtica** — o sea, sobre el
único caso en el que la detección nunca se dispara. Ahora hay pruebas de bloque corrompido
(`mismatch@1`), lectura corta (`short-read@1`), cancelación con limpieza y unidad no lista, todas en
menos de medio segundo y sin tocar ninguna unidad real. Verificado por reversión: anular la comparación
de patrones hace fallar la prueba del bloque corrompido.

**`Core/OperationFailure.LogLine`**, extraído de `MainWindow.ReportOperationErrorAsync`, para poder
comprobar que lo que escribe el camino de error **se vuelve a leer bien** por el visor de historial.

Y al recorrerlo de punta a punta apareció un defecto real, registrado como `T3-11`:

> `history.log` es un formato de **una entrada por línea**, pero `History.Log` escribía el texto recibido
> tal cual. `T0-01` —de esta misma mañana— había empezado a registrar `e.Exception` **completa, con su
> traza de pila**. Cada caída partía el historial en **decenas de entradas fantasma** sin marca de tiempo,
> categoría `Other` y resultado `Info`: un fallo disfrazado de información, justo en el registro que uno
> consulta cuando algo ha ido mal.

Lo arregla `HistoryEntry.SanitizeDetail` (pura), que aplana los saltos a `⏎`. No recorta la longitud a
propósito: en una entrada `CRASH:` la traza es justo lo que se quiere leer.

**La lección, que es la misma de siempre en este proyecto:** el arreglo de la mañana introdujo el defecto
de la tarde, y sobrevivió medio día porque el camino que tocaba no tenía pruebas. Un `catch` que nadie ha
visto ejecutarse no es una red.

**Lo que sigue sin estar verificado:** que esos `catch` *lleguen a ejecutarse*. Bajo test está lo que
escriben, no que se disparen. Eso solo lo demuestra desconectar la USB a mitad de cada operación.

Build 0/0, **359/359** (330 → +29). Auditoría **14/39**.

---

### 2026-08-13 — `T1-08`: la firma Authenticode dejó de ser un atajo

Se reafirmó **no firmar** (#13). Esa decisión, que parecía dejar `T1-08` en endurecimiento preventivo, es
justo lo que convierte el hallazgo en real:

> `VerifyAuthenticodeSignature` responde a «¿lo firmó **alguien** en quien Windows confía?», no a «¿lo
> firmamos **nosotros**?». Y `VerifyInstallerAsync` devolvía sin mirar el hash si la firma pasaba. Como el
> proyecto **no firma**, esa rama solo podía activarse sobre un binario que no produjimos: era un modo de
> saltarse el SHA-256 usando cualquier ejecutable firmado por cualquier CA de confianza — y al otro lado
> está `LaunchInstaller` ejecutando **como administrador**.

El SHA-256 pasa a ser obligatorio. El atajo queda tras `UpdateService.SignsItsInstallers` (`static readonly
bool`, no `const`: como constante el compilador pliega el `if` y salta CS0162, y aquí se compila a 0
advertencias). Se aplicó además el contenido original de la tarea: `WTD_REVOKE_WHOLECHAIN` +
`WTD_CACHE_ONLY_URL_RETRIEVAL`, para que un certificado revocado deje de valer **sin** que la validación
pase a depender de tener red.

**Verificado con un binario firmado de verdad**, no con una simulación: `dotnet.exe` tiene firma Authenticode
*embebida* y válida, y está garantizado presente porque las pruebas corren sobre él. (Los binarios de Windows
como `explorer.exe` no sirven: su firma es de *catálogo* y `WinVerifyTrust` con `WTD_CHOICE_FILE` no la ve.)
Dos pruebas nuevas: que una firma legítima siga aceptándose —el riesgo real de tocar los flags de
revocación— y que un instalador firmado sin hash publicado se rechace igual. Con el flag en `true` esta
última falla: se aceptaba un ejecutable de Microsoft como si fuera nuestro instalador. Ese es el agujero.

También se corrigió el mensaje `update.unverifiable` en los cinco idiomas: explicaba una regla («no está
firmado y…») que ya no existe.

Build 0/0, **330/330** (327 → +3). Auditoría **11/38**; del Tier 1 solo queda `T1-02`, que necesita un
Windows no ES/EN para verificarse.

---

### 2026-08-13 — Auditoría: `T1-06`/`T1-07`, la i18n medida desde el otro lado

Los nombres de los cinco presets integrados estaban **fijos en español** dentro de `Core/Presets.cs` y el
menú los pintaba tal cual en los cinco idiomas. `FormatPreset` gana un `NameKey` opcional y `Presets`
un `DisplayName` que lo resuelve con `L.T`; los presets del usuario lo dejan en `null` y se muestran
literales, que es lo correcto: ese nombre lo escribió una persona. **Detalle que importa:** la detección
de duplicados de `PresetsDialog` compara ahora contra el nombre **mostrado**, así que con la app en
inglés sigue sin poder crearse un preset propio llamado «Windows data disk». `NameKey` es opcional, de
modo que los ajustes guardados por versiones anteriores se leen sin migración.

Lo relevante no es la traducción, es **por qué la suite no la echaba de menos**. Este es el mismo patrón
que ya apareció en `T1-03`, `T1-04` y `T1-05`, y conviene tenerlo escrito:

> `EveryEntry_HasFiveNonEmptyTranslations` comprobaba que **lo registrado** estuviera traducido, no que
> **lo mostrado** estuviera registrado. No faltaba un test: había un test que cubría menos de lo que su
> nombre sugiere.

`tests/FormatDiskPro.Tests/LocalizationCoverageTests.cs` ataca el lado que faltaba: recorre el **código
fuente** de `src/FormatDiskPro/` buscando `Dictionary<string, string>` fuera de `Localization/` — la forma
exacta que tomó el fallo de `T1-05` (`FsDescEs`/`FsDescEn` dentro de `MainWindow`). Incluye dos guardas
contra el propio barrido: aborta si encuentra menos de 15 fuentes, y comprueba que el patrón sigue
reconociendo el diccionario real de `Localization.cs` — *un barrido que ha dejado de detectar nada no se
distingue de uno limpio*.

Verificado por reversión, no por confianza: quitar la clave a un preset y reintroducir un
`Dictionary<string,string>` en `MainWindow` hace fallar cuatro pruebas, cada una nombrando al culpable
con fichero y línea.

También se añadió `LanguageCollection`: `L.Current` es estado estático global y ya son tres clases las que
lo mueven; xUnit paraleliza entre colecciones, así que sin serializarlas entre sí habría fallos
intermitentes. Solo se frenan esas tres.

Build 0/0, **327/327** (321 → +6). Auditoría **10/38**; queda `T1-08` para cerrar el Tier 1.

---

### 2026-08-13 — Auditoría: primera tanda (T0 completo + 4 de T1)

Seis tareas del backlog. Build 0/0, **314/314** unitarias (289 → +25). Sin cambios funcionales: nada de
lo que hace la app cambia, solo deja de romperse y empieza a decirlo en cinco idiomas.

- **T0-01 · La app ya no muere en silencio.** `App.OnLaunched` engancha `UnhandledException`: registra
  `CRASH:` en el historial, fija `Handled` y muestra un diálogo. **El orden importa** — `e.Handled` se
  fija de forma síncrona antes del primer `await`, porque en un `async void` el método "vuelve" ahí y es
  entonces cuando WinUI lee la propiedad.
- **T0-02 · Cada operación cuenta su propio fallo.** Los cuatro handlers que solo tenían `try/finally`
  (verificar capacidad, chkdsk, reinicializar, benchmark) ya capturan, vía el helper compartido
  `MainWindow.ReportOperationErrorAsync`. El benchmark guarda la excepción y la trata **después** del
  `finally`, para respetar su decisión previa de cerrar la operación antes de abrir un modal.
- **T1-01 · `Core/DriveLetter` (nuevo archivo).** `IsSystemDrive` comparaba con `char.ToUpper`, sensible a
  la cultura: en turco `ToUpper('i')` es `'İ'`, así que la unidad `I:` habría dejado de reconocerse como
  disco de sistema y la guarda no se habría activado. La comparación se aisló en `Core` para poder
  probarla; el test fija `tr-TR` y **primero comprueba que la cultura se comporta como se afirma**, para
  no seguir en verde si .NET cambia ese detalle.
- **T1-03 · Contraste.** El gris de «Cancelado» en el historial pasa de `#868686` (3.52:1 sobre `#FBFBFB`)
  a `#6E6E6E` (**4.93:1**). No se eligió el primer valor que cumple (`#747474`, 4.52:1): demasiado justo.
  El tema oscuro ya cumplía. **Sigue sin test hasta `T1-04`** — ver abajo.
- **T1-05 · `fs.desc.*`.** Las descripciones de sistema de archivos eran dos diccionarios ES/EN dentro de
  `MainWindow`; PT/FR/IT veían inglés. Ahora viven en `Localization`, y el test nuevo no se conforma con
  que existan: **exige que PT/FR/IT difieran del inglés**, que es la forma exacta en que el fallo se
  camuflaba.
- **T1-09 · `UpdateService.SafeAssetFileName`.** El nombre de asset de GitHub iba directo a
  `Path.Combine`, que descarta su primer argumento ante una ruta absoluta. Ahora se sanea con
  `Path.GetFileName` + validación, con reserva al nombre versionado.

**La lección que dejan T1-01, T1-05 y T1-03 juntas:** las tres eran fallos que el proyecto **ya tenía
mecanismos para cazar** —tests de cultura, test de completitud de traducciones, test de contraste— y que
pasaron igualmente, porque cada mecanismo cubría un poco menos de lo que parecía. El patrón a vigilar no es
«falta un test», es «hay un test que cubre menos de lo que su nombre sugiere».

### 2026-08-13 — Suite de UI completa sobre hardware real (23/23) y un test roto desde la v1.15.2

Primera vez que se ejecutan **los 23 UI tests sin omitir ninguno**: USB de pruebas conectada
(`utilidades`, 29.3 GB, extraíble) **y** `FORMATDISKPRO_ALLOW_DESTRUCTIVE=1`. Todos en verde.

- **Ciclo destructivo completo, 59 s:** Formatear (NTFS, rápido) → Reinicializar (`Clear-Disk` +
  partición + formato). La letra se mantuvo en `G:` y el `finally` devolvió la etiqueta a `utilidades`.
  El paso 3 (FAT32 pequeña) se omitió **correctamente**: exige ≥ 32 GiB y la unidad son 29.3.
- **`VerifyCapacity`, 57 min 3 s:** escribió y releyó los ~29 GB libres. Unos 58 GB de E/S a ~17 MB/s de
  media. Deja la unidad limpia (borra sus propios archivos).
  - *Dato para el `T2-03` del roadmap:* a este tamaño la caché del SO no puede falsear la relectura —29 GB
    no caben en RAM—, así que esta corrida **no valida ni invalida** aquella preocupación. Confirma su
    alcance: el riesgo se concentra en unidades pequeñas, no aquí.

**El hallazgo de la sesión: `CheckDisk_ScanOnly_CompletesForTestDrive` llevaba roto desde la v1.15.2.**
El pase de UX de aquella versión apiló los botones del diálogo de chkdsk dentro del `Content` (para que
«Comprobar y reparar» no se truncara), lo que **eliminó el `PrimaryButton`** que el test invocaba. El test
no se actualizó.

Lo grave no es el test: es que **los cortes de v1.15.2 y v1.16.0 salieron en verde con él roto**. Sin la
USB, ese test se *omite*, y en el resumen de `dotnet test` «omitido» y «correcto» se distinguen mal. La
decisión de omitir en vez de fallar **sigue siendo la correcta** —un corte no debe caer por falta de
hardware—, pero no dejaba rastro de qué cobertura se estaba sacrificando. Anotado como **`T2-12`**.

Arreglado dando `AutomationId` explícito a los dos botones (`CheckScanButton` / `CheckRepairButton`): al
crearse en código no hay `x:Name` del que WinUI lo derive, así que quedaban fuera del alcance de los UI
tests. **Regla:** todo control creado en code-behind que un UI test deba tocar necesita su `AutomationId`
a mano.

### 2026-08-13 — Auditoría: `T1-04`, el inventario de color medido

Cierra el hueco que `T1-03` dejó abierto: aquel arregló un color **a mano y sin test**, que es literalmente
cómo se coló el fallo. Build 0/0, **321/321** (+7).

- **`SeverityPalette` pasa de ser una función a ser un inventario.** `All()` enumera *todos* los colores
  semánticos —severidad S.M.A.R.T., resultado del historial, texto primario, relleno neutro— en ambos temas
  y con el **umbral que le toca a cada uno**: 4.5:1 para texto, 3:1 para objeto gráfico (`ContrastRequirement`).
  El barrido recorre el inventario, no una función, así que **añadir un color es ponerlo bajo test**.
- **`Flatten` compone el alfa antes de medir.** El color de texto claro es `#E4000000` (el token real de
  Fluent, con alfa). La fórmula de WCAG solo está definida entre colores opacos: medirlo sin componerlo
  sobre el fondo habría dado un número que no corresponde a lo que se ve.
- **Los cuatro consumidores delegan:** `HistoryDialog.ColorFor`, `MainWindow.ProtectedColor`, `DriveBrush` y
  `CapacityBrush`. Correcto y fallido del historial **reutilizan** el verde y el rojo de severidad —mismo
  significado, mismo color— y hay un test que lo fija, para que no diverjan.
- **Verificado que el barrido caza el fallo original:** revirtiendo el gris a `#868686`, el test falla y
  nombra el color. Un test que nunca has visto fallar no es una red, es una suposición.

**`T3-05` se cierra aquí también**, porque este cambio dejó desactualizada la documentación que describía:
`AppTheme.xaml` decía «No hay colores hardcodeados» (nunca fue cierto) y §4 contaba la barra de capacidad
como una excepción aparte de `SeverityPalette` (ahora vive dentro). Ambos textos enumeran ya las dos únicas
excepciones reales.

> **Siguiente paso recomendado: `T1-07`** — el mismo movimiento, aplicado a la i18n.
> `EveryEntry_HasFiveNonEmptyTranslations` sigue recorriendo solo `L.Map`, así que un texto de UI nuevo
> fuera de `Localization` volvería a pasar desapercibido: es exactamente como entraron `T1-05` y `T1-06`.

### 2026-08-13 — Auditoría técnica transversal (sin cambios de código)

Revisión de las 12 áreas del repositorio completo contra el código real, no contra la documentación.
Resultado en [`ROADMAP.md`](ROADMAP.md) **Parte 2**: **37 tareas**, ninguna de ellas funcionalidad nueva.
El archivo se **fusionó de forma aditiva** — la Parte 1 (historial de tiers 1–9) se conserva íntegra.

Lo que hay que saber sin abrir el roadmap:

- **T0 — la app puede morir en mitad de una operación.** Cuatro handlers `async void`
  (`MnuVerify_Click`, `MnuCheck_Click`, `MnuReinit_Click`, `MnuBenchmark_Click`) usan `try/finally`
  **sin `catch`**, y ni `CapacityVerifier` ni `CheckDisk` atrapan `IOException`/`Win32Exception`. En
  Release **no hay red de seguridad**: el único `UnhandledException` del proyecto lo genera WinUI bajo
  `#if DEBUG`. Un USB que se desconecta durante *Verificar capacidad* —el escenario que la herramienta
  existe para provocar— termina el proceso sin aviso ni línea de historial.
- **La barrera de i18n que el test no veía.** `EveryEntry_HasFiveNonEmptyTranslations` solo recorre
  `L.Map`, así que daba verde mientras **PT/FR/IT veían en inglés** las descripciones de sistema de
  archivos (`MainWindow.FsDescEs/FsDescEn`) y **en español** los nombres de los presets integrados
  (`Presets.All`). Un test que solo cubre parte del texto de UI produce confianza, no cobertura.
- **El mismo patrón, en color.** Los RGB de severidad están duplicados a mano en `HistoryDialog.ColorFor`
  y `MainWindow.ProtectedColor`, fuera de `SeverityPalette` — que es justo lo que mide
  `SeverityPaletteTests`. Por ahí se coló un `#868686` sobre `#FBFBFB` a **3.52:1**, por debajo del 4.5:1
  de WCAG AA. Medido, no estimado.
- **`IsSystemDrive` usa `char.ToUpper` sensible a la cultura** en una guarda contra formatear el disco de
  sistema (bug de la I turca). `ParseDriveLetter` y `CheckDisk`, al lado, ya usan `ToUpperInvariant`.
- **Confirmado sano:** el blindaje anti-inyección de PowerShell (sin una sola ruta explotable), la
  ausencia de secretos, la disciplina de versión exacta del SDK y el pipeline de `release.ps1`. Las
  **289/289** unitarias se ejecutaron y pasan (224 ms).

### 2026-07-20 — Pase de refinamiento UX/UI dirigido por capturas — **v1.15.2**

Revisión visual de **cada** pantalla/diálogo conduciendo la app real por UI Automation. Se amplió
`tools/capture-screenshots.ps1` con un **modo galería** (`-Gallery`, `-Only`): en una corrida fotografía
los 12 diálogos/estados en claro **y** oscuro (proceso fresco por toma → una toma que falle no arrastra al
resto). Salida en `docs/screenshots/gallery/` (gitignorada, no toca las 3 del README). Encontró y arregló:

- **P1 — chkdsk: "Comprobar y reparar" salía truncado** a "Comprobar y repar" (3 botones no cabían en la
  fila del `ContentDialog`, en **ambos** temas). Ahora las dos acciones van **apiladas a todo el ancho**
  dentro del `Content` (nunca truncan, en ningún idioma); *Solo comprobar* queda enfocado para preservar
  Enter. `MainWindow.MnuCheck_Click`.
- **P2 — barra de capacidad en color de acento** → falsa alarma (roja con el disco sano). Ahora **semántica**
  por ocupación (neutro/ámbar/rojo). Ver §4 *Otros*.
- **P3 — S.M.A.R.T. "Velocidad: SSD"** era ambiguo (suena a MB/s) y redundante con "Tipo de medio". Renombrado
  a **"Velocidad de rotación"** en ES/PT/IT (EN/FR ya lo eran). `health.spindle`.
- **Fix de calidad de captura:** las 3 imágenes del README tenían una tira multicolor de 1px a la izquierda
  (columna del borde semitransparente del DWM). `Save-WindowPng` recorta 1px en los 4 lados. README
  regenerado limpio.

**Lección (costó un round trip):** las capturas deben hacerse contra el **publish self-contained**, no contra
`dotnet build`. Ver §4 *Otros*. **Falsos positivos descartados:** "JimÃ©nez" en Novedades (el changelog cita
el string corrupto del #45, intencional) y el subrayado rojo de cajas con foco (acento estándar de WinUI).

Build 0/0, **289/289** unitarias, **17 UI tests** pasan (6 omitidos sin la USB). Cortado con
`release.ps1 -UiTests` (los UI tests contra el publish self-contained, vía `FORMATDISKPRO_EXE`).

### 2026-07-13 — Tier 9 (#41, #42, #45) + capturas + cierre del proyecto — **v1.15.1**

**#41 — Los UI tests entran en el pipeline.** El obstáculo no era `release.ps1`: **6 tests fallaban por
diseño** sin la USB de pruebas conectada, así que integrarlos habría tumbado cualquier corte hecho sin el
hardware delante.
- `TestDriveFacts.cs`: `[TestDriveFact]` y `[DestructiveFact]` marcan `Skip` al **descubrir** el test si falta
  su precondición. **Un test omitido dice "no tengo el hardware"; uno fallido dice "la app está rota".**
  Confundirlos era el problema de fondo. Sin la USB: **17 pasan, 6 se omiten, 0 fallan**.
- Flag **`-UiTests`** con tres guardas: exige **terminal elevada**, **rechaza**
  `FORMATDISKPRO_ALLOW_DESTRUCTIVE=1` (un corte jamás debe formatear una unidad) y **rechaza**
  `-UiTests -SkipTests` juntos, que se contradicen y dejarían el release sin ninguna prueba.

**#42 — Instalador probado end-to-end** (con log de Inno):
- **Instalación limpia:** 511 archivos, **una sola** entrada de desinstalación, y `%AppData%` **conservado**
  (son datos del usuario).
- **Actualización in-place con el flujo silencioso real** (`/VERYSILENT /NORESTART /AUTOUPDATE=1`, los
  argumentos exactos de `UpdateService.LaunchInstaller`): cierra la app, actualiza y **la relanza sola**.
- ⚠️ **Hallazgo:** con un **diálogo modal abierto**, la app no atiende `CloseApplications` y Setup cae al aviso
  del `AppMutex` — **que bloquea incluso en `/VERYSILENT`**. Si se cancela ahí, `[InstallDelete]` ya borró
  `{app}\*` y la instalación queda **incompleta** (observado: 498 de 511 archivos). **No afecta a la
  auto-actualización real**: allí la app se cierra sola antes de que el instalador arranque. Solo se alcanza
  lanzándolo **a mano** con la app abierta y un diálogo encima — y entonces avisar es lo correcto.

**#45 — La codificación del `.csproj` se corrompía en CADA release.** Apareció **inspeccionando el binario
instalado**, no revisando código: el `.exe` publicado mostraba `Ricky Angel JimÃ©nez Bueno` en sus propiedades.
El bump de versión leía con `Get-Content -Raw` (ANSI, sin BOM) y reescribía como UTF-8 sin BOM → **una capa de
mojibake por release**, acumulada durante 14 versiones. Ver §4. Verificado simulando 3 bumps: el patrón viejo
corrompe en cada uno (`Jiménez` → `JimÃ©nez` → `JimÃƒÂ©nez`), el nuevo aguanta los tres.

**Capturas del README** (`docs/screenshots/`): las genera `tools/capture-screenshots.ps1` conduciendo la app
real por UI Automation — se **regeneran**, no se editan a mano, así que no envejecen en silencio. Exige
terminal elevada, preselecciona la unidad desde el `settings.json` (`LastDriveLetter`) en vez de manipular el
ComboBox, no redimensiona la ventana (es fija) y evita la unidad del sistema (sale `[Protegido]`: mala foto).

**Cierre del proyecto:** descartadas las dos ideas mayores (elevación `asInvoker`, ventana redimensionable).
Ver §4.

### 2026-07-12 — Tier 8 (#38–#40, #44) — seguridad y confianza — **v1.15.0**

Port de tres puntos que **WingetUSoft** (proyecto hermano) resolvió antes, con sus tests y sus tropiezos ya
conocidos. Pruebas: 269 → **289**.

- **#38 — Verificar el instalador antes de ejecutarlo elevado.** Era el agujero más serio: `UpdateService`
  descargaba por HTTPS y lo lanzaba **con permisos de administrador sin comprobar nada** (el README lo
  reconocía como "modelo de confianza asumido"). Detalle y consecuencias en §4.
- **#39 — Neutralizar fórmulas en el CSV del historial.** **Alcance honesto:** las líneas que escribe la app
  siempre empiezan por una palabra clave (`FORMAT`, `WIPE`…) y la etiqueta de volumen va incrustada a mitad
  del detalle, así que **no alcanza la primera posición del campo** — el plan original se equivocaba al
  atribuirle el riesgo. Lo que esto blinda: `history.log` es texto plano en `%AppData%` que otro proceso puede
  tocar, y cualquier formato de log futuro.
- **#40 — Contraste WCAG AA de los colores de severidad, medido por tests.** Los RGB salen de `HealthDialog` a
  `Core/SeverityPalette`, y un test mide el contraste real contra el fondo de cada tema (8 casos, mínimo
  4.5:1). **Los ocho ya pasaban:** aquí no había bug latente (en WingetUSoft, el mismo test sí destapó uno).
  El valor es preventivo — a partir de ahora, un color mal elegido **rompe el build**.
- **#44 — Build reproducible.** Apareció al compilar el instalador del propio Tier 8: **ya no compilaba**.
  Causa doble — MAX_PATH + `Microsoft.WindowsAppSDK` referenciado como `1.8.*`. Ver §4.

**Fix del pipeline:** `release.ps1` abortaba a mitad del push si se capturaba su salida (`Invoke-Git`). Ver §4.

### 2026-07-05 — `FormatDiskPro.UiTests`: de 4 a 24 tests, y 8 causas raíz reales

El proyecto de UI tests pasa de un smoke test a cubrir la app entera. Verificado contra **hardware real**,
incluido el ciclo destructivo completo (Formatear → Reinicializar → Reinicializar con FAT32 pequeña).

Conducir la app de verdad destapó **8 causas raíz** que ninguna revisión de código habría encontrado. Las que
siguen valiendo hoy están recogidas en §2 (*Pruebas de UI*): el proxy de Popup vacío junto a cada
`ContentDialog`, la necesidad de terminal elevada (UIPI), el backup de `%AppData%`, y la prohibición de correr
dos suites en paralelo (dos instancias elevadas compitiendo por el mismo `DrivePicker` y el mismo
`settings.json` producían fallos imposibles de diagnosticar).

### 2026-07-02 — Tier 7 (#37) — partición FAT32 pequeña al reinicializar — **v1.14.0**

Windows **nunca** permite crear un volumen FAT32 mayor de 32 GB (ni `Format-Volume` ni `format.com`: es una
restricción de la plataforma). El selector ya ocultaba FAT32 en discos ≥ 32 GB por eso, lo que dejaba sin
salida a quien necesita un USB grande con **una** partición FAT32 pequeña — el caso real: **flashear el
BIOS/UEFI de una placa base**, cuya utilidad solo lee FAT32.

Nueva opción en *Reinicializar unidad* (solo en extraíbles ≥ 32 GB) que crea una partición FAT32 de tamaño
elegible (1–32 GB) y deja el resto sin asignar. El flujo de *Iniciar* **no cambia**.

- **`SmallFat32PartitionBytes`** resta un margen de 4 MiB bajo el límite exacto en el tramo máximo: sin él, el
  redondeo de alineación de partición lo igualaba o superaba, y Windows rechazaba el formato.
- **Fix de plataforma (hallado con hardware real):** `Clear-Disk` **no siempre deja el disco en RAW**. El
  `Initialize-Disk` posterior se tolera cuando falla específicamente con *"already been initialized"* (el disco
  ya está listo para particionar); cualquier otro error se sigue propagando. **Afectaba a *toda* Reinicializar
  unidad**, no solo a esta opción.

### 2026-07-02 — Tier 6 (#28–#36) — pulido UX/UI — **v1.13.0**

No añade capacidades: refina **presentación y feedback** con patrones Fluent estándar.

InfoBar para la unidad protegida (antes competía con el estado transitorio en el footer) · `ConfirmDialog` con
foco inicial y Enter para confirmar (mantiene la fricción deliberada de escribir la letra, sin obligar a soltar
el teclado) · barra de capacidad · iconos por tipo de unidad · estado vacío del selector · salud coloreada en
la tarjeta · validación inline de la etiqueta · progreso en la barra de tareas (`ITaskbarList3`) · estado de
error en la barra de progreso.

**Fix incluido:** `LegalTextDialog` y `ConfirmDialog` **desbordaban el ancho de la ventana** — la licencia y
los avisos de terceros salían pegados a los bordes.

### 2026-06-27 — Tier 5 (#23–#27) — confianza, legal y sostenibilidad — **v1.12.0**

Relicencia de **MIT a GNU GPL v3.0** (copyleft: los derivados siguen abiertos), con el texto **embebido en el
`.exe`** y visible en *Ayuda → Licencia*. Disclaimer de uso destructivo/sin garantía, avisos de terceros, aviso
de privacidad (sin telemetría) y **donaciones voluntarias** (PayPal) que **no bloquean ninguna función**.

### 2026-06-25 / 06-27 — Tier 4 (#14–#22) — refinado de lo existente — **v1.10.0 / v1.11.0**

Pasadas de borrado seguro configurables (1/3/7; NIST 800-88: 1 basta en discos modernos) · IOPS en el benchmark
· idioma automático en el primer arranque · changelog en el aviso de actualización · umbrales de color + botón
Actualizar en S.M.A.R.T. · búsqueda/filtros y exportación CSV del historial · editar y reordenar presets ·
accesibilidad transversal · autorefresco de unidades por `WM_DEVICECHANGE`.

**v1.10.1 — fix de DPI/escalado:** la ventana se dimensiona por DPI y los diálogos llevan `MaxWidth`, para que
el texto no se corte en pantallas de alta densidad.

**v1.9.1 — mantenimiento:** correcciones de una revisión de código, sin tocar la lógica de formateo (doble
corchete en la unidad protegida, `MaxLength` de etiqueta dinámico por FS, validación de etiqueta compartida
entre formato y reinit, limpieza de claves de localización sin uso).

### 2026-06-24 — decisión: descartado el #13 (winget + firma) → Tier 3 cerrado

GitHub Releases con auto-actualización integrada es la distribución del proyecto, y **no se firmará el
instalador**. La firma sigue disponible como **opción** del pipeline, no como objetivo. Ver §4.

### 2026-06-23 — benchmark refinado a perfil CrystalDiskMark — **v1.9.0**

**Secuencial** (1 MiB, cola **Q8**) y **4 KiB aleatorio** (Q1), lectura y escritura. Toda la E/S **sin caché
del sistema** (`FILE_FLAG_NO_BUFFERING` + buffer alineado al sector); la fase secuencial mantiene varias
operaciones en vuelo para **no infravalorar NVMe/SSD**. Se mide por ventanas de tiempo (se adapta a unidades
rápidas y lentas) y se toma la **mediana** de 3 pasadas, que descarta el arranque en frío y los picos.

### 2026-06-22 — Tier 3 (#10–#12) — **v1.8.0** · Tier 2 completado — **v1.7.0**

Presets personalizados, **5 idiomas** (ES/EN/PT/FR/IT, con test de completitud: cada clave tiene sus 5
traducciones) y aviso al terminar (sonido + parpadeo, solo si la ventana no está en primer plano).

Antes, la **v1.7.0** cerró el Tier 2 con *Reinicializar unidad* (#8) y el *benchmark* (#9), más el diálogo de
novedades. **v1.7.1** lo corrigió: no aparecía al actualizar desde una versión que no guardaba
`LastVersionSeen`.

### 2026-06-21 — Tier 2 (#5–#7) y Tier 1 — **v1.4.0 / v1.5.0 / v1.6.0**

S.M.A.R.T. ampliado en diálogo dedicado (temperatura, horas, desgaste, errores; "No disponible" en unidades
que no exponen contadores, típico en USB) · chkdsk con *Solo comprobar* / *Comprobar y reparar* (la reparación
**bloqueada en el disco de sistema**, para no programar un reinicio) · detección y quita de **protección de
escritura**, que evita el fallo críptico al formatear.

**Tier 1 (v1.4.0):** persistencia de preferencias, ETA + velocidad, **borrado seguro con progreso real**
(sobrescritor propio, en sustitución de `cipher /w`) y visor de historial.

**Rediseño (v1.3.0):** sistema de tarjetas inspirado en Win11Debloat, con el **acento del sistema**. Incluyó el
fix del crash al cambiar el tema de Windows en caliente.

### 2026-06-19 / 06-20 — migración a WinUI 3 y el bug crítico de la 1.2.0

Migración de **Windows Forms a WinUI 3** (Windows App SDK 1.8, unpackaged): barra de título nativa, tema
automático y colores del sistema.

- **La 1.2.0 crasheaba al iniciar.** `dotnet publish` de una app WinUI 3 unpackaged **no copia el `.pri` propio
  de la app**, y sin él WinUI no puede resolver el XAML. Se arregló con el target `CopyAppPriToPublish`
  (v1.2.1). **No quitarlo.**
- **La 1.2.2 arregló la auto-actualización:** el cierre intencional para actualizar quedaba **bloqueado por
  `_isBusy`**, que cancelaba `Application.Current.Exit()`. La auto-actualización silenciosa funciona **desde la
  1.2.2 en adelante**.

### 2026-06-18 — **v1.1.0**: arquitectura por capas, hardening, tests, actualizaciones e instalador

La base del proyecto: separación `Core`/`Services`/`UI`, blindaje anti-inyección de los comandos de PowerShell,
suite de pruebas, auto-actualización vía GitHub Releases e instalador con Inno Setup.
