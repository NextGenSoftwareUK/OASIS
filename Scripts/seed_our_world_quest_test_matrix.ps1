<#
.SYNOPSIS
Seeds four repeatable Our World quest fixtures and fourteen distinct GeoNFTs on the development APIs.

.DESCRIPTION
The suite creates two AnyOrder quests and two InOrder quests. Every objective owns a
different source NFT and GeoNFT placement, with varied names, descriptions, images,
rarities and collection rules. Two non-quest fixtures deliberately have zero allowance
to verify portal suppression without making a quest impossible. A manifest makes
interrupted runs resumable and keeps fixture identities stable. This script never
edits or resets the Anorak quest.
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
$imageRoot = 'https://raw.githubusercontent.com/NextGenSoftwareUK/OASIS/Development/Docs/Assets/OurWorld/TreeBigIcons'
$fixtures = @(
    [ordered]@{ key='aurora-fern'; name='Aurora Fern'; description='A luminous fern used to test alphabetical sorting and once-per-player collection.'; symbol='OWAF'; rarity='Uncommon'; image="$imageRoot/rainbow-tree.png"; bearing=20; distance=95; quest='any'; perm=$false; share=$true; global=0; player=1; cooldown=0 },
    [ordered]@{ key='cobalt-mushroom'; name='Cobalt Mushroom'; description='A cobalt forest specimen used to test descriptions, rarity sorting and search.'; symbol='OWCM'; rarity='Rare'; image="$imageRoot/lightning-tree.png"; bearing=70; distance=110; quest='any'; perm=$false; share=$true; global=0; player=1; cooldown=0 },
    [ordered]@{ key='ember-orchid'; name='Ember Orchid'; description='A warm orange orchid used to prove AnyOrder objectives complete independently.'; symbol='OWEO'; rarity='Epic'; image="$imageRoot/mycelium-tree.png"; bearing=120; distance=125; quest='any'; perm=$false; share=$true; global=0; player=1; cooldown=0 },
    [ordered]@{ key='moonlit-reed'; name='Moonlit Reed'; description='The first ordered objective and a permanent spawn with a thirty-second cooldown.'; symbol='OWMR'; rarity='Common'; image="$imageRoot/fruit-tree.png"; bearing=190; distance=105; quest='ordered'; perm=$true; share=$true; global=0; player=1; cooldown=30 },
    [ordered]@{ key='prism-bloom'; name='Prism Bloom'; description='The second ordered objective; each avatar may collect it twice and cannot share a claimed placement.'; symbol='OWPB'; rarity='Legendary'; image="$imageRoot/skeleton-tree.png"; bearing=240; distance=125; quest='ordered'; perm=$false; share=$false; global=0; player=2; cooldown=0 },
    [ordered]@{ key='verdant-starfruit'; name='Verdant Starfruit'; description='The final ordered objective; its global limit overrides the per-player value.'; symbol='OWVS'; rarity='Mythic'; image="$imageRoot/rainbow-tree.png"; bearing=300; distance=145; quest='ordered'; perm=$false; share=$true; global=5; player=1; cooldown=0 }
    [ordered]@{ key='solar-lotus'; name='Solar Lotus'; description='A radiant permanent specimen with immediate unlimited respawn.'; symbol='OWSL'; rarity='Celestial'; image="$imageRoot/lightning-tree.png"; bearing=330; distance=165; quest='respawn'; perm=$true; share=$true; global=0; player=1; cooldown=0 },
    [ordered]@{ key='tideglass-moss'; name='Tideglass Moss'; description='A translucent permanent specimen that becomes available again after twenty seconds.'; symbol='OWTM'; rarity='Uncommon'; image="$imageRoot/mycelium-tree.png"; bearing=350; distance=185; quest='respawn'; perm=$true; share=$true; global=0; player=1; cooldown=20 },
    [ordered]@{ key='echo-seed'; name='Echo Seed'; description='An unlimited per-player specimen with a ten-second personal cooldown.'; symbol='OWES'; rarity='Rare'; image="$imageRoot/fruit-tree.png"; bearing=10; distance=205; quest='respawn'; perm=$false; share=$true; global=0; player=-1; cooldown=10 },
    [ordered]@{ key='crystal-thistle'; name='Crystal Thistle'; description='A globally scarce specimen; the global allowance of two overrides its larger player allowance.'; symbol='OWCT'; rarity='Epic'; image="$imageRoot/skeleton-tree.png"; bearing=145; distance=165; quest='limits'; perm=$false; share=$true; global=2; player=5; cooldown=0 },
    [ordered]@{ key='obsidian-pod'; name='Obsidian Pod'; description='A globally unlimited specimen proving that global minus one overrides a zero player allowance.'; symbol='OWOP'; rarity='Legendary'; image="$imageRoot/rainbow-tree.png"; bearing=165; distance=185; quest='limits'; perm=$false; share=$true; global=-1; player=0; cooldown=15 },
    [ordered]@{ key='silver-lichen'; name='Silver Lichen'; description='An exclusive placement claim that allows its claiming avatar two collections.'; symbol='OWLI'; rarity='Mythic'; image="$imageRoot/lightning-tree.png"; bearing=185; distance=205; quest='limits'; perm=$false; share=$false; global=0; player=2; cooldown=5 },
    [ordered]@{ key='dormant-bulb'; name='Dormant Bulb'; description='A deliberately unavailable per-player zero-limit fixture; no portal should appear.'; symbol='OWDB'; rarity='Dormant'; image="$imageRoot/mycelium-tree.png"; bearing=215; distance=225; quest='eligibility'; perm=$false; share=$true; global=0; player=0; cooldown=0 },
    [ordered]@{ key='exhausted-cone'; name='Exhausted Cone'; description='A deliberately unavailable exclusive zero-limit fixture used to verify suppression and filtering.'; symbol='OWXC'; rarity='Exhausted'; image="$imageRoot/fruit-tree.png"; bearing=235; distance=245; quest='eligibility'; perm=$false; share=$false; global=0; player=0; cooldown=60 }
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
        $source = Invoke-OasisApi $Web4BaseUrl "nft/load-nft-by-id/$($saved.sourceNFTId)/MongoDBOASIS/false"
        $source = Invoke-OasisApi $Web4BaseUrl 'nft/update-web4-nft' 'Post' @{
            id=$saved.sourceNFTId; title=$fixture.name; description=$fixture.description; imageUrl=$fixture.image
            currentOwnerAvatarId=$avatar.id; mintedByAvatarId=$avatar.id
            metaData=@{ 'OurWorld.TestSuite'='quest-mode-spawn-matrix'; 'OurWorld.FixtureKey'=$fixture.key; 'OurWorld.Rarity'=$fixture.rarity; 'OurWorld.Category'='Nature' }
        }
        if ([string]$source.title -ne $fixture.name -or [string]$source.imageUrl -ne $fixture.image) {
            throw "Source NFT metadata reconciliation failed for $($fixture.name)."
        }
        if ([string]::IsNullOrWhiteSpace([string]$saved.geoNFTId)) {
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
        @{ key='in-order'; name='Celestial Garden: Follow the Sequence'; description='Collect Moonlit Reed, Prism Bloom and Verdant Starfruit in that exact order.'; order=1; fixtures=@($fixtures | Where-Object quest -eq 'ordered') },
        @{ key='respawn-any-order'; name='Renewal Cycle: Living Echoes'; description='Collect three renewable specimens in any order and observe their distinct cooldown rules.'; order=0; fixtures=@($fixtures | Where-Object quest -eq 'respawn') },
        @{ key='limits-in-order'; name='Custodians of Scarcity'; description='Follow the sequence through global precedence, unlimited global supply and an exclusive claim.'; order=1; fixtures=@($fixtures | Where-Object quest -eq 'limits') }
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
    $fixtureCount = @($manifest.fixtures | Where-Object { $_.key -in @($fixtures.key) }).Count
    if ($fixtureCount -ne $fixtures.Count) { throw "Expected $($fixtures.Count) fixture records; found $fixtureCount." }
    Write-Host "Seeded and verified $($fixtures.Count) GeoNFT fixtures and $($questDefinitions.Count) quests across both ordering modes. Manifest: $ManifestPath" -ForegroundColor Green
} finally {
    $headers.Clear(); $avatar=$null; $Credential=$null
}
