param(
    [string]$OutputDir = (Join-Path (Split-Path $PSScriptRoot -Parent) "certs"),
    [string]$DnsName = "localhost"
)

$ErrorActionPreference = "Stop"

if (-not (Test-Path $OutputDir)) {
    New-Item -ItemType Directory -Path $OutputDir -Force | Out-Null
}

$certPath = Join-Path $OutputDir "dev-cert.pfx"
$crtPath = Join-Path $OutputDir "dev-cert.crt"
$keyPath = Join-Path $OutputDir "dev-cert.key"

# Check if dotnet dev-cert already exists
$existing = dotnet dev-certs https --check --trust 2>$null
if ($LASTEXITCODE -eq 0) {
    Write-Host "Existing dev-cert found. Exporting..." -ForegroundColor Yellow
    dotnet dev-certs https --export-path $certPath --password "dev" --format Pfx 2>$null
    if ($?) {
        Write-Host "Exported to: $certPath" -ForegroundColor Green
        exit 0
    }
}

Write-Host "Creating self-signed certificate for $DnsName..." -ForegroundColor Cyan

# Generate using openssl (if available)
$openssl = Get-Command openssl -ErrorAction SilentlyContinue
if ($openssl) {
    openssl req -x509 -nodes -days 365 -newkey rsa:2048 `
        -keyout $keyPath `
        -out $crtPath `
        -subj "/CN=$DnsName" 2>$null
    
    openssl pkcs12 -export -in $crtPath -inkey $keyPath `
        -out $certPath -password pass:dev 2>$null
    
    Write-Host "Certificate created:" -ForegroundColor Green
    Write-Host "  PFX: $certPath" -ForegroundColor Cyan
    Write-Host "  CRT: $crtPath" -ForegroundColor Cyan
    Write-Host "  Key: $keyPath" -ForegroundColor Cyan
} else {
    Write-Host "openssl not found. Using dotnet dev-certs instead..." -ForegroundColor Yellow
    dotnet dev-certs https --clean 2>$null
    dotnet dev-certs https --trust 2>$null
    dotnet dev-certs https --export-path $certPath --password "dev" --format Pfx 2>$null
    if ($?) {
        Write-Host "Certificate created: $certPath" -ForegroundColor Green
    } else {
        Write-Error "Failed to create certificate. Install openssl or dotnet SDK."
    }
}
