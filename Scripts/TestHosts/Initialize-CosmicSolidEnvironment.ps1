#Requires -Version 7.0
$ErrorActionPreference='Stop'
$taskRoot=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
$state=Join-Path $taskRoot 'LocalState/COSMIC/SolidCurrent'
$output=Join-Path $taskRoot 'TestResults/SolidCurrent'
$image='58bee7c3f6a20498d72eb6f8334ba84299b6df2c8fca119ce805fe8614512d7a'
New-Item -ItemType Directory -Force -Path $state,$output|Out-Null
$initializationLease=[IO.File]::Open((Join-Path $state 'environment-init.lock'),[IO.FileMode]::OpenOrCreate,[IO.FileAccess]::ReadWrite,[IO.FileShare]::None)
$manifestPath=Join-Path $state 'environment.json'
$keyPath=Join-Path $state 'storage-key.dpapi'
$key=$null;$owned=$false
try{
    if(Test-Path -LiteralPath $manifestPath){
        $manifest=Get-Content -LiteralPath $manifestPath -Raw|ConvertFrom-Json
        if($manifest.schema -ne 1 -or $manifest.owner -ne 'cosmic-cql' -or $manifest.runtimeImage -ne $image -or
            $manifest.demoPodName -ne 'oasis-cosmic-cql' -or (Get-FileHash -LiteralPath $keyPath).Hash -ne $manifest.protectedKeySha256){throw 'Owned SOLID environment identity differs; no replacement attempted.'}
        foreach($name in @('identity.dpapi','private-local-pod.dpapi','private-local-client.dpapi')){
            if(!(Test-Path -LiteralPath (Join-Path $state $name))){throw 'A retained protected identity resource is missing.'}
        }
        $key=[Security.Cryptography.ProtectedData]::Unprotect([IO.File]::ReadAllBytes($keyPath),$null,[Security.Cryptography.DataProtectionScope]::CurrentUser)
        if($key.Length -ne 32){throw 'Retained protected storage key is invalid.'}
        Write-Output "Reused owned SOLID demo environment: $state"
        return
    }
    if(Get-ChildItem -LiteralPath $state -Filter '*.pending*'){throw 'Reconcile original provisioning before another account/Pod operation.'}
    if(Test-Path -LiteralPath $keyPath){throw 'Unknown protected key exists without its ownership manifest; preserve it.'}
    $runtime=& wsl -d Ubuntu -u root -- podman inspect oasis-solid --format '{{.Image}} {{.State.Status}}'
    if($LASTEXITCODE -or "$runtime".Trim() -ne "$image exited"){throw 'The retained stopped SOLID container is required; no image/container replacement attempted.'}
    & wsl -d Ubuntu -u root -- podman start oasis-solid|Out-Null
    if($LASTEXITCODE){throw 'Retained SOLID server start failed.'}
    $owned=$true
    & (Join-Path $PSScriptRoot 'Wait-ReusableSolidListener.ps1') -MaximumWaitSeconds 30|ConvertTo-Json -Depth 5|Set-Content -LiteralPath (Join-Path $output 'fresh-listener.json')
    & (Join-Path $PSScriptRoot 'Initialize-ReusableSolidIdentity.ps1') -EnvironmentPath $state -PodName 'oasis-cosmic-cql' -LoginEmail 'cosmic-cql-demo@example.invalid'
    $key=[Security.Cryptography.RandomNumberGenerator]::GetBytes(32)
    $cipher=[Security.Cryptography.ProtectedData]::Protect($key,$null,[Security.Cryptography.DataProtectionScope]::CurrentUser)
    $stream=[IO.File]::Open($keyPath,[IO.FileMode]::CreateNew,[IO.FileAccess]::Write,[IO.FileShare]::None)
    try{$stream.Write($cipher);$stream.Flush($true)}finally{$stream.Dispose()}
    $manifest=@{schema=1;owner='cosmic-cql';createdUtc=[DateTimeOffset]::UtcNow.ToString('O');runtimeImage=$image;
        container='oasis-solid';volume='oasis-solid-data';demoPodName='oasis-cosmic-cql';historicalDataRecovered=$false;
        protectedKeySha256=(Get-FileHash -LiteralPath $keyPath).Hash}
    $bytes=[Text.Encoding]::UTF8.GetBytes(($manifest|ConvertTo-Json))
    $stream=[IO.File]::Open($manifestPath,[IO.FileMode]::CreateNew,[IO.FileAccess]::Write,[IO.FileShare]::None)
    try{$stream.Write($bytes);$stream.Flush($true)}finally{$stream.Dispose()}
    Write-Output "Created owned SOLID demo environment: $state"
}finally{
    if($key){[Security.Cryptography.CryptographicOperations]::ZeroMemory($key)}
    if($owned){
        & wsl -d Ubuntu -u root -- podman stop --time 10 oasis-solid|Out-Null
        if($LASTEXITCODE){Write-Warning 'Owned SOLID shutdown failed; preserve its data and inspect the live container.'}
    }
    $initializationLease.Dispose()
}
