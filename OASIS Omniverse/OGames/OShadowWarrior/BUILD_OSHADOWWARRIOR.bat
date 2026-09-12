@echo off
setlocal
REM OShadowWarrior - Raze + OASIS STAR API
REM Usage: BUILD_OSHADOWWARRIOR.bat [ batch ]

set "HERE=%~dp0"
set "RAZE_SRC=C:\Source\Raze"
set "OGENGINECLIENT=%HERE%..\..\OGEngineClient"

if exist "%HERE%..\..\run_oasis_header.bat" call "%HERE%..\..\run_oasis_header.bat" OSHADOWWARRIOR

if exist "%HERE%..\..\BUILD_AND_DEPLOY_STAR_CLIENT.bat" (
    call "%HERE%..\..\BUILD_AND_DEPLOY_STAR_CLIENT.bat"
    if errorlevel 1 (echo [OShadowWarrior] OGEngineClient build failed. & pause & exit /b 1)
)

if not exist "%RAZE_SRC%\source\sw\src" (
    echo [OShadowWarrior] Raze source not found at %RAZE_SRC%
    if not "%~1"=="batch" pause
    exit /b 1
)

echo [OShadowWarrior] Copying integration files into Raze source...
copy /Y "%HERE%osw_ogengine_integration.h"   "%RAZE_SRC%\source\sw\src\" >nul
copy /Y "%HERE%osw_ogengine_integration.cpp" "%RAZE_SRC%\source\sw\src\" >nul
copy /Y "%OGENGINECLIENT%\ogengine.h"        "%RAZE_SRC%\source\sw\src\" >nul

echo [OShadowWarrior] Building Raze...
if exist "%RAZE_SRC%\CMakeLists.txt" (
    if not exist "%RAZE_SRC%\build-vs" mkdir "%RAZE_SRC%\build-vs"
    cmake -S "%RAZE_SRC%" -B "%RAZE_SRC%\build-vs" -A x64 -DCMAKE_BUILD_TYPE=Release
    cmake --build "%RAZE_SRC%\build-vs" --config Release
)

echo.
echo [OShadowWarrior] Done.
if not "%~1"=="batch" pause
exit /b 0
