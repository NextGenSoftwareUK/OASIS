$ErrorActionPreference = 'Stop'

$fixture = [IO.Path]::GetFullPath($env:OASIS_POLKADOT_CI_FIXTURE)
if ($env:GITHUB_ACTIONS -ne 'true' -or [IO.Path]::GetDirectoryName($fixture) -ne $env:RUNNER_TEMP -or
    [IO.Path]::GetFileName($fixture) -notlike 'oasis-polkadot-chain.*' -or
    $env:OASIS_POLKADOT_FRESH_STORAGE -ne '1') {
    throw 'Requires the disposable native Polkadot CI fixture.'
}

$provider = 'PolkadotOASIS'
$identityPath = Join-Path $env:OASIS_POLKADOT_RECORD_EVIDENCE 'fresh-record-store.json'
$identity = Get-Content -Raw -LiteralPath $identityPath | ConvertFrom-Json
if ($identity.deploymentVerified -ne $true -or
    $identity.genesisHash -ne '0xe8cf5d7d636a08721efaa17acc52ed3d895fe2468dccb2520cc8e249ddb6b208' -or
    -not $identity.contractAccountId -or -not $identity.contractCodeHash -or -not $identity.ownerAccountId) {
    throw 'Polkadot fixture deployment identity is missing or invalid.'
}

$storage = @{
    OASISProviderBootType = 'Warm'
    AutoFailOverEnabled = $false
    AutoFailOverLocalProvidersEnabled = $false
    AutoReplicationEnabled = $false
    AutoLoadBalanceEnabled = $false
    PolkadotOASIS = @{
        RpcEndpoint = 'http://127.0.0.1:19944'
        GenesisHash = $identity.genesisHash
        ChainPrivateKey = '0x' + ('02' * 32)
        StorageContractAccountId = $identity.contractAccountId
        StorageCodeHash = $identity.contractCodeHash
        StorageOwnerAccountId = $identity.ownerAccountId
    }
}
foreach ($list in @(
    'AutoFailOverProviders', 'AutoFailOverLocalProviders', 'AutoReplicationProviders',
    'AutoLoadBalanceProviders', 'AutoFailOverProvidersForAvatarLogin',
    'AutoFailOverProvidersForCheckIfEmailAlreadyInUse', 'AutoFailOverProvidersForCheckIfUsernameAlreadyInUse',
    'AutoFailOverProvidersForCheckIfOASISSystemAccountExists'
)) {
    $storage[$list] = $provider
}
$dna = @{
    OASIS = @{
        HyperDriveMode = 'OASISHyperDrive2'
        Logging = @{ LoggingFramework = 'Default'; LogToConsole = $false; LogToFile = $false }
        ErrorHandling = @{ LogAllErrors = $false; LogAllWarnings = $false }
        Security = @{ SecretKey = [Convert]::ToHexString([Security.Cryptography.RandomNumberGenerator]::GetBytes(32)) }
        StorageProviders = $storage
        OASISHyperDriveConfig = @{
            AutoFailoverEnabled = $false
            AutoReplicationEnabled = $false
            AutoLoadBalancingEnabled = $false
            EnabledProviders = @($provider)
            AutoFailoverProviders = @($provider)
            AutoReplicationProviders = @($provider)
            LoadBalancingProviders = @($provider)
        }
    }
}
$dnaPath = Join-Path $fixture 'fixture-dna.json'
$dna | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath $dnaPath
chmod 600 $dnaPath
if ($LASTEXITCODE -ne 0) { throw 'Could not restrict fixture DNA permissions.' }
& (Join-Path $PSScriptRoot 'Invoke-WithProviderBuildLease.ps1') -Owner CI-PolkadotBootLoader -Operation {
    $project = 'OASIS Architecture/NextGenSoftware.OASIS.OASISBootLoader.TestHarness/NextGenSoftware.OASIS.OASISBootLoader.TestHarness.csproj'
    dotnet build $project /m:2 /nr:false /p:UseSharedCompilation=false
    if ($LASTEXITCODE -ne 0) { throw 'Polkadot BootLoader harness build failed.' }
    dotnet run --no-build --no-restore --project $project -- $dnaPath $provider
    if ($LASTEXITCODE -ne 0) { throw 'Native Polkadot BootLoader activation failed.' }
}
