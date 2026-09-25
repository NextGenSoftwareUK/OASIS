[CmdletBinding(SupportsShouldProcess, ConfirmImpact = 'Medium')]
param(
    [Parameter(Mandatory)]
    [ValidateSet('Legacy', 'OASISHyperDrive2')]
    [string]$Mode,

    [string]$Path = 'OASIS Architecture/NextGenSoftware.OASIS.API.DNA/Default/OASIS_DNA.json',

    [switch]$NoBackup
)

$ErrorActionPreference = 'Stop'
$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path
$resolvedPath = if ([IO.Path]::IsPathRooted($Path)) { [IO.Path]::GetFullPath($Path) }
    else { [IO.Path]::GetFullPath((Join-Path $repoRoot $Path)) }

if (!(Test-Path -LiteralPath $resolvedPath -PathType Leaf)) {
    throw "OASIS DNA file was not found: $resolvedPath"
}

$jsonText = Get-Content -LiteralPath $resolvedPath -Raw
try { $null = $jsonText | ConvertFrom-Json }
catch { throw "OASIS DNA is not valid JSON: $resolvedPath. $($_.Exception.Message)" }

$pattern = '(?m)("HyperDriveMode"\s*:\s*)"[^"]*"'
$matches = [regex]::Matches($jsonText, $pattern)
if ($matches.Count -ne 1) {
    throw "Expected exactly one HyperDriveMode property in '$resolvedPath', found $($matches.Count)."
}

$currentMode = [regex]::Match($matches[0].Value, '"([^"]*)"\s*$').Groups[1].Value
if ($currentMode -eq $Mode) {
    Write-Host "HyperDriveMode is already '$Mode' in $resolvedPath"
    return
}

if ($PSCmdlet.ShouldProcess($resolvedPath, "Change HyperDriveMode from '$currentMode' to '$Mode'")) {
    if (!$NoBackup) {
        $backupPath = "$resolvedPath.hyperdrive-$currentMode.bak"
        Copy-Item -LiteralPath $resolvedPath -Destination $backupPath -Force
        Write-Host "Backup: $backupPath"
    }

    $updated = [regex]::Replace($jsonText, $pattern, "`$1`"$Mode`"")
    [IO.File]::WriteAllText($resolvedPath, $updated, [Text.UTF8Encoding]::new($false))

    $verified = Get-Content -LiteralPath $resolvedPath -Raw | ConvertFrom-Json
    if ($verified.OASIS.HyperDriveMode -ne $Mode) {
        throw "HyperDriveMode verification failed after writing '$resolvedPath'."
    }
    Write-Host "HyperDriveMode changed from '$currentMode' to '$Mode' in $resolvedPath"
}
