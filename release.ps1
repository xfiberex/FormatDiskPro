<#
.SYNOPSIS
    Corta una versión de FormatDiskPro de principio a fin.

.DESCRIPTION
    Flujo completo en un paso:
      1. Valida la versión y el árbol de trabajo.
      2. Ejecuta las pruebas (salvo -SkipTests).
      3. Actualiza <Version> en el .csproj si cambió.
      4. Compila el instalador (publish self-contained + Inno Setup).
      5. Commit del bump de versión + tag anotado vX.Y.Z.
      6. Push de la rama y el tag a origin.
      7. Crea el GitHub Release adjuntando el instalador Y su .sha256.

    Para 'gh' reutiliza la credencial de GitHub ya cacheada (la del push) si no
    estuviera autenticado; nunca se imprime el token.

    IMPORTANTE: el asset .sha256 es OBLIGATORIO mientras se publique sin firmar. Desde la v1.15.0 la app
    verifica el instalador descargado antes de ejecutarlo como administrador
    (Services/UpdateService.VerifyInstallerAsync): firma Authenticode válida si la hay; si no, el hash
    SHA-256 publicado como asset. Sin ninguna de las dos, la app borra el instalador y la
    auto-actualización falla.

    Firmar (-CertThumbprint/-CertFile) sigue siendo lo deseable: evita el aviso de SmartScreen
    ("editor desconocido") y es una garantía más fuerte que el hash.

.PARAMETER Version
    Versión a publicar (X.Y.Z). Si se omite, usa la del .csproj.

.PARAMETER NotesFile
    Ruta a un archivo Markdown con las notas del release. Si se omite, las notas se toman de la sección
    del CHANGELOG de esa versión (que este script ya exige que exista), más el pie con el instalador y el
    .sha256. Antes se generaba una plantilla genérica que no contaba ningún cambio, y así salió la v1.24.0.

.PARAMETER SkipTests
    Omite la ejecución de pruebas (unitarias y de UI) — y con ellas, la medición de cobertura.

.PARAMETER UiTests
    Ejecuta también los UI tests (FlaUI/UIA3), que conducen la app REAL. No van en la solución, así que
    `dotnet test` no los toca: hay que pedirlos. Requieren TERMINAL ELEVADA (la app es
    requireAdministrator y un proceso no elevado no puede automatizar su ventana), y el script lo valida
    antes de correr nada. Los 6 tests que necesitan la USB física de pruebas se OMITEN solos si no está
    conectada (no fallan), y el que borra datos de verdad se omite salvo FORMATDISKPRO_ALLOW_DESTRUCTIVE=1
    — que este script RECHAZA: un corte de release nunca debe formatear una unidad.

    Al terminar —y otra vez en el resumen final— el script dice cuántos UI tests se OMITIERON y por qué,
    leyéndolo del .trx de la corrida: "omitido" y "correcto" se distinguen mal en la salida de dotnet test,
    y por eso los cortes de la v1.15.2 y la v1.16.0 salieron en verde con un test roto que estaba omitido.

    Sin este flag, el script avisa de que el release sale sin haber ejercido la app real.

.PARAMETER AllowDirty
    Permite continuar con cambios sin commitear en el árbol de trabajo.

.PARAMETER DryRun
    Valida y muestra el plan, pero no modifica nada (ni build, ni git, ni GitHub).

.EXAMPLE
    .\release.ps1 -Version 1.2.0
    .\release.ps1 -Version 1.2.0 -UiTests   # recomendado: ejerce también la app real
    .\release.ps1 -Version 1.2.0 -DryRun
    .\release.ps1 -Version 1.2.0 -NotesFile notas.md
#>
[CmdletBinding()]
param(
    [string]$Version,
    [string]$NotesFile,
    [switch]$SkipTests,
    [switch]$UiTests,
    [switch]$AllowDirty,
    [switch]$DryRun,
    # Firma de código (opcional): se reenvían a build-installer.ps1.
    [string]$CertThumbprint,
    [string]$CertFile,
    # SecureString y no [string] (T3-09): en claro quedaba en el historial de PowerShell y en la línea de
    # comandos del proceso. También se puede dar por FORMATDISKPRO_CERT_PASSWORD; ver build-installer.ps1.
    [SecureString]$CertPassword,
    [string]$TimestampUrl
)

$ErrorActionPreference = "Stop"

# Mínimo de cobertura de LÍNEA exigido a Core/ (la lógica pura). El 2026-08-15, al medirla por primera
# vez, estaba en 97.1%: el umbral se pone por debajo a propósito, para que sea un suelo que avise de una
# regresión real y no un número que obligue a escribir pruebas de relleno para no romper el corte.
# Subirlo es una decisión deliberada; bajarlo, un síntoma.
$CoreCoverageThreshold = 90

function Info($m)  { Write-Host "==> $m" -ForegroundColor Cyan }
function Ok($m)    { Write-Host "[OK] $m" -ForegroundColor Green }
function Warn($m)  { Write-Host "[!] $m" -ForegroundColor Yellow }
function Die($m)   { Write-Host "[X] $m" -ForegroundColor Red; exit 1 }

<#
.SYNOPSIS
    Ejecuta git de forma segura cuando la salida del script está redirigida. Devuelve el código de salida.

.DESCRIPTION
    git escribe por stderr en su operación NORMAL, sin que nada haya fallado: el resumen del push
    ("To https://github.com/..."), los avisos de finales de línea ("LF will be replaced by CRLF")...

    Ejecutando el script de forma normal eso es inocuo: stderr va a la consola y sigue adelante. PERO si
    alguien captura la salida —`.\release.ps1 ... | Tee-Object release.log`, un `2>&1 |`, un wrapper que
    recoja la salida—, Windows PowerShell 5.1 convierte cada línea de stderr de un exe nativo en un
    NativeCommandError y, con $ErrorActionPreference = "Stop", ABORTA el script aunque git haya devuelto 0.

    En un `git push` eso es especialmente malo: el script muere DESPUÉS de haber empujado la rama, y deja
    el release a medias (rama subida, sin tag ni GitHub Release). Ocurrió al cortar la v1.15.0 (2026-07-12),
    precisamente por lanzarlo con la salida filtrada.

    Aquí se baja la preferencia solo mientras corre git y se decide por $LASTEXITCODE, que es el único
    indicador fiable de si git falló. La salida se sigue mostrando, atenuada.
#>
function Invoke-Git {
    $eap = $ErrorActionPreference
    $ErrorActionPreference = "Continue"
    try {
        & git @args 2>&1 | ForEach-Object { Write-Host "    $_" -ForegroundColor DarkGray }
        return $LASTEXITCODE
    }
    finally { $ErrorActionPreference = $eap }
}

<#
.SYNOPSIS
    Cobertura de línea de Core/ a partir del informe Cobertura que deja `--collect:"XPlat Code Coverage"`.

.DESCRIPTION
    "389 pruebas" es un recuento, no una medida: no dice qué parte del código se ejercita. Se mide SOLO
    Core/ —la lógica pura— a propósito: es la capa que puede probarse entera sin hardware, así que ahí un
    hueco es una decisión, no una limitación. Services/ y UI/ dependen de discos, procesos y ventanas: su
    red son los UI tests, y medirlos con la misma vara daría un número que invitaría a escribir pruebas
    fáciles de lo que no importa.

    Devuelve $null si no encuentra informe (el corte lo trata como error: pedir cobertura y no obtenerla
    no puede pasar por "correcto").
#>
function Get-CoreCoverage {
    param([Parameter(Mandatory)][string]$CoverageDir)

    $report = Get-ChildItem $CoverageDir -Recurse -Filter "coverage.cobertura.xml" -ErrorAction SilentlyContinue |
        Sort-Object LastWriteTimeUtc -Descending | Select-Object -First 1
    if (-not $report) { return $null }

    try {
        $xml = New-Object System.Xml.XmlDocument
        $xml.Load($report.FullName)
    } catch { return $null }

    # El informe identifica cada clase por su archivo fuente: es lo que permite separar Core/ del resto.
    $classes = @($xml.SelectNodes("//class") | Where-Object { $_.filename -match '\\Core\\' })
    if ($classes.Count -eq 0) { return $null }

    $covered = 0; $total = 0
    $perClass = foreach ($c in $classes) {
        $ct = 0; $cc = 0
        foreach ($l in $c.lines.line) { $ct++; if ([int]$l.hits -gt 0) { $cc++ } }
        $covered += $cc; $total += $ct
        [pscustomobject]@{
            Name    = ($c.name -replace '^FormatDiskPro\.', '')
            Percent = if ($ct -gt 0) { [math]::Round(100 * $cc / $ct, 1) } else { 100 }
            Lines   = "$cc/$ct"
        }
    }

    [pscustomobject]@{
        Covered  = $covered
        Total    = $total
        Percent  = if ($total -gt 0) { [math]::Round(100 * $covered / $total, 1) } else { 0 }
        Report   = $report.FullName
        PerClass = @($perClass | Sort-Object Percent)
    }
}

<#
.SYNOPSIS
    Lee el .trx de una corrida de pruebas y devuelve cuántas pasaron y cuáles se OMITIERON (y por qué).

.DESCRIPTION
    Los tests de UI que necesitan una precondición ausente —la USB de pruebas, el opt-in destructivo— se
    OMITEN en vez de fallar (ver TestDriveFactAttribute). Ese diseño es el correcto: un corte de release no
    debe caer por falta de hardware. Pero en el resumen de `dotnet test` "omitido" y "correcto" se
    distinguen mal, y eso ya costó caro: el 2026-08-13, al conectar por fin la USB, apareció que
    CheckDisk_ScanOnly_CompletesForTestDrive llevaba ROTO desde la v1.15.2 — y los cortes de la v1.15.2 y
    la v1.16.0 habían salido "en verde" con él omitido.

    Omitir sigue siendo lo correcto; lo que faltaba era dejar rastro de qué cobertura se sacrificó. Por eso
    se pide el logger trx: la salida de consola no lista los tests omitidos ni su motivo, el .trx sí.

    Se usa $xml.Load (no [xml](Get-Content -Raw)): en PS 5.1, Get-Content sin -Encoding lee con la página de
    códigos ANSI y destroza los acentos de los motivos de omisión, que están en español. Load respeta la
    declaración del XML.
#>
function Get-TestRunSummary {
    param([Parameter(Mandatory)][string]$TrxPath)

    if (-not (Test-Path $TrxPath)) { return $null }
    try {
        $xml = New-Object System.Xml.XmlDocument
        $xml.Load($TrxPath)
    } catch { return $null }

    $results = @($xml.TestRun.Results.UnitTestResult)
    if ($results.Count -eq 0) { return $null }

    $skipped = @($results | Where-Object { $_.outcome -eq 'NotExecuted' })
    [pscustomobject]@{
        Total   = $results.Count
        Passed  = @($results | Where-Object { $_.outcome -eq 'Passed' }).Count
        Skipped = $skipped.Count
        SkippedDetail = @($skipped | ForEach-Object {
            $reason = $_.Output.ErrorInfo.Message
            [pscustomobject]@{
                Name   = $_.testName
                Reason = if ($reason) { ($reason -replace '\s+', ' ').Trim() } else { "sin motivo declarado" }
            }
        })
    }
}

<#
.SYNOPSIS
    Imprime qué cobertura de UI llevó realmente este corte. Se muestra dos veces: al terminar las pruebas y
    en el resumen final, que es donde se mira cuando ya todo ha salido bien.
#>
function Show-UiTestCoverage {
    param($Summary)

    if (-not $Summary) {
        if ($SkipTests)   { Warn "UI tests: NO ejecutados (-SkipTests). Este corte no ha ejercido la app real."; return }
        if (-not $UiTests) { Warn "UI tests: NO ejecutados (sin -UiTests). Este corte no ha ejercido la app real."; return }
        Warn "UI tests: ejecutados y correctos, pero no se pudo leer el informe (.trx): se desconoce cuántos se omitieron."
        return
    }

    if ($Summary.Skipped -gt 0) {
        Warn "UI tests: $($Summary.Passed)/$($Summary.Total) — $($Summary.Skipped) OMITIDOS por precondición ausente:"
        $Summary.SkippedDetail | ForEach-Object {
            Write-Host "      - $($_.Name): $($_.Reason)" -ForegroundColor DarkGray
        }
        # El consejo tiene que corresponder al motivo real: con la USB ya conectada, "conecta la USB"
        # es ruido y hace dudar de si el resumen se ha enterado de algo.
        $usbPending = @($Summary.SkippedDetail | Where-Object { $_.Reason -like "*USB de pruebas conectada*" }).Count
        if ($usbPending -gt 0) {
            Warn "Esa es la cobertura que este corte NO ejerció. Conecta la USB de pruebas ('utilidades') para recuperar $usbPending de ellos."
        } else {
            Warn "Esa es la cobertura que este corte NO ejerció. Todos los omitidos son opt-in (variable de entorno), no falta de hardware: un corte de release no debe ejecutarlos."
        }
    } else {
        Ok "UI tests: $($Summary.Passed)/$($Summary.Total) — 0 omitidos (cobertura completa sobre la app real)."
    }
}

# ── Rutas ──────────────────────────────────────────────────────────────────
$root          = $PSScriptRoot
$changelog     = Join-Path $root "CHANGELOG.md"
$csproj        = Join-Path $root "src\FormatDiskPro\FormatDiskPro.csproj"
$solution      = Join-Path $root "FormatDiskPro.slnx"
$buildScript   = Join-Path $root "src\FormatDiskPro\installer\build-installer.ps1"
$outputDir     = Join-Path $root "src\FormatDiskPro\installer\Output"
# Fuera de la solución a propósito (ver -UiTests): se lanza por ruta, solo si se pide.
$uiTestProject = Join-Path $root "tests\FormatDiskPro.UiTests\FormatDiskPro.UiTests.csproj"

if (-not (Test-Path $csproj))      { Die "No se encontró el proyecto: $csproj" }
if (-not (Test-Path $buildScript)) { Die "No se encontró el script de instalador: $buildScript" }
if ($UiTests -and -not (Test-Path $uiTestProject)) { Die "No se encontró el proyecto de UI tests: $uiTestProject" }

# Se rechaza en vez de ignorarse en silencio: quien pasa las dos cosas cree que su corte lleva la app real
# probada, y saldría sin ninguna prueba en absoluto.
if ($UiTests -and $SkipTests) { Die "-UiTests y -SkipTests se contradicen: -SkipTests omite TODAS las pruebas. Elige una." }

# ── Versión ────────────────────────────────────────────────────────────────
# OJO con la codificación: NO usar `Get-Content -Raw`. En PS 5.1 lee con la página de códigos ANSI del
# sistema, así que los bytes UTF-8 de un acento (é = C3 A9) se convierten en dos caracteres (Ã©) y, al
# reescribir el archivo como UTF-8 más abajo, la corrupción queda GRABADA. Como el bump de versión ocurre
# en CADA release, el daño se acumulaba capa sobre capa: <Authors>/<Copyright> del .csproj llevaban el
# nombre del autor destrozado tras 14 versiones, y esa basura acababa en las propiedades del .exe
# publicado. ReadAllText detecta el BOM y asume UTF-8 si no lo hay; se reescribe CONSERVANDO el BOM.
$csprojRaw = [System.IO.File]::ReadAllText($csproj)
$currentVersion = $null
if ($csprojRaw -match '<Version>(.*?)</Version>') { $currentVersion = $Matches[1] }

if (-not $Version) {
    if (-not $currentVersion) { Die "No hay <Version> en el .csproj y no se pasó -Version." }
    $Version = $currentVersion
}
if ($Version -notmatch '^\d+\.\d+\.\d+(\.\d+)?$') {
    Die "Versión inválida '$Version'. Usa el formato X.Y.Z (p. ej. 1.2.0)."
}
$tag = "v$Version"
Info "Versión a publicar: $Version  (tag $tag)"
if ($currentVersion -and $currentVersion -ne $Version) {
    Info "Bump de versión: $currentVersion -> $Version"
}

# ── CHANGELOG ──────────────────────────────────────────────────────────────
# Un CHANGELOG que se queda atrás es peor que no tenerlo: afirma ser el registro del proyecto y miente.
# El corte exige que la versión que se va a publicar YA tenga su sección, igual que exige el .sha256 y el
# mínimo de cobertura. Es lo único que impide que este archivo envejezca en silencio.
# El .* del final acepta la fecha con cualquier separador (guion normal o raya).
if (-not (Test-Path $changelog)) {
    Die "No se encontró CHANGELOG.md. Un corte no puede salir sin su registro de cambios."
}
$changelogRaw = [System.IO.File]::ReadAllText($changelog)
if ($changelogRaw -notmatch "(?m)^##\s*\[$([regex]::Escape($Version))\]") {
    Die @"
CHANGELOG.md no tiene una sección para la $Version.

Antes de cortar, mueve lo que haya bajo '## [Sin publicar]' a una sección nueva:

    ## [$Version] — $(Get-Date -Format 'yyyy-MM-dd')

y añade abajo su enlace:

    [$Version]: https://github.com/xfiberex/FormatDiskPro/releases/tag/$tag
"@
}
Ok "CHANGELOG.md tiene la sección de la $Version."

# ── Validaciones de git ──────────────────────────────────────────────────────
Push-Location $root
try {
    & git rev-parse --is-inside-work-tree *> $null
    if ($LASTEXITCODE -ne 0) { Die "Este directorio no es un repositorio git." }

    $branch = (& git rev-parse --abbrev-ref HEAD).Trim()
    Info "Rama: $branch"

    # ¿Tag ya existe? (local o remoto)
    $localTag  = (& git tag --list $tag)
    if ($localTag) { Die "El tag $tag ya existe localmente. Usa otra versión o bórralo antes." }
    $remoteTag = (& git ls-remote --tags origin $tag 2>$null)
    if ($remoteTag) { Die "El tag $tag ya existe en origin. Usa otra versión." }

    # ¿Hay archivos sin rastrear? (nuevos, no añadidos con git add)
    # Estos NO se incluirán en el commit del release — el usuario debe añadirlos explícitamente.
    $untracked = (& git status --porcelain) | Where-Object { $_ -match '^\?\?' }
    if ($untracked -and -not $AllowDirty) {
        Warn "Hay archivos nuevos sin rastrear (no se incluirán en el release):"
        $untracked | ForEach-Object { Write-Host "    $_" -ForegroundColor DarkGray }
        Die "Añade los archivos que necesites con 'git add <archivo>' y reintenta, o usa -AllowDirty para ignorarlos."
    } elseif ($untracked) {
        Warn "Archivos sin rastrear ignorados (-AllowDirty):"
        $untracked | ForEach-Object { Write-Host "    $_" -ForegroundColor DarkGray }
    }

    # ── Pruebas ──────────────────────────────────────────────────────────────
    $uiSummary = $null   # cobertura real de UI de este corte; se repite en el resumen final
    $coverage  = $null   # cobertura de línea de Core/ (T2-04)
    if ($SkipTests) {
        Warn "Pruebas omitidas (-SkipTests)."
    } else {
        # Se mide la cobertura en la misma pasada (T2-04): un corte no debe poder afirmar "389 pruebas"
        # sin saber qué parte de la lógica pura tocan.
        $covDir = Join-Path $env:TEMP "FormatDiskPro_coverage"
        if (Test-Path $covDir) { Remove-Item $covDir -Recurse -Force -ErrorAction SilentlyContinue }

        Info "Ejecutando pruebas unitarias (con cobertura)..."
        & dotnet test $solution --nologo --collect:"XPlat Code Coverage" --results-directory $covDir
        if ($LASTEXITCODE -ne 0) { Die "Las pruebas unitarias fallaron. Release abortado." }
        Ok "Pruebas unitarias correctas."

        $coverage = Get-CoreCoverage -CoverageDir $covDir
        if (-not $coverage) {
            Die "Se pidió cobertura y no se obtuvo informe. Un corte no puede salir sin saber qué cubre: revisa que coverlet.collector siga referenciado en el proyecto de pruebas."
        }
        if ($coverage.Percent -lt $CoreCoverageThreshold) {
            Warn "Clases de Core/ con menos cobertura:"
            $coverage.PerClass | Select-Object -First 5 | ForEach-Object {
                Write-Host "      - $($_.Name): $($_.Percent)% ($($_.Lines))" -ForegroundColor DarkGray
            }
            Die "Cobertura de Core/ $($coverage.Percent)% — por debajo del mínimo exigido ($CoreCoverageThreshold%). Release abortado."
        }
        Ok "Cobertura de Core/: $($coverage.Percent)% ($($coverage.Covered)/$($coverage.Total) líneas, mínimo $CoreCoverageThreshold%)."

        # ── UI tests (opcionales): los únicos que ejercen la app REAL ────────
        # No están en la solución a propósito: si lo estuvieran, el `dotnet test` de arriba los
        # arrastraría siempre, y necesitan condiciones que no toda máquina tiene (elevación, y la USB
        # de pruebas para 6 de ellos). Por eso se piden con -UiTests y se lanzan por ruta.
        if ($UiTests) {
            # 1. Elevación. La app es requireAdministrator: un proceso de pruebas NO elevado no puede
            #    automatizar su ventana por UI Automation. Sin esto, los 17 tests que sí corren fallarían
            #    todos a la vez con un error que no tiene nada que ver con el código.
            $identity = [Security.Principal.WindowsIdentity]::GetCurrent()
            $isAdmin = (New-Object Security.Principal.WindowsPrincipal($identity)).IsInRole(
                [Security.Principal.WindowsBuiltInRole]::Administrator)
            if (-not $isAdmin) {
                Die "-UiTests requiere una terminal ELEVADA: FormatDiskPro.exe exige administrador y un proceso no elevado no puede automatizar su ventana. Reabre PowerShell como administrador y reintenta."
            }

            # 2. Opt-in destructivo. Con la variable a 1, la suite FORMATEA de verdad la USB de pruebas.
            #    Un corte de release jamás debe hacer eso, ni aunque quien lo lanza la tuviera activada de
            #    una sesión anterior de depuración.
            if ($env:FORMATDISKPRO_ALLOW_DESTRUCTIVE -eq "1") {
                Die "FORMATDISKPRO_ALLOW_DESTRUCTIVE=1 está activa: la suite formatearía la USB de pruebas. Un corte de release nunca debe hacerlo. Limpia la variable (`$env:FORMATDISKPRO_ALLOW_DESTRUCTIVE = `$null) y reintenta."
            }

            # 3. La USB de pruebas es OPCIONAL: los tests que la necesitan se omiten solos si no está
            #    (ver TestDriveFactAttribute). Se avisa para que quede claro qué cobertura llevó el corte.
            $testUsb = [System.IO.DriveInfo]::GetDrives() | Where-Object {
                $_.DriveType -eq 'Removable' -and $_.IsReady -and $_.VolumeLabel -eq 'utilidades'
            } | Select-Object -First 1
            if ($testUsb) {
                Info "USB de pruebas detectada ($($testUsb.Name)): los tests de diagnóstico también correrán."
            } else {
                Warn "USB de pruebas ('utilidades') NO conectada: sus tests se OMITIRÁN (no fallarán). El resto sí ejerce la app real."
            }

            # El .trx es lo que permite decir CUÁNTOS se omitieron y por qué (ver Get-TestRunSummary): la
            # salida de consola no lo lista, y sin eso un corte no deja rastro de la cobertura que sacrificó.
            $trxDir  = Join-Path $env:TEMP "FormatDiskPro_uitests"
            $trxName = "uitests-$Version.trx"
            $trxPath = Join-Path $trxDir $trxName
            if (Test-Path $trxPath) { Remove-Item $trxPath -Force -ErrorAction SilentlyContinue }

            Info "Ejecutando UI tests (conducen la app real; se abrirán ventanas)..."
            & dotnet test $uiTestProject --filter "Category!=Slow" --nologo `
                --logger "trx;LogFileName=$trxName" --results-directory $trxDir
            if ($LASTEXITCODE -ne 0) { Die "Los UI tests fallaron. Release abortado." }
            Ok "UI tests correctos."

            $uiSummary = Get-TestRunSummary -TrxPath $trxPath
            Show-UiTestCoverage $uiSummary
        } else {
            Warn "UI tests NO ejecutados (sin -UiTests): este release sale sin haber ejercido la app real. Recomendado: .\release.ps1 -Version $Version -UiTests (desde una terminal elevada)."
        }
    }

    # ── Notas del release ──────────────────────────────────────────────────────
    # Sin -NotesFile, las notas salen del CHANGELOG. Antes salía una PLANTILLA GENÉRICA —"Instalador
    # self-contained para Windows x64…" y nada más—, y así se publicó la v1.24.0: el corte fue impecable y
    # el release no contaba ni uno de sus cambios. Es un fallo que no avisa, porque el script termina en
    # verde. La sección del CHANGELOG es OBLIGATORIA aquí arriba, así que ya está escrita y revisada: usarla
    # convierte "olvidarse las notas" en algo que no puede pasar.
    #
    # -NotesFile sigue existiendo y sigue mandando: el CHANGELOG es un registro por versión y unas notas de
    # publicación pueden querer contar lo mismo de otra forma.
    $notesPath = $NotesFile
    $tempNotes = $null
    if (-not $notesPath) {
        # De "## [X.Y.Z]" hasta el siguiente "## " (la siguiente versión) o el final.
        $lines = $changelogRaw -split "`r?`n"
        $start = -1; $end = $lines.Count
        for ($i = 0; $i -lt $lines.Count; $i++) {
            if ($lines[$i] -match "^##\s*\[$([regex]::Escape($Version))\]") { $start = $i + 1; continue }
            if ($start -ge 0 -and $lines[$i] -match '^##\s') { $end = $i; break }
        }
        $body = if ($start -ge 0) { ($lines[$start..($end - 1)] -join "`n").Trim().TrimEnd('-').Trim() } else { "" }
        if (-not $body) {
            Die "No se pudo extraer la sección de la $Version del CHANGELOG. Revísala o pasa -NotesFile."
        }

        $notes = @"
## FormatDiskPro v$Version

$body

---

Instalador self-contained para Windows x64 (no requiere instalar .NET).

Descarga ``FormatDiskPro-$Version-setup.exe`` y ejecútalo (requiere privilegios de administrador).

El asset ``FormatDiskPro-$Version-setup.exe.sha256`` es el hash con el que la app verifica la descarga antes de ejecutarla.
"@

        # SIN BOM ($false). Out-File -Encoding utf8 en PS 5.1 lo pone, y ese BOM viaja hasta el cuerpo del
        # release en GitHub: el diálogo de *Novedades* enseñaba entonces "## FormatDiskPro v1.24.0" con las
        # almohadillas a la vista, porque el marcador de encabezado ya no estaba al principio de la línea.
        $tempNotes = Join-Path $env:TEMP "fdp_release_$Version.md"
        [System.IO.File]::WriteAllText($tempNotes, $notes, (New-Object System.Text.UTF8Encoding($false)))
        $notesPath = $tempNotes
        Ok "Notas tomadas del CHANGELOG ($($body.Length) caracteres)."
    }
    if (-not (Test-Path $notesPath)) { Die "No se encontró el archivo de notas: $notesPath" }

    # ── DRY RUN: mostrar plan y salir ────────────────────────────────────────
    if ($DryRun) {
        Write-Host ""
        Warn "DRY RUN — no se modificará nada. Plan:"
        $signNote = if ($CertThumbprint -or $CertFile) { " (firmando con Authenticode)" } else { " (SIN firmar — la app verificará por el .sha256)" }
        Write-Host "    1. Actualizar <Version> a $Version en el .csproj" -ForegroundColor DarkGray
        Write-Host "    2. build-installer.ps1 -Version $Version$signNote" -ForegroundColor DarkGray
        Write-Host "    3. git add -u  (todos los archivos rastreados modificados)" -ForegroundColor DarkGray
        Write-Host "       git commit -m 'release: v$Version'" -ForegroundColor DarkGray
        Write-Host "       git tag -a $tag" -ForegroundColor DarkGray
        Write-Host "    4. git push origin $branch" -ForegroundColor DarkGray
        Write-Host "       git push origin $tag" -ForegroundColor DarkGray
        Write-Host "    5. gh release create $tag (assets: FormatDiskPro-$Version-setup.exe + .sha256)" -ForegroundColor DarkGray
        $notesOrigin = if ($NotesFile) { "-NotesFile $NotesFile" } else { "la sección [$Version] del CHANGELOG" }
        Write-Host "       Notas del release: $notesOrigin" -ForegroundColor DarkGray
        if (-not $SkipTests) {
            $uiNote = if ($UiTests) { "unitarias + UI tests (app real)" } else { "solo unitarias (sin -UiTests)" }
            Write-Host "    Pruebas ya ejecutadas en este dry run: $uiNote" -ForegroundColor DarkGray
            if ($coverage) {
                Write-Host "    Cobertura de Core/: $($coverage.Percent)% (mínimo $CoreCoverageThreshold%)" -ForegroundColor DarkGray
            }
        }
        Show-UiTestCoverage $uiSummary
        if ($tempNotes) { Remove-Item $tempNotes -Force -ErrorAction SilentlyContinue }
        Ok "Dry run completado."
        return
    }

    # ── 1. Bump de versión ───────────────────────────────────────────────────
    if ($currentVersion -ne $Version) {
        Info "Actualizando <Version> en el .csproj..."
        $newRaw = $csprojRaw -replace '<Version>.*?</Version>', "<Version>$Version</Version>"
        # CON BOM ($true), no sin él: es lo que hace que la próxima lectura —la del siguiente release, o la
        # de MSBuild— sepa con certeza que el archivo es UTF-8. Sin BOM, PS 5.1 y MSBuild caen en la página
        # de códigos ANSI y vuelven a romper los acentos de <Authors>/<Copyright>. Ver la nota de arriba.
        [System.IO.File]::WriteAllText($csproj, $newRaw, (New-Object System.Text.UTF8Encoding($true)))
    }

    # ── 2. Compilar instalador ─────────────────────────────────────────────────
    Info "Compilando el instalador..."
    $buildArgs = @{ Version = $Version }
    if ($CertThumbprint) { $buildArgs.CertThumbprint = $CertThumbprint }
    if ($CertFile)       { $buildArgs.CertFile       = $CertFile }
    if ($CertPassword)   { $buildArgs.CertPassword   = $CertPassword }
    if ($TimestampUrl)   { $buildArgs.TimestampUrl   = $TimestampUrl }
    & $buildScript @buildArgs
    if ($LASTEXITCODE -ne 0) { Die "La compilación del instalador falló." }
    $setup = Join-Path $outputDir "FormatDiskPro-$Version-setup.exe"
    if (-not (Test-Path $setup)) { Die "No se encontró el instalador esperado: $setup" }
    $sizeMB = [math]::Round((Get-Item $setup).Length / 1MB, 1)
    Ok "Instalador: $setup ($sizeMB MB)"

    # Lo genera build-installer.ps1. Es con lo que la app verifica la descarga mientras los instaladores
    # se publiquen sin firmar (UpdateService.VerifyInstallerAsync): si no se sube como asset, la
    # auto-actualización no puede verificar nada, borra el instalador y falla.
    $setupHash = "$setup.sha256"
    if (-not (Test-Path $setupHash)) { Die "No se encontró el checksum esperado: $setupHash" }
    Ok "Checksum: $setupHash"

    # ── 3. Commit + tag ──────────────────────────────────────────────────────
    # Añade todos los archivos rastreados modificados/eliminados (tracked changes).
    # Los archivos nuevos sin rastrear requieren 'git add' manual previo.
    Info "Preparando commit de release..."
    if ((Invoke-Git add -u) -ne 0) { Die "git add -u falló." }
    $staged = (& git diff --cached --name-only)
    if ($staged) {
        Info "Archivos incluidos en el commit:"
        $staged | ForEach-Object { Write-Host "    $_" -ForegroundColor DarkGray }
        if ((Invoke-Git commit -m "release: v$Version") -ne 0) { Die "git commit falló." }
        Ok "Commit de release creado."
    } else {
        Info "Sin cambios que commitear; se etiqueta el HEAD actual."
    }
    Info "Creando tag $tag..."
    if ((Invoke-Git tag -a $tag -m "FormatDiskPro $tag") -ne 0) { Die "git tag falló." }

    # ── 4. Push ──────────────────────────────────────────────────────────────
    # Vía Invoke-Git a propósito: git escribe el resumen del push por stderr y, con
    # $ErrorActionPreference = "Stop", eso abortaría el script DESPUÉS de haber empujado la rama,
    # dejando el release a medias (sin tag ni GitHub Release). Ver la nota de Invoke-Git.
    Info "Push de la rama y el tag a origin..."
    if ((Invoke-Git push origin $branch) -ne 0) { Die "git push de la rama falló." }
    if ((Invoke-Git push origin $tag) -ne 0) { Die "git push del tag falló. La rama YA está subida; reintenta." }
    Ok "Rama y tag publicados."

    # ── 5. GitHub Release ────────────────────────────────────────────────────
    $gh = @(
        "C:\Program Files\GitHub CLI\gh.exe",
        "C:\Program Files (x86)\GitHub CLI\gh.exe"
    ) | Where-Object { Test-Path $_ } | Select-Object -First 1
    if (-not $gh) {
        $cmd = Get-Command gh -ErrorAction SilentlyContinue
        if ($cmd) { $gh = $cmd.Source }
    }
    if (-not $gh) { Die "gh (GitHub CLI) no está instalado. Instálalo: winget install GitHub.cli  — el tag YA está publicado; crea el release manualmente o reintenta." }

    # Asegurar autenticación: si gh no está logueado, reutilizar la credencial cacheada de git.
    # PS 5.1: 2>$null en exes nativos con ErrorActionPreference=Stop genera NativeCommandError;
    # se baja a SilentlyContinue solo durante las llamadas que necesitan suprimir stderr.
    $eap = $ErrorActionPreference
    $ErrorActionPreference = "SilentlyContinue"
    & $gh auth status 2>$null
    $authOk = $LASTEXITCODE -eq 0
    $ErrorActionPreference = $eap

    if (-not $authOk) {
        Warn "gh no autenticado; reutilizando la credencial de git cacheada (local, no se muestra)."
        $eap = $ErrorActionPreference
        $ErrorActionPreference = "SilentlyContinue"
        $cred = "protocol=https`nhost=github.com`n`n" | & git credential fill 2>$null
        $ErrorActionPreference = $eap
        $pwdLine = $cred | Where-Object { $_ -like 'password=*' } | Select-Object -First 1
        if ($pwdLine) { $env:GH_TOKEN = $pwdLine.Substring(9) }
        if (-not $env:GH_TOKEN) { Die "No se pudo obtener credencial para gh. Ejecuta 'gh auth login' y reintenta (el tag ya está publicado)." }
    }

    Info "Creando el GitHub Release..."
    & $gh release create $tag --title "FormatDiskPro $tag" --notes-file $notesPath $setup $setupHash
    if ($LASTEXITCODE -ne 0) { Die "gh release create falló (el tag ya está publicado; puedes reintentar el release)." }

    if ($tempNotes) { Remove-Item $tempNotes -Force -ErrorAction SilentlyContinue }
    Write-Host ""
    Ok "Release $tag publicado: https://github.com/xfiberex/FormatDiskPro/releases/tag/$tag"
    # Se repite aquí a propósito: cuando el corte sale bien, este es el único bloque que se lee.
    if ($coverage) { Ok "Cobertura de Core/: $($coverage.Percent)% ($($coverage.Covered)/$($coverage.Total) líneas)." }
    Show-UiTestCoverage $uiSummary
}
finally {
    Pop-Location
}
