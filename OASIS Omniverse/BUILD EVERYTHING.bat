@echo off
setlocal
REM Build all 11 OGEngine targets with no prompts.
REM Use the individual RUN_*.bat scripts to launch after a successful build.

set "ROOT=%~dp0"
set "ROOT=%ROOT:~0,-1%"
cd /d "%ROOT%"

call "%ROOT%\run_oasis_header.bat" BUILD

echo [1/11] Building and deploying OGEngineClient...
call "%ROOT%\BUILD_AND_DEPLOY_STAR_CLIENT.bat"
if errorlevel 1 (
    echo [BUILD EVERYTHING] OGEngineClient failed.
    exit /b 1
)
echo.

echo [2/11] Building ODOOM (batch, no prompts)...
call "%ROOT%\ODOOM\BUILD ODOOM.bat" batch nosprites
if errorlevel 1 (
    echo [BUILD EVERYTHING] ODOOM build failed.
    exit /b 1
)
echo.

echo [3/11] Building OQuake (batch, no prompts)...
call "%ROOT%\OQuake\BUILD_OQUAKE.bat" batch
if errorlevel 1 (
    echo [BUILD EVERYTHING] OQuake build failed.
    exit /b 1
)
echo.

echo [4/11] Building OQuake2 - Yamagi Q2 (batch, no prompts)...
call "%ROOT%\OQuake2\BUILD_OQUAKE2.bat" batch
if errorlevel 1 (
    echo [BUILD EVERYTHING] OQuake2 build failed.
    exit /b 1
)
echo.

echo [5/11] Building OQuake2-RTX - Q2 RTX (batch, no prompts)...
call "%ROOT%\OQuake2-RTX\BUILD_OQUAKE2RTX.bat" batch
if errorlevel 1 (
    echo [BUILD EVERYTHING] OQuake2-RTX build failed.
    exit /b 1
)
echo.

echo [6/11] Building OQuake3 - Quake3e (batch, no prompts)...
call "%ROOT%\OQuake3\BUILD_OQUAKE3.bat" batch
if errorlevel 1 (
    echo [BUILD EVERYTHING] OQuake3 build failed.
    exit /b 1
)
echo.

echo [7/11] Building ODOOM3-BFG (batch, no prompts)...
call "%ROOT%\ODOOM3-BFG\BUILD_ODOOM3BFG.bat" batch
if errorlevel 1 (
    echo [BUILD EVERYTHING] ODOOM3-BFG build failed.
    exit /b 1
)
echo.

echo [8/11] Building ODOOM3 - dhewm3 (batch, no prompts)...
call "%ROOT%\ODOOM3\BUILD_ODOOM3.bat" batch
if errorlevel 1 (
    echo [BUILD EVERYTHING] ODOOM3 build failed.
    exit /b 1
)
echo.

echo [9/11] Building ODuke3D - EDuke32 (batch, no prompts)...
call "%ROOT%\ODuke3D\BUILD_ODUKE3D.bat" batch
if errorlevel 1 (
    echo [BUILD EVERYTHING] ODuke3D build failed.
    exit /b 1
)
echo.

echo [10/11] Building ODuke3D-RT - Duke-RT (batch, no prompts)...
call "%ROOT%\ODuke3D-RT\BUILD_ODUKE3DRT.bat" batch
if errorlevel 1 (
    echo [BUILD EVERYTHING] ODuke3D-RT build failed.
    exit /b 1
)
echo.

echo [11/11] Building OWolf3D - ECWolf (batch, no prompts)...
call "%ROOT%\OWolf3D\BUILD_OWOLF3D.bat" batch
if errorlevel 1 (
    echo [BUILD EVERYTHING] OWolf3D build failed.
    exit /b 1
)

if exist "%ROOT%\show_oasis_header.ps1" powershell -NoProfile -ExecutionPolicy Bypass -File "%ROOT%\show_oasis_header.ps1" -Success -Message "B U I L D   E V E R Y T H I N G   c o m p l e t e d   s u c c e s s f u l l y" -Message2 "Run RUN ODOOM.bat, RUN OQUAKE.bat, RUN_OQUAKE2.bat, RUN_OQUAKE2RTX.bat, RUN_OQUAKE3.bat, RUN_ODOOM3BFG.bat, RUN_ODOOM3.bat, RUN_ODUKE3D.bat, RUN_ODUKE3DRT.bat, or RUN_OWOLF3D.bat to launch."
echo.
echo ========================================
echo   Press any key to exit
echo ========================================
if not "%OASIS_BAT_NO_PAUSE%"=="1" pause >nul

exit /b 0
