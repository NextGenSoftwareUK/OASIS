#Requires -Version 7.0
$ErrorActionPreference='Stop'
$taskRoot=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
$work=Join-Path $taskRoot 'TestResults/IPFSCurrent'
New-Item -ItemType Directory -Force -Path $work | Out-Null
$lease=[IO.File]::Open((Join-Path $work 'environment.lock'),[IO.FileMode]::OpenOrCreate,[IO.FileAccess]::ReadWrite,[IO.FileShare]::None)
$owned=$null
$previous=@{}
foreach($name in @('IPFS_PATH','OASIS_IPFS_STORAGE_ENCRYPTION_KEY','OASIS_IPFS_TEST_RPC','OASIS_IPFS_TEST_DNA_PATH')){$previous[$name]=[Environment]::GetEnvironmentVariable($name)}
try {
    $freeBefore=(Get-PSDrive C).Free
    $binary=Join-Path $env:USERPROFILE '.cache/oasis-runtimes/kubo-0.43.1/ipfs.exe'
    if(!(Test-Path -LiteralPath $binary) -or (Get-FileHash -LiteralPath $binary -Algorithm SHA256).Hash -ne 'FD0D46DF76F455D938713C98C7FCA5FA117B74C934C9FC0D37730FDB33313651'){throw 'Retained pinned Kubo required; no download attempted.'}
    foreach($port in @(18547,18548,18549)){if(Get-NetTCPConnection -State Listen -LocalPort $port -ErrorAction SilentlyContinue){throw 'Owned IPFS test port occupied; refusing duplicate daemon or ambiguous outage fixture.'}}
    $env:IPFS_PATH=Join-Path $work 'repo'
    if(!(Test-Path -LiteralPath (Join-Path $env:IPFS_PATH 'config'))){
        if(Test-Path -LiteralPath $env:IPFS_PATH){throw 'Existing incomplete repository requires inspection; no reset.'}
        & $binary init --profile=test --empty-repo
        if($LASTEXITCODE){throw 'Owned repository initialization failed.'}
        & $binary config Addresses.API '/ip4/127.0.0.1/tcp/18547'
        if($LASTEXITCODE){throw 'Owned API configuration failed.'}
        & $binary config Addresses.Gateway '/ip4/127.0.0.1/tcp/18548'
        if($LASTEXITCODE){throw 'Owned gateway configuration failed.'}
    }
    $keyPath=Join-Path $work 'storage-key.dpapi'
    if(!(Test-Path -LiteralPath $keyPath)){
        $key=[Security.Cryptography.RandomNumberGenerator]::GetBytes(32)
        try {[IO.File]::WriteAllBytes($keyPath,[Security.Cryptography.ProtectedData]::Protect($key,$null,[Security.Cryptography.DataProtectionScope]::CurrentUser))}
        finally {[Security.Cryptography.CryptographicOperations]::ZeroMemory($key)}
    }
    $key=[Security.Cryptography.ProtectedData]::Unprotect([IO.File]::ReadAllBytes($keyPath),$null,[Security.Cryptography.DataProtectionScope]::CurrentUser)
    try {if($key.Length -ne 32){throw 'Invalid retained storage key; refusing replacement.'};$env:OASIS_IPFS_STORAGE_ENCRYPTION_KEY=[Convert]::ToBase64String($key)}
    finally {[Security.Cryptography.CryptographicOperations]::ZeroMemory($key)}
    $env:OASIS_IPFS_TEST_RPC='http://127.0.0.1:18547'
    $env:OASIS_IPFS_TEST_DNA_PATH=Join-Path $work 'owned-dna.json'
    $project=Join-Path $taskRoot 'Providers/Network/TestProjects/NextGenSoftware.OASIS.API.Providers.IPFSOASIS.UnitTests/NextGenSoftware.OASIS.API.Providers.IPFSOASIS.UnitTests.csproj'
    $providerProject=Join-Path $taskRoot 'Providers/Network/NextGenSoftware.OASIS.API.Providers.IPFSOASIS/NextGenSoftware.OASIS.API.Providers.IPFSOASIS.csproj'
    $sharedProject=Join-Path $taskRoot 'Providers/Shared/NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage/NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage.csproj'
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
    & dotnet test $project --no-build --no-restore --filter Category=NativeIPFS --logger 'trx;LogFileName=ipfs-native-current.trx' --results-directory $work -v:q
    if($LASTEXITCODE){throw 'Native IPFS storage verification failed.'}
    $collected=@(& $binary repo gc --quiet)
    if($LASTEXITCODE){throw 'Owned repository garbage collection failed; inspect retained content.'}
    Write-Output "Owned offline repository garbage-collected $($collected.Count) unpinned blocks."
    $report=Get-Item -LiteralPath (Join-Path $work 'ipfs-native-current.trx')
    if($report.LastWriteTimeUtc -lt $started){throw 'Native report stale.'}
    [xml]$trx=Get-Content -LiteralPath $report.FullName
    $counts=$trx.TestRun.ResultSummary.Counters
    if([int]$counts.total -ne 7 -or [int]$counts.passed -ne 7 -or [int]$counts.notExecuted -ne 0){throw 'Expected seven real passing cases, no skips.'}
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
