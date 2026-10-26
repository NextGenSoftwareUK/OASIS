param([switch]$ReadBridge)
$ErrorActionPreference = 'Stop'
$repo = Split-Path (Split-Path $PSScriptRoot -Parent) -Parent
$evidence = Join-Path $repo 'TestResults/PolkadotCurrent'
$sdk = Join-Path $repo 'Providers/Blockchain/NextGenSoftware.OASIS.API.Providers.PolkadotOASIS/SdkBridge'
if (!(Test-Path -LiteralPath $evidence -PathType Container)) { throw 'Reuse the retained environment.' }
$lease = [IO.FileStream]::new((Join-Path $evidence 'local-native-environment.lock'), [IO.FileMode]::OpenOrCreate, [IO.FileAccess]::ReadWrite, [IO.FileShare]::None)
$prior = $env:OASIS_POLKADOT_LOCAL_NATIVE
try {
    foreach ($name in @('local-native-pending.json','local-bootstrap-pending.json','local-asset-bootstrap-pending.json',
        'local-asset-mutation-pending.json','local-asset-api-pending.json','local-asset-transfer-pending.json','local-record-contract-pending.json')) {
        if (Test-Path -LiteralPath (Join-Path $evidence $name)) { throw "Pending original exists: $name. Reconcile read-only, never replay." }
    }
    $env:OASIS_POLKADOT_LOCAL_NATIVE = '1'
    Push-Location $sdk
    try {
        & node verify-record-contract-artifact.mjs
        if ($LASTEXITCODE -ne 0) { throw 'Artifact admission failed.' }
        if ($ReadBridge) { & node --test test-local-record-read-bridge.mjs }
        else { & node test-local-record-contract.mjs }
        if ($LASTEXITCODE -ne 0) { throw 'Native records test failed. Preserve original journals and fixture state.' }
    } finally { Pop-Location }
} finally {
    $env:OASIS_POLKADOT_LOCAL_NATIVE = $prior
    $lease.Dispose()
}
