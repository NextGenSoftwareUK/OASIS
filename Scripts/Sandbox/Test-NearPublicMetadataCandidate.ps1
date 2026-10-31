# Read-only source feasibility, not CQL/provider/settlement acceptance.
[CmdletBinding()]
param()
$ErrorActionPreference = 'Stop'
$root = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
$results = Join-Path $root 'TestResults/NearPublicSourceCandidateCurrent'
New-Item -ItemType Directory -Force -Path $results | Out-Null
$endpoint = 'https://rpc.mainnet.near.org/'
$accounts = @('near','wrap.near','aurora')
$client = [Net.Http.HttpClient]::new()
$client.Timeout = [TimeSpan]::FromSeconds(15)
$observations = [Collections.Generic.List[object]]::new()

function Invoke-PublicNearRpc([string]$method, [object]$parameters) {
    $requestId = $observations.Count + 1
    $payload = @{ jsonrpc='2.0'; id=$requestId; method=$method; params=$parameters } | ConvertTo-Json -Depth 8 -Compress
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

$report = @{ endpoint=$endpoint; utc=[DateTime]::UtcNow.ToString('O'); scope='source readiness, not CQL or independent finality proof'; requests=$observations }
try {
    $status = Invoke-PublicNearRpc 'status' @()
    if ($status.chain_id -cne 'mainnet' -or $status.sync_info.syncing -ne $false) { throw 'Expected synced NEAR mainnet.' }
    $rows = [Collections.Generic.List[object]]::new()
    $anchor = $null
    foreach ($account in $accounts) {
        $parameters = @{request_type='view_account';account_id=$account}
        if ($null -eq $anchor) { $parameters.finality='final' } else { $parameters.block_id=$anchor.block_hash }
        $value = Invoke-PublicNearRpc 'query' $parameters
        if ($null -eq $anchor) { $anchor=$value }
        if ($value.block_hash -cne $anchor.block_hash -or $value.block_height -ne $anchor.block_height) { throw 'Pinned account block differs.' }
        foreach ($field in @('amount','locked')) {
            if ($value.$field -isnot [string] -or $value.$field -cnotmatch '^(?:0|[1-9][0-9]{0,38})$') { throw 'Canonical UInt128 amount string required.' }
            $quantity=[Numerics.BigInteger]::Parse($value.$field)
            if ($quantity -gt [Numerics.BigInteger]::Pow(2,128)-1) { throw 'Amount exceedsUInt128.' }
        }
        if ($value.block_hash -cnotmatch '^[1-9A-HJ-NP-Za-km-z]{32,44}$' -or $value.code_hash -cnotmatch '^[1-9A-HJ-NP-Za-km-z]{32,44}$') { throw 'Bounded base58 hashes required.' }
        $rows.Add(@{account=$account;amount=$value.amount;locked=$value.locked;storageUsage=$value.storage_usage;codeHash=$value.code_hash})
    }
    $report.records=$rows; $report.blockHash=$anchor.block_hash; $report.blockHeight=$anchor.block_height; $report.passed=$true
} catch { $report.passed=$false; $report.error=$_.Exception.Message }
finally { $client.Dispose() }
$report | ConvertTo-Json -Depth 16 | Set-Content (Join-Path $results 'readiness.json') -Encoding utf8
if (!$report.passed) { throw 'NEAR source readiness failed; preserve evidence.' }
Write-Output 'Four bounded NEAR RPC reads passed: three accounts at one reported final block; no writes.'
