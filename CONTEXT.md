# Contexto del proyecto — FormatDiskPro

> **Propósito de este archivo.** Documento de contexto **vivo** que resume el estado del
> proyecto y las decisiones tomadas, para no perder continuidad al cambiar de equipo (PC)
> o de sesión. **Mantenerlo actualizado con cada cambio relevante**: actualizar
> _Estado actual_ y añadir una entrada en el _Registro de cambios_. Usar fechas absolutas.

- **Repositorio:** https://github.com/xfiberex/FormatDiskPro
- **Última actualización de este documento:** 2026-06-27
- **Versión actual:** **1.12.0** (en preparación — **Tier 5: confianza, legal y sostenibilidad**: relicenciado a
  **GNU GPL v3.0** (antes MIT) con licencia visible in-app, **disclaimer** de uso destructivo/sin garantía, **avisos de
  terceros**, **aviso de privacidad** y **donaciones opcionales (PayPal)** — pendiente la **URL de PayPal** del usuario
  antes de publicar). La **1.11.0** completó el **Tier 4**: **#16 umbrales de color + estado + botón Actualizar
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
- **Hoja de ruta:** ver [`ROADMAP.md`](ROADMAP.md) (Tier 2 y **Tier 3 completados** — **#13 winget/firma descartado**). **Tier 4 COMPLETADO** (#14–#22; v1.10.0 + v1.11.0). **Tier 5 — Confianza, legal y sostenibilidad (#23–#27) en v1.12.0:** relicenciado a GPLv3 + apartados legales in-app + donaciones PayPal. Tras Tier 5, solo queda lo deliberadamente fuera de alcance.
- **Stack:** C# 13 · .NET 10 · **WinUI 3** (Windows App SDK 1.8, unpackaged, `net10.0-windows10.0.19041.0`) · xUnit · Inno Setup 6

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
│  ├─ UpdateService.cs     GitHub Releases API: consulta, descarga con progreso, lanza instalador
│  └─ History.cs           Auditoría en %AppData%\FormatDiskPro\history.log
├─ UI/              WinUI 3 (Windows App SDK)
│  ├─ MainWindow.xaml / MainWindow.xaml.cs  Ventana principal + orquestación
│  ├─ ConfirmDialog.xaml / .xaml.cs         ContentDialog — confirmación reforzada (escribir la letra)
│  └─ DriveViewModel.cs                     Binding model para el ComboBox de unidades
├─ Localization/    L.cs — diccionario ES/EN, L.T("clave")
├─ installer/       installer.iss (Inno Setup) + build-installer.ps1 → Output/ (gitignored)
└─ Program.cs       Punto de entrada

tests/FormatDiskPro.Tests/   Pruebas xUnit sobre la lógica de Core (165 tests)
release.ps1                  Corte de versión en un paso (build + tag + GitHub Release)
FormatDiskPro.slnx           Solución (app + tests)
```

**Regla de oro:** la lógica de negocio testeable vive en `Core` (sin dependencias de
WinUI/Process/HttpClient). La UI y los servicios la consumen. Namespace único `FormatDiskPro`.

## 3. Estado actual

- ✅ Build de solución: **0 advertencias / 0 errores** (WinUI 3, WAS 1.8).
- ✅ Pruebas: **226/226** (`dotnet test`) — 224 previas + 2 del Tier 5 (`LegalText.License`/`ThirdParty`).
- ✅ **Tier 5 — confianza/legal/sostenibilidad (v1.12.0, pendiente de publicar):**
  Relicenciado **MIT → GNU GPL v3.0**: `LICENSE` con el texto oficial (descargado de gnu.org), embebido vía `.csproj`
  como recurso (`FormatDiskPro.LICENSE.txt`) + `THIRD-PARTY-NOTICES.txt`. `Core/LegalText` lee ambos recursos.
  `UI/AboutDialog` (Acerca de ampliado: descripción, versión, copyright/licencia, **disclaimer** de uso destructivo/sin
  garantía, **privacidad**, botones *GitHub* y *Apoyar el proyecto*); `UI/LegalTextDialog` (visor con scroll para
  *Ayuda → Licencia* y *Ayuda → Avisos de terceros*). `AppInfo.DonateUrl` (PayPal) + `RepoUrl`; donación **opcional**,
  sin bloquear nada. **Pendiente:** sustituir `AppInfo.DonateUrl` por la URL real de PayPal del usuario y añadir
  `.github/FUNDING.yml` antes de publicar v1.12.0.
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
- **Instalador (Inno Setup):** `AppId = {CEC07916-C9B5-4EA8-9102-3273384395AD}` — **no cambiar
  nunca** (permite actualización in-place). `PrivilegesRequired=admin`, `CloseApplications=yes`.
- **Versionado:** fuente única en `src/FormatDiskPro/FormatDiskPro.csproj` `<Version>`.
  El updater compara esa versión contra el `tag_name` del último release (`UpdateChecker.IsNewer`).
- **Actualizaciones:** chequeo silencioso al iniciar (`OnShown`) + manual (Ayuda → Buscar
  actualizaciones). Si hay versión mayor, descarga el asset `*.exe` (preferente "setup") y lo lanza.
- **Scripts PowerShell** que se ejecuten en Windows PowerShell 5.1 deben guardarse con **BOM UTF-8**
  (si no, los acentos rompen el parser). `release.ps1` ya lo tiene.
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
| Generar instalador | `src\FormatDiskPro\installer\build-installer.ps1` |
| **Publicar versión** | `.\release.ps1 -Version X.Y.Z` (usa `-DryRun` para simular) |

`release.ps1` hace: validar → tests → bump `<Version>` → build instalador → commit + tag `vX.Y.Z`
→ push → `gh release create` con el instalador. Flags: `-DryRun`, `-SkipTests`, `-AllowDirty`, `-NotesFile`.

## 6. Pendientes / ideas

- **Hoja de ruta de características:** [`ROADMAP.md`](ROADMAP.md) — **Tier 2 y Tier 3 completados**
  (Tier 3: presets, idiomas, avisos; **#13 winget/firma descartado el 2026-06-24**). **Tier 4 — Refinado
  de características existentes (#14–#22), propuesto:** pasadas de wipe configurables, IOPS en benchmark,
  umbrales S.M.A.R.T., refresco automático de unidades, idioma automático, filtro/CSV en historial,
  editar/reordenar presets, changelog en el aviso de actualización y pulido de accesibilidad. Es el backlog
  activo (no compromete versión/fecha). Aparte, solo queda lo deliberadamente fuera de alcance.
- Probar el instalador end-to-end (instalación + actualización in-place).
- (Opcional) Workflow de GitHub Actions que ejecute `release.ps1` o equivalente.
- (Opcional) Renombrar el `Name` interno del form / pulir cadenas.
- S2 (menor, por diseño): la validación de etiqueta no rechaza `'`, pero queda cubierto por el escape.

## 7. Cómo mantener este documento

1. Tras un cambio relevante, añadir una entrada en el **Registro de cambios** (fecha absoluta).
2. Actualizar **Estado actual** (versión, tests, lo publicado, pendientes).
3. Si cambia una convención o decisión, reflejarlo en la sección 4.
4. Commitear este archivo junto con el cambio para que viaje entre equipos.

---

## Registro de cambios

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
