#Requires -Version 7.0
[CmdletBinding()]
param([switch]$DynamoOnly,[switch]$ArchiveConfigurationReports,[switch]$InitializeSyntheticStore,[ValidateSet('configuration','activation')][string]$ArchiveReportStage='configuration',[switch]$RerunVerifiedDynamo)
$ErrorActionPreference='Stop'
$root=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
$awsSelectedNames=if($DynamoOnly){@('AWSOASIS','DynamoDBOASIS')}else{@('AWSOASIS','DynamoDBOASIS','AWSS3OASIS')}
function Preserve-VerifiedDynamoReports([switch]$CheckOnly) {
    if(!$DynamoOnly -or $ArchiveConfigurationReports){throw 'Verified reruns require exclusive Dynamo-only scope.'}
    $registry=Get-Content (Join-Path $root 'Docs/WIP/HyperDrive-Providers/CONFORMANCE_RESULTS.json') -Raw | ConvertFrom-Json
    $moves=@()
    foreach($provider in $awsSelectedNames){
        $entry=@($registry.results | Where-Object {$_.provider -eq $provider})
        if($entry.Count -ne 1 -or $entry[0].passed -ne 52 -or $entry[0].mode -ne 'native'){throw 'Previous complete native gate required.'}
        $aggregatePath=Join-Path $root $entry[0].report
        if((Get-FileHash -LiteralPath $aggregatePath).Hash -ne $entry[0].sha256){throw 'Previous aggregate report changed.'}
        $aggregate=Get-Content -LiteralPath $aggregatePath -Raw | ConvertFrom-Json
        foreach($mode in @('async','sync')){
            $path=Join-Path $root ('TestResults/ProviderConformanceCurrent/'+$provider+'/'+$mode+'/report.json')
            $prior=Get-Content -LiteralPath $path -Raw | ConvertFrom-Json
            $expected=@($aggregate.runs | Where-Object {$_.Synchronous -eq ($mode -eq 'sync')})
            if($expected.Count -ne 1 -or ($prior | ConvertTo-Json -Depth 30 -Compress) -ne ($expected[0] | ConvertTo-Json -Depth 30 -Compress) -or !$prior.Succeeded -or !$prior.FixtureCleanupComplete -or $prior.Passed.Count -ne 26 -or $prior.SuiteVersion -ne 'core-storage-v2' -or (Test-Path -LiteralPath ($path+'.next'))){throw 'Previous native fixture/report needs review, not replay.'}
            $previous=Join-Path (Split-Path $path) 'report-verified-previous.json'
            if(Test-Path -LiteralPath $previous){$old=Get-Content -LiteralPath $previous -Raw|ConvertFrom-Json;if(!$old.Succeeded -or !$old.FixtureCleanupComplete -or $old.SuiteVersion -ne 'core-storage-v2'){throw 'Preserve unknown previous evidence.'}}
            $moves+=@{path=$path;previous=$previous}
        }
    }
    if(!$CheckOnly){foreach($move in $moves){Move-Item -LiteralPath $move.path -Destination $move.previous -Force}}
}
if($RerunVerifiedDynamo){Preserve-VerifiedDynamoReports -CheckOnly}
if($ArchiveConfigurationReports){
    & python -B (Join-Path $root 'Scripts/Providers/archive_configuration_reports.py') --providers @awsSelectedNames --stage $ArchiveReportStage --check-only
    if($LASTEXITCODE){throw 'Original AWS configuration evidence changed.'}
}
foreach($name in $awsSelectedNames){
    foreach($mode in @('async','sync')){
        $report=Join-Path $root ('TestResults/ProviderConformanceCurrent/'+$name+'/'+$mode+'/report.json')
        if(!$ArchiveConfigurationReports -and !$RerunVerifiedDynamo -and ((Test-Path -LiteralPath $report) -or (Test-Path -LiteralPath ($report+'.next')))){throw 'Review preserved AWS conformance evidence before another run.'}
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
    if($RerunVerifiedDynamo){Preserve-VerifiedDynamoReports}
    $runtime=Join-Path $root 'TestResults/AWSProviderEvidence/runtime'
    $priorEndpoint=$env:DYNAMODB_SERVICE_URL;$owned=$null;$environmentLease=$null;$awsStoreClient=$null
    try{
        $env:DYNAMODB_SERVICE_URL=$null
        if(Test-Path -LiteralPath (Join-Path $runtime 'DynamoDBLocal.jar')){
            if((Get-FileHash -LiteralPath (Join-Path $runtime 'DynamoDBLocal.jar')).Hash -ne '67ED9662164DC9765FF3ABCA1C2F6F4EDB5F6A92269812450215A679BDC71620'){throw 'Existing DynamoDB runtime identity differs; no replacement.'}
            if((Get-Content -LiteralPath (Join-Path $runtime 'runtime-source-sha256.txt') -Raw).Trim() -ne '667CDE09F96F4785CEDE4D107D8066011E5EECEB5DD4566EA8C74C0D378FE919'){throw 'Existing DynamoDB provenance differs.'}
            $environmentLease=[IO.File]::Open((Join-Path (Split-Path $runtime) 'environment.lock'),[IO.FileMode]::OpenOrCreate,[IO.FileAccess]::ReadWrite,[IO.FileShare]::None)
            $owned=& (Join-Path $root 'Scripts/TestHosts/Use-ReusableDynamoContainer.ps1') -InitializeSyntheticStore:$InitializeSyntheticStore
            $env:DYNAMODB_SERVICE_URL='http://127.0.0.1:18000'
            [System.Runtime.Loader.AssemblyLoadContext]::Default.LoadFromAssemblyPath((Join-Path $awsConsumerOutput 'AWSSDK.Core.dll')) | Out-Null
            [System.Runtime.Loader.AssemblyLoadContext]::Default.LoadFromAssemblyPath((Join-Path $awsConsumerOutput 'AWSSDK.DynamoDBv2.dll')) | Out-Null
            $storeConfig=[Amazon.DynamoDBv2.AmazonDynamoDBConfig]::new();$storeConfig.ServiceURL=$env:DYNAMODB_SERVICE_URL;$storeConfig.AuthenticationRegion='us-east-1'
            $awsStoreClient=[Amazon.DynamoDBv2.AmazonDynamoDBClient]::new([Amazon.Runtime.BasicAWSCredentials]::new('localtestkey','localtestsecret'),$storeConfig)
            function Assert-EmptyDynamoFixtureStore([switch]$After) {
                $tables=$awsStoreClient.ListTablesAsync([Amazon.DynamoDBv2.Model.ListTablesRequest]::new()).GetAwaiter().GetResult()
                $expectedTables=@('OasisAvatars','OasisAvatarDetails','OasisHolons','OasisHolonWritePreparations','OasisHolonWriteReceipts')
                if($tables.LastEvaluatedTableName -or @($tables.TableNames).Count -notin @(0,5) -or ($After -and @($tables.TableNames).Count -ne 5)){throw 'Unexpected fixture table inventory; preserve data.'}
                $counts=@()
                foreach($table in $tables.TableNames){
                    if($table -notin $expectedTables){throw 'Unknown table in the retained test store.'}
                    $count=0;$request=[Amazon.DynamoDBv2.Model.ScanRequest]::new();$request.TableName=$table;$request.ConsistentRead=$true;$request.Select=[Amazon.DynamoDBv2.Select]::COUNT
                    do{$page=$awsStoreClient.ScanAsync($request).GetAwaiter().GetResult();if([int]$page.HttpStatusCode -ne 200){throw 'Native cleanup observation failed.'};$count+=$page.Count;$request.ExclusiveStartKey=$page.LastEvaluatedKey}while($page.LastEvaluatedKey.Count -gt 0)
                    $counts+=@{table=$table;records=$count}
                    if($count -ne 0){throw 'Retained test records require review; no new run or automatic deletion.'}
                }
                return $counts
            }
            $awsStoreBefore=@(Assert-EmptyDynamoFixtureStore)
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
        if($awsStoreClient){$awsStoreAfter=@(Assert-EmptyDynamoFixtureStore -After);@{before=$awsStoreBefore;after=$awsStoreAfter;allRecordsCleaned=$true} | ConvertTo-Json -Depth 6 | Set-Content (Join-Path (Split-Path $runtime) 'dynamo-cleanup-current.json')}
    }
    finally{
        $env:DYNAMODB_SERVICE_URL=$priorEndpoint
        try{if($awsStoreClient){$awsStoreClient.Dispose()};if($owned){& (Join-Path $root 'Scripts/TestHosts/Use-ReusableDynamoContainer.ps1') -Stop}}
        finally{if($environmentLease){$environmentLease.Dispose()}}
    }
}
