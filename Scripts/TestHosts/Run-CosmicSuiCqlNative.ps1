#Requires -Version 7.5
param([switch]$Federated,[switch]$Serve)
if($Serve -and !$Federated){throw 'Serve requires explicit federation.'}
$ErrorActionPreference='Stop'
$root=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
& (Join-Path $PSScriptRoot 'Invoke-WithProviderBuildLease.ps1') -Owner cosmic-sui-cql -Operation {
    $state=Join-Path $root 'LocalState/COSMIC/SuiCql'
    $results=Join-Path $root 'TestResults/SuiCqlCurrent'
    New-Item -ItemType Directory -Force $state,$results | Out-Null
    $lease=[IO.File]::Open((Join-Path $state 'runner.lock'),'OpenOrCreate','ReadWrite','None')
    $mongo=$null
    $nodeLauncher=$null
    $ownedSuiPid=$null
    $oldDeployment=$env:OASIS_SUI_CQL_DEPLOYMENT
    $oldBridge=$env:OASIS_SUI_SDK_BRIDGE
    $prior=$env:OASIS_SUI_CQL_STATE
    $oldMongo=$env:OASIS_MONGO_REPLICA_SET_CONNECTION
    $oldServe=$env:OASIS_SUI_MONGO_SERVE
    try {
        if(Test-Path (Join-Path $state 'demo-seed.pending.json')){throw 'Original setup pending; reconcile without replay.'}
        if(Test-Path (Join-Path $state 'mongo.pending.json')){throw 'Original Mongo fixture pending; reconcile first.'}
        if($Federated -and !(Test-Path (Join-Path $state 'demo-seed.json'))){throw 'Verify original native source before federation.'}
        foreach($relative in @('demo-seed.json','demo-deployment.json','publication.receipt.json')){
            if(!(Test-Path (Join-Path $state $relative))){throw 'Original admitted Sui source missing; no provisioning/replacement.'}
        }
        if(Test-Path (Join-Path $state 'publication.pending.json')){throw 'Original publication unresolved.'}
        $deployment='\\wsl.localhost\Ubuntu\var\tmp\oasis-sui\deployment.json'
        if(!(Test-Path $deployment)){throw 'Original protected deployment missing.'}
        $bindings=@(Get-Content '\\wsl.localhost\Ubuntu\var\tmp\oasis-sui\network\fullnode.yaml' | Where-Object {$_ -match '^json-rpc-address:'})
        if($bindings.Count -ne 1 -or $bindings[0] -cne 'json-rpc-address: "127.0.0.1:9000"'){throw 'Retained RPC must already bind loopback.'}
        $found=@(& wsl -d Ubuntu -u root -- pgrep -x sui 2>$null)
        if($found.Count -or $LASTEXITCODE -ne 1){throw 'Sui runtime already active or inventory failed; no adoption.'}
        $bridge=Join-Path $root 'Providers/Blockchain/NextGenSoftware.OASIS.API.Providers.SuiOASIS/SdkBridge/bridge.mjs'
        if(!(Test-Path (Join-Path (Split-Path $bridge) 'node_modules/@mysten/sui/package.json'))){throw 'Reuse/restore locked SDK before running; no automatic download.'}
        $env:OASIS_SUI_CQL_DEPLOYMENT=$deployment
        $env:OASIS_SUI_SDK_BRIDGE=$bridge
        # Preserve prior successful evidence; never overwrite an unresolved failure.
        foreach($name in @('sui-cql-native.trx','sui-mongo-federation.trx')) {
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
        $project=Join-Path $root 'Providers/Blockchain/TestProjects/NextGenSoftware.OASIS.API.Providers.SuiOASIS.IntegrationTests/NextGenSoftware.OASIS.API.Providers.SuiOASIS.IntegrationTests.csproj'
        if($Federated){
            & dotnet restore $project --source C:/Users/david/.nuget/packages /m:2 /nr:false /p:NuGetAudit=false -v:q
            if($LASTEXITCODE){throw 'Offline paired restore failed.'}
            & dotnet build (Join-Path $root 'Providers/Storage/NextGenSoftware.OASIS.API.Providers.MongoOASIS/NextGenSoftware.OASIS.API.Providers.MongoDBOASIS.csproj') --no-restore /p:BuildProjectReferences=false /m:2 /nr:false /p:UseSharedCompilation=false -v:q -clp:ErrorsOnly
            if($LASTEXITCODE){throw 'Mongo producer build failed.'}
        }
        & dotnet build (Join-Path $root 'Providers/Blockchain/NextGenSoftware.OASIS.API.Providers.SuiOASIS') --no-restore /p:BuildProjectReferences=false /m:2 /nr:false /p:UseSharedCompilation=false -v:q -clp:ErrorsOnly
        if($LASTEXITCODE){throw 'Sui producer build failed.'}
        & dotnet build $project --no-restore /p:BuildProjectReferences=false /m:2 /nr:false /p:UseSharedCompilation=false -v:q -clp:ErrorsOnly
        if($LASTEXITCODE){throw 'Native consumer build failed.'}
        $start=(& wsl -d Ubuntu -u root -- wslpath -a ((Join-Path $root 'Scripts/TestHosts/start-sui-test-network.sh').Replace('\','/'))).Trim()
        if($LASTEXITCODE){throw 'Sui source launcher path unavailable.'}
        $nodeLauncher=Start-Process wsl.exe -ArgumentList @('-d','Ubuntu','-u','root','--','bash',$start,'--reuse-only') -WindowStyle Hidden -PassThru -RedirectStandardOutput (Join-Path $results 'node-current.stdout.log') -RedirectStandardError (Join-Path $results 'node-current.stderr.log')
        $deadline=[DateTime]::UtcNow.AddSeconds(30)
        while([DateTime]::UtcNow -lt $deadline){
            $found=@(& wsl -d Ubuntu -u root -- pgrep -x sui 2>$null)
            if($found.Count -eq 1){$ownedSuiPid=$found[0].Trim();break}
            if($nodeLauncher.HasExited){throw 'Owned Sui launcher exited; no restart.'}
            Start-Sleep -Milliseconds 250
        }
        if(!$ownedSuiPid){throw 'Owned Sui PID unavailable; preserve runtime state.'}
        $exe=((& wsl -d Ubuntu -u root -- readlink -f "/proc/$ownedSuiPid/exe")-join '').Trim()
        if($exe -ne '/var/tmp/oasis-runtimes/sui-1.81.1/sui'){throw 'Owned Sui executable differs.'}
        $ready=$false
        while([DateTime]::UtcNow -lt $deadline){
            $sockets=@(& wsl -d Ubuntu -u root -- ss -H -ltnp)
            $rpc=@($sockets|Where-Object {$_ -match ':9000\s'})
            if($rpc.Count -eq 1){
                if($rpc[0] -notmatch '\s127\.0\.0\.1:9000\s' -or $rpc[0] -notmatch "pid=$ownedSuiPid,"){throw 'Native RPC owner/binding differs.'}
                $ready=$true;break
            }
            if($nodeLauncher.HasExited){throw 'Owned node stopped during readiness.'}
            Start-Sleep -Milliseconds 250
        }
        if(!$ready){throw 'Sui readiness deadline exceeded; no restart.'}
        $env:OASIS_SUI_CQL_STATE=$state
        & dotnet test $project --no-build --no-restore /m:2 /nr:false /p:UseSharedCompilation=false --filter FullyQualifiedName~SuiCqlNativeTests --logger 'trx;LogFileName=sui-cql-native.trx' --results-directory $results -v:q
        if($LASTEXITCODE){throw 'Native Sui CQL failed; preserve source and pending journal.'}
        [xml]$report=Get-Content (Join-Path $results 'sui-cql-native.trx')
        if($report.TestRun.ResultSummary.Counters.total -ne '1' -or $report.TestRun.ResultSummary.Counters.passed -ne '1'){throw 'Exactly one native pass required.'}
        if($Federated){
            if(Get-NetTCPConnection -State Listen -LocalPort 27029,5112 -ErrorAction SilentlyContinue){throw 'Paired ports occupied; no process adoption.'}
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
            $env:OASIS_SUI_MONGO_SERVE=if($Serve){'1'}else{'0'}
            & dotnet test $project --no-build --no-restore /m:2 /nr:false /p:UseSharedCompilation=false --filter FullyQualifiedName~SuiMongoCqlNativeTests --logger 'trx;LogFileName=sui-mongo-federation.trx' --results-directory $results -v:q *> (Join-Path $results 'sui-mongo-federation.log')
            if($LASTEXITCODE){throw 'Paired Sui CQL failed; preserve ownership journal.'}
            [xml]$paired=Get-Content (Join-Path $results 'sui-mongo-federation.trx')
            if($paired.TestRun.ResultSummary.Counters.total -ne '1' -or $paired.TestRun.ResultSummary.Counters.passed -ne '1'){throw 'Exactly one paired pass required.'}
            if(Test-Path (Join-Path $state 'mongo.pending.json')){throw 'Mongo fixture cleanup incomplete.'}
            Get-Content (Join-Path $results 'sui-mongo-federation.log') -Tail 4
        }
    }
    finally {
        try{if($mongo){if(!$mongo.HasExited){$mongo.Kill();if(!$mongo.WaitForExit(30000)){throw 'Owned Mongo teardown incomplete.'}};$mongo.Dispose()}}
        finally{
            try {
                if($ownedSuiPid){
                    $stop=(& wsl -d Ubuntu -u root -- wslpath -a ((Join-Path $root 'Scripts/TestHosts/stop-owned-sui-test-node.sh').Replace('\','/'))).Trim()
                    if($LASTEXITCODE){throw 'Owned stop-helper path unavailable.'}
                    & wsl -d Ubuntu -u root -- bash $stop $ownedSuiPid
                    if($LASTEXITCODE){throw 'Owned Sui shutdown incomplete.'}
                }
                if($nodeLauncher){if(!$nodeLauncher.WaitForExit(10000)){throw 'Owned launcher has not exited.'};$nodeLauncher.Dispose()}
            }
            finally{$env:OASIS_SUI_CQL_DEPLOYMENT=$oldDeployment;$env:OASIS_SUI_SDK_BRIDGE=$oldBridge;$env:OASIS_SUI_CQL_STATE=$prior;$env:OASIS_MONGO_REPLICA_SET_CONNECTION=$oldMongo;$env:OASIS_SUI_MONGO_SERVE=$oldServe;$lease.Dispose()}
        }
    }
}
