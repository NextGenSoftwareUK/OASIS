#Requires -Version 7.0
param([switch]$SuiMintOwner,[switch]$ReconcileControlledMintOwner,[switch]$HederaPreparedOwners,[switch]$HederaFull,[switch]$MongoProjection,[switch]$MongoTransactions,[switch]$MongoV2,[switch]$SuiPreparedLedger,[switch]$CosmosPreparedOwners,[switch]$CosmosFull,[switch]$AuthorityLedger,[switch]$SuiPreparedInspect,[switch]$Web3PreparedOwners,[switch]$MongoGraphReconcile,[switch]$ArchiveSuiPrepared,[switch]$SuiCSharpNft,[switch]$SuiCSharpAssets,[switch]$TronPreparedStorage,[switch]$TronFull,[switch]$SuiUnit)
$ErrorActionPreference='Stop'
if($SuiUnit -and $PSBoundParameters.Count -ne 1){throw 'Sui unit verification requires its own exclusive owned environment mode.'}
if($TronFull -and !$TronPreparedStorage){throw 'TRON full unit suite requires owned prepared storage mode.'}
if($TronPreparedStorage -and $PSBoundParameters.Count -ne $(if($TronFull){2}else{1})){throw 'TRON prepared storage requires its own exclusive mode.'}
if($SuiCSharpAssets -and !$SuiCSharpNft){throw 'Sui CSharp asset matrix requires admitted facade owner mode.'}
if($SuiCSharpNft -and !$SuiPreparedLedger){throw 'Sui CSharp NFT verification requires owned prepared-ledger environment.'}
if($ArchiveSuiPrepared -and !$SuiPreparedInspect){throw 'Reviewed Sui fixture archive requires its exclusive native inspection mode.'}
if($HederaFull -and !$HederaPreparedOwners){throw 'HederaFull requires the owned HederaPreparedOwners environment.'}
if($MongoGraphReconcile -and ($SuiMintOwner -or $ReconcileControlledMintOwner -or $HederaPreparedOwners -or $MongoProjection -or $MongoTransactions -or $MongoV2 -or $SuiPreparedLedger -or $CosmosPreparedOwners -or $CosmosFull -or $AuthorityLedger -or $SuiPreparedInspect -or $Web3PreparedOwners)){throw 'Graph fixture reconciliation requires its own exclusive mode.'}
if($Web3PreparedOwners -and ($SuiMintOwner -or $ReconcileControlledMintOwner -or $HederaPreparedOwners -or $MongoProjection -or $MongoTransactions -or $MongoV2 -or $SuiPreparedLedger -or $CosmosPreparedOwners -or $CosmosFull -or $AuthorityLedger -or $SuiPreparedInspect)){throw 'Choose Web3 prepared owner verification or one other suite.'}
if($SuiPreparedInspect -and ($SuiMintOwner -or $ReconcileControlledMintOwner -or $HederaPreparedOwners -or $MongoProjection -or $MongoTransactions -or $MongoV2 -or $SuiPreparedLedger -or $CosmosPreparedOwners -or $CosmosFull -or $AuthorityLedger)){throw 'Read-only prepared inspection requires its own exclusive mode.'}
if($AuthorityLedger -and ($SuiMintOwner -or $ReconcileControlledMintOwner -or $HederaPreparedOwners -or $MongoProjection -or $MongoTransactions -or $MongoV2 -or $SuiPreparedLedger -or $CosmosPreparedOwners -or $CosmosFull)){throw 'Choose authority ledger or one provider verification suite.'}
if($CosmosFull -and !$CosmosPreparedOwners){throw 'CosmosFull requires the owned CosmosPreparedOwners environment.'}
if($CosmosPreparedOwners -and ($SuiMintOwner -or $ReconcileControlledMintOwner -or $HederaPreparedOwners -or $MongoProjection -or $MongoTransactions -or $MongoV2 -or $SuiPreparedLedger)){throw 'Choose one provider verification suite.'}
if($SuiPreparedLedger -and ($SuiMintOwner -or $ReconcileControlledMintOwner -or $HederaPreparedOwners -or $MongoProjection -or $MongoTransactions -or $MongoV2)){throw 'Choose one provider verification suite.'}
if($MongoV2 -and ($SuiMintOwner -or $ReconcileControlledMintOwner -or $HederaPreparedOwners -or $MongoProjection -or $MongoTransactions)){throw 'Choose one provider verification suite.'}
if(($MongoProjection -or $MongoTransactions) -and ($SuiMintOwner -or $ReconcileControlledMintOwner -or $HederaPreparedOwners)){throw 'Choose one provider verification suite.'}
if($MongoProjection -and $MongoTransactions){throw 'Choose projection-only or transaction verification.'}
if($SuiMintOwner -and $ReconcileControlledMintOwner){throw 'Choose verification or explicit controlled-fixture reconciliation, not both.'}
if($HederaPreparedOwners -and ($SuiMintOwner -or $ReconcileControlledMintOwner)){throw 'Choose one provider owner suite.'}
$taskRoot=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
. (Join-Path $PSScriptRoot 'Limit-OwnedMongoLog.ps1')
$work=Join-Path $taskRoot 'TestResults/SubscriptionUsageCurrent'
$data=Join-Path $work 'mongo-data'
$runtime='C:\Program Files\MongoDB\Server\8.3\bin\mongod.exe'
if(!(Test-Path -LiteralPath $runtime -PathType Leaf)){throw 'Existing installed Mongo runtime is required; no download attempted.'}
if((& $runtime --version | Select-Object -First 1) -ne 'db version v8.3.11'){throw 'Retained runtime version differs; inspect before reuse.'}
New-Item -ItemType Directory -Force -Path $work | Out-Null
$lease=[IO.File]::Open((Join-Path $work 'environment.lock'),[IO.FileMode]::OpenOrCreate,[IO.FileAccess]::ReadWrite,[IO.FileShare]::None)
$owned=$null
$prior=@{}
foreach($name in 'OASIS_OWNED_RECOVERY_REPLICA_URI','SUBSCRIPTION_TEST_MONGODB_URI','OASIS_HEDERA_SDK_BRIDGE','OASIS_SUI_SDK_BRIDGE','OASIS_MONGO_REPLICA_SET_CONNECTION','OASIS_WEB3_PREPARED_OUTBOX_REQUIRED'){$prior[$name]=[Environment]::GetEnvironmentVariable($name)}
try{
    if($MongoV2){
        foreach($relative in @(
            'Providers/Shared/NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage/NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage.csproj',
            'Providers/Storage/NextGenSoftware.OASIS.API.Providers.MongoOASIS/NextGenSoftware.OASIS.API.Providers.MongoDBOASIS.csproj',
            'Providers/Storage/NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS/NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS.csproj',
            'OASIS Architecture/NextGenSoftware.OASIS.API.Core.HyperDrive.IntegrationTests/NextGenSoftware.OASIS.API.Core.HyperDrive.IntegrationTests.csproj')){
            & dotnet build (Join-Path $taskRoot $relative) --no-restore /p:BuildProjectReferences=false /m:2 /nr:false /p:UseSharedCompilation=false -v:q -clp:ErrorsOnly
            if($LASTEXITCODE){throw "Mongo V2 producer/consumer build failed: $relative"}
        }
    }
    if(Get-NetTCPConnection -State Listen -LocalPort 27029 -ErrorAction SilentlyContinue){throw 'Owned port already in use; no process will be stopped.'}
    if($MongoV2 -and (Get-NetTCPConnection -State Listen -LocalPort 27030 -ErrorAction SilentlyContinue)){throw 'Reserved Mongo credential-outage port is occupied; refusing verification.'}
    if([IO.Path]::GetFullPath($data) -ne (Join-Path $taskRoot 'TestResults/SubscriptionUsageCurrent/mongo-data')){throw 'Unexpected owned data path.'}
    New-Item -ItemType Directory -Force -Path $data | Out-Null
    Limit-OwnedMongoLog -WorkDirectory $work
    $mongoArguments=@('--dbpath',$data,'--bind_ip','127.0.0.1','--port','27029','--replSet','oasisRecoveryRs','--wiredTigerCacheSizeGB','0.25','--oplogSize','32','--logpath',(Join-Path $work 'mongo-current.log'),'--logappend')
    if($MongoV2 -or $AuthorityLedger){$mongoArguments+=@('--setParameter','enableTestCommands=1')}
    $owned=Start-Process -FilePath $runtime -ArgumentList $mongoArguments -PassThru -WindowStyle Hidden
    $startupDeadline=[DateTime]::UtcNow.AddSeconds(30)
    do {
        if($owned.HasExited){throw 'Owned Mongo exited before listening.'}
        $listener=Get-NetTCPConnection -State Listen -LocalPort 27029 -ErrorAction SilentlyContinue
        if($listener){
            if(@($listener | Where-Object OwningProcess -ne $owned.Id).Count){throw 'Listener belongs to a different process; refusing initialization.'}
            break
        }
        if([DateTime]::UtcNow -ge $startupDeadline){throw 'Owned Mongo did not begin listening within startup deadline.'}
        Start-Sleep -Milliseconds 100
    }while($true)
    $env:OASIS_OWNED_RECOVERY_REPLICA_URI='mongodb://127.0.0.1:27029/?directConnection=true&serverSelectionTimeoutMS=2000'
    $env:SUBSCRIPTION_TEST_MONGODB_URI='mongodb://127.0.0.1:27029/?replicaSet=oasisRecoveryRs&serverSelectionTimeoutMS=3000'
    $project=Join-Path $taskRoot 'OASIS Architecture/NextGenSoftware.OASIS.API.Core/Tests/SubscriptionUsage/SubscriptionUsage.Tests.csproj'
    & dotnet test $project --no-restore /m:2 /nr:false /p:UseSharedCompilation=false /p:BuildProjectReferences=false --filter FullyQualifiedName~OwnedReplicaInitializationTests --logger 'trx;LogFileName=owned-replica-init-current.trx' --results-directory $work -v:q -clp:ErrorsOnly
    if($LASTEXITCODE -ne 0){throw 'Owned replica initialization failed.'}
    if($Web3PreparedOwners){
        $env:OASIS_WEB3_PREPARED_OUTBOX_REQUIRED='1'
        & (Join-Path $PSScriptRoot 'Run-ReusableEthereumTests.ps1')
        $proof=Join-Path $taskRoot 'TestResults/Web3CoreCurrent/web3-native-current.trx'
        if(!(Get-Content -LiteralPath $proof -Raw).Contains('WEB3_REAL_OUTBOX_VERIFIED')){throw 'Real Web3 outbox verification did not execute.'}
    }elseif($AuthorityLedger){
        $ledgerProject=Join-Path $taskRoot 'ONODE/Tests/SubscriptionLedger.ProtocolTests/SubscriptionLedger.ProtocolTests.csproj'
        $ledgerStarted=[DateTime]::UtcNow
        & dotnet test $ledgerProject --no-restore /m:2 /nr:false /p:UseSharedCompilation=false /p:BuildProjectReferences=false --logger 'trx;LogFileName=authority-ledger-current.trx' --results-directory $work -v:q
        if($LASTEXITCODE -ne 0){throw 'Authority ledger regression failed.'}
        $ledgerReport=Get-Item -LiteralPath (Join-Path $work 'authority-ledger-current.trx') -ErrorAction Stop
        if($ledgerReport.LastWriteTimeUtc -lt $ledgerStarted){throw 'Authority ledger report is stale; no verification claim permitted.'}
        [xml]$ledgerEvidence=Get-Content -LiteralPath $ledgerReport.FullName
        $ledgerCounters=$ledgerEvidence.SelectSingleNode("//*[local-name()='Counters']")
        if(!$ledgerCounters -or [int]$ledgerCounters.total -lt 1 -or [int]$ledgerCounters.passed -ne [int]$ledgerCounters.total){throw 'Authority ledger must actually run and pass every case.'}
    }elseif($CosmosPreparedOwners){
        $cosmosProject=Join-Path $taskRoot 'Providers/Blockchain/TestProjects/NextGenSoftware.OASIS.API.Providers.CosmosBlockChainOASIS.UnitTests/NextGenSoftware.OASIS.API.Providers.CosmosBlockChainOASIS.UnitTests.csproj'
        if($CosmosFull){
            & dotnet test $cosmosProject --no-restore /m:2 /nr:false /p:UseSharedCompilation=false /p:BuildProjectReferences=false --logger 'trx;LogFileName=cosmos-full-owned-current.trx' --results-directory $work -v:q
        }else{
            & dotnet test $cosmosProject --no-restore /m:2 /nr:false /p:UseSharedCompilation=false /p:BuildProjectReferences=false --filter 'FullyQualifiedName~RealOfflineSignedDocumentVerifiesThroughCSharp' --logger 'trx;LogFileName=cosmos-signed-owner-mongo-current.trx' --results-directory $work -v:q
        }
    }elseif($SuiPreparedLedger -or $SuiPreparedInspect){
        $suiProject=Join-Path $taskRoot 'Providers/Blockchain/TestProjects/NextGenSoftware.OASIS.API.Providers.SuiOASIS.IntegrationTests/NextGenSoftware.OASIS.API.Providers.SuiOASIS.IntegrationTests.csproj'
        $suiFilter=if($SuiCSharpAssets){'FullyQualifiedName~SuiAdmittedNftFacadeTests'}elseif($SuiCSharpNft){'FullyQualifiedName~AdmittedCSharpNftFacadeMintsReadsEscrowsTransfersAndBurns'}elseif($ArchiveSuiPrepared){'FullyQualifiedName~ArchiveConfirmedRetainedLocalFixtureWithoutResubmission'}elseif($SuiPreparedInspect){'FullyQualifiedName~InspectRetainedPreparedAttemptsWithoutMutation'}else{'FullyQualifiedName~PreparedLocalWriteRetainsOriginalSignedEvidenceAndConfirmsLedger'}
        $suiReport=if($SuiCSharpAssets){'trx;LogFileName=sui-csharp-assets-live-current.trx'}elseif($SuiCSharpNft){'trx;LogFileName=sui-csharp-nft-live-current.trx'}elseif($ArchiveSuiPrepared){'trx;LogFileName=sui-prepared-original-archive-current.trx'}elseif($SuiPreparedInspect){'trx;LogFileName=sui-prepared-inspection-current.trx'}else{'trx;LogFileName=sui-prepared-ledger-current.trx'}
        & dotnet test $suiProject --no-build --no-restore --filter $suiFilter --logger $suiReport --results-directory (Join-Path $taskRoot 'TestResults/SuiCurrent') -v:q
    }elseif($MongoV2){
        $routingProject=Join-Path $taskRoot 'OASIS Architecture/NextGenSoftware.OASIS.API.Core.HyperDrive.IntegrationTests/NextGenSoftware.OASIS.API.Core.HyperDrive.IntegrationTests.csproj'
        $routingStarted=[DateTime]::UtcNow
        & dotnet test $routingProject --no-build --no-restore --filter 'FullyQualifiedName~IntegrationTests.MongoSqliteRealProviderRoutingTests.' --logger 'trx;LogFileName=mongo-sqlite-v2-current.trx' --results-directory $work -v:q
        if($LASTEXITCODE -ne 0){throw 'Mongo/SQLite V2 routing verification failed.'}
        $routingReport=Get-Item -LiteralPath (Join-Path $work 'mongo-sqlite-v2-current.trx') -ErrorAction Stop
        if($routingReport.LastWriteTimeUtc -lt $routingStarted){throw 'Mongo/SQLite V2 report is stale; no verification claim permitted.'}
        [xml]$routingEvidence=Get-Content -LiteralPath $routingReport.FullName
        $routingCounters=$routingEvidence.SelectSingleNode("//*[local-name()='Counters']")
        if(!$routingCounters -or [int]$routingCounters.total -ne 4 -or [int]$routingCounters.passed -ne 4){throw 'All four sync/async, Mongo/SQLite-primary matrix cases must actually execute and pass.'}
    }elseif($MongoProjection -or $MongoTransactions -or $MongoGraphReconcile){
        $env:OASIS_MONGO_REPLICA_SET_CONNECTION=$env:SUBSCRIPTION_TEST_MONGODB_URI
        $mongoProject=Join-Path $taskRoot 'Providers/Storage/NextGenSoftware.OASIS.API.Providers.MongoOASIS.IntegrationTests/NextGenSoftware.OASIS.API.Providers.MongoOASIS.IntegrationTests.csproj'
        $mongoFilter=if($MongoGraphReconcile){'FullyQualifiedName~ReconcileAcknowledgedRootOnlyFixture'}elseif($MongoTransactions){'Category!=ExternalPrimaryTermination&Category!=OwnedFixtureReconciliation&FullyQualifiedName!~AcknowledgedOperationRemainsIdempotentAcrossPrimaryElection'}else{'FullyQualifiedName~HostedMongoProjectionContractTests'}
        $mongoReport=if($MongoGraphReconcile){'trx;LogFileName=mongo-graph-reconciliation-current.trx'}elseif($MongoTransactions){'trx;LogFileName=mongo-single-node-transactions-current.trx'}else{'trx;LogFileName=mongo-projection-current.trx'}
        $mongoStarted=[DateTime]::UtcNow
        & dotnet test $mongoProject --no-restore /m:2 /nr:false /p:UseSharedCompilation=false /p:BuildProjectReferences=false --filter $mongoFilter --logger $mongoReport --results-directory $work -v:q -clp:ErrorsOnly
        if($LASTEXITCODE -ne 0){throw 'Mongo native verification failed.'}
        $mongoFile=Get-Item -LiteralPath (Join-Path $work ($mongoReport -replace '^trx;LogFileName=','')) -ErrorAction Stop
        if($mongoFile.LastWriteTimeUtc -lt $mongoStarted){throw 'Mongo native report is stale.'}
        [xml]$mongoEvidence=Get-Content -LiteralPath $mongoFile.FullName -Raw
        $mongoCounts=$mongoEvidence.TestRun.ResultSummary.Counters
        $expectedMongo=if($MongoTransactions){46}else{1}
        if([int]$mongoCounts.total -ne $expectedMongo -or [int]$mongoCounts.passed -ne $expectedMongo -or [int]$mongoCounts.notExecuted -ne 0){throw 'Expected complete selected Mongo native suite without skips.'}
    }elseif($TronPreparedStorage){
        $tronProject=Join-Path $taskRoot 'Providers/Blockchain/TestProjects/NextGenSoftware.OASIS.API.Providers.TRONOASIS.UnitTests/NextGenSoftware.OASIS.API.Providers.TRONOASIS.UnitTests.csproj'
        $tronStarted=[DateTime]::UtcNow
        $tronArgs=if($TronFull){@()}else{@('--filter','FullyQualifiedName~TronPreparedStorageTests')}
        & dotnet test $tronProject --no-restore /m:2 /nr:false /p:UseSharedCompilation=false /p:BuildProjectReferences=false @tronArgs --logger 'trx;LogFileName=tron-prepared-storage-owned-current.trx' --results-directory $work -v:q
        if($LASTEXITCODE -ne 0){throw 'TRON owned prepared storage verification failed.'}
        $report=Get-Item -LiteralPath (Join-Path $work 'tron-prepared-storage-owned-current.trx')
        if($report.LastWriteTimeUtc -lt $tronStarted){throw 'TRON prepared storage report is stale.'}
        [xml]$evidence=Get-Content -LiteralPath $report.FullName
        $counts=$evidence.TestRun.ResultSummary.Counters
        $expectedTron=if($TronFull){386}else{7}
        if([int]$counts.total -ne $expectedTron -or [int]$counts.passed -ne $expectedTron -or [int]$counts.notExecuted -ne 0){throw 'Expected complete selected TRON unit suite without skips.'}
    }elseif($SuiUnit){
        $env:OASIS_SUI_SDK_BRIDGE=Join-Path $taskRoot 'Providers/Blockchain/NextGenSoftware.OASIS.API.Providers.SuiOASIS/SdkBridge/bridge.mjs'
        if(!(Test-Path -LiteralPath $env:OASIS_SUI_SDK_BRIDGE) -or !(Test-Path -LiteralPath (Join-Path (Split-Path $env:OASIS_SUI_SDK_BRIDGE) 'node_modules/@mysten/sui/package.json'))){throw 'Retained Sui SDK installation required; no automatic download.'}
        $suiProject=Join-Path $taskRoot 'Providers/Blockchain/TestProjects/NextGenSoftware.OASIS.API.Providers.SuiOASIS.UnitTests/NextGenSoftware.OASIS.API.Providers.SuiOASIS.UnitTests.csproj'
        $started=[DateTime]::UtcNow
        & dotnet test $suiProject --no-restore /m:2 /nr:false /p:UseSharedCompilation=false /p:BuildProjectReferences=false --logger 'trx;LogFileName=sui-full-owned-current.trx' --results-directory $work -v:q
        if($LASTEXITCODE -ne 0){throw 'Sui owned unit regression failed.'}
        $report=Get-Item -LiteralPath (Join-Path $work 'sui-full-owned-current.trx')
        if($report.LastWriteTimeUtc -lt $started){throw 'Sui unit report is stale.'}
        [xml]$evidence=Get-Content -LiteralPath $report.FullName
        $counts=$evidence.TestRun.ResultSummary.Counters
        if([int]$counts.total -ne 130 -or [int]$counts.passed -ne 130 -or [int]$counts.notExecuted -ne 0){throw 'Expected130 Sui unit cases must execute and pass.'}
    }elseif($HederaPreparedOwners){
        $env:OASIS_HEDERA_SDK_BRIDGE=Join-Path $taskRoot 'Providers/Blockchain/NextGenSoftware.OASIS.API.Providers.HashgraphOASIS/SdkBridge/bridge.mjs'
        $hederaProject=Join-Path $taskRoot 'Providers/Blockchain/TestProjects/NextGenSoftware.OASIS.API.Providers.HashgraphOASIS.UnitTests/NextGenSoftware.OASIS.API.Providers.HashgraphOASIS.UnitTests.csproj'
        $hederaArgs=if($HederaFull){@('--logger','trx;LogFileName=hedera-full-owned-current.trx')}else{@('--filter','FullyQualifiedName~SignedEscrowStagesPersistExactEvidenceWithDistinctCorrelations','--logger','trx;LogFileName=hedera-real-signed-owner-mongo-current.trx')}
        $hederaStarted=[DateTime]::UtcNow
        & dotnet test $hederaProject --no-restore /m:2 /nr:false /p:UseSharedCompilation=false /p:BuildProjectReferences=false @hederaArgs --results-directory $work -v:q
        if($LASTEXITCODE -ne 0){throw 'Hedera owned regression failed.'}
        if($HederaFull){
            $report=Get-Item -LiteralPath (Join-Path $work 'hedera-full-owned-current.trx')
            if($report.LastWriteTimeUtc -lt $hederaStarted){throw 'Hedera full report is stale.'}
            [xml]$evidence=Get-Content -LiteralPath $report.FullName
            $counts=$evidence.TestRun.ResultSummary.Counters
            if([int]$counts.total -ne 113 -or [int]$counts.passed -ne 113 -or [int]$counts.notExecuted -ne 0){throw 'Expected113 passing Hedera unit cases without skips.'}
        }
    }elseif($SuiMintOwner -or $ReconcileControlledMintOwner){
        $mintProject=Join-Path $taskRoot 'Providers/Blockchain/TestProjects/NextGenSoftware.OASIS.API.Providers.SuiOASIS.IntegrationTests/NextGenSoftware.OASIS.API.Providers.SuiOASIS.IntegrationTests.csproj'
        $mintFilter=if($ReconcileControlledMintOwner){'FullyQualifiedName~ReconcileControlledMintOwnerFixture'}else{'FullyQualifiedName~PublicMintRetainsOriginalAttemptInReusableMongo'}
        $mintReport=if($ReconcileControlledMintOwner){'trx;LogFileName=controlled-mint-owner-reconciliation-current.trx'}else{'trx;LogFileName=sui-mint-owner-real-mongo-current.trx'}
        & dotnet test $mintProject --no-restore /m:2 /nr:false /p:UseSharedCompilation=false /p:BuildProjectReferences=false --filter $mintFilter --logger $mintReport --results-directory $work -v:q
    }else{
        & dotnet test $project --no-build --no-restore --filter 'FullyQualifiedName!~OwnedReplicaInitializationTests' --logger 'trx;LogFileName=usage-real-replica-current.trx' --results-directory $work -v:q
    }
    if($LASTEXITCODE -ne 0){throw 'Usage recovery regression failed.'}
}finally{
    try{
        if($owned){
            if(!$owned.HasExited){$owned.Kill();if(!$owned.WaitForExit(10000)){throw 'Owned Mongo did not terminate.'}}
            $owned.Dispose()
            Limit-OwnedMongoLog -WorkDirectory $work
        }
    }finally{
        foreach($name in $prior.Keys){
            if($null -eq $prior[$name]){Remove-Item -LiteralPath "Env:$name" -ErrorAction SilentlyContinue}
            else{[Environment]::SetEnvironmentVariable($name,$prior[$name])}
        }
        $lease.Dispose()
    }
}
# Retain one reusable replica data directory; individual fixture databases are removed by tests.
