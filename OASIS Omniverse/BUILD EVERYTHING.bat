@echo off
setlocal
REM Build STARAPIClient, ODOOM, OQuake, ODOOM3-BFG, ODOOM3, ODuke3D, ODuke3D-RT, and OWolf3D with no prompts.
REM Use the individual RUN_*.bat scripts to launch after a successful build.

set "ROOT=%~dp0"
set "ROOT=%ROOT:~0,-1%"
cd /d "%ROOT%"

call "%ROOT%\run_oasis_header.bat" BUILD

echo [1/8] Building and deploying STARAPIClient...
call "%ROOT%\BUILD_AND_DEPLOY_STAR_CLIENT.bat"
if errorlevel 1 (
    echo [BUILD EVERYTHING] STARAPIClient failed.
    exit /b 1
)
echo.

echo [2/8] Building ODOOM (batch, no prompts)...
call "%ROOT%\ODOOM\BUILD ODOOM.bat" batch nosprites
if errorlevel 1 (
    echo [BUILD EVERYTHING] ODOOM build failed.
    exit /b 1
)
echo.

echo [3/8] Building OQuake (batch, no prompts)...
call "%ROOT%\OQuake\BUILD_OQUAKE.bat" batch
if errorlevel 1 (
    echo [BUILD EVERYTHING] OQuake build failed.
    exit /b 1
)
echo.

echo [4/8] Building ODOOM3-BFG (batch, no prompts)...
call "%ROOT%\ODOOM3-BFG\BUILD_ODOOM3BFG.bat" batch
if errorlevel 1 (
    echo [BUILD EVERYTHING] ODOOM3-BFG build failed.
    exit /b 1
)
echo.

echo [5/8] Building ODOOM3 - dhewm3 (batch, no prompts)...
call "%ROOT%\ODOOM3\BUILD_ODOOM3.bat" batch
if errorlevel 1 (
    echo [BUILD EVERYTHING] ODOOM3 build failed.
    exit /b 1
)
echo.

echo [6/8] Building ODuke3D - EDuke32 (batch, no prompts)...
call "%ROOT%\ODuke3D\BUILD_ODUKE3D.bat" batch
if errorlevel 1 (
    echo [BUILD EVERYTHING] ODuke3D build failed.
    exit /b 1
)
echo.

echo [7/8] Building ODuke3D-RT - Duke-RT (batch, no prompts)...
call "%ROOT%\ODuke3D-RT\BUILD_ODUKE3DRT.bat" batch
if errorlevel 1 (
    echo [BUILD EVERYTHING] ODuke3D-RT build failed.
    exit /b 1
)
echo.

echo [8/8] Building OWolf3D - ECWolf (batch, no prompts)...
call "%ROOT%\OWolf3D\BUILD_OWOLF3D.bat" batch
if errorlevel 1 (
    echo [BUILD EVERYTHING] OWolf3D build failed.
    exit /b 1
)

if exist "%ROOT%\show_oasis_header.ps1" powershell -NoProfile -ExecutionPolicy Bypass -File "%ROOT%\show_oasis_header.ps1" -Success -Message "B U I L D   E V E R Y T H I N G   c o m p l e t e d   s u c c e s s f u l l y" -Message2 "Run RUN ODOOM.bat, RUN OQUAKE.bat, RUN_ODOOM3BFG.bat, RUN_ODOOM3.bat, RUN_ODUKE3D.bat, RUN_ODUKE3DRT.bat, or RUN_OWOLF3D.bat to launch."
echo.
echo ========================================
echo   Press any key to exit
echo ========================================
if not "%OASIS_BAT_NO_PAUSE%"=="1" pause >nul

exit /b 0
