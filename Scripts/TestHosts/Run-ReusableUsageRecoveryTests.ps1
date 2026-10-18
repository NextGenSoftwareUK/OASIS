#Requires -Version 7.0
param([switch]$SuiMintOwner,[switch]$ReconcileControlledMintOwner,[switch]$HederaPreparedOwners,[switch]$HederaFull,[switch]$MongoProjection,[switch]$MongoTransactions,[switch]$MongoV2,[switch]$SuiPreparedLedger,[switch]$CosmosPreparedOwners,[switch]$CosmosFull,[switch]$AuthorityLedger,[switch]$SuiPreparedInspect,[switch]$Web3PreparedOwners,[switch]$MongoGraphReconcile,[switch]$ArchiveSuiPrepared,[switch]$SuiCSharpNft,[switch]$SuiCSharpAssets,[switch]$TronPreparedStorage,[switch]$TronFull,[switch]$SuiUnit,[switch]$SuiCSharpEscrow,[switch]$CosmosNative,[switch]$CosmosReconcile,[switch]$CosmosRouting,[switch]$TronNative,[switch]$HederaNativeRead,[switch]$HederaReadInspect,[switch]$HederaReadArchive,[switch]$HederaRoutedRead,[switch]$HederaFeeRecovery,[switch]$CosmosRoutingInspect,[switch]$CosmosRoutingReconcile,[switch]$CosmosRoutingProbe,[switch]$CosmosReviewedFees,[switch]$CosmosRoutingFeeRecovery,[switch]$CosmosReviewedBank,[switch]$SuiCSharpHistory,[switch]$CosmosReviewedToken,[switch]$CosmosReviewedNFT,[switch]$CosmosRoutingVersions,[ValidateSet('Fees','DeployEscrow','LockEscrow')][string]$CosmosReviewScenario='Fees')
$ErrorActionPreference='Stop'
if($CosmosReviewScenario -ne 'Fees' -and (!$CosmosPreparedOwners -or !$CosmosReviewedFees -or $PSBoundParameters.Count -ne 3)){throw 'Escrow deployment requires exclusive existing-owner reviewed-fee mode.'}
if($CosmosReviewedNFT -and (!$CosmosPreparedOwners -or $PSBoundParameters.Count -ne 2)){throw 'Reviewed NFT interfaces require exclusive existing-owner environment mode.'}
if($CosmosReviewedToken -and (!$CosmosPreparedOwners -or $PSBoundParameters.Count -ne 2)){throw 'Reviewed token interfaces require exclusive existing-owner environment mode.'}
if($SuiCSharpHistory -and (!$SuiPreparedLedger -or $PSBoundParameters.Count -ne 2)){throw 'Read-only Sui history requires exclusive retained-ledger mode.'}
if($CosmosReviewedBank -and (!$CosmosPreparedOwners -or $PSBoundParameters.Count -ne 2)){throw 'Reviewed Cosmos bank test requires exclusive existing-owner environment mode.'}
if($CosmosRoutingFeeRecovery -and (!$CosmosPreparedOwners -or $PSBoundParameters.Count -ne $(if($CosmosRoutingVersions){3}else{2}))){throw 'Original-only Cosmos recovery requires exclusive existing-owner environment mode.'}
if($CosmosReviewedFees -and (!$CosmosPreparedOwners -or $PSBoundParameters.Count -ne $(if($CosmosReviewScenario -ne 'Fees'){3}else{2}))){throw 'Reviewed Cosmos fee test requires exclusive existing-owner environment mode.'}
if($CosmosRoutingReconcile -and !$CosmosRoutingInspect){throw 'Reviewed Cosmos routing cleanup requires explicit incident selection.'}
if($CosmosRoutingProbe -and (!$CosmosRoutingInspect -or $CosmosRoutingReconcile)){throw 'Zero-budget probe requires exclusive incident selection, never cleanup.'}
if($CosmosRoutingInspect -and (!$CosmosPreparedOwners -or $PSBoundParameters.Count -ne $(if($CosmosRoutingReconcile -or $CosmosRoutingProbe){3}else{2}))){throw 'Cosmos routing incident inspection/reconciliation requires exclusive owned environment mode.'}
if($HederaFeeRecovery -and (!$HederaPreparedOwners -or $PSBoundParameters.Count -ne 2)){throw 'Exact Hedera fee recovery requires its exclusive owned environment mode.'}
if($HederaRoutedRead -and !$HederaNativeRead){throw 'Routed Hedera read requires explicit native read mode.'}
if($HederaNativeRead -and (!$HederaPreparedOwners -or $HederaFull -or $PSBoundParameters.Count -ne $(if($HederaRoutedRead){3}else{2}))){throw 'Native Hedera read requires its exclusive owned environment mode.'}
if($HederaReadInspect -and (!$HederaPreparedOwners -or $PSBoundParameters.Count -ne 2)){throw 'Read-only Hedera inspection requires its exclusive owned environment mode.'}
if($HederaReadArchive -and (!$HederaPreparedOwners -or $PSBoundParameters.Count -ne 2)){throw 'Reviewed Hedera archive requires its exclusive owned environment mode.'}
if($TronNative -and (!$TronPreparedStorage -or $TronFull)){throw 'Independent TRON native verification requires its exclusive existing-owner mode.'}
if($CosmosRoutingVersions -and !$CosmosRouting -and !$CosmosRoutingFeeRecovery){throw 'Versioned routing requires exclusive Cosmos routing or original recovery mode.'}
if($CosmosRouting -and (!$CosmosPreparedOwners -or $PSBoundParameters.Count -ne $(if($CosmosRoutingVersions){3}else{2}))){throw 'Independent Cosmos routing requires its exclusive existing-owner mode.'}
if($CosmosReconcile -and !$CosmosNative){throw 'Reviewed reconciliation requires explicit native Cosmos mode.'}
if($CosmosNative -and (!$CosmosPreparedOwners -or $PSBoundParameters.Count -ne $(if($CosmosReconcile){3}else{2}))){throw 'Independent native Cosmos requires its exclusive existing-owner mode.'}
if($SuiUnit -and $PSBoundParameters.Count -ne 1){throw 'Sui unit verification requires its own exclusive owned environment mode.'}
if($SuiCSharpEscrow -and (!$SuiPreparedLedger -or $SuiCSharpNft -or $SuiCSharpAssets)){throw 'Sui CSharp escrow requires its exclusive owned prepared-ledger mode.'}
if($TronFull -and !$TronPreparedStorage){throw 'TRON full unit suite requires owned prepared storage mode.'}
if($TronPreparedStorage -and $PSBoundParameters.Count -ne $(if($TronFull -or $TronNative){2}else{1})){throw 'TRON prepared storage requires its own exclusive mode.'}
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
        $cosmosStarted=[DateTime]::UtcNow
        if($CosmosReviewedToken -or $CosmosReviewedNFT -or $CosmosReviewScenario -eq 'LockEscrow'){
            $cosmosProject=Join-Path $taskRoot 'OASIS Architecture/NextGenSoftware.OASIS.API.Core.HyperDrive.IntegrationTests/NextGenSoftware.OASIS.API.Core.HyperDrive.IntegrationTests.csproj'
            $nativeReport=if($CosmosReviewScenario -eq 'LockEscrow'){'cosmos-reviewed-escrow-interface-current.trx'}elseif($CosmosReviewedNFT){'cosmos-reviewed-nft-interface-current.trx'}else{'cosmos-reviewed-token-interface-current.trx'}
            $priorAssetReport=Join-Path $work $nativeReport
            if(Test-Path -LiteralPath $priorAssetReport){
                if((Get-Item -LiteralPath $priorAssetReport).Length -gt 1MB){throw 'Review oversized native financial evidence before making another copy.'}
                $priorAssetHash=(Get-FileHash -LiteralPath $priorAssetReport -Algorithm SHA256).Hash
                $assetFamily=if($CosmosReviewScenario -eq 'LockEscrow'){'escrow'}elseif($CosmosReviewedNFT){'nft'}else{'token'}
                $assetArchive=Join-Path $taskRoot "TestResults/HyperDriveCosmos/$assetFamily-financial-proof-$priorAssetHash.trx"
                if(!(Test-Path -LiteralPath $assetArchive)){[IO.File]::Copy($priorAssetReport,$assetArchive,$false)}
                if((Get-FileHash -LiteralPath $assetArchive -Algorithm SHA256).Hash -ne $priorAssetHash){throw 'Prior native financial report archive mismatch; refusing overwrite.'}
            }
            $assetMethod=if($CosmosReviewScenario -eq 'LockEscrow'){'IdBoundEscrowUsesPersistedAvatarWalletAndOriginalFees'}elseif($CosmosReviewedNFT){'NftInterfacesRetainOwnershipAndOriginalFees'}else{'CustomTokenInterfacesRetainExactSupplyOwnershipAndOriginalFees'}
            & dotnet test $cosmosProject --no-build --no-restore --filter "FullyQualifiedName~CosmosReviewedTokenInterfaceTests.$assetMethod" --logger "trx;LogFileName=$nativeReport" --results-directory $work -v:q
        }elseif($CosmosReviewedBank){
            $cosmosProject=Join-Path $taskRoot 'OASIS Architecture/NextGenSoftware.OASIS.API.Core.HyperDrive.IntegrationTests/NextGenSoftware.OASIS.API.Core.HyperDrive.IntegrationTests.csproj'
            $nativeReport='cosmos-reviewed-bank-interface-current.trx'
            & dotnet test $cosmosProject --no-build --no-restore --filter 'FullyQualifiedName~CosmosReviewedBankInterfaceTests.PublicBankInterfacesPreserveAmountsRangeStatusAndFinalFees' --logger "trx;LogFileName=$nativeReport" --results-directory $work -v:q
        }elseif($CosmosRoutingFeeRecovery){
            $cosmosProject=Join-Path $taskRoot 'OASIS Architecture/NextGenSoftware.OASIS.API.Core.HyperDrive.IntegrationTests/NextGenSoftware.OASIS.API.Core.HyperDrive.IntegrationTests.csproj'
            $nativeReport=if($CosmosRoutingVersions){'cosmos-versioned-routing-recovery-current.trx'}else{'cosmos-reviewed-routing-recovery-current.trx'}
            $recoveryMethod=if($CosmosRoutingVersions){'RecoverExactVersionedRoutingIncidentWithoutNativeReplay'}else{'RecoverExactTimedOutReviewedRoutingWithoutNativeReplay'}
            & dotnet test $cosmosProject --no-build --no-restore --filter "FullyQualifiedName~CosmosReviewedRoutingRecoveryTests.$recoveryMethod" --logger "trx;LogFileName=$nativeReport" --results-directory $work -v:q
        }elseif($CosmosReviewedFees){
            $cosmosProject=Join-Path $taskRoot 'OASIS Architecture/NextGenSoftware.OASIS.API.Core.HyperDrive.IntegrationTests/NextGenSoftware.OASIS.API.Core.HyperDrive.IntegrationTests.csproj'
            $nativeReport=if($CosmosReviewScenario -eq 'DeployEscrow'){'cosmos-reviewed-escrow-deployment-current.trx'}else{'cosmos-reviewed-final-fee-current.trx'}
            $reviewedMethod=if($CosmosReviewScenario -eq 'DeployEscrow'){'CosmosReviewedEscrowDeploymentTests.DeployOnceWithOriginalAdmittedUploadAndInstantiation'}else{'CosmosReviewedFinalFeeTests.ActualReviewedDeploymentRecoversSuccessfulAndChargedFailedOriginalsWithoutReplay'}
            & dotnet test $cosmosProject --no-build --no-restore --filter "FullyQualifiedName~$reviewedMethod" --logger "trx;LogFileName=$nativeReport" --results-directory $work -v:q
        }elseif($CosmosRoutingInspect){
            $cosmosProject=Join-Path $taskRoot 'OASIS Architecture/NextGenSoftware.OASIS.API.Core.HyperDrive.IntegrationTests/NextGenSoftware.OASIS.API.Core.HyperDrive.IntegrationTests.csproj'
            $nativeReport=if($CosmosRoutingProbe){'cosmos-routing-zero-budget-probe-current.trx'}elseif($CosmosRoutingReconcile){'cosmos-routing-profile-reconciliation-current.trx'}else{'cosmos-routing-profile-inspection-current.trx'}
            $incidentMethod=if($CosmosRoutingProbe){'ZeroBudgetPostOutageDeleteCannotPersistOrBroadcast'}elseif($CosmosRoutingReconcile){'ReconcileExactFailedProfileWithNewAdmittedCleanupWithoutReplay'}else{'InspectCurrentFailedProfileOriginalsWithoutMutation'}
            & dotnet test $cosmosProject --no-build --no-restore --filter "FullyQualifiedName~CosmosRoutingInspectionTests.$incidentMethod" --logger "trx;LogFileName=$nativeReport" --results-directory $work -v:q
        }elseif($CosmosRouting){
            $cosmosProject=Join-Path $taskRoot 'OASIS Architecture/NextGenSoftware.OASIS.API.Core.HyperDrive.IntegrationTests/NextGenSoftware.OASIS.API.Core.HyperDrive.IntegrationTests.csproj'
            $nativeReport=if($CosmosRoutingVersions){'cosmos-versioned-routing-current.trx'}else{'cosmos-independent-routing-current.trx'}
            $priorRoutingReport=Join-Path $work $nativeReport
            if(Test-Path -LiteralPath $priorRoutingReport){
                if((Get-Item -LiteralPath $priorRoutingReport).Length -gt 1MB){throw 'Review oversized routing financial evidence before copying.'}
                [xml]$priorRoutingEvidence=Get-Content -LiteralPath $priorRoutingReport
                if(!$priorRoutingEvidence.TestRun.ResultSummary.Counters){throw 'Prior routing evidence is malformed; inspect before overwrite.'}
                # Successful native reports also contain unique wallet baselines and originals.
                # Preserve once by content hash, not a fresh per-run directory.
                $priorRoutingHash=(Get-FileHash -LiteralPath $priorRoutingReport -Algorithm SHA256).Hash
                $routingArchive=Join-Path $taskRoot "TestResults/HyperDriveCosmos/routing-financial-proof-$priorRoutingHash.trx"
                if(!(Test-Path -LiteralPath $routingArchive)){[IO.File]::Copy($priorRoutingReport,$routingArchive,$false)}
                if((Get-FileHash -LiteralPath $routingArchive -Algorithm SHA256).Hash -ne $priorRoutingHash){throw 'Routing financial report archive mismatch; refusing overwrite.'}
            }
            $routingMethod=if($CosmosRoutingVersions){'VersionedRecordsReplicateAndRetainHistoricalValues'}else{'IndependentLeafReplicatesThroughRealCosmosWithPreparedOriginals'}
            & dotnet test $cosmosProject --no-build --no-restore --filter "FullyQualifiedName~CosmosIndependentRoutingTests.$routingMethod" --logger "trx;LogFileName=$nativeReport" --results-directory $work -v:q
        }elseif($CosmosNative){
            $cosmosProject=Join-Path $taskRoot 'Providers/Blockchain/TestProjects/NextGenSoftware.OASIS.API.Providers.CosmosBlockChainOASIS.IntegrationTests/NextGenSoftware.OASIS.API.Providers.CosmosBlockChainOASIS.IntegrationTests.csproj'
            $nativeMethod=if($CosmosReconcile){'ReconcileReviewedIndependentOriginalWithoutReplay'}else{'IndependentPreparedKeyUsesSignedAggregateBudgetAndOriginalNativeReceipts'}
            $nativeReport=if($CosmosReconcile){'cosmos-independent-reconciliation-current.trx'}else{'cosmos-independent-prepared-native-current.trx'}
            & dotnet test $cosmosProject --no-build --no-restore --filter "FullyQualifiedName~CosmosPreparedNativeLedgerTests.$nativeMethod" --logger "trx;LogFileName=$nativeReport" --results-directory $work -v:q
        }elseif($CosmosFull){
            & dotnet test $cosmosProject --no-restore /m:2 /nr:false /p:UseSharedCompilation=false /p:BuildProjectReferences=false --logger 'trx;LogFileName=cosmos-full-owned-current.trx' --results-directory $work -v:q
        }else{
            & dotnet test $cosmosProject --no-restore /m:2 /nr:false /p:UseSharedCompilation=false /p:BuildProjectReferences=false --filter 'FullyQualifiedName~RealOfflineSignedDocumentVerifiesThroughCSharp' --logger 'trx;LogFileName=cosmos-signed-owner-mongo-current.trx' --results-directory $work -v:q
        }
        if($LASTEXITCODE -ne 0){throw 'Cosmos owned regression failed.'}
        $cosmosReport=Get-Item -LiteralPath (Join-Path $work $(if($CosmosNative -or $CosmosRouting -or $CosmosRoutingInspect -or $CosmosReviewedFees -or $CosmosRoutingFeeRecovery -or $CosmosReviewedBank -or $CosmosReviewedToken -or $CosmosReviewedNFT){$nativeReport}elseif($CosmosFull){'cosmos-full-owned-current.trx'}else{'cosmos-signed-owner-mongo-current.trx'}))
        if($cosmosReport.LastWriteTimeUtc -lt $cosmosStarted){throw 'Cosmos report is stale.'}
        [xml]$cosmosEvidence=Get-Content -LiteralPath $cosmosReport.FullName
        $cosmosCounts=$cosmosEvidence.TestRun.ResultSummary.Counters
        $expectedCosmos=if($CosmosReviewScenario -eq 'LockEscrow'){2}elseif($CosmosReviewedToken -or $CosmosReviewedNFT -or ($CosmosRoutingVersions -and $CosmosRouting)){2}elseif($CosmosRoutingInspect -or $CosmosReviewedFees -or $CosmosRoutingFeeRecovery -or $CosmosReviewedBank){1}elseif($CosmosRouting){4}elseif($CosmosNative){if($CosmosReconcile){1}else{2}}elseif($CosmosFull){240}else{3}
        if([int]$cosmosCounts.total -ne $expectedCosmos -or [int]$cosmosCounts.passed -ne $expectedCosmos -or [int]$cosmosCounts.notExecuted -ne 0){throw 'All selected Cosmos cases must actually execute and pass without skips.'}
        if($CosmosReviewedToken -or $CosmosReviewedNFT -or $CosmosReviewScenario -eq 'LockEscrow'){
            $assetCount=if($CosmosReviewScenario -eq 'LockEscrow'){5}elseif($CosmosReviewedNFT){7}else{6}
            $tokenCombinations=[Collections.Generic.HashSet[bool]]::new()
            foreach($case in $cosmosEvidence.TestRun.Results.UnitTestResult){
                $line=([string]$case.Output.StdOut -split "`n"|Where-Object{$_.Trim().StartsWith('{"operation"')}|Select-Object -First 1)
                if(!$line){throw 'Reviewed asset native evidence is missing.'}
                $caseData=$line.Trim()|ConvertFrom-Json
                if([bool]$caseData.versioned -ne [bool]$CosmosRoutingVersions){throw 'Selected version-control mode does not match native evidence.'}
                $expectedOriginals=if($CosmosRoutingVersions){21}else{10}
                if($CosmosRoutingVersions -and (!$caseData.nativePrimary -or $caseData.historicalReadCount -ne 6 -or $caseData.currentVersionReadCount -ne 10 -or $caseData.historicalAbsenceReadCount -ne 3)){throw 'Exact Cosmos historical/current/cleanup acceptance is missing.'}
                if(!$tokenCombinations.Add([bool]$caseData.synchronous)){throw 'Duplicate asset interface mode cannot replace sync/async coverage.'}
                if(@($caseData.hashes).Count -ne $assetCount -or @($caseData.operation.ProviderAttempts).Count -ne $assetCount -or @($caseData.operation.Receipts).Count -ne $assetCount -or
                    (!$CosmosReviewedNFT -and $caseData.finalSupply -ne '0') -or ($CosmosReviewScenario -eq 'LockEscrow' -and $caseData.family -ne 'escrow') -or ($CosmosReviewedNFT -and ($caseData.family -ne 'nft' -or !$caseData.nftBurned -or !$caseData.unrelatedCustodyProofRejected))){
                    throw 'Expected original asset transactions, durable receipts and verified final asset state per mode.'
                }
            }
        }
        if($CosmosRouting){
            $routingCombinations=[Collections.Generic.HashSet[string]]::new()
            foreach($case in $cosmosEvidence.TestRun.Results.UnitTestResult){
                $line=([string]$case.Output.StdOut -split "`n"|Where-Object{$_.Trim().StartsWith('{"nativePrimary"')}|Select-Object -First 1)
                if(!$line){throw 'Native routing signed evidence is missing from the report.'}
                $caseData=$line.Trim()|ConvertFrom-Json
                if(!$routingCombinations.Add("$($caseData.nativePrimary):$($caseData.synchronous)")){throw 'Duplicate routing combination cannot substitute for the complete primary/sync matrix.'}
                if($caseData.strategyReadCount -ne 16 -or $caseData.roundRobinReadCount -ne 6 -or $caseData.freeReadCount -ne 8){throw 'Full current strategy/distribution/free-plan matrix evidence is missing.'}
                if($caseData.nearbyNativeReadCount -ne 18 -or $caseData.nearbyInvalidInputCount -ne 4){throw 'Native nearby boundaries/deletion and invalid-input acceptance are missing.'}
                $originals=$caseData.originals
                if(@($originals).Count -ne $expectedOriginals -or $caseData.profileMatchingReadCount -ne 28 -or $caseData.profileAbsenceReadCount -ne 28){throw 'Expected full profile/detail/leaf native attempts and matching/absence read coverage per routing case.'}
                $expectedOutageReads=if(!$caseData.nativePrimary -and $caseData.synchronous){14}else{0}
                if($caseData.outageProfileReadCount -ne $expectedOutageReads){throw 'Exact real stopped-node profile failover coverage is missing.'}
                foreach($original in $originals){
                    if(!$original.attempt.NativePreparation.SignedTransactionBase64 -or !$original.attempt.NativePreparation.SignDocBase64 -or $original.receipt.ProviderRequestId -ne $original.attempt.ProviderRequestId -or !$original.receipt.NativeFeeAudit){throw 'Original signed bytes/SignDoc or matching audited native fee receipt absent.'}
                    $audit=$original.receipt.NativeFeeAudit.PayloadJson|ConvertFrom-Json
                    if($original.receipt.NativeExecutionSucceeded -ne $true -or !$audit.NativeExecutionSucceeded -or $audit.NativeExecutionCode -ne 0 -or $original.receipt.CostSource -ne $caseData.review.EvidenceBasis){throw 'Exact typed/audited reviewed native success/fee basis evidence missing.'}
                }
                if($caseData.netFinalNativeReceiptCount -ne $expectedOriginals){throw 'Expected exact durable final-fee receipts per real V2 routing case.'}
            }
            if($routingCombinations.Count -ne $expectedCosmos){throw 'Every selected provider-order/sync combination must be represented.'}
        }
    }elseif($SuiPreparedLedger -or $SuiPreparedInspect){
        $suiProject=Join-Path $taskRoot 'Providers/Blockchain/TestProjects/NextGenSoftware.OASIS.API.Providers.SuiOASIS.IntegrationTests/NextGenSoftware.OASIS.API.Providers.SuiOASIS.IntegrationTests.csproj'
        $suiFilter=if($SuiCSharpHistory){'FullyQualifiedName~RetainedCSharpHistoryUsesCheckpointBoundsWithoutNativeMutation'}elseif($SuiCSharpEscrow){'FullyQualifiedName~AdmittedCSharpIdBoundTokenFacadeUsesEncryptedWalletAndOriginalReceipts'}elseif($SuiCSharpAssets){'FullyQualifiedName~SuiAdmittedNftFacadeTests&FullyQualifiedName!~AdmittedCSharpIdBoundTokenFacadeUsesEncryptedWalletAndOriginalReceipts&FullyQualifiedName!~RetainedCSharpHistoryUsesCheckpointBoundsWithoutNativeMutation'}elseif($SuiCSharpNft){'FullyQualifiedName~AdmittedCSharpNftFacadeMintsReadsEscrowsTransfersAndBurns'}elseif($ArchiveSuiPrepared){'FullyQualifiedName~ArchiveConfirmedRetainedLocalFixtureWithoutResubmission'}elseif($SuiPreparedInspect){'FullyQualifiedName~InspectRetainedPreparedAttemptsWithoutMutation'}else{'FullyQualifiedName~PreparedLocalWriteRetainsOriginalSignedEvidenceAndConfirmsLedger|FullyQualifiedName~NativeHttpReservesExecutesAndRecoversWithoutReplay'}
        $suiReport=if($SuiCSharpHistory){'trx;LogFileName=sui-retained-history-range-current.trx'}elseif($SuiCSharpEscrow){'trx;LogFileName=sui-csharp-escrow-live-current.trx'}elseif($SuiCSharpAssets){'trx;LogFileName=sui-csharp-assets-live-current.trx'}elseif($SuiCSharpNft){'trx;LogFileName=sui-csharp-nft-live-current.trx'}elseif($ArchiveSuiPrepared){'trx;LogFileName=sui-prepared-original-archive-current.trx'}elseif($SuiPreparedInspect){'trx;LogFileName=sui-prepared-inspection-current.trx'}else{'trx;LogFileName=sui-prepared-ledger-current.trx'}
        $suiStarted=[DateTime]::UtcNow
        & dotnet test $suiProject --no-build --no-restore --filter $suiFilter --logger $suiReport --results-directory (Join-Path $taskRoot 'TestResults/SuiCurrent') -v:q
        if($LASTEXITCODE -ne 0){throw 'Sui native verification failed; retain original ownership journals.'}
        if($SuiPreparedLedger -or $SuiPreparedInspect){
            $report=Get-Item -LiteralPath (Join-Path $taskRoot ('TestResults/SuiCurrent/'+($suiReport -replace '^trx;LogFileName=','')))
            if($report.LastWriteTimeUtc -lt $suiStarted){throw 'Sui prepared report is stale.'}
            [xml]$evidence=Get-Content -LiteralPath $report.FullName -Raw
            $counts=$evidence.TestRun.ResultSummary.Counters
            $expectedSui=if($SuiCSharpAssets){4}elseif($SuiCSharpNft -or $SuiCSharpEscrow -or $SuiPreparedInspect -or $SuiCSharpHistory){1}else{5}
            if(!$counts -or [int]$counts.total -ne $expectedSui -or [int]$counts.executed -ne $expectedSui -or [int]$counts.passed -ne $expectedSui -or [int]$counts.notExecuted -ne 0){throw "All $expectedSui selected Sui native cases must execute and pass."}
        }
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
        $mongoFilter=if($MongoGraphReconcile){'FullyQualifiedName~ReconcileAcknowledgedRootOnlyFixture'}elseif($MongoTransactions){'Category!=ExternalPrimaryTermination&Category!=OwnedFixtureReconciliation&FullyQualifiedName!~AcknowledgedOperationRemainsIdempotentAcrossPrimaryElection'}else{'FullyQualifiedName~HostedMongoProjectionContractTests|FullyQualifiedName~DirectMongoAvatarDetailContractTests'}
        $mongoReport=if($MongoGraphReconcile){'trx;LogFileName=mongo-graph-reconciliation-current.trx'}elseif($MongoTransactions){'trx;LogFileName=mongo-single-node-transactions-current.trx'}else{'trx;LogFileName=mongo-projection-current.trx'}
        $mongoStarted=[DateTime]::UtcNow
        & dotnet test $mongoProject --no-restore /m:2 /nr:false /p:UseSharedCompilation=false /p:BuildProjectReferences=false --filter $mongoFilter --logger $mongoReport --results-directory $work -v:q -clp:ErrorsOnly
        if($LASTEXITCODE -ne 0){throw 'Mongo native verification failed.'}
        $mongoFile=Get-Item -LiteralPath (Join-Path $work ($mongoReport -replace '^trx;LogFileName=','')) -ErrorAction Stop
        if($mongoFile.LastWriteTimeUtc -lt $mongoStarted){throw 'Mongo native report is stale.'}
        [xml]$mongoEvidence=Get-Content -LiteralPath $mongoFile.FullName -Raw
        $mongoCounts=$mongoEvidence.TestRun.ResultSummary.Counters
        $expectedMongo=if($MongoTransactions){48}elseif($MongoGraphReconcile){1}else{3}
        if([int]$mongoCounts.total -ne $expectedMongo -or [int]$mongoCounts.passed -ne $expectedMongo -or [int]$mongoCounts.notExecuted -ne 0){throw 'Expected complete selected Mongo native suite without skips.'}
    }elseif($TronPreparedStorage){
        $tronProject=Join-Path $taskRoot 'Providers/Blockchain/TestProjects/NextGenSoftware.OASIS.API.Providers.TRONOASIS.UnitTests/NextGenSoftware.OASIS.API.Providers.TRONOASIS.UnitTests.csproj'
        $tronStarted=[DateTime]::UtcNow
        $tronReport=if($TronNative){'tron-independent-prepared-native-current.trx'}else{'tron-prepared-storage-owned-current.trx'}
        if($TronNative){$tronProject=Join-Path $taskRoot 'OASIS Architecture/NextGenSoftware.OASIS.API.Core.HyperDrive.IntegrationTests/NextGenSoftware.OASIS.API.Core.HyperDrive.IntegrationTests.csproj'}
        $tronArgs=if($TronNative){@('--no-build','--filter','FullyQualifiedName~TronIndependentPreparedTests.IndependentPrivateKeyRetainsOriginalPermissionAndSolidifiedFees')}elseif($TronFull){@()}else{@('--filter','FullyQualifiedName~TronPreparedStorageTests')}
        & dotnet test $tronProject --no-restore /m:2 /nr:false /p:UseSharedCompilation=false /p:BuildProjectReferences=false @tronArgs --logger "trx;LogFileName=$tronReport" --results-directory $work -v:q
        if($LASTEXITCODE -ne 0){throw 'TRON owned prepared storage verification failed.'}
        $report=Get-Item -LiteralPath (Join-Path $work $tronReport)
        if($report.LastWriteTimeUtc -lt $tronStarted){throw 'TRON prepared storage report is stale.'}
        [xml]$evidence=Get-Content -LiteralPath $report.FullName
        $counts=$evidence.TestRun.ResultSummary.Counters
        $expectedTron=if($TronNative){1}elseif($TronFull){432}else{10}
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
        if([int]$counts.total -ne 155 -or [int]$counts.passed -ne 155 -or [int]$counts.notExecuted -ne 0){throw 'Expected155 Sui unit cases must execute and pass.'}
    }elseif($HederaPreparedOwners){
        $env:OASIS_HEDERA_SDK_BRIDGE=Join-Path $taskRoot 'Providers/Blockchain/NextGenSoftware.OASIS.API.Providers.HashgraphOASIS/SdkBridge/bridge.mjs'
        $hederaProject=Join-Path $taskRoot 'Providers/Blockchain/TestProjects/NextGenSoftware.OASIS.API.Providers.HashgraphOASIS.UnitTests/NextGenSoftware.OASIS.API.Providers.HashgraphOASIS.UnitTests.csproj'
        if($HederaNativeRead -or $HederaReadInspect -or $HederaReadArchive -or $HederaFeeRecovery){$hederaProject=Join-Path $taskRoot 'Providers/Blockchain/TestProjects/NextGenSoftware.OASIS.API.Providers.HashgraphOASIS.IntegrationTests/NextGenSoftware.OASIS.API.Providers.HashgraphOASIS.IntegrationTests.csproj'}
        $hederaArgs=if($HederaReadArchive){@('--filter','FullyQualifiedName~ArchiveExactReviewedPreExecutionReadFailureWithoutPayment','--logger','trx;LogFileName=hedera-read-pre-execution-archive-current.trx')}elseif($HederaReadInspect){@('--filter','FullyQualifiedName~InspectRetainedFailedReadLedgerWithoutSigningOrMutation','--logger','trx;LogFileName=hedera-read-ledger-inspection-current.trx')}elseif($HederaRoutedRead){@('--filter','FullyQualifiedName~RetainedTestnetRoutedReadUsesRealV2ActivationAndOriginalFees','--logger','trx;LogFileName=hedera-native-routed-read-current.trx')}elseif($HederaNativeRead){@('--filter','FullyQualifiedName~RetainedTestnetReadPreparesEveryPaymentBeforeTransportAndRecoversActualFees','--logger','trx;LogFileName=hedera-native-prepared-read-current.trx')}elseif($HederaFull){@('--logger','trx;LogFileName=hedera-full-owned-current.trx')}else{@('--filter','FullyQualifiedName~SignedEscrowStagesPersistExactEvidenceWithDistinctCorrelations','--logger','trx;LogFileName=hedera-real-signed-owner-mongo-current.trx')}
        $hederaStarted=[DateTime]::UtcNow
        if($HederaFeeRecovery){$hederaArgs=@('--filter','FullyQualifiedName~RecoverExactRetainedActivationFeesWithoutReplayOrSettlement','--logger','trx;LogFileName=hedera-activation-fee-recovery-current.trx')}
        & dotnet test $hederaProject --no-restore /m:2 /nr:false /p:UseSharedCompilation=false /p:BuildProjectReferences=false @hederaArgs --results-directory $work -v:q
        if($LASTEXITCODE -ne 0){throw 'Hedera owned regression failed.'}
        if($HederaFeeRecovery){
            $report=Get-Item -LiteralPath (Join-Path $work 'hedera-activation-fee-recovery-current.trx')
            if($report.LastWriteTimeUtc -lt $hederaStarted){throw 'Exact fee recovery report is stale.'}
            [xml]$evidence=Get-Content -LiteralPath $report.FullName -Raw
            $counts=$evidence.TestRun.ResultSummary.Counters
            if([int]$counts.total -ne 1 -or [int]$counts.passed -ne 1 -or [int]$counts.notExecuted -ne 0){throw 'Exact fee recovery must actually execute and pass.'}
        }
        if($HederaNativeRead -or $HederaReadInspect -or $HederaReadArchive){
            $report=Get-Item -LiteralPath (Join-Path $work $(if($HederaReadArchive){'hedera-read-pre-execution-archive-current.trx'}elseif($HederaReadInspect){'hedera-read-ledger-inspection-current.trx'}elseif($HederaRoutedRead){'hedera-native-routed-read-current.trx'}else{'hedera-native-prepared-read-current.trx'}))
            if($report.LastWriteTimeUtc -lt $hederaStarted){throw 'Native read report is stale.'}
            [xml]$evidence=Get-Content -LiteralPath $report.FullName -Raw
            $counts=$evidence.TestRun.ResultSummary.Counters
            if([int]$counts.total -ne 1 -or [int]$counts.passed -ne 1 -or [int]$counts.notExecuted -ne 0){throw 'The native prepared read must actually execute and pass.'}
        }
        if($HederaFull){
            $report=Get-Item -LiteralPath (Join-Path $work 'hedera-full-owned-current.trx')
            if($report.LastWriteTimeUtc -lt $hederaStarted){throw 'Hedera full report is stale.'}
            [xml]$evidence=Get-Content -LiteralPath $report.FullName
            $counts=$evidence.TestRun.ResultSummary.Counters
            if([int]$counts.total -ne 137 -or [int]$counts.passed -ne 137 -or [int]$counts.notExecuted -ne 0){throw 'Expected137 passing Hedera unit cases without skips.'}
        }
    }elseif($SuiMintOwner -or $ReconcileControlledMintOwner){
        $mintProject=Join-Path $taskRoot 'Providers/Blockchain/TestProjects/NextGenSoftware.OASIS.API.Providers.SuiOASIS.IntegrationTests/NextGenSoftware.OASIS.API.Providers.SuiOASIS.IntegrationTests.csproj'
        $mintFilter=if($ReconcileControlledMintOwner){'FullyQualifiedName~ReconcileControlledMintOwnerFixture'}else{'FullyQualifiedName~PublicMintRetainsOriginalAttemptInReusableMongo'}
        $mintReport=if($ReconcileControlledMintOwner){'trx;LogFileName=controlled-mint-owner-reconciliation-current.trx'}else{'trx;LogFileName=sui-mint-owner-real-mongo-current.trx'}
        & dotnet test $mintProject --no-restore /m:2 /nr:false /p:UseSharedCompilation=false /p:BuildProjectReferences=false --filter $mintFilter --logger $mintReport --results-directory $work -v:q
    }else{
        $usageStarted=[DateTime]::UtcNow
        & dotnet test $project --no-build --no-restore --filter 'FullyQualifiedName!~OwnedReplicaInitializationTests' --logger 'trx;LogFileName=usage-real-replica-current.trx' --results-directory $work -v:q
        if($LASTEXITCODE -ne 0){throw 'Usage recovery regression failed.'}
        $report=Get-Item -LiteralPath (Join-Path $work 'usage-real-replica-current.trx')
        if($report.LastWriteTimeUtc -lt $usageStarted){throw 'Usage recovery report is stale.'}
        [xml]$evidence=Get-Content -LiteralPath $report.FullName -Raw
        $counts=$evidence.TestRun.ResultSummary.Counters
        if([int]$counts.total -ne 310 -or [int]$counts.passed -ne 310 -or [int]$counts.notExecuted -ne 0){throw 'Expected310 passing usage recovery cases without skips.'}
    }
    if($LASTEXITCODE -ne 0){throw 'Usage recovery regression failed.'}
}finally{
    try{
        if($owned){
            if(!$owned.HasExited){$owned.Kill();if(!$owned.WaitForExit(30000)){throw 'Owned Mongo did not terminate within30seconds.'}}
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
