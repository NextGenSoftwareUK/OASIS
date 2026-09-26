@echo off
setlocal
REM OHeretic - UZDoom + OASIS STAR API
REM Usage: BUILD_OHERETIC.bat [ batch ]

set "HERE=%~dp0"
set "UZDOOM_SRC=C:\Source\ODOOM"
set "OGENGINECLIENT=%HERE%..\..\OGEngineClient"

if exist "%HERE%..\..\run_oasis_header.bat" call "%HERE%..\..\run_oasis_header.bat" OHERETIC

if exist "%HERE%..\..\BUILD_AND_DEPLOY_STAR_CLIENT.bat" (
    call "%HERE%..\..\BUILD_AND_DEPLOY_STAR_CLIENT.bat"
    if errorlevel 1 (echo [OHeretic] OGEngineClient build failed. & pause & exit /b 1)
)

if not exist "%UZDOOM_SRC%\src" (
    echo [OHeretic] UZDoom source not found at %UZDOOM_SRC%
    echo UZDoom powers OHeretic, OHexen, OStrife, and ODOOM. Clone to C:\Source\ODOOM
    if not "%~1"=="batch" pause
    exit /b 1
)

echo [OHeretic] Copying integration files into UZDoom source...
copy /Y "%HERE%oheretic_ogengine_integration.h"   "%UZDOOM_SRC%\src\" >nul
copy /Y "%HERE%oheretic_ogengine_integration.cpp" "%UZDOOM_SRC%\src\" >nul
copy /Y "%OGENGINECLIENT%\ogengine.h"             "%UZDOOM_SRC%\src\" >nul
if exist "%OGENGINECLIENT%\ogengine_sync.h" copy /Y "%OGENGINECLIENT%\ogengine_sync.h" "%UZDOOM_SRC%\src\" >nul

echo [OHeretic] Building UZDoom (covers ODOOM, OHeretic, OHexen, OStrife)...
if exist "%UZDOOM_SRC%\CMakeLists.txt" (
    if not exist "%UZDOOM_SRC%\build-vs" mkdir "%UZDOOM_SRC%\build-vs"
    cmake -S "%UZDOOM_SRC%" -B "%UZDOOM_SRC%\build-vs" -A x64 -DCMAKE_BUILD_TYPE=Release
    cmake --build "%UZDOOM_SRC%\build-vs" --config Release
)

echo.
echo [OHeretic] Done. Use with heretic.wad via UZDoom -iwad heretic.wad
if not "%~1"=="batch" pause
exit /b 0
