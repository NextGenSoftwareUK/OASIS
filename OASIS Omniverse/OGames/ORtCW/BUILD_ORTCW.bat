@echo off
setlocal
REM ORtCW - iortcw (Q3-engine) + OASIS STAR API
REM Usage: BUILD_ORTCW.bat [ batch ]

set "HERE=%~dp0"
set "IORTCW_SRC=C:\Source\ORtCW"
set "OGENGINECLIENT=%HERE%..\..\OGEngineClient"

if exist "%HERE%..\..\run_oasis_header.bat" call "%HERE%..\..\run_oasis_header.bat" ORTCW

if exist "%HERE%..\..\BUILD_AND_DEPLOY_STAR_CLIENT.bat" (
    call "%HERE%..\..\BUILD_AND_DEPLOY_STAR_CLIENT.bat"
    if errorlevel 1 (echo [ORtCW] OGEngineClient build failed. & pause & exit /b 1)
)

if not exist "%IORTCW_SRC%\SP_src" (
    echo [ORtCW] iortcw source not found at %IORTCW_SRC%
    echo Clone iortcw from https://github.com/iortcw/iortcw to C:\Source\ORtCW
    if not "%~1"=="batch" pause
    exit /b 1
)

echo [ORtCW] Copying integration files into iortcw source...
copy /Y "%HERE%ortcw_ogengine_integration.h"   "%IORTCW_SRC%\SP_src\game\" >nul
copy /Y "%HERE%ortcw_ogengine_integration.c"   "%IORTCW_SRC%\SP_src\game\" >nul
copy /Y "%OGENGINECLIENT%\ogengine.h"          "%IORTCW_SRC%\SP_src\game\" >nul
if exist "%OGENGINECLIENT%\ogengine_sync.h" copy /Y "%OGENGINECLIENT%\ogengine_sync.h" "%IORTCW_SRC%\SP_src\game\" >nul

echo [ORtCW] Building iortcw...
if exist "%IORTCW_SRC%\CMakeLists.txt" (
    if not exist "%IORTCW_SRC%\build-vs" mkdir "%IORTCW_SRC%\build-vs"
    cmake -S "%IORTCW_SRC%" -B "%IORTCW_SRC%\build-vs" -A x64 -DCMAKE_BUILD_TYPE=Release
    cmake --build "%IORTCW_SRC%\build-vs" --config Release
) else if exist "%IORTCW_SRC%\Makefile" (
    cd /d "%IORTCW_SRC%" && nmake
) else (
    echo [ORtCW] Build iortcw manually. See %IORTCW_SRC%\README.md
)

echo.
echo [ORtCW] Done. "Blazkowicz reports for duty."
if not "%~1"=="batch" pause
exit /b 0
