$ErrorActionPreference = 'Stop'

$fixture = [IO.Path]::GetFullPath($env:OASIS_BNB_CI_FIXTURE)
if ($env:GITHUB_ACTIONS -ne 'true' -or [IO.Path]::GetDirectoryName($fixture) -ne $env:RUNNER_TEMP -or
    [IO.Path]::GetFileName($fixture) -notlike 'oasis-bnb.*') {
    throw 'Requires the disposable native BNB Smart Chain CI fixture.'
}

$provider = 'BNBChainOASIS'
$settings = Get-Content -Raw -LiteralPath (Join-Path $fixture 'settings.json') | ConvertFrom-Json -AsHashtable
$storage = @{
    OASISProviderBootType = 'Warm'
    AutoFailOverEnabled = $false
    AutoFailOverLocalProvidersEnabled = $false
    AutoReplicationEnabled = $false
    AutoLoadBalanceEnabled = $false
    BNBChainOASIS = @{
        RpcEndpoint = 'http://127.0.0.1:19845'
        ChainId = '0x2ca'
        ChainPrivateKey = $settings.OASIS_BNB_CI_KEY
        ContractAddress = $settings.OASIS_BNB_CI_CONTRACT_SYNC
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
$env:OASIS_WEB3CORE_STORAGE_ENCRYPTION_KEY = $settings.OASIS_WEB3CORE_STORAGE_ENCRYPTION_KEY
& (Join-Path $PSScriptRoot 'Invoke-WithProviderBuildLease.ps1') -Owner CI-BNBChainBootLoader -Operation {
    $project = 'OASIS Architecture/NextGenSoftware.OASIS.OASISBootLoader.TestHarness/NextGenSoftware.OASIS.OASISBootLoader.TestHarness.csproj'
    dotnet build $project /m:2 /nr:false /p:UseSharedCompilation=false
    if ($LASTEXITCODE -ne 0) { throw 'BNB BootLoader harness build failed.' }
    dotnet run --no-build --no-restore --project $project -- $dnaPath $provider
    if ($LASTEXITCODE -ne 0) { throw 'Native BNB BootLoader activation failed.' }
}
