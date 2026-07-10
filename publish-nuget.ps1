
<#
.SYNOPSIS
    Pack and publish all 91 OASIS NuGet packages to nuget.org.

.PARAMETER ApiKey
    Your nuget.org API key. Can also be set via the NUGET_API_KEY environment variable.

.PARAMETER SkipPush
    Pack only — do not push to nuget.org. Useful for testing the build.

.PARAMETER Filter
    Only process packages whose PackageId contains this string (e.g. "STAR", "WEB6").

.PARAMETER NupkgOutput
    Directory to write .nupkg files into. Defaults to .\nupkgs

.EXAMPLE
    # Pack and push everything
    .\publish-nuget.ps1 -ApiKey "your-api-key-here"

.EXAMPLE
    # Pack only (no push), inspect output in .\nupkgs
    .\publish-nuget.ps1 -SkipPush

.EXAMPLE
    # Push only STAR packages
    .\publish-nuget.ps1 -ApiKey "your-key" -Filter "STAR"

.EXAMPLE
    # Use env var for key
    $env:NUGET_API_KEY = "your-key"
    .\publish-nuget.ps1
#>

[CmdletBinding()]
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
    Write-Error @"
No NuGet API key supplied.
Pass -ApiKey "your-key"  OR  set `$env:NUGET_API_KEY before running.
Get a key at: https://www.nuget.org/account/apikeys
"@
    exit 1
}

# ---------------------------------------------------------------------------
# Discover packages
# ---------------------------------------------------------------------------
$skipPatterns = @('Archived', 'External Libs', 'net-ipfs-http-client', 'ProviderNameOASIS')

$projects = Get-ChildItem -Recurse -Filter "*.csproj" $root | Where-Object {
    $path = $_.FullName
    $skip = $false
    foreach ($p in $skipPatterns) { if ($path -like "*$p*") { $skip = $true; break } }
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

$total = $projects.Count
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

$packFailed  = [System.Collections.Generic.List[string]]::new()
$packSuccess = [System.Collections.Generic.List[string]]::new()

$i = 0
foreach ($proj in $projects) {
    $i++
    $c = [System.IO.File]::ReadAllText($proj.FullName)
    $pkgId  = if ($c -match '<PackageId>(.*?)</PackageId>')   { $Matches[1] } else { $proj.BaseName }
    $pkgVer = if ($c -match '<Version>([\d\.]+)</Version>')   { $Matches[1] } else { "?" }

    Write-Host "[$i/$total] Packing $pkgId $pkgVer ..."

    $args = @(
        'pack', $proj.FullName,
        '--configuration', 'Release',
        '--output', $NupkgOutput,
        '--no-build',       # remove this line if you want a full rebuild each time
        '/p:NoBuild=false', # ensures Release assets are current
        '--nologo'
    )

    # Actually build + pack in one step for reliability
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
    $packFailed | ForEach-Object { Write-Host "  - $_" -ForegroundColor Red }
}

# ---------------------------------------------------------------------------
# Push to nuget.org
# ---------------------------------------------------------------------------
if ($SkipPush) {
    Write-Host ""
    Write-Host "-SkipPush specified — stopping before push."
    Write-Host "Packages are in: $NupkgOutput"
    exit ($packFailed.Count -gt 0 ? 1 : 0)
}

Write-Host ""
Write-Host "--- PHASE 2: dotnet nuget push ---"
Write-Host ""

$nupkgs = Get-ChildItem $NupkgOutput -Filter "*.nupkg" | Where-Object { $_.Name -notmatch '\.symbols\.nupkg$' }

$pushFailed  = [System.Collections.Generic.List[string]]::new()
$pushSuccess = [System.Collections.Generic.List[string]]::new()
$pushSkipped = [System.Collections.Generic.List[string]]::new()

$j = 0
foreach ($pkg in $nupkgs) {
    $j++
    Write-Host "[$j/$($nupkgs.Count)] Pushing $($pkg.Name) ..."

    $pushArgs = @(
        'nuget', 'push', $pkg.FullName,
        '--api-key', $ApiKey,
        '--source', 'https://api.nuget.org/v3/index.json',
        '--skip-duplicate',   # don't fail if version already exists
        '--no-symbols'
    )

    $result = & dotnet @pushArgs 2>&1
    $output = ($result | Out-String).Trim()

    if ($LASTEXITCODE -ne 0) {
        Write-Host "  [FAIL] $($pkg.Name)" -ForegroundColor Red
        Write-Host $output -ForegroundColor DarkRed
        $pushFailed.Add($pkg.Name)
    } elseif ($output -match 'already exists') {
        Write-Host "  [SKIP] $($pkg.Name) — already on nuget.org" -ForegroundColor Yellow
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
    $packFailed | ForEach-Object { Write-Host "  - $_" -ForegroundColor Red }
}
if ($pushFailed.Count -gt 0) {
    Write-Host ""
    Write-Host "Push failures:" -ForegroundColor Red
    $pushFailed | ForEach-Object { Write-Host "  - $_" -ForegroundColor Red }
}

exit (($packFailed.Count + $pushFailed.Count) -gt 0 ? 1 : 0)
