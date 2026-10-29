param()
$ErrorActionPreference='Stop'
$root=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
$state=Join-Path $root 'LocalState/COSMIC/PostgreSqlCql'
$results=Join-Path $root 'TestResults/PostgreSqlCqlCurrent'
New-Item -ItemType Directory -Force -Path $state,$results | Out-Null
$lease=[IO.File]::Open((Join-Path $state 'runner.lock'),[IO.FileMode]::OpenOrCreate,[IO.FileAccess]::ReadWrite,[IO.FileShare]::None)
$oldConnection=$env:OASIS_POSTGRES_CQL_CONNECTION
$oldState=$env:OASIS_POSTGRES_CQL_STATE
$lifecycle=@{Started=$false;Passed=$false};$secret=$null
try {
    if(Test-Path -LiteralPath (Join-Path $state 'demo-seed.pending.json')){throw 'Original setup is pending; inspect without replay.'}
    $identity=(& wsl -d Ubuntu -u root -- podman inspect oasis-postgres | ConvertFrom-Json)[0]
    if($LASTEXITCODE -or $identity.Id -ne 'cee58d44cc44aace8863ca53ffa955deed78a710a9800b750a484bb478206a69' -or $identity.Image -ne '79bd7c99e923138f136f8009d6bffa66e21e9d4fda5c0c561b00fc9c90cfe537'){throw 'Exact retained PostgreSQL container/image required.'}
    $ports=@($identity.HostConfig.PortBindings.'5432/tcp')
    if($ports.Count -ne 1 -or $ports[0].HostIp -ne '127.0.0.1' -or $ports[0].HostPort -ne '15432'){throw 'Expected loopback binding required.'}
    if(@($identity.Mounts | Where-Object {$_.Type -eq 'volume' -and $_.Name -eq 'oasis-postgres-data' -and $_.Destination -eq '/var/lib/postgresql/data'}).Count -ne 1){throw 'Retained data volume required.'}
    & (Join-Path $PSScriptRoot 'Invoke-WithProviderBuildLease.ps1') -Owner 'COSMIC PostgreSQL CQL native' -Operation {
        $project=Join-Path $root 'Providers/Storage/TestProjects/NextGenSoftware.OASIS.API.Providers.PostgreSQLOASIS.IntegrationTests'
        & dotnet build $project --no-restore /m:2 /nr:false /p:UseSharedCompilation=false /p:BuildProjectReferences=false *> (Join-Path $results 'native-build.log')
        if($LASTEXITCODE){throw 'Cached native consumer build failed.'}
        if($identity.State.Status -ne 'exited'){throw 'Require stopped retained container for exclusive native acceptance.'}
        $lifecycle.Started=$true
        & wsl -d Ubuntu -u root -- podman start oasis-postgres | Out-Null
        if($LASTEXITCODE){throw 'Retained PostgreSQL start failed.'}
        $deadline=[DateTime]::UtcNow.AddSeconds(30)
        do {
            $health=(& wsl -d Ubuntu -u root -- podman inspect --format '{{.State.Health.Status}}' oasis-postgres).Trim()
            if($LASTEXITCODE){throw 'Health observation failed; do not restart.'}
            if($health -eq 'healthy'){break}
            if($health -ne 'starting'){throw 'Retained service unhealthy.'}
            Start-Sleep -Milliseconds 500
        } while([DateTime]::UtcNow -lt $deadline)
        if($health -ne 'healthy'){throw 'Readiness timed out; preserve original.'}
        $secret=(& wsl -d Ubuntu -u root -- podman secret inspect --showsecret oasis-postgres-pwd | ConvertFrom-Json)[0].SecretData
        if([string]::IsNullOrWhiteSpace($secret)){throw 'Protected test credential required.'}
        $env:OASIS_POSTGRES_CQL_CONNECTION="Host=127.0.0.1;Port=15432;Database=oasis_provider_test;Username=oasis_test;Password=$secret;Search Path=cosmic_cql,pg_catalog;Timeout=10;Command Timeout=15;Pooling=false"
        $env:OASIS_POSTGRES_CQL_STATE=$state
        & dotnet test $project --no-build --no-restore /m:2 /nr:false /p:UseSharedCompilation=false --filter 'FullyQualifiedName~PostgreSqlCqlNativeTests' --results-directory $results --logger 'trx;LogFileName=postgres-cql-native.trx' *> (Join-Path $results 'native.log')
        if($LASTEXITCODE){throw 'Native acceptance failed; preserve source/journals.'}
        [xml]$report=Get-Content -LiteralPath (Join-Path $results 'postgres-cql-native.trx') -Raw
        if([int]$report.TestRun.ResultSummary.Counters.passed -ne 1 -or [int]$report.TestRun.ResultSummary.Counters.total -ne 1){throw 'One passed native test without skips required.'}
        $lifecycle.Passed=$true
    }
} finally {
    $env:OASIS_POSTGRES_CQL_CONNECTION=$oldConnection;$env:OASIS_POSTGRES_CQL_STATE=$oldState;$secret=$null
    if($lifecycle.Started -and $lifecycle.Passed){& wsl -d Ubuntu -u root -- podman stop --time 20 oasis-postgres | Out-Null;if($LASTEXITCODE){throw 'Owned container stop failed.'}}
    $lease.Dispose()
}
