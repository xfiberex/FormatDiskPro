<#
.SYNOPSIS
    Captura las capturas de pantalla del README conduciendo la app real por UI Automation.

.DESCRIPTION
    Lanza el FormatDiskPro.exe ya compilado, fuerza tema/idioma/unidad, espera a que la tarjeta de la
    unidad se rellene y guarda un PNG por tema en docs/screenshots/. En tema oscuro captura además el
    diálogo de salud S.M.A.R.T., que es la característica que mejor distingue al proyecto.

    REQUIERE TERMINAL ELEVADA: la app es `requireAdministrator` y un proceso no elevado no puede ni
    lanzarla sin UAC ni automatizar su ventana (mismo motivo que los UI tests).

    Su settings.json vive en %AppData%\FormatDiskPro — el MISMO archivo que usa la instalación real del
    usuario. El script lo RESPALDA antes de tocarlo y lo RESTAURA siempre, incluso si falla.

    Requiere sesión de escritorio interactiva y desatendida: la captura es una copia literal de lo que
    hay en pantalla (Graphics.CopyFromScreen sobre el rectángulo de la ventana), así que la ventana debe
    quedar en primer plano y sin nada encima. No muevas el ratón ni cambies de ventana mientras corre.

.PARAMETER Exe
    Ruta al FormatDiskPro.exe. Por defecto, el más reciente bajo src\FormatDiskPro\bin\.

.PARAMETER Theme
    light, dark o both (por defecto).

.PARAMETER Language
    Idioma de la UI: es, en, pt, fr, it. Por defecto es.

.PARAMETER Drive
    Letra de la unidad a preseleccionar. Por defecto, la primera que NO sea la del sistema (la del
    sistema sale como [Protegido], con los controles de formato deshabilitados: mala foto del producto).

.PARAMETER SkipHealth
    No captura el diálogo S.M.A.R.T.

.EXAMPLE
    .\tools\capture-screenshots.ps1
    .\tools\capture-screenshots.ps1 -Theme dark -Language en -Drive H
#>
[CmdletBinding()]
param(
    [string]$Exe,
    [ValidateSet('light', 'dark', 'both')][string]$Theme = 'both',
    [ValidateSet('es', 'en', 'pt', 'fr', 'it')][string]$Language = 'es',
    [string]$Drive,
    [string]$OutDir,
    [switch]$SkipHealth
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

$repoRoot = Split-Path -Parent $PSScriptRoot
if (-not $OutDir) { $OutDir = Join-Path $repoRoot 'docs\screenshots' }

Add-Type -AssemblyName System.Drawing
Add-Type -AssemblyName System.Windows.Forms
Add-Type -AssemblyName UIAutomationClient
Add-Type -AssemblyName UIAutomationTypes

Add-Type @'
using System;
using System.Runtime.InteropServices;
public static class Win32Capture
{
    [DllImport("user32.dll")] public static extern bool SetProcessDPIAware();
    [DllImport("user32.dll")] public static extern bool SetForegroundWindow(IntPtr hWnd);
    [DllImport("user32.dll")] public static extern bool MoveWindow(IntPtr hWnd, int x, int y, int w, int h, bool repaint);
    [DllImport("user32.dll")] public static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);
    [DllImport("dwmapi.dll")] public static extern int DwmGetWindowAttribute(IntPtr hWnd, int attr, out RECT value, int size);
    [StructLayout(LayoutKind.Sequential)] public struct RECT { public int Left, Top, Right, Bottom; }
    public const int DWMWA_EXTENDED_FRAME_BOUNDS = 9;
}
'@

# El proceso de PowerShell debe ser DPI-aware o las coordenadas de pantalla vendrían virtualizadas
# (en un monitor al 125/150 % la captura saldría desplazada y recortada).
[void][Win32Capture]::SetProcessDPIAware()

function Assert-Elevated {
    $id = [Security.Principal.WindowsIdentity]::GetCurrent()
    $isAdmin = (New-Object Security.Principal.WindowsPrincipal($id)).IsInRole(
        [Security.Principal.WindowsBuiltInRole]::Administrator)
    if (-not $isAdmin) {
        throw "Este script requiere una terminal ELEVADA: FormatDiskPro.exe es requireAdministrator, " +
              "y un proceso no elevado no puede automatizar su ventana. Reabre PowerShell como administrador."
    }
}

function Resolve-Exe {
    if ($Exe) {
        if (-not (Test-Path $Exe)) { throw "No existe el ejecutable: $Exe" }
        return (Resolve-Path $Exe).Path
    }
    # Se prefiere RELEASE: es el binario que se distribuye, y es el que deben retratar unas capturas
    # publicadas. Solo se cae a Debug si no hay build de Release (y entonces se avisa).
    $releaseRoot = Join-Path $repoRoot 'src\FormatDiskPro\bin\Release'
    $candidate = Get-ChildItem -Path $releaseRoot -Filter 'FormatDiskPro.exe' -Recurse -ErrorAction SilentlyContinue |
        Sort-Object LastWriteTime -Descending | Select-Object -First 1

    if (-not $candidate) {
        $debugRoot = Join-Path $repoRoot 'src\FormatDiskPro\bin\Debug'
        $candidate = Get-ChildItem -Path $debugRoot -Filter 'FormatDiskPro.exe' -Recurse -ErrorAction SilentlyContinue |
            Sort-Object LastWriteTime -Descending | Select-Object -First 1
        if ($candidate) { Write-Warning "No hay build de Release: se usará el de Debug. Para las capturas definitivas: dotnet build -c Release" }
    }

    if (-not $candidate) { throw "No se encontró FormatDiskPro.exe. Ejecuta primero: dotnet build -c Release" }
    return $candidate.FullName
}

function Get-SettingsPath { Join-Path $env:APPDATA 'FormatDiskPro\settings.json' }

# La unidad del sistema aparece como "[Protegido] C:" con TODOS los controles de formato deshabilitados:
# es una foto pésima del producto. Se elige la primera unidad lista que no sea esa.
function Resolve-CaptureDrive {
    if ($Drive) { return $Drive.TrimEnd(':').ToUpperInvariant() }

    $systemLetter = ([System.IO.Path]::GetPathRoot($env:SystemRoot))[0]
    $candidate = [System.IO.DriveInfo]::GetDrives() |
        Where-Object { $_.IsReady -and $_.Name[0] -ne $systemLetter -and $_.DriveType -in 'Fixed', 'Removable' } |
        Select-Object -First 1

    if (-not $candidate) {
        Write-Warning "No hay ninguna unidad distinta de la del sistema: la captura saldrá con la unidad [Protegido]."
        return $null
    }
    return $candidate.Name[0]
}

# settings.json a medida: tema, idioma y unidad fijos, y LastVersionSeen = versión del .exe para que NO
# salte el diálogo de Novedades por encima de la ventana que queremos fotografiar.
function Set-CaptureSettings([string]$exePath, [string]$themeName, [string]$driveLetter) {
    $settingsPath = Get-SettingsPath
    $dir = Split-Path -Parent $settingsPath
    if (-not (Test-Path $dir)) { New-Item -ItemType Directory -Path $dir -Force | Out-Null }

    $v = [System.Diagnostics.FileVersionInfo]::GetVersionInfo($exePath)
    $seen = "{0}.{1}.{2}" -f $v.FileMajorPart, $v.FileMinorPart, $v.FileBuildPart

    $settings = [ordered]@{
        Language         = $Language
        Theme            = $themeName
        LastDriveLetter  = $driveLetter
        LastVersionSeen  = $seen
        UserPresets      = @()
        NotifyOnFinish   = $false        # nada de sonidos ni parpadeos durante la captura
        SecureWipePasses = 1
        SmallFat32SizeGb = 32
    }
    $settings | ConvertTo-Json | Set-Content -Path $settingsPath -Encoding UTF8
}

function Find-MainWindow([int]$processId, [int]$timeoutSec = 40) {
    $deadline = (Get-Date).AddSeconds($timeoutSec)
    $root = [System.Windows.Automation.AutomationElement]::RootElement
    $cond = New-Object System.Windows.Automation.PropertyCondition(
        [System.Windows.Automation.AutomationElement]::ProcessIdProperty, $processId)
    while ((Get-Date) -lt $deadline) {
        $w = $root.FindFirst([System.Windows.Automation.TreeScope]::Children, $cond)
        if ($w -and $w.Current.BoundingRectangle.Width -gt 0) { return $w }
        Start-Sleep -Milliseconds 300
    }
    throw "La ventana principal no apareció en $timeoutSec s."
}

function Find-ByAutomationId($parent, [string]$automationId, [int]$timeoutSec = 20) {
    $deadline = (Get-Date).AddSeconds($timeoutSec)
    $cond = New-Object System.Windows.Automation.PropertyCondition(
        [System.Windows.Automation.AutomationElement]::AutomationIdProperty, $automationId)
    while ((Get-Date) -lt $deadline) {
        $el = $parent.FindFirst([System.Windows.Automation.TreeScope]::Descendants, $cond)
        if ($el) { return $el }
        Start-Sleep -Milliseconds 300
    }
    throw "No se encontró el control '$automationId'."
}

function Invoke-Element($element) {
    $element.GetCurrentPattern([System.Windows.Automation.InvokePattern]::Pattern).Invoke()
}

function Expand-Element($element) {
    $element.GetCurrentPattern([System.Windows.Automation.ExpandCollapsePattern]::Pattern).Expand()
}

function Save-WindowPng($hwnd, [string]$path) {
    [void][Win32Capture]::SetForegroundWindow($hwnd)
    Start-Sleep -Milliseconds 900   # deja que el DWM termine de repintar la ventana ya en primer plano

    $rect = New-Object Win32Capture+RECT
    $size = [System.Runtime.InteropServices.Marshal]::SizeOf($rect)
    # EXTENDED_FRAME_BOUNDS (no GetWindowRect): excluye el margen invisible de redimensionado del DWM,
    # que si no aparece como un borde transparente/negro alrededor de la captura.
    [void][Win32Capture]::DwmGetWindowAttribute($hwnd, [Win32Capture]::DWMWA_EXTENDED_FRAME_BOUNDS, [ref]$rect, $size)

    $w = $rect.Right - $rect.Left
    $h = $rect.Bottom - $rect.Top
    if ($w -le 0 -or $h -le 0) { throw "Rectángulo de ventana inválido ($w x $h)." }

    $bmp = New-Object System.Drawing.Bitmap $w, $h
    try {
        $g = [System.Drawing.Graphics]::FromImage($bmp)
        try { $g.CopyFromScreen($rect.Left, $rect.Top, 0, 0, (New-Object System.Drawing.Size $w, $h)) }
        finally { $g.Dispose() }
        $bmp.Save($path, [System.Drawing.Imaging.ImageFormat]::Png)
    } finally { $bmp.Dispose() }

    Write-Host "  Guardada: $path ($w x $h)" -ForegroundColor Green
}

function Capture-Theme([string]$exePath, [string]$themeName, [string]$driveLetter) {
    Write-Host "Capturando tema $themeName..." -ForegroundColor Cyan
    Set-CaptureSettings $exePath $themeName $driveLetter

    $proc = Start-Process -FilePath $exePath -PassThru
    try {
        $window = Find-MainWindow $proc.Id
        $hwnd = [IntPtr]$window.Current.NativeWindowHandle

        [void][Win32Capture]::ShowWindow($hwnd, 9)   # SW_RESTORE
        # La ventana es de tamaño FIJO por diseño (no redimensionable): solo se coloca, no se estira.
        $r = $window.Current.BoundingRectangle
        [void][Win32Capture]::MoveWindow($hwnd, 80, 30, [int]$r.Width, [int]$r.Height, $true)
        Start-Sleep -Seconds 2   # deja que la tarjeta de unidad se rellene (S.M.A.R.T. va por PowerShell)

        Save-WindowPng $hwnd (Join-Path $OutDir "main-$themeName.png")

        # Diálogo de salud S.M.A.R.T.: la característica que mejor distingue al proyecto. Es un
        # ContentDialog DENTRO de la ventana principal, así que se captura el mismo hwnd.
        # En try/catch aparte: si la navegación del menú falla, la captura principal (ya guardada en
        # disco) no debe perderse por ello.
        if ($themeName -eq 'dark' -and -not $SkipHealth) {
            try {
                Write-Host "  Abriendo Herramientas -> Salud del disco (S.M.A.R.T.)..." -ForegroundColor DarkGray
                Expand-Element (Find-ByAutomationId $window 'MnuTools')
                Start-Sleep -Milliseconds 600
                Invoke-Element (Find-ByAutomationId $window 'MnuHealth')

                # La consulta va contra Get-PhysicalDisk / Get-StorageReliabilityCounter (PowerShell):
                # tarda. Se espera al texto de nota del diálogo, que solo existe cuando ya está poblado.
                [void](Find-ByAutomationId $window 'NoteText' 60)
                Start-Sleep -Seconds 2

                Save-WindowPng $hwnd (Join-Path $OutDir 'health-dark.png')
            } catch {
                Write-Warning "  No se pudo capturar el diálogo S.M.A.R.T.: $($_.Exception.Message)"
            }
        }
    } finally {
        if (-not $proc.HasExited) {
            [void]$proc.CloseMainWindow()
            Start-Sleep -Seconds 2
            if (-not $proc.HasExited) { $proc.Kill() }
        }
    }
}

# ── Ejecución ───────────────────────────────────────────────────────────────────
Assert-Elevated
$exePath = Resolve-Exe
$driveLetter = Resolve-CaptureDrive
if (-not (Test-Path $OutDir)) { New-Item -ItemType Directory -Path $OutDir -Force | Out-Null }

Write-Host "Ejecutable: $exePath" -ForegroundColor Gray
Write-Host "Unidad:     $(if ($driveLetter) { "$driveLetter :" } else { '(por defecto)' })" -ForegroundColor Gray
Write-Host "Salida:     $OutDir" -ForegroundColor Gray

# Respaldo del settings.json REAL del usuario: es el mismo archivo que usa su instalación.
$settingsPath = Get-SettingsPath
$backup = $null
if (Test-Path $settingsPath) {
    $backup = Join-Path $env:TEMP ("FormatDiskPro.settings.capture.{0}.bak" -f (Get-Date -Format 'yyyyMMddHHmmss'))
    Copy-Item $settingsPath $backup -Force
    Write-Host "Respaldo de settings.json: $backup" -ForegroundColor Gray
}

try {
    $themes = if ($Theme -eq 'both') { @('light', 'dark') } else { @($Theme) }
    foreach ($t in $themes) { Capture-Theme $exePath $t $driveLetter }
    Write-Host "`nCapturas completadas en $OutDir" -ForegroundColor Green
} finally {
    if ($backup) {
        Copy-Item $backup $settingsPath -Force
        Remove-Item $backup -Force
        Write-Host "settings.json restaurado." -ForegroundColor Gray
    } elseif (Test-Path $settingsPath) {
        # No había settings previo: la app se estrenó con esta captura; no dejamos rastro.
        Remove-Item $settingsPath -Force
        Write-Host "settings.json de captura eliminado (no había uno previo)." -ForegroundColor Gray
    }
}
