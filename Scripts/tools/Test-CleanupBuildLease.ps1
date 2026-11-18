#Requires -Version 7.0
# Focused, read-only cleanup guards. No restore, build, native backend or output deletion.
$ErrorActionPreference = 'Stop'
. (Join-Path $PSScriptRoot 'cleanup-emergency-space.ps1') -LibraryOnly -DryRun
$actualRepoRoot = $repoRoot
$tempBoundary = [IO.Path]::GetFullPath([IO.Path]::GetTempPath()).TrimEnd('\') + '\'
$fixture = Join-Path $tempBoundary ('oasis-cleanup-lease-' + [Guid]::NewGuid().ToString('N'))
# Only a copy of the lock script and no-op callback: no provider output or build path.
$repoRoot = Join-Path $fixture 'workspace'
$leasePath = Join-Path $repoRoot 'Scripts/TestHosts/Invoke-WithProviderBuildLease.ps1'
$lockPath = Join-Path (Split-Path -Parent $repoRoot) '.oasis-provider-build/producer-consumer.lock'
$legacyShell = Join-Path $env:WINDIR 'System32/WindowsPowerShell/v1.0/powershell.exe'
$script:entered = 0
$script:checks = 0
function Assert-Guard([bool]$condition, [string]$message) {
    if (!$condition) { throw $message }
    $script:checks++
}
# Replace only the destructive callback in this test process; exercise the real wrapper/lock.
function Remove-GeneratedOutputsUnderLease { $script:entered++ }
function Test-CleanupProcess([string]$names) { return $script:activeProducer }
function Invoke-LegacyContender {
    $escaped = $leasePath.Replace("'", "''")
    $code = "`$ErrorActionPreference='Stop'; & '$escaped' -Owner 'cleanup-compatibility-test' -Operation { Write-Output 'LEASE-BODY-ENTERED' }"
    $info = [Diagnostics.ProcessStartInfo]::new($legacyShell)
    $info.Arguments = '-NoProfile -NonInteractive -EncodedCommand ' + [Convert]::ToBase64String([Text.Encoding]::Unicode.GetBytes($code))
    $info.UseShellExecute = $false
    $info.CreateNoWindow = $true
    $info.RedirectStandardOutput = $true
    $info.RedirectStandardError = $true
    $child = [Diagnostics.Process]::new()
    $child.StartInfo = $info
    $started = $false
    try {
        [void]$child.Start()
        $started = $true
        if (!$child.WaitForExit(10000)) { throw 'Owned compatibility-test child exceeded ten seconds.' }
        $stdout = $child.StandardOutput.ReadToEnd()
        $stderr = $child.StandardError.ReadToEnd()
        return @{ ExitCode=$child.ExitCode; Entered=$stdout.Contains('LEASE-BODY-ENTERED');
            PolicyDenied=($stderr -match 'PSSecurityException|UnauthorizedAccess|running scripts is disabled');
            SharingViolation=($stderr -match 'being used by another process') }
    } finally {
        if ($started -and !$child.HasExited) { $child.Kill($true); $child.WaitForExit() }
        $child.Dispose()
    }
}
try {
[void][IO.Directory]::CreateDirectory((Split-Path -Parent $leasePath))
[IO.File]::Copy((Join-Path $actualRepoRoot 'Scripts/TestHosts/Invoke-WithProviderBuildLease.ps1'), $leasePath)
[void][IO.Directory]::CreateDirectory((Split-Path -Parent $lockPath))
[IO.File]::WriteAllText($lockPath, '')
$ledgerBefore = if (Test-Path -LiteralPath $cleanupLedger) { (Get-FileHash $cleanupLedger).Hash } else { $null }
$script:activeProducer = $false
$held = [IO.File]::Open($lockPath, [IO.FileMode]::Open, [IO.FileAccess]::ReadWrite, [IO.FileShare]::None)
try {
    $failuresBefore = $cleanupFailureCount
    Remove-GeneratedOutputs -WarningAction SilentlyContinue
    Assert-Guard ($entered -eq 0 -and $cleanupFailureCount -eq $failuresBefore + 1) 'Busy lease allowed cleanup or hid the failed acquisition.'
    $busy = Invoke-LegacyContender
    Assert-Guard ($busy.ExitCode -ne 0 -and !$busy.Entered -and $busy.SharingViolation) ('Windows PowerShell did not prove lock refusal: ' + ($busy | ConvertTo-Json -Compress))
} finally { $held.Dispose() }
$free = Invoke-LegacyContender
Assert-Guard ($free.ExitCode -eq 0 -and $free.Entered) ('Windows PowerShell could not use the released common lease: ' + ($free | ConvertTo-Json -Compress))
$script:activeProducer = $true
$failuresBefore = $cleanupFailureCount
Remove-GeneratedOutputs -WarningAction SilentlyContinue
Assert-Guard ($entered -eq 0 -and $cleanupFailureCount -eq $failuresBefore + 1) 'Active producer allowed cleanup or was reported as complete.'
$script:activeProducer = $false
Remove-GeneratedOutputs
Assert-Guard ($entered -eq 1) 'Idle cleanup did not enter its callback exactly once.'
$WorktreePath = @()
function git { throw 'Default retirement must not query or remove unreviewed worktrees.' }
Remove-MergedWorktrees
Assert-Guard (!$WorktreePath.Count) 'Default cleanup selected an unreviewed worktree.'
$launcher = Get-Content (Join-Path $PSScriptRoot 'cleanup-emergency-space.bat') -Raw
Assert-Guard ($launcher -notmatch '(?i)ExecutionPolicy\s+Bypass') 'Launcher bypasses the configured execution policy.'
$ledgerAfter = if (Test-Path -LiteralPath $cleanupLedger) { (Get-FileHash $cleanupLedger).Hash } else { $null }
Assert-Guard ($ledgerBefore -eq $ledgerAfter) 'Read-only guard tests changed the cleanup ledger.'
Write-Output "PASS: $checks focused cleanup guards; Windows PowerShell/Core mutual exclusion; no output deletion, build, restore or backend calls."
} finally {
    $full = [IO.Path]::GetFullPath($fixture)
    if (!$full.StartsWith($tempBoundary, [StringComparison]::OrdinalIgnoreCase) -or
        (Split-Path -Leaf $full) -notmatch '^oasis-cleanup-lease-[0-9a-f]{32}$') { throw 'Refusing an unexpected fixture cleanup path.' }
    Remove-Item -LiteralPath $full -Recurse -Force -ErrorAction Stop
    if (Test-Path -LiteralPath $full) { throw 'Owned file-lock fixture remains after cleanup.' }
    Write-Output 'Owned file-lock fixture removed; production provider outputs and their live lease were untouched.'
}
