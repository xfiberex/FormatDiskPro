---
target: proyecto completo (WinUI 3, escritorio)
total_score: 30
max_score: 40
na_heuristics: 
p0_count: 0
p1_count: 2
target_identity: "file:C:\\Users\\User\\Desktop\\Cursos y Proyectos\\15 - Proyectos Profesionales\\FormatDiskPro\\src\\FormatDiskPro\\UI\\MainWindow.xaml"
target_fingerprint: "sha256:a13ba57cc402b0645f57afcf1e554acb8e95164f20e1f8ae994be1b891aab620"
target_path: "C:\\Users\\User\\Desktop\\Cursos y Proyectos\\15 - Proyectos Profesionales\\FormatDiskPro\\src\\FormatDiskPro\\UI\\MainWindow.xaml"
timestamp: 2026-11-11T02-08-33Z
slug: src-formatdiskpro-ui-mainwindow-xaml
---
Method: design review only (detector not applicable, native platform: WinUI 3 / XAML)

Target: whole app (MainWindow + 7 dialogs), FormatDiskPro 1.30.4. Evidence: 28-shot gallery (light/dark) + 4 shots at 656 DIP via tools/capture-screenshots.ps1, plus XAML/AppTheme/copy review.

## Heuristics (30/40, Good)
1 Visibility 3 (unknown values render as "● –") · 2 Real world 3 (history shows raw English log lines) · 3 Control 3 · 4 Consistency 3 (two confirm dialogs differ; InfoBar vs red text; default button varies) · 5 Error prevention 4 · 6 Recognition 3 · 7 Flexibility 3 · 8 Minimalist 3 (two nav rows, health shown 3x) · 9 Error recovery 3 · 10 Help 2 (Ayuda menu has no help)

## Specificity
Operate surface; Win11-Settings language is right. Identity lives in drive-header hierarchy, footer summary next to "Formatear D:", verb+object button. Missed: the confirm dialog (peak moment) is a settings table.

## Priority issues
- [P1] Format confirm omits the stakes: shows FS/cluster/label/type the user just chose, not disk type/model/used data (D: SATA SSD, 200,2 GB used). Reinit confirm does show model+size → inconsistent. ADVERTENCIA caps plain text; red instruction reads as error; placeholder "…". Fix: lead with target + data at risk, InfoBar Error, settings as one muted line, same skeleton for both confirms. → clarify, layout
- [P1] History detail = raw log ("UPDATE DOWNLOADED v1.30.2: C:\ProgramData\…"), English caps in Spanish UI; Humanize only formats bytes by design. Fix: localized per-event sentence in UI (raw stays in file/CSV), weight formats/wipes/failures over internal noise, separate "Vaciar historial". → clarify
- [P2] Top chrome: menu + quick bar ~80 DIP; at 656 DIP label + options below fold; health in menu, quick bar and non-clickable "● Normal". Fix: make health pill open S.M.A.R.T.; Benchmark/Historial as compact buttons in drive-card header; drop the row. → layout
- [P2] Unknown values as dashes: USB G: "● –" and "· –". Fix: "Salud no disponible" + tooltip; drop empty meta segment. → clarify
- [P2] Internal language in user copy: WhatsNew "(AUD-067)", "auditoría del 2026-10-11", wall of text; About ".NET 10 · WinUI 3". Fix: "Qué cambia para ti" 3 bullets, details on GitHub. → clarify

## Personas
Alex: no shortcut to drive picker; "Mi preset" prefilled; quick bar redundant with shortcuts. Jordan: "● –" on USB; no explanation that unchecking quick format takes hours; secure-wipe explanation tooltip-only; no help in Ayuda; raw history. Sam: strong; secure-wipe explanation tooltip-only → needs visible hint.

## Cognitive load: 1/8 fail (minimal choices: 6 nav entry points, 2 duplicated). Low.
## Emotional journey: peak = confirm (stops, doesn't reassure); end = completion notify (good); valley = history after failure.

## Minor
Default button inconsistent across dialogs (About: donate accent; WhatsNew: Close accent; Health/Presets: none). Amber 90% bar vs orange-accent primary collide. "Tipo: rápido" → "Formato: rápido". 10px scroll chevron barely visible. Same ⓘ on every history row; floating "48 de 48". "SIN NINGUNA GARANTÍA" caps. Footer "Cerrar" duplicates window X.

## Questions
Confirm shows the disk like the drive card? History: maintainer log or user diary? Does the quick bar need its own row?
