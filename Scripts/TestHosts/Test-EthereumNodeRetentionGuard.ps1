$ErrorActionPreference='Stop'
$launcher=Join-Path $PSScriptRoot 'Run-ReusableEthereumTests.ps1'
$tokens=$null;$errors=$null
$ast=[System.Management.Automation.Language.Parser]::ParseFile($launcher,[ref]$tokens,[ref]$errors)
if($errors.Count){throw ($errors|Out-String)}
$guards=@($ast.FindAll({param($node)
    $node -is [System.Management.Automation.Language.IfStatementAst] -and
    $node.Clauses.Count -eq 1 -and
    $node.Clauses[0].Item1.Extent.Text.Contains('$nativeChecksStarted') -and
    $node.Extent.Text.Contains('This ephemeral node is the original receipt/state authority.')
},$true))
if($guards.Count -ne 1){throw 'Expected exactly one native-state retention guard.'}
$condition=[scriptblock]::Create($guards[0].Clauses[0].Item1.Extent.Text)
# Evaluate ONLY the actual decision expression with controlled path observations.
# Never execute the runner/finally bodies, start/stop a node, build, or access original records.
& {
    param($decision)
    $historyPending='history';$ethereumPending='ethereum';$keyDeletePending='key-delete';$retainedRouting='routing'
    $script:observed=@{}
    function Test-Path {param([string]$LiteralPath) return $script:observed.ContainsKey($LiteralPath)}
    $checks=0
    foreach($started in @($false,$true)){
        foreach($verified in @($false,$true)){
            foreach($mask in 0..15){
                $nativeChecksStarted=$started;$nativeChecksVerified=$verified
                $script:observed=@{}
                $paths=@($historyPending,$ethereumPending,$keyDeletePending,$retainedRouting)
                for($index=0;$index -lt 4;$index++){if($mask -band (1 -shl $index)){$script:observed[$paths[$index]]=$true}}
                $actual=& $decision
                $expected=($started -and !$verified) -or ($mask -ne 0)
                if($actual -ne $expected){throw "Retention mismatch: started=$started verified=$verified evidenceMask=$mask"}
                $checks++
            }
        }
    }
    "PASS: $checks actual guard decisions; incomplete native checks or any original evidence preserve node, verified clean run permits stop. No service/files/records mutated."
} $condition
$source=$ast.Extent.Text
$begin=$source.IndexOf('$nativeChecksStarted=$true',[StringComparison]::Ordinal)
$firstTest=$source.IndexOf('& dotnet test $project',[StringComparison]::Ordinal)
$complete=$source.IndexOf('$nativeChecksVerified=$true',[StringComparison]::Ordinal)
$lastGate=$source.IndexOf('if([int]$web3Counts.total',[StringComparison]::Ordinal)
if($begin -lt 0 -or $firstTest -le $begin -or $lastGate -le $firstTest -or $complete -le $lastGate){throw 'Native lifecycle flags must bracket both suites and final acceptance gate.'}
'PASS: actual lifecycle flags bracket native test invocation and final acceptance count gate.'
