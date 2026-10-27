#Requires -Version 7.0
[CmdletBinding()]
param()
$ErrorActionPreference='Stop'
$PSNativeCommandUseErrorActionPreference=$false
$azureTaskRoot=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
$azureRuntime=Join-Path $env:USERPROFILE '.cache/oasis-runtimes/azurite-3.37.0/node_modules/azurite/dist/src/blob/main.js'
$azurePackage=Join-Path $env:USERPROFILE '.cache/oasis-runtimes/azurite-3.37.0/node_modules/azurite/package.json'
if(!(Test-Path -LiteralPath $azureRuntime) -or (Get-Content -LiteralPath $azurePackage -Raw|ConvertFrom-Json).version -ne '3.37.0'){throw 'Retained pinned Azurite missing; do not download or substitute a runtime.'}
$azureResults=Join-Path $azureTaskRoot 'TestResults/ProviderConformanceCurrent'
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
        $azureProcess=Start-Process -FilePath (Get-Command node.exe).Source -ArgumentList @($azureRuntime,'--blobHost','127.0.0.1','--blobPort','10000','--location',$azureData,'--disableTelemetry','--silent') -WorkingDirectory $azureWork -WindowStyle Hidden -PassThru -RedirectStandardOutput (Join-Path $azureWork 'azurite.out.log') -RedirectStandardError (Join-Path $azureWork 'azurite.err.log')
        $azureReady=$false
        for($azureAttempt=0;$azureAttempt -lt 60;$azureAttempt++){
            if($azureProcess.HasExited){throw 'Owned Azurite exited before readiness.'}
            $azureListener=Get-NetTCPConnection -LocalPort 10000 -State Listen -ErrorAction SilentlyContinue
            if($azureListener){
                if(@($azureListener|Where-Object OwningProcess -ne $azureProcess.Id).Count){throw 'Azurite listener is not the owned process.'}
                $azureReady=$true;break
            }
            Start-Sleep -Milliseconds 250
        }
        if(!$azureReady){throw 'Owned Azurite readiness timeout; preserve store.'}
        $env:OASIS_AZURE_STORAGE_CONFORMANCE='1'
        & dotnet test $azureProject --no-build --no-restore --filter 'FullyQualifiedName~AzureStorageConformanceTests' --logger 'trx;LogFileName=azure-blob-native-current.trx' --results-directory $azureResults -v:q
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
        if($null -ne $azureProcess -and !$azureProcess.HasExited){
            Stop-Process -Id $azureProcess.Id -Force
            if(!$azureProcess.WaitForExit(10000)){throw 'Owned Azurite did not exit; do not restart or remove the store.'}
        }
        $azureLease.Dispose()
    }
}
