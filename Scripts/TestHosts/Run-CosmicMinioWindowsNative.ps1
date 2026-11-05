#Requires -Version 7.0
[CmdletBinding()]
param([Parameter(Mandatory)][ValidateSet('Initialize','Retained')][string]$Mode)
$ErrorActionPreference = 'Stop'
$taskRoot = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
$state = Join-Path $taskRoot 'LocalState/COSMIC/RecoveryLaptopMinIO20250907'
if (!(Test-Path -LiteralPath (Join-Path $state 'preflight-stopped.json'))) { throw 'Qualified native MinIO environment required.' }
if ($Mode -eq 'Initialize' -and (Test-Path -LiteralPath (Join-Path $state 'source.json'))) { throw 'Existing source/outcome cannot be initialized again.' }
if ($Mode -eq 'Retained' -and !(Test-Path -LiteralPath (Join-Path $state 'bootstrap.ack.json'))) { throw 'Acknowledged retained source required.' }
& (Join-Path $PSScriptRoot 'Invoke-WithProviderBuildLease.ps1') -Owner cosmic-minio-windows-native -Operation {
    Push-Location $taskRoot
    try {
        $project = 'Providers/Storage/TestProjects/NextGenSoftware.OASIS.API.Providers.MinIOOASIS.IntegrationTests/NextGenSoftware.OASIS.API.Providers.MinIOOASIS.IntegrationTests.csproj'
        $buildLog = Join-Path $state ('build-' + [Guid]::NewGuid().ToString('N') + '.log')
        dotnet build $project /m:2 /nr:false /p:UseSharedCompilation=false *> $buildLog
        if ($LASTEXITCODE) { throw "Native fixture build failed; preserve $buildLog" }
        & python -B (Join-Path $PSScriptRoot 'run-cosmic-minio-windows.py') ('--' + $Mode.ToLowerInvariant())
        if ($LASTEXITCODE) { throw 'Native qualification failed; preserve all source, credentials and pending outcomes.' }
    }
    finally { Pop-Location }
}
