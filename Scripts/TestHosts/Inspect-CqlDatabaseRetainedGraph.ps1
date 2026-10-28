#Requires -Version 7.0
param([switch]$Canonical)
$ErrorActionPreference='Stop'
$root=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
$state=Join-Path $root 'LocalState/COSMIC/Neo4jCurrent/cql-v1-web4-fixture.json'
$expected='892A8AF780ED445887ECBD6C191A7577B053872A13E5CF35FE592A56899894C5'
if((Get-FileHash -LiteralPath $state).Hash -ne $expected){throw 'Reviewed snapshot identity changed; inspect before another audit.'}
$snapshot=Get-Content -LiteralPath $state -Raw | ConvertFrom-Json
# Include every declared identity group, not only primary and scalar IDs.
$ids=@('ids','scalarIds','restIds','transportIds','recoveryIds' | ForEach-Object {$snapshot.$_} | ForEach-Object {([guid]$_).ToString('D')})
if(@($ids|Sort-Object -Unique).Count -ne $ids.Count){throw 'Duplicate fixture identity.'}
$work=Join-Path $root 'TestResults/Neo4jCurrent'
New-Item -ItemType Directory -Force -Path $work | Out-Null
& (Join-Path $PSScriptRoot 'Invoke-WithProviderBuildLease.ps1') -Owner cosmic-cql-readonly-graph-audit -Operation {
    $started=$false;$attached=$null
    $previousRoot=$env:OASIS_CQL_GRAPH_INSPECTION_ROOT
    try{
        $identity=(& wsl.exe -d Ubuntu -u root -- podman inspect oasis-neo4j --format '{{.State.Status}} {{.Image}}') -join ''
        if($LASTEXITCODE -or $identity.Trim() -ne 'exited 6e47503356fe6b0e8f3ba48afc714ed95475d0c32ac97c62b30737af2e8846bd'){throw 'Expected retained stopped graph environment.'}
        if($Canonical){
            $project=Join-Path $root 'Providers/Storage/TestProjects/NextGenSoftware.OASIS.API.Providers.Neo4jOASIS.IntegrationTests/NextGenSoftware.OASIS.API.Providers.Neo4jOASIS.IntegrationTests.csproj'
            & dotnet build $project --no-restore /p:BuildProjectReferences=false /m:2 /nr:false /p:UseSharedCompilation=false -v:q
            if($LASTEXITCODE){throw 'Typed inspection consumer build failed.'}
            & (Join-Path $PSScriptRoot 'Assert-ReusableProviderBuildIdentity.ps1') -RepositoryRoot $root -ConsumerOutput (Join-Path (Split-Path $project) 'bin/Debug/net10.0') -VerifySourceDocuments
            $report=Join-Path $work 'cql-retained-canonical-inspection.json'
            if(Test-Path -LiteralPath $report){Move-Item -LiteralPath $report -Destination (Join-Path $work ((Get-FileHash $report).Hash+'.json'))}
            $trx=Join-Path $work 'cql-retained-canonical-inspection.trx'
            if(Test-Path -LiteralPath $trx){Copy-Item -LiteralPath $trx -Destination (Join-Path $work ((Get-FileHash $trx).Hash+'.trx'))}
        }
        $attached=Start-Process -FilePath wsl.exe -ArgumentList @('-d','Ubuntu','-u','root','--','podman','start','--attach','oasis-neo4j') -WindowStyle Hidden -PassThru -RedirectStandardOutput (Join-Path $work 'graph-inspection-service.log') -RedirectStandardError (Join-Path $work 'graph-inspection-service.err.log')
        $started=$true
        $ready=$false
        for($attempt=0;$attempt -lt 60;$attempt++){
            if($attached.HasExited){throw 'Owned graph attachment exited.'}
            & wsl.exe -d Ubuntu -u root -- podman exec oasis-neo4j cypher-shell -u neo4j -p oasis-local-test-only 'RETURN 1;' 2>$null | Out-Null
            if(!$LASTEXITCODE){$ready=$true;break}
            Start-Sleep -Milliseconds 500
        }
        if(!$ready){throw 'Graph readiness deadline exceeded; no inspection submitted.'}
        if($Canonical){
            $env:OASIS_CQL_GRAPH_INSPECTION_ROOT=$root
            $began=[DateTime]::UtcNow
            & dotnet test $project --no-build --no-restore --filter 'FullyQualifiedName~TypedDriverValidatesRetainedCanonicalVersionsAndReceiptBindingsWithoutWrites' --logger 'trx;LogFileName=cql-retained-canonical-inspection.trx' --results-directory $work -v:q
            if($LASTEXITCODE){throw 'Typed inspection failed; preserve evidence and native state.'}
            if((Get-Item -LiteralPath $trx).LastWriteTimeUtc -lt $began){throw 'Stale typed inspection report.'}
            [xml]$result=Get-Content -LiteralPath $trx -Raw
            $counts=$result.TestRun.ResultSummary.Counters
            if([int]$counts.total -ne 1 -or [int]$counts.passed -ne 1 -or [int]$counts.notExecuted -ne 0){throw 'Expected exactly one complete typed audit.'}
            Write-Output ('Typed canonical audit PASS; SHA256='+(Get-FileHash $report).Hash)
            return
        }
        function Read-CsvQuery([string]$query){
            # Only fixed read-only statements below enter this local audit owner.
            $rows=& wsl.exe -d Ubuntu -u root -- podman exec oasis-neo4j cypher-shell -u neo4j -p oasis-local-test-only --format plain $query
            if($LASTEXITCODE){throw 'Read-only graph inspection failed.'}
            @($rows|ConvertFrom-Csv)
        }
        $holons=@(Read-CsvQuery 'MATCH (h:Holon) RETURN h.Id AS id ORDER BY id;')
        $history=@(Read-CsvQuery "MATCH (v:OasisRecordVersion {Kind:'Holon'}) RETURN v.RecordId AS id, count(v) AS versions ORDER BY id;")
        $receipts=@(Read-CsvQuery 'MATCH (r:CosmicWriteReceipt) RETURN r.OperationId AS operation, count(r) AS records ORDER BY operation;')
        $live=@($holons|ForEach-Object {([guid]$_.id).ToString('D')})
        $operations=@($snapshot.sharingOperations)
        $report=[ordered]@{
            snapshotSha256=$expected;observedUtc=[DateTime]::UtcNow.ToString('O');stage=$snapshot.stage
            declaredIds=$ids.Count;liveHolons=$holons.Count
            missingIds=@($ids|Where-Object {$_ -notin $live})
            unexpectedIds=@($live|Where-Object {$_ -notin $ids})
            holons=$holons;history=$history;receipts=$receipts
            canonicalVersionsVerified=$false
            unexpectedHistoryIds=@($history|Where-Object {$_.id -notin $ids})
            unexpectedReceiptOperations=@($receipts|Where-Object {$_.operation -notin $operations})
            sqlitePathOutsideWorktree=$true
            cleanupAuthorized=$false
        }
        $path=Join-Path $work 'cql-retained-graph-inspection.json'
        if(Test-Path -LiteralPath $path){
            $archive=Join-Path $work ((Get-FileHash -LiteralPath $path).Hash+'.json')
            if(!(Test-Path -LiteralPath $archive)){Copy-Item -LiteralPath $path -Destination $archive}
        }
        $report|ConvertTo-Json -Depth 8|Set-Content -LiteralPath $path -Encoding utf8
        Write-Output ('Read-only graph audit: declared='+$ids.Count+' live='+$holons.Count+' missing='+$report.missingIds.Count+' unexpected='+$report.unexpectedIds.Count+' SHA256='+(Get-FileHash $path).Hash)
    }
    finally{
        $env:OASIS_CQL_GRAPH_INSPECTION_ROOT=$previousRoot
        if($started){& wsl.exe -d Ubuntu -u root -- podman stop --time 20 oasis-neo4j;if($LASTEXITCODE){throw 'Owned graph teardown failed.'}}
        if($null -ne $attached){$attached.WaitForExit(10000)|Out-Null;$attached.Dispose()}
    }
}
