param([switch]$Federated,[switch]$Serve)
if($Serve -and !$Federated){throw "Serve requires explicit federation."}
$ErrorActionPreference='Stop'
$root=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
$state=Join-Path $root 'LocalState/COSMIC/SqlServerCql'
$results=Join-Path $root 'TestResults/SqlServerCqlCurrent'
[void][IO.Directory]::CreateDirectory($state)
[void][IO.Directory]::CreateDirectory($results)
$tool='C:/Program Files/Microsoft SQL Server/170/Tools/Binn/SqlLocalDB.exe'
$instance='CosmicCql'
$instanceDirectory=Join-Path $env:LOCALAPPDATA 'Microsoft/Microsoft SQL Server Local DB/Instances/CosmicCql'
& (Join-Path $PSScriptRoot 'Invoke-WithProviderBuildLease.ps1') -Owner 'COSMIC SQL Server native CQL' -Operation {
    $lease=[IO.File]::Open((Join-Path $state 'runner.lock'),'OpenOrCreate','ReadWrite','None')
    $priorConnection=$env:OASIS_SQLSERVER_CQL_CONNECTION;$priorState=$env:OASIS_SQLSERVER_CQL_STATE
    $oldMongo=$env:OASIS_MONGO_REPLICA_SET_CONNECTION;$oldServe=$env:OASIS_SQLSERVER_MONGO_SERVE
    $mongo=$null
    $started=$false;$passed=$false
    try {
        if(Test-Path (Join-Path $state 'demo-seed.pending.json')){throw 'Source setup unresolved; do not replay.'}
        if(Test-Path (Join-Path $state 'mongo.pending.json')){throw 'Original Mongo fixture unresolved.'}
        if($Federated -and !(Test-Path (Join-Path $state 'demo-seed.json'))){throw 'Original source must be admitted first.'}
        $admission=Join-Path $state 'instance.json';$pending=Join-Path $state 'instance.pending.json'
        if(Test-Path $pending){throw 'Instance creation unresolved; inspect original outcome.'}
        $versions=(& $tool versions)-join ' '
        if($LASTEXITCODE -or $versions -notmatch '17\.0\.4025\.3'){throw 'Existing pinned SQL Server2025 runtime required.'}
        if(!(Test-Path $admission)){
            if(Test-Path $instanceDirectory){throw 'Unowned instance exists; no adoption.'}
            [IO.File]::WriteAllText($pending,(@{instance=$instance;version='17.0.4025.3';directory=$instanceDirectory}|ConvertTo-Json))
            & $tool create $instance 17.0 *> (Join-Path $results 'instance-create.log')
            if($LASTEXITCODE){throw 'Instance creation failed; retain intent.'}
            Move-Item -LiteralPath $pending -Destination $admission
        }
        $record=Get-Content $admission -Raw|ConvertFrom-Json
        if($record.instance -ne $instance -or $record.version -ne '17.0.4025.3' -or $record.directory -ne $instanceDirectory -or !(Test-Path $instanceDirectory)){throw 'Original instance admission differs.'}
        $info=(& $tool info $instance)-join "`n"
        if($LASTEXITCODE -or $info -notmatch 'State:\s+Stopped'){throw 'Expected stopped owned instance; no process adoption.'}
        $project=Join-Path $root 'Providers/Storage/TestProjects/NextGenSoftware.OASIS.API.Providers.SQLServerDBOASIS.IntegrationTests'
        & dotnet restore $project --source C:/Users/david/.nuget/packages /p:NuGetAudit=false /p:RestoreRecursive=false /m:2 /nr:false *> (Join-Path $results 'native-restore.log')
        if($LASTEXITCODE){throw 'Cached restore failed.'}
        if($Federated){
            & dotnet build (Join-Path $root 'Providers/Storage/NextGenSoftware.OASIS.API.Providers.MongoOASIS') --no-restore /m:2 /nr:false /p:UseSharedCompilation=false /p:BuildProjectReferences=false *> (Join-Path $results 'mongo-build.log')
            if($LASTEXITCODE){throw 'Mongo producer failed.'}
        }
        & dotnet build $project --no-restore /m:2 /nr:false /p:UseSharedCompilation=false /p:BuildProjectReferences=false *> (Join-Path $results 'native-build.log')
        if($LASTEXITCODE){throw 'Cached native consumer build failed.'}
        $started=$true
        & $tool start $instance *> (Join-Path $results 'instance-start.log')
        if($LASTEXITCODE){throw 'Owned LocalDB start failed.'}
        $env:OASIS_SQLSERVER_CQL_CONNECTION='Server=(localdb)\CosmicCql;Database=cosmic_cql_demo;Integrated Security=true;Encrypt=false;Pooling=false;Connect Timeout=10'
        $env:OASIS_SQLSERVER_CQL_STATE=$state
        & dotnet test $project --no-build --no-restore /m:2 /nr:false /p:UseSharedCompilation=false --filter FullyQualifiedName~SqlServerCqlNativeTests --results-directory $results --logger 'trx;LogFileName=sqlserver-cql-native.trx' *> (Join-Path $results 'native.log')
        if($LASTEXITCODE){throw 'Native acceptance failed; retain original source/journals.'}
        [xml]$report=Get-Content (Join-Path $results 'sqlserver-cql-native.trx') -Raw
        if($report.TestRun.ResultSummary.Counters.passed -ne '1' -or $report.TestRun.ResultSummary.Counters.total -ne '1'){throw 'One native pass without skips required.'}
        if($Federated){
            if(Get-NetTCPConnection -State Listen -LocalPort 27029,5113 -ErrorAction SilentlyContinue){throw 'Paired ports occupied; no process adoption.'}
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
            $env:OASIS_SQLSERVER_MONGO_SERVE=if($Serve){'1'}else{'0'}
            & dotnet test $project --no-build --no-restore /m:2 /nr:false /p:UseSharedCompilation=false --filter FullyQualifiedName~SqlServerMongoCqlNativeTests --logger 'trx;LogFileName=sqlserver-mongo-federation.trx' --results-directory $results -v:q *> (Join-Path $results 'sqlserver-mongo-federation.log')
            if($LASTEXITCODE){throw 'Paired SqlServer CQL failed; preserve ownership journal.'}
            [xml]$paired=Get-Content (Join-Path $results 'sqlserver-mongo-federation.trx')
            if($paired.TestRun.ResultSummary.Counters.total -ne '1' -or $paired.TestRun.ResultSummary.Counters.passed -ne '1'){throw 'Exactly one paired pass required.'}
            if(Test-Path (Join-Path $state 'mongo.pending.json')){throw 'Mongo fixture cleanup incomplete.'}
            Get-Content (Join-Path $results 'sqlserver-mongo-federation.log') -Tail 4
        }
        $passed=$true
    } finally {
        $env:OASIS_SQLSERVER_CQL_CONNECTION=$priorConnection;$env:OASIS_SQLSERVER_CQL_STATE=$priorState;$env:OASIS_MONGO_REPLICA_SET_CONNECTION=$oldMongo;$env:OASIS_SQLSERVER_MONGO_SERVE=$oldServe
        if($mongo){if(!$mongo.HasExited){$mongo.Kill();if(!$mongo.WaitForExit(30000)){throw "Owned Mongo teardown incomplete."}};$mongo.Dispose()}
        try {if($started -and $passed){& $tool stop $instance *> (Join-Path $results 'instance-stop.log');if($LASTEXITCODE){throw 'Owned instance stop failed.'}}}
        finally {$lease.Dispose()}
    }
}
