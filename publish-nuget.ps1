<#
.SYNOPSIS
    Pack and publish all 91 OASIS NuGet packages to nuget.org.

.PARAMETER ApiKey
    Your nuget.org API key. Can also be set via the NUGET_API_KEY environment variable.

.PARAMETER SkipPush
    Pack only - do not push to nuget.org. Useful for testing the build.

.PARAMETER Filter
    Only process packages whose PackageId contains this string (e.g. "STAR", "WEB6").

.PARAMETER NupkgOutput
    Directory to write .nupkg files into. Defaults to .\nupkgs

.EXAMPLE
    .\publish-nuget.ps1 -ApiKey "your-api-key-here"

.EXAMPLE
    .\publish-nuget.ps1 -SkipPush

.EXAMPLE
    .\publish-nuget.ps1 -ApiKey "your-key" -Filter "STAR"

.EXAMPLE
    $env:NUGET_API_KEY = "your-key"
    .\publish-nuget.ps1
#>

param(
    [string]$ApiKey = $env:NUGET_API_KEY,
    [switch]$SkipPush,
    [string]$Filter = "",
    [string]$NupkgOutput = "$PSScriptRoot\nupkgs"
)

$ErrorActionPreference = 'Stop'
$root = $PSScriptRoot

# ---------------------------------------------------------------------------
# Validate
# ---------------------------------------------------------------------------
if (-not $SkipPush -and -not $ApiKey) {
    Write-Error "No NuGet API key supplied. Pass -ApiKey ""your-key"" or set `$env:NUGET_API_KEY before running. Get a key at: https://www.nuget.org/account/apikeys"
    exit 1
}

# ---------------------------------------------------------------------------
# Discover packages
# ---------------------------------------------------------------------------
$skipPatterns = @('Archived', 'External Libs', 'net-ipfs-http-client', 'ProviderNameOASIS')

$projects = Get-ChildItem -Recurse -Filter "*.csproj" $root | Where-Object {
    $path = $_.FullName
    $skip = $false
    foreach ($p in $skipPatterns) {
        if ($path -like "*$p*") { $skip = $true; break }
    }
    -not $skip
} | Where-Object {
    $c = [System.IO.File]::ReadAllText($_.FullName)
    $c -match '<PackageId>'
} | Sort-Object FullName

if ($Filter) {
    $projects = $projects | Where-Object {
        $c = [System.IO.File]::ReadAllText($_.FullName)
        if ($c -match '<PackageId>(.*?)</PackageId>') { $Matches[1] -like "*$Filter*" } else { $false }
    }
}

$total = ($projects | Measure-Object).Count

Write-Host ""
Write-Host "======================================================"
Write-Host "  OASIS NuGet Publisher"
Write-Host "  Packages found : $total"
Write-Host "  Output dir     : $NupkgOutput"
Write-Host "  Push to NuGet  : $(-not $SkipPush)"
if ($Filter) { Write-Host "  Filter         : $Filter" }
Write-Host "======================================================"
Write-Host ""

if ($total -eq 0) { Write-Host "No packages matched. Exiting."; exit 0 }

New-Item -ItemType Directory -Force $NupkgOutput | Out-Null

# ---------------------------------------------------------------------------
# Pack all packages
# ---------------------------------------------------------------------------
Write-Host "--- PHASE 1: dotnet pack ---"
Write-Host ""

$packFailed  = New-Object System.Collections.Generic.List[string]
$packSuccess = New-Object System.Collections.Generic.List[string]

$i = 0
foreach ($proj in $projects) {
    $i++
    $c = [System.IO.File]::ReadAllText($proj.FullName)
    if ($c -match '<PackageId>(.*?)</PackageId>') { $pkgId = $Matches[1] } else { $pkgId = $proj.BaseName }
    if ($c -match '<Version>([\d\.]+)</Version>')  { $pkgVer = $Matches[1] } else { $pkgVer = "?" }

    Write-Host "[$i/$total] Packing $pkgId $pkgVer ..."

    $packArgs = @(
        'pack', $proj.FullName,
        '--configuration', 'Release',
        '--output', $NupkgOutput,
        '--nologo'
    )

    $result = & dotnet @packArgs 2>&1
    if ($LASTEXITCODE -ne 0) {
        Write-Host "  [FAIL] $pkgId" -ForegroundColor Red
        Write-Host ($result | Out-String).Trim() -ForegroundColor DarkRed
        $packFailed.Add($pkgId)
    } else {
        Write-Host "  [OK]   $pkgId" -ForegroundColor Green
        $packSuccess.Add($pkgId)
    }
}

Write-Host ""
Write-Host "Pack complete: $($packSuccess.Count) succeeded, $($packFailed.Count) failed."

if ($packFailed.Count -gt 0) {
    Write-Host ""
    Write-Host "Failed packs:" -ForegroundColor Red
    foreach ($f in $packFailed) { Write-Host "  - $f" -ForegroundColor Red }
}

# ---------------------------------------------------------------------------
# Push to nuget.org
# ---------------------------------------------------------------------------
if ($SkipPush) {
    Write-Host ""
    Write-Host "-SkipPush specified - stopping before push."
    Write-Host "Packages are in: $NupkgOutput"
    if ($packFailed.Count -gt 0) { exit 1 } else { exit 0 }
}

Write-Host ""
Write-Host "--- PHASE 2: dotnet nuget push ---"
Write-Host ""

$nupkgs = Get-ChildItem $NupkgOutput -Filter "*.nupkg" | Where-Object { $_.Name -notmatch '\.symbols\.nupkg$' }

$pushFailed  = New-Object System.Collections.Generic.List[string]
$pushSuccess = New-Object System.Collections.Generic.List[string]
$pushSkipped = New-Object System.Collections.Generic.List[string]

$j = 0
$nupkgTotal = ($nupkgs | Measure-Object).Count
foreach ($pkg in $nupkgs) {
    $j++
    Write-Host "[$j/$nupkgTotal] Pushing $($pkg.Name) ..."

    $pushArgs = @(
        'nuget', 'push', $pkg.FullName,
        '--api-key', $ApiKey,
        '--source', 'https://api.nuget.org/v3/index.json',
        '--skip-duplicate',
        '--no-symbols'
    )

    $result = & dotnet @pushArgs 2>&1
    $output = ($result | Out-String).Trim()

    if ($LASTEXITCODE -ne 0) {
        Write-Host "  [FAIL] $($pkg.Name)" -ForegroundColor Red
        Write-Host $output -ForegroundColor DarkRed
        $pushFailed.Add($pkg.Name)
    } elseif ($output -match 'already exists') {
        Write-Host "  [SKIP] $($pkg.Name) - already on nuget.org" -ForegroundColor Yellow
        $pushSkipped.Add($pkg.Name)
    } else {
        Write-Host "  [OK]   $($pkg.Name)" -ForegroundColor Green
        $pushSuccess.Add($pkg.Name)
    }
}

# ---------------------------------------------------------------------------
# Summary
# ---------------------------------------------------------------------------
Write-Host ""
Write-Host "======================================================"
Write-Host "  DONE"
Write-Host "  Packed   : $($packSuccess.Count) / $total"
Write-Host "  Pushed   : $($pushSuccess.Count)"
Write-Host "  Skipped  : $($pushSkipped.Count) (already published)"
Write-Host "  Failed   : $($packFailed.Count + $pushFailed.Count)"
Write-Host "======================================================"

if ($packFailed.Count -gt 0) {
    Write-Host ""
    Write-Host "Pack failures:" -ForegroundColor Red
    foreach ($f in $packFailed) { Write-Host "  - $f" -ForegroundColor Red }
}
if ($pushFailed.Count -gt 0) {
    Write-Host ""
    Write-Host "Push failures:" -ForegroundColor Red
    foreach ($f in $pushFailed) { Write-Host "  - $f" -ForegroundColor Red }
}

$totalFailed = $packFailed.Count + $pushFailed.Count
if ($totalFailed -gt 0) { exit 1 } else { exit 0 }
