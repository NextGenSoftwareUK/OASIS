# Explicit, evidence-bound completion of the original read-only console demo.
# Same executable/config/owner; preserves the failed original run. No automatic retry.
param([switch]$ValidateOnly)
$ErrorActionPreference = 'Stop'
$PSNativeCommandUseErrorActionPreference = $true
$root = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
$state = Join-Path $root 'LocalState/COSMIC/HostedBlockscoutPublicSourceCurrent'
$results = Join-Path $root 'TestResults/BlockscoutPublicSourceCurrent'
$candidate = Join-Path $state 'candidate-source.json'
$originalLog = Join-Path $results 'console-qualification.log'
$originalIntent = Get-Content -LiteralPath (Join-Path $state 'console-qualification.intent.json') -Raw | ConvertFrom-Json
$original = Get-Content -LiteralPath (Join-Path $state 'console-qualification.observed.json') -Raw | ConvertFrom-Json
$hash = (Get-FileHash -LiteralPath $candidate -Algorithm SHA256).Hash.ToLowerInvariant()
$failureHash = (Get-FileHash -LiteralPath $originalLog -Algorithm SHA256).Hash.ToLowerInvariant()
$config = Get-Content -LiteralPath $candidate -Raw | ConvertFrom-Json
$expected = @('0xc02aaa39b223fe8d0a0e5c4f27ead9083c756cc2',
    '0x6b175474e89094c44da98b954eedeac495271d0f', '0xa0b86991c6218b36c1d19d4a2e9eb0ce3606eb48')
if ($original.exitCode -eq 0 -or $original.sourceWrites -ne 0 -or $originalIntent.sourceWrites -ne 0 -or
    $original.sourceSha256 -cne $hash -or $originalIntent.sourceSha256 -cne $hash -or
    $config.apiUrl -cne 'https://eth.blockscout.com/api/v2/' -or
    ($config.addresses -join '|') -cne ($expected -join '|') -or
    $config.sourceOwner -cne $originalIntent.source.sourceOwner -or
    !(Select-String -LiteralPath $originalLog -SimpleMatch 'HttpClient.Timeout of 15 seconds elapsing' -Quiet)) {
    throw 'Exact original source-bound read-timeout evidence required; no process started.'
}
$intent = Join-Path $state 'read-only-completion.intent.json'
$log = Join-Path $results 'console-readonly-completion.log'
if ((Test-Path -LiteralPath $intent) -or (Test-Path -LiteralPath $log) -or
    (Test-Path -LiteralPath (Join-Path $state 'source.json'))) {
    throw 'Completion consumed or source already acknowledged; never replay.'
}
$binary = Join-Path $root 'Examples/CosmicCqlDemo/bin/Debug/net10.0/CosmicCqlDemo.dll'
$binaryHash = (Get-FileHash -LiteralPath $binary -Algorithm SHA256).Hash.ToLowerInvariant()
$plan = @{ schema = 1; sourceSha256 = $hash; sourceOwner = $config.sourceOwner
    originalFailureSha256 = $failureHash; binarySha256 = $binaryHash
    writesPermitted = $false; sourceWrites = 0; retryAllowed = $false; maximumInvocations = 1 }
if ($ValidateOnly) { $plan | ConvertTo-Json -Compress; return }
$utf8 = [Text.UTF8Encoding]::new($false)
function Write-New([string]$path, [byte[]]$bytes) {
    $file = [IO.File]::Open($path, [IO.FileMode]::CreateNew)
    try { $file.Write($bytes); $file.Flush($true) } finally { $file.Dispose() }
}
Write-New $intent $utf8.GetBytes(($plan | ConvertTo-Json -Compress))
$previousNativePreference = $PSNativeCommandUseErrorActionPreference
try {
    $PSNativeCommandUseErrorActionPreference = $false
    & dotnet $binary --blockscout-public $candidate *> $log
    $exitCode = $LASTEXITCODE
} finally { $PSNativeCommandUseErrorActionPreference = $previousNativePreference }
Write-New (Join-Path $state 'read-only-completion.observed.json') $utf8.GetBytes((@{
    exitCode = $exitCode; sourceSha256 = $hash; originalFailureSha256 = $failureHash
    sourceWrites = 0; retryAllowed = $false
} | ConvertTo-Json -Compress))
if ($exitCode -ne 0 -or !(Select-String -LiteralPath $log -SimpleMatch 'Read-only Blockscout demo complete: 3 full / 2 filtered records.' -Quiet)) {
    throw 'Original read-only completion incomplete; preserve outcome, no automatic retry.'
}
if ((Get-FileHash -LiteralPath $candidate -Algorithm SHA256).Hash.ToLowerInvariant() -cne $hash -or
    (Get-FileHash -LiteralPath $originalLog -Algorithm SHA256).Hash.ToLowerInvariant() -cne $failureHash -or
    (Get-FileHash -LiteralPath $binary -Algorithm SHA256).Hash.ToLowerInvariant() -cne $binaryHash) {
    throw 'Original source/failure/executable changed; no acknowledgement.'
}
Write-New (Join-Path $state 'source.json') ([IO.File]::ReadAllBytes($candidate))
Write-New (Join-Path $state 'native-qualified.acknowledged.json') $utf8.GetBytes((@{
    schema = 1; nativeQualified = $true; sourceSha256 = $hash; actualConsoleCounts = @(3,2)
    policy = 'explorer-reported-address-state'; sourceWrites = 0; consensusVerified = $false
    originalConsoleFailurePreserved = $true; originalFailureSha256 = $failureHash
    readOnlyCompletion = $true; reliabilityCertified = $false; hostedCertified = $false; browserCertified = $false
} | ConvertTo-Json -Compress))
'Original Blockscout read-only completion passed: 3 full / 2 filtered; original timeout preserved, no source writes.'
