#Requires -Version 7.0
[CmdletBinding()]
param()
$ErrorActionPreference='Stop'
$solidTaskRoot=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
$solidWork=Join-Path $solidTaskRoot 'TestResults/SolidCurrent'
foreach($solidMode in @('async','sync')){
    $solidReport=Join-Path $solidTaskRoot ('TestResults/ProviderConformanceCurrent/SOLIDOASIS/'+$solidMode+'/report.json')
    if((Test-Path -LiteralPath $solidReport) -or (Test-Path -LiteralPath ($solidReport+'.next'))){throw 'Preserve original SOLID canonical evidence before another run.'}
}
$solidTrx=Join-Path $solidTaskRoot 'TestResults/ProviderConformanceCurrent/solid-core-current.trx'
if(Test-Path -LiteralPath $solidTrx){throw 'Preserve previous SOLID native report.'}
& (Join-Path $solidTaskRoot 'Scripts/TestHosts/Invoke-WithProviderBuildLease.ps1') -Owner 'SOLID private canonical storage breadth' -Operation {
    New-Item -ItemType Directory -Path $solidWork -Force | Out-Null
    $solidLease=[IO.File]::Open((Join-Path $solidWork 'environment.lock'),[IO.FileMode]::OpenOrCreate,[IO.FileAccess]::ReadWrite,[IO.FileShare]::None)
    $solidStarted=$false
    try{
        if(Get-ChildItem -LiteralPath $solidWork -Filter '*pending*.json'){throw 'Original SOLID pending fixture requires review; no replay/reset.'}
        foreach($solidProjectRelative in @(
            'Providers/Shared/NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage/NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage.csproj',
            'Providers/Network/NextGenSoftware.OASIS.API.Providers.SOLIDOASIS/NextGenSoftware.OASIS.API.Providers.SOLIDOASIS.csproj',
            'Providers/Network/TestProjects/NextGenSoftware.OASIS.API.Providers.SOLIDOASIS.IntegrationTests/NextGenSoftware.OASIS.API.Providers.SOLIDOASIS.IntegrationTests.csproj')){
            $solidProject=Join-Path $solidTaskRoot $solidProjectRelative
            & dotnet restore $solidProject --source (Join-Path $env:USERPROFILE '.nuget/packages') --source 'C:/Program Files/dotnet/library-packs' /p:RestoreRecursive=false /p:NuGetAudit=false /p:IncludeCoreStorageConformance=true /m:2 /nr:false -v:q
            if($LASTEXITCODE){throw 'SOLID cached metadata unavailable; no automatic download.'}
            & dotnet build $solidProject --no-restore /p:IncludeCoreStorageConformance=true /p:BuildProjectReferences=false /m:2 /nr:false /p:UseSharedCompilation=false -v:q -clp:ErrorsOnly
            if($LASTEXITCODE){throw 'SOLID canonical producer/consumer build failed.'}
        }
        $solidConsumerOutput=Join-Path $solidTaskRoot 'Providers/Network/TestProjects/NextGenSoftware.OASIS.API.Providers.SOLIDOASIS.IntegrationTests/bin/Debug/net10.0'
        . (Join-Path $solidTaskRoot 'Scripts/TestHosts/Assert-ReusableProviderBuildIdentity.ps1') -RepositoryRoot $solidTaskRoot -ConsumerOutput $solidConsumerOutput -VerifySourceDocuments
        foreach($solidAssembly in @('NextGenSoftware.OASIS.API.Providers.SOLIDOASIS','NextGenSoftware.OASIS.API.Providers.SOLIDOASIS.IntegrationTests')){
            Write-Output ($solidAssembly+': sources='+(Assert-SourceDocuments (Join-Path $solidConsumerOutput ($solidAssembly+'.dll')) $solidTaskRoot))
        }
        $solidConfigured=@('storage-key.dpapi','private-local-client.dpapi','private-local-pod.dpapi').Where({!(Test-Path -LiteralPath (Join-Path $solidWork $_))}).Count -eq 0
        if($solidConfigured){
            $solidRaw=(& wsl -d Ubuntu -u root -- podman inspect oasis-solid)-join "`n"
            if($LASTEXITCODE){throw 'Retained SOLID container unavailable.'}
            $solidContainer=($solidRaw|ConvertFrom-Json)[0]
            if($solidContainer.Image -ne '58bee7c3f6a20498d72eb6f8334ba84299b6df2c8fca119ce805fe8614512d7a' -or $solidContainer.State.Status -ne 'exited'){throw 'Exact stopped retained SOLID runtime required.'}
            & wsl -d Ubuntu -u root -- podman start oasis-solid | Out-Null
            if($LASTEXITCODE){throw 'Retained SOLID start failed.'};$solidStarted=$true
            $null=& (Join-Path $solidTaskRoot 'Scripts/TestHosts/Wait-ReusableSolidListener.ps1')
        }
        # Missing original credentials are recorded as configuration failures before HTTP/storage.
        & dotnet test $solidProject --no-build --no-restore /p:IncludeCoreStorageConformance=true --filter 'FullyQualifiedName~SolidCoreStorageConformanceTests' --logger 'trx;LogFileName=solid-core-current.trx' --results-directory (Split-Path $solidTrx) -v:q
        $solidTestExit=$LASTEXITCODE
        & python -B (Join-Path $PSScriptRoot 'record_conformance_results.py') --providers SOLIDOASIS
        if($LASTEXITCODE){throw 'SOLID canonical report registration failed.'}
        & python -B (Join-Path $PSScriptRoot 'update_provider_matrix.py')
        if($LASTEXITCODE){throw 'SOLID matrix generation failed.'}
        if($solidTestExit){throw 'SOLID canonical suite recorded blockers; preserve original identity/key, no depth or promotion.'}
        [xml]$solidRun=Get-Content -LiteralPath $solidTrx -Raw
        $solidCounts=$solidRun.TestRun.ResultSummary.Counters
        if([int]$solidCounts.total -ne 2 -or [int]$solidCounts.passed -ne 2 -or [int]$solidCounts.notExecuted -ne 0){throw 'Both SOLID native schedules must execute without skips.'}
    }finally{
        try{if($solidStarted){& wsl -d Ubuntu -u root -- podman stop --time 20 oasis-solid | Out-Null;if($LASTEXITCODE){throw 'Owned SOLID stop failed; inspect existing runtime.'}}}
        finally{$solidLease.Dispose()}
    }
}
