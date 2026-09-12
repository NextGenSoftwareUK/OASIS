@echo off
setlocal

REM BUILD_ODOOM3BFG.bat — Build ODOOM3-BFG (RBDOOM-3-BFG with OASIS STAR integration)
REM
REM Usage:
REM   BUILD_ODOOM3BFG.bat          — interactive build
REM   BUILD_ODOOM3BFG.bat batch    — non-interactive (used by BUILD EVERYTHING.bat)
REM
REM Prerequisites:
REM   - Visual Studio 2019 (Community or higher) with C++ workload
REM   - CMake 3.15+ in PATH
REM   - C:\Source\ODOOM3-BFG\ exists (git clone of RBDOOM-3-BFG)
REM   - OGEngineClient built (ogengine.dll / ogengine.lib in OGEngineClient\)

set BATCH=%1
set BUILD_TYPE=Release
set SCRIPT_DIR=%~dp0

echo.
echo =======================================================
echo  ODOOM3-BFG - OASIS STAR Integration Build
echo =======================================================
echo.

REM Run PowerShell copy+build script
powershell.exe -ExecutionPolicy Bypass -File "%SCRIPT_DIR%Scripts\COPY_TO_RBDOOM3_AND_BUILD.ps1" ^
    -BuildType %BUILD_TYPE% %BATCH_FLAG%

if %ERRORLEVEL% neq 0 (
    echo.
    echo [ERROR] ODOOM3-BFG build failed. Check output above.
    if "%BATCH%"=="" pause
    exit /b 1
)

echo.
echo [ODOOM3-BFG] Build successful.
if "%BATCH%"=="" pause
exit /b 0
