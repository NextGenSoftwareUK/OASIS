$ErrorActionPreference = 'Stop'

$fixture = [IO.Path]::GetFullPath($env:OASIS_HOLO_LINUX_CI_FIXTURE)
if ($env:GITHUB_ACTIONS -ne 'true' -or [IO.Path]::GetDirectoryName($fixture) -ne $env:RUNNER_TEMP -or
    [IO.Path]::GetFileName($fixture) -notlike 'oasis-holo-linux.*') {
    throw 'Requires the disposable Linux HoloOASIS auto-start CI fixture.'
}

$provider = 'HoloOASIS'
$storage = @{
    OASISProviderBootType = 'Warm'
    AutoFailOverEnabled = $false
    AutoFailOverLocalProvidersEnabled = $false
    AutoReplicationEnabled = $false
    AutoLoadBalanceEnabled = $false
    HoloOASIS = @{
        LocalNodeURI = 'ws://localhost:8888'
        UseLocalNode = $true
        UseHoloNetwork = $false
        HoloNETORMUseReflection = $false
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
& (Join-Path $PSScriptRoot 'Invoke-WithProviderBuildLease.ps1') -Owner CI-HoloOASISLinuxBootLoader -Operation {
    $project = 'OASIS Architecture/NextGenSoftware.OASIS.OASISBootLoader.TestHarness/NextGenSoftware.OASIS.OASISBootLoader.TestHarness.csproj'
    dotnet build $project /m:2 /nr:false /p:UseSharedCompilation=false
    if ($LASTEXITCODE -ne 0) { throw 'HoloOASIS BootLoader harness build failed.' }
    $project = (Resolve-Path -LiteralPath $project).Path
    Push-Location -LiteralPath $fixture
    try {
        dotnet run --no-build --no-restore --no-launch-profile --project $project -- $dnaPath $provider
        if ($LASTEXITCODE -ne 0) { throw 'Native HoloOASIS BootLoader activation failed.' }
    }
    finally { Pop-Location }
    $conductor = Join-Path $env:HOLONET_CONDUCTOR_FOLDER '0.7.1/holochain'
    if (-not (Test-Path -LiteralPath $conductor -PathType Leaf)) {
        throw 'The SDK did not acquire the expected Linux conductor in the empty fixture cache.'
    }
    $version = & $conductor --version
    if ($LASTEXITCODE -ne 0 -or $version -notmatch '\b0\.7\.1\b') {
        throw 'The SDK-acquired conductor did not report version 0.7.1.'
    }
    Write-Output "SDK acquired and started $version"
}
