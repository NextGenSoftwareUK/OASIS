@echo off
setlocal

REM BUILD_ODUKE3DRT.bat — Build ODuke3D-RT (Duke-RT with OASIS STAR integration)
REM
REM Usage:
REM   BUILD_ODUKE3DRT.bat          — interactive build
REM   BUILD_ODUKE3DRT.bat batch    — non-interactive (used by BUILD EVERYTHING.bat)
REM
REM Prerequisites:
REM   - Visual Studio 2019 (Community or higher) with C++ workload
REM   - CMake 3.15+ in PATH
REM   - Vulkan SDK in PATH (Duke-RT uses Vulkan ray tracing)
REM   - C:\Source\ODuke3D-RT\ exists (git clone of Duke-RT)
REM   - STARAPIClient built (star_api.dll / star_api.lib in STARAPIClient\)
REM   - Source variable: DUKERT_SRC (default C:\Source\ODuke3D-RT)

set BATCH=%1
set BUILD_TYPE=Release
set SCRIPT_DIR=%~dp0

echo.
echo =======================================================
echo  ODuke3D-RT - OASIS STAR Integration Build (Duke-RT)
echo =======================================================
echo.

REM Run PowerShell copy+build script
powershell.exe -ExecutionPolicy Bypass -File "%SCRIPT_DIR%Scripts\COPY_TO_DUKERT_AND_BUILD.ps1" ^
    -BuildType %BUILD_TYPE%

if %ERRORLEVEL% neq 0 (
    echo.
    echo [ERROR] ODuke3D-RT build failed. Check output above.
    if "%BATCH%"=="" pause
    exit /b 1
)

echo.
echo [ODuke3D-RT] Build successful.
if "%BATCH%"=="" pause
exit /b 0
