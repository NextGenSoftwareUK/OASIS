#Requires -Version 7.0
[CmdletBinding()]
param([switch]$Stop)
$ErrorActionPreference='Stop'
# Caller owns LocalState/COSMIC/AwsCql/environment.lock. Shared runtime is read-only.
$root=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
$name='oasis-cosmic-cql-dynamo'
$image='6e47503356fe6b0e8f3ba48afc714ed95475d0c32ac97c62b30737af2e8846bd'
$runtime='C:/Source/OASIS/TestResults/AWSProviderEvidence/runtime'
$data=Join-Path $root 'LocalState/COSMIC/AwsCql/dynamo-data'
$arguments=@('-Xms32m','-Xmx256m','-Djava.library.path=/dynamo/DynamoDBLocal_lib','-jar','/dynamo/DynamoDBLocal.jar','-dbPath','/dynamo-data','-sharedDb','-disableTelemetry','-port','8000')
if(!(Test-Path -LiteralPath (Join-Path $runtime 'DynamoDBLocal.jar')) -or
    (Get-FileHash -LiteralPath (Join-Path $runtime 'DynamoDBLocal.jar')).Hash -ne '67ED9662164DC9765FF3ABCA1C2F6F4EDB5F6A92269812450215A679BDC71620'){throw 'Shared runtime missing or changed; no download/replacement.'}
$runtimeWsl=(& wsl -d Ubuntu -- wslpath -u $runtime).Trim()
if($LASTEXITCODE){throw 'Runtime path resolution failed.'}
$dataWsl=(& wsl -d Ubuntu -- wslpath -u ($data.Replace('\','/'))).Trim()
if($LASTEXITCODE){throw 'Data path resolution failed.'}
& wsl -d Ubuntu -u root -- podman container exists $name
$exists=$LASTEXITCODE
if($exists -notin 0,1){throw 'Cannot inspect CQL environment.'}
if($exists -eq 1){
    if($Stop){return}
    if(Get-NetTCPConnection -State Listen -LocalPort 18002 -ErrorAction SilentlyContinue){throw 'CQL Dynamo port occupied.'}
    & wsl -d Ubuntu -u root -- podman image exists $image
    if($LASTEXITCODE){throw 'Pinned image unavailable; no pull permitted.'}
    New-Item -ItemType Directory -Force -Path $data | Out-Null
    & wsl -d Ubuntu -u root -- podman create --pull=never --name $name --label one.oasis.provider-test=cosmic-cql-dynamo-loopback --image-volume=ignore --read-only --cap-drop=ALL --network bridge --memory=512m --memory-swap=512m --pids-limit=128 --tmpfs /tmp:rw,size=67108864 --publish 127.0.0.1:18002:8000 --volume "${runtimeWsl}:/dynamo:ro" --volume "${dataWsl}:/dynamo-data:rw" --workdir /dynamo --entrypoint java $image @arguments | Out-Null
    if($LASTEXITCODE){throw 'CQL environment creation failed.'}
}
$raw=& wsl -d Ubuntu -u root -- podman inspect $name
if($LASTEXITCODE){throw 'CQL container inspection failed.'}
$container=@(($raw -join "`n") | ConvertFrom-Json)[0]
$runtimeMount=@($container.Mounts|Where-Object Destination -eq '/dynamo')
$dataMount=@($container.Mounts|Where-Object Destination -eq '/dynamo-data')
$binding=@($container.HostConfig.PortBindings.'8000/tcp')
if($container.Image -ne $image -or $container.Config.Labels.'one.oasis.provider-test' -ne 'cosmic-cql-dynamo-loopback' -or
    $runtimeMount.Count -ne 1 -or $runtimeMount[0].Source -ne $runtimeWsl -or $runtimeMount[0].RW -or
    $dataMount.Count -ne 1 -or $dataMount[0].Source -ne $dataWsl -or !$dataMount[0].RW -or
    $container.Mounts.Count -ne 2 -or !$container.HostConfig.ReadonlyRootfs -or
    $binding.Count -ne 1 -or $binding[0].HostIp -ne '127.0.0.1' -or $binding[0].HostPort -ne '18002' -or
    ($container.Config.Entrypoint -join '') -ne 'java' -or
    ($container.Config.Cmd -join "`n") -ne ($arguments -join "`n")){throw 'Environment identity/mounts/port/command differ; no replacement.'}
if($Stop){
    if($container.State.Running){& wsl -d Ubuntu -u root -- podman stop -t 20 $name | Out-Null;if($LASTEXITCODE){throw 'CQL Dynamo stop failed.'}}
    return
}
if($container.State.Running){throw 'CQL Dynamo already running; do not adopt another run.'}
& wsl -d Ubuntu -u root -- podman start $name | Out-Null
if($LASTEXITCODE){throw 'CQL Dynamo start failed.'}
try{
    $deadline=[DateTime]::UtcNow.AddSeconds(30);$ready=$false
    $http=[Net.Http.HttpClient]::new();$http.Timeout=[TimeSpan]::FromSeconds(2)
    try{do{
        $running=(& wsl -d Ubuntu -u root -- podman inspect --format '{{.State.Running}}' $name)-join ''
        if($LASTEXITCODE -or $running -ne 'true'){throw 'CQL Dynamo exited during readiness; preserve logs/data.'}
        $response=$null
        try{
            $response=$http.GetAsync('http://127.0.0.1:18002').GetAwaiter().GetResult()
            $body=$response.Content.ReadAsStringAsync().GetAwaiter().GetResult()|ConvertFrom-Json
            if([int]$response.StatusCode -ne 400 -or $body.__type -ne 'com.amazonaws.dynamodb.v20120810#MissingAuthenticationToken'){
                throw [IO.InvalidDataException]::new('Unexpected Dynamo readiness response; do not adopt another endpoint.')
            }
            $ready=$true
        }catch [Net.Http.HttpRequestException]{}catch [Threading.Tasks.TaskCanceledException]{}
        finally{if($response){$response.Dispose()}}
        if(!$ready){Start-Sleep -Milliseconds 200}
    }while(!$ready -and [DateTime]::UtcNow -lt $deadline)}finally{$http.Dispose()}
    if(!$ready){throw 'CQL Dynamo readiness deadline exceeded.'}
}catch{
    & wsl -d Ubuntu -u root -- podman stop -t 20 $name | Out-Null
    throw
}
Write-Output 'DynamoDB Local at http://127.0.0.1:18002; shared runtime read-only, own persistent data.'
