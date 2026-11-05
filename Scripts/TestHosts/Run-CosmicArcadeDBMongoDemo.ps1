param([switch]$Serve)
$ErrorActionPreference = 'Stop'
$root = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
$state = Join-Path $root 'LocalState/COSMIC/RecoveryLaptopArcade26111'
$serveId = if ($Serve) { [Guid]::NewGuid().ToString('N') } else { $null }
$phase = if ($Serve) { "interactive-$serveId" } else { 'paired' }
if ((Test-Path (Join-Path $state "$phase-run.intent.json")) -or (Test-Path (Join-Path $state 'mongo.pending.json'))) {
    throw 'Existing paired run or Mongo intent requires reconciliation; no replay.'
}
if (!(Test-Path (Join-Path $state 'bootstrap.ack.json'))) { throw 'The acknowledged retained ArcadeDB source is required.' }
& (Join-Path $PSScriptRoot 'Invoke-WithProviderBuildLease.ps1') -Owner 'cosmic-arcade-mongo-native' -Operation {
    $mongo = $null
    $priorMongo = $env:OASIS_MONGO_REPLICA_SET_CONNECTION
    try {
        if (Get-NetTCPConnection -State Listen -LocalPort 24800,27029,5137 -ErrorAction SilentlyContinue) {
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
        if ($Serve) {
            & python -B (Join-Path $PSScriptRoot 'run-cosmic-arcade-windows.py') --paired --serve-run-id $serveId
        } else {
            & python -B (Join-Path $PSScriptRoot 'run-cosmic-arcade-windows.py') --paired
        }
        if ($LASTEXITCODE -ne 0) { throw 'ArcadeDB manager/Mongo/HTTP run failed; preserve original evidence.' }
    }
    finally {
        $env:OASIS_MONGO_REPLICA_SET_CONNECTION = $priorMongo
        if ($mongo) {
            if (!$mongo.HasExited) {
                $mongo.Kill()
                if (!$mongo.WaitForExit(30000)) { throw 'Owned Mongo stop incomplete.' }
            }
            @{ pid = $mongo.Id; stopped = $mongo.HasExited } | ConvertTo-Json |
                Set-Content -LiteralPath (Join-Path $state "$phase-mongo-stopped.json") -Encoding utf8
            $mongo.Dispose()
        }
    }
}
