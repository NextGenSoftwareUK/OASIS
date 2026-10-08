[CmdletBinding()]
param(
    [string]$UnityEditor = 'C:\Program Files\Unity\Hub\Editor\2022.3.62f3\Editor\Unity.exe',
    [string]$PackageDirectory = 'artifacts\unity\com.nextgensoftware.oasis.edge',
    [string]$LogDirectory = 'artifacts',
    [ValidateRange(1, 120)]
    [int]$EditorValidationTimeoutMinutes = 20,
    [ValidateRange(1, 240)]
    [int]$AndroidBuildTimeoutMinutes = 90
)

$ErrorActionPreference = 'Stop'
$repoRoot = Split-Path $PSScriptRoot -Parent
$artifactsRoot = [IO.Path]::GetFullPath((Join-Path $repoRoot 'artifacts'))
$packageRoot = [IO.Path]::GetFullPath((Join-Path $repoRoot $PackageDirectory))
$logRoot = [IO.Path]::GetFullPath((Join-Path $repoRoot $LogDirectory))
if (-not $packageRoot.StartsWith($artifactsRoot, [StringComparison]::OrdinalIgnoreCase) -or
    -not (Test-Path -LiteralPath (Join-Path $packageRoot 'build-manifest.json') -PathType Leaf)) {
    throw "A generated, manifested Unity Edge package inside '$artifactsRoot' is required."
}
if (-not $logRoot.StartsWith($artifactsRoot, [StringComparison]::OrdinalIgnoreCase)) {
    throw "The Unity validation log directory must remain inside '$artifactsRoot'."
}
New-Item -ItemType Directory -Path $logRoot -Force | Out-Null
if (-not (Test-Path -LiteralPath $UnityEditor -PathType Leaf)) { throw "Unity editor not found: $UnityEditor" }

function Invoke-UnityBatchProcess {
    param(
        [Parameter(Mandatory)] [string]$Phase,
        [Parameter(Mandatory)] [string[]]$Arguments,
        [Parameter(Mandatory)] [string]$LogPath,
        [Parameter(Mandatory)] [int]$TimeoutMinutes
    )

    Write-Host "Starting Unity $Phase validation (timeout: $TimeoutMinutes minutes)."
    $process = Start-Process -FilePath $UnityEditor -ArgumentList $Arguments `
        -WindowStyle Hidden -PassThru
    $deadline = [DateTime]::UtcNow.AddMinutes($TimeoutMinutes)
    $nextProgress = [DateTime]::UtcNow.AddMinutes(1)
    while (-not $process.WaitForExit(5000)) {
        if ([DateTime]::UtcNow -ge $deadline) {
            try { $process.Kill($true) } catch { Write-Warning "Unable to terminate timed-out Unity process tree: $($_.Exception.Message)" }
            $process.WaitForExit()
            if (Test-Path -LiteralPath $LogPath -PathType Leaf) {
                Write-Host "--- Timed-out Unity $Phase validation log ---"
                Get-Content -LiteralPath $LogPath | Write-Host
            }
            throw "Unity $Phase validation exceeded its $TimeoutMinutes-minute phase timeout. See '$LogPath'."
        }
        if ([DateTime]::UtcNow -ge $nextProgress) {
            Write-Host "Unity $Phase validation is still running (PID $($process.Id), elapsed $([Math]::Round(([DateTime]::UtcNow - $process.StartTime.ToUniversalTime()).TotalMinutes, 1)) minutes)."
            $nextProgress = [DateTime]::UtcNow.AddMinutes(1)
        }
    }
    return $process.ExitCode
}
$packageManifestPath = Join-Path $packageRoot 'build-manifest.json'
$packageManifest = Get-Content -LiteralPath $packageManifestPath -Raw | ConvertFrom-Json
$isHoloEnabled = $packageManifest.profile -eq 'HoloEnabled'
if ($isHoloEnabled) {
    $holoPluginRoot = Join-Path $packageRoot 'Runtime\Plugins\Android\Holochain'
    foreach ($required in @('holochain-service.aar', 'holochain-client.aar', 'holooasis-unity-bridge.aar',
            'dependencies\kotlin-stdlib-1.6.21.jar', 'dependencies\kotlinx-coroutines-android-1.6.4.jar')) {
        if (-not (Test-Path -LiteralPath (Join-Path $holoPluginRoot $required) -PathType Leaf)) {
            throw "The HoloEnabled Unity package is missing '$required'."
        }
    }
    $unsupportedKotlin = @(Get-ChildItem -LiteralPath (Join-Path $holoPluginRoot 'dependencies') -File |
        Where-Object { $_.Extension -eq '.jar' -and $_.Name -match '^kotlin-(stdlib|reflect)' -and
            $_.Name -notmatch '-1\.6\.21\.jar$' })
    if ($unsupportedKotlin.Count -ne 0) {
        throw "The HoloEnabled package contains Kotlin artifacts unsupported by Unity 2022.3 AGP 7.1.2: $($unsupportedKotlin.Name -join ', ')."
    }
    $managedRoot = Join-Path $packageRoot 'Runtime\Plugins\Managed'
    foreach ($requiredManaged in @('NextGenSoftware.OASIS.API.Providers.HoloOASIS.Edge.dll',
            'NextGenSoftware.Holochain.HoloNET.Client.dll', 'Chaos.NaCl.dll', 'MessagePack.dll')) {
        if (-not (Test-Path -LiteralPath (Join-Path $managedRoot $requiredManaged) -PathType Leaf)) {
            throw "The HoloEnabled Unity package is missing managed runtime '$requiredManaged'."
        }
    }
    foreach ($forbiddenManaged in @('holochain_serialisation_wrapper.dll', 'Sodium.Core.dll', 'hc.exe', 'holochain.exe', 'Microsoft.CSharp.dll')) {
        if (Test-Path -LiteralPath (Join-Path $managedRoot $forbiddenManaged)) {
            throw "The HoloEnabled Unity package contains forbidden platform/runtime artifact '$forbiddenManaged'."
        }
    }
    if ((Get-Item -LiteralPath (Join-Path $managedRoot 'NextGenSoftware.Holochain.HoloNET.Client.dll')).Length -gt 2MB) {
        throw 'The HoloEnabled Unity package contains an embedded rather than lightweight HoloNET build.'
    }
    foreach ($requiredRuntimeAsset in @(
            'Runtime\Holochain\HoloEdgeUnityLocalProvider.cs',
            'Runtime\Resources\OASIS\Holochain\oasis.happ.bytes')) {
        if (-not (Test-Path -LiteralPath (Join-Path $packageRoot $requiredRuntimeAsset) -PathType Leaf)) {
            throw "The HoloEnabled Unity package is missing runtime asset '$requiredRuntimeAsset'."
        }
    }
}

$iosKeychainPath = Join-Path $packageRoot 'Runtime\Plugins\iOS\OASISEdgeKeychain.mm'
$secureStorePath = Join-Path $packageRoot 'Runtime\UnityPlatformSecureSessionStore.cs'
if (!(Test-Path -LiteralPath $iosKeychainPath -PathType Leaf) -or !(Test-Path -LiteralPath $secureStorePath -PathType Leaf)) {
    throw 'Generated Edge package is missing its iOS Keychain native or managed adapter.'
}
$iosKeychainSource = Get-Content -Raw -LiteralPath $iosKeychainPath
$secureStoreSource = Get-Content -Raw -LiteralPath $secureStorePath
if ($iosKeychainSource -notmatch 'SecItemUpdate\s*\(' -or
    $iosKeychainSource -notmatch 'int\s+OasisEdgeKeychainLoad\s*\(\s*const char \*key\s*,\s*char \*\*value\s*\)') {
    throw 'The iOS Keychain adapter must update credentials atomically and return load status separately from credential data.'
}
$saveFunction = [regex]::Match($iosKeychainSource,
    'extern\s+"C"\s+int\s+OasisEdgeKeychainSave.*?(?=extern\s+"C")',
    [Text.RegularExpressions.RegexOptions]::Singleline).Value
if ([string]::IsNullOrWhiteSpace($saveFunction) -or $saveFunction -match 'SecItemDelete\s*\(') {
    throw 'The iOS Keychain save path must not delete an acknowledged credential before replacing it.'
}
if ($secureStoreSource -notmatch 'int\s+OasisEdgeKeychainLoad\s*\(\s*string key\s*,\s*out IntPtr value\s*\)' -or
    $secureStoreSource -notmatch 'IOSKeychainItemNotFound') {
    throw 'The Unity iOS secure-session adapter must distinguish item-not-found from Keychain failures.'
}

$linkerConfigPath = Join-Path $packageRoot 'Runtime\link.xml'
if (-not (Test-Path -LiteralPath $linkerConfigPath -PathType Leaf)) {
    throw "Generated Edge package is missing Runtime/link.xml."
}
[xml]$linkerConfig = Get-Content -LiteralPath $linkerConfigPath -Raw
$preservedAssemblies = @($linkerConfig.linker.assembly | ForEach-Object { [string]$_.fullname })
$requiredRuntimeAssemblies = @(
    'NextGenSoftware.OGEngine.Shared',
    'NextGenSoftware.OGEngine.Client.Edge',
    'NextGenSoftware.OASIS.Edge.Runtime',
    'NextGenSoftware.OASIS.API.Providers.EdgeSQLiteOASIS',
    'Microsoft.Data.Sqlite',
    'SQLitePCLRaw.batteries_v2',
    'SQLitePCLRaw.core',
    'SQLitePCLRaw.provider.e_sqlite3',
    'Newtonsoft.Json',
    'System.Text.Json'
)
if ($isHoloEnabled) {
    $requiredRuntimeAssemblies += @(
        'NextGenSoftware.OASIS.API.Providers.HoloOASIS.Edge',
        'NextGenSoftware.Holochain.HoloNET.Client',
        'Chaos.NaCl',
        'MessagePack',
        'MessagePack.Annotations'
    )
}
foreach ($assemblyName in $requiredRuntimeAssemblies) {
    if ($assemblyName -notin $preservedAssemblies) {
        throw "Generated Edge package does not preserve the runtime-reflected assembly '$assemblyName' for IL2CPP."
    }
}

$projectRoot = Join-Path $artifactsRoot 'unity-edge-validation-project'
if (Test-Path -LiteralPath $projectRoot) { Remove-Item -LiteralPath $projectRoot -Recurse -Force }
New-Item -ItemType Directory -Path (Join-Path $projectRoot 'Assets') -Force | Out-Null
New-Item -ItemType Directory -Path (Join-Path $projectRoot 'Packages') -Force | Out-Null
New-Item -ItemType Directory -Path (Join-Path $projectRoot 'ProjectSettings') -Force | Out-Null
$sampleSource = Join-Path $packageRoot 'Samples~\QuickStart'
if (-not (Test-Path -LiteralPath $sampleSource -PathType Container)) {
    throw 'The public Quick Start sample is missing from the generated package.'
}
Copy-Item -LiteralPath $sampleSource -Destination (Join-Path $projectRoot 'Assets\OASISEdgeQuickStart') `
    -Recurse -Force
New-Item -ItemType Directory -Path (Join-Path $projectRoot 'Assets\Editor') -Force | Out-Null
Copy-Item -LiteralPath (Join-Path $repoRoot 'Scripts\UnityValidation\OASISEdgePackageValidator.cs') `
    -Destination (Join-Path $projectRoot 'Assets\Editor\OASISEdgePackageValidator.cs') -Force

$packageReference = 'file:' + $packageRoot.Replace('\', '/')
@{
    dependencies = [ordered]@{ 'com.nextgensoftware.oasis.edge' = $packageReference }
} | ConvertTo-Json -Depth 4 | Set-Content -LiteralPath (Join-Path $projectRoot 'Packages\manifest.json') -Encoding utf8
Set-Content -LiteralPath (Join-Path $projectRoot 'ProjectSettings\ProjectVersion.txt') `
    -Value "m_EditorVersion: 2022.3.62f3`nm_EditorVersionWithRevision: 2022.3.62f3 (e9b2a6e6c3a0)" -Encoding utf8

$logPath = Join-Path $logRoot 'unity-edge-validation.log'
$unityExitCode = Invoke-UnityBatchProcess -Phase 'editor package' -LogPath $logPath `
    -TimeoutMinutes $EditorValidationTimeoutMinutes -Arguments @(
    '-batchmode', '-nographics', '-quit', '-projectPath', $projectRoot,
    '-executeMethod', 'NextGenSoftware.OASIS.Edge.Unity.Editor.OASISEdgePackageValidator.Validate',
    '-logFile', $logPath
)
if ($unityExitCode -ne 0) {
    if (Test-Path -LiteralPath $logPath -PathType Leaf) {
        Write-Host "--- Unity Edge validation log ---"
        Get-Content -LiteralPath $logPath | Write-Host
    }
    throw "Unity Edge package validation failed with exit code $unityExitCode. See '$logPath'."
}
$errors = Select-String -LiteralPath $logPath -Pattern 'error CS\d+|Assembly .* will not be loaded|Failed to resolve packages' -CaseSensitive:$false
if ($errors) { throw "Unity reported package compilation errors. See '$logPath'." }
if (-not (Select-String -LiteralPath $logPath -Pattern 'OASIS_EDGE_UNITY_PACKAGE_VALIDATION_PASSED' -Quiet)) {
    throw "Unity did not complete the Edge package secure-storage smoke test. See '$logPath'."
}
$androidLogPath = Join-Path $logRoot 'unity-edge-android-validation.log'
$androidExitCode = Invoke-UnityBatchProcess -Phase 'Android IL2CPP build' -LogPath $androidLogPath `
    -TimeoutMinutes $AndroidBuildTimeoutMinutes -Arguments @(
    '-batchmode', '-nographics', '-quit', '-projectPath', $projectRoot, '-buildTarget', 'Android',
    '-executeMethod', 'NextGenSoftware.OASIS.Edge.Unity.Editor.OASISEdgePackageValidator.ValidateAndroidBuild',
    '-logFile', $androidLogPath
)
if ($androidExitCode -ne 0 -or
    -not (Select-String -LiteralPath $androidLogPath -Pattern 'OASIS_EDGE_ANDROID_BUILD_VALIDATION_PASSED' -Quiet)) {
    if (Test-Path -LiteralPath $androidLogPath -PathType Leaf) {
        Write-Host "--- Unity Android validation log ---"
        Get-Content -LiteralPath $androidLogPath | Write-Host
    }
    throw "Unity Android Edge package validation failed. See '$androidLogPath'."
}
if ($isHoloEnabled) {
    $apkPath = Join-Path $projectRoot 'Build\OASISEdgeValidation.apk'
    Add-Type -AssemblyName System.IO.Compression.FileSystem
    $apk = [IO.Compression.ZipFile]::OpenRead($apkPath)
    try {
        $entryNames = @($apk.Entries | ForEach-Object FullName)
        foreach ($requiredEntry in @('lib/arm64-v8a/libholochain_conductor_runtime_ffi.so',
                'lib/arm64-v8a/libholochain_conductor_runtime_types_ffi.so', 'lib/arm64-v8a/libil2cpp.so')) {
            if ($requiredEntry -notin $entryNames) { throw "Validated APK is missing '$requiredEntry'." }
        }
        $foreignAbi = @($entryNames | Where-Object { $_ -match '^lib/(armeabi-v7a|x86|x86_64)/' })
        if ($foreignAbi.Count -ne 0) { throw 'The HoloEnabled validation APK is not ARM64-only.' }
    }
    finally { $apk.Dispose() }
}
Write-Host "Unity Edge package compiled successfully. Log: $logPath"
