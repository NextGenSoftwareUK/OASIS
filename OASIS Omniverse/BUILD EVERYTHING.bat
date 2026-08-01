@echo off
setlocal
REM Build STARAPIClient, ODOOM, OQuake, ODOOM3-BFG, ODOOM3, ODuke3D, and ODuke3D-RT with no prompts.
REM Use the individual RUN_*.bat scripts to launch after a successful build.

set "ROOT=%~dp0"
set "ROOT=%ROOT:~0,-1%"
cd /d "%ROOT%"

call "%ROOT%\run_oasis_header.bat" BUILD

echo [1/7] Building and deploying STARAPIClient...
call "%ROOT%\BUILD_AND_DEPLOY_STAR_CLIENT.bat"
if errorlevel 1 (
    echo [BUILD EVERYTHING] STARAPIClient failed.
    exit /b 1
)
echo.

echo [2/7] Building ODOOM (batch, no prompts)...
call "%ROOT%\ODOOM\BUILD ODOOM.bat" batch nosprites
if errorlevel 1 (
    echo [BUILD EVERYTHING] ODOOM build failed.
    exit /b 1
)
echo.

echo [3/7] Building OQuake (batch, no prompts)...
call "%ROOT%\OQuake\BUILD_OQUAKE.bat" batch
if errorlevel 1 (
    echo [BUILD EVERYTHING] OQuake build failed.
    exit /b 1
)
echo.

echo [4/7] Building ODOOM3-BFG (batch, no prompts)...
call "%ROOT%\ODOOM3-BFG\BUILD_ODOOM3BFG.bat" batch
if errorlevel 1 (
    echo [BUILD EVERYTHING] ODOOM3-BFG build failed.
    exit /b 1
)
echo.

echo [5/7] Building ODOOM3 - dhewm3 (batch, no prompts)...
call "%ROOT%\ODOOM3\BUILD_ODOOM3.bat" batch
if errorlevel 1 (
    echo [BUILD EVERYTHING] ODOOM3 build failed.
    exit /b 1
)
echo.

echo [6/7] Building ODuke3D - EDuke32 (batch, no prompts)...
call "%ROOT%\ODuke3D\BUILD_ODUKE3D.bat" batch
if errorlevel 1 (
    echo [BUILD EVERYTHING] ODuke3D build failed.
    exit /b 1
)
echo.

echo [7/7] Building ODuke3D-RT - Duke-RT (batch, no prompts)...
call "%ROOT%\ODuke3D-RT\BUILD_ODUKE3DRT.bat" batch
if errorlevel 1 (
    echo [BUILD EVERYTHING] ODuke3D-RT build failed.
    exit /b 1
)

if exist "%ROOT%\show_oasis_header.ps1" powershell -NoProfile -ExecutionPolicy Bypass -File "%ROOT%\show_oasis_header.ps1" -Success -Message "B U I L D   E V E R Y T H I N G   c o m p l e t e d   s u c c e s s f u l l y" -Message2 "Run RUN ODOOM.bat, RUN OQUAKE.bat, RUN_ODOOM3BFG.bat, RUN_ODOOM3.bat, RUN_ODUKE3D.bat, or RUN_ODUKE3DRT.bat to launch."
echo.
echo ========================================
echo   Press any key to exit
echo ========================================
if not "%OASIS_BAT_NO_PAUSE%"=="1" pause >nul

exit /b 0
