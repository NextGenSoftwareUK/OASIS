#Requires -Version 7.5
[CmdletBinding()]
param([switch]$RemoveVerifiedFixtures,[switch]$VerifyCleanupProgramReadOnly,[switch]$AfterRuntimeRestart,[switch]$InspectNativeHashes,[switch]$RemoveNativeHashVerified,[switch]$FinalizeNativeHashAbsence)
$ErrorActionPreference='Stop'
$root=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
$work=Join-Path $root 'TestResults/OracleCurrent'
$lease=[IO.File]::Open((Join-Path $work 'environment.lock'),[IO.FileMode]::OpenOrCreate,[IO.FileAccess]::ReadWrite,[IO.FileShare]::None)
try{
    if($AfterRuntimeRestart -and ((!$VerifyCleanupProgramReadOnly -and !$InspectNativeHashes -and !$RemoveNativeHashVerified -and !$FinalizeNativeHashAbsence) -or $RemoveVerifiedFixtures)){throw 'Runtime recovery requires an explicit original-inspection phase.'}
    if((Get-FileHash -LiteralPath (Join-Path $work 'oracle-native-current.trx')).Hash -ne '2B57C64EC4A84915973BBA905FC6E3505FF8C037F0F8CD5200E6F26A225E9780'){throw 'Exact original typed-fixture failure report required.'}
    $runtime=(& wsl -d Ubuntu -u root -- podman inspect --format '{{.Id}} {{.Image}} {{.State.Status}} {{.State.StartedAt}}' oasis-oracle-free).Trim()
    if($LASTEXITCODE -ne 0 -or (!$AfterRuntimeRestart -and $runtime -ne 'e8861e296e101147681c4e2f0a3115e870cafffbce5f92848eb75d8cf25d0982 358b32468eddc329751c28007a535cbc661f1090ab5d8a9f02267cf44ef1e3b9 running 2026-10-25 19:00:16.564390257 -0500 EST') -or ($AfterRuntimeRestart -and !$runtime.StartsWith('e8861e296e101147681c4e2f0a3115e870cafffbce5f92848eb75d8cf25d0982 358b32468eddc329751c28007a535cbc661f1090ab5d8a9f02267cf44ef1e3b9 running '))){throw 'Same original retained runtime required.'}
    if($AfterRuntimeRestart){
        $mounts=@(& wsl -d Ubuntu -u root -- podman inspect --format '{{json .Mounts}}' oasis-oracle-free|ConvertFrom-Json)
        if($LASTEXITCODE -ne 0 -or $mounts.Count -ne 1 -or $mounts[0].Type -ne 'volume' -or $mounts[0].Name -ne 'oasis-oracle-data' -or $mounts[0].Destination -ne '/opt/oracle/oradata'){throw 'Original Oracle volume required; no replacement.'}
    }
    $intents=[ordered]@{
        'canonical-metadata.pending.json'='F376A561375841F88B0F63712F47490A97AB61D2374A0D3D24CF6EE5510FA30D'
        'canonical-records.pending.json'='6EB3C2FE2AD2E45D7479AD61AB9923DE317D8FB85754AB54100D32E60A5B6A2A'
        'embedded-export-False.pending.json'='4A8A32A5AB9F767C699885E6B36D4C94156C409C46E7EA2634125230C1C0631C'
        'embedded-export-True.pending.json'='F1F748EAB9DE9C5B9D5BBE84B580B8E65AA7AE1CCA7BC7D4D6D700D2BF7B5E06'
        'graph-three-level.pending.json'='B1162321CA5739DEF1868E1DC69DC7EC3ADCA00BDF64AC6695BD89DE9D1A990F'
        'metadata-snapshot-False.pending.json'='E01923946E241F5F893FCE0751FE16C53564FBE9E53E947F687939D246D92E0B'
        'metadata-snapshot-True.pending.json'='D24CA2B71671727EBBC841718A6D3FF4AF5581C5B7D2F118E89F5C8EC952D9F2'
        'read-acknowledgements.pending.json'='13580FD39030F22CC65494331E47FC2BB5F238FD87D0A3E2EA990CA881558242'
        'scoped-visibility-False-False.pending.json'='931486F681B4F9C190C3E3D84E69151253D4840D863475CC3D33C8F111117E07'
        'scoped-visibility-False-True.pending.json'='302F6A4F5E6343A53BF9C3C05B40D5A0BFB97C0560211926E468E8A95CF049D5'
        'scoped-visibility-True-False.pending.json'='ECE1D35B62E85343A51998B764D3070A26E52188D5E688675533B68F3A04BEFD'
        'scoped-visibility-True-True.pending.json'='E1A4D6D67D6E3A222D2B883B110333C52A783A6449C2864979932054F6C378B7'
    }
    $pending=@(Get-ChildItem -LiteralPath $work -Filter '*.pending.json')
    $expectedNames=@($intents.Keys)
    if($VerifyCleanupProgramReadOnly -or $InspectNativeHashes -or $RemoveNativeHashVerified -or $FinalizeNativeHashAbsence){
        if((Get-FileHash -LiteralPath (Join-Path $work 'embedded-type-cleanup.pending.json')).Hash -ne '24183DCC7B63B73608B3AE2551F1C732FBBA3C27B3D613CA7E836A4C5553CEC0'){throw 'Exact failed cleanup intent required for read-only inspection.'}
        $expectedNames+=@('embedded-type-cleanup.pending.json')
    }
    if($FinalizeNativeHashAbsence){$expectedNames+=@('embedded-type-native-cleanup.pending.json')}
    if($pending.Count -ne $expectedNames.Count -or @(Compare-Object ($pending.Name|Sort-Object) ($expectedNames|Sort-Object)).Count){throw 'Unexpected pending originals; no adoption or sweep.'}
    function NativeSql([string]$Body){
        $sql="WHENEVER SQLERROR EXIT FAILURE ROLLBACK`nSET DEFINE OFF`nSET HEADING OFF FEEDBACK OFF PAGESIZE 0 LINESIZE 32767 LONG 200000 LONGCHUNKSIZE 200000 TRIMSPOOL ON`nALTER SESSION SET CONTAINER=FREEPDB1;`n$Body`nEXIT"
        $output=($sql| & wsl -d Ubuntu -u root -- podman exec -i oasis-oracle-free sqlplus -S '/ as sysdba') -join "`n"
        if($LASTEXITCODE -ne 0 -or $output -match '(?m)^(SP2-|ORA-)'){
            $diagnostic=($output -split "`n"|Where-Object{$_ -match '^\s*(SP2-|ORA-|PLS-)'} ) -join ' '
            throw "Native observation failed; retain original evidence. $diagnostic"
        }
        return $output.Trim()
    }
    $targets=@()
    foreach($name in $intents.Keys){
        $path=Join-Path $work $name
        if((Get-FileHash -LiteralPath $path).Hash -ne $intents[$name]){throw 'Original intent changed.'}
        $intent=Get-Content -LiteralPath $path -Raw|ConvertFrom-Json
        if($name -eq 'canonical-records.pending.json'){
            foreach($table in 'OASIS_AVATARS','OASIS_AVATAR_DETAILS','OASIS_HOLONS'){$targets+=@{Table=$table;Id=[Guid]$intent.id;Intent=$name}}
            $targets+=@{Table='OASIS_HOLONS';Id=[Guid]$intent.childId;Intent=$name}
        }elseif($name -eq 'read-acknowledgements.pending.json'){
            foreach($table in 'OASIS_AVATARS','OASIS_AVATAR_DETAILS'){$targets+=@{Table=$table;Id=[Guid]$intent.ids[0];Intent=$name}}
            foreach($id in @($intent.ids)[1..2]){$targets+=@{Table='OASIS_HOLONS';Id=[Guid]$id;Intent=$name}}
        }else{
            $ids=if($intent.PSObject.Properties.Name -contains 'ids'){@($intent.ids)}else{@($intent.id)}
            foreach($id in $ids){$targets+=@{Table='OASIS_HOLONS';Id=[Guid]$id;Intent=$name}}
        }
    }
    if($targets.Count -ne 38 -or @($targets|ForEach-Object{"$($_.Table)/$($_.Id)"}|Select-Object -Unique).Count -ne 38){throw 'Reviewed 38 exact native rows required.'}
    if($FinalizeNativeHashAbsence){
        $inventory=Join-Path $work 'embedded-type-native-hash-inventory.json'
        $proof=Join-Path $work 'embedded-type-native-cleanup.pending.json'
        if((Get-FileHash -LiteralPath $inventory).Hash -ne 'EC7BA7C3A5CC1FDF0B6EB047BCFE5A5AABB9FEFACDB266D10E7E468C3799F73A' -or (Get-FileHash -LiteralPath $proof).Hash -ne '2E3DB90F9D66C343F068F6F38709C39FD87F44ABA485FA05744FFDA0C6737B63'){throw 'Exact dispatched native cleanup evidence required.'}
        $intent=Get-Content -LiteralPath $proof -Raw|ConvertFrom-Json
        if($intent.Runtime -cne $runtime){throw 'Original cleanup runtime changed; inspect independently.'}
        foreach($group in $targets|Group-Object -Property {$_.Table}){
            if($group.Name -notin @('OASIS_HOLONS','OASIS_AVATARS','OASIS_AVATAR_DETAILS')){throw 'Invalid native table identity.'}
            $ids=@($group.Group|ForEach-Object{"HEXTORAW('"+[Convert]::ToHexString($_.Id.ToByteArray())+"')"})
            if((NativeSql "SELECT COUNT(*) FROM SYSTEM.$($group.Name) WHERE ID IN ($($ids -join ','));") -ne '0'){throw 'Originals are not independently absent; no deletion replay.'}
        }
        foreach($name in $intents.Keys){[IO.File]::Delete((Join-Path $work $name))}
        [IO.File]::Move((Join-Path $work 'embedded-type-cleanup.pending.json'),(Join-Path $work 'embedded-type-cleanup.known-not-applied.json'))
        [IO.File]::Move($proof,(Join-Path $work 'embedded-type-native-cleanup.resolved.json'))
        'Dispatched native cleanup independently resolved absent for all38 original IDs; no native mutation replay.'
        return
    }
    if($InspectNativeHashes -or $RemoveNativeHashVerified){
        if((Get-FileHash -LiteralPath (Join-Path $work 'embedded-type-failure-inventory.json')).Hash -ne 'E05172A9A9AAE6C57728E3C7E874EB6888FA8C33731243E72E7EF99C5F46B175'){throw 'Original read-only inventory required.'}
        $nativeRows=@()
        foreach($group in $targets|Group-Object Table){
            $ids=@($group.Group|ForEach-Object{"HEXTORAW('"+[Convert]::ToHexString($_.Id.ToByteArray())+"')"})
            $lines=(NativeSql "SELECT RAWTOHEX(ID)||'|'||DBMS_LOB.GETLENGTH(DATA_JSON)||'|'||RAWTOHEX(DBMS_CRYPTO.HASH(DATA_JSON,4)) FROM SYSTEM.$($group.Name) WHERE ID IN ($($ids -join ',')) ORDER BY RAWTOHEX(ID);") -split "`n"
            if($lines.Count -ne $group.Count){throw 'Native original count changed.'}
            foreach($line in $lines){
                if($line.Trim() -notmatch '^([0-9A-F]{32})\|([0-9]+)\|([0-9A-F]{64})$'){throw 'Native hash observation malformed.'}
                $nativeRows+= [ordered]@{Table=$group.Name;Hex=$Matches[1];Characters=[long]$Matches[2];SHA256=$Matches[3]}
            }
        }
        $nativeInventory=Join-Path $work 'embedded-type-native-hash-inventory.json'
        $nativeBytes=[Text.Encoding]::UTF8.GetBytes(([ordered]@{Runtime=$runtime;ReportSHA256='2B57C64EC4A84915973BBA905FC6E3505FF8C037F0F8CD5200E6F26A225E9780';PriorInventorySHA256='E05172A9A9AAE6C57728E3C7E874EB6888FA8C33731243E72E7EF99C5F46B175';Rows=$nativeRows}|ConvertTo-Json -Depth 5 -Compress))
        if($InspectNativeHashes){
            if(Test-Path -LiteralPath $nativeInventory){throw 'Preserve existing native hash inventory.'}
            $file=[IO.File]::Open($nativeInventory,[IO.FileMode]::CreateNew,[IO.FileAccess]::Write,[IO.FileShare]::None)
            try{$file.Write($nativeBytes);$file.Flush($true)}finally{$file.Dispose()}
            [pscustomobject]@{Rows=$nativeRows.Count;Bytes=$nativeBytes.Length;SHA256=(Get-FileHash -LiteralPath $nativeInventory).Hash;NativeWrites=0}|ConvertTo-Json -Compress
            return
        }
        $nativeHash='EC7BA7C3A5CC1FDF0B6EB047BCFE5A5AABB9FEFACDB266D10E7E468C3799F73A'
        if((Get-FileHash -LiteralPath $nativeInventory).Hash -ne $nativeHash){throw 'Reviewed native hash inventory required.'}
        $reviewed=Get-Content -LiteralPath $nativeInventory -Raw|ConvertFrom-Json
        if(@($reviewed.Rows).Count -ne $nativeRows.Count){throw 'Native row count changed after runtime recovery.'}
        foreach($row in $nativeRows){
            $original=@($reviewed.Rows|Where-Object{$_.Table -ceq $row.Table -and $_.Hex -ceq $row.Hex})
            if($original.Count -ne 1 -or $original[0].Characters -ne $row.Characters -or $original[0].SHA256 -cne $row.SHA256){throw 'Original native payload changed after runtime recovery.'}
        }
        $proof=Join-Path $work 'embedded-type-native-cleanup.pending.json'
        $resolved=Join-Path $work 'embedded-type-native-cleanup.resolved.json'
        if((Test-Path -LiteralPath $proof) -or (Test-Path -LiteralPath $resolved)){throw 'Review native cleanup outcome; no repeat.'}
        $proofBytes=[Text.Encoding]::UTF8.GetBytes((@{InventorySHA256=$nativeHash;Runtime=$runtime;Rows=38;State='before-native-hash-conditional-delete'}|ConvertTo-Json -Compress))
        $file=[IO.File]::Open($proof,[IO.FileMode]::CreateNew,[IO.FileAccess]::Write,[IO.FileShare]::None)
        try{$file.Write($proofBytes);$file.Flush($true)}finally{$file.Dispose()}
        $statements=@('BEGIN')
        foreach($row in $nativeRows){
            $statements+="DELETE FROM SYSTEM.$($row.Table) WHERE ID=HEXTORAW('$($row.Hex)') AND DBMS_LOB.GETLENGTH(DATA_JSON)=$($row.Characters) AND RAWTOHEX(DBMS_CRYPTO.HASH(DATA_JSON,4))='$($row.SHA256)'; IF SQL%ROWCOUNT<>1 THEN RAISE_APPLICATION_ERROR(-20001,'Original changed'); END IF;"
        }
        $statements+="COMMIT; END;`n/"
        [void](NativeSql ($statements -join "`n"))
        foreach($group in $nativeRows|Group-Object -Property {$_.Table}){
            if($group.Name -notin @('OASIS_HOLONS','OASIS_AVATARS','OASIS_AVATAR_DETAILS')){throw 'Invalid native table identity.'}
            $ids=@($group.Group|ForEach-Object{"HEXTORAW('$($_.Hex)')"})
            if((NativeSql "SELECT COUNT(*) FROM SYSTEM.$($group.Name) WHERE ID IN ($($ids -join ','));") -ne '0'){throw 'Native originals remain; no automatic retry.'}
        }
        foreach($name in $intents.Keys){if((Get-FileHash -LiteralPath (Join-Path $work $name)).Hash -ne $intents[$name]){throw 'Original intent changed during cleanup.'}}
        foreach($name in $intents.Keys){[IO.File]::Delete((Join-Path $work $name))}
        [IO.File]::Move((Join-Path $work 'embedded-type-cleanup.pending.json'),(Join-Path $work 'embedded-type-cleanup.known-not-applied.json'))
        [IO.File]::Move($proof,$resolved)
        '38 reviewed native-hash-matched synthetic rows removed atomically; exact-ID absence independently verified. Original test writes not replayed.'
        return
    }
    $observed=@()
    foreach($target in $targets){
        $hex=[Convert]::ToHexString($target.Id.ToByteArray())
        $payload=NativeSql "SELECT DATA_JSON FROM SYSTEM.$($target.Table) WHERE ID=HEXTORAW('$hex');"
        if([string]::IsNullOrWhiteSpace($payload)){throw 'Original native row is missing; inspect without guessing.'}
        $data=$payload|ConvertFrom-Json -AsHashtable -DateKind String
        if($data.Id -ne $target.Id.ToString()){throw 'Native/canonical identity differs.'}
        $name=if($target.Table -eq 'OASIS_HOLONS'){$data.Name}else{$data.Username}
        if([string]::IsNullOrWhiteSpace($name) -or $name -notmatch 'owned|metadata-snapshot'){throw 'Unexpected synthetic source identity.'}
        $observed+=@{Table=$target.Table;Id=$target.Id.ToString();Intent=$target.Intent;Name=$name;Version=$data.Version;Bytes=[Text.Encoding]::UTF8.GetByteCount($payload);SHA256=[Convert]::ToHexString([Security.Cryptography.SHA256]::HashData([Text.Encoding]::UTF8.GetBytes($payload)));Payload=$payload;Hex=$hex}
    }
    $inventory=Join-Path $work 'embedded-type-failure-inventory.json'
    $summary=[ordered]@{ReportSHA256='2B57C64EC4A84915973BBA905FC6E3505FF8C037F0F8CD5200E6F26A225E9780';Intents=$intents;Rows=@($observed|Select-Object Table,Id,Intent,Name,Version,Bytes,SHA256)}
    $bytes=[Text.Encoding]::UTF8.GetBytes(($summary|ConvertTo-Json -Depth 6 -Compress))
    if(!$RemoveVerifiedFixtures -and !$VerifyCleanupProgramReadOnly){
        if(Test-Path -LiteralPath $inventory){throw 'Review existing inventory; do not overwrite.'}
        $file=[IO.File]::Open($inventory,[IO.FileMode]::CreateNew,[IO.FileAccess]::Write,[IO.FileShare]::None)
        try{$file.Write($bytes);$file.Flush($true)}finally{$file.Dispose()}
        [pscustomobject]@{Rows=$observed.Count;Bytes=$bytes.Length;SHA256=(Get-FileHash -LiteralPath $inventory).Hash;NativeWrites=0}|ConvertTo-Json -Compress
        return
    }
    $reviewedHash='E05172A9A9AAE6C57728E3C7E874EB6888FA8C33731243E72E7EF99C5F46B175'
    if((Get-FileHash -LiteralPath $inventory).Hash -ne $reviewedHash -or [Convert]::ToHexString([Security.Cryptography.SHA256]::HashData($bytes)) -ne $reviewedHash){throw 'Exact reviewed inventory and unchanged native originals required.'}
    $proof=Join-Path $work 'embedded-type-cleanup.pending.json'
    $resolved=Join-Path $work 'embedded-type-cleanup.resolved.json'
    if(!$VerifyCleanupProgramReadOnly -and ((Test-Path -LiteralPath $proof) -or (Test-Path -LiteralPath $resolved))){throw 'Review original cleanup outcome; never repeat.'}
    $proofBytes=[Text.Encoding]::UTF8.GetBytes((@{InventorySHA256=$reviewedHash;Rows=38;State='before-exact-conditional-delete'}|ConvertTo-Json -Compress))
    if(!$VerifyCleanupProgramReadOnly){
        $file=[IO.File]::Open($proof,[IO.FileMode]::CreateNew,[IO.FileAccess]::Write,[IO.FileShare]::None)
        try{$file.Write($proofBytes);$file.Flush($true)}finally{$file.Dispose()}
    }
    $statements=@('DECLARE payload CLOB; matched PLS_INTEGER; BEGIN')
    foreach($row in $observed){
        if($row.Bytes -gt 30000){throw 'Original exceeds reviewed CLOB assembly bound.'}
        $statements+='payload:=EMPTY_CLOB();'
        for($offset=0;$offset -lt $row.Payload.Length;$offset+=800){
            $piece=$row.Payload.Substring($offset,[Math]::Min(800,$row.Payload.Length-$offset)).Replace("'","''")
            $statements+="payload:=payload||TO_CLOB('$piece');"
        }
        $statements+=if($VerifyCleanupProgramReadOnly){"SELECT COUNT(*) INTO matched FROM SYSTEM.$($row.Table) WHERE ID=HEXTORAW('$($row.Hex)') AND DBMS_LOB.COMPARE(DATA_JSON,payload)=0; IF matched<>1 THEN RAISE_APPLICATION_ERROR(-20001,'Original changed'); END IF;"}else{"DELETE FROM SYSTEM.$($row.Table) WHERE ID=HEXTORAW('$($row.Hex)') AND DBMS_LOB.COMPARE(DATA_JSON,payload)=0; IF SQL%ROWCOUNT<>1 THEN RAISE_APPLICATION_ERROR(-20001,'Original changed'); END IF;"}
    }
    $statements+=if($VerifyCleanupProgramReadOnly){"END;`n/"}else{"COMMIT; END;`n/"}
    [void](NativeSql ($statements -join "`n"))
    if($VerifyCleanupProgramReadOnly){'Read-only full-CLOB program matched all38 unchanged originals; no native mutation.';return}
    foreach($row in $observed){
        if((NativeSql "SELECT COUNT(*) FROM SYSTEM.$($row.Table) WHERE ID=HEXTORAW('$($row.Hex)');") -ne '0'){throw 'Native original remains; no automatic retry.'}
    }
    foreach($name in $intents.Keys){if((Get-FileHash -LiteralPath (Join-Path $work $name)).Hash -ne $intents[$name]){throw 'Intent changed during cleanup.'}}
    foreach($name in $intents.Keys){[IO.File]::Delete((Join-Path $work $name))}
    [IO.File]::Move($proof,$resolved)
    '38 reviewed original synthetic rows conditionally removed in one transaction and independently absent; no original write replay.'
}finally{$lease.Dispose()}
