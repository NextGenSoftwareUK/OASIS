<#
.SYNOPSIS
Seeds every Our World development GeoNFT, GeoHotSpot and quest fixture in dependency order.
.DESCRIPTION
Runs the canonical resumable seeders for the five Anorak GeoNFTs, the complete
GeoNFT/quest test matrix, the complete GeoHotSpot quest matrix, and the talking-tree
quest giver. Existing tagged records are reconciled in place rather than duplicated.
#>
[CmdletBinding()]
param(
    [string]$Web4BaseUrl = 'https://dev.api.web4.oasisomniverse.one',
    [string]$Web5BaseUrl = 'https://dev.api.starnet.oasisomniverse.one',
    [string]$CredentialPath = (Join-Path $env:LOCALAPPDATA 'OASIS/our-world-geonft-seed.credential.clixml'),
    [double]$Latitude = 31.54998,
    [double]$Longitude = 74.27728,
    [switch]$PlanOnly
)
$ErrorActionPreference = 'Stop'
$scripts = $PSScriptRoot
$anorakManifest = Join-Path ([IO.Path]::GetTempPath()) 'our-world-geonft-demo.json'
$matrixManifest = Join-Path $env:LOCALAPPDATA 'OASIS/our-world-quest-test-matrix.json'

function Invoke-SeedScript([string]$Name, [hashtable]$Arguments) {
    $path = Join-Path $scripts $Name
    if (-not (Test-Path -LiteralPath $path -PathType Leaf)) { throw "Required seed script is missing: $path" }
    Write-Host "`n=== $Name ===" -ForegroundColor Cyan
    & $path @Arguments
    if (-not $?) { throw "$Name failed." }
}

$common = @{ Web4BaseUrl=$Web4BaseUrl; CredentialPath=$CredentialPath; Latitude=$Latitude; Longitude=$Longitude }
if ($PlanOnly) { $common.PlanOnly = $true }
Invoke-SeedScript 'seed_our_world_geonfts.ps1' ($common + @{ OutputPath=$anorakManifest })

$questMatrix = @{ Web4BaseUrl=$Web4BaseUrl; Web5BaseUrl=$Web5BaseUrl; CredentialPath=$CredentialPath; Latitude=$Latitude; Longitude=$Longitude }
if ($PlanOnly) { $questMatrix.PlanOnly = $true }
Invoke-SeedScript 'seed_our_world_quest_test_matrix.ps1' $questMatrix

$hotSpotMatrix = @{ Web4BaseUrl=$Web4BaseUrl; Web5BaseUrl=$Web5BaseUrl; CredentialPath=$CredentialPath; GeoNFTManifestPath=$matrixManifest; Latitude=$Latitude; Longitude=$Longitude }
if ($PlanOnly) { $hotSpotMatrix.PlanOnly = $true }
Invoke-SeedScript 'seed_our_world_geohotspot_quest_matrix.ps1' $hotSpotMatrix

$treeQuest = @{ Web4BaseUrl=$Web4BaseUrl; Web5BaseUrl=$Web5BaseUrl; CredentialPath=$CredentialPath; ManifestPath=$anorakManifest }
if ($PlanOnly) { $treeQuest.PlanOnly = $true }
Invoke-SeedScript 'seed_our_world_tree_quest.ps1' $treeQuest

Write-Host "`nOur World full seed completed successfully." -ForegroundColor Green
