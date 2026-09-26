[CmdletBinding()]
param(
    [Parameter(Mandatory)]
    [ValidateSet('OASISAPI_DEV', 'OASISAPI_LIVE')]
    [string]$Database,
    [string]$OutputRoot = 'artifacts\mongodb-backups',
    [string]$MongoDumpPath
)

$ErrorActionPreference = 'Stop'
$repoRoot = Split-Path $PSScriptRoot -Parent
$allowedRoot = [IO.Path]::GetFullPath((Join-Path $repoRoot 'artifacts')) + [IO.Path]::DirectorySeparatorChar
$resolvedOutputRoot = [IO.Path]::GetFullPath((Join-Path $repoRoot $OutputRoot))
if (!$resolvedOutputRoot.StartsWith($allowedRoot, [StringComparison]::OrdinalIgnoreCase)) {
    throw "MongoDB backups must remain under '$allowedRoot'."
}

if ([string]::IsNullOrWhiteSpace($MongoDumpPath)) {
    $installed = Get-Command mongodump -ErrorAction SilentlyContinue
    $MongoDumpPath = if ($installed) { $installed.Source } else {
        'C:\Program Files\MongoDB\Tools\100\bin\mongodump.exe'
    }
}
if (!(Test-Path -LiteralPath $MongoDumpPath -PathType Leaf)) {
    throw "mongodump was not found at '$MongoDumpPath'. Install MongoDB Database Tools or pass -MongoDumpPath."
}

$backupDirectory = Join-Path $resolvedOutputRoot "$Database-$(Get-Date -Format 'yyyyMMdd-HHmmss')"
New-Item -ItemType Directory -Path $backupDirectory -Force | Out-Null
$secureUri = Read-Host "Paste the MongoDB connection string for $Database" -AsSecureString
$pointer = [IntPtr]::Zero
try {
    $pointer = [Runtime.InteropServices.Marshal]::SecureStringToBSTR($secureUri)
    $mongoUri = [Runtime.InteropServices.Marshal]::PtrToStringBSTR($pointer)
    if ([string]::IsNullOrWhiteSpace($mongoUri)) { throw 'A MongoDB connection string is required.' }

    & $MongoDumpPath --uri $mongoUri --db $Database --gzip --out $backupDirectory
    if ($LASTEXITCODE -ne 0) { throw "mongodump failed with exit code $LASTEXITCODE." }

    $databaseDirectory = Join-Path $backupDirectory $Database
    if (!(Test-Path -LiteralPath $databaseDirectory -PathType Container) -or
        !(Get-ChildItem -LiteralPath $databaseDirectory -File -Filter '*.bson.gz' -ErrorAction SilentlyContinue)) {
        throw "mongodump returned success but no compressed BSON files were created under '$databaseDirectory'."
    }
    Write-Host "MongoDB backup completed: $backupDirectory"
}
catch {
    if (Test-Path -LiteralPath $backupDirectory -PathType Container) {
        Remove-Item -LiteralPath $backupDirectory -Recurse -Force
    }
    throw
}
finally {
    if ($pointer -ne [IntPtr]::Zero) { [Runtime.InteropServices.Marshal]::ZeroFreeBSTR($pointer) }
    Remove-Variable mongoUri, secureUri, pointer -ErrorAction SilentlyContinue
}
