#Requires -Version 7.0
# One explicitly journalled read-only qualification of the original captured IDs.
# The earlier discovery/503 outcome remains immutable. Never rerun this script.
$ErrorActionPreference = 'Stop'
$root = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
$state = Join-Path $root 'LocalState/COSMIC/HostedNostrPublicSourceCurrent'
$results = Join-Path $root 'TestResults/NostrPublicSourceCurrent'
$utf8 = [Text.UTF8Encoding]::new($false, $true)
$original = Get-Content -LiteralPath (Join-Path $state 'native-availability.observed.json') -Raw | ConvertFrom-Json
$crypto = Get-Content -LiteralPath (Join-Path $state 'discovery-crypto.observed.json') -Raw | ConvertFrom-Json
if ($original.calls.Count -ne 2 -or $original.calls[1].exitCode -ne 1 -or
    $crypto.validEventHashes -ne 3 -or $crypto.validSchnorrSignatures -ne 3 -or
    $crypto.inputSha256 -cne $original.calls[0].sha256 -or
    $original.relay -cne 'wss://relay.damus.io/') {
    throw 'Retained original discovery/signature/failure proof required.'
}
$ids = @($original.calls[1].filter.ids)
if ($ids.Count -ne 3 -or @($ids | Select-Object -Unique).Count -ne 3) { throw 'Original three IDs required.' }
$candidate = @{ schema = 1; relayUrl = $original.relay; eventIds = $ids; sourceOwner = [Guid]::NewGuid().ToString('D') }
$raw = $utf8.GetBytes(($candidate | ConvertTo-Json -Depth 8 -Compress))
$digest = [Convert]::ToHexString([Security.Cryptography.SHA256]::HashData($raw)).ToLowerInvariant()
$intent = @{ source = $candidate; sourceSha256 = $digest; previousFailurePreserved = $true;
    sourceWrites = 0; retryAllowed = $false; purpose = 'single explicit actual manager/LINQ qualification of captured IDs' }
function Write-New([string]$path, [byte[]]$bytes) {
    $stream = [IO.File]::Open($path, [IO.FileMode]::CreateNew)
    try { $stream.Write($bytes); $stream.Flush($true) } finally { $stream.Dispose() }
}
if (Test-Path -LiteralPath (Join-Path $state 'source.json')) { throw 'Qualified source already exists; never replace owner.' }
Write-New (Join-Path $state 'console-qualification.intent.json') $utf8.GetBytes(($intent | ConvertTo-Json -Depth 10 -Compress))
Write-New (Join-Path $state 'candidate-source.json') $raw
$dll = Join-Path $root 'Examples/CosmicCqlDemo/bin/Debug/net10.0/CosmicCqlDemo.dll'
$log = Join-Path $results 'console-qualification.log'
if (Test-Path -LiteralPath $log) { throw 'Prior console outcome exists; reconcile without replay.' }
& dotnet $dll --nostr-public (Join-Path $state 'candidate-source.json') *> $log
$exitCode = $LASTEXITCODE
$observed = @{ exitCode = $exitCode; sourceSha256 = $digest; sourceWrites = 0; retryAllowed = $false }
Write-New (Join-Path $state 'console-qualification.observed.json') $utf8.GetBytes(($observed | ConvertTo-Json -Compress))
if ($exitCode -ne 0 -or !(Select-String -LiteralPath $log -SimpleMatch 'Read-only Nostr demo: 3 full / 2 filtered events.' -Quiet)) {
    throw 'Native qualification incomplete; original owner, configuration and outcome retained. No automatic retry.'
}
Write-New (Join-Path $state 'source.json') $raw
$ack = @{ schema = 1; nativeQualified = $true; sourceSha256 = $digest; nativeCounts = @(3, 2);
    actualConsoleCounts = @(3, 2); policy = 'signed-event-relay-observed'; previousFailurePreserved = $true }
Write-New (Join-Path $state 'native-qualified.acknowledged.json') $utf8.GetBytes(($ack | ConvertTo-Json -Compress))
Write-Output 'Nostr actual manager/LINQ qualification passed: 3 full / 2 filtered; no native writes.'
