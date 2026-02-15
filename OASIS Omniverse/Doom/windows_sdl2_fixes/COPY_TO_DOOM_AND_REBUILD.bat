@echo off
setlocal
REM Copy Windows SDL2 fixes into DOOM source and rebuild.
REM   No args: copy sources and rebuild (reuses existing build if present).
REM   "clean" arg: delete build folder and do full reconfigure. Close game first.
REM Set DOOM_SRC below if your path is not C:\Source\DOOM\linuxdoom-1.10

set "DOOM_SRC=C:\Source\DOOM\linuxdoom-1.10"
set "FIXES=%~dp0"

REM Find cmake (often not in PATH when double-clicking the .bat)
set "CMAKE_EXE=cmake"
where cmake >nul 2>nul
if errorlevel 1 (
    if exist "C:\Program Files\CMake\bin\cmake.exe" (set "CMAKE_EXE=C:\Program Files\CMake\bin\cmake.exe") else if exist "C:\Program Files (x86)\CMake\bin\cmake.exe" (set "CMAKE_EXE=C:\Program Files (x86)\CMake\bin\cmake.exe")
)

if not exist "%DOOM_SRC%\d_main.c" (
    echo ERROR: DOOM source not found at %DOOM_SRC%
    echo Edit the batch file and set DOOM_SRC to your linuxdoom-1.10 path.
    pause
    exit /b 1
)

echo DOOM source: %DOOM_SRC%
echo Copying fixes...
copy /Y "%FIXES%i_main.c" "%DOOM_SRC%\i_main.c"
copy /Y "%FIXES%i_video_sdl2.c" "%DOOM_SRC%\i_video_sdl2.c"
copy /Y "%FIXES%i_sound_win.c" "%DOOM_SRC%\i_sound_win.c"
copy /Y "%FIXES%..\odoom_version.h" "%DOOM_SRC%\odoom_version.h"
echo(

cd /d "%DOOM_SRC%"

set "ARG1=%~1"
if /i "%ARG1%"=="clean" goto do_clean
if exist "build\CMakeCache.txt" goto do_rebuild
goto do_configure

:do_clean
echo Clean rebuild: removing build folder...
if not exist build goto do_configure
rmdir /s /q build 2>nul
if exist build (
    echo ERROR: Could not delete build. Close doom_star.exe and try again.
    pause
    exit /b 1
)
goto do_configure

:do_rebuild
echo Build folder found - rebuilding only...
cd build
"%CMAKE_EXE%" --build . --config Release
if errorlevel 1 (
    echo(
    echo Build failed. Try: COPY_TO_DOOM_AND_REBUILD.bat clean
    echo Close the game first.
    pause
    exit /b 1
)
echo(
echo Done. Run the exe from build\Release\
pause
exit /b 0

:do_configure
if not exist build mkdir build
cd build

set "CMAKE_EXTRA="
if defined CMAKE_TOOLCHAIN_FILE set "CMAKE_EXTRA=-DCMAKE_TOOLCHAIN_FILE=%CMAKE_TOOLCHAIN_FILE%"
if defined SDL2_DIR set "CMAKE_EXTRA=-DSDL2_DIR=%SDL2_DIR%"
if "%CMAKE_EXTRA%"=="" if exist "C:\Source\vcpkg\scripts\buildsystems\vcpkg.cmake" set "CMAKE_EXTRA=-DCMAKE_TOOLCHAIN_FILE=C:/Source/vcpkg/scripts/buildsystems/vcpkg.cmake"
if "%CMAKE_EXTRA%"=="" if exist "C:\vcpkg\scripts\buildsystems\vcpkg.cmake" set "CMAKE_EXTRA=-DCMAKE_TOOLCHAIN_FILE=C:/vcpkg/scripts/buildsystems/vcpkg.cmake"
if "%CMAKE_EXTRA%"=="" if exist "%USERPROFILE%\vcpkg\scripts\buildsystems\vcpkg.cmake" set "CMAKE_EXTRA=-DCMAKE_TOOLCHAIN_FILE=%USERPROFILE%/vcpkg/scripts/buildsystems/vcpkg.cmake"
REM SDL2 official zip has cmake/sdl2-config.cmake (lowercase), not SDL2Config.cmake
if "%CMAKE_EXTRA%"=="" if exist "C:\SDL2\cmake\sdl2-config.cmake" set "CMAKE_EXTRA=-DSDL2_DIR=C:/SDL2/cmake"
if "%CMAKE_EXTRA%"=="" if exist "C:\SDL2\cmake\SDL2Config.cmake" set "CMAKE_EXTRA=-DSDL2_DIR=C:/SDL2/cmake"
if "%CMAKE_EXTRA%"=="" if exist "C:\Source\SDL2-2.30.2\cmake\sdl2-config.cmake" set "CMAKE_EXTRA=-DSDL2_DIR=C:/Source/SDL2-2.30.2/cmake"
if "%CMAKE_EXTRA%"=="" if exist "C:\Source\SDL2-2.30.2\cmake\SDL2Config.cmake" set "CMAKE_EXTRA=-DSDL2_DIR=C:/Source/SDL2-2.30.2/cmake"
if "%CMAKE_EXTRA%"=="" if exist "C:\Source\SDL2\cmake\sdl2-config.cmake" set "CMAKE_EXTRA=-DSDL2_DIR=C:/Source/SDL2/cmake"
if "%CMAKE_EXTRA%"=="" if exist "C:\Libraries\SDL2\cmake\sdl2-config.cmake" set "CMAKE_EXTRA=-DSDL2_DIR=C:/Libraries/SDL2/cmake"

REM Project install script puts SDL2 in C:\Source\SDL2-2.30.2 - run it if not found
if "%CMAKE_EXTRA%"=="" if exist "%DOOM_SRC%\install_sdl2.ps1" (
    echo SDL2 not found. Running project install script to download SDL2 to C:\Source...
    powershell -ExecutionPolicy Bypass -NoProfile -File "%DOOM_SRC%\install_sdl2.ps1"
    if exist "C:\Source\SDL2-2.30.2\cmake\sdl2-config.cmake" set "CMAKE_EXTRA=-DSDL2_DIR=C:/Source/SDL2-2.30.2/cmake"
)

if "%CMAKE_EXTRA%"=="" (
    echo ERROR: SDL2 not found. Set SDL2_DIR or CMAKE_TOOLCHAIN_FILE and run again.
    echo Or run manually: powershell -ExecutionPolicy Bypass -File "%DOOM_SRC%\install_sdl2.ps1"
    echo(
    pause
    exit /b 1
)

echo Configuring and building Release...
"%CMAKE_EXE%" .. -G "Visual Studio 17 2022" -A x64 %CMAKE_EXTRA%
if errorlevel 1 (
    echo CMake configure failed. If cmake not found, add CMake to PATH or install from https://cmake.org/download/
    pause
    exit /b 1
)
"%CMAKE_EXE%" --build . --config Release
if errorlevel 1 (
    echo Build failed.
    pause
    exit /b 1
)

echo(
echo Done. Run the exe from build\Release\
pause
