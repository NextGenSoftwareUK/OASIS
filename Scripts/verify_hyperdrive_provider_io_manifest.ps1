<#
.SYNOPSIS
Verifies that every public method in the Core Managers tree remains represented in
the checked-in HyperDrive provider-I/O review manifest.
.DESCRIPTION
Uses a deterministic multiline declaration scanner and records normalized signatures.
Run with -UpdateManifest after intentionally reviewing new manager methods, then
review the generated disposition before committing it.
#>
[CmdletBinding()]
param(
    [switch]$UpdateManifest,
    [string]$ManifestPath = 'Docs/Devs/HYPERDRIVE_V2_PUBLIC_MANAGER_METHOD_MANIFEST.csv'
)

$ErrorActionPreference = 'Stop'
$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path
$managerRoot = Join-Path $repoRoot 'OASIS Architecture/NextGenSoftware.OASIS.API.Core/Managers'
$manifest = [IO.Path]::GetFullPath((Join-Path $repoRoot $ManifestPath))

function Normalize([string]$Value) {
    return ([regex]::Replace($Value, '\s+', ' ')).Trim()
}

function Get-Disposition([string]$RelativePath, [string]$Body) {
    if ($RelativePath -match '/OASIS HyperDrive/Synchronization/') { return 'HostedTransactionalBoundary' }
    if ($RelativePath -match '/OASIS HyperDrive/Provider Management/') { return 'RuntimeControlPlane' }
    if ($RelativePath -match '/OASIS HyperDrive/OASISHyperDrive\.cs$') { return 'CentralV2Router' }
    if ($RelativePath -match '/OASIS HyperDrive/') { return 'LocalHyperDriveAnalytics' }
    if ($RelativePath -match '/(AvatarManager|HolonManager)/') { return 'ModeAwareLegacyAndV2Boundary' }
    if ($RelativePath -match '/EmailManager\.') { return 'ExplicitExternalTransport' }
    if ($Body -match '\b(?:_holonManager|HolonManager|_avatarManager|AvatarManager|_walletManager|WalletManager)\b') {
        return 'CoveredManagerDelegation'
    }
    if ($Body -match '\b(?:OASISHyperDrive|RouteRequest(?:Async)?|LoadBalanceRequest(?:Async)?)\b') {
        return 'CentralV2Delegation'
    }
    if ($Body -match '\b(?:IOASIS\w*Provider|OASISStorageProvider|CurrentStorageProvider|GetStorageProvider|ActivateProvider|ProviderManager)\b') {
        return 'ExplicitProviderNativeOrAdministrative'
    }
    return 'LocalOrchestration'
}

function Test-ProviderBacked([string]$Body, [string]$Disposition) {
    if ($Disposition -ne 'LocalOrchestration') { return $true }
    return $Body -match '\.(?:Load|Save|Delete|Remove|Search|Send|Mint|Burn|Transfer|Import|Export)[A-Z]\w*(?:Async)?\s*\('
}

$rows = foreach ($file in Get-ChildItem -LiteralPath $managerRoot -Recurse -Filter '*.cs' | Sort-Object FullName) {
    $lines = @(Get-Content -LiteralPath $file.FullName)
    $declarations = @()
    for ($index = 0; $index -lt $lines.Count; $index++) {
        if ($lines[$index] -notmatch '^\s*public\s+' -or
            $lines[$index] -match '^\s*public\s+(?:sealed\s+|abstract\s+|partial\s+)*(?:class|interface|enum|record|struct)\b') {
            continue
        }
        if ($lines[$index] -match '^\s*public\s+(?:static\s+)?(?:event|delegate)\b') { continue }
        $declaration = $lines[$index].Trim()
        $cursor = $index
        while ($declaration -notmatch '\(' -and
            $declaration -notmatch '[{;]|=>' -and
            $cursor + 1 -lt $lines.Count -and
            $cursor - $index -lt 30) {
            $cursor++
            $declaration += ' ' + $lines[$cursor].Trim()
        }
        if ($declaration -notmatch '\(' -or
            $declaration.IndexOfAny([char[]]@('{', ';')) -ge 0 -and
            $declaration.IndexOfAny([char[]]@('{', ';')) -lt $declaration.IndexOf('(')) { continue }
        while ($declaration -notmatch '\)' -and $cursor + 1 -lt $lines.Count -and $cursor - $index -lt 30) {
            $cursor++
            $declaration += ' ' + $lines[$cursor].Trim()
        }
        if ($declaration -notmatch '\)') { continue }
        $signatureText = $declaration.Substring(0, $declaration.LastIndexOf(')') + 1)
        if ($signatureText -match '\b(?:get|set|add|remove)\s*\(') { continue }
        $prefix = Normalize $signatureText.Substring(0, $signatureText.IndexOf('('))
        if ($prefix -match '[{;}]') { continue } # Property/event text reached before a later method.
        $prefix = $prefix -replace '^public\s+(?:(?:static|async|virtual|override|new|sealed|partial|unsafe|extern)\s+)*', ''
        if ($prefix -notmatch '\s') { continue } # Constructor, not a method.
        $declarations += [pscustomobject]@{ Index = $index; Line = $index + 1; Signature = Normalize $signatureText }
        $index = $cursor
    }

    for ($declarationIndex = 0; $declarationIndex -lt $declarations.Count; $declarationIndex++) {
        $declaration = $declarations[$declarationIndex]
        $endIndex = if ($declarationIndex + 1 -lt $declarations.Count) {
            $declarations[$declarationIndex + 1].Index - 1
        } else { $lines.Count - 1 }
        $body = ($lines[$declaration.Index..$endIndex] -join "`n")
        $relative = $file.FullName.Substring($repoRoot.Length + 1).Replace('\', '/')
        $line = $declaration.Line
        $signature = $declaration.Signature
        $disposition = Get-Disposition $relative $body
        [pscustomobject]@{
            SourceFile = $relative
            Line = $line
            Signature = $signature
            ProviderBacked = Test-ProviderBacked $body $disposition
            Disposition = $disposition
        }
    }
}

$rows = @($rows | Sort-Object SourceFile, Line, Signature)
if ($UpdateManifest) {
    $directory = Split-Path $manifest
    if (!(Test-Path -LiteralPath $directory -PathType Container)) {
        New-Item -ItemType Directory -Path $directory -Force | Out-Null
    }
    $rows | Export-Csv -LiteralPath $manifest -NoTypeInformation -Encoding utf8
    Write-Host "Updated $manifest with $($rows.Count) public manager methods."
    exit 0
}

if (!(Test-Path -LiteralPath $manifest -PathType Leaf)) {
    throw "Provider-I/O manifest is missing: $manifest. Run this script with -UpdateManifest and review it."
}

$expected = @(Import-Csv -LiteralPath $manifest)
$key = { param($row) "$($row.SourceFile)|$($row.Signature)" }
$actualKeys = @($rows | ForEach-Object { & $key $_ })
$expectedKeys = @($expected | ForEach-Object { & $key $_ })
$missing = @($actualKeys | Where-Object { $_ -notin $expectedKeys })
$stale = @($expectedKeys | Where-Object { $_ -notin $actualKeys })
$invalid = @($expected | Where-Object {
    $_.ProviderBacked -eq 'True' -and $_.Disposition -eq 'LocalOrchestration'
})

if ($missing.Count -or $stale.Count -or $invalid.Count) {
    if ($missing.Count) { Write-Error "Unreviewed public methods:`n$($missing -join "`n")" }
    if ($stale.Count) { Write-Error "Stale manifest methods:`n$($stale -join "`n")" }
    if ($invalid.Count) { Write-Error "Provider-backed methods lack an intentional disposition: $($invalid.Count)" }
    exit 1
}

$providerBackedCount = @($expected | Where-Object ProviderBacked -eq 'True').Count
Write-Host "HyperDrive provider-I/O manifest verified: $($expected.Count) public manager methods; $providerBackedCount provider-backed/delegating methods intentionally classified."
