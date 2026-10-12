# Área 11 · Documentación · hallazgos provisionales

> **VERSIÓN PARCIAL (punto de control 1).** Lo que sigue está confirmado en estática. Faltan por cerrar:
> `CHANGELOG.md` frente a Keep a Changelog y a los tags, `Get-Help` de los cuatro scripts, enlaces y anclas,
> `docs/release-notes-*.md`, comentarios XML y las mediciones de tamaño. Se sustituirá por la versión completa.

Revisado hasta ahora: `README.md`, `.github/CONTRIBUTING.md`, `.github/SECURITY.md`,
`.github/PULL_REQUEST_TEMPLATE.md`, las plantillas de issue, `CONTEXT.md` §1–§7 (líneas 1–393) y su índice de
versiones, cabecera, estado, índice y «Áreas auditadas sin hallazgos» del `ROADMAP.md`, `FormatDiskPro.csproj`,
`installer.iss`, los tres YAML de `.github/`.

## Hallazgos (provisional, sin ordenar todavía)

### P-11-01 · El requisito de sistema documentado (Windows 10 1809+) contradice al instalador, que exige la 2004
**Área:** Documentación · **Severidad:** Medio · **Confianza:** Alta · **Verificación:** estática · **Esfuerzo:** Bajo
**Ubicación:** `README.md:94`, `.github/CONTRIBUTING.md:13` frente a `src/FormatDiskPro/installer/installer.iss:67` (`MinVersion=10.0.19041`) y `FormatDiskPro.csproj:5` (`net10.0-windows10.0.19041.0`)
**Estado:** por clasificar

### P-11-02 · `CONTEXT.md` §3 vuelve a estar desfasado y se contradice consigo mismo
**Área:** Documentación · **Severidad:** Medio · **Confianza:** Alta · **Verificación:** estática · **Esfuerzo:** Bajo
**Ubicación:** `CONTEXT.md:158`, `:162`, `:164`, `:165`
**Estado:** regresión de `T9-14`
**Problema:** «626 / 626 (625 pasan · 1 se omite)» cuando la línea base de hoy da 690 (689 + 1 omitida) y el propio desglose suma 622; «Tiers abiertos: **Ninguno**» junto a «Tareas abiertas: **1** … Tier 10, abierto»; la frase «`T10-02`, bloqueada a propósito» escrita dos veces seguidas; «las tres tareas» seguido de cuatro; «39/40 completadas + 2 descartadas» (41) con `T2-10` (CI) como descartada cuando el CI existe.

### P-11-03 · El árbol de `CONTEXT.md` §2 omite tres archivos y afirma que ninguna parcial pasa de 800 líneas
**Ubicación:** `CONTEXT.md:44-63`, `:80-92`
**Estado:** regresión de `T9-15`
**Problema:** faltan `Core/PlanRejection.cs`, `UI/HighContrast.cs`, `UI/SaveFileDialog.cs`; `MainWindow.xaml.cs` mide 986 líneas.

### P-11-04 · El README y el instalador siguen citando Windows App SDK 1.8; el proyecto usa 2.5.1
**Ubicación:** `README.md:190`; `installer.iss:11`, `:25`, `:66`; frente a `FormatDiskPro.csproj:43`

### P-11-05 · Plantillas de GitHub desfasadas: «CI» figura como fuera de alcance y el botón sigue llamándose «Iniciar»
**Ubicación:** `.github/ISSUE_TEMPLATE/feature_request.yml:10`, `bug_report.yml:69`, `ROADMAP.md:3678-3680`

### P-11-06 · El índice del `ROADMAP.md` no incluye el Tier 13 y suma 108 tareas de 128; los tiers 11–13 están en orden inverso
**Ubicación:** `ROADMAP.md:350-369`, `:2345`, `:3070`, `:3292`; `:96-100`

### P-11-07 · `CONTEXT.md` y `ROADMAP.md` no cumplen la compacidad ni la estructura de `entregables.md`
(mediciones pendientes de escribir)

### P-11-08 · El README promete velocidad y ETA «en toda operación larga»; solo las dan dos
**Ubicación:** `README.md:80` frente a `MainWindow.xaml.cs:901`

## Hipótesis por verificar
(por completar)

## Puntos fuertes
(por completar)

## Para otras áreas
(por completar)
