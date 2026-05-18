@echo off
REM Krnl-AI CLI Installer (Windows CMD)
REM Usage: curl -fsSL https://krnlai.dev/install.cmd -o install.cmd && install.cmd && del install.cmd

echo ========================================
echo   Krnl-AI CLI - Installer
echo ========================================
echo.

REM Check if dotnet is available
where dotnet >nul 2>nul
if %ERRORLEVEL% NEQ 0 (
    echo [.] .NET SDK not found. Downloading installer...
    curl -fsSL https://dot.net/v1/dotnet-install.ps1 -o "%TEMP%\dotnet-install.ps1"
    if %ERRORLEVEL% NEQ 0 (
        echo [ERROR] Failed to download .NET installer. Check your internet connection.
        pause
        exit /b 1
    )
    echo [.] Installing .NET SDK...
    powershell -NoProfile -ExecutionPolicy Bypass -File "%TEMP%\dotnet-install.ps1" -Channel 10.0
    if %ERRORLEVEL% NEQ 0 (
        echo [ERROR] Failed to install .NET SDK.
        pause
        exit /b 1
    )
    set PATH=%USERPROFILE%\.dotnet;%PATH%
    echo [OK] .NET SDK installed
)

echo [.] Installing Krnl-AI CLI...
dotnet tool install -g KrnlAI.Cli 2>nul
if %ERRORLEVEL% NEQ 0 (
    dotnet tool update -g KrnlAI.Cli 2>nul
    if %ERRORLEVEL% NEQ 0 (
        echo [.] Building from source...
        if not exist "%TEMP%\krnlai-install" mkdir "%TEMP%\krnlai-install"
        cd /d "%TEMP%\krnlai-install"
        git clone --depth 1 https://github.com/krnlai/krnlai.git .
        cd src\KrnlAI.Cli
        dotnet pack -c Release -o "%TEMP%\krnlai-nupkg"
        dotnet tool install -g KrnlAI.Cli --add-source "%TEMP%\krnlai-nupkg"
        cd /d "%TEMP%"
        rmdir /s /q "%TEMP%\krnlai-install" 2>nul
        rmdir /s /q "%TEMP%\krnlai-nupkg" 2>nul
    )
)

echo.
echo [OK] Krnl-AI CLI installed successfully!
echo.
echo Next steps:
echo   krnlai --help          -- View all commands
echo   krnlai chat            -- Start interactive TUI
echo   krnlai new agent demo  -- Create a new agent
echo   krnlai upgrade         -- Check for updates
echo.
pause
