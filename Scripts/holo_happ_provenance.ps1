function Get-HoloHAppSourceDigest {
    param(
        [Parameter(Mandatory)][string]$SourceRoot,
        [Parameter(Mandatory)][string]$SourcePathsFile
    )

    $sourcePaths = Get-Content -LiteralPath $SourcePathsFile |
        Where-Object { ![string]::IsNullOrWhiteSpace($_) }
    foreach ($relativePath in $sourcePaths) {
        if (!(Test-Path -LiteralPath (Join-Path $SourceRoot $relativePath))) {
            throw "Required Holochain hApp source path is missing: $relativePath"
        }
    }

    # Git object IDs describe the committed source bytes independently of checkout
    # path separators, filesystem collation, and line-ending conversion. Both the
    # Linux builder and Windows release validator therefore hash the same source.
    $trackedFiles = @(& git -C $SourceRoot ls-files -- @sourcePaths)
    if ($LASTEXITCODE -ne 0 -or $trackedFiles.Count -eq 0) {
        throw 'Unable to enumerate the tracked Holochain hApp provenance files.'
    }
    [Array]::Sort($trackedFiles, [StringComparer]::Ordinal)
    $lines = foreach ($relativePath in $trackedFiles) {
        $objectId = (& git -C $SourceRoot rev-parse "HEAD:$relativePath").Trim()
        if ($LASTEXITCODE -ne 0 -or [string]::IsNullOrWhiteSpace($objectId)) {
            throw "Unable to resolve the Git object for Holochain hApp source '$relativePath'."
        }
        "$relativePath=$objectId"
    }

    $bytes = [Text.Encoding]::UTF8.GetBytes(($lines -join "`n"))
    $sha = [Security.Cryptography.SHA256]::Create()
    try { return ([BitConverter]::ToString($sha.ComputeHash($bytes))).Replace('-', '') }
    finally { $sha.Dispose() }
}
