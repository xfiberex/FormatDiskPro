# Área 05 · Accesibilidad y semántica · hallazgos provisionales

> **PRIMERA VERSIÓN (punto de control).** Contiene los hallazgos ya sostenidos por lectura de código.
> Falta incorporar la revisión de las capturas de hoy (`evidencias/galeria-*`) y el resto de secciones.
> Se sustituye por la versión completa al terminar.

Revisado (entero): `src/FormatDiskPro/UI/**` (10 XAML + todo el code-behind), `App.xaml(.cs)`,
`Core/SeverityPalette.cs`, `Core/FluentTextPalette.cs`, `Localization/Localization.cs`, `app.manifest`,
`TextContrastTests.cs`, `SeverityPaletteTests.cs`, `DialogLayoutTests.cs`, `AccessibilityTests.cs`,
`KeyboardAndNamingTests.cs`, `MainWindowActions.cs`. ROADMAP 275–332, 1400–1760, 2345–2760, 3018–3070,
3257–3410, 3636–3680 y CONTEXT §4.

## Hallazgos

### P-05-01 · El selector de unidad no tiene nombre accesible y anuncia cada unidad como «FormatDiskPro.UI.DriveViewModel»
**Área:** Accesibilidad · **Severidad:** Alto · **Confianza:** Alta · **Verificación:** estática (y observación en ejecución ya escrita en las pruebas del proyecto) · **Esfuerzo:** Bajo
**Ubicación:** `src/FormatDiskPro/UI/MainWindow.xaml:167-179` · `src/FormatDiskPro/UI/DriveViewModel.cs:6-40` · `tests/FormatDiskPro.UiTests/MainWindowActions.cs:39-47`
**Estado:** nuevo (misma causa raíz que `T7-07`, que solo cubrió los dos `ListView`)
**Problema:** `DrivePicker` no tiene `Header`, `AutomationProperties.Name` ni `LabeledBy`, y sus ítems son objetos `DriveViewModel` sin `ToString()`. El propio proyecto lo dejó escrito como obstáculo de pruebas: «la propiedad `Text`/`Name` del propio ComboBoxItem […] devuelve el `ToString()` por defecto del objeto enlazado ("FormatDiskPro.UI.DriveViewModel"), confirmado contra hardware real».
**Impacto:** con lector de pantalla todas las unidades suenan igual. Es el control que decide qué disco se borra. WCAG 2.2 · 4.1.2 (A) y 1.3.1 (A).
**Evidencia:** ver ubicaciones; `Grep` de `AutomationProperties` en `src/` no devuelve nada para `DrivePicker`.
**Solución propuesta:** nombre del combo por `LabeledBy` al título de la tarjeta, y nombre de cada ítem (`ToString()` → `DisplayText`, o `AutomationProperties.Name` en el contenedor).
**Criterio de aceptación:** prueba de UI que exija `Name` no vacío en el combo y que el `Name` de cada ítem contenga su letra.

### P-05-02 · Dos regiones activas declaradas que nunca se disparan, y un error que no se anuncia
**Área:** Accesibilidad · **Severidad:** Medio · **Confianza:** Alta · **Verificación:** estática · **Esfuerzo:** Bajo
**Ubicación:** `src/FormatDiskPro/UI/HistoryDialog.xaml:32-33` + `HistoryDialog.xaml.cs:104-107` · `MainWindow.Operations.cs:124`, `:174` · `MainWindow.FormatOptions.cs:583` · `MainWindow.HelpAndUpdates.cs:128` · `PresetsDialog.xaml:22-23` + `PresetsDialog.xaml.cs:199-203`
**Estado:** nuevo
**Problema:** `LiveSetting` solo dice cómo leer; lo que dispara la lectura es el evento (lo dice el propio código en `MainWindow.xaml.cs:557`). Solo `LabelErrorText` lo emite. WCAG 2.2 · 4.1.3 (AA).
**(detalle en la versión completa)**

## Pendiente de completar
Resto de hallazgos, hipótesis, comprobaciones en ejecución, puntos fuertes y notas para otras áreas.
