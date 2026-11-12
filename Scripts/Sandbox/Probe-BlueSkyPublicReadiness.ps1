#Requires -Version 7.0
# Explicit anonymous AppView GETs only. No PDS session, account or post is created.
$ErrorActionPreference='Stop'
$root=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
$state=Join-Path $root 'LocalState/COSMIC/HostedBlueSkyPublicSourceCurrent'
$results=Join-Path $root 'TestResults/BlueSkyPublicSourceCurrent'
New-Item -ItemType Directory -Force -Path $state,$results | Out-Null
$endpoint='https://public.api.bsky.app/'
$handles=@('bsky.app','atproto.com','jay.bsky.team')
$utf8=[Text.UTF8Encoding]::new($false,$true)
if (Test-Path -LiteralPath (Join-Path $state 'source.json')) { throw 'Original source exists; never replace owner/catalogue.' }
$intent=[IO.File]::Open((Join-Path $state 'native-qualification.intent.json'),[IO.FileMode]::CreateNew)
try {
    $intent.Write($utf8.GetBytes((@{endpoint=$endpoint;handles=$handles;sourceWrites=0;retryAllowed=$false;policy='cached-public-appview'}|ConvertTo-Json -Compress)))
    $intent.Flush($true)
} finally { $intent.Dispose() }
$core=Join-Path $root 'OASIS Architecture/NextGenSoftware.OASIS.API.Core/bin/Debug/net10.0/NextGenSoftware.OASIS.API.Core.dll'
[Reflection.Assembly]::LoadFrom($core) | Out-Null
[NextGenSoftware.OASIS.API.Core.Objects.NativeData.NativeConnectionGuard]::AllowPrivateOverride=$false
$client=[Net.Http.HttpClient]::new([NextGenSoftware.OASIS.API.Core.Objects.NativeData.NativeConnectionGuard]::CreateGuardedHttpHandler())
$client.Timeout=[TimeSpan]::FromSeconds(20)
$calls=[Collections.Generic.List[object]]::new()
function Read-Profiles([string[]]$actors) {
    $number=$calls.Count+1
    $query=($actors|ForEach-Object {'actors='+[Uri]::EscapeDataString($_)}) -join '&'
    $url=$endpoint+'xrpc/app.bsky.actor.getProfiles?'+$query
    $entry=@{actors=$actors;method='GET'};$calls.Add($entry)
    $response=$client.GetAsync($url,[Net.Http.HttpCompletionOption]::ResponseHeadersRead).GetAwaiter().GetResult()
    try {
        $entry.status=[int]$response.StatusCode
        $response.Content.LoadIntoBufferAsync(262144).GetAwaiter().GetResult() | Out-Null
        $raw=$response.Content.ReadAsByteArrayAsync().GetAwaiter().GetResult()
        $entry.bytes=$raw.Length;$entry.sha256=[Convert]::ToHexString([Security.Cryptography.SHA256]::HashData($raw)).ToLowerInvariant()
        $output=[IO.File]::Open((Join-Path $results ($number.ToString()+'-getProfiles.json')),[IO.FileMode]::CreateNew)
        try {$output.Write($raw);$output.Flush($true)} finally {$output.Dispose()}
        if ($entry.status -ne 200) { throw 'Native HTTP status rejected.' }
        $value=$utf8.GetString($raw)|ConvertFrom-Json -Depth 24 -DateKind String
        $rows=@($value.profiles)
        if ($value.error -or $rows.Count -ne $actors.Count -or @($rows.did|Select-Object -Unique).Count -ne $rows.Count) { throw 'Complete unique profile selection required.' }
        foreach($row in $rows) {
            if ($row.did -cnotmatch '^did:plc:[a-z2-7]{24}$' -or $row.handle -isnot [string] -or !$row.handle) { throw 'Canonical DID and public handle required.' }
        }
        return ,$rows
    } finally { $response.Dispose() }
}
$report=@{qualified=$false;cqlAcceptance=$false;sourceWrites=0;policy='cached-public-appview';calls=$calls}
try {
    $discovered=Read-Profiles $handles
    if (@($discovered|Where-Object {$_.handle -cnotin $handles}).Count) { throw 'Handle selection differs.' }
    $actors=@(foreach($handle in $handles) { ($discovered|Where-Object {$_.handle -ceq $handle}).did })
    $selections=@(foreach($count in @(3,2)) {
        $selected=@($actors|Select-Object -First $count)
        $rows=Read-Profiles $selected
        if (@($rows|Where-Object {$_.did -cnotin $selected}).Count) { throw 'DID selection differs.' }
        @{count=$count;dids=@($rows.did);handles=@($rows.handle)}
    })
    $source=@{schema=1;apiUrl=$endpoint;actors=$actors;sourceOwner=[Guid]::NewGuid().ToString('D')}
    $output=[IO.File]::Open((Join-Path $state 'source.json'),[IO.FileMode]::CreateNew)
    try {$output.Write($utf8.GetBytes(($source|ConvertTo-Json -Depth 8)));$output.Flush($true)} finally {$output.Dispose()}
    $report.qualified=$true;$report.selections=$selections
} catch {$report.failure=$_.Exception.GetType().FullName;$report.message=$_.Exception.Message}
finally {$client.Dispose()}
[IO.File]::WriteAllText((Join-Path $results 'qualification.json'),($report|ConvertTo-Json -Depth 12),$utf8)
$report|Select-Object qualified,cqlAcceptance,sourceWrites,policy,selections,failure,message|ConvertTo-Json -Depth 8 -Compress
if (!$report.qualified) { throw 'Original outcome retained; no alternate account/endpoint or implicit retry.' }
