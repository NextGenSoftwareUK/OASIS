param([switch]$ReconcileKnownReadFixture,[switch]$FullAwsSuite,[switch]$ReconcileAwsPreRead,[switch]$CleanupKnownAwsRun,[switch]$ReconcileValidationFixtures,
    [switch]$CleanupVerifiedAwsRun,[ValidatePattern('^[A-Fa-f0-9]{64}$')][string]$VerifiedReportSha256)
$ErrorActionPreference='Stop'
if(([int]$ReconcileKnownReadFixture.IsPresent+[int]$FullAwsSuite.IsPresent+[int]$ReconcileAwsPreRead.IsPresent+[int]$CleanupKnownAwsRun.IsPresent+[int]$ReconcileValidationFixtures.IsPresent+[int]$CleanupVerifiedAwsRun.IsPresent) -gt 1){throw 'Choose one explicit runner mode.'}
if($CleanupVerifiedAwsRun -ne [bool]$VerifiedReportSha256){throw 'Verified cleanup requires its explicit original full-suite report SHA256, exclusively.'}
$root=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
$runtime='C:\Source\OASIS-hyperdrive-v2-gaps\TestResults\AWSProviderEvidence\runtime'
$work=Join-Path $root 'TestResults/DynamoHistoryCurrent'
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
    if($ReconcileValidationFixtures){
        $originalReport=Join-Path $work 'dynamo-history-native-current.trx'
        if($pending.Count -ne 7 -or (Get-FileHash -LiteralPath $originalReport -Algorithm SHA256).Hash -ne '339A00426E80869F82FA8EBE1273A490BA40D94B6829791A35AA04AEABB95E53'){throw 'Exactly the reviewed validation-rejection run is required.'}
    }
    elseif($ReconcileKnownReadFixture){if($pending.Count -ne 1 -or $pending[0].Name -ne 'record-history-2.pending.json'){throw 'Exactly the reviewed read-fixture journal is required.'}}
    elseif($ReconcileAwsPreRead -or $CleanupKnownAwsRun -or $CleanupVerifiedAwsRun){if($pending.Count -ne 1 -or $pending[0].Name -ne 'aws-full-suite.pending.json'){throw 'Exactly the known full-suite intent is required.'}}
    elseif($pending.Count){throw 'Reconcile retained native originals before another run.'}
    $project=Join-Path $root 'Providers/Cloud/TestProjects/NextGenSoftware.OASIS.API.Providers.DynamoDBOASIS.IntegrationTests/NextGenSoftware.OASIS.API.Providers.DynamoDBOASIS.IntegrationTests.csproj'
    if($CleanupVerifiedAwsRun){
        $verifiedReport=Join-Path $work 'aws-full-native-current.trx'
        if((Get-FileHash -LiteralPath $verifiedReport).Hash -ne $VerifiedReportSha256){throw 'Original successful native report differs; no cleanup.'}
        [xml]$verifiedTrx=Get-Content -LiteralPath $verifiedReport -Raw
        $counts=$verifiedTrx.TestRun.ResultSummary.Counters
        $originalIntent=Get-Content -LiteralPath $pending[0].FullName -Raw | ConvertFrom-Json
        if([int]$counts.total -ne 130 -or [int]$counts.passed -ne 130 -or [int]$counts.notExecuted -ne 0 -or
            [DateTimeOffset]$verifiedTrx.TestRun.Times.start -lt [DateTimeOffset]$originalIntent.startedUtc){throw 'Complete original130/130 report newer than the pending intent required.'}
    }
    if($FullAwsSuite -or $ReconcileAwsPreRead -or $CleanupKnownAwsRun -or $CleanupVerifiedAwsRun){
        $project=Join-Path $root 'Providers/Cloud/TestProjects/NextGenSoftware.OASIS.API.Providers.AWSOASIS.IntegrationTests/NextGenSoftware.OASIS.API.Providers.AWSOASIS.IntegrationTests.csproj'
        foreach($dependency in @('Providers/Storage/NextGenSoftware.OASIS.API.Providers.LocalFileOASIS/NextGenSoftware.OASIS.API.Providers.LocalFileOASIS.csproj','Providers/Storage/NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS/NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS.csproj')){
            & dotnet build (Join-Path $root $dependency) --no-restore /p:BuildProjectReferences=false /m:2 /nr:false /p:UseSharedCompilation=false -v:q -clp:ErrorsOnly
            if($LASTEXITCODE -ne 0){throw 'Cached AWS native dependency build failed.'}
        }
    }
    foreach($build in @((Join-Path $root 'Providers/Shared/NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage/NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage.csproj'),(Join-Path $root 'Providers/Cloud/NextGenSoftware.OASIS.API.Providers.DynamoDBOASIS/NextGenSoftware.OASIS.API.Providers.DynamoDBOASIS.csproj'),(Join-Path $root 'Providers/Cloud/NextGenSoftware.OASIS.API.Providers.AWSOASIS/NextGenSoftware.OASIS.API.Providers.AWSOASIS.csproj'),$project)){
        & dotnet build $build --no-restore --no-incremental /p:BuildProjectReferences=false /m:2 /nr:false /p:UseSharedCompilation=false -v:q -clp:ErrorsOnly
        if($LASTEXITCODE -ne 0){throw 'Cached producer/consumer build failed.'}
    }
    & (Join-Path $PSScriptRoot 'Assert-ReusableProviderBuildIdentity.ps1') -RepositoryRoot $root -ConsumerOutput (Join-Path (Split-Path $project) 'bin/Debug/net10.0')
    $java=(Get-Command java -ErrorAction Stop).Source
    $owned=Start-Process -FilePath $java -ArgumentList @("-Djava.library.path=$runtime/DynamoDBLocal_lib",'-jar',"$runtime/DynamoDBLocal.jar",'-dbPath',$data,'-sharedDb','-disableTelemetry','-port','18000') -WindowStyle Hidden -WorkingDirectory $runtime -PassThru -RedirectStandardOutput (Join-Path $work 'native-current.log') -RedirectStandardError (Join-Path $work 'native-current.err')
    $deadline=[DateTime]::UtcNow.AddSeconds(20)
    do{
        if($owned.HasExited){throw 'Retained runtime exited before readiness.'}
        $listener=Get-NetTCPConnection -State Listen -LocalPort 18000 -ErrorAction SilentlyContinue
        if($listener){if($listener.OwningProcess -ne $owned.Id){throw 'Listener is not owned runtime.'};break}
        Start-Sleep -Milliseconds 200
    }while([DateTime]::UtcNow -lt $deadline)
    if(!$listener){throw 'Owned runtime readiness timeout.'}
    $env:DYNAMODB_SERVICE_URL='http://127.0.0.1:18000';$env:OASIS_DYNAMO_HISTORY_WORK=$work;$started=[DateTime]::UtcNow
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
    if($FullAwsSuite){
        & dotnet test $project --no-build --no-restore --filter 'FullyQualifiedName~AWSNativeRunLifecycle.BeginFromVerifiedEmptyRetainedTables' --logger 'trx;LogFileName=aws-full-begin-current.trx' --results-directory $work -v:q
        if($LASTEXITCODE -ne 0){throw 'Native empty-baseline/intent gate failed; do not start suite.'}
        & dotnet test $project --no-build --no-restore --filter 'FullyQualifiedName!~AWSNativeRunLifecycle' --logger 'trx;LogFileName=aws-full-native-current.trx' --results-directory $work -v:q
        if($LASTEXITCODE -ne 0){throw 'Full AWS suite failed; preserve original persistent data/intent. No cleanup/retry.'}
        $report=Get-Item (Join-Path $work 'aws-full-native-current.trx');[xml]$trx=Get-Content $report.FullName -Raw;$counts=$trx.TestRun.ResultSummary.Counters
        if($report.LastWriteTimeUtc -lt $started -or [int]$counts.total -ne 130 -or [int]$counts.passed -ne 130 -or [int]$counts.notExecuted -ne 0){throw 'Fresh full130/130 suite required before cleanup.'}
        $env:OASIS_AWS_CLEANUP_AUTHORITY='verified-full-suite-pass'
        & dotnet test $project --no-build --no-restore --filter 'FullyQualifiedName~AWSNativeRunLifecycle.CleanupOnlyAfterVerifiedFullSuitePass' --logger 'trx;LogFileName=aws-full-cleanup-current.trx' --results-directory $work -v:q
        if($LASTEXITCODE -ne 0 -or (Get-ChildItem -LiteralPath $work -Filter '*.pending.json')){throw 'Native exact cleanup incomplete; preserve remaining originals.'}
        return
    }
    $filter=if($ReconcileKnownReadFixture){'FullyQualifiedName~ReconcileKnownHistoryReadFixture'}else{'FullyQualifiedName~DynamoHistoryNativeTests&FullyQualifiedName!~ReconcileKnown'}
    $reportName=if($ReconcileKnownReadFixture){'dynamo-history-reconciliation-current.trx'}else{'dynamo-history-native-current.trx'}
    & dotnet test $project --no-build --no-restore --filter $filter --logger "trx;LogFileName=$reportName" --results-directory $work -v:q
    if($LASTEXITCODE -ne 0){throw 'Native history test failed; preserve journal/data for reconciliation.'}
    $report=Get-Item (Join-Path $work $reportName);[xml]$trx=Get-Content $report.FullName -Raw;$counts=$trx.TestRun.ResultSummary.Counters;$expected=if($ReconcileKnownReadFixture){1}else{79}
    if($report.LastWriteTimeUtc -lt $started -or [int]$counts.total -ne $expected -or [int]$counts.passed -ne $expected -or [int]$counts.notExecuted -ne 0 -or (Get-ChildItem -LiteralPath $work -Filter '*.pending.json')){throw 'Fresh complete native cases and cleared original journals required.'}
}finally{
    $env:DYNAMODB_SERVICE_URL=$previousEndpoint;$env:OASIS_DYNAMO_HISTORY_WORK=$previousWork
    $env:OASIS_AWS_CLEANUP_AUTHORITY=$previousCleanup
    if($owned -and !$owned.HasExited){Stop-Process -Id $owned.Id;$owned.WaitForExit(5000)|Out-Null}
    $lease.Dispose()
}
