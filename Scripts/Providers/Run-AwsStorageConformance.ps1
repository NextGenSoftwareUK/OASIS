#Requires -Version 7.0
[CmdletBinding()]
param()
$ErrorActionPreference='Stop'
$root=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
foreach($name in @('AWSOASIS','DynamoDBOASIS','AWSS3OASIS')){
    foreach($mode in @('async','sync')){
        $report=Join-Path $root ('TestResults/ProviderConformanceCurrent/'+$name+'/'+$mode+'/report.json')
        if((Test-Path -LiteralPath $report) -or (Test-Path -LiteralPath ($report+'.next'))){throw 'Review preserved AWS conformance evidence before another run.'}
    }
}
$results=Join-Path $root 'TestResults/ProviderConformanceCurrent'
foreach($name in @('aws-family-current.trx','aws-factory-current.trx')){if(Test-Path -LiteralPath (Join-Path $results $name)){throw 'Preserve the prior AWS conformance/factory TRX.'}}
& (Join-Path $root 'Scripts/TestHosts/Invoke-WithProviderBuildLease.ps1') -Owner 'AWS document/object canonical storage breadth' -Operation {
    $projects=@(
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
    $consumer=Join-Path (Split-Path $project) 'bin/Debug/net10.0'
    . (Join-Path $root 'Scripts/TestHosts/Assert-ReusableProviderBuildIdentity.ps1') -RepositoryRoot $root -ConsumerOutput $consumer -VerifySourceDocuments
    foreach($name in @('NextGenSoftware.OASIS.API.Providers.DynamoDBOASIS','NextGenSoftware.OASIS.API.Providers.AWSOASIS','NextGenSoftware.OASIS.Providers.Shared.S3','NextGenSoftware.OASIS.API.Providers.AWSS3OASIS','NextGenSoftware.OASIS.API.Providers.AWSOASIS.UnitTests')){
        Write-Output ($name+': sources='+(Assert-SourceDocuments (Join-Path $consumer ($name+'.dll')) $root))
    }
    & dotnet test $project --no-build --no-restore /p:IncludeCoreStorageConformance=true --filter 'FullyQualifiedName~AwsBootConfigurationTests|FullyQualifiedName~AwsS3ConfigurationTests' --logger 'trx;LogFileName=aws-factory-current.trx' --results-directory $results -v:q
    if($LASTEXITCODE){throw 'Shipping AWS/S3 factory controls failed; no native run.'}
    $runtime='C:/Source/OASIS-hyperdrive-v2-gaps/TestResults/AWSProviderEvidence/runtime'
    $priorEndpoint=$env:DYNAMODB_SERVICE_URL;$owned=$null;$environmentLease=$null
    try{
        $env:DYNAMODB_SERVICE_URL=$null
        if(Test-Path -LiteralPath (Join-Path $runtime 'DynamoDBLocal.jar')){
            if((Get-FileHash -LiteralPath (Join-Path $runtime 'DynamoDBLocal.jar')).Hash -ne '72640F61583045B630971AF8D9F34AC311CF3FD0A7F077EA96EC0D04BCBE1E10'){throw 'Existing DynamoDB runtime identity differs; no replacement.'}
            if((Get-Content -LiteralPath (Join-Path $runtime 'runtime-source-sha256.txt') -Raw).Trim() -ne 'A3CB28740EA3875A7B0E14CFB13CED686F36CE27BC00F13F8A7D72DDEEE6C9B1'){throw 'Existing DynamoDB provenance differs.'}
            $environmentLease=[IO.File]::Open((Join-Path (Split-Path $runtime) 'environment.lock'),[IO.FileMode]::OpenOrCreate,[IO.FileAccess]::ReadWrite,[IO.FileShare]::None)
            $owned=& (Join-Path $root 'Scripts/TestHosts/Use-ReusableDynamoContainer.ps1')
            $env:DYNAMODB_SERVICE_URL='http://127.0.0.1:18000'
        }
        & dotnet test $project --no-build --no-restore /p:IncludeCoreStorageConformance=true --filter 'FullyQualifiedName~AwsStorageConformanceTests' --logger 'trx;LogFileName=aws-family-current.trx' --results-directory $results -v:q
        $testExit=$LASTEXITCODE
        & python (Join-Path $PSScriptRoot 'record_conformance_results.py') --providers AWSOASIS DynamoDBOASIS AWSS3OASIS
        if($LASTEXITCODE){throw 'AWS shared report registration failed.'}
        & python (Join-Path $PSScriptRoot 'update_provider_matrix.py')
        if($LASTEXITCODE){throw 'AWS matrix update failed.'}
        if($testExit){throw 'AWS canonical conformance recorded blockers; no routing/depth or Dev promotion.'}
    }
    finally{
        $env:DYNAMODB_SERVICE_URL=$priorEndpoint
        try{if($owned){& (Join-Path $root 'Scripts/TestHosts/Use-ReusableDynamoContainer.ps1') -Stop}}
        finally{if($environmentLease){$environmentLease.Dispose()}}
    }
}
