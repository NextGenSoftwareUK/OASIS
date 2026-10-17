@echo off
setlocal EnableDelayedExpansion

:: OWolf3D Build Script (Windows)
:: Copies OASIS STAR integration files into the ECWolf source tree and builds.
:: Usage: BUILD_OWOLF3D.bat [batch]

set "BATCH_MODE=0"
if /i "%1"=="batch" set "BATCH_MODE=1"

set "ROOT=%~dp0"
set "SCRIPT=%ROOT%Scripts\COPY_TO_ECWOLF_AND_BUILD.ps1"

echo =============================================================
echo  OWolf3D Build
echo =============================================================

if "%BATCH_MODE%"=="1" (
    powershell -NoProfile -ExecutionPolicy Bypass -File "%SCRIPT%" -BuildType Release -BatchMode
) else (
    powershell -NoProfile -ExecutionPolicy Bypass -File "%SCRIPT%" -BuildType Release
)

if errorlevel 1 (
    echo.
    echo BUILD FAILED
    if "%BATCH_MODE%"=="0" pause
    exit /b 1
)

echo.
echo Build complete. Run OWolf3D with RUN_OWOLF3D.bat
if "%BATCH_MODE%"=="0" pause
endlocal
