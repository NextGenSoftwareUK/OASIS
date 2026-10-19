param([switch]$InspectRetainedFixture,[switch]$ReconcileRetainedRoutingFixture)
$ErrorActionPreference='Stop'
$root=(Resolve-Path (Join-Path $PSScriptRoot '../..')).Path
$work=Join-Path $root 'TestResults/CosmosDBCurrent'
New-Item -ItemType Directory -Force $work | Out-Null
$lease=[IO.File]::Open((Join-Path $work 'environment.lock'),[IO.FileMode]::OpenOrCreate,[IO.FileAccess]::ReadWrite,[IO.FileShare]::None)
$started=$false;$previous=$env:OASIS_COSMOSDB_NATIVE
try {
    Push-Location $root
    if(Get-NetTCPConnection -LocalPort 8082 -State Listen -ErrorAction SilentlyContinue) {throw 'Reserved Cosmos SDK outage endpoint8082 occupied.'}
    $state=(& wsl.exe -d Ubuntu -u root -- podman inspect oasis-cosmos --format '{{.State.Status}} {{.Image}}') -join ''
    if($LASTEXITCODE -ne 0 -or $state.Trim() -ne 'exited 77f8a39f5c39e0b7878c8d9cddd70ff08872695dec9b85d2055b38d5d58d5302') {throw 'Expected retained stopped Cosmos DB container/image; refusing replacement or interference.'}
    $project='Providers/Cloud/TestProjects/NextGenSoftware.OASIS.API.Providers.AzureCosmosDBOASIS.IntegrationTests/NextGenSoftware.OASIS.API.Providers.AzureCosmosDBOASIS.IntegrationTests.csproj'
    foreach($producer in @('Providers/Storage/NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS/NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS.csproj','Providers/Cloud/NextGenSoftware.OASIS.API.Providers.AzureCosmosDBOASIS/NextGenSoftware.OASIS.API.Providers.AzureCosmosDBOASIS.csproj',$project)) {
        & dotnet build $producer --no-restore /p:BuildProjectReferences=false /m:2 /nr:false /p:UseSharedCompilation=false -v:q -clp:ErrorsOnly
        if($LASTEXITCODE) {throw "Cached build failed: $producer"}
    }
    & wsl.exe -d Ubuntu -u root -- podman start oasis-cosmos
    if($LASTEXITCODE) {throw 'Retained Cosmos DB container did not start.'}
    $started=$true;$ready=$false
    for($i=0;$i -lt 20;$i++) {
        try {
            $response=Invoke-WebRequest 'https://127.0.0.1:8081/' -SkipCertificateCheck -SkipHttpErrorCheck -TimeoutSec 2
            if([int]$response.StatusCode -in @(200,401)) {$ready=$true;break}
        }
        catch {if($i -eq 19) {throw}}
        Start-Sleep -Milliseconds 250
    }
    if(!$ready) {throw 'Retained Cosmos DB gateway readiness timed out.'}
    $env:OASIS_COSMOSDB_NATIVE='1'
    $testsStarted=[DateTime]::UtcNow
    if($InspectRetainedFixture -and $ReconcileRetainedRoutingFixture){throw 'Select only one diagnostic mode.'}
    $filter=if($InspectRetainedFixture){'TestCategory=Inspection'}elseif($ReconcileRetainedRoutingFixture){'TestCategory=Reconciliation'}else{'TestCategory!=Inspection&TestCategory!=Reconciliation'}
    $reportName=if($InspectRetainedFixture){'cosmos-retained-inspection-current.trx'}elseif($ReconcileRetainedRoutingFixture){'cosmos-routing-reconciliation-current.trx'}else{'cosmos-native-current.trx'}
    & dotnet test $project --no-build --no-restore --filter $filter --logger "trx;LogFileName=$reportName" --results-directory $work -v:q
    if($LASTEXITCODE) {throw 'Cosmos DB native test failed.'}
    $reportFile=Get-Item -LiteralPath (Join-Path $work $reportName) -ErrorAction Stop
    if($reportFile.LastWriteTimeUtc -lt $testsStarted){throw 'Cosmos DB native report is stale; no verification claim permitted.'}
    [xml]$report=Get-Content -LiteralPath $reportFile.FullName -Raw
    $counts=$report.TestRun.ResultSummary.Counters
    $expected=if($InspectRetainedFixture -or $ReconcileRetainedRoutingFixture){1}else{36}
    if([int]$counts.total -ne $expected -or [int]$counts.passed -ne $expected -or [int]$counts.notExecuted -ne 0) {throw 'Exact expected native Cosmos DB case count with no skips required.'}
}
finally {
    $env:OASIS_COSMOSDB_NATIVE=$previous
    $stopFailed=$false
    if($started) {& wsl.exe -d Ubuntu -u root -- podman stop --time 20 oasis-cosmos;if($LASTEXITCODE) {$stopFailed=$true}}
    Pop-Location;$lease.Dispose()
    if($stopFailed) {throw 'Owned Cosmos DB container did not stop.'}
}
