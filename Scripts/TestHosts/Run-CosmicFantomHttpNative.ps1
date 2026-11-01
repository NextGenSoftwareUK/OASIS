#Requires -Version 7.0
# Reuse the fixed Fantom Opera catalogue and existing local Mongo replica set.
# The original fixture journal is never removed or replayed after this gate.
$ErrorActionPreference = 'Stop'
$root = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
Set-Location $root
$results = Join-Path $root 'TestResults/FantomPublicSourceCurrent'
$state = Join-Path $root 'LocalState/COSMIC/FantomMongoHttpCurrent'
if (!(Test-Path -LiteralPath (Join-Path $root 'LocalState/COSMIC/HostedFantomPublicSourceCurrent/source.json'))) {
    throw 'Retained Fantom Opera source is required; no implicit setup.'
}
if ((Test-Path -LiteralPath $state) -and @(Get-ChildItem -LiteralPath $state -File).Count) {
    throw 'Original HTTP fixture exists; reconcile it instead of replaying the gate.'
}
if (Test-Path -LiteralPath (Join-Path $results 'mongo-http-native.trx')) { throw 'Original result must be preserved.' }
New-Item -ItemType Directory -Force -Path $results | Out-Null
& (Join-Path $PSScriptRoot 'Invoke-WithProviderBuildLease.ps1') -Owner cosmic-fantom-http -Operation {
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
        dotnet test $project --no-build --no-restore --filter 'FullyQualifiedName~CqlFantomMongoNativeTests' --logger 'trx;LogFileName=mongo-http-native.trx' --results-directory $results *> (Join-Path $results 'mongo-http-native.log')
        if ($LASTEXITCODE) { throw 'Fantom/Mongo HTTP gate failed; preserve the original fixture outcome.' }
        [xml]$report = Get-Content -LiteralPath (Join-Path $results 'mongo-http-native.trx')
        $counts = $report.TestRun.ResultSummary.Counters
        if ($counts.total -ne '1' -or $counts.passed -ne '1' -or $counts.failed -ne '0') { throw 'Exactly one native HTTP test must pass without skips.' }
        if (!(Test-Path -LiteralPath (Join-Path $state 'fixture.cleaned.json'))) { throw 'Fixture cleanup is not acknowledged.' }
        Get-Content -LiteralPath (Join-Path $results 'mongo-http-native.log') -Tail 6
    } finally {
        $env:COSMIC_REPLICA_REPAIR_NATIVE = $prior
        if ($ownedMongo) {
            if (!$ownedMongo.HasExited) { $ownedMongo.Kill(); if (!$ownedMongo.WaitForExit(30000)) { throw 'Owned Mongo teardown incomplete.' } }
            $ownedMongo.Dispose()
        }
    }
}
