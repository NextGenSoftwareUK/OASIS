#Requires -Version 7.0
[CmdletBinding()]
param([Parameter(Mandatory)][string]$ConfigurationPath)
$ErrorActionPreference='Stop'
$root=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
$configuration=[IO.Path]::GetFullPath($ConfigurationPath)
if(!(Test-Path -LiteralPath $configuration -PathType Leaf)){throw 'Existing non-secret hosted configuration required.'}
if([string]::IsNullOrWhiteSpace($env:OASIS_HOSTED_COSMOS_AUTH_KEY)){throw 'Set the hosted auth key in the protected process environment; never pass it on the command line.'}
$work=Join-Path $root 'TestResults/HostedCosmosCurrent'
foreach($name in @('read-only-report.json','read-only-report.json.next','hosted-read-current.trx')){
    if(Test-Path -LiteralPath (Join-Path $work $name)){throw 'Preserve previous hosted outcome before another run.'}
}
$project=Join-Path $root 'Providers/Cloud/TestProjects/NextGenSoftware.OASIS.API.Providers.AzureCosmosDBOASIS.IntegrationTests/NextGenSoftware.OASIS.API.Providers.AzureCosmosDBOASIS.IntegrationTests.csproj'
& (Join-Path $PSScriptRoot 'Invoke-WithProviderBuildLease.ps1') -Owner cosmic-hosted-read -Operation {
    $priorConfig=$env:OASIS_HOSTED_COSMOS_CONFIG;$priorExecute=$env:OASIS_HOSTED_COSMOS_EXECUTE
    try{
        foreach($build in @('Providers/Cloud/NextGenSoftware.OASIS.API.Providers.AzureCosmosDBOASIS/NextGenSoftware.OASIS.API.Providers.AzureCosmosDBOASIS.csproj',$project)){
            $buildPath=if([IO.Path]::IsPathRooted($build)){$build}else{Join-Path $root $build}
            & dotnet build $buildPath --no-restore /p:BuildProjectReferences=false /m:2 /nr:false /p:UseSharedCompilation=false -v:q -clp:ErrorsOnly
            if($LASTEXITCODE){throw 'Hosted read producer/consumer build failed.'}
        }
        $env:OASIS_HOSTED_COSMOS_CONFIG=$configuration;$env:OASIS_HOSTED_COSMOS_EXECUTE='1'
        & dotnet test $project --no-build --no-restore --filter 'FullyQualifiedName~HostedCosmosReadTests.ReadExistingHostedRecordsThroughSharedQueries' --logger 'trx;LogFileName=hosted-read-current.trx' --results-directory $work /m:2 /nr:false /p:UseSharedCompilation=false
        if($LASTEXITCODE){throw 'Hosted read acceptance incomplete; preserve original evidence, no retry/fallback.'}
        [xml]$report=Get-Content -LiteralPath (Join-Path $work 'hosted-read-current.trx') -Raw
        $counts=$report.TestRun.ResultSummary.Counters
        if([int]$counts.total -ne 1 -or [int]$counts.passed -ne 1 -or [int]$counts.notExecuted -ne 0){throw 'One complete unskipped hosted acceptance case required.'}
        if(!(Test-Path -LiteralPath (Join-Path $work 'read-only-report.json'))){throw 'Hosted structured evidence missing.'}
    }finally{
        $env:OASIS_HOSTED_COSMOS_CONFIG=$priorConfig;$env:OASIS_HOSTED_COSMOS_EXECUTE=$priorExecute
    }
}
