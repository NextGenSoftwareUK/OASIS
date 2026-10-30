#Requires -Version 7.5
param([switch]$Federated,[switch]$Serve)
if ($Serve -and !$Federated) { throw 'Serve requires explicit federation.' }
$ErrorActionPreference = 'Stop'
$root = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
& (Join-Path $PSScriptRoot 'Invoke-WithProviderBuildLease.ps1') -Owner cosmic-firestore-cql -Operation {
    $state = Join-Path $root 'LocalState/COSMIC/FirestoreCql'
    $results = Join-Path $root 'TestResults/FirestoreCqlCurrent'
    New-Item -ItemType Directory -Force -Path $state,$results | Out-Null
    $lease = [IO.File]::Open((Join-Path $state 'environment.lock'),'OpenOrCreate','ReadWrite','None')
    $owned = $null
    $ownedMongo = $null
    $oldMongo = $env:OASIS_MONGO_REPLICA_SET_CONNECTION
    $oldServe = $env:OASIS_FIRESTORE_MONGO_SERVE
    $safeToStop = $true
    $oldHost = $env:FIRESTORE_EMULATOR_HOST
    $oldState = $env:OASIS_FIRESTORE_CQL_STATE
    $checkpoint = Join-Path $state 'checkpoint.json'
    $export = Join-Path $state 'checkpoint'
    $metadata = Join-Path $export 'firestore_export/firestore_export.overall_export_metadata'
    $seed = Join-Path $state 'demo-seed.json'
    try {
        if (Test-Path -LiteralPath (Join-Path $state 'demo-seed.pending.json')) {
            throw 'Original seed outcome is unresolved; no replay or replacement fixture.'
        }
        if (Get-NetTCPConnection -State Listen -LocalPort 18084,18085,18086 -ErrorAction SilentlyContinue) {
            throw 'CQL emulator ports occupied; no unrelated process adopted or stopped.'
        }
        $jar = Join-Path $env:USERPROFILE '.cache/firebase/emulators/cloud-firestore-emulator-v1.22.0.jar'
        if (!(Test-Path -LiteralPath $jar) -or (Get-FileHash $jar).Hash -ne '9B6498B7F62714D67F48F59B3818883CD682DBCD46B9F59511DE81C97BB5166C') {
            throw 'Pinned cached Firestore1.22.0 runtime required. No automatic download.'
        }
        if (Test-Path -LiteralPath $checkpoint) {
            $proof = Get-Content -LiteralPath $checkpoint -Raw | ConvertFrom-Json
            if ($proof.Project -ne 'oasis-cql-demo' -or !(Test-Path -LiteralPath $seed) -or
                (Get-FileHash $seed).Hash -ne $proof.SeedSha256) { throw 'Original checkpoint/seed identity differs.' }
            foreach ($file in $proof.Files) {
                $target = [IO.Path]::GetFullPath((Join-Path $export $file.Path))
                if (!$target.StartsWith($export + [IO.Path]::DirectorySeparatorChar,[StringComparison]::OrdinalIgnoreCase) -or
                    !(Test-Path -LiteralPath $target) -or (Get-FileHash $target).Hash -ne $file.Sha256) {
                    throw 'Original checkpoint file is missing or differs; never recreate it.'
                }
            }
            if (!(Test-Path -LiteralPath $metadata)) { throw 'Native checkpoint metadata missing.' }
        }
        elseif ((Test-Path -LiteralPath $export) -or (Test-Path -LiteralPath $seed)) {
            throw 'Unacknowledged original checkpoint/seed exists; reconcile instead of resetting.'
        }
        $project = Join-Path $root 'Providers/Cloud/TestProjects/NextGenSoftware.OASIS.API.Providers.GoogleCloudOASIS.IntegrationTests/NextGenSoftware.OASIS.API.Providers.GoogleCloudOASIS.IntegrationTests.csproj'
        if ($Federated) {
            $assets = Join-Path (Split-Path $project) 'obj/project.assets.json'
            if (!(Test-Path $assets) -or (Get-Content $assets -Raw) -notmatch 'NextGenSoftware.OASIS.API.Providers.MongoDBOASIS') {
                & dotnet restore $project --source C:/Users/david/.nuget/packages /m:2 /nr:false /p:NuGetAudit=false -v:q
                if ($LASTEXITCODE) { throw 'Offline paired restore failed.' }
            }
            & dotnet build (Join-Path $root 'Providers/Storage/NextGenSoftware.OASIS.API.Providers.MongoOASIS/NextGenSoftware.OASIS.API.Providers.MongoDBOASIS.csproj') --no-restore /p:BuildProjectReferences=false /m:2 /nr:false /p:UseSharedCompilation=false -v:q -clp:ErrorsOnly
            if ($LASTEXITCODE) { throw 'Mongo producer build failed.' }
        }
        foreach ($build in @((Join-Path $root 'Providers/Cloud/NextGenSoftware.OASIS.API.Providers.GoogleCloudOASIS/NextGenSoftware.OASIS.API.Providers.GoogleCloudOASIS.csproj'),$project)) {
            & dotnet build $build --no-restore /p:BuildProjectReferences=false /m:2 /nr:false /p:UseSharedCompilation=false -v:q -clp:ErrorsOnly
            if ($LASTEXITCODE) { throw 'Bounded Firestore build failed.' }
        }
        $arguments = @('-Xmx256m','-jar',$jar,'--host','127.0.0.1','--port','18084',
            '--webchannel_port','18085','--websocket_port','18086','--project_id','oasis-cql-demo',
            '--single_project_mode','--single_project_mode_error')
        if (Test-Path -LiteralPath $checkpoint) { $arguments += @('--import-data',$metadata) }
        $owned = Start-Process -FilePath (Get-Command java.exe).Source -ArgumentList $arguments -WorkingDirectory $state -WindowStyle Hidden -PassThru -RedirectStandardOutput (Join-Path $results 'firestore-service.log') -RedirectStandardError (Join-Path $results 'firestore-service.err')
        $ready = $false
        $deadline = [DateTime]::UtcNow.AddSeconds(30)
        do {
            if ($owned.HasExited) { throw 'Owned emulator exited during startup; inspect retained logs.' }
            $listeners = Get-NetTCPConnection -State Listen -LocalPort 18084 -ErrorAction SilentlyContinue
            if ($listeners) {
                if (@($listeners | Where-Object OwningProcess -ne $owned.Id).Count) { throw 'Native listener owner differs.' }
                $ready = $true
            }
            if (!$ready) { Start-Sleep -Milliseconds 200 }
        } while (!$ready -and [DateTime]::UtcNow -lt $deadline)
        if (!$ready) { throw 'Native emulator startup deadline exceeded.' }
        $env:FIRESTORE_EMULATOR_HOST = '127.0.0.1:18084'
        $env:OASIS_FIRESTORE_CQL_STATE = $state
        $safeToStop = $false
        $started = [DateTime]::UtcNow
        & dotnet test $project --no-build --no-restore /m:2 /nr:false --filter FullyQualifiedName~FirestoreCqlNativeTests --logger 'trx;LogFileName=firestore-cql-native.trx' --results-directory $results -v:q
        if ($LASTEXITCODE) { throw 'Native acceptance failed; retain the original live emulator for reconciliation.' }
        $report = Get-Item -LiteralPath (Join-Path $results 'firestore-cql-native.trx')
        [xml]$trx = Get-Content -LiteralPath $report.FullName
        $counts = $trx.TestRun.ResultSummary.Counters
        if ($report.LastWriteTimeUtc -lt $started -or [int]$counts.total -ne 1 -or [int]$counts.passed -ne 1 -or [int]$counts.notExecuted -ne 0) { throw 'Exactly one fresh native pass required.' }
        if (!(Test-Path -LiteralPath $checkpoint)) {
            New-Item -ItemType Directory -Path $export | Out-Null
            $body = @{ database='projects/oasis-cql-demo/databases/(default)';export_directory=$export;export_name='firestore_export' } | ConvertTo-Json -Compress
            Invoke-RestMethod -Uri 'http://127.0.0.1:18084/emulator/v1/projects/oasis-cql-demo:export' -Method Post -ContentType 'application/json' -Body $body -TimeoutSec 30 | Out-Null
            if (!(Test-Path -LiteralPath $metadata)) { throw 'Native export did not acknowledge metadata; preserve live original.' }
            $files = @(Get-ChildItem -LiteralPath $export -Recurse -File | ForEach-Object {
                @{ Path=[IO.Path]::GetRelativePath($export,$_.FullName);Sha256=(Get-FileHash $_.FullName).Hash }
            })
            if ($files.Count -eq 0) { throw 'Native checkpoint is empty.' }
            $bytes = [Text.Encoding]::UTF8.GetBytes((@{ Project='oasis-cql-demo';SeedSha256=(Get-FileHash $seed).Hash;Files=$files } | ConvertTo-Json -Depth 5))
            $file = [IO.File]::Open($checkpoint,'CreateNew','Write','None')
            try { $file.Write($bytes);$file.Flush($true) } finally { $file.Dispose() }
        }
        # After the first seed, subsequent tests are source-read-only. The one
        # original checkpoint is imported and hash-verified, never overwritten.
        $safeToStop = $true
        if ($Federated) {
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
            $env:OASIS_FIRESTORE_MONGO_SERVE=if($Serve){'1'}else{'0'}
            $pairedStarted=[DateTime]::UtcNow
            dotnet test $project --no-build --no-restore --filter 'FullyQualifiedName~NativeFirestoreMongoFederationRunsSdkAndHttpWithProviderProvenance' --logger 'trx;LogFileName=firestore-mongo-federation.trx' --results-directory $results *> (Join-Path $results 'firestore-mongo-federation.log')
            if($LASTEXITCODE){throw 'Firestore Mongo demo failed; preserve pending fixture.'}
            $pairedReport=Get-Item -LiteralPath (Join-Path $results 'firestore-mongo-federation.trx')
            if($pairedReport.LastWriteTimeUtc -lt $pairedStarted){throw 'Paired report is stale.'}
            [xml]$paired=Get-Content $pairedReport.FullName
            if($paired.TestRun.ResultSummary.Counters.total -ne '1' -or $paired.TestRun.ResultSummary.Counters.passed -ne '1'){throw 'Exactly one fresh federation case required.'}
            if(Test-Path (Join-Path $state 'mongo.pending.json')){throw 'Mongo fixture cleanup incomplete.'}
            Get-Content (Join-Path $results 'firestore-mongo-federation.log') -Tail 5
        }
    }
    finally {
        try {
            if ($owned) {
                if (!$owned.HasExited) {
                    if ($safeToStop) { $owned.Kill();if (!$owned.WaitForExit(15000)) { throw 'Owned emulator teardown incomplete.' } }
                    else { Write-Warning "Preserve original live Firestore PID $($owned.Id); no safe checkpoint acknowledgement. Do not restart/reset." }
                }
                $owned.Dispose()
            }
        }
        finally {
          try {
            if ($ownedMongo) {
                if (!$ownedMongo.HasExited) { $ownedMongo.Kill();if (!$ownedMongo.WaitForExit(30000)) { throw 'Owned Mongo teardown incomplete.' } }
                $ownedMongo.Dispose()
            }
          }
          finally {
            $env:OASIS_MONGO_REPLICA_SET_CONNECTION = $oldMongo
            $env:OASIS_FIRESTORE_MONGO_SERVE = $oldServe
            $env:FIRESTORE_EMULATOR_HOST = $oldHost
            $env:OASIS_FIRESTORE_CQL_STATE = $oldState
            $lease.Dispose()
          }
        }
    }
}
