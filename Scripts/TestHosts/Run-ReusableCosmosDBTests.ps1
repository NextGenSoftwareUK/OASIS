param([switch]$ReconcileScalarFixture,[switch]$InspectScalarFixture,[switch]$InspectRetainedFixture,[switch]$ReconcileRetainedRoutingFixture,[switch]$ReconcileCredentialFixture,[switch]$ReconcileRoleFixture)
$ErrorActionPreference='Stop'
$diagnosticModes=@($ReconcileScalarFixture,$InspectScalarFixture,$InspectRetainedFixture,$ReconcileRetainedRoutingFixture,$ReconcileCredentialFixture,$ReconcileRoleFixture) | Where-Object {$_}
if(@($diagnosticModes).Count -gt 1){throw 'Select only one explicit Cosmos DB diagnostic mode.'}
$root=(Resolve-Path (Join-Path $PSScriptRoot '../..')).Path
$work=Join-Path $root 'TestResults/CosmosDBCurrent'
$previousWork=$env:OASIS_COSMOSDB_WORK
New-Item -ItemType Directory -Force $work | Out-Null
$lease=[IO.File]::Open((Join-Path $work 'environment.lock'),[IO.FileMode]::OpenOrCreate,[IO.FileAccess]::ReadWrite,[IO.FileShare]::None)
$started=$false;$previous=$env:OASIS_COSMOSDB_NATIVE;$previousCredentialReconcile=$env:OASIS_COSMOS_CREDENTIAL_RECONCILE
try {
    Push-Location $root
    if($ReconcileCredentialFixture){
        if($InspectRetainedFixture -or $ReconcileRetainedRoutingFixture){throw 'Choose one explicit original recovery mode.'}
        $originals=@(Get-ChildItem -LiteralPath $work -Filter '*.pending.json')
        if($originals.Count -ne 1 -or $originals[0].Name -ne 'credentials.pending.json'){throw 'Exactly the reviewed original credential intent is required.'}
        $InspectRetainedFixture=$true;$env:OASIS_COSMOS_CREDENTIAL_RECONCILE='1'
    }
    if($InspectScalarFixture -or $ReconcileScalarFixture){
        $originals=@(Get-ChildItem -LiteralPath $work -Filter '*.pending.json')
        if($originals.Count -ne 1 -or $originals[0].Name -ne 'cosmos-query-scalar.pending.json' -or
            (Get-FileHash -LiteralPath $originals[0].FullName).Hash -ne '1E63DBEC39F0466F2CC2BC0487E8F93EF780F0D2A37D0CD5853BF51F85990BA8' -or
            (Get-FileHash -LiteralPath (Join-Path $work 'cosmos-native-current.trx')).Hash -ne '3692BD48BEA7461C07129AAE3649E964D472C8BDD1308A99FEC2B1D6405F234B'){
            throw 'Exactly the original failed scalar fixture/report required; no replay.'
        }
    }
    if(!$ReconcileScalarFixture -and !$InspectScalarFixture -and !$InspectRetainedFixture -and (Get-ChildItem -LiteralPath $work -Filter '*.pending.json')){throw 'Reconcile original Cosmos fixture journals before another run; do not replay or reset.'}
    $reportName=if($ReconcileScalarFixture){'cosmos-scalar-reconciliation-current.trx'}elseif($InspectScalarFixture){'cosmos-scalar-inspection-current.trx'}elseif($ReconcileRoleFixture){'cosmos-role-reconciliation-current.trx'}elseif($ReconcileCredentialFixture){'cosmos-credential-reconciliation-current.trx'}elseif($InspectRetainedFixture){'cosmos-retained-inspection-current.trx'}elseif($ReconcileRetainedRoutingFixture){'cosmos-routing-reconciliation-current.trx'}else{'cosmos-native-current.trx'}
    if(Test-Path -LiteralPath (Join-Path $work $reportName)) {throw 'Preserve and hash-verify the existing Cosmos DB report before another run; refusing to overwrite verification evidence.'}
    if(Get-NetTCPConnection -LocalPort 8082 -State Listen -ErrorAction SilentlyContinue) {throw 'Reserved Cosmos SDK outage endpoint8082 occupied.'}
    $state=(& wsl.exe -d Ubuntu -u root -- podman inspect oasis-cosmos --format '{{.State.Status}} {{.Image}}') -join ''
    if($LASTEXITCODE -ne 0 -or $state.Trim() -notin @('created b4edfeff93d70f2438ca55dbb14cdb8c6f111208d69c525baf62b7269ce44b56','exited b4edfeff93d70f2438ca55dbb14cdb8c6f111208d69c525baf62b7269ce44b56')) {throw 'Expected preserved stopped Cosmos DB container/image; refusing replacement or interference.'}
    $inspection=(& wsl.exe -d Ubuntu -u root -- podman inspect oasis-cosmos) -join "`n"
    if($LASTEXITCODE){throw 'Cosmos DB binding inspection failed.'}
    $ports=($inspection|ConvertFrom-Json)[0].HostConfig.PortBindings
    if(@($ports.PSObject.Properties).Count -ne 3){throw 'Unexpected Cosmos DB port mappings.'}
    foreach($port in @(1234,8080,8081)) {
        $binding=@($ports."$port/tcp")
        if($binding.Count -ne 1 -or $binding[0].HostIp -ne '127.0.0.1' -or $binding[0].HostPort -ne "$port"){throw 'Cosmos DB must use exact loopback-only mappings.'}
    }
    $project='Providers/Cloud/TestProjects/NextGenSoftware.OASIS.API.Providers.AzureCosmosDBOASIS.IntegrationTests/NextGenSoftware.OASIS.API.Providers.AzureCosmosDBOASIS.IntegrationTests.csproj'
    foreach($producer in @('Providers/Shared/NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage/NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage.csproj','Providers/Storage/NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS/NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS.csproj','Providers/Cloud/NextGenSoftware.OASIS.API.Providers.AzureCosmosDBOASIS/NextGenSoftware.OASIS.API.Providers.AzureCosmosDBOASIS.csproj',$project)) {
        & dotnet build $producer --no-restore --no-incremental /p:BuildProjectReferences=false /m:2 /nr:false /p:UseSharedCompilation=false -v:q -clp:ErrorsOnly
        if($LASTEXITCODE) {throw "Cached build failed: $producer"}
    }
    $consumer=Join-Path $root 'Providers/Cloud/TestProjects/NextGenSoftware.OASIS.API.Providers.AzureCosmosDBOASIS.IntegrationTests/bin/Debug/net10.0'
    & (Join-Path $PSScriptRoot 'Assert-ReusableProviderBuildIdentity.ps1') -RepositoryRoot $root -ConsumerOutput $consumer -VerifySourceDocuments
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
    $env:OASIS_COSMOSDB_WORK=$work
    $testsStarted=[DateTime]::UtcNow
    $filter=if($ReconcileScalarFixture){'TestCategory=ScalarReconciliation'}elseif($InspectScalarFixture){'TestCategory=ScalarInspection'}elseif($ReconcileRoleFixture){'TestCategory=RoleReconciliation'}elseif($InspectRetainedFixture){'TestCategory=Inspection'}elseif($ReconcileRetainedRoutingFixture){'TestCategory=Reconciliation'}else{'TestCategory!=Inspection&TestCategory!=Reconciliation&TestCategory!=RoleReconciliation&TestCategory!=ScalarInspection&TestCategory!=ScalarReconciliation'}
    & dotnet test $project --no-build --no-restore --filter $filter --logger "trx;LogFileName=$reportName" --results-directory $work -v:q
    if($LASTEXITCODE) {throw 'Cosmos DB native test failed.'}
    $reportFile=Get-Item -LiteralPath (Join-Path $work $reportName) -ErrorAction Stop
    if($reportFile.LastWriteTimeUtc -lt $testsStarted){throw 'Cosmos DB native report is stale; no verification claim permitted.'}
    [xml]$report=Get-Content -LiteralPath $reportFile.FullName -Raw
    $counts=$report.TestRun.ResultSummary.Counters
    $expected=if($ReconcileScalarFixture -or $InspectScalarFixture -or $InspectRetainedFixture -or $ReconcileRetainedRoutingFixture -or $ReconcileRoleFixture){1}else{62}
    if([int]$counts.total -ne $expected -or [int]$counts.passed -ne $expected -or [int]$counts.notExecuted -ne 0) {throw 'Exact expected native Cosmos DB case count with no skips required.'}
}
finally {
    $env:OASIS_COSMOSDB_NATIVE=$previous
    $env:OASIS_COSMOSDB_WORK=$previousWork
    $env:OASIS_COSMOS_CREDENTIAL_RECONCILE=$previousCredentialReconcile
    $stopFailed=$false
    if($started) {& wsl.exe -d Ubuntu -u root -- podman stop --time 20 oasis-cosmos;if($LASTEXITCODE) {$stopFailed=$true}}
    Pop-Location;$lease.Dispose()
    if($stopFailed) {throw 'Owned Cosmos DB container did not stop.'}
}
