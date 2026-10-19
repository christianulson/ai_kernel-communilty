# Builds the frontend for the Tauri desktop app.
# The webapp lives in the ROOT repository (ai_kernel/webapp). When it is present
# (relative path ../../../../webapp), build it and copy the output to the local
# ./dist folder. Otherwise, keep the existing ./dist (stale build) with a warning.
$ErrorActionPreference = 'Stop'
$rootWebapp = Join-Path $PSScriptRoot '../../../../webapp'
$distTarget = Join-Path $PSScriptRoot '../dist'

if (Test-Path (Join-Path $rootWebapp 'apps/kernel-ui/package.json')) {
    Push-Location $rootWebapp
    try {
        npx nx run @krnl-ai/kernel-ui:build
        if ($LASTEXITCODE -ne 0) { throw "nx build failed with exit code $LASTEXITCODE" }
    }
    finally {
        Pop-Location
    }

    $distSource = Join-Path $rootWebapp 'apps/kernel-ui/dist'
    if (Test-Path $distSource) {
        New-Item -ItemType Directory -Path $distTarget -Force | Out-Null
        Copy-Item -Path (Join-Path $distSource '*') -Destination $distTarget -Recurse -Force
    }
}
else {
    Write-Warning "Root webapp not present at $rootWebapp; using existing dist/ (stale build)"
}