param([switch]$Federated,[switch]$Serve)
if($Serve -and !$Federated){throw "Serve requires explicit federation."}
$ErrorActionPreference='Stop'
$root=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
$state=Join-Path $root 'LocalState/COSMIC/UpstashCql'
$results=Join-Path $root 'TestResults/UpstashCqlCurrent'
$resource=Join-Path $env:LOCALAPPDATA 'OASIS/ReusableProviderTests/Upstash'
[void][IO.Directory]::CreateDirectory($state)
[void][IO.Directory]::CreateDirectory($results)
& (Join-Path $PSScriptRoot 'Invoke-WithProviderBuildLease.ps1') -Owner 'COSMIC Upstash native CQL' -Operation {
    $lease=[IO.File]::Open((Join-Path $state 'runner.lock'),'OpenOrCreate','ReadWrite','None')
    $priorUrl=$env:OASIS_UPSTASH_CQL_URL;$priorToken=$env:OASIS_UPSTASH_CQL_TOKEN;$priorState=$env:OASIS_UPSTASH_CQL_STATE
    $priorMongo=$env:OASIS_MONGO_REPLICA_SET_CONNECTION;$priorServe=$env:OASIS_UPSTASH_MONGO_SERVE
    $mongo=$null
    $decoded=$null;$credential=$null
    try {
        if(Test-Path (Join-Path $state 'demo-seed.pending.json')){throw 'Original native write outcome unresolved; preserve without replay.'}
        if(Test-Path (Join-Path $state 'mongo.pending.json')){throw "Original Mongo fixture unresolved."}
        if($Federated -and !(Test-Path (Join-Path $state 'demo-seed.json'))){throw "Original admitted source required before federation."}
        $metadata=Get-Content (Join-Path $resource 'resource.json') -Raw|ConvertFrom-Json
        if([DateTimeOffset]::UtcNow -ge [DateTimeOffset]$metadata.ExpiresByUtc -or $metadata.Lifecycle -notlike '*official scratch*'){throw 'Unexpired existing official free scratch admission required; no provisioning.'}
        # Preserve prior successful evidence; never overwrite an unresolved failure.
        foreach($name in @('upstash-cql-native.trx','upstash-mongo-federation.trx')) {
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
        $project=Join-Path $root 'Providers/Cloud/TestProjects/NextGenSoftware.OASIS.API.Providers.UpstashOASIS.IntegrationTests'
        & dotnet restore $project --source C:/Users/david/.nuget/packages /p:NuGetAudit=false /p:RestoreRecursive=false /m:2 /nr:false *> (Join-Path $results 'native-restore.log')
        if($LASTEXITCODE){throw 'Cached restore failed.'}
        if($Federated){
            & dotnet build (Join-Path $root 'Providers/Storage/NextGenSoftware.OASIS.API.Providers.MongoOASIS') --no-restore /m:2 /nr:false /p:UseSharedCompilation=false /p:BuildProjectReferences=false *> (Join-Path $results 'mongo-build.log')
            if($LASTEXITCODE){throw 'Mongo producer failed.'}
        }
        & dotnet build (Join-Path $root 'Providers/Cloud/NextGenSoftware.OASIS.API.Providers.UpstashOASIS') --no-restore /m:2 /nr:false /p:UseSharedCompilation=false /p:BuildProjectReferences=false *> (Join-Path $results 'native-provider-build.log')
        if($LASTEXITCODE){throw 'Upstash producer build failed.'}
        & dotnet build $project --no-restore /m:2 /nr:false /p:UseSharedCompilation=false /p:BuildProjectReferences=false *> (Join-Path $results 'native-build.log')
        if($LASTEXITCODE){throw 'Cached native consumer build failed.'}
        $decoded=[Security.Cryptography.ProtectedData]::Unprotect([IO.File]::ReadAllBytes((Join-Path $resource 'credentials.dpapi')),$null,[Security.Cryptography.DataProtectionScope]::CurrentUser)
        $credential=[Text.Encoding]::UTF8.GetString($decoded)|ConvertFrom-Json
        $uri=[Uri]$credential.Url
        if($uri.Scheme -ne 'https' -or !$uri.Host.EndsWith('.upstash.io',[StringComparison]::OrdinalIgnoreCase) -or !$uri.IsDefaultPort -or $uri.UserInfo -or $uri.Query -or $uri.Fragment -or [string]::IsNullOrWhiteSpace($credential.Token)){throw 'Protected original Upstash REST identity required.'}
        $env:OASIS_UPSTASH_CQL_URL=$credential.Url;$env:OASIS_UPSTASH_CQL_TOKEN=$credential.Token;$env:OASIS_UPSTASH_CQL_STATE=$state
        & dotnet test $project --no-build --no-restore /m:2 /nr:false /p:UseSharedCompilation=false --filter FullyQualifiedName~UpstashCqlNativeTests --results-directory $results --logger 'trx;LogFileName=upstash-cql-native.trx' *> (Join-Path $results 'native.log')
        if($LASTEXITCODE){throw 'Hosted native acceptance failed; preserve source/intent, no automatic replay.'}
        [xml]$report=Get-Content (Join-Path $results 'upstash-cql-native.trx') -Raw
        if($report.TestRun.ResultSummary.Counters.passed -ne '1' -or $report.TestRun.ResultSummary.Counters.total -ne '1'){throw 'One native pass without skips required.'}
        if($Federated){
            if(Get-NetTCPConnection -State Listen -LocalPort 27029,5117 -ErrorAction SilentlyContinue){throw 'Paired ports occupied; no process adoption.'}
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
            $env:OASIS_UPSTASH_MONGO_SERVE=if($Serve){'1'}else{'0'}
            & dotnet test $project --no-build --no-restore /m:2 /nr:false /p:UseSharedCompilation=false --filter FullyQualifiedName~UpstashMongoCqlNativeTests --logger 'trx;LogFileName=upstash-mongo-federation.trx' --results-directory $results -v:q *> (Join-Path $results 'upstash-mongo-federation.log')
            if($LASTEXITCODE){throw 'Paired Upstash CQL failed; preserve ownership journal.'}
            [xml]$paired=Get-Content (Join-Path $results 'upstash-mongo-federation.trx')
            if($paired.TestRun.ResultSummary.Counters.total -ne '1' -or $paired.TestRun.ResultSummary.Counters.passed -ne '1'){throw 'Exactly one paired pass required.'}
            if(Test-Path (Join-Path $state 'mongo.pending.json')){throw 'Mongo fixture cleanup incomplete.'}
            Get-Content (Join-Path $results 'upstash-mongo-federation.log') -Tail 4
        }
    } finally {
        $env:OASIS_UPSTASH_CQL_URL=$priorUrl;$env:OASIS_UPSTASH_CQL_TOKEN=$priorToken;$env:OASIS_UPSTASH_CQL_STATE=$priorState;$env:OASIS_MONGO_REPLICA_SET_CONNECTION=$priorMongo;$env:OASIS_UPSTASH_MONGO_SERVE=$priorServe
        if($mongo){if(!$mongo.HasExited){$mongo.Kill();if(!$mongo.WaitForExit(30000)){throw "Owned Mongo stop incomplete."}};$mongo.Dispose()}
        if($decoded){[Array]::Clear($decoded,0,$decoded.Length)};$credential=$null
        $lease.Dispose()
    }
}
