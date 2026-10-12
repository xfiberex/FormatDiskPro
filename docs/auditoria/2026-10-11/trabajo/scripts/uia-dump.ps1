#Requires -Version 5.1
# Solo lectura: lanza FormatDiskPro, vuelca el árbol de UI Automation de la ventana principal (lo que lee
# un lector de pantalla), lista los ítems del selector de unidades y recorre el orden de tabulación.
# No pulsa ningún botón de acción. Cierra la app por UI Automation al terminar.
param(
    [Parameter(Mandatory)][string]$Exe,
    [Parameter(Mandatory)][string]$OutDir,
    [int]$Tabs = 45
)
$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName UIAutomationClient, UIAutomationTypes, System.Windows.Forms
Add-Type @'
using System; using System.Runtime.InteropServices;
public static class Fg { [DllImport("user32.dll")] public static extern bool SetForegroundWindow(IntPtr h); }
'@
$AE = [Windows.Automation.AutomationElement]
New-Item -ItemType Directory -Force -Path $OutDir | Out-Null

function Describe([Windows.Automation.AutomationElement]$e) {
    $c = $e.Current
    $lb = ''
    try { if ($c.LabeledBy) { $lb = $c.LabeledBy.Current.Name } } catch {}
    '{0} | name=[{1}] | id=[{2}] | focusable={3} | enabled={4} | labeledBy=[{5}] | help=[{6}] | accessKey=[{7}] | accel=[{8}]' -f `
        $c.ControlType.ProgrammaticName.Replace('ControlType.',''), $c.Name, $c.AutomationId,
        $c.IsKeyboardFocusable, $c.IsEnabled, $lb, $c.HelpText, $c.AccessKey, $c.AcceleratorKey
}

function Walk([Windows.Automation.AutomationElement]$e, [int]$depth, [System.Collections.Generic.List[string]]$out) {
    if ($depth -gt 14) { return }
    $out.Add(('  ' * $depth) + (Describe $e))
    $w = [Windows.Automation.TreeWalker]::ControlViewWalker
    $child = $w.GetFirstChild($e)
    while ($child) { Walk $child ($depth + 1) $out; $child = $w.GetNextSibling($child) }
}

$p = Start-Process -FilePath $Exe -PassThru
try {
    $sw = [Diagnostics.Stopwatch]::StartNew()
    while ($p.MainWindowHandle -eq 0 -and $sw.ElapsedMilliseconds -lt 20000) { Start-Sleep -Milliseconds 50; $p.Refresh() }
    Start-Sleep -Seconds 5      # que termine la carga de la tarjeta de unidad
    $root = $AE::FromHandle($p.MainWindowHandle)

    $tree = New-Object 'System.Collections.Generic.List[string]'
    Walk $root 0 $tree
    $tree | Set-Content -Encoding UTF8 (Join-Path $OutDir 'uia-arbol-principal.txt')
    "Elementos en el arbol: $($tree.Count)"

    # Controles interactivos sin nombre accesible
    "--- Interactivos SIN nombre ---"
    $tree | Where-Object { $_ -match 'focusable=True' -and $_ -match 'name=\[\]' } | ForEach-Object { $_.Trim() }

    # Selector de unidades: nombre propio y nombre de cada ítem
    "--- Selector de unidades ---"
    $pick = $root.FindFirst([Windows.Automation.TreeScope]::Descendants,
        (New-Object Windows.Automation.PropertyCondition($AE::AutomationIdProperty, 'DrivePicker')))
    if ($pick) {
        "DrivePicker: " + (Describe $pick)
        $ec = $pick.GetCurrentPattern([Windows.Automation.ExpandCollapsePattern]::Pattern)
        $ec.Expand(); Start-Sleep -Milliseconds 900
        $items = $pick.FindAll([Windows.Automation.TreeScope]::Descendants,
            (New-Object Windows.Automation.PropertyCondition($AE::ControlTypeProperty, [Windows.Automation.ControlType]::ListItem)))
        "Items: $($items.Count)"
        foreach ($i in $items) { "  item: " + (Describe $i) }
        $ec.Collapse(); Start-Sleep -Milliseconds 400
    } else { "DrivePicker NO encontrado por AutomationId" }

    # Orden de tabulación
    "--- Orden de tabulacion ($Tabs pulsaciones) ---"
    [void][Fg]::SetForegroundWindow($p.MainWindowHandle); Start-Sleep -Milliseconds 500
    $order = New-Object 'System.Collections.Generic.List[string]'
    for ($i = 1; $i -le $Tabs; $i++) {
        [System.Windows.Forms.SendKeys]::SendWait('{TAB}'); Start-Sleep -Milliseconds 140
        try {
            $f = $AE::FocusedElement
            $fc = $f.Current
            $order.Add(('{0,2}. {1} | name=[{2}] | id=[{3}]' -f $i, $fc.ControlType.ProgrammaticName.Replace('ControlType.',''), $fc.Name, $fc.AutomationId))
        } catch { $order.Add("$i. (sin foco legible)") }
    }
    $order | Set-Content -Encoding UTF8 (Join-Path $OutDir 'orden-tabulacion.txt')
    $order
}
finally {
    try {
        $p.Refresh()
        $AE::FromHandle($p.MainWindowHandle).GetCurrentPattern([Windows.Automation.WindowPattern]::Pattern).Close()
    } catch {}
    if (-not $p.WaitForExit(8000)) { try { $p.Kill() } catch {} ; "cierre: KILL" } else { "cierre: uia" }
}
