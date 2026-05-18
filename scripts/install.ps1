# Krnl-AI CLI Installer (PowerShell)
# Usage: irm https://krnlai.dev/install.ps1 | iex

$ErrorActionPreference = "Stop"
$ProgressPreference = "SilentlyContinue"

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "  Krnl-AI CLI - Installer" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

# Detect architecture
$arch = if ([Environment]::Is64BitOperatingSystem) { "x64" } else { "x86" }
Write-Host "Detected: Windows ($arch)" -ForegroundColor Yellow

# Check for .NET SDK
$dotnet = Get-Command dotnet -ErrorAction SilentlyContinue
if (-not $dotnet) {
    Write-Host "Installing .NET SDK..." -ForegroundColor Yellow
    $installScript = Join-Path $env:TEMP "dotnet-install.ps1"
    Invoke-WebRequest -Uri "https://dot.net/v1/dotnet-install.ps1" -OutFile $installScript
    & $installScript -Channel 10.0
    $env:PATH = "$env:USERPROFILE\.dotnet;$env:PATH"
    Write-Host "✅ .NET SDK installed" -ForegroundColor Green
}

# Install Krnl-AI CLI
Write-Host "Installing Krnl-AI CLI..." -ForegroundColor Yellow
$installed = $false

try {
    & dotnet tool install -g KrnlAI.Cli 2>&1 | Out-Null
    $installed = $true
} catch {
    try {
        & dotnet tool update -g KrnlAI.Cli 2>&1 | Out-Null
        $installed = $true
    } catch {
        Write-Host "Building from source..." -ForegroundColor Yellow
        $tmpDir = Join-Path $env:TEMP "krnlai-install"
        if (Test-Path $tmpDir) { Remove-Item -Recurse -Force $tmpDir }
        git clone --depth 1 "https://github.com/krnlai/krnlai.git" $tmpDir
        Push-Location (Join-Path $tmpDir "src\KrnlAI.Cli")
        dotnet pack -c Release -o (Join-Path $env:TEMP "krnlai-nupkg")
        dotnet tool install -g KrnlAI.Cli --add-source (Join-Path $env:TEMP "krnlai-nupkg")
        Pop-Location
        Remove-Item -Recurse -Force $tmpDir -ErrorAction SilentlyContinue
        $installed = $true
    }
}

if ($installed) {
    Write-Host ""
    Write-Host "✅ Krnl-AI CLI installed successfully!" -ForegroundColor Green
    Write-Host ""
    Write-Host "Next steps:" -ForegroundColor Yellow
    Write-Host "  krnlai --help          # View all commands"
    Write-Host "  krnlai chat            # Start interactive TUI"
    Write-Host "  krnlai new agent demo  # Create a new agent"
    Write-Host "  krnlai upgrade         # Check for updates"
} else {
    Write-Host "❌ Installation failed" -ForegroundColor Red
    exit 1
}
