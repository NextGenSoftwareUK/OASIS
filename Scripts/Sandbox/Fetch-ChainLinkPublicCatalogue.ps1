#Requires -Version 7.0
# One explicit catalogue observation from the URL used by Chainlink's own docs.
# This is discovery evidence, not native RPC or CQL acceptance.
$ErrorActionPreference = 'Stop'
$root = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
$state = Join-Path $root 'LocalState/COSMIC/HostedChainLinkPublicSourceCurrent'
$results = Join-Path $root 'TestResults/ChainLinkPublicSourceCurrent'
New-Item -ItemType Directory -Force -Path $state,$results | Out-Null
$intent = Join-Path $state 'catalogue-observation.intent.json'
$url = 'https://reference-data-directory.vercel.app/feeds-mainnet.json'
$utf8 = [Text.UTF8Encoding]::new($false)
$stream = [IO.File]::Open($intent,[IO.FileMode]::CreateNew)
try {
    $stream.Write($utf8.GetBytes((@{url=$url;budgetBytes=4194304;sourceWrites=0;retryAllowed=$false}|ConvertTo-Json -Compress)))
    $stream.Flush($true)
} finally { $stream.Dispose() }
$core = Join-Path $root 'OASIS Architecture/NextGenSoftware.OASIS.API.Core/bin/Debug/net10.0/NextGenSoftware.OASIS.API.Core.dll'
[Reflection.Assembly]::LoadFrom($core) | Out-Null
[NextGenSoftware.OASIS.API.Core.Objects.NativeData.NativeConnectionGuard]::AllowPrivateOverride = $false
$client = [Net.Http.HttpClient]::new([NextGenSoftware.OASIS.API.Core.Objects.NativeData.NativeConnectionGuard]::CreateGuardedHttpHandler())
$client.Timeout = [TimeSpan]::FromSeconds(20)
$report = @{ url=$url;qualified=$false;nativeAcceptance=$false;cqlAcceptance=$false;sourceWrites=0 }
try {
    $response = $client.GetAsync($url,[Net.Http.HttpCompletionOption]::ResponseHeadersRead).GetAwaiter().GetResult()
    try {
        $report.status = [int]$response.StatusCode
        $response.Content.LoadIntoBufferAsync(4194304).GetAwaiter().GetResult() | Out-Null
        $raw = $response.Content.ReadAsByteArrayAsync().GetAwaiter().GetResult()
        $report.bytes=$raw.Length
        $report.sha256=[Convert]::ToHexString([Security.Cryptography.SHA256]::HashData($raw)).ToLowerInvariant()
        $output = [IO.File]::Open((Join-Path $results 'mainnet-catalogue.json'),[IO.FileMode]::CreateNew)
        try { $output.Write($raw);$output.Flush($true) } finally { $output.Dispose() }
        if ($report.status -ne 200) { throw 'Catalogue HTTP status rejected.' }
        $parsed = $utf8.GetString($raw) | ConvertFrom-Json -Depth 32 -DateKind String
        if ($parsed -isnot [array] -or $parsed.Count -lt 3) { throw 'Catalogue array required.' }
        $report.records = $parsed.Count
        $report.qualified = $true
    } finally { $response.Dispose() }
} catch {
    $report.failure=$_.Exception.GetType().FullName
    $report.message=$_.Exception.Message
} finally { $client.Dispose() }
[IO.File]::WriteAllText((Join-Path $results 'catalogue-observation.json'),($report|ConvertTo-Json -Depth 8),$utf8)
$report | ConvertTo-Json -Compress
if (!$report.qualified) { throw 'Original catalogue outcome retained; no retry or fallback.' }
