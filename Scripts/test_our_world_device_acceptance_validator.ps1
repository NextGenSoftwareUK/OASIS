[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'
$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path
$validator = Join-Path $PSScriptRoot 'validate_our_world_device_acceptance.ps1'
$template = Join-Path $PSScriptRoot 'our-world-device-acceptance-evidence.template.json'
$testRoot = Join-Path ([IO.Path]::GetTempPath()) ("oasis-device-evidence-validator-{0}" -f [Guid]::NewGuid().ToString('N'))
$expectedPrefix = [IO.Path]::GetFullPath([IO.Path]::GetTempPath()).TrimEnd([IO.Path]::DirectorySeparatorChar) + [IO.Path]::DirectorySeparatorChar
$resolvedTestRoot = [IO.Path]::GetFullPath($testRoot)
if (!$resolvedTestRoot.StartsWith($expectedPrefix, [StringComparison]::OrdinalIgnoreCase)) {
    throw "Refusing to create the validator fixture outside the temporary directory: '$resolvedTestRoot'."
}

function Assert-Rejected {
    param([Parameter(Mandatory)][string]$EvidencePath, [Parameter(Mandatory)][string]$ExpectedMessage)
    $rejected = $false
    try {
        & $validator -EvidencePath $EvidencePath
    } catch {
        if ($_.Exception.Message -notlike $ExpectedMessage) { throw }
        $rejected = $true
    }
    if (!$rejected) { throw "Invalid evidence '$EvidencePath' was accepted; expected '$ExpectedMessage'." }
}

New-Item -ItemType Directory -Path $resolvedTestRoot | Out-Null
try {
    $rawEvidencePath = Join-Path $resolvedTestRoot 'device-run.log'
    [IO.File]::WriteAllText($rawEvidencePath, 'deterministic physical-device validator fixture')
    $rawEvidenceHash = (Get-FileHash -LiteralPath $rawEvidencePath -Algorithm SHA256).Hash
    $document = Get-Content -Raw -LiteralPath $template | ConvertFrom-Json
    $document.runId = 'device-validator-contract-test'
    $document.operator = 'release-gate'
    $document.PSObject.Properties.Remove('observedAtUtc')
    $document | Add-Member -NotePropertyName observedAtUtc -NotePropertyValue ([DateTimeOffset]::UtcNow.AddMinutes(-1).ToString('O'))
    $document.sourceCommit = (& git -C $repoRoot rev-parse HEAD).Trim()
    if ($LASTEXITCODE -ne 0) { throw 'Unable to resolve the fixture source commit.' }
    $document.build.version = 'validator-contract-test'
    $document.build.androidSha256 = 'a' * 64
    $document.build.iosSha256 = 'b' * 64
    foreach ($measurement in @($document.measurements)) {
        foreach ($metric in @('frameTimeP95Ms', 'cpuUtilizationP95Percent', 'peakPssMb', 'databaseGrowthMb', 'reconnectSeconds', 'syncDrainSeconds',
                'batteryDrainPercentPerHour', 'networkUploadMb', 'networkDownloadMb')) {
            $measurement.$metric = 1
        }
        foreach ($budget in @('maxFrameTimeP95Ms', 'maxCpuUtilizationP95Percent', 'maxPeakPssMb', 'maxDatabaseGrowthMb', 'maxReconnectSeconds', 'maxSyncDrainSeconds',
                'maxBatteryDrainPercentPerHour', 'maxNetworkUploadMb', 'maxNetworkDownloadMb')) {
            $measurement.$budget = 2
        }
    }
    foreach ($case in @($document.cases)) {
        $case.status = 'PASS'
        $case.notes = 'Deterministic validator contract fixture.'
        $case.artifacts = @([pscustomobject]@{ path = 'device-run.log'; sha256 = $rawEvidenceHash })
    }

    $validPath = Join-Path $resolvedTestRoot 'valid.json'
    $validatedPath = Join-Path $resolvedTestRoot 'validated.json'
    $document | ConvertTo-Json -Depth 12 | Set-Content -Encoding UTF8 -LiteralPath $validPath
    & $validator -EvidencePath $validPath -OutputPath $validatedPath
    $validated = Get-Content -Raw -LiteralPath $validatedPath | ConvertFrom-Json
    if ([int]$validated.passedCases -ne 13 -or [int]$validated.deviceCount -ne 3) {
        throw 'Positive validator fixture returned incorrect case or device counts.'
    }
    if ([IO.Path]::IsPathRooted([string]$validated.verifiedArtifacts[0].path)) {
        throw 'Validated evidence must retain portable relative artifact paths.'
    }

    $document.cases[0].artifacts[0].sha256 = '0' * 64
    $tamperedPath = Join-Path $resolvedTestRoot 'tampered.json'
    $document | ConvertTo-Json -Depth 12 | Set-Content -Encoding UTF8 -LiteralPath $tamperedPath
    Assert-Rejected -EvidencePath $tamperedPath -ExpectedMessage '*artifact hash does not match*'
    $document.cases[0].artifacts[0].sha256 = $rawEvidenceHash

    $document.measurements[0].frameTimeP95Ms = 3
    $overBudgetPath = Join-Path $resolvedTestRoot 'over-budget.json'
    $document | ConvertTo-Json -Depth 12 | Set-Content -Encoding UTF8 -LiteralPath $overBudgetPath
    Assert-Rejected -EvidencePath $overBudgetPath -ExpectedMessage '*exceeds maxFrameTimeP95Ms*'
    $document.measurements[0].frameTimeP95Ms = 1

    $document.measurements[0].cpuUtilizationP95Percent = 3
    $cpuOverBudgetPath = Join-Path $resolvedTestRoot 'cpu-over-budget.json'
    $document | ConvertTo-Json -Depth 12 | Set-Content -Encoding UTF8 -LiteralPath $cpuOverBudgetPath
    Assert-Rejected -EvidencePath $cpuOverBudgetPath -ExpectedMessage '*exceeds maxCpuUtilizationP95Percent*'
    $document.measurements[0].cpuUtilizationP95Percent = 1

    $document.measurements[0].PSObject.Properties.Remove('syncDrainSeconds')
    $missingSyncLatencyPath = Join-Path $resolvedTestRoot 'missing-sync-latency.json'
    $document | ConvertTo-Json -Depth 12 | Set-Content -Encoding UTF8 -LiteralPath $missingSyncLatencyPath
    Assert-Rejected -EvidencePath $missingSyncLatencyPath -ExpectedMessage '*require syncDrainSeconds and maxSyncDrainSeconds*'
    $document.measurements[0] | Add-Member -NotePropertyName syncDrainSeconds -NotePropertyValue 1

    $document.cases = @($document.cases | Where-Object id -ne 'ios-reconnect-drain')
    $missingCasePath = Join-Path $resolvedTestRoot 'missing-case.json'
    $document | ConvertTo-Json -Depth 12 | Set-Content -Encoding UTF8 -LiteralPath $missingCasePath
    Assert-Rejected -EvidencePath $missingCasePath -ExpectedMessage "*case 'ios-reconnect-drain' is missing*"

    Write-Host 'PASS physical-device evidence validator contract, tamper, budget and completeness tests.'
}
finally {
    if (Test-Path -LiteralPath $resolvedTestRoot) {
        [IO.Directory]::Delete($resolvedTestRoot, $true)
    }
}
