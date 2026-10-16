<#
.SYNOPSIS
    Corta una versión de FormatDiskPro de principio a fin.

.DESCRIPTION
    Flujo completo en un paso:
      1. Valida la versión, la sección del CHANGELOG, que la rama no vaya por detrás de origin, el árbol
         de trabajo y las herramientas de los últimos pasos (gh y su sesión, Inno Setup, el certificado).
         Todo antes de la primera prueba, y también con -DryRun.
      2. Ejecuta las pruebas (salvo -SkipTests) sobre una compilación Release sin advertencias.
      3. Actualiza <Version> en el .csproj si cambió (y lo restaura si el instalador falla).
      4. Compila el instalador (publish self-contained + Inno Setup).
      5. Commit del bump de versión + tag anotado vX.Y.Z, con el SHA-256 del instalador en su mensaje.
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

.PARAMETER ResumeRelease
    Retoma un corte que murió DESPUÉS de etiquetar: crea solo el GitHub Release, con el tag y el
    instalador que ya existen.

    Existe porque el consejo y la guarda se contradecían. Si `gh release create` fallaba, el mensaje decía
    "el tag ya está publicado; puedes reintentar el release" — pero al reintentar, la validación de arriba
    abortaba con "El tag vX.Y.Z ya existe localmente". Seguir el consejo al pie de la letra no llevaba a
    ningún sitio, justo en el momento en que el corte está a medias y hay prisa.

    Con este flag se omiten las pruebas, el bump, la compilación, el commit y el tag —todo eso ya ocurrió—
    y se comprueba que estén el instalador y su .sha256 en installer\Output, que el instalador sea el que
    anota el tag y que el tag esté en origin/<rama>. Si el tag aún no está en origin, se sube.

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
    [switch]$ResumeRelease,
    # Firma de código (opcional): se reenvían a build-installer.ps1.
    [string]$CertThumbprint,
    [string]$CertFile,
    # SecureString y no [string] (T3-09): en claro quedaba en el historial de PowerShell y en la línea de
    # comandos del proceso. También se puede dar por FORMATDISKPRO_CERT_PASSWORD; ver build-installer.ps1.
    [SecureString]$CertPassword,
    [string]$TimestampUrl
)

$ErrorActionPreference = "Stop"
# Modo estricto (`AUD-061`): una variable mal escrita, o una propiedad que no existe, abortan en vez de valer
# $null en silencio. Afecta también a build-installer.ps1, que se ejecuta en un ámbito hijo. OJO al leer XML:
# con el modo estricto, `$nodo.Hijo` lanza si el elemento no tiene ese hijo —también al enumerar los
# PropertyGroup de un .csproj—; por eso aquí se lee con SelectNodes/SelectSingleNode.
Set-StrictMode -Version Latest

# Mínimo de cobertura de LÍNEA exigido a Core/ (la lógica pura). El 2026-08-15, al medirla por primera
# vez, estaba en 97.1%: el umbral se pone por debajo a propósito, para que sea un suelo que avise de una
# regresión real y no un número que obligue a escribir pruebas de relleno para no romper el corte.
# Subirlo es una decisión deliberada; bajarlo, un síntoma.
$CoreCoverageThreshold = 90

# ¿Ha puesto ESTE script $env:GH_TOKEN desde la credencial cacheada? Solo entonces lo borra al terminar
# (ver el finally del final). Un token que ya viniera del entorno del usuario es suyo.
$script:ClearedGhToken = $false

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
    Ejecuta una consulta de git (solo lectura) y devuelve su salida, su código de salida y su error, sin
    abortar el script por lo que git escriba en stderr (`AUD-061`).

.DESCRIPTION
    Es la trampa de Invoke-Git, pero para las llamadas cuya SALIDA hace falta. `& git ... 2>$null` con la
    preferencia en "Stop" no silencia nada en PS 5.1: la primera línea de stderr se convierte en error
    terminante. Sin red, `git ls-remote` moría así, con un NativeCommandError crudo en vez de un mensaje del
    corte. Aquí stdout y stderr se separan, y quien llama decide por .ExitCode y da su propio mensaje.
#>
function Invoke-GitQuery {
    $eap = $ErrorActionPreference
    $ErrorActionPreference = "Continue"
    try {
        $all  = @(& git @args 2>&1)
        $code = $LASTEXITCODE
    }
    finally { $ErrorActionPreference = $eap }
    $isErr = { $_ -is [System.Management.Automation.ErrorRecord] }
    [pscustomobject]@{
        ExitCode = $code
        Output   = @($all | Where-Object { -not (& $isErr) } | ForEach-Object { "$_" })
        Error    = ((@($all | Where-Object $isErr | ForEach-Object { "$_".Trim() })) -join ' ').Trim()
    }
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

    Devuelve SIEMPRE un objeto con .Status, nunca $null (`T10-01`). Antes, cuatro situaciones muy distintas
    —no hay informe; lo hay y no se puede leer; lo hay y esta vacio; lo hay lleno pero sin ninguna clase de
    Core/— colapsaban en el mismo $null y salian por el mismo mensaje, que culpaba a coverlet.collector de
    no estar referenciado. Las dos veces que ha fallado de verdad, el paquete estaba referenciado.

    Un fallo de herramienta que senala mal se arregla dos veces: una buscando donde no es, y otra donde si.
    Eso ya lo dejo escrito `T8-06`; esto es aplicarlo al MENSAJE, no solo a la causa.

    .Status:
      ok         — medido; trae Covered/Total/Percent/PerClass.
      missing    — no hay ningun coverage.cobertura.xml bajo el directorio de resultados.
      unreadable — lo hay, pero no es XML valido.
      empty      — lo hay y es valido, pero no declara NI UNA clase. Es el artefacto de 235 bytes
                   (`<packages />`) de `T8-06`, y el que reaparecio al cortar la v1.25.0 con el arreglo de
                   `T8-06` puesto y ejecutado, por una causa que sigue sin conocerse (`T10-01`).
      nocore     — declara clases, pero ninguna bajo Core/. Eso no es un fallo del recolector: o se movio
                   la carpeta, o el filtro de esta funcion dejo de encajar.
#>
function Get-CoreCoverage {
    param([Parameter(Mandatory)][string]$CoverageDir)

    $report = Get-ChildItem $CoverageDir -Recurse -Filter "coverage.cobertura.xml" -ErrorAction SilentlyContinue |
        Sort-Object LastWriteTimeUtc -Descending | Select-Object -First 1
    if (-not $report) {
        return [pscustomobject]@{
            Status = 'missing'
            Report = $null
            Reason = "No hay ningun coverage.cobertura.xml bajo $CoverageDir."
        }
    }

    try {
        $xml = New-Object System.Xml.XmlDocument
        $xml.Load($report.FullName)
    } catch {
        return [pscustomobject]@{
            Status = 'unreadable'
            Report = $report.FullName
            Reason = "El informe existe ($($report.Length) bytes) pero no se pudo leer como XML: $($_.Exception.Message)"
        }
    }

    # El informe identifica cada clase por su archivo fuente: es lo que permite separar Core/ del resto.
    $all     = @($xml.SelectNodes("//class"))
    $classes = @($all | Where-Object { $_.filename -match '\\Core\\' })

    # Sin NINGUNA clase, el informe no mide nada de nada: es el `<packages />` de 235 bytes.
    if ($all.Count -eq 0) {
        return [pscustomobject]@{
            Status = 'empty'
            Report = $report.FullName
            Reason = "El informe existe ($($report.Length) bytes) y es XML valido, pero no declara ni una clase: no se instrumento nada."
        }
    }
    # Con clases pero ninguna de Core/, el problema no es el recolector sino este filtro o el arbol.
    if ($classes.Count -eq 0) {
        return [pscustomobject]@{
            Status = 'nocore'
            Report = $report.FullName
            Reason = "El informe mide $($all.Count) clases, pero ninguna bajo Core/. O se movio la carpeta, o el filtro de Get-CoreCoverage dejo de encajar."
        }
    }

    $covered = 0; $total = 0
    $perClass = foreach ($c in $classes) {
        $ct = 0; $cc = 0
        # SelectNodes y no $c.lines.line: con el modo estricto, una clase sin líneas (<lines />) lanzaría.
        foreach ($l in $c.SelectNodes('lines/line')) { $ct++; if ([int]$l.hits -gt 0) { $cc++ } }
        $covered += $cc; $total += $ct
        [pscustomobject]@{
            Name    = ($c.name -replace '^FormatDiskPro\.', '')
            Percent = if ($ct -gt 0) { [math]::Round(100 * $cc / $ct, 1) } else { 100 }
            Lines   = "$cc/$ct"
        }
    }

    [pscustomobject]@{
        Status   = 'ok'
        Reason   = $null
        Covered  = $covered
        Total    = $total
        Percent  = if ($total -gt 0) { [math]::Round(100 * $covered / $total, 1) } else { 0 }
        Report   = $report.FullName
        PerClass = @($perClass | Sort-Object Percent)
    }
}

<#
.SYNOPSIS
    Pone a salvo un directorio de resultados de cobertura que ha fallado, y devuelve donde lo dejo.

.DESCRIPTION
    El directorio de trabajo (`$env:TEMP\FormatDiskPro_coverage`) lo BORRA la siguiente corrida, asi que
    la unica prueba de un fallo desaparecia en cuanto se reintentaba el corte — que es exactamente lo
    primero que uno hace. Al cortar la v1.25.0 el informe salio vacio, el reintento salio bien, y lo unico
    que quedo del fallo fue lo que se copio a mano antes de relanzar.

    Se copia FUERA del repo a proposito: cualquier archivo nuevo dentro del arbol haria abortar el
    siguiente corte por arbol sucio (`T9-01`), y el remedio no puede estropear el diagnostico.
#>
function Save-CoverageEvidence {
    param(
        [Parameter(Mandatory)][AllowNull()][string]$CoverageDir,
        [Parameter(Mandatory)][string]$Label
    )

    if (-not $CoverageDir -or -not (Test-Path $CoverageDir)) { return $null }
    $stamp = Get-Date -Format 'yyyyMMdd-HHmmss'
    $dest  = Join-Path $env:TEMP "FormatDiskPro_cobertura_fallida\$Label-$stamp"
    try {
        New-Item -ItemType Directory -Path $dest -Force | Out-Null
        Copy-Item (Join-Path $CoverageDir '*') $dest -Recurse -Force -ErrorAction Stop
        return $dest
    } catch {
        return $null
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

    # Por XPath con local-name(): el .trx tiene espacio de nombres, y con el modo estricto `$x.Output` lanza
    # en un resultado que no tiene ese elemento (los que pasan no lo tienen).
    $results = @($xml.SelectNodes("//*[local-name()='Results']/*[local-name()='UnitTestResult']"))
    if ($results.Count -eq 0) { return $null }

    $skipped = @($results | Where-Object { $_.outcome -eq 'NotExecuted' })
    [pscustomobject]@{
        Total   = $results.Count
        Passed  = @($results | Where-Object { $_.outcome -eq 'Passed' }).Count
        Skipped = $skipped.Count
        SkippedDetail = @($skipped | ForEach-Object {
            $msg    = $_.SelectSingleNode("*[local-name()='Output']/*[local-name()='ErrorInfo']/*[local-name()='Message']")
            $reason = if ($msg) { $msg.InnerText } else { $null }
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
        # Con -UiTests y sin informe el corte ya ha muerto (`AUD-055`); aquí solo se llega con -ResumeRelease.
        Warn "UI tests: no ejecutados en esta pasada (-ResumeRelease)."
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

<#
.SYNOPSIS
    Imprime el resultado de las unitarias leído de su .trx, con las OMITIDAS nombradas y su motivo
    (`AUD-055`). Se muestra al terminar las pruebas, en el dry run y en el resumen final, como el de UI.
#>
function Show-UnitTestSummary {
    param($Summary)

    if (-not $Summary) { return }
    if ($Summary.Skipped -gt 0) {
        Warn "Unitarias: $($Summary.Passed)/$($Summary.Total) — $($Summary.Skipped) OMITIDAS:"
        $Summary.SkippedDetail | ForEach-Object {
            Write-Host "      - $($_.Name): $($_.Reason)" -ForegroundColor DarkGray
        }
    } else {
        Ok "Unitarias: $($Summary.Passed)/$($Summary.Total) — 0 omitidas."
    }
}

<#
.SYNOPSIS
    Líneas de la sección de una versión en el CHANGELOG: de su encabezado "## [X.Y.Z]" al siguiente "## ".
    Devuelve $null si no hay encabezado, y un array vacío si lo hay pero no le sigue nada.

.DESCRIPTION
    Antes se cortaba con $lines[$start..($end - 1)] sin comprobar que $end fuera mayor que $start
    (`AUD-054`). Con dos encabezados de versión seguidos, $end - 1 queda POR DEBAJO de $start, y en
    PowerShell un rango descendente no sale vacío: devuelve las líneas al revés, el propio encabezado entre
    ellas. Una sección vacía pasaba así por tener contenido, y salía como notas del release.
#>
function Get-ChangelogSection {
    param([Parameter(Mandatory)][string]$Raw, [Parameter(Mandatory)][string]$Version)

    $lines = $Raw -split "`r?`n"
    $start = -1; $end = $lines.Count
    for ($i = 0; $i -lt $lines.Count; $i++) {
        if ($start -lt 0) {
            if ($lines[$i] -match "^##\s*\[$([regex]::Escape($Version))\]") { $start = $i + 1 }
            continue
        }
        if ($lines[$i] -match '^##\s') { $end = $i; break }
    }
    if ($start -lt 0)    { return $null }
    if ($end -le $start) { return ,@() }
    return ,@($lines[$start..($end - 1)])
}

<#
.SYNOPSIS
    Ruta de gh (GitHub CLI), o $null. La usan la comprobación previa y el último paso.
#>
function Find-GitHubCli {
    $found = @(
        "C:\Program Files\GitHub CLI\gh.exe",
        "C:\Program Files (x86)\GitHub CLI\gh.exe"
    ) | Where-Object { Test-Path $_ } | Select-Object -First 1
    if (-not $found) {
        $cmd = Get-Command gh -ErrorAction SilentlyContinue
        if ($cmd) { $found = $cmd.Source }
    }
    return $found
}

<#
.SYNOPSIS
    Ruta de ISCC.exe (Inno Setup 6), o $null. Busca igual que build-installer.ps1, que es quien la usa.
#>
function Find-InnoSetup {
    $found = @(
        "C:\Program Files (x86)\Inno Setup 6\ISCC.exe",
        "C:\Program Files\Inno Setup 6\ISCC.exe"
    ) | Where-Object { Test-Path $_ } | Select-Object -First 1
    if (-not $found) {
        $cmd = Get-Command iscc.exe -ErrorAction SilentlyContinue
        if ($cmd) { $found = $cmd.Source }
    }
    return $found
}

<#
.SYNOPSIS
    Comprueba que el instalador es el que dice su .sha256, y devuelve su hash y el problema, si lo hay.

.DESCRIPTION
    El .sha256 es lo único con lo que la app verifica una actualización sin firmar antes de ejecutarla como
    administrador. Si instalador y .sha256 se desemparejan, la app descarga el release, no lo verifica y lo
    borra: la auto-actualización queda rota para todos hasta la versión siguiente (`AUD-021`).
#>
function Test-InstallerHash {
    param([Parameter(Mandatory)][string]$Setup, [Parameter(Mandatory)][string]$HashFile)

    $actual = (Get-FileHash $Setup -Algorithm SHA256).Hash
    $leaf   = Split-Path $Setup -Leaf
    $line   = [System.IO.File]::ReadAllText($HashFile).Trim()
    $problem = $null
    if ($line -notmatch '^([0-9A-Fa-f]{64}) \*(.+)$') {
        $problem = "el .sha256 no tiene la forma 'HASH *nombre': '$line'"
    } elseif ($Matches[2] -ne $leaf) {
        $problem = "el .sha256 nombra '$($Matches[2])', no '$leaf'"
    } elseif ($Matches[1] -ne $actual) {
        $problem = "el instalador tiene el hash $actual y su .sha256 dice $($Matches[1])"
    }
    [pscustomobject]@{ Hash = $actual; Problem = $problem }
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

# Las tres variables que hacen que una prueba TOQUE HARDWARE (`AUD-056`). Antes solo se miraba la primera, y
# solo con -UiTests: con FORMATDISKPRO_VERIFY_DRIVE viva de una sesión de depuración, las UNITARIAS del corte
# escribían en una unidad real sin que nada lo dijera. Se miran todas, aquí arriba y no junto a las pruebas,
# para que también un -DryRun lo diga antes de ejecutar nada. Si el corte va a ejecutar pruebas, aborta; si
# no (-SkipTests, -ResumeRelease), no tocan nada y basta con avisar.
$hardwareVars = [ordered]@{
    'FORMATDISKPRO_ALLOW_DESTRUCTIVE' = 'la suite de UI FORMATEARÍA la USB de pruebas'
    'FORMATDISKPRO_ALLOW_YANK'        = 'la suite de UI DESMONTARÍA la USB de pruebas a mitad de una operación'
    'FORMATDISKPRO_VERIFY_DRIVE'      = 'las unitarias ESCRIBIRÍAN ~64 MB en la unidad que nombra'
}
$hardwareSet = @($hardwareVars.Keys | Where-Object { -not [string]::IsNullOrWhiteSpace([Environment]::GetEnvironmentVariable($_)) })
if ($hardwareSet.Count -gt 0) {
    $hardwareList = $hardwareSet | ForEach-Object { "      - $_=$([Environment]::GetEnvironmentVariable($_)): $($hardwareVars[$_])" }
    if ($SkipTests -or $ResumeRelease) {
        Warn "Hay variables que hacen tocar hardware a las pruebas. Este corte no ejecuta pruebas, así que no tienen efecto:"
        $hardwareList | ForEach-Object { Write-Host $_ -ForegroundColor DarkGray }
    } else {
        Write-Host "[X] Hay variables que hacen tocar hardware a las pruebas, y un corte de release nunca debe hacerlo:" -ForegroundColor Red
        $hardwareList | ForEach-Object { Write-Host $_ -ForegroundColor Red }
        $clear = ($hardwareSet | ForEach-Object { "`$env:$_ = `$null" }) -join '; '
        Die "Límpialas ($clear) y reintenta."
    }
}

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
$changelogSection = Get-ChangelogSection -Raw $changelogRaw -Version $Version
if ($null -eq $changelogSection) {
    Die @"
CHANGELOG.md no tiene una sección para la $Version.

Antes de cortar, mueve lo que haya bajo '## [Sin publicar]' a una sección nueva:

    ## [$Version] — $(Get-Date -Format 'yyyy-MM-dd')

y añade abajo su enlace:

    [$Version]: https://github.com/xfiberex/FormatDiskPro/releases/tag/$tag
"@
}
# Que exista el encabezado no basta: tiene que contar algo. Solo subencabezados, separadores o líneas en
# blanco es una sección vacía, y saldría como notas del release (`AUD-054`).
$changelogContent = @($changelogSection | Where-Object {
    $_ -match '\S' -and $_ -notmatch '^\s*#' -and $_ -notmatch '^\s*-{3,}\s*$'
})
if ($changelogContent.Count -eq 0) {
    Die "La sección [$Version] del CHANGELOG está vacía: no tiene ni una línea de texto debajo del encabezado. ¿Falta mover lo de '## [Sin publicar]'?"
}
Ok "CHANGELOG.md tiene la sección de la $Version."

# Restauración en modo bloqueado durante todo el corte (`AUD-022`): si un packages.lock.json no corresponde a
# su proyecto, la compilación falla (NU1004) en vez de restaurar otra cosa y reescribirlo. Directory.Build.props
# lee esta variable; el finally del final la devuelve a como estaba.
$prevLockedMode = $env:RestoreLockedMode
$env:RestoreLockedMode = 'true'

# ── Validaciones de git ──────────────────────────────────────────────────────
Push-Location $root
try {
    # Las consultas van por Invoke-GitQuery (`AUD-061`): un fallo de git —sin red, sin repo— acaba en un
    # mensaje del corte, no en un NativeCommandError.
    if ((Invoke-GitQuery rev-parse --is-inside-work-tree).ExitCode -ne 0) { Die "Este directorio no es un repositorio git." }

    $branch = ((Invoke-GitQuery rev-parse --abbrev-ref HEAD).Output -join '').Trim()
    Info "Rama: $branch"

    # ¿Tag ya existe? (local o remoto)
    $localTag  = (Invoke-GitQuery tag --list $tag).Output
    # Antes, sin red, esta línea abortaba el corte con un error crudo; y si no abortaba, un fallo se leía
    # como «el tag no está en origin».
    $remoteQuery = Invoke-GitQuery ls-remote --tags origin $tag
    if ($remoteQuery.ExitCode -ne 0) {
        Die "No se pudo consultar origin (git ls-remote terminó con código $($remoteQuery.ExitCode)): sin eso no se sabe si el tag $tag ya existe ni si este clon va por detrás. ¿Hay red? Detalle: $($remoteQuery.Error)"
    }
    $remoteTag = $remoteQuery.Output

    if ($ResumeRelease) {
        # Retomando: el tag TIENE que existir ya; es lo que se está reaprovechando.
        if (-not $localTag) {
            Die "-ResumeRelease necesita que el tag $tag ya exista localmente, y no existe. Si el corte no llegó a etiquetar, lánzalo normal (sin -ResumeRelease)."
        }
        Info "Retomando el release de $tag (se omiten pruebas, bump, compilación, commit y tag)."
    }
    else {
        if ($localTag) {
            Die @"
El tag $tag ya existe localmente.

Si un corte anterior murió DESPUÉS de etiquetar y solo falta publicar el release:

    .\release.ps1 -Version $Version -ResumeRelease

Si lo que quieres es rehacer el corte entero, borra el tag primero:

    git tag -d $tag
    git push origin :refs/tags/$tag   # solo si llegó a subirse
"@
        }
        if ($remoteTag) { Die "El tag $tag ya existe en origin. Usa otra versión, o retoma con -ResumeRelease si solo falta publicar el release." }
    }

    # ── Rama y origin (`AUD-020`) ────────────────────────────────────────────
    # Antes no se miraba ni la rama ni origin. Con commits en origin que este clon no tiene —una fusión
    # hecha en GitHub, un PR de Dependabot—, el corte entero se ejecutaba (pruebas, bump, instalador, commit
    # y tag) y moría al subir la rama, con un mensaje que no decía cómo seguir. Se comprueba aquí, antes de
    # la primera prueba, también en el -DryRun.
    if ($branch -eq 'HEAD') { Die "HEAD está desacoplado: un corte sale de una rama. Cámbiate a master y reintenta." }
    if ($branch -ne 'master') {
        Warn "El corte se lanza desde '$branch', no desde master: el commit y el tag del release quedarán en esa rama."
    }
    $headsQuery = Invoke-GitQuery ls-remote --heads origin $branch
    if ($headsQuery.ExitCode -ne 0) {
        Die "No se pudo consultar origin/$branch (git ls-remote terminó con código $($headsQuery.ExitCode)). ¿Hay red? Detalle: $($headsQuery.Error)"
    }
    $onOrigin = $headsQuery.Output
    if (-not $onOrigin) {
        if ($ResumeRelease) { Die "-ResumeRelease necesita que '$branch' esté en origin, para comprobar que el tag $tag está en ella, y no está." }
        Warn "La rama '$branch' no existe en origin: el corte la creará al subirla."
    } else {
        Info "Comprobando origin/$branch..."
        if ((Invoke-Git fetch origin "+refs/heads/${branch}:refs/remotes/origin/$branch") -ne 0) {
            Die "git fetch de origin/$branch falló: sin él no se sabe si este clon va por detrás. Revisa la conexión y reintenta."
        }
        if ($ResumeRelease) {
            # Se publicaría un release cuyo código no está en la rama: quien la clone no tendría ese commit.
            # --is-ancestor: 0 = sí, 1 = no; cualquier otro código es que git no pudo responder.
            $ancestor = Invoke-GitQuery merge-base --is-ancestor "$tag^{commit}" "origin/$branch"
            if ($ancestor.ExitCode -gt 1) {
                Die "No se pudo comprobar si el tag $tag está en origin/$branch (git merge-base terminó con código $($ancestor.ExitCode)): $($ancestor.Error)"
            }
            if ($ancestor.ExitCode -ne 0) {
                Die "El tag $tag no está en origin/${branch}: publicarías un release cuyo código no está en la rama. Sube antes la rama con el commit del release ('git push origin $branch')."
            }
            Ok "El tag $tag está en origin/$branch."
        } else {
            $behind = [int](@((Invoke-GitQuery rev-list --count "HEAD..origin/$branch").Output) -join '')
            if ($behind -gt 0) {
                Die "origin/$branch tiene $behind commit(s) que este clon no tiene: el corte moriría al subir la rama, después de etiquetar. Trae los cambios ('git pull --ff-only'), vuelve a probar y reintenta."
            }
            $ahead = [int](@((Invoke-GitQuery rev-list --count "origin/$branch..HEAD").Output) -join '')
            Ok "Al día con origin/$branch ($ahead commit(s) por subir)."
        }
    }

    # ── Estado del árbol de trabajo ──────────────────────────────────────────
    # Se miran las DOS cosas, y son problemas distintos:
    #
    #   - Archivos SIN RASTREAR (`??`): NO entrarán en el commit del release, porque más abajo solo se
    #     hace `git add -u`. Quien esperase publicarlos se queda sin ellos.
    #
    #   - Archivos RASTREADOS MODIFICADOS: entrarán TODOS, porque `git add -u` los barre. Es el caso
    #     peligroso y hasta ahora no se comprobaba: trabajo a medias, ajeno al release, acababa dentro
    #     del commit "release: vX.Y.Z" sin que nadie lo pidiera. Y como el instalador se compila desde
    #     el ÁRBOL DE TRABAJO (build-installer.ps1 publica lo que hay en disco, no lo que hay en HEAD),
    #     el binario publicado podía no corresponder al commit que lleva el tag — justo la premisa sobre
    #     la que se apoya el .sha256, que existe porque se decidió no firmar.
    #
    # -AllowDirty cubre ambos casos, que es lo que su propia ayuda promete.
    $status    = @((Invoke-GitQuery status --porcelain).Output)
    $untracked = @($status | Where-Object { $_ -match '^\?\?' })
    $modified  = @($status | Where-Object { $_ -notmatch '^\?\?' })

    # Al retomar no se commitea ni se compila nada, así que un árbol sucio es irrelevante: lo que se va a
    # publicar es el instalador que ya existe.
    if ($ResumeRelease -and $modified) {
        Info "Árbol sucio, irrelevante al retomar: no se commitea ni se recompila nada ($($modified.Count) archivos modificados)."
    } elseif ($modified -and -not $AllowDirty) {
        Warn "El árbol de trabajo tiene cambios sin commitear, y 'git add -u' los incluiría TODOS en el commit del release:"
        $modified | ForEach-Object { Write-Host "    $_" -ForegroundColor DarkGray }
        Die "Commitea o revierte estos cambios y reintenta, o usa -AllowDirty si de verdad quieres publicarlos con el release."
    } elseif ($modified) {
        Warn "Árbol sucio aceptado (-AllowDirty): estos cambios ENTRARÁN en el commit del release y en el instalador:"
        $modified | ForEach-Object { Write-Host "    $_" -ForegroundColor DarkGray }
    }

    if ($untracked -and -not $AllowDirty) {
        Warn "Hay archivos nuevos sin rastrear (no se incluirán en el release):"
        $untracked | ForEach-Object { Write-Host "    $_" -ForegroundColor DarkGray }
        Die "Añade los archivos que necesites con 'git add <archivo>' y reintenta, o usa -AllowDirty para ignorarlos."
    } elseif ($untracked) {
        Warn "Archivos sin rastrear ignorados (-AllowDirty):"
        $untracked | ForEach-Object { Write-Host "    $_" -ForegroundColor DarkGray }
    }

    # ── Herramientas de los últimos pasos (`AUD-053`) ────────────────────────
    # Inno Setup, gh y su sesión se buscaban justo antes de usarlos: después de cambiar el .csproj, o
    # después de subir la rama y el tag. Faltar uno de ellos dejaba el corte a medias, y el -DryRun, que no
    # llega a esos pasos, los daba por buenos. Se comprueban aquí, antes de la primera prueba y en el
    # -DryRun también.
    $gh = Find-GitHubCli
    if (-not $gh) { Die "No se encuentra gh (GitHub CLI): sin él, el corte subiría la rama y el tag y moriría al crear el release. Instálalo (winget install GitHub.cli) y reintenta." }

    # Si gh no tiene sesión se reutiliza la credencial cacheada de git (local, no se muestra). El finally
    # del final borra el token, porque lo puso este script.
    $eap = $ErrorActionPreference
    $ErrorActionPreference = "SilentlyContinue"
    & $gh auth status *> $null
    $authOk = $LASTEXITCODE -eq 0
    $ErrorActionPreference = $eap
    if (-not $authOk -and -not $env:GH_TOKEN) {
        Warn "gh no tiene sesión; se reutilizará la credencial de git cacheada."
        $eap = $ErrorActionPreference
        $ErrorActionPreference = "SilentlyContinue"
        $cred = "protocol=https`nhost=github.com`n`n" | & git credential fill 2>$null
        $ErrorActionPreference = $eap
        $pwdLine = $cred | Where-Object { $_ -like 'password=*' } | Select-Object -First 1
        if ($pwdLine) { $env:GH_TOKEN = $pwdLine.Substring(9); $script:ClearedGhToken = $true }
        if (-not $env:GH_TOKEN) { Die "gh no tiene sesión y no hay credencial de git que reutilizar. Ejecuta 'gh auth login' y reintenta." }
    }

    if (-not $ResumeRelease) {
        $iscc = Find-InnoSetup
        if (-not $iscc) { Die "No se encuentra ISCC.exe (Inno Setup 6): el corte cambiaría el .csproj y moriría al compilar el instalador. Instálalo (winget install JRSoftware.InnoSetup) y reintenta." }
        if ($CertFile -and -not (Test-Path $CertFile)) { Die "-CertFile apunta a un archivo que no existe: $CertFile" }
        if ($CertThumbprint) {
            $thumb = $CertThumbprint -replace '\s', ''
            $cert = @('Cert:\CurrentUser\My', 'Cert:\LocalMachine\My') |
                ForEach-Object { Get-ChildItem $_ -ErrorAction SilentlyContinue } |
                Where-Object { $_.Thumbprint -eq $thumb } | Select-Object -First 1
            if (-not $cert)              { Die "No hay ningún certificado con la huella $thumb en el almacén (CurrentUser\My ni LocalMachine\My)." }
            if (-not $cert.HasPrivateKey) { Die "El certificado $thumb está en el almacén, pero sin su clave privada: no puede firmar." }
        }
        Ok "Herramientas: gh, su sesión e Inno Setup$(if ($CertThumbprint -or $CertFile) { ', y el certificado de firma' })."
    } else {
        Ok "Herramientas: gh y su sesión."
    }

    # ── Pruebas ──────────────────────────────────────────────────────────────
    $uiSummary   = $null   # cobertura real de UI de este corte; se repite en el resumen final
    $unitSummary = $null   # resultado de las unitarias leído de su .trx (`AUD-055`); también se repite
    $coverage  = $null   # cobertura de línea de Core/ (T2-04)
    if ($ResumeRelease) {
        Warn "Pruebas omitidas (-ResumeRelease): ya se ejecutaron en el corte que dejó este tag."
    } elseif ($SkipTests) {
        Warn "Pruebas omitidas (-SkipTests)."
    } else {
        # Se mide la cobertura en la misma pasada (T2-04): un corte no debe poder afirmar "389 pruebas"
        # sin saber qué parte de la lógica pura tocan.
        $covDir = Join-Path $env:TEMP "FormatDiskPro_coverage"
        if (Test-Path $covDir) { Remove-Item $covDir -Recurse -Force -ErrorAction SilentlyContinue }

        # Compilar ANTES de medir, y no dejar que lo haga `dotnet test`. Cuando la compilación NO está al
        # día, coverlet instrumenta los ensamblados que encuentra y acto seguido MSBuild los sobrescribe
        # con los recién compilados: el informe sale vacío —235 bytes, `<packages />`— y el corte muere en
        # "Se pidió cobertura y no se obtuvo informe", que suena a que falta el paquete y no es eso.
        #
        # No es una sospecha: medido dos veces seguidas. Con cambios sin compilar, informe vacío (2 de 2);
        # con la compilación al día, 1,6 MB (4 de 4). Y es justo lo que pasa SIEMPRE en un corte de verdad,
        # porque se corta después de tocar código.
        #
        # En Release y con -warnaserror (`AUD-052`): antes se probaba una compilación de depuración sin el
        # listón de cero advertencias, y lo que se publica sale de Release.
        #
        # Y desde una bin\Release LIMPIA. build-installer.ps1 publica self-contained, y esa publicación
        # compila en bin\Release\<tfm>\win-x64 dejando allí coreclr.dll y hostfxr.dll. Cuando un `dotnet build
        # -c Release` posterior regenera la configuración del runtime, el .exe pasa a depender del .NET
        # instalado; al ver coreclr.dll a su lado, el lanzador busca .NET SOLO en esa carpeta y responde «You
        # must install or update .NET». No pasa siempre —un build que no regenera esa configuración deja un
        # .exe que arranca—, y por eso se borra en vez de confiar: el 2026-10-14 dejó 45 de 50 pruebas de UI
        # en rojo, con .NET instalado.
        $appRelease = Join-Path $root "src\FormatDiskPro\bin\Release"
        if (Test-Path $appRelease) {
            try { Remove-Item $appRelease -Recurse -Force -ErrorAction Stop }
            catch { Die "No se pudo vaciar $appRelease (¿hay una FormatDiskPro.exe abierta desde ahí?): $($_.Exception.Message)" }
        }
        Info "Compilando la solución (Release, sin advertencias) antes de medir cobertura..."
        & dotnet build $solution -c Release -warnaserror --nologo -v q
        if ($LASTEXITCODE -ne 0) { Die "La compilación de la solución en Release falló, o tiene advertencias (-warnaserror). Release abortado." }
        $releaseExe = Get-ChildItem $appRelease -Recurse -Filter "FormatDiskPro.exe" -ErrorAction SilentlyContinue |
            Select-Object -First 1
        if (-not $releaseExe) { Die "La compilación en Release terminó, pero no se encuentra FormatDiskPro.exe bajo $appRelease." }

        # Con .trx (`AUD-055`): el código de salida dice que nada falló, no qué se omitió. Sin el informe, una
        # prueba que pasara a omitirse no dejaba rastro en el corte. Un .trx ausente o sin resultados es un
        # fallo: no hay de dónde sacar qué se probó.
        $unitTrxName = "unitarias-$Version.trx"
        Info "Ejecutando pruebas unitarias (con cobertura)..."
        & dotnet test $solution -c Release --no-build --nologo --collect:"XPlat Code Coverage" --results-directory $covDir `
            --logger "trx;LogFileName=$unitTrxName"
        if ($LASTEXITCODE -ne 0) { Die "Las pruebas unitarias fallaron. Release abortado." }
        $unitSummary = Get-TestRunSummary -TrxPath (Join-Path $covDir $unitTrxName)
        if (-not $unitSummary) {
            Die "Las pruebas unitarias terminaron sin fallos, pero su informe ($unitTrxName en $covDir) no existe o no tiene resultados: no se sabe qué se probó ni qué se omitió. Release abortado."
        }
        Show-UnitTestSummary $unitSummary

        # `T10-01`: cuatro fallos distintos dejan de salir por el mismo mensaje. Get-CoreCoverage ya no
        # devuelve $null nunca: dice CUAL de ellos es.
        $covResult = Get-CoreCoverage -CoverageDir $covDir
        if ($covResult.Status -ne 'ok') {
            Warn "Cobertura no medible ($($covResult.Status)): $($covResult.Reason)"
            $evidencia = Save-CoverageEvidence -CoverageDir $covDir -Label $Version
            if ($evidencia) { Warn "Informe conservado en: $evidencia" }
            else            { Warn "No se pudo conservar el informe (no habia directorio de resultados)." }

            # 'missing' y 'nocore' SI apuntan a algo concreto, y cada uno a un sitio distinto.
            if ($covResult.Status -eq 'missing') {
                Die "No se genero ningun informe de cobertura. Aqui si cabe sospechar del recolector: revisa que coverlet.collector siga referenciado en el proyecto de pruebas."
            }
            if ($covResult.Status -eq 'nocore') {
                Die "El informe mide, pero no ve Core/. Eso no es del recolector: o se movio src\FormatDiskPro\Core, o el filtro de Get-CoreCoverage dejo de encajar con las rutas del informe. Revisa $($covResult.Report)."
            }

            # 'empty' / 'unreadable': es el fallo de `T10-01`, cuya causa NO se conoce. Se repite la
            # medicion con los diagnosticos del recolector puestos —solo por aqui, para no ralentizar los
            # cortes que van bien— y el resultado NO desbloquea nada: el corte muere igual, unas lineas mas
            # abajo, gane o pierda. Un fallo intermitente al que se le pone un reintento deja de aparecer
            # sin dejar de existir, y esta puerta es la que decide si un corte sale.
            Info "Repitiendo la medicion con diagnosticos. Esto NO desbloquea el corte: solo busca dejar rastro."
            $diagDir = Join-Path $env:TEMP "FormatDiskPro_coverage_diag"
            if (Test-Path $diagDir) { Remove-Item $diagDir -Recurse -Force -ErrorAction SilentlyContinue }
            New-Item -ItemType Directory -Path $diagDir -Force | Out-Null
            $diagLog = Join-Path $diagDir "dotnet-test.diag.log"
            & dotnet test $solution -c Release --no-build --nologo --collect:"XPlat Code Coverage" --results-directory $diagDir --diag:$diagLog
            $segunda   = Get-CoreCoverage -CoverageDir $diagDir
            $diagKept  = Save-CoverageEvidence -CoverageDir $diagDir -Label "$Version-diag"

            if ($segunda.Status -eq 'ok') {
                $veredicto = "La segunda pasada SI midio ($($segunda.Percent)%). Es intermitente, como en el corte de la v1.25.0."
                $consejo   = "Relanzar el corte probablemente funcione — pero antes anota en ``T10-01`` que ha vuelto a pasar, con los diagnosticos de arriba. Es la unica forma de que esto deje de ser un fallo sin causa conocida."
            } else {
                $veredicto = "La segunda pasada TAMPOCO midio ($($segunda.Status)): $($segunda.Reason)"
                $consejo   = "Esto ya no es intermitente: es reproducible aqui y ahora. No relances el corte; arreglalo con los diagnosticos delante."
            }

            Die @"
La cobertura no se pudo medir, y no por falta del paquete.

  Que paso: $($covResult.Reason)
  Informe:  $(if ($evidencia) { $evidencia } else { $covResult.Report })
  Segunda pasada con --diag: $veredicto
  Diagnosticos: $(if ($diagKept) { $diagKept } else { "no se pudieron conservar" })

$consejo

Contexto: ``T8-06`` cerro UNA causa del informe vacio (compilar antes de medir, que se sigue haciendo).
``T10-01`` esta abierta justo por esta: al cortar la v1.25.0 volvio a pasar con ese arreglo puesto, y no se
reprodujo en tres intentos. Ver ROADMAP.md, Tier 10.
"@
        }
        $coverage = $covResult
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

            # 2. Opt-in destructivo y de desmontaje: ya rechazados arriba, junto con FORMATDISKPRO_VERIFY_DRIVE,
            #    antes de la primera prueba (`AUD-056`).

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

            # La app que se prueba es la de Release recién compilada, la misma que las unitarias. Sin decírselo,
            # AppFixture lanza el FormatDiskPro.exe más reciente que encuentre bajo bin\, sea el que sea.
            Info "Ejecutando UI tests sobre $($releaseExe.FullName) (conducen la app real; se abrirán ventanas)..."
            $prevExe = $env:FORMATDISKPRO_EXE
            $env:FORMATDISKPRO_EXE = $releaseExe.FullName
            try {
                & dotnet test $uiTestProject --filter "Category!=Slow" --nologo `
                    --logger "trx;LogFileName=$trxName" --results-directory $trxDir
                $uiExit = $LASTEXITCODE
            } finally { $env:FORMATDISKPRO_EXE = $prevExe }
            if ($uiExit -ne 0) { Die "Los UI tests fallaron. Release abortado." }
            Ok "UI tests correctos."

            $uiSummary = Get-TestRunSummary -TrxPath $trxPath
            # Antes, un .trx ilegible seguía adelante con un aviso que afirmaba que habían pasado (`AUD-055`).
            if (-not $uiSummary) {
                Die "Los UI tests terminaron sin fallos, pero su informe ($trxPath) no existe o no tiene resultados: no se sabe cuántos se omitieron. Release abortado."
            }
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
    $contents  = $null   # qué lleva el instalador (`AUD-022`): lo anota build-installer.ps1 junto a él
    if (-not $notesPath) {
        # La sección que ya se validó arriba (Get-ChangelogSection, `AUD-054`), sin el separador del final.
        $body = ($changelogSection -join "`n").Trim().TrimEnd('-').Trim()
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

    # ── Al retomar: el instalador tiene que ser el del tag (`AUD-021`) ───────
    # Se publicaba lo que hubiera en installer\Output sin mirar qué era. Si entre el corte fallido y el
    # reintento se recompilaba —otra rama, otro commit—, salía un binario que no corresponde al tag, y nada
    # avisaba. El corte anota el hash en el mensaje del tag; aquí se exige que el instalador presente sea
    # ese, y que su .sha256 lo diga. Va antes del -DryRun para que la simulación también lo compruebe.
    if ($ResumeRelease) {
        $setup     = Join-Path $outputDir "FormatDiskPro-$Version-setup.exe"
        $setupHash = "$setup.sha256"
        if (-not (Test-Path $setup)) {
            Die "-ResumeRelease no encuentra el instalador de la $Version en $setup. Si ya no está, rehaz el corte: borra el tag ('git tag -d $tag', y 'git push origin :refs/tags/$tag' si llegó a subirse) y vuelve a lanzarlo."
        }
        if (-not (Test-Path $setupHash)) { Die "-ResumeRelease no encuentra el checksum esperado: $setupHash" }

        $check = Test-InstallerHash -Setup $setup -HashFile $setupHash
        if ($check.Problem) { Die "-ResumeRelease no publica: $($check.Problem)." }

        $tagText   = (Invoke-GitQuery tag -l "--format=%(contents)" $tag).Output -join "`n"
        $taggedHash = if ($tagText -match 'SHA-256 del instalador: ([0-9A-Fa-f]{64})') { $Matches[1] } else { $null }
        if (-not $taggedHash) {
            Warn "El tag $tag no anota el hash del instalador (es anterior a ``AUD-021``): solo se ha podido cotejar con su .sha256."
        } elseif ($taggedHash -ne $check.Hash) {
            Die "-ResumeRelease no publica: el instalador de $outputDir no es el del tag. El tag $tag anota $taggedHash y el instalador tiene $($check.Hash). Recompílalo desde el commit del tag ('git checkout $tag') o rehaz el corte."
        } else {
            Ok "El instalador es el que anota el tag $tag ($($check.Hash))."
        }
        $sizeMB = [math]::Round((Get-Item $setup).Length / 1MB, 1)
        Ok "Instalador reutilizado: $setup ($sizeMB MB)"
        $contentsFile = "$setup.contenido.txt"
        if (Test-Path $contentsFile) {
            $contents = [System.IO.File]::ReadAllText($contentsFile).Trim()
        } else {
            Warn "No está $contentsFile (instalador anterior a ``AUD-022``): las notas no dirán qué runtime lleva."
        }
    }

    # ── DRY RUN: mostrar plan y salir ────────────────────────────────────────
    if ($DryRun) {
        Write-Host ""
        Warn "DRY RUN — no se modificará nada. Plan:"
        $signNote = if ($CertThumbprint -or $CertFile) { " (firmando con Authenticode)" } else { " (SIN firmar — la app verificará por el .sha256)" }

        # El plan tiene que decir lo que va a pasar DE VERDAD. Con -ResumeRelease casi todo se salta, y
        # listar los pasos completos sería la misma clase de mentira que el corte que publicaba una
        # plantilla genérica como notas (`T8-04`).
        if ($ResumeRelease) {
            Write-Host "    (-ResumeRelease: se OMITEN pruebas, bump, compilación, commit y tag)" -ForegroundColor DarkGray
            $tagNote = if ($remoteTag) { "ya está en origin" } else { "solo en local: se subirá" }
            Write-Host "    1. Reutilizar el tag $tag ($tagNote)" -ForegroundColor DarkGray
            Write-Host "    2. Reutilizar installer\Output\FormatDiskPro-$Version-setup.exe y su .sha256" -ForegroundColor DarkGray
            Write-Host "    3. gh release create $tag (assets: FormatDiskPro-$Version-setup.exe + .sha256)" -ForegroundColor DarkGray
        }
        else {
            Write-Host "    1. Actualizar <Version> a $Version en el .csproj" -ForegroundColor DarkGray
            Write-Host "    2. build-installer.ps1 -Version $Version$signNote" -ForegroundColor DarkGray
            $dirtyNote = if ($modified.Count -gt 0) { " — AHORA MISMO son $($modified.Count), listados arriba" } else { " — ahora mismo no hay ninguno" }
            Write-Host "    3. git add -u  (todos los archivos rastreados modificados$dirtyNote)" -ForegroundColor DarkGray
            Write-Host "       git commit -m 'release: v$Version'" -ForegroundColor DarkGray
            Write-Host "       git tag -a $tag" -ForegroundColor DarkGray
            Write-Host "    4. git push origin $branch" -ForegroundColor DarkGray
            Write-Host "       git push origin $tag" -ForegroundColor DarkGray
            Write-Host "    5. gh release create $tag (assets: FormatDiskPro-$Version-setup.exe + .sha256)" -ForegroundColor DarkGray
        }
        $notesOrigin = if ($NotesFile) { "-NotesFile $NotesFile" } else { "la sección [$Version] del CHANGELOG" }
        Write-Host "       Notas del release: $notesOrigin" -ForegroundColor DarkGray
        $contentsNote = if ($contents) { $contents } else { "el SDK, el runtime y el Windows App SDK que anote build-installer.ps1" }
        Write-Host "       Notas y tag declaran el contenido: $contentsNote" -ForegroundColor DarkGray
        if (-not $SkipTests -and -not $ResumeRelease) {
            $uiNote = if ($UiTests) { "unitarias + UI tests (app real)" } else { "solo unitarias (sin -UiTests)" }
            Write-Host "    Pruebas ya ejecutadas en este dry run: $uiNote" -ForegroundColor DarkGray
            if ($coverage) {
                Write-Host "    Cobertura de Core/: $($coverage.Percent)% (mínimo $CoreCoverageThreshold%)" -ForegroundColor DarkGray
            }
        }
        Show-UnitTestSummary $unitSummary
        Show-UiTestCoverage $uiSummary
        if ($tempNotes) { Remove-Item $tempNotes -Force -ErrorAction SilentlyContinue }
        Ok "Dry run completado."
        return
    }

    # ── 1. Bump de versión ───────────────────────────────────────────────────
    if ($ResumeRelease) {
        # Nada de esto se rehace: ya ocurrió en el corte que dejó el tag. Los dos assets ya se comprobaron
        # arriba, antes del -DryRun (`AUD-021`).
        # El tag puede estar solo en local si lo que falló fue su push.
        if (-not $remoteTag) {
            Info "El tag $tag aún no está en origin; subiéndolo..."
            if ((Invoke-Git push origin $tag) -ne 0) { Die "git push del tag falló." }
            Ok "Tag publicado."
        }
    }
    else {

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
    # Si el instalador falla, el .csproj vuelve a como estaba (`AUD-053`): antes quedaba con la versión nueva
    # y el corte siguiente lo encontraba como árbol sucio. build-installer.ps1 avisa con `throw`, no con un
    # código de salida, así que hacen falta las dos cosas.
    $installerError = $null
    try {
        & $buildScript @buildArgs
        if ($LASTEXITCODE -ne 0) { $installerError = "build-installer.ps1 terminó con código $LASTEXITCODE" }
    } catch { $installerError = $_.Exception.Message }
    $setup     = Join-Path $outputDir "FormatDiskPro-$Version-setup.exe"
    $setupHash = "$setup.sha256"
    if (-not $installerError -and -not (Test-Path $setup))     { $installerError = "no se encontró el instalador esperado: $setup" }
    # Lo genera build-installer.ps1. Es con lo que la app verifica la descarga mientras los instaladores
    # se publiquen sin firmar (UpdateService.VerifyInstallerAsync): si no se sube como asset, la
    # auto-actualización no puede verificar nada, borra el instalador y falla.
    if (-not $installerError -and -not (Test-Path $setupHash)) { $installerError = "no se encontró el checksum esperado: $setupHash" }
    # Qué lleva el instalador (`AUD-022`): sin esto, la release no podría declarar qué runtime empaqueta.
    $contentsFile = "$setup.contenido.txt"
    if (-not $installerError -and -not (Test-Path $contentsFile)) { $installerError = "no se encontró la anotación de contenido: $contentsFile" }
    if ($installerError) {
        if ($currentVersion -ne $Version) {
            [System.IO.File]::WriteAllText($csproj, $csprojRaw, (New-Object System.Text.UTF8Encoding($true)))
            Die "La compilación del instalador falló ($installerError). El .csproj vuelve a la ${currentVersion}: no ha cambiado nada."
        }
        Die "La compilación del instalador falló ($installerError)."
    }
    $sizeMB = [math]::Round((Get-Item $setup).Length / 1MB, 1)
    Ok "Instalador: $setup ($sizeMB MB)"
    $check = Test-InstallerHash -Setup $setup -HashFile $setupHash
    if ($check.Problem) { Die "El instalador recién compilado no coincide con su .sha256: $($check.Problem)." }
    Ok "Checksum: $setupHash ($($check.Hash))"
    $contents = [System.IO.File]::ReadAllText($contentsFile).Trim()
    Ok "Contenido: $contents"

    # ── 3. Commit + tag ──────────────────────────────────────────────────────
    # Añade todos los archivos rastreados modificados/eliminados (tracked changes).
    # Los archivos nuevos sin rastrear requieren 'git add' manual previo.
    Info "Preparando commit de release..."
    if ((Invoke-Git add -u) -ne 0) { Die "git add -u falló." }
    $staged = (Invoke-GitQuery diff --cached --name-only).Output
    if ($staged) {
        Info "Archivos incluidos en el commit:"
        $staged | ForEach-Object { Write-Host "    $_" -ForegroundColor DarkGray }
        if ((Invoke-Git commit -m "release: v$Version") -ne 0) { Die "git commit falló." }
        Ok "Commit de release creado."
    } else {
        Info "Sin cambios que commitear; se etiqueta el HEAD actual."
    }
    Info "Creando tag $tag..."
    # El hash del instalador va en el mensaje del tag: es lo que deja a -ResumeRelease comprobar que publica
    # el binario de ESTE corte y no uno recompilado después (`AUD-021`). Y lo que lleva dentro (`AUD-022`).
    if ((Invoke-Git tag -a $tag -m "FormatDiskPro $tag" -m "SHA-256 del instalador: $($check.Hash)" -m "Contenido: $contents") -ne 0) { Die "git tag falló." }

    # ── 4. Push ──────────────────────────────────────────────────────────────
    # Vía Invoke-Git a propósito: git escribe el resumen del push por stderr y, con
    # $ErrorActionPreference = "Stop", eso abortaría el script DESPUÉS de haber empujado la rama,
    # dejando el release a medias (sin tag ni GitHub Release). Ver la nota de Invoke-Git.
    Info "Push de la rama y el tag a origin..."
    if ((Invoke-Git push origin $branch) -ne 0) { Die "git push de la rama falló." }
    if ((Invoke-Git push origin $tag) -ne 0) {
        Die "git push del tag falló. La rama YA está subida y el tag existe en local: cuando lo arregles, retoma con  .\release.ps1 -Version $Version -ResumeRelease"
    }
    Ok "Rama y tag publicados."

    }   # fin del bloque que -ResumeRelease se salta

    # ── 5. GitHub Release ────────────────────────────────────────────────────
    # gh y su sesión ya se comprobaron antes de la primera prueba (`AUD-053`). El instalador se vuelve a
    # cotejar con su .sha256 justo antes de subirlo: es lo último que puede cambiar en disco (`AUD-021`).
    $check = Test-InstallerHash -Setup $setup -HashFile $setupHash
    if ($check.Problem) {
        Die "No se publica: $($check.Problem). La app no podría verificar la descarga y la auto-actualización fallaría para todos. El tag $tag ya existe: recompila el instalador desde ese commit o rehaz el corte."
    }

    # Las notas declaran qué lleva el instalador (`AUD-022`, `AUD-063`): ante un aviso de seguridad del runtime
    # de .NET, es lo que dice qué versiones de la app lo traen. Se añade a una copia, también con -NotesFile,
    # que es un archivo del usuario y no se toca.
    $publishNotes = $notesPath
    if ($contents) {
        $publishNotes = Join-Path $env:TEMP "fdp_release_${Version}_publicar.md"
        $withContents = [System.IO.File]::ReadAllText($notesPath).TrimEnd() + "`n`n**Contenido del instalador:** $contents`n"
        [System.IO.File]::WriteAllText($publishNotes, $withContents, (New-Object System.Text.UTF8Encoding($false)))
    }

    Info "Creando el GitHub Release..."
    & $gh release create $tag --title "FormatDiskPro $tag" --notes-file $publishNotes $setup $setupHash
    if ($LASTEXITCODE -ne 0) {
        Die "gh release create falló. El tag y el instalador ya están: cuando lo arregles, retoma SOLO este paso con  .\release.ps1 -Version $Version -ResumeRelease"
    }

    if ($tempNotes) { Remove-Item $tempNotes -Force -ErrorAction SilentlyContinue }
    if ($publishNotes -ne $notesPath) { Remove-Item $publishNotes -Force -ErrorAction SilentlyContinue }
    Write-Host ""
    Ok "Release $tag publicado: https://github.com/xfiberex/FormatDiskPro/releases/tag/$tag"
    # Se repite aquí a propósito: cuando el corte sale bien, este es el único bloque que se lee.
    if ($coverage) { Ok "Cobertura de Core/: $($coverage.Percent)% ($($coverage.Covered)/$($coverage.Total) líneas)." }
    Show-UnitTestSummary $unitSummary
    Show-UiTestCoverage $uiSummary
}
finally {
    # El token que se rellenó desde la credencial cacheada de git NO puede sobrevivir al script.
    # $env: es del PROCESO, así que sin esto sigue vivo en la terminal que lanzó el corte y lo hereda
    # cualquier proceso que se abra después desde ella. Se limpia solo si lo puso este script: si ya
    # venía del entorno del usuario, es suyo y no nos toca borrarlo.
    if ($script:ClearedGhToken) { $env:GH_TOKEN = $null }
    $env:RestoreLockedMode = $prevLockedMode
    Pop-Location
}
