#Requires -Version 7.0
param([switch]$Federated,[switch]$Serve)
$ErrorActionPreference='Stop'
if($Serve -and !$Federated){throw 'Serve requires the explicit federated demo.'}
$root=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
Set-Location $root
& Scripts/TestHosts/Invoke-WithProviderBuildLease.ps1 -Owner cosmic-aws-cql-native -Operation {
    $state=Join-Path $root 'LocalState/COSMIC/AwsCql'
    $results=Join-Path $root 'TestResults/AwsCqlCurrent'
    New-Item -ItemType Directory -Force -Path $state,$results | Out-Null
    foreach($required in @('demo-seed.json','dynamo-data/shared-local-instance.db','runtime-binding-ready.acknowledged.json')){
        if(!(Test-Path -LiteralPath (Join-Path $state $required))){throw ('Retained CQL prerequisite missing: '+$required)}
    }
    if(Test-Path -LiteralPath (Join-Path $state 'demo-seed.json.pending')){throw 'Unresolved source seed outcome; no replay.'}
    foreach($report in @('aws-cql-native.trx','aws-mongo-federation.trx')){
        if(Test-Path -LiteralPath (Join-Path $results $report)){throw ('Existing native result requires review before another run: '+$report)}
    }
    $environmentLease=[IO.File]::Open((Join-Path $state 'environment.lock'),[IO.FileMode]::OpenOrCreate,[IO.FileAccess]::ReadWrite,[IO.FileShare]::None)
    $priorEndpoint=$env:OASIS_AWS_CQL_ENDPOINT;$priorState=$env:OASIS_AWS_CQL_STATE
    $priorResume=$env:OASIS_AWS_CQL_RESUME_UNWRITTEN
    $priorMongo=$env:OASIS_MONGO_REPLICA_SET_CONNECTION;$priorServe=$env:OASIS_AWS_MONGO_SERVE
    $startedByRun=$false
    $ownedMongo=$null
    try{
        $project='Providers/Cloud/TestProjects/NextGenSoftware.OASIS.API.Providers.AWSOASIS.IntegrationTests/NextGenSoftware.OASIS.API.Providers.AWSOASIS.IntegrationTests.csproj'
        $assets=Join-Path (Split-Path $project) 'obj/project.assets.json'
        if(!(Test-Path $assets) -or (Get-Content $assets -Raw) -notmatch 'NextGenSoftware.OASIS.API.Providers.MongoDBOASIS'){
            dotnet restore $project --source C:/Users/david/.nuget/packages /m:2 /nr:false /p:UseSharedCompilation=false -v:q *> (Join-Path $results 'native-build.log')
            if($LASTEXITCODE){throw 'Offline AWS integration assets unavailable.'}
        }
        foreach($producer in @(
            'Providers/Shared/NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage/NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage.csproj',
            'Providers/Storage/NextGenSoftware.OASIS.API.Providers.MongoOASIS/NextGenSoftware.OASIS.API.Providers.MongoDBOASIS.csproj',
            'Providers/Cloud/NextGenSoftware.OASIS.API.Providers.DynamoDBOASIS/NextGenSoftware.OASIS.API.Providers.DynamoDBOASIS.csproj',
            'Providers/Cloud/NextGenSoftware.OASIS.API.Providers.AWSOASIS/NextGenSoftware.OASIS.API.Providers.AWSOASIS.csproj', $project)){
            dotnet build $producer --no-restore /m:2 /nr:false /p:UseSharedCompilation=false /p:BuildProjectReferences=false -v:q -clp:ErrorsOnly >> (Join-Path $results 'native-build.log')
            if($LASTEXITCODE){throw ('AWS producer failed: '+$producer)}
        }
        & Scripts/TestHosts/Use-CosmicDynamoEnvironment.ps1
        $startedByRun=$true
        $env:OASIS_AWS_CQL_ENDPOINT='http://127.0.0.1:18002';$env:OASIS_AWS_CQL_STATE=$state
        $env:OASIS_AWS_CQL_RESUME_UNWRITTEN='false'
        dotnet test $project --no-build --no-restore --filter 'FullyQualifiedName~NativeAwsCqlReadsThreeRecordsAndFiltersTwoWithoutScanning' --logger 'trx;LogFileName=aws-cql-native.trx' --results-directory $results *> (Join-Path $results 'aws-cql-native.log')
        if($LASTEXITCODE){throw 'Native AWS CQL failed; preserve original seed/state.'}
        [xml]$report=Get-Content (Join-Path $results 'aws-cql-native.trx')
        if($report.TestRun.ResultSummary.Counters.total -ne '1' -or $report.TestRun.ResultSummary.Counters.passed -ne '1'){throw 'Expected exactly one native AWS CQL case.'}
        Get-Content (Join-Path $results 'aws-cql-native.log') -Tail 5
        if($Federated){
            $mongoRuntime='C:/Program Files/MongoDB/Server/8.3/bin/mongod.exe'
            $mongoData=Join-Path $root 'LocalState/COSMIC/mongo-data'
            if(!(Test-Path (Join-Path $mongoData 'WiredTiger'))){throw 'Existing Mongo environment is missing; no reset.'}
            if(Get-NetTCPConnection -State Listen -LocalPort 27029 -ErrorAction SilentlyContinue){throw 'Mongo demo port occupied.'}
            if(((& $mongoRuntime --version)-join ' ') -notmatch 'v8\.3\.11\b'){throw 'Pinned Mongo runtime differs.'}
            $ownedMongo=Start-Process $mongoRuntime -ArgumentList @('--dbpath',$mongoData,'--bind_ip','127.0.0.1','--port','27029','--replSet','oasisRecoveryRs','--wiredTigerCacheSizeGB','0.25','--oplogSize','32','--logpath',(Join-Path $results 'mongo-service.log'),'--logappend') -WindowStyle Hidden -PassThru
            $ready=$false
            for($i=0;$i -lt 40;$i++){
                if($ownedMongo.HasExited){throw 'Owned Mongo exited during startup.'}
                $listeners=Get-NetTCPConnection -State Listen -LocalPort 27029 -ErrorAction SilentlyContinue
                if($listeners){if(@($listeners|Where-Object OwningProcess -ne $ownedMongo.Id).Count){throw 'Mongo listener owner differs.'};$ready=$true;break}
                Start-Sleep -Milliseconds 500
            }
            if(!$ready){throw 'Owned Mongo startup deadline exceeded.'}
            $env:OASIS_MONGO_REPLICA_SET_CONNECTION='mongodb://127.0.0.1:27029/?replicaSet=oasisRecoveryRs&serverSelectionTimeoutMS=3000'
            $env:OASIS_AWS_MONGO_SERVE=if($Serve){'1'}else{'0'}
            dotnet test $project --no-build --no-restore --filter 'FullyQualifiedName~NativeAwsMongoFederationRunsSdkAndHttpWithProviderProvenance' --logger 'trx;LogFileName=aws-mongo-federation.trx' --results-directory $results *> (Join-Path $results 'aws-mongo-federation.log')
            if($LASTEXITCODE){throw 'AWS Mongo demo failed; preserve pending fixture.'}
            [xml]$paired=Get-Content (Join-Path $results 'aws-mongo-federation.trx')
            if($paired.TestRun.ResultSummary.Counters.total -ne '1' -or $paired.TestRun.ResultSummary.Counters.passed -ne '1'){throw 'Exactly one fresh federation case required.'}
            if(Test-Path (Join-Path $state 'mongo.pending.json')){throw 'Mongo fixture cleanup incomplete.'}
            Get-Content (Join-Path $results 'aws-mongo-federation.log') -Tail 5
        }
    }
    finally{
        $env:OASIS_AWS_CQL_ENDPOINT=$priorEndpoint;$env:OASIS_AWS_CQL_STATE=$priorState
        $env:OASIS_AWS_CQL_RESUME_UNWRITTEN=$priorResume
        $env:OASIS_MONGO_REPLICA_SET_CONNECTION=$priorMongo;$env:OASIS_AWS_MONGO_SERVE=$priorServe
        try{if($startedByRun){& Scripts/TestHosts/Use-CosmicDynamoEnvironment.ps1 -Stop}}
        finally{
            try{if($ownedMongo){if(!$ownedMongo.HasExited){$ownedMongo.Kill();if(!$ownedMongo.WaitForExit(30000)){throw 'Owned Mongo teardown incomplete.'}};$ownedMongo.Dispose()}}
            finally{$environmentLease.Dispose()}
        }
    }
}
