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
| **Versión publicada** | **1.20.0** (2026-08-15) |
| **Estado** | 🏁 **Funcionalidad TERMINADA** (Tiers 1–9) · 🔧 **backlog de calidad abierto** tras la auditoría del 2026-08-13 ([`ROADMAP.md`](ROADMAP.md) Parte 2) |
| **Stack** | C# 13 · .NET 10 · **WinUI 3** (Windows App SDK **1.8.260529003**, unpackaged, `net10.0-windows10.0.19041.0`) · xUnit · FlaUI/UIA3 · Inno Setup 6 |
| **Licencia** | GPLv3 · avisos de terceros · donaciones opcionales (PayPal) |
| **Pruebas** | **398** unitarias · **27** de UI sobre la app real — **24 pasan / 3 se omiten** (solo los opt-in) con la USB conectada, verificado el 2026-08-15 |
| **Hoja de ruta** | [`ROADMAP.md`](ROADMAP.md) — Parte 1 (producto) cerrada · Parte 2 (calidad) **abierta** |
| **Última actualización** | 2026-08-15 (v1.20.0 publicada y verificada · auditoría **35/40**, **Tiers 0–3 cerrados** · **CI descartada: el testing es local**) |

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
│  ├─ ReinitPlan.cs       Estilo MBR/GPT por tamaño, partición FAT32 pequeña, parseo de la nueva letra
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
├─ Services/        Efectos colaterales (procesos / disco / red)
│  ├─ DiskService.cs       S.M.A.R.T., nº de disco, protección de escritura, expulsión (PowerShell)
│  ├─ SecureWipe.cs        Sobrescritor propio del espacio libre, con progreso
│  ├─ CheckDisk.cs         chkdsk (comprobar / reparar) con streaming de progreso
│  ├─ ReinitDrive.cs       Reinicializar disco extraíble: clean + partición + formato
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

tests/FormatDiskPro.Tests/    369 pruebas xUnit sobre Core y los helpers de Services
tests/FormatDiskPro.UiTests/  25 pruebas FlaUI/UIA3 sobre el .exe real — FUERA de la solución (ver abajo)
tools/capture-screenshots.ps1 Regenera docs/screenshots/ conduciendo la app por UI Automation
release.ps1                   Corte de versión en un paso (tests + instalador + tag + GitHub Release)
FormatDiskPro.slnx            Solución: app + Tests. UiTests NO está incluido, a propósito.
```

**Regla de oro:** la lógica testeable vive en `Core`, **sin dependencias de WinUI, `Process` ni `HttpClient`**.
La UI y los servicios la consumen. Namespace único `FormatDiskPro`.

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

## 3. Estado actual

| | |
|---|---|
| Build | 0 advertencias / 0 errores |
| Unitarias | **398 / 398** (289 + 109 de la auditoría) · se ejecutan **en local**, nunca en CI (ver §4) |
| UI tests | **27** en total · con la USB (`utilidades`, sin opt-in): **24 pasan / 3 se omiten / 0 fallan** (2026-08-15) · sin USB ni segundo disco: 15 pasan / 12 se omiten, y **el corte ya dice cuáles** |
| Instalador | Verificado por SHA-256 (hash emparejado con su instalador) y probado **end-to-end** (limpia + in-place) |
| Publicado | **v1.20.0** (2026-08-15) · `master` sin trabajo pendiente de publicar |
| Auditoría | 2026-08-13 — **35/40 completadas** + 1 descartada (`T2-10`, CI) · **Tiers 0–3 cerrados**; abiertas solo las 5 del Tier 4 ([`ROADMAP.md`](ROADMAP.md) Parte 2) |

**Tiers completados**

| Tier | Tema | Versión |
|---|---|---|
| 1 | Quick wins (persistencia, ETA, borrado seguro, historial) | 1.4.0 |
| 2 | Diagnóstico y gestión (S.M.A.R.T., chkdsk, protección de escritura, reinicializar, benchmark) | 1.5.0–1.7.0 |
| 3 | Presets, 5 idiomas, aviso al terminar (**#13 winget/firma descartado**) | 1.8.0 |
| 4 | Refinado de lo existente (#14–#22) | 1.10.0 / 1.11.0 |
| 5 | Confianza y legal: GPLv3, avisos, privacidad, donaciones (#23–#27) | 1.12.0 |
| 6 | Pulido UX/UI (#28–#36) | 1.13.0 |
| 7 | Partición FAT32 pequeña al reinicializar (#37) | 1.14.0 |
| 8 | **Seguridad**: verificación del instalador, anti CSV injection, contraste WCAG AA (#38–#40, #44) | 1.15.0 |
| 9 | **Infraestructura**: UI tests en el release, instalador probado, build reproducible (#41, #42, #45) | 1.15.1 |

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
- **No se firma el instalador** (#13, 2026-06-24): SmartScreen dirá "editor desconocido". La firma sigue
  disponible como **opción** del pipeline. Es lo que hace **necesaria** la verificación por SHA-256.

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
- **La barra de capacidad NO usa el color de acento** (desde el pase de UX del 2026-07-20). Un `ProgressBar`
  por defecto hereda el acento del sistema; en un equipo con **acento rojo** la barra de ocupación se veía
  roja con el disco medio vacío y leía como *alarma*. Ahora codifica ocupación, no marca: neutro <80 %,
  ámbar ≥80 %, rojo ≥90 % (`MainWindow.CapacityBrush`, con `SeverityPalette.NeutralFill` para el neutro).
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

## 6. Estado del proyecto: TERMINADO (2026-07-13)

**Tiers 1–9 completados. No hay trabajo pendiente.** La hoja de ruta está cerrada: lo que queda fuera está
**deliberadamente** fuera. Las dos ideas mayores que restaban —elevación `asInvoker` y ventana
redimensionable— se **descartaron**: ambas revertían una decisión de diseño correcta para lo que este producto
es (ver §4).

**Lo único a vigilar en el próximo corte** (no es trabajo, es una comprobación): la verificación del instalador
(#38) **aún no se ha ejercido en producción**. Solo actúa al actualizar **desde** una versión ≥ 1.15.0, y los
clientes ≤ 1.14.1 llegaron a la 1.15.0 con el código viejo, que no verificaba nada. El primer uso real es
**1.15.0 → 1.15.1**.

Pulido opcional, sin impacto: más capturas (hoy 3), renombrar el `Name` interno del form, y la validación de
etiqueta no rechaza `'` (menor, por diseño: el escape lo cubre).

## 7. Cómo mantener este documento

1. Tras un cambio relevante, añadir una entrada en el **Registro de cambios** (fecha absoluta).
2. Actualizar el **Estado actual** (§3) y, si cambia una convención o decisión, el **§4**.
3. Commitearlo **junto con el cambio**, para que el contexto viaje con el código.

---

## Registro de cambios

### Índice de versiones

| Versión | Qué trajo |
|---|---|
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
> es `T4-02`, y sigue abierto **a propósito**.

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
