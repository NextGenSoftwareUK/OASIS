@echo off
REM Quick build script for STAR API Native Wrapper
REM This script helps you build the wrapper library

echo ========================================
echo OASIS STAR API - Native Wrapper Builder
echo ========================================
echo.

cd /d "%~dp0NativeWrapper"

echo Step 1: Checking for build tools...
echo.

REM Check for Visual Studio
where cl >nul 2>&1
if %ERRORLEVEL% == 0 (
    echo [OK] Visual Studio compiler found
    goto :build
)

REM Check for CMake
where cmake >nul 2>&1
if %ERRORLEVEL% == 0 (
    echo [OK] CMake found
    goto :build_cmake
)

REM Check for MinGW
where gcc >nul 2>&1
if %ERRORLEVEL% == 0 (
    echo [OK] MinGW compiler found
    goto :build_mingw
)

echo [ERROR] No build tools found!
echo.
echo Please install one of:
echo   1. Visual Studio 2019+ with C++ tools
echo   2. CMake (https://cmake.org/download/)
echo   3. MinGW-w64
echo.
echo Then run this script again.
pause
exit /b 1

:build_cmake
echo.
echo Step 2: Building with CMake...
echo.

if not exist build mkdir build
cd build

cmake .. -G "Visual Studio 16 2019" -A x64
if %ERRORLEVEL% neq 0 (
    echo [ERROR] CMake configuration failed!
    pause
    exit /b 1
)

cmake --build . --config Release
if %ERRORLEVEL% neq 0 (
    echo [ERROR] Build failed!
    pause
    exit /b 1
)

cd ..
echo.
echo [SUCCESS] Build complete!
echo Library location: NativeWrapper\build\Release\star_api.dll
goto :end

:build
echo.
echo Step 2: Building with Visual Studio compiler...
echo.

if not exist build mkdir build
cd build

cl /EHsc /LD /O2 /I.. /D_WIN32 /D_WINHTTP /link winhttp.lib /OUT:star_api.dll ..\star_api.cpp

if %ERRORLEVEL% neq 0 (
    echo [ERROR] Build failed!
    echo Make sure you're running from "Developer Command Prompt for VS"
    pause
    exit /b 1
)

cd ..
echo.
echo [SUCCESS] Build complete!
echo Library location: NativeWrapper\build\star_api.dll
goto :end

:build_mingw
echo.
echo Step 2: Building with MinGW...
echo.

if not exist build mkdir build
cd build

g++ -shared -fPIC -O2 -I.. -o star_api.dll ..\star_api.cpp -lwinhttp

if %ERRORLEVEL% neq 0 (
    echo [ERROR] Build failed!
    pause
    exit /b 1
)

cd ..
echo.
echo [SUCCESS] Build complete!
echo Library location: NativeWrapper\build\star_api.dll
goto :end

:end
echo.
echo ========================================
echo Next Steps:
echo ========================================
echo 1. Set environment variables:
echo    set STAR_USERNAME=your_username
echo    set STAR_PASSWORD=your_password
echo.
echo 2. Build DOOM:
echo    cd C:\Source\DOOM\linuxdoom-1.10
echo    make
echo.
echo 3. Test integration!
echo.
pause



