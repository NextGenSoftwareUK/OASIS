#Requires -Version 7.0
$ErrorActionPreference='Stop'
$taskRoot=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
& (Join-Path $PSScriptRoot 'Initialize-CosmicIpfsEnvironment.ps1')
$state=Join-Path $taskRoot 'LocalState/COSMIC/IPFSCurrent'
$output=Join-Path $taskRoot 'TestResults/IPFSCurrent'
$manifest=Get-Content -LiteralPath (Join-Path $state 'environment.json') -Raw|ConvertFrom-Json
$config=Get-Content -LiteralPath (Join-Path $manifest.repo 'config') -Raw|ConvertFrom-Json
if($config.Addresses.API -ne '/ip4/127.0.0.1/tcp/18547' -or $config.Addresses.Gateway -ne '/ip4/127.0.0.1/tcp/18548'
    ){throw 'Owned IPFS loopback bindings differ.'}
$lease=[IO.File]::Open((Join-Path $state 'environment.lock'),[IO.FileMode]::OpenOrCreate,[IO.FileAccess]::ReadWrite,[IO.FileShare]::None)
$prior=$env:IPFS_PATH;$owned=$null
try{
    foreach($port in @(18547,18548,18549)){if(Get-NetTCPConnection -State Listen -LocalPort $port -ErrorAction SilentlyContinue){throw 'Owned IPFS port is already occupied.'}}
    $env:IPFS_PATH=$manifest.repo
    $owned=Start-Process -FilePath $manifest.runtime -ArgumentList @('daemon','--offline') -WindowStyle Hidden -PassThru -RedirectStandardOutput (Join-Path $output 'daemon-readiness.log') -RedirectStandardError (Join-Path $output 'daemon-readiness.err')
    $deadline=[DateTime]::UtcNow.AddSeconds(20);$ready=$false
    do{
        if($owned.HasExited){throw 'Owned IPFS daemon exited during readiness.'}
        try{$version=Invoke-RestMethod -NoProxy -Method Post -UserAgent 'OASIS-IPFS-NativeTests/1.0' -Uri 'http://127.0.0.1:18547/api/v0/version' -TimeoutSec 2;$ready=$version.Version -eq '0.43.1'}catch{$ready=$false}
        if(!$ready){Start-Sleep -Milliseconds 200}
    }while(!$ready -and [DateTime]::UtcNow -lt $deadline)
    if(!$ready){throw 'Owned IPFS offline readiness failed; preserve logs and repository.'}
    $identity=Invoke-RestMethod -NoProxy -Method Post -UserAgent 'OASIS-IPFS-NativeTests/1.0' -Uri 'http://127.0.0.1:18547/api/v0/id' -TimeoutSec 3
    if($identity.ID -ne $manifest.peerId){throw 'Native IPFS identity differs from the owned manifest.'}
    @{runtimeVersion=$version.Version;peerId=$identity.ID;offline=$true;historicalDataRecovered=$false}|ConvertTo-Json -Compress|Set-Content -LiteralPath (Join-Path $output 'readiness.json')
    'PASS: owned offline IPFS version and identity; no record mutation.'
}finally{
    if($owned -and !$owned.HasExited){
        try{Invoke-RestMethod -NoProxy -Method Post -UserAgent 'OASIS-IPFS-NativeTests/1.0' -Uri 'http://127.0.0.1:18547/api/v0/shutdown' -TimeoutSec 3|Out-Null}catch{Write-Warning 'Owned daemon shutdown RPC failed; preserve its process/repository for inspection.'}
        if(!$owned.WaitForExit(10000)){Write-Warning 'Owned daemon is still active; do not start another or remove its state.'}
    }
    $env:IPFS_PATH=$prior;$lease.Dispose()
}
