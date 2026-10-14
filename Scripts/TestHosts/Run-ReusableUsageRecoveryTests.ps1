#Requires -Version 7.0
param([switch]$SuiMintOwner,[switch]$ReconcileControlledMintOwner,[switch]$HederaPreparedOwners,[switch]$MongoProjection,[switch]$MongoTransactions,[switch]$MongoV2,[switch]$SuiPreparedLedger,[switch]$CosmosPreparedOwners)
$ErrorActionPreference='Stop'
if($CosmosPreparedOwners -and ($SuiMintOwner -or $ReconcileControlledMintOwner -or $HederaPreparedOwners -or $MongoProjection -or $MongoTransactions -or $MongoV2 -or $SuiPreparedLedger)){throw 'Choose one provider verification suite.'}
if($SuiPreparedLedger -and ($SuiMintOwner -or $ReconcileControlledMintOwner -or $HederaPreparedOwners -or $MongoProjection -or $MongoTransactions -or $MongoV2)){throw 'Choose one provider verification suite.'}
if($MongoV2 -and ($SuiMintOwner -or $ReconcileControlledMintOwner -or $HederaPreparedOwners -or $MongoProjection -or $MongoTransactions)){throw 'Choose one provider verification suite.'}
if(($MongoProjection -or $MongoTransactions) -and ($SuiMintOwner -or $ReconcileControlledMintOwner -or $HederaPreparedOwners)){throw 'Choose one provider verification suite.'}
if($MongoProjection -and $MongoTransactions){throw 'Choose projection-only or transaction verification.'}
if($SuiMintOwner -and $ReconcileControlledMintOwner){throw 'Choose verification or explicit controlled-fixture reconciliation, not both.'}
if($HederaPreparedOwners -and ($SuiMintOwner -or $ReconcileControlledMintOwner)){throw 'Choose one provider owner suite.'}
$taskRoot=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
$work=Join-Path $taskRoot 'TestResults/SubscriptionUsageCurrent'
$data=Join-Path $work 'mongo-data'
$runtime='C:\Program Files\MongoDB\Server\8.3\bin\mongod.exe'
if(!(Test-Path -LiteralPath $runtime -PathType Leaf)){throw 'Existing installed Mongo runtime is required; no download attempted.'}
if((& $runtime --version | Select-Object -First 1) -ne 'db version v8.3.11'){throw 'Retained runtime version differs; inspect before reuse.'}
New-Item -ItemType Directory -Force -Path $work | Out-Null
$lease=[IO.File]::Open((Join-Path $work 'environment.lock'),[IO.FileMode]::OpenOrCreate,[IO.FileAccess]::ReadWrite,[IO.FileShare]::None)
$owned=$null
$prior=@{}
foreach($name in 'OASIS_OWNED_RECOVERY_REPLICA_URI','SUBSCRIPTION_TEST_MONGODB_URI','OASIS_HEDERA_SDK_BRIDGE','OASIS_MONGO_REPLICA_SET_CONNECTION'){$prior[$name]=[Environment]::GetEnvironmentVariable($name)}
try{
    if(Get-NetTCPConnection -State Listen -LocalPort 27029 -ErrorAction SilentlyContinue){throw 'Owned port already in use; no process will be stopped.'}
    if([IO.Path]::GetFullPath($data) -ne (Join-Path $taskRoot 'TestResults/SubscriptionUsageCurrent/mongo-data')){throw 'Unexpected owned data path.'}
    New-Item -ItemType Directory -Force -Path $data | Out-Null
    $mongoArguments=@('--dbpath',$data,'--bind_ip','127.0.0.1','--port','27029','--replSet','oasisRecoveryRs','--wiredTigerCacheSizeGB','0.25','--oplogSize','32','--logpath',(Join-Path $work 'mongo-current.log'),'--logappend')
    if($MongoV2){$mongoArguments+=@('--setParameter','enableTestCommands=1')}
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
    & dotnet test $project --no-restore /m:2 /nr:false /p:UseSharedCompilation=false /p:BuildProjectReferences=false --filter FullyQualifiedName~OwnedReplicaInitializationTests --logger 'trx;LogFileName=owned-replica-init-current.trx' --results-directory $work -v:q
    if($LASTEXITCODE -ne 0){throw 'Owned replica initialization failed.'}
    if($CosmosPreparedOwners){
        $cosmosProject=Join-Path $taskRoot 'Providers/Blockchain/TestProjects/NextGenSoftware.OASIS.API.Providers.CosmosBlockChainOASIS.UnitTests/NextGenSoftware.OASIS.API.Providers.CosmosBlockChainOASIS.UnitTests.csproj'
        & dotnet test $cosmosProject --no-restore /m:2 /nr:false /p:UseSharedCompilation=false /p:BuildProjectReferences=false --filter 'FullyQualifiedName~RealOfflineSignedDocumentVerifiesThroughCSharp' --logger 'trx;LogFileName=cosmos-signed-owner-mongo-current.trx' --results-directory $work -v:q
    }elseif($SuiPreparedLedger){
        $suiProject=Join-Path $taskRoot 'Providers/Blockchain/TestProjects/NextGenSoftware.OASIS.API.Providers.SuiOASIS.IntegrationTests/NextGenSoftware.OASIS.API.Providers.SuiOASIS.IntegrationTests.csproj'
        & dotnet test $suiProject --no-build --no-restore --filter 'FullyQualifiedName~PreparedLocalWriteRetainsOriginalSignedEvidenceAndConfirmsLedger' --logger 'trx;LogFileName=sui-prepared-ledger-current.trx' --results-directory (Join-Path $taskRoot 'TestResults/SuiCurrent') -v:q
    }elseif($MongoV2){
        $routingProject=Join-Path $taskRoot 'OASIS Architecture/NextGenSoftware.OASIS.API.Core.HyperDrive.IntegrationTests/NextGenSoftware.OASIS.API.Core.HyperDrive.IntegrationTests.csproj'
        & dotnet test $routingProject --no-build --no-restore --filter 'FullyQualifiedName~IntegrationTests.MongoSqliteRealProviderRoutingTests.' --logger 'trx;LogFileName=mongo-sqlite-v2-current.trx' --results-directory $work -v:q
    }elseif($MongoProjection -or $MongoTransactions){
        $env:OASIS_MONGO_REPLICA_SET_CONNECTION=$env:SUBSCRIPTION_TEST_MONGODB_URI
        $mongoProject=Join-Path $taskRoot 'Providers/Storage/NextGenSoftware.OASIS.API.Providers.MongoOASIS.IntegrationTests/NextGenSoftware.OASIS.API.Providers.MongoOASIS.IntegrationTests.csproj'
        $mongoFilter=if($MongoTransactions){'Category!=ExternalPrimaryTermination&FullyQualifiedName!~AcknowledgedOperationRemainsIdempotentAcrossPrimaryElection'}else{'FullyQualifiedName~HostedMongoProjectionContractTests'}
        $mongoReport=if($MongoTransactions){'trx;LogFileName=mongo-single-node-transactions-current.trx'}else{'trx;LogFileName=mongo-projection-current.trx'}
        & dotnet test $mongoProject --no-restore /m:2 /nr:false /p:UseSharedCompilation=false /p:BuildProjectReferences=false --filter $mongoFilter --logger $mongoReport --results-directory $work -v:q
    }elseif($HederaPreparedOwners){
        $env:OASIS_HEDERA_SDK_BRIDGE=Join-Path $taskRoot 'Providers/Blockchain/NextGenSoftware.OASIS.API.Providers.HashgraphOASIS/SdkBridge/bridge.mjs'
        $hederaProject=Join-Path $taskRoot 'Providers/Blockchain/TestProjects/NextGenSoftware.OASIS.API.Providers.HashgraphOASIS.UnitTests/NextGenSoftware.OASIS.API.Providers.HashgraphOASIS.UnitTests.csproj'
        & dotnet test $hederaProject --no-restore /m:2 /nr:false /p:UseSharedCompilation=false /p:BuildProjectReferences=false --filter FullyQualifiedName~SignedEscrowStagesPersistExactEvidenceWithDistinctCorrelations --logger 'trx;LogFileName=hedera-real-signed-owner-mongo-current.trx' --results-directory $work -v:q
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
