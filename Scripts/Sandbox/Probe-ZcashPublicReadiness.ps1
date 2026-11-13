#Requires -Version 7.0
# First qualification of the selected public v1summary endpoint. Never retry.
$ErrorActionPreference = 'Stop'
$root = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
$state = Join-Path $root 'LocalState/COSMIC/ZcashPublicQualificationCurrent'
$results = Join-Path $root 'TestResults/ZcashPublicSourceCurrent'
New-Item -ItemType Directory -Force -Path $results | Out-Null
$endpoint = 'https://api.zecblock.com'
$requests = @(
    @{ path='/v1/blocks?min_height=0&max_height=0&limit=1&order=oldest'; heights='0'; genesis=$true },
    @{ path='/v1/blocks?min_height=1&max_height=3&limit=3&order=oldest'; heights='1,2,3'; genesis=$false },
    @{ path='/v1/blocks?min_height=1&max_height=2&limit=2&order=oldest'; heights='1,2'; genesis=$false }
)
function Write-NewJson([string]$path, $value) {
    $stream = [IO.File]::Open($path, [IO.FileMode]::CreateNew)
    try { $stream.Write([Text.Encoding]::UTF8.GetBytes(($value | ConvertTo-Json -Depth 20))); $stream.Flush($true) }
    finally { $stream.Dispose() }
}
$intent = @{ endpoint=$endpoint; sourceCommit='116b8a72f4d0aac6bafeecac34422246a75cc8ed'; maxRequests=3;
    byteLimit=262144; timeoutSeconds=15; nativeWrites=0; utc=[DateTime]::UtcNow.ToString('O') }
Write-NewJson (Join-Path $state 'qualification.intent.json') $intent
$handler = [Net.Http.HttpClientHandler]::new()
$handler.AllowAutoRedirect = $false
$client = [Net.Http.HttpClient]::new($handler)
$client.Timeout = [TimeSpan]::FromSeconds(15)
$report = @{ passed=$false; requests=@(); observations=@(); nativeWrites=0; proofVerified=$false; cqlAcceptance=$false }
try {
    foreach ($selection in $requests) {
        if ($report.requests.Count) { Start-Sleep -Seconds 1 }
        $attempt = @{ path=$selection.path; id=$report.requests.Count+1 }
        $report.requests += $attempt
        $cancel = [Threading.CancellationTokenSource]::new([TimeSpan]::FromSeconds(15))
        try {
            $response = $client.GetAsync($endpoint+$selection.path,[Net.Http.HttpCompletionOption]::ResponseHeadersRead,$cancel.Token).GetAwaiter().GetResult()
            try {
                $attempt.httpStatus = [int]$response.StatusCode
                $stream = $response.Content.ReadAsStreamAsync($cancel.Token).GetAwaiter().GetResult()
                $memory = [IO.MemoryStream]::new()
                try {
                    $buffer = [byte[]]::new(8192)
                    while (($count=$stream.ReadAsync($buffer,0,$buffer.Length,$cancel.Token).GetAwaiter().GetResult()) -gt 0) {
                        if ($memory.Length+$count -gt 262144) { throw 'Native byte budget exceeded.' }
                        $memory.Write($buffer,0,$count)
                    }
                    $raw = $memory.ToArray()
                } finally { $stream.Dispose(); $memory.Dispose() }
                $attempt.bytes = $raw.Length
                $attempt.sha256 = [Convert]::ToHexString([Security.Cryptography.SHA256]::HashData($raw)).ToLowerInvariant()
                $path = Join-Path $results ('response-'+$attempt.id+'.json')
                $file = [IO.File]::Open($path,[IO.FileMode]::CreateNew)
                try { $file.Write($raw); $file.Flush($true) } finally { $file.Dispose() }
                if ($attempt.httpStatus -ne 200) { throw 'Native HTTP read denied/failed; preserve response, no retry.' }
            } finally { $response.Dispose() }
        } finally { $cancel.Dispose() }
        $arguments = @((Join-Path $PSScriptRoot 'Validate-ZcashPublicReadiness.py'),$path,'--heights',$selection.heights)
        if ($selection.genesis) { $arguments += '--genesis' }
        $validated = & python @arguments
        if ($LASTEXITCODE) { throw 'Native response validation failed; no subsequent request.' }
        $report.observations += ($validated | ConvertFrom-Json)
    }
    $report.passed = $true
}
catch { $report.errorType=$_.Exception.GetType().Name; $report.error=$_.Exception.Message }
finally {
    $client.Dispose()
    Write-NewJson (Join-Path $state 'qualification.json') $report
    Write-NewJson (Join-Path $results 'qualification.json') $report
}
@{passed=$report.passed;requests=$report.requests.Count;observations=$report.observations.Count} | ConvertTo-Json -Compress
if (!$report.passed) { exit 1 }
