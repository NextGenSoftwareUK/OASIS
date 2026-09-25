[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)][string]$PublicGrantFragment,
    [Parameter(Mandatory = $true)][string]$OutputPath,
    [string]$Web4BaseUrl = 'https://dev.api.web4.oasisomniverse.one',
    [string]$Web5BaseUrl = 'https://dev.api.starnet.oasisomniverse.one',
    [string]$HostedOnodeBaseUrl = 'https://dev.api.web4.oasisomniverse.one',
    [string]$BaseConfig = 'OASIS Omniverse\OASIS Hub\Assets\StreamingAssets\omniverse_host_config.json'
)

$ErrorActionPreference = 'Stop'
$repoRoot = Split-Path $PSScriptRoot -Parent
$basePath = [IO.Path]::GetFullPath((Join-Path $repoRoot $BaseConfig))
$fragmentPath = [IO.Path]::GetFullPath((Join-Path $repoRoot $PublicGrantFragment))
$resolvedOutput = [IO.Path]::GetFullPath((Join-Path $repoRoot $OutputPath))
foreach ($url in @($Web4BaseUrl, $Web5BaseUrl, $HostedOnodeBaseUrl)) {
    $parsed = $null
    if (![Uri]::TryCreate($url, [UriKind]::Absolute, [ref]$parsed) -or $parsed.Scheme -ne 'https' -or
        $parsed.IsLoopback) { throw "Release OASIS endpoints must be non-loopback HTTPS URLs: '$url'." }
}
if (!(Test-Path -LiteralPath $basePath -PathType Leaf)) { throw "Base config not found: $basePath" }
if (!(Test-Path -LiteralPath $fragmentPath -PathType Leaf)) { throw "Public grant fragment not found: $fragmentPath" }

$config = Get-Content -LiteralPath $basePath -Raw | ConvertFrom-Json
$fragment = Get-Content -LiteralPath $fragmentPath -Raw | ConvertFrom-Json
if (!$fragment.enableEdgeRuntime -or [string]::IsNullOrWhiteSpace($fragment.edgeOfflineGrantPublicKey)) {
    throw 'The public grant fragment must enable Edge and contain its pinned ECDSA public key.'
}
try { [Convert]::FromBase64String($fragment.edgeOfflineGrantPublicKey) | Out-Null }
catch { throw 'The pinned Edge offline-grant public key is not valid base64.' }

$config.web4OasisApiBaseUrl = $Web4BaseUrl.TrimEnd('/')
$config.web5StarApiBaseUrl = $Web5BaseUrl.TrimEnd('/')
$config.avatarId = ''
$config.enableEdgeRuntime = $true
$config.edgeHostedOnodeBaseUrl = $HostedOnodeBaseUrl.TrimEnd('/')
$config.edgeOfflineGrantPublicKey = $fragment.edgeOfflineGrantPublicKey
$config.edgeOfflineGrantLifetimeMinutes = $fragment.edgeOfflineGrantLifetimeMinutes
$config.edgeOfflineScopes = $fragment.edgeOfflineScopes

$parent = Split-Path $resolvedOutput -Parent
New-Item -ItemType Directory -Path $parent -Force | Out-Null
$config | ConvertTo-Json -Depth 20 | Set-Content -LiteralPath $resolvedOutput -Encoding utf8NoBOM
Write-Host "Our World Edge release config created: $resolvedOutput"
