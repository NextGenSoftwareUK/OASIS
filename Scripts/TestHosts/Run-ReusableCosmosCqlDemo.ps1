#Requires -Version 7.0
[CmdletBinding()]
param([switch]$Serve)
$ErrorActionPreference='Stop'
$root=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
$state=Join-Path $root 'LocalState/COSMIC/CosmosCqlCurrent'
$work=Join-Path $root 'TestResults/CosmosCqlCurrent'
$project='Providers/Cloud/TestProjects/NextGenSoftware.OASIS.API.Providers.AzureCosmosDBOASIS.IntegrationTests/NextGenSoftware.OASIS.API.Providers.AzureCosmosDBOASIS.IntegrationTests.csproj'

# This runner owns one bounded native acceptance run, not the storage conformance
# suite. Never replay a pending create intent or replace the retained emulator.
if(Test-Path -LiteralPath (Join-Path $state 'rows.pending.json')){throw 'Reconcile the original CQL fixture journal before creating another fixture.'}
New-Item -ItemType Directory -Force -Path $state,$work | Out-Null
& (Join-Path $PSScriptRoot 'Invoke-WithProviderBuildLease.ps1') -Owner cosmic-cql-cloud-demo -Operation {
    $started=$false
    $names=@('OASIS_COSMOS_CQL_NATIVE','OASIS_COSMOS_CQL_STATE','OASIS_COSMOS_CQL_REVIEWED_COUNT','OASIS_COSMOS_CQL_REVIEWED_PROJECTION','OASIS_COSMOS_CQL_REVIEWED_HTTP','OASIS_COSMOS_CQL_SERVE')
    $previous=@{}
    foreach($name in $names){$previous[$name]=[Environment]::GetEnvironmentVariable($name)}
    try{
        $inspection=(& wsl.exe -d Ubuntu -u root -- podman inspect oasis-cosmos) -join "`n"
        if($LASTEXITCODE){throw 'Retained Cosmos DB environment is unavailable; inspect the resource ledger before provisioning.'}
        $container=($inspection|ConvertFrom-Json)[0]
        if($container.State.Status -notin @('exited','created') -or $container.Image -ne 'b4edfeff93d70f2438ca55dbb14cdb8c6f111208d69c525baf62b7269ce44b56'){throw 'Retained emulator identity/state differs; preserve it.'}
        $ports=$container.HostConfig.PortBindings
        if(@($ports.PSObject.Properties).Count -ne 3){throw 'Unexpected emulator bindings.'}
        foreach($port in @(1234,8080,8081)){
            $binding=@($ports."$port/tcp")
            if($binding.Count -ne 1 -or $binding[0].HostIp -ne '127.0.0.1' -or $binding[0].HostPort -ne "$port"){throw 'Emulator bindings must be loopback-only.'}
        }
        foreach($producer in @('Providers/Shared/NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage/NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage.csproj','Providers/Storage/NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS/NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS.csproj','Providers/Cloud/NextGenSoftware.OASIS.API.Providers.AzureCosmosDBOASIS/NextGenSoftware.OASIS.API.Providers.AzureCosmosDBOASIS.csproj',$project)){
            & dotnet build (Join-Path $root $producer) --no-restore /p:BuildProjectReferences=false /m:2 /nr:false /p:UseSharedCompilation=false -v:q
            if($LASTEXITCODE){throw 'CQL cloud producer/consumer build failed.'}
        }
        $consumer=Join-Path $root 'Providers/Cloud/TestProjects/NextGenSoftware.OASIS.API.Providers.AzureCosmosDBOASIS.IntegrationTests/bin/Debug/net10.0'
        & (Join-Path $PSScriptRoot 'Assert-ReusableProviderBuildIdentity.ps1') -RepositoryRoot $root -ConsumerOutput $consumer -VerifySourceDocuments
        # Preserve prior raw evidence under its content hash before reusing the
        # fixed result filename. Never archive a pending native write outcome.
        $report=Join-Path $work 'cosmos-cql-demo-current.trx'
        if(Test-Path -LiteralPath $report){
            $archive=Join-Path $work ((Get-FileHash -LiteralPath $report).Hash+'.trx')
            if(!(Test-Path -LiteralPath $archive)){Copy-Item -LiteralPath $report -Destination $archive}
        }
        & wsl.exe -d Ubuntu -u root -- podman start oasis-cosmos
        if($LASTEXITCODE){throw 'Emulator startup failed.'}
        $started=$true;$ready=$false;$readinessFailure='No successful gateway response.'
        for($attempt=0;$attempt -lt 30;$attempt++){
            try{$response=Invoke-WebRequest 'https://127.0.0.1:8081/' -SkipCertificateCheck -SkipHttpErrorCheck -TimeoutSec 2;if([int]$response.StatusCode -in @(200,401)){$ready=$true;break};$readinessFailure='Gateway HTTP status '+[int]$response.StatusCode}catch{$readinessFailure=$_.Exception.Message}
            Start-Sleep -Milliseconds 500
        }
        if(!$ready){throw ('Emulator readiness deadline exceeded: '+$readinessFailure)}
        foreach($name in $names){[Environment]::SetEnvironmentVariable($name,$null)}
        $env:OASIS_COSMOS_CQL_NATIVE='1';$env:OASIS_COSMOS_CQL_STATE=$state
        if($Serve){$env:OASIS_COSMOS_CQL_SERVE='1';Write-Output 'Teaching page will bind http://127.0.0.1:5088/ after native verification. POST /demo/shutdown to finish.'}
        $began=[DateTime]::UtcNow
        & dotnet test (Join-Path $root $project) --no-build --no-restore --filter 'FullyQualifiedName~CqlNativeRowTests.JournaledNativeRowsRespectVisibilityWindowsAndBounds' --logger 'trx;LogFileName=cosmos-cql-demo-current.trx' --results-directory $work -v:q
        if($LASTEXITCODE){throw 'Native CQL acceptance failed; preserve fixture/journal, no automatic retry.'}
        if((Get-Item -LiteralPath $report).LastWriteTimeUtc -lt $began){throw 'Stale native report.'}
        [xml]$result=Get-Content -LiteralPath $report -Raw
        $counts=$result.TestRun.ResultSummary.Counters
        if([int]$counts.total -ne 1 -or [int]$counts.passed -ne 1 -or [int]$counts.notExecuted -ne 0){throw 'Expected one complete native acceptance test.'}
        if(Test-Path -LiteralPath (Join-Path $state 'rows.pending.json')){throw 'Native fixture teardown is incomplete.'}
        Write-Output ('Native Cosmos CQL PASS; report SHA256 '+(Get-FileHash -LiteralPath $report).Hash)
    }
    finally{
        foreach($name in $names){[Environment]::SetEnvironmentVariable($name,$previous[$name])}
        if($started){& wsl.exe -d Ubuntu -u root -- podman stop --time 20 oasis-cosmos;if($LASTEXITCODE){throw 'Owned emulator teardown failed.'}}
    }
}
