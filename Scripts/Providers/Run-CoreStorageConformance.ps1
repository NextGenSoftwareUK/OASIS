#Requires -Version 7.0
[CmdletBinding()]
param([switch]$ControlledOnly)
$ErrorActionPreference='Stop'
$root=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
$includeNative=(!$ControlledOnly)
if(!$ControlledOnly){foreach($mode in @('async','sync')) {
    $prior=Join-Path $root ('TestResults/ProviderConformanceCurrent/LocalFileOASIS/'+$mode+'/report.json')
    if((Test-Path -LiteralPath $prior) -or (Test-Path -LiteralPath ($prior+'.next'))) {
        throw 'Previous LocalFile fixture/evidence remains; inspect recorded blocker before rebuilding or dispatching another run.'
    }
}
}
& (Join-Path $root 'Scripts/TestHosts/Invoke-WithProviderBuildLease.ps1') -Owner 'Shared core storage conformance breadth gate' -Operation {
    $projects=@(
        'Providers/Storage/NextGenSoftware.OASIS.API.Providers.LocalFileOASIS/NextGenSoftware.OASIS.API.Providers.LocalFileOASIS.csproj',
        'Providers/Shared/NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage.UnitTests/NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage.UnitTests.csproj')
    if($ControlledOnly){$projects=@($projects[1])}
    foreach($relative in $projects) {
        $project=Join-Path $root $relative
        & dotnet restore $project --source (Join-Path $env:USERPROFILE '.nuget/packages') --source 'C:/Program Files/dotnet/library-packs' /p:RestoreRecursive=false /p:NuGetAudit=false /p:IncludeNativeStorageConformance=$($includeNative.ToString().ToLowerInvariant()) /m:2 /nr:false -v:q
        if($LASTEXITCODE){throw 'Cached metadata restore failed; no network fallback or duplicate environment.'}
        & dotnet build $project --no-restore /p:IncludeNativeStorageConformance=$($includeNative.ToString().ToLowerInvariant()) /p:BuildProjectReferences=false /m:2 /nr:false /p:UseSharedCompilation=false -v:q
        if($LASTEXITCODE){throw "Conformance dependency build failed: $relative"}
    }
    $consumer=Join-Path (Split-Path $project) 'bin/Debug/net10.0'
    . (Join-Path $root 'Scripts/TestHosts/Assert-ReusableProviderBuildIdentity.ps1') -RepositoryRoot $root -ConsumerOutput $consumer -VerifySourceDocuments
    $sourceAssemblies=if($ControlledOnly){@('NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage.UnitTests')}else{@('NextGenSoftware.OASIS.API.Providers.LocalFileOASIS','NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage.UnitTests')}
    foreach($name in $sourceAssemblies) {
        Write-Output ($name+': sources='+(Assert-SourceDocuments (Join-Path $consumer ($name+'.dll')) $root))
    }
    $results=Join-Path $root 'TestResults/ProviderConformanceCurrent'
    $reportName=if($ControlledOnly){'core-storage-v2-controls.trx'}else{'core-storage-current.trx'}
    if(Test-Path -LiteralPath (Join-Path $results $reportName)){throw 'Preserve previous conformance report before another run.'}
    $filter=if($ControlledOnly){'FullyQualifiedName~CoreStorageConformanceTests&FullyQualifiedName!~SameInterfaceSuiteRunsAgainstNativeLocalFile'}else{'FullyQualifiedName~CoreStorageConformanceTests'}
    & dotnet test $project --no-build --no-restore /p:IncludeNativeStorageConformance=$($includeNative.ToString().ToLowerInvariant()) --filter $filter --logger "trx;LogFileName=$reportName" --results-directory $results -v:q
    $testExit=$LASTEXITCODE
    if(!$ControlledOnly){
        & python (Join-Path $PSScriptRoot 'record_conformance_results.py')
        if($LASTEXITCODE){throw 'Conformance result registration failed; preserve original reports.'}
    }
    & python (Join-Path $PSScriptRoot 'update_provider_matrix.py')
    if($LASTEXITCODE){throw 'Provider matrix registration failed.'}
    if($testExit){throw 'Shared suite found blockers; inspect matrix and preserve failed fixtures. Do not advance routing/depth.'}
}
