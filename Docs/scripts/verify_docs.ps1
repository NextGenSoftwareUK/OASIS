# OASIS Documentation Verification Script (PowerShell)
# This script verifies documentation accuracy against the codebase

$ErrorActionPreference = "Continue"
$errors = 0
$warnings = 0

function Write-Error-Message {
    param([string]$message)
    Write-Host "ERROR: $message" -ForegroundColor Red
    $script:errors++
}

function Write-Warning-Message {
    param([string]$message)
    Write-Host "WARNING: $message" -ForegroundColor Yellow
    $script:warnings++
}

function Write-Success-Message {
    param([string]$message)
    Write-Host "✓ $message" -ForegroundColor Green
}

Write-Host "=== OASIS Documentation Verification ===" -ForegroundColor Cyan
Write-Host ""

# Get project root (parent of docs directory)
$projectRoot = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)
Set-Location $projectRoot

Write-Host "Project root: $projectRoot"
Write-Host ""

# 1. Verify Provider Enum
Write-Host "1. Verifying Provider Enum..." -ForegroundColor Cyan
$providerEnumFile = "OASIS Architecture\NextGenSoftware.OASIS.API.Core\Enums\ProviderType.cs"
if (Test-Path $providerEnumFile) {
    $enumCount = (Select-String -Path $providerEnumFile -Pattern "OASIS," | Measure-Object).Count
    Write-Success-Message "Found $enumCount providers in enum"
    
    if ($enumCount -lt 55 -or $enumCount -gt 65) {
        Write-Warning-Message "Provider count ($enumCount) may not match documentation (expected ~60)"
    }
} else {
    Write-Error-Message "Provider enum file not found: $providerEnumFile"
}
Write-Host ""

# 2. Verify Provider Directories
Write-Host "2. Verifying Provider Implementations..." -ForegroundColor Cyan
if (Test-Path "Providers") {
    $providerDirs = (Get-ChildItem -Path "Providers" -Recurse -Directory -Filter "*OASIS" | Measure-Object).Count
    Write-Success-Message "Found $providerDirs provider directories"
} else {
    Write-Error-Message "Providers directory not found"
}
Write-Host ""

# 3. Verify OASIS_DNA.json
Write-Host "3. Verifying OASIS_DNA.json..." -ForegroundColor Cyan
if (Test-Path "OASIS_DNA.json") {
    Write-Success-Message "OASIS_DNA.json exists"
    
    try {
        $dna = Get-Content "OASIS_DNA.json" | ConvertFrom-Json
        Write-Success-Message "OASIS_DNA.json is valid JSON"
        
        if ($dna.OASIS.StorageProviders) {
            Write-Success-Message "StorageProviders section exists"
        } else {
            Write-Warning-Message "StorageProviders section not found in OASIS_DNA.json"
        }
    } catch {
        Write-Error-Message "OASIS_DNA.json is not valid JSON: $_"
    }
} else {
    Write-Error-Message "OASIS_DNA.json not found"
}
Write-Host ""

# 4. Verify HyperDrive Implementation
Write-Host "4. Verifying HyperDrive Implementation..." -ForegroundColor Cyan
$hyperDriveFile = "OASIS Architecture\NextGenSoftware.OASIS.API.Core\Managers\OASIS HyperDrive\OASISHyperDrive.cs"
if (Test-Path $hyperDriveFile) {
    Write-Success-Message "HyperDrive file exists"
    
    $content = Get-Content $hyperDriveFile -Raw
    
    $methods = @("FailoverRequestAsync", "ReplicateRequestAsync", "LoadBalanceRequestAsync")
    foreach ($method in $methods) {
        if ($content -match $method) {
            Write-Success-Message "$method method found"
        } else {
            Write-Error-Message "$method method not found"
        }
    }
} else {
    Write-Error-Message "HyperDrive file not found: $hyperDriveFile"
}
Write-Host ""

# 5. Verify Manager Classes
Write-Host "5. Verifying Manager Classes..." -ForegroundColor Cyan
$managersDir = "OASIS Architecture\NextGenSoftware.OASIS.API.Core\Managers"
if (Test-Path $managersDir) {
    $managerFiles = Get-ChildItem -Path $managersDir -Recurse -Filter "*Manager.cs" | 
        Where-Object { $_.FullName -notmatch "OASIS HyperDrive" }
    $managerCount = ($managerFiles | Measure-Object).Count
    Write-Success-Message "Found $managerCount manager classes"
    
    $keyManagers = @("AvatarManager", "HolonManager", "WalletManager", "KeyManager")
    foreach ($manager in $keyManagers) {
        $found = $managerFiles | Where-Object { $_.Name -eq "$manager.cs" }
        if ($found) {
            Write-Success-Message "$manager exists"
        } else {
            Write-Warning-Message "$manager not found"
        }
    }
} else {
    Write-Error-Message "Managers directory not found: $managersDir"
}
Write-Host ""

# 6. Check Documentation Dates
Write-Host "6. Checking Documentation Dates..." -ForegroundColor Cyan
$currentYear = (Get-Date).Year
$docsPath = "docs"

$docFiles = Get-ChildItem -Path $docsPath -Recurse -Filter "*.md"
$dateIssues = @()

foreach ($file in $docFiles) {
    $content = Get-Content $file -Raw
    if ($content -match "Last Updated:\s*([^\n]+)") {
        $dateStr = $matches[1] -replace '\*\*', '' -replace '\s', ''
        if ($dateStr -notmatch "December 2025" -and $dateStr -notmatch "$currentYear") {
            $dateIssues += "$($file.Name): $dateStr"
        }
    }
}

if ($dateIssues.Count -eq 0) {
    Write-Success-Message "All documentation dates are current"
} else {
    Write-Warning-Message "Found dates that may need updating:"
    foreach ($issue in $dateIssues) {
        Write-Warning-Message "  $issue"
    }
}
Write-Host ""

# Summary
Write-Host "=== Verification Summary ===" -ForegroundColor Cyan
if ($errors -eq 0 -and $warnings -eq 0) {
    Write-Host "All checks passed!" -ForegroundColor Green
    exit 0
} elseif ($errors -eq 0) {
    Write-Host "Verification complete with $warnings warning(s)" -ForegroundColor Yellow
    exit 0
} else {
    Write-Host "Verification failed with $errors error(s) and $warnings warning(s)" -ForegroundColor Red
    exit 1
}



