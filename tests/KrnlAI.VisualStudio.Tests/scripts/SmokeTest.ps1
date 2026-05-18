param(
    [string]$VsixPath = "",
    [switch]$SkipBuild
)

$ErrorActionPreference = "Stop"

if (-not $SkipBuild) {
    Write-Host "=== Building KrnlAI.VisualStudio ===" -ForegroundColor Cyan
    $buildResult = dotnet build (Join-Path $PSScriptRoot "..\..\KrnlAI.VisualStudio\KrnlAI.VisualStudio.csproj") -p:CreateVsixContainer=true
    if ($LASTEXITCODE -ne 0) {
        Write-Host "BUILD FAILED" -ForegroundColor Red
        exit 1
    }
    Write-Host "Build OK" -ForegroundColor Green
}

if (-not $VsixPath) {
    $VsixPath = Join-Path $PSScriptRoot "..\..\KrnlAI.VisualStudio\bin\Debug\KrnlAI.VisualStudio.vsix"
}

Write-Host "=== Checking VSIX file ===" -ForegroundColor Cyan
if (-not (Test-Path $VsixPath)) {
    Write-Host "VSIX not found at $VsixPath" -ForegroundColor Red
    Write-Host "Running with CreateVsixContainer..." -ForegroundColor Yellow
    dotnet build (Join-Path $PSScriptRoot "..\..\KrnlAI.VisualStudio\KrnlAI.VisualStudio.csproj") -p:CreateVsixContainer=true -p:OutputPath=(Join-Path $PSScriptRoot "..\..\KrnlAI.VisualStudio\bin\Debug") 2>&1 | Out-Null
}

if (Test-Path $VsixPath) {
    $file = Get-Item $VsixPath
    Write-Host "VSIX found: $($file.FullName)" -ForegroundColor Green
    Write-Host "Size: $([math]::Round($file.Length / 1KB, 1)) KB" -ForegroundColor Green

    # Basic VSIX validation: check it's a valid ZIP
    try {
        Add-Type -AssemblyName System.IO.Compression.FileSystem
        $zip = [System.IO.Compression.ZipFile]::OpenRead($file.FullName)
        $entries = $zip.Entries | ForEach-Object { $_.FullName }
        $zip.Dispose()

        Write-Host "`n=== VSIX Contents ===" -ForegroundColor Cyan
        $entries | ForEach-Object { Write-Host "  $_" }

        $requiredFiles = @(
            "extension.vsixmanifest",
            "[Content_Types].xml",
            "KrnlAI.VisualStudio.dll",
            "KrnlAISdk.dll"
        )

        Write-Host "`n=== Validating required files ===" -ForegroundColor Cyan
        $missing = $requiredFiles | Where-Object { -not ($entries -match [regex]::Escape($_)) }
        if ($missing) {
            Write-Host "MISSING FILES:" -ForegroundColor Red
            $missing | ForEach-Object { Write-Host "  - $_" }
            exit 1
        }
        Write-Host "All required files present" -ForegroundColor Green
    }
    catch {
        Write-Host "VSIX validation failed: $_" -ForegroundColor Red
        exit 1
    }
}
else {
    Write-Host "VSIX not found at $VsixPath" -ForegroundColor Red
    exit 1
}

Write-Host "`n=== Running unit tests ===" -ForegroundColor Cyan
$testResult = dotnet test (Join-Path $PSScriptRoot "..\KrnlAI.VisualStudio.Tests.csproj") --no-restore
if ($LASTEXITCODE -ne 0) {
    Write-Host "TESTS FAILED" -ForegroundColor Red
    exit 1
}

Write-Host "`n=== SMOKE TEST PASSED ===" -ForegroundColor Green
Write-Host "VSIX: $($file.FullName)" -ForegroundColor Green
Write-Host "Size: $([math]::Round($file.Length / 1KB, 1)) KB" -ForegroundColor Green
