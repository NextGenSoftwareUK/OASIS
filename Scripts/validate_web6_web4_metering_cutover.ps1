$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSScriptRoot

function Get-ActiveSource([string]$Path) {
    $disabledDepth = 0
    $active = [System.Collections.Generic.List[string]]::new()
    foreach ($line in [System.IO.File]::ReadAllLines($Path)) {
        $token = $line.Trim()
        if ($token -eq '#if false') { $disabledDepth++; continue }
        if ($disabledDepth -gt 0 -and $token.StartsWith('#if ')) { $disabledDepth++; continue }
        if ($disabledDepth -gt 0 -and $token -eq '#endif') { $disabledDepth--; continue }
        if ($disabledDepth -eq 0) { $active.Add($line) }
    }
    return [string]::Join("`n", $active)
}

$failures = [System.Collections.Generic.List[string]]::new()
$checks = [ordered]@{
    '\.CheckQuotaAsync\s*\(' = 'local quota preflight'
    '\.RecordUsageAsync\s*\(' = 'local token usage write'
    '\.RecordUnitUsageAsync\s*\(' = 'local unit usage write'
    'FindFirst\s*\(\s*"(?:plan|karma)"' = 'JWT plan/karma entitlement decision'
    'Request\.(?:Headers|Query)\s*(?:\[\s*"AvatarId"|\.TryGetValue\s*\(\s*"AvatarId")' = 'caller-selected billing avatar'
}
$scanRoots = @(
    (Join-Path $root 'WEB6/NextGenSoftware.OASIS.Web6.WebAPI'),
    (Join-Path $root 'WEB6/NextGenSoftware.OASIS.MCP.Server')
)
foreach ($base in $scanRoots) {
    foreach ($file in Get-ChildItem -LiteralPath $base -Filter '*.cs' -Recurse) {
        $source = Get-ActiveSource $file.FullName
        foreach ($entry in $checks.GetEnumerator()) {
            if ($source -match $entry.Key) {
                $relative = [System.IO.Path]::GetRelativePath($root, $file.FullName)
                $failures.Add("${relative}: active $($entry.Value)")
            }
        }
    }
}

$required = [ordered]@{
    'WEB6/NextGenSoftware.OASIS.Web6.WebAPI/Middleware/SubscriptionMiddleware.cs' = @('UsageEndpointPolicy','RequiresProviderMeasurement','ai.tokens')
    'OASIS Architecture/NextGenSoftware.OASIS.API.Core/Services/Subscriptions/Web4UsageExecution.cs' = @('AuthorizeUsageAsync','StartUsageAsync','TryBeginAsync')
    'OASIS Architecture/NextGenSoftware.OASIS.API.Core/Services/Subscriptions/Web4UsageSettlementWorker.cs' = @('SettleUsageAsync','AcknowledgeAsync','USAGE_PROVIDER_RECONCILIATION_REQUIRED')
    'ONODE/NextGenSoftware.OASIS.API.ONODE.WebAPI/Services/Subscription/MongoSubscriptionUsageRepository.cs' = @('WithTransactionAsync','ux_operation_id','SameAuthorization','SettlementFingerprint')
}
foreach ($entry in $required.GetEnumerator()) {
    $path = Join-Path $root $entry.Key
    $source = Get-ActiveSource $path
    foreach ($marker in $entry.Value) {
        if (-not $source.Contains($marker)) { $failures.Add("$($entry.Key): missing required marker $marker") }
    }
}

if ($failures.Count -gt 0) {
    Write-Error ("WEB6/WEB4 metering cutover validation failed:`n - " + ($failures -join "`n - "))
}
Write-Output 'WEB6/WEB4 metering cutover validation passed.'
