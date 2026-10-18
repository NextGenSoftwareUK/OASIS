param([ValidateSet('Verify','InspectPending','IndependentPrepared','IndependentInspect','IndependentReconcile','IndependentRouting','RoutingInspect','RoutingReconcile','RoutingProbe','ReviewedFees','RoutingFeeRecovery','ReviewedBank','ReviewedToken','ReviewedNFT','EscrowInspect','EscrowEstimate')][string]$Mode = 'Verify')
$ErrorActionPreference = 'Stop'
$taskRoot = [IO.Path]::GetFullPath((Split-Path (Split-Path $PSScriptRoot -Parent) -Parent))
$fixtureRoot = Join-Path $taskRoot 'TestResults/HyperDriveCosmos'
$integrationModes=@('IndependentRouting','RoutingInspect','RoutingReconcile','RoutingProbe','ReviewedFees','RoutingFeeRecovery','ReviewedBank','ReviewedToken','ReviewedNFT')
if ($Mode -eq 'Verify' -and (Test-Path -LiteralPath (Join-Path $fixtureRoot 'replication-pending.json'))) {
    throw 'Existing Cosmos replication intent requires reconciliation; refusing replay.'
}
if($Mode -in @('IndependentPrepared','IndependentRouting','ReviewedFees','ReviewedBank','ReviewedToken','ReviewedNFT')){
    if((Get-FileHash -LiteralPath (Join-Path $fixtureRoot 'replication-pending.json') -Algorithm SHA256).Hash -ne '9B5A15110B0F21FEF8FCA6D2CE215859EEBC8944E9F1F83733CACE90892DEA81'){throw 'Original intent changed; re-review isolation before native testing.'}
    if(Test-Path -LiteralPath (Join-Path $fixtureRoot 'prepared-native-pending.json')){throw 'Independent original-attempt journal requires reconciliation; no rerun allowed.'}
    if(Test-Path -LiteralPath (Join-Path $fixtureRoot 'independent-routing-pending.json')){throw 'Independent routing original requires reconciliation; no rerun allowed.'}
    if(Test-Path -LiteralPath (Join-Path $fixtureRoot 'routing-cleanup-pending.json')){throw 'Original routing cleanup requires reconciliation; no rerun allowed.'}
    if(Test-Path -LiteralPath (Join-Path $fixtureRoot 'reviewed-final-fee-pending.json')){throw 'Original reviewed-fee attempts require reconciliation; no rerun allowed.'}
    if(Test-Path -LiteralPath (Join-Path $fixtureRoot 'reviewed-bank-pending.json')){throw 'Original reviewed bank transfers require reconciliation; no rerun allowed.'}
    if(Test-Path -LiteralPath (Join-Path $fixtureRoot 'reviewed-token-pending.json')){throw 'Original reviewed token operations require reconciliation; no rerun allowed.'}
    if(Test-Path -LiteralPath (Join-Path $fixtureRoot 'reviewed-nft-pending.json')){throw 'Original reviewed NFT operations require reconciliation; no rerun allowed.'}
}
if($Mode -in @('IndependentPrepared','IndependentReconcile') -or $Mode -in $integrationModes){
    if(Test-Path -LiteralPath (Join-Path $fixtureRoot 'prepared-native-cleanup-pending.json')){throw 'An original cleanup is pending; inspect without replay.'}
    $nativeProject=Join-Path $taskRoot $(if($Mode -in $integrationModes){'OASIS Architecture/NextGenSoftware.OASIS.API.Core.HyperDrive.IntegrationTests/NextGenSoftware.OASIS.API.Core.HyperDrive.IntegrationTests.csproj'}else{'Providers/Blockchain/TestProjects/NextGenSoftware.OASIS.API.Providers.CosmosBlockChainOASIS.IntegrationTests/NextGenSoftware.OASIS.API.Providers.CosmosBlockChainOASIS.IntegrationTests.csproj'})
    foreach($project in @('Providers/Blockchain/NextGenSoftware.OASIS.API.Providers.CosmosBlockChainOASIS/NextGenSoftware.OASIS.API.Providers.CosmosBlockChainOASIS.csproj','Providers/Storage/NextGenSoftware.OASIS.API.Providers.LocalFileOASIS/NextGenSoftware.OASIS.API.Providers.LocalFileOASIS.csproj',$nativeProject)){
        & dotnet build $project --no-restore /p:BuildProjectReferences=false /m:2 /nr:false /p:UseSharedCompilation=false -v:q -clp:ErrorsOnly
        if($LASTEXITCODE -ne 0){throw 'Independent Cosmos current source build failed.'}
    }
    $outputBridge=if($Mode -in $integrationModes){'OASIS Architecture/NextGenSoftware.OASIS.API.Core.HyperDrive.IntegrationTests/bin/Debug/net10.0/CosmosSdkBridge'}else{'Providers/Blockchain/TestProjects/NextGenSoftware.OASIS.API.Providers.CosmosBlockChainOASIS.IntegrationTests/bin/Debug/net10.0/CosmosSdkBridge'}
    & (Join-Path $PSScriptRoot 'Use-ReusableHederaOutputDependencies.ps1') -Provider Cosmos -OutputBridge $outputBridge
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
$signingKey = $null
$failures = [Collections.Generic.List[Exception]]::new()
try {
    if ($containerState -eq 'exited') {
        & wsl -d Ubuntu -u root -- podman start oasis-wasmd
        if ($LASTEXITCODE -ne 0) { throw 'Existing Cosmos container resume failed; no replacement created.' }
        $containerStarted = $true
    }
    # The existing local development key stays in process memory and is never printed or persisted.
    if($Mode -notin @('RoutingInspect','RoutingFeeRecovery','EscrowInspect','EscrowEstimate')){
        $signingKey = ((& wsl -d Ubuntu -u root -- podman exec oasis-wasmd wasmd keys export owner --keyring-backend test --unarmored-hex --unsafe --yes 2>$null) -join "`n").Trim()
        if ($LASTEXITCODE -ne 0 -or $signingKey -notmatch '^[0-9a-fA-F]{64}$') { throw 'Existing local Cosmos signing key unavailable.' }
    }
    & wsl -d Ubuntu -u root -- podman exec -d oasis-wasmd wasmd start --home /root/.wasmd --rpc.laddr tcp://0.0.0.0:26657
    if ($LASTEXITCODE -ne 0) { throw 'Retained Cosmos node start failed.' }
    $started = $true
    $deadline = [DateTime]::UtcNow.AddSeconds(30)
    $status = $null
    $initialHeight = $null
    $ready = $false
    while ([DateTime]::UtcNow -lt $deadline) {
        try { $status = Invoke-RestMethod 'http://127.0.0.1:26657/status' -TimeoutSec 2 }
        catch { Start-Sleep -Milliseconds 250; continue }
        if($status.result.node_info.network -ne 'oasis-cosmos-local'){throw 'Unexpected chain identity at retained Cosmos RPC.'}
        $height=[long]$status.result.sync_info.latest_block_height
        if($null -eq $initialHeight){$initialHeight=$height}
        # SDK0.54 simulation needs a fresh check-state block header, not merely a live RPC endpoint.
        if($status.result.sync_info.catching_up -eq $false -and $height -gt $initialHeight){$ready=$true;break}
        Start-Sleep -Milliseconds 250
    }
    if (!$ready) { throw 'Retained Cosmos chain did not commit a fresh ready block after startup.' }
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
    if($Mode -in @('EscrowInspect','EscrowEstimate')){
        $escrowFence=Join-Path $fixtureRoot 'replication-pending.json'
        $escrowFenceHash=(Get-FileHash -LiteralPath $escrowFence -Algorithm SHA256).Hash
        if($escrowFenceHash -ne '9B5A15110B0F21FEF8FCA6D2CE215859EEBC8944E9F1F83733CACE90892DEA81'){throw 'Original Cosmos fence changed; inspect before continuing.'}
        $probeRequest=@{operation='query';rpcEndpoint='http://127.0.0.1:26657';chainId='oasis-cosmos-local';contractAddress=$contracts[0];
            message=@{asset=@{message=@{token_escrow_protocol=@{}}}}} | ConvertTo-Json -Depth 8 -Compress
        $probeRaw=$probeRequest | & node (Join-Path $taskRoot 'Providers/Blockchain/NextGenSoftware.OASIS.API.Providers.CosmosBlockChainOASIS/SdkBridge/bridge.mjs')
        $probeExit=$LASTEXITCODE
        $probe=($probeRaw -join "`n") | ConvertFrom-Json
        $codeRaw=& wsl -d Ubuntu -u root -- podman exec oasis-wasmd wasmd query wasm code-info 1 --node http://127.0.0.1:26657 --output json
        if($LASTEXITCODE -ne 0){throw 'Existing public contract code metadata query failed.'}
        $evidence=@{chain='oasis-cosmos-local';contract=$contracts[0];sdkQuery=$probe;codeInfo=(($codeRaw -join "`n") | ConvertFrom-Json);
            container=$containerId;image=$image;checkedAtUtc=[DateTime]::UtcNow.ToString('O');originalFenceSha256=$escrowFenceHash;
            signingKeyExported=$false;nativeSubmissions=0;verifiedProtocol=($probeExit -eq 0 -and $probe.ok -eq $true -and $probe.result -eq 1)}
        if($Mode -eq 'EscrowEstimate'){
            $wasmPath='\\wsl.localhost\Ubuntu\var\tmp\oasis-cosmos-contract-target\wasm32-unknown-unknown\release\oasis_cosmos_storage.wasm'
            $wasmHash='fcae1521a4564ae4548ad25bae886e432ad1049ece035b3f70ae3a4e961e014c'
            if((Get-FileHash -LiteralPath $wasmPath).Hash.ToLowerInvariant() -ne $wasmHash){throw 'Pinned current escrow bytecode differs; rebuild/review before estimating.'}
            $estimateRequest=@{operation='estimateUpload';rpcEndpoint='http://127.0.0.1:26657';chainId='oasis-cosmos-local';addressPrefix='wasm';
                sender=$evidence.codeInfo.creator;gasPrice='0.025stake';wasmBase64=[Convert]::ToBase64String([IO.File]::ReadAllBytes($wasmPath));expectedWasmSha256=$wasmHash} | ConvertTo-Json -Compress
            $estimateRaw=$estimateRequest | & node (Join-Path $taskRoot 'Providers/Blockchain/NextGenSoftware.OASIS.API.Providers.CosmosBlockChainOASIS/SdkBridge/bridge.mjs')
            $estimateExit=$LASTEXITCODE
            $evidence.uploadEstimate=($estimateRaw -join "`n") | ConvertFrom-Json
        }
        if((Get-FileHash -LiteralPath $escrowFence -Algorithm SHA256).Hash -ne $escrowFenceHash){throw 'Read-only inspection detected a changed original fence.'}
        $escrowEvidenceJson=$evidence | ConvertTo-Json -Depth 12
        $probeReport=if($Mode -eq 'EscrowEstimate'){'escrow-upload-estimate-current.json'}else{'escrow-protocol-inspection-current.json'}
        [IO.File]::WriteAllText((Join-Path $fixtureRoot $probeReport),$escrowEvidenceJson)
        Write-Output $escrowEvidenceJson
        if($Mode -eq 'EscrowEstimate' -and ($estimateExit -ne 0 -or !$evidence.uploadEstimate.ok)){throw 'Read-only upload simulation failed; no deployment authorized.'}
        if($Mode -eq 'EscrowInspect' -and !$evidence.verifiedProtocol){throw 'Retained contract does not establish escrow protocol1; inspect evidence before any escrow mutation.'}
    }elseif($Mode -eq 'ReviewedNFT'){
        & (Join-Path $PSScriptRoot 'Run-ReusableUsageRecoveryTests.ps1') -CosmosPreparedOwners -CosmosReviewedNFT
        if($LASTEXITCODE -ne 0){throw 'Actual NFT interface verification failed; preserve originals and ownership journal.'}
    }elseif($Mode -eq 'ReviewedToken'){
        & (Join-Path $PSScriptRoot 'Run-ReusableUsageRecoveryTests.ps1') -CosmosPreparedOwners -CosmosReviewedToken
        if($LASTEXITCODE -ne 0){throw 'Actual token interface verification failed; preserve originals and ownership journal.'}
    }elseif($Mode -eq 'ReviewedBank'){
        & (Join-Path $PSScriptRoot 'Run-ReusableUsageRecoveryTests.ps1') -CosmosPreparedOwners -CosmosReviewedBank
        if($LASTEXITCODE -ne 0){throw 'Actual bank interface verification failed; preserve original transfers and journal.'}
    }elseif($Mode -eq 'RoutingFeeRecovery'){
        & (Join-Path $PSScriptRoot 'Run-ReusableUsageRecoveryTests.ps1') -CosmosPreparedOwners -CosmosRoutingFeeRecovery
        if($LASTEXITCODE -ne 0){throw 'Original-only Cosmos fee recovery failed; preserve original journal and outbox.'}
    }elseif($Mode -eq 'ReviewedFees'){
        & (Join-Path $PSScriptRoot 'Run-ReusableUsageRecoveryTests.ps1') -CosmosPreparedOwners -CosmosReviewedFees
        if($LASTEXITCODE -ne 0){throw 'Actual reviewed fee verification failed; preserve original journal and outbox.'}
    }elseif($Mode -eq 'RoutingProbe'){
        & (Join-Path $PSScriptRoot 'Run-ReusableUsageRecoveryTests.ps1') -CosmosPreparedOwners -CosmosRoutingInspect -CosmosRoutingProbe
        if($LASTEXITCODE -ne 0){throw 'Zero-budget Cosmos probe failed; original incident remains fenced.'}
    }elseif($Mode -eq 'RoutingReconcile'){
        & (Join-Path $PSScriptRoot 'Run-ReusableUsageRecoveryTests.ps1') -CosmosPreparedOwners -CosmosRoutingInspect -CosmosRoutingReconcile
        if($LASTEXITCODE -ne 0){throw 'Reviewed routing cleanup failed; preserve original attempts and journals.'}
    }elseif($Mode -eq 'RoutingInspect'){
        & (Join-Path $PSScriptRoot 'Run-ReusableUsageRecoveryTests.ps1') -CosmosPreparedOwners -CosmosRoutingInspect
        if($LASTEXITCODE -ne 0){throw 'Read-only routing original inspection failed; preserve journals and ledger.'}
    }elseif($Mode -eq 'IndependentRouting'){
        & (Join-Path $PSScriptRoot 'Run-ReusableUsageRecoveryTests.ps1') -CosmosPreparedOwners -CosmosRouting
        if($LASTEXITCODE -ne 0){throw 'Independent native routing failed; preserve its original journal.'}
    }elseif($Mode -eq 'IndependentInspect'){
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
    $testStarted=[DateTime]::UtcNow
    & dotnet test (Join-Path $taskRoot 'OASIS Architecture/NextGenSoftware.OASIS.API.Core.HyperDrive.IntegrationTests/NextGenSoftware.OASIS.API.Core.HyperDrive.IntegrationTests.csproj') --no-build --no-restore --filter "FullyQualifiedName~IntegrationTests.CosmosRealProviderRoutingTests.$method" --logger "trx;LogFileName=$reportName" --results-directory $fixtureRoot -v:q
    if ($LASTEXITCODE -ne 0) { throw 'Real Cosmos V2 regression failed; preserve pending intent and inspect TRX.' }
    $report=Get-Item -LiteralPath (Join-Path $fixtureRoot $reportName)
    if($report.LastWriteTimeUtc -lt $testStarted){throw 'Cosmos routing report is stale.'}
    [xml]$evidence=Get-Content -LiteralPath $report.FullName
    $counts=$evidence.TestRun.ResultSummary.Counters
    if([int]$counts.total -ne 1 -or [int]$counts.passed -ne 1 -or [int]$counts.notExecuted -ne 0){throw 'The selected native Cosmos routing test must actually execute and pass.'}
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
