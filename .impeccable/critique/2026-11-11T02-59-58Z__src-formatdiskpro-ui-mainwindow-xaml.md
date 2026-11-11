---
target: proyecto completo (WinUI 3, escritorio), comprobación tras AUD-072–076
total_score: 31
max_score: 40
na_heuristics: 
p0_count: 0
p1_count: 0
target_identity: "file:C:\\Users\\User\\Desktop\\Cursos y Proyectos\\15 - Proyectos Profesionales\\FormatDiskPro\\src\\FormatDiskPro\\UI\\MainWindow.xaml"
target_fingerprint: "sha256:8110aeed18ef2e2b7ab6c82f97abb80dd6ba59a27b5aa56307c64b6eee4b4e59"
target_path: "C:\\Users\\User\\Desktop\\Cursos y Proyectos\\15 - Proyectos Profesionales\\FormatDiskPro\\src\\FormatDiskPro\\UI\\MainWindow.xaml"
timestamp: 2026-11-11T02-59-58Z
slug: src-formatdiskpro-ui-mainwindow-xaml
---
Method: design review only (detector not applicable, native platform: WinUI 3 / XAML)

Check run after AUD-072–AUD-076. Evidence: fresh 28-shot gallery (light/dark) + main/confirm/history at 656 DIP.

## Heuristics (31/40, Good; was 30)
1 Visibility 3 · 2 Real world 3 (history titles translated; technical detail line still raw by design) · 3 Control 3 · 4 Consistency 4 (one confirm skeleton, accent rule written, errors in InfoBars) · 5 Error prevention 4 · 6 Recognition 3 · 7 Flexibility 3 · 8 Minimalist 3 (row removed, health once; footer "Cerrar" still duplicates the X) · 9 Error recovery 3 · 10 Help 2 (Ayuda has no help)

## Priority issues
- [P2] No in-app help: Ayuda = updates, what's new, license, about. A first-timer has hints and tooltips but no task guide. Fix: "Ayuda → Guía rápida" (short in-app page or the README section). → onboard
- [P2] At 656 DIP the volume label is still below the fold (~90 DIP short); footer summary mitigates. Known, accepted in AUD-074. → layout (only if a hint can go without losing a11y)

## No regressions found
Confirm fits at 656 DIP; health pill reads as data with a chevron; NA wording on USB; history readable in both themes; informative dialogs without accent.

## Minor
History default view leads with internal update rows (de-weighted but first). Capacity amber vs orange/yellow accent. Footer "Cerrar".
