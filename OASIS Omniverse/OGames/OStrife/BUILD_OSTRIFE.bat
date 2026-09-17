@echo off
setlocal
REM OStrife - UZDoom + OASIS STAR API
REM Usage: BUILD_OSTRIFE.bat [ batch ]

set "HERE=%~dp0"
set "UZDOOM_SRC=C:\Source\ODOOM"
set "OGENGINECLIENT=%HERE%..\..\OGEngineClient"

if exist "%HERE%..\..\run_oasis_header.bat" call "%HERE%..\..\run_oasis_header.bat" OSTRIFE

if exist "%HERE%..\..\BUILD_AND_DEPLOY_STAR_CLIENT.bat" (
    call "%HERE%..\..\BUILD_AND_DEPLOY_STAR_CLIENT.bat"
    if errorlevel 1 (echo [OStrife] OGEngineClient build failed. & pause & exit /b 1)
)

if not exist "%UZDOOM_SRC%\src" (
    echo [OStrife] UZDoom source not found at %UZDOOM_SRC%
    if not "%~1"=="batch" pause
    exit /b 1
)

echo [OStrife] Copying integration files into UZDoom source...
copy /Y "%HERE%ostrife_ogengine_integration.h"   "%UZDOOM_SRC%\src\" >nul
copy /Y "%HERE%ostrife_ogengine_integration.cpp" "%UZDOOM_SRC%\src\" >nul
copy /Y "%OGENGINECLIENT%\ogengine.h"            "%UZDOOM_SRC%\src\" >nul

echo [OStrife] Building UZDoom...
if exist "%UZDOOM_SRC%\CMakeLists.txt" (
    if not exist "%UZDOOM_SRC%\build-vs" mkdir "%UZDOOM_SRC%\build-vs"
    cmake -S "%UZDOOM_SRC%" -B "%UZDOOM_SRC%\build-vs" -A x64 -DCMAKE_BUILD_TYPE=Release
    cmake --build "%UZDOOM_SRC%\build-vs" --config Release
)

echo.
echo [OStrife] Done. Use with strife1.wad via UZDoom -iwad strife1.wad
if not "%~1"=="batch" pause
exit /b 0
