#Requires -Version 7.0
# Single journalled real manager/LINQ run over the qualified alias cohort.
$ErrorActionPreference = 'Stop'
$root = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
$state = Join-Path $root 'LocalState/COSMIC/HostedMatrixPublicSourceCurrent'
$results = Join-Path $root 'TestResults/MatrixPublicSourceCurrent'
$utf8 = [Text.UTF8Encoding]::new($false, $true)
$native = Join-Path $results 'continuation.json'
$digest = (Get-FileHash -LiteralPath $native -Algorithm SHA256).Hash.ToLowerInvariant()
if ($digest -cne '0419a7bb9e809773666e99a46e3f3fdcd9ba2524ee321d91f0edf769e7cba690') {
    throw 'Original native qualification evidence differs.'
}
$observed = Get-Content -LiteralPath $native -Raw | ConvertFrom-Json
$aliases = @('#matrix:matrix.org', '#synapse:matrix.org', '#element:matrix.org')
if (!$observed.passed -or $observed.mutations -ne 0 -or $observed.rows.Count -ne 3 -or
    ($observed.rows.alias -join '|') -cne ($aliases -join '|')) { throw 'Complete qualified alias cohort required.' }
function Write-New([string]$path, [byte[]]$bytes) {
    $stream = [IO.File]::Open($path, [IO.FileMode]::CreateNew)
    try { $stream.Write($bytes); $stream.Flush($true) } finally { $stream.Dispose() }
}
New-Item -ItemType Directory -Force -Path $state | Out-Null
$candidate = @{ schema = 1; apiUrl = 'https://matrix.org/'; aliases = $aliases; sourceOwner = [Guid]::NewGuid().ToString('D') }
$raw = $utf8.GetBytes(($candidate | ConvertTo-Json -Depth 8 -Compress))
$sourceHash = [Convert]::ToHexString([Security.Cryptography.SHA256]::HashData($raw)).ToLowerInvariant()
Write-New (Join-Path $state 'console-qualification.intent.json') $utf8.GetBytes((@{
    nativeQualificationSha256 = $digest; source = $candidate; sourceSha256 = $sourceHash;
    nativeWrites = 0; retryAllowed = $false
} | ConvertTo-Json -Depth 8 -Compress))
Write-New (Join-Path $state 'candidate-source.json') $raw
$log = Join-Path $results 'console-qualification.log'
if (Test-Path -LiteralPath $log) { throw 'Prior console outcome exists; do not replay.' }
& dotnet (Join-Path $root 'Examples/CosmicCqlDemo/bin/Debug/net10.0/CosmicCqlDemo.dll') --matrix-public (Join-Path $state 'candidate-source.json') *> $log
$exitCode = $LASTEXITCODE
Write-New (Join-Path $state 'console-qualification.observed.json') $utf8.GetBytes((@{
    exitCode = $exitCode; sourceSha256 = $sourceHash; nativeWrites = 0; retryAllowed = $false
} | ConvertTo-Json -Compress))
if ($exitCode -ne 0 -or !(Select-String -LiteralPath $log -SimpleMatch 'Read-only Matrix demo complete: 3 full / 2 filtered records.' -Quiet)) {
    throw 'Console qualification incomplete; original owner/configuration/outcome retained.'
}
Write-New (Join-Path $state 'source.json') $raw
Write-New (Join-Path $state 'native-qualified.acknowledged.json') $utf8.GetBytes((@{
    schema = 1; nativeQualified = $true; sourceSha256 = $sourceHash; actualConsoleCounts = @(3, 2);
    policy = 'server-reported-alias-resolution'; originalValidationFailurePreserved = $true;
    hostedCertified = $false; browserCertified = $false
} | ConvertTo-Json -Compress))
'Matrix actual manager/LINQ qualification passed: 3 full / 2 filtered; no native writes.'
