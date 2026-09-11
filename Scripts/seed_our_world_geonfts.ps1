<#
.SYNOPSIS
Mints one Web4 NFT and places four API-backed GeoNFTs around Our World's desktop test location.
.DESCRIPTION
Mints one source Web4 NFT through the configured on-chain provider, then uses
that NFT as the source for four GeoNFT placements. The manifest is written as
soon as the source NFT exists so a failed placement can be resumed with
-OriginalNFTId. Credentials and tokens are never saved.
.EXAMPLE
./Scripts/seed_our_world_geonfts.ps1 -PlanOnly
.EXAMPLE
./Scripts/seed_our_world_geonfts.ps1
./Scripts/seed_our_world_geonfts.ps1 -OriginalNFTId <source-Web4-NFT-guid>
#>
[CmdletBinding()]
param(
    # Development deployment: never use a production API for demo seed data.
    [string]$Web4BaseUrl = 'https://dev.api.web4.oasisomniverse.one',
    [Guid]$OriginalNFTId = [Guid]::Empty,
    [PSCredential]$Credential,
    [ValidateRange(-85, 85)][double]$Latitude = 31.54998,
    [ValidateRange(-180, 180)][double]$Longitude = 74.27728,
    [string]$Provider = 'MongoDBOASIS',
    [string]$OnChainProvider = 'SolanaOASIS',
    [string]$NFTStandardType = 'SPL',
    [string]$Title = 'Our World Desktop GeoNFT Demo',
    [string]$Description = 'A seeded GeoNFT used to verify map placement around the desktop test origin.',
    [string]$Symbol = 'OWDEMO',
    [string]$OutputPath = (Join-Path ([IO.Path]::GetTempPath()) 'our-world-geonft-demo.json'),
    [switch]$PlanOnly
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

function Get-DemoCoordinate {
    param([double]$Bearing, [double]$Distance)
    $radians = [Math]::PI / 180
    $lat = $Latitude * $radians
    $lon = $Longitude * $radians
    $heading = $Bearing * $radians
    $angle = $Distance / 6371008.8
    $newLat = [Math]::Asin([Math]::Sin($lat) * [Math]::Cos($angle) +
        [Math]::Cos($lat) * [Math]::Sin($angle) * [Math]::Cos($heading))
    $newLon = $lon + [Math]::Atan2([Math]::Sin($heading) * [Math]::Sin($angle) * [Math]::Cos($lat),
        [Math]::Cos($angle) - [Math]::Sin($lat) * [Math]::Sin($newLat))
    [pscustomobject]@{
        lat = [Math]::Round($newLat / $radians, 8)
        long = [Math]::Round((($newLon / $radians + 540) % 360) - 180, 8)
    }
}

$layout = @(
    @{ label = 'North 30 m'; bearing = 0; distance = 30 },
    @{ label = 'East 45 m'; bearing = 90; distance = 45 },
    @{ label = 'South 60 m'; bearing = 180; distance = 60 },
    @{ label = 'West 75 m'; bearing = 270; distance = 75 }
)
$plan = @($layout | ForEach-Object {
    $coordinate = Get-DemoCoordinate $_.bearing $_.distance
    [pscustomobject]@{ label = $_.label; distanceMeters = $_.distance; lat = $coordinate.lat; long = $coordinate.long }
})
if ($PlanOnly) { $plan; return }
$server = $Web4BaseUrl.TrimEnd('/') -replace '/api$', ''
$api = "$server/api"
$openApi = Invoke-RestMethod -Uri "$server/swagger/v1/swagger.json" -TimeoutSec 30
$schemaRef = $openApi.paths.'/api/Nft/place-geo-nft'.post.requestBody.content.'application/json'.schema.'$ref'
$schemaName = $schemaRef -replace '^#/components/schemas/', ''
$placementSchema = $openApi.components.schemas.$schemaName
foreach ($axis in @('lat', 'long')) {
    if ($placementSchema.properties.$axis.type -ne 'number' -or $placementSchema.properties.$axis.format -ne 'double') {
        throw "The running API still has non-double $axis coordinates. Rebuild and restart WEB4 before seeding; fractional GPS coordinates must survive the request."
    }
}

$mintSchemaRef = $openApi.paths.'/api/Nft/mint-nft'.post.requestBody.content.'application/json'.schema.'$ref'
$mintSchemaName = $mintSchemaRef -replace '^#/components/schemas/', ''
$mintSchema = $openApi.components.schemas.$mintSchemaName
foreach ($field in @('title', 'offChainProvider', 'onChainProvider', 'nftStandardType')) {
    if ($null -eq $mintSchema.properties.$field) {
        throw "The running API does not expose the required mint field '$field'."
    }
}

if ($null -eq $Credential) { $Credential = Get-Credential -Message 'Local OASIS avatar login for Our World demo data' }
if ($null -eq $Credential) { throw 'An OASIS login is required.' }

function Get-OasisValue {
    param($Envelope, [string]$Operation)
    if ($null -eq $Envelope -or $null -eq $Envelope.PSObject.Properties['isError']) {
        throw "$Operation returned an invalid OASISResult."
    }
    if ($Envelope.isError) { throw "$Operation failed: $($Envelope.message)" }
    $value = $Envelope.PSObject.Properties['result']
    if ($null -ne $value) { return $value.Value }
    # The API omits result for a successful empty list.
    return $null
}

$loginBody = @{ username = $Credential.UserName; password = $Credential.GetNetworkCredential().Password } | ConvertTo-Json
try {
    $login = Invoke-RestMethod -Uri "$api/avatar/authenticate" -Method Post -ContentType 'application/json' -Body $loginBody -TimeoutSec 45
} finally { $loginBody = $null }
$avatar = Get-OasisValue $login.result 'Authenticate'
if ($null -eq $avatar -or [string]::IsNullOrWhiteSpace($avatar.jwtToken)) { throw 'Login returned no avatar session.' }
$headers = @{ Authorization = "Bearer $($avatar.jwtToken)" }

function Invoke-GeoApi {
    param([string]$Endpoint, [string]$Method = 'Get', $Body)
    $arguments = @{ Uri = "$api/$Endpoint"; Method = $Method; Headers = $headers; TimeoutSec = 45 }
    if ($null -ne $Body) {
        $arguments.ContentType = 'application/json'
        $arguments.Body = $Body | ConvertTo-Json -Depth 20
    }
    $response = Invoke-RestMethod @arguments
    Get-OasisValue $response $Endpoint
}

try {
    $manifest = [ordered]@{
        web4BaseUrl = $server; sourceNFTId = $null; sourceNFTCreated = $false
        center = @{ lat = $Latitude; long = $Longitude }; placements = @()
    }

    if ($OriginalNFTId -eq [Guid]::Empty) {
        $minted = Invoke-GeoApi 'nft/mint-nft' 'Post' @{
            title = $Title; description = $Description; symbol = $Symbol
            numberToMint = 1; price = 0; discount = 0
            offChainProvider = $Provider; onChainProvider = $OnChainProvider
            nftOffChainMetaType = 'OASIS'; nftStandardType = $NFTStandardType
            storeNFTMetaDataOnChain = $false
            waitTillNFTMinted = $true; waitForNFTToMintInSeconds = 180
            attemptToMintEveryXSeconds = 1; waitTillNFTVerified = $true
            waitForNFTToVerifyInSeconds = 180; attemptToVerifyEveryXSeconds = 1
            waitTillNFTSent = $true; waitForNFTToSendInSeconds = 180
            attemptToSendEveryXSeconds = 1
            metaData = @{ 'OurWorld.DemoSeed' = 'true'; 'OurWorld.CenterLatitude' = "$Latitude"; 'OurWorld.CenterLongitude' = "$Longitude" }
        }
        if ($null -eq $minted -or [string]::IsNullOrWhiteSpace($minted.id)) {
            throw 'Minting succeeded without returning a Web4 NFT ID; no GeoNFT placement was attempted.'
        }
        $OriginalNFTId = [Guid]$minted.id
        $manifest.sourceNFTCreated = $true
    }

    $original = Invoke-GeoApi "nft/load-nft-by-id/$OriginalNFTId/$Provider/false"
    if ($null -eq $original -or [Guid]$original.id -ne $OriginalNFTId) { throw 'The source Web4 NFT could not be loaded.' }
    $manifest.sourceNFTId = $OriginalNFTId.ToString()
    # Save the minted source before placing anything, so the run is safely resumable.
    $manifest | ConvertTo-Json -Depth 10 | Set-Content -LiteralPath $OutputPath -Encoding UTF8

    $existing = @(Invoke-GeoApi "nft/load-all-geo-nfts/$Provider/false")
    foreach ($point in $plan) {
        $matches = @($existing | Where-Object {
            $null -ne $_ -and $_.originalWeb4OASISNFTId -eq $OriginalNFTId.ToString() -and
            $_.placedByAvatarId -eq $avatar.id -and
            [Math]::Abs([double]$_.lat - $point.lat) -lt 0.000000001 -and
            [Math]::Abs([double]$_.long - $point.long) -lt 0.000000001
        })
        if ($matches.Count -gt 1) { throw "Duplicate existing placements at $($point.label); inspect them before proceeding." }
        $created = $false
        if ($matches.Count -eq 1) {
            $geo = $matches[0]
        } else {
            $geo = Invoke-GeoApi 'nft/place-geo-nft' 'Post' @{
                originalOASISNFTId = $OriginalNFTId.ToString()
                originalOASISNFTOffChainProvider = $Provider; geoNFTMetaDataProvider = $Provider
                lat = $point.lat; long = $point.long; permSpawn = $true
                allowOtherPlayersToAlsoCollect = $true; globalSpawnQuantity = -1
                playerSpawnQuantity = -1; respawnDurationInSeconds = 60
            }
            if ($null -eq $geo -or [string]::IsNullOrWhiteSpace($geo.id)) { throw "Placement at $($point.label) returned no GeoNFT ID." }
            $created = $true
        }
        if ([Math]::Abs([double]$geo.lat - $point.lat) -gt 0.000000001 -or
            [Math]::Abs([double]$geo.long - $point.long) -gt 0.000000001) {
            throw "API changed the coordinates of GeoNFT $($geo.id). Stop and inspect persistence."
        }
        $manifest.placements += [pscustomobject]@{ label = $point.label; id = $geo.id; lat = $point.lat; long = $point.long; created = $created }
        # Save progress after every successful placement, including interrupted runs.
        $manifest | ConvertTo-Json -Depth 10 | Set-Content -LiteralPath $OutputPath -Encoding UTF8
    }
    $persisted = @(Invoke-GeoApi "nft/load-all-geo-nfts/$Provider/false")
    foreach ($placed in $manifest.placements) {
        $saved = @($persisted | Where-Object { $null -ne $_ -and $_.id -eq $placed.id })
        if ($saved.Count -ne 1 -or [Math]::Abs([double]$saved[0].lat - $placed.lat) -gt 0.000000001 -or
            [Math]::Abs([double]$saved[0].long - $placed.long) -gt 0.000000001) {
            throw "Read-back verification failed for $($placed.label), ID $($placed.id)."
        }
    }
    $manifest.placements | Format-Table label, id, lat, long, created
    Write-Host "Verified all four API-backed placements. Manifest: $OutputPath"
} finally {
    $headers.Clear()
    $avatar = $null
    $login = $null
}
