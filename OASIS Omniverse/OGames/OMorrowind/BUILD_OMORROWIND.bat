@echo off
setlocal

REM BUILD_OMORROWIND.bat — Build OMorrowind (OpenMW with OASIS STAR integration)
REM
REM Usage:
REM   BUILD_OMORROWIND.bat          — interactive build
REM   BUILD_OMORROWIND.bat batch    — non-interactive (used by BUILD EVERYTHING.bat)
REM
REM Prerequisites:
REM   - Visual Studio 2019+ with C++ workload
REM   - CMake 3.15+ in PATH
REM   - Qt 5.15+ (set QTDIR or have Qt in PATH)
REM   - OpenMW source at C:\Source\OMorrowind (git clone of OpenMW fork)
REM   - OGEngineClient built (ogengine.dll / ogengine.lib)

set BATCH=%1
set BUILD_TYPE=Release
set SCRIPT_DIR=%~dp0
set OPENMW_SRC=C:\Source\OMorrowind
set BUILD_DIR=%OPENMW_SRC%\build

echo.
echo =======================================================
echo  OMorrowind - OASIS STAR Integration Build (OpenMW)
echo =======================================================
echo.

REM Copy integration files into OpenMW source tree
echo [OMorrowind] Copying OASIS integration files...
copy /Y "%SCRIPT_DIR%omorrowind_ogengine_integration.h"   "%OPENMW_SRC%\apps\openmw\"
copy /Y "%SCRIPT_DIR%omorrowind_ogengine_integration.cpp" "%OPENMW_SRC%\apps\openmw\"
copy /Y "%SCRIPT_DIR%oasisstar.json"                      "%OPENMW_SRC%\"

if not exist "%OPENMW_SRC%\apps\openmw\ogengine.h" (
    copy /Y "%SCRIPT_DIR%\..\..\OGLib\ogengine.h"     "%OPENMW_SRC%\apps\openmw\"
    copy /Y "%SCRIPT_DIR%\..\..\OGLib\ogengine_sync.h" "%OPENMW_SRC%\apps\openmw\"
)

REM CMake configure
if not exist "%BUILD_DIR%" mkdir "%BUILD_DIR%"
echo [OMorrowind] Configuring with CMake...
cmake -S "%OPENMW_SRC%" -B "%BUILD_DIR%" -DCMAKE_BUILD_TYPE=%BUILD_TYPE% -G "Visual Studio 17 2022"

if %ERRORLEVEL% neq 0 (
    echo [ERROR] CMake configure failed.
    if "%BATCH%"=="" pause
    exit /b 1
)

REM Build
echo [OMorrowind] Building...
cmake --build "%BUILD_DIR%" --config %BUILD_TYPE% --parallel

if %ERRORLEVEL% neq 0 (
    echo [ERROR] OMorrowind build failed. Check output above.
    if "%BATCH%"=="" pause
    exit /b 1
)

echo.
echo [OMorrowind] Build successful. Output: %BUILD_DIR%\%BUILD_TYPE%\
if "%BATCH%"=="" pause
exit /b 0
