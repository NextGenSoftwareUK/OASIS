#Requires -Version 7.0
# One journalled public native-account qualification; no CQL grant/source writes.
$ErrorActionPreference = 'Stop'
$root = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
$state = Join-Path $root 'LocalState/COSMIC/HostedEosioPublicSourceCurrent'
$results = Join-Path $root 'TestResults/EosioPublicSourceCurrent'
$intent = Join-Path $state 'public-qualification.intent.json'
if ((Test-Path -LiteralPath $intent) -or (Test-Path -LiteralPath (Join-Path $state 'source.json'))) {
    throw 'Original qualification/catalogue exists; reconcile rather than replay.'
}
New-Item -ItemType Directory -Force -Path $state,$results | Out-Null
$corePath = Join-Path $root 'OASIS Architecture/NextGenSoftware.OASIS.API.Core/bin/Debug/net10.0/NextGenSoftware.OASIS.API.Core.dll'
[Reflection.Assembly]::LoadFrom($corePath) | Out-Null
[NextGenSoftware.OASIS.API.Core.Objects.NativeData.NativeConnectionGuard]::AllowPrivateOverride = $false
$client = [Net.Http.HttpClient]::new([NextGenSoftware.OASIS.API.Core.Objects.NativeData.NativeConnectionGuard]::CreateGuardedHttpHandler())
$client.Timeout = [TimeSpan]::FromSeconds(15)
$utf8 = [Text.UTF8Encoding]::new($false)
$endpoint = 'https://vaulta.greymass.com/'
$expectedChain = 'aca376f206b8fc25a6ed44dbdc66547c36c6c33e3a119ffbeaef943642f0e906'
$keys = @('eosio','eosio.token','eosio.msig')
$journal = [IO.File]::Open($intent,[IO.FileMode]::CreateNew)
try {
    $bytes=$utf8.GetBytes((@{apiUrl=$endpoint;chainId=$expectedChain;accounts=$keys;sourceWrites=0;retryAllowed=$false}|ConvertTo-Json -Compress))
    $journal.Write($bytes);$journal.Flush($true)
} finally { $journal.Dispose() }
$calls=[Collections.Generic.List[object]]::new()
function Read-Native([string]$method,[hashtable]$body) {
    $number=$calls.Count+1; $entry=@{method=$method;body=$body};$calls.Add($entry)
    $request=[Net.Http.HttpRequestMessage]::new([Net.Http.HttpMethod]::Post,($endpoint+'v1/chain/'+$method))
    $request.Content=[Net.Http.StringContent]::new(($body|ConvertTo-Json -Compress),$utf8,'application/json')
    try {
        $response=$client.SendAsync($request,[Net.Http.HttpCompletionOption]::ResponseHeadersRead).GetAwaiter().GetResult()
        try {
            $entry.status=[int]$response.StatusCode
            $response.Content.LoadIntoBufferAsync(262144).GetAwaiter().GetResult() | Out-Null
            $raw=$response.Content.ReadAsByteArrayAsync().GetAwaiter().GetResult()
            $entry.bytes=$raw.Length; $entry.sha256=[Convert]::ToHexString([Security.Cryptography.SHA256]::HashData($raw)).ToLowerInvariant()
            [IO.File]::WriteAllBytes((Join-Path $results ($number.ToString()+'-'+$method+'.json')),$raw)
            if ($entry.status -ne 200) { throw 'Native HTTP status rejected.' }
            $value=$utf8.GetString($raw)|ConvertFrom-Json -Depth 20 -DateKind String
            if ($null -eq $value -or $null -ne $value.error) { throw 'Native result rejected.' }
            return $value
        } finally { $response.Dispose() }
    } finally { $request.Dispose() }
}
$report=@{apiUrl=$endpoint;chainId=$expectedChain;qualified=$false;sourceWrites=0;cqlAcceptance=$false;policy='latest native state per account';calls=$calls}
try {
    $before=Read-Native 'get_info' @{}
    if ($before.chain_id -cne $expectedChain) { throw 'Native mainnet chain identity differs.' }
    $selections=@()
    foreach ($count in @(3,2)) {
        $records=@(foreach($name in $keys[0..($count-1)]) {
            $account=Read-Native 'get_account' @{account_name=$name}
            if ($account.account_name -cne $name -or $null -eq $account.head_block_num -or $account.head_block_num -le 0 -or
                $account.created -isnot [string] -or [string]::IsNullOrWhiteSpace($account.created) -or $null -eq $account.ram_quota) {
                throw 'Native account identity/head/creation/resource fields missing.'
            }
            @{account=$name;headBlock=$account.head_block_num;created=$account.created;ramQuota=$account.ram_quota}
        })
        $selections+=@{count=$records.Count;records=$records}
    }
    $after=Read-Native 'get_info' @{}
    if ($after.chain_id -cne $expectedChain) { throw 'Network identity changed during native reads.' }
    $report.selections=$selections; $report.headBefore=$before.head_block_num;$report.headAfter=$after.head_block_num
    $source=@{schema=1;apiUrl=$endpoint;chainId=$expectedChain;accounts=$keys;sourceOwner=[Guid]::NewGuid().ToString()}
    $file=[IO.File]::Open((Join-Path $state 'source.json'),[IO.FileMode]::CreateNew)
    try { $bytes=$utf8.GetBytes(($source|ConvertTo-Json -Depth 5));$file.Write($bytes);$file.Flush($true) } finally { $file.Dispose() }
    $report.qualified=$true
} catch { $report.qualified=$false;$report.failure=$_.Exception.GetType().FullName;$report.message=$_.Exception.Message }
finally { $client.Dispose() }
[IO.File]::WriteAllText((Join-Path $results 'qualification.json'),($report|ConvertTo-Json -Depth 14),$utf8)
$report|ConvertTo-Json -Depth 14 -Compress
if (!$report.qualified) { throw 'Native EOSIO qualification failed; no retry/fallback/provider grant.' }
