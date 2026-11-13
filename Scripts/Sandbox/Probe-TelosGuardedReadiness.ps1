#Requires -Version 7.0
# Explicit shipping-transport qualification, not a runtime fallback.
param([switch]$ContinueAfterOutputFix)
$ErrorActionPreference = 'Stop'
$root = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
$state = Join-Path $root 'LocalState/COSMIC/HostedTelosPublicSourceCurrent'
$results = Join-Path $root 'TestResults/TelosPublicSourceCurrent/GuardedDotNet'
$prior = $null
if ($ContinueAfterOutputFix) {
    $prior = Get-Content (Join-Path $results 'qualification.json') -Raw -Encoding utf8 | ConvertFrom-Json
    $captured = Get-Content (Join-Path $results '1-eth_chainId.json') -Raw -Encoding utf8 | ConvertFrom-Json
    if ($prior.qualified -ne $false -or $prior.message -cne 'Telos mainnet identity differs.' -or $captured.result -cne '0x28' -or $captured.id -ne 1) { throw 'Original output-only probe failure required.' }
    $results = Join-Path $root 'TestResults/TelosPublicSourceCurrent/GuardedDotNetContinuation'
}
$intent = Join-Path $state $(if ($ContinueAfterOutputFix) { 'guarded-dotnet-continuation.intent.json' } else { 'guarded-dotnet-qualification.intent.json' })
if ((Test-Path -LiteralPath $intent) -or (Test-Path -LiteralPath (Join-Path $state 'source.json'))) {
    throw 'Original qualification/catalogue exists; reconcile rather than replay.'
}
$original = Get-Content (Join-Path $root 'TestResults/TelosPublicSourceCurrent/qualification.json') -Raw -Encoding utf8 | ConvertFrom-Json
if ($original.qualified -ne $false) { throw 'Preserved original observation required.' }
New-Item -ItemType Directory -Force -Path $results | Out-Null
$corePath = Join-Path $root 'OASIS Architecture/NextGenSoftware.OASIS.API.Core/bin/Debug/net10.0/NextGenSoftware.OASIS.API.Core.dll'
[Reflection.Assembly]::LoadFrom($corePath) | Out-Null
[NextGenSoftware.OASIS.API.Core.Objects.NativeData.NativeConnectionGuard]::AllowPrivateOverride = $false
$handler = [NextGenSoftware.OASIS.API.Core.Objects.NativeData.NativeConnectionGuard]::CreateGuardedHttpHandler()
$client = [Net.Http.HttpClient]::new($handler)
$client.Timeout = [TimeSpan]::FromSeconds(15)
$utf8 = [Text.UTF8Encoding]::new($false)
$stream = [IO.File]::Open($intent,[IO.FileMode]::CreateNew)
try {
    $bytes = $utf8.GetBytes('{"rpc":"https://rpc.telos.net/","transport":"shipping-guarded-dotnet","writes":0,"retryAllowed":false}')
    $stream.Write($bytes); $stream.Flush($true)
} finally { $stream.Dispose() }
$observations = [Collections.Generic.List[object]]::new()
if ($prior) { $observations.Add($prior.calls[0]) }
function Read-Rpc([string]$method, [object[]]$parameters) {
    $number = $observations.Count + 1
    $entry = @{method=$method; parameters=$parameters}
    $observations.Add($entry)
    $request = [Net.Http.HttpRequestMessage]::new([Net.Http.HttpMethod]::Post,'https://rpc.telos.net/')
    $request.Content = [Net.Http.StringContent]::new((@{jsonrpc='2.0';id=$number;method=$method;params=$parameters}|ConvertTo-Json -Depth 8 -Compress),$utf8,'application/json')
    try {
        $response = $client.SendAsync($request,[Net.Http.HttpCompletionOption]::ResponseHeadersRead).GetAwaiter().GetResult()
        try {
            $entry.status = [int]$response.StatusCode
            $response.Content.LoadIntoBufferAsync(262144).GetAwaiter().GetResult() | Out-Null
            $raw = $response.Content.ReadAsByteArrayAsync().GetAwaiter().GetResult()
            $entry.bytes=$raw.Length; $entry.sha256=[Convert]::ToHexString([Security.Cryptography.SHA256]::HashData($raw)).ToLowerInvariant()
            [IO.File]::WriteAllBytes((Join-Path $results ($number.ToString()+'-'+$method+'.json')),$raw)
            if ($entry.status -ne 200) { throw 'Native HTTP status rejected.' }
            $parsed=$utf8.GetString($raw)|ConvertFrom-Json -Depth 16
            if ($parsed.jsonrpc -cne '2.0' -or $parsed.id -ne $number -or $null -ne $parsed.error -or $null -eq $parsed.result) { throw 'Native envelope/result rejected.' }
            return $parsed.result
        } finally { $response.Dispose() }
    } finally { $request.Dispose() }
}
$report=@{rpc='https://rpc.telos.net/';qualified=$false;transport='shipping-guarded-dotnet';writes=0;cqlAcceptance=$false;calls=$observations}
try {
    if (!$ContinueAfterOutputFix -and (Read-Rpc 'eth_chainId' @()) -cne '0x28') { throw 'Telos mainnet identity differs.' }
    $genesis=Read-Rpc 'eth_getBlockByNumber' @('0x0',$false)
    if ($genesis.number -cne '0x0' -or $genesis.hash -cnotmatch '^0x[0-9a-f]{64}$') { throw 'Canonical native genesis required.' }
    $header=Read-Rpc 'eth_getBlockByNumber' @('finalized',$false)
    if ($header.hash -cnotmatch '^0x[0-9a-f]{64}$' -or $header.number -cnotmatch '^0x(?:0|[1-9a-f][0-9a-f]*)$') { throw 'Canonical finalized header required.' }
    $addresses=@(1..3 | ForEach-Object { '0x'+$_.ToString('x40') })
    $balances=@(foreach($address in $addresses) {
        $value=Read-Rpc 'eth_getBalance' @($address,@{blockHash=$header.hash;requireCanonical=$true})
        if ($value -cnotmatch '^0x(?:0|[1-9a-f][0-9a-f]{0,63})$') { throw 'Canonical UInt256 balance required.' }
        $value
    })
    $report.qualified=$true; $report.genesisHash=$genesis.hash; $report.blockHash=$header.hash
    $report.blockNumber=$header.number; $report.balances=$balances
    $source=@{schema=1;rpcUrl='https://rpc.telos.net/';chainId='0x28';addresses=$addresses;sourceOwner=[Guid]::NewGuid().ToString()}
    [IO.File]::WriteAllText((Join-Path $state 'source.json'),($source|ConvertTo-Json -Depth 5),$utf8)
} catch { $report.qualified=$false; $report.failure=$_.Exception.GetType().FullName; $report.message=$_.Exception.Message }
finally { $client.Dispose() }
[IO.File]::WriteAllText((Join-Path $results 'qualification.json'),($report|ConvertTo-Json -Depth 12),$utf8)
$report|ConvertTo-Json -Depth 12 -Compress
if (!$report.qualified) { throw 'Guarded qualification failed; no retry, fallback or provider grant.' }
