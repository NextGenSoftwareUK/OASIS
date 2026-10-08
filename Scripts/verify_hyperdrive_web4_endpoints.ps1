<#
.SYNOPSIS
Captures authenticated WEB4 development HyperDrive read-only release evidence.
.DESCRIPTION
Reads the bearer token from ONODE_JWT_TOKEN by default and never writes or prints it.
The three GET responses contain runtime configuration/status only and are saved as JSON
under the repository artifacts directory for release evidence.
#>
[CmdletBinding()]
param(
    [string]$BaseUrl = 'https://dev.api.web4.oasisomniverse.one',
    [string]$Token = $env:ONODE_JWT_TOKEN,
    [string]$EvidencePath = 'artifacts/hyperdrive-v2-gap-evidence/web4-development-endpoints.json'
)

$ErrorActionPreference = 'Stop'
if ([string]::IsNullOrWhiteSpace($Token)) {
    throw 'An authenticated WEB4 token is required. Set ONODE_JWT_TOKEN or pass -Token; the token will not be logged.'
}

$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path
$evidenceFile = [IO.Path]::GetFullPath((Join-Path $repoRoot $EvidencePath))
$repoPrefix = $repoRoot.TrimEnd([IO.Path]::DirectorySeparatorChar) + [IO.Path]::DirectorySeparatorChar
if (!$evidenceFile.StartsWith($repoPrefix, [StringComparison]::OrdinalIgnoreCase)) {
    throw "Evidence must remain inside '$repoRoot'."
}

$headers = @{ Authorization = "Bearer $Token" }
$root = $BaseUrl.TrimEnd('/')
$mode = Invoke-RestMethod -Uri "$root/api/hyperdrive/mode" -Headers $headers -Method Get -TimeoutSec 60
$config = Invoke-RestMethod -Uri "$root/api/hyperdrive/config" -Headers $headers -Method Get -TimeoutSec 60
$status = Invoke-RestMethod -Uri "$root/api/hyperdrive/status" -Headers $headers -Method Get -TimeoutSec 60

foreach ($entry in @(
    @{ Name = 'mode'; Value = $mode },
    @{ Name = 'config'; Value = $config },
    @{ Name = 'status'; Value = $status }
)) {
    if ($null -eq $entry.Value -or $entry.Value.IsError) {
        throw "The authenticated $($entry.Name) endpoint returned an error: $($entry.Value.Message)"
    }
}

if ([string]::IsNullOrWhiteSpace([string]$mode.Result)) {
    throw 'The mode endpoint did not return an effective mode.'
}
if ([string]::IsNullOrWhiteSpace([string]$mode.MetaData.effectiveConfigurationSource)) {
    throw 'The mode endpoint did not return effectiveConfigurationSource.'
}
$requiredConfigMetadata = @(
    'effectiveMode', 'effectiveConfigurationSource', 'effectiveConfigurationAppliedUtc',
    'effectiveAutoFailoverEnabled', 'effectiveAutoReplicationEnabled',
    'effectiveAutoLoadBalancingEnabled', 'effectiveFailoverProviders',
    'effectiveReplicationProviders', 'effectiveLoadBalancingProviders'
)
foreach ($name in $requiredConfigMetadata) {
    if ($null -eq $config.MetaData.$name) { throw "The config endpoint omitted '$name'." }
}
$requiredStatusFields = @(
    'Mode', 'EffectiveConfigurationSource', 'EffectiveConfigurationAppliedUtc',
    'AutoFailoverEnabled', 'AutoReplicationEnabled', 'AutoLoadBalancingEnabled',
    'LoadBalancingProviders', 'FailoverProviders', 'ReplicationProviders',
    'TotalProviders', 'ActiveProviders'
)
foreach ($name in $requiredStatusFields) {
    if ($null -eq $status.Result.$name) { throw "The status endpoint omitted '$name'." }
}
if ([string]$mode.Result -ne [string]$status.Result.Mode) {
    throw "Mode/status disagree: '$($mode.Result)' versus '$($status.Result.Mode)'."
}
if ([string]$mode.MetaData.effectiveConfigurationSource -ne [string]$status.Result.EffectiveConfigurationSource) {
    throw 'Mode/status disagree about the effective configuration source.'
}

$evidence = [ordered]@{
    capturedUtc = [DateTime]::UtcNow.ToString('O')
    baseUrl = $root
    mode = $mode
    config = $config
    status = $status
}
$directory = Split-Path $evidenceFile
if (!(Test-Path -LiteralPath $directory -PathType Container)) {
    New-Item -ItemType Directory -Path $directory -Force | Out-Null
}
$evidence | ConvertTo-Json -Depth 30 | Set-Content -LiteralPath $evidenceFile -Encoding utf8
Write-Host "Authenticated WEB4 HyperDrive evidence passed and was written to '$evidenceFile'."
