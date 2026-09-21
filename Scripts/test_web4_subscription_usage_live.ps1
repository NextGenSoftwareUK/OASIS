param(
    [Parameter(Mandatory=$true)][string]$BaseUrl,
    [Parameter(Mandatory=$true)][string]$BearerToken,
    [string]$SecondBearerToken
)
$ErrorActionPreference = 'Stop'
$headers = @{ Authorization = "Bearer $BearerToken" }

function Invoke-JsonPost([string]$Path, [object]$Body, [hashtable]$RequestHeaders = $headers) {
    Invoke-RestMethod -Method Post -Uri ($BaseUrl.TrimEnd('/') + $Path) -Headers $RequestHeaders -ContentType 'application/json' -Body ($Body | ConvertTo-Json -Depth 8)
}
function Expect-HttpError([int]$Status, [scriptblock]$Action) {
    try { & $Action; throw "Expected HTTP $Status but the request succeeded." }
    catch {
        if ($_.Exception.Response.StatusCode.value__ -ne $Status) { throw }
    }
}

$op = [guid]::NewGuid().ToString()
$authorization = @{
    operationId=$op; consumingService='WEB6'; endpoint='POST /v1/complete';
    meterCategory='completion'; requestedUnits=1; estimatedCostUsd=0.001
}
$first = Invoke-JsonPost '/api/subscription/usage/authorize' $authorization
$repeat = Invoke-JsonPost '/api/subscription/usage/authorize' $authorization
if (-not $first.allowed -or $repeat.operationId -ne $op) { throw 'Idempotent authorization failed.' }

$changed = $authorization.Clone(); $changed.endpoint = 'POST /v1/images'
Expect-HttpError 409 { Invoke-JsonPost '/api/subscription/usage/authorize' $changed }
if ($SecondBearerToken) {
    Expect-HttpError 409 { Invoke-JsonPost '/api/subscription/usage/authorize' $authorization @{ Authorization="Bearer $SecondBearerToken" } }
}

$settlement = @{
    operationId=$op; outcome='succeeded'; provider='TestProvider'; model='test-model';
    promptTokens=7; completionTokens=3; units=10; estimatedCostUsd=0.001;
    actualCostUsd=0.0009; costSource='provider'; pricingCatalogueVersion='live-test-v1'
}
$settled = Invoke-JsonPost '/api/subscription/usage/settle' $settlement
$settledAgain = Invoke-JsonPost '/api/subscription/usage/settle' $settlement
if (-not $settled.settled -or -not $settledAgain.alreadySettled) { throw 'Idempotent settlement failed.' }
$changedSettlement = $settlement.Clone(); $changedSettlement.completionTokens = 4
Expect-HttpError 409 { Invoke-JsonPost '/api/subscription/usage/settle' $changedSettlement }

foreach ($outcome in @('failed','cancelled')) {
    $caseOp = [guid]::NewGuid().ToString()
    $caseAuth = $authorization.Clone(); $caseAuth.operationId = $caseOp
    Invoke-JsonPost '/api/subscription/usage/authorize' $caseAuth | Out-Null
    $caseSettle = $settlement.Clone(); $caseSettle.operationId = $caseOp; $caseSettle.outcome = $outcome
    Invoke-JsonPost '/api/subscription/usage/settle' $caseSettle | Out-Null
}

$invalidService = $authorization.Clone(); $invalidService.operationId=[guid]::NewGuid().ToString(); $invalidService.consumingService='WEB11'
Expect-HttpError 400 { Invoke-JsonPost '/api/subscription/usage/authorize' $invalidService }
$negative = $authorization.Clone(); $negative.operationId=[guid]::NewGuid().ToString(); $negative.requestedUnits=-1
Expect-HttpError 400 { Invoke-JsonPost '/api/subscription/usage/authorize' $negative }

$current = Invoke-RestMethod -Uri ($BaseUrl.TrimEnd('/') + '/api/subscription/usage/current') -Headers $headers
$events = Invoke-RestMethod -Uri ($BaseUrl.TrimEnd('/') + '/api/subscription/usage/events?limit=20') -Headers $headers
if (-not $current.planId -or @($events).Count -lt 3) { throw 'Usage readback did not contain the expected ledger data.' }

# Concurrent unique operation IDs must all commit without lost aggregate increments.
$before = [long]$current.monthlyRequests
$jobs = 1..8 | ForEach-Object {
    $body = $authorization.Clone(); $body.operationId=[guid]::NewGuid().ToString()
    Start-ThreadJob -ScriptBlock {
        param($url,$h,$b)
        Invoke-RestMethod -Method Post -Uri $url -Headers $h -ContentType 'application/json' -Body ($b | ConvertTo-Json)
    } -ArgumentList ($BaseUrl.TrimEnd('/') + '/api/subscription/usage/authorize'),$headers,$body
}
$results = $jobs | Receive-Job -Wait -AutoRemoveJob
if (@($results | Where-Object allowed).Count -ne 8) { throw 'Concurrent authorization matrix failed.' }
$after = Invoke-RestMethod -Uri ($BaseUrl.TrimEnd('/') + '/api/subscription/usage/current') -Headers $headers
if ([long]$after.monthlyRequests -ne $before + 8) { throw 'Concurrent aggregate increment lost one or more operations.' }

Write-Output 'WEB4 subscription usage live matrix passed.'
