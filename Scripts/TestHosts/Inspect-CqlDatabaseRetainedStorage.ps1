#Requires -Version 7.0
$ErrorActionPreference='Stop'
$root=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
$data=Join-Path $root 'LocalState/COSMIC/mongo-data'
$work=Join-Path $root 'TestResults/Neo4jCurrent'
$runtime='C:/Program Files/MongoDB/Server/8.3/bin/mongod.exe'
if(!(Test-Path -LiteralPath $data -PathType Container)){throw 'Original retained Mongo data required for outcome inspection; a replacement cannot prove recovery.'}
New-Item -ItemType Directory -Force -Path $work | Out-Null
& (Join-Path $PSScriptRoot 'Invoke-WithProviderBuildLease.ps1') -Owner cosmic-cql-readonly-storage-audit -Operation {
    $owned=$null;$previous=$env:OASIS_CQL_STORAGE_INSPECTION_ROOT
    try{
        if(Get-NetTCPConnection -State Listen -LocalPort 27029 -ErrorAction SilentlyContinue){throw 'Retained Mongo port already owned; no interference.'}
        $version=(& $runtime --version) -join ' '
        if($LASTEXITCODE -or $version -notmatch 'v8\.3\.11\b'){throw 'Expected installed Mongo8.3.11 runtime.'}
        $project=Join-Path $root 'Providers/Storage/TestProjects/NextGenSoftware.OASIS.API.Providers.Neo4jOASIS.IntegrationTests/NextGenSoftware.OASIS.API.Providers.Neo4jOASIS.IntegrationTests.csproj'
        & dotnet build $project --no-restore /p:BuildProjectReferences=false /m:2 /nr:false /p:UseSharedCompilation=false -v:q
        if($LASTEXITCODE){throw 'Storage inspection consumer build failed.'}
        & (Join-Path $PSScriptRoot 'Assert-ReusableProviderBuildIdentity.ps1') -RepositoryRoot $root -ConsumerOutput (Join-Path (Split-Path $project) 'bin/Debug/net10.0') -VerifySourceDocuments
        foreach($name in @('cql-retained-storage-inspection.json','cql-retained-storage-inspection.trx')){
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
        # Existing replica configuration is reused; no replSetInitiate, schema
        # initialization, fixture writes or recovery operations are submitted.
        $env:OASIS_CQL_STORAGE_INSPECTION_ROOT=$root
        $began=[DateTime]::UtcNow
        & dotnet test $project --no-build --no-restore --filter 'FullyQualifiedName~RetainedMongoAndLocalSqliteInventoryPreservesDataAndReportsDurableOutcomes' --logger 'trx;LogFileName=cql-retained-storage-inspection.trx' --results-directory $work -v:q
        if($LASTEXITCODE){throw 'Retained storage inspection failed; preserve data and evidence.'}
        $trx=Join-Path $work 'cql-retained-storage-inspection.trx'
        if((Get-Item $trx).LastWriteTimeUtc -lt $began){throw 'Stale storage inspection report.'}
        [xml]$result=Get-Content -LiteralPath $trx -Raw
        $counts=$result.TestRun.ResultSummary.Counters
        if([int]$counts.total -ne 1 -or [int]$counts.passed -ne 1 -or [int]$counts.notExecuted -ne 0){throw 'Expected exactly one complete storage audit.'}
        $report=Join-Path $work 'cql-retained-storage-inspection.json'
        $inventory=Get-Content -LiteralPath $report -Raw|ConvertFrom-Json
        Write-Output ('Retained storage inventory complete; SHA256='+(Get-FileHash $report).Hash)
        if(!$inventory.usageJournalBindingsVerified){Write-Warning ('Unresolved usage identities: '+@($inventory.unboundUsageIds).Count+'; cleanup remains unauthorized.')}
    }
    finally{
        $env:OASIS_CQL_STORAGE_INSPECTION_ROOT=$previous
        if($null -ne $owned){
            if(!$owned.HasExited){$owned.Kill();if(!$owned.WaitForExit(30000)){throw 'Owned Mongo termination deadline exceeded.'}}
            $owned.Dispose()
        }
    }
}
