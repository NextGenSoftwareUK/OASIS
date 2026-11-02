#Requires -Version 7.0
$ErrorActionPreference = 'Stop'
$root = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
Set-Location $root
$state = Join-Path $root 'LocalState/COSMIC/HostedBittensorPublicSourceCurrent'
$source = Join-Path $state 'source.json'
$sourceHash = (Get-FileHash -LiteralPath $source -Algorithm SHA256).Hash.ToLowerInvariant()
$sdk = Get-Content -LiteralPath (Join-Path $state 'native-cql.ack.json') -Raw | ConvertFrom-Json
if ($sdk.sourceSha256 -cne $sourceHash -or $sdk.verifiedReadCounts[0] -ne 3 -or $sdk.verifiedReadCounts[1] -ne 2) {
    throw 'Preserved source-bound SDK CQL proof is required.'
}
$results = Join-Path $root 'TestResults/BittensorPublicSourceCurrent'
$log = Join-Path $results 'console.log'
$binary = Join-Path $root 'Examples/CosmicCqlDemo/bin/Debug/net10.0/CosmicCqlDemo.dll'
$intent = @{ sourceSha256=$sourceHash; binarySha256=(Get-FileHash -LiteralPath $binary -Algorithm SHA256).Hash.ToLowerInvariant(); utc=[DateTime]::UtcNow.ToString('O'); nativeWrites=0 }
$stream = [IO.File]::Open((Join-Path $state 'console.intent.json'), [IO.FileMode]::CreateNew)
try { $stream.Write([Text.Encoding]::UTF8.GetBytes(($intent | ConvertTo-Json -Compress))); $stream.Flush($true) }
finally { $stream.Dispose() }
& (Join-Path $PSScriptRoot 'Invoke-WithProviderBuildLease.ps1') -Owner 'cosmic-bittensor-console' -Operation {
    dotnet $binary --bittensor-public $source *> $log
    if ($LASTEXITCODE) { throw 'Original CLI acceptance failed; retain consumed intent and log.' }
}
$output = Get-Content -LiteralPath $log -Raw
if ($output -notmatch 'Read-only Bittensor demo complete: 3 full / 2 filtered records\.' -or
    ([regex]::Matches($output,'Verified content hash: 0x[0-9a-f]{64}')).Count -ne 3) {
    throw 'The actual CLI did not report the complete three/two walkthrough.'
}
if ((Get-FileHash -LiteralPath $source -Algorithm SHA256).Hash.ToLowerInvariant() -cne $sourceHash) { throw 'Source configuration changed.' }
$ack = @{ schema=1; sourceSha256=$sourceHash; full=3; subset=2; writes=$false; rediscovery=$false;
    outputSha256=(Get-FileHash -LiteralPath $log -Algorithm SHA256).Hash.ToLowerInvariant(); binarySha256=$intent.binarySha256 }
$stream = [IO.File]::Open((Join-Path $state 'console.acknowledged.json'), [IO.FileMode]::CreateNew)
try { $stream.Write([Text.Encoding]::UTF8.GetBytes(($ack | ConvertTo-Json -Compress))); $stream.Flush($true) }
finally { $stream.Dispose() }
Write-Output 'Bittensor actual CLI returned three/two; source-bound acknowledgement retained.'
