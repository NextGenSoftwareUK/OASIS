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
$rarities = @('Common', 'Rare', 'Epic', 'Legendary', 'Uncommon')
$imageRoot = 'https://raw.githubusercontent.com/NextGenSoftwareUK/OASIS/Development/Docs/Assets/OurWorld/TreeBigIcons'
$images = @("$imageRoot/rainbow-tree.png", "$imageRoot/lightning-tree.png", "$imageRoot/mycelium-tree.png", "$imageRoot/fruit-tree.png", "$imageRoot/skeleton-tree.png")
$objectives = @(for ($i=0; $i -lt $placements.Count; $i++) {
    $activateEvents = @()
    if ($i -eq 0) {
        $activateEvents = @(
            @{ eventType='ShowNarration'; targetGame='Our World'; narrationText="Anorak:`n`nFind and collect $($placements.Count) tokens of nature's regenerative power.`n`nEach token you find will help heal our world." },
            @{ eventType='ShowImage'; targetGame='Our World'; imageUrl='oasis://our-world/anorak'; imageTitle='Anorak' },
            @{ eventType='PlayAudio'; targetGame='Our World'; audioUrl='oasis://our-world/anorak-welcome'; audioTitle='Anorak welcomes you' }
        )
    }
    $completeEvents = @(
        # The authored objective-complete animation owns its synchronized chest,
        # blue particles and congratulations/pickup audio track.
        @{ eventType='PlayAnimation'; targetGame='Our World'; animationKey='objective-complete'; narrationText="Collect the $($names[$i])" }
    )
    if ($i -eq $placements.Count - 1) {
        $completeEvents += @{ eventType='PlayAnimation'; targetGame='Our World'; animationKey='quest-complete'; narrationText='Quest Complete: Restoration of Harmony' }
    }
    @{
        id = [Guid]::NewGuid().ToString(); order = $i
        title = "Collect the $($names[$i])"; description = "Find and collect the tree at $($placements[$i].label)."
        gameSource = 'Our World'
        needToCollectItems = @{ 'Our World' = @("geonft:$($ids[$i])") }
        crossGameEventsOnActivate = $activateEvents
        crossGameEventsOnComplete = $completeEvents
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
    for ($i=0; $i -lt $ids.Count; $i++) {
        $id = $ids[$i]
        $placement = @($geo | Where-Object id -eq $id)
        if ($placement.Count -ne 1) { throw "GeoNFT $id not found. Seed placements first." }
        $current = $placement[0]
        $canonicalDescription = "Anorak's $($names[$i]) token at $($placements[$i].label). Collect it to restore nature's harmony."
        $updated = Invoke-QuestSeedApi $Web4BaseUrl "nft/geo-nft/$id" 'Put' @{
            title=$names[$i]; description=$canonicalDescription; imageUrl=$images[$i]
            permSpawn=$false; allowOtherPlayersToAlsoCollect=$true
            globalSpawnQuantity=0; playerSpawnQuantity=1; respawnDurationInSeconds=60
            metaData=@{ 'OurWorld.DemoSeed'='true'; 'OurWorld.AnorakTree'=$names[$i]; 'OurWorld.Rarity'=$rarities[$i]; 'OurWorld.Category'='Nature' }
        }
        if ([string]$updated.title -ne $names[$i] -or [string]$updated.imageUrl -ne $images[$i]) {
            throw "GeoNFT $id did not persist its canonical Anorak identity."
        }
        $geo = @($geo | Where-Object id -ne $id) + @($updated)
    }
    $quests = @(Invoke-QuestSeedApi $Web5BaseUrl 'quests/all-for-avatar/game')
    $existing = @($quests | Where-Object startupSequence -eq 'anorak-trees')
    if ($existing.Count -gt 1) { throw 'Multiple Anorak intro quests exist; resolve duplicate seed records.' }
    if ($existing.Count -eq 1) {
        $saved = Invoke-QuestSeedApi $Web5BaseUrl "quests/$($existing[0].id)"
        $savedIds = @($saved.objectives | ForEach-Object { $_.needToCollectItems.'Our World' } | Sort-Object)
        $expected = @($ids | ForEach-Object { "geonft:$_" } | Sort-Object)
        $newTokens = @($expected | Where-Object { $_ -notin $savedIds })
        $removedTokens = @($savedIds | Where-Object { $_ -notin $expected })
        $completed = @($saved.objectives | Where-Object { $_.isCompleted -or [int]$_.currentCount -gt 0 })
        if (($removedTokens.Count -gt 0 -or $newTokens.Count -gt 0) -and $completed.Count -gt 0) {
            throw 'Reset Anorak progress before changing its placement manifest.'
        }
        if ($removedTokens.Count -gt 0) {
            foreach ($token in $removedTokens) {
                $retiredId = $token.Substring('geonft:'.Length)
                $null = Invoke-QuestSeedApi $Web4BaseUrl "nft/geo-nft/$retiredId" 'Put' @{
                    permSpawn=$false; allowOtherPlayersToAlsoCollect=$true
                    globalSpawnQuantity=0; playerSpawnQuantity=0; respawnDurationInSeconds=0
                    metaData=@{ 'OurWorld.DemoSeed'='true'; 'OurWorld.Retired'='true'; 'OurWorld.ReplacedByManifest'=$ManifestPath }
                }
                Write-Host "Retired superseded Anorak placement $retiredId"
            }
            $saved.objectives = $objectives
            Write-Host "Migrating Anorak quest to the canonical $($objectives.Count)-tree manifest."
        } elseif ($newTokens.Count -eq 1) {
            $newObjective = @($objectives | Where-Object { @($_.needToCollectItems.'Our World') -contains $newTokens[0] }) | Select-Object -First 1
            if ($null -eq $newObjective) { throw 'Could not author the fifth Anorak objective.' }
            $saved.objectives += $newObjective
            Write-Host "Appending fifth Anorak objective: $($newObjective.title)"
        }
        $saved.description = $quest.description
        $saved.objectiveCompletionOrder = 0
        # Upgrade the existing demo quest in place to the canonical cross-game
        # presentation contract without replacing its identity or progress.
        foreach ($authored in $objectives) {
            $token = [string]$authored.needToCollectItems.'Our World'[0]
            $persistedObjective = @($saved.objectives | Where-Object { @($_.needToCollectItems.'Our World') -contains $token }) | Select-Object -First 1
            if ($null -eq $persistedObjective) { throw "Could not map authored event data to objective $token." }
            $persistedObjective.title = $authored.title
            $persistedObjective.description = $authored.description
            $persistedObjective.order = $authored.order
            $persistedObjective.needToCollectItems = $authored.needToCollectItems
            $persistedObjective | Add-Member -NotePropertyName crossGameEventsOnActivate -NotePropertyValue $authored.crossGameEventsOnActivate -Force
            $persistedObjective | Add-Member -NotePropertyName crossGameEventsOnComplete -NotePropertyValue $authored.crossGameEventsOnComplete -Force
        }
        $saved = Invoke-QuestSeedApi $Web5BaseUrl "quests/$($saved.id)" 'Put' $saved
        Write-Host "Reusing quest $($saved.id)"
    } else {
        $saved = Invoke-QuestSeedApi $Web5BaseUrl 'quests' 'Post' $quest
        $persisted = Invoke-QuestSeedApi $Web5BaseUrl "quests/$($saved.id)"
        if ($null -eq $persisted -or [string]$persisted.id -ne [string]$saved.id) {
            throw "WEB5 reported quest $($saved.id) as saved, but it could not be loaded again. No inventory repair was attempted."
        }
        $saved = $persisted
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
            $item | Add-Member -NotePropertyName name -NotePropertyValue $source.title -Force
            $item | Add-Member -NotePropertyName description -NotePropertyValue $source.description -Force
            $item | Add-Member -NotePropertyName itemType -NotePropertyValue 'Nature' -Force
            $item | Add-Member -NotePropertyName image2DURI -NotePropertyValue $source.imageUrl -Force
            if ($item.metaData) { $item.metaData.PSObject.Properties.Remove('NFTId'); $item.metaData.PSObject.Properties.Remove('NftId') }
            $null = Invoke-QuestSeedApi $Web4BaseUrl "avatar/inventory/$($item.id)" 'Put' $item
            Write-Host "Repaired collected GeoNFT $id"
        }
    }
    # Reconcile already-correct GeoNFT inventory rows with the canonical placement
    # identity as well. Earlier seeds used one generic source NFT title for every
    # tree, which made inventory rows impossible to map to quest objectives.
    $inventory = @(Invoke-QuestSeedApi $Web4BaseUrl 'avatar/inventory')
    foreach ($item in $inventory) {
        $index = [Array]::IndexOf($ids, [string]$item.geoNFTId)
        if ($index -lt 0) { continue }
        $item | Add-Member -NotePropertyName name -NotePropertyValue $names[$index] -Force
        $item | Add-Member -NotePropertyName description -NotePropertyValue "Anorak's $($names[$index]) token at $($placements[$index].label). Collect it to restore nature's harmony." -Force
        $item | Add-Member -NotePropertyName itemType -NotePropertyValue 'Nature' -Force
        $item | Add-Member -NotePropertyName rarity -NotePropertyValue $rarities[$index] -Force
        $item | Add-Member -NotePropertyName image2DURI -NotePropertyValue $images[$index] -Force
        $null = Invoke-QuestSeedApi $Web4BaseUrl "avatar/inventory/$($item.id)" 'Put' $item
    }
    # Older collect calls could append the same placement more than once. Keep the
    # earliest acquisition and remove only duplicate rows for this manifest. The
    # current AvatarManager identity guard prevents these duplicates recurring.
    $inventory = @(Invoke-QuestSeedApi $Web4BaseUrl 'avatar/inventory')
    foreach ($id in $ids) {
        $duplicates = @($inventory | Where-Object { [string]$_.geoNFTId -eq $id } |
            Sort-Object acquiredOn, createdDate, id)
        if ($duplicates.Count -le 1) { continue }
        foreach ($duplicate in @($duplicates | Select-Object -Skip 1)) {
            $quantity = if ([int]$duplicate.quantity -gt 0) { [int]$duplicate.quantity } else { 1 }
            $null = Invoke-QuestSeedApi $Web4BaseUrl "avatar/inventory/$($duplicate.id)?quantity=$quantity" 'Delete'
            Write-Host "Removed duplicate inventory row $($duplicate.id) for GeoNFT $id"
        }
    }
    $inventory = @(Invoke-QuestSeedApi $Web4BaseUrl 'avatar/inventory')
    foreach ($id in $ids) {
        if (@($inventory | Where-Object { [string]$_.geoNFTId -eq $id }).Count -gt 1) {
            throw "Duplicate inventory rows remain for GeoNFT $id after repair."
        }
    }
    $verified = Invoke-QuestSeedApi $Web5BaseUrl "quests/$($saved.id)/inventory-progress" 'Post' @{}
    Write-Host "Verified $($verified.quest.objectives.Count) objectives. Status: $($verified.quest.status). Quest ID: $($verified.quest.id)"
} finally { $headers.Clear(); $avatar=$null; $Credential=$null }
