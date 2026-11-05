param([switch]$Federated,[switch]$Serve,[switch]$RecoveryLaptop,[switch]$Initialize)
$ErrorActionPreference='Stop'
if($Serve -and !$Federated){throw 'Serve requires the explicit Mongo-paired preset.'}
if(!$Federated -and !$RecoveryLaptop){throw 'Use the explicit Mongo-paired Firebase teaching preset.'}
$root=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
if ($RecoveryLaptop) {
    $recovery = Join-Path $root 'LocalState/COSMIC/RecoveryLaptopFirebaseRtdb4112'
    if (!(Test-Path (Join-Path $recovery 'runtime.receipt.json'))) { throw 'Verified recovery runtime admission required.' }
    & (Join-Path $PSScriptRoot 'Invoke-WithProviderBuildLease.ps1') -Owner 'COSMIC Firebase recovery demo' -Operation {
        $project = Join-Path $root 'Providers/Cloud/TestProjects/NextGenSoftware.OASIS.API.Providers.FirebaseOASIS.IntegrationTests'
        $buildLog = Join-Path $recovery ('build-' + [Guid]::NewGuid().ToString('N') + '.log')
        & dotnet build $project /m:2 /nr:false /p:UseSharedCompilation=false /clp:ErrorsOnly -v:q *> $buildLog
        if ($LASTEXITCODE) { throw 'Recovery build failed; preserve its log.' }
        $runnerArgs = @('-B', (Join-Path $PSScriptRoot 'run-cosmic-firebase-native.py'), '--recovery-laptop')
        if ($Initialize) { $runnerArgs += '--initialize' }
        if ($Federated) { $runnerArgs += '--federated' }
        if ($Serve) { $runnerArgs += '--serve' }
        & python @runnerArgs
        if ($LASTEXITCODE) { throw 'Recovery verification incomplete; preserve source, journals and reports.' }
    }
    return
}
if ($Initialize) { throw 'Initialization is confined to the explicit recovery profile.' }
$results=Join-Path $root 'TestResults/FirebaseCqlCurrent'
[void][IO.Directory]::CreateDirectory($results)
& (Join-Path $PSScriptRoot 'Invoke-WithProviderBuildLease.ps1') -Owner 'COSMIC Firebase paired demo' -Operation {
    $project=Join-Path $root 'Providers/Cloud/TestProjects/NextGenSoftware.OASIS.API.Providers.FirebaseOASIS.IntegrationTests'
    & dotnet restore $project --source C:/Users/david/.nuget/packages /p:NuGetAudit=false /p:RestoreRecursive=false /m:2 /nr:false *> (Join-Path $results 'paired-restore.log')
    if($LASTEXITCODE){throw 'Cached paired restore failed.'}
    foreach($relative in @('Providers/Cloud/NextGenSoftware.OASIS.API.Providers.FirebaseOASIS','Providers/Storage/NextGenSoftware.OASIS.API.Providers.MongoOASIS',$project)){
        $producer=if([IO.Path]::IsPathRooted($relative)){$relative}else{Join-Path $root $relative}
        & dotnet build $producer --no-restore /m:2 /nr:false /p:UseSharedCompilation=false /p:BuildProjectReferences=false *> (Join-Path $results ((Split-Path $producer -Leaf)+'-paired-build.log'))
        if($LASTEXITCODE){throw 'Paired build failed.'}
    }
    $runner=Join-Path $root 'Scripts/TestHosts/run-cosmic-firebase-native.py'
    if($Serve){& python $runner --federated --serve}
    else{& python $runner --federated}
    if($LASTEXITCODE){throw 'Firebase native/export-restore/paired verification incomplete; preserve source and journals.'}
}
