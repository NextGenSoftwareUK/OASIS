#Requires -Version 7.0
[CmdletBinding()]
param([switch]$Stop)
$ErrorActionPreference='Stop'
# Caller holds the existing AWS environment lease. Same JAR/data, no downloads or copies.
$name='oasis-dynamo-retained'
$image='6e47503356fe6b0e8f3ba48afc714ed95475d0c32ac97c62b30737af2e8846bd'
$runtime='/mnt/c/Source/OASIS-hyperdrive-v2-gaps/TestResults/AWSProviderEvidence/runtime'
$data='/mnt/c/Source/OASIS-hyperdrive-v2-gaps/TestResults/AWSProviderEvidence/retained-history-data'
$arguments=@('-Xms32m','-Xmx256m','-Djava.library.path=/dynamo/DynamoDBLocal_lib','-jar','/dynamo/DynamoDBLocal.jar','-dbPath','/dynamo-data','-sharedDb','-disableTelemetry','-port','8000')
if(!$Stop -and (Get-FileHash -LiteralPath 'C:/Source/OASIS-hyperdrive-v2-gaps/TestResults/AWSProviderEvidence/runtime/DynamoDBLocal.jar').Hash -ne '72640F61583045B630971AF8D9F34AC311CF3FD0A7F077EA96EC0D04BCBE1E10'){throw 'Pinned retained JAR differs; no replacement/download.'}
if(!$Stop -and (Get-NetTCPConnection -State Listen -LocalPort 18000,18001 -ErrorAction SilentlyContinue)){throw 'Retained Dynamo ports occupied; no duplicate service.'}
& wsl -d Ubuntu -u root -- podman container exists $name
$exists=$LASTEXITCODE -eq 0
if(!$exists){
    if($Stop){throw 'Owned retained Dynamo container is absent; no replacement on teardown.'}
    & wsl -d Ubuntu -u root -- podman image exists $image
    if($LASTEXITCODE){throw 'Retained pinned Java-capable image unavailable; no download permitted.'}
    $created=& wsl -d Ubuntu -u root -- podman create --pull=never --name $name --label one.oasis.provider-test=dynamo-retained-loopback --image-volume=ignore --read-only --cap-drop=ALL --network bridge --memory=512m --memory-swap=512m --pids-limit=128 --tmpfs /tmp:rw,size=67108864 --publish 127.0.0.1:18000:8000 --volume "${runtime}:/dynamo:ro" --volume "${data}:/dynamo-data:rw" --workdir /dynamo --entrypoint java $image @arguments
    if($LASTEXITCODE){throw 'Retained loopback container creation failed; no alternate runtime.'}
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
    $runtimeMount.Count -ne 1 -or $runtimeMount[0].Source -ne $runtime -or $runtimeMount[0].RW -or
    $dataMount.Count -ne 1 -or $dataMount[0].Source -ne $data -or !$dataMount[0].RW -or
    $container.Mounts.Count -ne 2 -or !$container.HostConfig.ReadonlyRootfs -or $container.HostConfig.NetworkMode -ne 'bridge' -or
    ($container.Config.Entrypoint -join '') -ne 'java' -or $container.Config.WorkingDir -ne '/dynamo' -or
    ($container.Config.Cmd -join "`n") -ne ($arguments -join "`n")){
    throw 'Retained Dynamo identity/image/bindings/mounts/command differ; never adopt or alter it.'
}
if($Stop){
    if($container.State.Running){& wsl -d Ubuntu -u root -- podman stop -t 20 $name|Out-Null;if($LASTEXITCODE){throw 'Owned Dynamo teardown failed.'}}
    return
}
if($container.State.Running){throw 'Retained Dynamo already running; do not adopt another run.'}
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
