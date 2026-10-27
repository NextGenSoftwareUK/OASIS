#Requires -Version 7.0
[CmdletBinding()]
param([switch]$Stop,[switch]$InitializeSyntheticStore,[switch]$RebindMovedEvidence,[switch]$CompactStore)
$ErrorActionPreference='Stop'
# Caller holds the existing AWS environment lease. Same JAR/data, no downloads or copies.
$name='oasis-dynamo-retained'
$image='6e47503356fe6b0e8f3ba48afc714ed95475d0c32ac97c62b30737af2e8846bd'
$dynamoRepositoryRoot=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
$runtimeWindows=Join-Path $dynamoRepositoryRoot 'TestResults/AWSProviderEvidence/runtime'
$dataWindows=Join-Path $dynamoRepositoryRoot 'TestResults/AWSProviderEvidence/retained-history-data'
$runtime=(& wsl -d Ubuntu -- wslpath -u ($runtimeWindows.Replace('\','/'))).Trim()
if($LASTEXITCODE){throw 'Cannot resolve repository runtime mount.'}
$data=(& wsl -d Ubuntu -- wslpath -u ($dataWindows.Replace('\','/'))).Trim()
if($LASTEXITCODE){throw 'Cannot resolve repository data mount.'}
if($CompactStore -and !$Stop){throw 'Compaction requires stopped-runtime teardown scope.'}
function Assert-BoundedDynamoStore([switch]$Force) {
    if(!(Test-Path -LiteralPath $dataWindows)){return}
    $bytes=(Get-ChildItem -LiteralPath $dataWindows -Recurse -File | Measure-Object Length -Sum).Sum
    if(!$Force -and $bytes -lt 200MB){return}
    # The environment lease is held and the runtime must be stopped before this call.
    @'
import sqlite3,sys
from pathlib import Path
root=Path(sys.argv[1])
if root.is_symlink():raise RuntimeError('Unexpected data-directory link')
for path in root.glob('*.db'):
    if path.is_symlink() or path.resolve().parent!=root.resolve():raise RuntimeError('Unexpected database target')
    with sqlite3.connect(path.resolve().as_uri()+'?mode=rw',uri=True,isolation_level=None,timeout=5) as db:
        if db.execute('PRAGMA quick_check').fetchall()!=[('ok',)]:raise RuntimeError('Database integrity check failed')
        if db.execute('PRAGMA wal_checkpoint(TRUNCATE)').fetchone()[0]!=0:raise RuntimeError('Database still in use')
        db.execute('VACUUM')
        if db.execute('PRAGMA quick_check').fetchall()!=[('ok',)]:raise RuntimeError('Post-compaction integrity check failed')
'@ | python -B - $dataWindows
    if($LASTEXITCODE){throw 'Stopped DynamoDB compaction failed; no reset attempted.'}
    $bytes=(Get-ChildItem -LiteralPath $dataWindows -Recurse -File | Measure-Object Length -Sum).Sum
    if($bytes -ge 200MB){throw 'DynamoDB remains over 200 MB after compaction; review owned fixtures before a seed rebuild, never erase unknown outcomes.'}
}
$arguments=@('-Xms32m','-Xmx256m','-Djava.library.path=/dynamo/DynamoDBLocal_lib','-jar','/dynamo/DynamoDBLocal.jar','-dbPath','/dynamo-data','-sharedDb','-disableTelemetry','-port','8000')
function New-RetainedDynamoContainer {
    & wsl -d Ubuntu -u root -- podman image exists $image
    if($LASTEXITCODE){throw 'Retained pinned Java-capable image unavailable; no download permitted.'}
    $null=& wsl -d Ubuntu -u root -- podman create --pull=never --name $name --label one.oasis.provider-test=dynamo-retained-loopback --image-volume=ignore --read-only --cap-drop=ALL --network bridge --memory=512m --memory-swap=512m --pids-limit=128 --tmpfs /tmp:rw,size=67108864 --publish 127.0.0.1:18000:8000 --volume "${runtime}:/dynamo:ro" --volume "${data}:/dynamo-data:rw" --workdir /dynamo --entrypoint java $image @arguments
    if($LASTEXITCODE){throw 'Retained loopback container creation failed; no alternate runtime.'}
}
if(!$Stop -and (Get-FileHash -LiteralPath (Join-Path $runtimeWindows 'DynamoDBLocal.jar')).Hash -ne '67ED9662164DC9765FF3ABCA1C2F6F4EDB5F6A92269812450215A679BDC71620'){throw 'Pinned retained JAR differs; no replacement/download.'}
if(!$Stop -and (Get-NetTCPConnection -State Listen -LocalPort 18000,18001 -ErrorAction SilentlyContinue)){throw 'Retained Dynamo ports occupied; no duplicate service.'}
& wsl -d Ubuntu -u root -- podman container exists $name
$exists=$LASTEXITCODE -eq 0
if(!$exists){
    if($Stop){throw 'Owned retained Dynamo container is absent; no replacement on teardown.'}
    New-RetainedDynamoContainer
}
$raw=(& wsl -d Ubuntu -u root -- podman inspect $name)-join "`n"
if($LASTEXITCODE){throw 'Owned container inspection failed.'}
$container=($raw|ConvertFrom-Json)[0]
$bindings=$container.HostConfig.PortBindings.'8000/tcp'
$runtimeMount=@($container.Mounts|Where-Object {$_.Destination -eq '/dynamo'})
$dataMount=@($container.Mounts|Where-Object {$_.Destination -eq '/dynamo-data'})
if($container.Image -ne $image -or $container.Config.Labels.'one.oasis.provider-test' -ne 'dynamo-retained-loopback' -or
    @($container.HostConfig.PortBindings.PSObject.Properties).Count -ne 1 -or @($bindings).Count -ne 1 -or
    $bindings[0].HostIp -ne '127.0.0.1' -or $bindings[0].HostPort -ne '18000' -or
    $runtimeMount.Count -ne 1 -or $runtimeMount[0].RW -or
    $dataMount.Count -ne 1 -or !$dataMount[0].RW -or
    $container.Mounts.Count -ne 2 -or !$container.HostConfig.ReadonlyRootfs -or $container.HostConfig.NetworkMode -ne 'bridge' -or
    ($container.Config.Entrypoint -join '') -ne 'java' -or $container.Config.WorkingDir -ne '/dynamo' -or
    ($container.Config.Cmd -join "`n") -ne ($arguments -join "`n")){
    throw 'Retained Dynamo identity/image/bindings/mounts/command differ; never adopt or alter it.'
}
if($runtimeMount[0].Source -ne $runtime -or $dataMount[0].Source -ne $data){
    if(!$RebindMovedEvidence -or $Stop -or $container.State.Status -ne 'exited'){throw 'Moved evidence needs an explicit stopped-container rebind; no old directory will be recreated.'}
    if(!(Test-Path -LiteralPath $runtimeWindows) -or !(Test-Path -LiteralPath $dataWindows)){throw 'Both moved evidence directories must already exist.'}
    foreach($oldMount in @($runtimeMount[0],$dataMount[0])){
        $oldWindows=(& wsl -d Ubuntu -- wslpath -w $oldMount.Source).Trim()
        if($LASTEXITCODE -or (Test-Path -LiteralPath $oldWindows)){throw 'Previous mount source still exists or cannot be resolved; inspect instead of replacing its owner.'}
    }
    $relocationPath=Join-Path (Split-Path $runtimeWindows) 'container-path-relocation.json'
    if(Test-Path -LiteralPath $relocationPath){throw 'Preserve existing relocation evidence before another cutover.'}
    $movedFiles=@((Get-Item -LiteralPath (Join-Path $runtimeWindows 'DynamoDBLocal.jar')))+(Get-ChildItem -LiteralPath $dataWindows -File)
    $movedHashes=@($movedFiles | ForEach-Object { @{path=$_.FullName;sha256=(Get-FileHash -LiteralPath $_.FullName).Hash} })
    $relocation=@{oldContainer=$container.Id;oldRuntimeSource=$runtimeMount[0].Source;oldDataSource=$dataMount[0].Source;runtimeSource=$runtime;dataSource=$data;files=$movedHashes;state='admitted'}
    $relocation | ConvertTo-Json -Depth 6 | Set-Content -LiteralPath $relocationPath
    # Only the validated stopped definition is removed, without -v. Bind data stays in place.
    & wsl -d Ubuntu -u root -- podman rm $container.Id | Out-Null
    if($LASTEXITCODE){throw 'Stopped container definition removal failed; no data deletion attempted.'}
    New-RetainedDynamoContainer
    foreach($file in $movedHashes){if((Get-FileHash -LiteralPath $file.path).Hash -ne $file.sha256){throw 'Moved file changed during rebind.'}}
    $relocation.state='verified';$relocation | ConvertTo-Json -Depth 6 | Set-Content -LiteralPath $relocationPath
    & $PSCommandPath -InitializeSyntheticStore:$InitializeSyntheticStore
    return
}
if($Stop){
    if($container.State.Running){& wsl -d Ubuntu -u root -- podman stop -t 20 $name|Out-Null;if($LASTEXITCODE){throw 'Owned Dynamo teardown failed.'}}
    # Native stop acknowledgement can precede release of Windows/WSL forwarding
    # listeners. Observe actual vacancy; never kill a shared relay or adopt a port.
    $deadline=[DateTime]::UtcNow.AddSeconds(30)
    while(Get-NetTCPConnection -State Listen -LocalPort 18000,18001 -ErrorAction SilentlyContinue){
        if([DateTime]::UtcNow -ge $deadline){throw 'Retained Dynamo stopped, but reserved listeners remain; no shared process will be killed.'}
        Start-Sleep -Milliseconds 200
    }
    Assert-BoundedDynamoStore -Force:$CompactStore
    return
}
if($container.State.Running){throw 'Retained Dynamo already running; do not adopt another run.'}
if(!(Test-Path -LiteralPath $dataWindows)){
    if(!$InitializeSyntheticStore){throw 'Historical Dynamo store is absent; explicitly initialize a new synthetic store, never imply historical recovery.'}
    New-Item -ItemType Directory -Path $dataWindows | Out-Null
    @{createdUtc=[DateTime]::UtcNow.ToString('o');purpose='New synthetic conformance store; historical data directory was absent';historicalDataRestored=$false;owner='HyperDrive provider breadth';runtimeJarSha256='67ED9662164DC9765FF3ABCA1C2F6F4EDB5F6A92269812450215A679BDC71620'} | ConvertTo-Json | Set-Content (Join-Path $dataWindows 'synthetic-store-provenance.json')
}elseif($InitializeSyntheticStore){throw 'Existing data directory must be reused, not reinitialized.'}
Assert-BoundedDynamoStore
$started=$false
try{
    & wsl -d Ubuntu -u root -- podman start $name|Out-Null
    if($LASTEXITCODE){throw 'Retained Dynamo start failed.'};$started=$true
    $deadline=[DateTime]::UtcNow.AddSeconds(30);$ready=$false
    $http=[Net.Http.HttpClient]::new();$http.Timeout=[TimeSpan]::FromSeconds(2)
    try{do{
        $state=(& wsl -d Ubuntu -u root -- podman inspect --format '{{.State.Running}}' $name)-join ''
        if($LASTEXITCODE -or $state -ne 'true'){throw 'Retained Dynamo exited during readiness; preserve logs/data.'}
        # WSL's forwarding socket can accept TCP before Jetty listens. Require the
        # actual Dynamo protocol response to an unsigned, non-mutating GET.
        $response=$null
        try{
            $response=$http.GetAsync('http://127.0.0.1:18000').GetAwaiter().GetResult()
            $body=$response.Content.ReadAsStringAsync().GetAwaiter().GetResult()|ConvertFrom-Json
            if([int]$response.StatusCode -ne 400 -or $body.__type -ne 'com.amazonaws.dynamodb.v20120810#MissingAuthenticationToken'){
                throw [IO.InvalidDataException]::new('Unexpected retained Dynamo readiness protocol; do not adopt another endpoint.')
            }
            $ready=$true
        }catch [Net.Http.HttpRequestException]{}catch [Threading.Tasks.TaskCanceledException]{}
        finally{if($response){$response.Dispose()}}
        if(!$ready){Start-Sleep -Milliseconds 200}
    }while(!$ready -and [DateTime]::UtcNow -lt $deadline)}finally{$http.Dispose()}
    if(!$ready){throw 'Retained Dynamo loopback readiness deadline.'}
    [pscustomobject]@{Name=$name;Image=$image;Endpoint='http://127.0.0.1:18000'}
}catch{
    if($started){& wsl -d Ubuntu -u root -- podman stop -t 20 $name|Out-Null}
    throw
}
