@echo off
REM BUILD_OHALFLIFE.bat — Build OHalfLife (Xash3D FWGS + HLSDK game DLL)
REM
REM Prerequisites:
REM   - Visual Studio 2019+ or MSVC Build Tools
REM   - CMake 3.16+
REM   - Xash3D FWGS cloned at %XASH3D_DIR% (default: C:\Source\xash3d-fwgs)
REM   - HLSDK-portable cloned at %HLSDK_DIR% (default: C:\Source\hlsdk-portable)
REM   - OGEngineClient.dll in %OGEDITOR_SDK% or alongside this script

setlocal EnableDelayedExpansion

set SCRIPT_DIR=%~dp0
set OASIS_DIR=%SCRIPT_DIR%..\..
set BUILD_DIR=%SCRIPT_DIR%build

if not defined XASH3D_DIR set XASH3D_DIR=C:\Source\xash3d-fwgs
if not defined HLSDK_DIR   set HLSDK_DIR=C:\Source\hlsdk-portable
if not defined OGENGINE_LIB set OGENGINE_LIB=%SCRIPT_DIR%..\..\OGEditorSDK\build\OGEngineClient.dll

echo.
echo =======================================================
echo  OHalfLife - OASIS STAR API Integration Build
echo =======================================================
echo.

REM --- Copy integration files into HLSDK ---
echo [OHalfLife] Copying integration files into HLSDK...
copy /Y "%SCRIPT_DIR%ohalflife_ogengine_integration.h"   "%HLSDK_DIR%\dlls\"
copy /Y "%SCRIPT_DIR%ohalflife_ogengine_integration.cpp" "%HLSDK_DIR%\dlls\"
copy /Y "%SCRIPT_DIR%..\..\OGEditorSDK\ogengine.h"       "%HLSDK_DIR%\dlls\"
copy /Y "%SCRIPT_DIR%..\..\OGEditorSDK\ogengine_sync.h"  "%HLSDK_DIR%\dlls\"
copy /Y "%SCRIPT_DIR%oasisstar.json"                      "%HLSDK_DIR%\dlls\"

REM --- Build HLSDK game DLL ---
echo [OHalfLife] Building HLSDK game DLL...
if not exist "%HLSDK_DIR%\build_hl" mkdir "%HLSDK_DIR%\build_hl"
cmake -S "%HLSDK_DIR%" -B "%HLSDK_DIR%\build_hl" ^
    -DCMAKE_BUILD_TYPE=RelWithDebInfo ^
    -DOGENGINE_INTEGRATION=ON
if errorlevel 1 ( echo [ERROR] CMake configure failed & exit /b 1 )
cmake --build "%HLSDK_DIR%\build_hl" --config RelWithDebInfo
if errorlevel 1 ( echo [ERROR] HLSDK build failed & exit /b 1 )

REM --- Build Xash3D FWGS ---
echo [OHalfLife] Building Xash3D FWGS engine...
if not exist "%XASH3D_DIR%\build" mkdir "%XASH3D_DIR%\build"
cmake -S "%XASH3D_DIR%" -B "%XASH3D_DIR%\build" ^
    -DCMAKE_BUILD_TYPE=RelWithDebInfo
if errorlevel 1 ( echo [ERROR] Xash3D CMake configure failed & exit /b 1 )
cmake --build "%XASH3D_DIR%\build" --config RelWithDebInfo
if errorlevel 1 ( echo [ERROR] Xash3D build failed & exit /b 1 )

REM --- Assemble OHalfLife build output ---
echo [OHalfLife] Assembling build output...
if not exist "%BUILD_DIR%" mkdir "%BUILD_DIR%"
if not exist "%BUILD_DIR%\valve\dlls" mkdir "%BUILD_DIR%\valve\dlls"

xcopy /Y /Q "%XASH3D_DIR%\build\RelWithDebInfo\xash.exe"    "%BUILD_DIR%\"
copy  /Y    "%HLSDK_DIR%\build_hl\RelWithDebInfo\hl.dll"     "%BUILD_DIR%\valve\dlls\"
copy  /Y    "%OGENGINE_LIB%"                                  "%BUILD_DIR%\"
copy  /Y    "%SCRIPT_DIR%oasisstar.json"                      "%BUILD_DIR%\"

REM Rename xash.exe to OHalfLife.exe for hub compatibility
if exist "%BUILD_DIR%\xash.exe" ren "%BUILD_DIR%\xash.exe" OHalfLife.exe

echo.
echo [OHalfLife] Build complete.
echo   Engine:   %BUILD_DIR%\OHalfLife.exe
echo   Game DLL: %BUILD_DIR%\valve\dlls\hl.dll
echo.
echo Next steps:
echo   1. Copy your Half-Life 'valve' folder content into %BUILD_DIR%\valve\
echo   2. Run OHalfLife.exe -game valve
echo   3. The OASIS STAR API integration will auto-initialise on map load.
