#Requires -Version 5.1
# DESTRUCTIVO, solo sobre la partición indicada de la USB de pruebas.
# Reproduce lo que hace la app al CANCELAR un formato completo lanzado con Format-Volume
# (FormatProcess.cs:55-61): arranca powershell.exe con el mismo script y, a los pocos segundos, mata el
# árbol de procesos. Después observa si el formato se detuvo o si siguió hasta el final sin la app.
# Al terminar deja la partición como estaba (FAT32, etiqueta original).
param(
    [Parameter(Mandatory)][char]$Letter,
    [Parameter(Mandatory)][int]$ExpectedDiskNumber,
    [int]$KillAfterSeconds = 4,
    [int]$WatchSeconds = 150
)
$ErrorActionPreference = 'Stop'

# ── Guardas: extraíble, en el disco USB esperado, pequeña ─────────────────────────────────────────
$part = Get-Partition -DriveLetter $Letter
$disk = $part | Get-Disk
$vol  = Get-Volume -DriveLetter $Letter
if ($disk.Number -ne $ExpectedDiskNumber) { throw "ABORTO: ${Letter}: esta en el disco $($disk.Number), no en el $ExpectedDiskNumber." }
if ($disk.BusType -ne 'USB' -or $disk.IsSystem -or $disk.IsBoot) { throw "ABORTO: el disco $($disk.Number) no es una USB de datos." }
if ($vol.DriveType -ne 'Removable') { throw "ABORTO: ${Letter}: no es extraible." }
if ($part.Size -gt 2GB) { throw "ABORTO: la particion mide mas de 2 GB; esta prueba usa la pequena." }
$files = @(Get-ChildItem "${Letter}:\" -Force -ErrorAction SilentlyContinue | Where-Object Name -ne 'System Volume Information')
if ($files.Count -gt 0) { throw "ABORTO: ${Letter}: no esta vacia ($($files.Count) elementos)." }
$origLabel = $vol.FileSystemLabel; $origFs = $vol.FileSystem
"Destino: ${Letter}: · disco $($disk.Number) ($($disk.FriendlyName), $($disk.BusType)) · $([math]::Round($part.Size/1MB)) MB · $origFs '$origLabel' · vacia"

function State { $v = Get-Volume -DriveLetter $Letter -ErrorAction SilentlyContinue; if ($v) { "fs=[$($v.FileSystem)] etiqueta=[$($v.FileSystemLabel)] estado=[$($v.OperationalStatus)]" } else { 'sin volumen' } }

# ── El script EXACTO que construye FormatLogic.BuildVolumeScript para un formato completo exFAT ───
$script = "Format-Volume -DriveLetter $Letter -FileSystem exFAT -AllocationUnitSize 131072 -NewFileSystemLabel 'CANCELTEST' -Full -Confirm:`$false -Force"
$enc = [Convert]::ToBase64String([Text.Encoding]::Unicode.GetBytes($script))
$psi = New-Object Diagnostics.ProcessStartInfo
$psi.FileName = 'powershell.exe'; $psi.Arguments = "-NonInteractive -NoProfile -EncodedCommand $enc"
$psi.UseShellExecute = $false; $psi.CreateNoWindow = $true
$psi.RedirectStandardOutput = $true; $psi.RedirectStandardError = $true

$sw = [Diagnostics.Stopwatch]::StartNew()
$p = [Diagnostics.Process]::Start($psi)
[void]$p.StandardOutput.ReadToEndAsync(); [void]$p.StandardError.ReadToEndAsync()
Start-Sleep -Seconds $KillAfterSeconds
"t=$([math]::Round($sw.Elapsed.TotalSeconds,1)) s · antes de cancelar: powershell vivo=$(-not $p.HasExited) · $(State)"
if ($p.HasExited) { "El formato termino antes de poder cancelarlo (codigo $($p.ExitCode)): prueba no concluyente." }
else {
    & taskkill.exe /PID $p.Id /T /F | Out-Null            # equivale a proc.Kill(entireProcessTree: true)
    [void]$p.WaitForExit(5000)
    "t=$([math]::Round($sw.Elapsed.TotalSeconds,1)) s · CANCELADO: arbol de powershell terminado (salio=$($p.HasExited))"

    $counter = "\PhysicalDisk($ExpectedDiskNumber*)\Disk Write Bytes/sec"
    $finished = $false
    while ($sw.Elapsed.TotalSeconds -lt $WatchSeconds) {
        $w = 0; try { $w = [math]::Round(((Get-Counter $counter -SampleInterval 2 -MaxSamples 1).CounterSamples | Measure-Object CookedValue -Sum).Sum / 1MB, 1) } catch {}
        $st = State
        "t=$([math]::Round($sw.Elapsed.TotalSeconds,1)) s · escritura en el disco ${ExpectedDiskNumber}: $w MB/s · $st"
        if ($st -match 'etiqueta=\[CANCELTEST\]') { $finished = $true; break }
        Start-Sleep -Seconds 4
    }
    if ($finished) { "RESULTADO: el formato SIGUIO y TERMINO $([math]::Round($sw.Elapsed.TotalSeconds)) s despues de arrancar, con la app ya en 'cancelado'." }
    else           { "RESULTADO: tras $WatchSeconds s el volumen no llego a exFAT 'CANCELTEST': $(State)" }
}

# ── Dejarlo como estaba ───────────────────────────────────────────────────────────────────────────
"Restaurando ${Letter}: a $origFs '$origLabel'…"
$tries = 0
while ($true) {
    try { Format-Volume -DriveLetter $Letter -FileSystem FAT32 -NewFileSystemLabel $origLabel -Confirm:$false -Force | Out-Null; break }
    catch { $tries++; if ($tries -ge 12) { throw }; Start-Sleep -Seconds 10 }
}
"Estado final: $(State) (reintentos de restauracion: $tries)"
