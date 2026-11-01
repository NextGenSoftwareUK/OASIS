param([switch]$Federated,[switch]$Serve,[switch]$Content)
if($Serve -and !$Federated){throw "Serve requires explicit federation."}
$ErrorActionPreference='Stop'
$root=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
$state=Join-Path $root 'LocalState/COSMIC/PostgreSqlCql'
$results=Join-Path $root 'TestResults/PostgreSqlCqlCurrent'
New-Item -ItemType Directory -Force -Path $state,$results | Out-Null
$lease=[IO.File]::Open((Join-Path $state 'runner.lock'),[IO.FileMode]::OpenOrCreate,[IO.FileAccess]::ReadWrite,[IO.FileShare]::None)
$oldContent=$env:OASIS_POSTGRES_CQL_CONTENT
$oldConnection=$env:OASIS_POSTGRES_CQL_CONNECTION
$oldState=$env:OASIS_POSTGRES_CQL_STATE
$oldMongo=$env:OASIS_MONGO_REPLICA_SET_CONNECTION;$oldServe=$env:OASIS_POSTGRES_MONGO_SERVE
$lifecycleMongo=@{Process=$null}
$lifecycle=@{Started=$false;Passed=$false};$secret=$null
try {
    # This checkout is recovering an existing source, not creating a replacement.
    # Missing provenance must fail before native activation or test seed branches.
    $requiredSource=@('demo-seed.json','source-snapshot.json','source-write-0.ack.json','source-write-1.ack.json','source-write-2.ack.json')
    if($Content){$requiredSource+=@('content-seed.json','content-source-snapshot.json','content-write-0.ack.json','content-write-1.ack.json','content-write-2.ack.json','content-write-3.ack.json')}
    foreach($name in $requiredSource){
        if(!(Test-Path -LiteralPath (Join-Path $state $name))){throw 'Original source provenance missing; do not reset or implicitly reseed.'}
    }
    if($Content -and (Test-Path -LiteralPath (Join-Path $state 'mongo-content.pending.json'))){throw 'Content Mongo fixture unresolved.'}
    if($Content -and (Test-Path -LiteralPath (Join-Path $state 'content-seed.pending.json'))){throw 'Content setup pending; preserve without replay.'}
    if(Test-Path -LiteralPath (Join-Path $state 'demo-seed.pending.json')){throw 'Original setup is pending; inspect without replay.'}
    if(Test-Path -LiteralPath (Join-Path $state 'mongo.pending.json')){throw 'Original Mongo fixture unresolved.'}
    if($Federated -and !(Test-Path -LiteralPath (Join-Path $state 'demo-seed.json'))){throw 'Original native source required before federation.'}
    $identity=(& wsl -d Ubuntu -u root -- podman inspect oasis-postgres | ConvertFrom-Json)[0]
    if($LASTEXITCODE -or $identity.Id -ne 'cee58d44cc44aace8863ca53ffa955deed78a710a9800b750a484bb478206a69' -or $identity.Image -ne '79bd7c99e923138f136f8009d6bffa66e21e9d4fda5c0c561b00fc9c90cfe537'){throw 'Exact retained PostgreSQL container/image required.'}
    $ports=@($identity.HostConfig.PortBindings.'5432/tcp')
    if($ports.Count -ne 1 -or $ports[0].HostIp -ne '127.0.0.1' -or $ports[0].HostPort -ne '15432'){throw 'Expected loopback binding required.'}
    if(@($identity.Mounts | Where-Object {$_.Type -eq 'volume' -and $_.Name -eq 'oasis-postgres-data' -and $_.Destination -eq '/var/lib/postgresql/data'}).Count -ne 1){throw 'Retained data volume required.'}
    & (Join-Path $PSScriptRoot 'Invoke-WithProviderBuildLease.ps1') -Owner 'COSMIC PostgreSQL CQL native' -Operation {
        # Preserve prior successful evidence; never overwrite an unresolved failure.
        foreach($name in $(if($Content){@('postgres-content-native.trx','postgres-content-mongo-federation.trx')}else{@('postgres-cql-native.trx','postgres-mongo-federation.trx')})) {
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
        $project=Join-Path $root 'Providers/Storage/TestProjects/NextGenSoftware.OASIS.API.Providers.PostgreSQLOASIS.IntegrationTests'
        if($Content){
            if(!(Test-Path -LiteralPath (Join-Path $project 'obj/project.assets.json'))){throw 'Existing integration assets required; inspect caches before restoring.'}
        } else {
        & dotnet restore $project --source C:/Users/david/.nuget/packages /p:NuGetAudit=false /p:RestoreRecursive=false /m:2 /nr:false *> (Join-Path $results 'native-restore.log')
        if($LASTEXITCODE){throw 'Cached dependency restore failed.'}
        }
        if($Federated){
            & dotnet build (Join-Path $root 'Providers/Storage/NextGenSoftware.OASIS.API.Providers.MongoOASIS') --no-restore /m:2 /nr:false /p:UseSharedCompilation=false /p:BuildProjectReferences=false *> (Join-Path $results 'mongo-build.log')
            if($LASTEXITCODE){throw 'Mongo producer build failed.'}
        }
        & dotnet build (Join-Path $root 'Providers/Storage/NextGenSoftware.OASIS.API.Providers.PostgreSQLOASIS') --no-restore /m:2 /nr:false /p:UseSharedCompilation=false /p:BuildProjectReferences=false *> (Join-Path $results 'native-provider-build.log')
        if($LASTEXITCODE){throw 'PostgreSQL producer build failed.'}
        & dotnet build $project --no-restore /m:2 /nr:false /p:UseSharedCompilation=false /p:BuildProjectReferences=false *> (Join-Path $results 'native-build.log')
        if($LASTEXITCODE){throw 'Cached native consumer build failed.'}
        if($identity.State.Status -ne 'exited'){throw 'Require stopped retained container for exclusive native acceptance.'}
        $lifecycle.Started=$true
        & wsl -d Ubuntu -u root -- podman start oasis-postgres | Out-Null
        if($LASTEXITCODE){throw 'Retained PostgreSQL start failed.'}
        $deadline=[DateTime]::UtcNow.AddSeconds(30)
        do {
            $health=(& wsl -d Ubuntu -u root -- podman inspect --format '{{.State.Health.Status}}' oasis-postgres).Trim()
            if($LASTEXITCODE){throw 'Health observation failed; do not restart.'}
            if($health -eq 'healthy'){break}
            if($health -ne 'starting'){throw 'Retained service unhealthy.'}
            Start-Sleep -Milliseconds 500
        } while([DateTime]::UtcNow -lt $deadline)
        if($health -ne 'healthy'){throw 'Readiness timed out; preserve original.'}
        $secret=(& wsl -d Ubuntu -u root -- podman secret inspect --showsecret oasis-postgres-pwd | ConvertFrom-Json)[0].SecretData
        if([string]::IsNullOrWhiteSpace($secret)){throw 'Protected test credential required.'}
        $env:OASIS_POSTGRES_CQL_CONNECTION="Host=127.0.0.1;Port=15432;Database=oasis_provider_test;Username=oasis_test;Password=$secret;Search Path=cosmic_cql,pg_catalog;Timeout=10;Command Timeout=15;Pooling=false"
        $env:OASIS_POSTGRES_CQL_STATE=$state
        $env:OASIS_POSTGRES_CQL_CONTENT=if($Content){'1'}else{'0'}
        $nativeFilter=if($Content){'FullyQualifiedName~PostgreSqlCqlContentNativeTests'}else{'FullyQualifiedName~PostgreSqlCqlNativeTests'}
        $nativeReport=if($Content){'postgres-content-native.trx'}else{'postgres-cql-native.trx'}
        $nativeLog=if($Content){'content-native.log'}else{'native.log'}
        & dotnet test $project --no-build --no-restore /m:2 /nr:false /p:UseSharedCompilation=false --filter $nativeFilter --results-directory $results --logger "trx;LogFileName=$nativeReport" *> (Join-Path $results $nativeLog)
        if($LASTEXITCODE){throw 'Native acceptance failed; preserve source/journals.'}
        [xml]$report=Get-Content -LiteralPath (Join-Path $results $nativeReport) -Raw
        if([int]$report.TestRun.ResultSummary.Counters.passed -ne 1 -or [int]$report.TestRun.ResultSummary.Counters.total -ne 1){throw 'One passed native test without skips required.'}
        if($Federated){
            if(Get-NetTCPConnection -State Listen -LocalPort 27029,5114 -ErrorAction SilentlyContinue){throw 'Paired ports occupied; no process adoption.'}
            $runtime='C:/Program Files/MongoDB/Server/8.3/bin/mongod.exe'
            $data=Join-Path $root 'LocalState/COSMIC/mongo-data'
            if(!(Test-Path (Join-Path $data 'WiredTiger'))){throw 'Original Mongo store missing.'}
            if(((& $runtime --version)-join ' ') -notmatch 'v8\.3\.11\b'){throw 'Pinned Mongo version differs.'}
            $lifecycleMongo.Process=Start-Process $runtime -ArgumentList @('--dbpath',$data,'--bind_ip','127.0.0.1','--port','27029','--replSet','oasisRecoveryRs','--wiredTigerCacheSizeGB','0.25','--oplogSize','32','--logpath',(Join-Path $results 'mongo-service.log'),'--logappend') -WindowStyle Hidden -PassThru
            $mongoReady=$false
            for($i=0;$i -lt 40;$i++){
                if($lifecycleMongo.Process.HasExited){throw 'Owned Mongo exited before readiness.'}
                $listeners=Get-NetTCPConnection -State Listen -LocalPort 27029 -ErrorAction SilentlyContinue
                if($listeners){if(@($listeners|Where-Object OwningProcess -ne $lifecycleMongo.Process.Id).Count){throw 'Mongo listener owner differs.'};$mongoReady=$true;break}
                Start-Sleep -Milliseconds 500
            }
            if(!$mongoReady){throw 'Owned Mongo startup deadline exceeded.'}
            $env:OASIS_MONGO_REPLICA_SET_CONNECTION='mongodb://127.0.0.1:27029/?replicaSet=oasisRecoveryRs&serverSelectionTimeoutMS=3000'
            $env:OASIS_POSTGRES_MONGO_SERVE=if($Serve){'1'}else{'0'}
            $pairedReport=if($Content){'postgres-content-mongo-federation.trx'}else{'postgres-mongo-federation.trx'}
            $pairedLog=if($Content){'postgres-content-mongo-federation.log'}else{'postgres-mongo-federation.log'}
            & dotnet test $project --no-build --no-restore /m:2 /nr:false /p:UseSharedCompilation=false --filter FullyQualifiedName~PostgreSqlMongoCqlNativeTests --logger "trx;LogFileName=$pairedReport" --results-directory $results -v:q *> (Join-Path $results $pairedLog)
            if($LASTEXITCODE){throw 'Paired PostgreSql CQL failed; preserve ownership journal.'}
            [xml]$paired=Get-Content (Join-Path $results $pairedReport)
            if($paired.TestRun.ResultSummary.Counters.total -ne '1' -or $paired.TestRun.ResultSummary.Counters.passed -ne '1'){throw 'Exactly one paired pass required.'}
            $mongoJournal=if($Content){'mongo-content.pending.json'}else{'mongo.pending.json'}
            if(Test-Path (Join-Path $state $mongoJournal)){throw 'Mongo fixture cleanup incomplete.'}
            Get-Content (Join-Path $results $pairedLog) -Tail 4
        }
        $lifecycle.Passed=$true
    }
} finally {
    $env:OASIS_POSTGRES_CQL_CONTENT=$oldContent;$env:OASIS_POSTGRES_CQL_CONNECTION=$oldConnection;$env:OASIS_POSTGRES_CQL_STATE=$oldState;$env:OASIS_MONGO_REPLICA_SET_CONNECTION=$oldMongo;$env:OASIS_POSTGRES_MONGO_SERVE=$oldServe;$secret=$null
    if($lifecycleMongo.Process){if(!$lifecycleMongo.Process.HasExited){$lifecycleMongo.Process.Kill();if(!$lifecycleMongo.Process.WaitForExit(30000)){throw "Owned Mongo stop incomplete."}};$lifecycleMongo.Process.Dispose()}
    if($lifecycle.Started -and $lifecycle.Passed){& wsl -d Ubuntu -u root -- podman stop --time 20 oasis-postgres | Out-Null;if($LASTEXITCODE){throw 'Owned container stop failed.'}}
    $lease.Dispose()
}
