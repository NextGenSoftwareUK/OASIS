#requires -Version 7.5
[CmdletBinding()]
param([switch]$RemoveVerifiedFixture)
$ErrorActionPreference='Stop'
# Exact original synthetic fixture only. Caller owns the global provider build lease.
$root=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
$work=Join-Path $root 'TestResults/GoogleCurrent'
$lease=[IO.File]::Open((Join-Path $work 'environment.lock'),[IO.FileMode]::OpenOrCreate,[IO.FileAccess]::ReadWrite,[IO.FileShare]::None)
try{
    $intent=Join-Path $work 'history-native-query.pending.json'
    $report=Join-Path $work 'google-history-native-current.trx'
    if((Get-FileHash $intent).Hash -ne 'F2A0DFD87D7FD9D101E0ECAD5885B3F477CC456DB97893F4C544961A5BD0AEA9' -or
        (Get-FileHash $report).Hash -ne '39A8F19C860E26185241612971E976F9C5DF794E5F785F3724D3F7F8EB623B39'){throw 'Exactly the reviewed original intent/report are required; no replay.'}
    $pending=@(Get-ChildItem -LiteralPath $work -Filter '*.pending.json')
    if($pending.Count -ne 1 -or $pending[0].Name -ne 'history-native-query.pending.json'){throw 'Unexpected unresolved original.'}
    $port=Get-NetTCPConnection -LocalPort 18080 -State Listen
    $process=Get-CimInstance Win32_Process -Filter "ProcessId=$($port.OwningProcess)"
    if($process.ProcessId -ne 30456 -or $process.Name -ne 'java.exe' -or $process.CommandLine -notmatch 'cloud-firestore-emulator-v1.22.0.jar' -or $process.CommandLine -notmatch '--project_id oasis-google-native'){throw 'Same retained original emulator required; never restart.'}
    $base='http://127.0.0.1:18080/v1/projects/oasis-google-native/databases/(default)/documents/'
    $expected=@(
        @{id='1e1b2fe4-0279-4c4f-9bbc-0eb7eaa6062e';hash='ED573EF63D38893B73EA1FE9B97F942CD442BCBFC9497FACD6115EB19E75FD51';name='current-root';version=2},
        @{id='396d6c8e-3c5c-4dc5-aeb2-e60d3ab64ed3';hash='7AE459C702B28DED746A2C770413DE4F7202CE49ADD65ADC1ABD6E942C82C3FB';name='child3';version=3})
    $rows=foreach($row in $expected){
        $document=ConvertFrom-Json -InputObject (Invoke-WebRequest -Uri ($base+'holons/'+$row.id) -TimeoutSec 10).Content -DateKind String -AsHashtable
        $json=$document.fields.data_json.stringValue
        $hash=[Convert]::ToHexString([Security.Cryptography.SHA256]::HashData([Text.Encoding]::UTF8.GetBytes($json)))
        $canonical=ConvertFrom-Json -InputObject $json -Depth 80 -DateKind String -AsHashtable
        if($hash -cne $row.hash -or $canonical['Id'] -cne $row.id -or $canonical['Name'] -cne $row.name -or $canonical['Version'] -ne $row.version -or
            $document.name -cne ('projects/oasis-google-native/databases/(default)/documents/holons/'+$row.id) -or $document.fields.id.stringValue -cne $row.id -or
            $document.updateTime -isnot [string] -or $document.updateTime -cnotmatch '^\d{4}-\d{2}-\d{2}T\d{2}:\d{2}:\d{2}(?:\.\d{1,9})?Z$'){throw 'Original native payload/identity/revision differs; preserve it.'}
        @{id=$row.id;name=$document.name;jsonSha256=$hash;updateTime=$document.updateTime}
    }
    $evidence=@{scope='exact historical-root assertion fixture; no save replay';rows=$rows;originalReportSha256=(Get-FileHash $report).Hash;originalIntentSha256=(Get-FileHash $intent).Hash;observedUtc=[DateTimeOffset]::UtcNow.ToString('O')}
    if(!$RemoveVerifiedFixture){$evidence|ConvertTo-Json -Depth 6;return}
    $recovery=Join-Path $work 'scoped-export-reconciliation.pending.json'
    $resolved=Join-Path $work 'scoped-export-reconciliation.resolved.json'
    if((Test-Path $recovery) -or (Test-Path $resolved)){throw 'Recovery already has evidence; do not repeat.'}
    $bytes=[Text.Encoding]::UTF8.GetBytes(($evidence|ConvertTo-Json -Depth 6))
    $stream=[IO.File]::Open($recovery,[IO.FileMode]::CreateNew,[IO.FileAccess]::Write,[IO.FileShare]::None)
    try{$stream.Write($bytes);$stream.Flush($true)}finally{$stream.Dispose()}
    $body=@{writes=@($rows|ForEach-Object {@{delete=$_.name;currentDocument=@{updateTime=$_.updateTime}}})}|ConvertTo-Json -Depth 7 -Compress
    $ack=ConvertFrom-Json -InputObject (Invoke-WebRequest -Uri ($base.TrimEnd('/')+':commit') -Method Post -ContentType 'application/json' -Body $body -TimeoutSec 15).Content -DateKind String
    if(@($ack.writeResults).Count -ne 2 -or [string]::IsNullOrWhiteSpace($ack.commitTime)){throw 'Conditional cleanup acknowledgement unclear; preserve original journals.'}
    foreach($row in $rows){
        $absent=$false;try{Invoke-WebRequest -Uri ($base+'holons/'+$row.id) -TimeoutSec 10|Out-Null}catch{if([int]$_.Exception.Response.StatusCode -eq 404){$absent=$true}else{throw}}
        if(!$absent){throw 'Original absence not independently verified.'}
    }
    if((Get-FileHash $intent).Hash -ne $evidence.originalIntentSha256){throw 'Original intent changed; preserve it.'}
    Remove-Item -LiteralPath $intent
    Move-Item -LiteralPath $recovery -Destination $resolved
    $evidence|ConvertTo-Json -Depth 6
}finally{$lease.Dispose()}
