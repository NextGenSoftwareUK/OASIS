#Requires -Version 7.0
$ErrorActionPreference = 'Stop'
$root = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
& (Join-Path $PSScriptRoot 'Invoke-WithProviderBuildLease.ps1') -Owner cosmic-ethereum-cql -Operation {
    $state = Join-Path $root 'LocalState/COSMIC/EvmCql'
    $results = Join-Path $root 'TestResults/EthereumCqlCurrent'
    New-Item -ItemType Directory -Force -Path $state,$results | Out-Null
    $lease = [IO.File]::Open((Join-Path $state 'environment.lock'), 'OpenOrCreate', 'ReadWrite', 'None')
    $nodeProcess = $null
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
        $runtimeManifest = Join-Path $state '../EvmRuntime/node_modules/ganache/package.json'
        if (!(Test-Path -LiteralPath $runtimeManifest) -or (Get-Content $runtimeManifest -Raw | ConvertFrom-Json).version -ne '7.9.2') {
            throw 'Reusable pinned Ganache7.9.2 runtime required. No automatic download.'
        }
        $keyPath = Join-Path $state 'storage-key.dpapi'
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
        }
        finally { [Security.Cryptography.CryptographicOperations]::ZeroMemory($key) }
        $env:OASIS_EVM_CQL_STATE = $state
        $provider = Join-Path $root 'Providers/Blockchain/NextGenSoftware.OASIS.API.Providers.EthereumOASIS/NextGenSoftware.OASIS.API.Providers.EthereumOASIS.csproj'
        $tests = Join-Path $root 'Providers/Blockchain/TestProjects/NextGenSoftware.OASIS.API.Providers.EthereumOASIS.IntegrationTests/NextGenSoftware.OASIS.API.Providers.EthereumOASIS.IntegrationTests.csproj'
        foreach ($project in @($provider,$tests)) {
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
        & dotnet test $tests --no-build --no-restore /m:2 /nr:false --filter FullyQualifiedName~EthereumCqlNativeTests --logger 'trx;LogFileName=ethereum-cql-native.trx' --results-directory $results -v:q
        if ($LASTEXITCODE) { throw 'Native CQL acceptance failed. Preserve chain, original key and setup journal.' }
        $report = Get-Item -LiteralPath (Join-Path $results 'ethereum-cql-native.trx')
        if ($report.LastWriteTimeUtc -lt $started) { throw 'Native result is stale.' }
        [xml]$trx = Get-Content -LiteralPath $report.FullName
        $counts = $trx.TestRun.ResultSummary.Counters
        if ([int]$counts.total -ne 1 -or [int]$counts.passed -ne 1 -or [int]$counts.notExecuted -ne 0) { throw 'Expected exactly one passing native scenario.' }
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
        $env:OASIS_ETHEREUM_STORAGE_ENCRYPTION_KEY = $oldKey
        $env:OASIS_EVM_CQL_STATE = $oldState
        $lease.Dispose()
      }
    }
}
