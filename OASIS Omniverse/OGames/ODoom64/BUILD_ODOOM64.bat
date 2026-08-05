@echo off
setlocal
REM ODoom64 - Doom64 EX+ + OASIS STAR API
REM Usage: BUILD_ODOOM64.bat [ batch ]

set "HERE=%~dp0"
set "DOOM64_SRC=C:\Source\ODoom64"
set "OGENGINECLIENT=%HERE%..\..\OGEngineClient"

if exist "%HERE%..\..\run_oasis_header.bat" call "%HERE%..\..\run_oasis_header.bat" ODOOM64

if exist "%HERE%..\..\BUILD_AND_DEPLOY_STAR_CLIENT.bat" (
    call "%HERE%..\..\BUILD_AND_DEPLOY_STAR_CLIENT.bat"
    if errorlevel 1 (echo [ODoom64] OGEngineClient build failed. & pause & exit /b 1)
)

if not exist "%DOOM64_SRC%\src" (
    echo [ODoom64] Doom64 EX+ source not found at %DOOM64_SRC%
    echo Clone Doom64 EX+ from https://github.com/azdo/doom64ex-plus to C:\Source\ODoom64
    if not "%~1"=="batch" pause
    exit /b 1
)

echo [ODoom64] Copying integration files into Doom64 EX+ source...
copy /Y "%HERE%odoom64_ogengine_integration.h" "%DOOM64_SRC%\src\doom64\" >nul
copy /Y "%HERE%odoom64_ogengine_integration.c" "%DOOM64_SRC%\src\doom64\" >nul
copy /Y "%OGENGINECLIENT%\ogengine.h"          "%DOOM64_SRC%\src\doom64\" >nul

echo [ODoom64] Building Doom64 EX+...
if exist "%DOOM64_SRC%\CMakeLists.txt" (
    if not exist "%DOOM64_SRC%\build-vs" mkdir "%DOOM64_SRC%\build-vs"
    cmake -S "%DOOM64_SRC%" -B "%DOOM64_SRC%\build-vs" -A x64 -DCMAKE_BUILD_TYPE=Release
    cmake --build "%DOOM64_SRC%\build-vs" --config Release
)

echo.
echo [ODoom64] Done. Requires doom64.wad.
if not "%~1"=="batch" pause
exit /b 0
