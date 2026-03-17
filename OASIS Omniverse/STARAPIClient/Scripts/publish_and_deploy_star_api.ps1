# Build and deploy STARAPIClient for Windows. For Linux/macOS use build-and-deploy-star-api-unix.sh.
param(
    [string]$Runtime = "win-x64",  # Windows: win-x64. Linux/macOS: use Scripts/build-and-deploy-star-api-unix.sh
    [switch]$RunSmokeTest,
    [switch]$ForceBuild
)

$ErrorActionPreference = "Stop"
Set-StrictMode -Version Latest

function Copy-FileWithRetry {
    param([string]$Source, [string]$Destination, [int]$MaxAttempts = 5, [int]$DelayMs = 500)
    $attempt = 0
    while ($true) {
        try {
            Copy-Item -LiteralPath $Source -Destination $Destination -Force -ErrorAction Stop
            return
        }
        catch [System.IO.IOException] {
            $attempt++
            if ($attempt -ge $MaxAttempts) { throw }
            Write-Host "  File in use, retrying in $DelayMs ms (attempt $attempt/$MaxAttempts)..."
            Start-Sleep -Milliseconds $DelayMs
        }
    }
}

$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$projectDir = Join-Path $scriptDir ".."
$projectPath = Join-Path $projectDir "STARAPIClient.csproj"
# dotnet publish can output to bin/Release/... or bin/x64/Release/... depending on platform/config
$publishDirDefault = Join-Path $projectDir "bin/Release/net9.0/$Runtime/publish"
$publishDirX64 = Join-Path $projectDir "bin/x64/Release/net9.0/$Runtime/publish"
$nativeDir = Join-Path $projectDir "bin/Release/net9.0/$Runtime/native"
$headerPath = Join-Path $projectDir "star_api.h"
$dllPathDefault = Join-Path $publishDirDefault "star_api.dll"
$dllPathX64 = Join-Path $publishDirX64 "star_api.dll"
$libPath = Join-Path $nativeDir "star_api.lib"

# Build only when: forced, or dll missing, or any project source (.cs / .csproj) or star_api.h is newer than star_api.dll
$needBuild = $ForceBuild
$dllPath = if (Test-Path $dllPathDefault) { $dllPathDefault } elseif (Test-Path $dllPathX64) { $dllPathX64 } else { $dllPathDefault }
if (!$needBuild -and (Test-Path $dllPath)) {
    $dllTime = (Get-Item $dllPath).LastWriteTimeUtc
    $sources = @(Get-ChildItem -Path $projectDir -Filter "*.cs" -Recurse -File -ErrorAction SilentlyContinue) +
               @(Get-ChildItem -Path $projectDir -Filter "*.csproj" -Recurse -File -ErrorAction SilentlyContinue)
    if (Test-Path $headerPath) { $sources += Get-Item $headerPath }
    foreach ($f in $sources) {
        if ($f.LastWriteTimeUtc -gt $dllTime) {
            $needBuild = $true
            break
        }
    }
}
if (!$needBuild -and (Test-Path $dllPath)) {
    Write-Host "STARAPIClient unchanged (star_api.dll is up to date), skipping build."
} else {
    if (!(Test-Path $dllPath)) { Write-Host "STARAPIClient not built yet or output missing; building..." }
    # Build Contracts first so STARAPIClient can Reference the DLL when PublishAot=true (avoids NETSDK1207 from netstandard2.1 in graph).
    $contractsPath = Join-Path $projectDir "..\..\OASIS Architecture\NextGenSoftware.OASIS.API.Contracts\NextGenSoftware.OASIS.API.Contracts.csproj"
    $contractsPath = [System.IO.Path]::GetFullPath($contractsPath)
    if (Test-Path $contractsPath) {
        Write-Host "Building OASIS.API.Contracts (required for NativeAOT publish)..."
        dotnet build $contractsPath -c Release --verbosity quiet
        if ($LASTEXITCODE -ne 0) { throw "Contracts build failed." }
    }
    Write-Host "Publishing NativeAOT WEB5 STAR API wrapper..."
    dotnet publish $projectPath -c Release -r $Runtime -p:PublishAot=true -p:SelfContained=true -p:NoWarn=NU1605
}

# Resolve DLL: prefer publish; if copy to publish failed (file in use), use native folder so we can still generate .lib and deploy
$nativeDllDefault = Join-Path $projectDir "bin/Release/net9.0/$Runtime/native/star_api.dll"
$nativeDllX64 = Join-Path $projectDir "bin/x64/Release/net9.0/$Runtime/native/star_api.dll"
$dllPath = $null
foreach ($candidate in @($dllPathDefault, $dllPathX64, $nativeDllX64, $nativeDllDefault)) {
    if (Test-Path $candidate) { $dllPath = $candidate; break }
}
if (!(Test-Path $dllPath)) { throw "Missing star_api.dll (checked publish and native under bin/Release and bin/x64/Release)." }
if ($dllPath -eq $nativeDllX64 -or $dllPath -eq $nativeDllDefault) {
    Write-Host "Using star_api.dll from native folder (publish copy may have been locked by another process)."
}
if (!(Test-Path $headerPath)) { throw "Missing header: $headerPath" }
$publishDir = Split-Path -Parent $dllPath

# Keep star_api.def in sync with C# UnmanagedCallersOnly so new exports never cause LNK2001 when building DOOM/Quake.
$generateDefScript = Join-Path $projectDir "Scripts\generate_star_api_def.ps1"
if (Test-Path $generateDefScript) {
    & $generateDefScript -ProjectDir $projectDir | Out-Null
}

# Generate star_api.lib from star_api.def (now auto-generated above) so .lib has all symbols regardless of trim. Else use dumpbin on the DLL.
$nativeDirForLib = if ($dllPath -eq $dllPathX64 -or $dllPath -eq $nativeDllX64) { Join-Path $projectDir "bin/x64/Release/net9.0/$Runtime/native" } else { $nativeDir }
$libPath = Join-Path $nativeDirForLib "star_api.lib"
if (Test-Path $libPath) { Remove-Item $libPath -Force -ErrorAction SilentlyContinue }
$libGenerated = $false
$staticDefPath = Join-Path $projectDir "star_api.def"
$dumpbinCmd = $null
$libCmd = $null
# 1) Prefer tools already in PATH (e.g. after BUILD_AND_DEPLOY_STAR_CLIENT.bat ran vcvars64)
$dumpbinCmd = (Get-Command dumpbin.exe -ErrorAction SilentlyContinue).Source
$libCmd = (Get-Command lib.exe -ErrorAction SilentlyContinue).Source
# If we have dumpbin but not lib, assume lib is in the same directory (standard VS layout)
if ($dumpbinCmd -and -not $libCmd) {
    $libNextToDumpbin = Join-Path (Split-Path -Parent $dumpbinCmd) "lib.exe"
    if (Test-Path $libNextToDumpbin) { $libCmd = $libNextToDumpbin }
}
# 2) Else find via vswhere (no -requires so Build Tools or any VS with C++ works). Trim all outputs (vswhere can return trailing newlines).
if ((-not $dumpbinCmd -or -not $libCmd) -and (Test-Path "${env:ProgramFiles(x86)}\Microsoft Visual Studio\Installer\vswhere.exe")) {
    $vswhere = "${env:ProgramFiles(x86)}\Microsoft Visual Studio\Installer\vswhere.exe"
    if (-not $dumpbinCmd) {
        $raw = (& $vswhere -latest -find "VC\Tools\MSVC\*\bin\Hostx64\x64\dumpbin.exe" 2>$null | Select-Object -First 1)
        $vcDumpbin = $null; if ($raw) { $t = "$raw".Trim(); if ($t.Length -gt 0) { try { $vcDumpbin = [System.IO.Path]::GetFullPath($t) } catch { $vcDumpbin = $t } } }
        if ($vcDumpbin -and (Test-Path -LiteralPath $vcDumpbin)) {
            $dumpbinCmd = $vcDumpbin
            $libNext = Join-Path (Split-Path -Parent $vcDumpbin) "lib.exe"
            if (-not $libCmd -and (Test-Path -LiteralPath $libNext)) { $libCmd = $libNext }
        }
    }
    if (-not $libCmd) {
        $raw = (& $vswhere -latest -find "VC\Tools\MSVC\*\bin\Hostx64\x64\lib.exe" 2>$null | Select-Object -First 1)
        $libPathVs = $null; if ($raw) { $t = "$raw".Trim(); if ($t.Length -gt 0) { try { $libPathVs = [System.IO.Path]::GetFullPath($t) } catch { $libPathVs = $t } } }
        if ($libPathVs -and (Test-Path -LiteralPath $libPathVs)) { $libCmd = $libPathVs }
    }
    # Fallback: installationPath then scan MSVC version folders for bin\Hostx64\x64. Prefer VSINSTALL if set by the batch (vcvars64 caller).
    if ((-not $dumpbinCmd -or -not $libCmd) -and (Test-Path $vswhere)) {
        $installPath = $env:VSINSTALL
        if (-not $installPath -or -not (Test-Path -LiteralPath $installPath)) {
            $installPath = (& $vswhere -latest -property installationPath 2>$null | Select-Object -First 1)
        }
        if ($installPath) { $installPath = "$installPath".Trim() }
        if ($installPath -and (Test-Path -LiteralPath $installPath)) {
            $vcTools = Join-Path $installPath "VC\Tools\MSVC"
            if (Test-Path -LiteralPath $vcTools) {
                $msvcDirs = Get-ChildItem -Path $vcTools -Directory -ErrorAction SilentlyContinue
                foreach ($ver in $msvcDirs) {
                    $x64bin = Join-Path $ver.FullName "bin\Hostx64\x64"
                    $d = Join-Path $x64bin "dumpbin.exe"
                    $l = Join-Path $x64bin "lib.exe"
                    if ((Test-Path -LiteralPath $d) -and (Test-Path -LiteralPath $l)) {
                        if (-not $dumpbinCmd) { $dumpbinCmd = $d }
                        if (-not $libCmd) { $libCmd = $l }
                        $env:PATH = "$x64bin;$env:PATH"
                        break
                    }
                }
            }
            # Last resort: recursive search under VC for Hostx64\x64 (some layouts differ)
            if ((-not $dumpbinCmd -or -not $libCmd) -and (Test-Path -LiteralPath (Join-Path $installPath "VC"))) {
                $dumpbins = Get-ChildItem -Path (Join-Path $installPath "VC") -Recurse -Filter "dumpbin.exe" -ErrorAction SilentlyContinue | Where-Object { $_.DirectoryName -match "Hostx64[\\/]x64" } | Select-Object -First 1
                if ($dumpbins) {
                    $vcBin = $dumpbins.DirectoryName.Trim()
                    if (-not $dumpbinCmd) { $dumpbinCmd = Join-Path $vcBin "dumpbin.exe" }
                    $libExePath = Join-Path $vcBin "lib.exe"
                    if (-not $libCmd -and (Test-Path -LiteralPath $libExePath)) { $libCmd = $libExePath }
                    if ($dumpbinCmd -and $libCmd) { $env:PATH = "$vcBin;$env:PATH" }
                }
            }
        }
    }
}
# 3) Final fallback: scan standard VS roots (works without vswhere, e.g. Build Tools)
if ((-not $dumpbinCmd -or -not $libCmd)) {
    foreach ($vsRoot in @("${env:ProgramFiles}\Microsoft Visual Studio", "${env:ProgramFiles(x86)}\Microsoft Visual Studio")) {
    if (-not (Test-Path $vsRoot)) { continue }
    $yearDirs = Get-ChildItem -Path $vsRoot -Directory -ErrorAction SilentlyContinue
    foreach ($yd in $yearDirs) {
        $vcTools = Join-Path $yd.FullName "VC\Tools\MSVC"
        if (-not (Test-Path $vcTools)) { continue }
        $msvcDirs = Get-ChildItem -Path $vcTools -Directory -ErrorAction SilentlyContinue
        foreach ($ver in $msvcDirs) {
            $x64bin = Join-Path $ver.FullName "bin\Hostx64\x64"
            $d = Join-Path $x64bin "dumpbin.exe"
            $l = Join-Path $x64bin "lib.exe"
            if ((Test-Path $d) -and (Test-Path $l)) {
                if (-not $dumpbinCmd) { $dumpbinCmd = $d }
                if (-not $libCmd) { $libCmd = $l }
                $env:PATH = "$x64bin;$env:PATH"
                break
            }
        }
        if ($dumpbinCmd -and $libCmd) { break }
    }
    if ($dumpbinCmd -and $libCmd) { break }
}
}
# Prefer static .def (source of truth from star_api.h) so .lib always has all symbols; no dependency on what the trimmed DLL exports.
if ((Test-Path $staticDefPath) -and $libCmd) {
    $libDir = Split-Path -Parent $libPath
    if (!(Test-Path $libDir)) { New-Item -ItemType Directory -Path $libDir -Force | Out-Null }
    & $libCmd /NOLOGO "/DEF:$staticDefPath" "/OUT:$libPath" /MACHINE:X64 2>&1 | Out-Null
    if (Test-Path $libPath) {
        $libGenerated = $true
        $exportCount = (Get-Content $staticDefPath | Where-Object { $_ -match '^\s*[a-zA-Z_][a-zA-Z0-9_]*\s*$' }).Count
        Write-Host "Generated star_api.lib from star_api.def ($exportCount exports)."
    }
}
if (-not $libGenerated -and $dumpbinCmd -and $libCmd) {
    $vcBin = Split-Path -Parent $dumpbinCmd
    if ($env:PATH -notlike "*$vcBin*") { $env:PATH = "$vcBin;$env:PATH" }
    # Quote paths so spaces (e.g. "OASIS Omniverse") don't break the command
    $dumpbinOut = & $dumpbinCmd /EXPORTS "$dllPath" 2>&1 | Out-String
    # Collect from ALL parsers and merge so we don't miss any export (e.g. different line formats in dumpbin output).
    $allSymbols = [System.Collections.Generic.HashSet[string]]::new([StringComparer]::OrdinalIgnoreCase)
    # Parser 1: strict regex (ordinal hint RVA symbol)
    foreach ($m in [regex]::Matches($dumpbinOut, '^\s+\d+\s+\d+\s+[0-9A-Fa-f]+\s+(\S+)\s*$', [System.Text.RegularExpressions.RegexOptions]::Multiline)) {
        $s = $m.Groups[1].Value.Trim(); if ($s -and $s -notmatch '^\d+$') { [void]$allSymbols.Add($s) }
    }
    # Parser 2: 4th column as symbol when name starts with letter/underscore
    foreach ($m in [regex]::Matches($dumpbinOut, '^\s*\d+\s+\d+\s+[0-9A-Fa-f]+\s+([A-Za-z_][A-Za-z0-9_]*)\s*$', [System.Text.RegularExpressions.RegexOptions]::Multiline)) {
        $s = $m.Groups[1].Value.Trim(); if ($s) { [void]$allSymbols.Add($s) }
    }
    # Parser 3: split by whitespace, take 4th token when line looks like ordinal hint hex name
    foreach ($line in ($dumpbinOut -split "`n")) {
        $parts = $line.Trim() -split '\s+'
        if ($parts.Count -ge 4 -and $parts[0] -match '^\d+$' -and $parts[1] -match '^\d+$' -and $parts[2] -match '^[0-9A-Fa-f]+$' -and $parts[3] -match '^[A-Za-z_]') { [void]$allSymbols.Add($parts[3]) }
    }
    # Use sorted order so .lib is deterministic; exclude pure numeric names
    $exportNames = @($allSymbols | Where-Object { $_ -and $_ -notmatch '^\d+$' } | Sort-Object)
    if (-not $exportNames -or $exportNames.Count -eq 0) {
        Write-Host "dumpbin /EXPORTS returned no parseable symbols. First 500 chars of output:"
        $len = if ($dumpbinOut) { $dumpbinOut.Length } else { 0 }
        if ($len -gt 0) { Write-Host $dumpbinOut.Substring(0, [Math]::Min(500, $len)) } else { Write-Host "(no output)" }
    }
    if ($exportNames -and $exportNames.Count -gt 0) {
        $defPath = Join-Path $publishDir "star_api.def"
        $defContent = "EXPORTS`r`n" + ($exportNames -join "`r`n")
        Set-Content -Path $defPath -Value $defContent -NoNewline -Encoding ASCII
        $libDir = Split-Path -Parent $libPath
        if (!(Test-Path $libDir)) { New-Item -ItemType Directory -Path $libDir -Force | Out-Null }
        # Quote paths so spaces (e.g. in "OASIS Omniverse") don't break lib.exe
        & $libCmd /NOLOGO "/DEF:$defPath" "/OUT:$libPath" /MACHINE:X64 2>&1 | Out-Null
        if (Test-Path $libPath) {
            $libGenerated = $true
            Write-Host "Generated star_api.lib from DLL exports ($($exportNames.Count) symbols)."
        }
    }
}
if (!(Test-Path $libPath)) {
    Write-Host "STAR .lib generation failed. Diagnostics:"
    Write-Host "  dumpbin: $(if ($dumpbinCmd) { $dumpbinCmd } else { 'not found' })"
    Write-Host "  lib:    $(if ($libCmd) { $libCmd } else { 'not found' })"
    $vswherePath = "${env:ProgramFiles(x86)}\Microsoft Visual Studio\Installer\vswhere.exe"
    Write-Host "  vswhere: $(if (Test-Path $vswherePath) { 'present' } else { 'not found' })"
    if (Test-Path $vswherePath) {
        $ip = (& $vswherePath -latest -property installationPath 2>$null | Select-Object -First 1)
        if ($ip) { $ip = "$ip".Trim(); $preview = if ($ip.Length -gt 80) { $ip.Substring(0, 80) + "..." } else { $ip }; Write-Host "  VS installPath: $preview" }
    }
    throw "Missing star_api.lib. To generate it, install Visual Studio or 'Build Tools for Visual Studio' with the 'Desktop development with C++' workload (includes dumpbin and lib). Then re-run this script, or run it from 'Developer Command Prompt for VS' so PATH is set. If you already have VS/Build Tools, close any process that might be locking star_api.dll and re-run."
}

$targets = @(
    (Join-Path $scriptDir "..\..\ODOOM"),
    (Join-Path $scriptDir "..\..\OQuake"),
    (Join-Path $scriptDir "..\..\OQuake\Code"),
    (Join-Path $scriptDir "..\..\OQuake\build"),  # So OQUAKE.exe has the same DLL we just built
    "C:\Source\UZDoom\src",
    "C:\Source\vkQuake\Quake"
)

foreach ($target in $targets)
{
    $resolvedTarget = [System.IO.Path]::GetFullPath($target)
    if (Test-Path $resolvedTarget)
    {
        Write-Host "Deploying WEB5 STAR API wrapper artifacts to $resolvedTarget"
        Copy-FileWithRetry $dllPath (Join-Path $resolvedTarget "star_api.dll")
        Copy-FileWithRetry $libPath (Join-Path $resolvedTarget "star_api.lib")
        Copy-FileWithRetry $headerPath (Join-Path $resolvedTarget "star_api.h")
    }
}

if ($RunSmokeTest)
{
    $smokeSource = Join-Path $projectDir "TestProjects\smoke_test.c"
    $smokeExe = Join-Path $projectDir "TestProjects\smoke_test.exe"

    Write-Host "Compiling smoke test..."
    Push-Location $projectDir
    try
    {
        if (Get-Command cl.exe -ErrorAction SilentlyContinue)
        {
            & cl.exe /nologo /W3 /I"$projectDir" "$smokeSource" /link /LIBPATH:"$nativeDir" star_api.lib /OUT:"$smokeExe"
        }
        elseif (Get-Command gcc.exe -ErrorAction SilentlyContinue)
        {
            & gcc.exe "$smokeSource" -I"$projectDir" "$libPath" -o "$smokeExe"
        }
        else
        {
            Write-Warning "No C compiler found (cl.exe/gcc.exe). Skipping smoke test compile."
            return
        }
    }
    finally
    {
        Pop-Location
    }

    if (Test-Path $smokeExe)
    {
        Write-Host "Running smoke test..."
        & $smokeExe
    }
    else
    {
        Write-Warning "Smoke test executable was not produced."
    }
}

Write-Host "WEB5 STAR API wrapper publish/deploy complete."

