@echo off
setlocal
REM OBlood - Raze + OASIS STAR API
REM Usage: BUILD_OBLOOD.bat [ batch ]

set "HERE=%~dp0"
set "RAZE_SRC=C:\Source\Raze"
set "OGENGINECLIENT=%HERE%..\..\OGEngineClient"

if exist "%HERE%..\..\run_oasis_header.bat" call "%HERE%..\..\run_oasis_header.bat" OBLOOD

if exist "%HERE%..\..\BUILD_AND_DEPLOY_STAR_CLIENT.bat" (
    call "%HERE%..\..\BUILD_AND_DEPLOY_STAR_CLIENT.bat"
    if errorlevel 1 (echo [OBlood] OGEngineClient build failed. & pause & exit /b 1)
)

if not exist "%RAZE_SRC%\source\blood\src" (
    echo [OBlood] Raze source not found at %RAZE_SRC%
    echo Clone Raze from https://github.com/ZDoom/Raze to C:\Source\Raze
    if not "%~1"=="batch" pause
    exit /b 1
)

echo [OBlood] Copying integration files into Raze source...
copy /Y "%HERE%oblood_ogengine_integration.h"   "%RAZE_SRC%\source\blood\src\" >nul
copy /Y "%HERE%oblood_ogengine_integration.cpp" "%RAZE_SRC%\source\blood\src\" >nul
copy /Y "%OGENGINECLIENT%\ogengine.h"           "%RAZE_SRC%\source\blood\src\" >nul
if exist "%OGENGINECLIENT%\ogengine_sync.h" copy /Y "%OGENGINECLIENT%\ogengine_sync.h" "%RAZE_SRC%\source\blood\src\" >nul

echo [OBlood] Building Raze (covers Blood, Exhumed, Shadow Warrior)...
if exist "%RAZE_SRC%\CMakeLists.txt" (
    if not exist "%RAZE_SRC%\build-vs" mkdir "%RAZE_SRC%\build-vs"
    cmake -S "%RAZE_SRC%" -B "%RAZE_SRC%\build-vs" -A x64 -DCMAKE_BUILD_TYPE=Release
    cmake --build "%RAZE_SRC%\build-vs" --config Release
) else (
    echo [OBlood] No CMakeLists.txt in %RAZE_SRC% — build manually.
)

echo.
echo [OBlood] Done. Raze binary runs Blood, Exhumed, and Shadow Warrior.
if not "%~1"=="batch" pause
exit /b 0
