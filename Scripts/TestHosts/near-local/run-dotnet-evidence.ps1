[CmdletBinding()]
param(
    [Parameter(Mandatory)] [string] $SettingsPath,
    [Parameter(Mandatory)] [string] $RepositoryRoot
)

$ErrorActionPreference = 'Stop'
$settings = Get-Content -LiteralPath $SettingsPath -Raw | ConvertFrom-Json
$environmentNames = @(
    'NEAROASIS_RPCENDPOINT',
    'NEAROASIS_NETWORKID',
    'NEAROASIS_CHAINID',
    'NEAROASIS_CONTRACTADDRESS',
    'NEAROASIS_ACCOUNTID',
    'NEAROASIS_PRIVATEKEY',
    'OASIS_NEAR_NODE',
    'OASIS_NEAR_SDK_BRIDGE'
)

try {
    foreach ($name in $environmentNames) {
        $value = $settings.$name
        if ([string]::IsNullOrWhiteSpace($value)) {
            throw "NEAR evidence setting '$name' is missing."
        }
        [Environment]::SetEnvironmentVariable($name, [string] $value, 'Process')
    }

    $tests = Join-Path $RepositoryRoot 'Providers/Blockchain/TestProjects/NextGenSoftware.OASIS.API.Providers.NEAROASIS.IntegrationTests/NextGenSoftware.OASIS.API.Providers.NEAROASIS.IntegrationTests.csproj'
    $results = Join-Path $RepositoryRoot 'artifacts/near-provider'
    New-Item -ItemType Directory -Path $results -Force | Out-Null
    & dotnet test $tests --configuration Release --nologo --logger "trx;LogFileName=$(Join-Path $results 'near-provider.trx')"
    if ($LASTEXITCODE -ne 0) {
        throw "NEAR provider tests failed with exit code $LASTEXITCODE."
    }
}
finally {
    foreach ($name in $environmentNames) {
        [Environment]::SetEnvironmentVariable($name, $null, 'Process')
    }
}
