#Requires -Version 7.0
[CmdletBinding()]
param([ValidateSet('TheGraphOASIS')][string]$Provider = 'TheGraphOASIS')

$ErrorActionPreference = 'Stop'
$root = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
$results = Join-Path $root 'TestResults/TheGraphStudioCurrent'
$state = Join-Path $root "LocalState/COSMIC/ReviewedPublicMongoHttpQualification/$Provider"
$project = Join-Path $root 'ONODE/NextGenSoftware.OASIS.API.ONODE.WebAPI.UnitTests/NextGenSoftware.OASIS.API.ONODE.WebAPI.UnitTests.csproj'
$log = Join-Path $results 'mongo-service.log'

# Retained fixture identities and uncertain outcomes must never become a new run.
if ((Test-Path (Join-Path $state 'fixture.intent.json')) -or (Test-Path $log)) {
    throw 'Original reviewed HTTP fixture or process evidence exists; reconcile it without replay.'
}
New-Item -ItemType Directory -Path $results -Force | Out-Null

& (Join-Path $PSScriptRoot 'Invoke-WithProviderBuildLease.ps1') -Owner 'cosmic-reviewed-public-mongo-http' -Operation {
    $mongo = $null
    $priorNative = $env:COSMIC_REVIEWED_PUBLIC_MONGO_NATIVE
    $priorNetwork = $env:COSMIC_REVIEWED_NETWORK_NATIVE
    try {
        if (Get-NetTCPConnection -State Listen -LocalPort 27029 -ErrorAction SilentlyContinue) {
            throw 'Mongo port is already owned; no process adoption.'
        }
        $source = & (Join-Path $PSScriptRoot 'Get-CosmicMongoDemoSource.ps1') -Root $root
        $mongo = Start-Process $source.Runtime -ArgumentList @('--dbpath', $source.Data,
            '--bind_ip', '127.0.0.1', '--port', '27029', '--replSet', 'oasisRecoveryRs',
            '--wiredTigerCacheSizeGB', '0.25', '--oplogSize', '32', '--logpath', $log) `
            -WindowStyle Hidden -PassThru
        @{ pid = $mongo.Id; data = $source.Data; provider = $Provider } | ConvertTo-Json |
            Set-Content -LiteralPath (Join-Path $results 'mongo-process.json') -Encoding utf8

        $ready = $false
        for ($attempt = 0; $attempt -lt 40; $attempt++) {
            if ($mongo.HasExited) { throw 'Owned Mongo exited before readiness.' }
            $listeners = Get-NetTCPConnection -State Listen -LocalPort 27029 -ErrorAction SilentlyContinue
            if ($listeners) {
                if (@($listeners | Where-Object OwningProcess -ne $mongo.Id).Count) {
                    throw 'Mongo listener ownership differs.'
                }
                $ready = $true
                break
            }
            Start-Sleep -Milliseconds 500
        }
        if (!$ready) { throw 'Owned Mongo readiness deadline exceeded.' }

        $env:COSMIC_REVIEWED_PUBLIC_MONGO_NATIVE = 'owned-local-27029'
        $env:COSMIC_REVIEWED_NETWORK_NATIVE = 'owned-loopback'
        & dotnet test $project --no-build --no-restore `
            --filter 'FullyQualifiedName~CqlReviewedPublicMongoHttpTests.TheGraphStudioNativeHttp' `
            --logger 'trx;LogFileName=web4-mongo-native.trx' --results-directory $results `
            /m:2 /nr:false /p:UseSharedCompilation=false *> (Join-Path $results 'web4-mongo-native.log')
        $testExit = $LASTEXITCODE
        Get-Content (Join-Path $results 'web4-mongo-native.log') -Tail 12
        if ($testExit) { throw 'Native HTTP gate failed; preserve its original fixture and receipts.' }
        [xml]$proof = Get-Content (Join-Path $results 'web4-mongo-native.trx') -Raw
        if ([int]$proof.TestRun.ResultSummary.Counters.passed -ne 1 -or
            [int]$proof.TestRun.ResultSummary.Counters.total -ne 1) {
            throw 'Exactly one native HTTP case must pass; skipped cases are not proof.'
        }
    }
    finally {
        $env:COSMIC_REVIEWED_PUBLIC_MONGO_NATIVE = $priorNative
        $env:COSMIC_REVIEWED_NETWORK_NATIVE = $priorNetwork
        if ($mongo) {
            if (!$mongo.HasExited) {
                $signal = & python -B (Join-Path $PSScriptRoot 'Signal-OwnedConsoleProcess.py') --pid $mongo.Id
                if ($LASTEXITCODE) { throw 'Owned Mongo signal failed; preserve its PID for reconciliation.' }
                $signal | Set-Content -LiteralPath (Join-Path $results 'mongo-signal.json') -Encoding utf8
                if (!$mongo.WaitForExit(30000)) { throw 'Owned Mongo did not complete graceful shutdown.' }
            }
            $events = @(Get-Content -LiteralPath $log -Tail 20 | ForEach-Object { $_ | ConvertFrom-Json })
            $closed = @($events | Where-Object { $_.id -eq 4795901 -and $_.msg -eq 'WiredTiger closed' }).Count -eq 1
            $stopped = @($events | Where-Object { $_.id -eq 8423404 -and $_.msg -eq 'mongod shutdown complete' }).Count -eq 1
            if (!$closed -or !$stopped -or $mongo.ExitCode -notin @(0, 12)) {
                throw 'Owned Mongo native shutdown proof is incomplete.'
            }
            @{ pid = $mongo.Id; stopped = $mongo.HasExited; exitCode = $mongo.ExitCode;
                storageClosed = $closed; shutdownComplete = $stopped } | ConvertTo-Json |
                Set-Content -LiteralPath (Join-Path $results 'mongo-stopped.json') -Encoding utf8
            $mongo.Dispose()
        }
    }
}
