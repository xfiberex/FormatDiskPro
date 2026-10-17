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
| **Estado, versión y pruebas** | **No se repiten aquí, a propósito** — viven en [§3 *Estado actual*](#3-estado-actual), que es su única fuente |
| **Stack** | C# 13 · .NET 10 · **WinUI 3** (Windows App SDK **2.5.1**, unpackaged, `net10.0-windows10.0.19041.0`) · xUnit · FlaUI/UIA3 · Inno Setup 6 · GitHub Actions (compilación + unitarias, CodeQL) |
| **Licencia** | `GPL-3.0-only`, solo la versión 3 (`AUD-069`; cabecera SPDX en cada fuente) · avisos de terceros · donaciones opcionales (PayPal) |
| **Qué falta y qué cambió** | [`ROADMAP.md`](ROADMAP.md) — **solo lo abierto**, por severidad · [`docs/roadmap-historico.md`](docs/roadmap-historico.md) — lo ya hecho · [`CHANGELOG.md`](CHANGELOG.md) — qué trajo cada versión |
| **Última auditoría** | [`docs/auditoria/2026-10-11/INFORME.md`](docs/auditoria/2026-10-11/INFORME.md) — foto fija de esa fecha; lo que dejó pendiente, en el [`ROADMAP.md`](ROADMAP.md) |
| **Última actualización** | **2026-10-13** — el qué y el porqué, en la primera entrada de [`docs/contexto-historico.md`](docs/contexto-historico.md) |

> **Esta tabla no lleva estado, y es deliberado (`T9-14`).** Lo llevó dos veces —versión, recuento de
> pruebas, tiers abiertos— y las dos acabó contradiciendo a §3. Aquí va lo que **no cambia con el trabajo
> diario**; si una fila caduca, su sitio es §3 y aquí un enlace.

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
│  ├─ FluentTextPalette.cs Los colores de TEXTO que la app toma de Fluent, con su valor real, para medirlos
│  ├─ HistoryEntry.cs     Parseo del historial + filtro + exportación CSV (anti CSV injection)
│  ├─ ReinitPlan.cs       Estilo MBR/GPT por tamaño, tamaños de FAT32 pequeña que caben, parseo de letras
│  ├─ PartitionPlan.cs    EL LAYOUT COMO DATO: particiones + validación tipada ANTES de borrar nada (T5-01)
│  ├─ PlanRejection.cs    Rechazo de un plan → clave de texto + valores: qué decirle a quien lo pidió (T13-18)
│  ├─ Benchmark.cs        Tamaño de prueba, velocidad, IOPS, mediana
│  ├─ ErrorText.cs        Describe(ex) — texto de un fallo que NUNCA sale vacío (tipo + HRESULT si hace falta)
│  ├─ Presets.cs          Presets integrados (nombre traducido vía NameKey) + validación de los del usuario
│  ├─ Throughput.cs       Velocidad y ETA de operaciones largas
│  ├─ DeviceChange.cs     Interpretación de WM_DEVICECHANGE (autorefresco de unidades)
│  ├─ ReleaseNotes.cs     Notas de versión (Markdown) → texto plano
│  ├─ LegalText.cs        Licencia GPLv3 y avisos de terceros embebidos en el .exe
│  ├─ UpdateChecker.cs    Comparación de versiones (IsNewer)
│  ├─ DriveLetter.cs      Comparación de letras de unidad invariante de cultura (guarda del disco de sistema)
│  ├─ DestructiveGuard.cs ¿SE PUEDE formatear / reinicializar? Motivo tipado de cada rechazo (AUD-015)
│  ├─ DriveIdentity.cs    Qué hay DETRÁS de una letra (disco + partición), para comparar antes y después (AUD-013)
│  ├─ LinkPolicy.cs       Qué direcciones puede abrir la app: https y solo los sitios del proyecto (AUD-004)
│  ├─ OperationFailure.cs Línea de historial de una operación fallida (camino de error de T0-02)
│  ├─ FormatHistory.cs    Línea de historial de un formato: wipe=True solo si el borrado se completó (AUD-009)
│  ├─ PowerShellError.cs  Del CLIXML que PowerShell escribe por su salida de error, el mensaje (AUD-011)
│  ├─ CheckDiskOutput.cs  ¿Llegó chkdsk a examinar? Y su salida, limpia para enseñarla (AUD-007)
│  ├─ HistoryRotation.cs  Política de rotación del historial (umbral y nombre de la generación anterior)
│  └─ AppInfo.cs          Versión, coordenadas del repo, enlace de donación
├─ Services/        Efectos colaterales (procesos / disco / red) — clases con interfaz, inyectadas
│  ├─ AppServices.cs       RAÍZ DE COMPOSICIÓN: el único sitio que decide qué implementación usa la app
│  ├─ ProcessRunner.cs     IProcessRunner: la costura que hace testeables los caminos de error (T4-02)
│  ├─ SystemTools.cs       Rutas ABSOLUTAS de powershell, chkdsk, format.com y explorer: único sitio que las decide (AUD-026)
│  ├─ DesktopShell.cs      Abrir un enlace o un archivo SIN elevación, vía el explorer.exe del escritorio (AUD-004)
│  ├─ StateFolder.cs       %AppData%\FormatDiskPro, y la comprobación de que no es un enlace a otro sitio (AUD-025)
│  ├─ AdminOnlyFolder.cs   Carpeta que solo escriben administradores; rechaza la que no pueda garantizarlo (AUD-002)
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
│  ├─ UpdateService.cs     GitHub Releases: consulta, descarga a %ProgramData%, VERIFICACIÓN (SHA-256), instalación
│  └─ History.cs           Auditoría (%AppData%\FormatDiskPro\history.log, rotado a history.1.log)
├─ UI/              WinUI 3 (Windows App SDK)
│  ├─ MainWindow          Ventana principal, repartida en partial class por asunto (tope: 800 líneas cada una):
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
│  ├─ SaveFileDialog.cs   «Guardar como» por COM (IFileSaveDialog): el selector de WinRT rechaza a un proceso elevado
│  ├─ HighContrast.cs     ¿Tema de alto contraste? Y los pinceles del sistema con los que pintar entonces
│  ├─ LiveRegion.cs       Aviso LiveRegionChanged compartido por ventana y diálogos (AUD-014)
│  ├─ Theme/AppTheme.xaml Tokens de diseño (tarjetas, encabezados, footer)
│  └─ DriveViewModel.cs   Binding del ComboBox de unidades (icono por tipo)
├─ Localization/    Localization.cs — 5 idiomas (ES/EN/PT/FR/IT), L.T("clave")
├─ installer/       installer.iss (Inno Setup) + build-installer.ps1 → Output/ (gitignored)
│                   new-selfsigned-cert.ps1: certificado autofirmado para PROBAR la firma, no para publicar
├─ App.xaml(.cs)    Aplicación WinUI: recursos, red global de excepciones, crea AppServices y la ventana
├─ app.manifest     requireAdministrator + PerMonitorV2
├─ FormatDiskPro.csproj  Versión (fuente única), Windows App SDK fijado, target CopyAppPriToPublish
├─ FormatDiskPro.ico     Icono de la app y del instalador
├─ Program.cs       Punto de entrada: mutex de la app y, si ya hay otra instancia, le cede el paso
└─ SingleInstance.cs  Trae al frente la instancia abierta (`AUD-037`)

tests/FormatDiskPro.Tests/    Pruebas xUnit sobre Core y los Services (con IProcessRunner falso); recuento en §3
tests/FormatDiskPro.UiTests/  Pruebas FlaUI/UIA3 sobre el .exe real — FUERA de la solución (ver abajo)
tools/capture-screenshots.ps1 Regenera docs/screenshots/ conduciendo la app por UI Automation
docs/                         Historial (roadmap-historico.md, contexto-historico.md), auditoría, capturas
.github/workflows/            CI: compilación + unitarias (las de UI solo se COMPILAN) y CodeQL — ver §4
CHANGELOG.md                  Qué cambió en cada versión (Keep a Changelog); el corte exige su sección
release.ps1                   Corte de versión en un paso (tests + instalador + tag + GitHub Release)
FormatDiskPro.slnx            Solución: app + Tests. UiTests NO está incluido, a propósito.
```

> El árbol se comprueba con `git ls-files src/`. **`MainWindow.xaml.cs` tiene hoy 986 líneas**, por encima
> del tope de 800 que fijó `T2-08`; devolverlo ahí es `AUD-042`.

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
  `release.ps1 -UiTests`. El CI (desde el 2026-09-18) los **compila** en cada push y PR, pero **no los
  ejecuta**.
- **Exigen terminal ELEVADA.** UIPI bloquea que un proceso no elevado automatice la ventana de uno que sí lo
  está, y la app es `requireAdministrator`. `AppFixture` lo comprueba y falla con un mensaje claro en vez de
  colgarse.
- **Precondición ausente ≠ fallo.** Los 6 tests que necesitan la **USB de pruebas** (partición extraíble
  etiquetada `utilidades`) **se OMITEN** si no está conectada, vía `[TestDriveFact]`; el que borra datos de
  verdad se omite salvo `FORMATDISKPRO_ALLOW_DESTRUCTIVE=1`, vía `[DestructiveFact]`. Antes **fallaban**, y por
  eso no se podían meter en el pipeline.
- **Un `ContentDialog` convive con MÁS DE UN `ControlType.Window`** en el árbol: WinUI deja un proxy de Popup
  vacío junto al diálogo real. `DialogHelper.FindDialog` se queda con el que tiene contenido (más hijos).
- **`AppFixture` respalda y restaura `settings.json` + `history.log`** de `%AppData%` en cada corrida (la app es
  unpackaged); la copia va también a `%LocalAppData%\FormatDiskPro.UiTests\`, y si una corrida muere, la siguiente la restaura (`AUD-040`).
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
  anterior**. Ver §4. El CI ya lo compila, así que un error de compilación no pasa desapercibido; pero en
  local la trampa del `--no-build` sigue en pie.

## 3. Estado actual

> **Aquí no se copian cifras que un comando da** (`AUD-018`, y antes `T9-14`): esta tabla se desfasó tres
> veces por repetirlas. Va lo que no se obtiene ejecutando nada, y para lo demás, el comando.

| | |
|---|---|
| **Publicado** | **v1.30.1** (2026-10-15): las siete tandas de la auditoría del 2026-10-11 (PRs #3 a #10) y los arreglos del corte. `master` sin trabajo pendiente de publicar |
| **Trabajo abierto** | El de la auditoría del 2026-10-11, más `T10-02` (bloqueada a propósito). La lista y el recuento viven **solo** en [`ROADMAP.md`](ROADMAP.md) |
| **Trabajo cerrado** | Producto (`#1`–`#45`, 2026-07-13), auditoría del 2026-08-13 (`T0`–`T4`, 2026-08-16) y los Tiers 5 a 13 (el último, 2026-09-22): en [`docs/roadmap-historico.md`](docs/roadmap-historico.md) |
| **Build** | Sin advertencias; el CI lo exige con `-warnaserror` en cada push y PR |
| **Unitarias** | `dotnet test` da el recuento. Línea base del 2026-10-15: **926** (925 pasan · 1 se omite: la de capacidad sobre unidad real, que pide `FORMATDISKPRO_VERIFY_DRIVE`). Cuatro de `AdminOnlyFolderTests` se omiten además si la terminal no está elevada |
| **Pruebas de UI** | `dotnet test tests\FormatDiskPro.UiTests --filter "Category!=Slow"`, en terminal elevada. Línea base del 2026-10-15, con la USB de pruebas y sin opt-in: **56** (51 pasan · 5 se omiten: 3 de `ALLOW_DESTRUCTIVE` y 2 de `ALLOW_YANK`). Sin la USB se omiten además las que la necesitan |
| **Cobertura de `Core/`** | La mide y la exige `release.ps1` en cada corte. 98,6 % el 2026-10-15 |
| **Instalador** | Verificado por SHA-256 (hash emparejado por nombre con su instalador) y probado de punta a punta, en instalación limpia y sobre una anterior |

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
- **Las pruebas de UI son LOCALES; desde el 2026-09-18 hay CI de compilación + unitarias.** La auditoría
  propuso un CI de solo unitarias (`T2-10`); se implementó, se revirtió y quedó descartado el 2026-08-15. El
  motivo no era el coste: en esta app la prueba que vale es la que **ejerce el binario real** contra
  hardware real (elevación + USB de pruebas), y eso **no cabe en un runner hospedado**; un ✅ verde que solo
  cubre los unitarios afirma más de lo que prueba — el problema que `T2-12` corrigió en el otro extremo del
  proceso. La consecuencia asumida era que un PR externo no ejecutaba nada hasta que el mantenedor lo
  corriera en su máquina, y **al hacerse público el repositorio dejó de ser asumible**. Desde entonces:
  - **`.github/workflows/ci.yml`** («Compilación y unitarias»): en cada push a `master` y cada PR, compila en
    Release con `-warnaserror`, ejecuta las unitarias y **compila, sin ejecutarlas,** las pruebas de UI. La
    objeción de 2026-08-15 se atiende en el propio check: su nombre es el texto de la insignia del README, y
    el resumen de cada ejecución dice lo que **no** ejecutó y qué unitarias se omitieron, con su motivo.
    Otro job (`AUD-058`) publica como `build-installer.ps1` y exige el `.pri` y rutas dentro de MAX_PATH, y
    analiza los `.ps1` con `powershell` 5.1 (BOM si no son ASCII); ese paso va sin acentos: lo lee el 5.1.
  - **`.github/workflows/codeql.yml`**: CodeQL sobre la app (no sobre las pruebas), en cada cambio y cada
    semana. La lista de alertas solo la ve quien tiene permiso de escritura, así que no choca con la
    divulgación privada de `SECURITY.md`. Va con `build-mode: manual` porque el código que genera el XAML
    solo existe tras compilar; el aviso «Cannot build an overlay-base database» que deja en cada ejecución
    es la consecuencia esperada (sin análisis incremental en los PR), **no** un motivo para pasar a `none`.
  - **`.github/dependabot.yml`**: las acciones y los NuGet **de las pruebas** (`AUD-057`); el Windows App SDK se sube a
    mano (ver *Build y publicación*). Un salto mayor de `coverlet.collector`, el de la cobertura, pide antes un corte de prueba.
  - **Lo que no cambia:** la puerta de calidad sigue siendo **`release.ps1 -UiTests` desde una terminal
    elevada**. El CI no la sustituye ni la acorta.
  - **Dos trampas del CI.** Ninguna prueba lee un `.md` ni nada de `docs/`, y por eso los workflows se saltan
    esos cambios: si una empieza a hacerlo, hay que quitar el filtro. Y los pasos van en `pwsh` (7), no en
    `powershell` (5.1), que lee los scripts sin BOM con la página ANSI (ver *Trampas de PowerShell 5.1*).
- **Protección de unidades:** SOLO se protege el **disco de sistema** (`IsSystemDrive()`). El resto
  —removibles, discos de datos fijos, RAM— **sí** se pueden formatear.
- **Las guardas destructivas se deciden en `Core/DestructiveGuard`, no en la ventana** (`AUD-015`,
  2026-10-13). *Formatear* y *Reinicializar* reúnen los hechos —letra, tipo, números de disco— y enseñan
  el motivo que les devuelve; no deciden. **Ante la duda, no:** un dato que no se pudo leer es un
  rechazo con texto propio, nunca un «no hay inconveniente» (`AUD-012`: con el disco de Windows sin leer,
  *Reinicializar* seguía). Una guarda nueva va ahí, con la prueba de su rechazo.
- **Una letra no es una identidad** (`AUD-013`, 2026-10-13). Entre abrir una confirmación destructiva y
  aceptarla, detrás de la misma letra puede haber pasado a haber otro disco. *Formatear* lee
  `DriveIdentity` antes y después de confirmar y no sigue si difieren. *Reinicializar* borra el **número
  de disco confirmado** —no «el que la letra designe» al arrancar—, y su script se niega, instantes
  antes de `Clear-Disk`, si el disco ya no es ese o es de sistema o de arranque. Una confirmación
  abierta se cierra sola si llega un aviso de dispositivo. Toda operación destructiva nueva sigue ese
  patrón: se identifica lo que se confirma y se comprueba antes de ejecutar.
- **La app corre elevada, y la cuenta sin elevar no decide qué ejecuta ni dónde escribe** (`AUD-002`,
  `AUD-004`, `AUD-025`, `AUD-026`; 2026-10-13). Cuatro reglas, cada una con su único sitio:
  - **Nada se lanza por nombre:** las rutas salen de `Services/SystemTools`, y una prueba lo exige.
  - **Nada se abre con el shell desde este proceso:** enlaces y archivos van por `Services/DesktopShell`
    (los abre el `explorer.exe` del escritorio, sin elevación), y solo las direcciones que pasen
    `Core/LinkPolicy`, una lista cerrada.
  - **Lo que se ejecuta elevado vive en una carpeta solo de administradores** (`Services/AdminOnlyFolder`)
    **y no se suelta entre verificarlo y ejecutarlo** (`VerifiedInstaller`). Una carpeta preexistente de
    otro dueño se rechaza, no se arregla.
  - **El estado sigue en `%AppData%`, pero no se toca a través de un enlace** (`Services/StateFolder`).
    Es comprobar y después usar; moverlo a `%ProgramData%` se descartó porque haría de máquina unas
    preferencias que hoy son de cada persona.
- **Un diálogo en el arranque espera a que el contenido esté cargado** (`WhenContentLoadedAsync`): en
  la primera activación aún no hay `XamlRoot`, y un `ContentDialog` sin él lanza.
- **El resultado se lee de lo que pasó, no del código de salida** (`AUD-006` a `AUD-009`, 2026-10-13).
  «Eject» termina en 0 aunque Windows se niegue; `chkdsk` usa el 3 para dos cosas opuestas; el borrado
  seguro devuelve un código que nadie miraba. Una operación nueva comprueba el efecto (¿sigue montado?,
  ¿llegó a examinar?) antes de anunciarlo, y lo que escribe en el historial sale de `Core/` con prueba.
  Lo que se **enseña** de una herramienta se lee en su codificación (`SystemTools.AnsiEncoding`) y, si
  viene de PowerShell, pasa antes por `Core/PowerShellError`.
- **Una región activa declarada no habla sola** (`AUD-014`, 2026-10-13). `LiveSetting` en el XAML dice
  *cómo* leer un texto; lo que hace que se lea es el evento, y hay que lanzarlo: `UI/LiveRegion.Announce`
  tras cambiar el texto, con el elemento ya visible. Los hitos del pie van por `SetStatusAndAnnounce`.
  Y **todo control interactivo tiene nombre accesible** (`AUD-003`): un `ComboBox` enlazado a objetos
  anuncia cada opción con su `ToString()`, no con la plantilla. Las dos cosas las vigilan pruebas de UI
  que escuchan los eventos de UI Automation y recorren la ventana buscando controles sin nombre.
- **Recargar la lista no es elegir una unidad** (`AUD-029`, 2026-10-13). `LoadDrives` ignora los
  cambios de selección que ella misma provoca y, si la unidad sigue siendo la misma (letra y huella del
  volumen), refresca lo que se lee y no toca lo que la persona escribió. Lo que deba restablecer
  opciones va en `OnDriveSelected(keepOptions: false)`, no en el manejador del selector.
- **El estado del pie se escribe DESPUÉS de `LoadDrives()`, nunca antes.** Seleccionar una unidad vacía
  `StatusText` (`ApplyProtection`), así que un mensaje puesto antes de recargar no llega a verse.
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
  - **`HttpCompletionOption.ResponseHeadersRead` no es decorativo:** deja el cuerpo fuera del `Timeout` de 30 s;
    con `ResponseContentRead`, un instalador de ~60 MB fallaría por debajo de 2 MB/s.
  - **Alcance honesto:** hash y `.exe` salen del mismo release: detecta manipulación **en tránsito**, no una cuenta comprometida.
- **Exportación CSV:** además del escape RFC 4180, se **neutralizan las fórmulas** (`=`/`+`/`-`/`@` → prefijo
  `'`). Escapar comillas protege la *estructura* del CSV, no al programa que lo abre.
- **La configuración de agentes se revisa como un workflow** (`AUD-023`): `.mcp.json`, `.claude/`, `.agents/skills/` y
  `skills-lock.json`; `agentes.yml` señala el PR y comprueba las skills (MIT de GitHub, Inc., `AUD-068`) contra su hash.

### Build y publicación

- **Publicación self-contained** (`WindowsAppSDKSelfContained=true`): el usuario final no instala .NET. El runtime
  que va dentro lo decide el SDK, no lo instalado: `build-installer.ps1` lo anota y el corte lo pone en tag y notas.
- **Compilación reproducible** (`AUD-022`): SDK en `global.json` (`latestPatch`), paquetes en `packages.lock.json`
  (bloqueado con `CI=true` y en el corte; un paquete cambiado lleva su archivo de bloqueo en el mismo commit), solo
  nuget.org, y `Microsoft.WindowsAppSDK` **exacto** (`2.5.1`): con comodín, lo publicado cambió solo y rompió el build.
- **El instalador se publica a `%TEMP%`, no dentro del repo — MAX_PATH.** Inno Setup no usa las APIs de rutas
  largas, y el publish del Windows App SDK trae nombres de hasta 76 caracteres
  (`WindowsAppSdk.AppxDeploymentExtensions.Desktop-EventLog-Instrumentation.dll`). Sumados a la ruta del repo
  pasan de 260 en cuanto el checkout no cuelga de una carpeta corta, e ISCC aborta con «El sistema no puede
  encontrar la ruta especificada» **sin decir cuál**.
- **Instalador (Inno Setup):** `AppId = {CEC07916-C9B5-4EA8-9102-3273384395AD}` — **no cambiar nunca**
  (actualización in-place). `PrivilegesRequired=admin`, `CloseApplications=yes`. `[InstallDelete]` vacía `{app}`,
  y `/DIR=` lo fija aunque la página esté oculta: `PrepareToInstall` aborta sobre una carpeta con contenido y
  sin `FormatDiskPro.exe` (`AUD-062`). Sin versión ni carpeta por defecto: se compila con `build-installer.ps1`,
  que pasa la versión también al publish (`-p:Version`, `AUD-065`).
- **Versionado:** fuente única en el `<Version>` del `.csproj`. El updater lo compara con el `tag_name` del
  último release.
- **Workaround del PRI:** `dotnet publish` de una app WinUI 3 *unpackaged* **no copia el `.pri` propio** de la
  app; sin él, WinUI no resuelve el XAML y la app **crashea al iniciar** (fue el bug de la 1.2.0). El target
  `CopyAppPriToPublish` del `.csproj` lo copia a mano. No quitarlo.
- **Flujo del mantenedor (2026-10-15): directo en `master`, sin ramas ni PRs.** Sin PR, el CI llega después del push: antes de cada commit, `-warnaserror` y unitarias en local (y UI si toca la app). El mantenedor hace los push y lanza el corte real; Claude prepara, simula (`-DryRun`) y comprueba después. Desde el 2026-10-16 un ruleset de GitHub («Proteger master») impide forzar y borrar `master`, sin exigir PR ni check; los ajustes del repo los cambia el mantenedor, porque el modo automático no deja a Claude tocarlos.
- **`FormatDiskPro.slnx` NO incluye `tests/FormatDiskPro.UiTests`, y eso es deliberado.** `release.ps1`
  hace `dotnet test $solution --collect:"XPlat Code Coverage"`, y meterlas ahí las ejecutaría en cada corte
  (necesitan hardware, terminal elevada y opt-in). **Trampa:** `dotnet build FormatDiskPro.slnx` **no las
  compila**, y con `dotnet test --no-build` se ejecuta la **DLL vieja** en verde (`T5-05`, 2026-08-16). Al
  tocarlas: `dotnet build` de su `.csproj` **explícitamente**, o `dotnet test` sobre él **sin** `--no-build`. El
  CI las compila en cada push desde el 2026-09-18; la trampa del `--no-build` en local sigue en pie.

### Trampas de PowerShell 5.1 (todas nacieron de un fallo real)

- **Los scripts van con BOM UTF-8.** Sin él, los acentos rompen el parser.
- **El `.csproj` también va con BOM, y NO se lee con `Get-Content -Raw`.** Sin BOM, PS 5.1 lo lee con la página
  de códigos ANSI: los bytes UTF-8 de `é` se vuelven `Ã©` y, al reescribirlo, la corrupción **queda grabada**.
  Como el bump de versión ocurre en **cada** release, el daño se acumulaba capa sobre capa: tras 14 versiones
  el nombre del autor de `<Authors>`/`<Copyright>` estaba destrozado **en las propiedades del `.exe`
  publicado**. `release.ps1` lo lee con `ReadAllText` (detecta el BOM) y lo reescribe **conservándolo**; igual todo script.
- **git + salida capturada = trampa.** git escribe por stderr en su operación **normal** (el resumen del
  `push`, los avisos de CRLF), sin que nada falle. Si la salida del script **se captura** (`| Tee-Object`,
  `2>&1 |`, un wrapper), PS 5.1 convierte cada línea de stderr de un exe nativo en `NativeCommandError` y, con
  `$ErrorActionPreference = "Stop"`, **aborta aunque git devuelva 0**. En un `push` eso deja el release **a
  medias**: rama subida, sin tag ni GitHub Release. Por eso los git que mutan estado van por **`Invoke-Git`**,
  que baja la preferencia mientras corre git y decide por `$LASTEXITCODE`; las **consultas**, por
  **`Invoke-GitQuery`** (`AUD-061`): `2>$null` con `Stop` **no silencia nada** y, sin red, `ls-remote` abortaba.
- **Modo estricto y XML** (`AUD-061`): con `Set-StrictMode -Version Latest` (los tres scripts), `$nodo.Hijo`
  **lanza** si falta el hijo, también al enumerar (`PropertyGroup.Version`). Se lee con `SelectNodes`/`SelectSingleNode`.

### Formateo: dos vías, y solo una se puede cancelar

- **Todo formato COMPLETO va por `format.com`; lo rápido y ReFS, por `Format-Volume`** (`AUD-001`,
  2026-10-13). Lo decide `FormatLogic.UsesFormatCom`, y no es una cuestión de dónde sale el porcentaje:
  **matar `format.com` detiene el formato; matar el PowerShell de `Format-Volume`, no** —lo ejecuta el
  proveedor de almacenamiento de Windows, que sigue sin el proceso que se lo pidió (medido: «cancelado» a
  los 4 s, terminado a los 50)—. Por eso en la vía de `Format-Volume` **Cancelar está deshabilitado** y el
  estado lo dice. Si añades una operación que lance `Format-Volume`, no le pongas un Cancelar que no
  puede cumplir.
- **`format.com` no acepta el tamaño de clúster en bytes a partir de 16 KB**: `/A:16384` da «Parámetro no
  válido» (código 4); quiere `16K`, `64K`, `1M` (y tampoco `1024K`). Lo escribe
  `FormatLogic.ComAllocationSize`. Hasta entonces, un formato completo con 16 KB o más fallaba siempre.
- **Un formato completo cancelado deja el volumen sin sistema de archivos**, y `DriveInfo.IsReady` da
  `false`. «No lista» no es «retirada»: la guarda de *Formatear* solo rechaza si detrás de la letra no
  hay partición, o esa unidad no tendría salida dentro de la app.

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
  **Ya no hay excepción fuera de `Core`:** la que había —los ocho colores de los botones de la barra de
  título, en `MainWindow.UpdateCaptionButtonColors`— desapareció con `T13-12`, que los sustituyó por
  `AppWindowTitleBar.PreferredTheme`. Ese cromo de ventana lo pinta ahora Windows con el tema que se le
  dice, y no hay ningún color propio que medir.
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
  estrella — ver la entrada de esa fecha en [`docs/contexto-historico.md`](docs/contexto-historico.md).
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
| CI (GitHub Actions) | Automático en cada push a `master` y cada PR; a mano: *Actions → Compilación y unitarias → Run workflow* |

`release.ps1` hace: validar (rama al día con origin, herramientas, también en `-DryRun`) → tests en Release con `-warnaserror` → bump → instalador → commit + tag con el SHA-256 del instalador → push → `gh release create` con el instalador **y su `.sha256`** (`AUD-020`/`021`/`052`/`053`).

> **Corte recomendado:** `.\release.ps1 -Version X.Y.Z -UiTests` desde una **terminal elevada**. Con el flag,
> el release **no sale si la app real falla**. `release.ps1` **aborta**, también en `-DryRun`, si encuentra
> `FORMATDISKPRO_ALLOW_DESTRUCTIVE`, `_ALLOW_YANK` o `_VERIFY_DRIVE`: un corte jamás toca hardware (`AUD-056`).
>
> Solo hace `git add -u`, así que **los archivos nuevos hay que `git add`earlos antes**.
>
> **Y aborta si [`CHANGELOG.md`](CHANGELOG.md) no tiene la sección de la versión, o si está vacía** (`T4-01`,
> `AUD-054`): mueve antes lo que haya bajo *Sin publicar* a `## [X.Y.Z] — fecha` y añade su enlace abajo.

## 6. Qué queda fuera, y por qué

Las tareas abiertas están en el [`ROADMAP.md`](ROADMAP.md); esta sección es solo el alcance.

Lo que falta, falta **a propósito**. Las decisiones y su porqué viven en §4 y, resumidas, en *Decisiones
cerradas* del [`ROADMAP.md`](ROADMAP.md#-decisiones-cerradas-no-reabrir): la app corre **siempre elevada**
(`asInvoker` descartado), la ventana es de **tamaño fijo**, no se **firma** el instalador (`#13`/`T4-03` —
de ahí que el `.sha256` sea obligatorio), las pruebas de UI **no corren en CI** (`T2-10`: necesitan la app
elevada y la USB; el CI, desde el 2026-09-18, cubre compilación y unitarias) y *Reinicializar* **no es un
gestor de particiones** (`T5-04`: crea layouts sobre un disco que se está borrando entero; nunca
redimensiona, fusiona ni mueve datos).

## 7. Cómo mantener este documento

1. Tras un cambio relevante, añadir una entrada **arriba** en
   [`docs/contexto-historico.md`](docs/contexto-historico.md), con fecha absoluta.
2. Actualizar el **Estado actual** (§3) si cambia algo que no dé un comando y, si cambia una convención o
   una decisión, el **§4**.
3. Si el cambio cierra una tarea, **moverla** del [`ROADMAP.md`](ROADMAP.md) a
   [`docs/roadmap-historico.md`](docs/roadmap-historico.md) y actualizar las cifras de su *Estado*.
4. Todo ello va **en el mismo commit que el cambio**, para que el contexto viaje con el código.
5. Este archivo se mantiene **por debajo de 500 líneas**: lo que es historia va a `docs/`, no aquí.

---

## 8. Falsos positivos verificados

> Hallazgos que una auditoría propuso y **descartó tras comprobarlos**. Constan para que la siguiente no
> los repita. Si el código cambia y el motivo deja de sostenerse, se quita la fila. Las áreas revisadas
> sin hallazgos en auditorías anteriores (inyección de comandos, secretos, tipografía francesa…) están en
> [*Áreas auditadas sin hallazgos*](docs/roadmap-historico.md#áreas-auditadas-sin-hallazgos), al final del
> historial de la hoja de ruta.

| Fecha | Lo que se propuso | Por qué es falso | Cómo se comprobó |
|---|---|---|---|
| 2026-10-11 | «La app en reposo ignora `WM_CLOSE`: no se cierra con `taskkill` ni cuando un instalador se lo pide» | Fallaba el observador, no la app: el mensaje se envió desde una terminal **sin elevar**, y UIPI descarta en silencio los mensajes y la automatización hacia una ventana elevada | Con la terminal elevada, `WM_CLOSE` y `taskkill` sin `/F` cierran la app en 4 de 4 intentos, con y sin carga de CPU |
| 2026-10-11 | «El arranque se triplicó con el Windows App SDK 2.x (1,7–2,4 s)» | Medición contaminada: incluía la elevación y la carga de otros procesos | En reposo, 512 ms hasta la ventana: igual que el 2026-08-26 |
| 2026-10-11 | «Una excepción en el constructor de la ventana deja un proceso elevado vivo y sin ventana» | El proceso termina solo (`0xC000027B`). Lo cierto es que la app no arranca y no dice por qué, y eso es `AUD-027` | Arranque con un `settings.json` que lleva `"UserPresets": null` |
| 2026-10-11 | «*Expulsar* nunca funciona: PowerShell termina antes de que el shell ejecute el verbo» | Sin nada abierto, los volúmenes se desmontan a los 3 s. Lo que falla es **informar** de si se expulsó (`AUD-008`) | Reproducción sobre la USB de pruebas, con y sin un archivo abierto |
| 2026-10-11 | Seis «claves de API» señaladas por gitleaks | Están en `obj/**/MsixContent/MSIX/*_license.xml`: salida de compilación del Windows App SDK, que git ignora | gitleaks 8.30.1 sobre el historial completo (153 commits): 0 hallazgos |

---

## Registro de cambios

El registro razonado —una entrada por cambio, con qué se hizo, por qué y qué se aprendió— vive en
[`docs/contexto-historico.md`](docs/contexto-historico.md), junto al índice de versiones. Las entradas
nuevas se añaden **allí, arriba**. Qué trajo cada versión, en [`CHANGELOG.md`](CHANGELOG.md).
