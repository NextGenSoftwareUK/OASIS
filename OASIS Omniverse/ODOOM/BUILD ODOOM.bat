@echo off
REM ODOOM - OASIS STAR API build. ODOOM is a fork of UZDoom. Full credit: https://github.com/UZDoom/UZDoom (GPL-3.0). See CREDITS_AND_LICENSE.md.
REM Single script: copy integration, configure ODOOM (UZDoom) with STAR, build, copy DLL and exe.
REM Uses pre-built star_api from OASIS Omniverse\Doom\ (no wrapper build).
REM   BUILD ODOOM.bat     = build only (output: ODOOM\build\ODOOM.exe)
REM   BUILD ^& RUN ODOOM.bat = build if needed and launch ODOOM
REM Edit UZDOOM_SRC below if your UZDoom source is not at C:\Source\UZDoom

set "UZDOOM_SRC=C:\Source\UZDoom"
set "HERE=%~dp0"
set "DOOM_FOLDER=%HERE%..\Doom"
set "NATIVEWRAPPER=%HERE%..\NativeWrapper"
set "ODOOM_INTEGRATION=%HERE%"

if not exist "%UZDOOM_SRC%\src\d_main.cpp" (
    echo ODOOM source ^(UZDoom^) not found at: %UZDOOM_SRC%
    echo Edit UZDOOM_SRC at the top of this script.
    pause
    exit /b 1
)
if not exist "%DOOM_FOLDER%\star_api.dll" (
    echo Pre-built star_api.dll not found at: %DOOM_FOLDER%
    pause
    exit /b 1
)
if not exist "%DOOM_FOLDER%\star_api.lib" (
    echo Pre-built star_api.lib not found at: %DOOM_FOLDER%
    pause
    exit /b 1
)
if not exist "%NATIVEWRAPPER%\star_api.h" (
    echo star_api.h not found at: %NATIVEWRAPPER%
    pause
    exit /b 1
)

REM ODOOM (UZDoom) build needs Python 3.6+. Find it if not in PATH.
set "PYTHON3_EXE="
for /f "tokens=*" %%i in ('python -c "import sys; print(sys.executable)" 2^>nul') do set "PYTHON3_EXE=%%i"
if not defined PYTHON3_EXE for /f "tokens=*" %%i in ('py -3 -c "import sys; print(sys.executable)" 2^>nul') do set "PYTHON3_EXE=%%i"
if not defined PYTHON3_EXE if exist "%LocalAppData%\Programs\Python\Python312\python.exe" set "PYTHON3_EXE=%LocalAppData%\Programs\Python\Python312\python.exe"
if not defined PYTHON3_EXE if exist "%LocalAppData%\Programs\Python\Python311\python.exe" set "PYTHON3_EXE=%LocalAppData%\Programs\Python\Python311\python.exe"
if not defined PYTHON3_EXE if exist "%ProgramFiles%\Python312\python.exe" set "PYTHON3_EXE=%ProgramFiles%\Python312\python.exe"
if not defined PYTHON3_EXE if exist "%ProgramFiles%\Python311\python.exe" set "PYTHON3_EXE=%ProgramFiles%\Python311\python.exe"
if not defined PYTHON3_EXE (
    echo.
    echo ODOOM ^(UZDoom^) requires Python 3.6 or newer. Install from https://www.python.org/downloads/
    echo Add Python to PATH or install to default location, then run this script again.
    pause
    exit /b 1
)
set "CMAKE_PYTHON_ARG=-DPython3_EXECUTABLE=%PYTHON3_EXE%"

echo === 1. Copy integration and ODOOM branding to ODOOM source ===
copy /Y "%ODOOM_INTEGRATION%uzdoom_star_integration.cpp" "%UZDOOM_SRC%\src\uzdoom_star_integration.cpp"
copy /Y "%ODOOM_INTEGRATION%uzdoom_star_integration.h" "%UZDOOM_SRC%\src\uzdoom_star_integration.h"
copy /Y "%ODOOM_INTEGRATION%odoom_branding.h" "%UZDOOM_SRC%\src\odoom_branding.h"
powershell -ExecutionPolicy Bypass -File "%ODOOM_INTEGRATION%apply_odoom_branding.ps1" -UZDOOM_SRC "%UZDOOM_SRC%"
if exist "%ODOOM_INTEGRATION%oasis_banner.png" (
    echo Copying OASIS banner to launcher and in-game loading screen...
    copy /Y "%ODOOM_INTEGRATION%oasis_banner.png" "%UZDOOM_SRC%\wadsrc\static\ui\banner-dark.png"
    copy /Y "%ODOOM_INTEGRATION%oasis_banner.png" "%UZDOOM_SRC%\wadsrc\static\ui\banner-light.png"
    copy /Y "%ODOOM_INTEGRATION%oasis_banner.png" "%UZDOOM_SRC%\wadsrc\static\graphics\bootlogo.png"
)

echo.
echo === 2. Configure ODOOM with OASIS STAR API ===
cd /d "%UZDOOM_SRC%"
if not exist build mkdir build
cd build
set "STAR_API_DIR=%NATIVEWRAPPER:\=/%"
set "STAR_API_LIB_DIR=%DOOM_FOLDER:\=/%"
cmake .. -G "Visual Studio 17 2022" -A x64 -DOASIS_STAR_API=ON -DSTAR_API_DIR="%STAR_API_DIR%" -DSTAR_API_LIB_DIR="%STAR_API_LIB_DIR%" %CMAKE_PYTHON_ARG%
if errorlevel 1 (
    echo ODOOM CMake configure failed.
    pause
    exit /b 1
)

echo.
echo === 3. Build ODOOM (Release) ===
cmake --build . --config Release
if errorlevel 1 (
    echo ODOOM build failed.
    pause
    exit /b 1
)

echo.
echo === 4. Copy full Release folder to ODOOM build, then rename exe ===
copy /Y "%DOOM_FOLDER%\star_api.dll" "%UZDOOM_SRC%\build\Release\"
if not exist "%ODOOM_INTEGRATION%build" mkdir "%ODOOM_INTEGRATION%build"
xcopy "%UZDOOM_SRC%\build\Release\*" "%ODOOM_INTEGRATION%build\" /Y /I /Q /E
copy /Y "%UZDOOM_SRC%\build\Release\uzdoom.exe" "%ODOOM_INTEGRATION%build\ODOOM.exe"
copy /Y "%DOOM_FOLDER%\star_api.dll" "%ODOOM_INTEGRATION%build\star_api.dll"
if exist "%ODOOM_INTEGRATION%build\uzdoom.exe" del "%ODOOM_INTEGRATION%build\uzdoom.exe"
if exist "%ODOOM_INTEGRATION%soft_oal.dll" copy /Y "%ODOOM_INTEGRATION%soft_oal.dll" "%ODOOM_INTEGRATION%build\soft_oal.dll"
echo   Copied full Release to %ODOOM_INTEGRATION%build\ ^(ODOOM.exe + all files^)

echo.
echo === Done ===
echo   Executable: %ODOOM_INTEGRATION%build\ODOOM.exe
echo   Put your WAD ^(e.g. doom2.wad^) in: %ODOOM_INTEGRATION%build\
echo   For sound/music: put soft_oal.dll ^(64-bit OpenAL Soft^) in the build folder if not already there.
echo     Download: https://openal-soft.org/openal-binaries/

if /i "%~1"=="run" (
    echo Launching ODOOM...
    if exist "%ODOOM_INTEGRATION%build\ODOOM.exe" (start "" "%ODOOM_INTEGRATION%build\ODOOM.exe") else (start "" "%UZDOOM_SRC%\build\Release\uzdoom.exe")
) else (
    echo To build and run: BUILD ^& RUN ODOOM.bat
)
pause
