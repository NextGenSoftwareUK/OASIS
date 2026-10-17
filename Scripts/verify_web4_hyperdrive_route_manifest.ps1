<#
.SYNOPSIS
Verifies that every WEB4 controller action is present in the checked-in HyperDrive
route review manifest.
.DESCRIPTION
This is a source-level completeness gate, not a claim that placeholder HTTP requests
exercise business behavior. It records each HTTP action and classifies its storage
boundary. Use -UpdateManifest only after reviewing additions and then commit the CSV.
#>
[CmdletBinding()]
param(
    [switch]$UpdateManifest,
    [string]$ManifestPath = 'Docs/Devs/HYPERDRIVE_WEB4_ROUTE_MANIFEST.csv'
)

$ErrorActionPreference = 'Stop'
$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path
$controllerRoot = Join-Path $repoRoot 'ONODE/NextGenSoftware.OASIS.API.ONODE.WebAPI/Controllers'
$manifest = [IO.Path]::GetFullPath((Join-Path $repoRoot $ManifestPath))

function Normalize([string]$value) {
    return ([regex]::Replace($value, '\s+', ' ')).Trim()
}

function Get-Disposition([string]$body, [string]$methodName, [string]$controllerName) {
    if ($controllerName -in @('HyperDriveController', 'HyperDriveSyncController', 'ProviderController')) {
        return 'HyperDriveOrProviderControlPlane'
    }
    if ($body -match ('\breturn\s+(?:await\s+)?' + [regex]::Escape($methodName) + '\s*\(')) {
        return 'ControllerActionDelegation'
    }
    if ($body -match '\b(?:AvatarManager|HolonManager|SearchManager|WalletManager)\b') {
        return 'DirectModeAwareManagerBoundary'
    }
    if ($body -match '\bOASISAPI\.[A-Za-z_][A-Za-z0-9_]*\b') {
        return 'CoveredFeatureManagerDelegation'
    }
    if ($body -match '\b[A-Za-z_][A-Za-z0-9_]*Manager(?:Base)?\b') {
        return 'CoveredFeatureManagerDelegation'
    }
    if ($body -match '\b_[a-zA-Z][a-zA-Z0-9_]*(?:Service|Repository|Client)\b') {
        return 'InjectedServiceRepositoryOrClient'
    }
    if ($body -match '\b(?:ProviderManager|IOASIS\w*Provider|GetAndActivateProvider|OASISStorageProvider)\b') {
        return 'ExplicitProviderNativeOrAdministrative'
    }
    if ($body -match '\b(?:HttpClient|SendAsync|PostAsync|GetAsync|SmtpClient)\b') {
        return 'ExplicitExternalTransport'
    }
    return 'LocalOrNonStorageEndpoint'
}

$rows = foreach ($file in Get-ChildItem -LiteralPath $controllerRoot -Recurse -Filter '*.cs' | Sort-Object FullName) {
    $lines = @(Get-Content -LiteralPath $file.FullName)
    $relative = $file.FullName.Substring($repoRoot.Length + 1).Replace('\', '/')
    $className = ''
    for ($classIndex = 0; $classIndex -lt $lines.Count; $classIndex++) {
        if ($lines[$classIndex] -match '\bclass\s+(?<name>[A-Za-z0-9_]+Controller)\b') {
            $className = $Matches.name
            break
        }
    }
    if ([string]::IsNullOrWhiteSpace($className)) { continue }

    for ($index = 0; $index -lt $lines.Count; $index++) {
        if ($lines[$index] -notmatch '^\s*\[Http(?<verb>Get|Post|Put|Patch|Delete)(?:\((?<template>.*)\))?\]\s*(?://.*)?$') { continue }
        $verb = $Matches.verb.ToUpperInvariant()
        $template = if ($Matches.template) { Normalize ($Matches.template -replace '^\s*"|"\s*$', '') } else { '' }
        $attributeLine = $index + 1
        $cursor = $index + 1
        while ($cursor -lt $lines.Count -and ($lines[$cursor] -match '^\s*\[' -or [string]::IsNullOrWhiteSpace($lines[$cursor]))) { $cursor++ }
        $declaration = ''
        $declarationLine = $cursor + 1
        while ($cursor -lt $lines.Count -and $cursor -lt $index + 40) {
            $declaration += ' ' + $lines[$cursor].Trim()
            if ($declaration -match '\)\s*(?:\{|=>)?\s*$') { break }
            $cursor++
        }
        $declaration = Normalize $declaration
        if ($declaration -notmatch '\bpublic\s+.*?\b(?<method>[A-Za-z_][A-Za-z0-9_]*)\s*\(') {
            throw "Could not resolve action declaration after ${relative}:$attributeLine"
        }
        $methodName = $Matches.method
        $bodyStart = $cursor
        $next = $cursor + 1
        while ($next -lt $lines.Count -and $lines[$next] -notmatch '^\s*\[Http(?:Get|Post|Put|Patch|Delete)') { $next++ }
        $body = ($lines[$bodyStart..([Math]::Max($bodyStart, $next - 1))] -join "`n")
        $disposition = Get-Disposition $body $methodName $className
        [pscustomobject]@{
            SourceFile = $relative
            Line = $attributeLine
            Controller = $className
            Action = $methodName
            Verb = $verb
            Template = $template
            StorageDisposition = $disposition
        }
        $index = $cursor
    }
}

$rows = @($rows | Sort-Object SourceFile, Line, Verb, Template)
if ($UpdateManifest) {
    $rows | Export-Csv -LiteralPath $manifest -NoTypeInformation -Encoding utf8
    Write-Host "Updated $manifest with $($rows.Count) WEB4 controller actions."
    exit 0
}

if (!(Test-Path -LiteralPath $manifest -PathType Leaf)) {
    throw "WEB4 route manifest is missing: $manifest. Run with -UpdateManifest and review it."
}

$expected = @(Import-Csv -LiteralPath $manifest)
$key = { param($row) "$($row.SourceFile)|$($row.Controller)|$($row.Action)|$($row.Verb)|$($row.Template)" }
$actualKeys = @($rows | ForEach-Object { & $key $_ })
$expectedKeys = @($expected | ForEach-Object { & $key $_ })
$missing = @($actualKeys | Where-Object { $_ -notin $expectedKeys })
$stale = @($expectedKeys | Where-Object { $_ -notin $actualKeys })
$invalid = @($expected | Where-Object { [string]::IsNullOrWhiteSpace($_.StorageDisposition) })

if ($missing.Count -or $stale.Count -or $invalid.Count) {
    if ($missing.Count) { Write-Error "Unreviewed WEB4 actions:`n$($missing -join "`n")" }
    if ($stale.Count) { Write-Error "Stale WEB4 actions:`n$($stale -join "`n")" }
    if ($invalid.Count) { Write-Error "$($invalid.Count) actions have no storage disposition." }
    exit 1
}

$providerRoutes = @($expected | Where-Object StorageDisposition -ne 'LocalOrNonStorageEndpoint').Count
Write-Host "WEB4 HyperDrive route manifest verified: $($expected.Count) controller actions; $providerRoutes provider/external-manager actions intentionally classified."
