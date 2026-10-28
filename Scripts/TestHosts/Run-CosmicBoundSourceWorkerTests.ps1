#Requires -Version 7.0
$ErrorActionPreference = 'Stop'
$taskRoot = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
$work = Join-Path $taskRoot 'TestResults/OwnedRoutingCurrent'
$state = Join-Path $taskRoot 'LocalState/COSMIC/OwnedRoutingCurrent'
$runtime = 'C:/Program Files/MongoDB/Server/8.3/bin/mongod.exe'
$data = Join-Path $taskRoot 'LocalState/COSMIC/mongo-data'
Push-Location $taskRoot
try {
& (Join-Path $PSScriptRoot 'Invoke-WithProviderBuildLease.ps1') -Owner 'cosmic-bound-source-worker-pairs' -Operation {
    New-Item -ItemType Directory -Force -Path $work | Out-Null
    foreach ($path in @((Join-Path $state 'worker-pair.pending.json'), (Join-Path $work 'worker-pair.db'),
        (Join-Path $work 'worker-pair.db-wal'), (Join-Path $work 'worker-pair.db-shm'), (Join-Path $work 'worker-pair.db-journal'))) {
        if (Test-Path -LiteralPath $path) { throw "Preserve and reconcile the owned fixture before reuse: $path" }
    }
    if (!(Test-Path -LiteralPath (Join-Path $data 'WiredTiger'))) { throw 'Retained Mongo data is required; do not initialize a new environment.' }
    if (((& $runtime --version) -join ' ') -notmatch 'v8\.3\.11\b') { throw 'Pinned Mongo runtime mismatch.' }
    if (Get-NetTCPConnection -State Listen -LocalPort 27029 -ErrorAction SilentlyContinue) { throw 'Owned Mongo port is occupied.' }
    $before = (Get-PSDrive C).Free
    $web4 = 'ONODE/NextGenSoftware.OASIS.API.ONODE.WebAPI/NextGenSoftware.OASIS.API.ONODE.WebAPI.csproj'
    $project = 'Providers/Storage/TestProjects/NextGenSoftware.OASIS.API.Providers.Neo4jOASIS.IntegrationTests/NextGenSoftware.OASIS.API.Providers.Neo4jOASIS.IntegrationTests.csproj'
    foreach ($entry in @(
        @('Providers/Shared/NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage/NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage.csproj','bound-worker-shared-build.log'),
        @('Providers/Storage/NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS/NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS.csproj','bound-worker-sqlite-build.log'),
        @('Providers/Storage/NextGenSoftware.OASIS.API.Providers.MongoOASIS/NextGenSoftware.OASIS.API.Providers.MongoDBOASIS.csproj','bound-worker-mongo-build.log'),
        @($web4,'bound-worker-pair-web4-build.log'),@($project,'bound-worker-pair-build.log'))) {
        dotnet build $entry[0] --no-restore --no-incremental /p:BuildProjectReferences=false /m:2 /nr:false /p:UseSharedCompilation=false *> (Join-Path $work $entry[1])
        if ($LASTEXITCODE) { throw "Producer/consumer build failed: $($entry[0])" }
    }
    # Each consumer must use the actual frozen producers. Never copy DLLs to
    # bypass a stale reference or rebuild in another environment.
    $producers = @(
        @('OASIS Architecture/NextGenSoftware.OASIS.API.Core','NextGenSoftware.OASIS.API.Core.dll'),
        @('Providers/Shared/NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage','NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage.dll'),
        @('Providers/Storage/NextGenSoftware.OASIS.API.Providers.MongoOASIS','NextGenSoftware.OASIS.API.Providers.MongoDBOASIS.dll'),
        @('Providers/Storage/NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS','NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS.dll')
    )
    foreach ($producer in $producers) {
        $expected = (Get-FileHash -LiteralPath (Join-Path $producer[0] "bin/Debug/net10.0/$($producer[1])")).Hash
        foreach ($consumer in $web4,$project) {
            $copy = Join-Path (Split-Path $consumer) "bin/Debug/net10.0/$($producer[1])"
            if ((Get-FileHash -LiteralPath $copy).Hash -ne $expected) { throw "Stale native producer in $consumer : $($producer[1])" }
        }
        Write-Output "$($producer[1]) SHA256=$expected"
    }
    $owned = $null
    $prior = $env:OASIS_MONGO_REPLICA_SET_CONNECTION
    try {
        $owned = Start-Process $runtime -ArgumentList @('--dbpath',$data,'--bind_ip','127.0.0.1','--port','27029',
            '--replSet','oasisRecoveryRs','--wiredTigerCacheSizeGB','0.25','--oplogSize','32',
            '--logpath',(Join-Path $work 'bound-worker-pair-mongo-service.log'),'--logappend') -WindowStyle Hidden -PassThru
        $ready = $false
        for ($attempt=0; $attempt -lt 40; $attempt++) {
            if ($owned.HasExited) { throw 'Owned Mongo process exited.' }
            $listeners = Get-NetTCPConnection -State Listen -LocalPort 27029 -ErrorAction SilentlyContinue
            if ($listeners) {
                if (@($listeners | Where-Object OwningProcess -ne $owned.Id).Count) { throw 'Native port ownership differs.' }
                $ready = $true
                break
            }
            Start-Sleep -Milliseconds 500
        }
        if (!$ready) { throw 'Owned Mongo startup deadline exceeded.' }
        $env:OASIS_MONGO_REPLICA_SET_CONNECTION = 'mongodb://127.0.0.1:27029/?replicaSet=oasisRecoveryRs&serverSelectionTimeoutMS=3000'
        dotnet test $project --no-build --no-restore --filter 'FullyQualifiedName~ShippingWorkerUsesBoundMongoAndSqliteSourcesAcrossProviderRestart' --logger 'trx;LogFileName=bound-worker-pair-current.trx' --results-directory $work *> (Join-Path $work 'bound-worker-pair-current.log')
        if ($LASTEXITCODE) { throw 'Native bound-source worker verification failed; preserve the original outcome.' }
        [xml]$report = Get-Content -LiteralPath (Join-Path $work 'bound-worker-pair-current.trx') -Raw
        $counts = $report.TestRun.ResultSummary.Counters
        if (!$counts -or [int]$counts.total -ne 6 -or [int]$counts.passed -ne 6 -or [int]$counts.notExecuted -ne 0) { throw 'Exactly six native direction/probe-availability/cold-save cases must pass without skips.' }
        if (Test-Path -LiteralPath (Join-Path $state 'worker-pair.pending.json')) { throw 'Owned fixture cleanup did not finish.' }
        Get-Content -LiteralPath (Join-Path $work 'bound-worker-pair-current.log') -Tail 4
        Get-FileHash -LiteralPath (Join-Path $work 'bound-worker-pair-current.trx')
    }
    finally {
        $env:OASIS_MONGO_REPLICA_SET_CONNECTION = $prior
        if ($owned) {
            if (!$owned.HasExited) { $owned.Kill(); if (!$owned.WaitForExit(30000)) { throw 'Owned Mongo process teardown incomplete.' } }
            $owned.Dispose()
        }
    }
    Write-Output "Free bytes before=$before after=$((Get-PSDrive C).Free)"
}
}
finally { Pop-Location }
