# Build and deploy the KrnlAI sidecar for Tauri
$ErrorActionPreference = "Stop"

$RepoRoot = Resolve-Path "$PSScriptRoot\..\..\.."
$SidecarProject = "$RepoRoot\src\KrnlAI.Sidecar"
$TauriBinDir = "$PSScriptRoot\..\src-tauri\binaries"

Write-Host "=== Building KrnlAI.Sidecar ===" -ForegroundColor Cyan

# Build
dotnet publish "$SidecarProject\KrnlAI.Sidecar.csproj" `
  -r win-x64 --self-contained -c Release `
  -o "$TauriBinDir"

Write-Host "=== Sidecar deployed to $TauriBinDir ===" -ForegroundColor Green
