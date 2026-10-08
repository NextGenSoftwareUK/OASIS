<#
.SYNOPSIS
Verifies sync/async V2 storage-operation parity and executable test coverage.
#>
[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'
$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path
$routerPath = Join-Path $repoRoot 'OASIS Architecture/NextGenSoftware.OASIS.API.Core/Managers/OASIS HyperDrive/OASISHyperDrive.cs'
$testPath = Join-Path $repoRoot 'OASIS Architecture/NextGenSoftware.OASIS.API.Core.UnitTests/HyperDrive/HyperDriveProviderExecutionTests.cs'
$router = Get-Content -Raw -LiteralPath $routerPath
$tests = Get-Content -Raw -LiteralPath $testPath

$caseCounts = [regex]::Matches($router, 'case\s+"(?<operation>[A-Za-z0-9]+)"\s*:') |
    ForEach-Object { $_.Groups['operation'].Value } |
    Group-Object
$handlerOperations = @($caseCounts | Where-Object Count -ge 2 | ForEach-Object Name | Sort-Object -Unique)
$singleHandler = @($caseCounts | Where-Object Count -eq 1 | ForEach-Object Name)
if ($singleHandler.Count) {
    throw "V2 operations exist in only one sync/async handler: $($singleHandler -join ', ')"
}

$testedOperations = @([regex]::Matches($tests, 'Operation\s*=\s*"(?<operation>[A-Za-z0-9]+)"') |
    ForEach-Object { $_.Groups['operation'].Value } |
    Sort-Object -Unique)
$untested = @($handlerOperations | Where-Object { $_ -notin $testedOperations })
if ($untested.Count) {
    throw "V2 storage operations have no executable provider-boundary contract: $($untested -join ', ')"
}

Write-Host "HyperDrive V2 storage-operation contracts verified: $($handlerOperations.Count) operations have sync/async parity and executable coverage."
