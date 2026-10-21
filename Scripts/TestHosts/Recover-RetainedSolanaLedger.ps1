#Requires -Version 7.0
# One approved recovery, not a per-run reset. Preserve uncertain original outcomes.
$ErrorActionPreference='Stop'
$taskRoot=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
$work=Join-Path $taskRoot 'TestResults/SolanaCurrent'
$lease=[IO.File]::Open((Join-Path $work 'environment.lock'),[IO.FileMode]::OpenOrCreate,[IO.FileAccess]::ReadWrite,[IO.FileShare]::None)
$owned=$null;$nativeId=$null;$record=$null
$receipt=Join-Path $work 'recovery-genesis.json'
try {
    if((Get-PSDrive C).Free -lt 10GB){throw 'Insufficient bounded recovery headroom.'}
    if(Test-Path -LiteralPath $receipt){throw 'Recovery receipt already exists; inspect it, never repeat recovery.'}
    $version=(& wsl.exe -d Ubuntu -- /var/tmp/oasis-runtimes/agave-4.3.0/solana-test-validator --version).Trim()
    if($version -ne 'solana-test-validator 4.3.0 (src:825efd18; feat:c9ad34d2, client:Agave)'){throw 'Retained pinned runtime required.'}
    & wsl.exe -d Ubuntu -- bash -lc 'test -z "$(pgrep -x solana-test-val)" && test -z "$(ss -ltnH sport = :18899)" && test -f /var/tmp/oasis-solana/fixture-keypair.json && test -f /var/tmp/oasis-solana/storage-key && test ! -L /var/tmp/oasis-solana/ledger && test "$(realpath /var/tmp/oasis-solana/ledger)" = /var/tmp/oasis-solana/ledger && test ! -e /var/tmp/oasis-solana/original-ledger-pre-recovery'
    if($LASTEXITCODE){throw 'Recovery ownership/process/path preflight failed.'}
    $original=(& wsl.exe -d Ubuntu -- sha256sum /var/tmp/oasis-solana/ledger/genesis.bin) -join ''
    if($LASTEXITCODE -or ($original -split '\s+')[0] -ne 'f59625dbf1bdbae790d4fcd2b3cd8fd336c557366f3e06d09966578400e12243'){throw 'Original genesis identity changed.'}
    $record=[ordered]@{Status='before-approved-rename';OriginalGenesisBinSha256='f59625dbf1bdbae790d4fcd2b3cd8fd336c557366f3e06d09966578400e12243';OriginalGenesis='HXfgJ8CoRQ7RcgETc178DX2FwbSQQK6nCCTNc5YwGRi2';ColdLedger='/var/tmp/oasis-solana/original-ledger-pre-recovery';OriginalOutcomes='unresolved; never replay';StartedUtc=[DateTime]::UtcNow.ToString('O')}
    [IO.File]::WriteAllText($receipt,($record|ConvertTo-Json))
    & wsl.exe -d Ubuntu -- mv -T -- /var/tmp/oasis-solana/ledger /var/tmp/oasis-solana/original-ledger-pre-recovery
    if($LASTEXITCODE){throw 'Original ledger rename failed; inspect receipt before continuing.'}
    $preserved=(& wsl.exe -d Ubuntu -- sha256sum /var/tmp/oasis-solana/original-ledger-pre-recovery/genesis.bin) -join ''
    if($LASTEXITCODE -or ($preserved -split '\s+')[0] -ne $record.OriginalGenesisBinSha256){throw 'Cold original genesis identity did not match after rename.'}
    $record.Status='original-preserved; new-ledger-starting'
    [IO.File]::WriteAllText($receipt,($record|ConvertTo-Json))
    $owned=Start-Process -FilePath wsl.exe -ArgumentList @('-d','Ubuntu','--','/usr/bin/taskset','-c','0-3','/var/tmp/oasis-runtimes/agave-4.3.0/solana-test-validator','--ledger','/var/tmp/oasis-solana/ledger','--mint','Fi4AJCtDCehEVc7CiQVcHC9t57BmKk3B6gtMjWqA4Ffr','--bind-address','127.0.0.1','--rpc-port','18899','--faucet-port','18900','--dynamic-port-range','18901-18927','--rpc-pubsub-worker-threads','1','--rpc-pubsub-notification-threads','1','--quiet') -WindowStyle Hidden -PassThru -RedirectStandardOutput (Join-Path $work 'recovery-validator.log') -RedirectStandardError (Join-Path $work 'recovery-validator.err')
    $deadline=[DateTime]::UtcNow.AddSeconds(120)
    $firstRoot=$null;$lastRoot=$null;$newGenesis=$null;$snapshot=$false
    do {
        if($owned.HasExited){throw 'Recovery validator exited; preserve both ledgers and inspect logs.'}
        $ids=@(& wsl.exe -d Ubuntu -- pgrep -x solana-test-val | Where-Object {$_ -match '^\d+$'})
        if($ids.Count -eq 1){$nativeId=[int]$ids[0]}
        try {
            $reply=Invoke-RestMethod 'http://127.0.0.1:18899' -Method Post -ContentType 'application/json' -Body '{"jsonrpc":"2.0","id":1,"method":"getSlot","params":[{"commitment":"finalized"}]}' -TimeoutSec 2
            if(!$reply.error -and $null -ne $reply.result){if($null -eq $firstRoot){$firstRoot=[long]$reply.result};$lastRoot=[long]$reply.result}
            $genesis=Invoke-RestMethod 'http://127.0.0.1:18899' -Method Post -ContentType 'application/json' -Body '{"jsonrpc":"2.0","id":1,"method":"getGenesisHash"}' -TimeoutSec 2
            if(!$genesis.error){$newGenesis=$genesis.result}
        } catch [System.Net.Http.HttpRequestException] { }
          catch [System.Threading.Tasks.TaskCanceledException] { }
        $snapshot=@(Get-ChildItem -LiteralPath '\\wsl.localhost\Ubuntu\var\tmp\oasis-solana\ledger' -Filter 'snapshot-*.tar.zst' -File -ErrorAction SilentlyContinue).Count -gt 0
        if($lastRoot -gt $firstRoot -and $snapshot -and $newGenesis){break}
        Start-Sleep -Milliseconds 500
    } while([DateTime]::UtcNow -lt $deadline)
    $record.NewGenesis=$newGenesis;$record.InitialFinalizedRoot=$firstRoot;$record.LastFinalizedRoot=$lastRoot;$record.HasSnapshot=$snapshot
    if(!$newGenesis -or $null -eq $firstRoot -or $lastRoot -le $firstRoot -or !$snapshot){$record.Status='incomplete; inspect same ledger';throw 'Recovery did not prove advancing finality plus retained snapshot; no provider transaction submitted.'}
    $record.Status='finality-advanced; replay admission still required'
} finally {
    if($owned -and !$owned.HasExited -and !$nativeId){
        $ids=@(& wsl.exe -d Ubuntu -- pgrep -x solana-test-val | Where-Object {$_ -match '^\d+$'})
        if($ids.Count -eq 1){$nativeId=[int]$ids[0]}
    }
    if($owned -and !$owned.HasExited -and $nativeId){
        & wsl.exe -d Ubuntu -- bash -lc "test `"`$(readlink -f /proc/$nativeId/exe)`" = /var/tmp/oasis-runtimes/agave-4.3.0/solana-test-validator && grep -zq /var/tmp/oasis-solana/ledger /proc/$nativeId/cmdline && kill -INT $nativeId"
        if($LASTEXITCODE -or !$owned.WaitForExit(10000)){Write-Warning 'Owned recovery shutdown not confirmed; do not restart.'}
    }
    elseif($owned -and !$owned.HasExited){Write-Warning 'Cannot identify owned validator safely; preserve receipt and inspect before restarting.'}
    if($record){$record.CompletedUtc=[DateTime]::UtcNow.ToString('O');[IO.File]::WriteAllText($receipt,($record|ConvertTo-Json))}
    $lease.Dispose()
}
