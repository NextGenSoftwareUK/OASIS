#Requires -Version 7.0
[CmdletBinding()]
param([Parameter(Mandatory)][ValidateSet('Initialize','Retained')][string]$Mode)
$ErrorActionPreference = 'Stop'
$taskRoot = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
$state = Join-Path $taskRoot 'LocalState/COSMIC/RecoveryLaptopPouchDBCql'
if (!(Test-Path -LiteralPath (Join-Path $state 'runtime.json'))) { throw 'Qualified native PouchDB runtime required.' }
if ($Mode -eq 'Initialize' -and (Test-Path -LiteralPath (Join-Path $state 'source.json'))) { throw 'Existing source cannot be initialized again.' }
if ($Mode -eq 'Retained' -and !(Test-Path -LiteralPath (Join-Path $state 'native-baseline.json'))) { throw 'Original retained baseline required.' }
& (Join-Path $PSScriptRoot 'Invoke-WithProviderBuildLease.ps1') -Owner cosmic-pouchdb-windows-native -Operation {
    Push-Location $taskRoot
    try {
        $project = 'Providers/Storage/TestProjects/NextGenSoftware.OASIS.API.Providers.PouchDBOASIS.IntegrationTests/NextGenSoftware.OASIS.API.Providers.PouchDBOASIS.IntegrationTests.csproj'
        $buildLog = Join-Path $state ('build-' + [Guid]::NewGuid().ToString('N') + '.log')
        dotnet build $project /m:2 /nr:false /p:UseSharedCompilation=false *> $buildLog
        if ($LASTEXITCODE) { throw "Native fixture build failed; preserve $buildLog" }
        & python -B (Join-Path $PSScriptRoot 'run-cosmic-pouchdb-windows.py') ('--' + $Mode.ToLowerInvariant())
        if ($LASTEXITCODE) { throw 'Native qualification failed; preserve all source/admission journals.' }
    }
    finally { Pop-Location }
}
