[CmdletBinding()]
param(
    [string]$UnityEditor = 'C:\Program Files\Unity\Hub\Editor\2022.3.62f3\Editor\Unity.exe',
    [string]$LogPath = 'artifacts\edge-release-validation\our-world-edge-integration.log',
    [string]$PackageDirectory = 'artifacts\unity\com.nextgensoftware.oasis.edge'
)

$ErrorActionPreference = 'Stop'
$repoRoot = Split-Path $PSScriptRoot -Parent
$projectRoot = Join-Path $repoRoot 'OASIS Omniverse\OASIS Hub'
$packageRoot = [IO.Path]::GetFullPath((Join-Path $repoRoot $PackageDirectory))
$resolvedLog = [IO.Path]::GetFullPath((Join-Path $repoRoot $LogPath))
if (-not (Test-Path -LiteralPath $UnityEditor -PathType Leaf)) { throw "Unity editor not found: $UnityEditor" }
if (-not (Test-Path -LiteralPath (Join-Path $packageRoot 'build-manifest.json') -PathType Leaf)) {
    throw "Build the manifested Unity Edge package before validating Our World: $packageRoot"
}
& (Join-Path $repoRoot 'Scripts\sync_our_world_edge_package.ps1') -PackageDirectory `
    ([IO.Path]::GetRelativePath($repoRoot, $packageRoot))

$kernel = Get-Content -LiteralPath (Join-Path $projectRoot 'Assets\Scripts\Runtime\OmniverseKernel.cs') -Raw
$login = Get-Content -LiteralPath (Join-Path $projectRoot 'Assets\Scripts\UI\LoginScreen.cs') -Raw
$gateway = Get-Content -LiteralPath (Join-Path $projectRoot 'Assets\Scripts\API\Web4Web5GatewayClient.cs') -Raw
$edgeProjection = Get-Content -LiteralPath (Join-Path $projectRoot 'Assets\Scripts\API\OmniverseEdgeProjectionSource.cs') -Raw
$hostConfig = Get-Content -LiteralPath (Join-Path $projectRoot 'Assets\Scripts\Config\OmniverseHostConfig.cs') -Raw
$hostConfigLoader = Get-Content -LiteralPath (Join-Path $projectRoot 'Assets\Scripts\Config\HostConfigLoader.cs') -Raw
$offlinePreference = Get-Content -LiteralPath (Join-Path $projectRoot 'Assets\Scripts\Runtime\OfflineSyncPreferenceStore.cs') -Raw
$globalSettings = Get-Content -LiteralPath (Join-Path $projectRoot 'Assets\Scripts\Runtime\GlobalSettingsService.cs') -Raw
$hud = Get-Content -LiteralPath (Join-Path $projectRoot 'Assets\Scripts\UI\SharedHudOverlay.cs') -Raw
$streamingConfig = Get-Content -LiteralPath (Join-Path $projectRoot 'Assets\StreamingAssets\omniverse_host_config.json') -Raw
$notificationStateMachine = Get-Content -LiteralPath (Join-Path $repoRoot 'OASIS Architecture\NextGenSoftware.OASIS.Edge.Runtime\EdgeRuntimeNotificationStateMachine.cs') -Raw
if ($kernel -notmatch 'InitializeOfflineAsync' -or $kernel -notmatch 'AuthenticateHostedSessionAsync' -or
    $kernel -notmatch 'UnityPlatformSecureSessionStore' -or $kernel -notmatch 'EcdsaEdgeOfflineGrantValidator') {
    throw 'Our World does not compose the signed, platform-secure Edge offline-session lifecycle.'
}
foreach ($notification in @(
    'Working offline - changes will sync automatically.',
    'Back online - synchronizing changes.',
    'Synchronization complete.'
)) {
    if ($notificationStateMachine -notmatch [regex]::Escape($notification)) {
        throw "Our World is missing the required Edge transition notification: $notification"
    }
}
if ($kernel -notmatch 'EdgeRuntimeNotificationStateMachine' -or
    $kernel -notmatch 'ConcurrentQueue<string>' -or $kernel -notmatch 'TryDequeue') {
    throw 'Our World must marshal Edge status notifications onto the Unity main thread.'
}
if ($login -match 'config\.apiKey\s*=' -or $kernel -match '_config\.apiKey' -or
    $hostConfig -match '\bapiKey\b' -or $streamingConfig -match '"apiKey"' -or
    $hostConfigLoader -match 'File\.WriteAllText') {
    throw 'Our World must not persist or reload bearer tokens from StreamingAssets.'
}
if ($hostConfigLoader -notmatch 'UNITY_ANDROID' -or
    $hostConfigLoader -notmatch 'UnityWebRequest\.Get' -or
    $kernel -notmatch 'await\s+HostConfigLoader\.LoadAsync') {
    throw 'Our World must load packaged StreamingAssets through the Android-compatible asynchronous path.'
}
if ($gateway -match 'PlayerPrefs' -or $hostConfig -notmatch 'hyperdrive\.sync') {
    throw 'Our World must use durable Edge projections and request the narrow automatic-sync grant scope.'
}
if ($offlinePreference -notmatch 'GetInt\(EnabledKey,\s*1\)' -or
    $kernel -notmatch 'PendingOperationCount\s*>\s*0' -or
    $kernel -notmatch 'synchronizeBeforeDisable' -or
    $hud -notmatch 'Sync Now & Disable') {
    throw 'Our World must default offline sync on and refuse unsafe disable while durable operations are pending.'
}
if ($gateway -notmatch 'CanAccessHostedUserApis' -or
    $globalSettings -notmatch 'PendingRemoteSettingsKey' -or
    $globalSettings -notmatch 'SynchronizePendingPreferencesAsync' -or
    $kernel -notmatch 'SynchronizePendingPreferencesAsync') {
    throw 'Offline startup and preference changes must remain local and reconcile through an authenticated hosted session.'
}

# Keep the documented offline plan tied to the actual gateway surface. Any newly added network method must be
# deliberately classified here instead of silently shipping as online-only.
$classifiedGatewayMethods = @(
    'GetSharedInventoryAsync', 'GetCrossGameQuestsAsync', 'GetCrossGameNftsAsync',
    'GetAvatarProfileAsync', 'GetClanMembersAsync', 'GetKarmaOverviewAsync',
    'GetGlobalPreferencesAsync', 'SaveGlobalPreferencesAsync', 'AuthenticateAsync'
)
$gatewayMethods = [regex]::Matches($gateway,
    'public\s+async\s+Task<[^\r\n]+?>\s+(?<name>\w+Async)\s*\(') |
    ForEach-Object { $_.Groups['name'].Value } | Sort-Object -Unique
$unclassifiedMethods = @($gatewayMethods | Where-Object { $_ -notin $classifiedGatewayMethods })
$removedMethods = @($classifiedGatewayMethods | Where-Object { $_ -notin $gatewayMethods })
if ($unclassifiedMethods.Count -gt 0 -or $removedMethods.Count -gt 0) {
    throw "Our World gateway call-surface classification is stale. Unclassified: $($unclassifiedMethods -join ', '). Missing: $($removedMethods -join ', '). Update Docs/Devs/OUR_WORLD_OFFLINE_PLAN.md and this release gate together."
}

$requiredOfflineRoutes = @{
    GetSharedInventoryAsync = 'GetInventoryAsync'
    GetCrossGameQuestsAsync = 'GetQuestsAsync'
    GetCrossGameNftsAsync = 'GetNftsAsync'
    GetAvatarProfileAsync = 'GetAvatarAsync'
    GetKarmaOverviewAsync = 'GetKarmaAsync'
    GetClanMembersAsync = 'GetClanMembersAsync'
}
foreach ($route in $requiredOfflineRoutes.GetEnumerator()) {
    $methodPattern = '(?s)public\s+async\s+Task<[^\r\n]+?>\s+' + [regex]::Escape($route.Key) +
        '\s*\([^)]*\)\s*\{(?<body>.*?)(?=\r?\n\s*public\s+async\s+Task<|\z)'
    $match = [regex]::Match($gateway, $methodPattern)
    if (-not $match.Success -or $match.Groups['body'].Value -notmatch
        ('HasEdgeProjectionSource[\s\S]*_edgeSource\.' + [regex]::Escape($route.Value) + '\s*\(')) {
        throw "Our World gateway method $($route.Key) no longer has its required durable Edge route $($route.Value)."
    }
    if ($edgeProjection -notmatch ('public\s+async\s+Task<[^\r\n]+?>\s+' + [regex]::Escape($route.Value) + '\s*\(')) {
        throw "Our World Edge projection source no longer implements $($route.Value)."
    }
}
if ($gateway -notmatch 'private\s+bool\s+HasEdgeProjectionSource\s*=>\s*_edgeSource\s*!=\s*null') {
    throw 'Our World Edge-enabled reads must use one durable projection path online and offline.'
}

$unityEdgeHost = Get-Content -LiteralPath (Join-Path $projectRoot 'Packages\com.nextgensoftware.oasis.edge\Runtime\OASISEdgeUnityHost.cs') -Raw
$ogEngineEdgeClientPath = Join-Path $repoRoot 'OASIS Omniverse\OGEngineClient\Edge\OGEngineEdgeClient.cs'
$ogEngineEdgeClient = Get-Content -LiteralPath $ogEngineEdgeClientPath -Raw
if ($unityEdgeHost -notmatch 'OGEngineEdgeClient' -or $unityEdgeHost -match 'new\s+OASISEdgeAPI') {
    throw 'Our World Unity must bind OGEngineClient and must not construct OASISEdgeAPI directly.'
}
if ($gateway -match 'OASISEdgeAPI' -or $edgeProjection -match 'OASISEdgeAPI') {
    throw 'Our World gateway/projection code must consume OGEngineClient rather than the low-level OASISEdgeAPI.'
}
if ($ogEngineEdgeClient -notmatch '(?s)AcquireOfflineSessionAsync.*SynchronizeAsync') {
    throw 'OGEngineClient must complete an authenticated initial synchronization after acquiring its offline grant.'
}

New-Item -ItemType Directory -Path (Split-Path $resolvedLog -Parent) -Force | Out-Null
$validationProject = Join-Path (Split-Path $resolvedLog -Parent) 'our-world-edge-validation-project'
if (Test-Path -LiteralPath $validationProject) { Remove-Item -LiteralPath $validationProject -Recurse -Force }
New-Item -ItemType Directory -Path $validationProject -Force | Out-Null
foreach ($directory in @('Assets', 'ProjectSettings', 'Packages')) {
    Copy-Item -LiteralPath (Join-Path $projectRoot $directory) -Destination $validationProject -Recurse -Force
}
$arguments = '-batchmode -nographics -quit -projectPath "' + $validationProject + '" -logFile "' + $resolvedLog + '"'
$process = Start-Process -FilePath $UnityEditor -ArgumentList $arguments -WindowStyle Hidden -Wait -PassThru
if ($process.ExitCode -ne 0) { throw "Our World Unity compilation failed with exit code $($process.ExitCode). See '$resolvedLog'." }
$errors = Select-String -LiteralPath $resolvedLog -Pattern 'error CS\d+|Failed to resolve packages|Scripts have compiler errors' -CaseSensitive:$false
if ($errors) { throw "Our World Unity compilation reported errors. See '$resolvedLog'." }
if (-not (Select-String -LiteralPath $resolvedLog -Pattern 'Exiting batchmode successfully' -Quiet)) {
    throw "Our World Unity compilation did not reach a successful batch-mode exit. See '$resolvedLog'."
}
Add-Content -LiteralPath $resolvedLog -Value 'OASIS_OUR_WORLD_EDGE_INTEGRATION_VALIDATION_PASSED'
Write-Host "Our World Edge integration compiled successfully. Log: $resolvedLog"
