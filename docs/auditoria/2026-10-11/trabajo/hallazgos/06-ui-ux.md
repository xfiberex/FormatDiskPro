# Área 06 · Diseño responsivo y UI/UX · hallazgos provisionales

> **PRIMERA VERSIÓN (punto de control).** Contiene los hallazgos ya sostenidos por lectura de código.
> Falta incorporar la revisión de las capturas de hoy (`evidencias/galeria-*`) y el resto de secciones.
> Se sustituye por la versión completa al terminar.

Revisado (entero): `src/FormatDiskPro/UI/**`, `App.xaml(.cs)`, `Localization/Localization.cs`,
`Core/AppInfo.cs`, `Core/Presets.cs`, `app.manifest`. ROADMAP 275–332, 1400–1900, 2345–2760, 3018–3070,
3257–3410, 3636–3680 y CONTEXT §4.

## Hallazgos

### P-06-01 · Durante una operación, los tres botones de la barra rápida siguen activos y no hacen nada
**Área:** UI/UX · **Severidad:** Bajo · **Confianza:** Alta · **Verificación:** estática · **Esfuerzo:** Bajo
**Ubicación:** `src/FormatDiskPro/UI/MainWindow.xaml.cs:942-968` (`SetControlsEnabled`) · `MainWindow.DriveInfo.cs:276-277` · `MainWindow.Operations.cs:136`, `:525` · `MainWindow.HelpAndUpdates.cs:34`
**Estado:** nuevo (consecuencia no vista de `T11-02`)
**Problema:** `SetControlsEnabled(false)` apaga los tres menús, pero no toca `BtnHealth`, `BtnBenchmark` ni `BtnHistory`; sus manejadores salen con `if (_isBusy) return;` sin decir nada.
**(detalle en la versión completa)**

### P-06-02 · Guardar un preset sin nombre responde «Ya existe un preset con ese nombre»
**Área:** UI/UX · **Severidad:** Bajo · **Confianza:** Alta · **Verificación:** estática · **Esfuerzo:** Bajo
**Ubicación:** `src/FormatDiskPro/UI/PresetsDialog.xaml.cs:89-93`, `:199-203` · `src/FormatDiskPro/Core/Presets.cs` (`IsNameAvailable`)
**Estado:** nuevo
**(detalle en la versión completa)**

## Pendiente de completar
Resto de hallazgos, hipótesis, comprobaciones en ejecución, puntos fuertes y notas para otras áreas.
