#Requires -Version 7.5
param([int]$ContinueOwnedNodePid=0)
$ErrorActionPreference = 'Stop'
$root = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
& (Join-Path $PSScriptRoot 'Invoke-WithProviderBuildLease.ps1') -Owner cosmic-cosmos-cql -Operation {
    $state = Join-Path $root 'LocalState/COSMIC/CosmosCql'
    $results = Join-Path $root 'TestResults/CosmosCqlCurrent'
    New-Item -ItemType Directory -Force -Path $state,$results | Out-Null
    $lease = [IO.File]::Open((Join-Path $state 'environment.lock'),'OpenOrCreate','ReadWrite','None')
    $names = @('OASIS_COSMOS_CQL_STATE','OASIS_COSMOS_CQL_SIGNING_KEY','OASIS_COSMOS_STORAGE_ENCRYPTION_KEY','OASIS_COSMOS_NODE','OASIS_COSMOS_SDK_BRIDGE')
    $prior = @{}
    foreach ($name in $names) { $prior[$name] = [Environment]::GetEnvironmentVariable($name) }
    $containerStarted = $false
    $nodeStarted = $false
    $safeToStop = $true
    $ownedPid = $null
    try {
        if (Test-Path -LiteralPath (Join-Path $state 'demo-seed.pending.json')) { throw 'Original setup requires reconciliation; no replay/reseed.' }
        if (!$ContinueOwnedNodePid -and (Get-NetTCPConnection -State Listen -LocalPort 26657 -ErrorAction SilentlyContinue)) { throw 'Cosmos RPC is in use; do not adopt its process.' }
        $identity = (& wsl -d Ubuntu -u root -- podman inspect oasis-wasmd --format '{{.Id}} {{.State.Status}} {{.ImageName}}') -join ''
        if ($LASTEXITCODE -or $identity -notmatch '^58a873de020d2f79ab11e7087361db25063c277a5b5a5c2cabba72263b35f8f3 (exited|running) docker.io/cosmwasm/wasmd@sha256:3741178a4d747fd5cf281550ab4ac30d771163af1c543f9da503c411a1691773$') {
            throw 'Retained Cosmos runtime identity differs; no image pull/replacement.'
        }
        $project = Join-Path $root 'Providers/Blockchain/TestProjects/NextGenSoftware.OASIS.API.Providers.CosmosBlockChainOASIS.IntegrationTests/NextGenSoftware.OASIS.API.Providers.CosmosBlockChainOASIS.IntegrationTests.csproj'
        foreach ($build in @((Join-Path $root 'Providers/Shared/NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage/NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage.csproj'),(Join-Path $root 'Providers/Blockchain/NextGenSoftware.OASIS.API.Providers.CosmosBlockChainOASIS/NextGenSoftware.OASIS.API.Providers.CosmosBlockChainOASIS.csproj'),$project)) {
            & dotnet build $build --no-restore /p:BuildProjectReferences=false /m:2 /nr:false /p:UseSharedCompilation=false -v:q -clp:ErrorsOnly
            if ($LASTEXITCODE) { throw 'Bounded Cosmos CQL build failed.' }
        }
        $outputBridge = 'Providers/Blockchain/TestProjects/NextGenSoftware.OASIS.API.Providers.CosmosBlockChainOASIS.IntegrationTests/bin/Debug/net10.0/CosmosSdkBridge'
        & (Join-Path $PSScriptRoot 'Use-ReusableHederaOutputDependencies.ps1') -Provider Cosmos -OutputBridge $outputBridge | Out-Null
        if ($identity.Contains(' exited ')) {
            & wsl -d Ubuntu -u root -- podman start oasis-wasmd | Out-Null
            if ($LASTEXITCODE) { throw 'Retained container start failed.' }
            $containerStarted = $true
        }
        $existingPid = ((& wsl -d Ubuntu -u root -- podman exec oasis-wasmd pgrep -x wasmd) -join '').Trim()
        if ($ContinueOwnedNodePid) {
            if ($LASTEXITCODE -ne 0 -or $existingPid -ne $ContinueOwnedNodePid.ToString()) { throw 'Original live node identity differs; no adoption/restart.' }
            $containerStarted = $true # This task started the preserved original container.
        }
        else {
            if ($LASTEXITCODE -ne 1) { throw 'Native node is already active or cannot be inspected.' }
            & wsl -d Ubuntu -u root -- podman exec -d oasis-wasmd wasmd start --home /root/.wasmd --rpc.laddr tcp://0.0.0.0:26657 | Out-Null
            if ($LASTEXITCODE) { throw 'Retained native node start failed.' }
        }
        $nodeStarted = $true
        $deadline = [DateTime]::UtcNow.AddSeconds(30)
        $initial = $null
        $ready = $false
        do {
            try {
                $status = Invoke-RestMethod 'http://127.0.0.1:26657/status' -TimeoutSec 2
                if ($status.result.node_info.network -ne 'oasis-cosmos-local') { throw 'Unexpected native chain identity.' }
                $height = [long]$status.result.sync_info.latest_block_height
                if ($null -eq $initial) { $initial = $height }
                $ready = !$status.result.sync_info.catching_up -and $height -gt $initial
            }
            catch [System.Net.Http.HttpRequestException] { }
            catch [System.Threading.Tasks.TaskCanceledException] { }
            if (!$ready) { Start-Sleep -Milliseconds 250 }
        } while (!$ready -and [DateTime]::UtcNow -lt $deadline)
        if (!$ready) { throw 'Native chain fresh-block readiness deadline exceeded.' }
        $ownedPid = ((& wsl -d Ubuntu -u root -- podman exec oasis-wasmd pgrep -x wasmd) -join '').Trim()
        if ($LASTEXITCODE -or $ownedPid -notmatch '^[0-9]+$') { throw 'One owned native process required.' }
        $codeRaw = & wsl -d Ubuntu -u root -- podman exec oasis-wasmd wasmd query wasm code-info 1 --node http://127.0.0.1:26657 --output json
        if ($LASTEXITCODE) { throw 'Retained code metadata unavailable.' }
        $code = ($codeRaw -join "`n") | ConvertFrom-Json
        if ($code.checksum -ne '8FB3C70F96A3FA83E3563B7CBDCE097C5FA4EA77DA7610EC725275AB39424A87') { throw 'Retained code1 differs; no upload or redeployment.' }
        if (!(Test-Path -LiteralPath (Join-Path $state 'demo-seed.json'))) {
            $listRaw = & wsl -d Ubuntu -u root -- podman exec oasis-wasmd wasmd query wasm list-contract-by-code 1 --node http://127.0.0.1:26657 --output json
            if ($LASTEXITCODE) { throw 'Native contract inventory unavailable.' }
            foreach ($address in (($listRaw -join "`n") | ConvertFrom-Json).contracts) {
                $infoRaw = & wsl -d Ubuntu -u root -- podman exec oasis-wasmd wasmd query wasm contract $address --node http://127.0.0.1:26657 --output json
                if ($LASTEXITCODE) { throw 'Native contract metadata unavailable.' }
                if ((($infoRaw -join "`n") | ConvertFrom-Json).contract_info.label -eq 'cosmic-cql-demo') { throw 'Original CQL contract exists without its manifest; never instantiate another.' }
            }
        }
        $keyPath = Join-Path $state 'storage-key.dpapi'
        if (!(Test-Path -LiteralPath $keyPath)) {
            if (Test-Path -LiteralPath (Join-Path $state 'demo-seed.json')) { throw 'Original encryption key missing; never replace.' }
            $key = [Security.Cryptography.RandomNumberGenerator]::GetBytes(32)
            try { [IO.File]::WriteAllBytes($keyPath,[Security.Cryptography.ProtectedData]::Protect($key,$null,'CurrentUser')) }
            finally { [Security.Cryptography.CryptographicOperations]::ZeroMemory($key) }
        }
        $key = [Security.Cryptography.ProtectedData]::Unprotect([IO.File]::ReadAllBytes($keyPath),$null,'CurrentUser')
        try {
            if ($key.Length -ne 32) { throw 'Original encryption key invalid.' }
            $env:OASIS_COSMOS_STORAGE_ENCRYPTION_KEY = [Convert]::ToBase64String($key)
        }
        finally { [Security.Cryptography.CryptographicOperations]::ZeroMemory($key) }
        # Existing public development identity stays only in memory/stdin, never logs or argv.
        $signingKey = ((& wsl -d Ubuntu -u root -- podman exec oasis-wasmd wasmd keys export owner --keyring-backend test --unarmored-hex --unsafe --yes 2>$null) -join '').Trim()
        if ($LASTEXITCODE -or $signingKey -notmatch '^[a-fA-F0-9]{64}$') { throw 'Existing local owner signing identity unavailable.' }
        $env:OASIS_COSMOS_CQL_SIGNING_KEY = $signingKey
        $env:OASIS_COSMOS_CQL_STATE = $state
        $env:OASIS_COSMOS_NODE = (Get-Command node.exe).Source
        $env:OASIS_COSMOS_SDK_BRIDGE = Join-Path $root ($outputBridge + '/bridge.mjs')
        $safeToStop = $false
        $started = [DateTime]::UtcNow
        & dotnet test $project --no-build --no-restore /m:2 /nr:false --filter FullyQualifiedName~CosmosCqlNativeTests --logger 'trx;LogFileName=cosmos-cql-native.trx' --results-directory $results -v:q
        if ($LASTEXITCODE) { throw 'Native CQL failed; preserve original live node and signed journals for reconciliation.' }
        $report = Get-Item -LiteralPath (Join-Path $results 'cosmos-cql-native.trx')
        [xml]$trx = Get-Content -LiteralPath $report.FullName
        $counts = $trx.TestRun.ResultSummary.Counters
        if ($report.LastWriteTimeUtc -lt $started -or [int]$counts.total -ne 1 -or [int]$counts.passed -ne 1 -or [int]$counts.notExecuted -ne 0) { throw 'Exactly one fresh native pass required.' }
        if (Test-Path -LiteralPath (Join-Path $state 'demo-seed.pending.json')) { throw 'Original setup journal remains unresolved.' }
        $safeToStop = $true
    }
    finally {
        try {
            if ($nodeStarted -and $safeToStop) {
                $currentPid = ((& wsl -d Ubuntu -u root -- podman exec oasis-wasmd pgrep -x wasmd) -join '').Trim()
                if ($ownedPid -and $currentPid -and $currentPid -ne $ownedPid) { throw 'Native process ownership changed; do not stop it.' }
                & wsl -d Ubuntu -u root -- bash /mnt/c/Source/OASIS-cosmic/Scripts/TestHosts/stop-owned-cosmos-test-node.sh
                if ($LASTEXITCODE) { throw 'Owned node did not stop; preserve and inspect.' }
            }
            if ($containerStarted -and $safeToStop) {
                & wsl -d Ubuntu -u root -- podman stop --time 10 oasis-wasmd | Out-Null
                if ($LASTEXITCODE) { throw 'Retained container teardown incomplete.' }
            }
            if (!$safeToStop) { Write-Warning "Preserve original live Cosmos PID $ownedPid and $state; no restart/reset or replay." }
        }
        finally {
            $signingKey = $null
            foreach ($name in $names) { [Environment]::SetEnvironmentVariable($name,$prior[$name]) }
            $lease.Dispose()
        }
    }
}
