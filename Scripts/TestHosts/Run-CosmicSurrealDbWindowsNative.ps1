#Requires -Version 7.0
[CmdletBinding()]
param([Parameter(Mandatory)][ValidateSet('Initialize','Retained','Reader','AdmitReader')][string]$Mode)
$ErrorActionPreference = 'Stop'
$taskRoot = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
$state = Join-Path $taskRoot 'LocalState/COSMIC/RecoveryLaptopSurrealDB335'
if (!(Test-Path -LiteralPath (Join-Path $state 'runtime.json'))) { throw 'Verified official Windows runtime receipt required.' }
$source = Join-Path $state 'windows-source'
if ($Mode -eq 'Initialize' -and (Test-Path -LiteralPath $source)) { throw 'Existing source/outcome cannot be initialized again.' }
if ($Mode -ne 'Initialize' -and !(Test-Path -LiteralPath (Join-Path $source 'native-baseline.json'))) { throw 'Retained source baseline required.' }
if ($Mode -eq 'AdmitReader' -and (Test-Path -LiteralPath (Join-Path $source 'reader.json'))) { throw 'Existing reader/outcome cannot be provisioned again.' }
& (Join-Path $PSScriptRoot 'Invoke-WithProviderBuildLease.ps1') -Owner cosmic-surreal-windows-native -Operation {
    Push-Location $taskRoot
    try {
        $project = 'Providers/Storage/TestProjects/NextGenSoftware.OASIS.API.Providers.SurrealDBOASIS.IntegrationTests/NextGenSoftware.OASIS.API.Providers.SurrealDBOASIS.IntegrationTests.csproj'
        $buildLog = Join-Path $state ('build-' + [Guid]::NewGuid().ToString('N') + '.log')
        dotnet build $project /m:2 /nr:false /p:UseSharedCompilation=false *> $buildLog
        if ($LASTEXITCODE) { throw "Native fixture build failed; preserve $buildLog" }
        $nativeMode = if ($Mode -eq 'AdmitReader') { '--admit-reader' } else { '--' + $Mode.ToLowerInvariant() }
        & python -B (Join-Path $PSScriptRoot 'run-cosmic-surrealdb-windows.py') $nativeMode
        if ($LASTEXITCODE) { throw 'Native qualification failed; preserve source, pending journals and logs.' }
    }
    finally { Pop-Location }
}
