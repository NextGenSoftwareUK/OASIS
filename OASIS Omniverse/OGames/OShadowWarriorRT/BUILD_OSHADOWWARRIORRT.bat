@echo off
setlocal
REM OShadowWarriorRT - Duke-RT (Raze fork, Vulkan RT) + OASIS STAR API
REM Usage: BUILD_OSHADOWWARRIORRT.bat [ batch ]

set "HERE=%~dp0"
set "DUKERT_SRC=C:\Source\OShadowWarriorRT"
set "OGENGINECLIENT=%HERE%..\..\OGEngineClient"

if exist "%HERE%..\..\run_oasis_header.bat" call "%HERE%..\..\run_oasis_header.bat" OSHADOWWARRIORRT

if exist "%HERE%..\..\BUILD_AND_DEPLOY_STAR_CLIENT.bat" (
    call "%HERE%..\..\BUILD_AND_DEPLOY_STAR_CLIENT.bat"
    if errorlevel 1 (echo [OShadowWarriorRT] OGEngineClient build failed. & pause & exit /b 1)
)

if not exist "%DUKERT_SRC%" (
    echo [OShadowWarriorRT] Duke-RT source not found at %DUKERT_SRC%
    echo Clone Duke-RT from https://github.com/postmemetic/Duke-RT to C:\Source\OShadowWarriorRT
    if not "%~1"=="batch" pause
    exit /b 1
)

echo [OShadowWarriorRT] Copying integration files...
copy /Y "%HERE%osw_rt_ogengine_integration.h"   "%DUKERT_SRC%\source\sw\src\" >nul
copy /Y "%HERE%osw_rt_ogengine_integration.cpp" "%DUKERT_SRC%\source\sw\src\" >nul
copy /Y "%OGENGINECLIENT%\ogengine.h"           "%DUKERT_SRC%\source\sw\src\" >nul

echo [OShadowWarriorRT] Building Duke-RT (Vulkan RTX)...
if exist "%DUKERT_SRC%\CMakeLists.txt" (
    if not exist "%DUKERT_SRC%\build-vs" mkdir "%DUKERT_SRC%\build-vs"
    cmake -S "%DUKERT_SRC%" -B "%DUKERT_SRC%\build-vs" -A x64 -DCMAKE_BUILD_TYPE=Release -DOASIS_STAR_SYNC_IN_CLIENT=1
    cmake --build "%DUKERT_SRC%\build-vs" --config Release
)

echo.
echo [OShadowWarriorRT] Done.
if not "%~1"=="batch" pause
exit /b 0
