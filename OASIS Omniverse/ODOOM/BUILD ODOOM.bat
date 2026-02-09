@echo off
REM ODOOM - UZDoom + OASIS STAR API. Credit: UZDoom (GPL-3.0). See CREDITS_AND_LICENSE.md.
REM Usage: BUILD ODOOM.bat [ run ]
REM   (none) = prompt clean/incremental, then copy, branding, build
REM   run    = incremental build then launch (no prompt)

set "UZDOOM_SRC=C:\Source\UZDoom"
set "HERE=%~dp0"
set "DOOM_FOLDER=%HERE%..\Doom"
set "NATIVEWRAPPER=%HERE%..\NativeWrapper"
set "ODOOM_INTEGRATION=%HERE%"

REM Generate ODOOM version from ODOOM/odoom_version.txt
if exist "%ODOOM_INTEGRATION%generate_odoom_version.ps1" powershell -NoProfile -ExecutionPolicy Bypass -File "%ODOOM_INTEGRATION%generate_odoom_version.ps1" -Root "%ODOOM_INTEGRATION%"
set "VERSION_DISPLAY=1.0 (Build 1)"
if exist "%ODOOM_INTEGRATION%version_display.txt" for /f "usebackq delims=" %%a in ("%ODOOM_INTEGRATION%version_display.txt") do set "VERSION_DISPLAY=%%a"

powershell -NoProfile -ExecutionPolicy Bypass -Command "$v=$env:VERSION_DISPLAY; if(-not $v){$v='1.0 (Build 1)'}; $w=60; function c($s){$p=[math]::Max(0,[int](($w-$s.Length)/2)); '  '+(' '*$p)+$s}; Write-Host ''; Write-Host ('  '+('='*$w)) -ForegroundColor DarkCyan; Write-Host (c('O A S I S   O D O O M  v'+$v)) -ForegroundColor Cyan; Write-Host (c('By NextGen World Ltd')) -ForegroundColor DarkGray; Write-Host ('  '+('='*$w)) -ForegroundColor DarkCyan; Write-Host (c('Enabling full interoperable games across the OASIS Omniverse!')) -ForegroundColor DarkMagenta; Write-Host ''"

set "DO_FULL_CLEAN=0"
if /i not "%~1"=="run" (
    echo.
    set /p "BUILD_CHOICE=  Full clean/rebuild (C) or incremental build (I)? [I]: "
)
if not defined BUILD_CHOICE set "BUILD_CHOICE=I"
if /i "%BUILD_CHOICE%"=="C" set "DO_FULL_CLEAN=1"

REM --- Prerequisites ---
if not exist "%UZDOOM_SRC%\src\d_main.cpp" (
    echo UZDoom source not found: %UZDOOM_SRC%
    echo Edit UZDOOM_SRC at top of script.
    pause
    exit /b 1
)
if not exist "%DOOM_FOLDER%\star_api.dll" (
    echo star_api not found: %DOOM_FOLDER%
    echo Build NativeWrapper or copy star_api.dll/lib from Doom folder.
    pause
    exit /b 1
)
if not exist "%DOOM_FOLDER%\star_api.lib" (
    echo star_api.lib not found: %DOOM_FOLDER%
    pause
    exit /b 1
)
if not exist "%NATIVEWRAPPER%\star_api.h" (
    echo star_api.h not found: %NATIVEWRAPPER%
    pause
    exit /b 1
)

set "PYTHON3_EXE="
for /f "tokens=*" %%i in ('python -c "import sys; print(sys.executable)" 2^>nul') do set "PYTHON3_EXE=%%i"
if not defined PYTHON3_EXE for /f "tokens=*" %%i in ('py -3 -c "import sys; print(sys.executable)" 2^>nul') do set "PYTHON3_EXE=%%i"
if not defined PYTHON3_EXE if exist "%LocalAppData%\Programs\Python\Python312\python.exe" set "PYTHON3_EXE=%LocalAppData%\Programs\Python\Python312\python.exe"
if not defined PYTHON3_EXE if exist "%LocalAppData%\Programs\Python\Python311\python.exe" set "PYTHON3_EXE=%LocalAppData%\Programs\Python\Python311\python.exe"
if not defined PYTHON3_EXE if exist "%ProgramFiles%\Python312\python.exe" set "PYTHON3_EXE=%ProgramFiles%\Python312\python.exe"
if not defined PYTHON3_EXE if exist "%ProgramFiles%\Python311\python.exe" set "PYTHON3_EXE=%ProgramFiles%\Python311\python.exe"
if not defined PYTHON3_EXE (
    echo Python 3 required. Install from https://www.python.org/downloads/ and add to PATH.
    pause
    exit /b 1
)
set "CMAKE_PYTHON_ARG=-DPython3_EXECUTABLE=%PYTHON3_EXE%"

echo.
echo [ODOOM] Installing...
copy /Y "%ODOOM_INTEGRATION%uzdoom_star_integration.cpp" "%UZDOOM_SRC%\src\uzdoom_star_integration.cpp" >nul
copy /Y "%ODOOM_INTEGRATION%uzdoom_star_integration.h" "%UZDOOM_SRC%\src\uzdoom_star_integration.h" >nul
copy /Y "%ODOOM_INTEGRATION%odoom_branding.h" "%UZDOOM_SRC%\src\odoom_branding.h" >nul
if exist "%ODOOM_INTEGRATION%odoom_version_generated.h" copy /Y "%ODOOM_INTEGRATION%odoom_version_generated.h" "%UZDOOM_SRC%\src\odoom_version_generated.h" >nul
powershell -ExecutionPolicy Bypass -File "%ODOOM_INTEGRATION%apply_odoom_branding.ps1" -UZDOOM_SRC "%UZDOOM_SRC%"
if exist "%ODOOM_INTEGRATION%oasis_banner.png" (
    copy /Y "%ODOOM_INTEGRATION%oasis_banner.png" "%UZDOOM_SRC%\wadsrc\static\ui\banner-dark.png" >nul
    copy /Y "%ODOOM_INTEGRATION%oasis_banner.png" "%UZDOOM_SRC%\wadsrc\static\ui\banner-light.png" >nul
    copy /Y "%ODOOM_INTEGRATION%oasis_banner.png" "%UZDOOM_SRC%\wadsrc\static\graphics\bootlogo.png" >nul
)

echo.
if "%DO_FULL_CLEAN%"=="1" (
    echo [ODOOM] Full clean...
    if exist "%UZDOOM_SRC%\build" rmdir /s /q "%UZDOOM_SRC%\build" & echo   build removed
)
echo [ODOOM] Configuring (CMake + STAR API)...
cd /d "%UZDOOM_SRC%"
if not exist build mkdir build
cd build
set "STAR_API_DIR=%NATIVEWRAPPER:\=/%"
set "STAR_API_LIB_DIR=%DOOM_FOLDER:\=/%"
cmake .. -G "Visual Studio 17 2022" -A x64 -DOASIS_STAR_API=ON -DSTAR_API_DIR="%STAR_API_DIR%" -DSTAR_API_LIB_DIR="%STAR_API_LIB_DIR%" %CMAKE_PYTHON_ARG%
if errorlevel 1 (echo CMake failed. & pause & exit /b 1)

echo.
echo [ODOOM] Building Release...
cmake --build . --config Release
if errorlevel 1 (echo Build failed. & pause & exit /b 1)

echo.
echo [ODOOM] Packaging output...
copy /Y "%DOOM_FOLDER%\star_api.dll" "%UZDOOM_SRC%\build\Release\star_api.dll" >nul
if not exist "%ODOOM_INTEGRATION%build" mkdir "%ODOOM_INTEGRATION%build"
xcopy "%UZDOOM_SRC%\build\Release\*" "%ODOOM_INTEGRATION%build" /Y /I /Q /E >nul
copy /Y "%UZDOOM_SRC%\build\Release\uzdoom.exe" "%ODOOM_INTEGRATION%build\ODOOM.exe" >nul
copy /Y "%DOOM_FOLDER%\star_api.dll" "%ODOOM_INTEGRATION%build\star_api.dll" >nul
if exist "%ODOOM_INTEGRATION%build\uzdoom.exe" del "%ODOOM_INTEGRATION%build\uzdoom.exe"
if exist "%ODOOM_INTEGRATION%soft_oal.dll" copy /Y "%ODOOM_INTEGRATION%soft_oal.dll" "%ODOOM_INTEGRATION%build\soft_oal.dll" >nul

echo.
echo ---
echo ODOOM ready: %ODOOM_INTEGRATION%build\ODOOM.exe
echo Put doom2.wad (and optional soft_oal.dll) in build folder.
echo Use "BUILD & RUN ODOOM.bat" to launch.
echo ---

if /i "%~1"=="run" (
    if exist "%ODOOM_INTEGRATION%build\ODOOM.exe" (
        echo Launching ODOOM...
        start "" "%ODOOM_INTEGRATION%build\ODOOM.exe"
    ) else (
        start "" "%UZDOOM_SRC%\build\Release\uzdoom.exe"
    )
)
pause
