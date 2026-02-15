@echo off
REM Build script for STAR API Native Wrapper on Windows
REM This script builds the library using Visual Studio or MinGW

echo Building OASIS STAR API Native Wrapper...
echo.

REM Check for Visual Studio
where cl >nul 2>&1
if %ERRORLEVEL% == 0 (
    echo Found Visual Studio compiler
    goto :build_vs
)

REM Check for MinGW
where gcc >nul 2>&1
if %ERRORLEVEL% == 0 (
    echo Found MinGW compiler
    goto :build_mingw
)

echo ERROR: No C++ compiler found!
echo Please install Visual Studio or MinGW
echo.
echo For Visual Studio:
echo   1. Install Visual Studio 2019 or later with C++ tools
echo   2. Open "Developer Command Prompt for VS"
echo   3. Run this script again
echo.
echo For MinGW:
echo   1. Install MinGW-w64
echo   2. Add to PATH
echo   3. Run this script again
pause
exit /b 1

:build_vs
echo.
echo Building with Visual Studio...
echo.

REM Create build directory
if not exist build mkdir build
cd build

REM Compile
cl /EHsc /LD /O2 /I.. /D_WIN32 /D_WINHTTP /link winhttp.lib /OUT:star_api.dll ..\star_api.cpp

if %ERRORLEVEL% == 0 (
    echo.
    echo SUCCESS: star_api.dll built!
    echo Location: build\star_api.dll
) else (
    echo.
    echo ERROR: Build failed!
    echo Make sure you're running from "Developer Command Prompt for VS"
)

cd ..
pause
exit /b %ERRORLEVEL%

:build_mingw
echo.
echo Building with MinGW...
echo.

REM Create build directory
if not exist build mkdir build
cd build

REM Compile
g++ -shared -fPIC -O2 -I.. -o star_api.dll ..\star_api.cpp -lwinhttp

if %ERRORLEVEL% == 0 (
    echo.
    echo SUCCESS: star_api.dll built!
    echo Location: build\star_api.dll
) else (
    echo.
    echo ERROR: Build failed!
    echo Make sure MinGW is properly installed
)

cd ..
pause
exit /b %ERRORLEVEL%



