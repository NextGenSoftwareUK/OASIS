#Requires -Version 7.0
# Exercise the real Windows cleanup entry point in dry-run mode under a busy lease.
$ErrorActionPreference = 'Stop'
$root = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
$lockPath = Join-Path (Split-Path -Parent $root) '.oasis-provider-build/producer-consumer.lock'
$cleanup = Join-Path $PSScriptRoot 'cleanup-emergency-space.ps1'
$ledger = Join-Path $root 'Docs/WIP/Cleanup/CLEANUP_LEDGER.md'
$before = (Get-FileHash -LiteralPath $ledger).Hash
$held = $null
try {
    try { $held = [IO.File]::Open($lockPath, [IO.FileMode]::Open, [IO.FileAccess]::ReadWrite, [IO.FileShare]::None) }
    catch [IO.IOException] {
        throw 'Another provider operation owns the lease; this test stopped without starting cleanup.'
    }
    $info = [Diagnostics.ProcessStartInfo]::new((Join-Path $PSHOME 'pwsh.exe'))
    $info.Arguments = '-NoProfile -NonInteractive -File "' + $cleanup + '" -DryRun -ClearBuildOutputs:$false'
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
        $output = $child.StandardOutput.ReadToEndAsync()
        $errors = $child.StandardError.ReadToEndAsync()
        if (!$child.WaitForExit(30000)) { throw 'Owned dry-run child exceeded thirty seconds.' }
        $text = $output.GetAwaiter().GetResult()
        [void]$errors.GetAwaiter().GetResult()
        if ($child.ExitCode -ne 1 -or $text -notmatch 'Generated-output cleanup stopped:' -or
            $text -notmatch '0 logical bytes verified removed' -or
            $text -match 'DRY RUN \(WSL|\[cleanup\] (Cleaning WSL|Compacting WSL)') {
            throw "Cleanup did not report a busy-lease partial failure correctly; exit=$($child.ExitCode)."
        }
    } finally {
        if ($started -and !$child.HasExited) { $child.Kill($true); $child.WaitForExit() }
        $child.Dispose()
    }
} finally { if ($held) { $held.Dispose() } }
if ((Get-FileHash -LiteralPath $ledger).Hash -ne $before) { throw 'Dry-run cleanup changed its ledger.' }
Write-Output 'PASS: real cleanup CLI refused a busy lease, returned1, removed0 bytes, left its ledger unchanged, and did not start WSL cleanup/compaction.'
