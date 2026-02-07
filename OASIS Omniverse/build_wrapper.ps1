# PowerShell script to build the native wrapper
# This script automates the build process

Write-Host "========================================" -ForegroundColor Green
Write-Host "OASIS STAR API - Native Wrapper Builder" -ForegroundColor Green
Write-Host "========================================" -ForegroundColor Green
Write-Host ""

$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$wrapperDir = Join-Path $scriptDir "NativeWrapper"
$buildDir = Join-Path $wrapperDir "build"

# Step 1: Create build directory
Write-Host "Step 1: Setting up build directory..." -ForegroundColor Cyan
if (Test-Path $buildDir) {
    Remove-Item $buildDir -Recurse -Force
}
New-Item -ItemType Directory -Path $buildDir -Force | Out-Null
Write-Host "[OK] Build directory created" -ForegroundColor Green
Write-Host ""

# Step 2: Find Visual Studio
Write-Host "Step 2: Detecting Visual Studio..." -ForegroundColor Cyan
$vsPaths = @(
    "C:\Program Files\Microsoft Visual Studio\2022\Community",
    "C:\Program Files\Microsoft Visual Studio\2022\Professional",
    "C:\Program Files\Microsoft Visual Studio\2022\Enterprise",
    "C:\Program Files\Microsoft Visual Studio\2019\Community",
    "C:\Program Files\Microsoft Visual Studio\2019\Professional",
    "C:\Program Files\Microsoft Visual Studio\2019\Enterprise",
    "C:\Program Files (x86)\Microsoft Visual Studio\2019\Community"
)

$vsPath = $null
$vcvarsPath = $null

foreach ($path in $vsPaths) {
    $vcvars = Join-Path $path "VC\Auxiliary\Build\vcvars64.bat"
    if (Test-Path $vcvars) {
        $vsPath = $path
        $vcvarsPath = $vcvars
        Write-Host "[OK] Found Visual Studio at: $path" -ForegroundColor Green
        break
    }
}

if (-not $vsPath) {
    Write-Host "[ERROR] Visual Studio not found!" -ForegroundColor Red
    Write-Host "Please install Visual Studio 2019 or later with C++ tools" -ForegroundColor Yellow
    Write-Host "Or build manually using the .vcxproj file" -ForegroundColor Yellow
    exit 1
}

# Step 3: Try CMake build
Write-Host ""
Write-Host "Step 3: Building with CMake..." -ForegroundColor Cyan

$cmakePath = Get-Command cmake -ErrorAction SilentlyContinue
if ($cmakePath) {
    Push-Location $buildDir
    
    try {
        # Determine VS version
        $vsVersion = "Visual Studio 17 2022"
        if ($vsPath -like "*2019*") {
            $vsVersion = "Visual Studio 16 2019"
        }
        
        Write-Host "Running CMake..." -ForegroundColor Yellow
        & cmake .. -G $vsVersion -A x64
        
        if ($LASTEXITCODE -eq 0) {
            Write-Host "Building..." -ForegroundColor Yellow
            & cmake --build . --config Release
            
            if ($LASTEXITCODE -eq 0) {
                $dllPath = Join-Path $buildDir "Release\star_api.dll"
                if (Test-Path $dllPath) {
                    Write-Host ""
                    Write-Host "[SUCCESS] Build complete!" -ForegroundColor Green
                    Write-Host "Library: $dllPath" -ForegroundColor Green
                    Pop-Location
                    exit 0
                }
            }
        }
    } catch {
        Write-Host "[WARNING] CMake build failed: $_" -ForegroundColor Yellow
    } finally {
        Pop-Location
    }
} else {
    Write-Host "[INFO] CMake not found, trying direct compilation..." -ForegroundColor Yellow
}

# Step 4: Try direct compilation
Write-Host ""
Write-Host "Step 4: Trying direct compilation..." -ForegroundColor Cyan

Push-Location $buildDir
try {
    # Initialize VS environment and compile
    $compileCmd = "cl /EHsc /LD /O2 /I.. /D_WIN32 /D_WINHTTP /link winhttp.lib /OUT:star_api.dll ..\star_api.cpp"
    
    # Use cmd to run vcvars and compile
    $fullCmd = "cmd /c `"call `"$vcvarsPath`" && $compileCmd`""
    Invoke-Expression $fullCmd
    
    if ($LASTEXITCODE -eq 0 -and (Test-Path "star_api.dll")) {
        Write-Host ""
        Write-Host "[SUCCESS] Build complete!" -ForegroundColor Green
        Write-Host "Library: $(Join-Path $buildDir 'star_api.dll')" -ForegroundColor Green
        Pop-Location
        exit 0
    } else {
        Write-Host "[ERROR] Direct compilation failed" -ForegroundColor Red
    }
} catch {
    Write-Host "[ERROR] Build failed: $_" -ForegroundColor Red
} finally {
    Pop-Location
}

Write-Host ""
Write-Host "[INFO] Automatic build failed. Please build manually:" -ForegroundColor Yellow
Write-Host "  1. Open Visual Studio" -ForegroundColor White
Write-Host "  2. Open: $wrapperDir\star_api.vcxproj" -ForegroundColor White
Write-Host "  3. Build -> Build Solution (Release, x64)" -ForegroundColor White
Write-Host ""
exit 1

