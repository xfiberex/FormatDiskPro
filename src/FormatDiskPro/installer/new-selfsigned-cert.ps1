# SPDX-License-Identifier: GPL-3.0-only

<#
.SYNOPSIS
    Crea un certificado de firma de codigo AUTOFIRMADO (self-signed) y devuelve su huella.

.DESCRIPTION
    Pensado para validar el pipeline de firma (build-installer.ps1 -CertThumbprint ...) y para
    despliegues en entornos controlados.

    IMPORTANTE: un certificado autofirmado NO elimina los avisos de SmartScreen ni de
    "editor desconocido" en las maquinas de otros usuarios, porque su cadena no es de confianza.
    La firma solo se valida en equipos donde se haya importado el certificado a:
      - "Entidades de certificacion raiz de confianza" (Root), y
      - "Editores de confianza" (TrustedPublisher).
    Para distribucion publica real se necesita un certificado OV/EV de una CA reconocida.

.PARAMETER Subject
    CN del certificado (por defecto "FormatDiskPro (Dev)").

.PARAMETER ExportPfx
    Ruta opcional para exportar el certificado (con clave privada) a un .pfx. Si no se da -Password,
    se pide sin eco.

.PARAMETER Password
    Contrasena del .pfx exportado, como SecureString. NO es [string] a proposito (AUD-060): como texto
    plano quedaba en el historial de PowerShell y en la linea de comandos del proceso, visible para
    cualquier proceso local. Lo mismo que -CertPassword de build-installer.ps1.

.PARAMETER Years
    Validez en anos (por defecto 3).

.PARAMETER Trust
    Importa el certificado a los almacenes de confianza del equipo ACTUAL (Root + TrustedPublisher)
    para que la firma valide aqui. Requiere ejecutar PowerShell como administrador.

.EXAMPLE
    .\new-selfsigned-cert.ps1
    .\new-selfsigned-cert.ps1 -ExportPfx fdp-dev.pfx                # pide la contrasena sin eco
    .\new-selfsigned-cert.ps1 -ExportPfx fdp-dev.pfx -Password (Read-Host -AsSecureString)
    .\new-selfsigned-cert.ps1 -Trust    # (como admin) firma validable en este equipo
#>
[CmdletBinding()]
param(
    [string]$Subject = "FormatDiskPro (Dev)",
    [string]$ExportPfx,
    [SecureString]$Password,
    [int]$Years = 3,
    [switch]$Trust
)

$ErrorActionPreference = "Stop"

# La contrasena se pide ANTES de crear el certificado: si no se da, no queda un certificado huerfano en
# el almacen sin el .pfx que se pidio.
if ($ExportPfx -and -not $Password) {
    $Password = Read-Host -AsSecureString "Contrasena para $ExportPfx"
}
if ($ExportPfx -and $Password.Length -eq 0) { throw "Para exportar a .pfx hace falta una contrasena no vacia." }

# --- Crear el certificado de firma de codigo en el almacen del usuario ------
$cert = New-SelfSignedCertificate `
    -Type CodeSigningCert `
    -Subject "CN=$Subject" `
    -KeyAlgorithm RSA -KeyLength 3072 `
    -CertStoreLocation "Cert:\CurrentUser\My" `
    -NotAfter (Get-Date).AddYears($Years) `
    -FriendlyName $Subject

Write-Host "[OK] Certificado autofirmado creado." -ForegroundColor Green
Write-Host ("  Subject:    {0}" -f $cert.Subject)
Write-Host ("  Thumbprint: {0}" -f $cert.Thumbprint) -ForegroundColor Cyan
Write-Host ("  Almacen:    Cert:\CurrentUser\My")
Write-Host ("  Valido hasta: {0:yyyy-MM-dd}" -f $cert.NotAfter)

# --- Exportar a .pfx (opcional) --------------------------------------------
if ($ExportPfx) {
    Export-PfxCertificate -Cert $cert -FilePath $ExportPfx -Password $Password | Out-Null
    Write-Host ("[OK] Exportado a: {0}  (NO lo subas al repo: esta en .gitignore)" -f $ExportPfx) -ForegroundColor Green
}

# --- Confiar en el certificado en este equipo (opcional) -------------------
if ($Trust) {
    try {
        $root = Get-Item "Cert:\LocalMachine\Root" ; $root.Open("ReadWrite") ; $root.Add($cert) ; $root.Close()
        $pub  = Get-Item "Cert:\LocalMachine\TrustedPublisher" ; $pub.Open("ReadWrite") ; $pub.Add($cert) ; $pub.Close()
        Write-Host "[OK] Certificado importado a Root y TrustedPublisher (LocalMachine)." -ForegroundColor Green
    } catch {
        Write-Warning "No se pudo importar a los almacenes del equipo (ejecuta PowerShell como administrador): $($_.Exception.Message)"
    }
}

Write-Host "`n--- Como firmar con este certificado ---" -ForegroundColor Yellow
Write-Host ("  .\build-installer.ps1 -Version X.Y.Z -CertThumbprint {0}" -f $cert.Thumbprint)
Write-Host ("  .\release.ps1 -Version X.Y.Z -CertThumbprint {0}" -f $cert.Thumbprint)
Write-Host "`nRecuerda: autofirmado NO quita SmartScreen para usuarios finales (cadena no confiable)." -ForegroundColor DarkYellow
