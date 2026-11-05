param([switch]$Serve, [switch]$HostedSource)
$ErrorActionPreference = 'Stop'
if ($Serve -and $HostedSource) { throw 'Hosted source qualification is noninteractive.' }
$root = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
$state = Join-Path $root 'LocalState/COSMIC/RecoveryLaptopPouchDBCql'
if ($HostedSource) { $state = Join-Path $state 'hosted-preparation' }
$serveId = if ($Serve) { [Guid]::NewGuid().ToString('N') } else { $null }
$phase = 'consumer-' + [Guid]::NewGuid().ToString('N')
if ((Test-Path (Join-Path $state "$phase-run.intent.json")) -or (Test-Path (Join-Path $state 'mongo.pending.json'))) {
    throw 'Existing paired run or Mongo intent requires reconciliation; no replay.'
}
$sourceAck = if ($HostedSource) { 'source/bootstrap.acknowledged.json' } else { 'bootstrap.ack.json' }
if (!(Test-Path -LiteralPath (Join-Path $state $sourceAck) -PathType Leaf)) { throw 'The acknowledged retained PouchDB source is required.' }
$run = {
    if ($HostedSource) {
        $project = Join-Path $root 'Providers/Storage/TestProjects/NextGenSoftware.OASIS.API.Providers.PouchDBOASIS.IntegrationTests/NextGenSoftware.OASIS.API.Providers.PouchDBOASIS.IntegrationTests.csproj'
        dotnet build $project /m:2 /nr:false /p:UseSharedCompilation=false *> (Join-Path $state "$phase-build.log")
        if ($LASTEXITCODE -ne 0) { throw 'Hosted federation test build failed.' }
    }
    $mongo = $null
    $mongoSignalSent = $false
    $priorMongo = $env:OASIS_MONGO_REPLICA_SET_CONNECTION
    try {
        $ports = if ($HostedSource) { @(13800,27029) } else { @(19600,27029,5141) }
        if (Get-NetTCPConnection -State Listen -LocalPort $ports -ErrorAction SilentlyContinue) {
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
        if ($HostedSource) {
            & node (Join-Path $PSScriptRoot 'Qualify-HostedPouchDbCql.cjs') --paired
        } elseif ($Serve) {
            & python -B (Join-Path $PSScriptRoot 'run-cosmic-pouchdb-windows.py') --paired --serve
        } else {
            & python -B (Join-Path $PSScriptRoot 'run-cosmic-pouchdb-windows.py') --paired
        }
        if ($LASTEXITCODE -ne 0) { throw 'PouchDB manager/Mongo/HTTP run failed; preserve original evidence.' }
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
    if ($HostedSource) {
        if (Get-NetTCPConnection -State Listen -LocalPort $ports -ErrorAction SilentlyContinue) {
            throw 'A hosted qualification port is still listening after shutdown.'
        }
        @{ phase = $phase; mongoStopped = $true; portsClosed = $ports } | ConvertTo-Json -Compress
    }
}

& (Join-Path $PSScriptRoot 'Invoke-WithProviderBuildLease.ps1') -Owner 'cosmic-pouchdb-mongo-native' -Operation {
    if (!$HostedSource) {
        & $run
        return
    }
    $coreRelative = 'OASIS Architecture/NextGenSoftware.OASIS.API.Core'
    $core = Join-Path $root $coreRelative
    $pin = git -C $root rev-parse "HEAD:$coreRelative"
    if ($LASTEXITCODE -ne 0 -or $pin -cnotmatch '^[a-f0-9]{40}$') { throw 'Committed Core pin required.' }
    $original = git -C $core rev-parse HEAD
    if ($LASTEXITCODE -ne 0 -or $original -cnotmatch '^[a-f0-9]{40}$' -or (git -C $core status --porcelain)) {
        throw 'Clean Core checkout required.'
    }
    if ($original -eq $pin) {
        & $run
        return
    }
    git -C $core symbolic-ref -q HEAD | Out-Null
    if ($LASTEXITCODE -ne 1) { throw 'Temporary checkout requires detached Core.' }
    git -C $core switch --detach $pin
    if ($LASTEXITCODE -ne 0) { throw 'Unable to select the committed Core pin.' }
    try { & $run }
    finally {
        if (git -C $core status --porcelain) { throw 'Core changed during qualification. Preserve the changes for reconciliation.' }
        git -C $core switch --detach $original
        if ($LASTEXITCODE -ne 0) { throw 'Unable to restore the original Core checkout.' }
    }
}
