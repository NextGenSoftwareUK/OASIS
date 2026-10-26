#Requires -Version 5.1
# Read-only contract tests: no packages, builds, native services or disposable environments.
$ErrorActionPreference = 'Stop'
$scriptPath = Join-Path $PSScriptRoot 'cleanup-emergency-space.ps1'
$testPath = $PSCommandPath
$tokens = $null; $parseErrors = $null
[void][Management.Automation.Language.Parser]::ParseFile($scriptPath,[ref]$tokens,[ref]$parseErrors)
if ($parseErrors.Count) { throw ($parseErrors | Out-String) }
. $scriptPath -LibraryOnly -DryRun
$checks = 0
function Assert-Cleanup([bool]$condition,[string]$message) {
    if (!$condition) { throw $message }; $script:checks++
}
function Assert-Refused([scriptblock]$action) {
    $refused = $false
    try { & $action | Out-Null } catch { $refused = $true }
    Assert-Cleanup $refused 'Unsafe target was not refused.'
}
foreach ($target in @($repoRoot,$env:USERPROFILE,(Join-Path $env:USERPROFILE 'Downloads'),(Join-Path $repoRoot '.git'))) {
    Assert-Refused { Get-CleanupInventory $target }
}
Assert-Refused { Convert-CleanupPath '' }
Assert-Cleanup ($repoRoot -notin $cleanupPreserve) 'An omitted optional exclusion must not preserve the entire repository.'
foreach ($name in @('secret.pem','wallet.json','.env','private_key.json','keystore.jks')) {
    Assert-Cleanup (Test-CleanupSecretFileName $name) "Secret not recognized: $name"
}
foreach ($name in @('routing.db','history.sqlite3','history.db-wal','operation.pending.json','Microsoft.Data.Sqlite.dll','Nethereum.HdWallet.dll','Provider.pdb','project.assets.json')) {
    Assert-Cleanup (!(Test-CleanupSecretFileName $name)) "Regenerable build/test output falsely protected: $name"
}
foreach ($leaf in @('TestResults\GoogleCurrent','Providers\Cloud\NextGenSoftware.OASIS.API.Providers.DynamoDBOASIS\bin')) {
    Assert-Cleanup ([string]::IsNullOrEmpty((Get-CleanupPreservationReason (Join-Path $repoRoot $leaf)))) "Build/test output still preserved: $leaf"
}
foreach ($path in $cleanupPreserve) {
    Assert-Cleanup (![string]::IsNullOrEmpty((Get-CleanupPreservationReason $path))) "Shared resource not protected: $path"
}
Assert-Cleanup (!(Test-CleanupOverlap 'C:\Source\OASIS\Project\bin' 'C:\Source\OASIS\Project\bin-other')) 'Sibling prefix must not count as ancestry.'
Assert-Cleanup (Test-CleanupOverlap 'C:\Source\OASIS\Project\bin\Debug' 'C:\Source\OASIS\Project\bin') 'Nested path must count as ancestry.'
$priorSource = (Get-FileHash -LiteralPath $testPath).Hash
$priorLedger = if (Test-Path -LiteralPath $cleanupLedger) { (Get-FileHash -LiteralPath $cleanupLedger).Hash } else { $null }
Remove-Safely $testPath 'Read-only dry-run self-test; do not delete source'
Assert-Cleanup ((Get-FileHash -LiteralPath $testPath).Hash -eq $priorSource) 'Dry run modified source.'
Assert-Cleanup ($(if (Test-Path -LiteralPath $cleanupLedger) { (Get-FileHash -LiteralPath $cleanupLedger).Hash } else { $null }) -eq $priorLedger) 'Dry run wrote cleanup ledger.'
$sample = Join-Path $repoRoot 'Providers\Cloud\NextGenSoftware.OASIS.API.Providers.DynamoDBOASIS'
foreach ($output in @(Find-GeneratedOutput $sample)) {
    Assert-Cleanup ((Split-Path -Leaf $output) -in @('bin','obj')) 'Discovery selected a non-build directory.'
    Assert-Cleanup ($output.StartsWith($sample + '\',[StringComparison]::OrdinalIgnoreCase)) 'Discovery escaped the project.'
}
Write-Host "PASS: $checks read-only cleanup contract checks; zero deletion/build/restore/runtime calls."
