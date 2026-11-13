#Requires -Version 7.0
[CmdletBinding()]
param()
$ErrorActionPreference = 'Stop'
$root = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
$state = Join-Path $root 'LocalState/COSMIC/EvmCql/Arbitrum'
$results = Join-Path $root 'TestResults/Web3CoreCqlCurrent'

# This launcher owns no node or key setup. Reuse the original compatible V2
# chain on loopback18547, chain31338, and its configured decryption key.
# It never starts Ganache, decrypts/replaces a retained key, deploys or seeds.
if (!(Test-Path -LiteralPath (Join-Path $state 'demo-seed.json'))) { throw 'Original Arbitrum V2 seed required.' }
if (Test-Path -LiteralPath (Join-Path $state 'demo-seed.pending.json')) { throw 'Reconcile original pending setup before reading.' }
if (![Environment]::GetEnvironmentVariable('OASIS_WEB3CORE_STORAGE_ENCRYPTION_KEY')) { throw 'Configure the original V2 storage key in the process environment; never paste it into docs or arguments.' }
if (!(Get-NetTCPConnection -State Listen -LocalPort 18547 -ErrorAction SilentlyContinue)) { throw 'The retained compatible V2 node must already be running on loopback18547.' }
[void][IO.Directory]::CreateDirectory($results)
$report = Join-Path $results 'owned-cql-native-read.trx'
if (Test-Path -LiteralPath $report) { throw 'Preserve/reconcile the original native read outcome before dispatching again.' }

& (Join-Path $PSScriptRoot 'Invoke-WithProviderBuildLease.ps1') -Owner 'cosmic-evm-v2-owned-read' -Operation {
    $previous = $env:OASIS_EVM_V2_READ_ONLY
    $previousState = $env:OASIS_EVM_V2_READ_STATE
    try {
        $env:OASIS_EVM_V2_READ_ONLY = '1'
        $env:OASIS_EVM_V2_READ_STATE = $state
        $project = Join-Path $root 'Providers/Blockchain/TestProjects/NextGenSoftware.OASIS.API.Providers.Web3CoreOASIS.UnitTests/NextGenSoftware.OASIS.API.Providers.Web3CoreOASIS.UnitTests.csproj'
        & dotnet test $project --no-build --no-restore --filter 'FullyQualifiedName~ExistingArbitrumV2RecordsRunManagerLinqThreeToTwoWithoutWrites' --results-directory $results --logger 'trx;LogFileName=owned-cql-native-read.trx' *> (Join-Path $results 'owned-cql-native-read.log')
        if ($LASTEXITCODE) { throw 'Native V2 read incomplete; preserve report and original environment.' }
        [xml]$proof = Get-Content -LiteralPath $report -Raw
        if ($proof.TestRun.ResultSummary.Counters.total -ne '1' -or $proof.TestRun.ResultSummary.Counters.passed -ne '1') { throw 'Exactly one passing native V2 case required; skips are not acceptance.' }
    }
    finally {
        $env:OASIS_EVM_V2_READ_ONLY = $previous
        $env:OASIS_EVM_V2_READ_STATE = $previousState
    }
}
