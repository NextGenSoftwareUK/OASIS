#Requires -Version 7.0
$ErrorActionPreference='Stop'
# Load only the pure admission functions. Never dot-source the runner's operations.
$tokens=$null;$errors=$null
$ast=[Management.Automation.Language.Parser]::ParseFile((Join-Path $PSScriptRoot 'Run-ReusableOracleTests.ps1'),[ref]$tokens,[ref]$errors)
if($errors.Count){throw 'Oracle runner syntax errors.'}
foreach($name in @('Assert-OracleProcessWriterOwnership','Assert-OracleProcessStopped')){
    $functions=@($ast.FindAll({param($node) $node -is [Management.Automation.Language.FunctionDefinitionAst]},$true)|Where-Object Name -eq $name)
    if($functions.Count -ne 1){throw "Exactly one policy function required: $name"}
    . ([scriptblock]::Create($functions[0].Extent.Text))
}
$verified=0
foreach($abrupt in @($false,$true)){
    $validCode=if($abrupt){137}else{143}
    foreach($case in @(
        @{Id='owned';Status='exited';Code=0;Oom=$false;Valid=(!$abrupt)},
        @{Id='owned';Status='exited';Code=143;Oom=$false;Valid=(!$abrupt)},
        @{Id='owned';Status='exited';Code=137;Oom=$false;Valid=$abrupt},
        @{Id='owned';Status='exited';Code=$validCode;Oom=$true;Valid=$false},
        @{Id='different';Status='exited';Code=$validCode;Oom=$false;Valid=$false},
        @{Id='owned';Status='running';Code=$validCode;Oom=$false;Valid=$false}
    )){
        $accepted=$true
        try{Assert-OracleProcessStopped -ExpectedContainerId 'owned' -Abrupt $abrupt -Actual ([pscustomobject]@{ContainerId=$case.Id;Status=$case.Status;ExitCode=$case.Code;OomKilled=$case.Oom})}
        catch{$accepted=$false}
        if($accepted -ne $case.Valid){throw 'Oracle stop policy accepted/rejected the wrong mode or runtime.'}
        $verified++
    }
}
foreach($case in @(
    @{Started=$true;Id='owned';Status='running';Valid=$true},
    @{Started=$false;Id='owned';Status='running';Valid=$false},
    @{Started=$true;Id='different';Status='running';Valid=$false},
    @{Started=$true;Id='owned';Status='exited';Valid=$false}
)){
    $accepted=$true
    try{Assert-OracleProcessWriterOwnership -StartedHere $case.Started -ExpectedContainerId 'owned' -Actual ([pscustomobject]@{ContainerId=$case.Id;Status=$case.Status})}
    catch{$accepted=$false}
    if($accepted -ne $case.Valid){throw 'Oracle runtime ownership policy mismatch.'}
    $verified++
}
Write-Output "$verified Oracle restart policy controls PASS; no runtime or database access."
