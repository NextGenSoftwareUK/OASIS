@echo off
setlocal

REM RUN_ODUKE3DRT.bat — Build (if needed) and launch ODuke3D-RT
REM
REM Usage: RUN_ODUKE3DRT.bat [gamedata_dir]
REM   gamedata_dir  — directory containing duke3d.grp (default: C:\Duke3D\)
REM
REM Environment variables:
REM   DUKERT_SRC         — path to ODuke3D-RT (Duke-RT fork) source (default C:\Source\ODuke3D-RT)
REM   STAR_USERNAME      — OASIS username
REM   STAR_PASSWORD      — OASIS password
REM   STAR_API_KEY       — API key (alternative to username/password)
REM   STAR_AVATAR_ID     — OASIS avatar ID

set DUKERT_SRC=%DUKERT_SRC%
if "%DUKERT_SRC%"=="" set DUKERT_SRC=C:\Source\ODuke3D-RT

set GAMEDATA=%~1
if "%GAMEDATA%"=="" set GAMEDATA=C:\Duke3D

set SCRIPT_DIR=%~dp0
set BUILD_OUT=%DUKERT_SRC%\build-vs2019-win64\Release\eduke32.exe

echo.
echo =======================================================
echo  ODuke3D-RT - Launch (Vulkan Ray Tracing)
echo =======================================================

if not exist "%BUILD_OUT%" (
    echo [ODuke3D-RT] Executable not found — building first...
    call "%SCRIPT_DIR%BUILD_ODUKE3DRT.bat"
    if %ERRORLEVEL% neq 0 (
        echo [ERROR] Build failed.
        pause
        exit /b 1
    )
)

echo.
echo [ODuke3D-RT] Launching: %BUILD_OUT%
echo [ODuke3D-RT] Game data: %GAMEDATA%
echo.

"%BUILD_OUT%" -j "%GAMEDATA%"

echo.
pause
