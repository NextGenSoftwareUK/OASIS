#Requires -Version 7.0
[CmdletBinding()]
param([ValidateRange(1,120)][int]$MaximumWaitSeconds=120)
$ErrorActionPreference='Stop'
# Caller owns SolidCurrent/environment.lock. Observe only the retained runtime;
# no download, start, HTTP/authentication retry or record mutation here.
$taskListenerRaw=& wsl -d Ubuntu -u root -- podman inspect oasis-solid
if($LASTEXITCODE -ne 0){throw 'Retained SOLID container unavailable.'}
$taskListenerContainer=($taskListenerRaw|ConvertFrom-Json)[0]
$taskListenerBindings=@($taskListenerContainer.HostConfig.PortBindings.'13000/tcp')
$taskListenerMounts=@($taskListenerContainer.Mounts)
if($taskListenerContainer.Image -ne '58bee7c3f6a20498d72eb6f8334ba84299b6df2c8fca119ce805fe8614512d7a' -or
    !$taskListenerContainer.State.Running -or $taskListenerBindings.Count -ne 1 -or
    $taskListenerBindings[0].HostIp -ne '127.0.0.1' -or $taskListenerBindings[0].HostPort -ne '13000' -or
    $taskListenerMounts.Count -ne 1 -or $taskListenerMounts[0].Name -ne 'oasis-solid-data' -or $taskListenerMounts[0].Destination -ne '/data'){
    throw 'Retained SOLID runtime identity/binding/volume differs; no replacement attempted.'
}
$taskListenerTimer=[Diagnostics.Stopwatch]::StartNew()
$taskListenerSamples=[Collections.Generic.List[object]]::new()
while($taskListenerTimer.Elapsed.TotalSeconds -lt $MaximumWaitSeconds){
    $taskListenerSockets=& wsl -d Ubuntu -u root -- podman exec oasis-solid cat /proc/net/tcp /proc/net/tcp6
    if($LASTEXITCODE -ne 0){throw 'Retained SOLID socket observation failed.'}
    $taskListenerReady=@($taskListenerSockets|Where-Object {$_ -match '^\s*\d+:\s+\S+:32C8\s+\S+\s+0A\s'}).Count -gt 0
    $taskListenerSamples.Add([pscustomobject]@{elapsedMs=$taskListenerTimer.ElapsedMilliseconds;listening=$taskListenerReady})
    if($taskListenerReady){return [pscustomobject]@{ReadyAfterMs=$taskListenerTimer.ElapsedMilliseconds;Samples=$taskListenerSamples}}
    Start-Sleep -Milliseconds 1000
}
throw "Retained SOLID did not bind port13000 within ${MaximumWaitSeconds}seconds; no HTTP/authentication request dispatched."
