@echo off
setlocal
REM OHexen - UZDoom + OASIS STAR API
REM Usage: BUILD_OHEXEN.bat [ batch ]

set "HERE=%~dp0"
set "UZDOOM_SRC=C:\Source\ODOOM"
set "OGENGINECLIENT=%HERE%..\..\OGEngineClient"

if exist "%HERE%..\..\run_oasis_header.bat" call "%HERE%..\..\run_oasis_header.bat" OHEXEN

if exist "%HERE%..\..\BUILD_AND_DEPLOY_STAR_CLIENT.bat" (
    call "%HERE%..\..\BUILD_AND_DEPLOY_STAR_CLIENT.bat"
    if errorlevel 1 (echo [OHexen] OGEngineClient build failed. & pause & exit /b 1)
)

if not exist "%UZDOOM_SRC%\src" (
    echo [OHexen] UZDoom source not found at %UZDOOM_SRC%
    if not "%~1"=="batch" pause
    exit /b 1
)

echo [OHexen] Copying integration files into UZDoom source...
copy /Y "%HERE%ohexen_ogengine_integration.h"   "%UZDOOM_SRC%\src\" >nul
copy /Y "%HERE%ohexen_ogengine_integration.cpp" "%UZDOOM_SRC%\src\" >nul
copy /Y "%OGENGINECLIENT%\ogengine.h"           "%UZDOOM_SRC%\src\" >nul

echo [OHexen] Building UZDoom...
if exist "%UZDOOM_SRC%\CMakeLists.txt" (
    if not exist "%UZDOOM_SRC%\build-vs" mkdir "%UZDOOM_SRC%\build-vs"
    cmake -S "%UZDOOM_SRC%" -B "%UZDOOM_SRC%\build-vs" -A x64 -DCMAKE_BUILD_TYPE=Release
    cmake --build "%UZDOOM_SRC%\build-vs" --config Release
)

echo.
echo [OHexen] Done. Use with hexen.wad via UZDoom -iwad hexen.wad
if not "%~1"=="batch" pause
exit /b 0
