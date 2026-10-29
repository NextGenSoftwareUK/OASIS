#Requires -Version 7.5
param([int]$ContinueOwnedNodePid = 0,[switch]$Federated,[switch]$Serve)
if ($Serve -and !$Federated) { throw 'Serve requires explicit federation.' }
$ErrorActionPreference = 'Stop'
$root = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
& (Join-Path $PSScriptRoot 'Invoke-WithProviderBuildLease.ps1') -Owner cosmic-polkadot-cql -Operation {
    $state = Join-Path $root 'LocalState/COSMIC/PolkadotCql'
    $results = Join-Path $root 'TestResults/PolkadotCqlCurrent'
    New-Item -ItemType Directory -Force -Path $state,$results | Out-Null
    $lease = [IO.File]::Open((Join-Path $state 'environment.lock'),'OpenOrCreate','ReadWrite','None')
    $node = $null
    $ownedMongo = $null
    $nodePid = $null
    $names = @('OASIS_MONGO_REPLICA_SET_CONNECTION','OASIS_POLKADOT_MONGO_SERVE','OASIS_POLKADOT_CQL_STATE','OASIS_POLKADOT_CQL_CONTRACT','OASIS_POLKADOT_SDK_BRIDGE','OASIS_POLKADOT_STORAGE_ENCRYPTION_KEY')
    $prior = @{}
    foreach ($name in $names) { $prior[$name] = [Environment]::GetEnvironmentVariable($name) }
    try {
        if (Test-Path (Join-Path $state 'demo-seed.pending.json')) { throw 'Original seed setup requires reconciliation; never replay.' }
        if (Test-Path (Join-Path $state 'mongo.pending.json')) { throw 'Original Mongo fixture requires reconciliation.' }
        if ($Federated -and !(Test-Path (Join-Path $state 'demo-seed.json'))) { throw 'Verify the native seed before federation.' }
        if (!$ContinueOwnedNodePid -and (Get-NetTCPConnection -State Listen -LocalPort 19944,19945 -ErrorAction SilentlyContinue)) { throw 'Polkadot ports occupied; do not adopt another process.' }
        $existing = (& wsl -d Ubuntu -- pgrep -f '[s]ubstrate-contracts-node.*--base-path /var/tmp/oasis-polkadot-evidence/ledger') -join ''
        if ($existing.Trim() -and $existing.Trim() -ne "$ContinueOwnedNodePid") { throw 'Retained Polkadot ledger is in use; preserve its owner.' }
        if ($LASTEXITCODE -notin 0,1) { throw 'Could not inspect retained node ownership.' }
        if ($ContinueOwnedNodePid) {
            $originalPid = [int]([IO.File]::ReadAllText((Join-Path $results 'node.pid')).Trim())
            $command = (& wsl -d Ubuntu -- ps -p $ContinueOwnedNodePid -o args=) -join ''
            if ($originalPid -ne $ContinueOwnedNodePid -or !$command.Contains('cosmic-cql-polkadot') -or !$command.Contains('/var/tmp/oasis-polkadot-evidence/ledger')) { throw 'Original live node ownership differs; no adoption.' }
            $nodePid = $ContinueOwnedNodePid
        }
        $sdk = Join-Path $root 'Providers/Blockchain/NextGenSoftware.OASIS.API.Providers.PolkadotOASIS/SdkBridge'
        if (!(Test-Path (Join-Path $sdk 'node_modules/@polkadot/api/package.json'))) { throw 'Restore the pinned SDK before native startup.' }
        $project = Join-Path $root 'Providers/Blockchain/TestProjects/NextGenSoftware.OASIS.API.Providers.PolkadotOASIS.IntegrationTests/NextGenSoftware.OASIS.API.Providers.PolkadotOASIS.IntegrationTests.csproj'
        & dotnet restore $project --source C:/Users/david/.nuget/packages /m:2 /nr:false /p:NuGetAudit=false -v:q
        if ($LASTEXITCODE) { throw 'Offline paired restore failed.' }
        if ($Federated) {
            & dotnet build (Join-Path $root 'Providers/Storage/NextGenSoftware.OASIS.API.Providers.MongoOASIS/NextGenSoftware.OASIS.API.Providers.MongoDBOASIS.csproj') --no-restore /p:BuildProjectReferences=false /m:2 /nr:false /p:UseSharedCompilation=false -v:q -clp:ErrorsOnly
            if ($LASTEXITCODE) { throw 'Mongo producer build failed.' }
        }
        & dotnet build $project --no-restore /p:BuildProjectReferences=false /m:2 /nr:false /p:UseSharedCompilation=false -v:q -clp:ErrorsOnly
        if ($LASTEXITCODE) { throw 'Native consumer build failed.' }

        # Protect the reusable encryption key with the current Windows account.
        $keyPath = Join-Path $state 'storage-key.dpapi'
        if (!(Test-Path $keyPath)) {
            if (Test-Path (Join-Path $state 'demo-seed.json')) { throw 'Original source key is missing; never replace it.' }
            $key = [Security.Cryptography.RandomNumberGenerator]::GetBytes(32)
            $protected = [Security.Cryptography.ProtectedData]::Protect($key,$null,[Security.Cryptography.DataProtectionScope]::CurrentUser)
            $file = [IO.File]::Open($keyPath,'CreateNew','Write','None')
            try { $file.Write($protected); $file.Flush($true) } finally { $file.Dispose() }
        }
        $key = [Security.Cryptography.ProtectedData]::Unprotect([IO.File]::ReadAllBytes($keyPath),$null,[Security.Cryptography.DataProtectionScope]::CurrentUser)
        $env:OASIS_POLKADOT_STORAGE_ENCRYPTION_KEY = [Convert]::ToBase64String($key)
        [Security.Cryptography.CryptographicOperations]::ZeroMemory($key)
        $env:OASIS_POLKADOT_CQL_STATE = $state
        $env:OASIS_POLKADOT_CQL_CONTRACT = Join-Path $root 'Docs/WIP/HyperDrive-Providers/NativeEvidence/Polkadot/reusable-record-store.json'
        $env:OASIS_POLKADOT_SDK_BRIDGE = Join-Path $sdk 'bridge.mjs'
        $windowsLauncher = (Join-Path $PSScriptRoot 'Start-CosmicPolkadotNode.sh').Replace('\','/')
        $launcher = ((& wsl -d Ubuntu -- wslpath -a $windowsLauncher) -join '').Trim()
        if ($LASTEXITCODE) { throw 'Could not resolve worktree launcher.' }
        $pidPath = Join-Path $results 'node.pid'
        if (!$ContinueOwnedNodePid) {
            if (Test-Path $pidPath) { Remove-Item -LiteralPath $pidPath }
            $node = Start-Process wsl.exe -ArgumentList @('-d','Ubuntu','--','bash',$launcher) -WindowStyle Hidden -PassThru -RedirectStandardOutput (Join-Path $results 'node.stdout.log') -RedirectStandardError (Join-Path $results 'node.stderr.log')
        }
        $ready = $false
        for ($attempt=0;$attempt -lt 60;$attempt++) {
            if ($node) {
                $node.Refresh()
                if ($node.HasExited) { throw 'Owned Polkadot node exited before readiness; inspect its logs.' }
            }
            try {
                $reply = Invoke-RestMethod -Uri 'http://127.0.0.1:19944' -Method Post -ContentType application/json -Body '{"jsonrpc":"2.0","id":1,"method":"chain_getBlockHash","params":[0]}' -TimeoutSec 2
                if ($reply.result -ne '0xe8cf5d7d636a08721efaa17acc52ed3d895fe2468dccb2520cc8e249ddb6b208') { throw 'Native genesis mismatch.' }
                $ready = $true; break
            } catch [System.Net.Http.HttpRequestException] { Start-Sleep -Seconds 1 }
              catch [System.OperationCanceledException] { Start-Sleep -Seconds 1 }
        }
        if (!$ready) { throw 'Owned node readiness timed out; preserve state.' }
        $nodePid = [int]([IO.File]::ReadAllText($pidPath).Trim())
        if (!$Federated) {
        & dotnet test $project --no-build --no-restore /m:2 /nr:false /p:UseSharedCompilation=false --filter 'FullyQualifiedName~PolkadotCqlNativeTests' --logger 'trx;LogFileName=polkadot-cql-native.trx' --results-directory $results -v:q
        if ($LASTEXITCODE) { throw 'Native CQL failed; preserve original setup and journals.' }
        [xml]$report = Get-Content (Join-Path $results 'polkadot-cql-native.trx')
        if ($report.TestRun.ResultSummary.Counters.total -ne '1' -or $report.TestRun.ResultSummary.Counters.passed -ne '1') { throw 'Exactly one executed native acceptance case required.' }
        }
        else {
            $mongoRuntime = 'C:/Program Files/MongoDB/Server/8.3/bin/mongod.exe'
            $mongoData = Join-Path $root 'LocalState/COSMIC/mongo-data'
            if (!(Test-Path (Join-Path $mongoData 'WiredTiger'))) { throw 'Retained Mongo environment missing; no reset.' }
            if (Get-NetTCPConnection -State Listen -LocalPort 27029,5102 -ErrorAction SilentlyContinue) { throw 'Paired demo port occupied.' }
            if (((& $mongoRuntime --version) -join ' ') -notmatch 'v8\.3\.11\b') { throw 'Pinned Mongo runtime differs.' }
            $ownedMongo = Start-Process $mongoRuntime -ArgumentList @('--dbpath',$mongoData,'--bind_ip','127.0.0.1','--port','27029','--replSet','oasisRecoveryRs','--wiredTigerCacheSizeGB','0.25','--oplogSize','32','--logpath',(Join-Path $results 'mongo-service.log'),'--logappend') -WindowStyle Hidden -PassThru
            $mongoReady = $false
            for ($i=0;$i -lt 40;$i++) {
                if ($ownedMongo.HasExited) { throw 'Owned Mongo exited during startup.' }
                $listeners = Get-NetTCPConnection -State Listen -LocalPort 27029 -ErrorAction SilentlyContinue
                if ($listeners) {
                    if (@($listeners | Where-Object OwningProcess -ne $ownedMongo.Id).Count) { throw 'Mongo listener owner differs.' }
                    $mongoReady = $true; break
                }
                Start-Sleep -Milliseconds 500
            }
            if (!$mongoReady) { throw 'Owned Mongo startup deadline exceeded.' }
            $env:OASIS_MONGO_REPLICA_SET_CONNECTION = 'mongodb://127.0.0.1:27029/?replicaSet=oasisRecoveryRs&serverSelectionTimeoutMS=3000'
            $env:OASIS_POLKADOT_MONGO_SERVE = if ($Serve) { '1' } else { '0' }
            & dotnet test $project --no-build --no-restore /m:2 /nr:false /p:UseSharedCompilation=false --filter 'FullyQualifiedName~PolkadotMongoCqlNativeTests' --logger 'trx;LogFileName=polkadot-mongo-federation.trx' --results-directory $results -v:q *> (Join-Path $results 'polkadot-mongo-federation.log')
            if ($LASTEXITCODE) { throw 'Polkadot Mongo demo failed; preserve pending fixture.' }
            [xml]$paired = Get-Content (Join-Path $results 'polkadot-mongo-federation.trx')
            if ($paired.TestRun.ResultSummary.Counters.total -ne '1' -or $paired.TestRun.ResultSummary.Counters.passed -ne '1') { throw 'Exactly one paired case required.' }
            if (Test-Path (Join-Path $state 'mongo.pending.json')) { throw 'Mongo fixture cleanup incomplete.' }
            Get-Content (Join-Path $results 'polkadot-mongo-federation.log') -Tail 5
        }
    }
    finally {
      try {
        if (($node -or $nodePid) -and !(Test-Path (Join-Path $state 'demo-seed.pending.json'))) {
            if (!$nodePid -and (Test-Path (Join-Path $results 'node.pid'))) { $nodePid = [int]([IO.File]::ReadAllText((Join-Path $results 'node.pid')).Trim()) }
            if ($nodePid) {
                $command = (& wsl -d Ubuntu -- ps -p $nodePid -o args=) -join ''
                if ($command.Contains('cosmic-cql-polkadot') -and $command.Contains('/var/tmp/oasis-polkadot-evidence/ledger')) {
                    & wsl -d Ubuntu -- kill -INT $nodePid
                    if ($LASTEXITCODE) { throw 'Could not stop owned node; preserve its process.' }
                    if ($node -and !$node.WaitForExit(30000)) { throw 'Owned node is still stopping; do not restart.' }
                    for ($stop=0;$stop -lt 30;$stop++) {
                        & wsl -d Ubuntu -- kill -0 $nodePid 2>$null
                        if ($LASTEXITCODE) { break }
                        Start-Sleep -Seconds 1
                    }
                    if (!$LASTEXITCODE) { throw 'Owned node is still stopping; do not restart.' }
                }
                elseif ($command.Trim()) { throw 'Owned node identity changed; preserve process for review.' }
            }
        }
      }
      finally {
        if ($ownedMongo) {
            if (!$ownedMongo.HasExited) { $ownedMongo.Kill(); if (!$ownedMongo.WaitForExit(30000)) { throw 'Owned Mongo teardown incomplete.' } }
            $ownedMongo.Dispose()
        }
        foreach ($name in $names) { [Environment]::SetEnvironmentVariable($name,$prior[$name]) }
        $lease.Dispose()
      }
    }
    # The expected kill -0 absence exit must not become the runner result.
    $global:LASTEXITCODE = 0
}
