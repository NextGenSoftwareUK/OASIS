@echo off
REM Automated Build and Setup Script for OASIS STAR API Integration
REM This script automates the build process

echo ========================================
echo OASIS STAR API - Automated Build Setup
echo ========================================
echo.

set SCRIPT_DIR=%~dp0
set OASIS_ROOT=%SCRIPT_DIR%..
set DOOM_PATH=%OASIS_ROOT%\..\DOOM\linuxdoom-1.10
set QUAKE_PATH=%OASIS_ROOT%\..\quake-rerelease-qc
set WRAPPER_DIR=%SCRIPT_DIR%NativeWrapper

echo Step 1: Building Native Wrapper...
echo.

cd /d "%WRAPPER_DIR%"

REM Try to find Visual Studio
set VS_FOUND=0

REM Check for vswhere
where vswhere.exe >nul 2>&1
if %ERRORLEVEL% == 0 (
    echo Found vswhere.exe, detecting Visual Studio...
    for /f "tokens=*" %%i in ('vswhere.exe -latest -property installationPath') do set VS_PATH=%%i
    if defined VS_PATH (
        set VS_FOUND=1
        echo Found Visual Studio at: %VS_PATH%
    )
)

REM Try common Visual Studio paths
if %VS_FOUND% == 0 (
    if exist "C:\Program Files\Microsoft Visual Studio\2022\Community\VC\Auxiliary\Build\vcvars64.bat" (
        set VS_PATH=C:\Program Files\Microsoft Visual Studio\2022\Community
        set VS_FOUND=1
    ) else if exist "C:\Program Files\Microsoft Visual Studio\2019\Community\VC\Auxiliary\Build\vcvars64.bat" (
        set VS_PATH=C:\Program Files\Microsoft Visual Studio\2019\Community
        set VS_FOUND=1
    ) else if exist "C:\Program Files (x86)\Microsoft Visual Studio\2019\Community\VC\Auxiliary\Build\vcvars64.bat" (
        set VS_PATH=C:\Program Files (x86)\Microsoft Visual Studio\2019\Community
        set VS_FOUND=1
    )
)

if %VS_FOUND% == 1 (
    echo.
    echo Building with Visual Studio...
    echo.
    
    REM Initialize Visual Studio environment
    call "%VS_PATH%\VC\Auxiliary\Build\vcvars64.bat"
    
    if not exist build mkdir build
    cd build
    
    REM Try CMake first
    where cmake >nul 2>&1
    if %ERRORLEVEL% == 0 (
        echo Using CMake...
        cmake .. -G "Visual Studio 16 2019" -A x64
        if %ERRORLEVEL% == 0 (
            cmake --build . --config Release
            if %ERRORLEVEL% == 0 goto :build_success
        )
    )
    
    REM Fall back to direct compilation
    echo Using direct compilation...
    cl /EHsc /LD /O2 /I.. /D_WIN32 /D_WINHTTP /link winhttp.lib /OUT:star_api.dll ..\star_api.cpp
    
    if %ERRORLEVEL% == 0 (
        :build_success
        echo.
        echo [SUCCESS] Native wrapper built!
        if exist Release\star_api.dll (
            echo Library: build\Release\star_api.dll
        ) else if exist star_api.dll (
            echo Library: build\star_api.dll
        )
    ) else (
        echo [ERROR] Build failed!
        goto :end
    )
) else (
    echo [WARNING] Visual Studio not found automatically.
    echo Please build manually using one of these methods:
    echo   1. Open star_api.vcxproj in Visual Studio
    echo   2. Run QUICK_BUILD.bat
    echo   3. Install CMake and run: cmake .. && cmake --build . --config Release
    echo.
    goto :skip_build
)

:skip_build
echo.
echo Step 2: Setting up environment variables...
echo.

REM Check if credentials are already set
if defined STAR_USERNAME (
    echo [INFO] STAR_USERNAME is already set
) else (
    echo [INFO] STAR_USERNAME not set - you'll need to set it manually
    echo    Run: set STAR_USERNAME=your_username
)

if defined STAR_PASSWORD (
    echo [INFO] STAR_PASSWORD is already set
) else (
    echo [INFO] STAR_PASSWORD not set - you'll need to set it manually
    echo    Run: set STAR_PASSWORD=your_password
)

echo.
echo Step 3: Verifying integration files...
echo.

REM Check DOOM integration
if exist "%DOOM_PATH%\doom_star_integration.c" (
    echo [OK] DOOM integration files found
) else (
    echo [WARNING] DOOM integration files not found at: %DOOM_PATH%
)

REM Check Quake integration
if exist "%QUAKE_PATH%\quake_star_integration.c" (
    echo [OK] Quake integration files found
) else (
    echo [WARNING] Quake integration files not found at: %QUAKE_PATH%
)

echo.
echo Step 4: Creating test script...
echo.

REM Create a test script
(
echo @echo off
echo REM Test script for STAR API integration
echo echo Testing STAR API Integration...
echo echo.
echo if defined STAR_USERNAME ^(
echo     echo [OK] STAR_USERNAME is set
echo ^) else ^(
echo     echo [WARNING] STAR_USERNAME not set
echo ^)
echo if defined STAR_PASSWORD ^(
echo     echo [OK] STAR_PASSWORD is set
echo ^) else ^(
echo     echo [WARNING] STAR_PASSWORD not set
echo ^)
echo echo.
echo echo To test DOOM:
echo echo   cd %DOOM_PATH%
echo echo   make
echo echo   .\linux\linuxxdoom.exe
echo echo.
echo pause
) > "%SCRIPT_DIR%test_star_api.bat"

echo [OK] Test script created: test_star_api.bat

echo.
echo ========================================
echo Setup Complete!
echo ========================================
echo.
echo Next Steps:
echo 1. Set your credentials:
echo    set STAR_USERNAME=your_username
echo    set STAR_PASSWORD=your_password
echo.
echo 2. Build ODOOM: OASIS Omniverse\ODOOM\BUILD ODOOM.bat
echo    Build OQuake: "OASIS Omniverse\OQuake\BUILD_OQUAKE.bat" (Developer Command Prompt for VS)
echo.
echo 3. Run: ODOOM\build\ODOOM.exe or OQuake\build\OQUAKE.exe
echo.
echo For detailed instructions, see:
echo   - README.md
echo   - DEVELOPER_ONBOARDING.md
echo.

:end
cd /d "%SCRIPT_DIR%"
pause



