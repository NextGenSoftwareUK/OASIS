#Requires -Version 7.5
[CmdletBinding()]
param([switch]$RepairAcknowledgedLostRecord,[switch]$Federated,[switch]$Serve)
if($Serve -and !$Federated){throw 'Serve requires explicit federation.'}
if($Federated -and $RepairAcknowledgedLostRecord){throw 'Repair and federation are separate operations.'}
$ErrorActionPreference='Stop'
$root=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
$state=Join-Path $root 'LocalState/COSMIC/TronCqlDeclared'
$results=Join-Path $root 'TestResults/TronCqlCurrent'
$name='oasis-tron-tre'
$containerId='ef2843babd111bde2709159e6eaacf2350977cccee738d62d1bcbd8833c46a12'
$pin='docker.io/tronbox/tre@sha256:cbf2973095933e3ae3237767082e29add2f9b647d7a4aebdfb7a953c6e5656ba'
$genesis='0000000000000000c93baa76a4a508f798a96f59156d9eb17ecede8ec845df2f'
New-Item -ItemType Directory -Path $state,$results -Force | Out-Null
$lease=[IO.File]::Open((Join-Path $state 'runner.lock'),'OpenOrCreate','ReadWrite','None')
try {
    & (Join-Path $PSScriptRoot 'Invoke-WithProviderBuildLease.ps1') -Owner cosmic-tron-declared -Operation {
        function InspectOwned {
            $raw=& wsl -d Ubuntu -u root -- podman inspect $name
            if($LASTEXITCODE){throw 'Retained TRON container missing; no replacement.'}
            $item=@(($raw -join "`n") | ConvertFrom-Json)[0]
            if($item.Id -ne $containerId -or $item.ImageName -ne $pin -or $item.HostConfig.Memory -ne 3GB -or $item.HostConfig.MemorySwap -ne 3GB){throw 'Retained node identity/resource admission differs.'}
            foreach($port in @('9090/tcp','50051/tcp','50052/tcp')) {
                $bindings=@($item.HostConfig.PortBindings.$port)
                if($bindings.Count -ne 1 -or $bindings[0].HostIp -ne '127.0.0.1'){throw 'Native ports must remain loopback only.'}
            }
            return $item
        }
        $names=@('OASIS_TRON_CQL_DECLARED','OASIS_TRON_CQL_STATE','OASIS_TRON_CQL_STORAGE_KEY','OASIS_TRON_CQL_BYTECODE','OASIS_TRON_CQL_REPAIR','OASIS_MONGO_REPLICA_SET_CONNECTION','OASIS_TRON_MONGO_SERVE')
        $prior=@{};foreach($variable in $names){$prior[$variable]=[Environment]::GetEnvironmentVariable($variable)}
        $started=$false;$mining=$false;$ownedMongo=$null
        $miningHelper=((Join-Path $PSScriptRoot 'set-owned-tron-mining.sh').Replace('\','/'))
        $miningPath=((& wsl -d Ubuntu -- wslpath -a $miningHelper)-join '').Trim()
        if($LASTEXITCODE){throw 'Cannot resolve owned mining helper.'}
        # These observations are not replacement historical fences. Their absence
        # remains explicit; this runner never reads/replays an old signed attempt.
        $originals=@{}
        foreach($file in @('pending-native-outcome.json','independent-prepared-native-pending.json')) {
            $path=Join-Path $root ('TestResults/HyperDriveTron/'+$file)
            $originals[$path]=if(Test-Path $path){(Get-FileHash $path).Hash}else{$null}
        }
        try {
            if(Test-Path (Join-Path $state 'source.pending.json')){throw 'Reconcile original declared setup; never replay/reseed.'}
            if((InspectOwned).State.Status -ne 'exited'){throw 'Retained node in use; do not adopt it.'}
            if(Get-NetTCPConnection -State Listen -LocalPort 9090,50051,50052 -ErrorAction SilentlyContinue){throw 'Native endpoint occupied.'}
            $report=Join-Path $results $(if($Federated){'tron-mongo-federation.trx'}elseif($RepairAcknowledgedLostRecord){'tron-declared-repair.trx'}else{'tron-declared-native.trx'})
            if(Test-Path $report){
                [xml]$old=Get-Content $report -Raw
                if($old.TestRun.ResultSummary.Counters.total -ne '1' -or $old.TestRun.ResultSummary.Counters.passed -ne '1'){throw 'Preserve failed/incomplete native report.'}
                if(!(Test-Path (Join-Path $state 'demo-seed.json'))){throw 'Completed source manifest required before re-verification.'}
                $archive=Join-Path $results 'Archive';New-Item -ItemType Directory -Path $archive -Force | Out-Null
                $target=Join-Path $archive ((Get-FileHash $report).Hash+'-'+[IO.Path]::GetFileName($report))
                if(Test-Path $target){throw 'Preserve existing native archive.'}
                Move-Item -LiteralPath $report -Destination $target
            }
            $project=Join-Path $root 'Providers/Blockchain/TestProjects/NextGenSoftware.OASIS.API.Providers.TRONOASIS.IntegrationTests/NextGenSoftware.OASIS.API.Providers.TRONOASIS.IntegrationTests.csproj'
            if($Federated){
                if(!(Test-Path (Join-Path $state 'demo-seed.json')) -or (Test-Path (Join-Path $state 'mongo.pending.json'))){throw 'Existing source and no unfinished Mongo fixture required.'}
                $assets=Join-Path (Split-Path $project) 'obj/project.assets.json'
                if((Get-Content $assets -Raw) -notmatch 'NextGenSoftware.OASIS.API.Providers.MongoDBOASIS'){
                    & dotnet restore $project --source C:/Users/david/.nuget/packages /m:2 /nr:false /p:NuGetAudit=false -v:q
                    if($LASTEXITCODE){throw 'Offline paired restore failed.'}
                }
                & dotnet build (Join-Path $root 'Providers/Storage/NextGenSoftware.OASIS.API.Providers.MongoOASIS/NextGenSoftware.OASIS.API.Providers.MongoDBOASIS.csproj') --no-restore /p:BuildProjectReferences=false /m:2 /nr:false /p:UseSharedCompilation=false -v:q -clp:ErrorsOnly
                if($LASTEXITCODE){throw 'Mongo producer build failed.'}
            }
            & dotnet build $project --no-restore /p:BuildProjectReferences=false /m:2 /nr:false /p:UseSharedCompilation=false -v:q -clp:ErrorsOnly
            if($LASTEXITCODE){throw 'Declared fixture build failed before node startup.'}
            & node (Join-Path $PSScriptRoot 'compile-cosmic-tron-cql.cjs')
            if($LASTEXITCODE){throw 'Cached contract compilation failed.'}
            $admission=Join-Path $state 'declared-source-admission.json'
            if(!(Test-Path $admission)) {
                $record=@{scope='Independent declared TRON CQL contract';genesis=$genesis;containerId=$containerId;originalRecovery=$false;originalFenceObservations=$originals;historicalUnknownTransaction='db05ea6d3067038a56dfc75b64d2c46c32824282e8242335ce007d1e34a415bb'} | ConvertTo-Json -Depth 5
                $file=[IO.File]::Open($admission,'CreateNew','Write','Read')
                try{$bytes=[Text.Encoding]::UTF8.GetBytes($record);$file.Write($bytes);$file.Flush($true)}finally{$file.Dispose()}
            }
            $admitted=Get-Content $admission -Raw | ConvertFrom-Json
            if($admitted.scope -ne 'Independent declared TRON CQL contract' -or $admitted.genesis -ne $genesis -or $admitted.containerId -ne $containerId -or $admitted.originalRecovery -ne $false){throw 'Declared admission differs.'}
            $keyPath=Join-Path $state 'storage-key.dpapi'
            if(!(Test-Path $keyPath)) {
                if(Test-Path (Join-Path $state 'demo-seed.json')){throw 'Original declared-source key missing; never replace it.'}
                $key=[Security.Cryptography.RandomNumberGenerator]::GetBytes(32)
                try {
                    $file=[IO.File]::Open($keyPath,'CreateNew','Write','None')
                    try{$protected=[Security.Cryptography.ProtectedData]::Protect($key,$null,'CurrentUser');$file.Write($protected);$file.Flush($true)}finally{$file.Dispose()}
                }finally{[Security.Cryptography.CryptographicOperations]::ZeroMemory($key)}
            }
            $key=[Security.Cryptography.ProtectedData]::Unprotect([IO.File]::ReadAllBytes($keyPath),$null,'CurrentUser')
            try{if($key.Length -ne 32){throw 'Invalid declared storage key.'};$env:OASIS_TRON_CQL_STORAGE_KEY=[Convert]::ToBase64String($key)}finally{[Security.Cryptography.CryptographicOperations]::ZeroMemory($key)}
            $env:OASIS_TRON_CQL_DECLARED='create-or-read-explicit-separate-source';$env:OASIS_TRON_CQL_STATE=$state
            $env:OASIS_TRON_CQL_BYTECODE=Join-Path $results 'declared-store-bytecode.txt'
            # Existing launcher resumes finalized config and the SAME ledger.
            # No quickstart initialization, container creation or historical replay.
            $helper=((Join-Path $PSScriptRoot 'recover-owned-tron-test-node.sh').Replace('\','/'))
            $nativePath=((& wsl -d Ubuntu -- wslpath -a $helper)-join '').Trim()
            if($LASTEXITCODE){throw 'Cannot resolve retained-node launcher.'}
            $started=$true
            & wsl -d Ubuntu -u root -- bash $nativePath
            if($LASTEXITCODE){throw 'Retained node resume failed; no retry/reset.'}
            $ready=$false;$deadline=[DateTime]::UtcNow.AddSeconds(90)
            do {
                if((InspectOwned).State.Status -ne 'running'){throw 'Retained node exited during readiness.'}
                try {
                    $block=Invoke-RestMethod 'http://127.0.0.1:9090/wallet/getblockbynum' -Method Post -ContentType application/json -Body '{"num":0}' -TimeoutSec 2
                    if($block.blockID){if($block.blockID -ne $genesis){throw 'Retained genesis differs.'};$ready=$true}
                } catch [System.Net.Http.HttpRequestException] {} catch [System.OperationCanceledException] {}
                if(!$ready){Start-Sleep -Milliseconds 500}
            }while(!$ready -and [DateTime]::UtcNow -lt $deadline)
            if(!$ready){throw 'Retained-node readiness deadline exceeded; no restart.'}
            & wsl -d Ubuntu -u root -- bash $miningPath start
            if($LASTEXITCODE){throw 'Owned interval mining failed.'};$mining=$true
            if($Federated){
            $mongoRuntime = 'C:/Program Files/MongoDB/Server/8.3/bin/mongod.exe'
            $mongoData = Join-Path $root 'LocalState/COSMIC/mongo-data'
            if (!(Test-Path (Join-Path $mongoData 'WiredTiger'))) { throw 'Retained Mongo environment missing; no reset.' }
            if (Get-NetTCPConnection -State Listen -LocalPort 27029,5108 -ErrorAction SilentlyContinue) { throw 'Paired demo port occupied.' }
            if (((& $mongoRuntime --version) -join ' ') -notmatch 'v8\.3\.11\b') { throw 'Pinned Mongo runtime differs.' }
            $ownedMongo = Start-Process $mongoRuntime -ArgumentList @('--dbpath',$mongoData,'--bind_ip','127.0.0.1','--port','27029','--replSet','oasisRecoveryRs','--wiredTigerCacheSizeGB','0.25','--oplogSize','32','--logpath',(Join-Path $results 'mongo-service.log'),'--logappend') -WindowStyle Hidden -PassThru
            $mongoReady = $false
            for ($i=0;$i -lt 40;$i++) {
                if ($ownedMongo.HasExited) { throw 'Owned Mongo exited during startup.' }
                $listeners = Get-NetTCPConnection -State Listen -LocalPort 27029 -ErrorAction SilentlyContinue
                if ($listeners) {
                    if (@($listeners | Where-Object OwningProcess -ne $ownedMongo.Id).Count) { throw 'Mongo listener owner differs.' }
                    $mongoReady = $true; break
                }
                Start-Sleep -Milliseconds 500
            }
            if (!$mongoReady) { throw 'Owned Mongo startup deadline exceeded.' }
            $env:OASIS_MONGO_REPLICA_SET_CONNECTION = 'mongodb://127.0.0.1:27029/?replicaSet=oasisRecoveryRs&serverSelectionTimeoutMS=3000'
            $env:OASIS_TRON_MONGO_SERVE = if ($Serve) { '1' } else { '0' }
            }
            $selectedTest=if($Federated){'TronMongoCqlNativeTests'}elseif($RepairAcknowledgedLostRecord){$env:OASIS_TRON_CQL_REPAIR='repair-last-acknowledged-synthetic-record';'ExplicitRepairOfAcknowledgedSyntheticRecordLostAtShutdown'}else{'DeclaredLocalSourceUsesJournaledWritesAndNativeCqlReads'}
            if(Test-Path $report){throw 'Preserve existing native report before rerun.'}
            & dotnet test $project --no-build --no-restore --filter "FullyQualifiedName~$selectedTest" --logger "trx;LogFileName=$([IO.Path]::GetFileName($report))" --results-directory $results -v:q
            if($LASTEXITCODE){throw 'Declared source native test failed; preserve all outcomes.'}
            [xml]$proof=Get-Content $report -Raw
            if($proof.TestRun.ResultSummary.Counters.total -ne '1' -or $proof.TestRun.ResultSummary.Counters.passed -ne '1'){throw 'Exactly one passing native schedule required.'}
            if($Federated -and (Test-Path (Join-Path $state 'mongo.pending.json'))){throw 'Mongo fixture cleanup incomplete.'}
        } finally {
          try {
            foreach($variable in $names){[Environment]::SetEnvironmentVariable($variable,$prior[$variable])}
            if($started -and (InspectOwned).State.Status -eq 'running') {
                if($mining){
                    & wsl -d Ubuntu -u root -- bash $miningPath drain
                    if($LASTEXITCODE){throw 'Owned snapshot drain failed; preserve running node for inspection.'}
                }
                $helper=((Join-Path $PSScriptRoot 'stop-owned-tron-test-node.sh').Replace('\','/'))
                $nativePath=((& wsl -d Ubuntu -- wslpath -a $helper)-join '').Trim()
                if($LASTEXITCODE){throw 'Cannot resolve exact-owned shutdown.'}
                & wsl -d Ubuntu -u root -- bash $nativePath
                if($LASTEXITCODE -or (InspectOwned).State.Status -ne 'exited'){throw 'Owned TRON has not stopped; no restart/escalation.'}
            }
            foreach($entry in $originals.GetEnumerator()) {
                $now=if(Test-Path $entry.Key){(Get-FileHash $entry.Key).Hash}else{$null}
                if($now -ne $entry.Value){throw 'Historical fence observation changed; preserve and reconcile.'}
            }
          } finally {
            if($ownedMongo){
                if(!$ownedMongo.HasExited){$ownedMongo.Kill();if(!$ownedMongo.WaitForExit(30000)){throw 'Owned Mongo teardown incomplete.'}}
                $ownedMongo.Dispose()
            }
          }
        }
    }
} finally {$lease.Dispose()}
