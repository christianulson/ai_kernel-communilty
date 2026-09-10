# Build and deploy the KrnlAI sidecar for Tauri
$ErrorActionPreference = "Stop"

$RepoRoot = Resolve-Path "$PSScriptRoot\..\..\.."
$SidecarProject = "$RepoRoot\src\KrnlAI.Sidecar"
$TauriBinDir = "$PSScriptRoot\..\src-tauri\binaries"

Write-Host "=== Building KrnlAI.Sidecar ===" -ForegroundColor Cyan

# Build
dotnet publish "$SidecarProject\KrnlAI.Sidecar.csproj" `
  -r win-x64 --self-contained -c Release -p:SignAssembly=false `
  -o "$TauriBinDir"

Write-Host "=== Sidecar deployed to $TauriBinDir ===" -ForegroundColor Green

# Rename to the Tauri externalBin target-triple name so the bundler picks it up
$TripleName = "krnlai-sidecar-x86_64-pc-windows-msvc.exe"
$TriplePath = Join-Path $TauriBinDir $TripleName
if (-not (Test-Path $TriplePath)) {
    Copy-Item -Path (Join-Path $TauriBinDir "KrnlAI.Sidecar.exe") -Destination $TriplePath -Force
}
Write-Host "=== externalBin ready: $TripleName ===" -ForegroundColor Green
