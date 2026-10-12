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
& dotnet test $testProject --no-build --no-restore -v:q --filter $testFilter --logger 'trx;LogFileName=hedera-nonfunded-current.trx' --results-directory $results
if ($LASTEXITCODE -ne 0) { throw 'Hedera non-funded regression failed. Inspect the retained TRX.' }
