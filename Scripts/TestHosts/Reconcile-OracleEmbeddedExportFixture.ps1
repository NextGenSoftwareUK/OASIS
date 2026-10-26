#Requires -Version 7.5
[CmdletBinding()]
param([switch]$RemoveVerifiedFixture,[switch]$FinalizeVerifiedAbsence)
$ErrorActionPreference='Stop'
# One reviewed failed synthetic cohort only; no reset, save replay or runtime restart.
$root=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
$work=Join-Path $root 'TestResults/OracleCurrent'
$lease=[IO.File]::Open((Join-Path $work 'environment.lock'),[IO.FileMode]::OpenOrCreate,[IO.FileAccess]::ReadWrite,[IO.FileShare]::None)
try{
    if($RemoveVerifiedFixture -and $FinalizeVerifiedAbsence){throw 'Choose one explicit reconciliation phase.'}
    $report=Join-Path $work 'oracle-native-current.trx'
    if((Get-FileHash -LiteralPath $report).Hash -ne 'B56C0B06B862861CEDF9FF2305506BB5F8F253C785056A991FE23418BEF5A12E'){throw 'Exact original failed report required.'}
    $runtime=(& wsl -d Ubuntu -u root -- podman inspect --format '{{.Id}} {{.Image}} {{.State.Status}} {{.State.StartedAt}}' oasis-oracle-free).Trim()
    if($LASTEXITCODE -ne 0 -or $runtime -ne 'e8861e296e101147681c4e2f0a3115e870cafffbce5f92848eb75d8cf25d0982 358b32468eddc329751c28007a535cbc661f1090ab5d8a9f02267cf44ef1e3b9 running 2026-10-25 19:00:16.564390257 -0500 EST'){throw 'Same original retained runtime required.'}
    $expected=@(
        @{Name='embedded-export-False.pending.json';Intent='7C170A9F2FDBB04AE856A7AD1C2D9EE6C49744DB7D30AF49670C1F98594FCFF2';Id='c732e2c5-3268-43e6-a9ff-93ad0267decd';Payload='1F3A6AE4C8282A07835B056B23523498E099BB83E95C40DEFD70947AF744BAA7'},
        @{Name='embedded-export-True.pending.json';Intent='C6C31D2F2200B0EA6EF988CEB07B0B98A174C70CE0EC131910832A6494BABC7F';Id='84d27524-26a3-4c93-821e-aedae988059f';Payload='0A76DE8EB5D9768B7B4590C4EE9A0F59512C5845680F9936FAD58F01740A21A5'}
    )
    function NativeSql([string]$Body){
        $sql="WHENEVER SQLERROR EXIT FAILURE ROLLBACK`nSET HEADING OFF FEEDBACK OFF PAGESIZE 0 LINESIZE 32767 LONG 100000 LONGCHUNKSIZE 100000 TRIMSPOOL ON`nALTER SESSION SET CONTAINER=FREEPDB1;`n$Body`nEXIT"
        $output=($sql | & wsl -d Ubuntu -u root -- podman exec -i oasis-oracle-free sqlplus -S '/ as sysdba') -join "`n"
        if($LASTEXITCODE -ne 0 -or $output -match '(?m)^(SP2-|ORA-)'){throw 'Native SQL verification failed; preserve originals.'}
        return $output.Trim()
    }
    if($FinalizeVerifiedAbsence){
        $proof=Join-Path $work 'embedded-export-fixture-reconciliation.pending.json'
        $resolved=Join-Path $work 'embedded-export-fixture-reconciliation.resolved.json'
        if((Get-FileHash -LiteralPath $proof).Hash -ne 'C99787116ED1255E27390B4B4357E506D51804EF26813F3153D6FC04E5EBDFC0' -or (Test-Path -LiteralPath $resolved)){throw 'Exact dispatched cleanup intent required.'}
        $pending=@(Get-ChildItem -LiteralPath $work -Filter '*.pending.json')
        if($pending.Count -ne 3 -or @(Compare-Object ($pending.Name|Sort-Object) (@($expected.Name)+@('embedded-export-fixture-reconciliation.pending.json')|Sort-Object)).Count){throw 'Unexpected pending operations.'}
        foreach($row in $expected){
            if((Get-FileHash -LiteralPath (Join-Path $work $row.Name)).Hash -ne $row.Intent){throw 'Original intent changed.'}
            $hex=[Convert]::ToHexString(([Guid]$row.Id).ToByteArray())
            if((NativeSql "SELECT COUNT(*) FROM SYSTEM.OASIS_HOLONS WHERE ID=HEXTORAW('$hex');") -ne '0'){throw 'Original is not independently absent; no deletion replay.'}
        }
        foreach($row in $expected){[IO.File]::Delete((Join-Path $work $row.Name))}
        [IO.File]::Move($proof,$resolved)
        'Reviewed dispatched cleanup independently resolved absent; no native mutation replay.'
        return
    }
    $pending=@(Get-ChildItem -LiteralPath $work -Filter '*.pending.json')
    if($pending.Count -ne 2 -or @(Compare-Object ($pending.Name|Sort-Object) ($expected.Name|Sort-Object)).Count){throw 'Only the two reviewed pending originals are allowed.'}
    $observed=@()
    foreach($row in $expected){
        $path=Join-Path $work $row.Name
        if((Get-FileHash -LiteralPath $path).Hash -ne $row.Intent){throw 'Original intent changed.'}
        $intent=Get-Content -LiteralPath $path -Raw|ConvertFrom-Json
        if($intent.id -ne $row.Id -or $intent.state -ne 'before-owned-legacy-embedded-export'){throw 'Unexpected original intent.'}
        $hex=[Convert]::ToHexString(([Guid]$row.Id).ToByteArray())
        $payload=NativeSql "SELECT DATA_JSON FROM SYSTEM.OASIS_HOLONS WHERE ID=HEXTORAW('$hex');"
        $hash=[Convert]::ToHexString([Security.Cryptography.SHA256]::HashData([Text.Encoding]::UTF8.GetBytes($payload)))
        if($hash -ne $row.Payload){throw 'Original native payload changed.'}
        $data=$payload|ConvertFrom-Json -AsHashtable -DateKind String
        if($data.Id -ne $row.Id -or $data.Version -ne 3 -or $data.Name -ne 'owned legacy embedded export current' -or $data._oasis_record_history.records.Count -ne 2){throw 'Unexpected original canonical fixture.'}
        foreach($version in 1,2){
            $stored=$data._oasis_record_history.records[$version-1]
            if($stored.Version -ne $version -or $stored.CreatedByAvatarId -ne $intent.owner -or $stored.ContainsKey('Children')){throw 'Not the reviewed serializer-omission failure.'}
        }
        $observed+=@{Id=$row.Id;Hex=$hex;Payload=$payload;SHA256=$hash;Intent=$row.Intent;Name=$row.Name}
    }
    if(!$RemoveVerifiedFixture){$observed|Select-Object Id,SHA256,Intent|ConvertTo-Json -Compress;return}
    $proof=Join-Path $work 'embedded-export-fixture-reconciliation.pending.json'
    $resolved=Join-Path $work 'embedded-export-fixture-reconciliation.resolved.json'
    if((Test-Path -LiteralPath $proof) -or (Test-Path -LiteralPath $resolved)){throw 'Review existing reconciliation; do not repeat.'}
    $bytes=[Text.Encoding]::UTF8.GetBytes((@{State='before-exact-conditional-delete';Rows=@($observed|Select-Object Id,SHA256,Intent)}|ConvertTo-Json -Depth 4 -Compress))
    $file=[IO.File]::Open($proof,[IO.FileMode]::CreateNew,[IO.FileAccess]::Write,[IO.FileShare]::None)
    try{$file.Write($bytes);$file.Flush($true)}finally{$file.Dispose()}
    $statements=@('DECLARE payload CLOB; BEGIN')
    foreach($row in $observed){
        $literal=$row.Payload.Replace("'","''")
        if([Text.Encoding]::UTF8.GetByteCount($literal) -gt 30000){throw 'Original exceeds reviewed literal bound.'}
        $statements+='payload:=EMPTY_CLOB();'
        for($offset=0;$offset -lt $row.Payload.Length;$offset+=800){
            $piece=$row.Payload.Substring($offset,[Math]::Min(800,$row.Payload.Length-$offset)).Replace("'","''")
            $statements+="payload:=payload||TO_CLOB('$piece');"
        }
        $statements+="DELETE FROM SYSTEM.OASIS_HOLONS WHERE ID=HEXTORAW('$($row.Hex)') AND DBMS_LOB.COMPARE(DATA_JSON,payload)=0; IF SQL%ROWCOUNT<>1 THEN RAISE_APPLICATION_ERROR(-20001,'Original changed'); END IF;"
    }
    $statements+="COMMIT; END;`n/"
    # SQLPlus completion plus independent native absence, not optional DBMS_OUTPUT,
    # is the observation boundary. Never re-dispatch after an uncertain outcome.
    [void](NativeSql ($statements -join "`n"))
    foreach($row in $observed){
        if((NativeSql "SELECT COUNT(*) FROM SYSTEM.OASIS_HOLONS WHERE ID=HEXTORAW('$($row.Hex)');") -ne '0'){throw 'Original still exists.'}
        if((Get-FileHash -LiteralPath (Join-Path $work $row.Name)).Hash -ne $row.Intent){throw 'Intent changed during cleanup.'}
    }
    foreach($row in $observed){[IO.File]::Delete((Join-Path $work $row.Name))}
    [IO.File]::Move($proof,$resolved)
    'Two exact original synthetic rows conditionally removed and independently absent; no write replay.'
}finally{$lease.Dispose()}
