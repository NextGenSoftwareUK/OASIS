#Requires -Version 7.0
# Original read-only .NET capture: no custom identity headers, retries or provisioning.
$ErrorActionPreference = 'Stop'
$root = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
$state = Join-Path $root 'LocalState/COSMIC/ThreeFoldPublicQualificationCurrent'
$results = Join-Path $root 'TestResults/ThreeFoldPublicSourceCurrent'
New-Item -ItemType Directory -Force $results | Out-Null
function Write-NewJson([string]$path,$value) {
    $file=[IO.File]::Open($path,[IO.FileMode]::CreateNew)
    try {$file.Write([Text.Encoding]::UTF8.GetBytes(($value|ConvertTo-Json -Depth 20)));$file.Flush($true)} finally {$file.Dispose()}
}
Write-NewJson (Join-Path $state 'qualification.intent.json') @{endpoint='https://gridproxy.grid.tf';sourceCommit='f8d1d856db5b13154d7262daa548aa81b38bbfe8';maxRequests=6;byteLimit=262144;timeoutSeconds=15;nativeWrites=0;utc=[DateTime]::UtcNow.ToString('O')}
$handler=[Net.Http.HttpClientHandler]::new();$handler.AllowAutoRedirect=$false;$handler.UseCookies=$false
$client=[Net.Http.HttpClient]::new($handler);$client.Timeout=[TimeSpan]::FromSeconds(15)
$report=@{passed=$false;requests=@();full=@();subset=@();nativeWrites=0;chainStateVerified=$false;cqlAcceptance=$false}
function Read-Native([string]$route,[string]$key) {
    if ($report.requests.Count) {Start-Sleep -Seconds 1}
    $attempt=@{path=$route;id=$report.requests.Count+1};$report.requests += $attempt
    $cancel=[Threading.CancellationTokenSource]::new([TimeSpan]::FromSeconds(15))
    try {
        $response=$client.GetAsync('https://gridproxy.grid.tf'+$route,[Net.Http.HttpCompletionOption]::ResponseHeadersRead,$cancel.Token).GetAwaiter().GetResult()
        try {
            $attempt.httpStatus=[int]$response.StatusCode
            $stream=$response.Content.ReadAsStreamAsync($cancel.Token).GetAwaiter().GetResult();$memory=[IO.MemoryStream]::new()
            try {
                $buffer=[byte[]]::new(8192)
                while (($count=$stream.ReadAsync($buffer,0,$buffer.Length,$cancel.Token).GetAwaiter().GetResult()) -gt 0) {
                    if ($memory.Length+$count -gt 262144) {throw 'Native byte budget exceeded'}
                    $memory.Write($buffer,0,$count)
                }
                $raw=$memory.ToArray()
            } finally {$stream.Dispose();$memory.Dispose()}
            $attempt.bytes=$raw.Length;$attempt.sha256=[Convert]::ToHexString([Security.Cryptography.SHA256]::HashData($raw)).ToLowerInvariant()
            $path=Join-Path $results ('response-'+$attempt.id+'.json')
            $file=[IO.File]::Open($path,[IO.FileMode]::CreateNew)
            try {$file.Write($raw);$file.Flush($true)} finally {$file.Dispose()}
            if ($attempt.httpStatus -ne 200) {throw 'Native HTTP denied/failed; remaining requests not sent'}
        } finally {$response.Dispose()}
    } finally {$cancel.Dispose()}
    $arguments=@((Join-Path $PSScriptRoot 'Validate-ThreeFoldPublicReadiness.py'),$path)
    if ($key) {$arguments += @('--ids',$key,'--single')}
    $parsed=& python @arguments
    if ($LASTEXITCODE) {throw 'Native schema/identity failed; remaining requests not sent'}
    return ($parsed|ConvertFrom-Json)
}
try {
    $catalogue=Read-Native '/nodes?page=1&size=3&sort_by=node_id&sort_order=asc&randomize=false' ''
    $keys=@($catalogue.rows|ForEach-Object {$_.nodeId})
    Write-NewJson (Join-Path $state 'discovered-keys.json') @{keys=$keys;policy='operator-reported-mainnet-proxy';independentChainProof=$false}
    foreach ($key in $keys) {$report.full += (Read-Native ('/nodes/'+$key) $key).rows}
    foreach ($key in $keys[0..1]) {$report.subset += (Read-Native ('/nodes/'+$key) $key).rows}
    $original=$catalogue.rows|ConvertTo-Json -Depth 10 -Compress
    if (($report.full|ConvertTo-Json -Depth 10 -Compress) -cne $original) {throw 'Stable discovery projection differs'}
    if (($report.full[0..1]|ConvertTo-Json -Depth 10 -Compress) -cne ($report.subset|ConvertTo-Json -Depth 10 -Compress)) {throw 'Subset projection differs'}
    $report.passed=$true
} catch {$report.errorType=$_.Exception.GetType().Name;$report.error=$_.Exception.Message}
finally {$client.Dispose();Write-NewJson (Join-Path $state 'qualification.json') $report;Write-NewJson (Join-Path $results 'qualification.json') $report}
@{passed=$report.passed;requests=$report.requests.Count;full=$report.full.Count;subset=$report.subset.Count}|ConvertTo-Json -Compress
if (!$report.passed) {exit 1}
