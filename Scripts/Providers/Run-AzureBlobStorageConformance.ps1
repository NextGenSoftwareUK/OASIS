#Requires -Version 7.0
[CmdletBinding()]
param([switch]$RerunVerified)
$ErrorActionPreference='Stop'
$PSNativeCommandUseErrorActionPreference=$false
$azureTaskRoot=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
. (Join-Path $azureTaskRoot 'Scripts/TestHosts/ReusableAzuriteHost.ps1')
$azureRuntime=Join-Path $env:USERPROFILE '.cache/oasis-runtimes/azurite-3.37.0/node_modules/azurite/dist/src/blob/main.js'
$azurePackage=Join-Path $env:USERPROFILE '.cache/oasis-runtimes/azurite-3.37.0/node_modules/azurite/package.json'
if(!(Test-Path -LiteralPath $azureRuntime) -or (Get-Content -LiteralPath $azurePackage -Raw|ConvertFrom-Json).version -ne '3.37.0'){throw 'Retained pinned Azurite missing; do not download or substitute a runtime.'}
$azureResults=Join-Path $azureTaskRoot 'TestResults/ProviderConformanceCurrent'
if($RerunVerified){
    $azureRegistry=Get-Content (Join-Path $azureTaskRoot 'Docs/WIP/HyperDrive-Providers/CONFORMANCE_RESULTS.json') -Raw|ConvertFrom-Json
    $azureEntry=@($azureRegistry.results|Where-Object provider -eq 'AzureStorageOASIS')
    if($azureEntry.Count -ne 1 -or $azureEntry[0].passed -ne 52 -or $azureEntry[0].mode -ne 'native' -or $azureEntry[0].suite -ne 'core-storage-v2'){throw 'Only a verified current native pair may be retired for rerun.'}
    $azureAggregatePath=Join-Path $azureTaskRoot $azureEntry[0].report
    if((Get-FileHash -LiteralPath $azureAggregatePath).Hash -ne $azureEntry[0].sha256){throw 'Registered Azure aggregate changed.'}
    $azureAggregate=Get-Content $azureAggregatePath -Raw|ConvertFrom-Json
    $azureMoves=@()
    foreach($azureMode in @('async','sync')){
        $azurePath=Join-Path $azureResults ('AzureStorageOASIS/'+$azureMode+'/report.json')
        $azurePriorReport=Get-Content $azurePath -Raw|ConvertFrom-Json
        $azureExpected=@($azureAggregate.runs|Where-Object {$_.Synchronous -eq ($azureMode -eq 'sync')})
        $azureArchive=Join-Path (Split-Path $azurePath) 'report-verified-previous.json'
        if($azureExpected.Count -ne 1 -or ($azurePriorReport|ConvertTo-Json -Depth 40 -Compress) -ne ($azureExpected[0]|ConvertTo-Json -Depth 40 -Compress) -or !$azurePriorReport.Succeeded -or !$azurePriorReport.FixtureCleanupComplete -or $azurePriorReport.Passed.Count -ne 26 -or (Test-Path ($azurePath+'.next')) -or (Test-Path $azureArchive)){throw 'Azure previous fixtures/reports require review; no retirement.'}
        $azureMoves+=@{Source=$azurePath;Destination=$azureArchive;Hash=(Get-FileHash $azurePath).Hash}
    }
    foreach($azureMove in $azureMoves){Move-Item -LiteralPath $azureMove.Source -Destination $azureMove.Destination;if((Get-FileHash $azureMove.Destination).Hash -ne $azureMove.Hash){throw 'Retired Azure report changed.'}}
}
foreach($azureMode in @('async','sync')){
    $azureReport=Join-Path $azureResults ('AzureStorageOASIS/'+$azureMode+'/report.json')
    if((Test-Path -LiteralPath $azureReport) -or (Test-Path -LiteralPath ($azureReport+'.next'))){throw 'Preserve previous Azure Blob reports and native fixture evidence before rerun.'}
}
foreach($azureName in @('azure-blob-controls-current.trx','azure-blob-native-current.trx')){
    if(Test-Path -LiteralPath (Join-Path $azureResults $azureName)){throw 'Preserve previous Azure Blob TRX before rerun.'}
}
$azureRouting=Join-Path $azureTaskRoot 'Providers/Cloud/TestProjects/NextGenSoftware.OASIS.API.Providers.AzureStorageOASIS.IntegrationTests/bin/Debug/net10.0/TestResults/AzureRoutingFiles'
if((Test-Path -LiteralPath $azureRouting) -and (Get-ChildItem -LiteralPath $azureRouting -File -Recurse|Select-Object -First 1)){throw 'Original Azure routing fixtures require reconciliation; no emulator start.'}
if(Get-NetTCPConnection -LocalPort 10000 -State Listen -ErrorAction SilentlyContinue){throw 'Retained Azurite port10000 occupied; do not start a duplicate.'}
& (Join-Path $azureTaskRoot 'Scripts/TestHosts/Invoke-WithProviderBuildLease.ps1') -Owner 'Azure Blob shared storage baseline' -Operation {
    $azureWork=Join-Path $azureTaskRoot 'TestResults/AzureCurrent'
    $azureData=Join-Path $azureWork 'conformance-data'
    New-Item -ItemType Directory -Path $azureData -Force|Out-Null
    $azureLease=[IO.File]::Open((Join-Path $azureWork 'environment.lock'),[IO.FileMode]::OpenOrCreate,[IO.FileAccess]::ReadWrite,[IO.FileShare]::None)
    $azureProcess=$null;$azurePrior=$env:OASIS_AZURE_STORAGE_CONFORMANCE
    try{
        foreach($azureRelative in @(
            'Providers/Shared/NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage/NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage.csproj',
            'Providers/Cloud/NextGenSoftware.OASIS.API.Providers.AzureStorageOASIS/NextGenSoftware.OASIS.API.Providers.AzureStorageOASIS.csproj',
            'Providers/Cloud/TestProjects/NextGenSoftware.OASIS.API.Providers.AzureStorageOASIS.UnitTests/NextGenSoftware.OASIS.API.Providers.AzureStorageOASIS.UnitTests.csproj')){
            $azureProject=Join-Path $azureTaskRoot $azureRelative
            & dotnet restore $azureProject --source (Join-Path $env:USERPROFILE '.nuget/packages') --source 'C:/Program Files/dotnet/library-packs' /p:RestoreRecursive=false /p:NuGetAudit=false /p:IncludeNativeStorageConformance=true /m:2 /nr:false -v:q
            if($LASTEXITCODE){throw 'Azure Blob cached metadata unavailable; no automatic download.'}
            & dotnet build $azureProject --no-restore /p:BuildProjectReferences=false /p:IncludeNativeStorageConformance=true /m:2 /nr:false /p:UseSharedCompilation=false -v:q -clp:ErrorsOnly
            if($LASTEXITCODE){throw 'Azure Blob cached build failed.'}
        }
        $azureConsumer=Join-Path (Split-Path $azureProject) 'bin/Debug/net10.0'
        . (Join-Path $azureTaskRoot 'Scripts/TestHosts/Assert-ReusableProviderBuildIdentity.ps1') -RepositoryRoot $azureTaskRoot -ConsumerOutput $azureConsumer -VerifySourceDocuments
        foreach($azureAssembly in @('NextGenSoftware.OASIS.API.Providers.AzureStorageOASIS','NextGenSoftware.OASIS.API.Providers.AzureStorageOASIS.UnitTests')){
            Write-Output ($azureAssembly+': source documents='+(Assert-SourceDocuments (Join-Path $azureConsumer ($azureAssembly+'.dll')) $azureTaskRoot))
        }
        & dotnet test $azureProject --no-build --no-restore --filter 'FullyQualifiedName!~AzureStorageConformanceTests' --logger 'trx;LogFileName=azure-blob-controls-current.trx' --results-directory $azureResults -v:q
        if($LASTEXITCODE){throw 'Azure Blob controlled regressions failed before emulator startup.'}
        [xml]$azureControls=Get-Content (Join-Path $azureResults 'azure-blob-controls-current.trx') -Raw
        $azureCounts=$azureControls.TestRun.ResultSummary.Counters
        if([int]$azureCounts.total -le 0 -or [int]$azureCounts.passed -ne [int]$azureCounts.total -or [int]$azureCounts.notExecuted -ne 0){throw 'Every selected Azure Blob control must execute and pass.'}
        $azureProcess=Start-ReusableAzuriteHost -Workspace $azureWork
        $azureReady=$false
        for($azureAttempt=0;$azureAttempt -lt 60;$azureAttempt++){
            if(Assert-ReusableAzuriteListener -HostHandle $azureProcess){
                $azureReady=$true;break
            }
            Start-Sleep -Milliseconds 250
        }
        if(!$azureReady){throw 'Owned Azurite readiness timeout; preserve store.'}
        $env:OASIS_AZURE_STORAGE_CONFORMANCE='1'
        & dotnet test $azureProject --no-build --no-restore --filter 'FullyQualifiedName~ShippingBlobFactoryRunsSharedStorageSuite' --logger 'trx;LogFileName=azure-blob-native-current.trx' --results-directory $azureResults -v:q
        $azureNativeExit=$LASTEXITCODE
        & python -B (Join-Path $PSScriptRoot 'record_conformance_results.py') --providers AzureStorageOASIS
        if($LASTEXITCODE){throw 'Azure Blob report registration failed; preserve original reports.'}
        & python -B (Join-Path $PSScriptRoot 'update_provider_matrix.py')
        if($LASTEXITCODE){throw 'Azure Blob matrix update failed.'}
        if($azureNativeExit){throw 'Azure Blob native suite failed; preserve reports and persistent emulator data, no replay.'}
        [xml]$azureNative=Get-Content (Join-Path $azureResults 'azure-blob-native-current.trx') -Raw
        $azureCounts=$azureNative.TestRun.ResultSummary.Counters
        if([int]$azureCounts.total -ne 2 -or [int]$azureCounts.passed -ne 2 -or [int]$azureCounts.notExecuted -ne 0){throw 'Both native Azure Blob schedules must pass without skips.'}
    }finally{
        $env:OASIS_AZURE_STORAGE_CONFORMANCE=$azurePrior
        if($null -ne $azureProcess){Stop-ReusableAzuriteHost -HostHandle $azureProcess}
        $azureLease.Dispose()
    }
}
