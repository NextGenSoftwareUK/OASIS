$ErrorActionPreference = 'Stop'

$fixture = [IO.Path]::GetFullPath($env:OASIS_TRON_CI_FIXTURE)
if ($env:GITHUB_ACTIONS -ne 'true' -or [IO.Path]::GetDirectoryName($fixture) -ne $env:RUNNER_TEMP -or
    [IO.Path]::GetFileName($fixture) -notlike 'oasis-tron.*' -or
    $env:OASIS_TRON_LIFECYCLE_NETWORK -ne 'local' -or
    $env:OASIS_TRON_LIFECYCLE_GENESIS -ne '0000000000000000c93baa76a4a508f798a96f59156d9eb17ecede8ec845df2f') {
    throw 'Requires the disposable native TRON TRE CI fixture.'
}

$provider = 'TRONOASIS'
$identity = Get-Content -Raw -LiteralPath (Join-Path $fixture 'reports/bootloader-contract.json') | ConvertFrom-Json
if (-not $identity.Contract -or -not $identity.Owner) { throw 'TRON fixture contract identity is missing.' }

$storage = @{
    OASISProviderBootType = 'Warm'
    AutoFailOverEnabled = $false
    AutoFailOverLocalProvidersEnabled = $false
    AutoReplicationEnabled = $false
    AutoLoadBalanceEnabled = $false
    TRONOASIS = @{
        RpcEndpoint = $env:OASIS_TRON_LIFECYCLE_RPC
        Network = $env:OASIS_TRON_LIFECYCLE_NETWORK
        ChainId = $env:OASIS_TRON_LIFECYCLE_CHAIN_ID
        FullNodeGrpcEndpoint = $env:OASIS_TRON_LIFECYCLE_FULL_GRPC
        SolidityNodeGrpcEndpoint = $env:OASIS_TRON_LIFECYCLE_SOLIDITY_GRPC
        GenesisBlockId = $env:OASIS_TRON_LIFECYCLE_GENESIS
        StorageContractAddress = $identity.Contract
        StorageOwnerAddress = $identity.Owner
        StorageSigningKeyEnvironmentVariable = 'OASIS_TRON_STORAGE_PRIVATE_KEY'
        StorageEncryptionKeyEnvironmentVariable = 'OASIS_TRON_STORAGE_ENCRYPTION_KEY'
        ContractFeeLimitSun = 1000000000
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
& (Join-Path $PSScriptRoot 'Invoke-WithProviderBuildLease.ps1') -Owner CI-TronBootLoader -Operation {
    $project = 'OASIS Architecture/NextGenSoftware.OASIS.OASISBootLoader.TestHarness/NextGenSoftware.OASIS.OASISBootLoader.TestHarness.csproj'
    dotnet build $project /m:2 /nr:false /p:UseSharedCompilation=false
    if ($LASTEXITCODE -ne 0) { throw 'TRON BootLoader harness build failed.' }
    dotnet run --no-build --no-restore --project $project -- $dnaPath $provider
    if ($LASTEXITCODE -ne 0) { throw 'Native TRON BootLoader activation failed.' }
}

