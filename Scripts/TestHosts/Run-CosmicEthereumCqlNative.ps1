#Requires -Version 7.0
param([switch]$Federated,[switch]$Serve,[ValidateSet('Ethereum','Polygon','Arbitrum')][string]$Provider='Ethereum')
if ($Serve -and !$Federated) { throw 'Serve requires explicit federation.' }
$ErrorActionPreference = 'Stop'
$root = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
& (Join-Path $PSScriptRoot 'Invoke-WithProviderBuildLease.ps1') -Owner cosmic-ethereum-cql -Operation {
    $state = Join-Path $root 'LocalState/COSMIC/EvmCql'
    if ($Provider -ne 'Ethereum') { $state = Join-Path $state $Provider }
    $results = Join-Path $root 'TestResults/EthereumCqlCurrent'
    New-Item -ItemType Directory -Force -Path $state,$results | Out-Null
    $lease = [IO.File]::Open((Join-Path $state 'environment.lock'), 'OpenOrCreate', 'ReadWrite', 'None')
    $nodeProcess = $null
    $ownedMongo = $null
    $oldMongo = $env:OASIS_MONGO_REPLICA_SET_CONNECTION
    $oldServe = $env:OASIS_EVM_MONGO_SERVE
    $oldProvider = $env:OASIS_EVM_CQL_PROVIDER
    $oldWeb3Key = $env:OASIS_WEB3CORE_STORAGE_ENCRYPTION_KEY
    $oldKey = $env:OASIS_ETHEREUM_STORAGE_ENCRYPTION_KEY
    $oldState = $env:OASIS_EVM_CQL_STATE
    try {
        if (Test-Path -LiteralPath (Join-Path $state 'demo-seed.pending.json')) {
            throw 'Unresolved original setup exists. Reconcile retained chain and journal; no replay/reset attempted.'
        }
        if (Get-NetTCPConnection -State Listen -LocalPort 18547 -ErrorAction SilentlyContinue) {
            throw 'EVM demo port is occupied; no unrelated process adopted or stopped.'
        }
        $node = (Get-Command node.exe -ErrorAction Stop).Source
        $runtimeManifest = Join-Path $root 'LocalState/COSMIC/EvmRuntime/node_modules/ganache/package.json'
        if (!(Test-Path -LiteralPath $runtimeManifest) -or (Get-Content $runtimeManifest -Raw | ConvertFrom-Json).version -ne '7.9.2') {
            throw 'Reusable pinned Ganache7.9.2 runtime required. No automatic download.'
        }
        $keyPath = Join-Path $root 'LocalState/COSMIC/EvmCql/storage-key.dpapi'
        if (!(Test-Path -LiteralPath $keyPath)) {
            if (Test-Path -LiteralPath (Join-Path $state 'demo-seed.json')) { throw 'Original seed encryption key is absent; never replace it.' }
            $key = [Security.Cryptography.RandomNumberGenerator]::GetBytes(32)
            try { [IO.File]::WriteAllBytes($keyPath,[Security.Cryptography.ProtectedData]::Protect($key,$null,'CurrentUser')) }
            finally { [Security.Cryptography.CryptographicOperations]::ZeroMemory($key) }
        }
        $key = [Security.Cryptography.ProtectedData]::Unprotect([IO.File]::ReadAllBytes($keyPath),$null,'CurrentUser')
        try {
            if ($key.Length -ne 32) { throw 'Original encryption key has invalid length.' }
            $env:OASIS_ETHEREUM_STORAGE_ENCRYPTION_KEY = [Convert]::ToBase64String($key)
            $env:OASIS_WEB3CORE_STORAGE_ENCRYPTION_KEY = $env:OASIS_ETHEREUM_STORAGE_ENCRYPTION_KEY
        }
        finally { [Security.Cryptography.CryptographicOperations]::ZeroMemory($key) }
        $env:OASIS_EVM_CQL_STATE = $state
        $env:OASIS_EVM_CQL_PROVIDER = $Provider
        $ethereumProject = Join-Path $root 'Providers/Blockchain/NextGenSoftware.OASIS.API.Providers.EthereumOASIS/NextGenSoftware.OASIS.API.Providers.EthereumOASIS.csproj'
        $tests = Join-Path $root 'Providers/Blockchain/TestProjects/NextGenSoftware.OASIS.API.Providers.EthereumOASIS.IntegrationTests/NextGenSoftware.OASIS.API.Providers.EthereumOASIS.IntegrationTests.csproj'
        if ($Federated) {
            $assets = Join-Path (Split-Path $tests) 'obj/project.assets.json'
            if (!(Test-Path $assets) -or (Get-Content $assets -Raw) -notmatch 'NextGenSoftware.OASIS.API.Providers.MongoDBOASIS') {
                & dotnet restore $tests --source C:/Users/david/.nuget/packages /m:2 /nr:false /p:NuGetAudit=false -v:q
                if ($LASTEXITCODE) { throw 'Offline federation restore failed.' }
            }
            & dotnet build (Join-Path $root 'Providers/Storage/NextGenSoftware.OASIS.API.Providers.MongoOASIS/NextGenSoftware.OASIS.API.Providers.MongoDBOASIS.csproj') --no-restore /p:BuildProjectReferences=false /m:2 /nr:false /p:UseSharedCompilation=false -v:q -clp:ErrorsOnly
            if ($LASTEXITCODE) { throw 'Mongo producer build failed.' }
        }
        if ($Provider -in @('Polygon','Arbitrum')) {
            & $node (Join-Path $PSScriptRoot 'compile-cosmic-web3core.cjs')
            if ($LASTEXITCODE) { throw 'Pinned contract compilation or ABI verification failed.' }
            foreach ($relative in @('Providers/Blockchain/NextGenSoftware.OASIS.API.Providers.Web3CoreOASIS/NextGenSoftware.OASIS.API.Providers.Web3CoreOASIS.csproj', 'Providers/Blockchain/NextGenSoftware.OASIS.API.Providers.PolygonOASIS/NextGenSoftware.OASIS.API.Providers.PolygonOASIS.csproj', 'Providers/Blockchain/NextGenSoftware.OASIS.API.Providers.ArbitrumOASIS/NextGenSoftware.OASIS.API.Providers.ArbitrumOASIS.csproj')) {
                & dotnet build (Join-Path $root $relative) --no-restore /p:BuildProjectReferences=false /m:2 /nr:false /p:UseSharedCompilation=false -v:q -clp:ErrorsOnly
                if ($LASTEXITCODE) { throw 'Web3Core family producer build failed.' }
            }
        }
        foreach ($project in @($ethereumProject,$tests)) {
            & dotnet build $project --no-restore /p:BuildProjectReferences=false /m:2 /nr:false /p:UseSharedCompilation=false -v:q -clp:ErrorsOnly
            if ($LASTEXITCODE) { throw "Bounded build failed: $project" }
        }
        $start = [Diagnostics.ProcessStartInfo]::new($node)
        $start.ArgumentList.Add((Join-Path $PSScriptRoot 'cosmic-evm-node.cjs'))
        $start.WorkingDirectory = $root
        $start.UseShellExecute = $false
        $start.CreateNoWindow = $true
        $start.RedirectStandardInput = $true
        $start.RedirectStandardOutput = $true
        $start.RedirectStandardError = $true
        $nodeProcess = [Diagnostics.Process]::Start($start)
        $stdout = $nodeProcess.StandardOutput.ReadToEndAsync()
        $stderr = $nodeProcess.StandardError.ReadToEndAsync()
        $deadline = [DateTime]::UtcNow.AddSeconds(30)
        $ready = $false
        do {
            if ($nodeProcess.HasExited) { throw 'Owned persistent EVM exited at startup; inspect retained logs.' }
            try {
                $reply = Invoke-RestMethod -Uri 'http://127.0.0.1:18547' -Method Post -ContentType 'application/json' -Body '{"jsonrpc":"2.0","id":1,"method":"eth_chainId","params":[]}' -TimeoutSec 2
                if ($reply.result -ne '0x7a6a') { throw 'Unexpected native chain identity; do not adopt it.' }
                $ready = $true
            }
            catch [System.Net.Http.HttpRequestException] { if ([DateTime]::UtcNow -ge $deadline) { throw } }
            catch [System.Threading.Tasks.TaskCanceledException] { if ([DateTime]::UtcNow -ge $deadline) { throw } }
            if (!$ready) { Start-Sleep -Milliseconds 200 }
        } while (!$ready -and [DateTime]::UtcNow -lt $deadline)
        if (!$ready) { throw 'Owned EVM readiness deadline exceeded.' }
        $started = [DateTime]::UtcNow
        $nativeReportName = $Provider.ToLowerInvariant() + "-cql-native.trx"
        $nativeFilter = "FullyQualifiedName~${Provider}CqlNativeTests"
        & dotnet test $tests --no-build --no-restore /m:2 /nr:false --filter $nativeFilter --logger ("trx;LogFileName=" + $nativeReportName) --results-directory $results -v:q
        if ($LASTEXITCODE) { throw 'Native CQL acceptance failed. Preserve chain, original key and setup journal.' }
        $report = Get-Item -LiteralPath (Join-Path $results $nativeReportName)
        if ($report.LastWriteTimeUtc -lt $started) { throw 'Native result is stale.' }
        [xml]$trx = Get-Content -LiteralPath $report.FullName
        $counts = $trx.TestRun.ResultSummary.Counters
        if ([int]$counts.total -ne 1 -or [int]$counts.passed -ne 1 -or [int]$counts.notExecuted -ne 0) { throw 'Expected exactly one passing native scenario.' }
        if ($Federated) {
            $mongoRuntime='C:/Program Files/MongoDB/Server/8.3/bin/mongod.exe'
            $mongoData=Join-Path $root 'LocalState/COSMIC/mongo-data'
            if(!(Test-Path (Join-Path $mongoData 'WiredTiger'))){throw 'Existing Mongo environment is missing; no reset.'}
            if(Get-NetTCPConnection -State Listen -LocalPort 27029 -ErrorAction SilentlyContinue){throw 'Mongo demo port occupied.'}
            if(((& $mongoRuntime --version)-join ' ') -notmatch 'v8\.3\.11\b'){throw 'Pinned Mongo runtime differs.'}
            $ownedMongo=Start-Process $mongoRuntime -ArgumentList @('--dbpath',$mongoData,'--bind_ip','127.0.0.1','--port','27029','--replSet','oasisRecoveryRs','--wiredTigerCacheSizeGB','0.25','--oplogSize','32','--logpath',(Join-Path $results 'mongo-service.log'),'--logappend') -WindowStyle Hidden -PassThru
            $ready=$false
            for($i=0;$i -lt 40;$i++){
                if($ownedMongo.HasExited){throw 'Owned Mongo exited during startup.'}
                $listeners=Get-NetTCPConnection -State Listen -LocalPort 27029 -ErrorAction SilentlyContinue
                if($listeners){if(@($listeners|Where-Object OwningProcess -ne $ownedMongo.Id).Count){throw 'Mongo listener owner differs.'};$ready=$true;break}
                Start-Sleep -Milliseconds 500
            }
            if(!$ready){throw 'Owned Mongo startup deadline exceeded.'}
            $env:OASIS_MONGO_REPLICA_SET_CONNECTION='mongodb://127.0.0.1:27029/?replicaSet=oasisRecoveryRs&serverSelectionTimeoutMS=3000'
            $env:OASIS_EVM_MONGO_SERVE=if($Serve){'1'}else{'0'}
            $pairedName = $Provider.ToLowerInvariant() + "-mongo-federation"
            $pairedStarted=[DateTime]::UtcNow
            dotnet test $tests --no-build --no-restore --filter 'FullyQualifiedName~NativeEvmMongoFederationRunsSdkAndHttpWithProviderProvenance' --logger ("trx;LogFileName=" + $pairedName + ".trx") --results-directory $results *> (Join-Path $results ($pairedName + ".log"))
            if($LASTEXITCODE){throw 'EVM Mongo demo failed; preserve pending fixture.'}
            $pairedReport=Get-Item -LiteralPath (Join-Path $results ($pairedName + ".trx"))
            if($pairedReport.LastWriteTimeUtc -lt $pairedStarted){throw 'Paired report is stale.'}
            [xml]$paired=Get-Content $pairedReport.FullName
            if($paired.TestRun.ResultSummary.Counters.total -ne '1' -or $paired.TestRun.ResultSummary.Counters.passed -ne '1'){throw 'Exactly one fresh federation case required.'}
            if(Test-Path (Join-Path $state 'mongo.pending.json')){throw 'Mongo fixture cleanup incomplete.'}
            Get-Content (Join-Path $results ($pairedName + ".log")) -Tail 5
        }
    }
    finally {
      try {
        if ($nodeProcess) {
            if (!$nodeProcess.HasExited) {
                $nodeProcess.StandardInput.WriteLine('stop')
                $nodeProcess.StandardInput.Flush()
                if (!$nodeProcess.WaitForExit(15000)) { throw "Owned EVM PID $($nodeProcess.Id) did not flush/exit. Preserve it; no forced termination." }
            }
            [IO.File]::WriteAllText((Join-Path $results 'evm-current.log'),$stdout.GetAwaiter().GetResult())
            [IO.File]::WriteAllText((Join-Path $results 'evm-current.err'),$stderr.GetAwaiter().GetResult())
            $nodeProcess.Dispose()
        }
      }
      finally {
       try {
        if ($ownedMongo) {
            if (!$ownedMongo.HasExited) { $ownedMongo.Kill(); if (!$ownedMongo.WaitForExit(30000)) { throw 'Owned Mongo teardown incomplete.' } }
            $ownedMongo.Dispose()
        }
       }
       finally {
        $env:OASIS_MONGO_REPLICA_SET_CONNECTION = $oldMongo
        $env:OASIS_EVM_MONGO_SERVE = $oldServe
        $env:OASIS_EVM_CQL_PROVIDER = $oldProvider
        $env:OASIS_WEB3CORE_STORAGE_ENCRYPTION_KEY = $oldWeb3Key
        $env:OASIS_ETHEREUM_STORAGE_ENCRYPTION_KEY = $oldKey
        $env:OASIS_EVM_CQL_STATE = $oldState
        $lease.Dispose()
       }
      }
    }
}
