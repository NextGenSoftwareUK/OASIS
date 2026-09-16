<#
.SYNOPSIS
Creates Anorak's API quest from the existing GeoNFT seed manifest (four or five objectives).
Repairs only inventory rows whose misplaced NFT ID is a GeoNFT in that manifest.
#>
[CmdletBinding()]
param(
    [string]$ManifestPath = (Join-Path ([IO.Path]::GetTempPath()) 'our-world-geonft-demo.json'),
    [string]$Web4BaseUrl = 'https://dev.api.web4.oasisomniverse.one',
    [string]$Web5BaseUrl = 'https://dev.api.starnet.oasisomniverse.one',
    [string]$CredentialPath = (Join-Path $env:LOCALAPPDATA 'OASIS/our-world-geonft-seed.credential.clixml'),
    [PSCredential]$Credential,
    [switch]$PlanOnly
)
$ErrorActionPreference = 'Stop'
$manifest = Get-Content -LiteralPath $ManifestPath -Raw | ConvertFrom-Json
$placements = @($manifest.placements)
if ($placements.Count -lt 4 -or $placements.Count -gt 5) { throw 'The tree demo requires four or five manifest placements.' }
$ids = @($placements | ForEach-Object { ([Guid]$_.id).ToString() } | Select-Object -Unique)
if ($ids.Count -ne $placements.Count -or $ids -contains [Guid]::Empty.ToString()) { throw 'Manifest contains duplicate or empty GeoNFT IDs.' }
$names = @('Rainbow Tree', 'Lightning Tree', 'Mycelium Tree', 'Fruit Tree', 'Skeleton Tree')
$objectives = @(for ($i=0; $i -lt $placements.Count; $i++) {
    @{
        id = [Guid]::NewGuid().ToString(); order = $i
        title = "Collect the $($names[$i])"; description = "Find and collect the tree at $($placements[$i].label)."
        gameSource = 'Our World'
        needToCollectItems = @{ 'Our World' = @("geonft:$($ids[$i])") }
    }
})
$quest = @{
    name = 'Restoration of Harmony: Find the Endangered Trees'
    description = "Find and collect $($placements.Count) tokens of nature's regenerative power.`n`nEach token you find will help heal our world."
    gameSource = 'Our World'; status = 1; objectives = $objectives
    metaData = @{ 'OurWorld.StartupSequence' = 'anorak-trees' }
}
if ($PlanOnly) { $quest | ConvertTo-Json -Depth 20; return }
# Fail before creating or repairing data if the target deployment is older than this contract.
$schema = Invoke-RestMethod -Uri "$($Web5BaseUrl.TrimEnd('/'))/swagger/v1/swagger.json"
if (-not $schema.paths.PSObject.Properties['/api/quests/{id}/inventory-progress']) {
    throw 'Deploy the WEB5 inventory-progress endpoint before running this seed. No data has been changed.'
}
if ($null -eq $Credential) { $Credential = Import-Clixml -LiteralPath $CredentialPath }
function Unwrap($response) {
    # WEB4 uses OASISHttpResponseMessage; WEB5 returns OASISResult directly.
    if ($null -eq $response.PSObject.Properties['isError']) { $response = $response.result }
    if ($null -eq $response -or $null -eq $response.PSObject.Properties['isError'] -or $response.isError) {
        throw "API operation failed: $($response.message)"
    }
    return $response.result
}
$loginBody = @{ username=$Credential.UserName; password=$Credential.GetNetworkCredential().Password } | ConvertTo-Json
try { $avatar = Unwrap (Invoke-RestMethod -Uri "$($Web4BaseUrl.TrimEnd('/'))/api/avatar/authenticate" -Method Post -ContentType 'application/json' -Body $loginBody) }
finally { $loginBody = $null }
$headers = @{ Authorization = "Bearer $($avatar.jwtToken)" }
function Invoke-QuestSeedApi($base, $path, $method='Get', $body=$null) {
    $args = @{ Uri="$($base.TrimEnd('/'))/api/$path"; Method=$method; Headers=$headers; TimeoutSec=90 }
    if ($null -ne $body) { $args.ContentType='application/json'; $args.Body=$body | ConvertTo-Json -Depth 60 }
    Unwrap (Invoke-RestMethod @args)
}
try {
    # The manifest is the authority for quest identity, not a title-based count of arbitrary pickups.
    $geo = @(Invoke-QuestSeedApi $Web4BaseUrl 'nft/load-all-geo-nfts/MongoDBOASIS/false')
    foreach ($id in $ids) { if (@($geo | Where-Object id -eq $id).Count -ne 1) { throw "GeoNFT $id not found. Seed placements first." } }
    $quests = @(Invoke-QuestSeedApi $Web5BaseUrl 'quests/all-for-avatar/game')
    $existing = @($quests | Where-Object startupSequence -eq 'anorak-trees')
    if ($existing.Count -gt 1) { throw 'Multiple Anorak intro quests exist; resolve duplicate seed records.' }
    if ($existing.Count -eq 1) {
        $saved = Invoke-QuestSeedApi $Web5BaseUrl "quests/$($existing[0].id)"
        $savedIds = @($saved.objectives | ForEach-Object { $_.needToCollectItems.'Our World' } | Sort-Object)
        $expected = @($ids | ForEach-Object { "geonft:$_" } | Sort-Object)
        if (@(Compare-Object $savedIds $expected).Count -gt 0) { throw 'Existing quest targets a different manifest. Refusing to overwrite progress.' }
        Write-Host "Reusing quest $($saved.id)"
    } else {
        $saved = Invoke-QuestSeedApi $Web5BaseUrl 'quests' 'Post' $quest
        Write-Host "Created quest $($saved.id)"
    }
    $inventory = @(Invoke-QuestSeedApi $Web4BaseUrl 'avatar/inventory')
    foreach ($item in $inventory) {
        # Explicit repair of the old collect-geo-nft route defect, only for known placements.
        if ($ids -contains [string]$item.nftId -and ([string]$item.geoNFTId -eq [Guid]::Empty.ToString() -or -not $item.geoNFTId)) {
            $id = [string]$item.nftId
            $source = $geo | Where-Object id -eq $id | Select-Object -First 1
            $item | Add-Member -NotePropertyName geoNFTId -NotePropertyValue $id -Force
            $item.nftId = [Guid]::Empty.ToString()
            $item.name = $source.title; $item.description = $source.description
            $item.itemType = 'Nature'
            $item | Add-Member -NotePropertyName image2DURI -NotePropertyValue $source.imageUrl -Force
            if ($item.metaData) { $item.metaData.PSObject.Properties.Remove('NFTId'); $item.metaData.PSObject.Properties.Remove('NftId') }
            $null = Invoke-QuestSeedApi $Web4BaseUrl "avatar/inventory/$($item.id)" 'Put' $item
            Write-Host "Repaired collected GeoNFT $id"
        }
    }
    $verified = Invoke-QuestSeedApi $Web5BaseUrl "quests/$($saved.id)/inventory-progress" 'Post' @{}
    Write-Host "Verified $($verified.objectives.Count) objectives. Status: $($verified.status). Quest ID: $($verified.id)"
} finally { $headers.Clear(); $avatar=$null; $Credential=$null }
