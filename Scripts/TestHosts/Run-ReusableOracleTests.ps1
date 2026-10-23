param(
    [switch]$ReconcileKarmaProjections,[switch]$MissingDeletion,[switch]$ReconcileKarmaAssertion,[switch]$KarmaManager,[switch]$LeastPrivilege,[switch]$V2Deletion,[switch]$DeleteAliases,[switch]$ReadAcknowledgements,[switch]$TableDurability,[switch]$ExistingSchema,[switch]$WriteRejections,[switch]$IdentityCapacity,[switch]$UpgradeIdentityCharacters,
    [switch]$RootParent,[switch]$HolonProjection,[switch]$IdentityRaces,
    [switch]$DetailDeletionResults,[switch]$AvatarDeletionResults,[switch]$SaveAcknowledgements,
    [switch]$AvatarProjection,[switch]$LifecycleGuards,[switch]$EncryptedProcess,
    [switch]$EncryptedReplay,[switch]$PreparationClaims,[switch]$ReconcileOriginal,
    [switch]$ReconcileAmbiguity,[switch]$ReconcileHistoricalExport,
    [switch]$ReconcileCorruptAvatar,[switch]$ReconcileProjectionAssertions
)
$selectedModes=@($ReconcileKarmaProjections,$MissingDeletion,$ReconcileKarmaAssertion,$KarmaManager,$LeastPrivilege,$V2Deletion,$DeleteAliases,$ReadAcknowledgements,$TableDurability,$ExistingSchema,$WriteRejections,$IdentityCapacity,$UpgradeIdentityCharacters,$RootParent,$HolonProjection,$IdentityRaces,
    $DetailDeletionResults,$AvatarDeletionResults,$SaveAcknowledgements,$AvatarProjection,$LifecycleGuards,
    $EncryptedProcess,$EncryptedReplay,$PreparationClaims,$ReconcileOriginal,$ReconcileAmbiguity,
    $ReconcileHistoricalExport,$ReconcileCorruptAvatar,$ReconcileProjectionAssertions).Where({$_.IsPresent}).Count
if($selectedModes -gt 1){throw 'Choose one original reconciliation mode.'}
$ErrorActionPreference='Stop'
$root=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
$work=Join-Path $root 'TestResults/OracleCurrent'
if(!(Test-Path -LiteralPath $work)){throw 'Retained Oracle work directory required; do not recreate an environment.'}
$lease=[IO.File]::Open((Join-Path $work 'environment.lock'),[IO.FileMode]::OpenOrCreate,[IO.FileAccess]::ReadWrite,[IO.FileShare]::None)
$previousConnection=$env:ORACLEDBOASIS_CONNECTIONSTRING;$previousWork=$env:OASIS_ORACLE_TEST_WORK;$previousResume=$env:OASIS_ORACLE_RESUME_CANONICAL
$previousAmbiguity=$env:OASIS_ORACLE_RECONCILE_AMBIGUITY
$previousSchemaUpgrade=$env:OASIS_ORACLE_ALLOW_TEST_IDENTITY_SCHEMA_UPGRADE
$startedHere=$false;$passed=$false
try{
    $pending=@(Get-ChildItem -LiteralPath $work -Filter '*.pending.json')
    if($ReconcileKarmaAssertion){if($pending.Count -ne 1 -or $pending[0].Name -ne 'v2-karma-manager.pending.json'){throw 'Exactly the original karma assertion journal required.'}}
    elseif($ReconcileOriginal){if($pending.Count -ne 1 -or $pending[0].Name -ne 'canonical-records.pending.json'){throw 'Exactly the original canonical journal required.'}}
    elseif($ReconcileAmbiguity){if($pending.Count -ne 1 -or $pending[0].Name -ne 'identity-ambiguity.pending.json'){throw 'Exactly the original duplicate identity journal required.'}}
    elseif($ReconcileHistoricalExport){if($pending.Count -ne 1 -or $pending[0].Name -ne 'canonical-records.pending.json'){throw 'Exactly the original historical export journal required.'}}
    elseif($ReconcileCorruptAvatar){if($pending.Count -ne 1 -or $pending[0].Name -ne 'create-race-0.pending.json'){throw 'Exactly the original corrupt avatar journal required.'}}
    elseif($ReconcileProjectionAssertions){if($pending.Count -ne 6){throw 'Exactly the six original projection-assertion journals required; test validates exact names and evidence.'}}
    elseif($pending.Count){throw 'Reconcile original Oracle fixture before another write.'}
    $image=(& wsl -d Ubuntu -u root -- podman inspect --format '{{.Image}}' oasis-oracle-free).Trim()
    if($LASTEXITCODE -ne 0 -or $image -ne '358b32468eddc329751c28007a535cbc661f1090ab5d8a9f02267cf44ef1e3b9'){throw 'Approved retained Oracle image/container required; no pull/create.'}
    $bindings=& wsl -d Ubuntu -u root -- podman inspect --format '{{json .HostConfig.PortBindings}}' oasis-oracle-free | ConvertFrom-Json
    if($LASTEXITCODE -ne 0 -or @($bindings.'1521/tcp').Count -ne 1 -or $bindings.'1521/tcp'[0].HostIp -ne '127.0.0.1' -or $bindings.'1521/tcp'[0].HostPort -ne '11521'){throw 'Expected configured loopback Oracle port mapping required.'}
    $mounts=@(& wsl -d Ubuntu -u root -- podman inspect --format '{{json .Mounts}}' oasis-oracle-free | ConvertFrom-Json)
    if($LASTEXITCODE -ne 0 -or $mounts.Count -ne 1 -or $mounts[0].Type -ne 'volume' -or $mounts[0].Name -ne 'oasis-oracle-data' -or $mounts[0].Destination -ne '/opt/oracle/oradata'){throw 'Exact retained Oracle data volume required; no replacement.'}
    foreach($project in @('Providers/Shared/NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage/NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage.csproj','Providers/Storage/NextGenSoftware.OASIS.API.Providers.OracleDBOASIS/NextGenSoftware.OASIS.API.Providers.OracleDBOASIS.csproj','Providers/Storage/TestProjects/NextGenSoftware.OASIS.API.Providers.OracleDBOASIS.IntegrationTests/NextGenSoftware.OASIS.API.Providers.OracleDBOASIS.IntegrationTests.csproj')){
        & dotnet build (Join-Path $root $project) --no-restore --no-incremental /p:BuildProjectReferences=false /m:2 /nr:false /p:UseSharedCompilation=false -v:q -clp:ErrorsOnly
        if($LASTEXITCODE -ne 0){throw 'Cached Oracle build failed.'}
    }
    & (Join-Path $PSScriptRoot 'Assert-ReusableProviderBuildIdentity.ps1') -RepositoryRoot $root -ConsumerOutput 'Providers/Storage/TestProjects/NextGenSoftware.OASIS.API.Providers.OracleDBOASIS.IntegrationTests/bin/Debug/net10.0'
    $state=(& wsl -d Ubuntu -u root -- podman inspect --format '{{.State.Status}}' oasis-oracle-free).Trim()
    if($state -eq 'exited'){
        & wsl -d Ubuntu -u root -- podman start oasis-oracle-free
        if($LASTEXITCODE -ne 0){throw 'Retained Oracle start failed.'};$startedHere=$true
    }elseif($state -ne 'running'){throw 'Unexpected retained Oracle lifecycle state; inspect without resetting.'}
    $deadline=[DateTime]::UtcNow.AddSeconds(50)
    do{
        $health=(& wsl -d Ubuntu -u root -- podman inspect --format '{{.State.Health.Status}}' oasis-oracle-free).Trim()
        if($LASTEXITCODE -ne 0){throw 'Oracle health inspection failed.'}
        if($health -eq 'healthy'){break}
        if($health -ne 'starting'){throw 'Retained Oracle is not healthy; preserve it for inspection.'}
        Start-Sleep -Milliseconds 500
    }while([DateTime]::UtcNow -lt $deadline)
    if($health -ne 'healthy'){throw 'Retained Oracle readiness timeout; no restart/reset.'}
    $secret=(& wsl -d Ubuntu -u root -- podman secret inspect --showsecret oasis-oracle-pwd | ConvertFrom-Json)[0].SecretData
    if([string]::IsNullOrWhiteSpace($secret)){throw 'Retained secret unavailable.'}
    $env:ORACLEDBOASIS_CONNECTIONSTRING="User Id=SYSTEM;Password=$secret;Data Source=127.0.0.1:11521/FREEPDB1;Connection Timeout=10;Pooling=false"
    $env:OASIS_ORACLE_TEST_WORK=$work;$env:OASIS_ORACLE_RESUME_CANONICAL=if($ReconcileOriginal){'1'}else{$null}
    $env:OASIS_ORACLE_RECONCILE_AMBIGUITY=if($ReconcileAmbiguity){'1'}else{$null}
    $env:OASIS_ORACLE_ALLOW_TEST_IDENTITY_SCHEMA_UPGRADE=if($UpgradeIdentityCharacters){'1'}else{$null}
    if($EncryptedProcess){
        $processStarted=[DateTime]::UtcNow
        $processProject=Join-Path $root 'Providers/Storage/TestProjects/NextGenSoftware.OASIS.API.Providers.OracleDBOASIS.IntegrationTests/NextGenSoftware.OASIS.API.Providers.OracleDBOASIS.IntegrationTests.csproj'
        foreach($phase in @('write','recover')){
            $phaseReport=Join-Path $work "oracle-encrypted-process-$phase.trx"
            if(Test-Path -LiteralPath $phaseReport){throw 'Preserve original process evidence before another run.'}
            $method=if($phase -eq 'write'){'PhaseOneCommitEncryptedOperation'}else{'PhaseTwoRecoverInDifferentProcess'}
            & dotnet test $processProject --no-build --no-restore --filter "FullyQualifiedName~OracleEncryptedProcessNativeTests.$method" --logger "trx;LogFileName=oracle-encrypted-process-$phase.trx" --results-directory $work /m:2 /nr:false
            if($LASTEXITCODE -ne 0){throw 'Oracle encrypted process verification failed; preserve original fixture, no replay/reset.'}
            $phaseFile=Get-Item -LiteralPath $phaseReport
            [xml]$phaseTrx=Get-Content -LiteralPath $phaseReport -Raw;$phaseCounts=$phaseTrx.TestRun.ResultSummary.Counters
            if($phaseFile.LastWriteTimeUtc -lt $processStarted -or [int]$phaseCounts.total -ne 2 -or [int]$phaseCounts.passed -ne 2 -or [int]$phaseCounts.notExecuted -ne 0){throw 'Fresh complete2/2 Oracle process phase required.'}
            if($phase -eq 'write' -and @(Get-ChildItem -LiteralPath $work -Filter 'encrypted-process-*.pending.json').Count -ne 2){throw 'Exactly two committed process intents required before recovery.'}
        }
        if(Get-ChildItem -LiteralPath $work -Filter '*.pending.json'){throw 'Original Oracle process fixtures remain unresolved.'}
        $passed=$true;return
    }
    $report=if($EncryptedReplay){'oracle-encrypted-replay-native-current.trx'}elseif($PreparationClaims){'oracle-preparation-native-current.trx'}elseif($ReconcileOriginal){'oracle-native-reconciliation-current.trx'}elseif($ReconcileAmbiguity){'oracle-ambiguity-reconciliation.trx'}elseif($ReconcileHistoricalExport){'oracle-history-export-reconciliation.trx'}elseif($ReconcileCorruptAvatar){'oracle-corrupt-avatar-reconciliation.trx'}elseif($ReconcileProjectionAssertions){'oracle-projection-assertion-reconciliation.trx'}else{'oracle-native-current.trx'}
    $filter=if($EncryptedReplay){'FullyQualifiedName~OracleEncryptedReplayNativeTests'}elseif($PreparationClaims){'FullyQualifiedName~OraclePreparationNativeTests'}elseif($ReconcileOriginal){'FullyQualifiedName~RealSdkCanonicalRecordsSelectorsAndMetadataPaths'}elseif($ReconcileAmbiguity){'FullyQualifiedName~DuplicateIdentityIsRejectedInsteadOfSelectingArbitraryAvatar'}elseif($ReconcileHistoricalExport){'FullyQualifiedName~ReconcileKnownHistoricalExportFixture'}elseif($ReconcileCorruptAvatar){'FullyQualifiedName~ReconcileKnownCorruptAvatarFixture'}elseif($ReconcileProjectionAssertions){'FullyQualifiedName~ReconcileKnownProjectionAssertionFixtures'}else{'FullyQualifiedName~OracleNativeAcceptanceTests&FullyQualifiedName!~Reconcile'}
    if($LifecycleGuards){$report='oracle-lifecycle-guard-native-current.trx';$filter='FullyQualifiedName~NativeLifecycleGuardsRejectStaleAndRelatedActiveSaves'}
    if($AvatarProjection){$report='oracle-avatar-projection-native-current.trx';$filter='FullyQualifiedName~NativeAvatarProjectionMismatchCannotBeHiddenOrRepaired'}
    if($SaveAcknowledgements){$report='oracle-save-ack-native-current.trx';$filter='FullyQualifiedName~NativeOrdinaryIdentitySaveCountsOnlyAcknowledgedCommit|FullyQualifiedName~AcknowledgedSaveCannotOverwriteChangedCallerContract'}
    if($AvatarDeletionResults){$report='oracle-avatar-delete-result-native-current.trx';$filter='FullyQualifiedName~NativeAvatarDeletionResultMetadataDefinesKnownOutcome'}
    if($DetailDeletionResults){$report='oracle-detail-delete-result-native-current.trx';$filter='FullyQualifiedName~NativeStandaloneDetailLifecyclePreservesAvatarAndReportsLostCommit'}
    if($IdentityRaces){$report='oracle-identity-races-native-current.trx';$filter='FullyQualifiedName~NativeIdentitySaveAndPairDeleteSerialize'}
    if($HolonProjection){$report='oracle-holon-projection-native-current.trx';$filter='FullyQualifiedName~NativeHolonProjectionCorruptionIsExplicitBeforeHistoryAndFiltering'}
    if($RootParent){$report='oracle-root-parent-native-current.trx';$filter='FullyQualifiedName~NativeRootParentSelectionAgreesAtLatestAndHistoricalVersions'}
    if($IdentityCapacity){$report='oracle-identity-character-native-current.trx';$filter='FullyQualifiedName~NativeIdentityDeclaredCharacterCapacityPreservesUnicode'}
    if($UpgradeIdentityCharacters){$report='oracle-identity-character-upgrade.trx';$filter='FullyQualifiedName~OracleIdentitySchemaUpgradeNativeTests'}
    if($WriteRejections){$report='oracle-write-rejections-native-current.trx';$filter='FullyQualifiedName~NativeIdentityColumnRejectionDoesNotBecomeUnknown|FullyQualifiedName~NativeCommitPhaseCapacityErrorRemainsUncertain'}
    if($ExistingSchema){$report='oracle-existing-schema-native-current.trx';$filter='FullyQualifiedName~NativeExistingSchemaActivationValidatesWithoutProvisioning'}
    if($TableDurability){$report='oracle-table-durability-native-current.trx';$filter='FullyQualifiedName~NativeTableDurabilityAdmissionUsesActualCatalogWithoutAlteringCanonicalTables'}
    if($ReadAcknowledgements){$report='oracle-read-ack-native-current.trx';$filter='FullyQualifiedName~NativeReadAcknowledgementsReportActualSuccessAndRootCounts'}
    if($DeleteAliases){$report='oracle-delete-alias-native-current.trx';$filter='FullyQualifiedName~NativeHolonDeleteAliasesCountAcknowledgedMutationOnly|FullyQualifiedName~NativeDetailDeleteInterfaceAliasesPreserveProfileAndOtherSelectors'}
    if($V2Deletion){$report='oracle-v2-delete-native-current.trx';$filter='FullyQualifiedName~NativeV2ManagerDeleteRoutesPreserveIndependentChildren'}
    if($LeastPrivilege){$report='oracle-restricted-schema-native-current.trx';$filter='FullyQualifiedName~NativeRestrictedSchemaActivatesAndStoresWithoutProvisioningPrivileges'}
    if($KarmaManager){$report='oracle-karma-manager-native-current.trx';$filter='FullyQualifiedName~NativeV2KarmaManagerPersistsLedgerAndDetailHistory'}
    if($ReconcileKarmaAssertion){$report='oracle-karma-history-shape-reconciliation.trx';$filter='FullyQualifiedName~ReconcileKnownKarmaHistoryAssertionFixture'}
    if($MissingDeletion){$report='oracle-missing-delete-native-current.trx';$filter='FullyQualifiedName~NativeMissingDeleteIsKnownAbsenceWithoutAcknowledgedMutation'}
    if($ReconcileKarmaProjections){$report='oracle-karma-projection-reconciliation.trx';$filter='FullyQualifiedName~ReconcileKnownKarmaLeaderboardProjectionFixtures'}
    if(Test-Path -LiteralPath (Join-Path $work $report)){throw 'Preserve the original native report before another run.'}
    $testStarted=[DateTime]::UtcNow
    & dotnet test (Join-Path $root 'Providers/Storage/TestProjects/NextGenSoftware.OASIS.API.Providers.OracleDBOASIS.IntegrationTests/NextGenSoftware.OASIS.API.Providers.OracleDBOASIS.IntegrationTests.csproj') --no-build --no-restore --filter $filter --logger "trx;LogFileName=$report" --results-directory $work /m:2 /nr:false
    if($LASTEXITCODE -ne 0 -or (Get-ChildItem -LiteralPath $work -Filter '*.pending.json')){throw 'Native Oracle verification incomplete; preserve originals, no cleanup/reset/replay.'}
    $reportFile=Get-Item -LiteralPath (Join-Path $work $report)
    [xml]$trx=Get-Content -LiteralPath $reportFile.FullName -Raw;$counts=$trx.TestRun.ResultSummary.Counters
    $expected=if($MissingDeletion){40}elseif($ReconcileKarmaAssertion -or $ReconcileKarmaProjections){1}elseif($KarmaManager -or $LeastPrivilege){2}elseif($V2Deletion){16}elseif($DeleteAliases){28}elseif($ReadAcknowledgements){2}elseif($TableDurability){6}elseif($ExistingSchema){2}elseif($WriteRejections){20}elseif($UpgradeIdentityCharacters){1}elseif($IdentityCapacity){4}elseif($RootParent){16}elseif($HolonProjection){64}elseif($IdentityRaces -or $DetailDeletionResults){8}elseif($AvatarDeletionResults -or $SaveAcknowledgements){12}elseif($AvatarProjection){128}elseif($LifecycleGuards){24}elseif($EncryptedReplay){4}elseif($PreparationClaims){6}elseif($ReconcileOriginal -or $ReconcileAmbiguity -or $ReconcileHistoricalExport -or $ReconcileCorruptAvatar -or $ReconcileProjectionAssertions){1}else{480}
    if($reportFile.LastWriteTimeUtc -lt $testStarted -or [int]$counts.total -ne $expected -or [int]$counts.passed -ne $expected -or [int]$counts.notExecuted -ne 0){throw 'Fresh complete unskipped Oracle native report required.'}
    $passed=$true
}finally{
    $env:ORACLEDBOASIS_CONNECTIONSTRING=$previousConnection;$env:OASIS_ORACLE_TEST_WORK=$previousWork;$env:OASIS_ORACLE_RESUME_CANONICAL=$previousResume;$secret=$null
    $env:OASIS_ORACLE_RECONCILE_AMBIGUITY=$previousAmbiguity
    $env:OASIS_ORACLE_ALLOW_TEST_IDENTITY_SCHEMA_UPGRADE=$previousSchemaUpgrade
    if($startedHere -and $passed){& wsl -d Ubuntu -u root -- podman stop --time 30 oasis-oracle-free}
    $lease.Dispose()
}
