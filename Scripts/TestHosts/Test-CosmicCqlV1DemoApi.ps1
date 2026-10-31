#Requires -Version 7.0
param([string]$BaseUrl = 'http://127.0.0.1:5088')

$ErrorActionPreference = 'Stop'
if ($BaseUrl -ne 'http://127.0.0.1:5088') { throw 'Use only the explicitly owned loopback demo.' }

$session = Invoke-RestMethod "$BaseUrl/demo/session"
$checks = [Collections.Generic.List[string]]::new()

function Query([object]$Body, [string]$Actor = 'Alice', [string]$Key = [guid]::NewGuid().ToString('D')) {
    $headers = @{ 'Idempotency-Key' = $Key }
    if ($Actor -ne 'Anonymous') { $headers.Authorization = 'Bearer ' + $session.tokens.$Actor }

    Invoke-WebRequest "$BaseUrl/api/data/query-cql" -Method Post -Headers $headers `
        -ContentType 'application/json' -Body ($Body | ConvertTo-Json -Depth 15) -SkipHttpErrorCheck
}

function CheckRows([object]$Response, [string[]]$Expected) {
    if ($Response.StatusCode -ne 200) { throw "Expected successful native query: $($Response.StatusCode)" }
    $rows = @((ConvertFrom-Json $Response.Content).result.rows | ForEach-Object name)
    if (($rows -join '|') -ne ($Expected -join '|')) { throw "Wrong native rows: $($rows -join ',')" }
    if ($Response.Headers['X-OASIS-Usage-Settlement'] -ne 'settled') { throw 'Response not durably settled.' }
}

$page = Invoke-WebRequest "$BaseUrl/demo"
if (!$page.Content.Contains('font: 14px/1.8') -or !$page.Content.Contains('height: 560px')) { throw 'Readable layout contract missing.' }
$swagger = Invoke-RestMethod "$BaseUrl/swagger/v1/swagger.json"
$operation = $swagger.paths.'/api/data/query-cql'.post
if (!($operation.parameters | Where-Object name -eq 'Idempotency-Key')) { throw 'Swagger omitted required idempotency header.' }
if ($swagger.components.schemas.CqlV1Request.example.provider -ne 'MongoDBOASIS') { throw 'Swagger example missing.' }
$checks.Add('readable-page-and-working-swagger-contract')

foreach ($provider in @('MongoDBOASIS', 'SQLLiteDBOASIS', 'Neo4jOASIS')) {
    $body = @{ provider = $provider; skip = 1; take = 1 }
    $key = [guid]::NewGuid().ToString('D')
    CheckRows (Query $body 'Alice' $key) @('C-public')
    if ((Query $body 'Alice' $key).StatusCode -ne 409) { throw 'Duplicate must not execute again.' }
    $checks.Add("$provider-visibility-page-and-no-replay")

    CheckRows (Query @{ provider = $provider; take = 10; filters = @(
        @{ metadataPath = @('precision'); scalar = 'Integer'; operator = 'Equal'; value = [long]9007199254740993 }
    ) }) @('B-owned', 'C-public', 'D-owned')
    $checks.Add("$provider-exact-int64")

    CheckRows (Query @{ provider = $provider; take = 10; filters = @(
        @{ field = 'Name'; operator = 'StartsWith'; value = 'B-' },
        @{ field = 'Name'; operator = 'EndsWith'; value = 'owned' }
    ) }) @('B-owned')
    $checks.Add("$provider-ordinal-strings")
}

CheckRows (Query @{ take = 10 } 'Visitor') @('C-public')
CheckRows (Query @{ take = 10 } 'Bob') @('A-index', 'A-private', 'C-public')
if ((Query @{} 'Anonymous').StatusCode -ne 401) { throw 'Anonymous query must reject.' }
$checks.Add('visitor-bob-and-anonymous-identities')

$taskRoot = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
@{ status = 'passed'; checks = $checks; count = $checks.Count; tokensRecorded = $false } |
    ConvertTo-Json -Depth 5 | Set-Content -LiteralPath (Join-Path $taskRoot 'TestResults/CosmicCqlDemo/v1-http-checks.json')
Write-Host "PASS: $($checks.Count) demo contract checks."
