#Requires -Version 7.0
# Reuse the fixed public Nostr catalogue and existing local Mongo replica set.
# The original fixture journal is never removed or replayed after this gate.
param([switch]$ContinueAcknowledgedFixture)
$ErrorActionPreference = 'Stop'
$root = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
Set-Location $root
$results = Join-Path $root 'TestResults/NostrPublicSourceCurrent'
$state = Join-Path $root 'LocalState/COSMIC/NostrMongoHttpCurrent'
if (!(Test-Path -LiteralPath (Join-Path $root 'LocalState/COSMIC/HostedNostrPublicSourceCurrent/source.json'))) {
    throw 'Retained public Nostr source is required; no implicit setup.'
}
$runLabel = if ($ContinueAcknowledgedFixture) { 'mongo-http-continuation' } else { 'mongo-http-native' }
if (Test-Path -LiteralPath (Join-Path $results ($runLabel + '.trx'))) { throw 'Original result must be preserved.' }
if ($ContinueAcknowledgedFixture) {
    foreach ($name in @('fixture.intent.json','save-0.acknowledged.json','save-1.acknowledged.json')) {
        if (!(Test-Path -LiteralPath (Join-Path $state $name))) { throw 'Original acknowledged fixture required; no seed.' }
    }
    [xml]$failed = Get-Content -LiteralPath (Join-Path $results 'mongo-http-native.trx')
    if ($failed.TestRun.ResultSummary.Counters.failed -ne '1') { throw 'Preserved original failed test required.' }
    $source = Join-Path $root 'LocalState/COSMIC/HostedNostrPublicSourceCurrent/source.json'
    $nativeAck = Get-Content -LiteralPath (Join-Path $root 'LocalState/COSMIC/HostedNostrPublicSourceCurrent/native-qualified.acknowledged.json') -Raw | ConvertFrom-Json
    $sourceHash = (Get-FileHash -LiteralPath $source -Algorithm SHA256).Hash.ToLowerInvariant()
    if ($sourceHash -cne $nativeAck.sourceSha256) { throw 'Original source changed.' }
    $journal = @{ fixtureSha256 = (Get-FileHash -LiteralPath (Join-Path $state 'fixture.intent.json') -Algorithm SHA256).Hash.ToLowerInvariant();
        sourceSha256 = $sourceHash; failedTrxSha256 = (Get-FileHash -LiteralPath (Join-Path $results 'mongo-http-native.trx') -Algorithm SHA256).Hash.ToLowerInvariant();
        operation = 'read-only continuation after test policy assertion correction'; additionalSaves = 0; retryAllowed = $false }
    $stream = [IO.File]::Open((Join-Path $state 'read-continuation.intent.json'),[IO.FileMode]::CreateNew)
    try { $stream.Write([Text.Encoding]::UTF8.GetBytes(($journal | ConvertTo-Json -Compress))); $stream.Flush($true) } finally { $stream.Dispose() }
} elseif ((Test-Path -LiteralPath $state) -and @(Get-ChildItem -LiteralPath $state -File).Count) {
    throw 'Original HTTP fixture exists; reconcile it instead of replaying the gate.'
}
New-Item -ItemType Directory -Force -Path $results | Out-Null
& (Join-Path $PSScriptRoot 'Invoke-WithProviderBuildLease.ps1') -Owner cosmic-nostr-http -Operation {
    $mongoRuntime = 'C:/Program Files/MongoDB/Server/8.3/bin/mongod.exe'
    $mongoData = Join-Path $root 'LocalState/COSMIC/mongo-data'
    if (!(Test-Path -LiteralPath (Join-Path $mongoData 'WiredTiger'))) { throw 'Existing Mongo environment missing; no reset.' }
    if (Get-NetTCPConnection -State Listen -LocalPort 27029 -ErrorAction SilentlyContinue) { throw 'Mongo port occupied; do not adopt another process.' }
    if (((& $mongoRuntime --version) -join ' ') -notmatch 'v8\.3\.11\b') { throw 'Pinned Mongo runtime differs.' }
    $project = 'ONODE/NextGenSoftware.OASIS.API.ONODE.WebAPI.UnitTests/NextGenSoftware.OASIS.API.ONODE.WebAPI.UnitTests.csproj'
    dotnet build $project --no-restore /m:2 /nr:false /p:UseSharedCompilation=false /p:BuildProjectReferences=false -v:q -clp:ErrorsOnly *> (Join-Path $results ($runLabel + '-build.log'))
    if ($LASTEXITCODE) { throw 'HTTP consumer build failed.' }
    $ownedMongo = $null
    $prior = $env:COSMIC_REPLICA_REPAIR_NATIVE
    $priorContinuation = $env:COSMIC_NOSTR_HTTP_CONTINUATION
    try {
        $ownedMongo = Start-Process $mongoRuntime -ArgumentList @('--dbpath',$mongoData,'--bind_ip','127.0.0.1','--port','27029','--replSet','oasisRecoveryRs','--wiredTigerCacheSizeGB','0.25','--oplogSize','32','--logpath',(Join-Path $results ($runLabel + '-service.log')),'--logappend') -WindowStyle Hidden -PassThru
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
        $env:COSMIC_NOSTR_HTTP_CONTINUATION = if ($ContinueAcknowledgedFixture) { 'acknowledged-read-only' } else { $null }
        dotnet test $project --no-build --no-restore --filter 'FullyQualifiedName~CqlNostrMongoNativeTests' --logger ('trx;LogFileName=' + $runLabel + '.trx') --results-directory $results *> (Join-Path $results ($runLabel + '.log'))
        if ($LASTEXITCODE) { throw 'Nostr/Mongo HTTP gate failed; preserve the original fixture outcome.' }
        [xml]$report = Get-Content -LiteralPath (Join-Path $results ($runLabel + '.trx'))
        $counts = $report.TestRun.ResultSummary.Counters
        if ($counts.total -ne '1' -or $counts.passed -ne '1' -or $counts.failed -ne '0') { throw 'Exactly one native HTTP test must pass without skips.' }
        if (!(Test-Path -LiteralPath (Join-Path $state 'fixture.cleaned.json'))) { throw 'Fixture cleanup is not acknowledged.' }
        Get-Content -LiteralPath (Join-Path $results ($runLabel + '.log')) -Tail 6
    } finally {
        $env:COSMIC_REPLICA_REPAIR_NATIVE = $prior
        $env:COSMIC_NOSTR_HTTP_CONTINUATION = $priorContinuation
        if ($ownedMongo) {
            if (!$ownedMongo.HasExited) { $ownedMongo.Kill(); if (!$ownedMongo.WaitForExit(30000)) { throw 'Owned Mongo teardown incomplete.' } }
            $ownedMongo.Dispose()
        }
    }
}
