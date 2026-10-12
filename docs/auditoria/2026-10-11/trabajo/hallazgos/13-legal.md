# Área 13 · Legal y cumplimiento · hallazgos provisionales

> **Versión 1 (en curso).** Primera versión con lo ya leído; se amplía cuando terminen las verificaciones
> marcadas como «pendiente». Commit auditado: `72f2902` (v1.29.0).
>
> **No es asesoramiento jurídico.** Se señala lo que falta o no coincide con lo que el código y el instalador
> hacen de verdad; lo que depende de interpretación va marcado como *requiere revisión legal*.
> **La jurisdicción del autor y la de los usuarios no se indicaron:** se aplica solo el núcleo común (ver
> *Zonas no cubiertas*).

**Revisado (leído entero):** `THIRD-PARTY-NOTICES.txt`, `README.md`, `src/FormatDiskPro/Core/LegalText.cs`,
`Core/AppInfo.cs`, `UI/AboutDialog.xaml` y `.xaml.cs`, `UI/LegalTextDialog.xaml` y `.xaml.cs`,
`UI/MainWindow.HelpAndUpdates.cs`, `Services/UpdateService.cs`, `Services/History.cs`, `.github/FUNDING.yml`,
`.github/CONTRIBUTING.md`, `.github/SECURITY.md`, `.github/ISSUE_TEMPLATE/bug_report.yml`,
`src/FormatDiskPro/installer/installer.iss`, `skills-lock.json`, los tres `.csproj`.
**Por búsqueda:** todo uso de red en `src/` (`HttpClient`, sockets, URL, WebView, telemetría), todas las
llamadas a `History.Log`, los textos `about.*` de `Localization.cs` en los cinco idiomas.
**Por muestra:** 4 de las 12 capturas de `docs/screenshots/` (ventana principal, S.M.A.R.T., historial,
reinicializar). **Pendiente en esta versión:** licencia real de los paquetes NuGet en caché, listado del
publish, cabeceras de licencia en las fuentes, texto de `LICENSE`, las ocho capturas restantes.

---

## Hallazgos

### P-13-01 · Los avisos de terceros dan por MIT todo lo redistribuido; la compatibilidad con GPLv3 del binario combinado no está analizada · *requiere revisión legal*
**Área:** Legal y cumplimiento · **Severidad:** Medio · **Confianza:** Media · **Verificación:** estática · **Esfuerzo:** Medio
**Ubicación:** `THIRD-PARTY-NOTICES.txt:23` («Todas las licencias de (A) son MIT, compatible con la GPLv3 de este proyecto») · `:37-40` (Windows App SDK: «Licencia: MIT License» con enlace al `LICENSE` del repositorio de GitHub) · `ROADMAP.md:3650-3652` y `T9-19` («.NET y Windows App SDK, ambos MIT… compatible con GPLv3»)
**Estado:** ya conocido como «revisado, sin hallazgos» (`ROADMAP.md` → *Áreas auditadas sin hallazgos*, y `T9-19`); se reabre por un **hecho nuevo**: el salto a Windows App SDK 2.x (`T13-20`) cambió lo que se redistribuye, y la afirmación se apoya en la licencia del repositorio de código, no en la del paquete binario que se empaqueta.
**Problema:** el aviso identifica la licencia del Windows App SDK por el archivo `LICENSE` del repositorio `microsoft/WindowsAppSDK`. Lo que el instalador redistribuye no es ese código fuente, sino los binarios del paquete NuGet `Microsoft.WindowsAppSDK` 2.5.1 y de sus subpaquetes, que se distribuyen con sus propios términos de licencia; parte de esos binarios (WinUI) no tiene código fuente publicado. Si esos términos no son MIT, la frase de la línea 23 es inexacta y la conclusión «compatible con GPLv3» deja de estar sostenida por lo escrito.
**Impacto:** (a) atribución: el aviso que la app enseña en *Ayuda → Avisos de terceros* puede estar declarando una licencia que no es la del componente; (b) de fondo: la GPLv3 pide que quien redistribuye el binario pueda ofrecer el código fuente correspondiente de todo salvo las «bibliotecas del sistema», y un runtime de interfaz que viaja **dentro** del instalador difícilmente cabe en esa excepción. El autor, como titular único, puede publicar su propio código como quiera; quien no puede cumplir es un tercero que redistribuya el instalador, y el propio proyecto respecto del código que aporten colaboradores externos bajo GPLv3 (ver P-13-08). El remedio habitual es un permiso adicional de la sección 7 de la GPLv3 que autorice la combinación con esos componentes.
**Evidencia:** fragmentos citados. **Pendiente (versión 2):** cotejar el archivo de licencia que acompaña al paquete en la caché de NuGet y el listado real del publish.
**Solución propuesta:** (1) sustituir en el aviso la licencia del repositorio por la del paquete que de verdad se redistribuye, con su texto o enlace; (2) decidir, con revisión legal, si se añade a `LICENSE`/README un permiso adicional (GPLv3 §7) para enlazar y distribuir con el Windows App SDK y el runtime de .NET; (3) corregir la línea 23 para que no afirme más de lo comprobado.
**Criterio de aceptación:** cada entrada (A) cita la licencia que acompaña al artefacto redistribuido, y el proyecto declara por escrito cómo encaja con su GPLv3.

### P-13-02 · La app conecta con GitHub en el primer arranque tras cada actualización aunque «Buscar actualizaciones al iniciar» esté desactivado; el texto de privacidad dice lo contrario · *requiere revisión legal*
**Área:** Legal y cumplimiento · **Severidad:** Bajo · **Confianza:** Alta · **Verificación:** estática · **Esfuerzo:** Bajo
**Ubicación:** `src/FormatDiskPro/UI/MainWindow.xaml.cs:280` (`await MaybeShowWhatsNewAsync();`, antes y fuera del `if (_settings.CheckUpdatesOnStartup)` de `:285`) · `UI/MainWindow.HelpAndUpdates.cs:84-107` · `Core/AppInfo.cs:19-20` · texto `about.privacy` (`Localization/Localization.cs:477`, cinco idiomas) · `README.md:198` · `.github/SECURITY.md:53`
**Estado:** ya conocido (`T9-18`) — **cierre incompleto**. Su criterio de aceptación era «una preferencia persistida que, desactivada, hace que **no salga ninguna petición de red en el arranque**».
**Problema:** `MaybeShowWhatsNewAsync` se ejecuta en todos los arranques y, cuando la versión instalada no coincide con `LastVersionSeen` (primer arranque tras cualquier actualización, también una hecha a mano), llama a `ShowWhatsNewAsync`, que pide `https://api.github.com/repos/xfiberex/FormatDiskPro/releases/tags/v<versión>` y, si falla, `releases/latest`. Ese camino no consulta la preferencia. El texto que la app enseña afirma: «…puedes desactivarla en Configuración → "Buscar actualizaciones al iniciar"; **entonces solo se conecta si la pides desde Ayuda**», y el comentario de `MainWindow.xaml.cs:282-283` la llama «la ÚNICA conexión a Internet de la app». La finalidad declarada («comprobar y descargar actualizaciones») tampoco menciona la descarga de las notas de versión.
**Impacto:** quien desactivó la conexión automática transmite igualmente su IP y el `User-Agent` `FormatDiskPro/<versión>` a GitHub una vez por actualización. Es poco dato y una sola vez, pero es exactamente lo que el aviso de privacidad promete que no ocurre; si eso tiene consecuencia jurídica depende de la jurisdicción.
**Evidencia:** lectura del flujo de arranque y de `ShowWhatsNewAsync`; el `HttpClient` es el único de la app (`UpdateService.cs:68-80`).
**Solución propuesta:** si la preferencia está desactivada, mostrar *Novedades* sin red (texto local o solo el enlace «Ver en GitHub») o no mostrarlo automáticamente; y precisar el texto: finalidad (actualizaciones **y** notas de versión) y a qué servicio (API de GitHub).
**Criterio de aceptación:** con la preferencia desactivada y `LastVersionSeen` distinto de la versión instalada, el arranque no emite ninguna petición de red; el texto de privacidad describe todas las conexiones que la app puede hacer.

### P-13-03 · Inno Setup figura como «no se redistribuye», pero su motor y su desinstalador viajan en cada instalador
**Área:** Legal y cumplimiento · **Severidad:** Bajo · **Confianza:** Alta · **Verificación:** estática · **Esfuerzo:** Bajo
**Ubicación:** `THIRD-PARTY-NOTICES.txt:50-54` («[B) SOLO CONSTRUCCIÓN]… Compila el instalador. No se redistribuye.») · `:23`
**Estado:** nuevo como inexactitud de hecho. `T9-19` trató el criterio de la lista, no la clasificación de Inno Setup.
**Problema:** un instalador compilado con Inno Setup **contiene** el programa de instalación de Inno Setup, y deja en la carpeta de la app su desinstalador (`unins000.exe`). Es código de Inno Setup redistribuido en forma binaria a cada usuario, bajo la licencia de Inno Setup, que no es MIT.
**Impacto:** el aviso clasifica mal un componente que sí llega a la máquina del usuario, y con ello la frase «todas las licencias de (A) son MIT» pasa a ser falsa en cuanto se corrige la clasificación. La atribución en sí ya está hecha (el archivo nombra a sus autores).
**Solución propuesta:** mover Inno Setup al grupo (A), citar su licencia como tal y ajustar la frase de la línea 23. Pendiente de cotejar el texto de la licencia instalada (versión 2).
**Criterio de aceptación:** la clasificación (A)/(B) de cada entrada coincide con lo que contiene el instalador.

### P-13-04 · Los avisos no cubren los componentes de terceros que traen dentro el runtime de .NET y el Windows App SDK 2.x
**Área:** Legal y cumplimiento · **Severidad:** Bajo · **Confianza:** Media · **Verificación:** estática · **Esfuerzo:** Medio
**Ubicación:** `THIRD-PARTY-NOTICES.txt:30-40` · `src/FormatDiskPro/installer/installer.iss:95` (se empaqueta todo el publish)
**Estado:** nuevo. Hecho nuevo: el `.csproj` documenta que la 2.x «trae 9 archivos más de Windows AI y Search que esta app no usa» (`FormatDiskPro.csproj:36-38`).
**Problema:** el archivo lista dos componentes redistribuidos y reproduce una vez el texto MIT genérico. El runtime de .NET *self-contained* incorpora a su vez componentes de terceros con sus propios avisos (el runtime publica un `THIRD-PARTY-NOTICES.TXT` propio), y el Windows App SDK 2.x añade binarios que antes no viajaban. Ninguno de esos avisos se reproduce ni se enlaza, y no hay una copia suelta de licencias junto al binario instalado.
**Impacto:** atribución incompleta de lo que el instalador entrega. Riesgo práctico bajo; es lo primero que mira quien revisa el cumplimiento de licencias de un binario.
**Evidencia:** pendiente de completar con el listado del publish (versión 2).
**Solución propuesta:** incluir (o enlazar por versión) los avisos de terceros del runtime de .NET y del Windows App SDK que se empaquetan, e instalar `LICENSE` y `THIRD-PARTY-NOTICES.txt` también como archivos en `{app}`.
**Criterio de aceptación:** cada binario de terceros del publish queda cubierto por una entrada o por un aviso incluido.

### P-13-05 · El repositorio redistribuye nueve *skills* de `github/awesome-copilot` sin su aviso de copyright ni su licencia
**Área:** Legal y cumplimiento · **Severidad:** Bajo · **Confianza:** Media · **Verificación:** estática · **Esfuerzo:** Bajo
**Ubicación:** `.agents/skills/*/SKILL.md` (9 archivos) · `skills-lock.json` · `THIRD-PARTY-NOTICES.txt` (sin entrada)
**Estado:** nuevo (0 menciones de `awesome-copilot` en `ROADMAP.md`; `CONTEXT.md` solo dice que existen).
**Problema:** los nueve `SKILL.md` son obra de terceros copiada en un repositorio público con licencia GPLv3. `skills-lock.json` registra la procedencia y un hash, pero ni la carpeta ni los avisos incluyen la licencia del origen ni su aviso de copyright, y nada indica que esos archivos **no** están bajo la GPLv3 del proyecto.
**Impacto:** incumplimiento formal de la condición de atribución de la licencia de origen (pendiente de confirmar cuál es; versión 2), y ambigüedad sobre qué licencia cubre esos archivos.
**Solución propuesta:** añadir `.agents/skills/LICENSE` (o un `NOTICE`) con la licencia y el copyright del origen, y una línea en `THIRD-PARTY-NOTICES.txt` (grupo «solo desarrollo»); o dejar de versionarlos y regenerarlos desde `skills-lock.json`.
**Criterio de aceptación:** cada archivo de terceros del repo tiene identificada su licencia y su titular.

### P-13-06 · La licencia se declara como «GPLv3» sin precisar «solo» o «o posterior», y las fuentes no llevan aviso de licencia
**Área:** Legal y cumplimiento · **Severidad:** Bajo · **Confianza:** Media · **Verificación:** estática · **Esfuerzo:** Bajo
**Ubicación:** `README.md:196` · `Localization.cs:473` (`about.copyright`) · `src/FormatDiskPro/FormatDiskPro.csproj:17-18` (sin `PackageLicenseExpression`) · fuentes de `src/` (pendiente de barrido)
**Estado:** nuevo.
**Problema:** ni el README, ni el diálogo *Acerca de*, ni el `.csproj` dicen si la licencia es `GPL-3.0-only` o `GPL-3.0-or-later`. La GPL recomienda un aviso por archivo con el copyright, la licencia y la ausencia de garantía; aquí la licencia vive en un único archivo en la raíz.
**Impacto:** ambigüedad para quien reutilice código suelto o lo combine con otro software GPL de versión distinta. No es un incumplimiento: es una recomendación de la propia licencia, y el copyright y la ausencia de garantía sí están en la interfaz.
**Solución propuesta:** declarar el identificador SPDX en el README y en `about.copyright`, y añadir a cada fuente una cabecera de una línea (`// SPDX-License-Identifier: GPL-3.0-or-later` o `-only`, según se decida).
**Criterio de aceptación:** el identificador SPDX aparece en README, *Acerca de* y fuentes, y es el mismo en los tres.

### P-13-07 · Al pedir el `history.log` se avisa de que contiene «letras y etiquetas de unidad», pero también guarda rutas con el nombre de usuario de Windows
**Área:** Legal y cumplimiento · **Severidad:** Bajo · **Confianza:** Alta · **Verificación:** estática · **Esfuerzo:** Bajo
**Ubicación:** `.github/ISSUE_TEMPLATE/bug_report.yml:78-80` · `.github/SECURITY.md:32-33` · `src/FormatDiskPro/UI/MainWindow.HelpAndUpdates.cs:236` (`UPDATE DOWNLOADED {rel.Version}: {path}`) · `UI/MainWindow.xaml.cs:278` (`SETTINGS UNREADABLE: se apartó a {preserved}`) · `App.xaml.cs:47` (`CRASH: {e.Exception}`)
**Estado:** nuevo.
**Problema:** la plantilla de error y la política de seguridad invitan a pegar líneas del historial en un issue **público** y advierten solo de letras y etiquetas. El historial también registra la ruta de descarga de cada actualización (`C:\Users\<usuario>\AppData\Local\Temp\FormatDiskPro_update\…`), la ruta del `settings.json` apartado y trazas de excepción completas.
**Impacto:** el usuario puede publicar su nombre de cuenta de Windows sin que el aviso se lo haya dicho. Dato menor, pero el proyecto es quien lo solicita.
**Solución propuesta:** ampliar el aviso («…y pueden contener rutas con tu nombre de usuario») o no registrar la ruta completa (basta el nombre del archivo).
**Criterio de aceptación:** el aviso enumera lo que el historial puede contener, o el historial deja de contener rutas del perfil.

### P-13-08 · `CONTRIBUTING.md` no dice bajo qué licencia entran las contribuciones · *requiere revisión legal*
**Área:** Legal y cumplimiento · **Severidad:** Bajo · **Confianza:** Alta · **Verificación:** estática · **Esfuerzo:** Bajo
**Ubicación:** `.github/CONTRIBUTING.md` (completo) · `.github/PULL_REQUEST_TEMPLATE.md`
**Estado:** nuevo. Hecho nuevo: el repositorio es público desde 2026-09 y la guía trata los PR externos como el caso normal.
**Problema:** la guía no menciona la licencia de las aportaciones ni pide certificación de origen (DCO) ni cesión. Por defecto rigen las condiciones de la plataforma (lo aportado entra bajo la licencia del repositorio), lo que deja al autor sin margen para añadir después un permiso adicional (P-13-01) o cambiar de licencia sin el acuerdo de cada colaborador.
**Solución propuesta:** una sección «Licencia de las contribuciones» que declare GPLv3 (con el identificador elegido en P-13-06) y, si se adopta el permiso adicional de P-13-01, que las contribuciones lo aceptan; valorar un DCO (`Signed-off-by`).
**Criterio de aceptación:** la guía y la plantilla de PR dicen bajo qué términos se acepta una contribución.

---

## Revisado sin problemas

- **Uso de red frente a la declaración de privacidad:** la única clase que abre conexiones es `UpdateService` (un `HttpClient`, `UpdateService.cs:68-80`) y solo hacia `api.github.com` y las URL de assets del propio release; no hay telemetría, analítica, seguimiento de errores, WebView ni otros SDK de red en `src/`. La excepción es el momento de la conexión, no el destino (P-13-02).
- **`User-Agent`:** `FormatDiskPro/<versión>` (`UpdateService.cs:74-75`); no lleva identificadores de máquina ni de usuario.
- **Números de serie:** la app no los lee ni los guarda (0 coincidencias de `Serial` en `src/`).
- **Aviso de ausencia de garantía y copyright en la interfaz:** *Acerca de* muestra copyright, licencia, «SIN NINGUNA GARANTÍA» y privacidad en los cinco idiomas; *Ayuda → Licencia* y *Avisos de terceros* enseñan los textos embebidos en el `.exe` (`LegalText.cs`, `FormatDiskPro.csproj:21-24`).
- **Oferta de código fuente:** los binarios se distribuyen desde el mismo sitio que el código (GitHub Releases adjunta el código del tag) y *Acerca de* enlaza al repositorio.
- **Donaciones:** enlace voluntario (`AppInfo.cs:34`, `FUNDING.yml`), abierto solo a petición del usuario; README y código afirman que ninguna función es de pago, y no se encontró ninguna comprobación de pago en el código.
- **Datos locales y su borrado:** historial con rotación a dos generaciones, botón de vaciado que borra ambas (`History.cs:133-143`), y la desinstalación ofrece borrar `%AppData%\FormatDiskPro` con «No» por defecto (`installer.iss:135-152`, `T9-20` resuelta).
- **Fuente de iconos:** Segoe Fluent Icons no se redistribuye (correcto en el aviso).
- **Capturas revisadas (4 de 12):** solo la interfaz de la app; sin números de serie, nombres de usuario ni logotipos de terceros. Etiquetas de volumen visibles: «Desarrollo», «utilidades».

## Hipótesis por verificar

- **Procedencia del icono `FormatDiskPro.ico`:** no hay constancia en el repo de su autoría ni de su licencia. Lo aclara una línea en `THIRD-PARTY-NOTICES.txt` o en `CONTEXT.md`.
- **Inno Setup y uso comercial:** las versiones recientes de Inno Setup distinguen uso comercial y no comercial en su página de licencias. Un proyecto gratuito con donaciones voluntarias es, en principio, no comercial; conviene leer la licencia de la versión instalada. *Requiere revisión legal.*
- **Marcas en textos:** «Windows», «NTFS», «exFAT», «ReFS», «BitLocker», «macOS», «Linux», «PayPal», «GitHub» se usan de forma descriptiva y sin logotipos. No hay declaración de no afiliación con Microsoft; es una buena práctica, no una obligación del núcleo común.

## Cosas a comprobar en ejecución

- Con `CheckUpdatesOnStartup=false` y `LastVersionSeen` de una versión anterior en `%AppData%\FormatDiskPro\settings.json`, arrancar la app observando el tráfico (o sin red): debe aparecer la petición a `api.github.com/…/releases/tags/v1.29.0` (P-13-02).
- Abrir *Ayuda → Avisos de terceros* y *Licencia* en la app instalada y comprobar que el texto es el del repositorio y se lee entero.
- En la carpeta de instalación, comprobar si hay algún archivo de licencia suelto y qué `unins*.exe` deja Inno Setup (P-13-03, P-13-04).
- En GitHub (solo lectura): que el repositorio detecta la licencia como GPL-3.0 y que el release adjunta el código fuente del tag.

## Puntos fuertes

- La declaración de privacidad es verificable contra el código y casi exacta: una sola clase de red, un solo destino, un `User-Agent` sin identificadores.
- Licencia y avisos de terceros van **embebidos en el ejecutable** y accesibles desde el menú, con pruebas que leen esos archivos; no dependen de archivos sueltos que se pierdan.
- El aviso de uso destructivo y de ausencia de garantía está en la interfaz, en cinco idiomas, y el README lo repite.
- Las plantillas de issue y `SECURITY.md` ya advierten de revisar el historial antes de publicarlo.
- Las donaciones son un enlace, sin SDK de pago ni funciones condicionadas.

## Zonas no cubiertas

- **Jurisdicción no indicada.** No se ha evaluado normativa de protección de datos de ningún país (ni si la transmisión de la IP a GitHub exige base legal o mención específica), ni normativa de consumo, ni requisitos de identificación del responsable más allá de lo que el proyecto ya publica.
- **Donaciones:** fiscalidad de los ingresos, condiciones de uso de PayPal para recibir donaciones y cualquier requisito local sobre captación de fondos.
- **Validez y alcance de la exclusión de garantía** de la GPLv3 para una herramienta que destruye datos: depende de la ley aplicable.
- **Compatibilidad GPLv3 ↔ licencias de los binarios de Microsoft redistribuidos** (P-13-01): se señala la duda y dónde está; la conclusión es jurídica.
- **Marcas:** no se ha comprobado si «FormatDiskPro» colisiona con marcas registradas ni las directrices de uso de marca de terceros.
- **Accesibilidad como requisito legal** (EAA, Section 508 u otras): pertenece al área 5 y depende de la jurisdicción.
