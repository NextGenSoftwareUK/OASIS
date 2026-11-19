param([switch]$Federated,[switch]$Serve)
if($Serve -and !$Federated){throw "Serve requires explicit federation."}
$ErrorActionPreference='Stop'
$root=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
$state=Join-Path $root 'LocalState/COSMIC/DuckDbCql'
$results=Join-Path $root 'TestResults/DuckDbCqlCurrent'
[void][IO.Directory]::CreateDirectory($state)
[void][IO.Directory]::CreateDirectory($results)
& (Join-Path $PSScriptRoot 'Invoke-WithProviderBuildLease.ps1') -Owner 'COSMIC DuckDB native CQL' -Operation {
    $lease=[IO.File]::Open((Join-Path $state 'runner.lock'),'OpenOrCreate','ReadWrite','None')
    $oldConnection=$env:OASIS_DUCKDB_CQL_CONNECTION;$oldState=$env:OASIS_DUCKDB_CQL_STATE
    $priorMongo=$env:OASIS_MONGO_REPLICA_SET_CONNECTION;$priorServe=$env:OASIS_DUCKDB_MONGO_SERVE
    $mongo=$null
    try {
        if(Test-Path (Join-Path $state 'demo-seed.pending.json')){throw 'Original source setup unresolved; preserve without replay.'}
        if(Test-Path (Join-Path $state 'mongo.pending.json')){throw "Original Mongo fixture unresolved."}
        if($Federated -and !(Test-Path (Join-Path $state 'demo-seed.json'))){throw "Original source required before federation."}
        if((Test-Path (Join-Path $state 'demo-seed.json')) -and !(Test-Path (Join-Path $state 'source.duckdb'))){throw 'Original DuckDB source missing; no replacement database.'}
        # Preserve prior successful evidence; never overwrite an unresolved failure.
        foreach($name in @('duckdb-cql-native.trx','duckdb-mongo-federation.trx')) {
            $reportPath=Join-Path $results $name
            if(Test-Path $reportPath){
                [xml]$priorReport=Get-Content $reportPath
                if($priorReport.TestRun.ResultSummary.Counters.passed -ne '1' -or $priorReport.TestRun.ResultSummary.Counters.total -ne '1'){throw 'Preserve unresolved prior report before running again.'}
                $archive=Join-Path $results 'Archive'
                New-Item -ItemType Directory -Force $archive | Out-Null
                $target=Join-Path $archive ((Get-FileHash $reportPath).Hash+'-'+$name)
                if(Test-Path $target){throw 'Prior report already archived; reconcile explicitly.'}
                Move-Item -LiteralPath $reportPath -Destination $target
            }
        }
        $project=Join-Path $root 'Providers/Storage/TestProjects/NextGenSoftware.OASIS.API.Providers.DuckDBOASIS.IntegrationTests'
        & dotnet restore $project --source C:/Users/david/.nuget/packages /p:NuGetAudit=false /p:RestoreRecursive=false /m:2 /nr:false *> (Join-Path $results 'native-restore.log')
        if($LASTEXITCODE){throw 'Cached restore failed.'}
        if($Federated){
            & dotnet build (Join-Path $root 'Providers/Storage/NextGenSoftware.OASIS.API.Providers.MongoOASIS') --no-restore /m:2 /nr:false /p:UseSharedCompilation=false /p:BuildProjectReferences=false *> (Join-Path $results 'mongo-build.log')
            if($LASTEXITCODE){throw 'Mongo producer failed.'}
        }
        & dotnet build (Join-Path $root 'Providers/Storage/NextGenSoftware.OASIS.API.Providers.DuckDBOASIS') --no-restore /m:2 /nr:false /p:UseSharedCompilation=false /p:BuildProjectReferences=false *> (Join-Path $results 'native-provider-build.log')
        if($LASTEXITCODE){throw 'DuckDB producer build failed.'}
        & dotnet build $project --no-restore /m:2 /nr:false /p:UseSharedCompilation=false /p:BuildProjectReferences=false *> (Join-Path $results 'native-build.log')
        if($LASTEXITCODE){throw 'Cached native consumer build failed.'}
        $env:OASIS_DUCKDB_CQL_CONNECTION='Data Source='+(Join-Path $state 'source.duckdb')
        $env:OASIS_DUCKDB_CQL_STATE=$state
        & dotnet test $project --no-build --no-restore /m:2 /nr:false /p:UseSharedCompilation=false --filter FullyQualifiedName~DuckDbCqlNativeTests --results-directory $results --logger 'trx;LogFileName=duckdb-cql-native.trx' *> (Join-Path $results 'native.log')
        if($LASTEXITCODE){throw 'Native DuckDB acceptance failed; retain source/intent.'}
        [xml]$report=Get-Content (Join-Path $results 'duckdb-cql-native.trx') -Raw
        if($report.TestRun.ResultSummary.Counters.passed -ne '1' -or $report.TestRun.ResultSummary.Counters.total -ne '1'){throw 'One native pass without skips required.'}
        if($Federated){
            if(Get-NetTCPConnection -State Listen -LocalPort 27029,5116 -ErrorAction SilentlyContinue){throw 'Paired ports occupied; no process adoption.'}
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
            $env:OASIS_DUCKDB_MONGO_SERVE=if($Serve){'1'}else{'0'}
            & dotnet test $project --no-build --no-restore /m:2 /nr:false /p:UseSharedCompilation=false --filter FullyQualifiedName~DuckDbMongoCqlNativeTests --logger 'trx;LogFileName=duckdb-mongo-federation.trx' --results-directory $results -v:q *> (Join-Path $results 'duckdb-mongo-federation.log')
            if($LASTEXITCODE){throw 'Paired DuckDb CQL failed; preserve ownership journal.'}
            [xml]$paired=Get-Content (Join-Path $results 'duckdb-mongo-federation.trx')
            if($paired.TestRun.ResultSummary.Counters.total -ne '1' -or $paired.TestRun.ResultSummary.Counters.passed -ne '1'){throw 'Exactly one paired pass required.'}
            if(Test-Path (Join-Path $state 'mongo.pending.json')){throw 'Mongo fixture cleanup incomplete.'}
            Get-Content (Join-Path $results 'duckdb-mongo-federation.log') -Tail 4
        }
    } finally {
        $env:OASIS_DUCKDB_CQL_CONNECTION=$oldConnection;$env:OASIS_DUCKDB_CQL_STATE=$oldState;$env:OASIS_MONGO_REPLICA_SET_CONNECTION=$priorMongo;$env:OASIS_DUCKDB_MONGO_SERVE=$priorServe
        if($mongo){if(!$mongo.HasExited){$mongo.Kill();if(!$mongo.WaitForExit(30000)){throw "Owned Mongo stop incomplete."}};$mongo.Dispose()}
        $lease.Dispose()
    }
}
