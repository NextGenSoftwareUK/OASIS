#Requires -Version 7.0
# One original manager/LINQ qualification. No dispatch, signing or source writes.
$ErrorActionPreference = 'Stop'
$root = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
$state = Join-Path $root 'LocalState/COSMIC/HostedWormholePublicSourceCurrent'
$results = Join-Path $root 'TestResults/WormholePublicSourceCurrent'
$utf8 = [Text.UTF8Encoding]::new($false, $true)
$native = Join-Path $results 'point-qualification.json'
$digest = (Get-FileHash -LiteralPath $native -Algorithm SHA256).Hash.ToLowerInvariant()
if ($digest -cne 'a2df8d9a28810f9fdbfcdd1d22030d8aeedb878ca66a1120ed9589fde1fe5a0e') {
    throw 'Original Wormhole point qualification evidence differs.'
}
$observed = Get-Content -LiteralPath $native -Raw | ConvertFrom-Json
if (!$observed.passed -or $observed.nativeWrites -ne 0 -or $observed.rows.Count -ne 3 -or
    @($observed.attempts | Where-Object httpStatus -ne 200).Count -ne 0) {
    throw 'Original three-operation qualification required.'
}
function Write-New([string]$path, [byte[]]$bytes) {
    $stream = [IO.File]::Open($path, [IO.FileMode]::CreateNew)
    try { $stream.Write($bytes); $stream.Flush($true) } finally { $stream.Dispose() }
}
if (Test-Path -LiteralPath $state) { throw 'Original source state exists; reconcile, never replace its owner.' }
New-Item -ItemType Directory -Path $state | Out-Null
$messages = [ordered]@{}
foreach ($row in $observed.rows) { $messages[$row.operationId] = $row.bodySha256 }
$candidate = [ordered]@{ schema = 1; apiUrl = 'https://api.wormholescan.io/'; messages = $messages; sourceOwner = [Guid]::NewGuid().ToString('D') }
$raw = $utf8.GetBytes(($candidate | ConvertTo-Json -Depth 8 -Compress))
$sourceHash = [Convert]::ToHexString([Security.Cryptography.SHA256]::HashData($raw)).ToLowerInvariant()
Write-New (Join-Path $state 'console-qualification.intent.json') $utf8.GetBytes((@{
    nativeQualificationSha256 = $digest; source = $candidate; sourceSha256 = $sourceHash;
    nativeWrites = 0; retryAllowed = $false
} | ConvertTo-Json -Depth 8 -Compress))
Write-New (Join-Path $state 'candidate-source.json') $raw
$log = Join-Path $results 'console-qualification.log'
if (Test-Path -LiteralPath $log) { throw 'Original console outcome exists; no automatic replay.' }
& dotnet (Join-Path $root 'Examples/CosmicCqlDemo/bin/Debug/net10.0/CosmicCqlDemo.dll') --wormhole-public (Join-Path $state 'candidate-source.json') *> $log
$exitCode = $LASTEXITCODE
Write-New (Join-Path $state 'console-qualification.observed.json') $utf8.GetBytes((@{
    exitCode = $exitCode; sourceSha256 = $sourceHash; nativeWrites = 0; retryAllowed = $false
} | ConvertTo-Json -Compress))
if ($exitCode -ne 0 -or !(Select-String -LiteralPath $log -SimpleMatch 'Read-only Wormhole demo complete: 3 full / 2 filtered records.' -Quiet)) {
    throw 'Wormhole console qualification incomplete; original owner/configuration/outcome retained.'
}
Write-New (Join-Path $state 'source.json') $raw
Write-New (Join-Path $state 'native-qualified.acknowledged.json') $utf8.GetBytes((@{
    schema = 1; nativeQualified = $true; sourceSha256 = $sourceHash; actualConsoleCounts = @(3,2);
    policy = 'indexer-reported-pinned-vaa-body'; sourceWrites = 0; signaturesVerified = $false;
    hostedCertified = $false; browserCertified = $false
} | ConvertTo-Json -Compress))
'Wormhole actual manager/LINQ qualification passed: 3 full / 2 filtered; no native writes.'
