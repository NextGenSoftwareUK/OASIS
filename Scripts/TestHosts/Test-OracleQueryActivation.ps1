#Requires -Version 7.0
[CmdletBinding()]
param()
$ErrorActionPreference = 'Stop'
$root = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
$results = Join-Path $root 'TestResults/HostedCqlIntegrationCurrent'
$seed = Join-Path $root 'LocalState/COSMIC/OracleContentCql/seed.json'
if (!(Test-Path -LiteralPath $seed)) { throw 'Retained source seed required; never reconstruct it.' }
$lease = [IO.File]::Open((Join-Path $root 'TestResults/OracleCurrent/environment.lock'),
    [IO.FileMode]::OpenOrCreate, [IO.FileAccess]::ReadWrite, [IO.FileShare]::None)
$oldConnection = $env:ORACLEDBOASIS_CONNECTIONSTRING
$oldSeed = $env:OASIS_ORACLE_CQL_SEED
$startedHere = $false
$identity = 'e8861e296e101147681c4e2f0a3115e870cafffbce5f92848eb75d8cf25d0982'
try {
    $state = (& wsl -d Ubuntu -u root -- podman inspect --format '{{json .}}' oasis-oracle-free) | ConvertFrom-Json
    if ($LASTEXITCODE -or $state.Id -ne $identity -or
        $state.Image -ne '358b32468eddc329751c28007a535cbc661f1090ab5d8a9f02267cf44ef1e3b9') {
        throw 'Retained Oracle identity differs; no start or replacement.'
    }
    if ($state.State.Status -eq 'exited') {
        & wsl -d Ubuntu -u root -- podman start oasis-oracle-free | Out-Null
        if ($LASTEXITCODE) { throw 'Oracle start outcome requires inspection; never reset it.' }
        $startedHere = $true
    } elseif ($state.State.Status -ne 'running') { throw 'Unexpected Oracle lifecycle state.' }

    $deadline = [DateTime]::UtcNow.AddSeconds(50)
    do {
        $health = (& wsl -d Ubuntu -u root -- podman inspect --format '{{.State.Health.Status}}' oasis-oracle-free).Trim()
        if ($LASTEXITCODE) { throw 'Oracle health inspection failed.' }
        if ($health -eq 'healthy') { break }
        if ($health -ne 'starting') { throw 'Retained Oracle health failed; no restart.' }
        Start-Sleep -Milliseconds 500
    } while ([DateTime]::UtcNow -lt $deadline)
    if ($health -ne 'healthy') { throw 'Oracle readiness timed out; no test sent.' }

    # Reuse the original secret in memory only. Never print or persist it.
    $secret = ((& wsl -d Ubuntu -u root -- podman secret inspect --showsecret oasis-oracle-pwd) | ConvertFrom-Json)[0].SecretData
    if ($LASTEXITCODE -or [string]::IsNullOrWhiteSpace($secret)) { throw 'Retained secret unavailable.' }
    $env:ORACLEDBOASIS_CONNECTIONSTRING = "User Id=SYSTEM;Password=$secret;Data Source=127.0.0.1:11521/FREEPDB1;Connection Timeout=10;Pooling=true;Min Pool Size=0;Max Pool Size=16"
    $env:OASIS_ORACLE_CQL_SEED = $seed
    & dotnet test (Join-Path $root 'Providers/Storage/TestProjects/NextGenSoftware.OASIS.API.Providers.OracleDBOASIS.IntegrationTests/NextGenSoftware.OASIS.API.Providers.OracleDBOASIS.IntegrationTests.csproj') `
        --no-build --no-restore --filter 'TestCategory=OracleQueryActivationNative' `
        --results-directory $results --logger 'trx;LogFileName=oracle-query-activation-native.trx' `
        /m:2 /nr:false /p:UseSharedCompilation=false
    if ($LASTEXITCODE) { throw 'Native Oracle query activation failed; preserve source and report.' }
} finally {
    $secret = $null
    $env:ORACLEDBOASIS_CONNECTIONSTRING = $oldConnection
    $env:OASIS_ORACLE_CQL_SEED = $oldSeed
    try { if ($startedHere) {
        $observed = (& wsl -d Ubuntu -u root -- podman inspect --format '{{.Id}}' oasis-oracle-free).Trim()
        if ($LASTEXITCODE -or $observed -ne $identity) { throw 'Runtime ownership changed; no stop attempted.' }
        & wsl -d Ubuntu -u root -- podman stop --time 20 oasis-oracle-free | Out-Null
        if ($LASTEXITCODE) { throw 'Owned Oracle stop outcome requires inspection.' }
        $stopped = (& wsl -d Ubuntu -u root -- podman inspect --format '{{.State.Status}}' oasis-oracle-free).Trim()
        if ($LASTEXITCODE -or $stopped -ne 'exited') { throw 'Oracle teardown was not observed.' }
    } } finally { $lease.Dispose() }
}
