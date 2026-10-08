[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'
$repoRoot = Split-Path -Parent $PSScriptRoot
$registrationPath = Join-Path $repoRoot 'OASIS Architecture/NextGenSoftware.OASIS.OASISBootLoader/OASISBootLoader.Register.cs'
$coveragePath = Join-Path $repoRoot 'Docs/Devs/HYPERDRIVE_V2_PROVIDER_IO_COVERAGE.md'

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

$results = foreach ($provider in $providers) {
    $label = Get-CoveragePattern $provider
    $escapedLabel = [regex]::Escape($label)
    $matchingRows = @($coverage | Where-Object { $_ -match "^\|\s*${escapedLabel}\s*\|" })
    $hasRuntimePass = @($matchingRows | Where-Object {
        $_ -match '\|\s*PASS(?:\s|\(|\|)' -or
        $_ -match '\|\s*\d+/\d+\s+[^|]+\|?\s*$'
    }).Count -gt 0

    [pscustomobject]@{
        Provider = $provider
        RuntimeEvidence = if ($hasRuntimePass) { 'PASS' } else { 'MISSING' }
    }
}

$results | Format-Table -AutoSize | Out-Host
$missing = @($results | Where-Object RuntimeEvidence -ne 'PASS')
if ($missing.Count -gt 0) {
    $names = $missing.Provider -join ', '
    throw "Registered providers without executable PASS evidence: $names"
}

Write-Host "All $($providers.Count) registered storage providers have executable PASS evidence."
