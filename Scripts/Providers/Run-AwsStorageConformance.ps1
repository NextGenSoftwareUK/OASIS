#Requires -Version 7.0
[CmdletBinding()]
param([switch]$DynamoOnly,[switch]$ArchiveConfigurationReports,[switch]$InitializeSyntheticStore,[ValidateSet('configuration','activation')][string]$ArchiveReportStage='configuration')
$ErrorActionPreference='Stop'
$root=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
$awsSelectedNames=if($DynamoOnly){@('AWSOASIS','DynamoDBOASIS')}else{@('AWSOASIS','DynamoDBOASIS','AWSS3OASIS')}
if($ArchiveConfigurationReports){
    & python -B (Join-Path $root 'Scripts/Providers/archive_configuration_reports.py') --providers @awsSelectedNames --stage $ArchiveReportStage --check-only
    if($LASTEXITCODE){throw 'Original AWS configuration evidence changed.'}
}
foreach($name in $awsSelectedNames){
    foreach($mode in @('async','sync')){
        $report=Join-Path $root ('TestResults/ProviderConformanceCurrent/'+$name+'/'+$mode+'/report.json')
        if(!$ArchiveConfigurationReports -and ((Test-Path -LiteralPath $report) -or (Test-Path -LiteralPath ($report+'.next')))){throw 'Review preserved AWS conformance evidence before another run.'}
    }
}
$results=Join-Path $root 'TestResults/ProviderConformanceCurrent'
$awsNativeReport=if($DynamoOnly){'dynamo-core-current.trx'}else{'aws-family-current.trx'}
foreach($name in @($awsNativeReport,'aws-factory-current.trx')){if(Test-Path -LiteralPath (Join-Path $results $name)){throw 'Preserve the prior AWS conformance/factory TRX.'}}
& (Join-Path $root 'Scripts/TestHosts/Invoke-WithProviderBuildLease.ps1') -Owner 'AWS document/object canonical storage breadth' -Operation {
    $projects=@(
        'Providers/Shared/NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage/NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage.csproj',
        'Providers/Cloud/NextGenSoftware.OASIS.API.Providers.DynamoDBOASIS/NextGenSoftware.OASIS.API.Providers.DynamoDBOASIS.csproj',
        'Providers/Cloud/NextGenSoftware.OASIS.API.Providers.AWSOASIS/NextGenSoftware.OASIS.API.Providers.AWSOASIS.csproj',
        'Providers/Shared/NextGenSoftware.OASIS.Providers.Shared.S3/NextGenSoftware.OASIS.Providers.Shared.S3.csproj',
        'Providers/Cloud/NextGenSoftware.OASIS.API.Providers.AWSS3OASIS/NextGenSoftware.OASIS.API.Providers.AWSS3OASIS.csproj',
        'Providers/Cloud/TestProjects/NextGenSoftware.OASIS.API.Providers.AWSOASIS.UnitTests/NextGenSoftware.OASIS.API.Providers.AWSOASIS.UnitTests.csproj')
    foreach($relative in $projects){
        $project=Join-Path $root $relative
        & dotnet restore $project --source (Join-Path $env:USERPROFILE '.nuget/packages') --source 'C:/Program Files/dotnet/library-packs' /p:RestoreRecursive=false /p:NuGetAudit=false /p:IncludeCoreStorageConformance=true /m:2 /nr:false -v:q
        if($LASTEXITCODE){throw 'Cached AWS metadata unavailable; no automatic download or duplicate environment.'}
        & dotnet build $project --no-restore /p:IncludeCoreStorageConformance=true /p:BuildProjectReferences=false /m:2 /nr:false /p:UseSharedCompilation=false -v:q -clp:ErrorsOnly
        if($LASTEXITCODE){throw 'AWS canonical host build failed.'}
    }
    $awsConsumerOutput=Join-Path (Split-Path $project) 'bin/Debug/net10.0'
    . (Join-Path $root 'Scripts/TestHosts/Assert-ReusableProviderBuildIdentity.ps1') -RepositoryRoot $root -ConsumerOutput $awsConsumerOutput -VerifySourceDocuments
    foreach($name in @('NextGenSoftware.OASIS.API.Providers.DynamoDBOASIS','NextGenSoftware.OASIS.API.Providers.AWSOASIS','NextGenSoftware.OASIS.Providers.Shared.S3','NextGenSoftware.OASIS.API.Providers.AWSS3OASIS','NextGenSoftware.OASIS.API.Providers.AWSOASIS.UnitTests')){
        Write-Output ($name+': sources='+(Assert-SourceDocuments (Join-Path $awsConsumerOutput ($name+'.dll')) $root))
    }
    & dotnet test $project --no-build --no-restore /p:IncludeCoreStorageConformance=true --filter 'FullyQualifiedName~AwsBootConfigurationTests|FullyQualifiedName~AwsS3ConfigurationTests' --logger 'trx;LogFileName=aws-factory-current.trx' --results-directory $results -v:q
    if($LASTEXITCODE){throw 'Shipping AWS/S3 factory controls failed; no native run.'}
    [xml]$awsFactoryTrx=Get-Content -LiteralPath (Join-Path $results 'aws-factory-current.trx') -Raw
    $awsFactoryCounts=$awsFactoryTrx.TestRun.ResultSummary.Counters
    if([int]$awsFactoryCounts.total -ne 27 -or [int]$awsFactoryCounts.passed -ne 27 -or [int]$awsFactoryCounts.notExecuted -ne 0){throw 'Exactly 27 executed shipping factory controls required before native dispatch.'}
    if($ArchiveConfigurationReports){
        & python -B (Join-Path $root 'Scripts/Providers/archive_configuration_reports.py') --providers @awsSelectedNames --stage $ArchiveReportStage
        if($LASTEXITCODE){throw 'Original configuration report archival incomplete.'}
    }
    $runtime='C:/Source/OASIS-hyperdrive-v2-gaps/TestResults/AWSProviderEvidence/runtime'
    $priorEndpoint=$env:DYNAMODB_SERVICE_URL;$owned=$null;$environmentLease=$null
    try{
        $env:DYNAMODB_SERVICE_URL=$null
        if(Test-Path -LiteralPath (Join-Path $runtime 'DynamoDBLocal.jar')){
            if((Get-FileHash -LiteralPath (Join-Path $runtime 'DynamoDBLocal.jar')).Hash -ne '67ED9662164DC9765FF3ABCA1C2F6F4EDB5F6A92269812450215A679BDC71620'){throw 'Existing DynamoDB runtime identity differs; no replacement.'}
            if((Get-Content -LiteralPath (Join-Path $runtime 'runtime-source-sha256.txt') -Raw).Trim() -ne '667CDE09F96F4785CEDE4D107D8066011E5EECEB5DD4566EA8C74C0D378FE919'){throw 'Existing DynamoDB provenance differs.'}
            $environmentLease=[IO.File]::Open((Join-Path (Split-Path $runtime) 'environment.lock'),[IO.FileMode]::OpenOrCreate,[IO.FileAccess]::ReadWrite,[IO.FileShare]::None)
            $owned=& (Join-Path $root 'Scripts/TestHosts/Use-ReusableDynamoContainer.ps1') -InitializeSyntheticStore:$InitializeSyntheticStore
            $env:DYNAMODB_SERVICE_URL='http://127.0.0.1:18000'
        }
        $awsNativeFilter=if($DynamoOnly){'FullyQualifiedName~AwsStorageConformanceTests.SharedDynamoStorageInterface'}else{'FullyQualifiedName~AwsStorageConformanceTests'}
        & dotnet test $project --no-build --no-restore /p:IncludeCoreStorageConformance=true --filter $awsNativeFilter --logger "trx;LogFileName=$awsNativeReport" --results-directory $results -v:q
        $testExit=$LASTEXITCODE
        & python -B (Join-Path $PSScriptRoot 'record_conformance_results.py') --providers @awsSelectedNames
        if($LASTEXITCODE){throw 'AWS shared report registration failed.'}
        & python (Join-Path $PSScriptRoot 'update_provider_matrix.py')
        if($LASTEXITCODE){throw 'AWS matrix update failed.'}
        if($testExit){throw 'AWS canonical conformance recorded blockers; no routing/depth or Dev promotion.'}
        [xml]$awsNativeTrx=Get-Content -LiteralPath (Join-Path $results $awsNativeReport) -Raw
        $awsNativeCounts=$awsNativeTrx.TestRun.ResultSummary.Counters
        $awsExpectedNative=if($DynamoOnly){4}else{6}
        if([int]$awsNativeCounts.total -ne $awsExpectedNative -or [int]$awsNativeCounts.passed -ne $awsExpectedNative -or [int]$awsNativeCounts.notExecuted -ne 0){throw 'Every selected native schedule must execute without skips.'}
    }
    finally{
        $env:DYNAMODB_SERVICE_URL=$priorEndpoint
        try{if($owned){& (Join-Path $root 'Scripts/TestHosts/Use-ReusableDynamoContainer.ps1') -Stop}}
        finally{if($environmentLease){$environmentLease.Dispose()}}
    }
}
