[CmdletBinding()]
param(
    [Parameter(Mandatory)] [string]$EvidencePath,
    [string]$OutputPath
)

$ErrorActionPreference = 'Stop'
$requiredCases = @(
    'android-online-sign-in',
    'android-flight-mode-warm-session',
    'android-flight-mode-cold-start',
    'android-network-loss-in-flight',
    'android-process-kill-offline-restart',
    'android-suspend-resume',
    'android-reconnect-drain',
    'android-hardware-backed-session',
    'ios-offline-lifecycle',
    'ios-process-kill-offline-restart',
    'ios-reconnect-drain',
    'holo-two-device-offline-convergence',
    'holo-two-device-hosted-reconciliation'
)
$metricPairs = [ordered]@{
    frameTimeP95Ms = 'maxFrameTimeP95Ms'
    cpuUtilizationP95Percent = 'maxCpuUtilizationP95Percent'
    peakPssMb = 'maxPeakPssMb'
    databaseGrowthMb = 'maxDatabaseGrowthMb'
    reconnectSeconds = 'maxReconnectSeconds'
    syncDrainSeconds = 'maxSyncDrainSeconds'
    batteryDrainPercentPerHour = 'maxBatteryDrainPercentPerHour'
    networkUploadMb = 'maxNetworkUploadMb'
    networkDownloadMb = 'maxNetworkDownloadMb'
}

function Assert-Text {
    param([object]$Value, [string]$Name)
    if ([string]::IsNullOrWhiteSpace([string]$Value)) { throw "Device evidence requires $Name." }
}

function Assert-Sha256 {
    param([object]$Value, [string]$Name)
    if ([string]$Value -notmatch '^[0-9a-fA-F]{64}$') { throw "$Name must be a 64-character SHA-256 digest." }
}

$resolved = (Resolve-Path -LiteralPath $EvidencePath).Path
$evidenceRoot = Split-Path -Parent $resolved
$document = Get-Content -Raw -LiteralPath $resolved | ConvertFrom-Json
if ([int]$document.schemaVersion -ne 2) { throw 'Unsupported device evidence schemaVersion.' }
Assert-Text $document.runId 'runId'
Assert-Text $document.operator 'operator'
if ([string]$document.sourceCommit -notmatch '^[0-9a-fA-F]{40}$') { throw 'sourceCommit must be a full 40-character Git commit.' }
if ([string]$document.profile -ne 'HoloEnabled') { throw "Physical release evidence must use the HoloEnabled profile." }
try {
    $observed = [DateTimeOffset]::Parse([string]$document.observedAtUtc,
        [Globalization.CultureInfo]::InvariantCulture, [Globalization.DateTimeStyles]::RoundtripKind)
} catch { throw 'observedAtUtc must be an ISO-8601 timestamp.' }
if ($observed -gt [DateTimeOffset]::UtcNow.AddMinutes(5)) { throw 'observedAtUtc cannot be in the future.' }
Assert-Text $document.build.version 'build.version'
Assert-Sha256 $document.build.androidSha256 'build.androidSha256'
Assert-Sha256 $document.build.iosSha256 'build.iosSha256'

$devices = @($document.devices)
$duplicates = @($devices | Group-Object id | Where-Object Count -gt 1)
if ($duplicates.Count -gt 0) { throw "Duplicate device ids: $($duplicates.Name -join ', ')." }
foreach ($device in $devices) {
    Assert-Text $device.id 'devices[].id'
    Assert-Text $device.model "device '$($device.id)' model"
    Assert-Text $device.osVersion "device '$($device.id)' osVersion"
    Assert-Text $device.architecture "device '$($device.id)' architecture"
    if ([string]$device.platform -notin @('Android', 'iOS')) { throw "Device '$($device.id)' has unsupported platform '$($device.platform)'." }
}
if (@($devices | Where-Object platform -eq 'Android').Count -lt 2) { throw 'At least two physical Android devices are required for Holochain convergence evidence.' }
if (@($devices | Where-Object platform -eq 'iOS').Count -lt 1) { throw 'At least one physical iOS device is required.' }

$measurements = @($document.measurements)
foreach ($platform in @('Android', 'iOS')) {
    $platformMeasurements = @($measurements | Where-Object platform -eq $platform)
    if ($platformMeasurements.Count -ne 1) { throw "Exactly one $platform measurement set is required." }
    $measurement = $platformMeasurements[0]
    foreach ($entry in $metricPairs.GetEnumerator()) {
        $actual = $measurement.($entry.Key)
        $maximum = $measurement.($entry.Value)
        if ($null -eq $actual -or $null -eq $maximum) { throw "$platform measurements require $($entry.Key) and $($entry.Value)." }
        $actualNumber = [double]$actual
        $maximumNumber = [double]$maximum
        if ($actualNumber -lt 0 -or $maximumNumber -le 0) { throw "$platform metric $($entry.Key) must be non-negative and its budget must be positive." }
        if ($actualNumber -gt $maximumNumber) { throw "$platform metric $($entry.Key) ($actualNumber) exceeds $($entry.Value) ($maximumNumber)." }
    }
}

$cases = @($document.cases)
$duplicateCases = @($cases | Group-Object id | Where-Object Count -gt 1)
if ($duplicateCases.Count -gt 0) { throw "Duplicate device acceptance case ids: $($duplicateCases.Name -join ', ')." }
$verifiedArtifacts = [Collections.Generic.List[object]]::new()
foreach ($id in $requiredCases) {
    $case = @($cases | Where-Object id -eq $id)
    if ($case.Count -ne 1) { throw "Device acceptance case '$id' is missing." }
    if ([string]$case[0].status -ne 'PASS') { throw "Device acceptance case '$id' is not PASS." }
    Assert-Text $case[0].notes "case '$id' notes"
    $artifacts = @($case[0].artifacts)
    if ($artifacts.Count -eq 0) { throw "Device acceptance case '$id' requires hashed raw evidence." }
    foreach ($artifact in $artifacts) {
        Assert-Text $artifact.path "case '$id' artifact path"
        Assert-Sha256 $artifact.sha256 "case '$id' artifact sha256"
        if ([IO.Path]::IsPathRooted([string]$artifact.path)) { throw "Case '$id' artifact paths must be relative to the evidence document." }
        $artifactPath = [IO.Path]::GetFullPath((Join-Path $evidenceRoot ([string]$artifact.path)))
        $rootPrefix = [IO.Path]::GetFullPath($evidenceRoot).TrimEnd([IO.Path]::DirectorySeparatorChar) + [IO.Path]::DirectorySeparatorChar
        if (!$artifactPath.StartsWith($rootPrefix, [StringComparison]::OrdinalIgnoreCase)) { throw "Case '$id' artifact escapes the evidence directory." }
        if (!(Test-Path -LiteralPath $artifactPath -PathType Leaf)) { throw "Case '$id' artifact does not exist: '$artifactPath'." }
        $actualHash = (Get-FileHash -LiteralPath $artifactPath -Algorithm SHA256).Hash
        if (!$actualHash.Equals([string]$artifact.sha256, [StringComparison]::OrdinalIgnoreCase)) { throw "Case '$id' artifact hash does not match: '$artifactPath'." }
        $verifiedArtifacts.Add([ordered]@{ caseId = $id; path = [string]$artifact.path; sha256 = $actualHash })
    }
}

$result = [ordered]@{
    schemaVersion = 2
    validatedAtUtc = [DateTime]::UtcNow.ToString('O')
    source = $resolved
    runId = $document.runId
    sourceCommit = ([string]$document.sourceCommit).ToLowerInvariant()
    profile = $document.profile
    deviceCount = $devices.Count
    passedCases = $requiredCases.Count
    verifiedArtifacts = $verifiedArtifacts
    measurements = $measurements
}
$json = $result | ConvertTo-Json -Depth 12
if (-not [string]::IsNullOrWhiteSpace($OutputPath)) {
    $fullOutput = [IO.Path]::GetFullPath($OutputPath)
    $parent = Split-Path -Parent $fullOutput
    if ($parent) { New-Item -ItemType Directory -Force -Path $parent | Out-Null }
    $json | Set-Content -Encoding UTF8 -LiteralPath $fullOutput
}
Write-Host "PASS $($requiredCases.Count) physical-device acceptance cases across $($devices.Count) devices for run '$($document.runId)'."
