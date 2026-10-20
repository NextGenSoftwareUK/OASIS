param([switch]$ReconcileComputedNameFixture)
$ErrorActionPreference='Stop'
$root=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
$work=Join-Path $root 'TestResults/PostgreSqlCurrent'
if(!(Test-Path -LiteralPath $work)){throw 'Retained PostgreSQL work directory required.'}
$lease=[IO.File]::Open((Join-Path $work 'environment.lock'),[IO.FileMode]::OpenOrCreate,[IO.FileAccess]::ReadWrite,[IO.FileShare]::None)
$oldConnection=$env:POSTGRESQLOASIS_CONNECTIONSTRING;$oldWork=$env:OASIS_POSTGRES_TEST_WORK
$startedHere=$false;$passed=$false;$secret=$null
try{
    $pending=@(Get-ChildItem -LiteralPath $work -Filter '*.pending.json')
    if($ReconcileComputedNameFixture){if($pending.Count -ne 4){throw 'Exactly four original computed-name assertion journals required.'}}
    elseif($pending.Count){throw 'Reconcile original PostgreSQL journals before new writes.'}
    $reportName=if($ReconcileComputedNameFixture){'postgres-computed-name-reconciliation.trx'}else{'postgres-native-current.trx'}
    $report=Join-Path $work $reportName
    if(Test-Path -LiteralPath $report){throw 'Preserve the prior compact native report before another run.'}
    $identity=(& wsl -d Ubuntu -u root -- podman inspect --format '{{.Image}}' oasis-postgres).Trim()
    if($LASTEXITCODE -ne 0 -or $identity -ne '79bd7c99e923138f136f8009d6bffa66e21e9d4fda5c0c561b00fc9c90cfe537'){throw 'Exact retained PostgreSQL runtime required; no pull/create/reset.'}
    $ports=& wsl -d Ubuntu -u root -- podman inspect --format '{{json .HostConfig.PortBindings}}' oasis-postgres | ConvertFrom-Json
    if($LASTEXITCODE -ne 0 -or @($ports.'5432/tcp').Count -ne 1 -or $ports.'5432/tcp'[0].HostIp -ne '127.0.0.1' -or $ports.'5432/tcp'[0].HostPort -ne '15432'){throw 'Expected loopback PostgreSQL port required.'}
    $mounts=@(& wsl -d Ubuntu -u root -- podman inspect --format '{{json .Mounts}}' oasis-postgres | ConvertFrom-Json)
    if($LASTEXITCODE -ne 0 -or @($mounts | Where-Object { $_.Type -eq 'volume' -and $_.Name -eq 'oasis-postgres-data' -and $_.Destination -eq '/var/lib/postgresql/data' }).Count -ne 1){throw 'Retained PostgreSQL data volume required.'}
    foreach($project in @('Providers/Storage/NextGenSoftware.OASIS.API.Providers.PostgreSQLOASIS/NextGenSoftware.OASIS.API.Providers.PostgreSQLOASIS.csproj','Providers/Storage/TestProjects/NextGenSoftware.OASIS.API.Providers.PostgreSQLOASIS.IntegrationTests/NextGenSoftware.OASIS.API.Providers.PostgreSQLOASIS.IntegrationTests.csproj')){
        & dotnet build (Join-Path $root $project) --no-restore /p:BuildProjectReferences=false /m:2 /nr:false /p:UseSharedCompilation=false -v:q -clp:ErrorsOnly
        if($LASTEXITCODE -ne 0){throw 'Cached PostgreSQL producer/consumer build failed.'}
    }
    & (Join-Path $PSScriptRoot 'Assert-ReusableProviderBuildIdentity.ps1') -RepositoryRoot $root -ConsumerOutput 'Providers/Storage/TestProjects/NextGenSoftware.OASIS.API.Providers.PostgreSQLOASIS.IntegrationTests/bin/Debug/net10.0'
    $state=(& wsl -d Ubuntu -u root -- podman inspect --format '{{.State.Status}}' oasis-postgres).Trim()
    if($state -eq 'exited'){
        & wsl -d Ubuntu -u root -- podman start oasis-postgres
        if($LASTEXITCODE -ne 0){throw 'Retained PostgreSQL start failed.'};$startedHere=$true
    }elseif($state -ne 'running'){throw 'Unexpected retained PostgreSQL state; inspect original, do not recreate.'}
    $deadline=[DateTime]::UtcNow.AddSeconds(30)
    do{
        $health=(& wsl -d Ubuntu -u root -- podman inspect --format '{{.State.Health.Status}}' oasis-postgres).Trim()
        if($LASTEXITCODE -ne 0){throw 'PostgreSQL health observation failed; no restart/reset.'}
        if($health -eq 'healthy'){break}
        if($health -ne 'starting'){throw 'Retained PostgreSQL unhealthy; preserve for inspection.'}
        Start-Sleep -Milliseconds 500
    }while([DateTime]::UtcNow -lt $deadline)
    if($health -ne 'healthy'){throw 'PostgreSQL readiness observation timed out; preserve the existing process, no restart/reset.'}
    $secret=(& wsl -d Ubuntu -u root -- podman secret inspect --showsecret oasis-postgres-pwd | ConvertFrom-Json)[0].SecretData
    if([string]::IsNullOrWhiteSpace($secret)){throw 'Protected PostgreSQL test secret unavailable.'}
    $env:POSTGRESQLOASIS_CONNECTIONSTRING="Host=127.0.0.1;Port=15432;Database=oasis_provider_test;Username=oasis_test;Password=$secret;Timeout=10;Command Timeout=15;Pooling=false"
    $env:OASIS_POSTGRES_TEST_WORK=$work
    $started=[DateTime]::UtcNow
    $filter=if($ReconcileComputedNameFixture){'FullyQualifiedName~ReconcileKnownComputedNameAssertionFixtures'}else{'FullyQualifiedName~PostgreSqlNativeAcceptanceTests&FullyQualifiedName!~Reconcile'}
    & dotnet test (Join-Path $root 'Providers/Storage/TestProjects/NextGenSoftware.OASIS.API.Providers.PostgreSQLOASIS.IntegrationTests/NextGenSoftware.OASIS.API.Providers.PostgreSQLOASIS.IntegrationTests.csproj') --no-build --no-restore /m:2 /nr:false --filter $filter --logger "trx;LogFileName=$reportName" --results-directory $work -v:q
    if($LASTEXITCODE -ne 0 -or (Get-ChildItem -LiteralPath $work -Filter '*.pending.json')){throw 'PostgreSQL native verification incomplete; preserve original records/journals.'}
    [xml]$trx=Get-Content -LiteralPath $report -Raw
    $counts=$trx.TestRun.ResultSummary.Counters
    $expected=if($ReconcileComputedNameFixture){1}else{68}
    if((Get-Item -LiteralPath $report).LastWriteTimeUtc -lt $started -or [int]$counts.total -ne $expected -or [int]$counts.passed -ne $expected -or [int]$counts.notExecuted -ne 0){throw 'Fresh complete native report without skips required.'}
    $passed=$true
}finally{
    $env:POSTGRESQLOASIS_CONNECTIONSTRING=$oldConnection;$env:OASIS_POSTGRES_TEST_WORK=$oldWork;$secret=$null
    if($startedHere -and $passed){& wsl -d Ubuntu -u root -- podman stop --time 20 oasis-postgres}
    $lease.Dispose()
}
