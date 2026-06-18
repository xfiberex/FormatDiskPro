# Contexto del proyecto — FormatDiskPro

> **Propósito de este archivo.** Documento de contexto **vivo** que resume el estado del
> proyecto y las decisiones tomadas, para no perder continuidad al cambiar de equipo (PC)
> o de sesión. **Mantenerlo actualizado con cada cambio relevante**: actualizar
> _Estado actual_ y añadir una entrada en el _Registro de cambios_. Usar fechas absolutas.

- **Repositorio:** https://github.com/xfiberex/FormatDiskPro
- **Última actualización de este documento:** 2026-06-18
- **Versión actual:** 1.1.0 (publicada como release con instalador)
- **Stack:** C# 13 · .NET 10 · Windows Forms (`net10.0-windows`) · xUnit · Inno Setup 6

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
├─ UI/              Windows Forms
│  ├─ MainForm.cs / MainForm.Designer.cs   Ventana principal + orquestación
│  └─ ConfirmFormatDialog.cs               Confirmación reforzada (escribir la letra)
├─ Localization/    L.cs — diccionario ES/EN, L.T("clave")
├─ installer/       installer.iss (Inno Setup) + build-installer.ps1 → Output/ (gitignored)
└─ Program.cs       Punto de entrada

tests/FormatDiskPro.Tests/   Pruebas xUnit sobre la lógica de Core (59 tests)
release.ps1                  Corte de versión en un paso (build + tag + GitHub Release)
FormatDiskPro.slnx           Solución (app + tests)
```

**Regla de oro:** la lógica de negocio testeable vive en `Core` (sin dependencias de
WinForms/Process/HttpClient). La UI y los servicios la consumen. Namespace único `FormatDiskPro`.

## 3. Estado actual

- ✅ Build de solución: **0 advertencias / 0 errores**.
- ✅ Pruebas: **59/59** (`dotnet test`).
- ✅ Release **v1.1.0** publicado en GitHub con `FormatDiskPro-1.1.0-setup.exe` adjunto.
- ✅ Formateo real verificado contra USB físico (NTFS, requiere elevación).
- ⏳ README actualizado en local; **pendiente de confirmar commit/push** al momento de escribir.
- ⏳ Instalador **no** probado end-to-end (instalación real); queda como verificación manual.

## 4. Decisiones y convenciones clave

- **Protección de unidades:** SOLO se protege el **disco de sistema** (donde está Windows),
  detectado con `IsSystemDrive()` (`Path.GetPathRoot(Environment.SystemDirectory)[0]`).
  El resto (removibles, discos de datos fijos, RAM) **sí** se pueden formatear.
- **Seguridad PowerShell:** comandos vía `-EncodedCommand` (Base64 UTF-16LE). Validar
  `char.IsLetter(letter)` antes de interpolar. Etiqueta escapada (`'`→`''`) en `Format-Volume`;
  para `format.com` se usa `ArgumentList` (escape por argumento).
- **Verificación de capacidad:** bloques de 8 MB (unidad del patrón anti-aliasing) agrupados
  en archivos de 1 GB (seguro en FAT32, pocos archivos).
- **Publicación:** `dotnet publish` **self-contained** `win-x64` → el usuario final NO necesita
  instalar .NET.
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
