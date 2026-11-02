#Requires -Version 7.0
# One journalled manager/LINQ qualification; no account, signing or source writes.
$ErrorActionPreference = 'Stop'
$root = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
$state = Join-Path $root 'LocalState/COSMIC/HostedStacksPublicSourceCurrent'
$results = Join-Path $root 'TestResults/StacksPublicSourceCurrent'
$utf8 = [Text.UTF8Encoding]::new($false, $true)
$native = Join-Path $results 'qualification.json'
$digest = (Get-FileHash -LiteralPath $native -Algorithm SHA256).Hash.ToLowerInvariant()
if ($digest -cne '2d2e5530b6948b70707a0f5673b778c50dab57587341b6a610e5f156961c3fd9') {
    throw 'Original Stacks native qualification evidence differs.'
}
$observed = Get-Content -LiteralPath $native -Raw | ConvertFrom-Json
$principals = @('SP000000000000000000002Q6VF78', 'SP000000000000000000002Q6VF78.pox-4', 'SP000000000000000000002Q6VF78.pox-5')
if (!$observed.qualified -or $observed.nativeWrites -ne 0 -or $observed.rows.Count -ne 3 -or
    ($observed.rows.principal -join '|') -cne ($principals -join '|')) { throw 'Original three-principal qualification required.' }
function Write-New([string]$path, [byte[]]$bytes) {
    $stream = [IO.File]::Open($path, [IO.FileMode]::CreateNew)
    try { $stream.Write($bytes); $stream.Flush($true) } finally { $stream.Dispose() }
}
New-Item -ItemType Directory -Force -Path $state | Out-Null
$candidate = @{ schema = 1; apiUrl = 'https://api.hiro.so/'; principals = $principals; sourceOwner = [Guid]::NewGuid().ToString('D') }
$raw = $utf8.GetBytes(($candidate | ConvertTo-Json -Depth 8 -Compress))
$sourceHash = [Convert]::ToHexString([Security.Cryptography.SHA256]::HashData($raw)).ToLowerInvariant()
Write-New (Join-Path $state 'console-qualification.intent.json') $utf8.GetBytes((@{
    nativeQualificationSha256 = $digest; source = $candidate; sourceSha256 = $sourceHash;
    nativeWrites = 0; retryAllowed = $false
} | ConvertTo-Json -Depth 8 -Compress))
Write-New (Join-Path $state 'candidate-source.json') $raw
$log = Join-Path $results 'console-qualification.log'
if (Test-Path -LiteralPath $log) { throw 'Original console outcome exists; no automatic replay.' }
& dotnet (Join-Path $root 'Examples/CosmicCqlDemo/bin/Debug/net10.0/CosmicCqlDemo.dll') --stacks-public (Join-Path $state 'candidate-source.json') *> $log
$exitCode = $LASTEXITCODE
Write-New (Join-Path $state 'console-qualification.observed.json') $utf8.GetBytes((@{
    exitCode = $exitCode; sourceSha256 = $sourceHash; nativeWrites = 0; retryAllowed = $false
} | ConvertTo-Json -Compress))
if ($exitCode -ne 0 -or !(Select-String -LiteralPath $log -SimpleMatch 'Read-only Stacks demo complete: 3 full / 2 filtered records.' -Quiet)) {
    throw 'Stacks console qualification incomplete; original owner/configuration/outcome retained.'
}
Write-New (Join-Path $state 'source.json') $raw
Write-New (Join-Path $state 'native-qualified.acknowledged.json') $utf8.GetBytes((@{
    schema = 1; nativeQualified = $true; sourceSha256 = $sourceHash; actualConsoleCounts = @(3,2);
    policy = 'node-reported-explicit-tip'; sourceWrites = 0; hostedCertified = $false; browserCertified = $false
} | ConvertTo-Json -Compress))
'Stacks actual manager/LINQ qualification passed: 3 full / 2 filtered; no native writes.'
