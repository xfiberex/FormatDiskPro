#Requires -Version 5.1
# Diagnóstico de solo lectura del cierre de la ventana: lanza la app en reposo (sin operación en curso),
# enumera sus ventanas de nivel superior y le pide cerrarse como lo hacen Alt+F4, la barra de tareas,
# `taskkill` sin /F o un instalador: con el mensaje WM_CLOSE. No pulsa ningún control de la app.
param([Parameter(Mandatory)][string]$Exe)
$ErrorActionPreference = 'Stop'
Add-Type @'
using System; using System.Text; using System.Collections.Generic; using System.Runtime.InteropServices;
public static class Wn {
    public delegate bool EnumProc(IntPtr h, IntPtr l);
    [DllImport("user32.dll")] public static extern bool EnumWindows(EnumProc cb, IntPtr l);
    [DllImport("user32.dll")] public static extern uint GetWindowThreadProcessId(IntPtr h, out uint pid);
    [DllImport("user32.dll", CharSet=CharSet.Unicode)] public static extern int GetClassName(IntPtr h, StringBuilder s, int n);
    [DllImport("user32.dll", CharSet=CharSet.Unicode)] public static extern int GetWindowText(IntPtr h, StringBuilder s, int n);
    [DllImport("user32.dll")] public static extern bool IsWindowVisible(IntPtr h);
    [DllImport("user32.dll")] public static extern bool IsWindow(IntPtr h);
    [DllImport("user32.dll")] public static extern IntPtr GetWindow(IntPtr h, uint cmd);
    [DllImport("user32.dll")] public static extern bool PostMessage(IntPtr h, uint m, IntPtr w, IntPtr l);
    public static List<string> Top(uint pid) {
        var r = new List<string>();
        EnumWindows((h, l) => {
            uint p; GetWindowThreadProcessId(h, out p);
            if (p == pid) {
                var c = new StringBuilder(256); GetClassName(h, c, 256);
                var t = new StringBuilder(256); GetWindowText(h, t, 256);
                r.Add(string.Format("0x{0:X} | clase={1} | titulo=[{2}] | visible={3} | owner=0x{4:X}", h.ToInt64(), c, t, IsWindowVisible(h), GetWindow(h, 4).ToInt64()));
            }
            return true;
        }, IntPtr.Zero);
        return r;
    }
}
'@
$WM_CLOSE = 0x0010

function Start-App {
    $p = Start-Process -FilePath $Exe -PassThru
    $sw = [Diagnostics.Stopwatch]::StartNew()
    while ($p.MainWindowHandle -eq 0 -and $sw.ElapsedMilliseconds -lt 20000) { Start-Sleep -Milliseconds 50; $p.Refresh() }
    Start-Sleep -Seconds 6
    $p.Refresh(); $p
}
function Wait-Gone($p, $h, [int]$ms) {
    $ex = $p.WaitForExit($ms)
    'proceso salio={0} | la ventana existe={1} visible={2}' -f $ex, [Wn]::IsWindow($h), [Wn]::IsWindowVisible($h)
}

"=== Prueba 1: WM_CLOSE a la ventana principal, dos veces ==="
$p = Start-App; $h = $p.MainWindowHandle
"MainWindowHandle = 0x{0:X}" -f $h.ToInt64()
"Ventanas de nivel superior del proceso:"; [Wn]::Top([uint32]$p.Id) | ForEach-Object { "  $_" }
"1er WM_CLOSE enviado: " + [Wn]::PostMessage($h, $WM_CLOSE, [IntPtr]::Zero, [IntPtr]::Zero)
"  tras 5 s -> " + (Wait-Gone $p $h 5000)
if (-not $p.HasExited) {
    "2o WM_CLOSE enviado: " + [Wn]::PostMessage($h, $WM_CLOSE, [IntPtr]::Zero, [IntPtr]::Zero)
    "  tras 5 s -> " + (Wait-Gone $p $h 5000)
}
if (-not $p.HasExited) { "Sigue viva tras dos WM_CLOSE: se termina con Kill."; $p.Kill(); $p.WaitForExit(3000) | Out-Null }
Start-Sleep -Seconds 2

"=== Prueba 2: taskkill sin /F (cierre ordenado) ==="
$p = Start-App; $h = $p.MainWindowHandle
$out = (& taskkill.exe /PID $p.Id 2>&1 | Out-String).Trim()
"taskkill dijo: $out"
"  tras 6 s -> " + (Wait-Gone $p $h 6000)
if (-not $p.HasExited) { "Sigue viva: se termina con Kill."; $p.Kill(); $p.WaitForExit(3000) | Out-Null }
