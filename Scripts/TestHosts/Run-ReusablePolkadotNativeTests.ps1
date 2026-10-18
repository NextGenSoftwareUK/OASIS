param([switch]$CheckOnly, [ValidateSet('Native','AssetSdk','AssetApi','AssetTransfer','History')][string]$Suite = 'Native')
$ErrorActionPreference = 'Stop'
$repo = Split-Path (Split-Path $PSScriptRoot -Parent) -Parent
$sdk = Join-Path $repo 'Providers/Blockchain/NextGenSoftware.OASIS.API.Providers.PolkadotOASIS/SdkBridge'
$evidence = Join-Path $repo 'TestResults/PolkadotCurrent'
$journal = Join-Path $evidence 'local-native-pending.json'
$bootstrap = Join-Path $evidence 'local-bootstrap-pending.json'
$assetBootstrap = Join-Path $evidence 'local-asset-bootstrap-pending.json'
$assetMutation = Join-Path $evidence 'local-asset-mutation-pending.json'
$assetApi = Join-Path $evidence 'local-asset-api-pending.json'
$assetTransfer = Join-Path $evidence 'local-asset-transfer-pending.json'
if (!(Test-Path -LiteralPath $evidence -PathType Container)) { throw 'Use the existing fixed evidence directory; no new environment is created.' }
if (!(Test-Path -LiteralPath (Join-Path $sdk 'node_modules/@polkadot/api/package.json'))) { throw 'Reuse the retained pinned SDK installation; this runner never installs dependencies.' }
$lease = [IO.FileStream]::new((Join-Path $evidence 'local-native-environment.lock'), [IO.FileMode]::OpenOrCreate, [IO.FileAccess]::ReadWrite, [IO.FileShare]::None)
$names = @('OASIS_POLKADOT_LOCAL_NATIVE','OASIS_POLKADOT_LOCAL_NATIVE_JOURNAL','OASIS_POLKADOT_LOCAL_BOOTSTRAP_JOURNAL','OASIS_POLKADOT_SDK_BRIDGE','OASIS_POLKADOT_LOCAL_ASSET_JOURNAL','OASIS_POLKADOT_LOCAL_ASSET_MUTATION_JOURNAL','OASIS_POLKADOT_LOCAL_ASSET_API_JOURNAL','OASIS_POLKADOT_LOCAL_ASSET_TRANSFER_JOURNAL')
$prior = @{}
foreach ($name in $names) { $prior[$name] = [Environment]::GetEnvironmentVariable($name) }
try {
    foreach ($pending in @($journal, $bootstrap, $assetBootstrap, $assetMutation, $assetApi, $assetTransfer)) {
        if (Test-Path -LiteralPath $pending) { throw "Pending original exists: $pending. Reconcile it read-only; never replay or delete it to rerun." }
    }
    $rpc = 'http://127.0.0.1:19944'
    $identity = Invoke-RestMethod $rpc -Method Post -ContentType 'application/json' -Body '{"jsonrpc":"2.0","id":1,"method":"chain_getBlockHash","params":[0]}' -TimeoutSec 10
    if ($identity.result -ne '0xe8cf5d7d636a08721efaa17acc52ed3d895fe2468dccb2520cc8e249ddb6b208') { throw 'Resume only the retained pinned local ledger; no reset, replacement or public network is allowed.' }
    $properties = Invoke-RestMethod $rpc -Method Post -ContentType 'application/json' -Body '{"jsonrpc":"2.0","id":2,"method":"system_properties","params":[]}' -TimeoutSec 10
    if ($properties.result.tokenDecimals -ne 12 -or $properties.result.tokenSymbol -ne 'UNIT') { throw 'Explicit local denomination differs; do not guess precision.' }
    if ($CheckOnly) { Write-Output 'Retained SDK, local genesis/properties, pending fences and exclusive lease verified; no build or submission.'; return }
    $env:OASIS_POLKADOT_LOCAL_NATIVE = '1'
    $env:OASIS_POLKADOT_LOCAL_NATIVE_JOURNAL = $journal
    $env:OASIS_POLKADOT_LOCAL_BOOTSTRAP_JOURNAL = $bootstrap
    $env:OASIS_POLKADOT_SDK_BRIDGE = Join-Path $sdk 'bridge.mjs'
    $env:OASIS_POLKADOT_LOCAL_ASSET_JOURNAL = $assetBootstrap
    $env:OASIS_POLKADOT_LOCAL_ASSET_MUTATION_JOURNAL = $assetMutation
    $env:OASIS_POLKADOT_LOCAL_ASSET_API_JOURNAL = $assetApi
    $env:OASIS_POLKADOT_LOCAL_ASSET_TRANSFER_JOURNAL = $assetTransfer
    Push-Location $sdk
    try {
        if ($Suite -ne 'History') {
            & node bootstrap-local-native.mjs; if ($LASTEXITCODE -ne 0) { throw 'Bootstrap failed; retain/reconcile any pending original.' }
        }
        if ($Suite -in @('AssetSdk','AssetApi','AssetTransfer')) {
            $scripts = @('bootstrap-local-asset.mjs','test-local-asset-preparation.mjs')
            if ($Suite -eq 'AssetSdk') { $scripts += 'test-local-asset-execution.mjs' }
            foreach ($script in $scripts) {
                & node $script
                if ($LASTEXITCODE -ne 0) { throw 'Asset SDK checks failed; retain/reconcile pending original, never replay.' }
            }
            if (Test-Path -LiteralPath $assetMutation) { throw 'Pending asset original remains; preserve it.' }
            if ($Suite -eq 'AssetSdk') { return }
        }
    }
    finally { Pop-Location }
    Push-Location $repo
    try {
        & dotnet build 'Providers/Blockchain/NextGenSoftware.OASIS.API.Providers.PolkadotOASIS/NextGenSoftware.OASIS.API.Providers.PolkadotOASIS.csproj' --no-restore /p:BuildProjectReferences=false /m:2 /nr:false /p:UseSharedCompilation=false --verbosity quiet
        if ($LASTEXITCODE -ne 0) { throw 'Current cached provider build failed; no dependency restore attempted.' }
        $testClass = switch ($Suite) { 'History' { 'PolkadotLocalHistoryTests' } 'AssetApi' { 'PolkadotLocalAssetTests' } 'AssetTransfer' { 'PolkadotLocalAssetTransferTests' } default { 'PolkadotLocalNativeTests' } }
        $reportName = switch ($Suite) { 'History' { 'polkadot-local-history-current.trx' } 'AssetApi' { 'polkadot-local-asset-api-current.trx' } 'AssetTransfer' { 'polkadot-local-asset-transfer-current.trx' } default { 'polkadot-local-native-current.trx' } }
        & dotnet test 'Providers/Blockchain/TestProjects/NextGenSoftware.OASIS.API.Providers.PolkadotOASIS.UnitTests/NextGenSoftware.OASIS.API.Providers.PolkadotOASIS.UnitTests.csproj' --no-restore /p:BuildProjectReferences=false /p:IncludePolkadotLocalNativeTests=true /m:2 /nr:false /p:UseSharedCompilation=false --filter "FullyQualifiedName~$testClass" --logger "trx;LogFileName=$reportName" --results-directory $evidence --verbosity quiet
        if ($LASTEXITCODE -ne 0) { throw 'Native tests failed; retain/reconcile pending originals before any rerun.' }
        [xml]$report = Get-Content -LiteralPath (Join-Path $evidence $reportName)
        $counters = $report.TestRun.ResultSummary.Counters
        $expectedRows = if ($Suite -eq 'History') { '4' } else { '2' }
        if ($counters.total -ne $expectedRows -or $counters.passed -ne $expectedRows -or $counters.failed -ne '0' -or $counters.notExecuted -ne '0') { throw 'Expected all selected native rows executed and passing; no skip-based certification.' }
        if (Test-Path -LiteralPath $journal) { throw 'Pending native original remains; preserve it.' }
        if (Test-Path -LiteralPath $assetApi) { throw 'Pending asset API original remains; preserve it.' }
        if (Test-Path -LiteralPath $assetTransfer) { throw 'Pending asset transfer original remains; preserve it.' }
    } finally { Pop-Location }
} finally {
    foreach ($name in $names) { [Environment]::SetEnvironmentVariable($name, $prior[$name]) }
    $lease.Dispose()
}
