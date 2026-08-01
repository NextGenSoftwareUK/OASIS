@echo off
setlocal

REM BUILD_ODUKE3D.bat — Build ODuke3D (EDuke32 with OASIS STAR integration)
REM
REM Usage:
REM   BUILD_ODUKE3D.bat          — interactive build
REM   BUILD_ODUKE3D.bat batch    — non-interactive (used by BUILD EVERYTHING.bat)
REM
REM Prerequisites:
REM   - MinGW-w64 (with gcc and make) in PATH  — OR  —
REM     Visual Studio 2019+ with C++ workload (for MSVC nmake build)
REM   - C:\Source\ODuke3D\ exists (git clone of ODuke3D, EDuke32 fork)
REM   - STARAPIClient built (star_api.dll / star_api.lib in STARAPIClient\)
REM   - EDuke32 source variable: EDUKE32_SRC (default C:\Source\ODuke3D)

set BATCH=%1
set BUILD_TYPE=Release
set SCRIPT_DIR=%~dp0

echo.
echo =======================================================
echo  ODuke3D - OASIS STAR Integration Build (EDuke32)
echo =======================================================
echo.

REM Run PowerShell copy+build script
powershell.exe -ExecutionPolicy Bypass -File "%SCRIPT_DIR%Scripts\COPY_TO_EDUKE32_AND_BUILD.ps1" ^
    -BuildType %BUILD_TYPE%

if %ERRORLEVEL% neq 0 (
    echo.
    echo [ERROR] ODuke3D build failed. Check output above.
    if "%BATCH%"=="" pause
    exit /b 1
)

echo.
echo [ODuke3D] Build successful.
if "%BATCH%"=="" pause
exit /b 0
