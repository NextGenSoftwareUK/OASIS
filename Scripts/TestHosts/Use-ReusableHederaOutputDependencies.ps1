param(
    [string]$OutputBridge = 'Providers/Blockchain/TestProjects/NextGenSoftware.OASIS.API.Providers.HashgraphOASIS.IntegrationTests/bin/Debug/net10.0/HederaSdkBridge',
    [ValidateSet('Hedera', 'Sui', 'Cosmos')][string]$Provider = 'Hedera'
)
$ErrorActionPreference = 'Stop'
$taskRoot = [IO.Path]::GetFullPath((Split-Path (Split-Path $PSScriptRoot -Parent) -Parent))
$providerDirectory = switch ($Provider) { 'Sui' { 'SuiOASIS' } 'Cosmos' { 'CosmosBlockChainOASIS' } default { 'HashgraphOASIS' } }
$sdkPackage = switch ($Provider) { 'Sui' { '@mysten/sui/package.json' } 'Cosmos' { '@cosmjs/stargate/package.json' } default { '@hiero-ledger/sdk/package.json' } }
$bridgeName = $Provider + 'SdkBridge'
$sourceBridge = Join-Path $taskRoot "Providers/Blockchain/NextGenSoftware.OASIS.API.Providers.$providerDirectory/SdkBridge"
$outputPath = [IO.Path]::GetFullPath((Join-Path $taskRoot $OutputBridge))
if (!$outputPath.StartsWith($taskRoot + [IO.Path]::DirectorySeparatorChar, [StringComparison]::OrdinalIgnoreCase) -or
    (Split-Path $outputPath -Leaf) -ne $bridgeName) {
    throw "Only an existing $bridgeName output inside this checkout is supported."
}
foreach ($manifest in @('package.json', 'package-lock.json')) {
    if ((Get-FileHash -LiteralPath (Join-Path $sourceBridge $manifest)).Hash -ne
        (Get-FileHash -LiteralPath (Join-Path $outputPath $manifest)).Hash) {
        throw "Output $manifest differs from the reusable SDK installation. Rebuild the output before linking."
    }
}
$installed = Join-Path $sourceBridge 'node_modules'
if (!(Test-Path -LiteralPath (Join-Path $installed $sdkPackage))) {
    throw 'The existing pinned SDK installation is missing. No install or download was attempted.'
}
$destination = Join-Path $outputPath 'node_modules'
if (Test-Path -LiteralPath $destination) {
    $entry = Get-Item -LiteralPath $destination -Force
    if ($entry.LinkType -ne 'Junction' -or
        [IO.Path]::GetFullPath([string]$entry.Target) -ne [IO.Path]::GetFullPath($installed)) {
        throw 'Existing output dependencies are not the expected reusable junction; preserve and inspect them.'
    }
} else {
    New-Item -ItemType Junction -Path $destination -Target $installed | Out-Null
}
& node (Join-Path $taskRoot 'Docker/verify-provider-sdk-bridge.mjs') $outputPath
if ($LASTEXITCODE -ne 0) { throw 'Output bridge dependency verification failed.' }
# Local-test setup only: Docker still installs pinned dependencies inside its image.
[pscustomobject]@{ OutputBridge = $outputPath; ReusedDependencies = $installed }
