param([ValidateSet('Verify','InspectPending','IndependentPrepared','IndependentInspect','IndependentReconcile')][string]$Mode = 'Verify')
$ErrorActionPreference = 'Stop'
$taskRoot = [IO.Path]::GetFullPath((Split-Path (Split-Path $PSScriptRoot -Parent) -Parent))
$fixtureRoot = Join-Path $taskRoot 'TestResults/HyperDriveCosmos'
if ($Mode -eq 'Verify' -and (Test-Path -LiteralPath (Join-Path $fixtureRoot 'replication-pending.json'))) {
    throw 'Existing Cosmos replication intent requires reconciliation; refusing replay.'
}
if($Mode -eq 'IndependentPrepared'){
    if((Get-FileHash -LiteralPath (Join-Path $fixtureRoot 'replication-pending.json') -Algorithm SHA256).Hash -ne '9B5A15110B0F21FEF8FCA6D2CE215859EEBC8944E9F1F83733CACE90892DEA81'){throw 'Original intent changed; re-review isolation before native testing.'}
    if(Test-Path -LiteralPath (Join-Path $fixtureRoot 'prepared-native-pending.json')){throw 'Independent original-attempt journal requires reconciliation; no rerun allowed.'}
}
if($Mode -in @('IndependentPrepared','IndependentReconcile')){
    if(Test-Path -LiteralPath (Join-Path $fixtureRoot 'prepared-native-cleanup-pending.json')){throw 'An original cleanup is pending; inspect without replay.'}
    $nativeProject=Join-Path $taskRoot 'Providers/Blockchain/TestProjects/NextGenSoftware.OASIS.API.Providers.CosmosBlockChainOASIS.IntegrationTests/NextGenSoftware.OASIS.API.Providers.CosmosBlockChainOASIS.IntegrationTests.csproj'
    foreach($project in @('Providers/Blockchain/NextGenSoftware.OASIS.API.Providers.CosmosBlockChainOASIS/NextGenSoftware.OASIS.API.Providers.CosmosBlockChainOASIS.csproj',$nativeProject)){
        & dotnet build $project --no-restore /p:BuildProjectReferences=false /m:2 /nr:false /p:UseSharedCompilation=false -v:q -clp:ErrorsOnly
        if($LASTEXITCODE -ne 0){throw 'Independent Cosmos current source build failed.'}
    }
    & (Join-Path $PSScriptRoot 'Use-ReusableHederaOutputDependencies.ps1') -Provider Cosmos -OutputBridge 'Providers/Blockchain/TestProjects/NextGenSoftware.OASIS.API.Providers.CosmosBlockChainOASIS.IntegrationTests/bin/Debug/net10.0/CosmosSdkBridge'
}
$containerId = (& wsl -d Ubuntu -u root -- podman inspect oasis-wasmd --format '{{.Id}}').Trim()
if ($LASTEXITCODE -ne 0 -or $containerId -ne '58a873de020d2f79ab11e7087361db25063c277a5b5a5c2cabba72263b35f8f3') { throw 'Retained Cosmos container identity mismatch.' }
$image = (& wsl -d Ubuntu -u root -- podman inspect oasis-wasmd --format '{{.ImageName}}').Trim()
if ($LASTEXITCODE -ne 0 -or $image -ne 'docker.io/cosmwasm/wasmd@sha256:3741178a4d747fd5cf281550ab4ac30d771163af1c543f9da503c411a1691773') { throw 'Retained Cosmos image mismatch.' }
$containerState = (& wsl -d Ubuntu -u root -- podman inspect oasis-wasmd --format '{{.State.Status}}').Trim()
if ($LASTEXITCODE -ne 0 -or $containerState -notin @('running','exited')) { throw 'Retained Cosmos container state requires inspection.' }
& wsl -d Ubuntu -u root -- podman exec oasis-wasmd pgrep -x wasmd *> $null
if ($LASTEXITCODE -eq 0) { throw 'Existing wasmd process is in use; refusing to take ownership.' }
$names = @('OASIS_COSMOS_TEST_RPC','OASIS_COSMOS_TEST_CHAIN','OASIS_COSMOS_TEST_KEY','OASIS_COSMOS_TEST_CONTRACT','OASIS_COSMOS_TEST_STOP_SCRIPT','OASIS_HYPERDRIVE_TEST_ROOT','OASIS_COSMOS_INSPECTION_ID')
$prior = @{}
foreach ($name in $names) { $prior[$name] = [Environment]::GetEnvironmentVariable($name,'Process') }
$started = $false
$containerStarted = $false
$failures = [Collections.Generic.List[Exception]]::new()
try {
    if ($containerState -eq 'exited') {
        & wsl -d Ubuntu -u root -- podman start oasis-wasmd
        if ($LASTEXITCODE -ne 0) { throw 'Existing Cosmos container resume failed; no replacement created.' }
        $containerStarted = $true
    }
    # The existing local development key stays in process memory and is never printed or persisted.
    $signingKey = ((& wsl -d Ubuntu -u root -- podman exec oasis-wasmd wasmd keys export owner --keyring-backend test --unarmored-hex --unsafe --yes 2>$null) -join "`n").Trim()
    if ($LASTEXITCODE -ne 0 -or $signingKey -notmatch '^[0-9a-fA-F]{64}$') { throw 'Existing local Cosmos signing key unavailable.' }
    & wsl -d Ubuntu -u root -- podman exec -d oasis-wasmd wasmd start --home /root/.wasmd --rpc.laddr tcp://0.0.0.0:26657
    if ($LASTEXITCODE -ne 0) { throw 'Retained Cosmos node start failed.' }
    $started = $true
    $deadline = [DateTime]::UtcNow.AddSeconds(30)
    $status = $null
    while ([DateTime]::UtcNow -lt $deadline) {
        try { $status = Invoke-RestMethod 'http://127.0.0.1:26657/status' -TimeoutSec 2; break }
        catch { Start-Sleep -Milliseconds 250 }
    }
    if ($status.result.node_info.network -ne 'oasis-cosmos-local' -or $status.result.sync_info.catching_up -ne $false) { throw 'Expected retained Cosmos chain is not ready.' }
    $contractsJson = & wsl -d Ubuntu -u root -- podman exec oasis-wasmd wasmd query wasm list-contract-by-code 1 --node http://127.0.0.1:26657 --output json
    if ($LASTEXITCODE -ne 0) { throw 'Existing contract inventory failed.' }
    $contracts = @((($contractsJson -join "`n") | ConvertFrom-Json).contracts)
    if ($contracts.Count -ne 1 -or $contracts[0] -notmatch '^wasm1[0-9a-z]+$') { throw 'Expected one retained Cosmos storage contract; refusing guessed deployment.' }
    $env:OASIS_COSMOS_TEST_RPC = 'http://127.0.0.1:26657'
    $env:OASIS_COSMOS_TEST_CHAIN = 'oasis-cosmos-local'
    $env:OASIS_COSMOS_TEST_KEY = $signingKey
    $env:OASIS_COSMOS_TEST_CONTRACT = $contracts[0]
    $env:OASIS_COSMOS_TEST_STOP_SCRIPT = '/mnt/c/Source/OASIS-hyperdrive-v2-gaps/Scripts/TestHosts/stop-owned-cosmos-test-node.sh'
    $env:OASIS_HYPERDRIVE_TEST_ROOT = $fixtureRoot
    if ($Mode -eq 'InspectPending') {
        $env:OASIS_COSMOS_INSPECTION_ID=[Guid]::NewGuid().ToString('D')
        & node (Join-Path $PSScriptRoot 'inspect-cosmos-pending-transactions.mjs')
        if ($LASTEXITCODE -ne 0) { throw 'Read-only Cosmos native transaction inspection failed; pending intent preserved.' }
    }
    if($Mode -eq 'IndependentInspect'){
        & node (Join-Path $PSScriptRoot 'inspect-cosmos-independent-prepared.mjs')
        if($LASTEXITCODE -ne 0){throw 'Independent original inspection failed; preserve evidence.'}
    }elseif($Mode -eq 'IndependentReconcile'){
        & (Join-Path $PSScriptRoot 'Run-ReusableUsageRecoveryTests.ps1') -CosmosPreparedOwners -CosmosNative -CosmosReconcile
        if($LASTEXITCODE -ne 0){throw 'Original independent reconciliation failed; no retry permitted.'}
    }elseif($Mode -eq 'IndependentPrepared'){
        & (Join-Path $PSScriptRoot 'Run-ReusableUsageRecoveryTests.ps1') -CosmosPreparedOwners -CosmosNative
        if($LASTEXITCODE -ne 0){throw 'Independent Cosmos native verification failed; preserve its original journal.'}
    }else{
    $method = if ($Mode -eq 'InspectPending') { 'InspectPendingCosmosV2IntentWithoutMutations' } else { 'V2ReplicatesToCosmosAndSelectsFromMeasuredRealProviderReads' }
    $reportName = if ($Mode -eq 'InspectPending') { 'cosmos-pending-inspection-current.trx' } else { 'cosmos-retained-v2-current.trx' }
    & dotnet test (Join-Path $taskRoot 'OASIS Architecture/NextGenSoftware.OASIS.API.Core.HyperDrive.IntegrationTests/NextGenSoftware.OASIS.API.Core.HyperDrive.IntegrationTests.csproj') --no-build --no-restore --filter "FullyQualifiedName~IntegrationTests.CosmosRealProviderRoutingTests.$method" --logger "trx;LogFileName=$reportName" --results-directory $fixtureRoot -v:q
    if ($LASTEXITCODE -ne 0) { throw 'Real Cosmos V2 regression failed; preserve pending intent and inspect TRX.' }
    }
}
catch { $failures.Add($_.Exception) }
finally {
    $signingKey = $null
    foreach ($name in $names) { [Environment]::SetEnvironmentVariable($name,$prior[$name],'Process') }
    if ($started) {
        try {
            & wsl -d Ubuntu -u root -- bash /mnt/c/Source/OASIS-hyperdrive-v2-gaps/Scripts/TestHosts/stop-owned-cosmos-test-node.sh
            if ($LASTEXITCODE -ne 0) { throw 'Owned Cosmos node stop failed; inspect before any restart.' }
        } catch { $failures.Add($_.Exception) }
    }
    if ($containerStarted) {
        try {
            & wsl -d Ubuntu -u root -- bash /mnt/c/Source/OASIS-hyperdrive-v2-gaps/Scripts/TestHosts/stop-owned-cosmos-test-container.sh
            if ($LASTEXITCODE -ne 0) { throw 'Owned Cosmos container stop failed; inspect before resuming.' }
        } catch { $failures.Add($_.Exception) }
    }
}
if ($failures.Count -eq 1) { [Runtime.ExceptionServices.ExceptionDispatchInfo]::Capture($failures[0]).Throw() }
if ($failures.Count -gt 1) { throw [AggregateException]::new('Cosmos verification and owned cleanup failures; original failure is first.', $failures) }
