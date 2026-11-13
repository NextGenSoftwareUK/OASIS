# One original manager/LINQ qualification over the retained explorer catalogue.
$ErrorActionPreference = 'Stop'
$PSNativeCommandUseErrorActionPreference = $true
$root = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
$results = Join-Path $root 'TestResults/BlockscoutPublicSourceCurrent'
$state = Join-Path $root 'LocalState/COSMIC/HostedBlockscoutPublicSourceCurrent'
$native = Join-Path $root 'LocalState/COSMIC/BlockscoutPublicQualificationCurrent/qualification.observed.json'
$proof = Get-Content -LiteralPath $native -Raw | ConvertFrom-Json
$addresses = @(
    '0xc02aaa39b223fe8d0a0e5c4f27ead9083c756cc2',
    '0x6b175474e89094c44da98b954eedeac495271d0f',
    '0xa0b86991c6218b36c1d19d4a2e9eb0ce3606eb48'
)
$observedAddresses = @($proof.observations | ForEach-Object { $_.address })
if (!$proof.qualified -or $proof.sourceWrites -ne 0 -or $proof.observations.Count -ne 3 -or
    ($observedAddresses -join '|') -cne ($addresses -join '|')) {
    throw 'Original guarded three-address native qualification required.'
}
for ($index = 0; $index -lt 3; $index++) {
    $path = Join-Path $results "address-$index.json"
    $record = $proof.observations[$index]
    if ($record.status -ne 200 -or
        (Get-FileHash -LiteralPath $path -Algorithm SHA256).Hash.ToLowerInvariant() -cne $record.sha256) {
        throw 'Original native evidence differs; never replace or requalify.'
    }
    $response = Get-Content -LiteralPath $path -Raw | ConvertFrom-Json
    if ($response.hash.ToLowerInvariant() -cne $addresses[$index]) { throw 'Original address identity differs.' }
}
if (Test-Path -LiteralPath $state) { throw 'Original source state exists; reconcile without owner replacement.' }
New-Item -ItemType Directory -Path $state | Out-Null
$utf8 = [Text.UTF8Encoding]::new($false)
function Write-New([string]$path, [byte[]]$bytes) {
    $file = [IO.File]::Open($path, [IO.FileMode]::CreateNew)
    try { $file.Write($bytes); $file.Flush($true) } finally { $file.Dispose() }
}
$candidate = [ordered]@{
    schema = 1; apiUrl = 'https://eth.blockscout.com/api/v2/'
    addresses = $addresses; sourceOwner = [Guid]::NewGuid().ToString('D')
}
$raw = $utf8.GetBytes(($candidate | ConvertTo-Json -Depth 8 -Compress))
$hash = [Convert]::ToHexString([Security.Cryptography.SHA256]::HashData($raw)).ToLowerInvariant()
Write-New (Join-Path $state 'console-qualification.intent.json') $utf8.GetBytes((@{
    sourceSha256 = $hash; source = $candidate; sourceWrites = 0; retryAllowed = $false
    nativeQualificationSha256 = (Get-FileHash -LiteralPath $native -Algorithm SHA256).Hash.ToLowerInvariant()
} | ConvertTo-Json -Depth 8 -Compress))
Write-New (Join-Path $state 'candidate-source.json') $raw
$log = Join-Path $results 'console-qualification.log'
if (Test-Path -LiteralPath $log) { throw 'Original console evidence exists; no replay.' }
$previousNativePreference = $PSNativeCommandUseErrorActionPreference
try {
    $PSNativeCommandUseErrorActionPreference = $false
    & dotnet (Join-Path $root 'Examples/CosmicCqlDemo/bin/Debug/net10.0/CosmicCqlDemo.dll') --blockscout-public (Join-Path $state 'candidate-source.json') *> $log
    $exitCode = $LASTEXITCODE
} finally { $PSNativeCommandUseErrorActionPreference = $previousNativePreference }
Write-New (Join-Path $state 'console-qualification.observed.json') $utf8.GetBytes((@{
    exitCode = $exitCode; sourceSha256 = $hash; sourceWrites = 0; retryAllowed = $false
} | ConvertTo-Json -Compress))
if ($exitCode -ne 0 -or !(Select-String -LiteralPath $log -SimpleMatch 'Read-only Blockscout demo complete: 3 full / 2 filtered records.' -Quiet)) {
    throw 'Console qualification incomplete; preserve the original owner, catalogue and outcome.'
}
Write-New (Join-Path $state 'source.json') $raw
Write-New (Join-Path $state 'native-qualified.acknowledged.json') $utf8.GetBytes((@{
    schema = 1; nativeQualified = $true; sourceSha256 = $hash; actualConsoleCounts = @(3,2)
    policy = 'explorer-reported-address-state'; sourceWrites = 0; consensusVerified = $false
    hostedCertified = $false; browserCertified = $false
} | ConvertTo-Json -Compress))
'Blockscout actual manager/LINQ qualification passed: 3 full / 2 filtered; no source writes.'
