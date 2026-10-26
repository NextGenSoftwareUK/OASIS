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
foreach ($name in @('routing.db','history.sqlite3','history.db-wal','operation.pending.json','operation.pending.json.next','secret.pem','wallet.json','.env','ext4.vhdx')) {
    Assert-Cleanup (Test-CleanupProtectedFileName $name) "Protected evidence not recognized: $name"
}
foreach ($name in @('Microsoft.Data.Sqlite.dll','Nethereum.HdWallet.dll','System.Private.ServiceModel.dll','Provider.pdb','project.assets.json')) {
    Assert-Cleanup (!(Test-CleanupProtectedFileName $name)) "Ordinary generated dependency falsely protected: $name"
}
Assert-Cleanup (Test-CleanupUnityCacheDatabase (Join-Path $repoRoot 'artifacts\unity-edge-validation-project\Library\ShaderCache.db')) 'Unity generated shader database must be reclaimable with its Library cache.'
Assert-Cleanup (!(Test-CleanupUnityCacheDatabase (Join-Path $repoRoot 'TestResults\GoogleCurrent\routing.db'))) 'Native test database must remain protected.'
Assert-Cleanup (!(Test-CleanupUnityCacheDatabase (Join-Path $repoRoot 'artifacts\unity-edge-validation-project\Library\secret.pem'))) 'Unity exception must not allow secrets.'
foreach ($project in $cleanupActiveProjects) {
    foreach ($leaf in @('bin','obj')) {
        Assert-Cleanup (![string]::IsNullOrEmpty((Get-CleanupPreservationReason (Join-Path $project $leaf)))) "Active output not protected: $project/$leaf"
    }
}
foreach ($path in $cleanupPreserve) {
    Assert-Cleanup (![string]::IsNullOrEmpty((Get-CleanupPreservationReason $path))) "Shared resource not protected: $path"
}
Assert-Cleanup (!(Test-CleanupOverlap 'C:\Source\OASIS\Project\bin' 'C:\Source\OASIS\Project\bin-other')) 'Sibling prefix must not count as ancestry.'
Assert-Cleanup (Test-CleanupOverlap 'C:\Source\OASIS\Project\bin\Debug' 'C:\Source\OASIS\Project\bin') 'Nested path must count as ancestry.'
$priorSource = (Get-FileHash -LiteralPath $testPath).Hash
$priorLedger = (Get-FileHash -LiteralPath $cleanupLedger).Hash
Remove-Safely $testPath 'Read-only dry-run self-test; do not delete source'
Assert-Cleanup ((Get-FileHash -LiteralPath $testPath).Hash -eq $priorSource) 'Dry run modified source.'
Assert-Cleanup ((Get-FileHash -LiteralPath $cleanupLedger).Hash -eq $priorLedger) 'Dry run wrote cleanup ledger.'
$priorProcessCheck = ${function:Test-CleanupProcess}
$priorRemoval = ${function:Remove-OASISPathSafely}
try {
    function Test-CleanupProcess { return $true }
    function Remove-OASISPathSafely { throw 'Active-build guard reached a removal path.' }
    Remove-GeneratedOutputs
    Assert-Cleanup ($cleanupFailureCount -eq 0) 'Active-build guard did not stop before mutation/lease.'
} finally {
    ${function:Test-CleanupProcess} = $priorProcessCheck
    ${function:Remove-OASISPathSafely} = $priorRemoval
}
$sample = Join-Path $repoRoot 'Providers\Cloud\NextGenSoftware.OASIS.API.Providers.DynamoDBOASIS'
foreach ($output in @(Find-GeneratedOutput $sample)) {
    Assert-Cleanup ((Split-Path -Leaf $output) -in @('bin','obj')) 'Discovery selected a non-build directory.'
    Assert-Cleanup ($output.StartsWith($sample + '\',[StringComparison]::OrdinalIgnoreCase)) 'Discovery escaped the project.'
}
Write-Host "PASS: $checks read-only cleanup contract checks; zero deletion/build/restore/runtime calls."
