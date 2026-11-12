# Read-only .NET transport probe. No signing, bodies, storage scans or provisioning.
[CmdletBinding()]
param()
$ErrorActionPreference = 'Stop'
$root = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
$results = Join-Path $root 'TestResults/PolkadotPublicSourceCandidateCurrent'
New-Item -ItemType Directory -Force -Path $results | Out-Null
$endpoint = 'https://rpc.polkadot.io/'
$client = [Net.Http.HttpClient]::new()
$client.Timeout = [TimeSpan]::FromSeconds(15)
$observations = [Collections.Generic.List[object]]::new()

function Invoke-HeaderRpc([string]$method, [object[]]$parameters) {
    $requestId = $observations.Count + 1
    $payload = @{ jsonrpc='2.0'; id=$requestId; method=$method; params=$parameters } | ConvertTo-Json -Compress -Depth 8
    $request = [Net.Http.HttpRequestMessage]::new([Net.Http.HttpMethod]::Post,$endpoint)
    $request.Content = [Net.Http.StringContent]::new($payload,[Text.Encoding]::UTF8,'application/json')
    try {
        $response = $client.SendAsync($request,[Net.Http.HttpCompletionOption]::ResponseHeadersRead).GetAwaiter().GetResult()
        try {
            $response.EnsureSuccessStatusCode() | Out-Null
            $response.Content.LoadIntoBufferAsync(262144).GetAwaiter().GetResult() | Out-Null
            $bytes = $response.Content.ReadAsByteArrayAsync().GetAwaiter().GetResult()
            if ($bytes.Length -gt 262144) { throw 'Response exceeds256KiB.' }
            $observations.Add(@{method=$method;bytes=$bytes.Length;sha256=[Convert]::ToHexString([Security.Cryptography.SHA256]::HashData($bytes))})
            $reply = [Text.Encoding]::UTF8.GetString($bytes) | ConvertFrom-Json -Depth 16
            if ($reply.jsonrpc -cne '2.0' -or $reply.id -ne $requestId -or $null -ne $reply.error -or $null -eq $reply.result) {
                throw 'RPC envelope differs.'
            }
            return $reply.result
        } finally { $response.Dispose() }
    } finally { $request.Dispose() }
}

$report = @{ endpoint=$endpoint; utc=[DateTime]::UtcNow.ToString('O'); scope='readiness only, no CQL or independent finality proof'; requests=$observations }
try {
    $genesis = Invoke-HeaderRpc 'chain_getBlockHash' @(0)
    if ($genesis -cne '0x91b171bb158e2d3848fa23a9f1c25182fb8e20313b2c1eb49219da7a70ce90c3') { throw 'Polkadot genesis differs.' }
    $report.genesis = $genesis
    $selected = Invoke-HeaderRpc 'chain_getFinalizedHead' @()
    $report.finalizedHead = $selected
    $rows = [Collections.Generic.List[object]]::new()
    for ($index=0; $index -lt 3; $index++) {
        if ($selected -cnotmatch '^0x[0-9a-f]{64}$') { throw 'Canonical block hash required.' }
        $header = Invoke-HeaderRpc 'chain_getHeader' @($selected)
        $rows.Add(@{hash=$selected;header=$header})
        $selected = $header.parentHash
    }
    $report.records = $rows
    $report.passed = $true
} catch {
    $report.passed = $false
    $report.error = $_.Exception.Message
} finally { $client.Dispose() }
$report | ConvertTo-Json -Depth 20 | Set-Content (Join-Path $results 'headers-dotnet.json') -Encoding utf8
if (!$report.passed) { throw 'Native header readiness failed; preserve evidence.' }
Write-Output 'Five bounded .NET RPC reads passed; independently validate SCALE header hashes before acceptance.'
