[CmdletBinding()]
param([Parameter(Mandatory)][string]$FixturePath,[ValidateSet('Concurrency','RestartReplay')][string]$Mode='Concurrency')
$ErrorActionPreference='Stop'
$f=Get-Content -Raw $FixturePath|ConvertFrom-Json
function Trigger($base,$token,$key) {
    $headers=@{Authorization="Bearer $token"}
    $body=[ordered]@{idempotencyKey=$key;triggerType=$f.triggerType;observedAtUtc=[DateTime]::UtcNow.ToString('O');latitude=$f.latitude;longitude=$f.longitude;accuracyMetres=$f.accuracyMetres;continuousDurationSeconds=$f.continuousDurationSeconds;gameSource=$f.gameSource}|ConvertTo-Json
    try {
        $payload=Invoke-RestMethod "$base/api/geohotspots/$($f.hotSpotId)/trigger" -Method Post -Headers $headers -ContentType application/json -Body $body -TimeoutSec 90
        [pscustomobject]@{transportError=$false;statusCode=200;payload=$payload}
    }
    catch {
        $statusCode=if ($_.Exception.Response) {[int]$_.Exception.Response.StatusCode} else {0}
        if ($statusCode -eq 0) { return [pscustomobject]@{transportError=$true;statusCode=0;payload=$null;message=$_.Exception.Message} }
        $payload=if ($_.ErrorDetails.Message) { try {$_.ErrorDetails.Message|ConvertFrom-Json} catch {$null} } else {$null}
        [pscustomobject]@{transportError=$false;statusCode=$statusCode;payload=$payload;message=$_.Exception.Message}
    }
}
if ($Mode -eq 'Concurrency') {
    if (@($f.web5BaseUrls).Count -lt 2 -or @($f.jwtTokens).Count -lt 2) { throw 'Concurrency fixture requires at least two WEB5 URLs and two JWTs.' }
    $jobs=@()
    # Start-Job process startup is variable. Each worker announces readiness and
    # waits on one release marker so both replicas receive competing requests.
    $barrierPath=Join-Path ([IO.Path]::GetTempPath()) ("oasis-geohotspot-"+[Guid]::NewGuid().ToString('N'))
    New-Item -ItemType Directory -Path $barrierPath|Out-Null
    for($i=0;$i -lt 2;$i++) {
        $jobs+=Start-Job -ScriptBlock {
            param($base,$token,$key,$fixture,$barrierPath,$workerIndex)
            $headers=@{Authorization="Bearer $token"}
            $body=[ordered]@{idempotencyKey=$key;triggerType=$fixture.triggerType;observedAtUtc=[DateTime]::UtcNow.ToString('O');latitude=$fixture.latitude;longitude=$fixture.longitude;accuracyMetres=$fixture.accuracyMetres;continuousDurationSeconds=$fixture.continuousDurationSeconds;gameSource=$fixture.gameSource}|ConvertTo-Json
            New-Item -ItemType File -Path (Join-Path $barrierPath "ready-$workerIndex")|Out-Null
            while (!(Test-Path (Join-Path $barrierPath 'release'))) { Start-Sleep -Milliseconds 10 }
            try {
                $payload=Invoke-RestMethod "$base/api/geohotspots/$($fixture.hotSpotId)/trigger" -Method Post -Headers $headers -ContentType application/json -Body $body -TimeoutSec 90
                [pscustomobject]@{transportError=$false;statusCode=200;payload=$payload}
            }
            catch {
                $statusCode=if ($_.Exception.Response) {[int]$_.Exception.Response.StatusCode} else {0}
                $payload=if ($_.ErrorDetails.Message) { try {$_.ErrorDetails.Message|ConvertFrom-Json} catch {$null} } else {$null}
                [pscustomobject]@{transportError=($statusCode -eq 0);statusCode=$statusCode;payload=$payload;message=$_.Exception.Message}
            }
        } -ArgumentList $f.web5BaseUrls[$i % $f.web5BaseUrls.Count],$f.jwtTokens[$i],([Guid]::NewGuid()).ToString(),$f,$barrierPath,$i
    }
    try {
        $readyDeadline=[DateTime]::UtcNow.AddSeconds(30)
        while (@(Get-ChildItem -LiteralPath $barrierPath -Filter 'ready-*').Count -lt 2 -and [DateTime]::UtcNow -lt $readyDeadline) { Start-Sleep -Milliseconds 25 }
        if (@(Get-ChildItem -LiteralPath $barrierPath -Filter 'ready-*').Count -ne 2) { throw 'Both concurrency workers did not reach the release barrier within 30 seconds.' }
        New-Item -ItemType File -Path (Join-Path $barrierPath 'release')|Out-Null
        $responses=@($jobs|Wait-Job|Receive-Job)
    }
    finally {
        $jobs|Remove-Job -Force -ErrorAction SilentlyContinue
        Remove-Item -LiteralPath $barrierPath -Recurse -Force -ErrorAction SilentlyContinue
    }
    if ($responses.Count -ne 2) { throw "Expected two trigger responses, got $($responses.Count)." }
    if (@($responses|Where-Object {$_.transportError}).Count -gt 0) { throw 'A replica was unreachable during the concurrency test.' }
    $accepted=@($responses|Where-Object {$_.statusCode -eq 200 -and $_.payload.result -and !$_.payload.isError})
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
    try {
        $payload=Invoke-RestMethod "$base/api/geohotspots/$($fixture.hotSpotId)/trigger" -Method Post -Headers $headers -ContentType application/json -Body $body -TimeoutSec 90
        [pscustomobject]@{transportError=$false;statusCode=200;payload=$payload}
    }
    catch {
        $statusCode=if ($_.Exception.Response) {[int]$_.Exception.Response.StatusCode} else {0}
        $payload=if ($_.ErrorDetails.Message) { try {$_.ErrorDetails.Message|ConvertFrom-Json} catch {$null} } else {$null}
        [pscustomobject]@{transportError=($statusCode -eq 0);statusCode=$statusCode;payload=$payload;message=$_.Exception.Message}
    }
} -ArgumentList $f.web5BaseUrls[0],$f.jwtTokens[0],$key,$f
Start-Sleep -Milliseconds $(if ($f.interruptDelayMilliseconds) {[int]$f.interruptDelayMilliseconds} else {250})
& ([scriptblock]::Create([string]$f.restartStopCommand))
$first=$requestJob|Wait-Job|Receive-Job; $requestJob|Remove-Job -Force
if (!$first.transportError) {
    if ($first.statusCode -eq 200 -and $first.payload.result) { throw 'The request committed before interruption; reduce interruptDelayMilliseconds or use a slower disposable provider.' }
    throw "The first request returned HTTP $($first.statusCode) instead of being interrupted. Fix the fixture/authentication before testing recovery."
}
& ([scriptblock]::Create([string]$f.restartStartCommand))
$ready=$false; $deadline=[DateTime]::UtcNow.AddSeconds(120); do { try {$null=Invoke-WebRequest "$($f.web5BaseUrls[0])/api/health" -TimeoutSec 2; $ready=$true; break}catch{Start-Sleep 1} } while([DateTime]::UtcNow -lt $deadline)
if (!$ready) { throw 'The disposable WEB5 instance did not become healthy within 120 seconds after restart.' }
$replay=Trigger $f.web5BaseUrls[0] $f.jwtTokens[0] $key
if ($replay.transportError -or $replay.statusCode -ne 200 -or $replay.payload.isError -or !$replay.payload.result) { throw 'Replay did not commit the pending operation.' }
$again=Trigger $f.web5BaseUrls[0] $f.jwtTokens[0] $key
if ($again.transportError -or $again.statusCode -ne 200 -or $again.payload.result.idempotencyKey -ne $replay.payload.result.idempotencyKey -or $again.payload.result.globalTriggerCount -ne $replay.payload.result.globalTriggerCount) { throw 'Committed replay was not idempotent.' }
Write-Host 'PASS restart/replay committed once.'
