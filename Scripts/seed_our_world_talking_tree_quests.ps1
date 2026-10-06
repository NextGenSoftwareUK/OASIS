<#
.SYNOPSIS
Seeds park talking-tree quests independently from the Anorak startup quest.
#>
[CmdletBinding()]
param(
    [string]$Web4BaseUrl='https://dev.api.web4.oasisomniverse.one',
    [string]$Web5BaseUrl='https://dev.api.starnet.oasisomniverse.one',
    [string]$CredentialPath=(Join-Path $env:LOCALAPPDATA 'OASIS/our-world-geonft-seed.credential.clixml'),
    [double]$Latitude=31.54998,
    [double]$Longitude=74.27728,
    [switch]$PlanOnly
)
$ErrorActionPreference='Stop'; Set-StrictMode -Version Latest
$suite='talking-tree-quests'
$definitions=@(
    @{
        key='oak-seeds'; hotSpotName='Oak Guardian Tree'; questName='Oak Guardian: Gather the Fallen Seeds'
        lat=$Latitude; long=$Longitude; item='seed:oak'; count=5; karma=60; xp=90
        greeting='The old oak stirs. Five of my seeds were scattered nearby. Please return them to me.'
        objective='Gather five fallen oak seeds'; audio='oasis://our-world/oak-tree-seed-request'
        completion='You found every fallen seed. The next generation of oaks is safe, and you have completed my quest. Thank you, guardian.'
        completionAudio='oasis://our-world/oak-tree-quest-complete'
    },
    @{
        key='park-litter'; hotSpotName='Park Steward Tree'; questName='Park Steward: Clear the Litter'
        lat=($Latitude+0.00035); long=($Longitude+0.00035); item='litter:park'; count=3; karma=100; xp=70
        greeting='A park tree wakes and asks for help clearing litter from beneath its canopy.'
        objective='Collect three pieces of park litter'; audio='oasis://our-world/tree-litter-request'
        completion='The litter is gone and the soil can breathe again. You completed my quest and restored this little corner of the park.'
        completionAudio='oasis://our-world/tree-litter-quest-complete'
    }
)
if($PlanOnly){$definitions|ConvertTo-Json -Depth 10;return}
function Unwrap($r){if($null-eq$r.PSObject.Properties['isError']){$r=$r.result};if($null-eq$r-or$r.isError){throw "API operation failed: $($r.message)"};$r.result}
function Value($object,[string]$name){if($null-eq$object){return $null};$property=$object.PSObject.Properties[$name];if($null-eq$property){return $null};$property.Value}
$credential=Import-Clixml $CredentialPath;$login=@{username=$credential.UserName;password=$credential.GetNetworkCredential().Password}|ConvertTo-Json
try{$avatar=Unwrap(Invoke-RestMethod "$Web4BaseUrl/api/avatar/authenticate" -Method Post -ContentType application/json -Body $login)}finally{$login=$null;$credential=$null}
$headers=@{Authorization="Bearer $($avatar.jwtToken)"}
function Api($path,$method='Get',$body=$null){$a=@{Uri="$($Web5BaseUrl.TrimEnd('/'))/api/$path";Headers=$headers;Method=$method;TimeoutSec=180};if($null-ne$body){$a.ContentType='application/json';$a.Body=$body|ConvertTo-Json -Depth 60};Unwrap(Invoke-RestMethod @a)}
try{
    $hotSpots=@(Api 'geohotspots')
    $quests=@(Api 'quests/all-for-avatar/game')
    foreach($definition in $definitions){
        $hotSpotBody=@{
            name=$definition.hotSpotName;description=$definition.greeting;lat=$definition.lat;long=$definition.long
            triggerType=2;timeInSecondsNeedToLookAt3DObjectOr2DImageToTriggerHotSpot=3
            hotSpotRadiusInMetres=80;boundaryType='Circle';boundaryLatitudes=@();boundaryLongitudes=@()
            recognitionTargetKey='our-world-tree-v1';recognitionTargetClass='Tree';minimumRecognitionConfidence=0.65
            allowOtherPlayersToAlsoCollect=$true;permSpawn=$true;globalSpawnQuantity=-1;playerSpawnQuantity=-1
            respawnDurationInSeconds=3;isVisibleOnMap=$true
            metaData=@{'OurWorld.TestSuite'=$suite;'OurWorld.TalkingTreeKey'=$definition.key;'GeoHotSpotType'='Map'}
        }
        $matchingHotSpots=@($hotSpots|Where-Object {$_.name-eq$definition.hotSpotName-or(Value (Value $_ 'metaData') 'OurWorld.TalkingTreeKey')-eq$definition.key})
        if($matchingHotSpots.Count-gt1){throw "Duplicate talking-tree hotspot '$($definition.hotSpotName)'."}
        $hotSpot=if($matchingHotSpots.Count-eq1){Api "geohotspots/$($matchingHotSpots[0].id)" 'Put' $hotSpotBody}else{Api 'geohotspots' 'Post' $hotSpotBody}
        $requirements=@($definition.item)*[int]$definition.count
        $objective=@{
            id=[Guid]::NewGuid().ToString();order=0;title=$definition.objective;description=$definition.greeting;gameSource='Our World'
            linkedGeoHotSpotId="$($hotSpot.id)";rewardKarma=[int]($definition.karma/2);rewardXP=[int]($definition.xp/2)
            needToCollectItems=@{'Our World'=$requirements}
            crossGameEventsOnActivate=@(
                @{eventType='ShowNarration';targetGame='Our World';narrationText=$definition.greeting},
                @{eventType='PlayAudio';targetGame='Our World';audioUrl=$definition.audio;audioTitle=$definition.hotSpotName}
            )
            crossGameEventsOnGeoHotSpotTriggered=@(
                @{eventType='ShowNarration';targetGame='Our World';narrationText=$definition.greeting}
            )
            crossGameEventsOnComplete=@(
                @{eventType='ShowNarration';targetGame='Our World';narrationText=$definition.completion},
                @{eventType='PlayAudio';targetGame='Our World';audioUrl=$definition.completionAudio;audioTitle="$($definition.hotSpotName) thanks you"},
                @{eventType='PlayAnimation';targetGame='Our World';animationKey='objective-complete';narrationText=$definition.objective},
                @{eventType='PlayAnimation';targetGame='Our World';animationKey='quest-complete';narrationText=$definition.questName}
            )
        }
        $questBody=@{
            name=$definition.questName;description=$definition.greeting;gameSource='Our World';status=1
            linkedGeoHotSpotId="$($hotSpot.id)";rewardKarma=$definition.karma;rewardXP=$definition.xp
            objectives=@($objective);metaData=@{'OurWorld.TestSuite'=$suite;'OurWorld.TalkingTreeKey'=$definition.key}
        }
        $matchingQuests=@($quests|Where-Object {$_.name-eq$definition.questName-or(Value (Value $_ 'metaData') 'OurWorld.TalkingTreeKey')-eq$definition.key})
        if($matchingQuests.Count-gt1){throw "Duplicate talking-tree quest '$($definition.questName)'."}
        $quest=if($matchingQuests.Count-eq1){Api "quests/$($matchingQuests[0].id)" 'Put' $questBody}else{Api 'quests' 'Post' $questBody}
        Write-Host "Seeded $($definition.hotSpotName) -> $($quest.name)"
    }
    foreach($legacy in @($hotSpots|Where-Object {$_.name-eq'Anorak Talking Tree'-or(Value (Value $_ 'metaData') 'OurWorld.TalkingTreeKey')-eq'anorak-tree-v1'})){
        $null=Api "geohotspots/$($legacy.id)" 'Delete'
        Write-Host "Removed legacy Anorak talking-tree hotspot $($legacy.id)"
    }
}finally{$headers.Clear();$avatar=$null}
