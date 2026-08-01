@echo off
setlocal

REM RUN_ODUKE3D.bat — Build (if needed) and launch ODuke3D
REM
REM Usage: RUN_ODUKE3D.bat [gamedata_dir]
REM   gamedata_dir  — directory containing duke3d.grp (default: C:\Duke3D\)
REM
REM Environment variables:
REM   EDUKE32_SRC        — path to ODuke3D (EDuke32 fork) source  (default C:\Source\ODuke3D)
REM   STAR_USERNAME      — OASIS username
REM   STAR_PASSWORD      — OASIS password
REM   STAR_API_KEY       — API key (alternative to username/password)
REM   STAR_AVATAR_ID     — OASIS avatar ID

set EDUKE32_SRC=%EDUKE32_SRC%
if "%EDUKE32_SRC%"=="" set EDUKE32_SRC=C:\Source\ODuke3D

set GAMEDATA=%~1
if "%GAMEDATA%"=="" set GAMEDATA=C:\Duke3D

set SCRIPT_DIR=%~dp0
set BUILD_OUT=%EDUKE32_SRC%\eduke32.exe

echo.
echo =======================================================
echo  ODuke3D - Launch
echo =======================================================

REM Build if exe does not exist
if not exist "%BUILD_OUT%" (
    echo [ODuke3D] Executable not found — building first...
    call "%SCRIPT_DIR%BUILD_ODUKE3D.bat"
    if %ERRORLEVEL% neq 0 (
        echo [ERROR] Build failed.
        pause
        exit /b 1
    )
)

echo.
echo [ODuke3D] Launching from: %BUILD_OUT%
echo [ODuke3D] Game data    : %GAMEDATA%
echo.

"%BUILD_OUT%" -j "%GAMEDATA%"

echo.
pause
