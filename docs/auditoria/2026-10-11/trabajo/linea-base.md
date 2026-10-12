# Línea base · FormatDiskPro

- Fecha: 2026-10-11 10:53 (hora local)
- Directorio: /c/Users/User/Desktop/Desarrollo y Proyectos/Cursos y Proyectos/15 - Proyectos Profesionales/FormatDiskPro

## Git
- Commit auditado: 72f29022bac5fc602f425de1e231045ce9994593
- Rama: master
- Árbol de trabajo: 1 archivo(s) con cambios sin commit (la auditoría los incluye)
- Último commit: 2026-09-22 · release: v1.29.0
- Commits totales: 155
- Autores distintos (12 meses): 2
- Remoto origin: https://github.com/xfiberex/FormatDiskPro.git
- Últimos tags: v1.29.0 v1.28.1 v1.28.0 v1.27.0 v1.26.0 

## Stack detectado (por archivos presentes)
- .NET
- XAML (WinUI, WPF o MAUI)
- Manifiesto de aplicación Windows (app.manifest)
- CI: GitHub Actions
- Servidores MCP del proyecto (.mcp.json)

## Documentos del proyecto
- README.md (202 líneas)
- CONTEXT.md (3387 líneas)
- ROADMAP.md (3680 líneas)
- CHANGELOG.md (844 líneas)
- LICENSE (674 líneas)
- .claude/settings.json (14 líneas)
- Auditorías previas en docs/auditoria/:
  - 2026-10-11
- ROADMAP.md usa IDs con el esquema antiguo T{tier}-{n}

## Tamaño
- Archivos versionados: 179
- Por extensión (las 15 más frecuentes):
  - .cs: 109
  - .md: 23
  - .png: 12
  - .xaml: 10
  - .yml: 7
  - .ps1: 4
  - .json: 3
  - .csproj: 3
  - .gitignore: 2
  - .txt: 1
  - .slnx: 1
  - .manifest: 1
  - .iss: 1
  - .ico: 1
- Líneas en archivos de código: 21130
- Archivos de código más grandes:
  - src/FormatDiskPro/UI/MainWindow.xaml.cs (líneas: 986)
  - release.ps1 (líneas: 866)
  - tools/capture-screenshots.ps1 (líneas: 661)
  - src/FormatDiskPro/UI/MainWindow.Operations.cs (líneas: 624)
  - src/FormatDiskPro/UI/MainWindow.FormatOptions.cs (líneas: 585)
  - tests/FormatDiskPro.Tests/ServiceErrorPathTests.cs (líneas: 573)
  - src/FormatDiskPro/Services/UpdateService.cs (líneas: 550)
  - src/FormatDiskPro/Localization/Localization.cs (líneas: 538)
  - tests/FormatDiskPro.Tests/UpdateServiceTests.cs (líneas: 488)
  - src/FormatDiskPro/UI/MainWindow.DriveInfo.cs (líneas: 480)

## Hotspots (archivos más modificados en los últimos 12 meses)
- CONTEXT.md (cambios: 110)
- ROADMAP.md (cambios: 74)
- src/FormatDiskPro/UI/MainWindow.xaml.cs (cambios: 46)
- src/FormatDiskPro/FormatDiskPro.csproj (cambios: 44)
- src/FormatDiskPro/Localization/Localization.cs (cambios: 40)
- CHANGELOG.md (cambios: 39)
- README.md (cambios: 38)
- src/FormatDiskPro/UI/MainWindow.xaml (cambios: 33)
- release.ps1 (cambios: 17)
- src/FormatDiskPro/UI/MainWindow.Preferences.cs (cambios: 16)
- src/FormatDiskPro/UI/Theme/AppTheme.xaml (cambios: 13)
- src/FormatDiskPro/UI/MainWindow.DriveInfo.cs (cambios: 13)
- tests/FormatDiskPro.Tests/LocalizationTests.cs (cambios: 12)
- src/FormatDiskPro/UI/HistoryDialog.xaml.cs (cambios: 12)
- src/FormatDiskPro/Services/AppSettings.cs (cambios: 12)

## Pruebas
- Proyectos de prueba .NET:
  - tests/FormatDiskPro.Tests/FormatDiskPro.Tests.csproj
  - tests/FormatDiskPro.UiTests/FormatDiskPro.UiTests.csproj
- Archivos que parecen de prueba: 57

## Workflows de CI
- .github/workflows/ci.yml
- .github/workflows/codeql.yml

## Herramientas disponibles en esta máquina
- git: git version 2.55.0.windows.5
- node: v24.20.0
- npm: 11.12.0
- pnpm: 11.20.0
- dotnet: 10.0.401
- python: no se encontr� Python; ejecutar sin argumentos para instalar desde el Microsoft Store o deshabilitar este acceso directo desde Configuraci�n > Aplicaciones > Configuraci�n avanzada de aplicaciones > Alias de ejecuci�n de aplicaciones.
- java: java version "1.8.0_503"
- docker: Docker version 29.8.2, build 7fc2dff
- gh: gh version 2.102.0 (2026-09-30)

## Resultados de la línea base (2026-10-11, terminal elevada, PowerShell 5.1)

- **Build:** `dotnet build -c Release -warnaserror` → 0 advertencias, 0 errores, 21,9 s. (Compila la solución: app + Tests. UiTests está fuera de la solución.)
- **Unitarias:** `dotnet test -c Release --no-build --collect:"XPlat Code Coverage"` → **690 total · 689 pasan · 0 fallan · 1 omitida**, 1 s de ejecución (8,7 s totales).
  - Omitida: `CapacityVerifierDriveTests.RunInAsync_OnRealDrive_VerifiesWithoutTheSystemCache`.
  - `CONTEXT.md` §3 dice 626 unitarias: desactualizado (candidato área 11).
- **Cobertura (coverlet, todo el ensamblado):** líneas 32,67 % · ramas 34,05 %. El corte mide solo `Core/` (ver `release.ps1`).
- **SCA:** `dotnet list package --vulnerable --include-transitive` → sin paquetes vulnerables en app, Tests ni UiTests (origen nuget.org).
- **Desactualizados:** app al día (`Microsoft.WindowsAppSDK 2.5.1`). Tests: coverlet.collector 6.0.2→10.1.0, Microsoft.NET.Test.Sdk 17.11.1→18.10.1, xunit 2.9.2→2.9.3, xunit.runner.visualstudio 2.8.2→4.0.1. UiTests: Test.Sdk 17.14.1→18.10.1, runner 3.1.4→4.0.1. Versiones distintas del mismo paquete entre los dos proyectos de prueba.
- **Entorno:** sin `pwsh` 7, sin gitleaks/semgrep/osv-scanner; Inno Setup 6 presente; `gh` autenticado; Docker apagado; monitor único 1920×1080.
- **Hardware:** USB de pruebas = disco físico 6 (G: FAT32 1 GB `UTILIDADES` + I: exFAT 28,3 GB), vacía. Variables destructivas sin activar.
