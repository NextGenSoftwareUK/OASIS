#Requires -Version 7.5
[CmdletBinding()]
param(
    [Parameter(Mandatory)][string]$ConfigurationPath,
    [ValidateSet('Ethereum','Polygon','Arbitrum')][string]$Provider='Ethereum',
    [switch]$Federated,
    [switch]$Serve
)
$ErrorActionPreference='Stop'
if($Serve -and !$Federated){throw 'Serve requires the explicit Mongo-paired preset.'}
$root=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
$config=[IO.Path]::GetFullPath($ConfigurationPath)
if(!(Test-Path -LiteralPath $config -PathType Leaf)){throw 'An explicit existing V2 source configuration is required; no legacy seed is adopted.'}
$source=Get-Content -LiteralPath $config -Raw|ConvertFrom-Json
if($source.Provider -cne $Provider){throw 'Selected provider must match the explicit source configuration.'}
if(![Environment]::GetEnvironmentVariable('OASIS_WEB3CORE_STORAGE_ENCRYPTION_KEY')){throw 'Configure the original V2 key in the process environment; never pass it as an argument.'}
$results=Join-Path $root ('TestResults/EvmV2ConfiguredCurrent/'+$Provider)
[void][IO.Directory]::CreateDirectory($results)
if(Test-Path -LiteralPath (Join-Path $results 'native-read.trx')){throw 'Preserve/reconcile the original result before another native dispatch.'}

# Existing source only: no node startup, key creation, contract deployment,
# writes, seed migration or fallback to the different legacy Ethereum ABI.
& (Join-Path $PSScriptRoot 'Invoke-WithProviderBuildLease.ps1') -Owner 'cosmic-configured-evm-v2-demo' -Operation {
    $names=@('OASIS_EVM_V2_CONFIGURED_READ','OASIS_EVM_V2_SOURCE_CONFIG','OASIS_EVM_V2_CONFIGURED_FEDERATED','OASIS_EVM_V2_CONFIGURED_SERVE')
    $prior=@{};foreach($name in $names){$prior[$name]=[Environment]::GetEnvironmentVariable($name)}
    try {
        $env:OASIS_EVM_V2_CONFIGURED_READ='1'
        $env:OASIS_EVM_V2_SOURCE_CONFIG=$config
        $env:OASIS_EVM_V2_CONFIGURED_FEDERATED=if($Federated){'1'}else{'0'}
        $env:OASIS_EVM_V2_CONFIGURED_SERVE=if($Serve){'1'}else{'0'}
        $project=Join-Path $root 'Providers/Blockchain/TestProjects/NextGenSoftware.OASIS.API.Providers.Web3CoreOASIS.UnitTests/NextGenSoftware.OASIS.API.Providers.Web3CoreOASIS.UnitTests.csproj'
        if($Serve){
            $port=@{Ethereum=5097;Polygon=5098;Arbitrum=5099}[$Provider]
            Write-Output "Read-only V2 Mongo teaching host: http://127.0.0.1:$port/ (after native admission). Existing records are preserved."
        }
        & dotnet test $project --no-build --no-restore --filter 'FullyQualifiedName~ExplicitConfiguredV2SourceRunsReadOnlyTeachingConsumer' --results-directory $results --logger 'trx;LogFileName=native-read.trx' *> (Join-Path $results 'native-read.log')
        if($LASTEXITCODE){throw 'Configured V2 read incomplete; preserve the original source and result.'}
        [xml]$proof=Get-Content -LiteralPath (Join-Path $results 'native-read.trx') -Raw
        if($proof.TestRun.ResultSummary.Counters.total -ne '1' -or $proof.TestRun.ResultSummary.Counters.passed -ne '1'){throw 'Exactly one unskipped native pass required.'}
    }
    finally {foreach($name in $names){[Environment]::SetEnvironmentVariable($name,$prior[$name])}}
}
