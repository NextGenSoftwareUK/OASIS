[CmdletBinding()]
param(
    [Parameter(Mandatory)] [string]$EvidencePath,
    [string]$OutputPath
)

$ErrorActionPreference = 'Stop'
$requiredCases = @(
    'location-arrival-radius',
    'location-dwell-reset',
    'ar-gaze-duration',
    'ar-touch',
    'map-ar-vr-ir-representation',
    'audio-video-text-link',
    'quest-objective-presentation',
    'two-avatar-deployed-replicas',
    'deployed-provider-interruption'
)

$resolved = (Resolve-Path -LiteralPath $EvidencePath).Path
$document = Get-Content -Raw -LiteralPath $resolved | ConvertFrom-Json
if ([string]::IsNullOrWhiteSpace([string]$document.runId)) { throw 'Acceptance evidence requires runId.' }
if ([string]::IsNullOrWhiteSpace([string]$document.operator)) { throw 'Acceptance evidence requires operator.' }
try {
    $null = [DateTimeOffset]::Parse(
        [string]$document.observedAtUtc,
        [Globalization.CultureInfo]::InvariantCulture,
        [Globalization.DateTimeStyles]::RoundtripKind)
} catch { throw 'Acceptance evidence observedAtUtc must be an ISO-8601 timestamp.' }

$cases = @($document.cases)
$duplicates = @($cases | Group-Object id | Where-Object Count -gt 1)
if ($duplicates.Count -gt 0) { throw "Duplicate acceptance case ids: $($duplicates.Name -join ', ')" }

foreach ($id in $requiredCases) {
    $case = @($cases | Where-Object id -eq $id)
    if ($case.Count -ne 1) { throw "Acceptance case '$id' is missing." }
    if ([string]$case[0].status -ne 'PASS') { throw "Acceptance case '$id' is not PASS." }
    if ([string]::IsNullOrWhiteSpace([string]$case[0].notes)) { throw "Acceptance case '$id' requires notes." }
    $artifacts = @($case[0].artifacts | Where-Object { -not [string]::IsNullOrWhiteSpace([string]$_) })
    if ($artifacts.Count -eq 0) { throw "Acceptance case '$id' requires at least one artifact path or URL." }
}

$result = [ordered]@{
    validatedAtUtc = [DateTime]::UtcNow.ToString('O')
    source = $resolved
    runId = $document.runId
    operator = $document.operator
    passedCases = $requiredCases.Count
}
$json = $result | ConvertTo-Json -Depth 4
if (-not [string]::IsNullOrWhiteSpace($OutputPath)) {
    $parent = Split-Path -Parent ([IO.Path]::GetFullPath($OutputPath))
    if ($parent) { New-Item -ItemType Directory -Force -Path $parent | Out-Null }
    $json | Set-Content -Encoding UTF8 -LiteralPath $OutputPath
}
Write-Host "PASS $($requiredCases.Count) GeoHotSpot live acceptance cases for run '$($document.runId)'."
