@echo off
setlocal
REM Build all 22 OGEngine targets with no prompts.
REM Use the individual RUN_*.bat scripts to launch after a successful build.

set "ROOT=%~dp0"
set "ROOT=%ROOT:~0,-1%"
cd /d "%ROOT%"

call "%ROOT%\run_oasis_header.bat" BUILD

echo [1/22] Building and deploying OGEngineClient...
call "%ROOT%\BUILD_AND_DEPLOY_STAR_CLIENT.bat"
if errorlevel 1 (
    echo [BUILD EVERYTHING] OGEngineClient failed.
    exit /b 1
)
echo.

echo [2/22] Building ODOOM (batch, no prompts)...
call "%ROOT%\OGames\ODOOM\BUILD ODOOM.bat" batch nosprites
if errorlevel 1 (
    echo [BUILD EVERYTHING] ODOOM build failed.
    exit /b 1
)
echo.

echo [3/22] Building OQuake (batch, no prompts)...
call "%ROOT%\OGames\OQuake\BUILD_OQUAKE.bat" batch
if errorlevel 1 (
    echo [BUILD EVERYTHING] OQuake build failed.
    exit /b 1
)
echo.

echo [4/22] Building OQuake2 - Yamagi Q2 (batch, no prompts)...
call "%ROOT%\OGames\OQuake2\BUILD_OQUAKE2.bat" batch
if errorlevel 1 (
    echo [BUILD EVERYTHING] OQuake2 build failed.
    exit /b 1
)
echo.

echo [5/22] Building OQuake2-RTX - Q2 RTX (batch, no prompts)...
call "%ROOT%\OGames\OQuake2-RTX\BUILD_OQUAKE2RTX.bat" batch
if errorlevel 1 (
    echo [BUILD EVERYTHING] OQuake2-RTX build failed.
    exit /b 1
)
echo.

echo [6/22] Building OQuake3 - Quake3e (batch, no prompts)...
call "%ROOT%\OGames\OQuake3\BUILD_OQUAKE3.bat" batch
if errorlevel 1 (
    echo [BUILD EVERYTHING] OQuake3 build failed.
    exit /b 1
)
echo.

echo [7/22] Building ODOOM3-BFG (batch, no prompts)...
call "%ROOT%\OGames\ODOOM3-BFG\BUILD_ODOOM3BFG.bat" batch
if errorlevel 1 (
    echo [BUILD EVERYTHING] ODOOM3-BFG build failed.
    exit /b 1
)
echo.

echo [8/22] Building ODOOM3 - dhewm3 (batch, no prompts)...
call "%ROOT%\OGames\ODOOM3\BUILD_ODOOM3.bat" batch
if errorlevel 1 (
    echo [BUILD EVERYTHING] ODOOM3 build failed.
    exit /b 1
)
echo.

echo [9/22] Building ODuke3D - EDuke32 (batch, no prompts)...
call "%ROOT%\OGames\ODuke3D\BUILD_ODUKE3D.bat" batch
if errorlevel 1 (
    echo [BUILD EVERYTHING] ODuke3D build failed.
    exit /b 1
)
echo.

echo [10/22] Building ODuke3D-RT - Duke-RT (batch, no prompts)...
call "%ROOT%\OGames\ODuke3D-RT\BUILD_ODUKE3DRT.bat" batch
if errorlevel 1 (
    echo [BUILD EVERYTHING] ODuke3D-RT build failed.
    exit /b 1
)
echo.

echo [11/22] Building OWolf3D - ECWolf (batch, no prompts)...
call "%ROOT%\OGames\OWolf3D\BUILD_OWOLF3D.bat" batch
if errorlevel 1 (
    echo [BUILD EVERYTHING] OWolf3D build failed.
    exit /b 1
)
echo.

echo [12/22] Building OHeretic - UZDoom (batch, no prompts)...
call "%ROOT%\OGames\OHeretic\BUILD_OHERETIC.bat" batch
if errorlevel 1 (
    echo [BUILD EVERYTHING] OHeretic build failed.
    exit /b 1
)
echo.

echo [13/22] Building OHexen - UZDoom (batch, no prompts)...
call "%ROOT%\OGames\OHexen\BUILD_OHEXEN.bat" batch
if errorlevel 1 (
    echo [BUILD EVERYTHING] OHexen build failed.
    exit /b 1
)
echo.

echo [14/22] Building OShadowWarrior - Raze (batch, no prompts)...
call "%ROOT%\OGames\OShadowWarrior\BUILD_OSHADOWWARRIOR.bat" batch
if errorlevel 1 (
    echo [BUILD EVERYTHING] OShadowWarrior build failed.
    exit /b 1
)
echo.

echo [15/22] Building OShadowWarriorRT - Duke-RT (batch, no prompts)...
call "%ROOT%\OGames\OShadowWarriorRT\BUILD_OSHADOWWARRIORRT.bat" batch
if errorlevel 1 (
    echo [BUILD EVERYTHING] OShadowWarriorRT build failed.
    exit /b 1
)
echo.

echo [16/22] Building OBlood - Raze (batch, no prompts)...
call "%ROOT%\OGames\OBlood\BUILD_OBLOOD.bat" batch
if errorlevel 1 (
    echo [BUILD EVERYTHING] OBlood build failed.
    exit /b 1
)
echo.

echo [17/22] Building OExhumed - Raze (batch, no prompts)...
call "%ROOT%\OGames\OExhumed\BUILD_OEXHUMED.bat" batch
if errorlevel 1 (
    echo [BUILD EVERYTHING] OExhumed build failed.
    exit /b 1
)
echo.

echo [18/22] Building OStrife - UZDoom (batch, no prompts)...
call "%ROOT%\OGames\OStrife\BUILD_OSTRIFE.bat" batch
if errorlevel 1 (
    echo [BUILD EVERYTHING] OStrife build failed.
    exit /b 1
)
echo.

echo [19/22] Building ODoom64 - Doom64 EX+ (batch, no prompts)...
call "%ROOT%\OGames\ODoom64\BUILD_ODOOM64.bat" batch
if errorlevel 1 (
    echo [BUILD EVERYTHING] ODoom64 build failed.
    exit /b 1
)
echo.

echo [20/22] Building OHexenII - uhexen2 (batch, no prompts)...
call "%ROOT%\OGames\OHexenII\BUILD_OHEXEN2.bat" batch
if errorlevel 1 (
    echo [BUILD EVERYTHING] OHexenII build failed.
    exit /b 1
)
echo.

echo [21/22] Building ORtCW - iortcw (batch, no prompts)...
call "%ROOT%\OGames\ORtCW\BUILD_ORTCW.bat" batch
if errorlevel 1 (
    echo [BUILD EVERYTHING] ORtCW build failed.
    exit /b 1
)
echo.

echo [22/22] Building OMorrowind - OpenMW (batch, no prompts)...
call "%ROOT%\OGames\OMorrowind\BUILD_OMORROWIND.bat" batch
if errorlevel 1 (
    echo [BUILD EVERYTHING] OMorrowind build failed.
    exit /b 1
)

if exist "%ROOT%\show_oasis_header.ps1" powershell -NoProfile -ExecutionPolicy Bypass -File "%ROOT%\show_oasis_header.ps1" -Success -Message "B U I L D   E V E R Y T H I N G   c o m p l e t e d   s u c c e s s f u l l y" -Message2 "Run RUN_ODOOM.bat, RUN_OQUAKE.bat, RUN_OQUAKE2.bat, RUN_OQUAKE2RTX.bat, RUN_OQUAKE3.bat, RUN_ODOOM3BFG.bat, RUN_ODOOM3.bat, RUN_ODUKE3D.bat, RUN_ODUKE3DRT.bat, RUN_OWOLF3D.bat or other RUN_*.bat to launch."
echo.
echo ========================================
echo   Press any key to exit
echo ========================================
if not "%OASIS_BAT_NO_PAUSE%"=="1" pause >nul

exit /b 0
