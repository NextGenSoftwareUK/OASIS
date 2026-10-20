param([switch]$EncryptedReplay,[switch]$PreparationClaims,[switch]$ReconcileOriginal,[switch]$ReconcileAmbiguity,[switch]$ReconcileHistoricalExport,[switch]$ReconcileCorruptAvatar,[switch]$ReconcileProjectionAssertions)
if(([int]$EncryptedReplay.IsPresent+[int]$PreparationClaims.IsPresent+[int]$ReconcileOriginal.IsPresent+[int]$ReconcileAmbiguity.IsPresent+[int]$ReconcileHistoricalExport.IsPresent+[int]$ReconcileCorruptAvatar.IsPresent+[int]$ReconcileProjectionAssertions.IsPresent) -gt 1){throw 'Choose one original reconciliation mode.'}
$ErrorActionPreference='Stop'
$root=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
$work=Join-Path $root 'TestResults/OracleCurrent'
if(!(Test-Path -LiteralPath $work)){throw 'Retained Oracle work directory required; do not recreate an environment.'}
$lease=[IO.File]::Open((Join-Path $work 'environment.lock'),[IO.FileMode]::OpenOrCreate,[IO.FileAccess]::ReadWrite,[IO.FileShare]::None)
$previousConnection=$env:ORACLEDBOASIS_CONNECTIONSTRING;$previousWork=$env:OASIS_ORACLE_TEST_WORK;$previousResume=$env:OASIS_ORACLE_RESUME_CANONICAL
$previousAmbiguity=$env:OASIS_ORACLE_RECONCILE_AMBIGUITY
$startedHere=$false;$passed=$false
try{
    $pending=@(Get-ChildItem -LiteralPath $work -Filter '*.pending.json')
    if($ReconcileOriginal){if($pending.Count -ne 1 -or $pending[0].Name -ne 'canonical-records.pending.json'){throw 'Exactly the original canonical journal required.'}}
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
    $report=if($EncryptedReplay){'oracle-encrypted-replay-native-current.trx'}elseif($PreparationClaims){'oracle-preparation-native-current.trx'}elseif($ReconcileOriginal){'oracle-native-reconciliation-current.trx'}elseif($ReconcileAmbiguity){'oracle-ambiguity-reconciliation.trx'}elseif($ReconcileHistoricalExport){'oracle-history-export-reconciliation.trx'}elseif($ReconcileCorruptAvatar){'oracle-corrupt-avatar-reconciliation.trx'}elseif($ReconcileProjectionAssertions){'oracle-projection-assertion-reconciliation.trx'}else{'oracle-native-current.trx'}
    $filter=if($EncryptedReplay){'FullyQualifiedName~OracleEncryptedReplayNativeTests'}elseif($PreparationClaims){'FullyQualifiedName~OraclePreparationNativeTests'}elseif($ReconcileOriginal){'FullyQualifiedName~RealSdkCanonicalRecordsSelectorsAndMetadataPaths'}elseif($ReconcileAmbiguity){'FullyQualifiedName~DuplicateIdentityIsRejectedInsteadOfSelectingArbitraryAvatar'}elseif($ReconcileHistoricalExport){'FullyQualifiedName~ReconcileKnownHistoricalExportFixture'}elseif($ReconcileCorruptAvatar){'FullyQualifiedName~ReconcileKnownCorruptAvatarFixture'}elseif($ReconcileProjectionAssertions){'FullyQualifiedName~ReconcileKnownProjectionAssertionFixtures'}else{'FullyQualifiedName~OracleNativeAcceptanceTests&FullyQualifiedName!~Reconcile'}
    $testStarted=[DateTime]::UtcNow
    & dotnet test (Join-Path $root 'Providers/Storage/TestProjects/NextGenSoftware.OASIS.API.Providers.OracleDBOASIS.IntegrationTests/NextGenSoftware.OASIS.API.Providers.OracleDBOASIS.IntegrationTests.csproj') --no-build --no-restore --filter $filter --logger "trx;LogFileName=$report" --results-directory $work /m:2 /nr:false
    if($LASTEXITCODE -ne 0 -or (Get-ChildItem -LiteralPath $work -Filter '*.pending.json')){throw 'Native Oracle verification incomplete; preserve originals, no cleanup/reset/replay.'}
    $reportFile=Get-Item -LiteralPath (Join-Path $work $report)
    [xml]$trx=Get-Content -LiteralPath $reportFile.FullName -Raw;$counts=$trx.TestRun.ResultSummary.Counters
    $expected=if($EncryptedReplay){4}elseif($PreparationClaims){6}elseif($ReconcileOriginal -or $ReconcileAmbiguity -or $ReconcileHistoricalExport -or $ReconcileCorruptAvatar -or $ReconcileProjectionAssertions){1}else{96}
    if($reportFile.LastWriteTimeUtc -lt $testStarted -or [int]$counts.total -ne $expected -or [int]$counts.passed -ne $expected -or [int]$counts.notExecuted -ne 0){throw 'Fresh complete unskipped Oracle native report required.'}
    $passed=$true
}finally{
    $env:ORACLEDBOASIS_CONNECTIONSTRING=$previousConnection;$env:OASIS_ORACLE_TEST_WORK=$previousWork;$env:OASIS_ORACLE_RESUME_CANONICAL=$previousResume;$secret=$null
    $env:OASIS_ORACLE_RECONCILE_AMBIGUITY=$previousAmbiguity
    if($startedHere -and $passed){& wsl -d Ubuntu -u root -- podman stop --time 30 oasis-oracle-free}
    $lease.Dispose()
}
