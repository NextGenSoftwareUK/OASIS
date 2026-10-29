#Requires -Version 7.0
param([switch]$FullDestinationOutage,[switch]$ActiveDeliveryOutage,[switch]$ReconcileActiveOutage,[switch]$ExhaustDeliveryBudget,[switch]$NativeRoutingProof,[switch]$ReconcileUnwrittenRoutingFixture,[switch]$ReconcileRoutingReadFailure,[switch]$NativePrimaryConsistencyProof)
if ($ReconcileUnwrittenRoutingFixture -and ($ActiveDeliveryOutage -or $ReconcileActiveOutage -or $ExhaustDeliveryBudget -or $NativeRoutingProof -or $ReconcileRoutingReadFailure -or $NativePrimaryConsistencyProof)) { throw 'Unwritten fixture reconciliation is exclusive.' }
if ($ReconcileUnwrittenRoutingFixture -or $ReconcileRoutingReadFailure) { $FullDestinationOutage = $true }
if ($ReconcileActiveOutage -and $ReconcileRoutingReadFailure) { throw 'Choose the exact reviewed outcome.' }
if ((@($ExhaustDeliveryBudget,$NativeRoutingProof,$NativePrimaryConsistencyProof) | Where-Object { $_ }).Count -gt 1) { throw 'Choose retry exhaustion or routing proof.' }
if ($ExhaustDeliveryBudget -or $NativeRoutingProof -or $NativePrimaryConsistencyProof) { $ActiveDeliveryOutage = $true }
if ($ActiveDeliveryOutage -and ($ReconcileActiveOutage -or $ReconcileRoutingReadFailure)) { throw 'Choose fresh proof or original-outcome reconciliation.' }
if ($ActiveDeliveryOutage -or $ReconcileActiveOutage) { $FullDestinationOutage = $true }
$ErrorActionPreference = 'Stop'
function Read-OfflineJournal([string]$path) {
    # The writer publishes complete snapshots by rename. Share deletion so an
    # in-flight reader retains its old snapshot without blocking replacement.
    $stream = [IO.FileStream]::new($path,[IO.FileMode]::Open,[IO.FileAccess]::Read,
        ([IO.FileShare]::ReadWrite -bor [IO.FileShare]::Delete))
    try {
        $reader = [IO.StreamReader]::new($stream)
        try { return ($reader.ReadToEnd() | ConvertFrom-Json) }
        finally { $reader.Dispose() }
    }
    finally { $stream.Dispose() }
}
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
    if ($ReconcileUnwrittenRoutingFixture) {
        $pending = Join-Path $state 'worker-offline.pending.json'
        if (!(Test-Path -LiteralPath $pending) -or (Test-Path -LiteralPath ($pending+'.next'))) { throw 'Exact original planned journal required.' }
        $intent = Read-OfflineJournal $pending
        if ($intent.Phase -ne 'planned' -or $intent.SourceFingerprint -or $intent.TargetFingerprint -or $intent.FirstSequence -ne 0) { throw 'Only the reviewed unwritten phase is admitted.' }
        foreach ($signal in 'stopped','restarted') {
            if (Test-Path -LiteralPath (Join-Path $work ('worker-offline-'+$signal))) { throw 'No process handoff was acknowledged in the reviewed failure.' }
        }
    }
    elseif ($ReconcileActiveOutage -or $ReconcileRoutingReadFailure) {
        $pending = Join-Path $state 'worker-offline.pending.json'
        if (!(Test-Path -LiteralPath $pending) -or (Test-Path -LiteralPath ($pending+'.next'))) { throw 'Exact original pending journal required.' }
        if ((Get-Content -LiteralPath $pending -Raw | ConvertFrom-Json).Phase -ne 'ready-to-stop') { throw 'Only the reviewed stopped-phase outcome is admitted.' }
        if (!(Test-Path -LiteralPath (Join-Path $work 'worker-offline-stopped')) -or (Test-Path -LiteralPath (Join-Path $work 'worker-offline-restarted'))) { throw 'Original stopped-only process boundary required.' }
    }
    elseif ($FullDestinationOutage) {
        foreach ($path in @((Join-Path $state 'worker-offline.pending.json'), (Join-Path $state 'worker-offline.pending.json.next'),
            (Join-Path $work 'worker-offline.db'), (Join-Path $work 'worker-offline.db-wal'),
            (Join-Path $work 'worker-offline.db-shm'), (Join-Path $work 'worker-offline.db-journal'), (Join-Path $work 'worker-offline-stopped'), (Join-Path $work 'worker-offline-restarted'))) {
            if (Test-Path -LiteralPath $path) { throw "Preserve and reconcile the offline fixture: $path" }
        }
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
    $testProcess = $null
    $prior = $env:OASIS_MONGO_REPLICA_SET_CONNECTION
    $priorPhase = $env:OASIS_OFFLINE_REPLICATION_PHASE
    $mongoArguments = @('--dbpath',$data,'--bind_ip','127.0.0.1','--port','27029',
        '--replSet','oasisRecoveryRs','--wiredTigerCacheSizeGB','0.25','--oplogSize','32',
        '--logpath',(Join-Path $work 'bound-worker-pair-mongo-service.log'),'--logappend')
    try {
        $owned = Start-Process $runtime -ArgumentList $mongoArguments -WindowStyle Hidden -PassThru
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
        if ($ReconcileUnwrittenRoutingFixture) {
            $env:OASIS_OFFLINE_REPLICATION_PHASE = 'cleanup-unwritten'
            & dotnet test $project --no-build --no-restore /m:2 /nr:false --filter 'FullyQualifiedName~ShippingWorkerPersistsNewSaveWhileMongoDestinationProcessIsStopped' --logger 'trx;LogFileName=worker-offline-cleanup-unwritten.trx' --results-directory $work *> (Join-Path $work 'worker-offline-cleanup-unwritten.log')
            if ($LASTEXITCODE) { throw 'Unwritten native fixture verification failed; preserve original state.' }
            [xml]$report = Get-Content -LiteralPath (Join-Path $work 'worker-offline-cleanup-unwritten.trx') -Raw
            $counts = $report.TestRun.ResultSummary.Counters
            if (!$counts -or [int]$counts.total -ne 1 -or [int]$counts.passed -ne 1 -or [int]$counts.notExecuted -ne 0) { throw 'Exactly one unskipped unwritten fixture check required.' }
            if (Test-Path -LiteralPath (Join-Path $state 'worker-offline.pending.json')) { throw 'Unwritten fixture cleanup incomplete.' }
            Get-Content -LiteralPath (Join-Path $work 'worker-offline-cleanup-unwritten.log') -Tail 4
        }
        elseif ($ReconcileActiveOutage -or $ReconcileRoutingReadFailure) {
            $recoveryPhase = if ($ReconcileRoutingReadFailure) { 'reconcile-routing' } else { 'reconcile-active' }
            $env:OASIS_OFFLINE_REPLICATION_PHASE = $recoveryPhase
            & dotnet test $project --no-build --no-restore /m:2 /nr:false --filter 'FullyQualifiedName~ShippingWorkerPersistsNewSaveWhileMongoDestinationProcessIsStopped' --logger "trx;LogFileName=worker-offline-$recoveryPhase.trx" --results-directory $work *> (Join-Path $work "worker-offline-$recoveryPhase.log")
            if ($LASTEXITCODE) { throw 'Original active-outage reconciliation failed; preserve fixture.' }
            [xml]$report = Get-Content -LiteralPath (Join-Path $work "worker-offline-$recoveryPhase.trx") -Raw
            $counts = $report.TestRun.ResultSummary.Counters
            if (!$counts -or [int]$counts.total -ne 1 -or [int]$counts.passed -ne 1 -or [int]$counts.notExecuted -ne 0) { throw 'Exactly one unskipped reconciliation required.' }
            if (Test-Path -LiteralPath (Join-Path $state 'worker-offline.pending.json')) { throw 'Original fixture cleanup incomplete.' }
            Remove-Item -LiteralPath (Join-Path $work 'worker-offline-stopped')
            Get-Content -LiteralPath (Join-Path $work "worker-offline-$recoveryPhase.log") -Tail 4
        }
        elseif ($ActiveDeliveryOutage) {
            $scenario = if ($ExhaustDeliveryBudget) { 'exhaust' } elseif ($NativeRoutingProof) { 'routing' } elseif ($NativePrimaryConsistencyProof) { 'consistency' } else { 'active' }
            $env:OASIS_OFFLINE_REPLICATION_PHASE = $scenario
            $testProcess = Start-Process dotnet -ArgumentList @('test',$project,'--no-build','--no-restore','/m:2','/nr:false',
                '--filter','FullyQualifiedName~ShippingWorkerPersistsNewSaveWhileMongoDestinationProcessIsStopped',
                '--logger',"trx;LogFileName=worker-offline-$scenario.trx",'--results-directory',$work) -WorkingDirectory $taskRoot -WindowStyle Hidden -PassThru `
                -RedirectStandardOutput (Join-Path $work "worker-offline-$scenario.log") -RedirectStandardError (Join-Path $work "worker-offline-$scenario.stderr.log")
            $stopped = $false; $restarted = $false
            $deadline = [DateTime]::UtcNow.AddMinutes(3)
            while (!$testProcess.HasExited) {
                if ([DateTime]::UtcNow -gt $deadline) { throw 'Active outage test deadline exceeded; preserve native fixture.' }
                $journal = Join-Path $state 'worker-offline.pending.json'
                if (!$restarted -and (Test-Path -LiteralPath $journal)) {
                    $phase = (Read-OfflineJournal $journal).Phase
                    if ($phase -eq 'ready-to-stop' -and !$stopped) {
                        if ($owned.HasExited) { throw 'Owned Mongo exited before requested outage.' }
                        $owned.Kill()
                        if (!$owned.WaitForExit(30000)) { throw 'Owned Mongo process did not stop.' }
                        $owned.Dispose(); $owned = $null
                        if (Get-NetTCPConnection -State Listen -LocalPort 27029 -ErrorAction SilentlyContinue) { throw 'Destination listener still present.' }
                        [IO.File]::WriteAllText((Join-Path $work 'worker-offline-stopped'),'owned process stopped and joined')
                        $stopped = $true
                    }
                    if ($phase -eq 'ready-to-restart' -and $stopped -and !$restarted) {
                        $owned = Start-Process $runtime -ArgumentList $mongoArguments -WindowStyle Hidden -PassThru
                        $ready = $false
                        for ($attempt=0; $attempt -lt 40; $attempt++) {
                            if ($owned.HasExited) { throw 'Owned Mongo restart exited.' }
                            $listeners = Get-NetTCPConnection -State Listen -LocalPort 27029 -ErrorAction SilentlyContinue
                            if ($listeners) {
                                if (@($listeners | Where-Object OwningProcess -ne $owned.Id).Count) { throw 'Restart listener ownership differs.' }
                                $ready = $true; break
                            }
                            Start-Sleep -Milliseconds 500
                        }
                        if (!$ready) { throw 'Owned Mongo restart readiness deadline exceeded.' }
                        [IO.File]::WriteAllText((Join-Path $work 'worker-offline-restarted'),'same owned native data restarted')
                        $restarted = $true
                    }
                }
                Start-Sleep -Milliseconds 100
                $testProcess.Refresh()
            }
            $testProcess.WaitForExit()
            if ($testProcess.ExitCode -ne 0 -or !$stopped -or !$restarted) { throw 'Active outage proof failed; preserve original fixture.' }
            [xml]$report = Get-Content -LiteralPath (Join-Path $work "worker-offline-$scenario.trx") -Raw
            $counts = $report.TestRun.ResultSummary.Counters
            if (!$counts -or [int]$counts.total -ne 1 -or [int]$counts.passed -ne 1 -or [int]$counts.notExecuted -ne 0) { throw 'Exactly one unskipped active-outage proof required.' }
            if (Test-Path -LiteralPath $journal) { throw 'Active outage fixture cleanup incomplete.' }
            foreach ($signal in 'stopped','restarted') { Remove-Item -LiteralPath (Join-Path $work ('worker-offline-'+$signal)) }
            Get-Content -LiteralPath (Join-Path $work "worker-offline-$scenario.log") -Tail 4
        }
        elseif ($FullDestinationOutage) {
            function Test-OfflinePhase([string]$phase) {
                $env:OASIS_OFFLINE_REPLICATION_PHASE = $phase
                & dotnet test $project --no-build --no-restore /m:2 /nr:false --filter 'FullyQualifiedName~ShippingWorkerPersistsNewSaveWhileMongoDestinationProcessIsStopped' --logger "trx;LogFileName=worker-offline-$phase.trx" --results-directory $work *> (Join-Path $work "worker-offline-$phase.log")
                if ($LASTEXITCODE) { throw "Offline phase failed: $phase; preserve original fixture." }
                [xml]$report = Get-Content -LiteralPath (Join-Path $work "worker-offline-$phase.trx") -Raw
                $counts = $report.TestRun.ResultSummary.Counters
                if (!$counts -or [int]$counts.total -ne 1 -or [int]$counts.passed -ne 1 -or [int]$counts.notExecuted -ne 0) { throw "Exactly one unskipped phase required: $phase" }
                Get-Content -LiteralPath (Join-Path $work "worker-offline-$phase.log") -Tail 3
            }
            Test-OfflinePhase 'seed'
            # Only this runner's exact owned process is stopped. Preserve its data.
            if ($owned.HasExited) { throw 'Owned Mongo exited before the requested outage.' }
            $owned.Kill()
            if (!$owned.WaitForExit(30000)) { throw 'Owned destination process did not stop.' }
            $owned.Dispose(); $owned = $null
            if (Get-NetTCPConnection -State Listen -LocalPort 27029 -ErrorAction SilentlyContinue) { throw 'Destination listener still present; no offline claim.' }
            Test-OfflinePhase 'offline'
            $owned = Start-Process $runtime -ArgumentList $mongoArguments -WindowStyle Hidden -PassThru
            $ready = $false
            for ($attempt=0; $attempt -lt 40; $attempt++) {
                if ($owned.HasExited) { throw 'Owned Mongo restart exited; preserve fixture.' }
                $listeners = Get-NetTCPConnection -State Listen -LocalPort 27029 -ErrorAction SilentlyContinue
                if ($listeners) {
                    if (@($listeners | Where-Object OwningProcess -ne $owned.Id).Count) { throw 'Restart listener ownership differs.' }
                    $ready = $true; break
                }
                Start-Sleep -Milliseconds 500
            }
            if (!$ready) { throw 'Owned Mongo restart deadline exceeded.' }
            Test-OfflinePhase 'deliver'
            if (Test-Path -LiteralPath (Join-Path $state 'worker-offline.pending.json')) { throw 'Offline fixture cleanup incomplete.' }
        }
        else {
        dotnet test $project --no-build --no-restore --filter 'FullyQualifiedName~ShippingWorkerUsesBoundMongoAndSqliteSourcesAcrossProviderRestart' --logger 'trx;LogFileName=bound-worker-pair-current.trx' --results-directory $work *> (Join-Path $work 'bound-worker-pair-current.log')
        if ($LASTEXITCODE) { throw 'Native bound-source worker verification failed; preserve the original outcome.' }
        [xml]$report = Get-Content -LiteralPath (Join-Path $work 'bound-worker-pair-current.trx') -Raw
        $counts = $report.TestRun.ResultSummary.Counters
        if (!$counts -or [int]$counts.total -ne 6 -or [int]$counts.passed -ne 6 -or [int]$counts.notExecuted -ne 0) { throw 'Exactly six native direction/probe-availability/cold-save cases must pass without skips.' }
        if (Test-Path -LiteralPath (Join-Path $state 'worker-pair.pending.json')) { throw 'Owned fixture cleanup did not finish.' }
        Get-Content -LiteralPath (Join-Path $work 'bound-worker-pair-current.log') -Tail 4
        Get-FileHash -LiteralPath (Join-Path $work 'bound-worker-pair-current.trx')
        }
    }
    finally {
        $env:OASIS_MONGO_REPLICA_SET_CONNECTION = $prior
        $env:OASIS_OFFLINE_REPLICATION_PHASE = $priorPhase
        if ($testProcess) {
            if (!$testProcess.HasExited) { $testProcess.Kill(); if (!$testProcess.WaitForExit(30000)) { throw 'Owned test process teardown incomplete.' } }
            $testProcess.Dispose()
        }
        if ($owned) {
            if (!$owned.HasExited) { $owned.Kill(); if (!$owned.WaitForExit(30000)) { throw 'Owned Mongo process teardown incomplete.' } }
            $owned.Dispose()
        }
    }
    Write-Output "Free bytes before=$before after=$((Get-PSDrive C).Free)"
}
}
finally { Pop-Location }
