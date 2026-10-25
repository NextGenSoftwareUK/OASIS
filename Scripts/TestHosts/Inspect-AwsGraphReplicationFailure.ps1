#Requires -Version 7.0
$ErrorActionPreference='Stop'
$awsInspectRoot=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
$awsInspectWork=Join-Path $awsInspectRoot 'TestResults/DynamoHistoryCurrent'
$awsInspectProject=Join-Path $awsInspectRoot 'Providers/Cloud/TestProjects/NextGenSoftware.OASIS.API.Providers.AWSOASIS.IntegrationTests/NextGenSoftware.OASIS.API.Providers.AWSOASIS.IntegrationTests.csproj'
if(Test-Path (Join-Path $awsInspectWork 'aws-generated-graph-original-inspection.json')){throw 'Preserve original inspection proof; do not overwrite.'}
if(Test-Path (Join-Path $awsInspectWork 'aws-generated-graph-original-inspection.trx')){throw 'Preserve original inspection report; do not overwrite.'}
& (Join-Path $PSScriptRoot 'Invoke-WithProviderBuildLease.ps1') -Owner 'Read-only original AWS graph inspection' -Operation {
    $awsInspectLease=[IO.File]::Open('C:\Source\OASIS-hyperdrive-v2-gaps\TestResults\AWSProviderEvidence\environment.lock',[IO.FileMode]::Open,[IO.FileAccess]::ReadWrite,[IO.FileShare]::None)
    $awsInspectOwned=$null;$awsInspectPreviousEndpoint=$env:DYNAMODB_SERVICE_URL;$awsInspectPreviousWork=$env:OASIS_DYNAMO_HISTORY_WORK
    try{
        & dotnet build $awsInspectProject --no-restore /p:BuildProjectReferences=false /m:2 /nr:false /p:UseSharedCompilation=false -v:q -clp:ErrorsOnly
        if($LASTEXITCODE -ne 0){throw 'Cached inspection build failed.'}
        $awsInspectOwned=& (Join-Path $PSScriptRoot 'Use-ReusableDynamoContainer.ps1')
        $env:DYNAMODB_SERVICE_URL='http://127.0.0.1:18000';$env:OASIS_DYNAMO_HISTORY_WORK=$awsInspectWork
        & dotnet test $awsInspectProject --no-build --no-restore --filter 'FullyQualifiedName~AWSNativeRunLifecycle.ReadOnlyInspectOriginalGeneratedGraphReplicationFailure' --logger 'trx;LogFileName=aws-generated-graph-original-inspection.trx' --results-directory $awsInspectWork /m:2 /nr:false
        if($LASTEXITCODE -ne 0){throw 'Original inspection failed; preserve all original evidence, no replay/cleanup.'}
    }finally{
        $env:DYNAMODB_SERVICE_URL=$awsInspectPreviousEndpoint;$env:OASIS_DYNAMO_HISTORY_WORK=$awsInspectPreviousWork
        try{if($awsInspectOwned){& (Join-Path $PSScriptRoot 'Use-ReusableDynamoContainer.ps1') -Stop}}finally{$awsInspectLease.Dispose()}
    }
}
