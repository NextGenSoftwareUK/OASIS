@echo off
setlocal
REM OHexenII - uhexen2 (Hammer of Thyrion) + OASIS STAR API
REM Usage: BUILD_OHEXEN2.bat [ batch ]

set "HERE=%~dp0"
set "UHEXEN2_SRC=C:\Source\OHexenII"
set "OGENGINECLIENT=%HERE%..\..\OGEngineClient"

if exist "%HERE%..\..\run_oasis_header.bat" call "%HERE%..\..\run_oasis_header.bat" OHEXEN2

if exist "%HERE%..\..\BUILD_AND_DEPLOY_STAR_CLIENT.bat" (
    call "%HERE%..\..\BUILD_AND_DEPLOY_STAR_CLIENT.bat"
    if errorlevel 1 (echo [OHexenII] OGEngineClient build failed. & pause & exit /b 1)
)

if not exist "%UHEXEN2_SRC%\engine" (
    echo [OHexenII] uhexen2 source not found at %UHEXEN2_SRC%
    echo Clone uhexen2 from https://sourceforge.net/p/uhexen2 to C:\Source\OHexenII
    if not "%~1"=="batch" pause
    exit /b 1
)

echo [OHexenII] Copying integration files into uhexen2 source...
copy /Y "%HERE%ohexen2_ogengine_integration.h" "%UHEXEN2_SRC%\engine\h2\" >nul
copy /Y "%HERE%ohexen2_ogengine_integration.c" "%UHEXEN2_SRC%\engine\h2\" >nul
copy /Y "%OGENGINECLIENT%\ogengine.h"          "%UHEXEN2_SRC%\engine\h2\" >nul

echo [OHexenII] Building uhexen2...
if exist "%UHEXEN2_SRC%\engine\h2\Makefile" (
    cmake -S "%UHEXEN2_SRC%" -B "%UHEXEN2_SRC%\build-vs" -A x64 -DCMAKE_BUILD_TYPE=Release 2>nul || (
        echo [OHexenII] No CMake support — build with: cd %UHEXEN2_SRC%\engine\h2 and nmake or make
    )
) else (
    echo [OHexenII] Manual build: cd %UHEXEN2_SRC%\engine\h2 and run make
)

echo.
echo [OHexenII] Done. 4 player classes: Paladin, Crusader, Necromancer, Assassin.
if not "%~1"=="batch" pause
exit /b 0
