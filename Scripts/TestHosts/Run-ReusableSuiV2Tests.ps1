param([ValidateSet('Healthy','Outage','Prepared','Fee','NFT','Token','Assets','InspectPrepared','ArchivePrepared','ArchiveReplication','CSharpNFT','CSharpAssets')][string]$Mode = 'Healthy')
$ErrorActionPreference = 'Stop'
$taskRoot = [IO.Path]::GetFullPath((Split-Path (Split-Path $PSScriptRoot -Parent) -Parent))
$fixtureRoot = Join-Path $taskRoot 'TestResults/HyperDriveSui'
$deploymentPath = '\\wsl.localhost\Ubuntu\var\tmp\oasis-sui\deployment.json'
if (!(Test-Path -LiteralPath $deploymentPath)) { throw 'Retained protected Sui deployment is unavailable; no copy or new wallet created.' }
if ($Mode -ne 'ArchiveReplication' -and (Test-Path -LiteralPath (Join-Path $fixtureRoot 'encrypted-replication-pending.json'))) { throw 'Pending Sui ownership intent requires reconciliation before another run.' }
if ($Mode -notin @('InspectPrepared','ArchivePrepared') -and (Test-Path -LiteralPath (Join-Path $taskRoot 'TestResults/SuiCurrent/prepared-live-pending.json'))) { throw 'Pending prepared Sui ownership requires reconciliation before another run.' }
if (Test-Path -LiteralPath (Join-Path $taskRoot 'TestResults/SuiCurrent/nft-live-pending.jsonl')) { throw 'Pending NFT ownership requires reconciliation before another run.' }
if (Test-Path -LiteralPath (Join-Path $taskRoot 'TestResults/SuiCurrent/token-live-pending.jsonl')) { throw 'Pending token ownership requires reconciliation before another run.' }
if (Test-Path -LiteralPath (Join-Path $taskRoot 'TestResults/SuiCurrent/csharp-nft-live-pending.jsonl')) { throw 'Pending CSharp NFT ownership requires original-attempt reconciliation before another run.' }
if (Test-Path -LiteralPath (Join-Path $taskRoot 'TestResults/SuiCurrent/csharp-token-live-pending.jsonl')) { throw 'Pending CSharp token ownership requires original-attempt reconciliation before another run.' }
& wsl -d Ubuntu -u root -- test -f /var/tmp/oasis-sui/network/network.yaml
if ($LASTEXITCODE -ne 0) { throw 'Existing Sui ledger configuration missing; refusing genesis creation.' }
& wsl -d Ubuntu -u root -- pgrep -x sui *> $null
if ($LASTEXITCODE -eq 0) { throw 'Existing Sui runtime is in use; refusing to take ownership.' }
$project = Join-Path $taskRoot 'OASIS Architecture/NextGenSoftware.OASIS.API.Core.HyperDrive.IntegrationTests/NextGenSoftware.OASIS.API.Core.HyperDrive.IntegrationTests.csproj'
if($Mode -in @('Prepared','Fee','InspectPrepared','ArchivePrepared','CSharpNFT','CSharpAssets')){$project=Join-Path $taskRoot 'Providers/Blockchain/TestProjects/NextGenSoftware.OASIS.API.Providers.SuiOASIS.IntegrationTests/NextGenSoftware.OASIS.API.Providers.SuiOASIS.IntegrationTests.csproj'}
& dotnet build $project --no-restore /p:BuildProjectReferences=false /m:2 /nr:false /p:UseSharedCompilation=false -v:q -clp:ErrorsOnly
if ($LASTEXITCODE -ne 0) { throw 'Current V2 integration consumer build failed.' }
$outputBridge=if($Mode -in @('Prepared','Fee','InspectPrepared','ArchivePrepared','CSharpNFT','CSharpAssets')){'Providers/Blockchain/TestProjects/NextGenSoftware.OASIS.API.Providers.SuiOASIS.IntegrationTests/bin/Debug/net10.0/SuiSdkBridge'}else{'OASIS Architecture/NextGenSoftware.OASIS.API.Core.HyperDrive.IntegrationTests/bin/Debug/net10.0/SuiSdkBridge'}
& (Join-Path $PSScriptRoot 'Use-ReusableHederaOutputDependencies.ps1') -Provider Sui -OutputBridge $outputBridge
$names = @('OASIS_SUI_TEST_RPC','OASIS_SUI_TEST_DEPLOYMENT','OASIS_SUI_TEST_OWNED_PID','OASIS_SUI_TEST_STOP_SCRIPT','OASIS_HYPERDRIVE_TEST_ROOT','OASIS_SUI_SDK_BRIDGE','OASIS_SUI_SUCCESSFUL_FEE_RECEIPT','OASIS_SUI_TEST_NFT_JOURNAL','OASIS_SUI_TEST_FAUCET','OASIS_SUI_TEST_TOKEN_JOURNAL')
$prior = @{}
foreach ($name in $names) { $prior[$name] = [Environment]::GetEnvironmentVariable($name,'Process') }
$ownedPid = $null
$launcher = $null
try {
    # Hidden launcher, retained executable/ledger, fixed compact startup logs.
    $launcher = Start-Process -FilePath 'wsl.exe' -ArgumentList @('-d','Ubuntu','-u','root','--','bash','/mnt/c/Source/OASIS-hyperdrive-v2-gaps/Scripts/TestHosts/start-sui-test-network.sh','--reuse-only') -WindowStyle Hidden -PassThru -RedirectStandardOutput (Join-Path $fixtureRoot 'node-current.stdout.log') -RedirectStandardError (Join-Path $fixtureRoot 'node-current.stderr.log')
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
    $settings = $null
    $env:OASIS_SUI_TEST_RPC = 'http://127.0.0.1:9000'
    $env:OASIS_SUI_TEST_DEPLOYMENT = $deploymentPath
    $env:OASIS_SUI_TEST_OWNED_PID = $ownedPid
    $env:OASIS_SUI_TEST_STOP_SCRIPT = '/mnt/c/Source/OASIS-hyperdrive-v2-gaps/Scripts/TestHosts/stop-owned-sui-test-node.sh'
    $env:OASIS_HYPERDRIVE_TEST_ROOT = $fixtureRoot
    if($Mode -eq 'Assets'){
        $env:OASIS_SUI_TEST_FAUCET='http://127.0.0.1:9123'
        $env:OASIS_SUI_TEST_TOKEN_JOURNAL=Join-Path $taskRoot 'TestResults/SuiCurrent/token-live-pending.jsonl'
        $env:OASIS_SUI_TEST_NFT_JOURNAL=Join-Path $taskRoot 'TestResults/SuiCurrent/nft-live-pending.jsonl'
        $bridgeRoot=Join-Path $taskRoot 'Providers/Blockchain/NextGenSoftware.OASIS.API.Providers.SuiOASIS/SdkBridge'
        & node --test --test-concurrency=1 --test-reporter=tap (Join-Path $bridgeRoot 'nft-runtime.test.mjs') (Join-Path $bridgeRoot 'token-runtime.test.mjs') 2>&1 | Tee-Object -FilePath (Join-Path $taskRoot 'TestResults/SuiCurrent/assets-live-current.tap')
        if($LASTEXITCODE -ne 0){throw 'Real asset fixtures failed; preserve ownership journals and do not replay.'}
    }elseif($Mode -eq 'Token'){
        $env:OASIS_SUI_TEST_FAUCET='http://127.0.0.1:9123'
        $env:OASIS_SUI_TEST_TOKEN_JOURNAL=Join-Path $taskRoot 'TestResults/SuiCurrent/token-live-pending.jsonl'
        & node --test --test-reporter=tap (Join-Path $taskRoot 'Providers/Blockchain/NextGenSoftware.OASIS.API.Providers.SuiOASIS/SdkBridge/token-runtime.test.mjs') 2>&1 | Tee-Object -FilePath (Join-Path $taskRoot 'TestResults/SuiCurrent/token-live-current.tap')
        if($LASTEXITCODE -ne 0){throw 'Real token fixture failed; preserve prior balances and any pending journal.'}
    }elseif($Mode -eq 'NFT'){
        $env:OASIS_SUI_TEST_FAUCET='http://127.0.0.1:9123'
        $env:OASIS_SUI_TEST_NFT_JOURNAL=Join-Path $taskRoot 'TestResults/SuiCurrent/nft-live-pending.jsonl'
        & node --test --test-reporter=tap (Join-Path $taskRoot 'Providers/Blockchain/NextGenSoftware.OASIS.API.Providers.SuiOASIS/SdkBridge/nft-runtime.test.mjs') 2>&1 | Tee-Object -FilePath (Join-Path $taskRoot 'TestResults/SuiCurrent/nft-live-current.tap')
        if($LASTEXITCODE -ne 0){throw 'Real NFT fixture failed; retain any pending journal and inspect original outcomes.'}
    }elseif($Mode -eq 'Fee'){
        $reportPath=Join-Path $taskRoot 'TestResults/SuiCurrent/sui-prepared-ledger-current.trx'
        [xml]$report=[IO.File]::ReadAllText($reportPath)
        if($report.SelectSingleNode("//*[local-name()='ResultSummary']").outcome -ne 'Completed'){throw 'Prepared evidence run is not completed.'}
        $output=$report.SelectSingleNode("//*[local-name()='UnitTestResult'][@outcome='Passed']//*[local-name()='StdOut']")
        $match=[regex]::Match($output.InnerText,'Confirmed original prepared write=([1-9A-HJ-NP-Za-km-z]+);')
        if(!$match.Success){throw 'No retained original prepared digest; do not create another transaction.'}
        $env:OASIS_SUI_SUCCESSFUL_FEE_RECEIPT=$match.Groups[1].Value
        $env:OASIS_SUI_SDK_BRIDGE=Join-Path $taskRoot 'Providers/Blockchain/NextGenSoftware.OASIS.API.Providers.SuiOASIS/SdkBridge/bridge.mjs'
        & dotnet test $project --no-build --no-restore --filter 'FullyQualifiedName~ExistingOriginalFeeReceiptBindsActualPayerAndExactComponents' --logger 'trx;LogFileName=sui-original-fee-current.trx' --results-directory (Join-Path $taskRoot 'TestResults/SuiCurrent') -v:q
        if($LASTEXITCODE -ne 0){throw 'Original Sui fee verification failed; no transaction was created.'}
    }elseif($Mode -in @('InspectPrepared','ArchivePrepared')){
        $env:OASIS_SUI_SDK_BRIDGE=Join-Path $taskRoot 'Providers/Blockchain/NextGenSoftware.OASIS.API.Providers.SuiOASIS/SdkBridge/bridge.mjs'
        & (Join-Path $PSScriptRoot 'Run-ReusableUsageRecoveryTests.ps1') -SuiPreparedInspect -ArchiveSuiPrepared:($Mode -eq 'ArchivePrepared')
    }elseif($Mode -in @('Prepared','CSharpNFT','CSharpAssets')){
        $env:OASIS_SUI_SDK_BRIDGE=Join-Path $taskRoot 'Providers/Blockchain/NextGenSoftware.OASIS.API.Providers.SuiOASIS/SdkBridge/bridge.mjs'
        & (Join-Path $PSScriptRoot 'Run-ReusableUsageRecoveryTests.ps1') -SuiPreparedLedger -SuiCSharpNft:($Mode -in @('CSharpNFT','CSharpAssets')) -SuiCSharpAssets:($Mode -eq 'CSharpAssets')
    }else{
        $method = if ($Mode -eq 'ArchiveReplication') {'ArchiveExactSearchValidationFixtureAfterReadback'} elseif ($Mode -eq 'Healthy') { 'V2ReplicatesToSuiAndSelectsFromMeasuredRealProviderReads' } else { 'V2FailsOverToRealStoreAfterSuiFailure' }
        & dotnet test $project --no-build --no-restore --filter "FullyQualifiedName~IntegrationTests.SuiRealProviderRoutingTests.$method" --logger "trx;LogFileName=sui-retained-v2-$($Mode.ToLowerInvariant())-current.trx" --results-directory $fixtureRoot -v:q
        if ($LASTEXITCODE -ne 0) { throw 'Current real Sui V2 verification failed; preserve ownership journal.' }
    }
} finally {
    foreach ($name in $names) { [Environment]::SetEnvironmentVariable($name,$prior[$name],'Process') }
    $settings = $null
    if ($ownedPid) {
        & wsl -d Ubuntu -u root -- test -e "/proc/$ownedPid/exe"
        if ($LASTEXITCODE -eq 0) {
            & wsl -d Ubuntu -u root -- bash /mnt/c/Source/OASIS-hyperdrive-v2-gaps/Scripts/TestHosts/stop-owned-sui-test-node.sh $ownedPid
            if ($LASTEXITCODE -ne 0) { throw 'Owned Sui runtime did not stop; inspect before another run.' }
        }
    }
    if ($launcher -and !$launcher.HasExited -and !$launcher.WaitForExit(10000)) { throw 'Owned WSL launcher has not exited; do not restart.' }
}
