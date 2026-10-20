#Requires -Version 7.0
$ErrorActionPreference='Stop'
$taskRoot=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
$work=Join-Path $taskRoot 'TestResults/EthereumCurrent'
$historyPending=Join-Path $taskRoot 'TestResults/Web3CoreCurrent/history-response-loss.pending.json'
$ethereumPending=Join-Path $work 'ethereum-accepted-loss.pending.json'
$keyDeletePending=Join-Path $work 'delete-key-native.pending.json'
$retainedRouting=Join-Path $taskRoot 'Providers/Blockchain/TestProjects/NextGenSoftware.OASIS.API.Providers.Web3CoreOASIS.UnitTests/bin/Debug/net10.0/TestResults/Web3CoreRouting/routing.db'
if(Test-Path -LiteralPath $historyPending){throw 'Pending original history fault requires reconciliation; never restart/reset the local chain.'}
if(Test-Path -LiteralPath $ethereumPending){throw 'Pending original Ethereum fault requires reconciliation; never restart/reset the local chain.'}
if(Test-Path -LiteralPath $keyDeletePending){throw 'Pending original provider-key deletion requires reconciliation; never restart/reset the local chain.'}
if(Test-Path -LiteralPath $retainedRouting){throw 'Retained original Web3 routing fixture requires reviewed reconciliation; no native restart/reset or fresh suite attempted.'}
New-Item -ItemType Directory -Force -Path $work | Out-Null
$lease=[IO.File]::Open((Join-Path $work 'environment.lock'),[IO.FileMode]::OpenOrCreate,[IO.FileAccess]::ReadWrite,[IO.FileShare]::None)
$owned=$null
$nativeChecksStarted=$false
$nativeChecksVerified=$false
$names=@('LOCAL_CHAIN_ID','ETHEREUMOASIS_TEST_RPC','ETHEREUMOASIS_TEST_PRIVATE_KEY','ETHEREUMOASIS_TEST_CHAIN_ID','ETHEREUMOASIS_TEST_FIXTURE_JSON','OASIS_ETHEREUM_STORAGE_ENCRYPTION_KEY','OASIS_WEB3CORE_STORAGE_ENCRYPTION_KEY','WEB3CORE_NATIVE_FIXTURE')
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
    & $node (Join-Path $PSScriptRoot 'compile-web3core-storage.cjs')
    if($LASTEXITCODE){throw 'Cached Web3Core contract build failed.'}
    $project=Join-Path $taskRoot 'Providers/Blockchain/TestProjects/NextGenSoftware.OASIS.API.Providers.EthereumOASIS.IntegrationTests/NextGenSoftware.OASIS.API.Providers.EthereumOASIS.IntegrationTests.csproj'
    foreach($relative in @(
        'Providers/Shared/NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage/NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage.csproj',
        'Providers/Storage/NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS/NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS.csproj',
        'Providers/Blockchain/NextGenSoftware.OASIS.API.Providers.Web3CoreOASIS/NextGenSoftware.OASIS.API.Providers.Web3CoreOASIS.csproj',
        'Providers/Blockchain/NextGenSoftware.OASIS.API.Providers.PolygonOASIS/NextGenSoftware.OASIS.API.Providers.PolygonOASIS.csproj',
        'Providers/Blockchain/NextGenSoftware.OASIS.API.Providers.ArbitrumOASIS/NextGenSoftware.OASIS.API.Providers.ArbitrumOASIS.csproj',
        'Providers/Blockchain/NextGenSoftware.OASIS.API.Providers.EthereumOASIS/NextGenSoftware.OASIS.API.Providers.EthereumOASIS.csproj')) {
        & dotnet build (Join-Path $taskRoot $relative) --no-restore /p:BuildProjectReferences=false /m:2 /nr:false /p:UseSharedCompilation=false -v:q -clp:ErrorsOnly
        if($LASTEXITCODE){throw "Producer build failed: $relative"}
    }
    & dotnet build $project --no-restore /p:BuildProjectReferences=false /m:2 /nr:false /p:UseSharedCompilation=false -v:q -clp:ErrorsOnly
    if($LASTEXITCODE){throw 'Consumer build failed.'}
    $web3Project=Join-Path $taskRoot 'Providers/Blockchain/TestProjects/NextGenSoftware.OASIS.API.Providers.Web3CoreOASIS.UnitTests/NextGenSoftware.OASIS.API.Providers.Web3CoreOASIS.UnitTests.csproj'
    & dotnet build $web3Project --no-restore /p:BuildProjectReferences=false /m:2 /nr:false /p:UseSharedCompilation=false -v:q -clp:ErrorsOnly
    if($LASTEXITCODE){throw 'Web3Core consumer build failed.'}
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
    $env:OASIS_WEB3CORE_STORAGE_ENCRYPTION_KEY=$env:OASIS_ETHEREUM_STORAGE_ENCRYPTION_KEY
    $env:WEB3CORE_NATIVE_FIXTURE=Join-Path $taskRoot 'TestResults/Web3CoreCurrent/web3-storage.json'
    $nativeChecksStarted=$true
    & dotnet test $project --no-build --no-restore /m:2 /nr:false --filter 'FullyQualifiedName~EthereumOASISIntegrationTests|FullyQualifiedName~EthereumParentDepthControlledTests|FullyQualifiedName~EvmSearchOwnershipControlledTests|FullyQualifiedName~EthereumProfilePublicationControlledTests|FullyQualifiedName~EthereumGraphPlanControlledTests' --logger 'trx;LogFileName=ethereum-native-current.trx' --results-directory $work -v:q
    if($LASTEXITCODE){throw 'Actual local Ethereum verification failed.'}
    $report=Get-Item -LiteralPath (Join-Path $work 'ethereum-native-current.trx')
    if($report.LastWriteTimeUtc -lt $started){throw 'Native report is stale.'}
    [xml]$trx=Get-Content -LiteralPath $report.FullName
    $counts=$trx.TestRun.ResultSummary.Counters
    if([int]$counts.total -ne 274 -or [int]$counts.passed -ne 274 -or [int]$counts.notExecuted -ne 0 -or (Test-Path -LiteralPath $keyDeletePending)){throw 'Expected274passing cases (262previous plus12controlled graph ownership cases), no skips or pending original deletion.'}
    & $node (Join-Path $PSScriptRoot 'verify-web3core-pages.cjs')
    if($LASTEXITCODE){throw 'Web3Core native page verification failed.'}
    $web3Started=[DateTime]::UtcNow
    $diagnosticArgs=@()
    if($env:OASIS_WEB3_NATIVE_DIAGNOSTICS -eq '1'){
        $diagnosticArgs=@('--diag',(Join-Path $taskRoot 'TestResults/Web3CoreCurrent/native-diagnostic.log'))
    }
    & dotnet test $web3Project --no-build --no-restore /m:2 /nr:false --filter FullyQualifiedName~Web3NativeStorageTests --logger 'trx;LogFileName=web3-native-current.trx' --results-directory (Join-Path $taskRoot 'TestResults/Web3CoreCurrent') @diagnosticArgs -v:q
    if($LASTEXITCODE){throw 'Web3Core native C# storage verification failed.'}
    $web3Report=Get-Item -LiteralPath (Join-Path $taskRoot 'TestResults/Web3CoreCurrent/web3-native-current.trx')
    if($web3Report.LastWriteTimeUtc -lt $web3Started){throw 'Web3Core native report is stale.'}
    [xml]$web3Trx=Get-Content -LiteralPath $web3Report.FullName
    $web3Counts=$web3Trx.TestRun.ResultSummary.Counters
    if([int]$web3Counts.total -ne 5 -or [int]$web3Counts.passed -ne 5 -or [int]$web3Counts.notExecuted -ne 0){throw 'Expected five native Web3Core scenarios including V2 Arbitrum history, no skips.'}
    $nativeChecksVerified=$true
    Write-Output "Free bytes before=$freeBefore after=$((Get-PSDrive C).Free)"
} finally {
    foreach($name in $names){[Environment]::SetEnvironmentVariable($name,$previous[$name])}
    if($owned -and !$owned.HasExited){
        if(($nativeChecksStarted -and !$nativeChecksVerified) -or (Test-Path -LiteralPath $retainedRouting) -or (Test-Path -LiteralPath $historyPending) -or (Test-Path -LiteralPath $ethereumPending) -or (Test-Path -LiteralPath $keyDeletePending)){Write-Warning "Native acceptance incomplete or original fault evidence remains; keep owned local node PID $($owned.Id) live for reconciliation. Do not restart/reset. This ephemeral node is the original receipt/state authority."}
        else{Stop-Process -Id $owned.Id; $owned.WaitForExit(5000) | Out-Null}
    }
    $lease.Dispose()
}
