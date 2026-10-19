[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'
$repoRoot = Split-Path -Parent $PSScriptRoot
$registrationPath = Join-Path $repoRoot 'OASIS Architecture/NextGenSoftware.OASIS.OASISBootLoader/OASISBootLoader.Register.cs'
$coveragePath = Join-Path $repoRoot 'Docs/WIP/HyperDrive-Providers/HYPERDRIVE_V2_PROVIDER_IO_COVERAGE.md'

$registration = Get-Content -LiteralPath $registrationPath
$coverage = Get-Content -LiteralPath $coveragePath
$providers = @(
    $registration |
        Select-String -Pattern '^\s*case ProviderType\.(?<provider>[A-Za-z0-9_]+):' |
        ForEach-Object { $_.Matches[0].Groups['provider'].Value } |
        Sort-Object -Unique
)

function Get-CoveragePattern([string] $provider) {
    switch ($provider) {
        'ArbitrumOASIS' { return 'ArbitrumOASIS Web3Core' }
        'EOSIOOASIS' { return 'EOSIOOASIS / TelosOASIS' }
        'TelosOASIS' { return 'EOSIOOASIS / TelosOASIS' }
        default { return $provider }
    }
}

# Historical filename retained for callers. This checks documentation inventory,
# not report contents, current binaries, SDK execution or live provider behavior.
$results = foreach ($provider in $providers) {
    $label = Get-CoveragePattern $provider
    $escapedLabel = [regex]::Escape($label)
    $matchingRows = @($coverage | Where-Object { $_ -match "^\|\s*${escapedLabel}\s*\|" })
    $hasCoverageEntry = $matchingRows.Count -gt 0

    [pscustomobject]@{
        Provider = $provider
        DocumentationEntry = if ($hasCoverageEntry) { 'RECORDED' } else { 'MISSING' }
    }
}

$results | Format-Table -AutoSize | Out-Host
$missing = @($results | Where-Object DocumentationEntry -ne 'RECORDED')
if ($missing.Count -gt 0) {
    $names = $missing.Provider -join ', '
    throw "Registered providers without a documented coverage entry: $names"
}

Write-Host "All $($providers.Count) registered storage providers have documented coverage entries."
Write-Host 'Documentation inventory only: no tests executed, live providers verified or release certification established.'
