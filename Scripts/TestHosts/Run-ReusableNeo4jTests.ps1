#Requires -Version 7.0
param([switch]$CqlV1Demo,[switch]$CqlV1Web4Reconcile,[switch]$CqlV1Web4,[switch]$ReconcileInterruptedEnumFixture,[switch]$CosmicCqlHostingRegression,[switch]$ReconcileIntegerFieldFixture,[switch]$ReconcileCqlFixture,[switch]$CosmicCqlRegression,[switch]$CosmicCql,[switch]$CosmicDeleteOwnership,[switch]$ReconcileIdentityRollback,[switch]$CosmicIdentitySave,[switch]$ReconcileDeltaCreationFixtures,[switch]$ReconcileTypedSearchFixtures,[switch]$CosmicTypedQueries,[switch]$ReconcileReadFrontierFixture,[switch]$CosmicReadDepth,[switch]$ReconcileCredentialHistoryFixture,[switch]$CosmicConfiguredHosting,[switch]$CosmicOutboxHosting,[switch]$CosmicMongoOutboxReconcile,[switch]$CosmicMongoOutboxDelivery,[switch]$ReconcileTimeoutFixtures,[switch]$CosmicOutboxDelivery,[switch]$CosmicPrimaryOutbox,[switch]$CosmicSchema,[switch]$CosmicConditional,[switch]$CosmicParentAssignment,[switch]$CosmicOrdinaryOutcomes,[switch]$CosmicReplica)
if(($CqlV1Demo -or $CqlV1Web4Reconcile -or $CqlV1Web4 -or $ReconcileInterruptedEnumFixture -or $CosmicCqlHostingRegression -or $ReconcileIntegerFieldFixture -or $ReconcileCqlFixture -or $CosmicCqlRegression -or $CosmicCql -or $CosmicDeleteOwnership -or $ReconcileIdentityRollback -or $CosmicIdentitySave -or $ReconcileDeltaCreationFixtures -or $ReconcileTypedSearchFixtures -or $CosmicTypedQueries -or $ReconcileReadFrontierFixture -or $CosmicReadDepth -or $ReconcileCredentialHistoryFixture -or $CosmicConfiguredHosting -or $CosmicOutboxHosting -or $CosmicMongoOutboxReconcile -or $CosmicMongoOutboxDelivery -or $ReconcileTimeoutFixtures -or $CosmicOutboxDelivery -or $CosmicPrimaryOutbox -or $CosmicSchema -or $CosmicConditional -or $CosmicParentAssignment -or $CosmicOrdinaryOutcomes -or $CosmicReplica) -and $PSBoundParameters.Count -ne 1){throw 'Choose one exclusive Neo4j verification mode.'}
$ErrorActionPreference='Stop'
$taskRoot=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
$work=Join-Path $taskRoot 'TestResults/Neo4jCurrent'
New-Item -ItemType Directory -Force -Path $work | Out-Null
$lease=[IO.File]::Open((Join-Path $work 'environment.lock'),[IO.FileMode]::OpenOrCreate,[IO.FileAccess]::ReadWrite,[IO.FileShare]::None)
$started=$false
$prior=$env:OASIS_OWNED_NEO4J_URI
$priorWork=$env:OASIS_OWNED_NEO4J_WORK
$freeBefore=(Get-PSDrive C).Free
try {
    $state=(& wsl -d Ubuntu -u root -- podman inspect oasis-neo4j --format '{{.State.Status}} {{.Image}}') -join ''
    if($LASTEXITCODE -or $state -ne 'created 6e47503356fe6b0e8f3ba48afc714ed95475d0c32ac97c62b30737af2e8846bd' -and $state -ne 'exited 6e47503356fe6b0e8f3ba48afc714ed95475d0c32ac97c62b30737af2e8846bd'){
        throw 'Exact retained stopped Neo4j container required; no download/replacement attempted.'
    }
    if(Get-NetTCPConnection -State Listen -LocalPort 17687 -ErrorAction SilentlyContinue){throw 'Owned Bolt port is already occupied.'}
    if(Get-NetTCPConnection -State Listen -LocalPort 17688 -ErrorAction SilentlyContinue){throw 'Owned simulated outage port is occupied.'}
    $provider=Join-Path $taskRoot 'Providers/Storage/NextGenSoftware.OASIS.API.Providers.Neo4jOASIS/NextGenSoftware.OASIS.API.Providers.Neo4jOASIS.csproj'
    $project=Join-Path $taskRoot 'Providers/Storage/TestProjects/NextGenSoftware.OASIS.API.Providers.Neo4jOASIS.IntegrationTests/NextGenSoftware.OASIS.API.Providers.Neo4jOASIS.IntegrationTests.csproj'
    $sqlite=Join-Path $taskRoot 'Providers/Storage/NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS/NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS.csproj'
    if($CqlV1Demo -or $CqlV1Web4Reconcile -or $CqlV1Web4 -or $CosmicMongoOutboxDelivery -or $CosmicMongoOutboxReconcile){
        if($env:SUBSCRIPTION_TEST_MONGODB_URI -ne 'mongodb://127.0.0.1:27029/?replicaSet=oasisRecoveryRs&serverSelectionTimeoutMS=3000'){throw 'Use the exclusive retained Mongo runner mode.'}
    }
    $shared=Join-Path $taskRoot 'Providers/Shared/NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage/NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage.csproj'
    $mongo=Join-Path $taskRoot 'Providers/Storage/NextGenSoftware.OASIS.API.Providers.MongoOASIS/NextGenSoftware.OASIS.API.Providers.MongoDBOASIS.csproj'
    # Private implementation changes can leave reference assemblies unchanged. Rebuild direct consumers so copy-local cannot retain stale provider copies.
    foreach($path in $shared,$sqlite,$mongo,$provider,$project){
        & dotnet build $path --no-restore --no-incremental /p:BuildProjectReferences=false /m:2 /nr:false /p:UseSharedCompilation=false -v:q -clp:ErrorsOnly
        if($LASTEXITCODE){throw 'Neo4j producer/consumer build failed.'}
    }
    $sharedName='NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage.dll'
    $expectedShared=(Get-FileHash -LiteralPath (Join-Path (Split-Path $shared) "bin/Debug/net10.0/$sharedName") -Algorithm SHA256).Hash
    foreach($consumer in $sqlite,$mongo,$provider,$project){
        $copy=Join-Path (Split-Path $consumer) "bin/Debug/net10.0/$sharedName"
        if((Get-FileHash -LiteralPath $copy -Algorithm SHA256).Hash -ne $expectedShared){throw "Stale Shared producer in native consumer: $consumer"}
    }
    Write-Output "Verified native Shared producer SHA256=$expectedShared"
    $coreName='NextGenSoftware.OASIS.API.Core.dll'
    $expectedCore=(Get-FileHash -LiteralPath (Join-Path $taskRoot "OASIS Architecture/NextGenSoftware.OASIS.API.Core/bin/Debug/net10.0/$coreName") -Algorithm SHA256).Hash
    foreach($consumer in $shared,$sqlite,$mongo,$provider,$project){
        if((Get-FileHash -LiteralPath (Join-Path (Split-Path $consumer) "bin/Debug/net10.0/$coreName") -Algorithm SHA256).Hash -ne $expectedCore){throw "Stale Core producer in native consumer: $consumer"}
    }
    Write-Output "Verified native Core producer SHA256=$expectedCore"
    & wsl -d Ubuntu -u root -- podman start oasis-neo4j
    if($LASTEXITCODE){throw 'Retained Neo4j service failed to start.'}
    $started=$true
    $deadline=[DateTime]::UtcNow.AddSeconds(60)
    do {
        & wsl -d Ubuntu -u root -- podman exec oasis-neo4j cypher-shell -u neo4j -p oasis-local-test-only 'RETURN 1;' 2>$null | Out-Null
        $ready=$LASTEXITCODE -eq 0
        if(!$ready){Start-Sleep -Milliseconds 500}
    } while(!$ready -and [DateTime]::UtcNow -lt $deadline)
    if(!$ready){throw 'Owned Neo4j did not become ready; no test submitted.'}
    $env:OASIS_OWNED_NEO4J_URI='bolt://127.0.0.1:17687'
    $env:OASIS_OWNED_NEO4J_WORK=$work
    $testStarted=[DateTime]::UtcNow
    $filter=if($CqlV1Demo){'FullyQualifiedName~SignedHttpQueriesUseNativeVisibilityPagingAndDurableReceiptsOnAllThreeProviders'}elseif($CqlV1Web4Reconcile){'FullyQualifiedName~ReviewedScalarAdmissionFixtureVerifiesSettledNoExecutionBeforeCleanup'}elseif($CqlV1Web4){'FullyQualifiedName~CqlV1Web4NativeTests|FullyQualifiedName~Neo4jNativeCqlTests|FullyQualifiedName~NativeCosmicConditionalGraphReceiptsAndRaces|FullyQualifiedName~NativeCosmicOrdinaryWriteOwnsCommitRollbackAndCleanup|FullyQualifiedName~NativeCosmicDeleteOwnership'}elseif($ReconcileInterruptedEnumFixture){'FullyQualifiedName~ReviewedInterruptedEnumFixtureIsUnchangedBeforeExactCleanup'}elseif($CosmicCqlHostingRegression){'FullyQualifiedName~Neo4jNativeCqlTests|FullyQualifiedName~NativeCosmicConditionalGraphReceiptsAndRaces|FullyQualifiedName~NativeCosmicOrdinaryWriteOwnsCommitRollbackAndCleanup|FullyQualifiedName~NativeCosmicDeleteOwnership|FullyQualifiedName~NativeCosmicConfiguredHost'}elseif($ReconcileIntegerFieldFixture){'FullyQualifiedName~ReviewedIntegerFieldCompilerFailureFixtureIsUnchangedBeforeExactCleanup'}elseif($ReconcileCqlFixture){'FullyQualifiedName~ReviewedGuidEmptyCompilerFailureFixtureIsUnchangedBeforeExactCleanup'}elseif($CosmicCqlRegression){'FullyQualifiedName~Neo4jNativeCqlTests|FullyQualifiedName~NativeCosmicConditionalGraphReceiptsAndRaces|FullyQualifiedName~NativeCosmicOrdinaryWriteOwnsCommitRollbackAndCleanup|FullyQualifiedName~NativeCosmicDeleteOwnership'}elseif($CosmicCql){'FullyQualifiedName~Neo4jNativeCqlTests'}elseif($CosmicDeleteOwnership){'FullyQualifiedName~NativeCosmicDeleteOwnership'}elseif($ReconcileIdentityRollback){'FullyQualifiedName~Neo4jIdentityRollbackFixtureReconciliation'}elseif($CosmicIdentitySave){'FullyQualifiedName~NativeCosmicIdentitySave'}elseif($ReconcileDeltaCreationFixtures){'FullyQualifiedName~Neo4jDeltaFixtureReconciliation'}elseif($ReconcileTypedSearchFixtures){'FullyQualifiedName~Neo4jSearchFixtureReconciliation'}elseif($CosmicTypedQueries){'FullyQualifiedName~NativeCosmicTypedQueryRolesAndRootOnlyReceipts'}elseif($ReconcileReadFrontierFixture){'FullyQualifiedName~Neo4jReadFrontierFixtureReconciliation'}elseif($CosmicReadDepth){'FullyQualifiedName~NativeCosmicReadDepthHonorsFullRangeAndHistoricalFrontiers'}elseif($ReconcileCredentialHistoryFixture){'FullyQualifiedName~Neo4jCredentialFixtureReconciliation'}elseif($CosmicConfiguredHosting){'FullyQualifiedName~NativeCosmicConfiguredHost'}elseif($CosmicOutboxHosting){'FullyQualifiedName~NativeCosmicOutboxHosting'}elseif($CosmicMongoOutboxReconcile){'FullyQualifiedName~NativeCosmicMongoNeoReviewedSoftFixture'}elseif($CosmicMongoOutboxDelivery){'FullyQualifiedName~NativeCosmicMongoNeoOutboxWorkerPreservesGraphLineage'}elseif($CosmicOutboxDelivery){'FullyQualifiedName~NativeCosmicOutboxDelivery'}elseif($CosmicPrimaryOutbox){'FullyQualifiedName~NativeCosmicPrimaryOutbox'}elseif($CosmicReplica){'FullyQualifiedName~NativeCosmicCommittedReplica'}elseif($CosmicOrdinaryOutcomes){'FullyQualifiedName~NativeCosmicOrdinaryWriteOwnsCommitRollbackAndCleanup'}elseif($CosmicParentAssignment){'FullyQualifiedName~NativeCosmicSelectedChildOwnsParentAssignment'}elseif($CosmicConditional){'FullyQualifiedName~NativeCosmicConditionalGraphReceiptsAndRaces'}elseif($CosmicSchema){'FullyQualifiedName~NativeCosmicSchemaUniqueness'}elseif($ReconcileTimeoutFixtures){'FullyQualifiedName~Neo4jTimeoutFixtureReconciliation'}else{'FullyQualifiedName~Neo4jNativeAvatarTests'}
    $reportName=if($CqlV1Demo){'neo4j-cql-v1-demo-current.trx'}elseif($CqlV1Web4Reconcile){'neo4j-cql-v1-web4-reconcile-current.trx'}elseif($CqlV1Web4){'neo4j-cql-v1-web4-current.trx'}elseif($ReconcileInterruptedEnumFixture){'neo4j-enum-interrupted-reviewed-fixture.trx'}elseif($CosmicCqlHostingRegression){'neo4j-cql-hosting-regression-current.trx'}elseif($ReconcileIntegerFieldFixture){'neo4j-integer-field-reviewed-fixture.trx'}elseif($ReconcileCqlFixture){'neo4j-cql-reviewed-fixture.trx'}elseif($CosmicCqlRegression){'neo4j-cql-regression-current.trx'}elseif($CosmicCql){'neo4j-cql-native-current.trx'}elseif($CosmicDeleteOwnership){'neo-delete-ownership-current.trx'}elseif($ReconcileIdentityRollback){'neo-identity-reviewed-rollback-current.trx'}elseif($CosmicIdentitySave){'neo-identity-save-current.trx'}elseif($ReconcileDeltaCreationFixtures){'neo-delta-reviewed-creation-current.trx'}elseif($ReconcileTypedSearchFixtures){'neo4j-reviewed-typed-search-current.trx'}elseif($CosmicTypedQueries){'neo4j-typed-queries-current.trx'}elseif($ReconcileReadFrontierFixture){'neo4j-reviewed-read-frontier-current.trx'}elseif($CosmicReadDepth){'neo4j-read-depth-current.trx'}elseif($ReconcileCredentialHistoryFixture){'neo4j-reviewed-credential-fixture-current.trx'}elseif($CosmicConfiguredHosting){'neo4j-cosmic-configured-host-current.trx'}elseif($CosmicOutboxHosting){'neo4j-cosmic-outbox-hosting-current.trx'}elseif($CosmicMongoOutboxReconcile){'neo4j-mongo-reviewed-soft-reconciliation.trx'}elseif($CosmicMongoOutboxDelivery){'neo4j-mongo-outbox-delivery-current.trx'}elseif($CosmicOutboxDelivery){'neo4j-cosmic-outbox-delivery-current.trx'}elseif($CosmicPrimaryOutbox){'neo4j-cosmic-primary-outbox-current.trx'}elseif($CosmicReplica){'neo4j-cosmic-replica-current.trx'}elseif($CosmicOrdinaryOutcomes){'neo4j-cosmic-ordinary-outcomes-current.trx'}elseif($CosmicParentAssignment){'neo4j-cosmic-parent-assignment-current.trx'}elseif($CosmicConditional){'neo4j-cosmic-conditional-current.trx'}elseif($CosmicSchema){'neo4j-cosmic-schema-current.trx'}elseif($ReconcileTimeoutFixtures){'neo4j-timeout-reconciliation-current.trx'}else{'neo4j-native-current.trx'}
    & dotnet test $project --no-build --no-restore /m:2 /nr:false --filter $filter --logger "trx;LogFileName=$reportName" --results-directory $work -v:q
    if($LASTEXITCODE){throw 'Native Neo4j test failed; preserve database for inspection.'}
    $report=Get-Item -LiteralPath (Join-Path $work $reportName)
    if($report.LastWriteTimeUtc -lt $testStarted){throw 'Native Neo4j report is stale.'}
    [xml]$evidence=Get-Content -LiteralPath $report.FullName
    $counts=$evidence.SelectSingleNode("//*[local-name()='Counters']")
    $expectedCount=if($CqlV1Demo){1}elseif($CqlV1Web4Reconcile){1}elseif($CqlV1Web4){129}elseif($ReconcileInterruptedEnumFixture){1}elseif($CosmicCqlHostingRegression){130}elseif($ReconcileIntegerFieldFixture){1}elseif($ReconcileCqlFixture){1}elseif($CosmicCqlRegression){128}elseif($CosmicCql){65}elseif($CosmicDeleteOwnership){34}elseif($ReconcileIdentityRollback){1}elseif($CosmicIdentitySave){20}elseif($ReconcileDeltaCreationFixtures){1}elseif($ReconcileTypedSearchFixtures){1}elseif($CosmicTypedQueries){12}elseif($ReconcileReadFrontierFixture){1}elseif($CosmicReadDepth){8}elseif($ReconcileCredentialHistoryFixture){1}elseif($CosmicConfiguredHosting){2}elseif($CosmicOutboxHosting){2}elseif($CosmicMongoOutboxReconcile){1}elseif($CosmicMongoOutboxDelivery){4}elseif($CosmicOutboxDelivery){6}elseif($CosmicPrimaryOutbox){2}elseif($CosmicReplica){6}elseif($CosmicOrdinaryOutcomes){16}elseif($CosmicParentAssignment){8}elseif($CosmicConditional){13}elseif($CosmicSchema){7}elseif($ReconcileTimeoutFixtures){1}else{265}
    if(!$counts -or [int]$counts.total -ne $expectedCount -or [int]$counts.passed -ne $expectedCount -or [int]$counts.notExecuted -ne 0){throw 'Exact fresh native case count, including timeout classification, with no skips required.'}
    if($CqlV1Demo -or $CqlV1Web4Reconcile -or $CqlV1Web4){
        $fixture=Join-Path $work 'cql-v1-web4-fixture.json'
        $owned=Get-Content -LiteralPath $fixture -Raw | ConvertFrom-Json
        $expected=[IO.Path]::GetFullPath((Join-Path $work 'cql-v1-web4.sqlite'))
        if($owned.stage -ne 'successful-awaiting-runner-cleanup' -or [IO.Path]::GetFullPath($owned.sqlite) -ne $expected){throw 'Fixture cleanup ownership/stage mismatch.'}
        $removed=@()
        foreach($target in @($expected,($expected+'-wal'),($expected+'-shm'))){
            if(Test-Path -LiteralPath $target){
                $item=Get-Item -LiteralPath $target
                if($item.PSIsContainer -or $item.LinkType){throw 'Refuse unexpected fixture filesystem resource.'}
                $removed+=@{path=$item.FullName;bytes=$item.Length}
                Remove-Item -LiteralPath $item.FullName
            }
        }
        @{removed=$removed;manifestSha256=(Get-FileHash -LiteralPath $fixture).Hash;testProcessExited=$true} | ConvertTo-Json -Depth 5 | Set-Content -LiteralPath (Join-Path $work 'cql-v1-web4-cleanup.json')
        Remove-Item -LiteralPath $fixture
    }
    Write-Output "Free bytes before=$freeBefore after=$((Get-PSDrive C).Free)"
} finally {
    try {
        if($started){
            & wsl -d Ubuntu -u root -- podman stop --time 20 oasis-neo4j
            if($LASTEXITCODE){throw 'Owned Neo4j shutdown failed; do not recreate or delete its data.'}
        }
    } finally {
        [Environment]::SetEnvironmentVariable('OASIS_OWNED_NEO4J_URI',$prior)
        [Environment]::SetEnvironmentVariable('OASIS_OWNED_NEO4J_WORK',$priorWork)
        $lease.Dispose()
    }
}
