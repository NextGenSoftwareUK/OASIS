param(
    [ValidateSet('Resume', 'Pause', 'Status')][string]$Action = 'Status',
    [switch]$InstallMissing
)
$ErrorActionPreference = 'Stop'
$runtimeName = 'oasis-tron-tre'
$runtimeImage = 'docker.io/tronbox/tre@sha256:cbf2973095933e3ae3237767082e29add2f9b647d7a4aebdfb7a953c6e5656ba'
function Invoke-Podman {
    param([string[]]$Arguments)
    $result = & wsl.exe -u root -e podman @Arguments
    if ($LASTEXITCODE -ne 0) { throw "Podman failed: $($Arguments[0])" }
    return $result
}
& wsl.exe -u root -e podman container exists $runtimeName
$exists = $LASTEXITCODE -eq 0
if (!$exists) {
    if ($Action -ne 'Resume') { throw 'Reusable TRON runtime has not been created.' }
    & wsl.exe -u root -e podman image exists $runtimeImage
    if ($LASTEXITCODE -ne 0) {
        if (!$InstallMissing) { throw 'Pinned runtime is not cached. Installation is explicit: use -InstallMissing once.' }
        if ((Get-PSDrive C).Free -lt 5GB) { throw 'Insufficient free space for bounded runtime installation.' }
        Invoke-Podman -Arguments @('pull', $runtimeImage) | Out-Null
    }
    Invoke-Podman -Arguments @('run', '-d', '--name', $runtimeName, '--pull=never', '--memory=2g', '--cpus=2',
        '--log-opt', 'max-size=10mb', '-p', '127.0.0.1:9090:9090', '-p', '127.0.0.1:50051:50051',
        '-p', '127.0.0.1:50052:50052', '-e', 'useDefaultPrivateKey=true', '-e', 'accounts=2', '-e', 'NO_COLOR=1', $runtimeImage) | Out-Null
}
$inspection = (Invoke-Podman -Arguments @('inspect', $runtimeName) | Out-String | ConvertFrom-Json)[0]
if ($inspection.ImageName -ne $runtimeImage) { throw 'Existing runtime image differs from the immutable pin; do not replace it automatically.' }
$state = $inspection.State.Status
if ($Action -eq 'Pause' -and $state -eq 'running') {
    Invoke-Podman -Arguments @('pause', $runtimeName) | Out-Null
    $state = 'paused'
} elseif ($Action -eq 'Resume') {
    if ($state -eq 'paused') { Invoke-Podman -Arguments @('unpause', $runtimeName) | Out-Null; $state = 'running' }
    if ($state -ne 'running') { throw 'Runtime stopped unexpectedly. Preserve its ledger and investigate; do not recreate or rerun startup automatically.' }
    $ready = $false
    for ($attempt = 0; $attempt -lt 20; $attempt++) {
        try {
            $health = Invoke-WebRequest -Uri 'http://127.0.0.1:9090/healthcheck' -TimeoutSec 2
            if ($health.Content.Trim() -eq 'OK') { $ready = $true; break }
        } catch { Start-Sleep -Milliseconds 500 }
    }
    if (!$ready) { throw 'Existing runtime did not become healthy within the bounded verification window.' }
}
[pscustomobject]@{ Name = $runtimeName; State = $state; Image = $runtimeImage; FreeBytes = (Get-PSDrive C).Free }
