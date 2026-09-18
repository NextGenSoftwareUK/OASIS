<#
.SYNOPSIS
Seeds two repeatable Our World quest fixtures and six distinct GeoNFTs on the development APIs.

.DESCRIPTION
The suite creates one AnyOrder quest and one InOrder quest. Every objective owns a
different source NFT and GeoNFT placement, with varied names, descriptions, images,
rarities and collection rules. A manifest makes interrupted runs resumable and keeps
the fixture identities stable. This script never edits or resets the Anorak quest.
#>
[CmdletBinding()]
param(
    [string]$Web4BaseUrl = 'https://dev.api.web4.oasisomniverse.one',
    [string]$Web5BaseUrl = 'https://dev.api.starnet.oasisomniverse.one',
    [string]$CredentialPath = (Join-Path $env:LOCALAPPDATA 'OASIS/our-world-geonft-seed.credential.clixml'),
    [PSCredential]$Credential,
    [double]$Latitude = 31.54998,
    [double]$Longitude = 74.27728,
    [string]$ManifestPath = (Join-Path $env:LOCALAPPDATA 'OASIS/our-world-quest-test-matrix.json'),
    [switch]$PlanOnly
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest
$imageRoot = 'https://raw.githubusercontent.com/NextGenSoftwareUK/Our-World/main/Assets/SFX%20Selects/endangered%20tokens/TreeBigIcons'
$fixtures = @(
    [ordered]@{ key='aurora-fern'; name='Aurora Fern'; description='A luminous fern used to test alphabetical sorting and once-per-player collection.'; symbol='OWAF'; rarity='Uncommon'; image="$imageRoot/1.png"; bearing=20; distance=95; quest='any'; perm=$false; share=$true; global=0; player=1; cooldown=0 },
    [ordered]@{ key='cobalt-mushroom'; name='Cobalt Mushroom'; description='A cobalt forest specimen used to test descriptions, rarity sorting and search.'; symbol='OWCM'; rarity='Rare'; image="$imageRoot/2.png"; bearing=70; distance=110; quest='any'; perm=$false; share=$true; global=0; player=1; cooldown=0 },
    [ordered]@{ key='ember-orchid'; name='Ember Orchid'; description='A warm orange orchid used to prove AnyOrder objectives complete independently.'; symbol='OWEO'; rarity='Epic'; image="$imageRoot/3.png"; bearing=120; distance=125; quest='any'; perm=$false; share=$true; global=0; player=1; cooldown=0 },
    [ordered]@{ key='moonlit-reed'; name='Moonlit Reed'; description='The first ordered objective and a permanent spawn with a thirty-second cooldown.'; symbol='OWMR'; rarity='Common'; image="$imageRoot/4.png"; bearing=190; distance=105; quest='ordered'; perm=$true; share=$true; global=0; player=1; cooldown=30 },
    [ordered]@{ key='prism-bloom'; name='Prism Bloom'; description='The second ordered objective; each avatar may collect it twice and cannot share a claimed placement.'; symbol='OWPB'; rarity='Legendary'; image="$imageRoot/Screenshot%202024-08-24%20at%2014.38.16%202.png"; bearing=240; distance=125; quest='ordered'; perm=$false; share=$false; global=0; player=2; cooldown=0 },
    [ordered]@{ key='verdant-starfruit'; name='Verdant Starfruit'; description='The final ordered objective; its global limit overrides the per-player value.'; symbol='OWVS'; rarity='Mythic'; image="$imageRoot/1.png"; bearing=300; distance=145; quest='ordered'; perm=$false; share=$true; global=5; player=1; cooldown=0 }
)

function Get-Coordinate([double]$bearing, [double]$distance) {
    $r = [Math]::PI / 180; $lat = $Latitude * $r; $lon = $Longitude * $r
    $heading = $bearing * $r; $angle = $distance / 6371008.8
    $newLat = [Math]::Asin([Math]::Sin($lat) * [Math]::Cos($angle) + [Math]::Cos($lat) * [Math]::Sin($angle) * [Math]::Cos($heading))
    $newLon = $lon + [Math]::Atan2([Math]::Sin($heading) * [Math]::Sin($angle) * [Math]::Cos($lat), [Math]::Cos($angle) - [Math]::Sin($lat) * [Math]::Sin($newLat))
    return @{ lat=[Math]::Round($newLat / $r, 8); long=[Math]::Round((($newLon / $r + 540) % 360) - 180, 8) }
}

$plan = @($fixtures | ForEach-Object { $point=Get-Coordinate $_.bearing $_.distance; [pscustomobject]@{
    key=$_.key; name=$_.name; quest=$_.quest; rarity=$_.rarity; image=$_.image; lat=$point.lat; long=$point.long
    permSpawn=$_.perm; allowOtherPlayersToAlsoCollect=$_.share; globalSpawnQuantity=$_.global
    playerSpawnQuantity=$_.player; respawnDurationInSeconds=$_.cooldown
} })
if ($PlanOnly) { $plan | ConvertTo-Json -Depth 10; return }

if ($null -eq $Credential) { $Credential = Import-Clixml -LiteralPath $CredentialPath }
function Unwrap($response, [string]$operation) {
    if ($null -eq $response.PSObject.Properties['isError']) { $response = $response.result }
    if ($null -eq $response -or $null -eq $response.PSObject.Properties['isError'] -or $response.isError) {
        throw "$operation failed: $($response.message)"
    }
    return $response.result
}
$loginJson = @{ username=$Credential.UserName; password=$Credential.GetNetworkCredential().Password } | ConvertTo-Json
try { $avatar = Unwrap (Invoke-RestMethod -Uri "$($Web4BaseUrl.TrimEnd('/'))/api/avatar/authenticate" -Method Post -ContentType 'application/json' -Body $loginJson -TimeoutSec 60) 'Authenticate' }
finally { $loginJson = $null }
$headers = @{ Authorization="Bearer $($avatar.jwtToken)" }
function Invoke-OasisApi([string]$base, [string]$path, [string]$method='Get', $body=$null) {
    $args=@{ Uri="$($base.TrimEnd('/'))/api/$path"; Method=$method; Headers=$headers; TimeoutSec=240 }
    if ($null -ne $body) { $args.ContentType='application/json'; $args.Body=$body | ConvertTo-Json -Depth 80 }
    return Unwrap (Invoke-RestMethod @args) "$method $path"
}

$manifest = [ordered]@{ version=1; avatarId=[string]$avatar.id; fixtures=@(); quests=@() }
if (Test-Path -LiteralPath $ManifestPath) {
    $loaded = Get-Content -LiteralPath $ManifestPath -Raw | ConvertFrom-Json
    if ([string]$loaded.avatarId -ne [string]$avatar.id) { throw 'The existing fixture manifest belongs to another avatar.' }
    $manifest.fixtures = @($loaded.fixtures)
    $manifest.quests = @($loaded.quests | Group-Object key | ForEach-Object { $_.Group | Select-Object -First 1 })
}
function Save-Manifest { $folder=Split-Path -Parent $ManifestPath; New-Item -ItemType Directory -Path $folder -Force | Out-Null; $manifest | ConvertTo-Json -Depth 30 | Set-Content -LiteralPath $ManifestPath -Encoding UTF8 }
Save-Manifest

try {
    $ownedNfts = @(Invoke-OasisApi $Web4BaseUrl "nft/load-all-nfts-for-avatar/$($avatar.id)")
    foreach ($fixture in $fixtures) {
        $saved = @($manifest.fixtures | Where-Object key -eq $fixture.key) | Select-Object -First 1
        $point = Get-Coordinate $fixture.bearing $fixture.distance
        if ($null -eq $saved) {
            $matchingNfts = @($ownedNfts | Where-Object { [string]$_.title -eq $fixture.name -and [string]$_.mintedByAvatarId -eq [string]$avatar.id })
            if ($matchingNfts.Count -gt 1) { throw "Multiple owned source NFTs named '$($fixture.name)' exist; resolve the duplicate before seeding." }
            if ($matchingNfts.Count -eq 1) {
                $nft = $matchingNfts[0]
                Write-Host "Reusing source NFT $($nft.id) for $($fixture.name)."
            } else {
                Write-Host "Minting $($fixture.name)..."
                $nft = Invoke-OasisApi $Web4BaseUrl 'nft/mint-nft' 'Post' @{
                title=$fixture.name; description=$fixture.description; symbol=$fixture.symbol; imageUrl=$fixture.image
                numberToMint=1; price=0; discount=0; offChainProvider='MongoDBOASIS'; onChainProvider='SolanaOASIS'
                nftOffChainMetaType='OASIS'; nftStandardType='SPL'; sendToAvatarAfterMintingId=$avatar.id
                storeNFTMetaDataOnChain=$false; waitTillNFTMinted=$true; waitForNFTToMintInSeconds=180; attemptToMintEveryXSeconds=1
                waitTillNFTVerified=$true; waitForNFTToVerifyInSeconds=180; attemptToVerifyEveryXSeconds=1
                waitTillNFTSent=$true; waitForNFTToSendInSeconds=180; attemptToSendEveryXSeconds=1
                metaData=@{ 'OurWorld.TestSuite'='quest-mode-spawn-matrix'; 'OurWorld.FixtureKey'=$fixture.key; 'OurWorld.Rarity'=$fixture.rarity; 'OurWorld.Category'='Nature' }
                }
                $ownedNfts += $nft
            }
            $saved=[pscustomobject]@{ key=$fixture.key; sourceNFTId=[string]$nft.id; geoNFTId=$null; name=$fixture.name; rarity=$fixture.rarity; quest=$fixture.quest }
            $manifest.fixtures += $saved; Save-Manifest
        }
        if ([string]::IsNullOrWhiteSpace([string]$saved.geoNFTId)) {
            $source = Invoke-OasisApi $Web4BaseUrl "nft/load-nft-by-id/$($saved.sourceNFTId)/MongoDBOASIS/false"
            if ([Guid]$source.currentOwnerAvatarId -eq [Guid]::Empty -and [string]$source.mintedByAvatarId -eq [string]$avatar.id) {
                $source = Invoke-OasisApi $Web4BaseUrl 'nft/update-web4-nft' 'Post' @{
                    id=$saved.sourceNFTId; currentOwnerAvatarId=$avatar.id; mintedByAvatarId=$avatar.id
                }
            }
            if ([string]$source.currentOwnerAvatarId -ne [string]$avatar.id) {
                throw "Source NFT $($saved.sourceNFTId) is not owned by the authenticated fixture avatar."
            }
            $geo = Invoke-OasisApi $Web4BaseUrl 'nft/place-geo-nft' 'Post' @{
                originalOASISNFTId=[string]$saved.sourceNFTId; originalOASISNFTOffChainProvider='MongoDBOASIS'; geoNFTMetaDataProvider='MongoDBOASIS'
                lat=$point.lat; long=$point.long; permSpawn=$fixture.perm; allowOtherPlayersToAlsoCollect=$fixture.share
                globalSpawnQuantity=$fixture.global; playerSpawnQuantity=$fixture.player; respawnDurationInSeconds=$fixture.cooldown
            }
            $saved.geoNFTId=[string]$geo.id; Save-Manifest
        } else {
            $geo = Invoke-OasisApi $Web4BaseUrl "nft/geo-nft/$($saved.geoNFTId)" 'Put' @{
                permSpawn=$fixture.perm; allowOtherPlayersToAlsoCollect=$fixture.share; globalSpawnQuantity=$fixture.global
                playerSpawnQuantity=$fixture.player; respawnDurationInSeconds=$fixture.cooldown
            }
        }
        foreach ($property in 'permSpawn','allowOtherPlayersToAlsoCollect','globalSpawnQuantity','playerSpawnQuantity','respawnDurationInSeconds') {
            $expected = switch ($property) { 'permSpawn' {$fixture.perm}; 'allowOtherPlayersToAlsoCollect' {$fixture.share}; 'globalSpawnQuantity' {$fixture.global}; 'playerSpawnQuantity' {$fixture.player}; default {$fixture.cooldown} }
            if ($geo.$property -ne $expected) { throw "$($fixture.key) did not persist $property." }
        }
    }

    $questDefinitions = @(
        @{ key='any-order'; name='Chromatic Canopy: Any Path'; description='Collect three unusual nature specimens in whichever order you choose.'; order=0; fixtures=@($fixtures | Where-Object quest -eq 'any') },
        @{ key='in-order'; name='Celestial Garden: Follow the Sequence'; description='Collect Moonlit Reed, Prism Bloom and Verdant Starfruit in that exact order.'; order=1; fixtures=@($fixtures | Where-Object quest -eq 'ordered') }
    )
    $allQuests = @(Invoke-OasisApi $Web5BaseUrl 'quests/all-for-avatar/game')
    foreach ($definition in $questDefinitions) {
        $existing = @($allQuests | Where-Object {
            $null -ne $_ -and (($_.PSObject.Properties['title'] -and [string]$_.title -eq $definition.name) -or
                ($_.PSObject.Properties['name'] -and [string]$_.name -eq $definition.name))
        })
        if ($existing.Count -gt 1) { throw "Duplicate visible fixture quest: $($definition.name)" }
        $manifestQuest = @($manifest.quests | Where-Object key -eq $definition.key) | Select-Object -First 1
        if ($existing.Count -eq 0 -and $null -ne $manifestQuest) {
            $candidate = Invoke-OasisApi $Web5BaseUrl "quests/$($manifestQuest.id)"
            if ([string]$candidate.name -eq $definition.name) { $existing = @($candidate) }
        }
        if ($existing.Count -eq 1) {
            $persisted = Invoke-OasisApi $Web5BaseUrl "quests/$($existing[0].id)"
            if ([Guid]$persisted.createdByAvatarId -eq [Guid]::Empty) {
                $persisted.createdByAvatarId = $avatar.id
                $persisted = Invoke-OasisApi $Web5BaseUrl "quests/$($persisted.id)" 'Put' $persisted
            }
            if ([string]$persisted.createdByAvatarId -ne [string]$avatar.id) { throw "Quest $($persisted.id) has the wrong creator." }
            Write-Host "Reusing quest $($definition.name) ($($persisted.id))"
            continue
        }
        $objectives=@(); $index=0
        foreach ($fixture in $definition.fixtures) {
            $saved=@($manifest.fixtures | Where-Object key -eq $fixture.key) | Select-Object -First 1
            $completeEvents=@(@{ eventType='PlayAnimation'; targetGame='Our World'; animationKey='objective-complete'; narrationText="Objective Complete: $($fixture.name)" })
            if ($index -eq $definition.fixtures.Count - 1) { $completeEvents += @{ eventType='PlayAnimation'; targetGame='Our World'; animationKey='quest-complete'; narrationText="Quest Complete: $($definition.name)" } }
            $objectives += @{ id=[Guid]::NewGuid().ToString(); order=$index; title="Collect $($fixture.name)"; description=$fixture.description; gameSource='Our World'; needToCollectItems=@{ 'Our World'=@("geonft:$($saved.geoNFTId)") }; crossGameEventsOnComplete=$completeEvents }
            $index++
        }
        $created=Invoke-OasisApi $Web5BaseUrl 'quests' 'Post' @{ name=$definition.name; description=$definition.description; gameSource='Our World'; status=1; createdByAvatarId=$avatar.id; objectiveCompletionOrder=$definition.order; objectives=$objectives; metaData=@{ 'OurWorld.TestSuite'='quest-mode-spawn-matrix'; 'OurWorld.TestSuiteKey'=$definition.key } }
        $created=Invoke-OasisApi $Web5BaseUrl "quests/$($created.id)"
        if ([string]$created.createdByAvatarId -ne [string]$avatar.id) { throw "Created quest $($created.id) did not persist its creator." }
        $manifest.quests += [pscustomobject]@{ key=$definition.key; id=[string]$created.id; name=$definition.name; objectiveCompletionOrder=$definition.order }; Save-Manifest
        Write-Host "Created $($definition.name) ($($created.id))"
    }
    $visibleQuests = @(Invoke-OasisApi $Web5BaseUrl 'quests/all-for-avatar/game')
    foreach ($definition in $questDefinitions) {
        $matches = @($visibleQuests | Where-Object { [string]$_.name -eq $definition.name })
        if ($matches.Count -ne 1) { throw "Expected one visible '$($definition.name)' quest; found $($matches.Count)." }
        $expectedOrder = if ($definition.order -eq 0) { 'AnyOrder' } else { 'InOrder' }
        if ([string]$matches[0].objectiveCompletionOrder -ne $expectedOrder) { throw "$($definition.name) did not persist $expectedOrder." }
    }
    Write-Host "Seeded and verified six GeoNFT fixtures and two quest modes. Manifest: $ManifestPath" -ForegroundColor Green
} finally {
    $headers.Clear(); $avatar=$null; $Credential=$null
}
