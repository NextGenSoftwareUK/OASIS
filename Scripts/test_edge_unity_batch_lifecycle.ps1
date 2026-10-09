$ErrorActionPreference = 'Stop'

$repoRoot = Split-Path $PSScriptRoot -Parent
$validatorPath = Join-Path $repoRoot 'Scripts\validate_edge_unity_package.ps1'
$workflowPath = Join-Path $repoRoot '.github\workflows\edge-runtime-validation.yml'
$source = Get-Content -LiteralPath $validatorPath -Raw
$workflowSource = Get-Content -LiteralPath $workflowPath -Raw
if ($source -match '(?m)^[^#\r\n]*Remove-Item\s+-LiteralPath\s+\$projectRoot') {
    throw 'Unity validation must reuse its owned project/cache rather than recursively delete it on each run.'
}
if ($source -notmatch 'reusable Edge validation project is already open in Unity') {
    throw 'Unity validation must reject an active owner before refreshing its project inputs.'
}
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
        'EditorValidationTimeoutMinutes', 'AndroidBuildTimeoutMinutes',
        '-RedirectStandardOutput $stdoutPath', '-RedirectStandardError $stderrPath',
        'Get-Content -LiteralPath $diagnosticPath -Tail 120')) {
    if ($source.IndexOf($requiredSupervisorContract, [StringComparison]::Ordinal) -lt 0) {
        throw "Unity batch supervisor is missing required contract '$requiredSupervisorContract'."
    }
}

$credentialGateIndex = $workflowSource.IndexOf('name: Require Unity activation credentials', [StringComparison]::Ordinal)
$activationIndex = $workflowSource.IndexOf('name: Activate Unity license', [StringComparison]::Ordinal)
$validationIndex = $workflowSource.IndexOf('name: Validate Edge release', [StringComparison]::Ordinal)
$runtimeIndex = $workflowSource.IndexOf('name: Ensure Unity Windows runtime prerequisites', [StringComparison]::Ordinal)
if ($runtimeIndex -lt 0 -or $runtimeIndex -ge $credentialGateIndex -or
    $workflowSource -notmatch '\./OASIS/Scripts/ensure_unity_windows_runtime\.ps1') {
    throw 'Edge validation must provision Unity Windows runtime prerequisites before activation and editor launch.'
}
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
foreach ($requiredTrigger in @('Scripts/validate_edge_unity_package.ps1', 'Scripts/test_edge_unity_batch_lifecycle.ps1',
        'Scripts/ensure_unity_windows_runtime.ps1', 'Scripts/UnityValidation/**')) {
    if ([regex]::Matches($workflowSource, [regex]::Escape("- '$requiredTrigger'")).Count -ne 2) {
        throw "Both push and pull-request path filters must validate changes to '$requiredTrigger'."
    }
}

# Exercise the real supervisor independently of an installed/licensed Unity editor.
$tokens = $null
$parseErrors = $null
$ast = [System.Management.Automation.Language.Parser]::ParseInput($source, [ref]$tokens, [ref]$parseErrors)
if ($parseErrors.Count -ne 0) { throw 'Unity validator has PowerShell syntax errors.' }
$supervisor = $ast.Find({ param($node)
    $node -is [System.Management.Automation.Language.FunctionDefinitionAst] -and
    $node.Name -eq 'Invoke-UnityBatchProcess'
}, $true)
if ($null -eq $supervisor) { throw 'Unity batch supervisor function was not found.' }
Invoke-Expression $supervisor.Extent.Text
$apkAssertion = $ast.Find({ param($node)
    $node -is [System.Management.Automation.Language.FunctionDefinitionAst] -and
    $node.Name -eq 'Assert-EdgeAndroidApkEntries'
}, $true)
if ($null -eq $apkAssertion) { throw 'Android native-payload assertion is missing.' }
Invoke-Expression $apkAssertion.Extent.Text
$sqliteEntries = @('lib/arm64-v8a/libil2cpp.so', 'lib/arm64-v8a/libe_sqlite3.so')
Assert-EdgeAndroidApkEntries -EntryNames $sqliteEntries -HoloEnabled $false
$holoEntries = $sqliteEntries + @('lib/arm64-v8a/libholochain_conductor_runtime_ffi.so',
    'lib/arm64-v8a/libholochain_conductor_runtime_types_ffi.so')
Assert-EdgeAndroidApkEntries -EntryNames $holoEntries -HoloEnabled $true
foreach ($invalidEntries in @(
    ,@('lib/armeabi-v7a/libmonobdwgc-2.0.so', 'lib/armeabi-v7a/libe_sqlite3.so'),
    ,@('lib/arm64-v8a/libe_sqlite3.so'),
    ,@('lib/arm64-v8a/libil2cpp.so'),
    ,($sqliteEntries + @('lib/x86_64/libil2cpp.so'))
)) {
    $rejected = $false
    try { Assert-EdgeAndroidApkEntries -EntryNames $invalidEntries -HoloEnabled $false }
    catch { $rejected = $true }
    if (-not $rejected) { throw 'Invalid Android backend/ABI/native closure was accepted.' }
}
$rejected = $false
try { Assert-EdgeAndroidApkEntries -EntryNames $sqliteEntries -HoloEnabled $true }
catch { $rejected = $true }
if (-not $rejected) { throw 'Holo-enabled APK without conductor libraries was accepted.' }
Write-Host 'Android payload regression passed: both valid profiles accepted; Mono, missing libraries and foreign ABIs rejected.'
$UnityEditor = (Get-Command pwsh -ErrorAction Stop).Source
$fixtureDirectory = Join-Path $repoRoot 'artifacts\unity-supervisor-regression'
New-Item -ItemType Directory -Path $fixtureDirectory -Force | Out-Null
$fixtureLog = Join-Path $fixtureDirectory 'fixture.log'
$fixtureArguments = [Convert]::ToBase64String([Text.Encoding]::Unicode.GetBytes(
    '[Console]::Out.WriteLine("fixture stdout"); [Console]::Error.WriteLine("fixture stderr"); exit 7'))
try {
    $fixtureExitCode = Invoke-UnityBatchProcess -Phase 'supervisor regression' -LogPath $fixtureLog `
        -TimeoutMinutes 1 -Arguments @('-NoProfile', '-EncodedCommand', $fixtureArguments)
    if ($fixtureExitCode -ne 7) { throw "Supervisor did not preserve failed exit code: $fixtureExitCode." }
    if ((Get-Content -LiteralPath "$fixtureLog.stdout.log" -Raw) -notmatch 'fixture stdout' -or
        (Get-Content -LiteralPath "$fixtureLog.stderr.log" -Raw) -notmatch 'fixture stderr') {
        throw 'Supervisor did not capture both diagnostic streams.'
    }
    $timeoutArguments = [Convert]::ToBase64String([Text.Encoding]::Unicode.GetBytes(
        '[Console]::Out.WriteLine("fixture pid=" + $PID); [Console]::Error.WriteLine("timeout fixture stderr"); Start-Sleep -Seconds 120'))
    $timeoutRejected = $false
    try {
        $null = Invoke-UnityBatchProcess -Phase 'timeout regression' -LogPath $fixtureLog `
            -TimeoutMinutes 1 -Arguments @('-NoProfile', '-EncodedCommand', $timeoutArguments)
    } catch {
        if ($_.Exception.Message -notmatch 'exceeded its 1-minute phase timeout') { throw }
        $timeoutRejected = $true
    }
    if (!$timeoutRejected) { throw 'Supervisor accepted a timed-out process.' }
    $timeoutOutput = Get-Content -LiteralPath "$fixtureLog.stdout.log" -Raw
    if ($timeoutOutput -notmatch 'fixture pid=(\d+)') { throw 'Timeout process identity was not captured.' }
    $fixtureProcessId = [int]$Matches[1]
    if (Get-Process -Id $fixtureProcessId -ErrorAction SilentlyContinue) { throw 'Timed-out fixture is still running.' }
    if ((Get-Content -LiteralPath "$fixtureLog.stderr.log" -Raw) -notmatch 'timeout fixture stderr') {
        throw 'Timeout stderr diagnostic was not preserved.'
    }
} finally {
    # Only this fixture's stopped process outputs are disposable; never remove Unity project caches here.
    foreach ($fixtureOutput in @("$fixtureLog.stdout.log", "$fixtureLog.stderr.log")) {
        if (Test-Path -LiteralPath $fixtureOutput -PathType Leaf) {
            Remove-Item -LiteralPath $fixtureOutput -Force
        }
    }
}
Write-Host 'Unity supervisor regression passed: failed exit code, timeout termination and diagnostic streams verified.'
