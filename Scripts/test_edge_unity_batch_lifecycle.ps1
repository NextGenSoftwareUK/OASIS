$ErrorActionPreference = 'Stop'

$repoRoot = Split-Path $PSScriptRoot -Parent
$validatorPath = Join-Path $repoRoot 'Scripts\validate_edge_unity_package.ps1'
$workflowPath = Join-Path $repoRoot '.github\workflows\edge-runtime-validation.yml'
$source = Get-Content -LiteralPath $validatorPath -Raw
$workflowSource = Get-Content -LiteralPath $workflowPath -Raw
$invocations = [regex]::Matches(
    $source,
    '(?ms)Invoke-UnityBatchProcess\s+-Phase\s+''[^'']+''.*?-Arguments\s+@\((?<arguments>.*?)\)')

if ($invocations.Count -ne 2) {
    throw "Expected exactly two Unity batch invocations in '$validatorPath'; found $($invocations.Count)."
}

foreach ($invocation in $invocations) {
    $arguments = $invocation.Groups['arguments'].Value
    foreach ($requiredArgument in @("'-batchmode'", "'-nographics'", "'-quit'", "'-executeMethod'", "'-logFile'")) {
        if ($arguments.IndexOf($requiredArgument, [StringComparison]::Ordinal) -lt 0) {
            throw "Unity batch invocation is missing required lifecycle argument $requiredArgument."
        }
    }
}

foreach ($requiredSupervisorContract in @(
        '$process.WaitForExit(5000)', '$process.Kill($true)',
        'EditorValidationTimeoutMinutes', 'AndroidBuildTimeoutMinutes')) {
    if ($source.IndexOf($requiredSupervisorContract, [StringComparison]::Ordinal) -lt 0) {
        throw "Unity batch supervisor is missing required contract '$requiredSupervisorContract'."
    }
}

$credentialGateIndex = $workflowSource.IndexOf('name: Require Unity activation credentials', [StringComparison]::Ordinal)
$activationIndex = $workflowSource.IndexOf('name: Activate Unity license', [StringComparison]::Ordinal)
$validationIndex = $workflowSource.IndexOf('name: Validate Edge release', [StringComparison]::Ordinal)
if ($credentialGateIndex -lt 0 -or $activationIndex -lt 0 -or $validationIndex -lt 0 -or
    $credentialGateIndex -ge $activationIndex -or $activationIndex -ge $validationIndex) {
    throw 'Edge validation must require credentials and activate Unity before launching the editor.'
}
foreach ($requiredActivationContract in @(
        'secrets.UNITY_USERNAME', 'secrets.UNITY_PASSWORD',
        'RageAgainstThePixel/activate-unity-license@e73ff1f81db81a2595f86642123ce2b73b9a9258')) {
    if ($workflowSource.IndexOf($requiredActivationContract, [StringComparison]::Ordinal) -lt 0) {
        throw "Edge workflow is missing Unity activation contract '$requiredActivationContract'."
    }
}

Write-Host 'Edge Unity batch lifecycle contract passed: Unity is activated before both supervised validator processes run.'
