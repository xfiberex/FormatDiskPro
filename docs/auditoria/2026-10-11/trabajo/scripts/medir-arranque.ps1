#Requires -Version 5.1
# Mide el arranque en frío/templado de FormatDiskPro.exe: tiempo hasta que existe la ventana principal
# y hasta que su árbol de UI Automation ya tiene el selector de unidades (ventana utilizable).
# Solo lectura: lanza la app, mide y la cierra por UI Automation (WindowPattern.Close).
param(
    [Parameter(Mandatory)][string]$Exe,
    [int]$Runs = 5,
    [string]$Label = ''
)
$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName UIAutomationClient, UIAutomationTypes

$idCond = New-Object Windows.Automation.PropertyCondition(
    [Windows.Automation.AutomationElement]::AutomationIdProperty, 'DrivePicker')

$rows = @()
for ($i = 1; $i -le $Runs; $i++) {
    $sw = [Diagnostics.Stopwatch]::StartNew()
    $p = Start-Process -FilePath $Exe -PassThru
    while ($p.MainWindowHandle -eq 0 -and $sw.ElapsedMilliseconds -lt 30000) {
        Start-Sleep -Milliseconds 10
        $p.Refresh()
    }
    $msWindow = $sw.ElapsedMilliseconds

    # Ventana utilizable: el selector de unidades ya está en el árbol de automatización.
    $root = $null; $picker = $null
    while ($sw.ElapsedMilliseconds -lt 30000 -and -not $picker) {
        try {
            if (-not $root) { $root = [Windows.Automation.AutomationElement]::FromHandle($p.MainWindowHandle) }
            $picker = $root.FindFirst([Windows.Automation.TreeScope]::Descendants, $idCond)
        } catch { $root = $null }
        if (-not $picker) { Start-Sleep -Milliseconds 20 }
    }
    $msUsable = if ($picker) { $sw.ElapsedMilliseconds } else { -1 }

    Start-Sleep -Seconds 6          # reposo: que terminen la carga de unidades y la consulta de actualización
    $p.Refresh()
    $ws = [math]::Round($p.WorkingSet64 / 1MB)
    $pm = [math]::Round($p.PrivateMemorySize64 / 1MB)

    $closed = 'uia'
    try {
        $root = [Windows.Automation.AutomationElement]::FromHandle($p.MainWindowHandle)
        $root.GetCurrentPattern([Windows.Automation.WindowPattern]::Pattern).Close()
    } catch { $closed = 'error-uia' }
    if (-not $p.WaitForExit(8000)) { $p.Kill(); $closed = 'KILL' }

    $rows += [pscustomobject]@{
        Run = $i; VentanaMs = $msWindow; UtilizableMs = $msUsable
        WorkingSetMB = $ws; PrivadaMB = $pm; Cierre = $closed
    }
    Start-Sleep -Seconds 2
}

"== $Label =="
"Exe: $Exe"
$rows | Format-Table -AutoSize | Out-String
$warm = $rows | Select-Object -Skip 1
"Templado (sin el primer arranque): ventana media {0:N0} ms (min {1}, max {2}) | utilizable media {3:N0} ms" -f `
    ($warm | Measure-Object VentanaMs -Average).Average,
    ($warm | Measure-Object VentanaMs -Minimum).Minimum,
    ($warm | Measure-Object VentanaMs -Maximum).Maximum,
    ($warm | Measure-Object UtilizableMs -Average).Average
