# Contexto del proyecto — FormatDiskPro

> **Propósito de este archivo.** Documento de contexto **vivo** que resume el estado del
> proyecto y las decisiones tomadas, para no perder continuidad al cambiar de equipo (PC)
> o de sesión. **Mantenerlo actualizado con cada cambio relevante**: actualizar
> _Estado actual_ y añadir una entrada en el _Registro de cambios_. Usar fechas absolutas.

- **Repositorio:** https://github.com/xfiberex/FormatDiskPro
- **Última actualización de este documento:** 2026-07-12
- **Versión actual:** **1.15.0** (**Tier 8 — seguridad y confianza**: **#38** el instalador descargado se
  **verifica antes de ejecutarse como administrador** (firma Authenticode → si no, SHA-256 contra el asset
  `*.exe.sha256` del release; sin ninguna de las dos, se borra y no se ejecuta), **#39** neutralización de
  **fórmulas en la exportación CSV** del historial y **#40** **contraste WCAG AA** de los colores de severidad
  S.M.A.R.T., ahora medido por tests. Incluye el fix de build **MAX_PATH** (la publicación intermedia se movió
  a `%TEMP%` porque Inno Setup no maneja rutas > 260 y el Windows App SDK incorporó un archivo de nombre largo
  que rompía la compilación del instalador), el **fijado de la versión del Windows App SDK** —era `1.8.*`,
  flotante: el build no era reproducible y ese archivo apareció **solo**— y la corrección de
  `<Authors>`/`<Copyright>`, que estaban **corrompidos** y salían así en las propiedades del `.exe`). La **1.14.1**
  fue mantenimiento de las pruebas de UI. La **1.14.0** trajo el **Tier 7 — #37: partición FAT32 pequeña al
  reinicializar en discos grandes**, con tamaño seleccionable 1/2/4/8/16/32 GB, verificado con hardware real;
  incluye el **fix de `Initialize-Disk`** que hacía fallar todo Reinicializar unidad en algunos USB, y el
  pulido UX posterior: enlace directo, aviso en Iniciar, estados vacíos de Salud/Conexión y `FormatBytes` sin
  «.0». La
  **1.13.0** trajo el **Tier 6: pulido UX/UI**: InfoBar de unidad protegida,
  `ConfirmDialog` con foco+Enter, barra de capacidad, iconos por tipo de unidad, estado vacío del selector,
  salud coloreada, validación inline de etiqueta, progreso en la barra de tareas y estado de error en la
  barra de progreso; incluye también el **fix de ancho** de `LegalTextDialog`/`ConfirmDialog`, que desbordaban
  la ventana. La **1.12.0** trajo el **Tier 5**: relicenciado a **GNU GPL v3.0** (antes MIT) con licencia
  visible in-app, **disclaimer** de uso destructivo/sin garantía, **avisos de terceros**, **aviso de
  privacidad** y **donaciones opcionales (PayPal)**. La **1.11.0** completó el **Tier 4**: **#16 umbrales de color + estado + botón Actualizar
  en S.M.A.R.T.**, **#19 filtro + exportación CSV del historial**, **#20 editar/reordenar presets**, **#22 accesibilidad
  (nombres accesibles, aceleradores de menú, F5)** y **#17 refresco automático de unidades (`WM_DEVICECHANGE`)**. La **1.10.1**
  fue un **fix UI/UX de adaptación a DPI/escalado** (ventana dimensionada por DPI +
  diálogos con `MaxWidth` para que el texto se ajuste y no se corte en pantallas de alta densidad). La **1.10.0** trajo los
  **Tier 4 quick wins**: **#15 IOPS en el benchmark**, **#14 pasadas de borrado seguro configurables (1/3/7)**, **#18 idioma
  automático en el primer arranque** y **#21 changelog en el aviso de actualización**. La **1.9.1** fue mantenimiento: correcciones de una
  revisión de código + limpieza, sin tocar la lógica de formateo (fix del doble corchete en la unidad protegida,
  `MaxLength` de etiqueta dinámico por FS, validación de etiqueta compartida formato/reinit, borrado de
  `MainForm.resx` y de 6 claves de localización sin uso). La **1.9.0** refinó el **benchmark (#9)** al perfil estilo CrystalDiskMark:
  SEQ Q8 + RND4K, E/S sin caché, mediana de pasadas. La **1.8.0** trajo **Tier 3 #10 (presets personalizados) +
  #11 (más idiomas: PT/FR/IT) + #12 (aviso al terminar)**. La **1.7.1** corrigió el disparo del diálogo de novedades al
  actualizar desde una versión sin `LastVersionSeen`. La **1.7.0** trajo **Tier 2 #8 (reinicializar) + #9 (benchmark)**
  → **Tier 2 completado**, y el diálogo de novedades.
  La 1.6.0 trajo **#6 (chkdsk) + #7 (protección de escritura)**; la 1.5.0 el **#5 S.M.A.R.T. ampliado**; la 1.4.0 el
  **Tier 1** (persistencia, ETA/velocidad, borrado seguro con progreso real, visor de historial); la 1.3.0 el rediseño
  UI/UX inspirado en Win11Debloat + fixes de tema. La auto-actualización silenciosa aplica **desde la 1.2.2 en adelante**
  (1.2.2 corrigió el bug de cierre que cancelaba `Application.Current.Exit()` por `_isBusy`). La 1.2.0 sigue obsoleta/rota.
- **Hoja de ruta:** ver [`ROADMAP.md`](ROADMAP.md) (Tier 2 y **Tier 3 completados** — **#13 winget/firma descartado**). **Tier 4 COMPLETADO** (#14–#22; v1.10.0 + v1.11.0). **Tier 5 COMPLETADO** (#23–#27; v1.12.0): relicenciado a GPLv3 + apartados legales in-app + donaciones PayPal. **Tier 6 COMPLETADO** (#28–#36; v1.13.0): pulido UX/UI. **Tier 7 COMPLETADO** (#37; pendiente de publicar): partición FAT32 pequeña al reinicializar en discos grandes.
- **Stack:** C# 13 · .NET 10 · **WinUI 3** (Windows App SDK 1.8, unpackaged, `net10.0-windows10.0.19041.0`) · xUnit · FlaUI (pruebas UI) · Inno Setup 6

---

## 1. Qué es

Utilidad de **formateo y gestión de unidades** para Windows (NTFS/exFAT/ReFS/FAT32/FAT),
con diagnóstico S.M.A.R.T., verificación de capacidad real (detección de USB falsos),
borrado seguro, presets, interfaz bilingüe ES/EN, tema claro/oscuro, **actualizaciones
automáticas vía GitHub Releases** e **instalador**. Requiere privilegios de administrador
(`requireAdministrator` en `app.manifest`).

## 2. Arquitectura (separación por capas)

```
src/FormatDiskPro/
├─ Core/            Lógica PURA y testeable (sin UI, sin procesos, sin red)
│  ├─ FormatLogic.cs     Construcción de comandos de formato, parseo de %, formato de bytes, MaxLabelLength
│  ├─ UpdateChecker.cs   Comparación de versiones (parseo de tags, IsNewer)
│  ├─ AppInfo.cs         Versión en ejecución + coordenadas del repo GitHub
│  ├─ Presets.cs         Configuraciones de formato predefinidas
│  ├─ Throughput.cs      ETA + formato de velocidad (operaciones largas)
│  ├─ ReinitPlan.cs      Estilo MBR/GPT por tamaño + parseo de la nueva letra (#8)
│  ├─ Benchmark.cs       Tamaño de prueba + cálculo de velocidad (#9)
│  └─ ReleaseNotes.cs    Markdown de las notas de versión → texto plano (diálogo de novedades)
├─ Services/        Efectos colaterales (procesos / disco / red)
│  ├─ DiskService.cs       S.M.A.R.T., nº de disco, protección, expulsión (PowerShell -EncodedCommand)
│  ├─ CapacityVerifier.cs  Verificación de capacidad real (patrón anti-aliasing por bloque)
│  ├─ SecureWipe.cs        Borrado seguro con progreso real (sobrescritor por bloques)
│  ├─ CheckDisk.cs         chkdsk (comprobar/reparar) con streaming de progreso (#6)
│  ├─ ReinitDrive.cs       Reinicializar disco extraíble: clean + partición + formato (#8)
│  ├─ BenchmarkRunner.cs   Benchmark de lectura/escritura, no destructivo (#9)
│  ├─ AppSettings.cs       Persistencia de preferencias (settings.json: idioma/tema/unidad/presets/aviso)
│  ├─ Notifier.cs          Aviso al terminar: sonido + parpadeo de barra de tareas (Win32)
│  ├─ TaskbarProgress.cs   Progreso en el icono de la barra de tareas (ITaskbarList3, Win32) (#35)
│  ├─ UpdateService.cs     GitHub Releases API: consulta, descarga con progreso, lanza instalador
│  └─ History.cs           Auditoría en %AppData%\FormatDiskPro\history.log
├─ UI/              WinUI 3 (Windows App SDK)
│  ├─ MainWindow.xaml / MainWindow.xaml.cs  Ventana principal + orquestación
│  ├─ ConfirmDialog.xaml / .xaml.cs         ContentDialog — confirmación reforzada (escribir la letra)
│  └─ DriveViewModel.cs                     Binding model para el ComboBox de unidades
├─ Localization/    L.cs — diccionario ES/EN, L.T("clave")
├─ installer/       installer.iss (Inno Setup) + build-installer.ps1 → Output/ (gitignored)
└─ Program.cs       Punto de entrada

tests/FormatDiskPro.Tests/    Pruebas xUnit sobre la lógica de Core (269 tests)
tests/FormatDiskPro.UiTests/  Pruebas de UI/UX con FlaUI (UIA3) sobre el .exe real — ver nota abajo
release.ps1                   Corte de versión en un paso (build + tag + GitHub Release)
FormatDiskPro.slnx            Solución (app + FormatDiskPro.Tests — UiTests NO está incluido, ver nota)
```

**Regla de oro:** la lógica de negocio testeable vive en `Core` (sin dependencias de
WinUI/Process/HttpClient). La UI y los servicios la consumen. Namespace único `FormatDiskPro`.

**Pruebas de UI (`FormatDiskPro.UiTests`, FlaUI/UIA3) — cobertura de la app completa, 23/23 verificado
con hardware real (2026-07-05, incluido el ciclo de vida destructivo completo Formatear → Reinicializar
→ Reinicializar con FAT32 pequeña) + 1 test lento marcado `Category=Slow` excluido del filtro por
defecto (ver más abajo):**
lanza el `FormatDiskPro.exe` compilado y lo automatiza vía UI Automation, localizando controles por
`AutomationId` (en WinUI, el `x:Name` del XAML se expone como `AutomationId` sin configuración extra).
Un `ContentDialog` puede coexistir con MÁS DE UN `ControlType.Window` en el árbol de MainWindow (WinUI
deja un proxy de Popup vacío, "Ventana emergente", junto al diálogo real, confirmado por volcado de
árbol): `DialogHelper.FindDialog` busca TODOS los candidatos y se queda con el que tiene contenido
(más hijos), con `WaitForDialogContaining(fixture, automationId)` como variante para cuando ese
heurístico no basta (p. ej. un `MenuFlyout` que tarda en cerrarse). Los botones nativos del diálogo
(`PrimaryButton`/`SecondaryButton`/`CloseButton`) sí heredan su AutomationId de la plantilla de WinUI
igual que un control nombrado en el XAML de la app. **Deliberadamente fuera de `FormatDiskPro.slnx`** para no
afectar `dotnet test`/`release.ps1` (que siguen corriendo solo los 269 tests rápidos de `Core`): son
pruebas E2E lentas que además **requieren una terminal elevada** (UIPI bloquea la automatización de un
proceso no elevado contra la ventana elevada del `.exe`, que pide `requireAdministrator`). `AppFixture`
comprueba la elevación al vuelo (falla con un mensaje claro en vez de colgarse), resuelve el `.exe` más
reciente bajo `src/FormatDiskPro/bin/**` (o `FORMATDISKPRO_EXE`), y hace **backup/restauración de
`%AppData%\FormatDiskPro\settings.json`+`history.log`** alrededor de cada corrida (la app es unpackaged:
sin esto, cambiar idioma/tema/unidad durante las pruebas filtraría esos cambios a la instalación real
del usuario).
- **24 tests** en 6 clases, todas bajo `[Collection("FormatDiskPro app")]` (una sola instancia de la
  app compartida, sin paralelismo entre ellas): `MainWindowTests` (smoke), `FormatOptionsUiTests`
  (checkboxes/combos/`RestoreButton`, sin tocar ninguna unidad), `MenuDialogsTests` (Acerca de/
  Licencia/Avisos de terceros/Novedades/Historial/Presets/chkdsk-cancelar — todos de solo lectura),
  `SettingsTests` (idioma/tema, con roll-back a español/automático al final), `DriveDiagnosticsTests`
  y `DestructiveLifecycleTests` (necesitan la unidad USB de pruebas, ver abajo). **23 corren en el
  filtro por defecto**; `VerifyCapacity_CompletesForTestDrive` (`DriveDiagnosticsTests`) lleva
  `[Trait("Category", "Slow")]` — escribe/relee prácticamente todo el espacio libre reportado (a
  propósito: así detecta capacidad falsificada) y en una unidad real de decenas de GB puede tardar
  más de 30 min, así que queda fuera de una corrida rutinaria.
- **Unidad USB de pruebas dedicada** (`TestDrive.cs`, localizada por **etiqueta de volumen**, no por
  letra — Reinicializar puede reasignarla): partición `utilidades` en un USB dedicado, autorizada
  explícitamente por el usuario para pruebas destructivas (2026-07-05; el disco físico solo tenía esa
  partición y otra "Bios Flash" desechable, que Reinicializar fusionó en una sola al reinicializar el
  disco completo — comportamiento esperado, ver registro de cambios). `DriveDiagnosticsTests` corre
  S.M.A.R.T./chkdsk-solo-comprobar/Benchmark contra ella — no destructivo (Benchmark escribe un
  archivo propio y lo borra al terminar). `DestructiveLifecycleTests` añade las guardas de
  `ConfirmDialog` (letra incorrecta deshabilita el botón; Cancelar no ejecuta nada — corren siempre,
  sin riesgo) y un ciclo de vida completo (Formatear → Reinicializar normal → Reinicializar con FAT32
  pequeña si la unidad cualifica, ≥32 GB) que **sí borra la unidad de verdad**, gateado tras la
  variable de entorno `FORMATDISKPRO_ALLOW_DESTRUCTIVE=1` (sin ella, falla con mensaje claro en vez de
  arriesgar cualquier unidad conectada por accidente). Al terminar, relabela la unidad de vuelta a
  `TestDrive.PrimaryLabel` directamente por `DriveInfo` (no por la UI): xUnit no garantiza el orden
  entre `[Fact]` de una clase, así que sin esto los otros tests de la misma clase quedarían en flaky
  según el orden en que corra este.
- Ejecutar: `dotnet build -c Release` (o Debug) del proyecto principal, luego, **desde una terminal
  como administrador**: `dotnet test tests/FormatDiskPro.UiTests --filter "Category!=Slow"` (con la
  USB de pruebas conectada para `DriveDiagnosticsTests`/`DestructiveLifecycleTests`; con
  `FORMATDISKPRO_ALLOW_DESTRUCTIVE=1` además para el ciclo de vida completo). Para `VerifyCapacity`
  aparte: `dotnet test tests/FormatDiskPro.UiTests --filter "Category=Slow"` con tiempo de sobra
  (puede superar los 30 min en discos grandes).
- **Nunca lanzar dos corridas de `dotnet test` de este proyecto en paralelo** contra la misma app/unidad:
  cada una lanza su propia instancia elevada del `.exe` y ambas compiten por el mismo `DrivePicker`
  real y por `%AppData%\FormatDiskPro\settings.json`/`history.log` — confirmado que produce fallos
  imposibles de diagnosticar (etiqueta cambiada a medio camino, "unidad no encontrada" en pruebas que
  antes pasaban) hasta notar los dos procesos corriendo a la vez.

## 3. Estado actual

- ✅ Build de solución: **0 advertencias / 0 errores** (WinUI 3, WAS 1.8).
- ✅ Pruebas: **289/289** (`dotnet test`) — 269 previas + 20 del Tier 8 (6 de verificación del instalador,
  7 de neutralización de fórmulas en el CSV y 7 de contraste WCAG AA de la paleta de severidad).
- ✅ UI tests: **17 pasan / 6 omitidos / 0 fallan** sin la USB de pruebas conectada (antes esos 6 salían en
  **rojo**: ver Tier 9 #41). Ya integrables en el release con `-UiTests`.
- ✅ **Tier 9 — Infraestructura (#41, #42, #45), COMPLETADO (2026-07-13, sin publicar):** los UI tests entran
  en el pipeline (`release.ps1 -UiTests`), el instalador está probado **end-to-end** (instalación limpia +
  actualización in-place con el flujo silencioso real, que cierra la app y la relanza), y se arregló que
  `release.ps1` **corrompiera la codificación del `.csproj` en cada bump** —el `.exe` publicado mostraba el
  nombre del autor destrozado en sus propiedades—, hallado inspeccionando el binario instalado.
- ✅ **README con capturas** (`docs/screenshots/`), generadas por `tools/capture-screenshots.ps1` conduciendo
  la app real: no se editan a mano, se regeneran.
- 🏁 **PROYECTO TERMINADO (2026-07-13).** Hoja de ruta cerrada; las dos ideas mayores (elevación `asInvoker`,
  ventana redimensionable) **descartadas** — ver §4 y §6.
- ✅ **Tier 8 — Seguridad y confianza (#38–#40), COMPLETADO (2026-07-12, publicado en v1.15.0):**
  **#38** la auto-actualización descargaba el instalador y lo **ejecutaba elevado sin comprobar nada**; ahora
  verifica firma Authenticode y, si no la hay (no se firma: #13 descartado), el **SHA-256** contra el asset
  `*.exe.sha256` del release — sin ninguna de las dos, borra el instalador y aborta. **#39** `ToCsv` escapaba
  según RFC 4180 pero no neutralizaba **fórmulas** (`=`/`+`/`-`/`@` → Excel las ejecuta). **#40** los colores de
  severidad se elegían a ojo; ahora un test mide su **contraste real (WCAG AA 4.5:1)** en ambos temas —los
  ocho casos ya pasaban, así que aquí no había bug latente: el valor es que no pueda aparecer.
- ✅ **Tier 7 — Partición FAT32 pequeña al reinicializar (#37), COMPLETADO Y VERIFICADO con hardware real
  (2026-07-02, publicado en v1.14.0):**
  Windows nunca permite crear un volumen FAT32 mayor de 32 GB (ni `Format-Volume` ni `format.com`; confirmado por
  investigación web: Microsoft solo lo levanta para `format.com` en builds **Insider** de 2026, no en Windows
  estable), así que el selector de sistema de archivos ya ocultaba FAT32 en discos ≥ 32 GB sin dar alternativa.
  Caso de uso real: flashear el BIOS/UEFI de una placa base desde un USB grande cuya utilidad de firmware solo
  lee FAT32. Nueva casilla `SmallFat32Check` en *Opciones de formato* (visible solo en unidades extraíbles ≥ 32 GB),
  que fuerza FAT32 y crea, **solo vía Herramientas → Reinicializar unidad…** (el flujo normal *Iniciar* no cambia:
  sigue ocultando FAT32 ahí porque de verdad no se puede formatear en el sitio un volumen tan grande), una
  partición con `New-Partition -Size <bytes>` (antes solo existía `-UseMaximumSize`), dejando el resto del disco
  físico sin asignar. **Tamaño seleccionable** (1/2/4/8/16/32 GB) vía `ComboBox` junto a la casilla, persistido.
  - Dónde: `Core/FormatLogic.Fat32MaxBytes` (constante compartida, dedup del literal ya duplicado en el selector),
    `Core/ReinitPlan.AllowedSmallFat32SizesGb`/`NormalizeSmallFat32SizeGb`/`SmallFat32PartitionBytes(int sizeGb)`
    (puros testeables; en el tramo máximo restan 4 MiB frente al límite exacto, margen ante redondeo/alineación
    de partición; mismo esquema que `SecureWipe.AllowedPasses`/`NormalizePasses`), `Services/ReinitDrive.RunAsync`
    (nuevo parámetro opcional `long? partitionSizeBytes`; `null` conserva el script de hoy byte a byte),
    `UI/MainWindow` (`SmallFat32Check` + `SmallFat32SizePicker` + hint explicativo localizado),
    `AppSettings.SmallFat32SizeGb` (persistencia, por defecto 32).
  - `MnuReinit_Click` resuelve `smallFat32`/el FS forzado **antes** de validar la etiqueta (FAT32 limita a 11
    caracteres, no los 32 de NTFS/ReFS que pudiera tener el selector) — evita validar contra el límite equivocado.
  - 5 claves de localización nuevas (`opt.smallFat32`, `opt.smallFat32Size`, `opt.smallFat32Hint`,
    `reinit.summaryFat32Small`, `reinit.doneBodyFat32Small`), 5 idiomas cada una.
  - **Fix de plataforma hallado en la primera prueba real** (ver registro de cambios): `Clear-Disk` no dejaba el
    disco en RAW en el hardware de prueba; `Initialize-Disk` se envuelve en `try/catch` tolerando específicamente
    "already been initialized". Afecta a *toda* Reinicializar unidad (#8), no solo a esta opción.
  - **Verificado por el usuario** con un USB de 64 GB real: Reinicializar normal y con FAT32 pequeña, ambos OK
    tras el fix de `Initialize-Disk`.
- ✅ **Tier 6 — Pulido UX/UI (#28–#36), COMPLETADO (2026-07-02, publicado en v1.13.0):**
  **#28** aviso de unidad protegida como `InfoBar` Fluent sobre las tarjetas (antes texto rojo en el
  `StatusText` del footer, que competía con el estado de operaciones); **#29** `ConfirmDialog` con foco
  inicial en la caja y **Enter** para confirmar cuando la letra coincide; **#30** barra de capacidad
  usado/libre en la tarjeta Unidad (`CapacityBar`, oculta si la unidad no está lista, con nombre accesible
  y tooltip `info.used`). Los tres **verificados por el usuario**. **#31** iconos por tipo de unidad en el
  selector (`DriveViewModel.Glyph`: USB/RAM/disco fijo, siguen el color del texto); **#32** estado vacío
  del selector (`PlaceholderText` con la clave `drive.none`); **#33** línea «Salud:» de la tarjeta
  coloreada con `Core/SmartInfo.HealthLevel` (puro, +8 tests) vía `HealthDialog.LevelBrush` (ahora
  estático compartido; el diálogo S.M.A.R.T. usa el mismo nivel para su fila de estado, ganando el texto
  localizado y sin pintar en rojo un estado no reportado). **#34** validación inline de la etiqueta
  (`Core/FormatLogic.ValidateLabel`, puro, +15 tests, compartido por el hint en vivo y el modal de envío).
  **#35** progreso en la barra de tareas (`Services/TaskbarProgress`, helper COM/Win32 propio, no-op si el
  shell no lo soporta). **#36** `ProgressBar.ShowError` al fallar/cancelar (`_lastOperationFailed` +
  `_cancelRequested`, combinados en `EndOperation`). **#31–#36 sin verificación visual explícita** del
  usuario antes de publicar (build+tests en verde; #28–#30 sí se validaron con capturas).
- ✅ **Fix (mismo día):** `LegalTextDialog`/`ConfirmDialog` (`MaxWidth` 460→420 DIP) desbordaban el ancho de
  la ventana (500 DIP) — ver entrada del registro de cambios; verificado por el usuario.
- ✅ **Tier 5 — confianza/legal/sostenibilidad (v1.12.0, publicado):**
  Relicenciado **MIT → GNU GPL v3.0**: `LICENSE` con el texto oficial (descargado de gnu.org), embebido vía `.csproj`
  como recurso (`FormatDiskPro.LICENSE.txt`) + `THIRD-PARTY-NOTICES.txt`. `Core/LegalText` lee ambos recursos.
  `UI/AboutDialog` (Acerca de ampliado: descripción, versión, copyright/licencia, **disclaimer** de uso destructivo/sin
  garantía, **privacidad**, botones *GitHub* y *Apoyar el proyecto*); `UI/LegalTextDialog` (visor con scroll para
  *Ayuda → Licencia* y *Ayuda → Avisos de terceros*). `AppInfo.DonateUrl` (PayPal, URL real ya configurada) + `RepoUrl`;
  donación **opcional**, sin bloquear nada. `.github/FUNDING.yml` añadido.
- ✅ **Tier 4 COMPLETADO (resto publicado en 1.11.0):**
  **#16 S.M.A.R.T. con umbrales** — `Core/SmartInfo` (`SmartLevel` + `TemperatureLevel`/`WearLevel`/`ErrorLevel` puros);
  el `HealthDialog` colorea temperatura/desgaste/errores por rango con **texto de estado** (Normal/Atención/Crítico) y
  un botón **Actualizar** que reconsulta sin cerrar. **#19 Historial filtro/CSV** — `HistoryEntry.Matches` (búsqueda +
  categoría/resultado) y `HistoryEntry.ToCsv` (RFC 4180), puros; el visor añade búsqueda, dos filtros y **Exportar CSV**
  (lo filtrado, vía `FileSavePicker`). **#20 Editar/reordenar presets** — `Presets.IsRenameAvailable` (puro); el diálogo
  permite renombrar, actualizar la config a la actual y subir/bajar, persistiendo el orden. **#22 Accesibilidad** —
  `AutomationProperties.Name`+tooltip en botones de icono, **aceleradores** de menú (Alt+inicial localizada) y **F5**
  para refrescar. **#17 Refresco automático** — `Core/DeviceChange` (puro) + subclassing de la ventana para escuchar
  `WM_DEVICECHANGE` (llegada/retirada de dispositivo) y recargar unidades con **debounce** de 600 ms.
- ✅ **Tier 4 quick wins (publicado en 1.10.0):**
  **#15 IOPS** — `Core/Benchmark.Iops` (`bytes/s ÷ 4096`) + `Random4KBlockBytes`; el diálogo de resultado del
  benchmark muestra las IOPS entre paréntesis junto a los MB/s del 4 KiB aleatorio (estilo CrystalDiskMark).
  **#14 Pasadas configurables** — selector **1/3/7** en *Opciones de formato* (`WipePassesPicker`, activo solo con
  *Borrado seguro* marcado), persistido en `AppSettings.SecureWipePasses` y mostrado en la confirmación (`×N` si > 1);
  helpers puros `SecureWipe.AllowedPasses`/`NormalizePasses`. **#18 Idioma automático** — `L.FromCulture` (puro) siembra
  el idioma desde `CultureInfo.CurrentUICulture` solo en el primer arranque (gated por `AppSettings.LoadedFromFile`);
  después manda la elección del usuario. **#21 Changelog en el aviso** — `MainWindow.ShowUpdateAvailableAsync` muestra
  el cuerpo del release (ya en `ReleaseInfo.Notes`, vía `ReleaseNotes.ToPlainText`) con scroll antes de descargar.
- ✅ **Tier 3 #10 + #11 + #12 (publicado en 1.8.0):**
  **Presets personalizados** (`Core/Presets.NormalizeName`/`IsNameAvailable` puros, `AppSettings.UserPresets`,
  `UI/PresetsDialog`): guardar la config actual con nombre y eliminarlos; aparecen en *Presets* y se gestionan desde
  *Presets → Gestionar presets…*. **Más idiomas** (`Localization` refactorizado a arreglo por idioma; añadidos
  **PT/FR/IT**; `L.FromCode`/`ToCode`): selección en *Configuración → Idioma*, persistida. **Aviso al terminar**
  (`Services/Notifier`): sonido + parpadeo de la barra de tareas al acabar operaciones ≥ 10 s, solo si la ventana no
  está en primer plano; interruptor *Configuración → Avisar al terminar* (`AppSettings.NotifyOnFinish`, por defecto on).
- ✅ **Diálogo de novedades (1.7.0, corregido en 1.7.1):** tras una actualización, al primer arranque de la nueva versión
  se muestran las **novedades** de la versión instalada (cuerpo del release de GitHub vía `UpdateService.GetReleaseByTagAsync`,
  convertido a texto con `Core/ReleaseNotes.ToPlainText`), una sola vez. También bajo demanda en *Ayuda → Novedades…*
  (`UI/WhatsNewDialog`), con botón "Ver en GitHub". La detección de "actualización" usa `AppSettings.LastVersionSeen` y,
  cuando la versión previa no guardaba ese campo, el flag `AppSettings.LoadedFromFile` (existía `settings.json` = uso previo)
  para distinguir actualización de instalación nueva (fix 1.7.1).
- ✅ **Tier 2 #8 (reinicializar unidad) + #9 (benchmark) — publicado en 1.7.0 → Tier 2 completado:**
  **Reinicializar** (`Core/ReinitPlan.cs` puro + `Services/ReinitDrive.cs` con streaming de etapas +
  `DiskService.GetDiskNumberAsync`): *Herramientas → Reinicializar unidad…* limpia el disco extraíble y recrea
  una única partición formateada (cmdlets de Storage, no diskpart). **Solo extraíbles** + guardas reforzadas
  (bloqueo de sistema/protegido, disco físico ≠ Windows, confirmación escribiendo la letra). **Benchmark**
  (`Core/Benchmark.cs` puro + `Services/BenchmarkRunner.cs`): *Herramientas → Benchmark rápido…* mide
  la velocidad con un archivo temporal de ~512 MB (no destructivo), permitido en cualquier unidad lista.
  **Rediseñado (2026-06-23), perfil estilo CrystalDiskMark:** cuatro cifras — **secuencial** (1 MiB, cola **Q8**
  con overlapped I/O vía `Task.WhenAll`) y **4 KiB aleatorio** (Q1), lectura y escritura. E/S **sin caché**
  (`FILE_FLAG_NO_BUFFERING` vía `RandomAccess` + buffer alineado con `GCHandle`), medición por **ventanas de
  tiempo** (adapta unidades rápidas/lentas) y **mediana** de 3 pasadas. `BenchmarkResult` = `Sequential`/`Random4K`
  (cada uno con lectura/escritura); el diálogo de resultado muestra las cuatro.
- ✅ **Tier 2 #6 (chkdsk) + #7 (protección de escritura) — publicado en 1.6.0, probado por el usuario:**
  `Services/CheckDisk.cs` + ítem *Herramientas → Comprobar errores (chkdsk)…* (modo solo-comprobar/reparar,
  progreso parseado, reparación bloqueada en disco de sistema); `DiskService.IsDiskReadOnlyAsync`/`ClearReadOnlyAsync`
  con chequeo automático al pulsar Iniciar + ítem *Quitar protección de escritura…*.
- ✅ **Tier 2 #5 — S.M.A.R.T. ampliado (publicado en 1.5.0, probado por el usuario en claro/oscuro):**
  `Core/SmartInfo.cs` (modelo+parser), `DiskService.GetSmartAsync`, `UI/HealthDialog.xaml` abierto desde
  *Herramientas → Salud del disco (S.M.A.R.T.)…*: temperatura, horas de encendido, desgaste SSD, RPM y errores;
  consulta bajo demanda; "No disponible" para unidades sin contadores (USB).
- ✅ Release **v1.6.0** publicado en GitHub con `FormatDiskPro-1.6.0-setup.exe` adjunto (probado por el usuario, OK).
- ✅ **Tier 1 (publicado en 1.4.0):** persistencia de preferencias (`Services/AppSettings.cs` →
  `%AppData%\FormatDiskPro\settings.json`: idioma/tema/última unidad); **ETA + velocidad** en operaciones con
  bytes (`Core/Throughput.cs`, ventana deslizante en el timer); **borrado seguro con progreso real**
  (`Services/SecureWipe.cs`, sobrescritor propio que reemplaza a `cipher /w`); **visor de historial integrado**
  (`Core/HistoryEntry.cs` parser + `UI/HistoryDialog.xaml`). Decisión: 1 pasada por defecto en el wipe.
- ✅ Barra de título: **`Window.ExtendsContentIntoTitleBar = true`** (a nivel de Window) + `PreferredHeightOption = Tall` (48 px) + icono 16 px + `CaptionTextBlockStyle`. WinUI tematiza solo los botones caption (min/max/cerrar) según el tema **efectivo** del contenido.
- ✅ Tema: sigue el tema del sistema automáticamente (`UISettings.ColorValuesChanged`); opción manual Automático/Claro/Oscuro en menú. Colores derivados de recursos/valores Fluent (`SystemFillColorCritical`, `TextFillColorPrimary`).
- ✅ **Sistema de diseño (UI/UX) inspirado en Win11Debloat (2026-06-21):** contenido organizado en **tarjetas de
  sección** (`Border` con borde de 1px + esquinas 8px) y **encabezados con icono + título de acento**. El acento usa
  el **color de Windows del usuario** vía recursos Fluent (`AccentTextFillColorPrimaryBrush`), adaptándose a claro/oscuro
  sin colores hardcodeados. Tokens centralizados en `UI/Theme/AppTheme.xaml` (estilos `AppCardStyle`, `SectionIconStyle`,
  `SectionTitleStyle`, `InfoTextStyle`, `HintTextStyle`, `CardDividerStyle`, `FooterBarStyle`). **Barra de acción inferior**
  (footer) separada por una línea: `Cerrar` a la izquierda, `Iniciar` (acento) a la derecha. Ventana **500×900** (antes 840).
- ✅ Ventana fija no redimensionable (`OverlappedPresenter.IsResizable/IsMaximizable = false`); Mica con degradación a Acrylic si el sistema no la soporta.
- ⚠️ **Instalador/empaquetado:** corregido el bug que hacía crashear la 1.2.0 al iniciar (`dotnet publish`
  no incluía `FormatDiskPro.pri`; ahora un target MSBuild lo copia). Instalador limpia la instalación
  previa (`[InstallDelete]`), cierra la app vía `AppMutex`, hace **auto-actualización silenciosa con
  relanzado**, y soporta **firma Authenticode** opcional (`build-installer.ps1`/`release.ps1`). **1.2.1
  publicada** sin firmar (un cert OV/EV es lo único que quitaría SmartScreen).
- ✅ **Auto-update — bug del cierre corregido (1.2.2):** `AppWindow_Closing` cancelaba
  `Application.Current.Exit()` durante la auto-actualización (porque `_isBusy` seguía activo por la descarga)
  y mostraba "Operación en progreso", dejando la app vieja abierta y reteniendo el `AppMutex`/los archivos →
  el instalador no podía reemplazarla. Añadido flag `_closingForUpdate` que permite el cierre intencional.
  Este bug venía idéntico desde la 1.1.0 (WinForms) y la 1.2.1. **Nota:** la prueba 1.1.0→1.2.1 muestra el
  instalador **interactivo** (diálogo de idioma + asistente) porque la 1.1.0 publicada lanza el instalador
  **sin** `/VERYSILENT` (código congelado); el flujo silencioso solo aplica desde la versión con este fix
  (1.2.2) en adelante.
- ✅ Verificación funcional pendiente: formato real en USB, verificación de capacidad, historial, actualizaciones.

## 4. Decisiones y convenciones clave

- **Protección de unidades:** SOLO se protege el **disco de sistema** (donde está Windows),
  detectado con `IsSystemDrive()` (`Path.GetPathRoot(Environment.SystemDirectory)[0]`).
  El resto (removibles, discos de datos fijos, RAM) **sí** se pueden formatear.
- **Seguridad PowerShell:** comandos vía `-EncodedCommand` (Base64 UTF-16LE). Validar
  `char.IsLetter(letter)` antes de interpolar. Etiqueta escapada (`'`→`''`) en `Format-Volume`;
  para `format.com` se usa `ArgumentList` (escape por argumento).
- **Verificación de capacidad:** bloques de 8 MB (unidad del patrón anti-aliasing) agrupados
  en archivos de 1 GB (seguro en FAT32, pocos archivos).
- **Publicación:** `dotnet publish` **self-contained** `win-x64` (`WindowsAppSDKSelfContained=true`) →
  el usuario final NO necesita instalar .NET ni Windows App SDK.
- **La app corre SIEMPRE elevada (`requireAdministrator` en `app.manifest`) — NO REABRIR (2026-07-13).**
  Se evaluó el modelo `asInvoker` + worker elevado por named pipe (el de WingetUSoft) y **se descartó**: esta
  app formatea, borra y reinicializa discos, así que **casi todo lo que hace necesita administrador**. El
  "menor privilegio" sería nominal (pediría UAC igual, solo más tarde y más veces) a cambio de refactorizar
  **todos** los `Services`, que hoy asumen proceso elevado. Consecuencia asumida: los UI tests y
  `tools/capture-screenshots.ps1` **exigen terminal elevada**, y ambos lo validan con un mensaje claro.
- **La ventana es de tamaño FIJO (500×900) — NO REABRIR (2026-07-13).** Es un **diálogo de tarea**, no un
  espacio de trabajo: ningún contenido gana con más ancho y el layout de tarjetas ya cabe entero. No portar
  `WindowSizing`/`ContentScroller` de WingetUSoft: allí la ventana lista paquetes en una tabla y lo
  necesitaba; aquí resolvería un problema que no existe.
- **Instalador (Inno Setup):** `AppId = {CEC07916-C9B5-4EA8-9102-3273384395AD}` — **no cambiar
  nunca** (permite actualización in-place). `PrivilegesRequired=admin`, `CloseApplications=yes`.
- **Versionado:** fuente única en `src/FormatDiskPro/FormatDiskPro.csproj` `<Version>`.
  El updater compara esa versión contra el `tag_name` del último release (`UpdateChecker.IsNewer`).
- **Actualizaciones:** chequeo silencioso al iniciar (`OnShown`) + manual (Ayuda → Buscar
  actualizaciones). Si hay versión mayor, descarga el asset `*.exe` (preferente "setup"), lo **verifica** y
  lo lanza.
- **Verificación del instalador (desde v1.15.0, #38) — NO ROMPER:** el instalador se ejecuta **elevado**, así
  que antes se comprueba que es el del proyecto: firma Authenticode válida → OK; si no, **SHA-256** contra el
  asset `*.exe.sha256`. Sin ninguna de las dos, se **borra y no se ejecuta**. Consecuencias operativas:
  - **Todo release debe subir su `.sha256`** o la auto-actualización lo rechazará. `build-installer.ps1` lo
    genera (**después** de firmar, si se firma: firmar cambia el binario) y `release.ps1` lo sube como segundo
    asset y **aborta si falta**.
  - La descarga vive en su propio método (`DownloadToFileAsync`) **a propósito**: su `FileStream` es
    `FileShare.None` y debe cerrarse **antes** de verificar. Si se vuelve a fusionar con la verificación, esta
    no podrá ni abrir el archivo ("lo está usando otro proceso" — el proceso es la propia app) y la
    actualización fallará **siempre**. Hay test que lo caza.
  - `HttpCompletionOption.ResponseHeadersRead` en la descarga **no es decorativo**: deja el cuerpo fuera del
    `Timeout` de 30 s del `HttpClient`. Con `ResponseContentRead`, un instalador de ~60 MB fallaría en toda
    conexión que no llegue a 2 MB/s.
- **Publicación del instalador a `%TEMP%` (no dentro del repo) — MAX_PATH:** Inno Setup no usa las APIs de
  rutas largas, y el publish self-contained del Windows App SDK trae nombres de hasta 76 caracteres
  (`WindowsAppSdk.AppxDeploymentExtensions.Desktop-EventLog-Instrumentation.dll`). Sumados a
  `<repo>\src\FormatDiskPro\bin\Release\<tfm>\win-x64\publish\`, pasan de 260 en cuanto el repo no cuelga de
  una carpeta corta, e ISCC aborta con «El sistema no puede encontrar la ruta especificada» sin decir cuál.
  Publicar en `%TEMP%\FormatDiskPro-publish` lo deja resuelto sea cual sea la ubicación del repo.
- **`Microsoft.WindowsAppSDK` con versión EXACTA (`1.8.260529003`), no flotante — no volver a `1.8.*`:**
  con comodín, NuGet resuelve el paquete más nuevo que encaje y **el conjunto de archivos publicados cambia
  solo**, sin tocar el repo. Así apareció, de un día para otro, el archivo que rompió el build (ver MAX_PATH
  arriba). Subir de versión debe ser un cambio **deliberado y probado**, no un efecto colateral de la fecha en
  que se compile.
- **Scripts PowerShell** que se ejecuten en Windows PowerShell 5.1 deben guardarse con **BOM UTF-8**
  (si no, los acentos rompen el parser). `release.ps1` ya lo tiene.
- **El `.csproj` también va con BOM UTF-8, y NO se lee con `Get-Content -Raw`.** Sin BOM, PS 5.1 lo lee con la
  página de códigos ANSI: los bytes UTF-8 de `é` se vuelven `Ã©` y, al reescribirlo, la corrupción queda
  grabada. Como el bump de versión ocurre en **cada** release, el daño se acumulaba capa sobre capa y el
  nombre del autor de `<Authors>`/`<Copyright>` acabó destrozado **en las propiedades del `.exe` publicado**.
  `release.ps1` usa `[System.IO.File]::ReadAllText` (detecta el BOM) y reescribe **conservándolo**. Cualquier
  script que toque el `.csproj` debe hacer lo mismo.
- **git + PS 5.1 + salida capturada = trampa.** git escribe por stderr en su operación **normal** (el resumen
  del `push`, los avisos de CRLF), sin que nada haya fallado. Si la salida del script se captura
  (`| Tee-Object`, `2>&1 |`, un wrapper), PS 5.1 convierte cada línea de stderr de un exe nativo en
  `NativeCommandError` y, con `$ErrorActionPreference = "Stop"`, **aborta aunque git devuelva 0**. En un
  `push` eso deja el release **a medias**: rama subida, sin tag ni GitHub Release (ocurrió al cortar la
  v1.15.0). Por eso los git que mutan estado van por **`Invoke-Git`** en `release.ps1`, que baja la
  preferencia mientras corre git y decide por `$LASTEXITCODE` (el único indicador fiable).
- **`gh` (GitHub CLI):** si no está autenticado, los scripts reutilizan la credencial de git
  cacheada (`git credential fill` → `GH_TOKEN`), solo en local, sin imprimir el token.
- **Skills de buenas prácticas** en `.agents/skills/` (registro `awesome-copilot`); ver la
  tabla de uso en `.claude/CLAUDE.md`. Framework de pruebas del proyecto: **xUnit** (`csharp-xunit`);
  no usar mstest/nunit/tunit aunque estén presentes.

## 5. Tareas comunes

| Tarea | Comando |
|-------|---------|
| Compilar | `dotnet build -c Release` |
| Pruebas | `dotnet test` |
| UI tests (app real, **terminal elevada**) | `dotnet test tests\FormatDiskPro.UiTests --filter "Category!=Slow"` |
| Regenerar capturas del README (**terminal elevada**) | `.\tools\capture-screenshots.ps1` |
| Generar instalador | `src\FormatDiskPro\installer\build-installer.ps1` |
| **Publicar versión** | `.\release.ps1 -Version X.Y.Z -UiTests` (usa `-DryRun` para simular) |

`release.ps1` hace: validar → tests → bump `<Version>` → build instalador → commit + tag `vX.Y.Z`
→ push → `gh release create` con el instalador **y su `.sha256`**. Flags: `-DryRun`, `-SkipTests`,
**`-UiTests`**, `-AllowDirty`, `-NotesFile`.

> **Corte recomendado:** `.\release.ps1 -Version X.Y.Z -UiTests` desde una **terminal elevada**. Con
> `-UiTests` el release también ejerce la **app real** (FlaUI/UIA3) y no sale si falla.
>
> Los UI tests **no están en la solución** (si lo estuvieran, el `dotnet test` de los unitarios los
> arrastraría siempre): se lanzan por ruta y solo con el flag. Requieren **elevación** —la app es
> `requireAdministrator` y un proceso no elevado no puede automatizar su ventana—, y el script lo valida.
> Los 6 que necesitan la **USB de pruebas** (partición extraíble etiquetada `utilidades`) **se omiten solos**
> si no está conectada: omitido ≠ fallido. `FORMATDISKPRO_ALLOW_DESTRUCTIVE=1` hace que la suite **formatee**
> esa USB de verdad; `release.ps1` **aborta** si la encuentra activa.

## 6. Estado del proyecto: TERMINADO (2026-07-13)

**Tiers 1–9 completados. No hay trabajo pendiente.** La hoja de ruta ([`ROADMAP.md`](ROADMAP.md)) está
cerrada: lo que queda fuera está **deliberadamente** fuera. Las dos ideas mayores que restaban
—**elevación `asInvoker`** y **ventana redimensionable**— se **descartaron** el 2026-07-13: ambas revertían
una decisión de diseño que es correcta para lo que este producto es (ver §4, "NO REABRIR").

**Lo único a vigilar en el próximo corte** (no es trabajo, es una comprobación):

- **La verificación del instalador (#38) aún no se ha ejercido en producción.** Solo actúa al actualizar
  **desde** una versión ≥ 1.15.0, y los clientes ≤ 1.14.1 llegaron a la 1.15.0 con el código viejo, que no
  verificaba nada. El primer uso real será **1.15.0 → la siguiente versión**: comprobar entonces que la app
  acepta el instalador. Si el release no lleva su `.sha256`, **lo rechazará** (`release.ps1` aborta si falta).
- **`master` va por delante de la v1.15.0 publicada:** lleva el Tier 9 y las capturas. La v1.15.0 que está en
  Releases todavía muestra el nombre del autor **corrupto** en las propiedades del `.exe` (#45); se corrige
  solo con el siguiente corte.

Pulido opcional, sin impacto: más capturas (hoy 3), renombrar el `Name` interno del form, y S2 (menor, por
diseño: la validación de etiqueta no rechaza `'`, pero el escape lo cubre).

## 7. Cómo mantener este documento

1. Tras un cambio relevante, añadir una entrada en el **Registro de cambios** (fecha absoluta).
2. Actualizar **Estado actual** (versión, tests, lo publicado, pendientes).
3. Si cambia una convención o decisión, reflejarlo en la sección 4.
4. Commitear este archivo junto con el cambio para que viaje entre equipos.

---

## Registro de cambios

### 2026-07-13 — decisión: proyecto TERMINADO — descartadas las dos ideas mayores

Cerrada la hoja de ruta. Las dos únicas ideas que quedaban revertían una decisión de diseño deliberada, y el
usuario las **descarta** porque esa decisión es **correcta para lo que el producto es**:

- **Elevación `asInvoker` + worker elevado por named pipe — NO.** Era el patrón de WingetUSoft (la app corre
  sin privilegios y eleva solo lo que los exige). Aquí no aporta: FormatDiskPro **formatea, borra y
  reinicializa discos**, así que casi todo lo que hace necesita administrador. El "menor privilegio" sería
  **nominal** —pediría UAC igual, solo más tarde y más veces— a cambio de refactorizar **todos** los
  `Services`, que asumen proceso elevado. Pedirlo de entrada es honesto: el manifiesto lo declara en vez de
  escalar por sorpresa. Consecuencia asumida y ya gestionada: los UI tests y el script de capturas exigen
  terminal elevada, y ambos lo validan con un mensaje claro.
- **Ventana redimensionable + snap layouts — NO.** La ventana es **fija (500×900) por diseño**: es un
  **diálogo de tarea**, no un espacio de trabajo. Ningún contenido gana con más ancho (no hay tablas ni
  listas largas) y el layout de tarjetas ya cabe entero. Portar `WindowSizing`/`ContentScroller` de
  WingetUSoft resolvería un problema que **aquí no existe** — allí la ventana lista paquetes en una tabla, y
  sí lo necesitaba.

Reflejado en `ROADMAP.md` (nuevo bloque *Estado: proyecto terminado* + *Decisiones cerradas*), en §4 y §6 de
este documento, y en el plan de comparación del workspace.

**Estado final:** Tiers 1–9 · 289 unitarios · 17 UI tests sobre la app real (6 omitidos sin la USB) ·
instalador verificado por SHA-256 y probado end-to-end · GPLv3 + avisos de terceros + donaciones · README con
capturas generadas. Único apunte para el próximo corte: `master` va por delante de la v1.15.0 publicada.

### 2026-07-13 — docs: capturas de pantalla del README, generadas conduciendo la app real

El README de un proyecto publicado no tenía **ninguna captura** — para quien llega desde LinkedIn o GitHub,
es lo primero que vende, y no había nada que ver hasta instalarlo.

- `tools/capture-screenshots.ps1` (nuevo, port del de WingetUSoft): conduce la app **real** por UI
  Automation, fija tema/idioma/unidad, abre el diálogo S.M.A.R.T. y fotografía la ventana. Las capturas se
  **regeneran**, no se editan a mano, así que no envejecen en silencio con la UI.
- Genera `docs/screenshots/`: `main-light.png`, `main-dark.png` y `health-dark.png` (486 × 893).
- **Diferencias con el script de WingetUSoft** (allí la app corre `asInvoker` y es redimensionable):
  - **Exige terminal elevada** (`Assert-Elevated`): la app es `requireAdministrator` y un proceso no elevado
    no puede automatizar su ventana. Falla con un mensaje claro en vez de dar capturas en negro.
  - El `settings.json` vive en **`%AppData%`** (no `%LocalAppData%`) y tiene **`LastDriveLetter`**: la unidad
    se preselecciona **desde el archivo**, sin pelearse con el `ComboBox` por UI Automation.
  - La ventana es de **tamaño fijo por diseño**: se coloca, pero **no se redimensiona**.
  - Elige por defecto una unidad **que no sea la del sistema**: esa sale como `[Protegido]` con los controles
    deshabilitados — una foto pésima del producto.
- Como en WingetUSoft, **respalda y restaura** el `settings.json` real del usuario (es el mismo archivo que
  usa su instalación), y prefiere el binario de **Release** (el que se distribuye), avisando si cae a Debug.

### 2026-07-13 — feat: Tier 9 (#41, #42, #45) — infraestructura y calidad

**#41 — Los UI tests entran en el pipeline de release.** `release.ps1` solo corría los unitarios; los UI
tests —los únicos que ejercen la app **real**— se lanzaban a mano. El obstáculo no era el script: **6 tests
fallaban por diseño** sin la USB de pruebas conectada (`TestDrive.RequireLetter` lanza), así que integrarlos
habría tumbado cualquier corte hecho sin el hardware delante.
- `tests/FormatDiskPro.UiTests/TestDriveFacts.cs` (nuevo): `[TestDriveFact]` y `[DestructiveFact]`, que ponen
  `Skip` al **descubrir** el test si falta su precondición. Un test **omitido** dice "no tengo el hardware";
  uno **fallido** dice "la app está rota" — confundirlos era justo el problema. Sin la USB: **17 pasan, 6 se
  omiten, 0 fallan** (antes, 6 en rojo).
- `release.ps1`: flag **`-UiTests`** con tres guardas — exige **terminal elevada** (la app es
  `requireAdministrator`: un proceso no elevado no puede automatizar su ventana, y los 17 fallarían de golpe
  por un motivo ajeno al código), **rechaza** `FORMATDISKPRO_ALLOW_DESTRUCTIVE=1` (un corte jamás debe
  formatear una unidad, ni aunque quien lo lanza la tuviera activa de una sesión de depuración) y **rechaza**
  `-UiTests -SkipTests` juntos, que se contradicen. Sin el flag, avisa de que el release sale sin ejercer la
  app real. La USB es **opcional**: si no está, se avisa y sus tests se omiten.
- Los UI tests **siguen fuera de la solución** a propósito: si estuvieran, el `dotnet test` de los unitarios
  los arrastraría siempre. Se lanzan por ruta, solo si se piden.
- Verificado de punta a punta: las dos guardas abortan cuando deben, y el camino feliz corre 289 unitarios +
  17 UI tests contra la app real.

**#42 — Instalador probado end-to-end** (contra el instalador real de la v1.15.0, con log de Inno):
- **Instalación limpia:** desinstala sin dejar rastro y **conserva** `%AppData%\FormatDiskPro` (datos del
  usuario: `settings.json`, `history.log`); instala 511 archivos en ~5 s, deja **una sola** entrada de
  desinstalación y sus accesos directos, y la app arranca.
- **Actualización in-place con el flujo silencioso real** (`/VERYSILENT /NORESTART /AUTOUPDATE=1`: los
  argumentos exactos de `UpdateService.LaunchInstaller`): cierra la app en ejecución, actualiza y **la relanza
  sola**.
- ⚠️ **Hallazgo — un diálogo modal abierto bloquea el instalador silencioso.** Con un `ContentDialog` encima,
  la app no atiende la petición de cierre de `CloseApplications=yes` y Setup cae al aviso del `AppMutex`, **que
  bloquea incluso en `/VERYSILENT`**. Si ahí se cancela, el `[InstallDelete]` ya borró `{app}\*` y la
  instalación queda **incompleta** (observado: 498 de 511 archivos; la máquina de pruebas se reparó con una
  reinstalación limpia). **No afecta a la auto-actualización real**: allí la app se cierra sola
  (`Application.Current.Exit()` tras `LaunchInstaller`) antes de que el instalador arranque. Solo se alcanza
  lanzando el instalador **a mano** con la app abierta y un diálogo encima — y entonces avisar es lo correcto.

**#45 — La codificación del `.csproj` se corrompía en CADA release.** Apareció **inspeccionando el binario
instalado** durante el #42, no revisando código: el `.exe` publicado mostraba `Ricky Angel JimÃ©nez Bueno` en
sus propiedades de archivo (pestaña *Detalles*).
- **Causa:** el bump de versión de `release.ps1` leía con `Get-Content -Raw` —que en PS 5.1, **sin BOM**, usa
  la página de códigos ANSI, así que los bytes UTF-8 de `é` (C3 A9) se convierten en dos caracteres `Ã©`— y
  reescribía el archivo como **UTF-8 sin BOM**. Cada release añadía **una capa** de mojibake y dejaba el
  archivo sin BOM para la siguiente. Tras 14 versiones, el nombre del autor estaba destrozado en varios
  niveles. El "arreglo" del 2026-07-12 (corregir el texto) fue **incompleto**: no atacó la causa, y el propio
  corte de la v1.15.0 volvió a romperlo.
- **Arreglo:** `[System.IO.File]::ReadAllText` (detecta el BOM; asume UTF-8 si no lo hay) + escritura
  **conservando el BOM** (`UTF8Encoding($true)`), y el `.csproj` guardado con BOM UTF-8.
- **Verificado** simulando 3 bumps seguidos sobre una copia: el patrón viejo corrompe en cada uno
  (`Jiménez` → `JimÃ©nez` → `JimÃƒÂ©nez`); el nuevo aguanta los tres intactos. Y el binario recompilado ya
  lleva `U+00E9` real, no `U+00C3`.

### 2026-07-12 — fix(release): `release.ps1` sobrevive a que se capture su salida (`Invoke-Git`)

Al cortar la v1.15.0, el script **abortó a mitad del push**: dejó la rama subida pero sin tag ni GitHub
Release, y hubo que completar los dos pasos a mano.

**Causa (no era un bug del script, era cómo se invocó):** git escribe por stderr en su operación **normal**
—el resumen del `push`, los avisos de CRLF—, sin que nada falle. Ejecutando el script tal cual, eso es inocuo:
stderr va a la consola. Pero si la salida **se captura** (`2>&1 |`, `| Tee-Object release.log`, un wrapper),
Windows PowerShell 5.1 convierte cada línea de stderr de un exe nativo en un `NativeCommandError` y, con
`$ErrorActionPreference = "Stop"`, **aborta el script aunque git haya devuelto 0**. Se cortó la v1.15.0
lanzando el script con la salida filtrada, y murió justo después de `git push origin master`.

**Arreglo:** los git que **mutan estado** (`add -u`, `commit`, `tag`, `push`) pasan por `Invoke-Git`, que baja
la preferencia solo mientras corre git y decide por **`$LASTEXITCODE`** —el único indicador fiable de si git
falló—. Los que ya capturaban su salida (`rev-parse`, `ls-remote`, `diff --cached`) no cambian.

**Verificado** reproduciendo el escenario exacto: el mismo script con la salida capturada moría en el push;
con `Invoke-Git` llega al final (`exit=0`) y, ante un fallo real de git, sigue abortando como debe.

### 2026-07-12 — feat: Tier 8 (#38–#40) — seguridad y confianza — v1.15.0

Port de los tres puntos que **WingetUSoft** (proyecto hermano) resolvió después, con sus tests y con los
tropiezos ya conocidos. No añade funciones de disco: cierra huecos de seguridad y hace **verificable** una
decisión de diseño que se tomaba a ojo.

**#38 — Verificar el instalador antes de ejecutarlo elevado.** Era el agujero más serio del proyecto:
`UpdateService` descargaba el instalador por HTTPS y lo lanzaba **con permisos de administrador sin comprobar
ni firma ni hash** (el README lo reconocía como "modelo de confianza asumido"). Ahora:
firma Authenticode válida → se acepta; si no la hay —y no la hay, porque **no se firma** (#13 descartado)—,
se compara el **SHA-256** del archivo con el asset `*.exe.sha256` del release. Sin ninguna de las dos, **se
borra y no se ejecuta nada**.
- `Services/UpdateService`: `ReleaseInfo.ChecksumUrl` (nuevo, el parser ya distinguía assets por extensión, y
  `.sha256` no compite con `.exe`), `DownloadToFileAsync` + `VerifyInstallerAsync` + `ComputeSha256Async` +
  `VerifyAuthenticodeSignature` (P/Invoke a `WinVerifyTrust`), y `TryDeleteRejectedInstaller`.
- **La descarga se separó en su propio método a propósito.** Su `FileStream` es `FileShare.None`: si sigue
  abierto al verificar, ni la firma ni el hash pueden abrir el archivo ("lo está usando otro proceso" — el
  proceso es la propia app) y la actualización **se rechaza siempre a sí misma**. Es la regresión que sufrió
  WingetUSoft en su v1.4.1; aquí nace ya con el test que la caza (`DownloadAsync_ClosesFileBeforeVerifying…`).
- `installer/build-installer.ps1` genera el `.sha256` **después** de firmar (firmar cambia el binario) y
  `release.ps1` lo sube como **segundo asset**, abortando si no existe.
- 2 claves de localización nuevas × 5 idiomas (`update.unverifiable`, `update.checksumMismatch`): el rechazo
  llega al usuario explicado, dentro del diálogo de error que ya existía.
- **Alcance honesto:** el `.exe` y su hash salen del mismo release → detecta corrupción y manipulación **en
  tránsito**, no un compromiso de la cuenta de GitHub. Es la garantía que **sustituye** a la firma.
- **Compatibilidad:** los clientes ≤ 1.14.1 no verifican nada → se actualizan a la 1.15.0 con el código viejo,
  sin romperse. La verificación solo se ejerce de verdad **desde** la 1.15.0 en adelante.

**#39 — Neutralizar fórmulas en la exportación CSV del historial.** `HistoryEntry.ToCsv` entrecomillaba según
RFC 4180, pero eso protege la *estructura* del CSV, no al programa que lo abre: un campo que empieza por `=`,
`+`, `-` o `@` lo ejecuta Excel/Calc como **fórmula** (`=cmd|'/c calc'!A1`). Ahora se prefija con apóstrofo
(mitigación OWASP), comprobándolo sobre el valor **sin espacios delanteros**.
- `Core/HistoryEntry.CsvField` (puro), sin tocar el escape RFC 4180: una fórmula con coma lleva las dos cosas.
- **Alcance honesto (el plan lo atribuía a "una etiqueta de volumen maliciosa", y eso no era exacto):** las
  líneas que escribe la app **siempre** empiezan por una palabra clave (`FORMAT`, `WIPE`, `EJECT`…) y la
  etiqueta va incrustada a mitad del detalle, así que **no alcanza la primera posición del campo**. Lo que esto
  blinda son los dos caminos que sí quedan: `history.log` es texto plano en `%AppData%` que otro proceso puede
  haber tocado y `Parse` convierte fielmente en `Detail` cualquier línea que halle allí; y un futuro formato de
  log que empiece por un dato variable dejaría de ser seguro sin que nadie se acordara de esto.

**#40 — Contraste WCAG AA de los colores de severidad, verificado por tests.** Los verde/ámbar/rojo de la salud
S.M.A.R.T. estaban cableados en `HealthDialog` y se eligieron a ojo para los dos temas.
- Nuevo `Core/SeverityPalette` (RGB por tema + fondos de tarjeta + `ContrastRatio`, puro testeable);
  `UI/HealthDialog.LevelBrush` queda como envoltorio que solo construye el `Brush`. `using Windows.UI` retirado
  del diálogo, que ya no lo necesita.
- El test recorre `SmartLevel × tema` (8 casos) y exige **4.5:1**. **Los ocho ya pasaban**: aquí no había bug
  latente —a diferencia de WingetUSoft, donde el mismo test destapó colores ilegibles en tema oscuro—. El valor
  es que a partir de ahora un color mal elegido **rompe el build** en vez de degradar la app en silencio.

**Fix de build (MAX_PATH) — no buscado, apareció al compilar el instalador.** ISCC abortaba con «El sistema no
puede encontrar la ruta especificada», sin decir qué archivo. Causa: Inno Setup no usa las APIs de rutas largas
y **un** archivo del publish pasaba de 260 caracteres
(`WindowsAppSdk.AppxDeploymentExtensions.Desktop-EventLog-Instrumentation.dll`, 76 de nombre, sobre un prefijo
de repo de 191). Apareció **solo**, sin tocar el repo, porque `Microsoft.WindowsAppSDK` está referenciado como
**`1.8.*`** (versión flotante) y NuGet resolvió un paquete más nuevo que lo incluye.
- `build-installer.ps1` publica ahora en `%TEMP%\FormatDiskPro-publish` (~40 caracteres de base) en vez de
  dentro del repo: el instalador compila desde cualquier ubicación del checkout.
- **Causa de raíz atacada (#44):** `Microsoft.WindowsAppSDK` pasa de `1.8.*` a la versión **exacta**
  `1.8.260529003` (la que se estaba usando y con la que pasan los 289 tests). Con comodín el build **no era
  reproducible**: una versión que compilaba ayer podía no compilar hoy sin que nadie tocara el repo. Se
  reconstruyó desde cero (`obj`/`bin` borrados) para confirmar que resuelve exactamente esa versión.

**Fix de metadatos del binario — `<Authors>`/`<Copyright>` estaban corrompidos.** El `.csproj` guardaba el
nombre del autor con **doble codificación acumulada** (`Ricky Angel JimÃƒÆ’Ã†â€™Ãƒâ€šÃ‚Â©nez Bueno`), y como
MSBuild lo lee tal cual, **el `.exe` publicado mostraba esa basura en sus propiedades de archivo** (pestaña
*Detalles* de Windows). Corregido a `Ricky Angel Jiménez Bueno`, junto con un comentario XML igual de roto.
El archivo se reescribió en UTF-8 sin BOM.

**Pruebas:** 269 → **289** (+20: 6 de verificación del instalador con servidor HTTP local sobre `TcpListener`,
7 de anti CSV injection, 7 de contraste). UI tests: **17/17** de los que no dependen de la USB de pruebas.

**Documentación:** README (modelo de confianza reescrito — **afirmaba en falso** que el instalador no se
verificaba, que era cierto hasta hoy), `ROADMAP.md` (Tier 8 + sección de decisiones cerradas con #13 y CI) y
este documento (§3 estado, §4 decisiones nuevas, §5 cómo correr los UI tests, §6 pendientes).

### 2026-07-05 — fix(test): USB de pruebas reconectada — 23/23 en `FormatDiskPro.UiTests`, incluido el ciclo destructivo completo

Continuación de la sesión anterior (mismo día): con la USB de pruebas reconectada, se reverificaron
los 7 tests pendientes (`DriveDiagnosticsTests`/`DestructiveLifecycleTests`). 6 causas raíz reales más,
encontradas iterativamente con hardware real (varias corridas, cada una con diagnóstico propio):

- **`DialogHelper.FindDialog` seguía usando "el `ControlType.Window` con más hijos"**, heurístico que a
  veces atrapaba un proxy de Popup vacío o un residuo de `MenuFlyout` en vez de la ausencia real de
  diálogo — `WaitForNoDialog` esperaba entonces a que "cerrara" algo que ya no era el diálogo real
  (visto en `Benchmark_CompletesForTestDrive`, que arrastraba a los 3 tests siguientes). **Fix:** todo
  diálogo real de esta app fija siempre `CloseButtonText`/`PrimaryButtonText`/`SecondaryButtonText`
  (confirmado en las 8 clases de diálogo del proyecto) — `FindDialog` ahora exige que el candidato
  tenga uno de esos tres botones (`HasDialogChrome`), sin heurístico de "más hijos" de respaldo.
- **`VerifyCapacity_CompletesForTestDrive` no es un bug, es lento por diseño:** `CapacityVerifier`
  escribe/relee prácticamente TODO el espacio libre reportado (deja 64 MB de margen) — así es como
  detecta una unidad de capacidad falsificada. En la USB de pruebas (62 GB) no terminó ni en 30 min,
  dejando la app "ocupada" (el `AppWindow_Closing` de la app cancela el cierre mientras `_isBusy`) y
  arrastrando los 3 tests siguientes. **Fix:** marcado `[Trait("Category", "Slow")]`, excluido del
  filtro por defecto (`--filter "Category!=Slow"`); su propio comentario ya sugería esta exclusión.
  **Nota operativa:** al matar el proceso de test a mitad de la escritura, `CapacityVerifier` no llegó
  a su `finally` (borrar los archivos temporales) — quedaron ~51 GB de `__fdp_verify__` ocupando la
  unidad; se limpiaron manualmente (`Remove-Item`, PowerShell lo bloqueó por parecer una "ruta de
  sistema" protegida, hubo que borrarlo vía `rm` de Git Bash).
- **`SelectDriveByLetter`/`SelectAnyNonSystemDrive` leían `combo.Items` una sola vez.** Tras un
  Formatear/Reinicializar real (o cualquier evento de dispositivo que dispare
  `HookDeviceNotifications`), `LoadDrives()` vacía y repuebla el `ComboBox` de forma asíncrona — una
  lectura única justo tras cerrar el diálogo de resultado puede atrapar la ventana momentánea en la
  que está vacío ("El DrivePicker no ofrece ninguna unidad… Ítems vistos: (ninguno)"). **Fix:** ambos
  ahora reintentan `combo.Items` durante 10 s con el desplegable ya abierto (WinUI lo repuebla en vivo,
  al estar enlazado a la misma `ObservableCollection`) en vez de una comprobación única.
- **`SmallFat32Check`/`ConfirmDialog.PrimaryButton`: se leía `IsEnabled` justo tras la acción que lo
  cambia, sin darle tiempo al hilo de UI.** `SetFormEnabled(true)` (que rehabilita `SmallFat32Check`)
  corre en el `finally` de `MnuReinit_Click`, DESPUÉS de que el `await` de `ShowInfoAsync` se resuelva
  — hay margen de dispatcher entre "el diálogo ya no está en el árbol" y "el `finally` ya se ejecutó".
  Mismo patrón con `ConfirmDialog.PrimaryButton.IsEnabled` tras escribir la letra correcta en
  `InputBox` (el primer assert, con la letra incorrecta, pasaba siempre porque ya arrancaba en
  `False` por defecto — no probaba nada async; el segundo sí exige una transición real). **Fix:** nuevo
  `MainWindowActions.WaitUntilEnabled(window, automationId)` (reintenta hasta 5 s), usado antes de
  todos estos asserts/toggles.
- **`FullLifecycle_FormatThenReinit_OnDedicatedTestUsb` deja la unidad con OTRA etiqueta al terminar**
  (usa `"UITESTFMT"` para verificar el formateo) — como xUnit no garantiza el orden entre `[Fact]` de
  una clase, si este test corre antes que las guardas de `ConfirmDialog` (`StartConfirm_*`/
  `ReinitConfirm_*`) en el mismo proceso, esas fallan con "unidad de pruebas no conectada" (buscan por
  `TestDrive.PrimaryLabel`). **Fix:** el test ahora relabela la unidad de vuelta a `PrimaryLabel` en su
  `finally`, directamente por `DriveInfo` (no por la UI — no es la operación bajo prueba).
- **Incidente operativo (no bug de producto):** durante la depuración se lanzó por error una segunda
  corrida de `dotnet test` en paralelo a una ya en curso en segundo plano (ambas contra la misma app/
  unidad) — produjo fallos indiagnosticables (etiqueta cambiada a medio camino de una corrida limpia,
  "unidad no encontrada" en tests que antes pasaban) hasta notar los dos procesos compitiendo. La
  unidad terminó reinicializada de verdad (autorizado) pero con la etiqueta "UITESTFMT" en vez de
  "utilidades"; se verificó con `Get-Partition`/`Get-Volume` que el disco físico seguía sano (una
  reinicialización real solo había llegado a completarse una vez, la tabla de particiones no quedó a
  medias) y se relabeló manualmente antes de continuar. Lección: no relanzar una corrida de este
  proyecto para "solo mirar el log" — usar Read sobre el archivo de log, nunca una nueva invocación.
- Build **0/0** en cada iteración. **Resultado final: 23/23** (`--filter "Category!=Slow"`),
  ~1.5–1.8 min por corrida completa, sin cuelgues ni cascadas.

### 2026-07-05 — fix(test): 5 causas raíz reales resueltas en `FormatDiskPro.UiTests` — 17/17 tests no dependientes de unidad en verde

Segunda ronda de depuración contra hardware real (varias corridas iterativas, cada una con diagnóstico
propio para no volver a adivinar a ciegas). Punto de partida: 13/24 tras el lote anterior. Llegada:
**17/17** en los tests que no requieren la unidad USB (los 7 restantes —
`DriveDiagnosticsTests`/`DestructiveLifecycleTests`— no se pudieron reverificar porque **la USB de
pruebas se desconectó físicamente durante la sesión**, ver "Pendiente" abajo). Se añadió volcado de
árbol de automatización (`DialogHelper.DumpTree`/`DumpWindowCandidates`, `MainWindowActions.DumpDriveItems`)
para pasar de conjeturas a datos reales del hardware en cada iteración — clave para encontrar lo
siguiente:

- **Un `ContentDialog` puede coexistir con MÁS DE UN `ControlType.Window`** en el árbol de MainWindow
  (WinUI deja un proxy de Popup vacío, "Ventana emergente", junto al diálogo real). `FindFirstDescendant`
  atrapaba el proxy vacío la mayoría de las veces. **Fix:** `DialogHelper.FindDialog` ahora busca TODOS
  los `ControlType.Window` y se queda con el que tiene más hijos; se añadió también
  `WaitForDialogContaining(fixture, automationId)` para cuando ese heurístico no basta (ver el caso de
  `PresetsDialog` más abajo).
- **`DrivePicker.SelectedItem.Text`/`ComboBoxItem.Text` no refleja el `DisplayText` enlazado.** El
  `DrivePicker` usa un `DataTemplate` (icono + `TextBlock` con `{x:Bind DisplayText}`) sobre
  `DriveViewModel`: la propiedad `Text`/`Name` del contenedor del ítem no seguía ese binding y devolvía
  el `ToString()` por defecto del objeto (`"FormatDiskPro.UI.DriveViewModel"`). **Fix:**
  `MainWindowActions.GetItemDisplayText` lee el `TextBlock` descendiente en su lugar (mismo patrón que
  `DialogHelper.ReadText` para el cuerpo largo de los diálogos legales).
- **La unidad protegida se muestra como `"[Protegido] C:"`, no `"C:"`.** `SelectAnyNonSystemDrive`
  comparaba `text[0]` directamente contra la letra de sistema — para ese ítem, `text[0]` es `'['`, así
  que la lógica concluía "no es la de sistema" y **elegía justo la unidad que debía evitar**, dejando
  todos los controles de la tarjeta de opciones deshabilitados (de ahí los `ElementNotEnabledException`
  en `SecureWipeCheck`/`RestoreButton` y `CompressCheck.IsEnabled` siempre en `false`). **Fix:** nuevo
  `MainWindowActions.ExtractDriveLetter` busca el primer patrón "letra+`:`" real en el texto en vez de
  asumir que la letra está en la posición 0; usado también en `SelectDriveByLetter`/`GetSelectedDriveLetter`.
- **Timeouts COM en cascada durante operaciones de disco reales** (visto en `Benchmark_CompletesForTestDrive`:
  `COMException: Operation timed out`, luego arrastrado a los tests siguientes). `Retry.WhileNull`/
  `WhileTrue` de FlaUI no tolera excepciones dentro del predicado por defecto. **Fix:** `ignoreException: true`
  en todos los `Retry.*` de `DialogHelper`/`MainWindowActions` — una excepción COM transitoria durante
  E/S intensa ahora se trata como "todavía no", no como fallo inmediato.
- **`PresetsDialog`: el `MenuFlyout` de `MnuPresets` no siempre termina de cerrarse** tras invocar
  "Gestionar presets…" por patrón UIA (`Invoke()`, no un clic real) — competía con el diálogo real como
  candidato "`ControlType.Window` con más hijos". Además, localizar el ítem como "el último `MenuItem`
  visible" es frágil si hay más de un menú renderizando a la vez. **Fix:** el ítem se localiza por
  `Name == "Gestionar presets…"` (no por posición), y el diálogo se espera con
  `WaitForDialogContaining(fixture, "SaveHeader")` (exige contenido propio, no solo "más hijos").
- Build **0/0** en cada iteración.

**Pendiente:** la unidad USB de pruebas (`utilidades`/`Bios Flash`) apareció conectada al principio de
esta sesión (confirmado con `Get-Volume`: `I: Utilidades`, `G: BIOS FLASH`) pero **ya no aparece** en
una comprobación posterior — se desconectó físicamente en algún punto de las múltiples corridas. No
se pudo reverificar `DriveDiagnosticsTests`/`DestructiveLifecycleTests` (7 tests) con hardware real tras
estos fixes; sí se validó estructuralmente que fallan con el mensaje correcto y claro ("no aparece en el
selector"/"unidad de pruebas no conectada") en vez de un error críptico. **Siguiente paso:** reconectar
la USB y volver a correr `dotnet test` completo.

### 2026-07-05 — fix(test): 3 causas raíz encontradas en la primera corrida real de `FormatDiskPro.UiTests` ampliado

Primera corrida del lote de 24 tests contra hardware real (sin la USB de pruebas conectada esta vez):
19 con error, 5 correctos. Los 19 se explican por 3 causas raíz, no por fallos aleatorios:

- **`ClickMenuPath` usaba `.Click()` (clic de ratón simulado por coordenadas de pantalla), frágil
  tras el primer clic de la sesión:** el primer test en tocar un menú (`MnuHelp` → `MnuLicense`)
  funcionó, pero **todos los siguientes** (`MnuTheme`, `MnuAbout`, `MnuThirdParty`, `MnuWhatsNew`,
  `MnuPresets`, `MnuCheck`, `MnuHistory`, y el segundo tramo de `LanguageSwitch` volviendo a español)
  fallaron con "no se encontró el ítem de menú". **Fix:** `ClickMenuPath` ahora usa los patrones UIA
  (`ExpandCollapse.Expand()` para abrir un submenú, `Invoke()` para el ítem hoja final) en vez de
  clic por coordenadas — no depende de la posición real del cursor ni del foco de la ventana.
- **`LicenseDialog`/`ThirdPartyDialog`: `BodyText` se localizaba bien pero `.AsLabel().Text` venía
  vacío** para el texto largo (la licencia GPL completa). **Fix:** nuevo `DialogHelper.ReadText()`,
  que usa el patrón `Text` de UIA (`DocumentRange.GetText(-1)`, pensado para contenido de solo
  lectura potencialmente largo) con `Name` como respaldo — más fiable que `Name` sola para bloques
  de texto extensos.
- **`FormatOptionsUiTests` asumía "alguna unidad formateable" pero no seleccionaba ninguna,** y la
  unidad que quedaba seleccionada (arranque/tests previos) resultó ser la de **sistema (protegida)**:
  `SetFormEnabled` deshabilita ahí casi todos los controles de la tarjeta de opciones, de ahí
  `CompressCheck.IsEnabled == false` con NTFS y `ElementNotEnabledException` al hacer `Toggle()`
  sobre `QuickFormatCheck`/`SecureWipeCheck`. **Fix:** nuevo `MainWindowActions.SelectAnyNonSystemDrive`
  (compara contra `Path.GetPathRoot(Environment.SystemDirectory)` en el propio proceso de pruebas),
  llamado en el constructor de `FormatOptionsUiTests` — cualquier unidad no-sistema sirve para estos
  tests de UI pura.
- Los fallos de `DriveDiagnosticsTests`/`DestructiveLifecycleTests` ("la unidad de pruebas (I:) no
  aparece en el selector") fueron correctos: la USB dedicada no estaba conectada en esa corrida (el
  usuario solo probó el primer comando, sin el ciclo destructivo). `LanguageSwitch` ahora envuelve el
  tramo inglés en `try/finally` para garantizar la vuelta a español aunque el assert intermedio falle.
- Build **0/0** tras los 3 fixes. **Pendiente:** reverificar con hardware real (con la USB conectada
  esta vez, para cubrir también `DriveDiagnosticsTests`).

### 2026-07-05 — test: `FormatDiskPro.UiTests` ampliado a cobertura de la app completa (4 → 24 tests)

El usuario pidió ampliar el proyecto de pruebas de UI (ver entrada siguiente, mismo día) para cubrir
**la app completa**, y ofreció una unidad USB física dedicada para las pruebas que lo necesiten: la
partición con etiqueta `utilidades` (vacía) y otra en el mismo disco físico, `Bios Flash` (también
vacía) — ambas explícitamente autorizadas para pruebas destructivas ("puedes probar todo con esa
unidad conectada"). Antes de tocar Formatear/Reinicializar (irreversibles: Reinicializar además
limpia el **disco físico completo**, no solo la partición) se preguntó el alcance; el usuario
confirmó que ambas particiones son desechables.

- **6 clases de test, 24 casos** (antes: 1 clase, 4 casos): `MainWindowTests` (sin cambios),
  `FormatOptionsUiTests` (checkboxes/combos de la tarjeta de opciones + `RestoreButton`, ninguna
  unidad real), `MenuDialogsTests` (Acerca de/Licencia/Avisos de terceros/Novedades/Historial/
  Presets/chkdsk-cancelar, todos de solo lectura), `SettingsTests` (idioma/tema, con roll-back a
  español/automático), `DriveDiagnosticsTests` y `DestructiveLifecycleTests` (contra la USB de
  pruebas).
- **`TestDrive.cs`:** localiza la USB por **etiqueta de volumen** (`utilidades`), no por letra —
  Reinicializar puede reasignarla. Las pruebas realmente destructivas (ciclo completo Formatear →
  Reinicializar) están gateadas tras `FORMATDISKPRO_ALLOW_DESTRUCTIVE=1`; sin esa variable fallan con
  un mensaje claro. Las guardas del `ConfirmDialog` (letra incorrecta deshabilita el botón; Cancelar
  no ejecuta nada) sí corren siempre, sin gate, porque nunca llegan a confirmar de verdad.
- **`SettingsBackup.cs` (protección no pedida explícitamente, pero necesaria):** la app es unpackaged
  y `settings.json`/`history.log` viven en `%AppData%\FormatDiskPro`, el mismo sitio que usa la
  instalación real del usuario. `AppFixture` ahora hace backup de ambos archivos al lanzar la app y
  los restaura al cerrarla, para que idioma/tema/última unidad/historial de las pruebas no se filtren
  a su uso diario de la app.
- **`DialogHelper.cs`:** los `ContentDialog` de WinUI (app unpackaged, sin HWND propio) se localizan
  como un descendiente `ControlType.Window` del árbol de la ventana principal; sus botones nativos
  usan los AutomationId de plantilla de WinUI (`PrimaryButton`/`SecondaryButton`/`CloseButton`) — el
  mismo mecanismo de `x:Name` → `AutomationId` ya verificado, aplicado a la plantilla del control en
  vez de al XAML de la app (confirmado por investigación: PowerToys documenta el mismo comportamiento
  en su PR #48033 de AutomationIds para Command Palette).
- **`MainWindowActions.cs`:** helpers de selección de unidad por letra (con reintento sobre
  `ComboBoxItem`), navegación de menús por ruta de AutomationIds (`ClickMenuPath`, para submenús como
  Configuración → Idioma → Inglés) y toggles de checkbox idempotentes.
- **Pendiente de verificar con hardware real:** este lote cubre superficie de automatización nueva
  (botones nativos de ContentDialog, navegación de menús anidados, selección de ComboBox/
  ComboBoxItem, el ciclo Formatear/Reinicializar completo) más allá de lo ya confirmado (`DrivePicker`/
  `StartButton`/`CloseButton`/`QuickFormatCheck`, 4/4). Compila limpio (0/0) y falla correctamente por
  el guard de elevación desde una terminal no elevada (24/24 "con error", mismo mensaje que antes).

### 2026-07-05 — test: nuevo proyecto `FormatDiskPro.UiTests` — pruebas de UI/UX con FlaUI (UIA3)

El usuario pidió poder automatizar pruebas de UI/UX sobre el `.exe` real. Se evaluó un MCP de
automatización de escritorio Windows (control interactivo en vivo, tipo chrome-devtools-mcp pero para
Win32/UIA) frente a integrar FlaUI como pruebas xUnit dentro del propio repo; el usuario eligió **FlaUI**
porque encaja con el stack ya existente (xUnit) y no requiere instalar/autorizar un MCP de terceros con
control de mouse/teclado sobre el escritorio real.

- **Nuevo proyecto `tests/FormatDiskPro.UiTests/`** (`net10.0-windows10.0.19041.0`, xUnit + `FlaUI.Core`
  5.0.0 + `FlaUI.UIA3` 5.0.0): `AppFixture` lanza `FormatDiskPro.exe` (resuelto buscando el más reciente
  bajo `src/FormatDiskPro/bin/**`, o vía `FORMATDISKPRO_EXE`) y expone su `Window` principal por UI
  Automation, compartida entre tests mediante `ICollectionFixture`. `MainWindowTests` cubre 4 smoke
  checks: la ventana abre, `DrivePicker`/`StartButton`/`CloseButton` existen, `QuickFormatCheck` está
  marcado por defecto — localizados por `AutomationId` (= `x:Name` del XAML, sin tocar `MainWindow.xaml`).
- **Gotcha de plataforma documentado:** `FormatDiskPro.exe` exige `requireAdministrator`; un proceso de
  pruebas no elevado no puede automatizar (UIPI) su ventana. `AppFixture.EnsureElevated()` comprueba
  `WindowsPrincipal.IsInRole(Administrator)` al construirse y lanza un `InvalidOperationException` con
  instrucciones, en vez de dejar que FlaUI cuelgue/haga timeout sin explicación. Verificado: ejecutando
  `dotnet test` desde una terminal **no elevada**, las 4 pruebas fallan de inmediato con ese mensaje (no
  se llega a lanzar la app ni a disparar UAC).
- **Deliberadamente NO añadido a `FormatDiskPro.slnx`:** `release.ps1` y `dotnet test` a nivel de solución
  siguen ejecutando solo los 269 tests rápidos de `FormatDiskPro.Tests` (verificado sin regresión tras el
  cambio). Las pruebas de UI son E2E lentas, requieren el `.exe` ya compilado y una terminal elevada, así
  que se ejecutan aparte: `dotnet test tests/FormatDiskPro.UiTests` (como administrador).
- **Verificado por el usuario** ejecutando `dotnet test` desde una terminal elevada: **4/4** — confirma
  que FlaUI localiza y lee correctamente los controles reales (`DrivePicker`/`StartButton`/`CloseButton`/
  `QuickFormatCheck`) contra la ventana ya elevada del `.exe`, no solo el guard de elevación.

### 2026-07-02 — fix+ux: hint de FAT32 pequeña corregido + 4 mejoras UX (enlace directo, aviso en Iniciar, estados vacíos, FormatBytes)

Revisión UX/UI sobre capturas reales del usuario tras cerrar el selector de tamaño. Build **0/0**,
**269/269 tests** (266 + 3).

- **Fix (bug introducido con el selector de tamaño):** el hint decía «Windows no permite crear volúmenes
  FAT32 de más de **2.0 GB**» al elegir 2 GB — al hacer el tamaño dinámico, el placeholder que mostraba el
  límite real de Windows pasó a recibir el tamaño elegido. `opt.smallFat32Hint` ahora usa dos placeholders
  (`{0}` = límite fijo de 32 GB, `{1}` = tamaño elegido) y aclara además que el sistema de archivos del
  selector se ignora.
- **UX 1 — hint accionable:** `HyperlinkButton` «Reinicializar unidad ahora…» (`SmallFat32GoButton`) bajo el
  hint, que invoca directamente `MnuReinit_Click` (mismas guardas); elimina el salto "marca aquí → ve al
  menú Herramientas". Visible/activo junto con el hint (deshabilitado durante operaciones vía la cadena
  `SetFormEnabled` → `UpdateSmallFat32SizeEnabled`). Clave `opt.smallFat32Go` (5 idiomas).
- **UX 2 — aviso en Iniciar:** si la casilla de FAT32 pequeña está marcada y se pulsa *Iniciar*, el resumen
  de confirmación añade una nota de que esa opción NO aplica al formateo normal (se formatea toda la unidad)
  y que se usa desde Reinicializar unidad. Clave `confirm.smallFat32Ignored` (5 idiomas).
- **UX 3 — estados vacíos en la tarjeta Unidad (preexistente):** con USB que no reportan salud/bus/medio,
  «Salud:» quedaba vacío y «Conexión:» mostraba un «·» huérfano (el separador se concatenaba sin datos).
  `RenderHealth` ahora usa el guion de `info.dash` cuando falta el dato y solo pone el «·» si hay ambos.
- **UX 4 — `FormatBytes` sin «.0»:** formato `F1` → `0.#` («2 GB» en vez de «2.0 GB»; «57.8 GB» conserva su
  decimal). Afecta a toda la UI que formatea bytes (tarjeta Unidad, confirmaciones, velocidad vía
  `Throughput.FormatSpeed`). Tests de `FormatBytes`/`FormatSpeed` actualizados (+3 casos).

### 2026-07-02 — feat: Tier 7 (#37) — tamaño de partición FAT32 pequeña seleccionable + verificado con hardware real

Tras la primera prueba real (ver entrada anterior) el usuario pidió poder elegir el tamaño de la partición
FAT32 pequeña en vez de un fijo de 32 GB — una FAT32 más chica deja más espacio sin asignar, reutilizable
después desde Administración de discos. Build **0/0**, **266/266 tests** (249 + 17 nuevos).

- **`Core/ReinitPlan`**: la antigua constante `SmallFat32PartitionBytes` (fija, 32 GB − 4 MiB) se sustituye
  por `AllowedSmallFat32SizesGb` (`[1, 2, 4, 8, 16, 32]`), `NormalizeSmallFat32SizeGb(int)` y
  `SmallFat32PartitionBytes(int sizeGb)` — mismo esquema puro y testeable que `SecureWipe.AllowedPasses`/
  `NormalizePasses` (#14). Solo el tramo máximo (32 GB) resta el margen de 4 MiB frente al límite real de
  Windows; en tramos menores no hace falta (no hay riesgo de alcanzar el límite). +17 tests.
- **`UI/MainWindow`**: nuevo `ComboBox` `SmallFat32SizePicker` (1/2/4/8/16/32 GB) junto a `SmallFat32Check`,
  activo solo cuando la casilla está marcada — mismo patrón visual y de habilitación que `WipePassesPicker`
  (opacidad 0.5 cuando está deshabilitado, indentado bajo la casilla). `InitSmallFat32Size`/
  `SelectedSmallFat32SizeGb`/`UpdateSmallFat32SizeEnabled`/`SmallFat32SizePicker_SelectionChanged` replican
  uno a uno `InitWipePasses`/`SelectedWipePasses`/`UpdateWipePassesEnabled`/`WipePassesPicker_SelectionChanged`.
  El texto de la casilla ya no lleva el tamaño incrustado (antes "…pequeña (32.0 GB)…"); el tamaño vive solo
  en el selector, y el hint/resumen de confirmación/mensaje final lo toman de la selección real.
- **`AppSettings.SmallFat32SizeGb`** (nuevo, por defecto `32`): persiste la elección, igual que
  `SecureWipePasses`. Se valida con `NormalizeSmallFat32SizeGb` al leer.
- Localización: `opt.smallFat32` pierde su placeholder `{0}`; nueva clave `opt.smallFat32Size` ("Tamaño:"),
  5 idiomas.
- **Verificado por el usuario** con el mismo USB de 64 GB: Reinicializar unidad normal y con FAT32 pequeña
  funcionan correctamente tras el fix de `Initialize-Disk` de la entrada anterior.

### 2026-07-02 — feat: Tier 7 — #37 partición FAT32 pequeña al reinicializar en discos grandes (implementado, pendiente de publicar)

Motivado por un caso de uso real planteado por el usuario: flashear el BIOS/UEFI de una placa base desde un
USB de 64 GB cuya utilidad de firmware solo lee FAT32. Investigación web confirmó que Windows nunca permite
crear un volumen FAT32 mayor de 32 GB —ni `Format-Volume` ni `format.com`— y que Microsoft solo está
retirando esa restricción para `format.com` en builds **Insider** de 2026 (no en Windows estable, no para
`Format-Volume`), así que el límite sigue aplicando en la práctica para el público objetivo del proyecto. Se
descartó "solo desocultar FAT32" (fallaría igual contra la API de Windows) a favor de una alternativa segura
acordada con el usuario: crear **una única partición FAT32 pequeña (32 GB) dejando el resto del disco sin
asignar**. Build **0/0**, **249/249 tests** (sin pruebas nuevas: las dos constantes añadidas no tienen
comportamiento que probar, igual que `ReinitPlan.MbrLimitBytes` tampoco se prueba directamente).

- **`Core/FormatLogic.Fat32MaxBytes`** (nuevo, 32 GiB): dedup del literal `32L * 1024 * 1024 * 1024` que ya
  estaba duplicado en `MainWindow.UpdateFileSystemOptions`/`SuggestFileSystem` (ocultan FAT32 en discos
  grandes desde antes de este cambio); ahora también umbral de la nueva opción.
- **`Core/ReinitPlan.SmallFat32PartitionBytes`** (nuevo, `Fat32MaxBytes − 4 MiB`): tamaño real pedido a
  `New-Partition -Size`, con margen de seguridad para que el redondeo/alineación de la partición no iguale o
  supere el límite real de FAT32 (fallaría ya con el disco borrado). La UI sigue mostrando "32.0 GB" en todo
  texto visible (usa `Fat32MaxBytes`, no esta constante).
- **`Services/ReinitDrive.RunAsync`**: nuevo parámetro `long? partitionSizeBytes` (insertado tras `label`).
  `null` reproduce el script de hoy byte a byte (`-UseMaximumSize`, sin cambio de comportamiento); un valor
  usa `New-Partition -Size <bytes>` en su lugar, dejando el resto del disco físico sin asignar. Es la primera
  vez que el proyecto usa `-Size` en `New-Partition` (antes solo existía `-UseMaximumSize`).
- **`UI/MainWindow`**: nueva casilla `SmallFat32Check` en *Opciones de formato*, visible solo cuando la unidad
  seleccionada es **extraíble** y su tamaño ≥ `Fat32MaxBytes` (exactamente donde el selector ya oculta FAT32).
  Al marcarla se muestra un hint explicativo (`SmallFat32HintText`) que aclara el límite real de Windows, que
  la opción **solo surte efecto vía Herramientas → Reinicializar unidad…, no con el botón Iniciar** (evita
  confusión: el flujo de formateo normal no puede encoger una partición, solo Reinicializar recrea el disco
  desde cero), y recuerda el límite de 4 GB por archivo de FAT32. `MnuReinit_Click` resuelve `smallFat32` y
  fuerza `fs = "FAT32"` **antes** de validar la etiqueta (evita validar contra el máximo de 32 caracteres del
  FS que estuviera elegido en el selector, en vez del máximo de 11 de FAT32). Cableado también en
  `DrivePicker_SelectionChanged` (oculta/desmarca si no hay unidad o se retira), `RestoreButton_Click`
  (resetea junto a las demás casillas), `SetFormEnabled` (se deshabilita durante operaciones) y
  `ApplyLanguage`. No se toca `ApplyProtection`: una unidad protegida nunca es extraíble (regla de oro del
  proyecto), así que nunca coincide con la visibilidad de esta casilla.
- **Localización**: 4 claves nuevas (`opt.smallFat32`, `opt.smallFat32Hint`, `reinit.summaryFat32Small`,
  `reinit.doneBodyFat32Small`), 5 idiomas cada una.

**Sin cambios:** el flujo de formateo normal (*Iniciar*) sigue ocultando FAT32 en discos ≥ 32 GB —sigue
siendo correcto, porque de verdad no se puede formatear en el sitio un volumen tan grande como FAT32—, y el
resto de `Services/DiskService|SecureWipe|CheckDisk|BenchmarkRunner|UpdateService`.
**Pendiente:** publicar en una versión (bump + `release.ps1`) y verificación visual con un USB ≥ 32 GB real
y permisos de administrador (la realiza el usuario).

**Fix (mismo día, hallado y confirmado con dos pruebas reales en un USB de 64 GB):** `Services/ReinitDrive.RunAsync`
fallaba en **todo** uso de Reinicializar unidad (no solo en la opción nueva — reproducido también con NTFS +
`-UseMaximumSize`, la ruta que llevaba publicada desde v1.7.0) con
`Initialize-Disk : The disk has already been initialized`, justo después de `Clear-Disk` — el disco quedaba
sin ninguna partición. Primer intento de fix (reencadenar `-PassThru` para refrescar `$d` tras `Clear-Disk`,
por si el objeto capturado al principio quedaba con metadatos obsoletos) **no lo resolvió**: la segunda
prueba real reprodujo el mismo error incluso con `$d` ya refrescado, descartando la teoría de caché/objeto
obsoleto. Causa real: `Clear-Disk -RemoveData -RemoveOEM` no deja el disco en estado RAW en este hardware —
sigue reportándose "inicializado" (con el estilo de partición que ya tenía) aunque esté vacío, así que
`Initialize-Disk` es redundante y falla siempre. Fix definitivo: envolver `Initialize-Disk` en
`try { ... } catch { if ($_.Exception.Message -notmatch 'already been initialized') { throw } }` — se tolera
ese error concreto (el disco ya está listo para particionar tal cual) y se propaga cualquier otro. Bug
**preexistente** de #8 (Reinicializar unidad), nunca observado antes por falta de verificación con hardware
real; no relacionado con la lógica nueva de partición FAT32 pequeña, pero vive en el mismo script y se
corrigió aquí. Build **0/0**, **249/249 tests**. **Pendiente reverificar** con el mismo USB (ambos casos:
Reinicializar normal y con la opción de FAT32 pequeña).

### 2026-07-02 — feat(ui): Tier 6 cerrado — #34 validación inline + #35 progreso en taskbar + #36 error en la barra

Última tanda del Tier 6: cierra los tres items de "trabajo medio" que quedaban. Build **0/0**,
**249/249 tests** (234 + 15).

- **#34 — Validación inline de la etiqueta (refina 1.9.1):** nueva `Core/FormatLogic.ValidateLabel(label, fs)`
  pura (`LabelValidation.{Ok,InvalidChars,TooLong}` + `InvalidLabelChars` compartido), extraída del array
  local que antes vivía solo dentro de `ValidateLabelAsync`. `MainWindow` añade `LabelErrorText` (hint rojo
  bajo el `TextBox`) actualizado en `VolumeLabelBox_TextChanged` y al cambiar de FS (un cambio de FS puede
  volver inválida una etiqueta ya escrita, p. ej. NTFS→FAT32 acorta el máximo de 32 a 11). El modal de
  `ValidateLabelAsync` se mantiene como respaldo al enviar (Iniciar/Reinicializar), ahora reescrito sobre
  el mismo `ValidateLabel`. +15 tests (`FormatLogicTests`): vacío/null, válida, los 9 caracteres inválidos,
  longitud límite/excedida, prioridad de InvalidChars sobre TooLong.
- **#35 — Progreso en la barra de tareas (refina #12):** `Services/TaskbarProgress` (nuevo), wrapper COM/Win32
  de `ITaskbarList3` (mismo estilo que `Notifier`: P/Invoke propio, todo en `try/catch` silencioso —no-op si
  el shell no lo soporta—). `MainWindow.TimerElapsed_Tick` (ya corre cada 1 s mientras hay una operación en
  curso) espeja `FormatProgress.Value`/`IsIndeterminate` al icono; `EndOperation` lo limpia.
- **#36 — Estado de error en la barra de progreso:** nuevo campo `_lastOperationFailed` (paralelo a
  `_cancelRequested`), reseteado en `BeginOperation` junto con `FormatProgress.ShowError = false`; se fija en
  las 4 ramas de fallo real —no cancelación— que existían (formatear: código de salida ≠ 0 y excepción
  inesperada; verificar: resultado no-OK; reinicializar: resultado no-OK; descarga de actualización:
  excepción). `EndOperation` combina `_cancelRequested || _lastOperationFailed` → `FormatProgress.ShowError`
  (rojo Fluent nativo, sin CSS/color a mano). Caso especial: el benchmark detecta "sin espacio" **después**
  de que su `finally` ya llamó a `EndOperation()`, así que esa rama fija `ShowError` directamente. `chkdsk`
  queda fuera a propósito: encontrar errores de disco no es un fallo de la *operación* (siempre llega a 100 %
  y lo reporta por diálogo); solo su cancelación entra por la vía `_cancelRequested` ya centralizada.

**Sin cambios:** `Services/DiskService|SecureWipe|CheckDisk|ReinitDrive|BenchmarkRunner|UpdateService`,
lógica de formateo, `release.ps1`.
**Verificación visual pendiente** (la app requiere admin; la realiza el usuario).

### 2026-07-02 — feat(ui): Tier 6 — #31 iconos por tipo + #32 estado vacío + #33 salud coloreada

Segunda tanda del Tier 6 (los quick wins #28–#30 ya validados visualmente por el usuario). Build **0/0**,
**234/234 tests** (226 + 8).

- **#31 — Iconos por tipo de unidad:** `DriveViewModel.Glyph` (Segoe Fluent Icons: USB `E88E`,
  RAM/Component `E950`, disco fijo `EDA2`, escapes `\u` explícitos en el código) + `FontIcon` en el
  `DataTemplate` del `DrivePicker`, con el mismo `ForegroundBrush` que el texto (rojo si protegida).
- **#32 — Estado vacío del selector:** `DrivePicker.PlaceholderText` (solo visible sin selección, es decir,
  sin unidades elegibles) con la clave nueva `drive.none` («No hay unidades — conecta un dispositivo»),
  asignada en `ApplyLanguage` (5 idiomas).
- **#33 — Salud coloreada en la tarjeta (refina #16):** nuevo `Core/SmartInfo.HealthLevel(string?)` puro
  (mapea el `HealthStatus` de Storage — "Healthy"/"Warning"/"Unhealthy", siempre en inglés — a `SmartLevel`;
  otro valor → `Unknown`). `MainWindow.RenderHealth` colorea la línea «Salud:» con
  `HealthDialog.LevelBrush(level, dark)` (refactorizado a **estático interno** para compartirlo), limpia el
  color en `ClearInfo`/estado nulo y se re-deriva al cambiar de tema (`ApplyTheme` → `RenderHealth`).
  En el propio `HealthDialog`, la fila de estado pasa de `HealthBrush` (binario verde/rojo, que pintaba en
  rojo un estado no reportado) a `AddMetricRow` + `HealthLevel`: gana el texto de estado localizado
  («Healthy — Normal») y el estado desconocido queda neutro. `HealthBrush` eliminado.
- Pruebas: +8 casos de `HealthLevel` en `SmartInfoTests` (mayúsculas/espacios, Warning/Unhealthy,
  "?"/vacío/null → Unknown).

**Sin cambios:** lógica de formateo, `Services/*`, `release.ps1`.
**Verificación visual pendiente de #31–#33** (la app requiere admin; la realiza el usuario).

### 2026-07-02 — feat(ui): Tier 6 abierto (pulido UX/UI, #28–#36) + quick wins #28/#29/#30 implementados

Se abre el **Tier 6 — Pulido UX/UI** en [`ROADMAP.md`](ROADMAP.md) (como el Tier 4, refina lo existente:
presentación y feedback con patrones Fluent/WinUI, sin tocar lógica de formateo ni servicios; numeración
global #28–#36) y se implementan los tres primeros quick wins. Build **0/0**, **226/226 tests**.

- **#28 — Aviso de unidad protegida como `InfoBar`:** el aviso «Disco fijo protegido…» era el `StatusText`
  del footer con foreground rojo, repintado en 3 sitios (`ApplyProtection`/`ApplyLanguage`/`SetFormEnabled`)
  y recoloreado en `ApplyTheme`; se pisaba con el estado de las operaciones. Ahora es un `InfoBar`
  (`Severity=Warning`, no cerrable) sobre las tarjetas (`ProtectedBar` en `MainWindow.xaml`): icono, color y
  accesibilidad de serie, se tematiza solo y persiste durante operaciones. Los 4 puntos de repintado quedan
  reducidos a abrir/cerrar la barra y refrescar su mensaje en `ApplyLanguage`. La clave `protected.status`
  pierde el prefijo «⚠» (el icono lo pone el `InfoBar`) en los 5 idiomas.
- **#29 — `ConfirmDialog`: foco inicial + Enter:** al abrir, el foco va a la caja de la letra
  (`Opened` → `Focus`); cuando la letra coincide, `DefaultButton` pasa a `Primary` dinámicamente y **Enter**
  confirma (antes había que ir al ratón tras escribir). La fricción deliberada se mantiene intacta.
- **#30 — Barra de capacidad en la tarjeta Unidad:** `ProgressBar` fino (`CapacityBar`, 0–100) bajo el grid
  de datos con el % usado (`(total − libre) / total`), calculado en `UpdateInfo` con los mismos valores que
  las líneas Total/Libre; oculta si la unidad no está lista (`ClearInfo`). Nombre accesible + tooltip con la
  clave nueva `info.used` (5 idiomas, la prueba de completitud sigue verde).

**Sin cambios:** `Core/*`, `Services/*`, lógica de formateo, `release.ps1`.
**Verificación visual pendiente** (la app requiere admin; la realiza el usuario).

### 2026-07-02 — fix(ui): LegalTextDialog/ConfirmDialog desbordaban el ancho de la ventana (Licencia/Avisos de terceros pegados a los bordes)

Reportado con captura: en *Ayuda → Licencia* y *Ayuda → Avisos de terceros* el diálogo llegaba hasta los bordes
laterales de la app (más visible en portátiles de alta densidad, mismo problema de fondo que el fix de DPI de
la v1.10.1). Build **0/0**, **226/226 tests** (sin cambios de lógica).

- **Causa:** la ventana principal mide **500 DIP** de ancho (`MainWindow.DesignWidthDip`) y `ContentDialog`
  añade **24 DIP de padding por lado** (48 DIP fijos, tema Fluent por defecto). `LegalTextDialog` fijaba
  `MaxWidth="460"` en su `ScrollViewer` → 460 + 48 = **508 DIP**, **más ancho que la propia ventana**; WinUI lo
  comprimía al espacio disponible y el diálogo quedaba pegado a ambos bordes. `ConfirmDialog` tenía el mismo
  `MaxWidth="460"` (mismo bug latente, no reportado aún porque se usa menos).
  Los demás diálogos (`HistoryDialog` 420, `AboutDialog` 400, `WhatsNewDialog`/`PresetsDialog` 380,
  `HealthDialog` 360) ya dejaban margen suficiente y no lo sufrían.
- **Fix:** `MaxWidth` de `460` → **`420`** en `UI/LegalTextDialog.xaml` y `UI/ConfirmDialog.xaml` (total 468 DIP,
  igual que `HistoryDialog`, con 32 DIP de margen respecto a la ventana). Verificado por el usuario en laptop.

**Sin cambios:** `Core/*`, `Services/*`, lógica de formateo, `release.ps1`.

### 2026-06-27 — feat: Tier 5 — relicencia GPLv3 + apartados legales in-app + donaciones (#23–#27) — v1.12.0 (en preparación)

Capa de **confianza/legal/sostenibilidad**, decidida con el usuario: licencia **GPLv3** (en vez de MIT), apartados
legales dentro de la app y **donación voluntaria** por PayPal (no obligatoria, no bloquea nada). **Sin tocar la
lógica de formateo.** Build **0/0**, **226/226 tests** (224 + 2). **No publicado aún:** falta la URL de PayPal.

- **#23 Relicencia a GPLv3 + licencia in-app:** `LICENSE` reemplazado por el texto oficial GNU GPL v3.0 (descargado
  de gnu.org). Se **embebe** en el ejecutable (`.csproj` `<EmbeddedResource ... LogicalName="FormatDiskPro.LICENSE.txt">`)
  y se lee con `Core/LegalText.License()`. Visible en *Ayuda → Licencia* (`UI/LegalTextDialog`, scroll + selección).
  `.csproj`: `<Authors>`/`<Copyright>` (Ricky Angel Jiménez Bueno).
- **#24 Disclaimer:** aviso de formateo/borrado **irreversible** y **sin garantía** en *Ayuda → Acerca de*
  (`UI/AboutDialog`, claves `about.disclaimer*`).
- **#25 Avisos de terceros:** `THIRD-PARTY-NOTICES.txt` (.NET, WinUI/WAS, Segoe Fluent Icons, Inno Setup) embebido;
  `Core/LegalText.ThirdParty()`; *Ayuda → Avisos de terceros* (`UI/LegalTextDialog`).
- **#26 Privacidad:** declaración (sin telemetría; solo GitHub Releases por HTTPS) en *Acerca de*.
- **#27 Donaciones:** botón *Apoyar el proyecto* (PayPal) en *Acerca de* (`AppInfo.DonateUrl`), opcional. Pendiente
  poner la URL real + `.github/FUNDING.yml`.
- **Menú Ayuda:** nuevos ítems *Licencia…* y *Avisos de terceros…*; *Acerca de…* ahora abre `AboutDialog` (antes era
  un `ShowInfoAsync`). Localización (`about.*`, `menu.license`, `menu.thirdParty`, `legal.unavailable`) en 5 idiomas.
- Pruebas: `LegalTextTests` (la licencia embebida es GPLv3; los avisos listan componentes y licencias).

**Sin cambios:** lógica de formateo, servicios de disco/red/instalador, `release.ps1`.
**Verificación visual pendiente** (la app requiere admin; la realiza el usuario).

### 2026-06-27 — feat: Tier 4 completado (#16 S.M.A.R.T. + #19 historial + #20 presets + #22 a11y + #17 autorefresco) — v1.11.0

Cierre del **Tier 4** con los cinco items restantes, **sin tocar la lógica de formateo**. Build **0/0**,
**224/224 tests** (193 + 31). Cada feature mantiene su lógica pura testeable en `Core`.

**#16 — S.M.A.R.T. con umbrales de color + estado + Actualizar** (refina #5)
- `Core/SmartInfo`: `enum SmartLevel { Unknown, Ok, Warning, Critical }` + `TemperatureLevel` (≤50/≤60/>60),
  `WearLevel` (<70/<90/≥90), `ErrorLevel` (0/<100/≥100), puros y testeables.
- `UI/HealthDialog`: temperatura, desgaste y errores se colorean por nivel con **texto de estado anexo**
  (Normal/Atención/Crítico — accesibilidad, no solo color); botón **Actualizar** (`SecondaryButton`) reconsulta
  sin cerrar (deferral + `args.Cancel`). Claves `health.refresh`/`health.level.{ok,warning,critical}`.

**#19 — Historial: filtro + exportación CSV** (refina #4)
- `Core/HistoryEntry`: `Matches(search, category, result)` (búsqueda sin distinción de mayúsculas + filtros) y
  `ToCsv(entries)` (cabecera + filas, escape RFC 4180), puros.
- `UI/HistoryDialog`: caja de búsqueda + dos `ComboBox` (categoría/resultado) + botón **Exportar CSV** (exporta lo
  filtrado vía `FileSavePicker`; el hwnd se pasa desde `MainWindow`). Guard `_ready` para no filtrar en la
  construcción. Claves `history.search`/`noMatch`/`filter.allCat`/`filter.allRes`/`export`.

**#20 — Editar y reordenar presets** (refina #10)
- `Core/Presets.IsRenameAvailable(newName, currentName, existing)` (puro): como `IsNameAvailable` pero excluye el
  propio nombre actual.
- `UI/PresetsDialog`: botones por fila **subir/bajar/editar/eliminar**; "editar" entra en modo edición (renombra y,
  con casilla, actualiza la config a la actual) reutilizando el área superior; el orden se persiste reconstruyendo
  `AppSettings.UserPresets` desde la lista mostrada. Claves `preset.editHeader`/`updateBtn`/`cancelEdit`/
  `updateConfig`/`moveUp`/`moveDown`/`edit`/`delete`.

**#22 — Accesibilidad transversal** (refina la capa UI)
- `AutomationProperties.Name` + tooltip en los botones de icono (acciones de preset vía `IconBtn_Loaded`; selector
  de pasadas). **Aceleradores** de menú: `MenuBarItem.AccessKey` = primera letra del título localizado (Alt+letra,
  correcto en cada idioma). **F5** (`KeyboardAccelerator`) sobre el botón de refrescar.

**#17 — Refresco automático de unidades** (refina la gestión base)
- `Core/DeviceChange` (puro): constantes `WmDeviceChange`/`DbtDeviceArrival`/`DbtDeviceRemoveComplete` +
  `IsArrivalOrRemoval(wParam)`.
- `UI/MainWindow`: subclassing de la ventana (`SetWindowSubclass`/`DefSubclassProc`/`RemoveWindowSubclass` de
  comctl32) para escuchar `WM_DEVICECHANGE`; al llegar/retirarse un dispositivo se recarga `LoadDrives()` con
  **debounce** de 600 ms (`DispatcherTimer`), ignorando si `_isBusy`. La referencia al delegado se mantiene viva;
  el hook se retira en `AppWindow_Closing`.

**Localización:** ~20 claves nuevas en 5 idiomas; la prueba de completitud sigue verde.
**Sin cambios:** lógica de formateo, servicios de disco/red/instalador, `release.ps1`.
**Verificación visual pendiente** (la app requiere admin; la realiza el usuario).

### 2026-06-26 — fix(ui): adaptación a DPI/escalado — ventana por DPI + diálogos con MaxWidth — v1.10.1

En pantallas con escalado alto (p. ej. un portátil con la misma resolución en píxeles que un monitor grande pero
menos pulgadas → Windows aplica 150 %/175 %) los diálogos salían apretados y el texto se cortaba (reportado con
captura del diálogo de Novedades). **Causa:** el tamaño de ventana se fijaba con `AppWindow.Resize(500, 900)` en
**píxeles físicos crudos**, así que al 150 % el contenido solo recibía ~333×600 DIP y los diálogos de ancho fijo
(380/360 DIP) desbordaban y se recortaban. Build **0/0**, **193/193 tests** (cambios solo de UI). Verificado por el usuario.

- `MainWindow`: nuevo `SizeAndCenterWindow()` — lee el DPI del monitor (`GetDpiForWindow` vía P/Invoke), convierte el
  tamaño de diseño **500×900 DIP** a píxeles físicos (`* dpi/96`) y lo **acota al área de trabajo** (si no cabe a lo
  alto, el contenido ya tiene scroll); centra dentro del área de trabajo con offset X/Y (correcto en multimonitor).
  Sustituye al `Resize(500,900)` + `CenterWindow()` crudos. Constantes `DesignWidthDip`/`DesignHeightDip`.
- Diálogos: `Width` fijo → **`MaxWidth`** en `WhatsNewDialog`, `PresetsDialog`, `HealthDialog`, `HistoryDialog`
  (con `MinWidth=280` para el `ListView`) y en el panel del aviso de actualización (`ShowUpdateAvailableAsync`), para
  que el texto **se ajuste** en vez de cortarse. Scroll **horizontal deshabilitado** en los visores de notas
  (Novedades / aviso de actualización) para que `TextWrapping` siempre envuelva. `ConfirmDialog` ya era adaptable.

**Sin cambios:** `Core/*`, `Services/*`, lógica de formateo, pruebas, `release.ps1`.

### 2026-06-26 — feat: Tier 4 quick wins (#15 IOPS + #14 pasadas + #18 idioma auto + #21 changelog) — v1.10.0

Primer corte del **Tier 4** (refinado, no añade features nuevas), **sin tocar la lógica de formateo**.
Build **0/0**, **193/193 tests** (172 + 21). La firma de `bench.resultBody` cambia (2 args más para las IOPS).

**#15 — IOPS en el benchmark** (refina #9)
- `Core/Benchmark`: `Iops(bytesPerSec, blockBytes)` = `bytes/s ÷ tamaño de bloque` (puro) + const `Random4KBlockBytes` (4096).
- `UI/MainWindow`: el diálogo de resultado muestra las IOPS entre paréntesis junto a los MB/s del 4 KiB aleatorio
  (`FormatIops` redondea y añade "IOPS"); `bench.resultBody` pasa a 7 args (`{5}`/`{6}` = IOPS escritura/lectura).
- Pruebas: `Iops` (división, bloque no positivo → 0, velocidad 0 → 0).

**#14 — Pasadas de borrado seguro configurables** (refina #3)
- `Core/SecureWipe`: `AllowedPasses` = `[1,3,7]` + `NormalizePasses` (valor válido o 1), puros.
- `AppSettings.SecureWipePasses` (por defecto 1, validado al usarse). Selector `WipePassesPicker` (1/3/7) en
  *Opciones de formato*, **activo solo con *Borrado seguro* marcado** (atenuado si no); persiste al cambiar.
  La confirmación muestra `+ Borrado seguro ×N` (si N > 1) y el log de auditoría añade `passes=N`. La constante fija
  `SecureWipePasses = 1` desaparece; el flujo (`StartButton_Click` → `RunFormatAsync` → `SecureWipe.RunAsync`) recibe N.
- Decisión: las pasadas son una **preferencia global**, no por preset (los presets no cambian de esquema).
- Pruebas: `NormalizePasses` (válidos/ inválidos → 1), `AllowedPasses`, round-trip de `AppSettings.SecureWipePasses`.

**#18 — Idioma automático en el primer arranque** (refina #11)
- `Localization.FromCulture(cultureName)` (puro): toma la parte de idioma de dos letras (antes de `-`/`_`) y la
  mapea con `FromCode`; vacío/desconocido → Es. En el constructor de `MainWindow`, si **no** existía `settings.json`
  (`!AppSettings.LoadedFromFile`), siembra `Language` desde `CultureInfo.CurrentUICulture`; tras el primer guardado
  manda la elección del usuario. Guard `_uiReady` para no persistir por eventos disparados durante la construcción.
- Pruebas: `FromCulture` (es-ES/en-US/pt-BR/fr-FR/it-IT, solo idioma, no soportado → Es, vacío/null → Es).

**#21 — Changelog en el aviso de actualización** (refina updates)
- `MainWindow.ShowUpdateAvailableAsync(rel)`: el diálogo *"Actualización disponible"* muestra el cuerpo del release
  (ya en `ReleaseInfo.Notes` que devuelve `CheckForUpdateAsync`, convertido con `ReleaseNotes.ToPlainText`) en un
  `ScrollViewer` antes de descargar; botones *Descargar e instalar* / *Más tarde*. Sustituye al confirm de texto plano.

**Localización:** `opt.passes`, `update.availBody`/`update.changelog`/`update.download`/`update.later` (5 idiomas);
`bench.resultBody` ampliado a IOPS; **eliminada** la clave `update.available` (reemplazada).

**Sin cambios:** `Core/FormatLogic`, lógica de formateo, servicios de disco/red/instalador, `release.ps1`.
**Verificación visual pendiente** (la app requiere admin; la realiza el usuario).

### 2026-06-25 — docs: abierto el Tier 4 — Refinado de características existentes (propuesto, #14–#22)

Tras cerrar Tiers 1–3 y descartar el #13, se abre en [`ROADMAP.md`](ROADMAP.md) un **Tier 4** de
**refinamiento** (no añade features nuevas: pule las existentes; numeración global continúa en #14). Es un
backlog **propuesto** acordado con el usuario, sin comprometer versión ni fecha. Solo documentación
(`ROADMAP.md` + `CONTEXT.md`), sin cambios de código.

- **#14** Pasadas de borrado seguro configurables (1/3/7) — refina #3; `Core/SecureWipe` ya soporta N pasadas.
- **#15** IOPS junto a MB/s en el benchmark — refina #9; cálculo puro en `Core/Benchmark`.
- **#16** S.M.A.R.T. con umbrales de color + texto de estado + botón Actualizar — refina #5.
- **#17** Refresco automático de unidades (`WM_DEVICECHANGE`) — refina la gestión base.
- **#18** Idioma automático en el primer arranque (`CultureInfo`) — refina #11.
- **#19** Búsqueda/filtro + exportación CSV en el historial — refina #4.
- **#20** Editar y reordenar presets — refina #10.
- **#21** Changelog en el aviso «Actualización disponible» — refina updates (reusa `ReleaseNotes`).
- **#22** Pulido de accesibilidad (nombre del botón borrar preset, aceleradores, tab order) — capa UI.

Prioridad sugerida: quick wins (#15, #21, #14, #18) → trabajo medio (#16, #22, #19, #20) → integración (#17).

### 2026-06-25 — fix: correcciones de una revisión de código (UI/calidad/docs), sin tocar la lógica de formateo

Revisión completa del proyecto (código, seguridad, rendimiento, UI/UX, accesibilidad, arquitectura, pruebas).
Resultado: arquitectura y blindaje sólidos; se aplicaron 7 correcciones menores. Build **0/0**, **172/172 tests**.

- **UI — doble corchete en la unidad protegida** (`UI/DriveViewModel.cs`): `protected.tag` ya incluye
  corchetes y espacio (`"[Protegido] "`), pero `DisplayText` los envolvía otra vez → `[[Protegido] ]C:\…`.
  Corregido a `$"{tag}{label}"` → `[Protegido] C:\…`.
- **Etiqueta — `MaxLength` dinámico por FS** (`UI/MainWindow.xaml.cs`, `UpdateLabelMaxLength`): el `TextBox`
  tenía `MaxLength="32"` fijo aunque FAT/FAT32/exFAT permiten 11; ahora se ajusta en
  `FileSystemPicker_SelectionChanged` para dar feedback inmediato.
- **Validación de etiqueta compartida** (`UI/MainWindow.xaml.cs`, `ValidateLabelAsync`): se extrajo la
  validación (caracteres inválidos + longitud por FS) a un helper único usado por *Iniciar* y por
  *Reinicializar unidad…* — antes Reinit solo validaba caracteres, no la longitud.
- **Localización — evento muerto** (`Localization/Localization.cs`): se elimina `L.Changed` (declarado e
  invocado pero sin suscriptores; el refresco se hace llamando a `ApplyLanguage()`).
- **Docs — `.csproj`**: el comentario del target `CopyAppPriToPublish` estaba corrupto (mojibake por
  recodificación); reescrito limpio (ASCII), sin cambios funcionales.
- **Docs — `AppSettings.Language`**: el XML-doc decía `"es"/"en"`; ahora refleja los 5 idiomas (`es/en/pt/fr/it`).
- **Docs — `README.md`**: añadida nota del *modelo de confianza* de la auto-actualización (HTTPS sí; sin
  verificación de firma/hash antes de ejecutar elevado → se confía en la cuenta/releases de GitHub; firma
  Authenticode opcional). Refleja la decisión ya tomada de no firmar (Tier 3 #13 descartado).

**Limpieza (mismo día):**
- **Borrado `UI/MainForm.resx`**: plantilla vacía de WinForms (solo el esquema, sin datos) que quedó tras la
  migración a WinUI 3; no la referenciaba ninguna clase (`MainForm` ya no existe). Sin impacto en el build.
- **6 claves de localización sin usar eliminadas** de `Localization.cs` (216 → 210): `app.subtitle`,
  `drive.label` (restos del subtítulo/Header retirados en el rediseño), `status.verifying` (se usa
  `verify.writing`/`verify.reading`), `msg.info` (se usan `msg.warning`/`msg.error`), `preset.title`
  (solo se usa `preset.body`) y `preset.deleteTip` (tooltip nunca cableado en `PresetsDialog`). Verificado
  con diff de conjuntos contra HEAD: exactamente esas 6, ninguna en uso (incluidas las dinámicas
  `reinit.stage.{clean,init,partition,format}`, que se conservan).
- **Se conserva** `FormatLogic.DecodeArguments` (solo lo usan los tests, pero respalda el test de round-trip
  que prueba que `EncodeArguments` es reversible). Las skills de frameworks de test no usados
  (`csharp-mstest/nunit/tunit`) se mantienen por decisión documentada en `.claude/CLAUDE.md`.

**Sin cambios:** `Core/FormatLogic` (lógica), lógica de formateo, servicios de red/instalador, `release.ps1`.

### 2026-06-24 — decisión: descartado el Tier 3 #13 (winget + firma) → Tier 3 cerrado

Decisión del usuario: **no** se publicará paquete **winget** ni se **firmará el instalador**. La
distribución por **GitHub Releases** con auto-actualización integrada se considera suficiente y fiable
para el público objetivo ("quien descarga de GitHub ya asume su fiabilidad"). El soporte de firma sigue
disponible de forma **opcional** en `build-installer.ps1`/`release.ps1`, pero deja de ser un objetivo del
proyecto. Con #10/#11/#12 ya publicados (v1.8.0) y #13 descartado, el **Tier 3 queda cerrado**. Sin
cambios de código; solo `ROADMAP.md` y `CONTEXT.md` sincronizados.

### 2026-06-23 — refine: Tier 2 #9 (benchmark) — perfil estilo CrystalDiskMark (SEQ Q8 + RND4K, sin caché, mediana) — v1.9.0

Rediseño del benchmark para medir la **velocidad real** del medio en la mayoría de unidades. Evolución en el
día: primero E/S sin caché + promedio de pasadas (256 → 512 MB); luego perfil completo estilo CrystalDiskMark.
Build **0/0**, **172/172 tests** (165 + 7 del benchmark). La firma de `BenchmarkResult` cambia (la UI se adaptó).

- **Métricas (4):** **secuencial** (bloque 1 MiB, cola **Q8**) y **4 KiB aleatorio** (Q1), lectura y escritura.
- `Core/Benchmark.cs`: `record BenchmarkScore(Read, Write)` + `BenchmarkResult(Sequential, Random4K, TestBytes)`;
  `TargetTestBytes` 512 MiB; `PlanTestBytes(free, blockSize)` **trunca a múltiplo de bloque** (la E/S sin caché
  exige alineación) y exige ≥ 1 bloque; **`Median(ReadOnlySpan<double>)`** (robusto al arranque frío/picos, sustituye
  a la media) y **`RandomAlignedOffset(length, block, rng)`** (offset alineado al azar). Todo puro y testeable.
- `Services/BenchmarkRunner.cs`: motor de E/S **sin caché** (`FILE_FLAG_NO_BUFFERING` = `(FileOptions)0x20000000`
  vía **`RandomAccess`** sobre `File.OpenHandle`, handle `Asynchronous`). Fase secuencial con **cola Q8** (varias
  E/S en vuelo con `Task.WhenAll`) para no infravalorar NVMe/SSD; RND4K a Q1. Buffers **alineados a 4096 B** (sobre-
  asignados y fijados con `GCHandle`, sin `unsafe`). Medición por **ventanas de tiempo** (1,5 s; se adapta a
  unidades rápidas/lentas) y **mediana** de `Passes`=3 ventanas. El archivo se rellena una vez (fase *Preparing*)
  para que las lecturas sean válidas; las ventanas lo recorren en bucle. `BenchPhase` ampliado a 5 fases.
- `UI/MainWindow`: el handler mapea las 5 fases a estado y el diálogo de resultado muestra las **cuatro** cifras.
  Pulido del cierre: `EndOperation()` corre **antes** del diálogo de resultado (el pie deja de estar "ocupado":
  cronómetro parado, botón "Cerrar") y un flag `benchRunning` descarta callbacks de progreso tardíos que pisaban
  el estado final; la cancelación se rastrea con un flag local y los desenlaces (cancelado/sin espacio/resultado)
  se muestran tras el `finally`.
- Localización (5 idiomas): `bench.confirmBody` (descripción nueva), 5 claves de fase (`bench.preparing`/`seqWrite`/
  `seqRead`/`rndWrite`/`rndRead`), `bench.resultBody` (4 métricas) y `bench.note` ("sin caché; SEQ Q8 + RND4K Q1,
  mediana de 3 pasadas"). Sin referencia a CrystalDiskMark en el mensaje de UI (preferencia del usuario).
- Pruebas: `PlanTestBytes` (bloque 1 MiB), `Median` (par/impar/no muta) y `RandomAlignedOffset` (rango/alineación).
- Docs: `ROADMAP.md` #9, `README.md` y `CONTEXT.md` sincronizados.

### 2026-06-22 — feat: Tier 3 #10 (presets personalizados) + #11 (más idiomas) + #12 (aviso al terminar) — v1.8.0

Tres mejoras de pulido, **sin tocar la lógica de formateo**. Build **0/0**, **165/165 tests** (141 + 24).

**#10 — Presets personalizados**
- `Core/Presets`: `NormalizeName` (recorta/colapsa espacios) e `IsNameAvailable` (no vacío, ≤ 40, único
  case-insensitive), puros y testeables; `Presets.All` (integrados) intacto.
- `AppSettings.UserPresets` (`List<FormatPreset>`, persistido en `settings.json`; round-trip testeado).
- `UI/PresetsDialog`: guardar la config actual con nombre (valida) y eliminar presets propios. `BuildPresetsMenu`
  reescrito: integrados → propios → *Gestionar presets…* (`MnuManagePresets`). Se reconstruye en `ApplyLanguage`.

**#11 — Más idiomas (ES/EN/PT/FR/IT)**
- `Localization.cs`: el `Map` pasa de tupla `(Es, En)` a **`string[]`** indexado por `AppLang` (orden Es, En, Pt, Fr, It);
  `T` indexa por idioma con fallback. ~250 claves traducidas a PT/FR/IT. `AppLang` ampliado; `L.FromCode`/`ToCode`
  para persistir el código. Menú *Idioma* con 5 opciones; restauración con `L.FromCode(_settings.Language)`.
- Prueba de **completitud**: cada entrada tiene exactamente 5 traducciones no vacías (vía `InternalsVisibleTo`).

**#12 — Aviso al terminar**
- `Services/Notifier`: `ShouldNotify(elapsed, enabled, cancelled, threshold)` puro + `OperationFinished(hwnd)` (Win32
  `MessageBeep` + `FlashWindowEx` con `FLASHW_TRAY|FLASHW_TIMERNOFG`), solo si la ventana no está en primer plano.
- `AppSettings.NotifyOnFinish` (por defecto `true`); llamada en `EndOperation` con umbral de **10 s** y la ventana vía
  `WindowNative.GetWindowHandle`. Interruptor *Configuración → Avisar al terminar* (`MnuNotify`).

**Localización (nuevas claves):** `menu.lang.pt/fr/it`, `menu.notify`, `menu.managePresets`, bloque `preset.*` ampliado.
**Build:** `<InternalsVisibleTo>FormatDiskPro.Tests</InternalsVisibleTo>` en el `.csproj` (para probar `L.Map`).

**Sin cambios:** `Core/FormatLogic`, lógica de formateo, servicios de red/instalador, `release.ps1`.

### 2026-06-22 — release: v1.7.1 — fix: el diálogo de novedades no aparecía al actualizar desde 1.6.0

El diálogo de novedades (1.7.0) no se mostraba al actualizar **desde una versión que no guardaba
`LastVersionSeen`** (p. ej. 1.6.0 → 1.7.0): al venir el campo en `null`, la lógica lo trataba como
instalación nueva y, por seguridad, no mostraba nada. Build **0/0**, **141/141 tests** (138 + 3 nuevos).

- `AppSettings.LoadedFromFile` (nuevo, `[JsonIgnore]`, no persistido): marca si la configuración se cargó
  de un `settings.json` **existente** (uso previo de la app), capturado en `Load` antes de cualquier guardado.
- `MaybeShowWhatsNewAsync`: si no hay versión registrada, ahora muestra las novedades **solo si** ya existía
  configuración (`LoadedFromFile`) → distingue *actualización desde una versión sin el campo* de *instalación
  nueva*. Con versión registrada, se mantiene la comparación por igualdad.
- Pruebas: `AppSettingsTests` cubre `LoadedFromFile` (defaults/instancia nueva = false, carga de archivo = true,
  no se serializa).

**Sin cambios:** features de Tier 2, lógica de formateo, servicios de red/instalador, `release.ps1`.

### 2026-06-22 — release: v1.7.0 — feat: Tier 2 #8 (reinicializar unidad) + #9 (benchmark) → Tier 2 completado

Las dos últimas herramientas de "diagnóstico/gestión" + el **diálogo de novedades**, **sin tocar la lógica de
formateo**. Build **0/0**, **138/138 tests** (110 + 14 `ReinitPlan` + 6 `Benchmark` + 8 `ReleaseNotes`).

**#8 — Reinicializar unidad** (decisiones del usuario: backend cmdlets de Storage; alcance solo extraíbles)
- `Core/ReinitPlan.cs` (puro, testeable): `StyleFor` (GPT si > 2 TB, si no MBR), `ParseNewLetter`
  (marcador `LETTER:X`), `ToPowerShell`, `record ReinitResult`.
- `Services/ReinitDrive.cs`: un script `-EncodedCommand` que limpia + inicializa + crea partición + formatea
  (`Clear-Disk`/`Initialize-Disk`/`New-Partition`/`Format-Volume`), emitiendo marcadores `STAGE:*` por stdout
  que se transmiten en streaming a la UI (espejo de `CheckDisk`); cancelación con `Kill(entireProcessTree)`.
- `DiskService.GetDiskNumberAsync` (`(Get-Disk).Number`) para la **guarda crítica** disco objetivo ≠ disco de Windows.
- UI: *Herramientas → Reinicializar unidad…* (`MnuReinit`), **solo `DriveType.Removable`**, bloqueo de
  sistema/protegido, comparación de nº de disco físico, y **confirmación reforzada** reutilizando `ConfirmDialog`
  (escribir la letra) con resumen que advierte que se borra **todo el disco físico**. Tras éxito, `LoadDrives()`
  selecciona la nueva letra.

**#9 — Benchmark rápido** (decisión del usuario: permitido en cualquier unidad lista)
- `Core/Benchmark.cs` (puro, testeable): `PlanTestBytes` (256 MB acotado por libre − margen 64 MB),
  `BytesPerSec`, `record BenchmarkResult`.
- `Services/BenchmarkRunner.cs`: escribe un archivo temporal por bloques de 8 MB con `WriteThrough` y lo relee
  con `SequentialScan`, cronometrando con `Stopwatch`; **no destructivo** (borra el temporal en `finally`).
  Reutiliza la mecánica de E/S de `SecureWipe`/`CapacityVerifier`.
- UI: *Herramientas → Benchmark rápido…* (`MnuBenchmark`), progreso por fase (escritura/lectura) en el footer y
  diálogo de resultado con MB/s + nota de caché. `Throughput.FormatSpeed` para el formato.

**Diálogo de novedades** (petición del usuario: mostrar tras actualizar las mismas notas que se publican en GitHub)
- `Core/ReleaseNotes.ToPlainText` (puro, testeable): Markdown → texto plano (encabezados, viñetas, negritas, código,
  enlaces, líneas en blanco). `AppInfo.ReleaseByTagApiUrl` + `UpdateService.GetReleaseByTagAsync` (refactor que extrae
  `ParseRelease`/`GetFromUrlAsync` compartidos con `GetLatestAsync`).
- `AppSettings.LastVersionSeen` persiste la versión vista; `MaybeShowWhatsNewAsync` (en `OnFirstActivated`) muestra las
  novedades **una sola vez** cuando la versión cambia (no en la instalación inicial). `UI/WhatsNewDialog` (notas en
  scroll + "Ver en GitHub"); ítem manual *Ayuda → Novedades…* (`MnuWhatsNew`).

**Localización:** claves `menu.reinit`/`menu.benchmark`/`menu.whatsnew`, bloques `reinit.*` (guardas, etapas, resumen,
resultado), `bench.*` (confirmación, fases, resultado, nota) y `whatsnew.*` (título, versión, GitHub, vacío) en ES/EN.

**Sin cambios:** `Core/FormatLogic`, lógica de formateo, `installer/`, `release.ps1`.

### 2026-06-21 — release: v1.6.0 — feat: Tier 2 #6 (chkdsk) + #7 (protección de escritura)

Dos herramientas de diagnóstico/gestión, **sin tocar la lógica de formateo**. Build **0/0**, **110/110 tests**.
**Publicado como v1.6.0** (probado por el usuario).

**#7 — Protección de escritura** (decisión del usuario: auto + menú)
- `DiskService.IsDiskReadOnlyAsync` (`(Get-Disk).IsReadOnly`) y `ClearReadOnlyAsync` (`Set-Disk -IsReadOnly $false`),
  con el patrón seguro `-EncodedCommand` (sin diskpart).
- **Chequeo automático en `StartButton_Click`**: si el disco está en solo lectura, ofrece quitar la protección
  antes de formatear (evita el fallo críptico). Ítem manual *Herramientas → Quitar protección de escritura…*
  (`MnuUnlock`), bloqueado en el disco de sistema.

**#6 — chkdsk** (`Services/CheckDisk.cs`)
- `RunAsync(letter, repair, progress, ct)`: lanza `chkdsk X:` (solo lectura, universal) o `chkdsk X: /f` (reparar),
  espejo del runner de `RunFormatComAsync` (stream de stdout + `FormatLogic.ExtractPercent` → progreso; escribe "N"
  por stdin para no colgarse si pide programar reinicio; cancelación por `Kill`). `Interpret(code, repair)` puro
  (`enum CheckResult`), testeado. Ítem *Herramientas → Comprobar errores (chkdsk)…* (`MnuCheck`) con diálogo de
  modo (solo-comprobar/reparar/cancelar); **reparación bloqueada en el disco de sistema** (no se ofrece el botón).

**Localización:** `menu.check`, `menu.unlock` + bloques `unlock.*` y `check.*` (ES/EN).
**Verificación:** probada por el usuario (chkdsk solo-comprobar/reparar; detección/quita de protección) — OK.

### 2026-06-21 — release: v1.5.0 — feat: Tier 2 #5 — S.M.A.R.T. ampliado (diálogo de salud del disco)

Detalle de diagnóstico del disco físico más allá de Salud/Bus/Media, **sin tocar la lógica de formateo**.
Decisión del usuario: **diálogo dedicado** (no recargar el panel de info compacto). Build **0/0**, **102/102 tests**.
**Publicado como v1.5.0** (probado por el usuario en claro/oscuro, funcionando).

- **`Core/SmartInfo.cs`** (modelo + `Parse` puro, testeable): salud, bus, medio, RPM, temperatura, horas de
  encendido, desgaste SSD y errores de lectura/escritura; campos numéricos anulables (`null` si la unidad no
  los expone). Tests: `SmartInfoTests` (línea completa, USB sin contadores, no numéricos, líneas inválidas).
- **`DiskService.GetSmartAsync`**: consulta extendida (`Get-StorageReliabilityCounter` con `-ErrorAction
  SilentlyContinue`) vía el patrón seguro `-EncodedCommand` existente. `GetHealthAsync`/`HealthInfo` intactos
  (panel inline rápido, sin riesgo).
- **`UI/HealthDialog.xaml(.cs)`**: diálogo con carga **bajo demanda** (en `Opened`: "Consultando…" → rellena),
  filas etiqueta/valor con fallback **"No disponible"**, estado de salud coloreado según el tema, botón con
  esquinas redondeadas. Abierto desde el nuevo ítem **Herramientas → Salud del disco (S.M.A.R.T.)…** (`MnuHealth`).
- **Localización:** `menu.health` + bloque `health.*` (ES/EN). 

**Verificación:** probada por el usuario (disco SATA SSD: salud, temperatura, horas, desgaste; en claro y oscuro) — OK.

### 2026-06-21 — release: v1.4.0 — feat: Tier 1 de mejoras de UX/diagnóstico

Cuatro features que no tocan la lógica de formateo, respetando la arquitectura por capas. Plan acordado
con el usuario (sobrescritor propio para el borrado seguro, 1 pasada por defecto). Build **0/0**, **94/94 tests**.
**Publicado como v1.4.0** (probado por el usuario, funcionando). Hoja de ruta restante en `ROADMAP.md`.

1. **Persistencia de configuración** — `Services/AppSettings.cs` (`Load/Save` defensivos sobre
   `%AppData%\FormatDiskPro\settings.json`, `System.Text.Json`). Persiste idioma, tema (auto/light/dark) y
   última unidad. Cableado en `MainWindow`: se restaura en el constructor (nuevo `ApplyThemeMode`, refactor de
   los handlers de tema/idioma) y se guarda en cada cambio. Tests: `AppSettingsTests` (round-trip + defaults
   ante corrupto/ausente).
2. **ETA + velocidad (MB/s)** — `Core/Throughput.cs` (`Eta`, `FormatSpeed`, `FormatEta`, puro). El timer de 1 s
   compone `mm:ss · MB/s · ETA` con **velocidad instantánea por ventana deslizante** (campos `_opBytesDone/_opTotalBytes`),
   alimentado por los callbacks de verificación y borrado seguro. Footer reorganizado a 2 columnas (estado truncable
   + tiempo/velocidad a la derecha). Tests: `ThroughputTests`.
3. **Borrado seguro con progreso real** — `Services/SecureWipe.cs` (estructura espejo de `CapacityVerifier`):
   sobrescribe el espacio libre por bloques de 8 MB (archivos de 1 GB, margen de 64 MB) reportando %/bytes;
   última pasada aleatoria, previas 0x00/0xFF; **1 pasada por defecto**. Reemplaza a `DiskService.SecureWipeAsync`
   (cipher /w, **eliminado**) en `RunFormatAsync`, con barra determinista. Caveat documentado: TRIM en SSD.
   Tests: `SecureWipeTests` (helpers `PlannedBytes`/`PassPattern`).
4. **Visor de historial integrado** — `Core/HistoryEntry.cs` (parser puro: categoría + resultado a partir de las
   líneas de `History.Log`); `UI/HistoryDialog.xaml(.cs)` (ListView con chip de color por resultado, abrir archivo,
   vaciar con confirmación en flyout); `History.ReadLines()`/`Clear()`. `MnuHistory_Click` abre el visor.
   Tests: `HistoryEntryTests`.

**Localización:** nuevas claves `status.wiping.progress` y bloque `history.*` (ES/EN).
**Pulido del visor:** `HistoryDialog` con botón de esquinas redondeadas (`CloseButtonStyle` CornerRadius 4) y
ancho reducido (380) para alinearlo con la proporción de la app; colores del chip según el tema efectivo.
**Verificación:** probada por el usuario (wipe, persistencia entre arranques, visor en claro/oscuro) — OK.

### 2026-06-21 — fix/ui: crash al cambiar el tema del sistema + botones de caption sin re-tematizar

Dos bugs reportados tras el rediseño (ambos preexistentes en la lógica de tema):

**1) Cierre inesperado al cambiar el tema de Windows en modo Automático** (`UI/MainWindow.xaml.cs`)
- Causa: la suscripción a **`UISettings.ColorValuesChanged`** se dispara en un **hilo en segundo
  plano**; en WinUI 3 *desktop* esto provoca cierres intermitentes al cambiar el tema del sistema
  (carrera con el re-tematizado interno de WinUI), pese a marshalizar con `DispatcherQueue`.
- Corrección: se sustituye por **`FrameworkElement.ActualThemeChanged`** (se dispara en el hilo de
  UI cuando cambia el tema **efectivo** del contenido, incluido el cambio del tema de Windows con
  `RequestedTheme = Default`). En modo forzado Claro/Oscuro no se dispara con el sistema (correcto).
  Se elimina `OnSystemThemeChanged`; `IsSystemDark()`/`_uiSettings` se conservan para la semilla
  inicial y el menú **Tema → Automático**.

**2) Los botones de caption (min/max/cerrar) no cambiaban de color al alternar el tema en caliente**
- Causa: con `Window.ExtendsContentIntoTitleBar = true` se confiaba en el auto-tematizado de WinUI,
  que **no refresca** los botones de caption en un cambio de tema en vivo (se quedaban con los
  colores del tema anterior: glifos con bajo contraste / fondo oscuro en claro).
- Corrección: nuevo `UpdateCaptionButtonColors(bool dark)` (llamado desde `ApplyTheme`) que fija
  explícitamente **todos** los colores de los botones (foreground normal/hover/pressed/inactive +
  fondos background/hover/pressed) según el tema **efectivo**, con `ButtonBackgroundColor` transparente
  (Mica). **Segundo problema reportado (tema forzado):** dejar los fondos hover/pressed en `null` no
  bastaba —su valor por defecto sigue el tema del **sistema**, no el `RequestedTheme` forzado—, así que
  al forzar Claro con Windows en Oscuro (o viceversa) el hover del botón quedaba con el tema contrario
  (recuadro oscuro sobre la app clara). Ahora los fondos hover/pressed se derivan del tema efectivo
  (overlays sutiles: blanco sobre oscuro / negro sobre claro). **Compromiso:** al fijar el fondo hover,
  el botón Cerrar deja de ponerse rojo (la API `AppWindowTitleBar` es global para todos los botones);
  se prioriza la consistencia con el tema forzado.

Build **0/0**, **59/59 tests** ✅. **Verificación visual pendiente** (la app requiere admin).

### 2026-06-21 — feat/ui: sistema de diseño inspirado en Win11Debloat (tarjetas + acento del sistema)

Rediseño del lenguaje visual de la UI tomando como referencia el aspecto de **Win11Debloat**
(la imagen aportada por el usuario; el repo oficial `raphire/win11debloat` es en realidad un
script de PowerShell **sin GUI**, así que la imagen es la fuente de verdad del diseño). Se
adapta el *lenguaje visual* (no el layout literal de 3 columnas) a la ventana fija de la utilidad.

**Decisiones acordadas con el usuario**
- **Tema:** se mantiene claro/oscuro/automático (no se fija oscuro); se re-estiliza para ambos.
- **Acento:** se usa el **color de acento que el usuario tiene seleccionado en Windows 11**
  (no un teal fijo), vía recursos Fluent que ya lo siguen y se adaptan a claro/oscuro.
- **Alcance:** rediseño visual completo con tokens centralizados.

**Cambios (capa `UI/`)**
- Nuevo `UI/Theme/AppTheme.xaml` (ResourceDictionary) con tokens centralizados, fusionado en
  `App.xaml`: `AppCardStyle` (tarjeta: borde 1px + esquinas 8px), `SectionIconStyle` /
  `SectionTitleStyle` (encabezado con icono + título en `AccentTextFillColorPrimaryBrush`),
  `InfoTextStyle`, `HintTextStyle`, `CardDividerStyle`, `FooterBarStyle`.
- `MainWindow.xaml` reorganizado en **3 tarjetas de sección** con encabezado (icono Segoe Fluent +
  título de acento): **Unidad** (selector + botón refrescar + panel de info con separador),
  **Configuración de formato** (FS + descripción + unidad de asignación + etiqueta + restaurar) y
  **Opciones de formato** (checkboxes). El footer (`Border` con línea superior) agrupa barra de
  progreso, estado/tiempo y los botones: **Cerrar** (izquierda) e **Iniciar** (acento, derecha),
  al estilo Win11Debloat (Back/Next). Filas del Grid: 48 / Auto(menú) / *(scroll) / Auto(footer).
- `MainWindow.xaml.cs`: `ApplyLanguage` localiza los nuevos títulos de sección (`UnitGroupLbl`,
  `FormatGroupLbl`) y se elimina el `Header` redundante del `DrivePicker` (lo cubre el título de
  la tarjeta). Ventana **500×900** (antes 500×840) para que las tarjetas respiren sin scroll.
- `Localization.cs`: nuevas claves `section.drive` ("Unidad"/"Drive") y `section.format`
  ("Configuración de formato"/"Format settings").

**Sin cambios:** `Core/`, `Services/`, lógica de negocio, `installer/`, `release.ps1`.
Build **0/0**, **59/59 tests** ✅. **Verificación visual pendiente** (la app requiere admin; no se
pudo lanzar desde la sesión no elevada del agente — la realiza el usuario).

### 2026-06-20 — release: v1.2.2 — fix(auto-update): el cierre intencional para actualizar quedaba bloqueado

**Causa raíz**
- En el flujo de auto-actualización (`DownloadAndRunUpdateAsync`), tras descargar se llama a
  `LaunchInstaller(silent)` y `Application.Current.Exit()`. Pero `Exit()` dispara `AppWindow_Closing`,
  que veía `_isBusy == true` (la operación de descarga aún no había terminado en el `finally`), ponía
  `args.Cancel = true` y mostraba el diálogo **"Operación en progreso"**. Resultado: la app **no se cerraba**,
  seguía reteniendo el `AppMutex` y los archivos de `{app}`, y el instalador no podía reemplazarla.
- Bug heredado **idéntico** de la 1.1.0 (WinForms, `OnFormClosing` con la misma guarda `_isBusy`).

**Corrección** (`UI/MainWindow.xaml.cs`)
- Nuevo flag `_closingForUpdate`. Se pone en `true` **antes** de `LaunchInstaller` + `Exit()`.
- `AppWindow_Closing`: si `_closingForUpdate`, deja cerrar (no cancela ni muestra el diálogo).
- `EndOperation`: si `_closingForUpdate`, no restaura la UI (evita tocar controles durante el teardown).
- Build 0/0, 59/59 tests ✅.

**Sobre la prueba 1.1.0 → 1.2.1 del usuario (capturas)**
- El **instalador interactivo** (diálogo "Seleccione el Idioma" + asistente) es **esperado** en ese salto:
  la 1.1.0 publicada lanza el instalador **sin** `/VERYSILENT` (`LaunchInstaller(path)` sin flags — código
  congelado en `v1.1.0`). No se puede cambiar retroactivamente para quien ya está en 1.1.0. La auto-actualización
  **silenciosa** solo aplica desde la versión que incluya este fix (1.2.2) en adelante.

**Publicado:** **v1.2.2** vía `release.ps1 -Version 1.2.2` (commit `release: v1.2.2` en `master`, tag
`v1.2.2`, GitHub Release con `FormatDiskPro-1.2.2-setup.exe` adjunto, sin firmar). El salto silencioso
real solo puede validarse actualizando **desde** una build con este fix (1.2.2 → 1.2.3+).

### 2026-06-19 — release: v1.2.1

- Publicada **v1.2.1** (`release: v1.2.1` en `master`, tag `v1.2.1`, GitHub Release con
  `FormatDiskPro-1.2.1-setup.exe` adjunto, **sin firmar**). Corrige el crash de arranque de la 1.2.0
  (faltaba `FormatDiskPro.pri`) e incorpora `[InstallDelete]`, `AppMutex` y auto-actualización silenciosa.
- `gh` no estaba instalado: el Release se creó vía API de GitHub reutilizando la credencial git cacheada.
- **Nota para usuarios en la 1.2.0 rota:** su auto-updater no corre (la app no abre) → descarga manual de la 1.2.1.

### 2026-06-19 — fix(crítico): instalador/actualización — la 1.2.0 crasheaba al iniciar

**Causa raíz (crash al iniciar tras actualizar a 1.2.0)**
- `dotnet publish` para WinUI 3 *unpackaged* (self-contained) **no copiaba `FormatDiskPro.pri`** (el índice de recursos propio de la app) a la carpeta de publicación; solo iban los PRI del framework (`Microsoft.UI.*.pri`). Sin ese PRI, WinUI no resuelve el XAML ni `XamlControlsResources` → la app **arranca y se cierra al instante**. Verificado: el `.pri` existe en `bin\...\win-x64\` pero faltaba en `...\publish\`.
- No era específico de la actualización: una instalación limpia de la 1.2.0 también habría crasheado.

**Correcciones**
- `FormatDiskPro.csproj`: target MSBuild `CopyAppPriToPublish` (`AfterTargets="Publish"`) que copia `$(TargetName).pri` a `$(PublishDir)`. Verificado: tras el fix, el publish ya contiene `FormatDiskPro.pri` + los 3 PRI del framework.
- `installer.iss`: nueva sección **`[InstallDelete]` `Type: filesandordirs; Name: "{app}\*"`** — limpia la instalación previa antes de copiar. Imprescindible al pasar de 1.1.0 (WinForms, framework-dependent) a 1.2.0 (WinUI 3, self-contained): el conjunto de archivos cambia por completo. No hay datos de usuario en `{app}` (historial en `%AppData%`).
- `installer.iss` + `Program.cs`: **`AppMutex=Global\FormatDiskPro.Instance`** y mutex con nombre creado al iniciar la app → Setup detecta y cierra la app de forma fiable antes de actualizar (incluso elevada).

**Soporte de actualización completado (misma fecha)**
- **Auto-actualización silenciosa + relanzado:** `UpdateService.LaunchInstaller(silent)` invoca el instalador con `/VERYSILENT /NORESTART /AUTOUPDATE=1`; `installer.iss` (`[Code] IsAutoUpdate` + `[Run]` con `Check: IsAutoUpdate`, sin `runascurrentuser` para heredar la elevación y evitar un 2.º UAC) relanza la app al terminar. La instalación manual sigue mostrando el asistente (`postinstall skipifsilent`).
- **Firma Authenticode (opcional, parametrizada):** `build-installer.ps1` acepta `-CertThumbprint` / `-CertFile` / `-CertPassword` / `-TimestampUrl`; firma el exe publicado y el instalador con sello de tiempo RFC3161. `Find-SignTool` localiza `signtool.exe` (Windows SDK App Cert Kit, ClickOnce SDK, o `bin\<ver>\<arch>`). `release.ps1` reenvía esos parámetros. Sin certificado, avisa y omite. Script `installer/new-selfsigned-cert.ps1` genera un certificado **autofirmado** de prueba (`-Trust` lo importa a Root/TrustedPublisher del equipo). **Validado end-to-end** con un autofirmado (firma OK + timestamp). ⚠️ Un autofirmado **NO** quita SmartScreen para usuarios finales (cadena no confiable); para distribución real hace falta un cert **OV/EV** de CA reconocida. `.pfx`/`.snk`/`.cer` ya están en `.gitignore`. (`build-installer.ps1` reconvertido a UTF-8 BOM.)
- **Limpieza de temporales:** `UpdateService.DownloadAsync` purga `%Temp%\FormatDiskPro_update` antes de descargar.
- Validado: solución compila 0/0, 59/59 tests, y `installer.iss` compila con ISCC (EXIT 0) incluyendo el PRI.

**Acción pendiente del usuario**: cortar y publicar **1.2.1** con `release.ps1` (idealmente firmado). Los usuarios en 1.1.0 se actualizarán bien (auto-updater → instalador corregido). Quienes ya cayeron en la 1.2.0 rota deben **descargar 1.2.1 manualmente** (su app no abre, así que el auto-updater no corre). **Pendiente: prueba end-to-end real** (la realiza el usuario).

**Recomendaciones restantes (opcionales)**: CI/CD que ejecute `release.ps1`; certificado de firma de código.

### 2026-06-19 — fix/ui: paridad Windows 11 25H2 y tematización de botones de caption

**Botones de título (min/max/cerrar) — bug de tematización**
- Causa raíz: `UpdateTitleBarColors()` derivaba el color de los botones de `UISettings.GetColorValue(UIColorType.Foreground)`, que refleja el **modo de aplicación del sistema**, no el tema **efectivo** de la app. Al forzar Claro/Oscuro desde el menú en contra del sistema, los glifos quedaban con el color equivocado (casi invisibles).
- Solución: se sustituye `AppWindow.TitleBar.ExtendsContentIntoTitleBar` por la propiedad a nivel de **`Window.ExtendsContentIntoTitleBar = true`**; WinUI dibuja y tematiza los botones caption automáticamente siguiendo el `RequestedTheme` del contenido (incluye hover/pressed/inactive y el rojo de cerrar). Se elimina `UpdateTitleBarColors()`.
- `AppWindow.TitleBar.PreferredHeightOption = TitleBarHeightOption.Tall` (48 px, look 25H2); fila `AppTitleBar` a 48 px con icono 16 px (`BitmapImage` desde `FormatDiskPro.ico`) y `CaptionTextBlockStyle`.

**Colores Fluent (en vez de hardcodeados)**
- `ProtectedColor()` → valores de `SystemFillColorCritical` (`#C42B1C` claro / `#FF99A4` oscuro).
- `DriveBrush()` no protegido → `TextFillColorPrimary` (`#E4000000` claro / `#FFFFFF` oscuro).
- `ConfirmDialog`: `PromptText.Foreground` ahora usa `{ThemeResource SystemFillColorCriticalBrush}` en XAML (se resuelve contra el `RequestedTheme` del diálogo → corrige el contraste pobre del rojo fijo en modo oscuro).

**Iconografía y accesibilidad**
- Botón Refrescar: glifo Unicode `↻` → `FontIcon` Segoe Fluent Icons (`&#xE72C;`) + `AutomationProperties.Name` localizado.
- Etiquetas de `Unidad`, `Sistema de archivos`, `Tamaño de unidad de asignación` y `Etiqueta del volumen` migradas de `TextBlock` suelto a la propiedad **`Header`** del `ComboBox`/`TextBox` (patrón Fluent + asociación para lectores de pantalla). `ApplyLanguage` ahora setea `.Header`.

**Robustez**
- Ventana fija no redimensionable (`OverlappedPresenter.IsResizable/IsMaximizable = false`).
- `SetSystemBackdrop()`: Mica si `MicaController.IsSupported()`, si no degrada a `DesktopAcrylicBackdrop`.

**Sin cambios**: `Core/`, `Services/`, `Localization/L.cs`, lógica de negocio. Build 0/0, 59/59 tests ✅.

### 2026-06-19 — feat: barra de título nativa, tema automático y colores de sistema

**Barra de título (title bar)**
- `ExtendsContentIntoTitleBar = true`: el contenido XAML se extiende hasta el borde superior; Mica cubre la ventana de forma uniforme (sin línea de separación sistema/XAML).
- `AppTitleBar` XAML (Grid 32 px, Row 0): área de arrastre registrada con `SetTitleBar()`; el `TextBlock` "FormatDiskPro" hereda el foreground del tema automáticamente.
- `SetIcon()` con `FormatDiskPro.ico` para la barra de tareas y ALT+Tab.
- Eliminada la banda azul del encabezado (`Background="#FF006EB4"` con título + subtítulo de unidad) — la información de unidad ya está disponible en el ComboBox y el panel de info.

**Colores de los botones de caption**
- `UpdateTitleBarColors()` lee `UISettings.GetColorValue(UIColorType.Foreground)` para obtener el color exacto del sistema (sin valores hardcodeados), haciendo que los botones min/max/close sean visualmente idénticos a los nativos de Windows 11.
- `ButtonBackgroundColor = transparent`: el área de fondo de los botones muestra Mica pura.

**Tema del sistema**
- La app sigue el tema de Windows en tiempo real: `UISettings.ColorValuesChanged` → `DispatcherQueue.TryEnqueue` → `ApplyTheme(IsSystemDark())`.
- `IsSystemDark()` lee `UIColorType.Background` (fondo oscuro ↔ R < 128).
- `((FrameworkElement)Content).RequestedTheme = ElementTheme.Default` se establece una vez en el constructor; WinUI 3 y Mica se adaptan solos.
- Menú **Configuración → Tema** restaurado con tres opciones: **Automático** (por defecto, sigue el sistema), **Claro** (fuerza `ElementTheme.Light`) y **Oscuro** (fuerza `ElementTheme.Dark`). Los cambios del sistema solo se aplican si está activo el modo Automático (`_autoTheme`).
- Clave de localización `menu.theme.auto` añadida (ES: "Automático" / EN: "Automatic").

### 2026-06-19 — feat: migración UI de Windows Forms a WinUI 3 (Windows App SDK 1.8, unpackaged)

**Cambios de UI (capa `UI/`)**
- `MainForm.cs` + `MainForm.Designer.cs` → `MainWindow.xaml` + `MainWindow.xaml.cs` (WinUI 3)
- `ConfirmFormatDialog.cs` → `ConfirmDialog.xaml` + `ConfirmDialog.xaml.cs` (ContentDialog)
- Nuevo `DriveViewModel.cs` — modelo de binding con `INotifyPropertyChanged` para el ComboBox de unidades
- Fondo Mica nativo (`MicaBackdrop`), controles Fluent Design 2, `MenuBar` + `ToggleMenuFlyoutItem`
- Tema claro/oscuro via `ElementTheme` (sin hardcoding de colores de sistema)
- `MessageBox` → `ContentDialog` helpers (`ShowInfoAsync`, `ShowConfirmAsync`)
- `System.Windows.Forms.Timer` → `DispatcherTimer`
- Ventana fija 500×840 px centrada en pantalla

**Infraestructura**
- `FormatDiskPro.csproj`: `net10.0-windows` → `net10.0-windows10.0.19041.0`; `UseWindowsForms` → `UseWinUI`; añadido `WindowsPackageType=None`, `WindowsAppSDKSelfContained=true`, `DISABLE_XAML_GENERATED_MAIN`; `PackageReference Microsoft.WindowsAppSDK 1.8.*`
- `Program.cs`: entry point WinUI 3 con `DispatcherQueueSynchronizationContext`
- `App.xaml` + `App.xaml.cs`: nuevo punto de entrada de aplicación XAML
- `app.manifest`: añadida declaración DPI `PerMonitorV2`
- `Localization.cs`: corregida referencia "Windows Forms" → "WinUI 3" en `about.body`
- `FormatDiskPro.Tests.csproj`: TFM actualizado a `net10.0-windows10.0.19041.0` (compatible con main project)

**Sin cambios**: `Core/`, `Services/`, `Localization/L.cs`, `installer/`, `release.ps1`, 59 tests ✅

### 2026-06-18 — docs: skills del proyecto en CLAUDE.md
- Añadida a `.claude/CLAUDE.md` la tabla de uso de las skills de `.agents/skills/` (cuándo usar cada una).
- Fijado **xUnit** como framework de pruebas (`csharp-xunit`); mstest/nunit/tunit presentes pero sin uso.

### 2026-06-18 — v1.1.0: arquitectura por capas, hardening, tests, actualizaciones e instalador

**Seguridad / lógica**
- Solo el disco de sistema queda protegido (antes: todos los discos fijos).
- Diálogo de **confirmación reforzada** al pulsar Iniciar (escribir la letra de la unidad).
- Verificación de capacidad **bloqueada** en disco protegido/sistema (L1).
- Detección de disco de sistema centralizada en `IsSystemDrive()` (L2).
- `format.com` usa `ArgumentList` (escape por argumento) (S1).
- Validación de longitud de etiqueta por FS — `FormatLogic.MaxLabelLength` (L5).
- Buffer de arrastre para no perder el `%` partido entre lecturas de `format.com` (L3).
- Eliminado handler muerto `chkQuickFormat_CheckedChanged` (L4).

**Rendimiento**
- `ExtractPercent` → `[GeneratedRegex]`.
- `CapacityVerifier`: bloques de 8 MB agrupados en archivos de 1 GB (de ~7.500 a ~60 archivos en 60 GB).

**Arquitectura / calidad**
- Lógica pura extraída a `Core/FormatLogic.cs`.
- Reestructura a `src/FormatDiskPro/` con capas `Core` / `Services` / `UI` / `Localization`.
- `Form1` → `MainForm` (clase, archivos, Designer, `Program.cs`).
- Proyecto de pruebas **xUnit** (`tests/FormatDiskPro.Tests`): FormatLogic, Presets, Localization, UpdateChecker.

**Actualizaciones (GitHub Releases)**
- `Core/AppInfo.cs` (versión + repo), `Core/UpdateChecker.cs` (comparación de versiones, testeada).
- `Services/UpdateService.cs` (API latest, descarga con progreso, lanza instalador).
- UI: menú **Ayuda → Buscar actualizaciones**, chequeo automático al inicio, "Acerca de" con versión real.

**Instalador / publicación**
- `installer/installer.iss` (Inno Setup, self-contained, admin, AppId fijo).
- `installer/build-installer.ps1` (publish self-contained + ISCC → `Output/`).
- `release.ps1` (corte de versión en un paso).
- `.gitignore`: ignora `**/installer/Output/` y `**/publish/`.
- README actualizado (instalación, actualizaciones, build del instalador, arquitectura, licencia, badges).
- Publicado: commits en `master`, tag `v1.1.0` y GitHub Release con el instalador.
