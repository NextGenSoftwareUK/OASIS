[CmdletBinding()]
param([switch]$Stop)
$ErrorActionPreference='Stop'
$redisName='oasis-redis-retained'
$redisImage='5d79a9ce29f8b3fe50fd5c07ee1bf654b5cec557b895835825bf6825d525e4f2'
$redisVolume='oasis-redis-data'
$redisRoot=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
function Invoke-RedisPrivatePipe([object[]]$RedisCommands){
    $redisResp=[Text.StringBuilder]::new()
    foreach($redisCommand in $RedisCommands){
        [void]$redisResp.Append('*'+$redisCommand.Count+"`r`n")
        foreach($redisArgument in $redisCommand){
            [void]$redisResp.Append('$'+[Text.Encoding]::UTF8.GetByteCount($redisArgument)+"`r`n").Append($redisArgument).Append("`r`n")
        }
    }
    $redisStart=[Diagnostics.ProcessStartInfo]::new()
    $redisStart.FileName=(Get-Command wsl.exe -ErrorAction Stop).Source
    foreach($redisArgument in @('-d','Ubuntu','-u','root','--','podman','exec','-i',$redisName,'redis-cli','--pipe')){$redisStart.ArgumentList.Add($redisArgument)}
    $redisStart.UseShellExecute=$false;$redisStart.CreateNoWindow=$true
    $redisStart.RedirectStandardInput=$true;$redisStart.RedirectStandardOutput=$true;$redisStart.RedirectStandardError=$true
    $redisStart.StandardInputEncoding=[Text.UTF8Encoding]::new($false)
    $redisProcess=[Diagnostics.Process]::new();$redisProcess.StartInfo=$redisStart
    try{
        if(!$redisProcess.Start()){throw 'Owned Redis credential pipe did not start.'}
        $redisOut=$redisProcess.StandardOutput.ReadToEndAsync();$redisErr=$redisProcess.StandardError.ReadToEndAsync()
        $redisProcess.StandardInput.Write($redisResp.ToString());$redisProcess.StandardInput.Close()
        if(!$redisProcess.WaitForExit(15000)){$redisProcess.Kill();throw 'Owned Redis credential pipe timed out; inspect same service.'}
        $redisPipeText=$redisOut.GetAwaiter().GetResult();[void]$redisErr.GetAwaiter().GetResult()
        if($redisProcess.ExitCode -ne 0 -or $redisPipeText -notmatch 'errors:\s*0,\s*replies:\s*\d+'){
            throw 'Owned Redis authentication/configuration was not acknowledged; preserve existing credential and service.'
        }
    }finally{$redisResp.Clear()|Out-Null;$redisProcess.Dispose()}
}
& wsl -d Ubuntu -u root -- podman container exists $redisName
$redisExists=$LASTEXITCODE -eq 0
if(!$redisExists){
    if($Stop){throw 'Retained Redis container absent; teardown must not create it.'}
    if(Get-NetTCPConnection -State Listen -LocalPort 18679 -ErrorAction SilentlyContinue){throw 'Redis test port already occupied.'}
    & wsl -d Ubuntu -u root -- podman image exists $redisImage
    if($LASTEXITCODE){throw 'Pinned Redis runtime missing; no implicit download.'}
    & wsl -d Ubuntu -u root -- podman volume exists $redisVolume
    if($LASTEXITCODE){& wsl -d Ubuntu -u root -- podman volume create $redisVolume;if($LASTEXITCODE){throw 'Redis volume creation failed'}}
    & wsl -d Ubuntu -u root -- podman create --name $redisName --memory 256m --cpus 1 -p 127.0.0.1:18679:6379 -v oasis-redis-data:/data $redisImage redis-server --bind 0.0.0.0 --protected-mode yes --appendonly yes --maxmemory 128mb --maxmemory-policy noeviction
    if($LASTEXITCODE){throw 'Retained Redis container creation failed'}
}
$redisInspectText=& wsl -d Ubuntu -u root -- podman inspect $redisName
if($LASTEXITCODE){throw 'Redis ownership inspection failed'}
$redisInspect=($redisInspectText -join "`n")|ConvertFrom-Json
if(@($redisInspect).Count -ne 1 -or ($redisInspect.Image -replace '^sha256:','') -ne $redisImage){throw 'Redis runtime identity differs'}
$redisBindings=@($redisInspect.HostConfig.PortBindings.'6379/tcp')
if(@($redisInspect.HostConfig.PortBindings.PSObject.Properties).Count -ne 1 -or $redisBindings.Count -ne 1 -or $redisBindings[0].HostIp -ne '127.0.0.1' -or $redisBindings[0].HostPort -ne '18679'){throw 'Redis must have its exact loopback-only test binding'}
$redisExpectedArgs=@('redis-server','--bind','0.0.0.0','--protected-mode','yes','--appendonly','yes','--maxmemory','128mb','--maxmemory-policy','noeviction')
if(($redisInspect.Config.Cmd -join "`0") -cne ($redisExpectedArgs -join "`0")){throw 'Retained Redis command differs; no implicit reconfiguration'}
$redisMounts=@($redisInspect.Mounts)
if($redisMounts.Count -ne 1 -or $redisMounts[0].Type -ne 'volume' -or $redisMounts[0].Name -ne $redisVolume -or $redisMounts[0].Destination -ne '/data'){throw 'Retained Redis data volume differs'}
if($Stop){
    if(Get-ChildItem -LiteralPath (Join-Path $redisRoot 'TestResults/RedisCurrent') -Filter '*.pending.json' -ErrorAction SilentlyContinue){throw 'Redis original fixture journal unresolved; preserve service/data'}
    if($redisInspect.State.Running){& wsl -d Ubuntu -u root -- podman stop --time 10 $redisName;if($LASTEXITCODE){throw 'Owned Redis stop failed'}}
    $redisDeadline=[DateTime]::UtcNow.AddSeconds(15)
    while(Get-NetTCPConnection -State Listen -LocalPort 18679 -ErrorAction SilentlyContinue){if([DateTime]::UtcNow -gt $redisDeadline){throw 'Redis listener remains after stop'};Start-Sleep -Milliseconds 200}
    return
}
if(!$redisInspect.State.Running){
    if(Get-NetTCPConnection -State Listen -LocalPort 18679 -ErrorAction SilentlyContinue){throw 'Redis test port occupied before owned start'}
    & wsl -d Ubuntu -u root -- podman start $redisName
    if($LASTEXITCODE){throw 'Owned Redis start failed'}
}
$redisDeadline=[DateTime]::UtcNow.AddSeconds(20)
do{
    $redisPing=& wsl -d Ubuntu -u root -- podman exec $redisName redis-cli PING
    if($LASTEXITCODE -eq 0 -and ($redisPing -ceq 'PONG' -or ($redisPing -join '') -match '^NOAUTH')){break}
    Start-Sleep -Milliseconds 200
}while([DateTime]::UtcNow -lt $redisDeadline)
if($LASTEXITCODE -ne 0 -or !($redisPing -ceq 'PONG' -or ($redisPing -join '') -match '^NOAUTH')){throw 'Owned Redis readiness unresolved; inspect same container, do not recreate it.'}
$redisSecure=& (Join-Path $PSScriptRoot 'Get-ReusableRedisTestCredential.ps1') -CreateIfMissing
try{
    $redisPassword=[Net.NetworkCredential]::new('', $redisSecure).Password
    # Explicit observed states: fresh loopback-only server, or already authenticated.
    # No auth reset after a rejected credential and no disabled protected-mode fallback.
    if($redisPing -ceq 'PONG'){
        Invoke-RedisPrivatePipe @(@('CONFIG','SET','requirepass',$redisPassword),@('AUTH',$redisPassword),@('PING'))
    }else{Invoke-RedisPrivatePipe @(@('AUTH',$redisPassword),@('PING'))}
    Write-Output 'Retained Redis8.2.2 authenticated readiness verified; protected mode retained.'
}finally{$redisPassword=$null;$redisSecure.Dispose()}
