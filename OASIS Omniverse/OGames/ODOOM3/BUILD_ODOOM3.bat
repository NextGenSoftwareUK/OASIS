@echo off
setlocal

REM BUILD_ODOOM3.bat — Build ODOOM3 (dhewm3 with OASIS STAR integration)
REM
REM Usage:
REM   BUILD_ODOOM3.bat          — interactive build
REM   BUILD_ODOOM3.bat batch    — non-interactive (used by BUILD EVERYTHING.bat)
REM
REM Prerequisites:
REM   - Visual Studio 2019 (Community or higher) with C++ workload
REM   - CMake 3.15+ in PATH
REM   - C:\Source\ODOOM3\ exists (git clone of dhewm3)
REM   - OGEngineClient built (ogengine.dll / ogengine.lib in OGEngineClient\)

set BATCH=%1
set BUILD_TYPE=Release
set SCRIPT_DIR=%~dp0

echo.
echo =======================================================
echo  ODOOM3 - OASIS STAR Integration Build (dhewm3)
echo =======================================================
echo.

REM Run PowerShell copy+build script
powershell.exe -ExecutionPolicy Bypass -File "%SCRIPT_DIR%Scripts\COPY_TO_DHEWM3_AND_BUILD.ps1" ^
    -BuildType %BUILD_TYPE%

if %ERRORLEVEL% neq 0 (
    echo.
    echo [ERROR] ODOOM3 build failed. Check output above.
    if "%BATCH%"=="" pause
    exit /b 1
)

echo.
echo [ODOOM3] Build successful.
if "%BATCH%"=="" pause
exit /b 0
