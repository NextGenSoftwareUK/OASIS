#Requires -Version 7.0
[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'
if ($env:GITHUB_ACTIONS -ne 'true') { throw 'Run this qualification only in the isolated CI runner.' }
$ownedUri = 'mongodb://127.0.0.1:27029/?replicaSet=oasisRecoveryRs&serverSelectionTimeoutMS=3000'
if ($env:SUBSCRIPTION_TEST_MONGODB_URI -cne $ownedUri) { throw 'The exact owned Mongo fixture is required.' }
$candidateRoot = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
$project = Join-Path $candidateRoot 'OASIS Architecture/NextGenSoftware.OASIS.API.Core.HyperDrive.IntegrationTests/NextGenSoftware.OASIS.API.Core.HyperDrive.IntegrationTests.csproj'
$testName = 'NextGenSoftware.OASIS.API.Core.HyperDrive.IntegrationTests.MongoSqliteRealProviderRoutingTests.ALoginDeadlineDoesNotSuppressTheRealSqliteFallback'
$resultsRoot = [IO.Path]::Combine([IO.Path]::GetTempPath(), 'oasis-login-qualification-' + [Guid]::NewGuid().ToString('N'))
[IO.Directory]::CreateDirectory($resultsRoot) | Out-Null
try {
    # This runner owns the whole build/test lease. Do not invoke inside another lease.
    & (Join-Path $PSScriptRoot 'Invoke-WithProviderBuildLease.ps1') -Owner 'login-failover-single-case' -Operation {
        dotnet test $project --configuration Release --filter "FullyQualifiedName=$testName" --results-directory $resultsRoot --logger 'trx;LogFileName=login.trx' /m:2 /nr:false /p:UseSharedCompilation=false
        if ($LASTEXITCODE -ne 0) { throw 'The real login failover regression failed.' }
        [xml]$report = Get-Content -LiteralPath (Join-Path $resultsRoot 'login.trx') -Raw
        $cases = @($report.TestRun.Results.UnitTestResult)
        $counters = $report.TestRun.ResultSummary.Counters
        if ($cases.Count -ne 1 -or $cases[0].testName -cne $testName -or
            $cases[0].outcome -cne 'Passed' -or [int]$counters.total -ne 1 -or
            [int]$counters.passed -ne 1 -or [int]$counters.failed -ne 0) {
            throw 'Qualification requires exactly one executed, passing named case.'
        }
        Write-Host 'LOGIN_FAILOVER_SINGLE_CASE_ACCEPTED total=1 passed=1'
    }
}
finally {
    $resolvedResults = [IO.Path]::GetFullPath($resultsRoot)
    $expectedParent = [IO.Path]::GetFullPath([IO.Path]::GetTempPath()).TrimEnd([IO.Path]::DirectorySeparatorChar)
    if ([IO.Path]::GetDirectoryName($resolvedResults) -cne $expectedParent -or
        -not [IO.Path]::GetFileName($resolvedResults).StartsWith('oasis-login-qualification-', [StringComparison]::Ordinal)) {
        throw 'Refusing cleanup outside the owned temporary report directory.'
    }
    if (Test-Path -LiteralPath $resolvedResults) { Remove-Item -LiteralPath $resolvedResults -Recurse -Force }
    if (Test-Path -LiteralPath $resolvedResults) { throw 'Owned qualification report cleanup failed.' }
    Write-Host 'LOGIN_FAILOVER_REPORT_CLEANUP absence verified'
}
