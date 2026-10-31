#Requires -Version 7.0
# Export a new public source for the owned hosted URI. Never start or mutate a retained Pod.
[CmdletBinding()]
param()
$ErrorActionPreference='Stop'
$taskRoot=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
$taskOriginal=Join-Path $taskRoot 'LocalState/COSMIC/SolidCurrent'
$taskSource=Join-Path $taskRoot 'LocalState/COSMIC/HostedSolidSourceCurrent'
$taskResults=Join-Path $taskRoot 'TestResults/HostedCqlIntegrationCurrent'
$taskProject=Join-Path $taskRoot 'Providers/Network/TestProjects/NextGenSoftware.OASIS.API.Providers.SOLIDOASIS.IntegrationTests/NextGenSoftware.OASIS.API.Providers.SOLIDOASIS.IntegrationTests.csproj'
if(!(Test-Path -LiteralPath "$taskOriginal/storage-key.dpapi")){throw 'Retained original SOLID key required; do not replace it.'}
$taskLease=[IO.File]::Open("$taskOriginal/environment.lock",'OpenOrCreate','ReadWrite','None')
$taskPriorRoot=$env:OASIS_HOSTED_SOLID_SOURCE_ROOT
$taskPriorKey=$env:OASIS_HOSTED_SOLID_ORIGINAL_KEY
try {
    $env:OASIS_HOSTED_SOLID_SOURCE_ROOT=$taskSource
    $env:OASIS_HOSTED_SOLID_ORIGINAL_KEY="$taskOriginal/storage-key.dpapi"
    & "$PSScriptRoot/Invoke-WithProviderBuildLease.ps1" -Owner cosmic-hosted-solid-export -Operation {
        dotnet build $taskProject --no-restore /p:BuildProjectReferences=false /m:2 /nr:false /p:UseSharedCompilation=false -v:q *> "$taskResults/solid-host-export-build.log"
        if($LASTEXITCODE){throw 'Export build failed; no source operation started.'}
        dotnet test $taskProject --no-build --no-restore /m:2 /nr:false --filter 'Category=SolidHostedSourceExport' --logger 'trx;LogFileName=solid-host-export.trx' --results-directory $taskResults -v:q *> "$taskResults/solid-host-export-test.log"
        if($LASTEXITCODE){throw 'Export verification failed; preserve original state and logs.'}
        [xml]$taskTrx=Get-Content "$taskResults/solid-host-export.trx" -Raw
        $taskCounts=$taskTrx.TestRun.ResultSummary.Counters
        if([int]$taskCounts.total -ne 1 -or [int]$taskCounts.passed -ne 1 -or [int]$taskCounts.notExecuted -ne 0){throw 'Exactly one export control must pass without skips.'}
    }
    Write-Output 'Three retained encrypted public samples exported and verified. This is not a hosted Pod readiness result.'
} finally {
    $env:OASIS_HOSTED_SOLID_SOURCE_ROOT=$taskPriorRoot
    $env:OASIS_HOSTED_SOLID_ORIGINAL_KEY=$taskPriorKey
    $taskLease.Dispose()
}
