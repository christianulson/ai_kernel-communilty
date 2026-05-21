param(
    [switch]$NoDocker,
    [switch]$NoWebapp,
    [switch]$NoApi
)

$ErrorActionPreference = "Stop"
$root = Split-Path $PSScriptRoot -Parent

Write-Host "=== Krnl-AI Dev Environment ===" -ForegroundColor Cyan

if (-not $NoDocker) {
    Write-Host "Starting Docker infrastructure..." -ForegroundColor Yellow
    docker compose -f "$root\docker-compose.yml" up -d mysql redis qdrant
}

if (-not $NoApi) {
    Write-Host "Starting KrnlAI.Api..." -ForegroundColor Yellow
    $kernelJob = Start-Job -ScriptBlock {
        param($dir) Set-Location $dir; dotnet run --project src/KrnlAI.Api
    } -ArgumentList $root
}

if (-not $NoApi) {
    Write-Host "Starting LLMGateway.Api..." -ForegroundColor Yellow
    $llmJob = Start-Job -ScriptBlock {
        param($dir) Set-Location $dir; dotnet run --project src/LLMGateway.Api
    } -ArgumentList $root
}

if (-not $NoWebapp) {
    Write-Host "Starting webapp..." -ForegroundColor Yellow
    $webappJob = Start-Job -ScriptBlock {
        Set-Location "$args\webapp"; npm exec nx serve @krnl-ai/kernel-ui
    } -ArgumentList $root
}

Write-Host ""
Write-Host "All services started. Press Ctrl+C to stop." -ForegroundColor Green
Write-Host "  KrnlAI.Api:     http://localhost:5000" -ForegroundColor Cyan
Write-Host "  LLMGateway.Api: http://localhost:5001" -ForegroundColor Cyan
Write-Host "  Webapp:         http://localhost:4200" -ForegroundColor Cyan

try {
    Wait-Event
}
finally {
    if ($kernelJob) { Stop-Job $kernelJob; Remove-Job $kernelJob }
    if ($llmJob) { Stop-Job $llmJob; Remove-Job $llmJob }
    if ($webappJob) { Stop-Job $webappJob; Remove-Job $webappJob }
}
