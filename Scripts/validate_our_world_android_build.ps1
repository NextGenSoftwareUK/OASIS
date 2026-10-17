[CmdletBinding()]
param(
    [string]$UnityEditor = 'C:\Program Files\Unity\Hub\Editor\2022.3.62f3\Editor\Unity.exe',
    [string]$PackageDirectory = 'artifacts\unity\com.nextgensoftware.oasis.edge',
    [string]$ArtifactsDirectory = 'artifacts\our-world-android-validation',
    [string]$HostConfigPath
)

$ErrorActionPreference = 'Stop'
$repoRoot = Split-Path $PSScriptRoot -Parent
$projectRoot = Join-Path $repoRoot 'OASIS Omniverse\OASIS Hub'
$packageRoot = [IO.Path]::GetFullPath((Join-Path $repoRoot $PackageDirectory))
$artifactsRoot = [IO.Path]::GetFullPath((Join-Path $repoRoot $ArtifactsDirectory))
$allowedArtifactsRoot = [IO.Path]::GetFullPath((Join-Path $repoRoot 'artifacts')) + [IO.Path]::DirectorySeparatorChar
if (!$artifactsRoot.StartsWith($allowedArtifactsRoot, [StringComparison]::OrdinalIgnoreCase)) {
    throw "Android validation output must remain under '$allowedArtifactsRoot'."
}
if (!(Test-Path -LiteralPath $UnityEditor -PathType Leaf)) { throw "Unity editor not found: $UnityEditor" }
if (!(Test-Path -LiteralPath (Join-Path $packageRoot 'build-manifest.json') -PathType Leaf)) {
    throw "A manifested Unity Edge package is required: $packageRoot"
}

& (Join-Path $repoRoot 'Scripts\sync_our_world_edge_package.ps1') -PackageDirectory `
    ([IO.Path]::GetRelativePath($repoRoot, $packageRoot))

$validationProject = Join-Path $artifactsRoot 'project'
$releaseConfig = $null
New-Item -ItemType Directory -Path $validationProject -Force | Out-Null
foreach ($directory in @('Assets', 'ProjectSettings', 'Packages')) {
    $copiedDirectory = Join-Path $validationProject $directory
    if (Test-Path -LiteralPath $copiedDirectory) {
        $resolvedCopiedDirectory = [IO.Path]::GetFullPath($copiedDirectory)
        if (!$resolvedCopiedDirectory.StartsWith(([IO.Path]::GetFullPath($validationProject) + [IO.Path]::DirectorySeparatorChar),
            [StringComparison]::OrdinalIgnoreCase)) { throw "Unsafe validation-project path: $resolvedCopiedDirectory" }
        Remove-Item -LiteralPath $resolvedCopiedDirectory -Recurse -Force
    }
    Copy-Item -LiteralPath (Join-Path $projectRoot $directory) -Destination $validationProject -Recurse -Force
}
if (![string]::IsNullOrWhiteSpace($HostConfigPath)) {
    $releaseConfigPath = [IO.Path]::GetFullPath((Join-Path $repoRoot $HostConfigPath))
    if (!(Test-Path -LiteralPath $releaseConfigPath -PathType Leaf)) { throw "Release host config not found: $releaseConfigPath" }
    $releaseConfig = Get-Content -LiteralPath $releaseConfigPath -Raw | ConvertFrom-Json
    $hostUri = $null
    if (!$releaseConfig.enableEdgeRuntime -or [string]::IsNullOrWhiteSpace($releaseConfig.edgeOfflineGrantPublicKey) -or
        ![Uri]::TryCreate($releaseConfig.edgeHostedOnodeBaseUrl, [UriKind]::Absolute, [ref]$hostUri) -or
        $hostUri.Scheme -ne 'https' -or $hostUri.IsLoopback) {
        throw 'Release Android validation requires enabled Edge, a pinned public grant key, and a non-loopback HTTPS ONODE.'
    }
    Copy-Item -LiteralPath $releaseConfigPath -Destination `
        (Join-Path $validationProject 'Assets\StreamingAssets\omniverse_host_config.json') -Force
}

$apkPath = Join-Path $artifactsRoot 'OurWorld-Edge-Android.apk'
$logPath = Join-Path $artifactsRoot 'our-world-android-build.log'
Remove-Item -LiteralPath $apkPath -Force -ErrorAction SilentlyContinue
Remove-Item -LiteralPath $logPath -Force -ErrorAction SilentlyContinue
$env:OASIS_OUR_WORLD_ANDROID_OUTPUT = $apkPath
try {
    $arguments = @(
        '-batchmode', '-nographics', '-quit', '-projectPath', $validationProject,
        '-buildTarget', 'Android', '-executeMethod',
        'OASIS.Omniverse.UnityHost.Editor.OurWorldAndroidBuildValidator.BuildAndroidPlayer',
        '-logFile', $logPath
    )
    $process = Start-Process -FilePath $UnityEditor -ArgumentList $arguments -WindowStyle Hidden -Wait -PassThru
}
finally { Remove-Item Env:OASIS_OUR_WORLD_ANDROID_OUTPUT -ErrorAction SilentlyContinue }

if ($process.ExitCode -ne 0 -or !(Test-Path -LiteralPath $apkPath -PathType Leaf)) {
    throw "Our World Android player build failed. See '$logPath'."
}
if (!(Select-String -LiteralPath $logPath -Pattern 'OASIS_OUR_WORLD_ANDROID_BUILD_PASSED' -Quiet)) {
    throw "Our World Android build did not emit its success invariant. See '$logPath'."
}
[System.Reflection.Assembly]::LoadWithPartialName('System.IO.Compression') | Out-Null
$archive = [System.IO.Compression.ZipFile]::OpenRead($apkPath)
try {
    $entries = @($archive.Entries | ForEach-Object { $_.FullName })
    foreach ($requiredEntry in @(
        'assets/omniverse_host_config.json',
        'lib/arm64-v8a/libil2cpp.so',
        'lib/arm64-v8a/libe_sqlite3.so',
        'classes.dex'
    )) {
        if ($entries -notcontains $requiredEntry) { throw "Android APK is missing '$requiredEntry'." }
    }
    $unexpectedSqliteAbis = @($entries | Where-Object {
        $_ -match '^lib/(?!arm64-v8a/)[^/]+/libe_sqlite3\.so$'
    })
    if ($unexpectedSqliteAbis.Count -gt 0) {
        throw "ARM64 release APK unexpectedly contains SQLite ABIs: $($unexpectedSqliteAbis -join ', ')"
    }
    if ($null -ne $releaseConfig) {
        $configEntry = $archive.GetEntry('assets/omniverse_host_config.json')
        $reader = [IO.StreamReader]::new($configEntry.Open())
        try { $packagedConfig = $reader.ReadToEnd() | ConvertFrom-Json }
        finally { $reader.Dispose() }
        if (!$packagedConfig.enableEdgeRuntime -or
            $packagedConfig.edgeOfflineGrantPublicKey -ne $releaseConfig.edgeOfflineGrantPublicKey -or
            $packagedConfig.edgeHostedOnodeBaseUrl -ne $releaseConfig.edgeHostedOnodeBaseUrl) {
            throw 'The packaged Android host config does not match the validated Edge release configuration.'
        }
    }
}
finally { $archive.Dispose() }
Write-Host "Our World Android Edge build passed: $apkPath"
