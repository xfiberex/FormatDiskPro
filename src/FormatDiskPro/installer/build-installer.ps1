<#
.SYNOPSIS
    Publica FormatDiskPro (self-contained, win-x64) y compila el instalador con Inno Setup.

.DESCRIPTION
    1. Lee la versión del .csproj (o usa -Version).
    2. dotnet publish -c Release -r win-x64 --self-contained true
    3. Compila installer.iss con ISCC; el .exe queda en installer/Output.

.PARAMETER Version
    Versión a estampar (por defecto: la del .csproj).

.EXAMPLE
    .\build-installer.ps1
    .\build-installer.ps1 -Version 1.2.0
#>
[CmdletBinding()]
param(
    [string]$Version,
    [string]$Configuration = "Release",
    [string]$Runtime       = "win-x64"
)

$ErrorActionPreference = "Stop"

$installerDir = $PSScriptRoot
$projectDir   = Split-Path $installerDir -Parent          # src\FormatDiskPro
$csproj       = Join-Path $projectDir "FormatDiskPro.csproj"
$publishDir   = Join-Path $projectDir "bin\$Configuration\net10.0-windows\$Runtime\publish"

if (-not (Test-Path $csproj)) { throw "No se encontró el proyecto: $csproj" }

# --- Versión ---------------------------------------------------------------
if (-not $Version) {
    $csprojXml = [xml](Get-Content $csproj)
    $Version = ($csprojXml.Project.PropertyGroup.Version | Where-Object { $_ }) | Select-Object -First 1
    if (-not $Version) { $Version = "1.0.0" }
}
Write-Host "==> Versión: $Version" -ForegroundColor Cyan

# --- Localizar ISCC --------------------------------------------------------
$iscc = @(
    "C:\Program Files (x86)\Inno Setup 6\ISCC.exe",
    "C:\Program Files\Inno Setup 6\ISCC.exe"
) | Where-Object { Test-Path $_ } | Select-Object -First 1
if (-not $iscc) {
    $cmd = Get-Command iscc.exe -ErrorAction SilentlyContinue
    if ($cmd) { $iscc = $cmd.Source }
}
if (-not $iscc) { throw "No se encontró ISCC.exe. Instala Inno Setup 6: winget install JRSoftware.InnoSetup" }

# --- Publicar (self-contained) ---------------------------------------------
Write-Host "==> Publicando ($Configuration / $Runtime, self-contained)..." -ForegroundColor Cyan
if (Test-Path $publishDir) { Remove-Item $publishDir -Recurse -Force }

& dotnet publish $csproj `
    -c $Configuration `
    -r $Runtime `
    --self-contained true `
    -p:PublishSingleFile=false `
    -p:DebugType=none `
    -p:DebugSymbols=false `
    -o $publishDir
if ($LASTEXITCODE -ne 0) { throw "dotnet publish falló (código $LASTEXITCODE)" }

# --- Compilar instalador ---------------------------------------------------
$iss = Join-Path $installerDir "installer.iss"
Write-Host "==> Compilando instalador con Inno Setup..." -ForegroundColor Cyan
& $iscc "/DMyAppVersion=$Version" "/DPublishDir=$publishDir" $iss
if ($LASTEXITCODE -ne 0) { throw "ISCC falló (código $LASTEXITCODE)" }

$setup = Join-Path $installerDir "Output\FormatDiskPro-$Version-setup.exe"
if (Test-Path $setup) {
    $sizeMB = [math]::Round((Get-Item $setup).Length / 1MB, 1)
    Write-Host "`n[OK] Instalador generado: $setup ($sizeMB MB)" -ForegroundColor Green
} else {
    Write-Warning "ISCC terminó pero no se encontró el instalador esperado en $setup"
}
