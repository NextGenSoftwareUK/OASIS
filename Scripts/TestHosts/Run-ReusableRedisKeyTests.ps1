#requires -Version 7.5
param([switch]$SharedAdapter,[switch]$PrivateProvider,[switch]$ConformanceOnly,[switch]$ReconcileConformance)
$ErrorActionPreference='Stop'
# Caller owns the global provider build lease; this is the one Redis environment owner.
$redisRepo=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
$redisWork=Join-Path $redisRepo 'TestResults/RedisCurrent'
$redisConformanceBuild=$ConformanceOnly -or $ReconcileConformance
if($ConformanceOnly){
    foreach($mode in @('async','sync')){
        $prior=Join-Path $redisRepo ('TestResults/ProviderConformanceCurrent/RedisOASIS/'+$mode+'/report.json')
        if((Test-Path -LiteralPath $prior) -or (Test-Path -LiteralPath ($prior+'.next'))){throw 'Review preserved Redis conformance evidence before another run.'}
    }
}
if($redisConformanceBuild){
    foreach($name in @('storage-key.dpapi','test-auth.dpapi')){
        if(!(Test-Path -LiteralPath (Join-Path $env:LOCALAPPDATA ('OASIS/ReusableProviderTests/Redis/'+$name)))){throw 'Existing Redis credentials/key required; no generation/replacement.'}
    }
    & wsl -d Ubuntu -u root -- podman container exists oasis-redis-retained
    if($LASTEXITCODE){throw 'Retained Redis container required; no creation.'}
}
New-Item -ItemType Directory -Force -Path $redisWork|Out-Null
$redisLease=[IO.File]::Open((Join-Path $redisWork 'environment.lock'),[IO.FileMode]::OpenOrCreate,[IO.FileAccess]::ReadWrite,[IO.FileShare]::None)
$redisStarted=$false
$redisOldEndpoint=$env:OASIS_REDIS_NATIVE_ENDPOINT;$redisOldWork=$env:OASIS_REDIS_NATIVE_WORK
$redisOldPassword=$env:OASIS_REDIS_NATIVE_PASSWORD
$redisOldStorageKey=$env:OASIS_REDIS_STORAGE_ENCRYPTION_KEY
try{
    if(@($SharedAdapter,$PrivateProvider,$ConformanceOnly,$ReconcileConformance).Where({$_.IsPresent}).Count -gt 1){throw 'Choose one native evidence cohort.'}
    if(!$SharedAdapter -and !$PrivateProvider -and !$redisConformanceBuild){throw 'Select -PrivateProvider for current encrypted storage, or -SharedAdapter for transport-only evidence. Legacy plaintext fixture execution is retired.'}
    if(Get-ChildItem -LiteralPath $redisWork -Filter '*.pending.json'){throw 'Original Redis outcome unresolved; no replay or replacement'}
    $redisProofNames=if($ReconcileConformance){@('redis-core-reconciliation.trx')}elseif($ConformanceOnly){@('redis-core-conformance.trx')}elseif($PrivateProvider){@('redis-private-provider-current.trx','redis-private-preflight.trx','private-provider-empty.json','private-provider-async.resolved.json','private-provider-sync.resolved.json','legacy-admission-async.resolved.json','legacy-admission-sync.resolved.json')}else{@('redis-shared-adapter-native-current.trx','shared-adapter.resolved.json')}
    foreach($redisProofName in $redisProofNames){
        if(Test-Path -LiteralPath (Join-Path $redisWork $redisProofName)){throw 'Preserve and hash-verify predecessor Redis evidence before another run'}
    }
    $redisProvider=Join-Path $redisRepo 'Providers/Storage/NextGenSoftware.OASIS.API.Providers.RedisOASIS/NextGenSoftware.OASIS.API.Providers.RedisOASIS.csproj'
    $redisProject=Join-Path $redisRepo 'Providers/Storage/TestProjects/NextGenSoftware.OASIS.API.Providers.RedisOASIS.IntegrationTests/NextGenSoftware.OASIS.API.Providers.RedisOASIS.IntegrationTests.csproj'
    if(!(Test-Path -LiteralPath (Join-Path (Split-Path $redisProject) 'obj/project.assets.json'))){
        & dotnet restore $redisProject --no-dependencies --source 'C:/Users/david/.nuget/packages' /p:NuGetAudit=false -v:minimal
        if($LASTEXITCODE){throw 'Existing SDK/test cache insufficient; inspect missing packages, no automatic download'}
    }
    foreach($redisBuildProject in @($redisProvider,$redisProject)){
        if($redisConformanceBuild){
            & dotnet restore $redisBuildProject --source (Join-Path $env:USERPROFILE '.nuget/packages') --source 'C:/Program Files/dotnet/library-packs' /p:RestoreRecursive=false /p:NuGetAudit=false /p:IncludeCoreStorageConformance=true /m:2 /nr:false -v:q
            if($LASTEXITCODE){throw 'Pinned Redis package metadata unavailable; restore reviewed packages once to the shared cache, not another environment.'}
        }
        & dotnet build $redisBuildProject --no-restore /p:IncludeCoreStorageConformance=$($redisConformanceBuild.ToString().ToLowerInvariant()) /p:BuildProjectReferences=false /m:2 /nr:false /p:UseSharedCompilation=false -v:q -clp:ErrorsOnly
        if($LASTEXITCODE){throw 'Cached Redis native build failed'}
    }
    $redisDependencies=@('Core','Shared')
    & (Join-Path $PSScriptRoot 'Assert-ReusableProviderBuildIdentity.ps1') -RepositoryRoot $redisRepo -ConsumerOutput (Join-Path (Split-Path $redisProject) 'bin/Debug/net10.0') -Dependencies $redisDependencies -VerifySourceDocuments
    if($redisConformanceBuild){
        . (Join-Path $PSScriptRoot 'Assert-ReusableProviderBuildIdentity.ps1') -RepositoryRoot $redisRepo -ConsumerOutput (Join-Path (Split-Path $redisProject) 'bin/Debug/net10.0') -VerifySourceDocuments
        foreach($assembly in @('NextGenSoftware.OASIS.API.Providers.RedisOASIS','NextGenSoftware.OASIS.API.Providers.RedisOASIS.IntegrationTests')){
            Write-Output ($assembly+': sources='+(Assert-SourceDocuments (Join-Path (Split-Path $redisProject) ('bin/Debug/net10.0/'+$assembly+'.dll')) $redisRepo))
        }
    }
    & (Join-Path $PSScriptRoot 'Use-ReusableRedisContainer.ps1')
    $redisStarted=$true
    $redisSecure=& (Join-Path $PSScriptRoot 'Get-ReusableRedisTestCredential.ps1')
    try{$env:OASIS_REDIS_NATIVE_PASSWORD=[Net.NetworkCredential]::new('', $redisSecure).Password}finally{$redisSecure.Dispose()}
    $env:OASIS_REDIS_NATIVE_ENDPOINT='127.0.0.1:18679';$env:OASIS_REDIS_NATIVE_WORK=$redisWork
    if($PrivateProvider){
        & dotnet test $redisProject --no-build --no-restore --filter 'TestCategory=OwnedNativePrivatePreflight' --logger 'trx;LogFileName=redis-private-preflight.trx' --results-directory $redisWork /m:2 /nr:false -v:q
        if($LASTEXITCODE){throw 'Readonly native empty-store admission failed; no key initialization or mutation.'}
        [xml]$redisPreflight=Get-Content -LiteralPath (Join-Path $redisWork 'redis-private-preflight.trx') -Raw
        if([int]$redisPreflight.TestRun.ResultSummary.Counters.total -ne 1 -or [int]$redisPreflight.TestRun.ResultSummary.Counters.passed -ne 1){throw 'Exactly one passing native preflight required.'}
        $redisStorageSecure=& (Join-Path $PSScriptRoot 'Get-ReusableRedisStorageKey.ps1') -CreateIfMissing -EmptyStoreProof (Join-Path $redisWork 'private-provider-empty.json')
        try{$env:OASIS_REDIS_STORAGE_ENCRYPTION_KEY=[Net.NetworkCredential]::new('', $redisStorageSecure).Password}finally{$redisStorageSecure.Dispose()}
    }
    if($redisConformanceBuild){
        $redisStorageSecure=& (Join-Path $PSScriptRoot 'Get-ReusableRedisStorageKey.ps1')
        try{$env:OASIS_REDIS_STORAGE_ENCRYPTION_KEY=[Net.NetworkCredential]::new('', $redisStorageSecure).Password}finally{$redisStorageSecure.Dispose()}
    }
    $redisTestStart=[DateTime]::UtcNow
    $redisFilter=if($ReconcileConformance){'FullyQualifiedName~ReconcileKnownCoreSoftDeleteFixtures'}elseif($ConformanceOnly){'FullyQualifiedName~CoreStorageSuiteAgainstRetainedRedis'}elseif($PrivateProvider){'TestCategory=OwnedNativePrivateProvider|TestCategory=OwnedNativeLegacyLayoutAdmission'}else{'TestCategory=OwnedNativeSharedAdapter'}
    $redisReportName=if($ReconcileConformance){'redis-core-reconciliation.trx'}elseif($ConformanceOnly){'redis-core-conformance.trx'}elseif($PrivateProvider){'redis-private-provider-current.trx'}elseif($SharedAdapter){'redis-shared-adapter-native-current.trx'}else{'redis-native-key-current.trx'}
    & dotnet test $redisProject --no-build --no-restore --filter $redisFilter --logger "trx;LogFileName=$redisReportName" --results-directory $redisWork /m:2 /nr:false -v:q
    $redisTestExit=$LASTEXITCODE
    if($ConformanceOnly){
        & python (Join-Path $redisRepo 'Scripts/Providers/record_conformance_results.py') --providers RedisOASIS
        if($LASTEXITCODE){throw 'Redis shared report registration failed.'}
        & python (Join-Path $redisRepo 'Scripts/Providers/update_provider_matrix.py')
        if($LASTEXITCODE){throw 'Redis matrix update failed.'}
    }
    if($redisTestExit){throw 'Redis native guard failed; retain original evidence and unresolved fixtures'}
    $redisReport=Get-Item -LiteralPath (Join-Path $redisWork $redisReportName)
    [xml]$redisTrx=Get-Content -LiteralPath $redisReport.FullName -Raw
    $redisCounts=$redisTrx.TestRun.ResultSummary.Counters
    $redisExpected=if($redisConformanceBuild){2}elseif($SharedAdapter){1}else{4}
    if($redisReport.LastWriteTimeUtc -lt $redisTestStart -or [int]$redisCounts.total -ne $redisExpected -or [int]$redisCounts.passed -ne $redisExpected -or [int]$redisCounts.notExecuted -ne 0){throw 'Fresh complete native evidence required'}
    if(Get-ChildItem -LiteralPath $redisWork -Filter '*.pending.json'){throw 'Redis originals remain unresolved'}
}finally{
    $env:OASIS_REDIS_NATIVE_ENDPOINT=$redisOldEndpoint;$env:OASIS_REDIS_NATIVE_WORK=$redisOldWork
    $env:OASIS_REDIS_NATIVE_PASSWORD=$redisOldPassword
    $env:OASIS_REDIS_STORAGE_ENCRYPTION_KEY=$redisOldStorageKey
    try{
        if($redisStarted -and !(Get-ChildItem -LiteralPath $redisWork -Filter '*.pending.json')){& (Join-Path $PSScriptRoot 'Use-ReusableRedisContainer.ps1') -Stop}
        elseif($redisStarted){Write-Warning 'Preserve original Redis container/data; unresolved journal exists.'}
    }finally{$redisLease.Dispose()}
}
