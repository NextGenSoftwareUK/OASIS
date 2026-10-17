param()
$ErrorActionPreference = 'Stop'
$taskRoot = [IO.Path]::GetFullPath((Split-Path (Split-Path $PSScriptRoot -Parent) -Parent))
$testProject = Join-Path $taskRoot 'Providers/Blockchain/TestProjects/NextGenSoftware.OASIS.API.Providers.HashgraphOASIS.IntegrationTests/NextGenSoftware.OASIS.API.Providers.HashgraphOASIS.IntegrationTests.csproj'
$results = Join-Path $taskRoot 'TestResults/HederaCurrent'
# No restore/install, new environment, credentials or funded submissions. Build only this test project;
# callers must build changed provider dependencies first to avoid using stale referenced output.
& dotnet build $testProject --no-restore /m:2 /nr:false /p:UseSharedCompilation=false /p:BuildProjectReferences=false -v:q -clp:ErrorsOnly
if ($LASTEXITCODE -ne 0) { throw 'Hedera test-project build failed.' }
& (Join-Path $PSScriptRoot 'Use-ReusableHederaOutputDependencies.ps1') | Out-Host
# Keep both funded classes explicit; a missing credential is a failure, not proof of a non-funded run.
$testFilter = 'FullyQualifiedName!~HederaTestnetTransferTests&FullyQualifiedName!~HederaTestnetStorageTests'
$started=[DateTime]::UtcNow
& dotnet test $testProject --no-build --no-restore -v:q --filter $testFilter --logger 'trx;LogFileName=hedera-nonfunded-current.trx' --results-directory $results
if ($LASTEXITCODE -ne 0) { throw 'Hedera non-funded regression failed. Inspect the retained TRX.' }
$report=Get-Item -LiteralPath (Join-Path $results 'hedera-nonfunded-current.trx')
if($report.LastWriteTimeUtc -lt $started){throw 'Hedera non-funded report is stale; no verification claim permitted.'}
[xml]$evidence=Get-Content -LiteralPath $report.FullName -Raw
$counters=$evidence.SelectSingleNode("//*[local-name()='Counters']")
$summary=$evidence.SelectSingleNode("//*[local-name()='ResultSummary']")
$executed=@($evidence.SelectNodes("//*[local-name()='UnitTestResult']"))
$definitions=@($evidence.SelectNodes("//*[local-name()='TestMethod']"))
if(!$counters -or ($summary.outcome -ne 'Completed' -and $summary.outcome -ne 'Passed') -or
    [int]$counters.total -lt 1 -or [int]$counters.executed -ne [int]$counters.total -or
    [int]$counters.passed -ne [int]$counters.total -or $executed.Count -ne [int]$counters.total -or
    @($executed|Where-Object outcome -ne 'Passed').Count -gt 0 -or $definitions.Count -eq 0){
    throw 'Hedera non-funded verification must actually execute and pass every selected case without skips.'
}
if(@($definitions|Where-Object {$_.className -match '\.(HederaTestnetTransferTests|HederaTestnetStorageTests)(,|$)'}).Count -gt 0){
    throw 'Funded Hedera classes appeared in non-funded evidence; inspect before claiming safe verification.'
}
