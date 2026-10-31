# Read-only protocol readiness probe. This is not a CQL/provider acceptance test.
[CmdletBinding()]
param()
$ErrorActionPreference = 'Stop'
$root = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
$results = Join-Path $root 'TestResults/ArbitrumPublicSourceCandidateCurrent'
New-Item -ItemType Directory -Force -Path $results | Out-Null
$endpoint = 'https://arb1.arbitrum.io/rpc'
$addresses = @(1..3 | ForEach-Object { '0x' + $_.ToString('x40') })
$client = [Net.Http.HttpClient]::new()
$client.Timeout = [TimeSpan]::FromSeconds(15)

function Invoke-PublicRpc($payload) {
    $body = [Net.Http.StringContent]::new(($payload | ConvertTo-Json -Depth 12 -Compress), [Text.Encoding]::UTF8, 'application/json')
    try {
        $request = [Net.Http.HttpRequestMessage]::new([Net.Http.HttpMethod]::Post, $endpoint)
        $request.Content = $body
        $response = $client.SendAsync($request, [Net.Http.HttpCompletionOption]::ResponseHeadersRead).GetAwaiter().GetResult()
        try {
            $response.EnsureSuccessStatusCode() | Out-Null
            $response.Content.LoadIntoBufferAsync(262144).GetAwaiter().GetResult() | Out-Null
            $bytes = $response.Content.ReadAsByteArrayAsync().GetAwaiter().GetResult()
            if ($bytes.Length -gt 262144) { throw 'RPC response exceeds the 256 KiB probe budget.' }
            return [Text.Encoding]::UTF8.GetString($bytes) | ConvertFrom-Json -Depth 16
        } finally { $response.Dispose(); $request.Dispose() }
    } finally { $body.Dispose() }
}
function Assert-Envelope($value, [int]$id) {
    if ($value.jsonrpc -ne '2.0' -or $value.id -ne $id -or $null -ne $value.error -or $null -eq $value.result) {
        throw 'RPC response identity/result differs; no readiness success recorded.'
    }
}
try {
    # Bind the network before interpreting native addresses or balances.
    $chain = Invoke-PublicRpc @{ jsonrpc='2.0'; id=1; method='eth_chainId'; params=@() }
    Assert-Envelope $chain 1
    if ($chain.result -cne '0xa4b1') { throw 'Expected Arbitrum mainnet chain ID.' }

    $genesis = Invoke-PublicRpc @{ jsonrpc='2.0'; id=8; method='eth_getBlockByNumber'; params=@('0x0', $false) }
    Assert-Envelope $genesis 8
    if ($genesis.result.hash -cnotmatch '^0x[0-9a-f]{64}$' -or $genesis.result.number -cne '0x0') {
        throw 'Canonical genesis identity required.'
    }

    # Resolve one finalized block, then pin every balance to its canonical hash.
    # false requests transaction hashes, never full transaction bodies.
    $header = Invoke-PublicRpc @{ jsonrpc='2.0'; id=2; method='eth_getBlockByNumber'; params=@('finalized', $false) }
    Assert-Envelope $header 2
    if ($header.result.hash -cnotmatch '^0x[0-9a-f]{64}$' -or $header.result.number -cnotmatch '^0x(?:0|[1-9a-f][0-9a-f]*)$') {
        throw 'Canonical finalized block identity required.'
    }
    $block = @{ blockHash=$header.result.hash; requireCanonical=$true }
    $selections = @()
    foreach ($count in @(3,2)) {
        $nextId = if ($count -eq 3) { 3 } else { 6 }
        $selected = @($addresses | Select-Object -First $count)
        $requests = @($selected | ForEach-Object {
            @{ jsonrpc='2.0'; id=$nextId++; method='eth_getBalance'; params=@($_, $block) }
        })
        $responses = @(Invoke-PublicRpc $requests)
        if ($responses.Count -ne $count -or @($responses.id | Select-Object -Unique).Count -ne $count) {
            throw 'Complete unique RPC batch responses required.'
        }
        $rows = @()
        for ($index=0; $index -lt $count; $index++) {
            $request = $requests[$index]
            $response = @($responses | Where-Object id -EQ $request.id)
            if ($response.Count -ne 1) { throw 'RPC batch response ID missing.' }
            Assert-Envelope $response[0] $request.id
            $hex = $response[0].result
            if ($hex -cnotmatch '^0x(?:0|[1-9a-f][0-9a-f]{0,63})$') { throw 'Canonical UInt256 balance required.' }
            $balance = [Numerics.BigInteger]::Parse('0' + $hex.Substring(2), [Globalization.NumberStyles]::AllowHexSpecifier)
            $rows += @{ address=$selected[$index]; balanceWei=$balance.ToString([Globalization.CultureInfo]::InvariantCulture) }
        }
        $selections += @{ selected=$count; rows=$rows }
    }
    $report = @{ schema=1; endpoint=$endpoint; chainId='0xa4b1'; genesisHash=$genesis.result.hash; blockHash=$header.result.hash;
        blockNumber=$header.result.number; selections=$selections; httpRequests=5; rpcOperations=8;
        cqlAcceptance=$false; hostedAcceptance=$false; writes=0 }
    $report | ConvertTo-Json -Depth 12 | Set-Content -LiteralPath (Join-Path $results 'native-metadata-probe.json') -Encoding utf8
    [pscustomobject]@{ Chain='Arbitrum mainnet'; NativeRows='3/2'; CanonicalHashPinned=$true; CqlVerified=$false; Writes=0 }
} finally { $client.Dispose() }
