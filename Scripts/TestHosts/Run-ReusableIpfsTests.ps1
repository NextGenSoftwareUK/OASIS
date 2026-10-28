#Requires -Version 7.0
param([switch]$ArchiveCompletedDemo,[switch]$ServePointDemo,[switch]$CqlInteractive,[switch]$CqlWeb4,[switch]$CqlPoint,[switch]$ConformanceOnly,[switch]$InspectConformance,[switch]$ReconcileConformance,[switch]$ReconcileGraphConformance,[switch]$ReconcileDeleteConformance,[switch]$ReconcileParentConformance,[switch]$ReconcileV2,[switch]$NetworkDiagnostic,[switch]$ReconcileNative,[switch]$ReconcileRootVersion,[switch]$InspectCredential,[switch]$InspectNamedRoles,[switch]$InspectDeleteFault,[string]$EnvironmentPath)
$ErrorActionPreference='Stop'
if(($ArchiveCompletedDemo -or $ServePointDemo) -and !$CqlInteractive){throw 'ServePointDemo requires the explicit CqlInteractive mode.'}
$suiteModes=@($CqlInteractive,$CqlWeb4,$CqlPoint,$ConformanceOnly,$InspectConformance,$ReconcileConformance,$ReconcileGraphConformance,$ReconcileDeleteConformance,$ReconcileParentConformance)|Where-Object {$_}
if($suiteModes.Count -gt 1 -or ($suiteModes.Count -gt 0 -and ($ReconcileV2 -or $NetworkDiagnostic -or $ReconcileNative -or $ReconcileRootVersion -or $InspectCredential -or $InspectNamedRoles -or $InspectDeleteFault))){
    throw 'Select one shared conformance or original-fixture inspection/reconciliation mode alone.'
}
$taskRoot=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
$work=if($EnvironmentPath){[IO.Path]::GetFullPath($EnvironmentPath)}else{Join-Path $taskRoot 'LocalState/COSMIC/IPFSCurrent'}
$results=Join-Path $taskRoot 'TestResults/IPFSCurrent'
New-Item -ItemType Directory -Path $results -Force|Out-Null
if(!(Test-Path -LiteralPath (Join-Path $work 'repo/config')) -or !(Test-Path -LiteralPath (Join-Path $work 'storage-key.dpapi'))){
    throw 'Retained IPFS repository and original protected storage key required. Select the existing EnvironmentPath; never initialize a duplicate or replace its key.'
}
$lease=[IO.File]::Open((Join-Path $work 'environment.lock'),[IO.FileMode]::OpenOrCreate,[IO.FileAccess]::ReadWrite,[IO.FileShare]::None)
if($ArchiveCompletedDemo -and $work -ne [IO.Path]::GetFullPath((Join-Path $taskRoot 'LocalState/COSMIC/IPFSCurrent'))){throw 'Completed-demo archive is limited to this worktree default environment.'}
if($ArchiveCompletedDemo){& (Join-Path $PSScriptRoot 'Archive-CompletedProtocolPointDemo.ps1') -Provider IPFS}
if($ConformanceOnly){
    foreach($mode in @('async','sync')){
        $report=Join-Path $taskRoot ('TestResults/ProviderConformanceCurrent/IPFSOASIS/'+$mode+'/report.json')
        if((Test-Path -LiteralPath $report) -or (Test-Path -LiteralPath ($report+'.next')) -or
           (Test-Path -LiteralPath (Join-Path $work ('conformance-'+$mode+'.pending.json'))) -or
           (Test-Path -LiteralPath (Join-Path $work ('conformance-'+$mode+'.pending.json.next'))) -or
           (Test-Path -LiteralPath (Join-Path $work ('conformance-'+$mode+'-dna.json')))){
            $lease.Dispose();throw 'Preserve the prior shared-suite report/fixture before rerunning IPFS.'
        }
    }
}
$owned=$null
$previous=@{}
foreach($name in @('OASIS_CQL_POINT_SERVE','IPFS_PATH','OASIS_IPFS_STORAGE_ENCRYPTION_KEY','OASIS_IPFS_TEST_RPC','OASIS_IPFS_TEST_DNA_PATH')){$previous[$name]=[Environment]::GetEnvironmentVariable($name)}
try {
    $env:OASIS_CQL_POINT_SERVE=if($ServePointDemo){'1'}else{'0'}
    $freeBefore=(Get-PSDrive C).Free
    if(!$ReconcileV2 -and !$NetworkDiagnostic -and !$ReconcileNative -and !$ReconcileRootVersion -and !$InspectCredential -and !$InspectNamedRoles -and !$InspectDeleteFault -and (Test-Path -LiteralPath (Join-Path $work 'owned-dna.json'))){
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
    # Cached System.Memory 4.5.5 satisfies the bundled SDK's >=4.5.2 constraint.
    # Keep NU1603 visible without downloading the obsolete minimum solely to build this host.
    & dotnet build $ipfsClientProject --no-restore /p:WarningsNotAsErrors=NU1603 /p:BuildProjectReferences=false /m:2 /nr:false /p:UseSharedCompilation=false -v:q -clp:ErrorsOnly
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
    & dotnet build $project --no-restore /p:IncludeCoreStorageConformance=$($ConformanceOnly.IsPresent -or $InspectConformance.IsPresent -or $ReconcileConformance.IsPresent -or $ReconcileGraphConformance.IsPresent -or $ReconcileDeleteConformance.IsPresent -or $ReconcileParentConformance.IsPresent) /p:BuildProjectReferences=false /m:2 /nr:false /p:UseSharedCompilation=false -v:q -clp:ErrorsOnly
    if($LASTEXITCODE){throw 'Consumer build failed.'}
    $owned=Start-Process -FilePath $binary -ArgumentList @('daemon','--offline') -WindowStyle Hidden -PassThru -RedirectStandardOutput (Join-Path $results 'daemon-current.log') -RedirectStandardError (Join-Path $results 'daemon-current.err')
    $ready=$false; $lastObservation='No RPC response'; $deadline=[DateTime]::UtcNow.AddSeconds(20)
    do {
        if($owned.HasExited){throw 'Owned IPFS daemon exited.'}
        try {$version=Invoke-RestMethod -NoProxy -UserAgent 'OASIS-IPFS-NativeTests/1.0' -Method Post -Uri ($env:OASIS_IPFS_TEST_RPC+'/api/v0/version') -TimeoutSec 2;$ready=$version.Version -eq '0.43.1';$lastObservation='Version='+$version.Version} catch {$ready=$false;$lastObservation=$_.Exception.Message}
        if(!$ready){Start-Sleep -Milliseconds 200}
    } while(!$ready -and [DateTime]::UtcNow -lt $deadline)
    if(!$ready){throw "Pinned offline daemon readiness failed: $lastObservation"}
    $started=[DateTime]::UtcNow
    $filter=if($CqlInteractive){'Category=IPFSCqlInteractiveNative'}elseif($CqlWeb4){'Category=IPFSCqlWeb4Native'}elseif($CqlPoint){'Category=IPFSCqlNative'}elseif($ReconcileParentConformance){'Category=IPFSParentFixtureCleanup'}elseif($ReconcileDeleteConformance){'Category=IPFSDeleteFixtureCleanup'}elseif($ReconcileGraphConformance){'Category=IPFSGraphFixtureCleanup'}elseif($ReconcileConformance){'Category=IPFSCoreStorageCleanup'}elseif($InspectConformance){'Category=IPFSCoreStorageInspect'}elseif($ConformanceOnly){'Category=IPFSCoreStorageConformance'}elseif($InspectDeleteFault){'Category=IPFSDeleteFaultInspection'}elseif($InspectNamedRoles){'Category=IPFSNamedRoleInspection'}elseif($InspectCredential){'Category=IPFSCredentialInspection'}elseif($ReconcileRootVersion){'Category=IPFSRootVersionReconciliation'}elseif($ReconcileNative){'Category=IPFSNativeFixtureReconciliation'}elseif($NetworkDiagnostic){'Category=IPFSNetworkDiagnostic'}elseif($ReconcileV2){'Category=IPFSFixtureReconciliation'}else{'Category=NativeIPFS'}
    $reportName=if($CqlInteractive){'ipfs-cql-interactive-native.trx'}elseif($CqlWeb4){'ipfs-cql-web4-native.trx'}elseif($CqlPoint){'ipfs-cql-native.trx'}elseif($ReconcileParentConformance){'ipfs-parent-cleanup.trx'}elseif($ReconcileDeleteConformance){'ipfs-delete-cleanup.trx'}elseif($ReconcileGraphConformance){'ipfs-graph-cleanup.trx'}elseif($ReconcileConformance){'ipfs-core-cleanup.trx'}elseif($InspectConformance){'ipfs-core-inspect.trx'}elseif($ConformanceOnly){'ipfs-core-current.trx'}elseif($InspectDeleteFault){'ipfs-delete-fault-inspection-current.trx'}elseif($InspectNamedRoles){'ipfs-named-role-inspection-current.trx'}elseif($InspectCredential){'ipfs-credential-inspection-current.trx'}elseif($ReconcileRootVersion){'ipfs-root-version-reconciliation-current.trx'}elseif($ReconcileNative){'ipfs-native-reconciliation-current.trx'}elseif($NetworkDiagnostic){'ipfs-network-current.trx'}elseif($ReconcileV2){'ipfs-reconciliation-current.trx'}else{'ipfs-native-current.trx'}
    & dotnet test $project --no-build --no-restore --filter $filter --logger "trx;LogFileName=$reportName" --results-directory $results -v:q
    $testExit=$LASTEXITCODE
    if($ConformanceOnly){
        & python (Join-Path $taskRoot 'Scripts/Providers/record_conformance_results.py') --providers IPFSOASIS
        if($LASTEXITCODE){throw 'IPFS shared report registration failed.'}
        & python (Join-Path $taskRoot 'Scripts/Providers/update_provider_matrix.py')
        if($LASTEXITCODE){throw 'IPFS matrix update failed.'}
    }
    if($testExit){throw 'Native IPFS storage verification failed; preserve exact fixture evidence.'}
    if(!$CqlInteractive -and !$CqlWeb4 -and !$CqlPoint -and !$ConformanceOnly -and !$InspectConformance -and !$ReconcileConformance -and !$ReconcileGraphConformance -and !$ReconcileDeleteConformance -and !$ReconcileParentConformance -and !$InspectCredential -and !$InspectNamedRoles -and !$InspectDeleteFault){
        $collected=@(& $binary repo gc --quiet)
        if($LASTEXITCODE){throw 'Owned repository garbage collection failed; inspect retained content.'}
        Write-Output "Owned offline repository garbage-collected $($collected.Count) unpinned blocks."
    }
    $report=Get-Item -LiteralPath (Join-Path $results $reportName)
    if($report.LastWriteTimeUtc -lt $started){throw 'Native report stale.'}
    [xml]$trx=Get-Content -LiteralPath $report.FullName
    $counts=$trx.TestRun.ResultSummary.Counters
    $expected=if($ConformanceOnly){2}elseif($CqlInteractive -or $CqlWeb4 -or $CqlPoint -or $ReconcileParentConformance -or $ReconcileDeleteConformance -or $ReconcileGraphConformance -or $ReconcileConformance -or $InspectConformance -or $InspectCredential -or $InspectNamedRoles -or $InspectDeleteFault -or $ReconcileV2 -or $NetworkDiagnostic -or $ReconcileNative -or $ReconcileRootVersion){1}else{16}
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
