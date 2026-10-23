#requires -Version 7.5
$ErrorActionPreference='Stop'
# Caller owns the global provider build lease; this is the one Redis environment owner.
$redisRepo=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
$redisWork=Join-Path $redisRepo 'TestResults/RedisCurrent'
New-Item -ItemType Directory -Force -Path $redisWork|Out-Null
$redisLease=[IO.File]::Open((Join-Path $redisWork 'environment.lock'),[IO.FileMode]::OpenOrCreate,[IO.FileAccess]::ReadWrite,[IO.FileShare]::None)
$redisStarted=$false
$redisOldEndpoint=$env:OASIS_REDIS_NATIVE_ENDPOINT;$redisOldWork=$env:OASIS_REDIS_NATIVE_WORK
$redisOldPassword=$env:OASIS_REDIS_NATIVE_PASSWORD
try{
    if(Get-ChildItem -LiteralPath $redisWork -Filter '*.pending.json'){throw 'Original Redis outcome unresolved; no replay or replacement'}
    foreach($redisProofName in @('redis-native-key-current.trx','key-guard-async.snapshot.json','key-guard-sync.snapshot.json','key-guard-async.resolved.json','key-guard-sync.resolved.json')){
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
    & (Join-Path $PSScriptRoot 'Assert-ReusableProviderBuildIdentity.ps1') -RepositoryRoot $redisRepo -ConsumerOutput (Join-Path (Split-Path $redisProject) 'bin/Debug/net10.0') -Dependencies Core -VerifySourceDocuments
    & (Join-Path $PSScriptRoot 'Use-ReusableRedisContainer.ps1')
    $redisStarted=$true
    $redisSecure=& (Join-Path $PSScriptRoot 'Get-ReusableRedisTestCredential.ps1')
    try{$env:OASIS_REDIS_NATIVE_PASSWORD=[Net.NetworkCredential]::new('', $redisSecure).Password}finally{$redisSecure.Dispose()}
    $env:OASIS_REDIS_NATIVE_ENDPOINT='127.0.0.1:18679';$env:OASIS_REDIS_NATIVE_WORK=$redisWork
    $redisTestStart=[DateTime]::UtcNow
    & dotnet test $redisProject --no-build --no-restore --filter 'TestCategory=OwnedNativeKeyDeletion' --logger 'trx;LogFileName=redis-native-key-current.trx' --results-directory $redisWork /m:2 /nr:false -v:q
    if($LASTEXITCODE){throw 'Redis native guard failed; retain original evidence and unresolved fixtures'}
    $redisReport=Get-Item -LiteralPath (Join-Path $redisWork 'redis-native-key-current.trx')
    [xml]$redisTrx=Get-Content -LiteralPath $redisReport.FullName -Raw
    $redisCounts=$redisTrx.TestRun.ResultSummary.Counters
    if($redisReport.LastWriteTimeUtc -lt $redisTestStart -or [int]$redisCounts.total -ne 2 -or [int]$redisCounts.passed -ne 2 -or [int]$redisCounts.notExecuted -ne 0){throw 'Fresh complete2/2 native guard evidence required'}
    if(Get-ChildItem -LiteralPath $redisWork -Filter '*.pending.json'){throw 'Redis originals remain unresolved'}
}finally{
    $env:OASIS_REDIS_NATIVE_ENDPOINT=$redisOldEndpoint;$env:OASIS_REDIS_NATIVE_WORK=$redisOldWork
    $env:OASIS_REDIS_NATIVE_PASSWORD=$redisOldPassword
    try{
        if($redisStarted -and !(Get-ChildItem -LiteralPath $redisWork -Filter '*.pending.json')){& (Join-Path $PSScriptRoot 'Use-ReusableRedisContainer.ps1') -Stop}
        elseif($redisStarted){Write-Warning 'Preserve original Redis container/data; unresolved journal exists.'}
    }finally{$redisLease.Dispose()}
}
