#Requires -Version 7.0
param([switch]$VerifyPending)
$ErrorActionPreference='Stop'
$taskRoot=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
$taskWork=Join-Path $taskRoot 'TestResults/SolidCurrent'
$taskLease=[IO.File]::Open((Join-Path $taskWork 'environment.lock'),[IO.FileMode]::OpenOrCreate,[IO.FileAccess]::ReadWrite,[IO.FileShare]::None)
$taskPending=Join-Path $taskWork 'solid-init-reconfiguration.pending.json'
$taskReceipt=Join-Path $taskWork 'solid-init-reconfiguration.json'
function Write-NewSolidEvidence($path,$value){
    $bytes=[Text.Encoding]::UTF8.GetBytes(($value|ConvertTo-Json -Depth 8))
    $stream=[IO.File]::Open($path,[IO.FileMode]::CreateNew,[IO.FileAccess]::Write,[IO.FileShare]::None)
    try{$stream.Write($bytes);$stream.Flush($true)}finally{$stream.Dispose()}
}
function Get-SolidVolumeDigest{
    $digest=& wsl -d Ubuntu -u root -- bash -o pipefail -c 'tar --sort=name -C /var/lib/containers/storage/volumes/oasis-solid-data/_data -cf - . | sha256sum'
    if($LASTEXITCODE -ne 0 -or "$digest" -notmatch '^([a-f0-9]{64})\s'){throw 'Retained volume digest unavailable.'}
    return $Matches[1]
}
function Confirm-SolidDefinition($after,$evidence){
    $image='58bee7c3f6a20498d72eb6f8334ba84299b6df2c8fca119ce805fe8614512d7a'
    $cmd=@('-c','@css:config/file.json','-f','/data','-b','http://localhost:13000/','-p','13000','-l','warn')
    $expected=@('podman','create','--pull=never','--init','--init-path','/usr/bin/catatonit','--name','oasis-solid','--hostname',$evidence.hostname,'--memory','768m','--cpus','2','-p','127.0.0.1:13000:13000','-v','oasis-solid-data:/data',$image)+$cmd
    $ports=@($after.HostConfig.PortBindings.'13000/tcp')
    if($evidence.image -ne $image -or $after.Image -ne $image -or $after.State.Status -ne 'created' -or
        ($after.Config.CreateCommand -join "`n") -cne ($expected -join "`n") -or
        ($after.Config.Cmd -join "`n") -cne ($cmd -join "`n") -or ($after.Config.Entrypoint -join ' ') -ne 'node bin/server.js' -or
        $after.HostConfig.Memory -ne 805306368 -or $after.HostConfig.MemorySwap -ne 1610612736 -or $after.HostConfig.NanoCpus -ne 2000000000 -or
        $ports.Count -ne 1 -or $ports[0].HostIp -ne '127.0.0.1' -or $ports[0].HostPort -ne '13000' -or
        @($after.Mounts).Count -ne 1 -or $after.Mounts[0].Name -ne 'oasis-solid-data' -or $after.Mounts[0].Destination -ne '/data' -or
        $after.Mounts[0].Source -ne '/var/lib/containers/storage/volumes/oasis-solid-data/_data'){
        throw 'Created definition differs from the approved exact configuration.'
    }
    $imageRaw=& wsl -d Ubuntu -u root -- podman image inspect $image
    if($LASTEXITCODE){throw 'Pinned image configuration unavailable.'}
    $imageConfig=($imageRaw|ConvertFrom-Json)[0].Config
    # Podman injects HOME for a started root process; a never-started definition
    # may omit it. Compare image-owned settings and explicit hostname separately.
    $expectedEnv=@($imageConfig.Env)+@('container=podman',('HOSTNAME='+$evidence.hostname))
    $normalize={param($rows) ($rows|Where-Object {$_ -ne 'HOME=/root'}|Sort-Object) -join "`n"}
    if([string]$after.Config.User -cne [string]$imageConfig.User -or (& $normalize $after.Config.Env) -cne (& $normalize $expectedEnv)){
        throw 'Image-owned environment or user differs.'
    }
    if((Get-SolidVolumeDigest) -ne $evidence.volumeDigestBefore){throw 'Volume digest changed before start.'}
    foreach($pair in $evidence.protectedIdentityHashes.GetEnumerator()){
        if((Get-FileHash -LiteralPath (Join-Path $taskWork $pair.Key)).Hash -ne $pair.Value){throw 'Protected identity changed.'}
    }
    $evidence.state='definition-verified-not-started';$evidence.newContainerId=$after.Id;$evidence.volumeDigestAfter=$evidence.volumeDigestBefore;$evidence.freeAfter=(Get-PSDrive C).Free
    Write-NewSolidEvidence $taskReceipt $evidence
    Remove-Item -LiteralPath $taskPending
}
try{
    if($VerifyPending){
        if((Test-Path $taskReceipt) -or !(Test-Path $taskPending) -or @(Get-ChildItem $taskWork -Filter '*pending*.json').Count -ne 1){throw 'Exactly one original init intent required.'}
        $evidence=Get-Content -LiteralPath $taskPending -Raw|ConvertFrom-Json -AsHashtable
        $raw=& wsl -d Ubuntu -u root -- podman inspect oasis-solid
        if($LASTEXITCODE){throw 'Created definition unavailable; no recreation attempted.'}
        Confirm-SolidDefinition ($raw|ConvertFrom-Json)[0] $evidence
        'Original created definition verified; no second removal/create/start.'
        return
    }
    if((Test-Path $taskReceipt) -or (Get-ChildItem $taskWork -Filter '*pending*.json')){throw 'Original receipt/intent requires inspection; never repeat reconfiguration blindly.'}
    $raw=& wsl -d Ubuntu -u root -- podman inspect oasis-solid
    if($LASTEXITCODE){throw 'Retained container unavailable.'}
    $before=($raw|ConvertFrom-Json)[0]
    $image='58bee7c3f6a20498d72eb6f8334ba84299b6df2c8fca119ce805fe8614512d7a'
    $command=@('-c','@css:config/file.json','-f','/data','-b','http://localhost:13000/','-p','13000','-l','warn')
    $original=@('podman','run','-d','--pull=never','--name','oasis-solid','--memory','768m','--cpus','2','-p','127.0.0.1:13000:13000','-v','oasis-solid-data:/data',$image)+$command
    $mounts=@($before.Mounts);$ports=@($before.HostConfig.PortBindings.'13000/tcp')
    if($before.Image -ne $image -or $before.State.Status -ne 'exited' -or
        ($before.Config.CreateCommand -join "`n") -cne ($original -join "`n") -or
        ($before.Config.Entrypoint -join ' ') -ne 'node bin/server.js' -or
        $before.HostConfig.Memory -ne 805306368 -or $before.HostConfig.NanoCpus -ne 2000000000 -or
        $mounts.Count -ne 1 -or $mounts[0].Name -ne 'oasis-solid-data' -or $mounts[0].Destination -ne '/data' -or
        $mounts[0].Source -ne '/var/lib/containers/storage/volumes/oasis-solid-data/_data' -or
        $ports.Count -ne 1 -or $ports[0].HostIp -ne '127.0.0.1' -or $ports[0].HostPort -ne '13000'){
        throw 'Exact approved stopped definition differs; no removal.'
    }
    $diff=@(& wsl -d Ubuntu -u root -- podman diff oasis-solid)
    if($LASTEXITCODE -or @($diff|Where-Object {$_ -ne 'C /etc'}).Count){throw 'Unreviewed writable-layer data; no removal.'}
    $version=& wsl -d Ubuntu -u root -- /usr/bin/catatonit --version
    $size=& wsl -d Ubuntu -u root -- stat -c '%s' /usr/bin/catatonit
    if($LASTEXITCODE -or "$version" -ne 'tini version 0.2.1_catatonit' -or "$size" -ne '805440'){throw 'Retained init runtime differs; no installation.'}
    $identities=@{}
    foreach($name in @('private-local-client.dpapi','private-local-pod.dpapi')){$identities[$name]=(Get-FileHash -LiteralPath (Join-Path $taskWork $name)).Hash}
    $volume=Get-SolidVolumeDigest
    $evidence=[ordered]@{startedUtc=[DateTime]::UtcNow.ToString('O');image=$image;oldContainerId=$before.Id;oldCreateCommand=$original;hostname=$before.Config.Hostname;volumeDigestBefore=$volume;protectedIdentityHashes=$identities;initPath='/usr/bin/catatonit';initVersion="$version";initBytes=805440;freeBefore=(Get-PSDrive C).Free;state='before-definition-removal'}
    Write-NewSolidEvidence $taskPending $evidence
    # Remove ONLY the verified stopped definition. Never pass --volumes.
    & wsl -d Ubuntu -u root -- podman rm $before.Id
    if($LASTEXITCODE){throw 'Definition removal failed; preserve pending intent.'}
    $taskCreateArgs=@('create','--pull=never','--init','--init-path','/usr/bin/catatonit','--name','oasis-solid','--hostname',$before.Config.Hostname,'--memory','768m','--cpus','2','-p','127.0.0.1:13000:13000','-v','oasis-solid-data:/data',$image)+$command
    & wsl -d Ubuntu -u root -- podman @taskCreateArgs
    if($LASTEXITCODE){throw 'Creation failed; preserve volume and recovery intent.'}
    $raw=& wsl -d Ubuntu -u root -- podman inspect oasis-solid
    if($LASTEXITCODE){throw 'New definition observation failed.'}
    $after=($raw|ConvertFrom-Json)[0]
    Confirm-SolidDefinition $after $evidence
    'Same pinned SOLID definition recreated with init; volume and protected identity hashes unchanged. Not yet runtime verification.'
}finally{$taskLease.Dispose()}
