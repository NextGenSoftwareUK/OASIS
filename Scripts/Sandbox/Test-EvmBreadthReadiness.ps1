# Explicit read-only protocol qualification; never CQL/hosted acceptance.
[CmdletBinding()]
param([Parameter(Mandatory)][ValidateSet('Base','Avalanche','BNBChain','Optimism')][string]$Network)
$ErrorActionPreference='Stop'
$root=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
$results=Join-Path $root 'TestResults/EvmBreadthReadinessCurrent'
New-Item -ItemType Directory -Force -Path $results | Out-Null
$descriptor=switch($Network){
    'Optimism' { @{endpoint='https://mainnet.optimism.io/';chainId='0xa'} }
    'Base' { @{endpoint='https://mainnet.base.org/';chainId='0x2105'} }
    'Avalanche' { @{endpoint='https://api.avax.network/ext/bc/C/rpc';chainId='0xa86a'} }
    'BNBChain' { @{endpoint='https://bsc-dataseed.bnbchain.org/';chainId='0x38'} }
}
$client=[Net.Http.HttpClient]::new()
$client.Timeout=[TimeSpan]::FromSeconds(15)
$observations=[Collections.Generic.List[object]]::new()
function Invoke-ReadRpc([string]$method,[object[]]$parameters){
    $requestId=$observations.Count+1
    $payload=@{jsonrpc='2.0';id=$requestId;method=$method;params=$parameters}|ConvertTo-Json -Compress -Depth 12
    $request=[Net.Http.HttpRequestMessage]::new([Net.Http.HttpMethod]::Post,$descriptor.endpoint)
    $request.Content=[Net.Http.StringContent]::new($payload,[Text.Encoding]::UTF8,'application/json')
    try{
        $response=$client.SendAsync($request,[Net.Http.HttpCompletionOption]::ResponseHeadersRead).GetAwaiter().GetResult()
        try{
            $response.EnsureSuccessStatusCode()|Out-Null
            $response.Content.LoadIntoBufferAsync(262144).GetAwaiter().GetResult()|Out-Null
            $bytes=$response.Content.ReadAsByteArrayAsync().GetAwaiter().GetResult()
            if($bytes.Length -gt 262144){throw 'Response exceeds256KiB.'}
            $observations.Add(@{method=$method;bytes=$bytes.Length;sha256=[Convert]::ToHexString([Security.Cryptography.SHA256]::HashData($bytes))})
            $value=[Text.Encoding]::UTF8.GetString($bytes)|ConvertFrom-Json -Depth 16
            if($value.jsonrpc -cne '2.0' -or $value.id -ne $requestId -or $null -ne $value.error -or $null -eq $value.result){
                if($null -ne $value.error){throw ('RPC error code '+$value.error.code+': '+$value.error.message)}
                throw 'RPC identity/result differs.'
            }
            return $value.result
        }finally{$response.Dispose()}
    }finally{$request.Dispose()}
}
$report=@{network=$Network;endpoint=$descriptor.endpoint;chainId=$descriptor.chainId;requests=$observations;
    utc=[DateTime]::UtcNow.ToString('O');cqlAcceptance=$false;hostedAcceptance=$false;writes=0}
try{
    if((Invoke-ReadRpc 'eth_chainId' @()) -cne $descriptor.chainId){throw 'Chain identity differs.'}
    $genesis=Invoke-ReadRpc 'eth_getBlockByNumber' @('0x0',$false)
    if($genesis.hash -cnotmatch '^0x[0-9a-f]{64}$' -or $genesis.number -cne '0x0'){throw 'Canonical genesis identity required.'}
    $report.genesisHash=$genesis.hash
    if($Network -eq 'BNBChain' -and $genesis.hash -cne '0x0d21840abff46b96c84b2ac9e10e4f5cdaeb5693cb665db62a2f3b02d2d57b5b'){throw 'BSC canonical genesis differs.'}
    $header=Invoke-ReadRpc 'eth_getBlockByNumber' @('finalized',$false)
    if($header.hash -cnotmatch '^0x[0-9a-f]{64}$' -or $header.number -cnotmatch '^0x(?:0|[1-9a-f][0-9a-f]*)$'){throw 'Canonical finalized header required.'}
    $report.blockHash=$header.hash;$report.blockNumber=$header.number
    $selections=@()
    foreach($count in @(3,2)){
        $rows=@()
        foreach($index in 1..$count){
            $address='0x'+$index.ToString('x40')
            $balance=Invoke-ReadRpc 'eth_getBalance' @($address,@{blockHash=$header.hash;requireCanonical=$true})
            if($balance -cnotmatch '^0x(?:0|[1-9a-f][0-9a-f]{0,63})$'){throw 'Canonical UInt256 balance required.'}
            $quantity=[Numerics.BigInteger]::Parse('0'+$balance.Substring(2),[Globalization.NumberStyles]::AllowHexSpecifier)
            $rows+=@{address=$address;nativeBaseUnits=$quantity.ToString([Globalization.CultureInfo]::InvariantCulture)}
        }
        $selections+=@{count=$count;rows=$rows}
    }
    $report.selections=$selections;$report.passed=$true
}catch{$report.passed=$false;$report.error=$_.Exception.Message}
finally{$client.Dispose()}
$report|ConvertTo-Json -Depth 16|Set-Content (Join-Path $results ($Network+'.json')) -Encoding utf8
[pscustomobject]@{Network=$Network;Passed=$report.passed;Genesis=$report.genesisHash;Error=$report.error;Writes=0}
if(!$report.passed){throw 'Protocol qualification failed; no fallback or state downgrade.'}
