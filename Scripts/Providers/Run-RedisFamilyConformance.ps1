#Requires -Version 7.0
[CmdletBinding()]
param()
$ErrorActionPreference='Stop'
$root=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
foreach($name in @('KeyDBOASIS','ValKeyOASIS','DragonflyOASIS','GarnetOASIS')){
    foreach($mode in @('async','sync')){
        $report=Join-Path $root ('TestResults/ProviderConformanceCurrent/'+$name+'/'+$mode+'/report.json')
        if((Test-Path -LiteralPath $report) -or (Test-Path -LiteralPath ($report+'.next'))){throw 'Review preserved Redis-family conformance evidence before another run.'}
    }
}
$results=Join-Path $root 'TestResults/ProviderConformanceCurrent'
if(Test-Path -LiteralPath (Join-Path $results 'redis-family-controlled-v2.trx')){throw 'Preserve the prior controlled family report.'}
& (Join-Path $root 'Scripts/TestHosts/Invoke-WithProviderBuildLease.ps1') -Owner 'Redis compatible family controlled conformance' -Operation {
    $projects=@('Providers/Storage/NextGenSoftware.OASIS.API.Providers.RedisOASIS/NextGenSoftware.OASIS.API.Providers.RedisOASIS.csproj')
    foreach($name in @('KeyDBOASIS','ValKeyOASIS','DragonflyOASIS','GarnetOASIS')){$projects+=('Providers/Storage/NextGenSoftware.OASIS.API.Providers.'+$name+'/NextGenSoftware.OASIS.API.Providers.'+$name+'.csproj')}
    $projects+='Providers/Storage/TestProjects/NextGenSoftware.OASIS.API.Providers.RedisOASIS.UnitTests/NextGenSoftware.OASIS.API.Providers.RedisOASIS.UnitTests.csproj'
    foreach($relative in $projects){
        $project=Join-Path $root $relative
        & dotnet restore $project --source (Join-Path $env:USERPROFILE '.nuget/packages') --source 'C:/Program Files/dotnet/library-packs' /p:RestoreRecursive=false /p:NuGetAudit=false /p:IncludeRedisFamilyContracts=true /m:2 /nr:false -v:q
        if($LASTEXITCODE){throw 'Cached Redis-family metadata unavailable; no automatic download or duplicate environment.'}
        & dotnet build $project --no-restore /p:IncludeRedisFamilyContracts=true /p:BuildProjectReferences=false /m:2 /nr:false /p:UseSharedCompilation=false -v:q -clp:ErrorsOnly
        if($LASTEXITCODE){throw 'Redis-family producer/consumer build failed.'}
    }
    $consumer=Join-Path (Split-Path $project) 'bin/Debug/net10.0'
    . (Join-Path $root 'Scripts/TestHosts/Assert-ReusableProviderBuildIdentity.ps1') -RepositoryRoot $root -ConsumerOutput $consumer -VerifySourceDocuments
    foreach($assembly in @('NextGenSoftware.OASIS.API.Providers.RedisOASIS','NextGenSoftware.OASIS.API.Providers.KeyDBOASIS','NextGenSoftware.OASIS.API.Providers.ValKeyOASIS','NextGenSoftware.OASIS.API.Providers.DragonflyOASIS','NextGenSoftware.OASIS.API.Providers.GarnetOASIS','NextGenSoftware.OASIS.API.Providers.RedisOASIS.UnitTests')){
        Write-Output ($assembly+': sources='+(Assert-SourceDocuments (Join-Path $consumer ($assembly+'.dll')) $root))
    }
    & dotnet test $project --no-build --no-restore /p:IncludeRedisFamilyContracts=true --filter 'FullyQualifiedName~RedisFamilyContractTests' --logger 'trx;LogFileName=redis-family-controlled-v2.trx' --results-directory $results -v:q
    $testExit=$LASTEXITCODE
    & python (Join-Path $PSScriptRoot 'record_conformance_results.py') --providers KeyDBOASIS ValKeyOASIS DragonflyOASIS GarnetOASIS
    if($LASTEXITCODE){throw 'Controlled family report registration failed.'}
    & python (Join-Path $PSScriptRoot 'update_provider_matrix.py')
    if($LASTEXITCODE){throw 'Family matrix update failed.'}
    if($testExit){throw 'Redis-family controlled gate failed; no native certification or Dev promotion.'}
}
