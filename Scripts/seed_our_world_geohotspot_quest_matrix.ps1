<#
.SYNOPSIS
Seeds repeatable Our World GeoHotSpot quests covering the complete end-to-end contract.
.DESCRIPTION
Creates twelve tagged hotspots and three quests. The Cartesian 1,536-case policy
matrix remains automated; these fixtures provide playable representatives for every
trigger, reward, ordering, content/event, visibility, placement and spawn-policy dimension.
#>
[CmdletBinding()]
param(
    [string]$Web4BaseUrl='https://dev.api.web4.oasisomniverse.one',
    [string]$Web5BaseUrl='https://dev.api.starnet.oasisomniverse.one',
    [string]$CredentialPath=(Join-Path $env:LOCALAPPDATA 'OASIS/our-world-geonft-seed.credential.clixml'),
    [string]$ManifestPath=(Join-Path $env:LOCALAPPDATA 'OASIS/our-world-geohotspot-quest-matrix.json'),
    [string]$GeoNFTManifestPath=(Join-Path $env:LOCALAPPDATA 'OASIS/our-world-quest-test-matrix.json'),
    [double]$Latitude=31.54998,
    [double]$Longitude=74.27728,
    [switch]$PlanOnly
)
$ErrorActionPreference='Stop'; Set-StrictMode -Version Latest
$suite='geohotspot-end-to-end-matrix'
$imageRoot='https://raw.githubusercontent.com/NextGenSoftwareUK/OASIS/Development/Docs/Assets/OurWorld/TreeBigIcons'
$cases=@(
 @{key='arrival-map';name='Violet Arrival Gate';group='triggers';trigger='WhenArrivedAtGeoLocation';content='Map';bearing=25;distance=270;radius=20;perm=$false;share=$true;global=0;player=1;cooldown=0;safe=$true;near=$false;visible=$true},
 @{key='dwell-text';name='Lavender Dwell Archive';group='triggers';trigger='WhenAtGeoLocationForXSeconds';content='Text';bearing=55;distance=290;radius=18;dwell=8;perm=$true;share=$true;global=0;player=1;cooldown=20;safe=$true;near=$false;visible=$true},
 @{key='gaze-ar';name='Amethyst Gaze Beacon';group='triggers';trigger='WhenLookingAtObjectOrImageForXSecondsInARMode';content='AR';bearing=85;distance=310;radius=15;gaze=5;perm=$true;share=$true;global=0;player=1;cooldown=0;safe=$false;near=$false;visible=$true},
 @{key='touch-ir';name='Orchid Touch Sigil';group='triggers';trigger='WhenObjectOrImageIsTouchedInARMode';content='IR';bearing=115;distance=330;radius=15;perm=$false;share=$true;global=0;player=2;cooldown=0;safe=$false;near=$false;visible=$true},
 @{key='audio-inventory';name='Plum Resonance Cache';group='rewards';trigger='WhenArrivedAtGeoLocation';content='Audio';bearing=145;distance=270;radius=20;perm=$false;share=$true;global=0;player=1;cooldown=0;safe=$true;near=$false;visible=$true;inventory=0},
 @{key='video-geonft';name='Indigo Memory Well';group='rewards';trigger='WhenAtGeoLocationForXSeconds';content='Video';bearing=175;distance=290;radius=18;dwell=5;perm=$false;share=$true;global=3;player=1;cooldown=0;safe=$true;near=$false;visible=$true;geonft=0},
 @{key='website-multi';name='Purple Crossroads Cache';group='rewards';trigger='WhenObjectOrImageIsTouchedInARMode';content='WebsiteLink';bearing=205;distance=310;radius=15;perm=$false;share=$true;global=-1;player=0;cooldown=10;safe=$true;near=$false;visible=$true;inventory=1;geonft=1},
 @{key='vr-events';name='Ultraviolet Event Nexus';group='rewards';trigger='WhenLookingAtObjectOrImageForXSecondsInARMode';content='VR';bearing=235;distance=330;radius=15;gaze=4;perm=$false;share=$false;global=0;player=2;cooldown=5;safe=$false;near=$false;visible=$true;inventory=2},
 @{key='global-precedence';name='Royal Global Reserve';group='policies';trigger='WhenArrivedAtGeoLocation';content='Map';bearing=265;distance=270;radius=20;perm=$false;share=$true;global=2;player=5;cooldown=0;safe=$true;near=$false;visible=$true},
 @{key='player-unlimited';name='Mauve Personal Spring';group='policies';trigger='WhenArrivedAtGeoLocation';content='Map';bearing=295;distance=290;radius=20;perm=$false;share=$true;global=0;player=-1;cooldown=10;safe=$true;near=$false;visible=$true},
 @{key='exclusive-near';name='Heather Exclusive Portal';group='policies';trigger='WhenArrivedAtGeoLocation';content='Map';bearing=325;distance=310;radius=20;perm=$false;share=$false;global=0;player=1;cooldown=0;safe=$false;near=$true;visible=$true},
 @{key='permanent-delayed';name='Magenta Renewal Portal';group='policies';trigger='WhenAtGeoLocationForXSeconds';content='Text';bearing=355;distance=330;radius=18;dwell=3;perm=$true;share=$true;global=0;player=1;cooldown=30;safe=$true;near=$true;visible=$true}
)
function Point($bearing,$distance){$r=[Math]::PI/180;$lat=$Latitude*$r;$lon=$Longitude*$r;$h=$bearing*$r;$a=$distance/6371008.8;$nlat=[Math]::Asin([Math]::Sin($lat)*[Math]::Cos($a)+[Math]::Cos($lat)*[Math]::Sin($a)*[Math]::Cos($h));$nlon=$lon+[Math]::Atan2([Math]::Sin($h)*[Math]::Sin($a)*[Math]::Cos($lat),[Math]::Cos($a)-[Math]::Sin($lat)*[Math]::Sin($nlat));@{lat=[Math]::Round($nlat/$r,8);long=[Math]::Round((($nlon/$r+540)%360)-180,8)}}
if($PlanOnly){$cases|ConvertTo-Json -Depth 10;return}
function Unwrap($r){if($null-eq$r.PSObject.Properties['isError']){$r=$r.result};if($null-eq$r-or$r.isError){throw "API operation failed: $($r.message)"};$r.result}
$credential=Import-Clixml $CredentialPath;$login=@{username=$credential.UserName;password=$credential.GetNetworkCredential().Password}|ConvertTo-Json
try{$avatar=Unwrap(Invoke-RestMethod "$Web4BaseUrl/api/avatar/authenticate" -Method Post -ContentType application/json -Body $login)}finally{$login=$null;$credential=$null}
$headers=@{Authorization="Bearer $($avatar.jwtToken)"}
function Api($base,$path,$method='Get',$body=$null){$a=@{Uri="$($base.TrimEnd('/'))/api/$path";Headers=$headers;Method=$method;TimeoutSec=180};if($null-ne$body){$a.ContentType='application/json';$a.Body=$body|ConvertTo-Json -Depth 80};Unwrap(Invoke-RestMethod @a)}
$manifest=[ordered]@{version=1;avatarId="$($avatar.id)";suite=$suite;hotspots=@();quests=@();rewards=@()}
if(Test-Path $ManifestPath){$manifest=Get-Content $ManifestPath -Raw|ConvertFrom-Json;if("$($manifest.avatarId)"-ne"$($avatar.id)"){throw 'Manifest belongs to another avatar.'}}
function SaveManifest{New-Item -ItemType Directory -Force (Split-Path $ManifestPath)|Out-Null;$manifest|ConvertTo-Json -Depth 40|Set-Content $ManifestPath -Encoding UTF8}
function CaseValue($case,[string]$key,$defaultValue){if($case.ContainsKey($key)){return $case[$key]};return $defaultValue}
function TriggerValue([string]$name){switch($name){'WhenArrivedAtGeoLocation'{0}'WhenAtGeoLocationForXSeconds'{1}'WhenLookingAtObjectOrImageForXSecondsInARMode'{2}'WhenObjectOrImageIsTouchedInARMode'{3}default{throw "Unknown trigger type '$name'."}}}
try{
 if(!(Test-Path $GeoNFTManifestPath)){throw 'Seed the GeoNFT quest matrix first; its tagged GeoNFTs are the reward fixtures.'}
 $geoManifest=Get-Content $GeoNFTManifestPath -Raw|ConvertFrom-Json;$geoRewards=@($geoManifest.fixtures|Where-Object geoNFTId|Select-Object -First 2)
 if($geoRewards.Count-ne2){throw 'Two GeoNFT reward fixtures are required.'}
 while(@($manifest.rewards).Count-lt3){$i=@($manifest.rewards).Count;$reward=Api $Web5BaseUrl 'inventoryitems' 'Post' @{name=@('Violet Access Shard','Amethyst Field Key','Purple Signal Crystal')[$i];description='Tagged GeoHotSpot matrix reward.';quantity=1;stack=$true;gameSource='Our World';itemType='QuestItem';rarity=@('Uncommon','Rare','Epic')[$i];metaData=@{'OurWorld.TestSuite'=$suite}};$manifest.rewards+=@{id="$($reward.id)";name=$reward.name};SaveManifest}
 $all=@(Api $Web5BaseUrl 'geohotspots')
 foreach($c in $cases) {
   $saved=@($manifest.hotspots|Where-Object key -eq $c.key)|Select-Object -First 1
   if($null-ne$saved) {
     $persisted=Api $Web5BaseUrl "geohotspots/$($saved.id)"
     if($null-ne$persisted){continue}
     $manifest.hotspots=@($manifest.hotspots|Where-Object key -ne $c.key)
     SaveManifest
   }
   $existing=@($all|Where-Object name -eq $c.name)
   if($existing.Count-gt1){throw "Duplicate hotspot $($c.name)"}
   if($existing.Count-eq1){$hot=$existing[0]}
   else {
     $p=Point $c.bearing $c.distance
     $body=@{
       name=$c.name; description="$($c.content) fixture for $($c.trigger)."; lat=$p.lat; long=$p.long
       triggerType=(TriggerValue $c.trigger); hotSpotRadiusInMetres=$c.radius
       timeInSecondsNeedToBeAtLocationToTriggerHotSpot=(CaseValue $c 'dwell' 0)
       timeInSecondsNeedToLookAt3DObjectOr2DImageToTriggerHotSpot=(CaseValue $c 'gaze' 0)
       allowOtherPlayersToAlsoCollect=$c.share; permSpawn=$c.perm
       globalSpawnQuantity=$c.global; playerSpawnQuantity=$c.player; respawnDurationInSeconds=$c.cooldown
       spawnInSafeZone=$c.safe; spawnNearPlayer=$c.near; spawnWithinXMetersFromPlayer=80
       spawnXMetersAwayFromPlayer=$c.distance; isVisibleOnMap=$c.visible
       image2DURI="$imageRoot/mycelium-tree.png"
       textContent="GeoHotSpot matrix: $($c.content)"
       websiteUrl='https://oasisweb4.one'
       metaData=@{'OurWorld.TestSuite'=$suite;'OurWorld.FixtureKey'=$c.key;'OurWorld.ContentType'=$c.content;'GeoHotSpotType'=$c.content}
     }
     $inventoryIndex=CaseValue $c 'inventory' $null
     $geoNFTIndex=CaseValue $c 'geonft' $null
     if($null-ne$inventoryIndex){$body.rewardIds=@("$($manifest.rewards[$inventoryIndex].id)")}
     if($null-ne$geoNFTIndex){$body.geoNFTRewardIds=@("$($geoRewards[$geoNFTIndex].geoNFTId)")}
     $hot=Api $Web5BaseUrl 'geohotspots' 'Post' $body
   }
   $manifest.hotspots+=@{key=$c.key;id="$($hot.id)";name=$c.name;group=$c.group}
   SaveManifest
 }
 $eventTypes=@('ShowNarration','ShowImage','PlayAudio','PlayVideo','OpenWebsite','PlayAnimation','SpawnEntity','UnlockPortal','TeleportTo')
 $questRows=@(Api $Web5BaseUrl 'quests/all-for-avatar/game')
 foreach($group in 'triggers','rewards','policies') {
   $title=switch($group){'triggers'{'GeoHotSpot Signals: Four Ways In'}'rewards'{'GeoHotSpot Rewards: Purple Protocol'}default{'GeoHotSpot Limits: Shared Ground'}}
   $existing=@($questRows|Where-Object name -eq $title)
   if($existing.Count-gt1){throw "Duplicate quest '$title'."}
   $members=@($manifest.hotspots|Where-Object group -eq $group)
   $objectives=@();$index=0
   foreach($h in $members) {
     $events=@()
     if($group-eq'rewards') {
       for($e=$index*3;$e-lt[Math]::Min($index*3+3,$eventTypes.Count);$e++) {
         $events+=@{eventType=$eventTypes[$e];targetGame='Our World';narrationText="$($eventTypes[$e]) fixture";imageUrl="$imageRoot/rainbow-tree.png";audioUrl='https://oasisweb4.one';videoUrl='https://oasisweb4.one';websiteUrl='https://oasisweb4.one';animationKey='objective-complete';entityClassname='GeoHotSpotFixture';spawnCount=1;portalId='fixture';targetMap='UnityWorldSpace'}
       }
     }
     $objectives+=@{title="Trigger $($h.name)";description="Complete the $group fixture $($h.name).";gameSource='Our World';order=$index;linkedGeoHotSpotId=$h.id;dictionaries=@{needToGoToGeoHotSpots=@{'Our World'=@("$($h.id)")}};crossGameEventsOnGeoHotSpotTriggered=$events}
     $index++
   }
   $order=if($group-eq'triggers'){0}else{1}
   $questBody=@{name=$title;description="Playable $group coverage for the GeoHotSpot API.";gameSource='Our World';status=1;createdByAvatarId=$avatar.id;objectiveCompletionOrder=$order;objectives=$objectives;metaData=@{'OurWorld.TestSuite'=$suite;'OurWorld.TestGroup'=$group}}
   $quest=if($existing.Count-eq1){Api $Web5BaseUrl "quests/$($existing[0].id)" 'Put' $questBody}else{Api $Web5BaseUrl 'quests' 'Post' $questBody}
   $manifest.quests=@($manifest.quests|Where-Object group -ne $group)
   $manifest.quests+=@{group=$group;id="$($quest.id)";name=$title};SaveManifest
 }
 foreach($fixture in $manifest.hotspots) {
   $verifiedHotSpot=Api $Web5BaseUrl "geohotspots/$($fixture.id)"
   if($null-eq$verifiedHotSpot){throw "GeoHotSpot verification failed for '$($fixture.name)': the API returned no persisted record."}
   $verifiedId=$verifiedHotSpot.PSObject.Properties['id']
   if($null-eq$verifiedId -or [string]$verifiedId.Value-ne[string]$fixture.id){throw "GeoHotSpot verification failed for '$($fixture.name)': $(ConvertTo-Json $verifiedHotSpot -Depth 4 -Compress)"}
 }
 foreach($fixtureQuest in $manifest.quests) {
   $verifiedQuest=Api $Web5BaseUrl "quests/$($fixtureQuest.id)"
   $expectedIds=@($manifest.hotspots|Where-Object group -eq $fixtureQuest.group|ForEach-Object {[string]$_.id})
   $actualIds=@($verifiedQuest.objectives|ForEach-Object {[string]$_.linkedGeoHotSpotId})
   if($actualIds.Count-ne4 -or @($expectedIds|Where-Object {$_ -notin $actualIds}).Count-ne0) {
     throw "Quest verification failed for '$($fixtureQuest.name)': linked GeoHotSpot objectives do not match the manifest."
   }
 }
 Write-Host "Seeded $($manifest.hotspots.Count) GeoHotSpots, $($manifest.rewards.Count) rewards and $($manifest.quests.Count) quests. Manifest: $ManifestPath"
}finally{$headers.Clear();$avatar=$null}
