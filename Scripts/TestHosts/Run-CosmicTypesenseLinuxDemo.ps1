param([switch]$Federated,[switch]$Serve)
if($Serve -and !$Federated){throw 'Serve requires federation.'}
$ErrorActionPreference='Stop'
$root=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
$results=Join-Path $root 'TestResults/TypesenseCqlCurrent'
[void][IO.Directory]::CreateDirectory($results)
& (Join-Path $PSScriptRoot 'Invoke-WithProviderBuildLease.ps1') -Owner 'COSMIC Typesense native demo' -Operation {
    $project=Join-Path $root 'Providers/Storage/TestProjects/NextGenSoftware.OASIS.API.Providers.TypesenseOASIS.IntegrationTests'
    & dotnet restore $project --source C:/Users/david/.nuget/packages /p:NuGetAudit=false /p:RestoreRecursive=false /m:2 /nr:false *> (Join-Path $results 'native-restore.log')
    if($LASTEXITCODE){throw 'Cached native restore failed.'}
    foreach($producer in @((Join-Path $root 'Providers/Storage/NextGenSoftware.OASIS.API.Providers.TypesenseOASIS'),$project)){
        & dotnet build $producer --no-restore /m:2 /nr:false /p:UseSharedCompilation=false /p:BuildProjectReferences=false *> (Join-Path $results ((Split-Path $producer -Leaf)+'-native-build.log'))
        if($LASTEXITCODE){throw 'Native build failed.'}
    }
    $scriptPath=(Join-Path $root 'Scripts/TestHosts/run-cosmic-typesense-linux.py').Replace('\','/')
    $linuxPath=(& wsl -d Ubuntu -- wslpath -u $scriptPath).Trim()
    if($LASTEXITCODE){throw 'Worktree path resolution failed.'}
    if($Serve){ & wsl -d Ubuntu -u root -- python3 -B $linuxPath --federated --serve }
    elseif($Federated){ & wsl -d Ubuntu -u root -- python3 -B $linuxPath --federated }
    else{ & wsl -d Ubuntu -u root -- python3 -B $linuxPath }
    if($LASTEXITCODE){throw 'Native/restart verification incomplete; preserve original journals.'}
}
