#requires -Version 7.5
param([switch]$SharedAdapter,[switch]$PrivateProvider)
$ErrorActionPreference='Stop'
# Caller owns the global provider build lease; this is the one Redis environment owner.
$redisRepo=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
$redisWork=Join-Path $redisRepo 'TestResults/RedisCurrent'
New-Item -ItemType Directory -Force -Path $redisWork|Out-Null
$redisLease=[IO.File]::Open((Join-Path $redisWork 'environment.lock'),[IO.FileMode]::OpenOrCreate,[IO.FileAccess]::ReadWrite,[IO.FileShare]::None)
$redisStarted=$false
$redisOldEndpoint=$env:OASIS_REDIS_NATIVE_ENDPOINT;$redisOldWork=$env:OASIS_REDIS_NATIVE_WORK
$redisOldPassword=$env:OASIS_REDIS_NATIVE_PASSWORD
$redisOldStorageKey=$env:OASIS_REDIS_STORAGE_ENCRYPTION_KEY
try{
    if($SharedAdapter -and $PrivateProvider){throw 'Choose one native evidence cohort.'}
    if(!$SharedAdapter -and !$PrivateProvider){throw 'Select -PrivateProvider for current encrypted storage, or -SharedAdapter for transport-only evidence. Legacy plaintext fixture execution is retired.'}
    if(Get-ChildItem -LiteralPath $redisWork -Filter '*.pending.json'){throw 'Original Redis outcome unresolved; no replay or replacement'}
    $redisProofNames=if($PrivateProvider){@('redis-private-provider-current.trx','redis-private-preflight.trx','private-provider-empty.json','private-provider-async.resolved.json','private-provider-sync.resolved.json','legacy-admission-async.resolved.json','legacy-admission-sync.resolved.json')}else{@('redis-shared-adapter-native-current.trx','shared-adapter.resolved.json')}
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
        & dotnet build $redisBuildProject --no-restore /p:BuildProjectReferences=false /m:2 /nr:false /p:UseSharedCompilation=false -v:q -clp:ErrorsOnly
        if($LASTEXITCODE){throw 'Cached Redis native build failed'}
    }
    $redisDependencies=@('Core','Shared')
    & (Join-Path $PSScriptRoot 'Assert-ReusableProviderBuildIdentity.ps1') -RepositoryRoot $redisRepo -ConsumerOutput (Join-Path (Split-Path $redisProject) 'bin/Debug/net10.0') -Dependencies $redisDependencies -VerifySourceDocuments
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
    $redisTestStart=[DateTime]::UtcNow
    $redisFilter=if($PrivateProvider){'TestCategory=OwnedNativePrivateProvider|TestCategory=OwnedNativeLegacyLayoutAdmission'}else{'TestCategory=OwnedNativeSharedAdapter'}
    $redisReportName=if($PrivateProvider){'redis-private-provider-current.trx'}elseif($SharedAdapter){'redis-shared-adapter-native-current.trx'}else{'redis-native-key-current.trx'}
    & dotnet test $redisProject --no-build --no-restore --filter $redisFilter --logger "trx;LogFileName=$redisReportName" --results-directory $redisWork /m:2 /nr:false -v:q
    if($LASTEXITCODE){throw 'Redis native guard failed; retain original evidence and unresolved fixtures'}
    $redisReport=Get-Item -LiteralPath (Join-Path $redisWork $redisReportName)
    [xml]$redisTrx=Get-Content -LiteralPath $redisReport.FullName -Raw
    $redisCounts=$redisTrx.TestRun.ResultSummary.Counters
    $redisExpected=if($SharedAdapter){1}else{4}
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
