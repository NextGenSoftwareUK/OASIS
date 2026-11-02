#Requires -Version 7.0
# One original manager/LINQ qualification. No dispatch, signing or source writes.
$ErrorActionPreference = 'Stop'
$root = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
$state = Join-Path $root 'LocalState/COSMIC/HostedChainflipPublicSourceCurrent'
$results = Join-Path $root 'TestResults/ChainflipPublicSourceCurrent'
$utf8 = [Text.UTF8Encoding]::new($false, $true)
$native = Join-Path $results 'native-qualified-sdk.json'
$digest = (Get-FileHash -LiteralPath $native -Algorithm SHA256).Hash.ToLowerInvariant()
if ($digest -cne 'f885f6574279d71cfb23168a53b09f719682485f963a4f1b21146865cfe57016') {
    throw 'Original Chainflip point qualification evidence differs.'
}
$observed = Get-Content -LiteralPath $native -Raw | ConvertFrom-Json
$swapIds = @('500000','500001','500002')
if (!$observed.qualified -or $observed.nativeWrites -ne 0 -or $observed.rows.Count -ne 3 -or
    $observed.transport -cne 'shipping-sdk-with-standard-native-guard' -or
    ($observed.rows.swapId -join '|') -cne ($swapIds -join '|')) {
    throw 'Original canonical SDK three-swap qualification required.'
}
function Write-New([string]$path, [byte[]]$bytes) {
    $stream = [IO.File]::Open($path, [IO.FileMode]::CreateNew)
    try { $stream.Write($bytes); $stream.Flush($true) } finally { $stream.Dispose() }
}
if (Test-Path -LiteralPath $state) { throw 'Original source state exists; reconcile, never replace its owner.' }
New-Item -ItemType Directory -Path $state | Out-Null
$candidate = [ordered]@{ schema = 1; apiUrl = 'https://chainflip-swap.chainflip.io/'; swapIds = $swapIds; sourceOwner = [Guid]::NewGuid().ToString('D') }
$raw = $utf8.GetBytes(($candidate | ConvertTo-Json -Depth 8 -Compress))
$sourceHash = [Convert]::ToHexString([Security.Cryptography.SHA256]::HashData($raw)).ToLowerInvariant()
Write-New (Join-Path $state 'console-qualification.intent.json') $utf8.GetBytes((@{
    nativeQualificationSha256 = $digest; source = $candidate; sourceSha256 = $sourceHash;
    nativeWrites = 0; retryAllowed = $false
} | ConvertTo-Json -Depth 8 -Compress))
Write-New (Join-Path $state 'candidate-source.json') $raw
$log = Join-Path $results 'console-qualification.log'
if (Test-Path -LiteralPath $log) { throw 'Original console outcome exists; no automatic replay.' }
& dotnet (Join-Path $root 'Examples/CosmicCqlDemo/bin/Debug/net10.0/CosmicCqlDemo.dll') --chainflip-public (Join-Path $state 'candidate-source.json') *> $log
$exitCode = $LASTEXITCODE
Write-New (Join-Path $state 'console-qualification.observed.json') $utf8.GetBytes((@{
    exitCode = $exitCode; sourceSha256 = $sourceHash; nativeWrites = 0; retryAllowed = $false
} | ConvertTo-Json -Compress))
if ($exitCode -ne 0 -or !(Select-String -LiteralPath $log -SimpleMatch 'Read-only Chainflip demo complete: 3 full / 2 filtered records.' -Quiet)) {
    throw 'Chainflip console qualification incomplete; original owner/configuration/outcome retained.'
}
Write-New (Join-Path $state 'source.json') $raw
Write-New (Join-Path $state 'native-qualified.acknowledged.json') $utf8.GetBytes((@{
    schema = 1; nativeQualified = $true; sourceSha256 = $sourceHash; actualConsoleCounts = @(3,2);
    policy = 'indexer-reported-swap-state'; sourceWrites = 0;
    hostedCertified = $false; browserCertified = $false
} | ConvertTo-Json -Compress))
'Chainflip actual manager/LINQ qualification passed: 3 full / 2 filtered; no native writes.'
