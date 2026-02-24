@echo off
REM Test script for STAR API integration
REM This script helps verify the integration is working

echo ========================================
echo OASIS STAR API Integration Test
echo ========================================
echo.

REM Check if native wrapper is built
echo Step 1: Checking native wrapper...
if exist "NativeWrapper\build\Release\star_api.dll" (
    echo [OK] star_api.dll found
) else if exist "NativeWrapper\build\star_api.dll" (
    echo [OK] star_api.dll found
) else (
    echo [WARNING] star_api.dll not found!
    echo Please build the native wrapper first (see QUICK_BUILD.bat)
    echo.
)

REM Check environment variables
echo.
echo Step 2: Checking environment variables...
if defined STAR_USERNAME (
    echo [OK] STAR_USERNAME is set
) else (
    echo [WARNING] STAR_USERNAME not set
)

if defined STAR_PASSWORD (
    echo [OK] STAR_PASSWORD is set
) else (
    echo [WARNING] STAR_PASSWORD not set
)

if defined STAR_API_KEY (
    echo [OK] STAR_API_KEY is set
) else (
    echo [INFO] STAR_API_KEY not set (using SSO instead)
)

if defined STAR_AVATAR_ID (
    echo [OK] STAR_AVATAR_ID is set
) else (
    echo [INFO] STAR_AVATAR_ID not set (using SSO instead)
)

REM Check DOOM integration files
echo.
echo Step 3: Checking DOOM integration...
if exist "..\..\DOOM\linuxdoom-1.10\doom_star_integration.c" (
    echo [OK] DOOM integration files found
) else (
    echo [WARNING] DOOM integration files not found
    echo Expected: C:\Source\DOOM\linuxdoom-1.10\doom_star_integration.c
)

REM Check Quake integration files
echo.
echo Step 4: Checking Quake integration...
if exist "..\..\quake-rerelease-qc\quake_star_integration.c" (
    echo [OK] Quake integration files found
) else (
    echo [WARNING] Quake integration files not found
    echo Expected: C:\Source\quake-rerelease-qc\quake_star_integration.c
)

echo.
echo ========================================
echo Test Complete!
echo ========================================
echo.
echo Next Steps:
echo 1. Build native wrapper (if not built)
echo 2. Set environment variables (if not set)
echo 3. Build DOOM
echo 4. Run DOOM and check console output
echo.
pause



