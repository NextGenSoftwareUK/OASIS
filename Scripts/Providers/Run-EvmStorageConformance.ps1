#Requires -Version 7.0
[CmdletBinding()]
param()
$ErrorActionPreference='Stop'
$root=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
foreach($name in @('EthereumOASIS','ArbitrumOASIS','PolygonOASIS')) {
    foreach($mode in @('async','sync')) {
        $path=Join-Path $root ('TestResults/ProviderConformanceCurrent/'+$name+'/'+$mode+'/report.json')
        if((Test-Path -LiteralPath $path) -or (Test-Path -LiteralPath ($path+'.next'))){throw 'Review preserved EVM conformance evidence before another run.'}
    }
}
& (Join-Path $root 'Scripts/TestHosts/Invoke-WithProviderBuildLease.ps1') -Owner 'EVM family core storage breadth gate' -Operation {
    $projects=@(
        'Providers/Blockchain/NextGenSoftware.OASIS.API.Providers.Web3CoreOASIS/NextGenSoftware.OASIS.API.Providers.Web3CoreOASIS.csproj',
        'Providers/Blockchain/NextGenSoftware.OASIS.API.Providers.EthereumOASIS/NextGenSoftware.OASIS.API.Providers.EthereumOASIS.csproj',
        'Providers/Blockchain/NextGenSoftware.OASIS.API.Providers.PolygonOASIS/NextGenSoftware.OASIS.API.Providers.PolygonOASIS.csproj',
        'Providers/Blockchain/NextGenSoftware.OASIS.API.Providers.ArbitrumOASIS/NextGenSoftware.OASIS.API.Providers.ArbitrumOASIS.csproj',
        'Providers/Blockchain/TestProjects/NextGenSoftware.OASIS.API.Providers.Web3CoreOASIS.UnitTests/NextGenSoftware.OASIS.API.Providers.Web3CoreOASIS.UnitTests.csproj')
    foreach($relative in $projects) {
        $project=Join-Path $root $relative
        & dotnet restore $project --source (Join-Path $env:USERPROFILE '.nuget/packages') --source 'C:/Program Files/dotnet/library-packs' /p:RestoreRecursive=false /p:NuGetAudit=false /p:IncludeCoreStorageConformance=true /m:2 /nr:false -v:q
        if($LASTEXITCODE){throw 'Cached EVM metadata restore failed; record blocker instead of installing another environment.'}
        & dotnet build $project --no-restore /p:IncludeCoreStorageConformance=true /p:BuildProjectReferences=false /m:2 /nr:false /p:UseSharedCompilation=false -v:q -clp:ErrorsOnly
        if($LASTEXITCODE){throw "EVM family build failed: $relative"}
    }
    $consumer=Join-Path (Split-Path $project) 'bin/Debug/net10.0'
    . (Join-Path $root 'Scripts/TestHosts/Assert-ReusableProviderBuildIdentity.ps1') -RepositoryRoot $root -ConsumerOutput $consumer -VerifySourceDocuments
    foreach($name in @('NextGenSoftware.OASIS.API.Providers.Web3CoreOASIS','NextGenSoftware.OASIS.API.Providers.EthereumOASIS','NextGenSoftware.OASIS.API.Providers.PolygonOASIS','NextGenSoftware.OASIS.API.Providers.ArbitrumOASIS','NextGenSoftware.OASIS.API.Providers.Web3CoreOASIS.UnitTests')) {
        Write-Output ($name+': sources='+(Assert-SourceDocuments (Join-Path $consumer ($name+'.dll')) $root))
    }
    $results=Join-Path $root 'TestResults/ProviderConformanceCurrent'
    if(Test-Path -LiteralPath (Join-Path $results 'evm-family-current.trx')){throw 'Preserve the previous EVM report.'}
    & dotnet test $project --no-build --no-restore /p:IncludeCoreStorageConformance=true --filter 'FullyQualifiedName~EvmStorageConformanceTests|FullyQualifiedName~EvmFamilyFactoryTests' --logger 'trx;LogFileName=evm-family-current.trx' --results-directory $results -v:q
    $testExit=$LASTEXITCODE
    & python (Join-Path $PSScriptRoot 'record_conformance_results.py') --providers EthereumOASIS ArbitrumOASIS PolygonOASIS
    if($LASTEXITCODE){throw 'EVM shared report registration failed.'}
    & python (Join-Path $PSScriptRoot 'update_provider_matrix.py')
    if($LASTEXITCODE){throw 'Provider matrix update failed.'}
    if($testExit){throw 'EVM conformance has recorded blockers; no routing/depth or Dev promotion.'}
}
