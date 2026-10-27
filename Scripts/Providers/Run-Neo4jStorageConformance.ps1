#Requires -Version 7.0
[CmdletBinding()]
param()
$ErrorActionPreference='Stop'
$taskRoot=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
$work=Join-Path $taskRoot 'TestResults/Neo4jCurrent'
foreach($mode in @('async','sync')){
    $report=Join-Path $taskRoot ('TestResults/ProviderConformanceCurrent/Neo4jOASIS/'+$mode+'/report.json')
    if((Test-Path -LiteralPath $report) -or (Test-Path -LiteralPath ($report+'.next'))){throw 'Preserve original canonical Neo4j fixture/report before any new write.'}
}
& (Join-Path $taskRoot 'Scripts/TestHosts/Invoke-WithProviderBuildLease.ps1') -Owner 'Neo4j canonical native storage breadth' -Operation {
    New-Item -ItemType Directory -Path $work -Force | Out-Null
    $environmentLease=[IO.File]::Open((Join-Path $work 'environment.lock'),[IO.FileMode]::OpenOrCreate,[IO.FileAccess]::ReadWrite,[IO.FileShare]::None)
    $started=$false;$prior=$env:OASIS_OWNED_NEO4J_URI
    try{
        $state=(& wsl -d Ubuntu -u root -- podman inspect oasis-neo4j --format '{{.Id}} {{.State.Status}} {{.Image}}') -join ''
        if($LASTEXITCODE -or $state -ne '490d5e2c9bbbdf3686e0bd120ee8799cc17ab6b84bfa74cf9e5d9faf3183727b exited 6e47503356fe6b0e8f3ba48afc714ed95475d0c32ac97c62b30737af2e8846bd'){throw 'Expected the same stopped retained Neo4j container/image; no replacement or interference.'}
        $projects=@(
            'Providers/Shared/NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage/NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage.csproj',
            'Providers/Storage/NextGenSoftware.OASIS.API.Providers.Neo4jOASIS/NextGenSoftware.OASIS.API.Providers.Neo4jOASIS.csproj',
            'Providers/Storage/TestProjects/NextGenSoftware.OASIS.API.Providers.Neo4jOASIS.IntegrationTests/NextGenSoftware.OASIS.API.Providers.Neo4jOASIS.IntegrationTests.csproj')
        foreach($relative in $projects){
            $project=Join-Path $taskRoot $relative
            & dotnet restore $project --source (Join-Path $env:USERPROFILE '.nuget/packages') --source 'C:/Program Files/dotnet/library-packs' /p:RestoreRecursive=false /p:NuGetAudit=false /p:IncludeCoreStorageConformance=true /m:2 /nr:false -v:q
            if($LASTEXITCODE){throw 'Required cached Neo4j metadata missing; no automatic download.'}
            & dotnet build $project --no-restore /p:IncludeCoreStorageConformance=true /p:BuildProjectReferences=false /m:2 /nr:false /p:UseSharedCompilation=false -v:q -clp:ErrorsOnly
            if($LASTEXITCODE){throw 'Neo4j conformance producer/consumer build failed.'}
        }
        $neoOutput=Join-Path (Split-Path $project) 'bin/Debug/net10.0'
        . (Join-Path $taskRoot 'Scripts/TestHosts/Assert-ReusableProviderBuildIdentity.ps1') -RepositoryRoot $taskRoot -ConsumerOutput $neoOutput -VerifySourceDocuments
        foreach($name in @('NextGenSoftware.OASIS.API.Providers.Neo4jOASIS','NextGenSoftware.OASIS.API.Providers.Neo4jOASIS.IntegrationTests')){
            Write-Output ($name+': sources='+(Assert-SourceDocuments (Join-Path $neoOutput ($name+'.dll')) $taskRoot))
        }
        & wsl -d Ubuntu -u root -- podman start oasis-neo4j
        if($LASTEXITCODE){throw 'Retained Neo4j start failed.'};$started=$true
        $deadline=[DateTime]::UtcNow.AddSeconds(60);$ready=$false
        do{
            & wsl -d Ubuntu -u root -- podman exec oasis-neo4j cypher-shell -u neo4j -p oasis-local-test-only 'RETURN 1;' *> $null
            if(!$LASTEXITCODE){$ready=$true;break};Start-Sleep -Milliseconds 500
        }while([DateTime]::UtcNow -lt $deadline)
        if(!$ready){throw 'Retained Neo4j readiness failed; no test writes dispatched.'}
        $env:OASIS_OWNED_NEO4J_URI='bolt://127.0.0.1:17687'
        $results=Join-Path $taskRoot 'TestResults/ProviderConformanceCurrent'
        if(Test-Path -LiteralPath (Join-Path $results 'neo-core-current.trx')){throw 'Preserve prior Neo4j test report.'}
        & dotnet test $project --no-build --no-restore /p:IncludeCoreStorageConformance=true --filter 'TestCategory=Neo4jCoreStorageConformance' --logger 'trx;LogFileName=neo-core-current.trx' --results-directory $results -v:q
        $testExit=$LASTEXITCODE
        & python -B (Join-Path $PSScriptRoot 'record_conformance_results.py') --providers Neo4jOASIS
        if($LASTEXITCODE){throw 'Neo4j canonical report registration failed.'}
        & python -B (Join-Path $PSScriptRoot 'update_provider_matrix.py')
        if($LASTEXITCODE){throw 'Neo4j matrix update failed.'}
        if($testExit){throw 'Native Neo4j conformance failed; preserve graph/report, no automatic cleanup or replay.'}
        [xml]$trx=Get-Content (Join-Path $results 'neo-core-current.trx')
        if($trx.TestRun.ResultSummary.Counters.total -ne 2 -or $trx.TestRun.ResultSummary.Counters.passed -ne 2 -or $trx.TestRun.ResultSummary.Counters.notExecuted -ne 0){throw 'Both actual native Neo4j modes required, no skips.'}
    }finally{
        $env:OASIS_OWNED_NEO4J_URI=$prior
        try{if($started){& wsl -d Ubuntu -u root -- podman stop --time 20 oasis-neo4j;if($LASTEXITCODE){throw 'Owned Neo4j did not stop; inspect before reuse.'}}}
        finally{$environmentLease.Dispose()}
    }
}
