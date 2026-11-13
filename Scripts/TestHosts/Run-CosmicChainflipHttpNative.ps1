#Requires -Version 7.0
param([switch]$ContinueOriginalReadOnly)
# Reuse the fixed public Chainflip catalogue and existing local Mongo replica set.
# The original fixture journal is never removed or replayed after this gate.
$ErrorActionPreference = 'Stop'
$root = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
Set-Location $root
$results = Join-Path $root 'TestResults/ChainflipPublicSourceCurrent'
$state = Join-Path $root 'LocalState/COSMIC/ChainflipMongoHttpCurrent'
if (!(Test-Path -LiteralPath (Join-Path $root 'LocalState/COSMIC/HostedChainflipPublicSourceCurrent/source.json'))) {
    throw 'Retained public Chainflip source is required; no implicit setup.'
}
$source = Join-Path $root 'LocalState/COSMIC/HostedChainflipPublicSourceCurrent/source.json'
$sourceHash = (Get-FileHash -LiteralPath $source -Algorithm SHA256).Hash.ToLowerInvariant()
$nativeAck = Get-Content -LiteralPath (Join-Path $root 'LocalState/COSMIC/HostedChainflipPublicSourceCurrent/native-qualified.acknowledged.json') -Raw | ConvertFrom-Json
if (!$nativeAck.nativeQualified -or $nativeAck.sourceSha256 -cne $sourceHash -or
    ($nativeAck.actualConsoleCounts -join ',') -cne '3,2') { throw 'Original source-bound native qualification required.' }
if ((Test-Path -LiteralPath $state) -and @(Get-ChildItem -LiteralPath $state -File).Count) {
    if (!$ContinueOriginalReadOnly) { throw 'Original HTTP fixture exists; explicit read-only completion required.' }
}
$resultName = if ($ContinueOriginalReadOnly) { 'mongo-http-native-readonly-completion' } else { 'mongo-http-native' }
if (Test-Path -LiteralPath (Join-Path $results ($resultName + '.trx'))) { throw 'Original result must be preserved; no replay.' }
if ($ContinueOriginalReadOnly) {
    foreach ($name in @('fixture.intent.json','save-0.acknowledged.json','save-1.acknowledged.json')) {
        if (!(Test-Path -LiteralPath (Join-Path $state $name))) { throw 'Original two acknowledged writes required.' }
    }
}
New-Item -ItemType Directory -Force -Path $results | Out-Null
& (Join-Path $PSScriptRoot 'Invoke-WithProviderBuildLease.ps1') -Owner cosmic-chainflip-http -Operation {
    $mongoRuntime = 'C:/Program Files/MongoDB/Server/8.3/bin/mongod.exe'
    $mongoData = Join-Path $root 'LocalState/COSMIC/mongo-data'
    if (!(Test-Path -LiteralPath (Join-Path $mongoData 'WiredTiger'))) { throw 'Existing Mongo environment missing; no reset.' }
    if (Get-NetTCPConnection -State Listen -LocalPort 27029 -ErrorAction SilentlyContinue) { throw 'Mongo port occupied; do not adopt another process.' }
    if (((& $mongoRuntime --version) -join ' ') -notmatch 'v8\.3\.11\b') { throw 'Pinned Mongo runtime differs.' }
    $project = 'ONODE/NextGenSoftware.OASIS.API.ONODE.WebAPI.UnitTests/NextGenSoftware.OASIS.API.ONODE.WebAPI.UnitTests.csproj'
    dotnet build $project --no-restore /m:2 /nr:false /p:UseSharedCompilation=false /p:BuildProjectReferences=false -v:q -clp:ErrorsOnly *> (Join-Path $results 'mongo-http-build.log')
    if ($LASTEXITCODE) { throw 'HTTP consumer build failed.' }
    $ownedMongo = $null
    $prior = $env:COSMIC_REPLICA_REPAIR_NATIVE
    $priorContinuation = $env:COSMIC_CHAINFLIP_HTTP_CONTINUATION
    try {
        $ownedMongo = Start-Process $mongoRuntime -ArgumentList @('--dbpath',$mongoData,'--bind_ip','127.0.0.1','--port','27029','--replSet','oasisRecoveryRs','--wiredTigerCacheSizeGB','0.25','--oplogSize','32','--logpath',(Join-Path $results 'mongo-service.log'),'--logappend') -WindowStyle Hidden -PassThru
        $deadline = [DateTime]::UtcNow.AddSeconds(20)
        do {
            if ($ownedMongo.HasExited) { throw 'Owned Mongo exited during readiness.' }
            $listeners = @(Get-NetTCPConnection -State Listen -LocalPort 27029 -ErrorAction SilentlyContinue)
            if ($listeners.Count) {
                if (@($listeners | Where-Object OwningProcess -ne $ownedMongo.Id).Count) { throw 'Mongo listener owner differs.' }
                break
            }
            Start-Sleep -Milliseconds 200
        } while ([DateTime]::UtcNow -lt $deadline)
        if (!$listeners.Count) { throw 'Owned Mongo readiness deadline exceeded.' }
        $env:COSMIC_REPLICA_REPAIR_NATIVE = 'owned-local-27029'
        $env:COSMIC_CHAINFLIP_HTTP_CONTINUATION = if ($ContinueOriginalReadOnly) { 'original-read-only' } else { $null }
        dotnet test $project --no-build --no-restore --filter 'FullyQualifiedName~CqlChainflipMongoNativeTests' --logger ("trx;LogFileName=" + $resultName + ".trx") --results-directory $results *> (Join-Path $results ($resultName + '.log'))
        if ($LASTEXITCODE) { throw 'Chainflip/Mongo HTTP gate failed; preserve the original fixture outcome.' }
        [xml]$report = Get-Content -LiteralPath (Join-Path $results ($resultName + '.trx'))
        $counts = $report.TestRun.ResultSummary.Counters
        if ($counts.total -ne '1' -or $counts.passed -ne '1' -or $counts.failed -ne '0') { throw 'Exactly one native HTTP test must pass without skips.' }
        if (!(Test-Path -LiteralPath (Join-Path $state 'fixture.cleaned.json'))) { throw 'Fixture cleanup is not acknowledged.' }
        Get-Content -LiteralPath (Join-Path $results ($resultName + '.log')) -Tail 6
    } finally {
        $env:COSMIC_REPLICA_REPAIR_NATIVE = $prior
        $env:COSMIC_CHAINFLIP_HTTP_CONTINUATION = $priorContinuation
        if ($ownedMongo) {
            if (!$ownedMongo.HasExited) { $ownedMongo.Kill(); if (!$ownedMongo.WaitForExit(30000)) { throw 'Owned Mongo teardown incomplete.' } }
            $ownedMongo.Dispose()
        }
    }
}
# Written only after the test, exact owned-fixture cleanup and process teardown.
if (Get-NetTCPConnection -State Listen -LocalPort 27029 -ErrorAction SilentlyContinue) { throw 'Mongo listener remains after owned teardown.' }
if ((Get-FileHash -LiteralPath $source -Algorithm SHA256).Hash.ToLowerInvariant() -cne $sourceHash) { throw 'Source configuration changed.' }
$ack = @{ schema = 1; web4LocalQualified = $true; sourceSha256 = $sourceHash;
    policy = 'indexer-reported-swap-state'; nativeCounts = @(3,2); federationCounts = @(5,4);
    foreignPrivate = 0; foreignPublic = 3; savedPrivateCopies = 2; fixtureCleaned = $true;
    hostedCertified = $false; browserCertified = $false }
$path = Join-Path $root 'LocalState/COSMIC/HostedChainflipPublicSourceCurrent/web4-local-qualified.acknowledged.json'
$stream = [IO.File]::Open($path, [IO.FileMode]::CreateNew)
try { $stream.Write([Text.Encoding]::UTF8.GetBytes(($ack | ConvertTo-Json -Compress))); $stream.Flush($true) } finally { $stream.Dispose() }
