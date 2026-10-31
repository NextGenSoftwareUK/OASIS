$ErrorActionPreference='Stop'
$launcher=Join-Path $PSScriptRoot 'near-local/run-dotnet-evidence.ps1'
$tokens=$null;$errors=$null
$ast=[System.Management.Automation.Language.Parser]::ParseFile($launcher,[ref]$tokens,[ref]$errors)
if($errors.Count){throw ($errors | Out-String)}
$guards=@($ast.FindAll({param($node)
    $node -is [System.Management.Automation.Language.IfStatementAst] -and
    $node.Extent.Text.Contains('Original retained NEAR storage key is missing')
},$true))
if($guards.Count -ne 1){throw 'Expected exactly one retained-key guard.'}
$guard=[scriptblock]::Create($guards[0].Extent.Text)
# Invoke only the actual guard AST: no settings, SDK, DPAPI read, build or node startup.
$keyPath=$launcher
& $guard
$keyPath=Join-Path $PSScriptRoot ('missing-key-guard-'+[Guid]::NewGuid().ToString('N'))
if(Test-Path -LiteralPath $keyPath){throw 'Missing-path test precondition failed.'}
$rejected=$false
try{& $guard}catch{
    if($_.Exception.Message -ne 'Original retained NEAR storage key is missing; recover it before testing the existing encrypted ledger. No replacement is generated.'){throw}
    $rejected=$true
}
if(!$rejected -or (Test-Path -LiteralPath $keyPath)){throw 'Missing original key must fail without creating a replacement.'}
'PASS: existing path accepted; missing original key rejected without creating files or accessing secrets.'
