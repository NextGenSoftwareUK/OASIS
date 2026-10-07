#Requires -RunAsAdministrator
[CmdletBinding()]
param(
    [Parameter(ValueFromPipeline, ValueFromPipelineByPropertyName)]
    [string[]]$VhdxPath,

    [switch]$DryRun
)

$ErrorActionPreference = "Stop"

function Write-CompactionLog([string]$Message) {
    Write-Host "[compact-wsl-vhdx] $Message" -ForegroundColor Cyan
}

if (-not $VhdxPath) {
    $searchPatterns = @(
        (Join-Path $env:LOCALAPPDATA "Packages\CanonicalGroupLimited.*\LocalState\ext4.vhdx"),
        (Join-Path $env:LOCALAPPDATA "wsl\*\ext4.vhdx")
    )

    $VhdxPath = @(
        foreach ($pattern in $searchPatterns) {
            Get-Item -Path $pattern -ErrorAction SilentlyContinue | Select-Object -ExpandProperty FullName
        }
    ) | Sort-Object -Unique
}

if (-not $VhdxPath) {
    Write-CompactionLog "No WSL ext4.vhdx files were found; nothing to compact."
    return
}

$resolvedVhdxPaths = foreach ($path in $VhdxPath) {
    if (-not (Test-Path -LiteralPath $path -PathType Leaf)) {
        throw "WSL virtual disk does not exist: $path"
    }

    (Resolve-Path -LiteralPath $path).Path
}

if ($DryRun) {
    Write-CompactionLog "DRY RUN: wsl.exe --shutdown"
    foreach ($path in $resolvedVhdxPaths) {
        Write-CompactionLog "DRY RUN: Compact $path"
    }
    return
}

Write-CompactionLog "Shutting down WSL before accessing its virtual disks..."
& wsl.exe --shutdown
if ($LASTEXITCODE -ne 0) {
    throw "wsl.exe --shutdown failed with exit code $LASTEXITCODE."
}

foreach ($path in $resolvedVhdxPaths) {
    $sizeBefore = (Get-Item -LiteralPath $path).Length
    $diskPartScriptPath = Join-Path ([System.IO.Path]::GetTempPath()) ("compact-wsl-{0}.txt" -f [guid]::NewGuid().ToString("N"))

    try {
        @(
            "select vdisk file=`"$path`""
            "attach vdisk readonly"
            "compact vdisk"
            "detach vdisk"
            "exit"
        ) | Set-Content -LiteralPath $diskPartScriptPath -Encoding ASCII

        Write-CompactionLog "Compacting $path..."
        & diskpart.exe /s $diskPartScriptPath
        if ($LASTEXITCODE -ne 0) {
            throw "DiskPart failed to compact '$path' with exit code $LASTEXITCODE."
        }
    }
    finally {
        if (Test-Path -LiteralPath $diskPartScriptPath) {
            Remove-Item -LiteralPath $diskPartScriptPath -Force
        }
    }

    $sizeAfter = (Get-Item -LiteralPath $path).Length
    $reclaimedGB = [math]::Round(($sizeBefore - $sizeAfter) / 1GB, 2)
    Write-CompactionLog "Finished $path (reclaimed $reclaimedGB GB)."
}
