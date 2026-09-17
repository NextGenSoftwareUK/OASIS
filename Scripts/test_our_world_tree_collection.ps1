<#
.SYNOPSIS
Exercise the four/five Anorak pickups on the development APIs, then leave a clean test run.
.DESCRIPTION
Explicit opt-in via -Apply. Uses the same scoped reset before and after the test.
Requires the GeoNFT collection-rules deployment. Does not verify Unity visuals.
#>
[CmdletBinding()]
param([switch]$Apply,
    [string]$Web4BaseUrl='https://dev.api.web4.oasisomniverse.one',
    [string]$Web5BaseUrl='https://dev.api.starnet.oasisomniverse.one',
    [string]$CredentialPath=(Join-Path $env:LOCALAPPDATA 'OASIS/our-world-geonft-seed.credential.clixml'))
$ErrorActionPreference='Stop'
if (!$Apply) { throw 'This collects demo trees and resets their data afterward. Use -Apply to execute.' }
function Unwrap($r) {
    if ($null -eq $r.PSObject.Properties['isError']) {$r=$r.result}
    if ($null -eq $r.PSObject.Properties['isError'] -or $r.isError) {throw "API failure: $($r.message)"}
    return $r.result
}
$credential=Import-Clixml $CredentialPath
$body=@{username=$credential.UserName;password=$credential.GetNetworkCredential().Password}|ConvertTo-Json
try {$avatar=Unwrap (Invoke-RestMethod "$Web4BaseUrl/api/avatar/authenticate" -Method Post -ContentType application/json -Body $body)}
finally {$body=$null;$credential=$null}
$headers=@{Authorization="Bearer $($avatar.jwtToken)"}
function Request($base,$path,$method='Get',$data=$null) {
    $args=@{Uri="$base/api/$path";Method=$method;Headers=$headers;TimeoutSec=90}
    if ($null -ne $data) {$args.ContentType='application/json';$args.Body=ConvertTo-Json -InputObject $data -Depth 40}
    Invoke-RestMethod @args
}
function Api($base,$path,$method='Get',$data=$null) {Unwrap (Request $base $path $method $data)}
$resetNeeded=$false
try {
    $list=@(Api $Web5BaseUrl 'quests/all-for-avatar/game')
    $quests=@($list|Where-Object startupSequence -eq 'anorak-trees')
    if ($quests.Count -ne 1) {throw 'Expected exactly one Anorak quest.'}
    $quest=Api $Web5BaseUrl "quests/$($quests[0].id)"
    $ids=@($quest.objectives|ForEach-Object {$_.needToCollectItems.'Our World'}|ForEach-Object {
        if ($_ -notmatch '^geonft:([0-9a-f-]{36})$') {throw 'Unexpected objective requirement'}
        $Matches[1]
    })
    if ($ids.Count -notin @(4,5)) {throw 'Expected four/five GeoNFT objectives.'}
    $null=Api $Web4BaseUrl 'nft/geo-nft-collection-status' 'Post' $ids
    & "$PSScriptRoot/reset_our_world_tree_progress.ps1" -Apply -Web4BaseUrl $Web4BaseUrl -Web5BaseUrl $Web5BaseUrl -CredentialPath $CredentialPath
    $resetNeeded=$true
    $all=@(Api $Web4BaseUrl 'nft/load-all-geo-nfts/MongoDBOASIS/false')
    $canonical=@($all|Where-Object {$_.id -in $ids})
    $demo=@($all|Where-Object {
        $n=$_
        $n.metaData.'OurWorld.DemoSeed' -eq 'true' -and
        @($canonical|Where-Object {[Math]::Abs($_.lat-$n.lat) -lt 0.000000001 -and [Math]::Abs($_.long-$n.long) -lt 0.000000001}).Count -gt 0
    })
    if (@($ids|Where-Object {$_ -notin $demo.id}).Count) {throw 'Only tagged demo placements may be changed.'}
    foreach ($nft in $demo) {
        $saved=Api $Web4BaseUrl "nft/geo-nft/$($nft.id)" 'Put' @{
            permSpawn=$false;allowOtherPlayersToAlsoCollect=$true;globalSpawnQuantity=0;playerSpawnQuantity=1;respawnDurationInSeconds=60
        }
        if ($saved.permSpawn -or !$saved.allowOtherPlayersToAlsoCollect -or $saved.globalSpawnQuantity -ne 0 -or $saved.playerSpawnQuantity -ne 1 -or $saved.respawnDurationInSeconds -ne 60) {throw 'Rule update did not round-trip.'}
    }
    Write-Host "Verified once-per-player settings on $($demo.Count) demo placements."
    $before=@(Api $Web4BaseUrl 'nft/geo-nft-collection-status' 'Post' $ids)
    if ($before.Count -ne $ids.Count -or @($before|Where-Object {!$_.canCollect -or $_.playerCollectionCount -ne 0}).Count) {throw 'Fresh placements are not all eligible.'}
    $count=0
    foreach ($id in $ids) {
        $request=@{geoNFTId=$id;collectedByAvatarId=$avatar.id;itemType='Nature';gameSource='Our World';quantity=1;stack=$true}
        $item=Api $Web4BaseUrl 'nft/collect-geo-nft' 'Post' $request
        if ($item.geoNFTId -ne $id -or $item.quantity -ne 1 -or $item.itemType -ne 'Nature') {throw 'Pickup did not create the expected Nature inventory row.'}
        $duplicate=Request $Web4BaseUrl 'nft/collect-geo-nft' 'Post' $request
        if (!$duplicate.isError) {throw 'Once-per-player duplicate was accepted.'}
        $inventory=@(Api $Web4BaseUrl 'avatar/inventory')
        $matching=@($inventory|Where-Object geoNFTId -eq $id)
        if ($matching.Count -ne 1 -or $matching[0].quantity -ne 1) {throw 'Duplicate attempt changed inventory.'}
        $progress=Api $Web5BaseUrl "quests/$($quest.id)/inventory-progress" 'Post' @{}
        $count++
        if (@($progress.quest.objectives|Where-Object isCompleted).Count -ne $count) {throw 'Pickup did not advance exactly one objective.'}
        $events=@($progress.crossGameEventsToDispatch)
        $objectiveEvents=@($events|Where-Object {$_.eventType -eq 'PlayAnimation' -and $_.animationKey -eq 'objective-complete'})
        if ($objectiveEvents.Count -ne 1) {throw "Expected one objective animation event, got $($objectiveEvents.Count)."}
        $finalEvents=@($events|Where-Object {$_.eventType -eq 'PlayAnimation' -and $_.animationKey -eq 'quest-complete'})
        $expectedFinal=if ($count -eq $ids.Count) {1} else {0}
        if ($finalEvents.Count -ne $expectedFinal) {throw 'Quest-complete event count is incorrect.'}
        $repeat=Api $Web5BaseUrl "quests/$($quest.id)/inventory-progress" 'Post' @{}
        if (@($repeat.crossGameEventsToDispatch|Where-Object {$null -ne $_}).Count -ne 0) {throw 'Unchanged reconciliation replayed events.'}
        Write-Host "PASS pickup $count/$($ids.Count): Nature inventory, duplicate rejected, one objective transition, no replay."
    }
    $after=@(Api $Web4BaseUrl 'nft/geo-nft-collection-status' 'Post' $ids)
    if (@($after|Where-Object {$_.canCollect -or $_.playerCollectionCount -ne 1}).Count) {throw 'Collected placements remained eligible.'}
    Write-Host 'PASS all Anorak API collection/progression checks. Unity visual verification is separate.'
} finally {
    $headers.Clear();$avatar=$null
    if ($resetNeeded) {
        & "$PSScriptRoot/reset_our_world_tree_progress.ps1" -Apply -Web4BaseUrl $Web4BaseUrl -Web5BaseUrl $Web5BaseUrl -CredentialPath $CredentialPath
    }
}
