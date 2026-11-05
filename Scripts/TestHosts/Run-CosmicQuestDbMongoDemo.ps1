param([switch]$Serve,[switch]$Initialize)
$ErrorActionPreference = 'Stop'
$root = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
$state = Join-Path $root 'LocalState/COSMIC/RecoveryLaptopQuestDB1010'
if ($Initialize -and $Serve) { throw 'Initialize and accept the native source before interactive use.' }
$phase = 'mongo-' + [Guid]::NewGuid().ToString('N')
if (Test-Path (Join-Path $state 'browser-source/mongo.pending.json')) { throw 'Unresolved Mongo intent; preserve original outcome.' }
if ($Initialize) {
    if (Test-Path (Join-Path $state 'browser-source')) { throw 'Browser source exists; never reinitialize it.' }
} elseif (!(Test-Path (Join-Path $state 'browser-source/bootstrap.ack.json'))) {
    throw 'Accepted retained QuestDB browser source required.'
}
& (Join-Path $PSScriptRoot 'Invoke-WithProviderBuildLease.ps1') -Owner 'cosmic-questdb-browser-native' -Operation {
    $mongo = $null
    $mongoSignalSent = $false
    $priorMongo = $env:OASIS_MONGO_REPLICA_SET_CONNECTION
    try {
        if (Get-NetTCPConnection -State Listen -LocalPort 29000,27029,5134 -ErrorAction SilentlyContinue) {
            throw 'A required native demo port is occupied; no process adoption.'
        }
        $source = & (Join-Path $PSScriptRoot 'Get-CosmicMongoDemoSource.ps1') -Root $root -RecoveryMongo
        $log = Join-Path $state "$phase-mongo.log"
        if (Test-Path $log) { throw 'Existing Mongo output requires reconciliation.' }
        $mongo = Start-Process $source.Runtime -ArgumentList @('--dbpath',$source.Data,'--bind_ip','127.0.0.1',
            '--port','27029','--replSet','oasisRecoveryRs','--wiredTigerCacheSizeGB','0.25','--oplogSize','32',
            '--logpath',$log) -WindowStyle Hidden -PassThru
        @{ pid = $mongo.Id; data = $source.Data; phase = $phase } | ConvertTo-Json |
            Set-Content -LiteralPath (Join-Path $state "$phase-mongo-process.json") -Encoding utf8
        $ready = $false
        for ($i=0; $i -lt 40; $i++) {
            if ($mongo.HasExited) { throw 'Owned Mongo exited before readiness.' }
            $listeners = Get-NetTCPConnection -State Listen -LocalPort 27029 -ErrorAction SilentlyContinue
            if ($listeners) {
                if (@($listeners | Where-Object OwningProcess -ne $mongo.Id).Count) { throw 'Mongo listener ownership differs.' }
                $ready = $true
                break
            }
            Start-Sleep -Milliseconds 500
        }
        if (!$ready) { throw 'Owned Mongo readiness deadline exceeded.' }
        $env:OASIS_MONGO_REPLICA_SET_CONNECTION = 'mongodb://127.0.0.1:27029/?replicaSet=oasisRecoveryRs&serverSelectionTimeoutMS=3000'
        $project = Join-Path $root 'Providers/Storage/TestProjects/NextGenSoftware.OASIS.API.Providers.QuestDBOASIS.IntegrationTests'
        & dotnet build $project /m:2 /nr:false /p:UseSharedCompilation=false /clp:ErrorsOnly -v:q *> (Join-Path $state "$phase-build.log")
        if ($LASTEXITCODE) { throw 'QuestDB consumer build failed; preserve original build output.' }
        $runnerArgs = @('-B', (Join-Path $PSScriptRoot 'run-cosmic-questdb-browser.py'))
        if ($Initialize) { $runnerArgs += '--initialize' }
        if ($Serve) { $runnerArgs += '--serve' }
        & python @runnerArgs
        if ($LASTEXITCODE) { throw 'QuestDB browser/HTTP flow incomplete; preserve original source and evidence.' }
    }
    finally {
        $env:OASIS_MONGO_REPLICA_SET_CONNECTION = $priorMongo
        if ($mongo) {
            if (!$mongo.HasExited) {
                $signal = & python -B (Join-Path $PSScriptRoot 'Signal-OwnedConsoleProcess.py') --pid $mongo.Id
                $signalCode = $LASTEXITCODE
                $signal | Set-Content -LiteralPath (Join-Path $state "$phase-mongo-signal.log") -Encoding utf8
                if ($signalCode -ne 0) { throw 'Owned Mongo console signal failed; preserve PID for reconciliation.' }
                $signalReceipt = ($signal -join "`n") | ConvertFrom-Json
                $mongoSignalSent = $signalReceipt.ctrlCSent -eq $true -and $signalReceipt.targetPid -eq $mongo.Id
                if (!$mongo.WaitForExit(30000)) { throw 'Owned Mongo graceful stop incomplete.' }
                # MongoDB8.2 Windows consoleTerminate calls exitCleanly(ExitCode::kill=12).
                if ($mongo.ExitCode -ne 12) { throw 'Owned Mongo console exit differs from its native contract.' }
            }
            $events = @(Get-Content -LiteralPath $log -Tail 15 | ForEach-Object { $_ | ConvertFrom-Json })
            $storageClosed = @($events | Where-Object { $_.id -eq 4795901 -and $_.msg -eq 'WiredTiger closed' }).Count -eq 1
            $shutdownComplete = @($events | Where-Object { $_.id -eq 8423404 -and $_.msg -eq 'mongod shutdown complete' }).Count -eq 1
            $nativeExit = @($events | Where-Object { $_.id -eq 23138 -and $_.attr.exitCode -eq 12 }).Count -eq 1
            if (!$mongoSignalSent -or !$storageClosed -or !$shutdownComplete -or !$nativeExit) { throw 'Owned Mongo did not prove complete native console shutdown.' }
            @{ pid = $mongo.Id; stopped = $mongo.HasExited; exitCode = $mongo.ExitCode; ctrlCSent = $mongoSignalSent; storageClosed = $storageClosed; shutdownComplete = $shutdownComplete } | ConvertTo-Json |
                Set-Content -LiteralPath (Join-Path $state "$phase-mongo-stopped.json") -Encoding utf8
            $mongo.Dispose()
        }
    }
}
