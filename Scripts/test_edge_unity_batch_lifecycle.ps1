$ErrorActionPreference = 'Stop'

$repoRoot = Split-Path $PSScriptRoot -Parent
$validatorPath = Join-Path $repoRoot 'Scripts\validate_edge_unity_package.ps1'
$source = Get-Content -LiteralPath $validatorPath -Raw
$invocations = [regex]::Matches(
    $source,
    '(?ms)Start-Process\s+-FilePath\s+\$UnityEditor\s+-ArgumentList\s+@\((?<arguments>.*?)\)\s+-WindowStyle\s+Hidden\s+-Wait\s+-PassThru')

if ($invocations.Count -ne 2) {
    throw "Expected exactly two Unity batch invocations in '$validatorPath'; found $($invocations.Count)."
}

foreach ($invocation in $invocations) {
    $arguments = $invocation.Groups['arguments'].Value
    foreach ($requiredArgument in @("'-batchmode'", "'-nographics'", "'-quit'", "'-executeMethod'", "'-logFile'")) {
        if ($arguments.IndexOf($requiredArgument, [StringComparison]::Ordinal) -lt 0) {
            throw "Unity batch invocation is missing required lifecycle argument $requiredArgument."
        }
    }
}

Write-Host 'Edge Unity batch lifecycle contract passed: both validator processes terminate deterministically.'
