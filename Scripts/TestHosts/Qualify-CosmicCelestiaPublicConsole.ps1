# One explicit manager/LINQ qualification using the original native catalogue.
# Never recreates an owner or replays a consumed console qualification.
$ErrorActionPreference = 'Stop'
$PSNativeCommandUseErrorActionPreference = $true
$root = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
$results = Join-Path $root 'TestResults/CelestiaPublicSourceCurrent'
$state = Join-Path $root 'LocalState/COSMIC/HostedCelestiaPublicSourceCurrent'
$native = Join-Path $root 'LocalState/COSMIC/CelestiaPublicQualificationCurrent/qualification.observed.json'
$proof = Get-Content -LiteralPath $native -Raw | ConvertFrom-Json
if (!$proof.qualified -or $proof.sourceWrites -ne 0 -or $proof.observations.Count -ne 4 -or
    ($proof.selectedHeights -join '|') -cne '15521793|15521794|15521795') {
    throw 'Original guarded native three-header qualification required.'
}
$headers = [ordered]@{}
for ($index = 0; $index -lt 3; $index++) {
    $id = $index + 2
    $path = Join-Path $results "rpc-$id.json"
    $record = $proof.observations[$index + 1]
    if ($record.id -ne $id -or $record.httpStatus -ne 200 -or
        (Get-FileHash -LiteralPath $path -Algorithm SHA256).Hash.ToLowerInvariant() -cne $record.sha256) {
        throw 'Original native evidence differs; do not replace or requalify.'
    }
    $response = Get-Content -LiteralPath $path -Raw | ConvertFrom-Json
    $height = $proof.selectedHeights[$index]
    if ($response.result.header.chain_id -cne 'celestia' -or $response.result.header.height -cne $height -or
        $response.result.commit.height -cne $height) { throw 'Original header identity differs.' }
    $headers[$height] = $response.result.commit.block_id.hash
}
if (Test-Path -LiteralPath $state) { throw 'Original source state exists; reconcile, never replace its owner.' }
New-Item -ItemType Directory -Path $state | Out-Null
$utf8 = [Text.UTF8Encoding]::new($false)
function Write-New([string]$path, [byte[]]$bytes) {
    $file = [IO.File]::Open($path, [IO.FileMode]::CreateNew)
    try { $file.Write($bytes); $file.Flush($true) } finally { $file.Dispose() }
}
$candidate = [ordered]@{
    schema = 1; rpcUrl = 'https://public-endpoint.celestia-mainnet.quiknode.pro/'
    chainId = 'celestia'; headers = $headers; sourceOwner = [Guid]::NewGuid().ToString('D')
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
    & dotnet (Join-Path $root 'Examples/CosmicCqlDemo/bin/Debug/net10.0/CosmicCqlDemo.dll') --celestia-public (Join-Path $state 'candidate-source.json') *> $log
    $exitCode = $LASTEXITCODE
} finally { $PSNativeCommandUseErrorActionPreference = $previousNativePreference }
Write-New (Join-Path $state 'console-qualification.observed.json') $utf8.GetBytes((@{
    exitCode = $exitCode; sourceSha256 = $hash; sourceWrites = 0; retryAllowed = $false
} | ConvertTo-Json -Compress))
if ($exitCode -ne 0 -or !(Select-String -LiteralPath $log -SimpleMatch 'Read-only Celestia demo complete: 3 full / 2 filtered records.' -Quiet)) {
    throw 'Console qualification incomplete; preserve original owner, pins, configuration and outcome.'
}
Write-New (Join-Path $state 'source.json') $raw
Write-New (Join-Path $state 'native-qualified.acknowledged.json') $utf8.GetBytes((@{
    schema = 1; nativeQualified = $true; sourceSha256 = $hash; actualConsoleCounts = @(3,2)
    policy = 'node-reported-pinned-header'; sourceWrites = 0; consensusVerified = $false
    dataAvailabilitySampled = $false; hostedCertified = $false; browserCertified = $false
} | ConvertTo-Json -Compress))
'Celestia actual manager/LINQ qualification passed: 3 full / 2 filtered; no native writes.'
