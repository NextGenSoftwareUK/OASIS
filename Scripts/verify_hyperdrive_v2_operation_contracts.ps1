<#
.SYNOPSIS
Statically inventories sync/async V2 switch labels and declared provider-boundary test operations.
This does not run tests, prove branch execution or establish full provider/option coverage.
#>
[CmdletBinding()]
param(
    [string]$RouterText,
    [switch]$InventoryOnly
)

$ErrorActionPreference = 'Stop'
$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path
$routerPath = Join-Path $repoRoot 'OASIS Architecture/NextGenSoftware.OASIS.API.Core/Managers/OASIS HyperDrive/OASISHyperDrive.cs'
$testRoots = @(
    'OASIS Architecture/NextGenSoftware.OASIS.API.Core.UnitTests',
    'OASIS Architecture/NextGenSoftware.OASIS.API.Core.HyperDrive.IntegrationTests',
    'Providers/Storage/TestProjects/NextGenSoftware.OASIS.API.Providers.Neo4jOASIS.IntegrationTests'
)
function Get-RouterMethod([string]$Source, [string]$Name) {
    $escaped = [regex]::Escape($Name)
    $pattern = '(?ms)^ {8}private[^\r\n]*\b' + $escaped + '(?:<[^>\r\n]+>)?\([\s\S]*?(?=^ {8}private |\z)'
    $matches = [regex]::Matches($Source, $pattern)
    if ($matches.Count -ne 1) { throw "Expected one router method '$Name', found $($matches.Count)." }
    return $matches[0].Value
}

$router = if ($PSBoundParameters.ContainsKey('RouterText')) { $RouterText } else { Get-Content -Raw -LiteralPath $routerPath }
$sync = Get-RouterMethod $router 'RouteUnpreparedStorageOperation'
$async = Get-RouterMethod $router 'RouteUnpreparedStorageOperationAsync'
$casePattern = 'case\s+"(?<operation>[A-Za-z0-9]+)"\s*:'
$syncOperations = @([regex]::Matches($sync, $casePattern) | ForEach-Object { $_.Groups['operation'].Value } | Sort-Object -Unique)
$asyncOperations = @([regex]::Matches($async, $casePattern) | ForEach-Object { $_.Groups['operation'].Value } | Sort-Object -Unique)
if (!$syncOperations.Count -or !$asyncOperations.Count) { throw 'Direct storage handlers must declare operation labels.' }
$unpaired = @($syncOperations | Where-Object { $_ -notin $asyncOperations }) + @($asyncOperations | Where-Object { $_ -notin $syncOperations })
if ($unpaired.Count) { throw "V2 operations exist in only one sync/async handler: $($unpaired -join ', ')" }
$handlerOperations = $syncOperations

# Conditional payloads use generic dispatch before the direct-operation switch.
$conditionalOperations = @()
if ($router -match '\bCreateConditionalRequest\(') {
    $converter = Get-RouterMethod $router 'CreateConditionalRequest'
    $conditionalOperations = @([regex]::Matches($converter, 'request\.Operation\s*==\s*"(?<operation>[A-Za-z0-9]+)"') |
        ForEach-Object { $_.Groups['operation'].Value } | Sort-Object -Unique)
    foreach ($entry in @(@('RouteStorageOperation', ''), @('RouteStorageOperationAsync', 'Async'))) {
        $body = Get-RouterMethod $router $entry[0]
        $suffix = $entry[1]
        foreach ($pattern in @('request\.ConditionalWrite\s*!=\s*null', "ResolveConditionalPreparation$suffix\(", "ExecuteConditionalHolonWrite$suffix\(", 'ConvertConditionalResult<T>\(')) {
            if ($body -notmatch $pattern) { throw "Conditional dispatch '$($entry[0])' is missing '$pattern'." }
        }
    }
    if (!$conditionalOperations.Count) { throw 'Conditional converter declares no supported request operations.' }
}
if ($InventoryOnly) {
    return [pscustomobject]@{ DirectOperations=$handlerOperations; ConditionalOperations=$conditionalOperations }
}
# Include the executable three-provider project as well as the Core test projects.
# Its routed recovery cases observe the actual MongoDB/SQLite/Neo4j readers.
$tests = (@($testRoots | ForEach-Object { Get-ChildItem -LiteralPath (Join-Path $repoRoot $_) -Recurse -Filter '*.cs' } |
    Where-Object { $_.FullName -notmatch '[\\/](?:bin|obj)[\\/]' } |
    ForEach-Object { Get-Content -Raw -LiteralPath $_.FullName })) -join "`n"

# A case names a storage request or calls a manager/provider boundary. Native
# fixtures also receive a typed HolonManager parameter rather than fixture.Manager.
$testedOperations = @(@([regex]::Matches($tests, 'Operation\s*=\s*"(?<operation>[A-Za-z0-9]+)"')) +
        @([regex]::Matches($tests, '\.Manager\.(?<operation>[A-Za-z0-9]+?)(?:Async)?\s*\(')) |
    ForEach-Object { $_.Groups['operation'].Value } |
    Sort-Object -Unique)
$typedReceivers = @(@([regex]::Matches($tests,
        '\b(?:HolonManager|IOASISStorageProvider|IConditionalHolonPropertyStorageProvider)\??\s+(?<receiver>[A-Za-z_]\w*)\b')) +
        @([regex]::Matches($tests, '\bvar\s+(?<receiver>[A-Za-z_]\w*)\s*=\s*new\s+HolonManager\b')) |
    ForEach-Object { $_.Groups['receiver'].Value } | Sort-Object -Unique)
$typedOperations = @($typedReceivers | ForEach-Object {
    $receiver = [regex]::Escape($_)
    [regex]::Matches($tests, "\b$receiver\s*\.\s*(?<operation>[A-Za-z0-9]+?)(?:Async)?\s*\(") |
        ForEach-Object { $_.Groups['operation'].Value }
})
$testedOperations = @(($testedOperations + $typedOperations) | Sort-Object -Unique)
$untested = @($handlerOperations | Where-Object { $_ -notin $testedOperations })
if ($untested.Count) {
    throw "V2 storage operations have no executable provider-boundary contract: $($untested -join ', ')"
}

Write-Host "HyperDrive V2 static operation inventory: $($handlerOperations.Count) direct operations have paired handler labels and declared boundary cases; $($conditionalOperations.Count) conditional payload kinds have paired generic dispatch entry points. Runtime and full conditional/provider/option coverage require separate evidence."
