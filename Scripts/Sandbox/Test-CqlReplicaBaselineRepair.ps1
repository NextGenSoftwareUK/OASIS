# Reuse the admitted Windows Mongo environment; never create a new store/runtime.
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
        $env:COSMIC_REPLICA_REPAIR_NATIVE='owned-local-27029'
        dotnet test (Join-Path $root 'ONODE/NextGenSoftware.OASIS.API.ONODE.WebAPI.UnitTests/NextGenSoftware.OASIS.API.ONODE.WebAPI.UnitTests.csproj') `
            --no-build --no-restore --filter 'FullyQualifiedName~ReplicaBaselineRepairCommandTests' --results-directory $results `
            --logger 'trx;LogFileName=mongo-sqlite-repair.trx' /m:2 /nr:false /p:UseSharedCompilation=false -v:q *> (Join-Path $results 'native-test.log')
        if($LASTEXITCODE){throw 'Native repair proof failed; preserve owned fixtures/journals.'}
        [xml]$report=Get-Content (Join-Path $results 'mongo-sqlite-repair.trx') -Raw
        $counts=$report.TestRun.ResultSummary.Counters
        if([int]$counts.total -ne 1 -or [int]$counts.passed -ne 1 -or [int]$counts.notExecuted -ne 0){throw 'Exactly one native pass, no skips required.'}
        Get-Content (Join-Path $results 'native-test.log') -Tail 4
    }
    finally {
        Remove-Item Env:COSMIC_REPLICA_REPAIR_NATIVE -ErrorAction SilentlyContinue
        if($owned){if(!$owned.HasExited){$owned.Kill();if(!$owned.WaitForExit(30000)){throw 'Owned service did not stop.'}};$owned.Dispose()}
    }
}
