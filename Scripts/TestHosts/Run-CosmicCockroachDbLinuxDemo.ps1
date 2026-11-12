param([switch]$Federated,[switch]$Serve)
$ErrorActionPreference='Stop'
if(!$Federated){throw 'Use the explicit Mongo-paired teaching preset.'}
$root=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
$results=Join-Path $root 'TestResults/CockroachDbCqlCurrent'
[void][IO.Directory]::CreateDirectory($results)
& (Join-Path $PSScriptRoot 'Invoke-WithProviderBuildLease.ps1') -Owner 'COSMIC CockroachDb paired demo' -Operation {
    $project=Join-Path $root 'Providers/Storage/TestProjects/NextGenSoftware.OASIS.API.Providers.CockroachDBOASIS.IntegrationTests'
    & dotnet restore $project --source C:/Users/david/.nuget/packages /p:NuGetAudit=false /p:RestoreRecursive=false /m:2 /nr:false *> (Join-Path $results 'paired-restore.log')
    if($LASTEXITCODE){throw 'Cached paired restore failed.'}
    foreach($relative in @('Providers/Storage/NextGenSoftware.OASIS.API.Providers.PostgreSQLOASIS','Providers/Storage/NextGenSoftware.OASIS.API.Providers.CockroachDBOASIS','Providers/Storage/NextGenSoftware.OASIS.API.Providers.MongoOASIS',$project)){
        $producer=if([IO.Path]::IsPathRooted($relative)){$relative}else{Join-Path $root $relative}
        & dotnet build $producer --no-restore /m:2 /nr:false /p:UseSharedCompilation=false /p:BuildProjectReferences=false *> (Join-Path $results ((Split-Path $producer -Leaf)+'-paired-build.log'))
        if($LASTEXITCODE){throw 'Paired build failed.'}
    }
    $scriptPath=(Join-Path $root 'Scripts/TestHosts/run-cosmic-cockroachdb-linux.py').Replace('\','/')
    $linuxPath=(& wsl -d Ubuntu -- wslpath -u $scriptPath).Trim()
    if($LASTEXITCODE){throw 'Worktree Linux path resolution failed.'}
    if($Serve){& wsl -d Ubuntu -- python3 $linuxPath --federated --serve}
    else{& wsl -d Ubuntu -- python3 $linuxPath --federated}
    if($LASTEXITCODE){throw 'Native/restart/paired verification incomplete; preserve original journals.'}
}
