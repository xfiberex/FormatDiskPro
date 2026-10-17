# SPDX-License-Identifier: GPL-3.0-only

<#
.SYNOPSIS
    Comprueba que cada SKILL.md de .agents/skills es el que fija skills-lock.json (AUD-023).

.DESCRIPTION
    Las skills son instrucciones que un agente de codigo obedece al trabajar en el repo, y vienen de un
    tercero (github/awesome-copilot). skills-lock.json guarda el hash de cada una desde que se instalo,
    pero nada lo comprobaba: un PR podia cambiar lo que el agente del mantenedor cree sin que el hash
    dijera nada. Lo ejecuta el workflow "Configuracion de agentes" en cada PR que toque esas rutas.

    El hash es el que calcula la herramienta que las instala (sourceType autoskills-registry), comprobado
    contra las nueve el 2026-10-16:

        computedHash = sha256( "SKILL.md:" + sha256hex(bytes de SKILL.md) )

    Ese es el caso de una carpeta con un solo archivo. Con mas archivos no se sabe como los combina, asi que
    cualquier otro archivo bajo .agents/skills es un error: no hay hash que lo cubra. La unica excepcion es
    .agents/skills/LICENSE, la licencia del origen (AUD-068).

    Se leen los BYTES del archivo: un checkout que convierta los finales de linea (core.autocrlf) cambiaria
    el hash. El workflow corre en Linux por eso.

.EXAMPLE
    .\tools\check-skills.ps1
#>
[CmdletBinding()]
param(
    [string]$Root
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest
# Aqui y no como valor por defecto del parametro: en PowerShell 5.1, $PSScriptRoot llega vacio ahi.
if (-not $Root) { $Root = Split-Path -Parent $PSScriptRoot }

function Get-Sha256Hex([byte[]]$bytes) {
    $sha = [System.Security.Cryptography.SHA256]::Create()
    try { return ([System.BitConverter]::ToString($sha.ComputeHash($bytes)) -replace '-', '').ToLowerInvariant() }
    finally { $sha.Dispose() }
}

$skillsDir = Join-Path $Root '.agents/skills'
$lockPath  = Join-Path $Root 'skills-lock.json'
$problems  = New-Object System.Collections.Generic.List[string]

if (-not (Test-Path -LiteralPath $lockPath))  { throw "No existe $lockPath." }
if (-not (Test-Path -LiteralPath $skillsDir)) { throw "No existe $skillsDir." }

$lock   = Get-Content -LiteralPath $lockPath -Raw | ConvertFrom-Json
$locked = @($lock.skills.PSObject.Properties | ForEach-Object { $_.Name })

# 1. Cada skill bloqueada existe y su SKILL.md da el hash bloqueado.
foreach ($name in $locked) {
    $file = Join-Path $skillsDir "$name/SKILL.md"
    if (-not (Test-Path -LiteralPath $file)) { $problems.Add("$name : skills-lock.json la fija, pero no existe .agents/skills/$name/SKILL.md"); continue }
    $expected = $lock.skills.$name.computedHash
    $fileHash = Get-Sha256Hex ([System.IO.File]::ReadAllBytes($file))
    $actual   = Get-Sha256Hex ([System.Text.Encoding]::UTF8.GetBytes("SKILL.md:$fileHash"))
    if ($actual -ne $expected) {
        $problems.Add("$name : SKILL.md no corresponde a su hash bloqueado (esperado $expected, calculado $actual)")
    } else {
        Write-Host "[OK] $name"
    }
}

# 2. Nada mas bajo .agents/skills: ni skills sin bloquear ni archivos que el hash no cubre.
$allowed = @('LICENSE') + @($locked | ForEach-Object { "$_/SKILL.md" })
$baseLen = (Resolve-Path -LiteralPath $skillsDir).Path.TrimEnd('\', '/').Length + 1
Get-ChildItem -LiteralPath $skillsDir -Recurse -File -Force | ForEach-Object {
    $rel = $_.FullName.Substring($baseLen) -replace '\\', '/'
    if ($allowed -notcontains $rel) {
        $problems.Add(".agents/skills/$rel : no lo cubre ningun hash de skills-lock.json")
    }
}

if ($problems.Count -gt 0) {
    $problems | ForEach-Object { Write-Host "[X] $_" -ForegroundColor Red }
    Write-Host "Si el cambio es deliberado (una skill nueva o actualizada), reinstalala con su herramienta para que skills-lock.json se actualice en el mismo commit." -ForegroundColor Yellow
    exit 1
}
Write-Host "Las $($locked.Count) skills corresponden a skills-lock.json." -ForegroundColor Green
