param([switch]$Federated,[switch]$Serve)
$ErrorActionPreference='Stop'
if(!$Federated){throw 'Use the explicit Mongo-paired Linux teaching preset.'}
$root=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
$results=Join-Path $root 'TestResults/DragonflyCqlCurrent'
[void][IO.Directory]::CreateDirectory($results)
& (Join-Path $PSScriptRoot 'Invoke-WithProviderBuildLease.ps1') -Owner 'COSMIC Dragonfly Linux paired demo' -Operation {
    $project=Join-Path $root 'Providers/Storage/TestProjects/NextGenSoftware.OASIS.API.Providers.DragonflyOASIS.IntegrationTests'
    & dotnet restore $project --source C:/Users/david/.nuget/packages /p:NuGetAudit=false /p:RestoreRecursive=false /m:2 /nr:false *> (Join-Path $results 'paired-restore.log')
    if($LASTEXITCODE){throw 'Cached paired restore failed.'}
    foreach($name in @('RedisOASIS','DragonflyOASIS','MongoOASIS')){
        & dotnet build (Join-Path $root ('Providers/Storage/NextGenSoftware.OASIS.API.Providers.'+$name)) --no-restore /m:2 /nr:false /p:UseSharedCompilation=false /p:BuildProjectReferences=false *> (Join-Path $results ($name+'-paired-build.log'))
        if($LASTEXITCODE){throw 'Paired producer build failed.'}
    }
    & dotnet build $project --no-restore /m:2 /nr:false /p:UseSharedCompilation=false /p:BuildProjectReferences=false *> (Join-Path $results 'paired-build.log')
    if($LASTEXITCODE){throw 'Paired consumer build failed.'}
    $scriptPath=(Join-Path $root 'Scripts/TestHosts/run-cosmic-dragonfly-linux.py').Replace('\','/')
    $linuxPath=(& wsl -d Ubuntu -- wslpath -u $scriptPath).Trim()
    if($LASTEXITCODE){throw 'Worktree Linux path resolution failed.'}
    if($Serve){& wsl -d Ubuntu -- python3 $linuxPath --federated --serve}
    else{& wsl -d Ubuntu -- python3 $linuxPath --federated}
    if($LASTEXITCODE){throw 'Linux native/checkpoint/paired verification incomplete; inspect original journals.'}
}
