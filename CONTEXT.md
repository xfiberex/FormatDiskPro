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
| **Versión publicada** | **1.15.2** (2026-07-20) |
| **Estado** | 🏁 **TERMINADO** — Tiers 1–9 completados, sin trabajo pendiente |
| **Stack** | C# 13 · .NET 10 · **WinUI 3** (Windows App SDK **1.8.260529003**, unpackaged, `net10.0-windows10.0.19041.0`) · xUnit · FlaUI/UIA3 · Inno Setup 6 |
| **Licencia** | GPLv3 · avisos de terceros · donaciones opcionales (PayPal) |
| **Pruebas** | **289** unitarias · **23** de UI sobre la app real (17 pasan / 6 se omiten sin la USB de pruebas) |
| **Hoja de ruta** | [`ROADMAP.md`](ROADMAP.md) — cerrada |
| **Última actualización** | 2026-07-20 (pase UX/UI — v1.15.2) |

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
│  ├─ SeverityPalette.cs  Colores verde/ámbar/rojo por tema — contraste WCAG AA verificado por tests
│  ├─ HistoryEntry.cs     Parseo del historial + filtro + exportación CSV (anti CSV injection)
│  ├─ ReinitPlan.cs       Estilo MBR/GPT por tamaño, partición FAT32 pequeña, parseo de la nueva letra
│  ├─ Benchmark.cs        Tamaño de prueba, velocidad, IOPS, mediana
│  ├─ SecureWipe.cs*      Patrón y nº de pasadas del borrado seguro (*la parte pura)
│  ├─ Presets.cs          Presets integrados + validación/renombrado de los del usuario
│  ├─ Throughput.cs       Velocidad y ETA de operaciones largas
│  ├─ DeviceChange.cs     Interpretación de WM_DEVICECHANGE (autorefresco de unidades)
│  ├─ ReleaseNotes.cs     Notas de versión (Markdown) → texto plano
│  ├─ LegalText.cs        Licencia GPLv3 y avisos de terceros embebidos en el .exe
│  ├─ UpdateChecker.cs    Comparación de versiones (IsNewer)
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
│  ├─ UpdateService.cs     GitHub Releases: consulta, descarga, VERIFICACIÓN (firma/SHA-256), instalación
│  └─ History.cs           Auditoría (%AppData%\FormatDiskPro\history.log)
├─ UI/              WinUI 3 (Windows App SDK)
│  ├─ MainWindow          Ventana principal y orquestación
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

tests/FormatDiskPro.Tests/    289 pruebas xUnit sobre Core y los helpers de Services
tests/FormatDiskPro.UiTests/  23 pruebas FlaUI/UIA3 sobre el .exe real — FUERA de la solución (ver abajo)
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
| Unitarias | **289 / 289** |
| UI tests | **17 pasan · 6 omitidos · 0 fallan** sin la USB conectada |
| Instalador | Verificado por SHA-256 y probado **end-to-end** (limpia + in-place) |
| Publicado | v1.15.2 |

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
- **Protección de unidades:** SOLO se protege el **disco de sistema** (`IsSystemDrive()`). El resto
  —removibles, discos de datos fijos, RAM— **sí** se pueden formatear.
- **No se firma el instalador** (#13, 2026-06-24): SmartScreen dirá "editor desconocido". La firma sigue
  disponible como **opción** del pipeline. Es lo que hace **necesaria** la verificación por SHA-256.

### Seguridad

- **PowerShell vía `-EncodedCommand`** (Base64 UTF-16LE), nunca por concatenación. Validar
  `char.IsLetter(letter)` antes de interpolar. Etiqueta escapada (`'`→`''`) en `Format-Volume`; para
  `format.com`, `ArgumentList` (escape por argumento).
- **Verificación del instalador (desde v1.15.0, #38) — NO ROMPER.** El instalador se ejecuta **elevado**, así
  que antes se comprueba que es el del proyecto: firma Authenticode válida → OK; si no, **SHA-256** contra el
  asset `*.exe.sha256`. Sin ninguna de las dos, **se borra y no se ejecuta**. Consecuencias operativas:
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
- **La barra de capacidad NO usa el color de acento** (desde el pase de UX del 2026-07-20). Un `ProgressBar`
  por defecto hereda el acento del sistema; en un equipo con **acento rojo** la barra de ocupación se veía
  roja con el disco medio vacío y leía como *alarma*. Ahora codifica ocupación, no marca: neutro <80 %,
  ámbar ≥80 %, rojo ≥90 % (`MainWindow.CapacityBrush`). Ámbar/rojo reusan `SeverityPalette` (theme-aware,
  contraste medido); el neutro sigue `_darkMode`. Es —junto a `SeverityPalette`— la otra excepción
  deliberada al "sin colores hardcodeados" de `AppTheme.xaml`: una barra de datos debe usar color semántico.
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
