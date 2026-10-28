#Requires -Version 7.0
param([switch]$ReconcileFixture,[switch]$MongoEntityProjection)
if($ReconcileFixture -and $MongoEntityProjection){throw 'Choose one exclusive native task.'}
$ErrorActionPreference='Stop'
$root=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
$data=Join-Path $root 'LocalState/COSMIC/mongo-data'
$work=Join-Path $root 'TestResults/Neo4jCurrent'
$runtime='C:/Program Files/MongoDB/Server/8.3/bin/mongod.exe'
if(!(Test-Path -LiteralPath $data -PathType Container)){throw 'Original retained Mongo data required for outcome inspection; a replacement cannot prove recovery.'}
New-Item -ItemType Directory -Force -Path $work | Out-Null
& (Join-Path $PSScriptRoot 'Invoke-WithProviderBuildLease.ps1') -Owner cosmic-cql-readonly-storage-audit -Operation {
    $owned=$null;$attached=$null;$previous=$env:OASIS_CQL_STORAGE_INSPECTION_ROOT;$previousReconcile=$env:OASIS_CQL_RECONCILE_FIXTURE;$previousProjection=$env:OASIS_CQL_MONGO_ENTITY_PROJECTION_ROOT
    try{
        if(Get-NetTCPConnection -State Listen -LocalPort 27029 -ErrorAction SilentlyContinue){throw 'Retained Mongo port already owned; no interference.'}
        $version=(& $runtime --version) -join ' '
        if($LASTEXITCODE -or $version -notmatch 'v8\.3\.11\b'){throw 'Expected installed Mongo8.3.11 runtime.'}
        $project=Join-Path $root 'Providers/Storage/TestProjects/NextGenSoftware.OASIS.API.Providers.Neo4jOASIS.IntegrationTests/NextGenSoftware.OASIS.API.Providers.Neo4jOASIS.IntegrationTests.csproj'
        if($MongoEntityProjection){
            & dotnet build (Join-Path $root 'Providers/Storage/NextGenSoftware.OASIS.API.Providers.MongoOASIS/NextGenSoftware.OASIS.API.Providers.MongoDBOASIS.csproj') --no-restore /p:BuildProjectReferences=false /m:2 /nr:false /p:UseSharedCompilation=false -v:q
            if($LASTEXITCODE){throw 'Mongo entity projection producer build failed.'}
        }
        & dotnet build $project --no-restore /p:BuildProjectReferences=false /m:2 /nr:false /p:UseSharedCompilation=false -v:q
        if($LASTEXITCODE){throw 'Storage inspection consumer build failed.'}
        & (Join-Path $PSScriptRoot 'Assert-ReusableProviderBuildIdentity.ps1') -RepositoryRoot $root -ConsumerOutput (Join-Path (Split-Path $project) 'bin/Debug/net10.0') -VerifySourceDocuments
        $reportNames=if($MongoEntityProjection){@('mongo-entity-projection-native.json','mongo-entity-projection-native.trx')}elseif($ReconcileFixture){@('cql-retained-fixture-reconciliation.trx')}else{@('cql-retained-storage-inspection.json','cql-retained-storage-inspection.trx')}
        foreach($name in $reportNames){
            $path=Join-Path $work $name
            if(Test-Path -LiteralPath $path){Move-Item -LiteralPath $path -Destination (Join-Path $work ((Get-FileHash $path).Hash+[IO.Path]::GetExtension($path)))}
        }
        $owned=Start-Process -FilePath $runtime -ArgumentList @('--dbpath',$data,'--bind_ip','127.0.0.1','--port','27029','--replSet','oasisRecoveryRs','--wiredTigerCacheSizeGB','0.25','--oplogSize','32','--logpath',(Join-Path $work 'mongo-inspection-service.log'),'--logappend') -WindowStyle Hidden -PassThru
        $ready=$false
        for($attempt=0;$attempt -lt 60;$attempt++){
            if($owned.HasExited){throw 'Retained Mongo exited before readiness.'}
            $listener=Get-NetTCPConnection -State Listen -LocalPort 27029 -ErrorAction SilentlyContinue
            if($listener){if(@($listener|Where-Object OwningProcess -ne $owned.Id).Count){throw 'Mongo listener ownership differs.'};$ready=$true;break}
            Start-Sleep -Milliseconds 500
        }
        if(!$ready){throw 'Mongo listener readiness deadline exceeded.'}
        if($ReconcileFixture){
            $identity=(& wsl.exe -d Ubuntu -u root -- podman inspect oasis-neo4j --format '{{.State.Status}} {{.Image}}') -join ''
            if($LASTEXITCODE -or $identity.Trim() -ne 'exited 6e47503356fe6b0e8f3ba48afc714ed95475d0c32ac97c62b30737af2e8846bd'){throw 'Expected retained stopped graph environment.'}
            $attached=Start-Process -FilePath wsl.exe -ArgumentList @('-d','Ubuntu','-u','root','--','podman','start','--attach','oasis-neo4j') -WindowStyle Hidden -PassThru -RedirectStandardOutput (Join-Path $work 'reconciliation-neo4j.log') -RedirectStandardError (Join-Path $work 'reconciliation-neo4j.err.log')
            $graphReady=$false
            for($attempt=0;$attempt -lt 60;$attempt++){
                if($attached.HasExited){throw 'Owned graph attachment exited.'}
                & wsl.exe -d Ubuntu -u root -- podman exec oasis-neo4j cypher-shell -u neo4j -p oasis-local-test-only 'RETURN 1;' 2>$null|Out-Null
                if(!$LASTEXITCODE){$graphReady=$true;break}
                Start-Sleep -Milliseconds 500
            }
            if(!$graphReady){throw 'Graph readiness deadline exceeded.'}
            $env:OASIS_CQL_RECONCILE_FIXTURE='892A8AF7-reviewed-fixture'
        }else{$env:OASIS_CQL_RECONCILE_FIXTURE=$null}
        # Existing replica configuration is reused; no replSetInitiate, schema
        # initialization, fixture writes or recovery operations are submitted.
        $env:OASIS_CQL_STORAGE_INSPECTION_ROOT=$root
        $env:OASIS_CQL_MONGO_ENTITY_PROJECTION_ROOT=if($MongoEntityProjection){$root}else{$null}
        $began=[DateTime]::UtcNow
        $test=if($MongoEntityProjection){'FullyQualifiedName~NativeMongoEntityProjectionPreservesStagesAndProofBeforeDtoConstruction'}elseif($ReconcileFixture){'FullyQualifiedName~ReconcileReviewedRetainedThreeDatabaseFixtureAfterArchivingEvidence'}else{'FullyQualifiedName~RetainedMongoAndLocalSqliteInventoryPreservesDataAndReportsDurableOutcomes'}
        $reportName=if($MongoEntityProjection){'mongo-entity-projection-native.trx'}elseif($ReconcileFixture){'cql-retained-fixture-reconciliation.trx'}else{'cql-retained-storage-inspection.trx'}
        & dotnet test $project --no-build --no-restore --filter $test --logger "trx;LogFileName=$reportName" --results-directory $work -v:q
        if($LASTEXITCODE){throw 'Retained storage inspection failed; preserve data and evidence.'}
        $trx=Join-Path $work $reportName
        if((Get-Item $trx).LastWriteTimeUtc -lt $began){throw 'Stale storage inspection report.'}
        [xml]$result=Get-Content -LiteralPath $trx -Raw
        $counts=$result.TestRun.ResultSummary.Counters
        if([int]$counts.total -ne 1 -or [int]$counts.passed -ne 1 -or [int]$counts.notExecuted -ne 0){throw 'Expected exactly one complete storage audit.'}
        if($MongoEntityProjection){
            $proof=Get-Content -LiteralPath (Join-Path $work 'mongo-entity-projection-native.json') -Raw|ConvertFrom-Json
            if($proof.checks -ne 10 -or !$proof.exactCleanupVerified){throw 'Native entity projection proof incomplete.'}
            Write-Output 'Mongo entity projection: ten native cases and fixture cleanup pass.'
            return
        }
        if($ReconcileFixture){
            $journal=Join-Path $root 'LocalState/COSMIC/Neo4jCurrent/ReconciledFixture892A8AF7/reconciliation.json'
            $completion=Get-Content -LiteralPath $journal -Raw|ConvertFrom-Json
            if($completion.phase -ne 'completed-original-and-local-sqlite-preserved' -or !$completion.graphCommitted -or @($completion.removedDatabases).Count -ne 2){throw 'Fixture reconciliation is incomplete; preserve journal, no retry.'}
            Write-Output ('Owned fixture reconciliation complete; journal SHA256='+(Get-FileHash $journal).Hash)
            return
        }
        $report=Join-Path $work 'cql-retained-storage-inspection.json'
        $inventory=Get-Content -LiteralPath $report -Raw|ConvertFrom-Json
        Write-Output ('Retained storage inventory complete; SHA256='+(Get-FileHash $report).Hash)
        if(!$inventory.usageJournalBindingsVerified){Write-Warning ('Unresolved usage identities: '+@($inventory.unboundUsageIds).Count+'; cleanup remains unauthorized.')}
    }
    finally{
        $env:OASIS_CQL_STORAGE_INSPECTION_ROOT=$previous
        $env:OASIS_CQL_RECONCILE_FIXTURE=$previousReconcile
        $env:OASIS_CQL_MONGO_ENTITY_PROJECTION_ROOT=$previousProjection
        try{
            if($null -ne $attached){
                & wsl.exe -d Ubuntu -u root -- podman stop --time 20 oasis-neo4j
                if($LASTEXITCODE){throw 'Owned graph teardown failed.'}
                $attached.WaitForExit(10000)|Out-Null;$attached.Dispose()
            }
        }finally{
            if($null -ne $owned){
                if(!$owned.HasExited){$owned.Kill();if(!$owned.WaitForExit(30000)){throw 'Owned Mongo termination deadline exceeded.'}}
                $owned.Dispose()
            }
        }
    }
}
