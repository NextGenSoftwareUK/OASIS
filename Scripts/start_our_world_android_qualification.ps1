[CmdletBinding()]
param(
    [Parameter(Mandatory)] [string]$ApkPath,
    [string]$OutputDirectory = 'artifacts/our-world-physical-qualification',
    [string]$PackageName = 'com.NextGen-World-Ltd.OASIS-Omniverse',
    [switch]$Install,
    [switch]$AllowMoreThanTwoDevices
)

$ErrorActionPreference = 'Stop'
$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path

function Resolve-Adb {
    $command = Get-Command adb -ErrorAction SilentlyContinue
    if ($command) { return $command.Source }

    $candidates = @(
        (Join-Path $env:LOCALAPPDATA 'Android\Sdk\platform-tools\adb.exe'),
        'C:\Program Files (x86)\Android\android-sdk\platform-tools\adb.exe'
    )
    $unityEditors = 'C:\Program Files\Unity\Hub\Editor'
    if (Test-Path -LiteralPath $unityEditors) {
        $candidates += @(Get-ChildItem -LiteralPath $unityEditors -Directory -ErrorAction SilentlyContinue |
            Sort-Object Name -Descending |
            ForEach-Object { Join-Path $_.FullName 'Editor\Data\PlaybackEngines\AndroidPlayer\SDK\platform-tools\adb.exe' })
    }
    foreach ($candidate in $candidates) {
        if (-not [string]::IsNullOrWhiteSpace($candidate) -and (Test-Path -LiteralPath $candidate -PathType Leaf)) {
            return (Resolve-Path -LiteralPath $candidate).Path
        }
    }
    throw 'Android platform-tools were not found. Install the Unity Android Build Support SDK or Android SDK platform-tools.'
}

function Invoke-Adb {
    param([Parameter(Mandatory)][string[]]$Arguments, [switch]$AllowFailure)
    $output = & $script:adb @Arguments 2>&1
    $exitCode = $LASTEXITCODE
    if (-not $AllowFailure -and $exitCode -ne 0) {
        throw "adb $($Arguments -join ' ') failed with exit code $exitCode.`n$($output -join [Environment]::NewLine)"
    }
    return @($output)
}

function Get-DeviceProperty {
    param([string]$Serial, [string]$Name)
    return ((Invoke-Adb -Arguments @('-s', $Serial, 'shell', 'getprop', $Name)) -join '').Trim()
}

$resolvedApk = (Resolve-Path -LiteralPath $ApkPath).Path
if ([IO.Path]::GetExtension($resolvedApk) -ne '.apk') { throw 'ApkPath must identify an APK file.' }
$resolvedOutput = [IO.Path]::GetFullPath((Join-Path $repoRoot $OutputDirectory))
$repoPrefix = $repoRoot.TrimEnd([IO.Path]::DirectorySeparatorChar) + [IO.Path]::DirectorySeparatorChar
if (-not $resolvedOutput.StartsWith($repoPrefix, [StringComparison]::OrdinalIgnoreCase)) {
    throw "OutputDirectory must remain inside the OASIS workspace: '$resolvedOutput'."
}

$adb = Resolve-Adb
$deviceLines = Invoke-Adb -Arguments @('devices', '-l')
$serials = @($deviceLines | Select-Object -Skip 1 | ForEach-Object {
    if ($_ -match '^(\S+)\s+device(?:\s|$)' -and $_ -notmatch '^emulator-') { $Matches[1] }
})
if ($serials.Count -lt 2) {
    throw "HoloOASIS physical qualification requires two authorized physical Android devices; adb found $($serials.Count). Connect both phones with USB debugging enabled."
}
if (-not $AllowMoreThanTwoDevices -and $serials.Count -ne 2) {
    throw "Expected exactly two physical Android devices but found $($serials.Count). Disconnect unrelated devices or pass -AllowMoreThanTwoDevices."
}

$runId = 'holo-android-{0}-{1}' -f ([DateTime]::UtcNow.ToString('yyyyMMddTHHmmssZ')), ([Guid]::NewGuid().ToString('N').Substring(0, 8))
$runDirectory = Join-Path $resolvedOutput $runId
New-Item -ItemType Directory -Path $runDirectory -Force | Out-Null
$apkHash = (Get-FileHash -LiteralPath $resolvedApk -Algorithm SHA256).Hash
$sourceCommit = (& git -C $repoRoot rev-parse HEAD).Trim()
if ($LASTEXITCODE -ne 0 -or $sourceCommit -notmatch '^[0-9a-fA-F]{40}$') { throw 'Unable to resolve the OASIS source commit.' }

$devices = [Collections.Generic.List[object]]::new()
foreach ($serial in $serials) {
    $isEmulator = Get-DeviceProperty $serial 'ro.kernel.qemu'
    if ($isEmulator -eq '1') { throw "Device '$serial' is an emulator; physical evidence cannot use it." }
    $abis = Get-DeviceProperty $serial 'ro.product.cpu.abilist'
    if ($abis -notmatch '(^|,)arm64-v8a(,|$)') { throw "Device '$serial' does not advertise arm64-v8a: '$abis'." }

    $device = [ordered]@{
        id = $serial
        platform = 'Android'
        manufacturer = Get-DeviceProperty $serial 'ro.product.manufacturer'
        model = Get-DeviceProperty $serial 'ro.product.model'
        osVersion = Get-DeviceProperty $serial 'ro.build.version.release'
        apiLevel = Get-DeviceProperty $serial 'ro.build.version.sdk'
        architecture = 'arm64-v8a'
        abiList = $abis
        buildFingerprint = Get-DeviceProperty $serial 'ro.build.fingerprint'
    }
    $devices.Add([pscustomobject]$device)

    $deviceDirectory = Join-Path $runDirectory $serial
    New-Item -ItemType Directory -Path $deviceDirectory -Force | Out-Null
    (Invoke-Adb -Arguments @('-s', $serial, 'shell', 'dumpsys', 'battery')) |
        Set-Content -Encoding UTF8 -LiteralPath (Join-Path $deviceDirectory 'battery-before.txt')
    (Invoke-Adb -Arguments @('-s', $serial, 'shell', 'dumpsys', 'meminfo', $PackageName) -AllowFailure) |
        Set-Content -Encoding UTF8 -LiteralPath (Join-Path $deviceDirectory 'meminfo-before.txt')
    if ($Install) {
        (Invoke-Adb -Arguments @('-s', $serial, 'install', '-r', $resolvedApk)) |
            Set-Content -Encoding UTF8 -LiteralPath (Join-Path $deviceDirectory 'install.log')
        (Invoke-Adb -Arguments @('-s', $serial, 'shell', 'dumpsys', 'package', $PackageName)) |
            Set-Content -Encoding UTF8 -LiteralPath (Join-Path $deviceDirectory 'package-after-install.txt')
    }
}

$session = [ordered]@{
    schemaVersion = 1
    runId = $runId
    capturedAtUtc = [DateTime]::UtcNow.ToString('O')
    sourceCommit = $sourceCommit.ToLowerInvariant()
    profile = 'HoloEnabled'
    apk = [ordered]@{ path = $resolvedApk; sha256 = $apkHash; bytes = (Get-Item -LiteralPath $resolvedApk).Length }
    packageName = $PackageName
    adb = $adb
    installed = [bool]$Install
    devices = $devices
    qualificationStatus = 'PENDING'
    note = 'Inventory/setup evidence only. It is not PASS evidence for any physical acceptance case.'
}
$sessionPath = Join-Path $runDirectory 'android-qualification-session.json'
$session | ConvertTo-Json -Depth 8 | Set-Content -Encoding UTF8 -LiteralPath $sessionPath

$hashes = Get-ChildItem -LiteralPath $runDirectory -File -Recurse | Sort-Object FullName | ForEach-Object {
    [ordered]@{
        path = [IO.Path]::GetRelativePath($runDirectory, $_.FullName).Replace('\', '/')
        sha256 = (Get-FileHash -LiteralPath $_.FullName -Algorithm SHA256).Hash
    }
}
$hashes | ConvertTo-Json -Depth 4 | Set-Content -Encoding UTF8 -LiteralPath (Join-Path $runDirectory 'SHA256SUMS.json')
Write-Host "Prepared physical Android qualification run '$runId' for $($devices.Count) devices."
Write-Host "Session: $sessionPath"
Write-Host 'No acceptance case was marked PASS; execute and capture the required lifecycle, profiling and convergence cases next.'
