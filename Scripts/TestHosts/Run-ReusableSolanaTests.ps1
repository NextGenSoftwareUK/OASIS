#Requires -Version 7.0
param([switch]$Diagnostics,[switch]$InspectRecent,[ValidateRange(2,120)][int]$ObservationSeconds=2)
$ErrorActionPreference='Stop'
if(!$InspectRecent -and $ObservationSeconds -ne 2){throw 'ObservationSeconds is only supported for keyless InspectRecent mode.'}
$taskRoot=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
$work=Join-Path $taskRoot 'TestResults/SolanaCurrent'
New-Item -ItemType Directory -Force -Path $work | Out-Null
$lease=[IO.File]::Open((Join-Path $work 'environment.lock'),[IO.FileMode]::OpenOrCreate,[IO.FileAccess]::ReadWrite,[IO.FileShare]::None)
$owned=$null
$nativeId=$null
$secret=$null
$names=@('SOLANA_RPC_URL','SOLANA_PRIVATE_KEY','SOLANA_PUBLIC_KEY','OASIS_SOLANA_STORAGE_ENCRYPTION_KEY','OASIS_SOLANA_NATIVE_EVIDENCE')
$previous=@{}
foreach($name in $names){$previous[$name]=[Environment]::GetEnvironmentVariable($name)}
try {
    if(!$InspectRecent -and (Test-Path -LiteralPath (Join-Path $work 'sdk-concurrent.pending.json'))){throw 'Inspect original concurrent Solana submissions; do not replay or reset.'}
    if(!$InspectRecent -and (Test-Path -LiteralPath (Join-Path $work 'solana-validator-current.trx'))){throw 'Preserve and hash-verify the existing native report before another run.'}
    $freeBefore=(Get-PSDrive C).Free
    $prerequisites='test -x /var/tmp/oasis-runtimes/agave-4.3.0/solana-test-validator && test -f /var/tmp/oasis-solana/ledger/genesis.bin'
    if(!$InspectRecent){$prerequisites+=' && test -f /var/tmp/oasis-solana/fixture-keypair.json && test -f /var/tmp/oasis-solana/storage-key'}
    & wsl -d Ubuntu -- bash -lc $prerequisites
    if($LASTEXITCODE){throw 'Retained runtime, ledger and keys are required; no download/reset/generation attempted.'}
    $version=(& wsl -d Ubuntu -- /var/tmp/oasis-runtimes/agave-4.3.0/solana-test-validator --version).Trim()
    if($version -ne 'solana-test-validator 4.3.0 (src:825efd18; feat:c9ad34d2, client:Agave)'){throw 'Retained runtime version differs.'}
    & (Join-Path $PSScriptRoot 'Assert-ReusableSolanaReplayBridge.ps1')
    & wsl -d Ubuntu -- bash -lc 'test -z "$(pgrep -x solana-test-val)" && test -z "$(ss -ltnH sport = :18899)"'
    if($LASTEXITCODE){throw 'A validator or owned port is already active; refusing duplicate environment.'}
    $project=Join-Path $taskRoot 'Providers/Blockchain/TestProjects/NextGenSoftware.OASIS.API.Providers.SOLANAOASIS.IntegrationTests/NextGenSoftware.OASIS.API.Providers.SOLANAOASIS.IntegrationTests.csproj'
    if(!$InspectRecent){
        & dotnet build $project --no-restore /p:BuildProjectReferences=false /m:2 /nr:false /p:UseSharedCompilation=false -v:q
        if($LASTEXITCODE){throw 'Consumer build failed; no validator started.'}
    }
    # Do not independently purge shreds: retained snapshots/tower may still require them for replay.
    # Bound runs and monitor disk; prune only after independently verifying a sufficient durable snapshot.
    $owned=Start-Process -FilePath wsl.exe -ArgumentList @('-d','Ubuntu','--','/usr/bin/taskset','-c','0-3','/var/tmp/oasis-runtimes/agave-4.3.0/solana-test-validator','--ledger','/var/tmp/oasis-solana/ledger','--bind-address','127.0.0.1','--rpc-port','18899','--faucet-port','18900','--dynamic-port-range','18901-18927','--rpc-pubsub-worker-threads','1','--rpc-pubsub-notification-threads','1','--quiet') -WindowStyle Hidden -PassThru -RedirectStandardOutput (Join-Path $work 'validator-current.log') -RedirectStandardError (Join-Path $work 'validator-current.err')
    $deadline=[DateTime]::UtcNow.AddSeconds(30)
    $ready=$false
    do {
        if($owned.HasExited){throw 'Owned validator exited during startup.'}
        $ids=@(& wsl -d Ubuntu -- pgrep -x solana-test-val | Where-Object { $_ -match '^\d+$' })
        if($ids.Count -eq 1){$nativeId=[int]$ids[0]}
        try { $health=Invoke-RestMethod -Uri 'http://127.0.0.1:18899' -Method Post -ContentType 'application/json' -Body '{"jsonrpc":"2.0","id":1,"method":"getHealth"}' -TimeoutSec 2; $ready=$health.result -eq 'ok' } catch { $ready=$false }
        if(!$ready -or !$nativeId){Start-Sleep -Milliseconds 250}
    } while((!$ready -or !$nativeId) -and [DateTime]::UtcNow -lt $deadline)
    if(!$ready -or !$nativeId){throw 'Validator did not become healthy; original process will not be restarted.'}
    $genesis=Invoke-RestMethod -Uri 'http://127.0.0.1:18899' -Method Post -ContentType 'application/json' -Body '{"jsonrpc":"2.0","id":1,"method":"getGenesisHash"}' -TimeoutSec 3
    # Approved one-time recovery: original ledger/uncertain outcomes remain cold and unresolved.
    # New identity independently observed with finalized0->101 and offline snapshot100/root102 admission.
    if($genesis.result -ne 'F22uw23o1HuahQiQVfd7Lfi6Q51fv4KNoYAvDnDDpEvj'){throw 'Retained ledger genesis differs; refusing writes.'}
    $slotBody='{"jsonrpc":"2.0","id":1,"method":"getSlot","params":[{"commitment":"finalized"}]}'
    $initialRoot=Invoke-RestMethod -Uri 'http://127.0.0.1:18899' -Method Post -ContentType 'application/json' -Body $slotBody -TimeoutSec 3
    if($null -eq $initialRoot.result -or $initialRoot.error){throw 'Finalized slot lookup failed; refusing writes.'}
    if(!$InspectRecent){
        $rootAdvanced=$false
        for($observation=0;$observation -lt 20;$observation++){
            Start-Sleep -Milliseconds 500
            $currentRoot=Invoke-RestMethod -Uri 'http://127.0.0.1:18899' -Method Post -ContentType 'application/json' -Body $slotBody -TimeoutSec 3
            if($null -eq $currentRoot.result -or $currentRoot.error){throw 'Finalized slot observation failed; refusing writes.'}
            if([long]$currentRoot.result -gt [long]$initialRoot.result){$rootAdvanced=$true;break}
        }
        if(!$rootAdvanced){throw "Validator finalized root did not advance from $($initialRoot.result); no test transaction submitted. Inspect retained consensus state without resetting it."}
    }
    if($InspectRecent){
        # Public fixture address verified against the retained genesis/key identity.
        # Inspection never needs client signing/encryption keys or a .NET build.
        $publicAddress='Fi4AJCtDCehEVc7CiQVcHC9t57BmKk3B6gtMjWqA4Ffr'
        $body=@{jsonrpc='2.0';id=1;method='getSignaturesForAddress';params=@($publicAddress,@{limit=5;commitment='finalized'})} | ConvertTo-Json -Depth 5 -Compress
        $recent=Invoke-RestMethod -Uri 'http://127.0.0.1:18899' -Method Post -ContentType 'application/json' -Body $body -TimeoutSec 5
        if($recent.error){throw 'Read-only native signature inspection failed.'}
        if($null -eq $recent.result){throw 'Native signature inspection omitted its result.'}
        $slots=@{}
        foreach($commitment in 'processed','confirmed','finalized'){
            $slotRequest=@{jsonrpc='2.0';id=1;method='getSlot';params=@(@{commitment=$commitment})} | ConvertTo-Json -Depth 4 -Compress
            $slotReply=Invoke-RestMethod -Uri 'http://127.0.0.1:18899' -Method Post -ContentType 'application/json' -Body $slotRequest -TimeoutSec 3
            if($slotReply.error -or $null -eq $slotReply.result){throw "Read-only $commitment slot inspection failed."}
            $slots[$commitment]=$slotReply.result
        }
        $votes=Invoke-RestMethod -Uri 'http://127.0.0.1:18899' -Method Post -ContentType 'application/json' -Body '{"jsonrpc":"2.0","id":1,"method":"getVoteAccounts"}' -TimeoutSec 3
        if($votes.error -or $null -eq $votes.result){throw 'Read-only vote account inspection failed.'}
        # Observe the same live validator, without client keys, submissions, warp or reset.
        $observationDeadline=[DateTime]::UtcNow.AddSeconds($ObservationSeconds)
        $observedRoots=[Collections.Generic.List[long]]::new()
        do {
            Start-Sleep -Seconds 2
            if($owned.HasExited){throw 'Owned validator exited during read-only consensus observation.'}
            $rootAfter=Invoke-RestMethod -Uri 'http://127.0.0.1:18899' -Method Post -ContentType 'application/json' -Body $slotBody -TimeoutSec 3
            if($rootAfter.error -or $null -eq $rootAfter.result){throw 'Read-only final root observation failed.'}
            $observedRoots.Add([long]$rootAfter.result)
        } while([DateTime]::UtcNow -lt $observationDeadline)
        @{Genesis=$genesis.result;FinalizedSlot=$initialRoot.result;FinalizedAfterObservation=$rootAfter.result;Slots=$slots;
            CurrentVotes=@($votes.result.current | Select-Object votePubkey,nodePubkey,activatedStake,lastVote,rootSlot);
            DelinquentVotes=@($votes.result.delinquent | Select-Object votePubkey,nodePubkey,activatedStake,lastVote,rootSlot);
            ObservationSeconds=$ObservationSeconds;ObservedFinalizedRoots=@($observedRoots);
            Count=@($recent.result).Count;Transactions=@($recent.result | Select-Object signature,slot,confirmationStatus,err)} | ConvertTo-Json -Depth 6 -Compress | Write-Output
        return
    }
    Add-Type -Path (Join-Path (Split-Path $project) 'bin/Debug/net10.0/Solnet.Wallet.dll')
    $secret=[byte[]](Get-Content -LiteralPath '\\wsl.localhost\Ubuntu\var\tmp\oasis-solana\fixture-keypair.json' -Raw | ConvertFrom-Json)
    $env:SOLANA_PRIVATE_KEY=[Solnet.Wallet.Utilities.Encoders]::Base58.EncodeData($secret)
    $env:SOLANA_PUBLIC_KEY=[Solnet.Wallet.Utilities.Encoders]::Base58.EncodeData([byte[]]$secret[32..63])
    $env:SOLANA_RPC_URL='http://127.0.0.1:18899'
    $env:OASIS_SOLANA_NATIVE_EVIDENCE=$work
    $env:OASIS_SOLANA_STORAGE_ENCRYPTION_KEY=(Get-Content -LiteralPath '\\wsl.localhost\Ubuntu\var\tmp\oasis-solana\storage-key' -Raw).Trim()
    $testStarted=[DateTime]::UtcNow
    $testArguments=@('test',$project,'--no-build','--no-restore','/m:2','/nr:false','--logger','trx;LogFileName=solana-validator-current.trx','--results-directory',$work,'-v:q')
    if($Diagnostics){$testArguments+=@('--diag',(Join-Path $work 'validator-test-current.diag.log'))}
    & dotnet @testArguments
    if($LASTEXITCODE){throw 'Real Solana integration failed; inspect fixed report.'}
    $report=Get-Item -LiteralPath (Join-Path $work 'solana-validator-current.trx')
    if($report.LastWriteTimeUtc -lt $testStarted){throw 'Integration report is stale.'}
    [xml]$trx=Get-Content -LiteralPath $report.FullName
    $counts=$trx.TestRun.ResultSummary.Counters
    if([int]$counts.total -ne 1 -or [int]$counts.passed -ne 1 -or [int]$counts.notExecuted -ne 0){throw 'Expected exactly one passing real test and no skips.'}
    Write-Output "Free bytes before=$freeBefore after=$((Get-PSDrive C).Free)"
} finally {
    if($secret){[Security.Cryptography.CryptographicOperations]::ZeroMemory($secret)}
    foreach($name in $names){[Environment]::SetEnvironmentVariable($name,$previous[$name])}
    if($owned -and !$owned.HasExited -and !$nativeId){
        $ids=@(& wsl -d Ubuntu -- pgrep -x solana-test-val | Where-Object { $_ -match '^\d+$' })
        if($ids.Count -eq 1){$nativeId=[int]$ids[0]}
    }
    if($owned -and !$owned.HasExited -and $nativeId){
        & wsl -d Ubuntu -- bash -lc "test `"`$(readlink -f /proc/$nativeId/exe)`" = /var/tmp/oasis-runtimes/agave-4.3.0/solana-test-validator && grep -zq /var/tmp/oasis-solana/ledger /proc/$nativeId/cmdline && kill -INT $nativeId"
        if($LASTEXITCODE){Write-Warning 'Owned native shutdown check failed; no unrelated process stopped.'}
        elseif(!$owned.WaitForExit(10000)){Write-Warning 'Owned validator has not exited; do not restart or remove its ledger.'}
    }
    $lease.Dispose()
}
