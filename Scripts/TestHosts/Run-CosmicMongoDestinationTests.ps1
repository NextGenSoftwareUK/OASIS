#Requires -Version 7.0
param([switch]$SqlitePreflight,[switch]$MigrationPreflight,[switch]$SourceReceipt,[switch]$ReplicaLifecycle,[switch]$InspectReplicaLifecycleFailure,[switch]$ReconcileReplicaLifecycleFailure)
if($PSBoundParameters.Count -gt 1){throw 'Choose one explicit mode.'}
Set-StrictMode -Version Latest
$ErrorActionPreference='Stop'
$root=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
Set-Location $root
& Scripts/TestHosts/Invoke-WithProviderBuildLease.ps1 -Owner cosmic-mongo-destination-verification -Operation {
    $work=Join-Path $root 'TestResults/OwnedRoutingCurrent'
    $runtime='C:/Program Files/MongoDB/Server/8.3/bin/mongod.exe'
    $data=Join-Path $root 'LocalState/COSMIC/mongo-data'
    if(Test-Path 'LocalState/COSMIC/OwnedRoutingCurrent/mongo-destination.pending.json'){throw 'Pending outcome must be reconciled.'}
    if(Get-NetTCPConnection -State Listen -LocalPort 27029 -ErrorAction SilentlyContinue){throw 'Owned port occupied.'}
    if(!(Test-Path (Join-Path $data 'WiredTiger'))){throw 'Retained replica data missing.'}
    if(((& $runtime --version) -join ' ') -notmatch 'v8\.3\.11\b'){throw 'Pinned runtime mismatch.'}
    Get-PSDrive C | Select-Object Free
    $project='Providers/Storage/NextGenSoftware.OASIS.API.Providers.MongoOASIS.IntegrationTests/NextGenSoftware.OASIS.API.Providers.MongoOASIS.IntegrationTests.csproj'
    if($SqlitePreflight){$project='OASIS Architecture/NextGenSoftware.OASIS.API.Core.HyperDrive.IntegrationTests/NextGenSoftware.OASIS.API.Core.HyperDrive.IntegrationTests.csproj'}
    New-Item -ItemType Directory -Force -Path $work | Out-Null
    New-Item -ItemType Directory -Force -Path (Join-Path $root 'TestResults/SubscriptionUsageCurrent') | Out-Null
    $filter=if($SqlitePreflight){'FullyQualifiedName~NativeSqliteMongoPreflightDetectsIndependentTargetEditsAndHardDelete'}elseif($MigrationPreflight){'FullyQualifiedName~NativeReceiptPreflightClassifiesMissingCurrentLaterAndHardDeletedReplicaWithoutMutation'}elseif($SourceReceipt){'FullyQualifiedName~BoundOriginalReceiptRequiresActualNativeSourceWithoutRewritingIntent'}elseif($ReconcileReplicaLifecycleFailure){'FullyQualifiedName~ReconcileInspectedReplicaLifecycleSetupFailureWithoutReplayingWrites'}elseif($InspectReplicaLifecycleFailure){'FullyQualifiedName~InspectOriginalReplicaLifecycleFailureWithoutMutatingStores'}elseif($ReplicaLifecycle){'FullyQualifiedName~NativeCommittedReplicasBindSourceLineageGraphHeadAndReceipt'}else{'FullyQualifiedName~MongoDestinationCatalogueRetainsNativeMeaningAtomically'}
    $reportName=if($SqlitePreflight){'sqlite-mongo-receipt-preflight.trx'}elseif($MigrationPreflight){'mongo-receipt-preflight.trx'}elseif($SourceReceipt){'mongo-bound-source-receipt.trx'}elseif($ReconcileReplicaLifecycleFailure){'mongo-replica-lifecycle-reconciliation.trx'}elseif($InspectReplicaLifecycleFailure){'mongo-replica-lifecycle-inspection.trx'}elseif($ReplicaLifecycle){'mongo-replica-lifecycle.trx'}else{'mongo-destination-catalogue.trx'}
    $logName=if($SqlitePreflight){'sqlite-mongo-receipt-preflight.log'}elseif($MigrationPreflight){'mongo-receipt-preflight.log'}elseif($SourceReceipt){'mongo-bound-source-receipt.log'}elseif($ReconcileReplicaLifecycleFailure){'mongo-replica-lifecycle-reconciliation.log'}elseif($InspectReplicaLifecycleFailure){'mongo-replica-lifecycle-inspection.log'}elseif($ReplicaLifecycle){'mongo-replica-lifecycle.log'}else{'mongo-destination-catalogue.log'}
    $expectedCount=if($SqlitePreflight){2}elseif($MigrationPreflight){1}elseif($SourceReceipt){2}elseif($ReconcileReplicaLifecycleFailure){1}elseif($InspectReplicaLifecycleFailure){1}elseif($ReplicaLifecycle){2}else{6}
    foreach($producer in @(
        'OASIS Architecture/NextGenSoftware.OASIS.API.Core/NextGenSoftware.OASIS.API.Core.csproj',
        'Providers/Shared/NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage/NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage.csproj',
        'Providers/Storage/NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS/NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS.csproj',
        'Providers/Storage/NextGenSoftware.OASIS.API.Providers.MongoOASIS/NextGenSoftware.OASIS.API.Providers.MongoDBOASIS.csproj',
        $project)) {
        & dotnet build $producer --no-restore /p:BuildProjectReferences=false /m:2 /nr:false /p:UseSharedCompilation=false -v:q -clp:ErrorsOnly >> (Join-Path $work 'mongo-destination-test-build.log')
        if($LASTEXITCODE){throw ('Build failed: '+$producer)}
    }
    $owned=$null
    $prior=$env:OASIS_MONGO_REPLICA_SET_CONNECTION
    try{
        $owned=Start-Process $runtime -ArgumentList @('--dbpath',$data,'--bind_ip','127.0.0.1','--port','27029','--replSet','oasisRecoveryRs','--wiredTigerCacheSizeGB','0.25','--oplogSize','32','--setParameter','enableTestCommands=1','--logpath',(Join-Path $work 'mongo-destination-service.log'),'--logappend') -WindowStyle Hidden -PassThru
        $ready=$false
        for($i=0;$i -lt 40;$i++){
            if($owned.HasExited){throw 'Owned Mongo exited.'}
            $listeners=Get-NetTCPConnection -State Listen -LocalPort 27029 -ErrorAction SilentlyContinue
            if($listeners){if(@($listeners|Where-Object OwningProcess -ne $owned.Id).Count){throw 'Port ownership differs.'};$ready=$true;break}
            Start-Sleep -Milliseconds 500
        }
        if(!$ready){throw 'Startup deadline exceeded.'}
        $env:OASIS_MONGO_REPLICA_SET_CONNECTION='mongodb://127.0.0.1:27029/?replicaSet=oasisRecoveryRs&serverSelectionTimeoutMS=3000'
        $testStarted=[DateTime]::UtcNow
        dotnet test $project --no-build --no-restore /m:2 /nr:false /p:UseSharedCompilation=false --filter $filter --results-directory $work --logger ('trx;LogFileName='+$reportName) *> (Join-Path $work $logName)
        if($LASTEXITCODE){throw 'Native routing verification failed; preserve original outcome.'}
        $reportFile=Get-Item -LiteralPath (Join-Path $work $reportName)
        if($reportFile.LastWriteTimeUtc -lt $testStarted){throw 'A fresh native report is required.'}
        [xml]$report=Get-Content -LiteralPath $reportFile.FullName -Raw
        $counts=$report.TestRun.ResultSummary.Counters
        if([int]$counts.total -ne $expectedCount -or [int]$counts.passed -ne $expectedCount -or [int]$counts.notExecuted -ne 0){throw 'The exact selected native cohort must pass with no skips.'}
        if(Test-Path 'LocalState/COSMIC/OwnedRoutingCurrent/mongo-destination.pending.json'){throw 'Fixture cleanup incomplete.'}
        Get-Content (Join-Path $work $logName) -Tail 5
        Get-FileHash (Join-Path $work $reportName)
    }finally{
        $env:OASIS_MONGO_REPLICA_SET_CONNECTION=$prior
        if($owned){if(!$owned.HasExited){$owned.Kill();if(!$owned.WaitForExit(30000)){throw 'Owned process teardown incomplete.'}};$owned.Dispose()}
    }
    Get-PSDrive C | Select-Object Free
}
