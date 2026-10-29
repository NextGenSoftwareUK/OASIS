#Requires -Version 7.5
param([switch]$Federated,[switch]$Serve)
if($Serve -and !$Federated){throw 'Serve requires explicit federation.'}
$ErrorActionPreference='Stop'
$root=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
. (Join-Path $PSScriptRoot 'ReusableAzuriteHost.ps1')
& (Join-Path $PSScriptRoot 'Invoke-WithProviderBuildLease.ps1') -Owner cosmic-azureblob-cql -Operation {
    $state=Join-Path $root 'LocalState/COSMIC/AzureBlobCql'
    $results=Join-Path $root 'TestResults/AzureBlobCqlCurrent'
    New-Item -ItemType Directory -Force $state,$results | Out-Null
    $lease=[IO.File]::Open((Join-Path $state 'environment.lock'),'OpenOrCreate','ReadWrite','None')
    $hostHandle=$null
    $mongo=$null
    $prior=$env:OASIS_AZURE_BLOB_CQL_STATE
    $oldMongo=$env:OASIS_MONGO_REPLICA_SET_CONNECTION
    $oldServe=$env:OASIS_AZURE_BLOB_MONGO_SERVE
    try {
        if(Test-Path (Join-Path $state 'demo-seed.pending.json')){throw 'Original setup pending; reconcile without replay.'}
        if(Test-Path (Join-Path $state 'mongo.pending.json')){throw 'Original Mongo fixture pending; reconcile first.'}
        if($Federated -and !(Test-Path (Join-Path $state 'demo-seed.json'))){throw 'Verify original native source before federation.'}
        if((Test-Path (Join-Path $state 'demo-seed.json')) -and !(Test-Path (Join-Path $state 'azurite-data'))){throw 'Original native store missing; never replace it.'}
        $project=Join-Path $root 'Providers/Cloud/TestProjects/NextGenSoftware.OASIS.API.Providers.AzureStorageOASIS.IntegrationTests/NextGenSoftware.OASIS.API.Providers.AzureStorageOASIS.IntegrationTests.csproj'
        if($Federated){
            & dotnet restore $project --source C:/Users/david/.nuget/packages /m:2 /nr:false /p:NuGetAudit=false -v:q
            if($LASTEXITCODE){throw 'Offline paired restore failed.'}
            & dotnet build (Join-Path $root 'Providers/Storage/NextGenSoftware.OASIS.API.Providers.MongoOASIS/NextGenSoftware.OASIS.API.Providers.MongoDBOASIS.csproj') --no-restore /p:BuildProjectReferences=false /m:2 /nr:false /p:UseSharedCompilation=false -v:q -clp:ErrorsOnly
            if($LASTEXITCODE){throw 'Mongo producer build failed.'}
        }
        & dotnet build $project --no-restore /p:BuildProjectReferences=false /m:2 /nr:false /p:UseSharedCompilation=false -v:q -clp:ErrorsOnly
        if($LASTEXITCODE){throw 'Native consumer build failed.'}
        $hostHandle=Start-ReusableAzuriteHost -Workspace $results -DataDirectory (Join-Path $state 'azurite-data')
        $ready=$false
        for($i=0;$i -lt 60;$i++){
            if(Assert-ReusableAzuriteListener -HostHandle $hostHandle){$ready=$true;break}
            Start-Sleep -Milliseconds 250
        }
        if(!$ready){throw 'Owned Azurite readiness timed out; no replacement.'}
        $env:OASIS_AZURE_BLOB_CQL_STATE=$state
        & dotnet test $project --no-build --no-restore /m:2 /nr:false /p:UseSharedCompilation=false --filter FullyQualifiedName~AzureBlobCqlNativeTests --logger 'trx;LogFileName=azureblob-cql-native.trx' --results-directory $results -v:q
        if($LASTEXITCODE){throw 'Native Blob CQL failed; preserve source and pending journal.'}
        [xml]$report=Get-Content (Join-Path $results 'azureblob-cql-native.trx')
        if($report.TestRun.ResultSummary.Counters.total -ne '1' -or $report.TestRun.ResultSummary.Counters.passed -ne '1'){throw 'Exactly one native pass required.'}
        if($Federated){
            if(Get-NetTCPConnection -State Listen -LocalPort 27029,5106 -ErrorAction SilentlyContinue){throw 'Paired ports occupied; no process adoption.'}
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
            $env:OASIS_AZURE_BLOB_MONGO_SERVE=if($Serve){'1'}else{'0'}
            & dotnet test $project --no-build --no-restore /m:2 /nr:false /p:UseSharedCompilation=false --filter FullyQualifiedName~AzureBlobMongoCqlNativeTests --logger 'trx;LogFileName=azureblob-mongo-federation.trx' --results-directory $results -v:q *> (Join-Path $results 'azureblob-mongo-federation.log')
            if($LASTEXITCODE){throw 'Paired Blob CQL failed; preserve ownership journal.'}
            [xml]$paired=Get-Content (Join-Path $results 'azureblob-mongo-federation.trx')
            if($paired.TestRun.ResultSummary.Counters.total -ne '1' -or $paired.TestRun.ResultSummary.Counters.passed -ne '1'){throw 'Exactly one paired pass required.'}
            if(Test-Path (Join-Path $state 'mongo.pending.json')){throw 'Mongo fixture cleanup incomplete.'}
            Get-Content (Join-Path $results 'azureblob-mongo-federation.log') -Tail 4
        }
    }
    finally {
        try {if($hostHandle){Stop-ReusableAzuriteHost -HostHandle $hostHandle}}
        finally {
            try{if($mongo){if(!$mongo.HasExited){$mongo.Kill();if(!$mongo.WaitForExit(30000)){throw 'Owned Mongo teardown incomplete.'}};$mongo.Dispose()}}
            finally{$env:OASIS_AZURE_BLOB_CQL_STATE=$prior;$env:OASIS_MONGO_REPLICA_SET_CONNECTION=$oldMongo;$env:OASIS_AZURE_BLOB_MONGO_SERVE=$oldServe;$lease.Dispose()}
        }
    }
}
