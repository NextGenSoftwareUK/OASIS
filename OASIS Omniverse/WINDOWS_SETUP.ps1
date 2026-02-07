# OASIS STAR API - Windows Setup Script
# This script helps set up the integration for DOOM and Quake forks on Windows

param(
    [string]$DoomPath = "C:\Source\DOOM",
    [string]$QuakePath = "C:\Source\quake-rerelease-qc",
    [string]$OasisPath = "C:\Source\OASIS-master",
    [switch]$BuildWrapper,
    [switch]$CopyFiles,
    [switch]$SetupEnv
)

Write-Host "OASIS STAR API - Windows Setup" -ForegroundColor Green
Write-Host "================================" -ForegroundColor Green
Write-Host ""

# Check paths
if (-not (Test-Path $OasisPath)) {
    Write-Host "Error: OASIS path not found: $OasisPath" -ForegroundColor Red
    Write-Host "Please update the -OasisPath parameter" -ForegroundColor Yellow
    exit 1
}

# Step 1: Build Native Wrapper
if ($BuildWrapper) {
    Write-Host "Step 1: Building Native Wrapper..." -ForegroundColor Cyan
    
    $wrapperPath = Join-Path $OasisPath "Game Integration\NativeWrapper"
    $buildPath = Join-Path $wrapperPath "build"
    
    if (-not (Test-Path $wrapperPath)) {
        Write-Host "Error: Native wrapper path not found: $wrapperPath" -ForegroundColor Red
        exit 1
    }
    
    # Create build directory
    if (-not (Test-Path $buildPath)) {
        New-Item -ItemType Directory -Path $buildPath | Out-Null
    }
    
    Push-Location $buildPath
    
    try {
        # Check for Visual Studio
        $vsPath = & "${env:ProgramFiles(x86)}\Microsoft Visual Studio\Installer\vswhere.exe" -latest -property installationPath 2>$null
        if ($vsPath) {
            Write-Host "Found Visual Studio at: $vsPath" -ForegroundColor Green
            cmake .. -G "Visual Studio 16 2019" -A x64
            cmake --build . --config Release
        } else {
            Write-Host "Visual Studio not found. Trying MinGW..." -ForegroundColor Yellow
            cmake .. -G "MinGW Makefiles"
            cmake --build . --config Release
        }
        
        Write-Host "Native wrapper built successfully!" -ForegroundColor Green
    } catch {
        Write-Host "Error building native wrapper: $_" -ForegroundColor Red
        Pop-Location
        exit 1
    }
    
    Pop-Location
}

# Step 2: Copy Integration Files
if ($CopyFiles) {
    Write-Host "`nStep 2: Copying Integration Files..." -ForegroundColor Cyan
    
    # Copy to DOOM
    if (Test-Path $DoomPath) {
        $doomTarget = Join-Path $DoomPath "linuxdoom-1.10"
        if (Test-Path $doomTarget) {
            Write-Host "Copying files to DOOM..." -ForegroundColor Yellow
            Copy-Item (Join-Path $OasisPath "Game Integration\Doom\doom_star_integration.c") $doomTarget -Force
            Copy-Item (Join-Path $OasisPath "Game Integration\Doom\doom_star_integration.h") $doomTarget -Force
            Copy-Item (Join-Path $OasisPath "Game Integration\NativeWrapper\star_api.h") $doomTarget -Force
            Write-Host "DOOM files copied!" -ForegroundColor Green
        } else {
            Write-Host "Warning: DOOM target directory not found: $doomTarget" -ForegroundColor Yellow
        }
    } else {
        Write-Host "Warning: DOOM path not found: $DoomPath" -ForegroundColor Yellow
    }
    
    # Copy to Quake
    if (Test-Path $QuakePath) {
        Write-Host "Copying files to Quake..." -ForegroundColor Yellow
        Copy-Item (Join-Path $OasisPath "Game Integration\Quake\quake_star_integration.c") $QuakePath -Force
        Copy-Item (Join-Path $OasisPath "Game Integration\Quake\quake_star_integration.h") $QuakePath -Force
        Copy-Item (Join-Path $OasisPath "Game Integration\NativeWrapper\star_api.h") $QuakePath -Force
        Write-Host "Quake files copied!" -ForegroundColor Green
    } else {
        Write-Host "Warning: Quake path not found: $QuakePath" -ForegroundColor Yellow
    }
}

# Step 3: Setup Environment Variables
if ($SetupEnv) {
    Write-Host "`nStep 3: Setting up Environment Variables..." -ForegroundColor Cyan
    Write-Host "Please enter your STAR API credentials:" -ForegroundColor Yellow
    
    $username = Read-Host "Username (or press Enter to skip SSO)"
    if ($username) {
        $password = Read-Host "Password" -AsSecureString
        $passwordPlain = [Runtime.InteropServices.Marshal]::PtrToStringAuto(
            [Runtime.InteropServices.Marshal]::SecureStringToBSTR($password)
        )
        
        [System.Environment]::SetEnvironmentVariable("STAR_USERNAME", $username, "User")
        [System.Environment]::SetEnvironmentVariable("STAR_PASSWORD", $passwordPlain, "User")
        Write-Host "SSO credentials set!" -ForegroundColor Green
    }
    
    $apiKey = Read-Host "API Key (or press Enter to skip)"
    if ($apiKey) {
        $avatarId = Read-Host "Avatar ID"
        [System.Environment]::SetEnvironmentVariable("STAR_API_KEY", $apiKey, "User")
        [System.Environment]::SetEnvironmentVariable("STAR_AVATAR_ID", $avatarId, "User")
        Write-Host "API key credentials set!" -ForegroundColor Green
    }
    
    Write-Host "`nNote: You may need to restart your terminal for environment variables to take effect." -ForegroundColor Yellow
}

# Summary
Write-Host "`n========================================" -ForegroundColor Green
Write-Host "Setup Complete!" -ForegroundColor Green
Write-Host "========================================" -ForegroundColor Green
Write-Host ""
Write-Host "Next Steps:" -ForegroundColor Cyan
Write-Host "1. Follow integration instructions:" -ForegroundColor White
Write-Host "   - DOOM: Game Integration\Doom\WINDOWS_INTEGRATION.md" -ForegroundColor Gray
Write-Host "   - Quake: Game Integration\Quake\WINDOWS_INTEGRATION.md" -ForegroundColor Gray
Write-Host "2. Modify source files as instructed" -ForegroundColor White
Write-Host "3. Build the games" -ForegroundColor White
Write-Host "4. Test cross-game item sharing!" -ForegroundColor White
Write-Host ""



