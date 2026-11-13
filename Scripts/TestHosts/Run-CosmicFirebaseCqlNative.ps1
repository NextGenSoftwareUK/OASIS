param([switch]$Federated,[switch]$Serve)
$ErrorActionPreference='Stop'
if(!$Federated){throw 'Use the explicit Mongo-paired Firebase teaching preset.'}
$root=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
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
