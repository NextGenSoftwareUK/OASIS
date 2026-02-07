# Automated execution of next steps for STAR API integration
# This script automates the build and setup process

Write-Host "========================================" -ForegroundColor Green
Write-Host "OASIS STAR API - Automated Setup" -ForegroundColor Green
Write-Host "========================================" -ForegroundColor Green
Write-Host ""

$ErrorActionPreference = "Continue"
$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$oasisRoot = Split-Path -Parent $scriptDir
$wrapperDir = Join-Path $scriptDir "NativeWrapper"
$buildDir = Join-Path $wrapperDir "build"
$doomPath = Join-Path (Split-Path -Parent $oasisRoot) "DOOM\linuxdoom-1.10"
$quakePath = Join-Path (Split-Path -Parent $oasisRoot) "quake-rerelease-qc"

# Step 1: Build Native Wrapper
Write-Host "Step 1: Building Native Wrapper..." -ForegroundColor Cyan
Write-Host ""

# Check for Visual Studio
$vsPaths = @(
    "C:\Program Files\Microsoft Visual Studio\2022\Community",
    "C:\Program Files\Microsoft Visual Studio\2022\Professional",
    "C:\Program Files\Microsoft Visual Studio\2019\Community"
)

$vsPath = $null
$vcvarsPath = $null

foreach ($path in $vsPaths) {
    $vcvars = Join-Path $path "VC\Auxiliary\Build\vcvars64.bat"
    if (Test-Path $vcvars) {
        $vsPath = $path
        $vcvarsPath = $vcvars
        break
    }
}

if ($vsPath) {
    Write-Host "[OK] Found Visual Studio at: $vsPath" -ForegroundColor Green
    
    # Try to open the project file
    $projectFile = Join-Path $wrapperDir "star_api.vcxproj"
    if (Test-Path $projectFile) {
        Write-Host "Opening Visual Studio project..." -ForegroundColor Yellow
        $devenv = Join-Path $vsPath "Common7\IDE\devenv.exe"
        if (Test-Path $devenv) {
            Start-Process $devenv -ArgumentList "`"$projectFile`"" -WorkingDirectory $wrapperDir
            Write-Host "[INFO] Visual Studio opened. Please build the project (Release, x64)" -ForegroundColor Yellow
            Write-Host "       Then press any key to continue..." -ForegroundColor Yellow
            $null = $Host.UI.RawUI.ReadKey("NoEcho,IncludeKeyDown")
        }
    }
    
    # Check if build was successful
    $dllPath = Join-Path $buildDir "Release\star_api.dll"
    if (-not (Test-Path $dllPath)) {
        $dllPath = Join-Path $buildDir "star_api.dll"
    }
    
    if (Test-Path $dllPath) {
        Write-Host "[SUCCESS] Native wrapper built!" -ForegroundColor Green
        Write-Host "Library: $dllPath" -ForegroundColor Green
    } else {
        Write-Host "[WARNING] Native wrapper not found. Please build manually." -ForegroundColor Yellow
        Write-Host "  1. In Visual Studio: Build -> Build Solution (Release, x64)" -ForegroundColor White
        Write-Host "  2. Or run: QUICK_BUILD.bat" -ForegroundColor White
    }
} else {
    Write-Host "[WARNING] Visual Studio not found automatically." -ForegroundColor Yellow
    Write-Host "Please build manually using one of:" -ForegroundColor White
    Write-Host "  1. Open star_api.vcxproj in Visual Studio" -ForegroundColor White
    Write-Host "  2. Run QUICK_BUILD.bat" -ForegroundColor White
}

Write-Host ""

# Step 2: Check/Create environment setup script
Write-Host "Step 2: Creating environment setup script..." -ForegroundColor Cyan

$envScript = @"
@echo off
REM Environment setup for STAR API
REM Set your credentials here

set STAR_USERNAME=your_username_here
set STAR_PASSWORD=your_password_here

REM Or use API key:
REM set STAR_API_KEY=your_api_key_here
REM set STAR_AVATAR_ID=your_avatar_id_here

echo Environment variables set for STAR API
echo.
echo STAR_USERNAME=%STAR_USERNAME%
echo STAR_PASSWORD=***hidden***
echo.
echo To use these in current session, run: call set_star_env.bat
echo.
"@

$envScriptPath = Join-Path $scriptDir "set_star_env.bat"
$envScript | Out-File -FilePath $envScriptPath -Encoding ASCII
Write-Host "[OK] Created: set_star_env.bat" -ForegroundColor Green
Write-Host "     Edit this file and add your credentials" -ForegroundColor Yellow
Write-Host ""

# Step 3: Create DOOM build script
Write-Host "Step 3: Creating DOOM build script..." -ForegroundColor Cyan

if (Test-Path $doomPath) {
    $doomBuildScript = @"
@echo off
REM Build script for DOOM with STAR API integration

echo Building DOOM with STAR API integration...
echo.

cd /d "$doomPath"

REM Load STAR API environment
if exist "..\..\OASIS-master\Game Integration\set_star_env.bat" (
    call "..\..\OASIS-master\Game Integration\set_star_env.bat"
)

REM Build
make

if %ERRORLEVEL% == 0 (
    echo.
    echo [SUCCESS] DOOM built successfully!
    echo.
    echo To run:
    echo   .\linux\linuxxdoom.exe
) else (
    echo.
    echo [ERROR] Build failed!
    echo Check the error messages above
)

pause
"@
    
    $doomBuildPath = Join-Path $doomPath "build_doom_star.bat"
    $doomBuildScript | Out-File -FilePath $doomBuildPath -Encoding ASCII
    Write-Host "[OK] Created: $doomBuildPath" -ForegroundColor Green
} else {
    Write-Host "[WARNING] DOOM path not found: $doomPath" -ForegroundColor Yellow
}

Write-Host ""

# Step 4: Create test script
Write-Host "Step 4: Creating test script..." -ForegroundColor Cyan

$testScript = @"
@echo off
REM Test script for STAR API integration

echo ========================================
echo STAR API Integration Test
echo ========================================
echo.

REM Check environment
echo Checking environment variables...
if defined STAR_USERNAME (
    echo [OK] STAR_USERNAME is set
) else (
    echo [WARNING] STAR_USERNAME not set
    echo Run: set_star_env.bat first
)

if defined STAR_PASSWORD (
    echo [OK] STAR_PASSWORD is set
) else (
    echo [WARNING] STAR_PASSWORD not set
)

echo.

REM Check native wrapper
echo Checking native wrapper...
if exist "NativeWrapper\build\Release\star_api.dll" (
    echo [OK] star_api.dll found
) else if exist "NativeWrapper\build\star_api.dll" (
    echo [OK] star_api.dll found
) else (
    echo [WARNING] star_api.dll not found
    echo Please build the native wrapper first
)

echo.

REM Check DOOM integration
echo Checking DOOM integration...
if exist "$doomPath\doom_star_integration.c" (
    echo [OK] DOOM integration files found
) else (
    echo [WARNING] DOOM integration files not found
)

echo.
echo ========================================
echo Test Complete
echo ========================================
echo.
pause
"@

$testScriptPath = Join-Path $scriptDir "test_integration_complete.bat"
$testScript | Out-File -FilePath $testScriptPath -Encoding ASCII
Write-Host "[OK] Created: test_integration_complete.bat" -ForegroundColor Green
Write-Host ""

# Step 5: Summary
Write-Host "========================================" -ForegroundColor Green
Write-Host "Setup Complete!" -ForegroundColor Green
Write-Host "========================================" -ForegroundColor Green
Write-Host ""
Write-Host "Next Steps:" -ForegroundColor Cyan
Write-Host "1. Edit set_star_env.bat and add your credentials" -ForegroundColor White
Write-Host "2. Build native wrapper (Visual Studio should be open)" -ForegroundColor White
Write-Host "3. Run: $doomPath\build_doom_star.bat" -ForegroundColor White
Write-Host "4. Test: Run DOOM and check console output" -ForegroundColor White
Write-Host ""
Write-Host "Files Created:" -ForegroundColor Cyan
Write-Host "  - set_star_env.bat (edit with your credentials)" -ForegroundColor White
Write-Host "  - $doomPath\build_doom_star.bat" -ForegroundColor White
Write-Host "  - test_integration_complete.bat" -ForegroundColor White
Write-Host ""



