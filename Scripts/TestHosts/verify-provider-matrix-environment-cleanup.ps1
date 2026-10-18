#Requires -Version 7.0
# Exercises the actual runner cleanup block without starting providers or downloading runtimes.
$ErrorActionPreference = 'Stop'
$runnerPath = Join-Path $PSScriptRoot '../run_four_provider_hyperdrive_matrix.ps1'
$parseTokens = $null
$parseErrors = $null
$runnerAst = [Management.Automation.Language.Parser]::ParseFile(
    $runnerPath, [ref]$parseTokens, [ref]$parseErrors)
if ($parseErrors.Count) { throw 'Provider matrix runner does not parse.' }
$blocks = @($runnerAst.FindAll({ param($node)
    $node -is [Management.Automation.Language.TryStatementAst] -and $node.Finally -and
        $node.Finally.Extent.Text.Contains('foreach ($ownedProcess in $started)')
}, $true))
if ($blocks.Count -ne 1) { throw 'Expected exactly one owned-process cleanup block.' }
$blockText = $blocks[0].Finally.Extent.Text
$cleanup = [scriptblock]::Create($blockText.Substring(1, $blockText.Length - 2))
$probeName = 'OASIS_MATRIX_ENVIRONMENT_CLEANUP_PROBE'
$originalProbe = [Environment]::GetEnvironmentVariable($probeName, 'Process')
try {
    foreach ($priorValue in @($null, 'retained-probe-value')) {
        $started = [Collections.Generic.List[Diagnostics.Process]]::new()
        $previousEnvironment = @{ $probeName = $priorValue }
        [Environment]::SetEnvironmentVariable($probeName, 'changed-probe-value', 'Process')
        & $cleanup
        $variables = [Environment]::GetEnvironmentVariables('Process')
        if ($null -eq $priorValue) {
            if ($variables.Contains($probeName)) { throw 'Absent variable became an empty environment entry.' }
        } elseif ([Environment]::GetEnvironmentVariable($probeName, 'Process') -ne $priorValue) {
            throw 'Original nonempty environment value was not restored.'
        }
    }
    Write-Output 'Actual matrix environment cleanup PASS: absent and nonempty values restored.'
} finally {
    if ($null -eq $originalProbe) {
        if (Test-Path -LiteralPath "Env:$probeName") { Remove-Item -LiteralPath "Env:$probeName" }
    } else { [Environment]::SetEnvironmentVariable($probeName, $originalProbe, 'Process') }
}
