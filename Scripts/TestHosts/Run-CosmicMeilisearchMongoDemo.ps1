param([switch]$DiagnoseStartup,[switch]$ResumePaired,[switch]$Serve)
$ErrorActionPreference = 'Stop'
$root = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
$state = Join-Path $root 'LocalState/COSMIC/RecoveryLaptopMeili1561'
if ($DiagnoseStartup -and $ResumePaired) { throw 'Diagnostics and paired resumption are separate operations.' }
if ($Serve -and ($DiagnoseStartup -or $ResumePaired)) { throw 'Interactive demo is a separate explicit operation.' }
$serveId = if ($Serve) { [Guid]::NewGuid().ToString('N') } else { $null }
$phase = if ($Serve) { "interactive-$serveId" } elseif ($DiagnoseStartup) { 'concurrent-startup-diagnostic' } elseif ($ResumePaired) { 'paired-resumed' } else { 'paired' }
if ((Test-Path (Join-Path $state "$phase-run.intent.json")) -or (Test-Path (Join-Path $state 'mongo.pending.json'))) {
    throw 'Existing paired run or Mongo intent requires reconciliation; no replay.'
}
& (Join-Path $PSScriptRoot 'Invoke-WithProviderBuildLease.ps1') -Owner 'cosmic-meili-mongo-native' -Operation {
    $mongo = $null
    $priorMongo = $env:OASIS_MONGO_REPLICA_SET_CONNECTION
    try {
        if (Get-NetTCPConnection -State Listen -LocalPort 17700,27029,5116 -ErrorAction SilentlyContinue) {
            throw 'A required native demo port is occupied; no process adoption.'
        }
        $source = & (Join-Path $PSScriptRoot 'Get-CosmicMongoDemoSource.ps1') -Root $root -RecoveryMongo
        $log = Join-Path $state "$phase-mongo.log"
        if (Test-Path $log) { throw 'Existing Mongo diagnostic output requires reconciliation.' }
        $mongo = Start-Process $source.Runtime -ArgumentList @('--dbpath',$source.Data,'--bind_ip','127.0.0.1',
            '--port','27029','--replSet','oasisRecoveryRs','--wiredTigerCacheSizeGB','0.25','--oplogSize','32',
            '--logpath',$log) -WindowStyle Hidden -PassThru
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
            & python -B (Join-Path $PSScriptRoot 'run-cosmic-meili-windows.py') --paired --serve-run-id $serveId
        } elseif ($DiagnoseStartup) {
            & python -B (Join-Path $PSScriptRoot 'run-cosmic-meili-windows.py') --diagnose-startup with-mongo
        } elseif ($ResumePaired) {
            & python -B (Join-Path $PSScriptRoot 'run-cosmic-meili-windows.py') --paired --resume-paired
        } else {
            & python -B (Join-Path $PSScriptRoot 'run-cosmic-meili-windows.py') --paired
        }
        if ($LASTEXITCODE -ne 0) { throw 'Meilisearch manager/Mongo/HTTP run failed; preserve original evidence.' }
    }
    finally {
        $env:OASIS_MONGO_REPLICA_SET_CONNECTION = $priorMongo
        if ($mongo) {
            if (!$mongo.HasExited) {
                $mongo.Kill()
                if (!$mongo.WaitForExit(30000)) { throw 'Owned Mongo stop incomplete.' }
            }
            $mongo.Dispose()
        }
    }
}
