[CmdletBinding()]
param([Parameter(Mandatory)][string]$FixturePath,[ValidateSet('Concurrency','RestartReplay')][string]$Mode='Concurrency')
$ErrorActionPreference='Stop'
$f=Get-Content -Raw $FixturePath|ConvertFrom-Json
function Trigger($base,$token,$key) {
    $headers=@{Authorization="Bearer $token"}
    $body=[ordered]@{idempotencyKey=$key;triggerType=$f.triggerType;observedAtUtc=[DateTime]::UtcNow.ToString('O');latitude=$f.latitude;longitude=$f.longitude;accuracyMetres=$f.accuracyMetres;continuousDurationSeconds=$f.continuousDurationSeconds;gameSource=$f.gameSource}|ConvertTo-Json
    try { Invoke-RestMethod "$base/api/geohotspots/$($f.hotSpotId)/trigger" -Method Post -Headers $headers -ContentType application/json -Body $body -TimeoutSec 90 }
    catch { if ($_.ErrorDetails.Message) { $_.ErrorDetails.Message|ConvertFrom-Json } else { throw } }
}
if ($Mode -eq 'Concurrency') {
    if (@($f.web5BaseUrls).Count -lt 2 -or @($f.jwtTokens).Count -lt 2) { throw 'Concurrency fixture requires at least two WEB5 URLs and two JWTs.' }
    $jobs=@()
    for($i=0;$i -lt 2;$i++) {
        $jobs+=Start-Job -ScriptBlock {
            param($base,$token,$key,$fixture)
            $headers=@{Authorization="Bearer $token"}
            $body=[ordered]@{idempotencyKey=$key;triggerType=$fixture.triggerType;observedAtUtc=[DateTime]::UtcNow.ToString('O');latitude=$fixture.latitude;longitude=$fixture.longitude;accuracyMetres=$fixture.accuracyMetres;continuousDurationSeconds=$fixture.continuousDurationSeconds;gameSource=$fixture.gameSource}|ConvertTo-Json
            try { Invoke-RestMethod "$base/api/geohotspots/$($fixture.hotSpotId)/trigger" -Method Post -Headers $headers -ContentType application/json -Body $body -TimeoutSec 90 }
            catch { if ($_.ErrorDetails.Message) { $_.ErrorDetails.Message|ConvertFrom-Json } else { throw } }
        } -ArgumentList $f.web5BaseUrls[$i % $f.web5BaseUrls.Count],$f.jwtTokens[$i],([Guid]::NewGuid()).ToString(),$f
    }
    $responses=@($jobs|Wait-Job|Receive-Job); $jobs|Remove-Job -Force
    $accepted=@($responses|Where-Object {$_.result -and !$_.isError})
    if ($accepted.Count -ne [int]$f.expectedAcceptedCount) { throw "Expected $($f.expectedAcceptedCount) accepted triggers, got $($accepted.Count)." }
    Write-Host "PASS synchronized two-avatar trigger: $($accepted.Count) accepted."
    return
}
if (!$f.restartStopCommand -or !$f.restartStartCommand) { throw 'RestartReplay fixture requires restartStopCommand and restartStartCommand.' }
$key=([Guid]::NewGuid()).ToString()
$requestJob=Start-Job -ScriptBlock {
    param($base,$token,$key,$fixture)
    $headers=@{Authorization="Bearer $token"}
    $body=[ordered]@{idempotencyKey=$key;triggerType=$fixture.triggerType;observedAtUtc=[DateTime]::UtcNow.ToString('O');latitude=$fixture.latitude;longitude=$fixture.longitude;accuracyMetres=$fixture.accuracyMetres;continuousDurationSeconds=$fixture.continuousDurationSeconds;gameSource=$fixture.gameSource}|ConvertTo-Json
    try { Invoke-RestMethod "$base/api/geohotspots/$($fixture.hotSpotId)/trigger" -Method Post -Headers $headers -ContentType application/json -Body $body -TimeoutSec 90 }
    catch { if ($_.ErrorDetails.Message) { $_.ErrorDetails.Message|ConvertFrom-Json } else { [pscustomobject]@{isError=$true;message=$_.Exception.Message} } }
} -ArgumentList $f.web5BaseUrls[0],$f.jwtTokens[0],$key,$f
Start-Sleep -Milliseconds $(if ($f.interruptDelayMilliseconds) {[int]$f.interruptDelayMilliseconds} else {250})
& ([scriptblock]::Create([string]$f.restartStopCommand))
$first=$requestJob|Wait-Job|Receive-Job; $requestJob|Remove-Job -Force
if (!$first.isError -and $first.result) { throw 'The request committed before interruption; reduce interruptDelayMilliseconds or use a slower disposable provider.' }
& ([scriptblock]::Create([string]$f.restartStartCommand))
$deadline=[DateTime]::UtcNow.AddSeconds(120); do { try {$null=Invoke-WebRequest "$($f.web5BaseUrls[0])/api/health" -TimeoutSec 2;break}catch{Start-Sleep 1} } while([DateTime]::UtcNow -lt $deadline)
$replay=Trigger $f.web5BaseUrls[0] $f.jwtTokens[0] $key
if ($replay.isError -or !$replay.result) { throw 'Replay did not commit the pending operation.' }
$again=Trigger $f.web5BaseUrls[0] $f.jwtTokens[0] $key
if ($again.result.idempotencyKey -ne $replay.result.idempotencyKey -or $again.result.globalTriggerCount -ne $replay.result.globalTriggerCount) { throw 'Committed replay was not idempotent.' }
Write-Host 'PASS restart/replay committed once.'
