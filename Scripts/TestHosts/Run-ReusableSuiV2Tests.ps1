param([ValidateSet('Healthy','Outage','Prepared','Fee','NFT','Token','Assets','Escrow','CSharpEscrow','InspectPrepared','ArchivePrepared','ArchiveReplication','ArchiveWalletSearch','CSharpNFT','CSharpAssets','History','Balance')][string]$Mode = 'Healthy',[string]$EvidenceRoot,[string]$ProviderEvidenceRoot)
$ErrorActionPreference = 'Stop'
$taskRoot = [IO.Path]::GetFullPath((Split-Path (Split-Path $PSScriptRoot -Parent) -Parent))
$fixtureRoot = if($EvidenceRoot){[IO.Path]::GetFullPath($EvidenceRoot)}else{Join-Path $taskRoot 'TestResults/HyperDriveSui'}
$defaultProviderRoot=Join-Path $taskRoot 'TestResults/SuiCurrent'
$providerEvidenceRoot=if($ProviderEvidenceRoot){[IO.Path]::GetFullPath($ProviderEvidenceRoot)}else{$defaultProviderRoot}
foreach($existingRoot in @($fixtureRoot,$providerEvidenceRoot)){
    if(!(Test-Path -LiteralPath $existingRoot -PathType Container)){throw 'Existing retained Sui evidence directory required; no replacement will be created.'}
}
if($providerEvidenceRoot -ne $defaultProviderRoot -and $Mode -notin @('Balance','CSharpNFT','CSharpAssets')){
    throw 'Alternate retained evidence paths are reviewed only for Balance and admitted CSharp NFT/assets; other child modes require their own path review.'
}
$deploymentPath = '\\wsl.localhost\Ubuntu\var\tmp\oasis-sui\deployment.json'
$fullnodeConfig='\\wsl.localhost\Ubuntu\var\tmp\oasis-sui\network\fullnode.yaml'
$rpcBindings=@(Get-Content -LiteralPath $fullnodeConfig | Where-Object {$_ -match '^json-rpc-address:'})
if($rpcBindings.Count -ne 1 -or $rpcBindings[0] -cne 'json-rpc-address: "127.0.0.1:9000"'){
    throw 'Reviewed retained Sui fullnode config must bind RPC to loopback BEFORE startup.'
}
$startPath=(Join-Path $PSScriptRoot 'start-sui-test-network.sh').Replace('\','/')
$startLines=@(& wsl -d Ubuntu -u root -- wslpath -a $startPath)
if($LASTEXITCODE -ne 0 -or $startLines.Count -ne 1){throw 'Current Sui launcher path could not be resolved.'}
$linuxStartScript=$startLines[0].Trim()
$stopPath=(Join-Path $PSScriptRoot 'stop-owned-sui-test-node.sh').Replace('\','/')
$stopLines=@(& wsl -d Ubuntu -u root -- wslpath -a $stopPath)
if($LASTEXITCODE -ne 0 -or $stopLines.Count -ne 1){throw 'Current Sui stop-helper path could not be resolved.'}
$linuxStopScript=$stopLines[0].Trim()
if(Test-Path -LiteralPath (Join-Path $providerEvidenceRoot 'prepared-http-native-pending.jsonl')){throw 'Pending HTTP native Sui ownership requires original reconciliation; do not replay.'}
if (!(Test-Path -LiteralPath $deploymentPath)) { throw 'Retained protected Sui deployment is unavailable; no copy or new wallet created.' }
if ($Mode -notin @('ArchiveReplication','ArchiveWalletSearch') -and (Test-Path -LiteralPath (Join-Path $fixtureRoot 'encrypted-replication-pending.json'))) { throw 'Pending Sui ownership intent requires reconciliation before another run.' }
if ($Mode -notin @('InspectPrepared','ArchivePrepared') -and (Test-Path -LiteralPath (Join-Path $providerEvidenceRoot 'prepared-live-pending.json'))) { throw 'Pending prepared Sui ownership requires reconciliation before another run.' }
if (Test-Path -LiteralPath (Join-Path $providerEvidenceRoot 'nft-live-pending.jsonl')) { throw 'Pending NFT ownership requires reconciliation before another run.' }
if (Test-Path -LiteralPath (Join-Path $providerEvidenceRoot 'token-live-pending.jsonl')) { throw 'Pending token ownership requires reconciliation before another run.' }
if (Test-Path -LiteralPath (Join-Path $providerEvidenceRoot 'csharp-nft-live-pending.jsonl')) { throw 'Pending CSharp NFT ownership requires original-attempt reconciliation before another run.' }
if (Test-Path -LiteralPath (Join-Path $providerEvidenceRoot 'csharp-token-live-pending.jsonl')) { throw 'Pending CSharp token ownership requires original-attempt reconciliation before another run.' }
if (Test-Path -LiteralPath (Join-Path $providerEvidenceRoot 'escrow-live-pending.jsonl')) { throw 'Pending escrow ownership requires original-attempt reconciliation; do not republish or replay.' }
if (Test-Path -LiteralPath (Join-Path $providerEvidenceRoot 'csharp-escrow-live-pending.jsonl')) { throw 'Pending CSharp escrow requires original-attempt reconciliation; do not replay.' }
& wsl -d Ubuntu -u root -- test -f /var/tmp/oasis-sui/network/network.yaml
if ($LASTEXITCODE -ne 0) { throw 'Existing Sui ledger configuration missing; refusing genesis creation.' }
& wsl -d Ubuntu -u root -- pgrep -x sui *> $null
if ($LASTEXITCODE -eq 0) { throw 'Existing Sui runtime is in use; refusing to take ownership.' }
$project = Join-Path $taskRoot 'OASIS Architecture/NextGenSoftware.OASIS.API.Core.HyperDrive.IntegrationTests/NextGenSoftware.OASIS.API.Core.HyperDrive.IntegrationTests.csproj'
if($Mode -in @('Prepared','Fee','InspectPrepared','ArchivePrepared','CSharpNFT','CSharpAssets','CSharpEscrow','History','Balance')){$project=Join-Path $taskRoot 'Providers/Blockchain/TestProjects/NextGenSoftware.OASIS.API.Providers.SuiOASIS.IntegrationTests/NextGenSoftware.OASIS.API.Providers.SuiOASIS.IntegrationTests.csproj'}
foreach($sourceProject in @((Join-Path $taskRoot 'Providers/Blockchain/NextGenSoftware.OASIS.API.Providers.SuiOASIS/NextGenSoftware.OASIS.API.Providers.SuiOASIS.csproj'),(Join-Path $taskRoot 'Providers/Storage/NextGenSoftware.OASIS.API.Providers.LocalFileOASIS/NextGenSoftware.OASIS.API.Providers.LocalFileOASIS.csproj'),$project)){
    & dotnet build $sourceProject --no-restore /p:BuildProjectReferences=false /m:2 /nr:false /p:UseSharedCompilation=false -v:q -clp:ErrorsOnly
    if ($LASTEXITCODE -ne 0) { throw 'Current Sui producer/consumer build failed; do not substitute stale assemblies.' }
}
& (Join-Path $PSScriptRoot 'Assert-ReusableProviderBuildIdentity.ps1') -RepositoryRoot $taskRoot -ConsumerOutput (Join-Path (Split-Path $project -Parent) 'bin/Debug/net10.0') -VerifySourceDocuments
$outputBridge=if($Mode -in @('Prepared','Fee','InspectPrepared','ArchivePrepared','CSharpNFT','CSharpAssets','CSharpEscrow','History','Balance')){'Providers/Blockchain/TestProjects/NextGenSoftware.OASIS.API.Providers.SuiOASIS.IntegrationTests/bin/Debug/net10.0/SuiSdkBridge'}else{'OASIS Architecture/NextGenSoftware.OASIS.API.Core.HyperDrive.IntegrationTests/bin/Debug/net10.0/SuiSdkBridge'}
& (Join-Path $PSScriptRoot 'Use-ReusableHederaOutputDependencies.ps1') -Provider Sui -OutputBridge $outputBridge
$names = @('OASIS_SUI_TEST_PROVIDER_EVIDENCE_ROOT','OASIS_SUI_TEST_RPC','OASIS_SUI_TEST_DEPLOYMENT','OASIS_SUI_TEST_OWNED_PID','OASIS_SUI_TEST_STOP_SCRIPT','OASIS_HYPERDRIVE_TEST_ROOT','OASIS_SUI_SDK_BRIDGE','OASIS_SUI_SUCCESSFUL_FEE_RECEIPT','OASIS_SUI_TEST_NFT_JOURNAL','OASIS_SUI_TEST_FAUCET','OASIS_SUI_TEST_TOKEN_JOURNAL','OASIS_SUI_TEST_ESCROW_JOURNAL','OASIS_SUI_TEST_ESCROW_CACHE')
$prior = @{}
foreach ($name in $names) { $prior[$name] = [Environment]::GetEnvironmentVariable($name,'Process') }
$ownedPid = $null
$launcher = $null
try {
    # Hidden launcher, retained executable/ledger, fixed compact startup logs.
    $launcher = Start-Process -FilePath 'wsl.exe' -ArgumentList @('-d','Ubuntu','-u','root','--','bash',$linuxStartScript,'--reuse-only') -WindowStyle Hidden -PassThru -RedirectStandardOutput (Join-Path $fixtureRoot 'node-current.stdout.log') -RedirectStandardError (Join-Path $fixtureRoot 'node-current.stderr.log')
    $deadline = [DateTime]::UtcNow.AddSeconds(30)
    while ([DateTime]::UtcNow -lt $deadline) {
        $found = @(& wsl -d Ubuntu -u root -- pgrep -x sui 2>$null)
        if ($found.Count -eq 1) { $ownedPid = $found[0].Trim(); break }
        if ($launcher.HasExited) { throw 'Retained Sui startup terminated; inspect fixed startup logs.' }
        Start-Sleep -Milliseconds 250
    }
    if (!$ownedPid -or $ownedPid -notmatch '^[0-9]+$') { throw 'No uniquely owned Sui PID established.' }
    $executable = (& wsl -d Ubuntu -u root -- readlink -f "/proc/$ownedPid/exe").Trim()
    if ($executable -ne '/var/tmp/oasis-runtimes/sui-1.81.1/sui') { throw 'Owned runtime executable mismatch.' }
    $settings = [IO.File]::ReadAllText($deploymentPath) | ConvertFrom-Json
    $chain = $null
    $readinessError = $null
    while ([DateTime]::UtcNow -lt $deadline) {
        try {
            $chain = Invoke-RestMethod 'http://127.0.0.1:9000' -Method Post -ContentType 'application/json' -Body '{"jsonrpc":"2.0","id":1,"method":"sui_getChainIdentifier","params":[]}' -TimeoutSec 2
            if ($chain.result) { break }
        } catch { $readinessError = $_.Exception.Message }
        Start-Sleep -Milliseconds 250
    }
    if (!$chain.result) { throw "Retained Sui RPC is not ready: $readinessError" }
    # JSON-RPC reports the short legacy identifier; this provider uses the
    # official gRPC SDK's full genesis identifier. Verify at that same boundary.
    $probeInput = @{operation='probe';rpcEndpoint='http://127.0.0.1:9000';network='localnet';chainId=$settings.chainId} | ConvertTo-Json -Compress
    $probeOutput = $probeInput | & node (Join-Path $taskRoot 'Providers/Blockchain/NextGenSoftware.OASIS.API.Providers.SuiOASIS/SdkBridge/bridge.mjs')
    if ($LASTEXITCODE -ne 0) { throw 'Retained Sui SDK chain verification failed.' }
    $probe = ($probeOutput -join "`n") | ConvertFrom-Json
    if (!$probe.ok -or $probe.result.chainIdentifier -ne $settings.chainId) { throw 'Retained Sui SDK does not match protected deployment chain.' }
    $sockets=@(& wsl -d Ubuntu -u root -- ss -H -ltnp)
    if($LASTEXITCODE -ne 0){throw 'Cannot verify retained Sui RPC socket ownership.'}
    $rpcSockets=@($sockets | Where-Object {$_ -match ':9000\s'})
    if($rpcSockets.Count -ne 1 -or $rpcSockets[0] -notmatch '\s127\.0\.0\.1:9000\s' -or $rpcSockets[0] -notmatch "pid=$ownedPid,"){
        throw 'Sui RPC must be one loopback-only socket owned by the verified runtime PID.'
    }
    $settings = $null
    $env:OASIS_SUI_TEST_RPC = 'http://127.0.0.1:9000'
    $env:OASIS_SUI_TEST_DEPLOYMENT = $deploymentPath
    $env:OASIS_SUI_TEST_OWNED_PID = $ownedPid
    $env:OASIS_SUI_TEST_STOP_SCRIPT = $linuxStopScript
    $env:OASIS_HYPERDRIVE_TEST_ROOT = $fixtureRoot
    if($Mode -in @('CSharpNFT','CSharpAssets')){$env:OASIS_SUI_TEST_PROVIDER_EVIDENCE_ROOT=$providerEvidenceRoot}
    if($Mode -eq 'Escrow'){
        $env:OASIS_SUI_TEST_ESCROW_JOURNAL=Join-Path $providerEvidenceRoot 'escrow-live-pending.jsonl'
        $env:OASIS_SUI_TEST_ESCROW_CACHE=Join-Path $providerEvidenceRoot 'sui-escrow-deployment.json'
        & node --test --test-reporter=tap (Join-Path $taskRoot 'Providers/Blockchain/NextGenSoftware.OASIS.API.Providers.SuiOASIS/SdkBridge/token-escrow-runtime.test.mjs') 2>&1 | Tee-Object -FilePath (Join-Path $providerEvidenceRoot 'escrow-live-current.tap')
        if($LASTEXITCODE -ne 0){throw 'Real escrow acceptance failed; preserve signed ownership and never republish/replay.'}
    }elseif($Mode -eq 'Assets'){
        $env:OASIS_SUI_TEST_FAUCET='http://127.0.0.1:9123'
        $env:OASIS_SUI_TEST_TOKEN_JOURNAL=Join-Path $providerEvidenceRoot 'token-live-pending.jsonl'
        $env:OASIS_SUI_TEST_NFT_JOURNAL=Join-Path $providerEvidenceRoot 'nft-live-pending.jsonl'
        $bridgeRoot=Join-Path $taskRoot 'Providers/Blockchain/NextGenSoftware.OASIS.API.Providers.SuiOASIS/SdkBridge'
        & node --test --test-concurrency=1 --test-reporter=tap (Join-Path $bridgeRoot 'nft-runtime.test.mjs') (Join-Path $bridgeRoot 'token-runtime.test.mjs') 2>&1 | Tee-Object -FilePath (Join-Path $providerEvidenceRoot 'assets-live-current.tap')
        if($LASTEXITCODE -ne 0){throw 'Real asset fixtures failed; preserve ownership journals and do not replay.'}
    }elseif($Mode -eq 'Token'){
        $env:OASIS_SUI_TEST_FAUCET='http://127.0.0.1:9123'
        $env:OASIS_SUI_TEST_TOKEN_JOURNAL=Join-Path $providerEvidenceRoot 'token-live-pending.jsonl'
        & node --test --test-reporter=tap (Join-Path $taskRoot 'Providers/Blockchain/NextGenSoftware.OASIS.API.Providers.SuiOASIS/SdkBridge/token-runtime.test.mjs') 2>&1 | Tee-Object -FilePath (Join-Path $providerEvidenceRoot 'token-live-current.tap')
        if($LASTEXITCODE -ne 0){throw 'Real token fixture failed; preserve prior balances and any pending journal.'}
    }elseif($Mode -eq 'NFT'){
        $env:OASIS_SUI_TEST_FAUCET='http://127.0.0.1:9123'
        $env:OASIS_SUI_TEST_NFT_JOURNAL=Join-Path $providerEvidenceRoot 'nft-live-pending.jsonl'
        & node --test --test-reporter=tap (Join-Path $taskRoot 'Providers/Blockchain/NextGenSoftware.OASIS.API.Providers.SuiOASIS/SdkBridge/nft-runtime.test.mjs') 2>&1 | Tee-Object -FilePath (Join-Path $providerEvidenceRoot 'nft-live-current.tap')
        if($LASTEXITCODE -ne 0){throw 'Real NFT fixture failed; retain any pending journal and inspect original outcomes.'}
    }elseif($Mode -eq 'Balance'){
        $env:OASIS_SUI_SDK_BRIDGE=Join-Path $taskRoot 'Providers/Blockchain/NextGenSoftware.OASIS.API.Providers.SuiOASIS/SdkBridge/bridge.mjs'
        $started=[DateTime]::UtcNow
        & dotnet test $project --no-build --no-restore --filter 'FullyQualifiedName~RetainedNativeBalanceSurfacesAgreeWithExactSdkUnitsWithoutMutation' --logger 'trx;LogFileName=sui-native-balance-current.trx' --results-directory (Join-Path $providerEvidenceRoot '.') -v:q
        if($LASTEXITCODE -ne 0){throw 'Read-only Sui balance verification failed.'}
        $report=Get-Item -LiteralPath (Join-Path $providerEvidenceRoot 'sui-native-balance-current.trx')
        if($report.LastWriteTimeUtc -lt $started){throw 'Sui balance report is stale.'}
        [xml]$evidence=Get-Content -LiteralPath $report.FullName -Raw
        $counts=$evidence.TestRun.ResultSummary.Counters
        if([int]$counts.total -ne 2 -or [int]$counts.passed -ne 2 -or [int]$counts.notExecuted -ne 0){throw 'Both retained native balance cases must execute and pass.'}
    }elseif($Mode -eq 'Fee'){
        $reportPath=Join-Path $providerEvidenceRoot 'sui-prepared-ledger-current.trx'
        [xml]$report=[IO.File]::ReadAllText($reportPath)
        if($report.SelectSingleNode("//*[local-name()='ResultSummary']").outcome -ne 'Completed'){throw 'Prepared evidence run is not completed.'}
        $output=$report.SelectSingleNode("//*[local-name()='UnitTestResult'][@outcome='Passed']//*[local-name()='StdOut']")
        $match=[regex]::Match($output.InnerText,'Confirmed original prepared write=([1-9A-HJ-NP-Za-km-z]+);')
        if(!$match.Success){throw 'No retained original prepared digest; do not create another transaction.'}
        $env:OASIS_SUI_SUCCESSFUL_FEE_RECEIPT=$match.Groups[1].Value
        $env:OASIS_SUI_SDK_BRIDGE=Join-Path $taskRoot 'Providers/Blockchain/NextGenSoftware.OASIS.API.Providers.SuiOASIS/SdkBridge/bridge.mjs'
        & dotnet test $project --no-build --no-restore --filter 'FullyQualifiedName~ExistingOriginalFeeReceiptBindsActualPayerAndExactComponents' --logger 'trx;LogFileName=sui-original-fee-current.trx' --results-directory (Join-Path $providerEvidenceRoot '.') -v:q
        if($LASTEXITCODE -ne 0){throw 'Original Sui fee verification failed; no transaction was created.'}
    }elseif($Mode -in @('InspectPrepared','ArchivePrepared')){
        $env:OASIS_SUI_SDK_BRIDGE=Join-Path $taskRoot 'Providers/Blockchain/NextGenSoftware.OASIS.API.Providers.SuiOASIS/SdkBridge/bridge.mjs'
        & (Join-Path $PSScriptRoot 'Run-ReusableUsageRecoveryTests.ps1') -SuiPreparedInspect -ArchiveSuiPrepared:($Mode -eq 'ArchivePrepared')
    }elseif($Mode -eq 'History'){
        $env:OASIS_SUI_SDK_BRIDGE=Join-Path $taskRoot 'Providers/Blockchain/NextGenSoftware.OASIS.API.Providers.SuiOASIS/SdkBridge/bridge.mjs'
        $env:OASIS_SUI_TEST_ESCROW_CACHE=Join-Path $providerEvidenceRoot 'sui-escrow-deployment.json'
        & (Join-Path $PSScriptRoot 'Run-ReusableUsageRecoveryTests.ps1') -SuiPreparedLedger -SuiCSharpHistory
        if($LASTEXITCODE -ne 0){throw 'Retained read-only history verification failed; no native replay allowed.'}
    }elseif($Mode -in @('Prepared','CSharpNFT','CSharpAssets','CSharpEscrow')){
        $env:OASIS_SUI_SDK_BRIDGE=Join-Path $taskRoot 'Providers/Blockchain/NextGenSoftware.OASIS.API.Providers.SuiOASIS/SdkBridge/bridge.mjs'
        $env:OASIS_SUI_TEST_ESCROW_CACHE=Join-Path $providerEvidenceRoot 'sui-escrow-deployment.json'
        & (Join-Path $PSScriptRoot 'Run-ReusableUsageRecoveryTests.ps1') -SuiPreparedLedger -SuiCSharpNft:($Mode -in @('CSharpNFT','CSharpAssets')) -SuiCSharpAssets:($Mode -eq 'CSharpAssets') -SuiCSharpEscrow:($Mode -eq 'CSharpEscrow')
    }else{
        $method = if ($Mode -eq 'ArchiveReplication') {'ArchiveExactSearchValidationFixtureAfterReadback'} elseif ($Mode -eq 'ArchiveWalletSearch') {'ArchiveExactWalletSearchFixtureAfterReadback'} elseif ($Mode -eq 'Healthy') { 'V2ReplicatesToSuiAndSelectsFromMeasuredRealProviderReads' } else { 'V2FailsOverToRealStoreAfterSuiFailure' }
        $priorReport=Join-Path $fixtureRoot "sui-retained-v2-$($Mode.ToLowerInvariant())-current.trx"
        if(Test-Path -LiteralPath $priorReport){
            if((Get-Item -LiteralPath $priorReport).Length -gt 1MB){throw 'Review oversized prior Sui evidence before copying.'}
            $priorHash=(Get-FileHash -LiteralPath $priorReport -Algorithm SHA256).Hash
            $archive=Join-Path $fixtureRoot "sui-routing-proof-$priorHash.trx"
            if(!(Test-Path -LiteralPath $archive)){[IO.File]::Copy($priorReport,$archive,$false)}
            if((Get-FileHash -LiteralPath $archive -Algorithm SHA256).Hash -ne $priorHash){throw 'Prior Sui proof archive mismatch; refusing overwrite.'}
        }
        $testStarted=[DateTime]::UtcNow
        & dotnet test $project --no-build --no-restore --filter "FullyQualifiedName~IntegrationTests.SuiRealProviderRoutingTests.$method" --logger "trx;LogFileName=sui-retained-v2-$($Mode.ToLowerInvariant())-current.trx" --results-directory $fixtureRoot -v:q
        if ($LASTEXITCODE -ne 0) { throw 'Current real Sui V2 verification failed; preserve ownership journal.' }
        $report=Get-Item -LiteralPath (Join-Path $fixtureRoot "sui-retained-v2-$($Mode.ToLowerInvariant())-current.trx")
        if($report.LastWriteTimeUtc -lt $testStarted){throw 'Sui native routing report is stale.'}
        [xml]$evidence=Get-Content -LiteralPath $report.FullName -Raw
        $counts=$evidence.TestRun.ResultSummary.Counters
        $expectedCount=if($Mode -eq 'Outage'){2}else{1}
        if([int]$counts.total -ne $expectedCount -or [int]$counts.passed -ne $expectedCount -or [int]$counts.notExecuted -ne 0){throw 'Every selected Sui native routing test must actually execute and pass.'}
    }
} finally {
    foreach ($name in $names) { [Environment]::SetEnvironmentVariable($name,$prior[$name],'Process') }
    $settings = $null
    if ($ownedPid) {
        & wsl -d Ubuntu -u root -- test -e "/proc/$ownedPid/exe"
        if ($LASTEXITCODE -eq 0) {
            & wsl -d Ubuntu -u root -- bash $linuxStopScript $ownedPid
            if ($LASTEXITCODE -ne 0) { throw 'Owned Sui runtime did not stop; inspect before another run.' }
        }
    }
    if ($launcher -and !$launcher.HasExited -and !$launcher.WaitForExit(10000)) { throw 'Owned WSL launcher has not exited; do not restart.' }
}
