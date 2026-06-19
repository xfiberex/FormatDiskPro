# Contexto del proyecto — FormatDiskPro

> **Propósito de este archivo.** Documento de contexto **vivo** que resume el estado del
> proyecto y las decisiones tomadas, para no perder continuidad al cambiar de equipo (PC)
> o de sesión. **Mantenerlo actualizado con cada cambio relevante**: actualizar
> _Estado actual_ y añadir una entrada en el _Registro de cambios_. Usar fechas absolutas.

- **Repositorio:** https://github.com/xfiberex/FormatDiskPro
- **Última actualización de este documento:** 2026-06-19
- **Versión actual:** 1.1.0 (publicada como release con instalador)
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
│  └─ Presets.cs         Configuraciones de formato predefinidas
├─ Services/        Efectos colaterales (procesos / disco / red)
│  ├─ DiskService.cs       S.M.A.R.T., expulsión, borrado seguro (PowerShell -EncodedCommand)
│  ├─ CapacityVerifier.cs  Verificación de capacidad real (patrón anti-aliasing por bloque)
│  ├─ UpdateService.cs     GitHub Releases API: consulta, descarga con progreso, lanza instalador
│  └─ History.cs           Auditoría en %AppData%\FormatDiskPro\history.log
├─ UI/              WinUI 3 (Windows App SDK)
│  ├─ MainWindow.xaml / MainWindow.xaml.cs  Ventana principal + orquestación
│  ├─ ConfirmDialog.xaml / .xaml.cs         ContentDialog — confirmación reforzada (escribir la letra)
│  └─ DriveViewModel.cs                     Binding model para el ComboBox de unidades
├─ Localization/    L.cs — diccionario ES/EN, L.T("clave")
├─ installer/       installer.iss (Inno Setup) + build-installer.ps1 → Output/ (gitignored)
└─ Program.cs       Punto de entrada

tests/FormatDiskPro.Tests/   Pruebas xUnit sobre la lógica de Core (59 tests)
release.ps1                  Corte de versión en un paso (build + tag + GitHub Release)
FormatDiskPro.slnx           Solución (app + tests)
```

**Regla de oro:** la lógica de negocio testeable vive en `Core` (sin dependencias de
WinUI/Process/HttpClient). La UI y los servicios la consumen. Namespace único `FormatDiskPro`.

## 3. Estado actual

- ✅ Build de solución: **0 advertencias / 0 errores** (WinUI 3, WAS 1.8).
- ✅ Pruebas: **59/59** (`dotnet test`).
- ✅ Release **v1.1.0** publicado en GitHub con `FormatDiskPro-1.1.0-setup.exe` adjunto.
- ✅ Barra de título: **`Window.ExtendsContentIntoTitleBar = true`** (a nivel de Window) + `PreferredHeightOption = Tall` (48 px) + icono 16 px + `CaptionTextBlockStyle`. WinUI tematiza solo los botones caption (min/max/cerrar) según el tema **efectivo** del contenido.
- ✅ Tema: sigue el tema del sistema automáticamente (`UISettings.ColorValuesChanged`); opción manual Automático/Claro/Oscuro en menú. Colores derivados de recursos/valores Fluent (`SystemFillColorCritical`, `TextFillColorPrimary`).
- ✅ Ventana fija no redimensionable (`OverlappedPresenter.IsResizable/IsMaximizable = false`); Mica con degradación a Acrylic si el sistema no la soporta.
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
