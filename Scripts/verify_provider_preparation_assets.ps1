param([string]$BuildOutput = 'bin/Debug/net10.0')
$ErrorActionPreference = 'Stop'
$repoRoot = Split-Path -Parent $PSScriptRoot
$source = Join-Path $repoRoot 'Providers/Shared/NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage/prepared-submission.mjs'
$sourceHash = (Get-FileHash -LiteralPath $source -Algorithm SHA256).Hash
foreach ($provider in @('SuiOASIS','CosmosBlockChainOASIS')) {
    $bridge = if ($provider -eq 'SuiOASIS') { 'SuiSdkBridge' } else { 'CosmosSdkBridge' }
    $projectRoot = Join-Path $repoRoot "Providers/Blockchain/NextGenSoftware.OASIS.API.Providers.$provider"
    $output = Join-Path (Join-Path $projectRoot $BuildOutput) $bridge
    $asset = Join-Path $output 'prepared-submission.mjs'
    if (-not (Test-Path -LiteralPath $asset -PathType Leaf)) { throw "$provider preparation module is absent from the requested build output." }
    if ((Get-FileHash -LiteralPath $asset -Algorithm SHA256).Hash -ne $sourceHash) { throw "$provider preparation module differs from the shared source." }
    & node --check $asset
    if ($LASTEXITCODE -ne 0) { throw "$provider preparation module syntax failed." }
    if ($provider -eq 'CosmosBlockChainOASIS') {
        foreach ($name in @('patch-cosmjs.mjs','package.json')) {
            $original = Join-Path $projectRoot "SdkBridge/$name"
            $copied = Join-Path $output $name
            if ((Get-FileHash -LiteralPath $original).Hash -ne (Get-FileHash -LiteralPath $copied).Hash) { throw "Cosmos output $name differs from source." }
        }
    }
    Write-Output "$provider preparation assets match source: $sourceHash"
}
