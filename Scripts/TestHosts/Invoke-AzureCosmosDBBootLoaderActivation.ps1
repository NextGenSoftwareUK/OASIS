$ErrorActionPreference = 'Stop'

$fixture = [IO.Path]::GetFullPath($env:OASIS_AZURE_COSMOS_CI_FIXTURE)
if ($env:GITHUB_ACTIONS -ne 'true' -or [IO.Path]::GetDirectoryName($fixture) -ne $env:RUNNER_TEMP -or
    [IO.Path]::GetFileName($fixture) -notlike 'oasis-azure-cosmos.*') {
    throw 'Requires the disposable Azure Cosmos DB emulator CI fixture.'
}

$provider = 'AzureCosmosDBOASIS'
$storage = @{
    OASISProviderBootType = 'Warm'
    AutoFailOverEnabled = $false
    AutoFailOverLocalProvidersEnabled = $false
    AutoReplicationEnabled = $false
    AutoLoadBalanceEnabled = $false
    AzureCosmosDBOASIS = @{
        ServiceEndpoint = 'https://localhost:8081'
        # Published development key for the disposable Microsoft emulator.
        AuthKey = 'C2y6yDjf5/R+ob0N8A7Cgv30VRDJIWEHLM+4QDU5DE2nQ9nDuVTqobD4b8mGGyPMbIZnqyMsEcaGQy67XIw/Jw=='
        DBName = 'oasis-bootloader-ci'
        CollectionNames = 'avatarItems,avatarDetailItems,holonItems'
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
& (Join-Path $PSScriptRoot 'Invoke-WithProviderBuildLease.ps1') -Owner CI-AzureCosmosDBBootLoader -Operation {
    $project = 'OASIS Architecture/NextGenSoftware.OASIS.OASISBootLoader.TestHarness/NextGenSoftware.OASIS.OASISBootLoader.TestHarness.csproj'
    dotnet build $project /m:2 /nr:false /p:UseSharedCompilation=false
    if ($LASTEXITCODE -ne 0) { throw 'Azure Cosmos DB BootLoader harness build failed.' }
    dotnet run --no-build --no-restore --project $project -- $dnaPath $provider
    if ($LASTEXITCODE -ne 0) { throw 'Native Azure Cosmos DB BootLoader activation failed.' }
}
