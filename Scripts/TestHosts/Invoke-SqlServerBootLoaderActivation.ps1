#Requires -Version 7.0
[CmdletBinding()]
param(
    [Parameter(Mandatory)]
    [string]$HarnessAssembly
)

$ErrorActionPreference = 'Stop'
$harness = (Resolve-Path -LiteralPath $HarnessAssembly).Path
if ([IO.Path]::GetFileName($harness) -ne 'NextGenSoftware.OASIS.OASISBootLoader.TestHarness.dll') {
    throw 'Supply the built BootLoader test harness assembly.'
}

$instance = 'OASISHyperDriveRegistrationProbe'
$database = 'oasis_hyperdrive_boot_probe'
$provider = 'SQLServerDBOASIS'
$server = "(localdb)\$instance"
$repo = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
$fixture = Join-Path $repo 'TestResults/HyperDrive-Providers/SqlServerBootLoaderFixture'
$instances = @(SqlLocalDB info)
if ($LASTEXITCODE -ne 0) { throw 'Could not list LocalDB instances.' }
if ($instances -contains $instance -or (Test-Path -LiteralPath $fixture)) {
    throw 'The registration fixture already exists. Reconcile its ownership before retrying.'
}

New-Item -ItemType Directory -Path $fixture | Out-Null
$created = $false
try {
    SqlLocalDB create $instance '17.0'
    if ($LASTEXITCODE -ne 0) { throw 'LocalDB creation failed.' }
    $created = $true
    SqlLocalDB start $instance
    if ($LASTEXITCODE -ne 0) { throw 'LocalDB startup failed.' }

    $dataFile = (Join-Path $fixture 'boot-probe.mdf').Replace("'", "''")
    $logFile = (Join-Path $fixture 'boot-probe.ldf').Replace("'", "''")
    sqlcmd -S $server -E -C -b -d master -Q "CREATE DATABASE [$database] ON (NAME = N'boot_probe', FILENAME = N'$dataFile') LOG ON (NAME = N'boot_probe_log', FILENAME = N'$logFile');"
    if ($LASTEXITCODE -ne 0) { throw 'Disposable database creation failed.' }

    $storage = @{
        OASISProviderBootType = 'Warm'
        AutoFailOverEnabled = $false
        AutoFailOverLocalProvidersEnabled = $false
        AutoReplicationEnabled = $false
        AutoLoadBalanceEnabled = $false
        SQLServerDBOASIS = @{
            ConnectionString = "Server=$server;Database=$database;Integrated Security=true;TrustServerCertificate=true"
        }
    }
    foreach ($list in @(
        'AutoFailOverProviders', 'AutoFailOverLocalProviders', 'AutoReplicationProviders',
        'AutoLoadBalanceProviders', 'AutoFailOverProvidersForAvatarLogin',
        'AutoFailOverProvidersForCheckIfEmailAlreadyInUse',
        'AutoFailOverProvidersForCheckIfUsernameAlreadyInUse',
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
    dotnet $harness $dnaPath $provider
    if ($LASTEXITCODE -ne 0) { throw 'BootLoader activation harness failed.' }

    $schemaCheck = @"
SET NOCOUNT ON;
IF (SELECT COUNT(*) FROM sys.tables WHERE schema_id = SCHEMA_ID('dbo') AND name IN
    ('OASISAvatars', 'OASISAvatarDetails', 'OASISHolons',
     'OASISHolonHistory', 'OASISAvatarHistory', 'OASISAvatarDetailHistory')) <> 6
    THROW 51000, 'BootLoader activation did not create all six expected tables.', 1;
SELECT 'Verified six SQL Server tables created through activation.' AS Evidence;
"@
    sqlcmd -S $server -E -C -b -d $database -Q $schemaCheck
    if ($LASTEXITCODE -ne 0) { throw 'Independent SQL Server schema verification failed.' }
}
finally {
    if ($created) {
        SqlLocalDB stop $instance
        if ($LASTEXITCODE -ne 0) { throw 'Could not stop the owned LocalDB instance.' }
        SqlLocalDB delete $instance
        if ($LASTEXITCODE -ne 0) { throw 'Could not remove the owned LocalDB instance.' }
    }

    $resolved = (Resolve-Path -LiteralPath $fixture).Path
    if ($resolved -ne (Join-Path $repo 'TestResults/HyperDrive-Providers/SqlServerBootLoaderFixture')) {
        throw 'Unexpected fixture cleanup path.'
    }
    Remove-Item -LiteralPath $resolved -Recurse -Force
    if (Test-Path -LiteralPath $resolved) { throw 'SQL Server fixture cleanup failed.' }
    Write-Output 'Removed the isolated SQL Server activation fixture.'
}
