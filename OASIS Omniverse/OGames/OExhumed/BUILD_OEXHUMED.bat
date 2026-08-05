@echo off
setlocal
REM OExhumed - Raze + OASIS STAR API (Exhumed/PowerSlave)
REM Usage: BUILD_OEXHUMED.bat [ batch ]

set "HERE=%~dp0"
set "RAZE_SRC=C:\Source\Raze"
set "OGENGINECLIENT=%HERE%..\..\OGEngineClient"

if exist "%HERE%..\..\run_oasis_header.bat" call "%HERE%..\..\run_oasis_header.bat" OEXHUMED

if exist "%HERE%..\..\BUILD_AND_DEPLOY_STAR_CLIENT.bat" (
    call "%HERE%..\..\BUILD_AND_DEPLOY_STAR_CLIENT.bat"
    if errorlevel 1 (echo [OExhumed] OGEngineClient build failed. & pause & exit /b 1)
)

if not exist "%RAZE_SRC%\source\exhumed\src" (
    echo [OExhumed] Raze source not found at %RAZE_SRC%
    echo Clone Raze from https://github.com/ZDoom/Raze to C:\Source\Raze
    if not "%~1"=="batch" pause
    exit /b 1
)

echo [OExhumed] Copying integration files into Raze source...
copy /Y "%HERE%oexhumed_ogengine_integration.h"   "%RAZE_SRC%\source\exhumed\src\" >nul
copy /Y "%HERE%oexhumed_ogengine_integration.cpp" "%RAZE_SRC%\source\exhumed\src\" >nul
copy /Y "%OGENGINECLIENT%\ogengine.h"             "%RAZE_SRC%\source\exhumed\src\" >nul
if exist "%OGENGINECLIENT%\ogengine_sync.h" copy /Y "%OGENGINECLIENT%\ogengine_sync.h" "%RAZE_SRC%\source\exhumed\src\" >nul

echo [OExhumed] Building Raze (covers Blood, Exhumed, Shadow Warrior)...
if exist "%RAZE_SRC%\CMakeLists.txt" (
    if not exist "%RAZE_SRC%\build-vs" mkdir "%RAZE_SRC%\build-vs"
    cmake -S "%RAZE_SRC%" -B "%RAZE_SRC%\build-vs" -A x64 -DCMAKE_BUILD_TYPE=Release
    cmake --build "%RAZE_SRC%\build-vs" --config Release
)

echo.
echo [OExhumed] Done.
if not "%~1"=="batch" pause
exit /b 0
