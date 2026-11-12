#Requires -Version 7.0
# Read-only native qualification over the three catalogue-verified Ethereum feeds.
# Never creates a source catalogue until every identity and ABI check succeeds.
$ErrorActionPreference = 'Stop'
$root = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
$state = Join-Path $root 'LocalState/COSMIC/HostedChainLinkPublicSourceCurrent'
$results = Join-Path $root 'TestResults/ChainLinkPublicSourceCurrent'
$catalogue = Get-Content -LiteralPath (Join-Path $state 'selected-catalogue.json') -Raw -Encoding utf8 | ConvertFrom-Json -Depth 20
$catalogueBytes = [IO.File]::ReadAllBytes((Join-Path $results 'mainnet-catalogue.json'))
if ([Convert]::ToHexString([Security.Cryptography.SHA256]::HashData($catalogueBytes)).ToLowerInvariant() -cne $catalogue.catalogueSha256) { throw 'Original catalogue differs.' }
if ($catalogue.feeds.Count -ne 3 -or (Test-Path -LiteralPath (Join-Path $state 'source.json'))) { throw 'Three original feeds and no prior source required.' }
$rpc = 'https://ethereum-rpc.publicnode.com/'
$utf8 = [Text.UTF8Encoding]::new($false,$true)
$stream = [IO.File]::Open((Join-Path $state 'native-qualification.intent.json'),[IO.FileMode]::CreateNew)
try {
    $stream.Write($utf8.GetBytes((@{rpc=$rpc;catalogueSha256=$catalogue.catalogueSha256;sourceWrites=0;retryAllowed=$false}|ConvertTo-Json -Compress)))
    $stream.Flush($true)
} finally { $stream.Dispose() }
$core = Join-Path $root 'OASIS Architecture/NextGenSoftware.OASIS.API.Core/bin/Debug/net10.0/NextGenSoftware.OASIS.API.Core.dll'
[Reflection.Assembly]::LoadFrom($core) | Out-Null
[NextGenSoftware.OASIS.API.Core.Objects.NativeData.NativeConnectionGuard]::AllowPrivateOverride = $false
$client = [Net.Http.HttpClient]::new([NextGenSoftware.OASIS.API.Core.Objects.NativeData.NativeConnectionGuard]::CreateGuardedHttpHandler())
$client.Timeout = [TimeSpan]::FromSeconds(15)
$calls = [Collections.Generic.List[object]]::new()

function Read-Rpc([string]$method, [object[]]$parameters) {
    $number = $calls.Count + 1
    $entry = @{ method=$method;parameters=$parameters }
    $calls.Add($entry)
    $request = [Net.Http.HttpRequestMessage]::new([Net.Http.HttpMethod]::Post,$rpc)
    $request.Content = [Net.Http.StringContent]::new((@{jsonrpc='2.0';id=$number;method=$method;params=$parameters}|ConvertTo-Json -Depth 8 -Compress),$utf8,'application/json')
    try {
        $response = $client.SendAsync($request,[Net.Http.HttpCompletionOption]::ResponseHeadersRead).GetAwaiter().GetResult()
        try {
            $entry.status=[int]$response.StatusCode
            $response.Content.LoadIntoBufferAsync(262144).GetAwaiter().GetResult() | Out-Null
            $raw=$response.Content.ReadAsByteArrayAsync().GetAwaiter().GetResult()
            $entry.bytes=$raw.Length;$entry.sha256=[Convert]::ToHexString([Security.Cryptography.SHA256]::HashData($raw)).ToLowerInvariant()
            $output=[IO.File]::Open((Join-Path $results ($number.ToString()+'-'+$method+'.json')),[IO.FileMode]::CreateNew)
            try { $output.Write($raw);$output.Flush($true) } finally { $output.Dispose() }
            if ($entry.status -ne 200) { throw 'Native HTTP status rejected.' }
            $parsed=$utf8.GetString($raw)|ConvertFrom-Json -Depth 16 -DateKind String
            if ($parsed.jsonrpc -cne '2.0' -or $parsed.id -ne $number -or $null -ne $parsed.error -or $null -eq $parsed.result) { throw 'Native envelope/result rejected.' }
            return $parsed.result
        } finally { $response.Dispose() }
    } finally { $request.Dispose() }
}

function Unsigned([string]$hex) {
    return [Numerics.BigInteger]::Parse('0'+$hex,[Globalization.NumberStyles]::AllowHexSpecifier,[Globalization.CultureInfo]::InvariantCulture)
}

function Read-Description([string]$encoded) {
    if ($encoded -cnotmatch '^0x[0-9a-f]+$' -or ($encoded.Length-2)%64 -ne 0 -or $encoded.Length -lt 130) { throw 'Canonical ABI string required.' }
    $hex=$encoded.Substring(2)
    if ((Unsigned $hex.Substring(0,64)) -ne 32) { throw 'Canonical ABI string offset required.' }
    $length=Unsigned $hex.Substring(64,64)
    if ($length -lt 1 -or $length -gt 128 -or $hex.Length -ne 128+([Math]::Ceiling([double]$length/32)*64)) { throw 'Bounded complete ABI string required.' }
    $bytes=[Convert]::FromHexString($hex.Substring(128))
    for ($i=[int]$length;$i -lt $bytes.Length;$i++) { if ($bytes[$i] -ne 0) { throw 'ABI string padding differs.' } }
    return $utf8.GetString($bytes,0,[int]$length)
}

$report=@{rpc=$rpc;qualified=$false;cqlAcceptance=$false;sourceWrites=0;policy='rpc-reported-finalized';calls=$calls}
try {
    if ((Read-Rpc 'eth_chainId' @()) -cne '0x1') { throw 'Ethereum chain identity differs.' }
    $genesis=Read-Rpc 'eth_getBlockByNumber' @('0x0',$false)
    if ($genesis.hash -cne '0xd4e56740f876aef8c010b86a40d5f56745a118d0906a34e69aec8c0db1cb8fa3' -or $genesis.number -cne '0x0') { throw 'Ethereum genesis differs.' }
    $header=Read-Rpc 'eth_getBlockByNumber' @('finalized',$false)
    if ($header.hash -cnotmatch '^0x[0-9a-f]{64}$' -or $header.timestamp -cnotmatch '^0x(?:0|[1-9a-f][0-9a-f]*)$' -or $header.number -cnotmatch '^0x(?:0|[1-9a-f][0-9a-f]*)$') { throw 'Canonical finalized header required.' }
    $report.blockHash=$header.hash;$report.blockNumber=$header.number
    $blockTime=Unsigned $header.timestamp.Substring(2)
    $selections=@(foreach ($count in @(3,2)) {
        $rows=@(foreach ($feed in @($catalogue.feeds | Select-Object -First $count)) {
            $address=$feed.proxyAddress.ToLowerInvariant()
            if ($address -cnotmatch '^0x[0-9a-f]{40}$') { throw 'Canonical feed address required.' }
            $block=@{blockHash=$header.hash;requireCanonical=$true}
            $description=Read-Description (Read-Rpc 'eth_call' @(@{to=$address;data='0x7284e416'},$block))
            if ($description -cne $feed.name) { throw 'Native feed description differs from official catalogue.' }
            $decimals=Read-Rpc 'eth_call' @(@{to=$address;data='0x313ce567'},$block)
            if ($decimals -cnotmatch '^0x[0-9a-f]{64}$' -or (Unsigned $decimals.Substring(2)) -ne $feed.decimals) { throw 'Native decimals differ from official catalogue.' }
            $round=Read-Rpc 'eth_call' @(@{to=$address;data='0xfeaf968c'},$block)
            if ($round -cnotmatch '^0x[0-9a-f]{320}$') { throw 'Five complete ABI words required.' }
            $hex=$round.Substring(2)
            $roundId=Unsigned $hex.Substring(0,64)
            $answer=[Numerics.BigInteger]::Parse($hex.Substring(64,64),[Globalization.NumberStyles]::AllowHexSpecifier,[Globalization.CultureInfo]::InvariantCulture)
            $started=Unsigned $hex.Substring(128,64);$updated=Unsigned $hex.Substring(192,64);$answered=Unsigned $hex.Substring(256,64)
            if ($roundId -le 0 -or $roundId -ge [Numerics.BigInteger]::Pow(2,80) -or $answered -ge [Numerics.BigInteger]::Pow(2,80) -or $started -le 0 -or $updated -lt $started -or $updated -gt $blockTime) { throw 'Native round identity/timestamps differ.' }
            @{address=$address;name=$description;decimals=[int]$feed.decimals;roundId=$roundId.ToString();answer=$answer.ToString();startedAt=$started.ToString();updatedAt=$updated.ToString();answeredInRound=$answered.ToString();ageAtBlockSeconds=($blockTime-$updated).ToString()}
        })
        @{count=$count;rows=$rows}
    })
    $report.selections=$selections;$report.qualified=$true
    $source=@{schema=1;rpcUrl=$rpc;chainId='0x1';sourceOwner=[Guid]::NewGuid().ToString('D');feeds=@($catalogue.feeds | ForEach-Object { @{address=$_.proxyAddress.ToLowerInvariant();name=$_.name;decimals=[int]$_.decimals} })}
    $output=[IO.File]::Open((Join-Path $state 'source.json'),[IO.FileMode]::CreateNew)
    try { $output.Write($utf8.GetBytes(($source|ConvertTo-Json -Depth 8)));$output.Flush($true) } finally { $output.Dispose() }
} catch { $report.qualified=$false;$report.failure=$_.Exception.GetType().FullName;$report.message=$_.Exception.Message }
finally { $client.Dispose() }
[IO.File]::WriteAllText((Join-Path $results 'qualification.json'),($report|ConvertTo-Json -Depth 14),$utf8)
$report | Select-Object qualified,cqlAcceptance,sourceWrites,blockHash,blockNumber,failure,message | ConvertTo-Json -Compress
if (!$report.qualified) { throw 'Original native outcome retained; no retry, alternate endpoint or implicit downgrade.' }
