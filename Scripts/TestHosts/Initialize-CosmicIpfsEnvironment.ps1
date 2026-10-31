#Requires -Version 7.0
$ErrorActionPreference='Stop'
$taskRoot=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
$state=Join-Path $taskRoot 'LocalState/COSMIC/IPFSCurrent'
$output=Join-Path $taskRoot 'TestResults/IPFSCurrent'
$runtime=Join-Path $env:USERPROFILE '.cache/oasis-runtimes/kubo-0.43.1/ipfs.exe'
$runtimeHash='FD0D46DF76F455D938713C98C7FCA5FA117B74C934C9FC0D37730FDB33313651'
if(!(Test-Path -LiteralPath $runtime) -or (Get-FileHash -LiteralPath $runtime).Hash -ne $runtimeHash){throw 'The retained pinned Kubo runtime is required; no download attempted.'}
if((& $runtime version --number).Trim() -ne '0.43.1'){throw 'Retained Kubo version differs.'}
foreach($path in @($state,$output)){
    $parent=[IO.DirectoryInfo]$path
    while($parent -and $parent.FullName.StartsWith($taskRoot,[StringComparison]::OrdinalIgnoreCase)){
        if($parent.Exists -and $parent.LinkTarget){throw 'Owned environment paths must not traverse links.'}
        $parent=$parent.Parent
    }
    New-Item -ItemType Directory -Path $path -Force | Out-Null
}
$lease=[IO.File]::Open((Join-Path $state 'environment.lock'),[IO.FileMode]::OpenOrCreate,[IO.FileAccess]::ReadWrite,[IO.FileShare]::None)
$prior=$env:IPFS_PATH
$key=$null
function Write-NewEnvironmentJson([string]$Path,$Value){
    $stream=[IO.File]::Open($Path,[IO.FileMode]::CreateNew,[IO.FileAccess]::Write,[IO.FileShare]::None)
    try{$bytes=[Text.Encoding]::UTF8.GetBytes(($Value|ConvertTo-Json -Depth 8));$stream.Write($bytes);$stream.Flush($true)}finally{$stream.Dispose()}
}
try{
    $repo=Join-Path $state 'repo'
    $configPath=Join-Path $repo 'config'
    $keyPath=Join-Path $state 'storage-key.dpapi'
    $manifestPath=Join-Path $state 'environment.json'
    $pending=Join-Path $state 'initialization.pending.json'
    if(Test-Path -LiteralPath $pending){throw 'Inspect the incomplete original initialization; no reset or automatic replay.'}
    if(Test-Path -LiteralPath $manifestPath){
        $manifest=Get-Content -LiteralPath $manifestPath -Raw|ConvertFrom-Json
        if(!(Test-Path -LiteralPath $configPath) -or !(Test-Path -LiteralPath $keyPath)){throw 'Preserve the existing environment; a required resource disappeared.'}
        $config=Get-Content -LiteralPath $configPath -Raw|ConvertFrom-Json
        if($manifest.schema -ne 1 -or $manifest.repo -ne $repo -or $manifest.runtimeSha256 -ne $runtimeHash -or
            $config.Identity.PeerID -ne $manifest.peerId -or (Get-FileHash -LiteralPath $keyPath).Hash -ne $manifest.protectedKeySha256){throw 'Existing environment identity differs; no replacement attempted.'}
        $key=[Security.Cryptography.ProtectedData]::Unprotect([IO.File]::ReadAllBytes($keyPath),$null,[Security.Cryptography.DataProtectionScope]::CurrentUser)
        if($key.Length -ne 32){throw 'Existing protected storage key is invalid.'}
        Write-Output "Reused owned IPFS demo environment: $state"
        return
    }
    if((Test-Path -LiteralPath $repo) -or (Test-Path -LiteralPath $keyPath)){throw 'Unknown original resources exist without an ownership manifest; preserve them.'}
    foreach($port in @(18547,18548,18549)){
        if(Get-NetTCPConnection -State Listen -LocalPort $port -ErrorAction SilentlyContinue){throw 'A selected IPFS loopback port is already occupied.'}
    }
    Write-NewEnvironmentJson $pending @{schema=1;owner='cosmic-cql';createdUtc=[DateTimeOffset]::UtcNow.ToString('O');repo=$repo;historicalDataRecovered=$false}
    $env:IPFS_PATH=$repo
    & $runtime init --empty-repo --profile=test *> (Join-Path $output 'initialization.log')
    if($LASTEXITCODE){throw 'Pinned IPFS initialization failed; preserve its pending marker and partial resources.'}
    # The repository stays persistent; all daemon runs use offline mode and loopback only.
    $config=Get-Content -LiteralPath $configPath -Raw|ConvertFrom-Json
    $config.Addresses.API='/ip4/127.0.0.1/tcp/18547'
    $config.Addresses.Gateway='/ip4/127.0.0.1/tcp/18548'
    $config.Addresses.Swarm=@('/ip4/127.0.0.1/tcp/18549')
    $config.Bootstrap=@()
    $config.Discovery.MDNS.Enabled=$false
    [IO.File]::WriteAllText($configPath,($config|ConvertTo-Json -Depth 64),[Text.UTF8Encoding]::new($false))
    $key=[Security.Cryptography.RandomNumberGenerator]::GetBytes(32)
    $protected=[Security.Cryptography.ProtectedData]::Protect($key,$null,[Security.Cryptography.DataProtectionScope]::CurrentUser)
    $keyStream=[IO.File]::Open($keyPath,[IO.FileMode]::CreateNew,[IO.FileAccess]::Write,[IO.FileShare]::None)
    try{$keyStream.Write($protected);$keyStream.Flush($true)}finally{$keyStream.Dispose()}
    Write-NewEnvironmentJson $manifestPath @{schema=1;owner='cosmic-cql';createdUtc=[DateTimeOffset]::UtcNow.ToString('O');
        repo=$repo;runtime=$runtime;runtimeVersion='0.43.1';runtimeSha256=$runtimeHash;peerId=$config.Identity.PeerID;
        protectedKeySha256=(Get-FileHash -LiteralPath $keyPath).Hash;historicalDataRecovered=$false;
        purpose='User-authorized fresh reusable offline CQL demo environment; not recovery of earlier data'}
    Remove-Item -LiteralPath $pending
    Write-Output "Created owned IPFS demo environment: $state"
}finally{
    if($key){[Security.Cryptography.CryptographicOperations]::ZeroMemory($key)}
    $env:IPFS_PATH=$prior
    $lease.Dispose()
}
