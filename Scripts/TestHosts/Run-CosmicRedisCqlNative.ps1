#Requires -Version 7.5
param([switch]$Federated,[switch]$Serve)
if($Serve -and !$Federated){throw "Serve requires explicit federation."}
$ErrorActionPreference='Stop'
$root=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
$state=Join-Path $root 'LocalState/COSMIC/RedisCql'
$results=Join-Path $root 'TestResults/RedisCqlCurrent'
[void][IO.Directory]::CreateDirectory($state)
[void][IO.Directory]::CreateDirectory($results)
& (Join-Path $PSScriptRoot 'Invoke-WithProviderBuildLease.ps1') -Owner 'COSMIC Redis native CQL' -Operation {
    $lease=[IO.File]::Open((Join-Path $state 'runner.lock'),'OpenOrCreate','ReadWrite','None')
    $names=@('OASIS_REDIS_NATIVE_PASSWORD','OASIS_REDIS_STORAGE_ENCRYPTION_KEY','OASIS_REDIS_CQL_STATE','OASIS_MONGO_REPLICA_SET_CONNECTION','OASIS_REDIS_MONGO_SERVE')
    $prior=@{};foreach($name in $names){$prior[$name]=[Environment]::GetEnvironmentVariable($name)}
    $started=$false
    $mongo=$null
    try {
        if(Test-Path (Join-Path $state 'demo-seed.pending.json')){throw 'Original source setup unresolved; preserve it.'}
        if(Test-Path (Join-Path $state 'mongo.pending.json')){throw 'Original Mongo outcome unresolved.'}
        if($Federated -and !(Test-Path (Join-Path $state 'demo-seed.json'))){throw 'Original source must be admitted first.'}
        $identity=(& wsl -d Ubuntu -u root -- podman inspect oasis-redis-retained --format '{{.Id}} {{.State.Status}}')-join ''
        if($LASTEXITCODE -or $identity.Trim() -ne '48458f978518783740065970cccbf74f6f8626a97660c625467fa561bf4d8005 exited'){throw 'Original Redis container must exist and be stopped; no adoption/provisioning.'}
        $project=Join-Path $root 'Providers/Storage/TestProjects/NextGenSoftware.OASIS.API.Providers.RedisOASIS.IntegrationTests'
        & dotnet restore $project --source C:/Users/david/.nuget/packages /p:NuGetAudit=false /m:2 /nr:false *> (Join-Path $results 'native-restore.log')
        if($LASTEXITCODE){throw 'Offline native restore failed.'}
        & dotnet build (Join-Path $root 'Providers/Storage/NextGenSoftware.OASIS.API.Providers.RedisOASIS') --no-restore /m:2 /nr:false /p:UseSharedCompilation=false /p:BuildProjectReferences=false *> (Join-Path $results 'native-provider-build.log')
        if($LASTEXITCODE){throw 'Redis producer failed.'}
        if($Federated){
            & dotnet build (Join-Path $root 'Providers/Storage/NextGenSoftware.OASIS.API.Providers.MongoOASIS') --no-restore /m:2 /nr:false /p:UseSharedCompilation=false /p:BuildProjectReferences=false *> (Join-Path $results 'mongo-build.log')
            if($LASTEXITCODE){throw 'Mongo producer failed.'}
        }
        & dotnet build $project --no-restore /m:2 /nr:false /p:UseSharedCompilation=false /p:BuildProjectReferences=false *> (Join-Path $results 'native-build.log')
        if($LASTEXITCODE){throw 'Native consumer failed.'}
        $secure=& (Join-Path $PSScriptRoot 'Get-ReusableRedisTestCredential.ps1')
        try{$env:OASIS_REDIS_NATIVE_PASSWORD=[Net.NetworkCredential]::new('', $secure).Password}finally{$secure.Dispose()}
        $secure=& (Join-Path $PSScriptRoot 'Get-ReusableRedisStorageKey.ps1')
        try{$env:OASIS_REDIS_STORAGE_ENCRYPTION_KEY=[Net.NetworkCredential]::new('', $secure).Password}finally{$secure.Dispose()}
        $env:OASIS_REDIS_CQL_STATE=$state
        # Ownership was established while this exact container was stopped.
        # Teardown must also reconcile a helper failure after startup began.
        $started=$true
        & (Join-Path $PSScriptRoot 'Use-ReusableRedisContainer.ps1') *> (Join-Path $results 'native-service.log')
        if($LASTEXITCODE){throw 'Retained Redis startup failed.'}
        & dotnet test $project --no-build --no-restore /m:2 /nr:false /p:UseSharedCompilation=false --filter FullyQualifiedName~RedisCqlNativeTests --results-directory $results --logger 'trx;LogFileName=redis-cql-native.trx' *> (Join-Path $results 'native.log')
        if($LASTEXITCODE){throw 'Native Redis CQL failed; preserve source intent/data.'}
        [xml]$report=Get-Content (Join-Path $results 'redis-cql-native.trx')
        if($report.TestRun.ResultSummary.Counters.total -ne '1' -or $report.TestRun.ResultSummary.Counters.passed -ne '1'){throw 'Exactly one native pass required.'}
        Get-Content (Join-Path $results 'native.log') -Tail 3
        if($Federated){
            if(Get-NetTCPConnection -State Listen -LocalPort 27029,5111 -ErrorAction SilentlyContinue){throw 'Paired ports occupied; no process adoption.'}
            $runtime='C:/Program Files/MongoDB/Server/8.3/bin/mongod.exe'
            $data=Join-Path $root 'LocalState/COSMIC/mongo-data'
            if(!(Test-Path (Join-Path $data 'WiredTiger'))){throw 'Original Mongo store missing.'}
            if(((& $runtime --version)-join ' ') -notmatch 'v8\.3\.11\b'){throw 'Pinned Mongo version differs.'}
            $mongo=Start-Process $runtime -ArgumentList @('--dbpath',$data,'--bind_ip','127.0.0.1','--port','27029','--replSet','oasisRecoveryRs','--wiredTigerCacheSizeGB','0.25','--oplogSize','32','--logpath',(Join-Path $results 'mongo-service.log'),'--logappend') -WindowStyle Hidden -PassThru
            $mongoReady=$false
            for($i=0;$i -lt 40;$i++){
                if($mongo.HasExited){throw 'Owned Mongo exited before readiness.'}
                $listeners=Get-NetTCPConnection -State Listen -LocalPort 27029 -ErrorAction SilentlyContinue
                if($listeners){if(@($listeners|Where-Object OwningProcess -ne $mongo.Id).Count){throw 'Mongo listener owner differs.'};$mongoReady=$true;break}
                Start-Sleep -Milliseconds 500
            }
            if(!$mongoReady){throw 'Owned Mongo startup deadline exceeded.'}
            $env:OASIS_MONGO_REPLICA_SET_CONNECTION='mongodb://127.0.0.1:27029/?replicaSet=oasisRecoveryRs&serverSelectionTimeoutMS=3000'
            $env:OASIS_REDIS_MONGO_SERVE=if($Serve){'1'}else{'0'}
            & dotnet test $project --no-build --no-restore /m:2 /nr:false /p:UseSharedCompilation=false --filter FullyQualifiedName~RedisMongoCqlNativeTests --logger 'trx;LogFileName=redis-mongo-federation.trx' --results-directory $results -v:q *> (Join-Path $results 'redis-mongo-federation.log')
            if($LASTEXITCODE){throw 'Paired Redis CQL failed; preserve ownership journal.'}
            [xml]$paired=Get-Content (Join-Path $results 'redis-mongo-federation.trx')
            if($paired.TestRun.ResultSummary.Counters.total -ne '1' -or $paired.TestRun.ResultSummary.Counters.passed -ne '1'){throw 'Exactly one paired pass required.'}
            if(Test-Path (Join-Path $state 'mongo.pending.json')){throw 'Mongo fixture cleanup incomplete.'}
            Get-Content (Join-Path $results 'redis-mongo-federation.log') -Tail 4
        }
    }
    finally {
        try {
            try{if($mongo){if(!$mongo.HasExited){$mongo.Kill();if(!$mongo.WaitForExit(30000)){throw 'Owned Mongo teardown incomplete.'}};$mongo.Dispose()}}
            finally{if($started){& (Join-Path $PSScriptRoot 'Use-ReusableRedisContainer.ps1') -Stop *> (Join-Path $results 'native-stop.log')}}
        }
        finally{foreach($name in $names){[Environment]::SetEnvironmentVariable($name,$prior[$name])};$lease.Dispose()}
    }
}
