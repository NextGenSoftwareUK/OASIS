#Requires -Version 7.0
[CmdletBinding()]
param()
$ErrorActionPreference='Stop'
$activityTaskRoot=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
$activityResults=Join-Path $activityTaskRoot 'TestResults/ProviderConformanceCurrent'
$activityReport=Join-Path $activityResults 'ActivityPubOASIS/async/report.json'
foreach($activityPath in @($activityReport,($activityReport+'.next'),(Join-Path $activityResults 'activitypub-capability-unit-current.trx'),(Join-Path $activityResults 'activitypub-capability-native-current.trx'))){
    if(Test-Path -LiteralPath $activityPath){throw 'Preserve the original ActivityPub capability evidence before another run.'}
}
& (Join-Path $activityTaskRoot 'Scripts/TestHosts/Invoke-WithProviderBuildLease.ps1') -Owner 'ActivityPub real public-read capability breadth' -Operation {
    $activityWork=Join-Path $activityTaskRoot 'TestResults/ActivityPubCurrent'
    New-Item -ItemType Directory -Path $activityWork -Force | Out-Null
    $activityLease=[IO.File]::Open((Join-Path $activityWork 'environment.lock'),[IO.FileMode]::OpenOrCreate,[IO.FileAccess]::ReadWrite,[IO.FileShare]::None)
    $activityPrior=$env:OASIS_ACTIVITYPUB_PUBLIC_LIVE
    try{
        foreach($activityRelative in @(
            'Providers/Shared/NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage/NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage.csproj',
            'Providers/Network/NextGenSoftware.OASIS.API.Providers.ActivityPubOASIS/NextGenSoftware.OASIS.API.Providers.ActivityPubOASIS.csproj',
            'Providers/Network/TestProjects/NextGenSoftware.OASIS.API.Providers.ActivityPubOASIS.UnitTests/NextGenSoftware.OASIS.API.Providers.ActivityPubOASIS.UnitTests.csproj',
            'Providers/Network/TestProjects/NextGenSoftware.OASIS.API.Providers.ActivityPubOASIS.IntegrationTests/NextGenSoftware.OASIS.API.Providers.ActivityPubOASIS.IntegrationTests.csproj')){
            $activityProject=Join-Path $activityTaskRoot $activityRelative
            & dotnet restore $activityProject --source (Join-Path $env:USERPROFILE '.nuget/packages') --source 'C:/Program Files/dotnet/library-packs' /p:RestoreRecursive=false /p:NuGetAudit=false /p:IncludeCapabilityConformance=true /m:2 /nr:false -v:q
            if($LASTEXITCODE){throw 'ActivityPub cached metadata unavailable; no automatic download.'}
            & dotnet build $activityProject --no-restore /p:IncludeCapabilityConformance=true /p:BuildProjectReferences=false /m:2 /nr:false /p:UseSharedCompilation=false -v:q -clp:ErrorsOnly
            if($LASTEXITCODE){throw 'ActivityPub cached producer/consumer build failed.'}
        }
        foreach($activityHost in @('UnitTests','IntegrationTests')){
            $activityConsumerOutput=Join-Path $activityTaskRoot ('Providers/Network/TestProjects/NextGenSoftware.OASIS.API.Providers.ActivityPubOASIS.'+$activityHost+'/bin/Debug/net10.0')
            . (Join-Path $activityTaskRoot 'Scripts/TestHosts/Assert-ReusableProviderBuildIdentity.ps1') -RepositoryRoot $activityTaskRoot -ConsumerOutput $activityConsumerOutput -VerifySourceDocuments
            foreach($activityAssembly in @('NextGenSoftware.OASIS.API.Providers.ActivityPubOASIS',('NextGenSoftware.OASIS.API.Providers.ActivityPubOASIS.'+$activityHost))){
                Write-Output ($activityAssembly+': sources='+(Assert-SourceDocuments (Join-Path $activityConsumerOutput ($activityAssembly+'.dll')) $activityTaskRoot))
            }
        }
        $activityUnit=Join-Path $activityTaskRoot 'Providers/Network/TestProjects/NextGenSoftware.OASIS.API.Providers.ActivityPubOASIS.UnitTests/NextGenSoftware.OASIS.API.Providers.ActivityPubOASIS.UnitTests.csproj'
        & dotnet test $activityUnit --no-build --no-restore --logger 'trx;LogFileName=activitypub-capability-unit-current.trx' --results-directory $activityResults -v:q
        if($LASTEXITCODE){throw 'ActivityPub controlled regression gate failed before public requests.'}
        [xml]$activityUnitTrx=Get-Content -LiteralPath (Join-Path $activityResults 'activitypub-capability-unit-current.trx') -Raw
        $activityCounts=$activityUnitTrx.TestRun.ResultSummary.Counters
        if([int]$activityCounts.total -le 0 -or [int]$activityCounts.passed -ne [int]$activityCounts.total -or [int]$activityCounts.notExecuted -ne 0){throw 'All ActivityPub controlled regressions must execute without skips.'}
        $env:OASIS_ACTIVITYPUB_PUBLIC_LIVE='1'
        # Exactly one GET-only native case, no old skipped integration cases or social mutations.
        & dotnet test $activityProject --no-build --no-restore /p:IncludeCapabilityConformance=true --filter 'FullyQualifiedName~ActivityPubCapabilityConformanceTests' --logger 'trx;LogFileName=activitypub-capability-native-current.trx' --results-directory $activityResults -v:q
        $activityNativeExit=$LASTEXITCODE
        & python -B (Join-Path $PSScriptRoot 'record_conformance_results.py') --providers ActivityPubOASIS
        if($LASTEXITCODE){throw 'ActivityPub capability report registration failed.'}
        & python -B (Join-Path $PSScriptRoot 'update_provider_matrix.py')
        if($LASTEXITCODE){throw 'ActivityPub matrix update failed.'}
        if($activityNativeExit){throw 'ActivityPub native public capability gate failed; preserve evidence, no retries or social mutation.'}
        [xml]$activityNativeTrx=Get-Content -LiteralPath (Join-Path $activityResults 'activitypub-capability-native-current.trx') -Raw
        $activityCounts=$activityNativeTrx.TestRun.ResultSummary.Counters
        if([int]$activityCounts.total -ne 1 -or [int]$activityCounts.passed -ne 1 -or [int]$activityCounts.notExecuted -ne 0){throw 'One complete native capability case without skips required.'}
    }finally{$env:OASIS_ACTIVITYPUB_PUBLIC_LIVE=$activityPrior;$activityLease.Dispose()}
}
