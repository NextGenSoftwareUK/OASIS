#Requires -Version 7.0
$ErrorActionPreference='Stop'
$taskRoot=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
$work=Join-Path $taskRoot 'TestResults/EthereumCurrent'
New-Item -ItemType Directory -Force -Path $work | Out-Null
$lease=[IO.File]::Open((Join-Path $work 'environment.lock'),[IO.FileMode]::OpenOrCreate,[IO.FileAccess]::ReadWrite,[IO.FileShare]::None)
$owned=$null
$names=@('LOCAL_CHAIN_ID','ETHEREUMOASIS_TEST_RPC','ETHEREUMOASIS_TEST_PRIVATE_KEY','ETHEREUMOASIS_TEST_CHAIN_ID','ETHEREUMOASIS_TEST_FIXTURE_JSON','OASIS_ETHEREUM_STORAGE_ENCRYPTION_KEY')
$previous=@{}
foreach($name in $names){$previous[$name]=[Environment]::GetEnvironmentVariable($name)}
try {
    $freeBefore=(Get-PSDrive C).Free
    $node=Join-Path $env:USERPROFILE '.cache/codex-runtimes/codex-primary-runtime/dependencies/node/bin/node.exe'
    $runtime=Join-Path $taskRoot 'Providers/Blockchain/NextGenSoftware.OASIS.API.Providers.Web3CoreOASIS'
    $cli=Join-Path $runtime 'node_modules/hardhat/internal/cli/cli.js'
    if(!(Test-Path -LiteralPath $node) -or !(Test-Path -LiteralPath $cli)){throw 'Existing Node/Hardhat runtime required; no download attempted.'}
    if(Get-NetTCPConnection -State Listen -LocalPort 18545 -ErrorAction SilentlyContinue){throw 'Owned EVM port already active; refusing duplicate service.'}
    if(Get-NetTCPConnection -State Listen -LocalPort 18546 -ErrorAction SilentlyContinue){throw 'Owned simulated-outage port is occupied; refusing unrelated endpoint.'}
    & $node (Join-Path $PSScriptRoot 'compile-ethereum-storage-fixture.cjs')
    if($LASTEXITCODE){throw 'Cached Solidity fixture build failed.'}
    $project=Join-Path $taskRoot 'Providers/Blockchain/TestProjects/NextGenSoftware.OASIS.API.Providers.EthereumOASIS.IntegrationTests/NextGenSoftware.OASIS.API.Providers.EthereumOASIS.IntegrationTests.csproj'
    & dotnet build $project --no-restore /p:BuildProjectReferences=false /m:2 /nr:false /p:UseSharedCompilation=false -v:q
    if($LASTEXITCODE){throw 'Consumer build failed.'}
    $env:LOCAL_CHAIN_ID='31337'
    $owned=Start-Process -FilePath $node -ArgumentList @($cli,'node','--hostname','127.0.0.1','--port','18545','--config',(Join-Path $runtime 'hardhat.config.cjs')) -WorkingDirectory $runtime -WindowStyle Hidden -PassThru -RedirectStandardOutput (Join-Path $work 'evm-current.log') -RedirectStandardError (Join-Path $work 'evm-current.err')
    $deadline=[DateTime]::UtcNow.AddSeconds(20)
    $ready=$false
    do {
        if($owned.HasExited){throw 'Owned EVM exited at startup.'}
        try {$reply=Invoke-RestMethod -Uri 'http://127.0.0.1:18545' -Method Post -ContentType 'application/json' -Body '{"jsonrpc":"2.0","id":1,"method":"eth_chainId","params":[]}' -TimeoutSec 2; $ready=$reply.result -eq '0x7a69'} catch {$ready=$false}
        if(!$ready){Start-Sleep -Milliseconds 200}
    } while(!$ready -and [DateTime]::UtcNow -lt $deadline)
    if(!$ready){throw 'Local EVM did not expose expected chain31337.'}
    $env:ETHEREUMOASIS_TEST_RPC='http://127.0.0.1:18545'
    $env:ETHEREUMOASIS_TEST_CHAIN_ID='31337'
    # Public Hardhat deterministic DEVELOPMENT key. Never use/fund on a public chain.
    $env:ETHEREUMOASIS_TEST_PRIVATE_KEY='0xac0974bec39a17e36ba4a6b4d238ff944bacb478cbed5efcae784d7bf4f2ff80'
    $env:ETHEREUMOASIS_TEST_FIXTURE_JSON=Join-Path $work 'reject-second-holon.json'
    $keyPath=Join-Path $work 'storage-key.dpapi'
    if(!(Test-Path -LiteralPath $keyPath)){
        $key=[Security.Cryptography.RandomNumberGenerator]::GetBytes(32)
        try { [IO.File]::WriteAllBytes($keyPath,[Security.Cryptography.ProtectedData]::Protect($key,$null,[Security.Cryptography.DataProtectionScope]::CurrentUser)) }
        finally { [Security.Cryptography.CryptographicOperations]::ZeroMemory($key) }
    }
    $key=[Security.Cryptography.ProtectedData]::Unprotect([IO.File]::ReadAllBytes($keyPath),$null,[Security.Cryptography.DataProtectionScope]::CurrentUser)
    try {
        if($key.Length -ne 32){throw 'Retained storage key is invalid; refusing replacement.'}
        $env:OASIS_ETHEREUM_STORAGE_ENCRYPTION_KEY=[Convert]::ToBase64String($key)
    } finally { [Security.Cryptography.CryptographicOperations]::ZeroMemory($key) }
    $started=[DateTime]::UtcNow
    & dotnet test $project --no-build --no-restore /m:2 /nr:false --filter FullyQualifiedName~EthereumOASISIntegrationTests --logger 'trx;LogFileName=ethereum-native-current.trx' --results-directory $work -v:q
    if($LASTEXITCODE){throw 'Actual local Ethereum verification failed.'}
    $report=Get-Item -LiteralPath (Join-Path $work 'ethereum-native-current.trx')
    if($report.LastWriteTimeUtc -lt $started){throw 'Native report is stale.'}
    [xml]$trx=Get-Content -LiteralPath $report.FullName
    $counts=$trx.TestRun.ResultSummary.Counters
    if([int]$counts.total -ne 54 -or [int]$counts.passed -ne 54 -or [int]$counts.notExecuted -ne 0){throw 'Expected fifty-four native passing cases, no skips.'}
    Write-Output "Free bytes before=$freeBefore after=$((Get-PSDrive C).Free)"
} finally {
    foreach($name in $names){[Environment]::SetEnvironmentVariable($name,$previous[$name])}
    if($owned -and !$owned.HasExited){Stop-Process -Id $owned.Id; $owned.WaitForExit(5000) | Out-Null}
    $lease.Dispose()
}
