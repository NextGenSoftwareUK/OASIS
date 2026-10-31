#Requires -Version 7.5
param([switch]$Federated,[switch]$Serve)
if($Serve -and !$Federated){throw 'Serve requires explicit federation.'}
$ErrorActionPreference='Stop'
$root=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
& (Join-Path $PSScriptRoot 'Invoke-WithProviderBuildLease.ps1') -Owner cosmic-aptos-cql -Operation {
    $state=Join-Path $root 'LocalState/COSMIC/AptosCql'
    $results=Join-Path $root 'TestResults/AptosCqlCurrent'
    New-Item -ItemType Directory -Force $state,$results | Out-Null
    $lease=[IO.File]::Open((Join-Path $state 'runner.lock'),'OpenOrCreate','ReadWrite','None')
    $mongo=$null
    $prior=$env:OASIS_APTOS_CQL_STATE
    $oldMongo=$env:OASIS_MONGO_REPLICA_SET_CONNECTION
    $oldServe=$env:OASIS_APTOS_MONGO_SERVE
    $oldKey=$env:OASIS_APTOS_PRIVATE_KEY
    try {
        if(Test-Path (Join-Path $state 'demo-seed.pending.json')){throw 'Original setup pending; reconcile without replay.'}
        if(Test-Path (Join-Path $state 'mongo.pending.json')){throw 'Original Mongo fixture pending; reconcile first.'}
        if($Federated -and !(Test-Path (Join-Path $state 'demo-seed.json'))){throw 'Verify original native source before federation.'}
        foreach($relative in @('node/0/db','environment.json','demo-seed.json','profile/.aptos/config.yaml')){
            if(!(Test-Path (Join-Path $state $relative))){throw 'Original Aptos source missing; do not recreate.'}
        }
        if(Test-Path (Join-Path $state 'setup.pending.json')){throw 'Unresolved original Aptos setup.'}
        $nativeListener=Get-NetTCPConnection -State Listen -LocalPort 18080 -ErrorAction SilentlyContinue
        if(!$nativeListener){throw 'Start the admitted Aptos node with Start-CosmicAptosCqlNode.ps1 first.'}
        $expectedCli=Join-Path ([Environment]::GetFolderPath('LocalApplicationData')) 'OASIS/provider-evidence/aptos-cli/9.6.0/aptos.exe'
        foreach($ownerPid in @($nativeListener.OwningProcess|Sort-Object -Unique)){
            $owner=Get-CimInstance Win32_Process -Filter "ProcessId=$ownerPid"
            if($owner.ExecutablePath -ne $expectedCli -or $owner.CommandLine -notlike '*run-localnet*' -or $owner.CommandLine -notlike '*AptosCql*node*'){
                throw 'Native listener differs from the admitted node; no process adoption.'
            }
        }
        $profile=Get-Content (Join-Path $state 'profile/.aptos/config.yaml') -Raw
        $env:OASIS_APTOS_PRIVATE_KEY=([regex]::Match($profile,'private_key:\s*"?([^\r\n"]+)')).Groups[1].Value.Trim()
        if(!$env:OASIS_APTOS_PRIVATE_KEY){throw 'Original profile credential unavailable.'}
        # Preserve prior successful evidence; never overwrite an unresolved failure.
        foreach($name in @('aptos-cql-native.trx','aptos-mongo-federation.trx')) {
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
        $project=Join-Path $root 'Providers/Blockchain/TestProjects/NextGenSoftware.OASIS.API.Providers.AptosOASIS.IntegrationTests/NextGenSoftware.OASIS.API.Providers.AptosOASIS.IntegrationTests.csproj'
        if($Federated){
            & dotnet restore $project --source C:/Users/david/.nuget/packages /m:2 /nr:false /p:NuGetAudit=false -v:q
            if($LASTEXITCODE){throw 'Offline paired restore failed.'}
            & dotnet build (Join-Path $root 'Providers/Storage/NextGenSoftware.OASIS.API.Providers.MongoOASIS/NextGenSoftware.OASIS.API.Providers.MongoDBOASIS.csproj') --no-restore /p:BuildProjectReferences=false /m:2 /nr:false /p:UseSharedCompilation=false -v:q -clp:ErrorsOnly
            if($LASTEXITCODE){throw 'Mongo producer build failed.'}
        }
        & dotnet build (Join-Path $root 'Providers/Blockchain/NextGenSoftware.OASIS.API.Providers.AptosOASIS/NextGenSoftware.OASIS.API.Providers.AptosOASIS.csproj') --no-restore /p:BuildProjectReferences=false /m:2 /nr:false /p:UseSharedCompilation=false -v:q -clp:ErrorsOnly
        if($LASTEXITCODE){throw 'Aptos producer build failed.'}
        & dotnet build $project --no-restore /p:BuildProjectReferences=false /m:2 /nr:false /p:UseSharedCompilation=false -v:q -clp:ErrorsOnly
        if($LASTEXITCODE){throw 'Native consumer build failed.'}
        $env:OASIS_APTOS_CQL_STATE=$state
        & dotnet test $project --no-build --no-restore /m:2 /nr:false /p:UseSharedCompilation=false --filter FullyQualifiedName~AptosCqlNativeTests --logger 'trx;LogFileName=aptos-cql-native.trx' --results-directory $results -v:q
        if($LASTEXITCODE){throw 'Native Aptos CQL failed; preserve source and pending journal.'}
        [xml]$report=Get-Content (Join-Path $results 'aptos-cql-native.trx')
        if($report.TestRun.ResultSummary.Counters.total -ne '1' -or $report.TestRun.ResultSummary.Counters.passed -ne '1'){throw 'Exactly one native pass required.'}
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
            $env:OASIS_APTOS_MONGO_SERVE=if($Serve){'1'}else{'0'}
            & dotnet test $project --no-build --no-restore /m:2 /nr:false /p:UseSharedCompilation=false --filter FullyQualifiedName~AptosMongoCqlNativeTests --logger 'trx;LogFileName=aptos-mongo-federation.trx' --results-directory $results -v:q *> (Join-Path $results 'aptos-mongo-federation.log')
            if($LASTEXITCODE){throw 'Paired Aptos CQL failed; preserve ownership journal.'}
            [xml]$paired=Get-Content (Join-Path $results 'aptos-mongo-federation.trx')
            if($paired.TestRun.ResultSummary.Counters.total -ne '1' -or $paired.TestRun.ResultSummary.Counters.passed -ne '1'){throw 'Exactly one paired pass required.'}
            if(Test-Path (Join-Path $state 'mongo.pending.json')){throw 'Mongo fixture cleanup incomplete.'}
            Get-Content (Join-Path $results 'aptos-mongo-federation.log') -Tail 4
        }
    }
    finally {
        try{if($mongo){if(!$mongo.HasExited){$mongo.Kill();if(!$mongo.WaitForExit(30000)){throw 'Owned Mongo teardown incomplete.'}};$mongo.Dispose()}}
        finally{$env:OASIS_APTOS_CQL_STATE=$prior;$env:OASIS_MONGO_REPLICA_SET_CONNECTION=$oldMongo;$env:OASIS_APTOS_MONGO_SERVE=$oldServe;$env:OASIS_APTOS_PRIVATE_KEY=$oldKey;$lease.Dispose()}
    }
}
