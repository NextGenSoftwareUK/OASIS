[CmdletBinding()]
param([switch]$KeepArtifacts,[string]$PythonPath,[ValidateSet('Both','Concurrency','RestartReplay')][string]$Mode='Both')
$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSScriptRoot
$manage = Join-Path $PSScriptRoot 'manage_local_geohotspot_resilience_host.ps1'
$test = Join-Path $PSScriptRoot 'test_geohotspot_live_resilience.ps1'
$state = Join-Path ([IO.Path]::GetTempPath()) ("oasis-geohotspot-resilience-" + [Guid]::NewGuid().ToString('N'))
$hotspot = [Guid]::NewGuid().ToString('D')
$tokenOne = 'local-avatar-one-' + [Guid]::NewGuid().ToString('N')
$tokenTwo = 'local-avatar-two-' + [Guid]::NewGuid().ToString('N')

function Wait-Healthy([int]$Port) {
    $deadline = [DateTime]::UtcNow.AddSeconds(30)
    do {
        try { $null = Invoke-RestMethod "http://127.0.0.1:$Port/api/health" -TimeoutSec 1; return }
        catch { Start-Sleep -Milliseconds 100 }
    } while ([DateTime]::UtcNow -lt $deadline)
    throw "Local resilience host on port $Port did not become healthy."
}
function Stop-All {
    foreach ($port in 5055,5056) { & $manage -Action Stop -StateDirectory $state -Port $port }
}
function Write-Fixture([int]$delay) {
    $stop = "& '$manage' -Action Stop -StateDirectory '$state' -Port 5055"
    $start = "& '$manage' -Action Start -StateDirectory '$state' -Port 5055 -DelayMilliseconds $delay" + $(if ($PythonPath) { " -PythonPath '$PythonPath'" } else { '' })
    $fixture = [ordered]@{
        web5BaseUrls = @('http://127.0.0.1:5055','http://127.0.0.1:5056')
        jwtTokens = @($tokenOne,$tokenTwo)
        hotSpotId = $hotspot; triggerType = 'WhenArrivedAtGeoLocation'
        latitude = 31.54998; longitude = 74.27728; accuracyMetres = 1
        continuousDurationSeconds = 0; gameSource = 'Our World'; expectedAcceptedCount = 1
        interruptDelayMilliseconds = 250; restartStopCommand = $stop; restartStartCommand = $start
    }
    $path = Join-Path $state 'fixture.json'
    $fixture | ConvertTo-Json -Depth 5 | Set-Content -LiteralPath $path -Encoding utf8
    return $path
}

New-Item -ItemType Directory -Path $state | Out-Null
Set-Content -LiteralPath (Join-Path $state 'hotspot-id.txt') -Value $hotspot -NoNewline
@{$tokenOne=[Guid]::NewGuid().ToString('D');$tokenTwo=[Guid]::NewGuid().ToString('D')} | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $state 'tokens.json') -Encoding utf8
try {
    if ($Mode -in @('Both','Concurrency')) {
        & $manage -Action Start -StateDirectory $state -Port 5055 -PythonPath $PythonPath
        Wait-Healthy 5055
        & $manage -Action Start -StateDirectory $state -Port 5056 -PythonPath $PythonPath
        Wait-Healthy 5056
        $fixture = Write-Fixture 0
        & $test -FixturePath $fixture -Mode Concurrency
    }

    if ($Mode -in @('Both','RestartReplay')) {
        Stop-All
        Get-ChildItem -LiteralPath $state -Filter 'state.sqlite3*' | Remove-Item -Force
        & $manage -Action Start -StateDirectory $state -Port 5055 -DelayMilliseconds 1500 -PythonPath $PythonPath
        Wait-Healthy 5055
        $fixture = Write-Fixture 0
        & $test -FixturePath $fixture -Mode RestartReplay
    }
    Write-Host "PASS disposable local GeoHotSpot concurrency and restart/replay suite."
}
finally {
    Stop-All
    if ($KeepArtifacts) { Write-Host "Artifacts retained at $state" }
    else { Remove-Item -LiteralPath $state -Recurse -Force -ErrorAction SilentlyContinue }
}
