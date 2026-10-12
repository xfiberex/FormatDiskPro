#Requires -Version 5.1
# Reproduce, fuera de la app, cómo lanzan los servicios sus procesos, con los mismos argumentos:
#   A) Format-Volume que falla (letra inexistente): qué texto llega por stderr, que es lo que la app
#      enseña en el diálogo de error. No formatea nada.
#   B) chkdsk /f sobre la USB de pruebas con un archivo abierto en ella: la app escribe una sola «N» y no
#      cierra la entrada estándar. chkdsk no llega a reparar nada: se le responde que no.
param(
    [Parameter(Mandatory)][char]$UsbLetter,
    [char]$MissingLetter = 'Q'
)
$ErrorActionPreference = 'Stop'

function New-Psi([string]$file, [string]$arguments, [bool]$stdin) {
    $psi = New-Object Diagnostics.ProcessStartInfo
    $psi.FileName = $file; $psi.Arguments = $arguments
    $psi.UseShellExecute = $false; $psi.CreateNoWindow = $true
    $psi.RedirectStandardOutput = $true; $psi.RedirectStandardError = $true
    $psi.RedirectStandardInput = $stdin
    $psi
}

"=== A) stderr de un Format-Volume que falla (lo que la app pone en el dialogo de error) ==="
if (Test-Path "${MissingLetter}:\") { throw "La letra $MissingLetter existe: elige otra." }
$script = "Format-Volume -DriveLetter $MissingLetter -FileSystem exFAT -AllocationUnitSize 131072 -Confirm:`$false -Force"
$enc = [Convert]::ToBase64String([Text.Encoding]::Unicode.GetBytes($script))
$p = [Diagnostics.Process]::Start((New-Psi 'powershell.exe' "-NonInteractive -NoProfile -EncodedCommand $enc" $false))
$o = $p.StandardOutput.ReadToEndAsync(); $e = $p.StandardError.ReadToEndAsync(); $p.WaitForExit()
"codigo de salida: $($p.ExitCode)"
"stdout ($($o.Result.Length) caracteres): [$($o.Result.Trim())]"
$err = $e.Result
"stderr ($($err.Length) caracteres), primeros 600:"
$err.Substring(0, [Math]::Min(600, $err.Length))

""
"=== B) chkdsk ${UsbLetter}: /f con un archivo abierto en la unidad ==="
$vol = Get-Volume -DriveLetter $UsbLetter
if ($vol.DriveType -ne 'Removable') { throw "La unidad ${UsbLetter}: no es extraible: no se toca." }
$held = "${UsbLetter}:\__audit_open_handle__.tmp"
$fs = [IO.File]::Open($held, 'Create', 'ReadWrite', 'None')
try {
    $c = [Diagnostics.Process]::Start((New-Psi 'chkdsk.exe' "${UsbLetter}: /f" $true))
    $out = $c.StandardOutput.ReadToEndAsync(); $cerr = $c.StandardError.ReadToEndAsync()
    $c.StandardInput.WriteLine('N'); $c.StandardInput.Flush()      # lo mismo que CheckDisk.cs:64-65
    $sw = [Diagnostics.Stopwatch]::StartNew()
    $alone = $c.WaitForExit(25000)
    "Con una sola 'N' y la entrada sin cerrar: salio por si solo en 25 s = $alone"
    if (-not $alone) {
        $c.StandardInput.Close()                                    # lo que haria cerrar la entrada
        $eof = $c.WaitForExit(15000)
        "Tras cerrar la entrada estandar: salio = $eof ($([math]::Round($sw.Elapsed.TotalSeconds,1)) s en total)"
        if (-not $eof) { $c.Kill(); [void]$c.WaitForExit(5000); "No salio: se termino con Kill." }
    }
    "codigo de salida: $($c.ExitCode)"
    "salida de chkdsk (preguntas incluidas):"
    ($out.Result -split "`r?`n" | Where-Object { $_.Trim() } | ForEach-Object { "  | $_" })
}
finally {
    $fs.Dispose()
    Remove-Item $held -Force -ErrorAction SilentlyContinue
}
"Unidad ${UsbLetter}: tras la prueba -> " + ((Get-Volume -DriveLetter $UsbLetter | ForEach-Object { "$($_.FileSystemLabel) $($_.FileSystem) $($_.HealthStatus)" }))
