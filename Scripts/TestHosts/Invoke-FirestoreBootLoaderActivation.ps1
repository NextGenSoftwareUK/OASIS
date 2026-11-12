$ErrorActionPreference = 'Stop'

$fixture = [IO.Path]::GetFullPath($env:OASIS_FIRESTORE_CI_FIXTURE)
if ($env:GITHUB_ACTIONS -ne 'true' -or [IO.Path]::GetDirectoryName($fixture) -ne $env:RUNNER_TEMP -or
    [IO.Path]::GetFileName($fixture) -notlike 'oasis-firestore.*') {
    throw 'Requires the disposable Firestore emulator CI fixture.'
}

$provider = 'GoogleCloudOASIS'
$storage = @{
    OASISProviderBootType = 'Warm'
    AutoFailOverEnabled = $false
    AutoFailOverLocalProvidersEnabled = $false
    AutoReplicationEnabled = $false
    AutoLoadBalanceEnabled = $false
    GoogleCloudOASIS = @{
        ProjectId = 'demo-oasis-firestore-startup'
        EnableFirestore = $true
        EnableStorage = $false
        EnableBigQuery = $false
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
$env:OASIS_FIRESTORE_BOOTLOADER_TEST = '1'
$env:OASIS_FIRESTORE_BOOTLOADER_DNA = $dnaPath
& (Join-Path $PSScriptRoot 'Invoke-WithProviderBuildLease.ps1') -Owner CI-FirestoreBootLoader -Operation {
    $project = 'OASIS Architecture/NextGenSoftware.OASIS.OASISBootLoader.UnitTests/NextGenSoftware.OASIS.OASISBootLoader.UnitTests.csproj'
    dotnet test $project --filter 'FullyQualifiedName~FirestoreNativeStartupTests' --logger 'trx;LogFileName=firestore-startup.trx' --results-directory $fixture /m:2 /nr:false /p:UseSharedCompilation=false
    if ($LASTEXITCODE -ne 0) { throw 'Native Firestore BootLoader check failed.' }
    [xml]$report = Get-Content -Raw -LiteralPath (Join-Path $fixture 'firestore-startup.trx')
    $counters = $report.SelectSingleNode('//*[local-name()="Counters"]')
    if (-not $counters -or $counters.total -ne '1' -or $counters.passed -ne '1') {
        throw 'Expected one executed and passing native Firestore startup test.'
    }
}
