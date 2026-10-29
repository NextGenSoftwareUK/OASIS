#Requires -Version 7.5
$ErrorActionPreference='Stop'
$root=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
. (Join-Path $PSScriptRoot 'ReusableAzuriteHost.ps1')
& (Join-Path $PSScriptRoot 'Invoke-WithProviderBuildLease.ps1') -Owner cosmic-azureblob-cql -Operation {
    $state=Join-Path $root 'LocalState/COSMIC/AzureBlobCql'
    $results=Join-Path $root 'TestResults/AzureBlobCqlCurrent'
    New-Item -ItemType Directory -Force $state,$results | Out-Null
    $lease=[IO.File]::Open((Join-Path $state 'environment.lock'),'OpenOrCreate','ReadWrite','None')
    $hostHandle=$null
    $prior=$env:OASIS_AZURE_BLOB_CQL_STATE
    try {
        if(Test-Path (Join-Path $state 'demo-seed.pending.json')){throw 'Original setup pending; reconcile without replay.'}
        if((Test-Path (Join-Path $state 'demo-seed.json')) -and !(Test-Path (Join-Path $state 'azurite-data'))){throw 'Original native store missing; never replace it.'}
        $project=Join-Path $root 'Providers/Cloud/TestProjects/NextGenSoftware.OASIS.API.Providers.AzureStorageOASIS.IntegrationTests/NextGenSoftware.OASIS.API.Providers.AzureStorageOASIS.IntegrationTests.csproj'
        & dotnet build $project --no-restore /p:BuildProjectReferences=false /m:2 /nr:false /p:UseSharedCompilation=false -v:q -clp:ErrorsOnly
        if($LASTEXITCODE){throw 'Native consumer build failed.'}
        $hostHandle=Start-ReusableAzuriteHost -Workspace $results -DataDirectory (Join-Path $state 'azurite-data')
        $ready=$false
        for($i=0;$i -lt 60;$i++){
            if(Assert-ReusableAzuriteListener -HostHandle $hostHandle){$ready=$true;break}
            Start-Sleep -Milliseconds 250
        }
        if(!$ready){throw 'Owned Azurite readiness timed out; no replacement.'}
        $env:OASIS_AZURE_BLOB_CQL_STATE=$state
        & dotnet test $project --no-build --no-restore /m:2 /nr:false /p:UseSharedCompilation=false --filter FullyQualifiedName~AzureBlobCqlNativeTests --logger 'trx;LogFileName=azureblob-cql-native.trx' --results-directory $results -v:q
        if($LASTEXITCODE){throw 'Native Blob CQL failed; preserve source and pending journal.'}
        [xml]$report=Get-Content (Join-Path $results 'azureblob-cql-native.trx')
        if($report.TestRun.ResultSummary.Counters.total -ne '1' -or $report.TestRun.ResultSummary.Counters.passed -ne '1'){throw 'Exactly one native pass required.'}
    }
    finally {
        try {if($hostHandle){Stop-ReusableAzuriteHost -HostHandle $hostHandle}}
        finally {$env:OASIS_AZURE_BLOB_CQL_STATE=$prior;$lease.Dispose()}
    }
}
