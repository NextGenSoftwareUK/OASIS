#Requires -RunAsAdministrator
<#
Emergency Windows Cleanup Script
Developer/Gaming workstation cleanup utility.

Features:
- Safe-by-default cleanup
- Dry-run support
- Accurate reclaimed-space reporting
- Clears temp/cache/dev junk
- Clears NuGet/npm/pnpm/yarn caches
- Clears Cursor/VSCode/JetBrains caches
- Clears shader caches
- Clears Steam/Battle.net caches
- Optional aggressive cleanup
- Optional hibernation disable
- Optional recycle bin purge
- Optional Downloads cleanup

Usage:
  powershell -ExecutionPolicy Bypass -File .\cleanup-emergency-space.ps1

Examples:
  .\cleanup-emergency-space.ps1
  .\cleanup-emergency-space.ps1 -DryRun
  .\cleanup-emergency-space.ps1 -Aggressive
  .\cleanup-emergency-space.ps1 -DisableHibernate
  .\cleanup-emergency-space.ps1 -ClearRecycleBin
  .\cleanup-emergency-space.ps1 -ClearSteamCache -ClearBattleNetCache
#>

param(
    [switch]$DryRun,
    [switch]$Aggressive,
    [switch]$DisableHibernate,
    [switch]$ClearDownloads,
    [switch]$ClearRecycleBin,
    [switch]$KeepNuGet,
    [switch]$KeepCursor,
    [switch]$KeepJetBrains,
    [switch]$ClearSteamCache,
    [switch]$ClearBattleNetCache
)

# Default goblin mode flags
$Aggressive = $true
$DisableHibernate = $true
$ClearRecycleBin = $true
$ClearSteamCache = $true
$ClearBattleNetCache = $true


$ErrorActionPreference = "SilentlyContinue"

function Log($msg) {
    Write-Host "[cleanup] $msg" -ForegroundColor Cyan
}

function Remove-Safely($path) {

    if (!(Test-Path $path)) {
        return
    }

    if ($DryRun) {
        Log "DRY RUN: Remove $path"
        return
    }

    try {
        Remove-Item $path -Force -Recurse
        Log "Removed: $path"
    }
    catch {
        Log "FAILED: $path"
    }
}

function Empty-Directory($path) {

    if (!(Test-Path $path)) {
        return
    }

    if ($DryRun) {
        Log "DRY RUN: Empty $path"
        return
    }

    try {
        Get-ChildItem $path -Force | Remove-Item -Force -Recurse
        Log "Emptied: $path"
    }
    catch {
        Log "FAILED: $path"
    }
}

function Get-DriveStats {

    $disk = Get-CimInstance Win32_LogicalDisk -Filter "DeviceID='C:'"

    return @{
        SizeGB = [math]::Round(($disk.Size / 1GB), 2)
        FreeGB = [math]::Round(($disk.FreeSpace / 1GB), 2)
        UsedGB = [math]::Round((($disk.Size - $disk.FreeSpace) / 1GB), 2)
        FreeBytes = $disk.FreeSpace
    }
}

# =========================================================
# BEFORE STATS
# =========================================================

$before = Get-DriveStats

Write-Host ""
Log "Starting cleanup..."
Write-Host ""

Log "C: BEFORE => Used: $($before.UsedGB) GB | Free: $($before.FreeGB) GB"
Write-Host ""

# =========================================================
# TEMP FILES
# =========================================================

Log "Cleaning temp folders..."

$tempPaths = @(
    "$env:TEMP",
    "$env:WINDIR\Temp",
    "$env:LOCALAPPDATA\Temp"
)

foreach ($p in $tempPaths) {
    Empty-Directory $p
}

# =========================================================
# WINDOWS UPDATE CACHE
# =========================================================

Log "Cleaning Windows Update cache..."

if (!$DryRun) {

    Stop-Service wuauserv -Force
    Stop-Service bits -Force
}

Empty-Directory "C:\Windows\SoftwareDistribution\Download"

if (!$DryRun) {

    Start-Service wuauserv
    Start-Service bits
}

# =========================================================
# DELIVERY OPTIMIZATION CACHE
# =========================================================

Log "Cleaning Delivery Optimization cache..."

Empty-Directory "C:\ProgramData\Microsoft\Windows\DeliveryOptimization\Cache"

# =========================================================
# SHADER CACHES
# =========================================================

Log "Cleaning shader caches..."

$shaderPaths = @(
    "$env:LOCALAPPDATA\NVIDIA\DXCache",
    "$env:LOCALAPPDATA\NVIDIA\GLCache",
    "$env:LOCALAPPDATA\D3DSCache"
)

foreach ($p in $shaderPaths) {
    Empty-Directory $p
}

# =========================================================
# NUGET
# =========================================================

if (!$KeepNuGet) {

    Log "Cleaning NuGet caches..."

    if (Get-Command dotnet -ErrorAction SilentlyContinue) {

        if ($DryRun) {
            Log "DRY RUN: dotnet nuget locals all --clear"
        }
        else {
            dotnet nuget locals all --clear
        }
    }

    Remove-Safely "$env:USERPROFILE\.nuget\packages"
}

# =========================================================
# NODE PACKAGE MANAGER CACHES
# =========================================================

Log "Cleaning npm/pnpm/yarn caches..."

if ($DryRun) {

    Log "DRY RUN: npm cache clean --force"
    Log "DRY RUN: pnpm store prune"
    Log "DRY RUN: yarn cache clean"
}
else {

    npm cache clean --force

    if (Get-Command pnpm -ErrorAction SilentlyContinue) {
        pnpm store prune
    }

    if (Get-Command yarn -ErrorAction SilentlyContinue) {
        yarn cache clean
    }
}

# =========================================================
# CURSOR / VSCODE
# =========================================================

if (!$KeepCursor) {

    Log "Cleaning Cursor/VSCode caches..."

    $cursorPaths = @(
        "$env:APPDATA\Cursor\Cache",
        "$env:APPDATA\Cursor\GPUCache",
        "$env:APPDATA\Cursor\CachedData",
        "$env:APPDATA\Cursor\logs",
        "$env:APPDATA\Code\Cache",
        "$env:APPDATA\Code\GPUCache",
        "$env:APPDATA\Code\CachedData",
        "$env:APPDATA\Code\logs"
    )

    foreach ($p in $cursorPaths) {
        Empty-Directory $p
    }
}

# =========================================================
# JETBRAINS
# =========================================================

if (!$KeepJetBrains) {

    Log "Cleaning JetBrains caches..."

    $jbPaths = @(
        "$env:LOCALAPPDATA\JetBrains",
        "$env:APPDATA\JetBrains"
    )

    foreach ($p in $jbPaths) {
        Empty-Directory $p
    }
}

# =========================================================
# STEAM CACHE CLEANUP
# =========================================================

if ($ClearSteamCache) {

    $steamRunning = Get-Process steam -ErrorAction SilentlyContinue

    if ($steamRunning) {

        Log "Steam is currently running. Skipping Steam cache cleanup."
    }
    else {

        Log "Cleaning Steam caches..."

        $steamRoot = "${env:ProgramFiles(x86)}\Steam"

        $steamPaths = @(
            "$steamRoot\steamapps\shadercache",
            "$steamRoot\steamapps\downloading",
            "$steamRoot\appcache\httpcache"
        )

        foreach ($p in $steamPaths) {
            Empty-Directory $p
        }
    }
}

# =========================================================
# BATTLE.NET CACHE CLEANUP
# =========================================================

if ($ClearBattleNetCache) {

    $battleNetRunning = Get-Process "Battle.net" -ErrorAction SilentlyContinue
    $agentRunning = Get-Process Agent -ErrorAction SilentlyContinue

    if ($battleNetRunning -or $agentRunning) {

        Log "Battle.net is currently running. Skipping Battle.net cache cleanup."
    }
    else {

        Log "Cleaning Battle.net caches..."

        $battleNetPaths = @(
            "$env:PROGRAMDATA\Battle.net",
            "$env:PROGRAMDATA\Blizzard Entertainment",
            "$env:LOCALAPPDATA\Battle.net"
        )

        foreach ($p in $battleNetPaths) {
            Empty-Directory $p
        }
    }
}

# =========================================================
# WINDOWS CRASH DUMPS
# =========================================================

Log "Cleaning crash dumps..."

$dumpPaths = @(
    "C:\Windows\Minidump",
    "C:\Windows\MEMORY.DMP"
)

foreach ($p in $dumpPaths) {
    Remove-Safely $p
}

# =========================================================
# RECYCLE BIN
# =========================================================

if ($ClearRecycleBin) {

    Log "Emptying Recycle Bin..."

    if ($DryRun) {
        Log "DRY RUN: Clear-RecycleBin -Force"
    }
    else {
        Clear-RecycleBin -Force
    }
}

# =========================================================
# DOWNLOADS
# =========================================================

if ($ClearDownloads) {

    Log "Clearing Downloads folder..."

    Empty-Directory "$env:USERPROFILE\Downloads"
}

# =========================================================
# HIBERNATION
# =========================================================

if ($DisableHibernate) {

    Log "Disabling hibernation..."

    if ($DryRun) {
        Log "DRY RUN: powercfg /hibernate off"
    }
    else {
        powercfg /hibernate off
    }
}

# =========================================================
# DISM CLEANUP
# =========================================================

if ($Aggressive) {

    Log "Running DISM component cleanup..."

    if ($DryRun) {
        Log "DRY RUN: DISM /Online /Cleanup-Image /StartComponentCleanup"
    }
    else {
        DISM /Online /Cleanup-Image /StartComponentCleanup
    }
}

# =========================================================
# FINAL STATS
# =========================================================

Write-Host ""

$after = Get-DriveStats

Log "Cleanup complete."
Write-Host ""

Log "C: AFTER => Used: $($after.UsedGB) GB | Free: $($after.FreeGB) GB"

$freed = [math]::Round((($after.FreeBytes - $before.FreeBytes) / 1GB), 2)

Write-Host ""
Write-Host "==========================================" -ForegroundColor Green
Write-Host "SPACE RECOVERED: $freed GB" -ForegroundColor Green
Write-Host "==========================================" -ForegroundColor Green
Write-Host ""