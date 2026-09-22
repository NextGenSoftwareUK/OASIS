param(
    [Parameter(Mandatory=$true)][string]$BaseUrl,
    [string]$BearerToken = $env:WEB4_TEST_BEARER,
    [string]$SecondBearerToken = $env:WEB4_SECOND_TEST_BEARER,
    [string]$PythonExecutable = 'python',
    [string]$ServiceCases,
    [string]$Report
)
# Thin launcher for the one WEB4-WEB10 matrix; service keys are read from environment.
$ErrorActionPreference = 'Stop'
$previousBase = $env:WEB4_TEST_BASE_URL
$previousBearer = $env:WEB4_TEST_BEARER
$previousSecond = $env:WEB4_SECOND_TEST_BEARER
try {
    $env:WEB4_TEST_BASE_URL = $BaseUrl
    $env:WEB4_TEST_BEARER = $BearerToken
    $env:WEB4_SECOND_TEST_BEARER = $SecondBearerToken
    $arguments = @((Join-Path $PSScriptRoot 'test_subscription_usage_live.py'))
    if ($ServiceCases) { $arguments += @('--service-cases', $ServiceCases) }
    if ($Report) { $arguments += @('--report', $Report) }
    & $PythonExecutable @arguments
    if ($LASTEXITCODE -ne 0) { throw "Live usage matrix failed (exit $LASTEXITCODE). See its prerequisite/assertion output." }
}
finally {
    $env:WEB4_TEST_BASE_URL = $previousBase
    $env:WEB4_TEST_BEARER = $previousBearer
    $env:WEB4_SECOND_TEST_BEARER = $previousSecond
}
