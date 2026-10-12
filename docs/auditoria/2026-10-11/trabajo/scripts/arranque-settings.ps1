#Requires -Version 5.1
# Arranca la app con un settings.json VÁLIDO como JSON pero con un valor nulo donde el código espera
# una lista o un texto, y observa si aparece ventana. Respalda y restaura settings.json e history.log
# del usuario pase lo que pase. No pulsa ningún control.
param(
    [Parameter(Mandatory)][string]$Exe,
    [Parameter(Mandatory)][string]$Case,          # nombre del caso, para el informe
    [Parameter(Mandatory)][string]$Property,      # propiedad a poner a null
    [string]$EvidenceDir
)
$ErrorActionPreference = 'Stop'
Add-Type @'
using System; using System.Text; using System.Collections.Generic; using System.Runtime.InteropServices;
public static class Wv {
    public delegate bool EnumProc(IntPtr h, IntPtr l);
    [DllImport("user32.dll")] public static extern bool EnumWindows(EnumProc cb, IntPtr l);
    [DllImport("user32.dll")] public static extern uint GetWindowThreadProcessId(IntPtr h, out uint pid);
    [DllImport("user32.dll")] public static extern bool IsWindowVisible(IntPtr h);
    public static int VisibleTop(uint pid) {
        int n = 0;
        EnumWindows((h, l) => { uint p; GetWindowThreadProcessId(h, out p); if (p == pid && IsWindowVisible(h)) n++; return true; }, IntPtr.Zero);
        return n;
    }
}
'@

$dir      = Join-Path $env:APPDATA 'FormatDiskPro'
$settings = Join-Path $dir 'settings.json'
$history  = Join-Path $dir 'history.log'
$bakS = [IO.File]::ReadAllBytes($settings)
$bakH = [IO.File]::ReadAllBytes($history)

$p = $null
try {
    $obj = Get-Content $settings -Raw | ConvertFrom-Json
    $obj.$Property = $null
    [IO.File]::WriteAllText($settings, ($obj | ConvertTo-Json -Depth 6), (New-Object Text.UTF8Encoding($false)))
    "== Caso: $Case  ($Property = null) =="
    "settings.json de prueba: " + ((Get-Content $settings -Raw) -replace '\s+', ' ')

    $p = Start-Process -FilePath $Exe -PassThru
    Start-Sleep -Seconds 9
    $p.Refresh()
    "proceso vivo        : $(-not $p.HasExited)"
    if (-not $p.HasExited) {
        "MainWindowHandle    : $($p.MainWindowHandle)"
        "ventanas visibles   : $([Wv]::VisibleTop([uint32]$p.Id))"
        "hilos / memoria     : $($p.Threads.Count) / $([math]::Round($p.WorkingSet64/1MB)) MB"
    } else { "codigo de salida    : $($p.ExitCode)" }

    $new = @(Get-Content $history -Encoding UTF8 | Select-Object -Skip (@([Text.Encoding]::UTF8.GetString($bakH) -split "`r?`n" | Where-Object { $_ }).Count))
    "lineas nuevas en el historial: $($new.Count)"
    foreach ($l in $new) { "  " + $l.Substring(0, [Math]::Min(330, $l.Length)) }
    if ($EvidenceDir -and $new.Count -gt 0) {
        $new | ForEach-Object { $_.Substring(0, [Math]::Min(1200, $_.Length)) } |
            Set-Content -Encoding UTF8 (Join-Path $EvidenceDir "arranque-$Case-historial.txt")
    }
}
finally {
    if ($p -and -not $p.HasExited) { try { $p.Kill(); [void]$p.WaitForExit(4000) } catch {} }
    [IO.File]::WriteAllBytes($settings, $bakS)
    [IO.File]::WriteAllBytes($history,  $bakH)
    "ajustes e historial del usuario restaurados"
}
