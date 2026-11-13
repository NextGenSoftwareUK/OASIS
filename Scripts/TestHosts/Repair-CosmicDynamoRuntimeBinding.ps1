#Requires -Version 7.0
param([switch]$InspectOnly)
$ErrorActionPreference='Stop'
$root=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
$state=Join-Path $root 'LocalState/COSMIC/AwsCql'
$data=Join-Path $state 'dynamo-data/shared-local-instance.db'
$seed=Join-Path $state 'demo-seed.json'
$runtime=Join-Path $root 'TestResults/AWSProviderEvidence/runtime'
$name='oasis-cosmic-cql-dynamo'
$retired='oasis-cosmic-cql-dynamo-before-runtime-rebind'
$image='6e47503356fe6b0e8f3ba48afc714ed95475d0c32ac97c62b30737af2e8846bd'
$arguments=@('-Xms32m','-Xmx256m','-Djava.library.path=/dynamo/DynamoDBLocal_lib','-jar','/dynamo/DynamoDBLocal.jar','-dbPath','/dynamo-data','-sharedDb','-disableTelemetry','-port','8000')

function Write-BindingEvidence($path,$value){
    $bytes=[Text.Encoding]::UTF8.GetBytes(($value|ConvertTo-Json -Depth 10))
    $stream=[IO.File]::Open($path,[IO.FileMode]::CreateNew,[IO.FileAccess]::Write,[IO.FileShare]::Read)
    try{$stream.Write($bytes);$stream.Flush($true)}finally{$stream.Dispose()}
}

if(!(Test-Path -LiteralPath $data) -or !(Test-Path -LiteralPath $seed)){throw 'Original CQL data/seed required; no source creation.'}
if(Test-Path -LiteralPath $runtime){throw 'Runtime cache already exists; inspect/reuse it, never overwrite.'}
if(Test-Path -LiteralPath (Join-Path $state 'runtime-binding-migration.intent.json')){throw 'Existing migration intent; reconcile instead of replaying.'}
if(Get-NetTCPConnection -State Listen -LocalPort 18002 -ErrorAction SilentlyContinue){throw 'CQL port occupied; no adoption.'}
$lease=[IO.File]::Open((Join-Path $state 'environment.lock'),[IO.FileMode]::OpenOrCreate,[IO.FileAccess]::ReadWrite,[IO.FileShare]::None)
try{
    & wsl -d Ubuntu -u root -- podman container exists $retired
    if($LASTEXITCODE -ne 1){throw 'Prior descriptor exists or cannot be inspected; no migration.'}
    $old=@(& wsl -d Ubuntu -u root -- podman inspect $name | ConvertFrom-Json)[0]
    if($LASTEXITCODE){throw 'Original CQL descriptor unavailable.'}
    $dataWsl=(& wsl -d Ubuntu -u root -- wslpath -a -u (Split-Path $data).Replace('\','/'))
    if($LASTEXITCODE -or [string]::IsNullOrWhiteSpace($dataWsl)){throw 'Assigned data path unresolved.'}
    $runtimeWsl=(& wsl -d Ubuntu -u root -- wslpath -a -u $runtime.Replace('\','/'))
    if($LASTEXITCODE -or [string]::IsNullOrWhiteSpace($runtimeWsl)){throw 'Assigned runtime path unresolved.'}
    $oldRuntime=@($old.Mounts|Where-Object Destination -eq '/dynamo')
    $oldData=@($old.Mounts|Where-Object Destination -eq '/dynamo-data')
    $ports=@($old.HostConfig.PortBindings.'8000/tcp')
    if($old.State.Status -ne 'exited' -or $old.Image -ne $image -or !$old.HostConfig.ReadonlyRootfs -or
       $old.Config.Labels.'one.oasis.provider-test' -ne 'cosmic-cql-dynamo-loopback' -or
       $old.Mounts.Count -ne 2 -or $oldRuntime.Count -ne 1 -or $oldRuntime[0].RW -or
       $oldRuntime[0].Source -ne '/mnt/c/Source/OASIS/TestResults/AWSProviderEvidence/runtime' -or
       $oldData.Count -ne 1 -or !$oldData[0].RW -or $oldData[0].Source -ne $dataWsl.Trim() -or
       $ports.Count -ne 1 -or $ports[0].HostIp -ne '127.0.0.1' -or $ports[0].HostPort -ne '18002' -or
       ($old.Config.Entrypoint -join '') -ne 'java' -or ($old.Config.Cmd -join "`n") -ne ($arguments -join "`n")){
        throw 'Retained CQL identity/command/mounts differ; no migration or source reset.'
    }
    $before=@{oldContainerId=$old.Id;retiredName=$retired;targetRuntime=$runtime;dataPath=$data;dataSha256=(Get-FileHash $data).Hash;seedSha256=(Get-FileHash $seed).Hash;image=$image;command=$arguments;oldMounts=$old.Mounts;readOnlyRootfs=$true;sourceReseeded=$false}
    if($InspectOnly){$before|ConvertTo-Json -Depth 10;return}
    Write-BindingEvidence (Join-Path $state 'runtime-binding-migration.intent.json') $before
    # Only the stopped descriptor changes. The data bind stays on the same file.
    New-Item -ItemType Directory -Path $runtime | Out-Null
    & wsl -d Ubuntu -u root -- podman rename $name $retired
    if($LASTEXITCODE){throw 'Descriptor rename outcome needs reconciliation; intent retained.'}
    $newId=(& wsl -d Ubuntu -u root -- podman create --pull=never --name $name --label one.oasis.provider-test=cosmic-cql-dynamo-loopback --image-volume=ignore --read-only --cap-drop=ALL --network bridge --memory=512m --memory-swap=512m --pids-limit=128 --tmpfs /tmp:rw,size=67108864 --publish 127.0.0.1:18002:8000 --volume "$($runtimeWsl.Trim()):/dynamo:ro" --volume "$($dataWsl.Trim()):/dynamo-data:rw" --workdir /dynamo --entrypoint java $image @arguments)
    if($LASTEXITCODE -or [string]::IsNullOrWhiteSpace($newId)){throw 'New descriptor outcome needs reconciliation; do not recreate.'}
    if((Get-FileHash $data).Hash -ne $before.dataSha256 -or (Get-FileHash $seed).Hash -ne $before.seedSha256){throw 'Original source changed; preserve both descriptors.'}
    Write-BindingEvidence (Join-Path $state 'runtime-binding-created.acknowledged.json') @{newContainerId=$newId.Trim();oldContainerId=$old.Id;dataUnchanged=$true;started=$false}
    & (Join-Path $PSScriptRoot 'Restore-ReusableDynamoRuntime.ps1') -CosmicCql
    if(!$?){throw 'Runtime staging incomplete; preserve migration state.'}
    if((Get-FileHash $data).Hash -ne $before.dataSha256 -or (Get-FileHash $seed).Hash -ne $before.seedSha256){throw 'Original source changed during staging.'}
    Write-BindingEvidence (Join-Path $state 'runtime-binding-ready.acknowledged.json') @{newContainerId=$newId.Trim();dataUnchanged=$true;seedUnchanged=$true;jarSha256=(Get-FileHash (Join-Path $runtime 'DynamoDBLocal.jar')).Hash;nativeQualified=$false}
    'Runtime binding repaired; both descriptors remain stopped and original data is unchanged. Native qualification is still required.'
}finally{$lease.Dispose()}
