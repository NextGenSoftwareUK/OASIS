#Requires -Version 7.0
param([switch]$ReconcileV2,[switch]$NetworkDiagnostic,[switch]$ReconcileNative,[switch]$ReconcileRootVersion,[switch]$InspectCredential,[switch]$InspectNamedRoles,[string]$EnvironmentPath)
$ErrorActionPreference='Stop'
$taskRoot=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
$work=if($EnvironmentPath){[IO.Path]::GetFullPath($EnvironmentPath)}else{Join-Path $taskRoot 'TestResults/IPFSCurrent'}
if(!(Test-Path -LiteralPath (Join-Path $work 'repo/config')) -or !(Test-Path -LiteralPath (Join-Path $work 'storage-key.dpapi'))){
    throw 'Retained IPFS repository and original protected storage key required. Select the existing EnvironmentPath; never initialize a duplicate or replace its key.'
}
$lease=[IO.File]::Open((Join-Path $work 'environment.lock'),[IO.FileMode]::OpenOrCreate,[IO.FileAccess]::ReadWrite,[IO.FileShare]::None)
$owned=$null
$previous=@{}
foreach($name in @('IPFS_PATH','OASIS_IPFS_STORAGE_ENCRYPTION_KEY','OASIS_IPFS_TEST_RPC','OASIS_IPFS_TEST_DNA_PATH')){$previous[$name]=[Environment]::GetEnvironmentVariable($name)}
try {
    $freeBefore=(Get-PSDrive C).Free
    if(!$ReconcileV2 -and !$NetworkDiagnostic -and !$ReconcileNative -and !$ReconcileRootVersion -and !$InspectCredential -and !$InspectNamedRoles -and (Test-Path -LiteralPath (Join-Path $work 'owned-dna.json'))){
        throw 'Original failed IPFS fixture requires read-only review before fresh native acceptance; no build/start/reset attempted.'
    }
    $binary=Join-Path $env:USERPROFILE '.cache/oasis-runtimes/kubo-0.43.1/ipfs.exe'
    if(!(Test-Path -LiteralPath $binary) -or (Get-FileHash -LiteralPath $binary -Algorithm SHA256).Hash -ne 'FD0D46DF76F455D938713C98C7FCA5FA117B74C934C9FC0D37730FDB33313651'){throw 'Retained pinned Kubo required; no download attempted.'}
    foreach($port in @(18547,18548,18549)){if(Get-NetTCPConnection -State Listen -LocalPort $port -ErrorAction SilentlyContinue){throw 'Owned IPFS test port occupied; refusing duplicate daemon or ambiguous outage fixture.'}}
    $env:IPFS_PATH=Join-Path $work 'repo'
    if(!(Test-Path -LiteralPath (Join-Path $env:IPFS_PATH 'config'))){
        throw 'Retained IPFS repository disappeared; refusing initialization.'
    }
    $keyPath=Join-Path $work 'storage-key.dpapi'
    if(!(Test-Path -LiteralPath $keyPath)){
        throw 'Original retained IPFS storage key disappeared; restore it, never generate a replacement.'
    }
    $key=[Security.Cryptography.ProtectedData]::Unprotect([IO.File]::ReadAllBytes($keyPath),$null,[Security.Cryptography.DataProtectionScope]::CurrentUser)
    try {if($key.Length -ne 32){throw 'Invalid retained storage key; refusing replacement.'};$env:OASIS_IPFS_STORAGE_ENCRYPTION_KEY=[Convert]::ToBase64String($key)}
    finally {[Security.Cryptography.CryptographicOperations]::ZeroMemory($key)}
    $env:OASIS_IPFS_TEST_RPC='http://127.0.0.1:18547'
    $env:OASIS_IPFS_TEST_DNA_PATH=Join-Path $work 'owned-dna.json'
    $project=Join-Path $taskRoot 'Providers/Network/TestProjects/NextGenSoftware.OASIS.API.Providers.IPFSOASIS.UnitTests/NextGenSoftware.OASIS.API.Providers.IPFSOASIS.UnitTests.csproj'
    $providerProject=Join-Path $taskRoot 'Providers/Network/NextGenSoftware.OASIS.API.Providers.IPFSOASIS/NextGenSoftware.OASIS.API.Providers.IPFSOASIS.csproj'
    $sharedProject=Join-Path $taskRoot 'Providers/Shared/NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage/NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage.csproj'
    $ipfsClientProject=Join-Path $taskRoot 'External Libs/net-ipfs-http-client-master/src/IpfsHttpClient.csproj'
    & dotnet build $ipfsClientProject --no-restore /p:BuildProjectReferences=false /m:2 /nr:false /p:UseSharedCompilation=false -v:q -clp:ErrorsOnly
    if($LASTEXITCODE){throw 'IPFS SDK producer build failed.'}
    & dotnet build $sharedProject --no-restore /p:BuildProjectReferences=false /m:2 /nr:false /p:UseSharedCompilation=false -v:q -clp:ErrorsOnly
    if($LASTEXITCODE){throw 'Shared storage build failed.'}
    $sqliteProject=Join-Path $taskRoot 'Providers/Storage/NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS/NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS.csproj'
    & dotnet build $sqliteProject --no-restore /p:BuildProjectReferences=false /m:2 /nr:false /p:UseSharedCompilation=false -v:q -clp:ErrorsOnly
    if($LASTEXITCODE){throw 'SQLite producer build failed.'}
    $localProject=Join-Path $taskRoot 'Providers/Storage/NextGenSoftware.OASIS.API.Providers.LocalFileOASIS/NextGenSoftware.OASIS.API.Providers.LocalFileOASIS.csproj'
    & dotnet build $localProject --no-restore /p:BuildProjectReferences=false /m:2 /nr:false /p:UseSharedCompilation=false -v:q -clp:ErrorsOnly
    if($LASTEXITCODE){throw 'Local file provider build failed.'}
    & dotnet build $providerProject --no-restore /p:BuildProjectReferences=false /m:2 /nr:false /p:UseSharedCompilation=false -v:q -clp:ErrorsOnly
    if($LASTEXITCODE){throw 'Provider build failed.'}
    & dotnet build $project --no-restore /p:BuildProjectReferences=false /m:2 /nr:false /p:UseSharedCompilation=false -v:q -clp:ErrorsOnly
    if($LASTEXITCODE){throw 'Consumer build failed.'}
    $owned=Start-Process -FilePath $binary -ArgumentList @('daemon','--offline') -WindowStyle Hidden -PassThru -RedirectStandardOutput (Join-Path $work 'daemon-current.log') -RedirectStandardError (Join-Path $work 'daemon-current.err')
    $ready=$false; $lastObservation='No RPC response'; $deadline=[DateTime]::UtcNow.AddSeconds(20)
    do {
        if($owned.HasExited){throw 'Owned IPFS daemon exited.'}
        try {$version=Invoke-RestMethod -NoProxy -UserAgent 'OASIS-IPFS-NativeTests/1.0' -Method Post -Uri ($env:OASIS_IPFS_TEST_RPC+'/api/v0/version') -TimeoutSec 2;$ready=$version.Version -eq '0.43.1';$lastObservation='Version='+$version.Version} catch {$ready=$false;$lastObservation=$_.Exception.Message}
        if(!$ready){Start-Sleep -Milliseconds 200}
    } while(!$ready -and [DateTime]::UtcNow -lt $deadline)
    if(!$ready){throw "Pinned offline daemon readiness failed: $lastObservation"}
    $started=[DateTime]::UtcNow
    $filter=if($InspectNamedRoles){'Category=IPFSNamedRoleInspection'}elseif($InspectCredential){'Category=IPFSCredentialInspection'}elseif($ReconcileRootVersion){'Category=IPFSRootVersionReconciliation'}elseif($ReconcileNative){'Category=IPFSNativeFixtureReconciliation'}elseif($NetworkDiagnostic){'Category=IPFSNetworkDiagnostic'}elseif($ReconcileV2){'Category=IPFSFixtureReconciliation'}else{'Category=NativeIPFS'}
    $reportName=if($InspectNamedRoles){'ipfs-named-role-inspection-current.trx'}elseif($InspectCredential){'ipfs-credential-inspection-current.trx'}elseif($ReconcileRootVersion){'ipfs-root-version-reconciliation-current.trx'}elseif($ReconcileNative){'ipfs-native-reconciliation-current.trx'}elseif($NetworkDiagnostic){'ipfs-network-current.trx'}elseif($ReconcileV2){'ipfs-reconciliation-current.trx'}else{'ipfs-native-current.trx'}
    & dotnet test $project --no-build --no-restore --filter $filter --logger "trx;LogFileName=$reportName" --results-directory $work -v:q
    if($LASTEXITCODE){throw 'Native IPFS storage verification failed.'}
    if(!$InspectCredential -and !$InspectNamedRoles){
        $collected=@(& $binary repo gc --quiet)
        if($LASTEXITCODE){throw 'Owned repository garbage collection failed; inspect retained content.'}
        Write-Output "Owned offline repository garbage-collected $($collected.Count) unpinned blocks."
    }
    $report=Get-Item -LiteralPath (Join-Path $work $reportName)
    if($report.LastWriteTimeUtc -lt $started){throw 'Native report stale.'}
    [xml]$trx=Get-Content -LiteralPath $report.FullName
    $counts=$trx.TestRun.ResultSummary.Counters
    $expected=if($InspectCredential -or $InspectNamedRoles -or $ReconcileV2 -or $NetworkDiagnostic -or $ReconcileNative -or $ReconcileRootVersion){1}else{16}
    if([int]$counts.total -ne $expected -or [int]$counts.passed -ne $expected -or [int]$counts.notExecuted -ne 0){throw "Expected $expected passing selected cases, no skips."}
    Write-Output "Free bytes before=$freeBefore after=$((Get-PSDrive C).Free)"
} finally {
    try {
        if($owned -and !$owned.HasExited){
            if($ready){Invoke-RestMethod -NoProxy -UserAgent 'OASIS-IPFS-NativeTests/1.0' -Method Post -Uri 'http://127.0.0.1:18547/api/v0/shutdown' -TimeoutSec 5 | Out-Null}
            else {Stop-Process -Id $owned.Id}
            if(!$owned.WaitForExit(10000)){throw 'Owned daemon still live; do not restart or remove repository.'}
        }
    } finally {foreach($name in $previous.Keys){[Environment]::SetEnvironmentVariable($name,$previous[$name])};$lease.Dispose()}
}
