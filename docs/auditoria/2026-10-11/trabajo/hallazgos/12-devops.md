# Área 12 · DevOps y configuración · hallazgos provisionales

> **Versión 1 (en curso).** Primera versión escrita con lo ya leído; se amplía en cuanto terminen las
> verificaciones marcadas como «pendiente». Commit auditado: `72f2902` (v1.29.0). Re-auditoría: cada
> hallazgo lleva su **Estado** frente a `ROADMAP.md` / `CONTEXT.md`.

**Revisado (leído entero):** `release.ps1` (866 líneas), `src/FormatDiskPro/installer/build-installer.ps1`,
`installer.iss`, `new-selfsigned-cert.ps1`, `tools/capture-screenshots.ps1`, `.github/workflows/ci.yml`,
`.github/workflows/codeql.yml`, `.github/dependabot.yml`, los tres `.csproj`, `FormatDiskPro.slnx`,
`.gitignore`, `.mcp.json`, `.claude/settings.json`, `.claude/CLAUDE.md`, `skills-lock.json`, `app.manifest`,
`.github/CONTRIBUTING.md`, `SECURITY.md`, `PULL_REQUEST_TEMPLATE.md`, `ISSUE_TEMPLATE/bug_report.yml`,
`Services/UpdateService.cs` y `UI/MainWindow.HelpAndUpdates.cs` (contrato con el corte y con el instalador).
**Leído por secciones:** `CONTEXT.md` (1–350), `ROADMAP.md` (1–430, 786–1155, 1812–2345, 3620–3680) y búsqueda
por palabra clave en ambos y en `CHANGELOG.md` para cada hallazgo.
**No ejecutado (prohibido en el encargo):** `release.ps1`, `build-installer.ps1`, `capture-screenshots.ps1`.

**Herramientas ejecutadas (solo lectura):**
- `git status --porcelain --ignored`, `git ls-files`: árbol limpio salvo `docs/auditoria/`; no existen
  `global.json`, `nuget.config`, `packages.lock.json`, `Directory.Build.props`, `Directory.Packages.props`,
  `.editorconfig`, `.gitattributes` ni `CODEOWNERS`.
- Bytes iniciales de cada script: `release.ps1`, `build-installer.ps1`, `capture-screenshots.ps1` y
  `FormatDiskPro.csproj` llevan BOM UTF-8; `installer.iss` **no** (33 líneas con caracteres no ASCII);
  `new-selfsigned-cert.ps1` no lo lleva y no lo necesita (0 caracteres no ASCII).
- Búsqueda de sintaxis exclusiva de PowerShell 7 (`&&`, `||`, `??`, ternario) en los cuatro `.ps1`: 0 casos.
- Reproducción en un script desechable (fuera del repo) de cómo llega un `[string[]]` con `powershell -File`
  en Windows PowerShell 5.1.26100: ver P-12-13.

Datos de ejecución aportados por el coordinador (no son hallazgos de este archivo; se citan donde cambian
la lectura): `master` sin protección ni rulesets; alertas y actualizaciones de seguridad de Dependabot
desactivadas; secret scanning y push protection activados; 0 alertas de CodeQL; PR #2 de Dependabot abierto
desde el 2026-10-01; el release v1.29.0 lleva `.exe` + `.sha256` y el hash coincide.

---

## Checklist de `release.ps1` (anexo Windows §3), punto por punto

| # | Lo que el script debe impedir o hacer | ¿Cumple? | Dónde | Observación |
|---|---|---|---|---|
| 1 | Abortar si falta la sección de la versión en `CHANGELOG.md` | **Sí**, con un hueco | `release.ps1:396-412` (existencia) · `:660-662` (cuerpo vacío) | La guarda de cuerpo vacío no salta si dos encabezados `## [x]` van pegados (P-12-06). Con `-NotesFile` no se comprueba que la sección tenga contenido. |
| 2 | Abortar si está activa la variable de tests destructivos | **Parcial** | `release.ps1:605-607` | Solo `FORMATDISKPRO_ALLOW_DESTRUCTIVE`, solo con valor exacto `"1"` y solo dentro de `if ($UiTests)`. `FORMATDISKPRO_ALLOW_YANK` y `FORMATDISKPRO_VERIFY_DRIVE` no se miran (P-12-08). |
| 3 | Abortar con árbol sucio salvo `-AllowDirty`, y avisar de que solo hace `git add -u` | **Sí** | `release.ps1:467-491` (modificados `:475-478`, sin rastrear `:484-487`) · plan en `:707-708` · `git add -u` en `:787` | `T9-01` sigue resuelta en el camino normal. Matiz: con `-AllowDirty`, los archivos sin rastrear se anuncian como «ignorados» (`:489`) pero el SDK los compila igual (P-12-02, nota). |
| 4 | Distinguir «omitido» de «correcto» leyendo el `.trx` | **Parcial** | UI: `release.ps1:289-314`, `:321-347`, `:622-634`, `:857` | Solo para las pruebas de UI. Las unitarias se dan por «correctas» con el código de salida (`:519-521`), sin `.trx` (P-12-07). |
| 5 | `-DryRun` que valide y muestre el plan sin tocar nada; `-UiTests`; aviso si se publica sin haber ejercido la app | **Sí**, con huecos | `-DryRun`: `release.ps1:689-728` · `-UiTests`: `:591-637` · aviso: `:636` y `:325-326` | `-DryRun` ejecuta las pruebas (compila y escribe en `bin/`, `obj/` y `%TEMP%`) y **no** valida las herramientas de los pasos que omite: ISCC, `gh` y su autenticación, certificado (P-12-05). |
| 6 | `.sha256` como asset obligatorio | **Sí** | `release.ps1:779-780`, `:739`, `:847` · lo genera `build-installer.ps1:217-219` | Se exige que el archivo exista; no se recalcula ni se compara con el `.exe` antes de subirlo (P-12-02). |
| 7 | Firma Authenticode con sello de tiempo | **Implementado como opción** (no firmar es decisión cerrada) | `build-installer.ps1:107-125` (`/fd SHA256 /tr … /td SHA256`, `:109`) · TSA por defecto `:49` | Sin observaciones: la marca de tiempo RFC 3161 y los dos resúmenes SHA-256 están puestos. |
| 8 | Contraseña de certificado como `SecureString` o variable de entorno | **Sí** en el corte; **no** en el script auxiliar | `release.ps1:91` · `build-installer.ps1:48`, `:56-58`, `:64-69` | `new-selfsigned-cert.ps1:41` sigue con `[string]$Password` (P-12-14). |
| 9 | Nota de PowerShell 5.1: sin `&&`/`||`/ternario/`??`; codificación; `$ErrorActionPreference='Stop'` y `Set-StrictMode` | **Parcial** | `Stop`: `release.ps1:95`, `build-installer.ps1:52` · escrituras con codificación explícita: `release.ps1:682` (sin BOM), `:759` (con BOM), `build-installer.ps1:219` (ASCII) | Sin sintaxis de PS 7 (comprobado). **No** hay `Set-StrictMode` en `release.ps1` ni en `build-installer.ps1` (solo en `capture-screenshots.ps1:68`). Dos redirecciones de stderr sin la protección que el propio script documenta en `:825` (`:418`, `:426`) (P-12-15). |

**Atomicidad: qué queda a medias si falla cada paso** (lectura estática del flujo):

| Falla en… | Qué queda | ¿Reintento guiado? |
|---|---|---|
| Validaciones, pruebas, cobertura (`:359-638`) | Nada (solo `bin/`, `obj/`, `%TEMP%`) | Sí |
| Compilar el instalador (`:763-770`) | `.csproj` con el bump **sin commitear** | No: el reintento aborta por «árbol sucio» y el mensaje no dice que el cambio es del propio script (P-12-05) |
| `git commit` (`:792`) | Bump en el índice, instalador en `Output\` | No: hay que pasar `-AllowDirty` o deshacer |
| `git tag` (`:798`) | Commit local sin tag | Sí: relanzar normal etiqueta el HEAD (`:795`) |
| `git push` de la rama (`:805`) | Commit + tag locales | **No**: mensaje sin consejo; `-ResumeRelease` no sube la rama (P-12-01) |
| `git push` del tag (`:806-808`) | Rama subida, tag local | Sí: `-ResumeRelease` |
| Buscar `gh` / autenticarlo (`:814-844`) | Rama y tag publicados, sin release | Sí (indirecto): el mensaje dice «reintenta» y es la guarda del tag la que nombra `-ResumeRelease` |
| `gh release create` (`:847-850`) | Igual que arriba; posible borrador en GitHub | Sí: `-ResumeRelease` (ver hipótesis H-3) |

**Tareas cerradas del corte, comprobadas:** `T9-01` (árbol sucio: resuelta, `:467-491`), `T8-04` (notas desde
el CHANGELOG: resuelta, `:649-685`), `T8-05` (sin BOM en las notas: resuelta, `:682`), `T8-06` (compilar antes
de medir: resuelta, `:514-519`), `T10-01` (cuatro estados de cobertura: resuelta, `:171-237`, `:525-576`),
`T2-12` (omitidos de UI desde el `.trx`: resuelta para UI, `:289-347`), `T4-01` (puerta del CHANGELOG:
resuelta, `:396-412`), `T9-02` (`-ResumeRelease`: resuelta, con el hueco de P-12-02), `T9-03` (`GH_TOKEN` se
limpia: resuelta, `:842`, `:864`), `T3-09` (SecureString: resuelta en los dos scripts del corte). Ninguna
cerrada en falso en este archivo; `T9-17` tiene un cierre incompleto (P-12-16).

---

## Hallazgos

### P-12-01 · El corte no comprueba en qué rama está ni si va por detrás de `origin`: lo descubre al hacer push, con el commit y el tag ya creados
**Área:** DevOps · **Severidad:** Medio · **Confianza:** Alta · **Verificación:** estática · **Esfuerzo:** Bajo
**Ubicación:** `release.ps1:421-422` (solo imprime la rama) · `:426` (único contacto con `origin` antes del push: `ls-remote` del tag) · `:805` (push de la rama, sin consejo) · `:745-749` (`-ResumeRelease` sube solo el tag)
**Estado:** nuevo. Relacionado con `T9-02` (el reintento tras un fallo), que no cubre este paso.
**Problema:** el script acepta cualquier rama (`$branch = (& git rev-parse --abbrev-ref HEAD).Trim()`, incluida `HEAD` suelta) y nunca hace `git fetch` ni compara con `origin/$branch`. Si `origin` tiene commits que el clon local no tiene, todo el corte se ejecuta —pruebas, bump, instalador, `git commit`, `git tag -a`— y falla en `:805` con `Die "git push de la rama falló."`, que es el único mensaje de fallo de la fase de publicación que no dice cómo seguir.
**Impacto:** corte a medias con recuperación no guiada. Lo que hace probable el caso es un **hecho nuevo** respecto a las auditorías anteriores: desde el 2026-09-18 hay fusiones en remoto (Dependabot mensual, PR externos con el repositorio público), y hoy mismo hay un PR de Dependabot abierto (#2); fusionarlo desde GitHub deja el `master` local por detrás. La salida natural (`git pull --rebase`) reescribe el commit `release: vX.Y.Z` y deja el tag apuntando al commit viejo; si después se sigue el consejo de la guarda del tag (`-ResumeRelease`), se publica un release cuyo tag **no está en `master`**, porque ese modo sube el tag pero no la rama ni comprueba que el commit etiquetado esté contenido en `origin/$branch`. Si lo fusionado en remoto era código y no workflows, el instalador ya compilado tampoco lo contiene.
**Evidencia:** lectura completa del script: no hay ningún `fetch`, `rev-list` ni `merge-base`. `master` no tiene protección ni rulesets (dato del coordinador), así que nada externo lo impide.
**Solución propuesta:** al principio de las validaciones de git: `git fetch origin $branch`, abortar si `git rev-list --count HEAD..origin/$branch` es mayor que 0 («tu rama va N commits por detrás: `git pull` antes de cortar»), y avisar o abortar si `$branch` no es `master` (con un `-AllowBranch` explícito si se quiere conservar la posibilidad). En `:805`, dar el mismo tipo de mensaje que en `:807`. En `-ResumeRelease`, comprobar `git merge-base --is-ancestor $tag origin/$branch` y subir la rama si falta.
**Criterio de aceptación:** con un commit de más en `origin/master`, `release.ps1 -DryRun` aborta antes de las pruebas nombrando cuántos commits faltan; tras un fallo simulado del push de la rama, seguir el mensaje deja rama, tag y release coherentes.

### P-12-02 · `-ResumeRelease` publica el instalador que haya en `installer\Output` sin comprobar que es el del tag ni que su `.sha256` le corresponde
**Área:** DevOps · **Severidad:** Medio · **Confianza:** Alta · **Verificación:** estática · **Esfuerzo:** Bajo
**Ubicación:** `release.ps1:731-742` · `:473-474` · camino normal: `:771-781`, `:847`
**Estado:** nuevo. Es la premisa de `T9-01` («el binario publicado podía no corresponder al commit que lleva el tag») reapareciendo por el camino que añadió `T9-02`; `T9-01` sigue resuelta en el camino normal.
**Problema:** al retomar, el script solo exige que existan `FormatDiskPro-$Version-setup.exe` y su `.sha256` (`Test-Path`, `:736-739`) y declara expresamente que el estado del árbol da igual: `Info "Árbol sucio, irrelevante al retomar: no se commitea ni se recompila nada…"` (`:474`). No hay nada que ate ese archivo al commit etiquetado (ni hash anotado en el tag, ni fecha comparada, ni recomprobación), y en ninguno de los dos caminos se recalcula el SHA-256 para compararlo con el contenido del `.sha256` antes de subirlo.
**Impacto:** dos escenarios concretos. (a) Entre el corte fallido y el reintento se vuelve a ejecutar `build-installer.ps1` —para depurar el fallo, o con el árbol ya tocado—: sobrescribe el `.exe` y el `.sha256` con el mismo nombre (la versión del `.csproj` ya es la nueva) y `-ResumeRelease` publica **un binario que no sale del commit etiquetado**; el hash cuadra, así que nada avisa. (b) Si el par se desempareja (el `.exe` se regenera o se firma a mano y el `.sha256` no), se publica un release cuyo instalador la app descarga, no verifica y borra: la auto-actualización queda rota para todos los usuarios hasta publicar otra versión. La verificación por hash es la única garantía del canal porque no se firma (decisión cerrada), así que la correspondencia binario↔tag es justo lo que hay que custodiar.
**Evidencia:** `release.ps1:734-742` solo contiene `Test-Path` y `Get-Item … .Length`. `build-installer.ps1:207-219` regenera siempre los dos archivos con el nombre de la versión.
**Solución propuesta:** (1) en ambos caminos, antes de `gh release create`, recalcular `Get-FileHash $setup` y abortar si no coincide con el primer campo del `.sha256`; (2) al etiquetar, escribir el hash en el mensaje del tag anotado (`git tag -a $tag -m "FormatDiskPro $tag`nsha256: $hash"`) y, en `-ResumeRelease`, exigir que el hash del `.exe` presente sea el del tag; (3) dejar de llamar «ignorados» a los archivos sin rastrear con `-AllowDirty` (`:489`): los `.cs` y `.xaml` sin rastrear bajo `src/FormatDiskPro/` entran en el instalador por el globbing del SDK aunque no entren en el commit, y el aviso debería decirlo o el script negarse cuando haya fuentes sin rastrear.
**Criterio de aceptación:** con el `.exe` de `Output\` sustituido por otro tras etiquetar, `-ResumeRelease` aborta nombrando la discrepancia; con un `.sha256` alterado, ambos caminos abortan antes de subir nada.

### P-12-03 · El build no es reproducible más allá del Windows App SDK: SDK de .NET sin fijar, sin lock file y sin registro del runtime que se empaqueta
**Área:** DevOps · **Severidad:** Medio · **Confianza:** Alta · **Verificación:** estática · **Esfuerzo:** Medio
**Ubicación:** raíz del repo (no existe `global.json`, `nuget.config` ni `packages.lock.json`) · `.github/workflows/ci.yml:49` y `:62-64` · `.github/workflows/codeql.yml:42` y `:55-57` · `src/FormatDiskPro/installer/build-installer.ps1:183-190`
**Estado:** nuevo. `ROADMAP.md` (#44, «Build reproducible») y `CONTEXT.md` §4 solo fijan `Microsoft.WindowsAppSDK`; ninguno menciona `global.json` ni lock files (0 coincidencias en ambos).
**Problema:** tres piezas flotan y el argumento que el proyecto ya escribió para no dejar flotar el Windows App SDK («subir de versión debe ser un cambio deliberado, no un efecto colateral de la fecha en que se compile», `FormatDiskPro.csproj:26-41`) les aplica igual:
1. **SDK de .NET.** El CI usa `dotnet-version: '10.0.x'` sobre `runs-on: windows-latest`; en local se compila con lo que haya instalado (10.0.401 según la línea base). Con `-warnaserror` (`ci.yml:74`, `:84`), una banda nueva del SDK que traiga analizadores o avisos nuevos pone en rojo PR que no han tocado nada relacionado, que es exactamente el motivo por el que el propio workflow excluye `NU1901–NU1904`.
2. **Runtime empaquetado.** La app se publica *self-contained* (`--self-contained true`), así que el runtime de .NET que llega a cada usuario es el parche que conozca el SDK de la máquina del mantenedor el día del corte. Nada en el repo ni en el script lo fija, lo comprueba ni lo deja escrito, y sus parches de seguridad solo llegan a los usuarios con una versión nueva de la app, que corre siempre elevada y procesa JSON de la red.
3. **Restauración de NuGet.** Sin `packages.lock.json` (ni `RestoreLockedMode` en CI/corte) no se comprueba el contenido de lo restaurado, y sin `nuget.config` en el repo las fuentes son las de cada máquina. El metapaquete 2.x del Windows App SDK resuelve además subpaquetes «cada uno por su versión» (`FormatDiskPro.csproj:38-40`).
**Impacto:** no se puede reconstruir el instalador de una versión publicada ni decir qué runtime lleva sin abrirlo; el CI y el corte pueden compilar con SDK distintos; un fallo del CI puede no tener causa en el repositorio.
**Evidencia:** `git ls-files` y búsqueda en disco (sin esos archivos); fragmentos citados.
**Solución propuesta:** `global.json` con la versión del SDK y `rollForward: latestPatch` (o `disable` si se quiere exactitud), y `global-json-file: global.json` en `setup-dotnet`; `RestorePackagesWithLockFile=true` + `packages.lock.json` versionados y `dotnet restore --locked-mode` en CI y en `release.ps1` (habilita además la caché de `setup-dotnet`); `nuget.config` con `<clear/>` y solo nuget.org; y que `build-installer.ps1` escriba en su salida —y `release.ps1` en las notas o en un asset— la versión del SDK y la del runtime empaquetado (ver P-12-17).
**Criterio de aceptación:** dos compilaciones del mismo commit en máquinas distintas usan el mismo SDK y los mismos paquetes (el CI falla si el lock no cuadra), y cada release declara la versión del runtime que contiene.

### P-12-12 · Configuración de agentes de IA versionada en un repositorio público: comando sin fijar, instrucciones de terceros sin verificar y fuera de la lista de «revisar con cuidado»
**Área:** DevOps · **Severidad:** Medio · **Confianza:** Media · **Verificación:** estática · **Esfuerzo:** Bajo
**Ubicación:** `.mcp.json:3-10` · `.claude/settings.json:2-13` · `.claude/CLAUDE.md` (secciones *CodeGraph* y *Skills del proyecto*) · `.agents/skills/*/SKILL.md` (9 archivos) · `skills-lock.json` · `.github/PULL_REQUEST_TEMPLATE.md:28` · `.github/CONTRIBUTING.md:47-48`
**Estado:** nuevo (0 menciones de `.mcp.json`, MCP o `awesome-copilot` en `ROADMAP.md` y `CONTEXT.md`).
**Problema:** el repositorio versiona cuatro cosas que un agente de código ejecuta u obedece al abrir el proyecto: (1) `.mcp.json` declara un servidor que arranca `"command": "codegraph"` resuelto por `PATH`, sin ruta, versión ni procedencia documentada; (2) `.claude/settings.json` preautoriza ocho herramientas `mcp__codegraph__*` sin pedir permiso; (3) `.claude/CLAUDE.md` ordena a cualquier agente leer `.agents/skills/<nombre>/SKILL.md` «antes del trabajo correspondiente»; (4) esos nueve `SKILL.md` son contenido de un tercero (`github/awesome-copilot`), y `skills-lock.json` guarda un `computedHash` por skill que nada comprueba (ni el CI ni ningún script). La guía de contribución y la plantilla de PR señalan como delicado **solo** `.github/workflows/` («se ejecutan con los permisos del repositorio»); estos archivos no aparecen.
**Impacto:** un PR externo que cambie `.mcp.json`, `.claude/settings.json`, `CLAUDE.md` o un `SKILL.md` modifica lo que el agente del mantenedor ejecuta o cree cuando revisa esa rama en local. En este proyecto el agravante es concreto: la terminal de trabajo es con frecuencia **elevada** (las pruebas de UI y las capturas lo exigen) y es la máquina que guarda la credencial con la que se publica un instalador que los usuarios ejecutan como administrador. Además, quien clone el repo y acepte el servidor MCP ejecuta el `codegraph` que tenga en su `PATH`, sin que el proyecto diga cuál es.
**Evidencia:** archivos citados. Lo que subiría la confianza a Alta: comprobar en una rama de prueba si el cliente vuelve a pedir aprobación cuando cambia el `command` de un servidor ya aprobado con el mismo nombre. No se encontró texto malicioso en los archivos leídos (pendiente de completar el barrido de los nueve `SKILL.md`).
**Solución propuesta:** añadir `.mcp.json`, `.claude/`, `.agents/` y `skills-lock.json` a la lista de rutas sensibles de `CONTRIBUTING.md` y de la plantilla de PR; un paso de CI que falle si el hash de cada `SKILL.md` no coincide con `skills-lock.json`; documentar qué es `codegraph`, de dónde se instala y qué versión; valorar sacar `.mcp.json` y `.claude/settings.json` del repo público (configuración local) dejando un ejemplo. `CODEOWNERS` sobre esas rutas si algún día se protege `master`.
**Criterio de aceptación:** un PR que toque cualquiera de esas rutas queda señalado (check o plantilla), y el CI detecta un `SKILL.md` que no corresponde a su hash bloqueado.

### P-12-04 · El corte prueba una compilación Debug y sin `-warnaserror`; el binario Release que publica no es el que probó, y el CI lo ve después de publicado
**Área:** DevOps · **Severidad:** Bajo · **Confianza:** Alta · **Verificación:** estática · **Esfuerzo:** Bajo
**Ubicación:** `release.ps1:515` (`dotnet build $solution --nologo -v q`) · `:519` (`dotnet test … --no-build`) · `:628-629` (UI) · `build-installer.ps1:183-190` (`dotnet publish -c Release`) · `.github/workflows/ci.yml:74-84`
**Estado:** nuevo.
**Problema:** la compilación y las unitarias del corte van sin `-c`, es decir en **Debug**, y sin `-warnaserror`; el instalador sale de un `dotnet publish -c Release` posterior que ya no pasa por ninguna prueba ni por el listón de «0 advertencias». El CI sí compila en Release con `-warnaserror`, pero `release.ps1` es quien sube la rama (`:805`), de modo que el CI del commit publicado se ejecuta **después** de crear el release, y el tag no dispara ningún workflow.
**Impacto:** el listón que `CONTRIBUTING.md` declara («0 advertencias, 0 errores: es el listón») no forma parte de la puerta de publicación; un aviso nuevo —incluidos los de auditoría de NuGet, que el CI silencia a propósito— puede publicarse sin que nada lo detenga. Pendiente de completar: qué ejecutable conducen las pruebas de UI del corte (se añade en la versión 2).
**Evidencia:** fragmentos citados; ningún `-warnaserror` ni `-c Release` en `release.ps1`.
**Solución propuesta:** `dotnet build $solution -c Release -warnaserror` y `dotnet test … -c Release --no-build` en el corte (igual que el CI), y `-warnaserror` en el `dotnet publish`.
**Criterio de aceptación:** un aviso de compilación introducido a propósito aborta `release.ps1 -DryRun`.

### P-12-05 · Las herramientas de los pasos tardíos se validan después de mutar: ISCC tras el bump, `gh` tras el push; `-DryRun` no las comprueba
**Área:** DevOps · **Severidad:** Bajo · **Confianza:** Alta · **Verificación:** estática · **Esfuerzo:** Bajo
**Ubicación:** `release.ps1:753-760` (bump) → `:769-770` (instalador) · `build-installer.ps1:129`, `:139`, `:177`, `:191`, `:205` (fallos por `throw`) · `release.ps1:814-844` (`gh` y su autenticación, después de `:805-806`) · `:689-728` (`-DryRun`)
**Estado:** nuevo. Misma familia que `T9-02`.
**Problema:** (1) el `.csproj` se reescribe antes de saber si el instalador se puede compilar; `build-installer.ps1` señala sus fallos con `throw`, así que el `if ($LASTEXITCODE -ne 0) { Die … }` de `:770` no llega a ejecutarse: el corte muere con la excepción cruda y el `.csproj` queda con el bump sin commitear. El reintento aborta entonces por «árbol sucio» listando como cambio ajeno lo que dejó el propio script. (2) `gh` se busca y se autentica cuando la rama y el tag ya están en `origin`; si falta o no hay credencial, queda un tag publicado sin release. (3) `-DryRun` valida versión, CHANGELOG, árbol y pruebas, pero no ISCC, `signtool`/certificado, `gh` ni su autenticación.
**Impacto:** cortes a medias evitables y un `-DryRun` que da por bueno un plan que el corte real no puede completar. Sin daño al usuario final: la app consulta `releases/latest`, así que un tag sin release es invisible.
**Evidencia:** orden de las líneas citadas.
**Solución propuesta:** un bloque de «preflight» antes de las pruebas, compartido con `-DryRun`: localizar ISCC y `gh`, comprobar `gh auth status` (o la credencial cacheada), y `signtool` + certificado si se pidió firmar. Envolver la llamada a `build-installer.ps1` en `try/catch` que restaure el `.csproj` (`[IO.File]::WriteAllText($csproj, $csprojRaw, …)` con BOM) y diga cómo reintentar.
**Criterio de aceptación:** con `gh` fuera del `PATH` o ISCC desinstalado, `release.ps1 -DryRun` aborta nombrando la herramienta; un fallo forzado de ISCC deja el `.csproj` como estaba.

### P-12-06 · La guarda de «sección del CHANGELOG vacía» no salta cuando dos encabezados de versión van pegados
**Área:** DevOps · **Severidad:** Bajo · **Confianza:** Alta · **Verificación:** estática · **Esfuerzo:** Bajo
**Ubicación:** `release.ps1:653-662`
**Estado:** nuevo (afina `T8-04` / `T4-01`, que siguen resueltas en el caso normal).
**Problema:** el cuerpo se toma con `$lines[$start..($end - 1)]`. Si la línea siguiente al encabezado de la versión es ya otro `## …` (sección recién creada y vacía, sin línea en blanco), `$end` vale `$start` y el rango `$start..($start-1)` **es descendente** en PowerShell: devuelve el encabezado siguiente y el propio. `$body` no queda vacío y `if (-not $body) { Die … }` no se dispara.
**Impacto:** el release se publicaría con dos encabezados como notas, y eso es también lo que enseña el diálogo *Novedades* de la app: el fallo silencioso que `T8-04` quiso hacer imposible.
**Evidencia:** semántica del operador de rango; reproducción en script desechable pendiente (versión 2).
**Solución propuesta:** `if ($start -ge 0 -and $end -gt $start) { … } else { "" }`, y exigir que el cuerpo contenga alguna línea que no sea encabezado.
**Criterio de aceptación:** con `## [X]` seguido directamente de `## [anterior]`, el corte aborta con «No se pudo extraer la sección».

### P-12-07 · «Omitido» y «correcto» solo se distinguen en las pruebas de UI: el corte no lee ningún `.trx` de las unitarias
**Área:** DevOps · **Severidad:** Bajo · **Confianza:** Alta · **Verificación:** estática · **Esfuerzo:** Bajo
**Ubicación:** `release.ps1:519-521` · `:292-299` · `:324-328`
**Estado:** nuevo (extiende `T2-12`, resuelta para UI; el CI sí lo hace para las unitarias, `ci.yml:99-122`).
**Problema:** las unitarias se dan por buenas con `Ok "Pruebas unitarias correctas."` sin logger `trx`, así que una prueba que pase a omitirse no deja rastro en el corte (hoy hay 1 omitida por precondición, según la línea base). En UI, si el `.trx` no se puede leer o no contiene resultados, `Get-TestRunSummary` devuelve `$null` y el corte sigue con un aviso que afirma «ejecutados y correctos» (`:327`), también si el filtro no encontró ninguna prueba.
**Impacto:** la lección de `T2-12` queda aplicada a medias en el script que decide si se publica.
**Solución propuesta:** añadir `--logger trx` a `:519`, reutilizar `Get-TestRunSummary` e imprimir «N/M — K omitidas» con su motivo; tratar como fallo un `.trx` de UI ausente o con 0 resultados.
**Criterio de aceptación:** un `[Fact(Skip=…)]` añadido a una unitaria aparece nombrado en el resumen final del corte.

### P-12-08 · El corte solo rechaza una de las tres variables que hacen tocar hardware, y solo con el valor exacto `1`
**Área:** DevOps · **Severidad:** Bajo · **Confianza:** Media · **Verificación:** estática · **Esfuerzo:** Bajo
**Ubicación:** `release.ps1:605-607`
**Estado:** nuevo. `ROADMAP.md` (`T2-13`) dice de `FORMATDISKPRO_ALLOW_YANK` que «no debe correr por sorpresa en un corte de release», pero el script no la mira.
**Problema:** la guarda es `if ($env:FORMATDISKPRO_ALLOW_DESTRUCTIVE -eq "1")`, dentro de `if ($UiTests)`. No hay ninguna referencia a `FORMATDISKPRO_ALLOW_YANK` (desmonta la USB a la fuerza a mitad de operación) ni a `FORMATDISKPRO_VERIFY_DRIVE` (activa en las **unitarias** la prueba que escribe sin caché sobre una unidad real).
**Impacto:** con una de esas variables viva de una sesión de depuración, el corte ejecuta pruebas contra hardware sin avisarlo. Lo que falta por confirmar (y por eso la confianza es Media): con qué valores exactos se activan los atributos de prueba (`TestDriveFacts.cs`); si aceptan algo distinto de `"1"`, la guarda de `ALLOW_DESTRUCTIVE` también se puede esquivar.
**Solución propuesta:** rechazar (o al menos anunciar en el resumen) las tres variables si no están vacías, antes de la primera `dotnet test`, y compartir el criterio de activación con los atributos.
**Criterio de aceptación:** con cualquiera de las tres definidas, `release.ps1 -DryRun` aborta o lo declara en el plan.

### P-12-09 · La versión mínima de Windows se declara con tres valores distintos, y el motivo escrito en el instalador ya no es cierto
**Área:** DevOps · **Severidad:** Bajo · **Confianza:** Alta · **Verificación:** estática · **Esfuerzo:** Bajo
**Ubicación:** `README.md:94` («Windows 10 1809+ u 11, x64») · `.github/CONTRIBUTING.md:13` · `src/FormatDiskPro/installer/installer.iss:66-67` (`MinVersion=10.0.19041`) y comentarios `:10-12`, `:25` · `src/FormatDiskPro/FormatDiskPro.csproj:5` (TFM `net10.0-windows10.0.19041.0`, sin `TargetPlatformMinVersion` ni `SupportedOSPlatformVersion`) · `README.md:190` («Windows App SDK 1.8»)
**Estado:** pendiente de cotejar con la única mención de «1809» en `ROADMAP.md`/`CONTEXT.md` (versión 2).
**Problema:** el README y la guía de contribución prometen Windows 10 **1809**; el instalador se niega por debajo de **2004 (19041)**; y el comentario que lo justifica («Windows App SDK 1.8 requiere como mínimo Windows 10 v2004») cita una versión del SDK que el proyecto ya no usa (2.5.1 desde `T13-20`). El `.csproj` no declara mínimo, así que hereda el del TFM.
**Impacto:** quien tenga Windows 10 1809–1909 y lea el README descarga 60 MB para encontrarse el rechazo del instalador; quien mantenga el proyecto no tiene un único sitio que diga cuál es el mínimo real ni por qué.
**Solución propuesta:** decidir el mínimo (hoy, de hecho, 19041), declararlo en el `.csproj` (`<SupportedOSPlatformVersion>`), y alinear README, CONTRIBUTING, `MinVersion` y los tres comentarios de `installer.iss`.
**Criterio de aceptación:** una búsqueda de «1809», «19041» y «1.8» en el repo devuelve un único valor coherente.

### P-12-10 · Efecto no previsto de excluir NuGet de Dependabot: los paquetes de prueba no los actualiza nadie y ya divergen entre los dos proyectos
**Área:** DevOps · **Severidad:** Bajo · **Confianza:** Alta · **Verificación:** estática + línea base · **Esfuerzo:** Bajo
**Ubicación:** `.github/dependabot.yml:1-20` · `tests/FormatDiskPro.Tests/FormatDiskPro.Tests.csproj:12-17` · `tests/FormatDiskPro.UiTests/FormatDiskPro.UiTests.csproj:12-16` · `.github/workflows/ci.yml:74`, `:84`
**Estado:** nuevo como efecto. **No se reabre la decisión** («Dependabot solo para acciones porque el Windows App SDK se sube a mano»): el motivo escrito cubre un paquete y la exclusión alcanza a todos.
**Hecho nuevo:** la línea base del 2026-10-11 mide el resultado: `Microsoft.NET.Test.Sdk` 17.11.1 en un proyecto y 17.14.1 en el otro (última 18.10.1), `xunit` 2.9.2 frente a 2.9.3, `xunit.runner.visualstudio` 2.8.2 frente a 3.1.4 (última 4.0.1), `coverlet.collector` 6.0.2 (última 10.1.0). Y hoy ningún mecanismo vigila vulnerabilidades de NuGet: el CI silencia `NU1901–NU1904`, `release.ps1` no audita, y las alertas y actualizaciones de seguridad de Dependabot están desactivadas (dato del coordinador).
**Impacto:** acotado a paquetes de prueba (no se redistribuyen) y sin vulnerabilidades conocidas hoy; el riesgo es que el recolector de cobertura que decide si un corte sale (`T10-02` sigue abierta sobre él) lleve cuatro versiones mayores de retraso sin que nadie lo haya decidido.
**Solución propuesta:** añadir el ecosistema `nuget` a `dependabot.yml` con `ignore` para `Microsoft.WindowsAppSDK*` (la decisión queda intacta) y un grupo «pruebas»; unificar versiones con `Directory.Packages.props`; y un paso de `dotnet list package --vulnerable --include-transitive` en `release.ps1` que avise o aborte.
**Criterio de aceptación:** los dos proyectos de prueba referencian las mismas versiones y existe un mecanismo automático que propone sus actualizaciones sin tocar el Windows App SDK.

### P-12-11 · El CI no ejercita lo que solo falla al publicar: ni el publish *self-contained*, ni el instalador, ni los scripts de PowerShell 5.1
**Área:** DevOps · **Severidad:** Bajo · **Confianza:** Alta · **Verificación:** estática · **Esfuerzo:** Medio
**Ubicación:** `.github/workflows/ci.yml:66-84` · raíz (sin `.editorconfig` ni `.gitattributes`)
**Estado:** nuevo. **No reabre** que las pruebas de UI no corran en CI.
**Problema:** los tres fallos de publicación que el proyecto documenta —el `.pri` que `dotnet publish` no copia, el archivo que pasó de `MAX_PATH`, y un script sin BOM que PowerShell 5.1 no puede leer— solo se manifiestan al publicar o al ejecutar los scripts en 5.1, y el CI no hace ninguna de las dos cosas (usa `pwsh` y `dotnet build`). La convención «los scripts y el `.csproj` van con BOM» (`CONTEXT.md` §4) no la custodia ninguna prueba ni ningún archivo de configuración de editor.
**Impacto:** una regresión en esa zona se descubre el día del corte, en la máquina del mantenedor. Falla hacia el lado seguro (no se publica), pero tarde.
**Solución propuesta:** un paso `dotnet publish … --self-contained true -o $env:RUNNER_TEMP\p` que compruebe que existen `FormatDiskPro.exe` y `FormatDiskPro.pri` y que ninguna ruta relativa pasa de un tope; un paso con `shell: powershell` (5.1) que analice sintácticamente los cuatro `.ps1` (`[System.Management.Automation.Language.Parser]::ParseFile`) y verifique el BOM de los que llevan caracteres no ASCII; `.editorconfig` con `charset = utf-8-bom` para `*.ps1` y `*.csproj`.
**Criterio de aceptación:** un PR que quite el BOM de `release.ps1` o rompa el target `CopyAppPriToPublish` queda en rojo.

### P-12-13 · `tools/capture-screenshots.ps1` falla o se degrada cuando lo lanza otro proceso: `-Only` con `-File` no filtra nada, los nombres desconocidos se descartan en silencio y los acentos de la salida se rompen
**Área:** DevOps · **Severidad:** Bajo · **Confianza:** Confirmado · **Verificación:** ejecución (coordinador) + reproducción del mecanismo + estática · **Esfuerzo:** Bajo
**Ubicación:** `tools/capture-screenshots.ps1:58` (`[string[]]$Only`) · `:56-57` (ejemplo `-Only checkdisk,confirm`) · `:588-591` · salida con acentos: `:406`, `:418`, `:595`
**Estado:** nuevo (las dos menciones de `-Only` en `ROADMAP.md`/`CONTEXT.md` describen el parámetro, no este fallo).
**Problema:** (1) `powershell -File tools\capture-screenshots.ps1 -Gallery -Only main,confirm` aborta con «El filtro -Only no coincide con ninguna toma»: con `-File` los argumentos no se interpretan como expresiones de PowerShell y la lista llega como **una sola cadena** `"main,confirm"`; el script no la separa. (2) Dentro de PowerShell, un nombre mal escrito se pierde sin aviso: `$shots | Where-Object { $Only -contains $_.Name }` se queda con los que coinciden y solo lanza si no coincide **ninguno**; la corrida termina en «Galería COMPLETA» con menos tomas de las pedidas, que es la confusión entre omitido y correcto que `T9-05` corrigió para el resto de la galería. (3) Al capturar la salida desde otro proceso, «Galería» llega como «Galer�a»: el script no fija la codificación de la consola y Windows PowerShell 5.1 escribe la salida redirigida en la página de códigos OEM.
**Impacto:** el script es la herramienta de evidencias de las áreas 5 y 6 y la forma documentada de reverificar un subconjunto; lanzado desde `cmd`, Git Bash, una tarea programada o un agente (que es como se usa durante una auditoría) el filtro no funciona y el resumen se lee mal.
**Evidencia:** reproducción con un script desechable en Windows PowerShell 5.1.26100: con `-File … -Only main,confirm` → `Count=1`, `item=[main,confirm]`, 0 coincidencias; con `-Command "& … -Only main,confirm"` → `Count=2`, 2 coincidencias; con `-Only main,confirmm` → 1 coincidencia y ningún error. Los puntos (1) y (3) los observó el coordinador sobre el script real.
**Solución propuesta:** normalizar al entrar: `$Only = @($Only | ForEach-Object { $_ -split ',' } | ForEach-Object { $_.Trim() } | Where-Object { $_ })`; validar cada nombre contra la lista de tomas y abortar nombrando los desconocidos; fijar `[Console]::OutputEncoding = [System.Text.UTF8Encoding]::new($false)` al inicio (restaurándolo en el `finally`). Lo tercero aplica igual a `release.ps1` y `build-installer.ps1`, cuyos mensajes también llevan acentos.
**Criterio de aceptación:** las dos formas de invocación producen las mismas tomas; `-Only main,noexiste` aborta nombrando `noexiste`; la salida capturada conserva los acentos.

### P-12-14 · `new-selfsigned-cert.ps1` sigue recibiendo la contraseña del `.pfx` como `[string]` en la línea de comandos
**Área:** DevOps · **Severidad:** Bajo · **Confianza:** Alta · **Verificación:** estática · **Esfuerzo:** Bajo
**Ubicación:** `src/FormatDiskPro/installer/new-selfsigned-cert.ps1:41` (`[string]$Password`) · `:34` (ejemplo `-Password "secreta"`) · `:66`
**Estado:** nuevo: es el patrón que `T3-09` corrigió en `build-installer.ps1` y `release.ps1`, en el tercer script, que `T3-09` no listaba (0 menciones del archivo en `ROADMAP.md`).
**Problema:** la contraseña con la que se exporta la clave privada queda en el historial de PSReadLine y en la línea de comandos del proceso; el ejemplo de la ayuda invita a escribirla así.
**Impacto:** bajo: certificado autofirmado de pruebas y firma descartada. Importa por coherencia con el propio criterio del proyecto y porque `-Trust` instala ese certificado como raíz de confianza del equipo.
**Solución propuesta:** `[SecureString]$Password` (con `Read-Host -AsSecureString` si falta) y pasarla tal cual a `Export-PfxCertificate`; corregir el ejemplo.
**Criterio de aceptación:** ningún script del repo declara una contraseña como `[string]`.

### P-12-15 · Residuos de las trampas de PowerShell 5.1 en el corte: sin `Set-StrictMode` y dos redirecciones de stderr sin proteger
**Área:** DevOps · **Severidad:** Bajo · **Confianza:** Alta · **Verificación:** estática · **Esfuerzo:** Bajo
**Ubicación:** `release.ps1:95` y `build-installer.ps1:52` (solo `$ErrorActionPreference`) · `release.ps1:418` (`& git rev-parse … *> $null`) · `:426` (`& git ls-remote --tags origin $tag 2>$null`) frente a la nota de `:825-826`
**Estado:** nuevo (0 menciones de `StrictMode` en `ROADMAP.md`/`CONTEXT.md`).
**Problema:** el script que publica no activa `Set-StrictMode -Version Latest` (sí lo hace `capture-screenshots.ps1:68`): una variable mal escrita vale `$null` en silencio. Y las líneas 418 y 426 redirigen stderr de un exe nativo con `Stop` activo, justo lo que el comentario de `:825` describe como generador de `NativeCommandError` y que más abajo se evita bajando la preferencia: si `origin` no responde o el directorio no es un repositorio, el corte muere con el error crudo de PowerShell en vez de con su mensaje (`:419`), y el código de salida de `ls-remote` no se mira.
**Impacto:** falla hacia el lado seguro (antes de mutar nada); el coste es de diagnóstico. En PowerShell 7 el efecto es el contrario: un `ls-remote` fallido pasaría por «el tag no existe en origin».
**Solución propuesta:** `Set-StrictMode -Version Latest` en ambos scripts (revisando los accesos a propiedades XML opcionales), y una función `Invoke-Native` hermana de `Invoke-Git` para las llamadas de solo lectura que necesitan silenciar stderr, comprobando `$LASTEXITCODE`.
**Criterio de aceptación:** sin red, `release.ps1 -DryRun` aborta con un mensaje propio que nombra a `origin`.

### P-12-16 · `[InstallDelete] {app}\*` sigue pudiendo vaciar una carpeta ajena si el destino se da con `/DIR=`
**Área:** DevOps · **Severidad:** Bajo · **Confianza:** Media · **Verificación:** estática · **Esfuerzo:** Bajo
**Ubicación:** `src/FormatDiskPro/installer/installer.iss:54` (`DisableDirPage=yes`) · `:91` (`Type: filesandordirs; Name: "{app}\*"`)
**Estado:** ya conocido (`T9-17`) — cierre incompleto. El criterio de aceptación de `T9-17` era «instalar sobre un directorio con un archivo ajeno no lo borra, **o el usuario no puede elegir ese directorio**».
**Problema:** `DisableDirPage=yes` oculta la página, pero el parámetro estándar `/DIR="x:\ruta"` de Inno Setup sigue fijando el destino desde la línea de comandos; el borrado previo de `{app}\*` se ejecuta igual.
**Impacto:** un despliegue por script (`setup.exe /VERYSILENT /DIR="D:\Herramientas"`) vacía esa carpeta sin aviso. Caso raro y deliberado por parte de quien lo lanza, pero es pérdida de datos silenciosa.
**Evidencia:** estática. Lo confirmaría instalar en una máquina virtual con `/DIR=` apuntando a una carpeta con un archivo ajeno.
**Solución propuesta:** en `[Code]`, `PrepareToInstall` que aborte si `{app}` existe, no está vacío y no contiene `FormatDiskPro.exe`; el borrado vuelve a ser «terreno propio» por comprobación y no por suposición.
**Criterio de aceptación:** con `/DIR=` a una carpeta ajena no vacía, el instalador se niega y no borra nada.

### P-12-17 · No se genera SBOM ni queda registro de lo que cada instalador empaqueta
**Área:** DevOps · **Severidad:** Bajo · **Confianza:** Alta · **Verificación:** estática · **Esfuerzo:** Medio
**Ubicación:** `release.ps1:847` (assets: solo `.exe` y `.sha256`) · `build-installer.ps1:183-227`
**Estado:** nuevo (0 menciones de SBOM).
**Problema:** el instalador lleva el runtime de .NET y el Windows App SDK completos (más de 500 archivos, 214,6 MB según `ROADMAP.md`), y ni el release ni el repositorio dicen qué versiones exactas van dentro.
**Impacto:** ante un aviso de seguridad del runtime o del SDK no se puede responder «qué versiones de FormatDiskPro están afectadas» sin instalar cada una.
**Solución propuesta:** generar un SBOM (CycloneDX o SPDX) a partir del publish y adjuntarlo como tercer asset; como mínimo, una línea en las notas del release con SDK, runtime y Windows App SDK.
**Criterio de aceptación:** cada release publica, en un asset o en sus notas, las versiones de runtime y SDK que contiene.

### P-12-18 · El comando de cobertura que recomienda `CONTRIBUTING.md` deja archivos sin rastrear que hacen abortar el siguiente corte
**Área:** DevOps · **Severidad:** Bajo · **Confianza:** Alta · **Verificación:** estática · **Esfuerzo:** Bajo
**Ubicación:** `.github/CONTRIBUTING.md:24` (`dotnet test --collect:"XPlat Code Coverage"`) · `.gitignore` (sin `TestResults/` ni `*.trx`) · `release.ps1:484-487`
**Estado:** nuevo.
**Problema:** sin `--results-directory`, `dotnet test` escribe en `tests/FormatDiskPro.Tests/TestResults/<guid>/`, dentro del repo y sin ignorar. La guarda de `T9-01` aborta después el corte por «archivos nuevos sin rastrear». El propio `release.ps1` lo evita midiendo en `%TEMP%` (`:503`) y lo explica en `:249-250`; la guía no.
**Solución propuesta:** añadir `TestResults/` y `*.trx` a `.gitignore`.
**Criterio de aceptación:** tras ejecutar el comando de la guía, `git status --porcelain` no lista nada nuevo.

### P-12-19 · `build-installer.ps1 -Version` etiqueta el instalador pero no el binario; los valores por defecto de `installer.iss` están obsoletos
**Área:** DevOps · **Severidad:** Bajo · **Confianza:** Alta · **Verificación:** estática · **Esfuerzo:** Bajo
**Ubicación:** `src/FormatDiskPro/installer/build-installer.ps1:183-190` (sin `-p:Version`) y `:204` · `installer.iss:3`, `:20-22` (`MyAppVersion "1.1.0"`), `:25-28` (`PublishDir` dentro del repo)
**Estado:** nuevo.
**Problema:** usado suelto con una versión distinta de la del `.csproj` (uso que su ayuda documenta), produce `FormatDiskPro-X-setup.exe` cuyo `.exe` interior declara otra versión; como la app compara su versión de ensamblado con el tag del release, ese instalador se ofrecería a sí mismo como actualización indefinidamente. Además, la cabecera de `installer.iss` invita a compilar con `iscc installer.iss`, que usaría la versión 1.1.0 y una ruta de publish dentro del repo, justo la que la decisión de `MAX_PATH` descarta.
**Impacto:** nulo en el flujo de `release.ps1` (hace el bump antes); real si alguien compila a mano.
**Solución propuesta:** pasar `-p:Version=$Version` al publish o abortar si `-Version` difiere del `.csproj`; quitar los valores por defecto de `installer.iss` y fallar con `#error` si faltan `MyAppVersion` o `PublishDir`.
**Criterio de aceptación:** no se puede producir un instalador cuya versión difiera de la del ejecutable que contiene.

---

## Revisado sin problemas

- **Permisos y disparadores del CI:** `permissions: contents: read` global en los dos workflows y `security-events: write` solo en el job de CodeQL; `pull_request` (no `pull_request_target`); sin secretos; `persist-credentials: false` en ambos checkouts; `timeout-minutes` (20 y 30) y `concurrency` con cancelación en los dos.
- **Acciones fijadas por SHA completo**, con su versión en comentario (4 acciones). La correspondencia SHA↔etiqueta queda para la comprobación en GitHub.
- **Inyección en workflows:** las únicas expresiones interpoladas son contextos no controlables por un PR (`github.ref`, número de PR, resultados de pasos pasados por `env`).
- **Sintaxis de PowerShell 7 en scripts que corren en 5.1:** ninguna.
- **Codificación de lo que escribe el corte:** notas sin BOM (`release.ps1:682`), `.csproj` con BOM (`:759`), `.sha256` en ASCII sin salto final (`build-installer.ps1:219`), lectura de XML con `XmlDocument.Load`.
- **`GH_TOKEN`:** no se imprime; se limpia en el `finally` solo si lo puso el script (`release.ps1:842`, `:864`).
- **`.gitignore`:** excluye `*.pfx`, `*.p12`, `*.snk`, `*.cer`, `installer/Output/`, `publish/`, `artifacts/`.
- **`app.manifest`:** `requireAdministrator`, `PerMonitorV2`, `supportedOS` de Windows 10/11; coherente con las decisiones cerradas.
- **`build-installer.ps1:142`** lee el `.csproj` con `Get-Content`, contra la convención escrita; inocuo porque el archivo lleva BOM, solo se leen campos ASCII y no se reescribe.
- **Decisiones cerradas respetadas:** no se propone firmar, publicar en winget, ejecutar pruebas de UI en CI, cambiar el destino del publish ni tocar el `AppId`.

## Hipótesis por verificar

- **H-1 · `installer.iss` sin BOM y con acentos.** El archivo empieza por `3b 20 3d` y tiene 33 líneas no ASCII, entre ellas el mensaje `spanish.ConfirmDeleteUserData` (`:110`). Si la versión de Inno Setup instalada trata como ANSI un `.iss` sin BOM, la pregunta de la desinstalación saldría con los acentos rotos. Depende de la versión de ISCC. Lo confirma desinstalar la v1.29.0 y leer el diálogo, o las notas de versión de Inno Setup instalado.
- **H-2 · Carrera entre `AppMutex` y la actualización silenciosa.** La app lanza el instalador con `/VERYSILENT /NORESTART /AUTOUPDATE=1` (`UpdateService.cs:526`), sin `/SUPPRESSMSGBOXES`, y sale a continuación (`MainWindow.HelpAndUpdates.cs:242-243`). Si Setup comprueba `AppMutex` (`installer.iss:73`) antes de que el proceso termine, mostraría el aviso «la aplicación está en ejecución» en plena actualización silenciosa. El flujo se probó de extremo a extremo (#42), así que es una duda de robustez, no un fallo observado.
- **H-3 · Borrador huérfano tras un `gh release create` interrumpido.** Si `gh` crea el release como borrador, sube los assets y publica, un fallo a mitad dejaría un borrador con ese tag y `-ResumeRelease` volvería a fallar. Depende del comportamiento de la versión de `gh`.
- **H-4 · Salida capturada y exes nativos que no son git.** `Invoke-Git` protege a git; `dotnet`, ISCC y `gh release create` se invocan directos. Con la salida del corte capturada, una línea de stderr de `gh` después del push abortaría el script aunque el release se hubiera creado.
- **H-5 · `ArchitecturesAllowed=x64compatible`** permite instalar en Windows 11 ARM64 bajo emulación; el README dice «x64». No hay indicio de que se haya probado.

## Cosas a comprobar en ejecución

- **En `release.ps1 -DryRun`** (sin `-UiTests`): que aborta o no con un commit de más en `origin/master` (P-12-01); que el plan no menciona ISCC ni `gh` (P-12-05); que no lista ninguna unitaria omitida (P-12-07); con `FORMATDISKPRO_ALLOW_YANK=1` y `FORMATDISKPRO_VERIFY_DRIVE=X:` definidas, que no lo rechaza ni lo anuncia (P-12-08); que compila en Debug (ruta `bin\Debug` en la salida) (P-12-04).
- **En GitHub, con `gh` en solo lectura:** `gh api repos/actions/checkout/git/ref/tags/v7.0.1` (y las otras tres) para confirmar que cada SHA fijado es el de su etiqueta; `gh release list` para ver si hay borradores; `gh api repos/xfiberex/FormatDiskPro/git/ref/tags/v1.29.0` frente a `git merge-base --is-ancestor v1.29.0 origin/master`; `gh pr view 2` para ver qué acciones cambia el PR abierto.
- **En el publish (`%TEMP%\FormatDiskPro-publish`):** versión del runtime empaquetado (`FormatDiskPro.runtimeconfig.json` / `.deps.json`) frente a la última de .NET 10 (P-12-03, P-12-17); `ProductVersion` de `FormatDiskPro.dll` (si lleva `+<sha>`, es el del commit **anterior** al del tag, porque se compila antes de commitear).
- **Instalador en máquina virtual:** `/DIR=` a una carpeta con contenido (P-12-16); desinstalación en español para leer el mensaje con acentos (H-1); actualización silenciosa con la app abierta (H-2); instalación en Windows 10 anterior a 2004 (P-12-09).
- **`tools/capture-screenshots.ps1`:** `-Only` con `-File` y con un nombre inexistente (P-12-13).

## Puntos fuertes

- `release.ps1` falla hacia el lado seguro en casi todo lo que se puede validar antes de mutar, y documenta **por qué** existe cada guarda con el incidente que la originó; es raro encontrar un script de publicación con esa trazabilidad.
- La puerta de cobertura distingue cuatro causas de fallo, conserva las pruebas fuera del repo y se niega a reintentar para no ocultar un fallo intermitente (`T10-01`).
- El `.sha256` se genera después de firmar, se exige como asset y la app lo empareja por nombre y lo lee con tope de tamaño: el contrato entre el corte y el actualizador está cerrado por los dos lados.
- Los workflows son mínimos y están bien acotados: permisos de solo lectura, acciones por SHA, `persist-credentials: false`, resumen que dice lo que **no** se ejecutó.
- `capture-screenshots.ps1` respalda y restaura `settings.json`, comprueba que la ventana es la de la app y sale con código distinto de cero si la galería queda incompleta.

## Para otras áreas

- **Seguridad (área 2):** `UpdateService.cs:217-227` descarga el instalador a `%TEMP%\FormatDiskPro_update`, lo verifica (`:206`) y lo ejecuta después por ruta (`:523-528`) desde un proceso elevado; entre la verificación y el lanzamiento el archivo está en una carpeta escribible por procesos no elevados del mismo usuario (ventana TOCTOU). Y `OpenUrl(rel.HtmlUrl)` (`MainWindow.HelpAndUpdates.cs:163`) pasa a `ShellExecute`, desde un proceso elevado, una cadena que viene del JSON de la red sin comprobar que sea `https://`.
- **Documentación (área 11):** `README.md:190` y `installer.iss:11`, `:25`, `:66` siguen diciendo «Windows App SDK 1.8»; `CONTEXT.md` §3 dice 626 unitarias (la línea base mide 690).
- **UX / documentación:** la captura `docs/screenshots/history-light.png` del README enseña una entrada `CRASH: Microsoft.UI.Xaml.Markup.XamlParseException…` de la propia app.
- **QA (área 10):** `T10-02` (causa del informe de cobertura vacío) sigue abierta y bloqueada a propósito; no se reporta aquí.
