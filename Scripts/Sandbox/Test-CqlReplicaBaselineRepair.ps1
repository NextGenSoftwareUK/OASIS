# Reuse the admitted Windows Mongo environment; never create a new store/runtime.
param([switch]$SqliteSource,[switch]$NeoSource,[switch]$NeoTarget,[switch]$ManagerCutover)
if(@($SqliteSource,$NeoSource,$NeoTarget,$ManagerCutover | Where-Object {$_}).Count -gt 1){throw "Choose one exclusive route proof."}
$ErrorActionPreference='Stop'
$root=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
$runtime='C:/Program Files/MongoDB/Server/8.3/bin/mongod.exe'
$data=Join-Path $root 'LocalState/COSMIC/mongo-data'
$results=Join-Path $root 'TestResults/ReplicaRepairNativeCurrent'
if(!(Test-Path (Join-Path $data 'WiredTiger'))){throw 'Retained Mongo store required; no provisioning.'}
if(Test-Path Env:COSMIC_REPLICA_REPAIR_NATIVE){throw 'Existing native profile must not be overwritten.'}
New-Item -ItemType Directory -Path $results -Force | Out-Null
& (Join-Path $root 'Scripts/TestHosts/Invoke-WithProviderBuildLease.ps1') -Owner cosmic-native-repair-proof -Operation {
    $owned=$null
    $neoOwned=$null
    try {
        if(Get-NetTCPConnection -State Listen -LocalPort 27029 -ErrorAction SilentlyContinue){throw 'Mongo port occupied; no process adoption.'}
        if(((& $runtime --version)-join ' ') -notmatch 'v8\.3\.11\b'){throw 'Admitted Mongo8.3.11 required.'}
        $owned=Start-Process $runtime -ArgumentList @('--dbpath',$data,'--bind_ip','127.0.0.1','--port','27029','--replSet','oasisRecoveryRs',
            '--wiredTigerCacheSizeGB','0.25','--oplogSize','32','--logpath',(Join-Path $results 'mongo-service.log'),'--logappend') -WindowStyle Hidden -PassThru
        $ready=$false
        for($attempt=0;$attempt -lt 40;$attempt++) {
            if($owned.HasExited){throw 'Owned Mongo exited before readiness.'}
            $listeners=Get-NetTCPConnection -State Listen -LocalPort 27029 -ErrorAction SilentlyContinue
            if($listeners){if(@($listeners|Where-Object OwningProcess -ne $owned.Id).Count){throw 'Listener ownership differs.'};$ready=$true;break}
            Start-Sleep -Milliseconds 500
        }
        if(!$ready){throw 'Owned service readiness deadline exceeded; do not restart.'}
        if($NeoSource -or $NeoTarget -or $ManagerCutover){
            $neoState=(& wsl -d Ubuntu -u root -- podman inspect oasis-neo4j --format '{{.State.Status}} {{.Image}}') -join ''
            if($LASTEXITCODE -ne 0 -or $neoState -ne 'exited 6e47503356fe6b0e8f3ba48afc714ed95475d0c32ac97c62b30737af2e8846bd'){throw 'Retained stopped pinned Neo container required; no adoption/provisioning.'}
            $neoOwned=Start-Process -FilePath 'wsl.exe' -ArgumentList @('-d','Ubuntu','-u','root','--','podman','start','--attach','oasis-neo4j') -PassThru -WindowStyle Hidden -RedirectStandardOutput (Join-Path $results 'neo-attached.log') -RedirectStandardError (Join-Path $results 'neo-attached.err.log')
            $neoReady=$false;$neoDeadline=[DateTime]::UtcNow.AddSeconds(60)
            do {
                if($neoOwned.HasExited){throw 'Owned Neo attachment exited before readiness.'}
                & wsl -d Ubuntu -u root -- podman exec oasis-neo4j cypher-shell -u neo4j -p oasis-local-test-only 'RETURN 1;' 2>$null | Out-Null
                $neoReady=$LASTEXITCODE -eq 0
                if(!$neoReady){Start-Sleep -Milliseconds 500}
            }while(!$neoReady -and [DateTime]::UtcNow -lt $neoDeadline)
            if(!$neoReady){throw 'Owned Neo readiness deadline exceeded; preserve fixture.'}
        }
        $env:COSMIC_REPLICA_REPAIR_NATIVE='owned-local-27029'
        $filter=if($ManagerCutover){'FullyQualifiedName~ManagerSaveMaintainsRecoveredMongoToNeoRoute|FullyQualifiedName~ManagerSaveMaintainsRecoveredSqliteToNeoRoute'}elseif($NeoTarget){'FullyQualifiedName~ReviewedCommandRepairsActualSqliteToNeo|FullyQualifiedName~ReviewedCommandRepairsActualMongoToNeo'}elseif($NeoSource){'FullyQualifiedName~ReviewedCommandRepairsActualNeoToMongo'}elseif($SqliteSource){'FullyQualifiedName~ReviewedCommandRepairsActualSqliteToMongo'}else{'FullyQualifiedName~ReviewedCommandRepairsActualMongoToSqlite'}
        $reportName=if($ManagerCutover){'managed-neo-cutover.trx'}elseif($NeoTarget){'reviewed-neo-target-repair.trx'}elseif($NeoSource){'neo-mongo-repair.trx'}elseif($SqliteSource){'sqlite-mongo-repair.trx'}else{'mongo-sqlite-repair.trx'}
        dotnet test (Join-Path $root 'ONODE/NextGenSoftware.OASIS.API.ONODE.WebAPI.UnitTests/NextGenSoftware.OASIS.API.ONODE.WebAPI.UnitTests.csproj') `
            --no-build --no-restore --filter $filter --results-directory $results `
            --logger "trx;LogFileName=$reportName" /m:2 /nr:false /p:UseSharedCompilation=false -v:q *> (Join-Path $results 'native-test.log')
        if($LASTEXITCODE){throw 'Native repair proof failed; preserve owned fixtures/journals.'}
        [xml]$report=Get-Content (Join-Path $results $reportName) -Raw
        $counts=$report.TestRun.ResultSummary.Counters
        $expectedCount=if($NeoTarget -or $ManagerCutover){2}else{1}
        if([int]$counts.total -ne $expectedCount -or [int]$counts.passed -ne $expectedCount -or [int]$counts.notExecuted -ne 0){throw 'Exact selected native pass count, no skips required.'}
        Get-Content (Join-Path $results 'native-test.log') -Tail 4
    }
    finally {
        Remove-Item Env:COSMIC_REPLICA_REPAIR_NATIVE -ErrorAction SilentlyContinue
        try {
        if($neoOwned){
            try {
                & wsl -d Ubuntu -u root -- podman stop --time 20 oasis-neo4j | Out-Null
                if($LASTEXITCODE){throw 'Owned Neo stop failed; preserve process evidence.'}
                if(!$neoOwned.WaitForExit(30000)){throw 'Owned Neo attachment did not exit.'}
            }finally{$neoOwned.Dispose()}
        }
        } finally {
        if($owned){if(!$owned.HasExited){$owned.Kill();if(!$owned.WaitForExit(30000)){throw 'Owned service did not stop.'}};$owned.Dispose()}
        }
    }
}
