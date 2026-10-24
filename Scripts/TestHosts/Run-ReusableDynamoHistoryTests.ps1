param([switch]$CleanupCanonicalHistory,[switch]$InspectCanonicalHistory,[switch]$CleanupSqliteKarmaHistory,[switch]$InspectSqliteKarmaHistory,[switch]$ReconcileKarmaSeed,[switch]$OrdinarySaveAcknowledgements,[switch]$OrdinarySaveLifecycle,[switch]$DetailDeletionResults,[switch]$BatchRevisions,[switch]$EncryptedProcess,[switch]$EncryptedReplay,[switch]$PreparationClaims,[switch]$ReconcileConditionalInitial,[switch]$ConditionalGraphs,[switch]$ReconcileKnownReadFixture,[switch]$FullAwsSuite,[switch]$ReconcileAwsPreRead,[switch]$CleanupKnownAwsRun,[switch]$ReconcileValidationFixtures,[switch]$ReconcileReflectionFixtures,
    [switch]$IndependentExportVersions,[switch]$DuplicatePageRegression,[switch]$CleanupKarmaMutation,[switch]$InspectKarmaMutation,[switch]$CleanupKarmaSeed,[switch]$CleanupVerifiedAwsRun,[ValidatePattern('^[A-Fa-f0-9]{64}$')][string]$VerifiedReportSha256)
$ErrorActionPreference='Stop'
$selectedModes=@($CleanupCanonicalHistory,$InspectCanonicalHistory,$CleanupSqliteKarmaHistory,$InspectSqliteKarmaHistory,$CleanupKarmaMutation,$InspectKarmaMutation,$CleanupKarmaSeed,$ReconcileKarmaSeed,$OrdinarySaveAcknowledgements,$OrdinarySaveLifecycle,$DetailDeletionResults,
    $BatchRevisions,$EncryptedProcess,$EncryptedReplay,$PreparationClaims,$ReconcileConditionalInitial,$ConditionalGraphs,
    $ReconcileKnownReadFixture,$FullAwsSuite,$ReconcileAwsPreRead,$CleanupKnownAwsRun,$ReconcileValidationFixtures,
    $ReconcileReflectionFixtures,$CleanupVerifiedAwsRun,$IndependentExportVersions,$DuplicatePageRegression).Where({$_.IsPresent})
if($selectedModes.Count -gt 1){throw 'Choose one explicit runner mode.'}
if($CleanupVerifiedAwsRun -ne [bool]$VerifiedReportSha256){throw 'Verified cleanup requires its explicit original full-suite report SHA256, exclusively.'}
$root=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
$runtime='C:\Source\OASIS-hyperdrive-v2-gaps\TestResults\AWSProviderEvidence\runtime'
$work=Join-Path $root 'TestResults/DynamoHistoryCurrent'
if($DuplicatePageRegression -and (Test-Path -LiteralPath (Join-Path $work 'dynamo-duplicate-pages-native-current.trx'))){
    throw 'Preserve the original duplicate-page native regression report before another run.'
}
if($IndependentExportVersions -and (Test-Path -LiteralPath (Join-Path $work 'dynamo-independent-export-native-current.trx'))){
    throw 'Preserve the original independent-export native report before another run.'
}
if($FullAwsSuite){
    foreach($reportName in @('aws-full-begin-current.trx','aws-full-native-current.trx')){
        if(Test-Path -LiteralPath (Join-Path $work $reportName)){throw 'Preserve and hash-verify the previous full-suite reports before another native run.'}
    }
}
if($FullAwsSuite -and (Test-Path -LiteralPath (Join-Path $work 'aws-full-suite-cleanup-current.json'))){throw 'Review and preserve prior cleanup inventory BEFORE starting another native suite.'}
$data=Join-Path (Split-Path $runtime) 'retained-history-data'
if(!(Test-Path -LiteralPath (Join-Path $runtime 'DynamoDBLocal.jar')) -or (Get-Content (Join-Path $runtime 'runtime-source-sha256.txt') -Raw).Trim() -ne 'A3CB28740EA3875A7B0E14CFB13CED686F36CE27BC00F13F8A7D72DDEEE6C9B1'){throw 'Reviewed retained DynamoDB runtime/provenance required; no download/install.'}
if(Get-NetTCPConnection -State Listen -LocalPort 18000,18001 -ErrorAction SilentlyContinue){throw 'Reserved DynamoDB ports occupied; no duplicate service.'}
New-Item -ItemType Directory -Force -Path $work,$data | Out-Null
$lease=[IO.File]::Open((Join-Path (Split-Path $runtime) 'environment.lock'),[IO.FileMode]::OpenOrCreate,[IO.FileAccess]::ReadWrite,[IO.FileShare]::None)
$owned=$null;$previousEndpoint=$env:DYNAMODB_SERVICE_URL;$previousWork=$env:OASIS_DYNAMO_HISTORY_WORK
$previousCleanup=$env:OASIS_AWS_CLEANUP_AUTHORITY
try{
    $pending=@(Get-ChildItem -LiteralPath $work -Filter '*.pending.json')
    if($ReconcileConditionalInitial){
        if($pending.Count -ne 4 -or (Get-FileHash -LiteralPath (Join-Path $work 'dynamo-conditional-native-current.trx')).Hash -ne 'C3F05D24795A1A0A0A14973110802E0A5D218837BA60EF77C475DE68A5C85EE0'){throw 'Exactly the original four conditional graph assertion fixtures required.'}
    }
    elseif($ReconcileReflectionFixtures){
        if($pending.Count -ne 9 -or (Get-FileHash -LiteralPath (Join-Path $work 'dynamo-history-native-current.trx')).Hash -ne '802B54BCF845A7131349072CA49E0B7AB8ABEAC0837D5345F1B55B4518478A3B'){throw 'Exactly the reviewed reflection-signature run required.'}
    }
    elseif($ReconcileValidationFixtures){
        $originalReport=Join-Path $work 'dynamo-history-native-current.trx'
        if($pending.Count -ne 7 -or (Get-FileHash -LiteralPath $originalReport -Algorithm SHA256).Hash -ne '339A00426E80869F82FA8EBE1273A490BA40D94B6829791A35AA04AEABB95E53'){throw 'Exactly the reviewed validation-rejection run is required.'}
    }
    elseif($ReconcileKnownReadFixture){if($pending.Count -ne 1 -or $pending[0].Name -ne 'record-history-2.pending.json'){throw 'Exactly the reviewed read-fixture journal is required.'}}
    elseif($CleanupCanonicalHistory -or $InspectCanonicalHistory -or $CleanupSqliteKarmaHistory -or $InspectSqliteKarmaHistory -or $CleanupKarmaMutation -or $InspectKarmaMutation -or $CleanupKarmaSeed -or $ReconcileKarmaSeed -or $ReconcileAwsPreRead -or $CleanupKnownAwsRun -or $CleanupVerifiedAwsRun){if($pending.Count -ne 1 -or $pending[0].Name -ne 'aws-full-suite.pending.json'){throw 'Exactly the known full-suite intent is required.'}}
    elseif($pending.Count){throw 'Reconcile retained native originals before another run.'}
    $project=Join-Path $root 'Providers/Cloud/TestProjects/NextGenSoftware.OASIS.API.Providers.DynamoDBOASIS.IntegrationTests/NextGenSoftware.OASIS.API.Providers.DynamoDBOASIS.IntegrationTests.csproj'
    if($CleanupVerifiedAwsRun){
        $verifiedReport=Join-Path $work 'aws-full-native-current.trx'
        if((Get-FileHash -LiteralPath $verifiedReport).Hash -ne $VerifiedReportSha256){throw 'Original successful native report differs; no cleanup.'}
        [xml]$verifiedTrx=Get-Content -LiteralPath $verifiedReport -Raw
        $counts=$verifiedTrx.TestRun.ResultSummary.Counters
        $originalIntent=Get-Content -LiteralPath $pending[0].FullName -Raw | ConvertFrom-Json
        if([int]$counts.total -ne 134 -or [int]$counts.passed -ne 134 -or [int]$counts.notExecuted -ne 0 -or
            [DateTimeOffset]$verifiedTrx.TestRun.Times.start -lt [DateTimeOffset]$originalIntent.startedUtc){throw 'Complete original134/134 report newer than the pending intent required.'}
    }
    if($CleanupCanonicalHistory -or $InspectCanonicalHistory -or $CleanupSqliteKarmaHistory -or $InspectSqliteKarmaHistory -or $CleanupKarmaMutation -or $InspectKarmaMutation -or $CleanupKarmaSeed -or $ReconcileKarmaSeed -or $FullAwsSuite -or $ReconcileAwsPreRead -or $CleanupKnownAwsRun -or $CleanupVerifiedAwsRun){
        $project=Join-Path $root 'Providers/Cloud/TestProjects/NextGenSoftware.OASIS.API.Providers.AWSOASIS.IntegrationTests/NextGenSoftware.OASIS.API.Providers.AWSOASIS.IntegrationTests.csproj'
        # Read-only reconciliation uses the pinned Sqlite SDK directly, not either
        # storage adapter. Preserve their frozen outputs while their owners edit.
        if(!($CleanupCanonicalHistory -or $InspectCanonicalHistory -or $CleanupSqliteKarmaHistory -or $InspectSqliteKarmaHistory -or $CleanupKarmaMutation -or $InspectKarmaMutation -or $ReconcileKarmaSeed -or $CleanupKarmaSeed)){
        foreach($dependency in @('Providers/Storage/NextGenSoftware.OASIS.API.Providers.LocalFileOASIS/NextGenSoftware.OASIS.API.Providers.LocalFileOASIS.csproj','Providers/Storage/NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS/NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS.csproj')){
            & dotnet build (Join-Path $root $dependency) --no-restore /p:BuildProjectReferences=false /m:2 /nr:false /p:UseSharedCompilation=false -v:q -clp:ErrorsOnly
            if($LASTEXITCODE -ne 0){throw 'Cached AWS native dependency build failed.'}
        }
        }
    }
    foreach($build in @((Join-Path $root 'Providers/Shared/NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage/NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage.csproj'),(Join-Path $root 'Providers/Cloud/NextGenSoftware.OASIS.API.Providers.DynamoDBOASIS/NextGenSoftware.OASIS.API.Providers.DynamoDBOASIS.csproj'),(Join-Path $root 'Providers/Cloud/NextGenSoftware.OASIS.API.Providers.AWSOASIS/NextGenSoftware.OASIS.API.Providers.AWSOASIS.csproj'),$project)){
        & dotnet build $build --no-restore --no-incremental /p:BuildProjectReferences=false /m:2 /nr:false /p:UseSharedCompilation=false -v:q -clp:ErrorsOnly
        if($LASTEXITCODE -ne 0){throw 'Cached producer/consumer build failed.'}
    }
    & (Join-Path $PSScriptRoot 'Assert-ReusableProviderBuildIdentity.ps1') -RepositoryRoot $root -ConsumerOutput (Join-Path (Split-Path $project) 'bin/Debug/net10.0') -VerifySourceDocuments
    $owned=& (Join-Path $PSScriptRoot 'Use-ReusableDynamoContainer.ps1')
    $env:DYNAMODB_SERVICE_URL='http://127.0.0.1:18000';$env:OASIS_DYNAMO_HISTORY_WORK=$work;$started=[DateTime]::UtcNow
    if($EncryptedProcess){
        foreach($phase in @('write','recover')){
            $phaseReport=Join-Path $work "dynamo-encrypted-process-$phase.trx"
            if(Test-Path -LiteralPath $phaseReport){throw 'Preserve original process evidence before another run.'}
            $method=if($phase -eq 'write'){'PhaseOneCommitEncryptedOperation'}else{'PhaseTwoRecoverInDifferentProcess'}
            & dotnet test $project --no-build --no-restore --filter "FullyQualifiedName~DynamoEncryptedProcessNativeTests.$method" --logger "trx;LogFileName=dynamo-encrypted-process-$phase.trx" --results-directory $work /m:2 /nr:false -v:q
            if($LASTEXITCODE -ne 0){throw 'Encrypted process verification failed; retain original fixtures, no replay/reset.'}
            $phaseFile=Get-Item -LiteralPath $phaseReport
            [xml]$phaseTrx=Get-Content -LiteralPath $phaseReport -Raw;$phaseCounts=$phaseTrx.TestRun.ResultSummary.Counters
            if($phaseFile.LastWriteTimeUtc -lt $started -or [int]$phaseCounts.total -ne 2 -or [int]$phaseCounts.passed -ne 2 -or [int]$phaseCounts.notExecuted -ne 0){throw 'Fresh complete2/2 process phase required.'}
            if($phase -eq 'write' -and @(Get-ChildItem -LiteralPath $work -Filter 'encrypted-process-*.pending.json').Count -ne 2){throw 'Exactly two committed process intents required before recovery.'}
        }
        if(Get-ChildItem -LiteralPath $work -Filter '*.pending.json'){throw 'Original process fixtures remain unresolved.'}
        return
    }
    if($ReconcileReflectionFixtures){
        & dotnet test $project --no-build --no-restore --filter 'FullyQualifiedName~ReconcileKnownReflectionSignatureFixtures' --logger 'trx;LogFileName=dynamo-reflection-reconciliation-current.trx' --results-directory $work -v:q
        if($LASTEXITCODE -ne 0 -or (Get-ChildItem -LiteralPath $work -Filter '*.pending.json')){throw 'Original reflection fixtures require further reconciliation; no replay/reset.'}
        return
    }
    if($ReconcileValidationFixtures){
        & dotnet test $project --no-build --no-restore --filter 'FullyQualifiedName~ReconcileKnownValidationRejectionFixtures' --logger 'trx;LogFileName=dynamo-validation-reconciliation-current.trx' --results-directory $work -v:q
        if($LASTEXITCODE -ne 0 -or (Get-ChildItem -LiteralPath $work -Filter '*.pending.json')){throw 'Original validation fixtures require further reconciliation; no replay/reset.'}
        return
    }
    if($CleanupKnownAwsRun){
        $env:OASIS_AWS_CLEANUP_AUTHORITY='known-run-reconciled'
        & dotnet test $project --no-build --no-restore --filter 'FullyQualifiedName~AWSNativeRunLifecycle.CleanupKnownReadOnlyReconciledRun' --logger 'trx;LogFileName=aws-known-run-cleanup.trx' --results-directory $work -v:q
        if($LASTEXITCODE -ne 0 -or (Get-ChildItem -LiteralPath $work -Filter '*.pending.json')){throw 'Known-run reconciliation cleanup incomplete; preserve remaining originals.'}
        return
    }
    if($CleanupVerifiedAwsRun){
        $env:OASIS_AWS_CLEANUP_AUTHORITY='verified-full-suite-pass'
        & dotnet test $project --no-build --no-restore --filter 'FullyQualifiedName~AWSNativeRunLifecycle.CleanupOnlyAfterVerifiedFullSuitePass' --logger 'trx;LogFileName=aws-full-cleanup-recovery-current.trx' --results-directory $work -v:q
        if($LASTEXITCODE -ne 0 -or (Get-ChildItem -LiteralPath $work -Filter '*.pending.json')){throw 'Verified original cleanup incomplete; retain remaining originals.'}
        if((Get-FileHash -LiteralPath $verifiedReport).Hash -ne $VerifiedReportSha256){throw 'Original report changed during cleanup.'}
        return
    }
    if($ReconcileAwsPreRead){
        & dotnet test $project --no-build --no-restore --filter 'FullyQualifiedName~AWSNativeRunLifecycle.ReadOnlyReconcileKnownPreReadFailover' --logger 'trx;LogFileName=aws-known-pre-read-reconciliation.trx' --results-directory $work -v:q
        if($LASTEXITCODE -ne 0){throw 'Original read-only reconciliation failed; preserve all evidence.'}
        return
    }
    if($CleanupKarmaMutation){
        if(Test-Path -LiteralPath (Join-Path $work 'aws-karma-mutation-cleanup-current.trx')){throw 'Preserve original cleanup report.'}
        $env:OASIS_AWS_CLEANUP_AUTHORITY='known-karma-mutation-reconciled'
        & dotnet test $project --no-build --no-restore --filter 'FullyQualifiedName~AWSNativeRunLifecycle.CleanupOnlyReviewedKarmaMutationFailure' --logger 'trx;LogFileName=aws-karma-mutation-cleanup-current.trx' --results-directory $work -v:q
        if($LASTEXITCODE -ne 0 -or (Get-ChildItem -LiteralPath $work -Filter '*.pending.json')){throw 'Reviewed mutation cleanup incomplete; preserve originals.'}
        return
    }
    if($CleanupSqliteKarmaHistory){
        if(Test-Path -LiteralPath (Join-Path $work 'aws-sqlite-karma-history-cleanup-current.trx')){throw 'Preserve original history cleanup report.'}
        $env:OASIS_AWS_CLEANUP_AUTHORITY='known-sqlite-karma-history-reconciled'
        & dotnet test $project --no-build --no-restore --filter 'FullyQualifiedName~AWSNativeRunLifecycle.CleanupOnlyReviewedSqliteKarmaHistoryFailure' --logger 'trx;LogFileName=aws-sqlite-karma-history-cleanup-current.trx' --results-directory $work -v:q
        if($LASTEXITCODE -ne 0 -or (Get-ChildItem -LiteralPath $work -Filter '*.pending.json')){throw 'Reviewed history cleanup incomplete; preserve originals.'}
        return
    }
    if($CleanupCanonicalHistory){
        if(Test-Path -LiteralPath (Join-Path $work 'aws-canonical-history-cleanup-current.trx')){throw 'Preserve original canonical history cleanup.'}
        $env:OASIS_AWS_CLEANUP_AUTHORITY='known-canonical-history-reconciled'
        & dotnet test $project --no-build --no-restore --filter 'FullyQualifiedName~AWSNativeRunLifecycle.CleanupOnlyReviewedCanonicalHistoryAssertionFailure' --logger 'trx;LogFileName=aws-canonical-history-cleanup-current.trx' --results-directory $work -v:q
        if($LASTEXITCODE -ne 0 -or (Get-ChildItem -LiteralPath $work -Filter '*.pending.json')){throw 'Reviewed canonical history cleanup incomplete; preserve originals.'}
        return
    }
    if($InspectCanonicalHistory){
        if(Test-Path -LiteralPath (Join-Path $work 'aws-canonical-history-inspection-current.trx')){throw 'Preserve original canonical history inspection.'}
        & dotnet test $project --no-build --no-restore --filter 'FullyQualifiedName~AWSNativeRunLifecycle.ReadOnlyInspectCanonicalHistoryAssertionFailure' --logger 'trx;LogFileName=aws-canonical-history-inspection-current.trx' --results-directory $work -v:q
        if($LASTEXITCODE -ne 0){throw 'Read-only canonical history inspection failed; preserve originals.'}
        return
    }
    if($InspectSqliteKarmaHistory){
        if(Test-Path -LiteralPath (Join-Path $work 'aws-sqlite-karma-history-inspection-current.trx')){throw 'Preserve original history inspection.'}
        & dotnet test $project --no-build --no-restore --filter 'FullyQualifiedName~AWSNativeRunLifecycle.ReadOnlyInspectSqliteKarmaHistoryFailure' --logger 'trx;LogFileName=aws-sqlite-karma-history-inspection-current.trx' --results-directory $work -v:q
        if($LASTEXITCODE -ne 0){throw 'Read-only history inspection failed; preserve originals.'}
        return
    }
    if($InspectKarmaMutation){
        if(Test-Path -LiteralPath (Join-Path $work 'aws-karma-mutation-inspection-current.trx')){throw 'Preserve original mutation inspection evidence.'}
        & dotnet test $project --no-build --no-restore --filter 'FullyQualifiedName~AWSNativeRunLifecycle.ReadOnlyInspectKarmaMutationFailure' --logger 'trx;LogFileName=aws-karma-mutation-inspection-current.trx' --results-directory $work -v:q
        if($LASTEXITCODE -ne 0){throw 'Read-only mutation inspection failed; preserve original data, no replay.'}
        return
    }
    if($ReconcileKarmaSeed -or $CleanupKarmaSeed){
        $recoveryReport=if($CleanupKarmaSeed){'aws-karma-seed-cleanup-current.trx'}else{'aws-karma-seed-reconciliation-current.trx'}
        $recoveryMethod=if($CleanupKarmaSeed){'CleanupOnlyReviewedKarmaSeedFailure'}else{'ReadOnlyReconcileKnownKarmaSeedFailure'}
        if(Test-Path -LiteralPath (Join-Path $work $recoveryReport)){throw 'Preserve the original karma recovery report before another phase.'}
        if($CleanupKarmaSeed){$env:OASIS_AWS_CLEANUP_AUTHORITY='known-karma-seed-reconciled'}
        & dotnet test $project --no-build --no-restore --filter "FullyQualifiedName~AWSNativeRunLifecycle.$recoveryMethod" --logger "trx;LogFileName=$recoveryReport" --results-directory $work -v:q
        if($LASTEXITCODE -ne 0){throw 'Karma fixture recovery failed; preserve remaining originals, no replay.'}
        if($CleanupKarmaSeed -and (Get-ChildItem -LiteralPath $work -Filter '*.pending.json')){throw 'Reviewed cleanup incomplete; preserve remaining originals.'}
        return
    }
    if($FullAwsSuite){
        & dotnet test $project --no-build --no-restore --filter 'FullyQualifiedName~AWSNativeRunLifecycle.BeginFromVerifiedEmptyRetainedTables' --logger 'trx;LogFileName=aws-full-begin-current.trx' --results-directory $work -v:q
        if($LASTEXITCODE -ne 0){throw 'Native empty-baseline/intent gate failed; do not start suite.'}
        & dotnet test $project --no-build --no-restore --filter 'FullyQualifiedName!~AWSNativeRunLifecycle' --logger 'trx;LogFileName=aws-full-native-current.trx' --results-directory $work -v:q
        if($LASTEXITCODE -ne 0){throw 'Full AWS suite failed; preserve original persistent data/intent. No cleanup/retry.'}
        $report=Get-Item (Join-Path $work 'aws-full-native-current.trx');[xml]$trx=Get-Content $report.FullName -Raw;$counts=$trx.TestRun.ResultSummary.Counters
        if($report.LastWriteTimeUtc -lt $started -or [int]$counts.total -ne 134 -or [int]$counts.passed -ne 134 -or [int]$counts.notExecuted -ne 0){throw 'Fresh full134/134 suite required before cleanup.'}
        $env:OASIS_AWS_CLEANUP_AUTHORITY='verified-full-suite-pass'
        & dotnet test $project --no-build --no-restore --filter 'FullyQualifiedName~AWSNativeRunLifecycle.CleanupOnlyAfterVerifiedFullSuitePass' --logger 'trx;LogFileName=aws-full-cleanup-current.trx' --results-directory $work -v:q
        if($LASTEXITCODE -ne 0 -or (Get-ChildItem -LiteralPath $work -Filter '*.pending.json')){throw 'Native exact cleanup incomplete; preserve remaining originals.'}
        return
    }
    $filter=if($EncryptedReplay){'FullyQualifiedName~DynamoEncryptedReplayNativeTests'}elseif($PreparationClaims){'FullyQualifiedName~DynamoPreparationNativeTests'}elseif($ReconcileConditionalInitial){'FullyQualifiedName~ReconcileOriginalFourInitialGraphAssertionFixtures'}elseif($ConditionalGraphs){'FullyQualifiedName~DynamoConditionalNativeTests'}elseif($ReconcileKnownReadFixture){'FullyQualifiedName~ReconcileKnownHistoryReadFixture'}else{'FullyQualifiedName~DynamoHistoryNativeTests&FullyQualifiedName!~ReconcileKnown'}
    $reportName=if($EncryptedReplay){'dynamo-encrypted-replay-native-current.trx'}elseif($PreparationClaims){'dynamo-preparation-native-current.trx'}elseif($ReconcileConditionalInitial){'dynamo-conditional-reconciliation-current.trx'}elseif($ConditionalGraphs){'dynamo-conditional-native-current.trx'}elseif($ReconcileKnownReadFixture){'dynamo-history-reconciliation-current.trx'}else{'dynamo-history-native-current.trx'}
    if($BatchRevisions){$filter='FullyQualifiedName~DynamoBatchRevisionNativeTests';$reportName='dynamo-batch-revision-native-current.trx'}
    if($DetailDeletionResults){$filter='FullyQualifiedName~StandaloneDetailNativeLifecyclePreservesAvatarAndOriginalCondition|FullyQualifiedName~NativeSoftDeletePreservesCurrentAndArchivedScalarMetadata';$reportName='dynamo-detail-delete-result-native-current.trx'}
    if($OrdinarySaveLifecycle){$filter='FullyQualifiedName~NativeOrdinarySaveMustPreserveObservedTombstone';$reportName='dynamo-ordinary-save-lifecycle-native-current.trx'}
    if($OrdinarySaveAcknowledgements){$filter='FullyQualifiedName~NativeOrdinarySaveCountsOnlyKnownCommitBeforeCallerPublication';$reportName='dynamo-ordinary-save-ack-native-current.trx'}
    if($IndependentExportVersions){$reportName='dynamo-independent-export-native-current.trx'}
    if($DuplicatePageRegression){$reportName='dynamo-duplicate-pages-native-current.trx'}
    if(Test-Path -LiteralPath (Join-Path $work $reportName)){throw 'Preserve the original native report before another run.'}
    & dotnet test $project --no-build --no-restore --filter $filter --logger "trx;LogFileName=$reportName" --results-directory $work -v:q
    if($LASTEXITCODE -ne 0){throw 'Native history test failed; preserve journal/data for reconciliation.'}
$report=Get-Item (Join-Path $work $reportName);[xml]$trx=Get-Content $report.FullName -Raw;$counts=$trx.TestRun.ResultSummary.Counters;$expected=if($EncryptedReplay){8}elseif($PreparationClaims){4}elseif($ReconcileConditionalInitial){1}elseif($ConditionalGraphs){8}elseif($ReconcileKnownReadFixture){1}else{171}
    if($BatchRevisions){$expected=4}
    if($DetailDeletionResults){$expected=14}
    if($OrdinarySaveLifecycle){$expected=48}
    if($OrdinarySaveAcknowledgements){$expected=12}
    if($report.LastWriteTimeUtc -lt $started -or [int]$counts.total -ne $expected -or [int]$counts.passed -ne $expected -or [int]$counts.notExecuted -ne 0 -or (Get-ChildItem -LiteralPath $work -Filter '*.pending.json')){throw 'Fresh complete native cases and cleared original journals required.'}
}finally{
    $env:DYNAMODB_SERVICE_URL=$previousEndpoint;$env:OASIS_DYNAMO_HISTORY_WORK=$previousWork
    $env:OASIS_AWS_CLEANUP_AUTHORITY=$previousCleanup
    try{if($owned){& (Join-Path $PSScriptRoot 'Use-ReusableDynamoContainer.ps1') -Stop}}
    finally{$lease.Dispose()}
}
