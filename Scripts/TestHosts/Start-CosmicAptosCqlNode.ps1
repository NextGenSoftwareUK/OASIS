#Requires -Version 7.5
# Resume the admitted source only. This command never provisions or resets a chain.
$ErrorActionPreference='Stop'
$root=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
$state=Join-Path $root 'LocalState/COSMIC/AptosCql'
$results=Join-Path $root 'TestResults/AptosCqlCurrent'
$cli=Join-Path ([Environment]::GetFolderPath('LocalApplicationData')) 'OASIS/provider-evidence/aptos-cli/9.6.0/aptos.exe'
$admission=Get-Content (Join-Path $state 'node-admission.json') -Raw|ConvertFrom-Json
if($admission.owner -ne 'COSMIC-CQL' -or $admission.cliVersion -ne '9.6.0' -or $admission.chainId -ne 4){throw 'Original node admission differs.'}
if((Get-FileHash $cli).Hash -ne $admission.cliSha256){throw 'Original pinned CLI differs.'}
foreach($relative in @('node/0/db','node/0/genesis.blob','node/0/node.yaml','node/waypoint.txt','environment.json','demo-seed.json','profile/.aptos/config.yaml')){
    if(!(Test-Path (Join-Path $state $relative))){throw "Original native source missing: $relative; never recreate it."}
}
foreach($relative in @('setup.pending.json','demo-seed.pending.json','mongo.pending.json')){
    if(Test-Path (Join-Path $state $relative)){throw "Unresolved original outcome: $relative; preserve and reconcile first."}
}
if(Get-NetTCPConnection -State Listen -LocalPort 18080,18081,18070 -ErrorAction SilentlyContinue){throw 'Native ports occupied; no process adoption.'}
[void][IO.Directory]::CreateDirectory($results)
$lease=[IO.File]::Open((Join-Path $state 'node.lock'),'OpenOrCreate','ReadWrite','None')
try {
    Write-Output 'Resuming original Aptos source. Ctrl-C stops this owned node; preserve its ledger.'
    # The retained account is already funded. Starting a faucet would create a
    # new faucet account during each resume, outside this read-only demo scope.
    & $cli node run-localnet --no-txn-stream --no-faucet --bind-to 127.0.0.1 --test-dir (Join-Path $state 'node') `
        --test-config-override (Join-Path $root 'Scripts/TestHosts/aptos-node-override.yaml') `
        --ready-server-listen-port 18070 *> (Join-Path $results 'node-current.log')
    if($LASTEXITCODE){throw 'Owned Aptos node exited; inspect retained ledger/log before any restart.'}
} finally {$lease.Dispose()}
