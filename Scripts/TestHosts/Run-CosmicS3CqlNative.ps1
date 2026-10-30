#Requires -Version 7.5
[CmdletBinding()]
param([switch]$Serve)
$ErrorActionPreference='Stop'
$root=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
$state=Join-Path $root 'LocalState/COSMIC/S3Cql'
$results=Join-Path $root 'TestResults/AwsS3CqlCurrent'
$image='3d5ffffec36e6efdda5ed906939f1b4d0976f03fc33d1ea31ddcc67833089691'
$container='oasis-cosmic-cql-s3'
$project=Join-Path $root 'Providers/Cloud/TestProjects/NextGenSoftware.OASIS.API.Providers.AWSOASIS.UnitTests/NextGenSoftware.OASIS.API.Providers.AWSOASIS.UnitTests.csproj'
New-Item -ItemType Directory -Path $state,$results -Force | Out-Null
# Separate from fixture-owned environment.lock; held through teardown.
$lease=[IO.File]::Open((Join-Path $state 'runner.lock'),'OpenOrCreate','ReadWrite','None')
try {
    foreach($pending in @('demo-seed.pending.json','mongo.pending.json')) {
        if(Test-Path (Join-Path $state $pending)){throw 'Reconcile original fixture before starting; no replay/reseed.'}
    }
    & (Join-Path $PSScriptRoot 'Invoke-WithProviderBuildLease.ps1') -Owner cosmic-s3-cql -Operation {
        $names=@('OASIS_S3_CQL_NATIVE','OASIS_S3_CQL_PAIRED','OASIS_S3_CQL_STATE','OASIS_S3_CQL_ENDPOINT','OASIS_S3_CQL_BUCKET','OASIS_S3_CQL_STORAGE_KEY','MINIO_ACCESS_KEY','MINIO_SECRET_KEY','OASIS_MONGO_REPLICA_SET_CONNECTION','OASIS_S3_MONGO_SERVE')
        $prior=@{};foreach($name in $names){$prior[$name]=[Environment]::GetEnvironmentVariable($name)}
        $started=$false;$mongo=$null
        function NativeCase([string]$filter,[string]$report) {
            $path=Join-Path $results $report
            if(Test-Path $path){
                [xml]$old=Get-Content $path -Raw
                if($old.TestRun.ResultSummary.Counters.total -ne '1' -or $old.TestRun.ResultSummary.Counters.passed -ne '1'){throw 'Preserve failed/incomplete native report.'}
                $archive=Join-Path $results 'Archive';New-Item -ItemType Directory -Path $archive -Force | Out-Null
                $target=Join-Path $archive ((Get-FileHash $path).Hash+'-'+$report)
                if(Test-Path $target){throw 'Preserve existing archive.'}
                Move-Item -LiteralPath $path -Destination $target
            }
            & dotnet test $project --no-build --no-restore --filter $filter --logger "trx;LogFileName=$report" --results-directory $results -v:q
            if($LASTEXITCODE){throw 'Native case failed; preserve fixture/key/data for reconciliation.'}
            [xml]$proof=Get-Content $path -Raw
            if($proof.TestRun.ResultSummary.Counters.total -ne '1' -or $proof.TestRun.ResultSummary.Counters.passed -ne '1'){throw 'Exactly one executed passing native case required.'}
        }
        function StartStore {
            & wsl -d Ubuntu -u root -- podman start $container | Out-Null
            if($LASTEXITCODE){throw 'Owned S3-compatible store failed to start.'}
            $deadline=[DateTime]::UtcNow.AddSeconds(30)
            do {
                try {$reply=Invoke-WebRequest 'http://127.0.0.1:19100/minio/health/ready' -TimeoutSec 2;if($reply.StatusCode -eq 200){return}}
                catch [System.Net.Http.HttpRequestException] {}
                catch [System.OperationCanceledException] {}
                Start-Sleep -Milliseconds 250
            } while([DateTime]::UtcNow -lt $deadline)
            throw 'Owned compatible-service readiness deadline exceeded.'
        }
        try {
            if(Get-NetTCPConnection -State Listen -LocalPort 19100,19101,27029,5107 -ErrorAction SilentlyContinue){throw 'Owned demo ports occupied; do not adopt another operation.'}
            & wsl -d Ubuntu -u root -- podman image exists $image
            if($LASTEXITCODE){throw 'Pinned cached image required; no download.'}
            foreach($build in @('Providers/Shared/NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage/NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage.csproj','Providers/Cloud/NextGenSoftware.OASIS.API.Providers.AWSS3OASIS/NextGenSoftware.OASIS.API.Providers.AWSS3OASIS.csproj','Providers/Storage/NextGenSoftware.OASIS.API.Providers.MongoOASIS/NextGenSoftware.OASIS.API.Providers.MongoDBOASIS.csproj',$project)) {
                $path=if([IO.Path]::IsPathFullyQualified($build)){$build}else{Join-Path $root $build}
                & dotnet build $path --no-restore /p:BuildProjectReferences=false /m:2 /nr:false /p:UseSharedCompilation=false -v:q -clp:ErrorsOnly
                if($LASTEXITCODE){throw 'Bounded producer/consumer build failed.'}
            }
            $data=Join-Path $state 'data';New-Item -ItemType Directory -Path $data -Force | Out-Null
            $windowsData=$data.Replace('\','/')
            $mount=((& wsl -d Ubuntu -- wslpath -a $windowsData)-join '').Trim()
            if($LASTEXITCODE -or !$mount.StartsWith('/mnt/c/Source/OASIS-cosmic/LocalState/COSMIC/')){throw 'Data mount outside owned state.'}
            & wsl -d Ubuntu -u root -- podman container exists $container
            if($LASTEXITCODE -eq 1){
                # Public test-only credentials; loopback host binding, never a cloud account.
                & wsl -d Ubuntu -u root -- podman create --name $container --label one.oasis.owner=cosmic-s3-cql --read-only --tmpfs /tmp:rw,size=64m -e HOME=/tmp -e MINIO_ROOT_USER=cosmic-local-test -e MINIO_ROOT_PASSWORD=cosmic-local-test-only -p 127.0.0.1:19100:9000 -p 127.0.0.1:19101:9001 -v "${mount}:/data:rw" $image server /data --address :9000 --console-address :9001 | Out-Null
                if($LASTEXITCODE){throw 'Owned compatible-service creation failed.'}
            } elseif($LASTEXITCODE){throw 'Container inventory failed.'}
            $raw=& wsl -d Ubuntu -u root -- podman inspect $container
            if($LASTEXITCODE){throw 'Container inspection failed.'}
            $owned=@(($raw -join "`n") | ConvertFrom-Json)[0]
            $dataMount=@($owned.Mounts | Where-Object Destination -eq '/data')
            if($owned.Image -ne $image -or $owned.Config.Labels.'one.oasis.owner' -ne 'cosmic-s3-cql' -or $owned.State.Running -or $dataMount.Count -ne 1 -or $dataMount[0].Source -ne $mount -or !$dataMount[0].RW -or !$owned.HostConfig.ReadonlyRootfs -or ($owned.Config.Cmd -join ' ') -ne 'server /data --address :9000 --console-address :9001'){throw 'Container identity/command/mount differs; preserve it.'}
            foreach($port in @(@('9000/tcp','19100'),@('9001/tcp','19101'))) {
                $bindings=@($owned.HostConfig.PortBindings.($port[0]))
                if($bindings.Count -ne 1 -or $bindings[0].HostIp -ne '127.0.0.1' -or $bindings[0].HostPort -ne $port[1]){throw 'Loopback binding differs.'}
            }
            $keyPath=Join-Path $state 'storage-key.dpapi'
            if(!(Test-Path $keyPath)) {
                if(Test-Path (Join-Path $state 'demo-seed.json')){throw 'Original key missing; never replace it.'}
                $key=[Security.Cryptography.RandomNumberGenerator]::GetBytes(32)
                try{[IO.File]::WriteAllBytes($keyPath,[Security.Cryptography.ProtectedData]::Protect($key,$null,'CurrentUser'))}
                finally{[Security.Cryptography.CryptographicOperations]::ZeroMemory($key)}
            }
            $key=[Security.Cryptography.ProtectedData]::Unprotect([IO.File]::ReadAllBytes($keyPath),$null,'CurrentUser')
            try{if($key.Length -ne 32){throw 'Invalid retained key.'};$env:OASIS_S3_CQL_STORAGE_KEY=[Convert]::ToBase64String($key)}
            finally{[Security.Cryptography.CryptographicOperations]::ZeroMemory($key)}
            $env:OASIS_S3_CQL_NATIVE='1';$env:OASIS_S3_CQL_STATE=$state;$env:OASIS_S3_CQL_ENDPOINT='http://127.0.0.1:19100/';$env:OASIS_S3_CQL_BUCKET='cosmic-cql-owned'
            $env:MINIO_ACCESS_KEY='cosmic-local-test';$env:MINIO_SECRET_KEY='cosmic-local-test-only'
            $started=$true;StartStore
            NativeCase 'FullyQualifiedName~EncryptedNativeSourceFiltersThreeToTwoAndPreservesObjects' 's3-cql-native.trx'
            & wsl -d Ubuntu -u root -- podman stop -t 20 $container | Out-Null
            if($LASTEXITCODE){throw 'Store restart stop failed.'}
            StartStore
            NativeCase 'FullyQualifiedName~EncryptedNativeSourceFiltersThreeToTwoAndPreservesObjects' 's3-cql-restart.trx'
            $mongoData=Join-Path $root 'LocalState/COSMIC/mongo-data'
            if(!(Test-Path (Join-Path $mongoData 'WiredTiger'))){throw 'Retained Mongo data required.'}
            $runtime='C:/Program Files/MongoDB/Server/8.3/bin/mongod.exe'
            if(((& $runtime --version)-join ' ') -notmatch 'v8\.3\.11\b'){throw 'Retained Mongo version differs.'}
            $mongo=Start-Process $runtime -ArgumentList @('--dbpath',$mongoData,'--bind_ip','127.0.0.1','--port','27029','--replSet','oasisRecoveryRs','--wiredTigerCacheSizeGB','0.25','--oplogSize','32','--logpath',(Join-Path $results 'mongo-service.log'),'--logappend') -WindowStyle Hidden -PassThru
            $ready=$false
            for($i=0;$i -lt 40;$i++){
                if($mongo.HasExited){throw 'Owned Mongo exited.'}
                $listeners=Get-NetTCPConnection -State Listen -LocalPort 27029 -ErrorAction SilentlyContinue
                if($listeners){if(@($listeners | Where-Object OwningProcess -ne $mongo.Id).Count){throw 'Mongo listener owner differs.'};$ready=$true;break}
                Start-Sleep -Milliseconds 500
            }
            if(!$ready){throw 'Mongo readiness exceeded.'}
            $env:OASIS_MONGO_REPLICA_SET_CONNECTION='mongodb://127.0.0.1:27029/?replicaSet=oasisRecoveryRs&serverSelectionTimeoutMS=3000'
            $env:OASIS_S3_CQL_PAIRED='1';$env:OASIS_S3_MONGO_SERVE=if($Serve){'1'}else{'0'}
            NativeCase 'FullyQualifiedName~NativeAwsS3MongoFederationRunsSdkAndHttpWithProviderProvenance' 's3-mongo-federation.trx'
            if(Test-Path (Join-Path $state 'mongo.pending.json')){throw 'Owned Mongo fixture cleanup incomplete.'}
        } finally {
            foreach($name in $names){[Environment]::SetEnvironmentVariable($name,$prior[$name])}
            if($mongo){if(!$mongo.HasExited){$mongo.Kill();if(!$mongo.WaitForExit(30000)){throw 'Owned Mongo did not stop.'}};$mongo.Dispose()}
            if($started){& wsl -d Ubuntu -u root -- podman stop -t 20 $container | Out-Null;if($LASTEXITCODE){throw 'Owned S3 service did not stop.'}}
        }
    }
} finally {$lease.Dispose()}
