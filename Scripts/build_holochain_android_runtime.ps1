[CmdletBinding()]
param(
    [string]$OutputDirectory = 'artifacts/holochain-android-runtime',
    [switch]$CleanSource,
    [switch]$PackageExistingBuild
)

$ErrorActionPreference = 'Stop'
$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path
$versionFile = Join-Path $PSScriptRoot 'holochain_android_runtime.version.json'
$compatibilityPatches = @(
    (Join-Path $PSScriptRoot 'patches/holochain-android-runtime-0.3.0-client-parcelers.patch'),
    (Join-Path $PSScriptRoot 'patches/holochain-android-runtime-0.3.0-unity-kotlin.patch')
)
$pin = Get-Content -LiteralPath $versionFile -Raw | ConvertFrom-Json
$outputRoot = [IO.Path]::GetFullPath((Join-Path $repoRoot $OutputDirectory))
$artifactsRoot = [IO.Path]::GetFullPath((Join-Path $repoRoot 'artifacts'))
if (!$outputRoot.StartsWith($artifactsRoot, [StringComparison]::OrdinalIgnoreCase)) {
    throw "Holochain Android build output must remain inside '$artifactsRoot'."
}

$sourceRoot = Join-Path $outputRoot 'source'
if ($CleanSource -and (Test-Path -LiteralPath $sourceRoot)) {
    $resolvedSource = [IO.Path]::GetFullPath($sourceRoot)
    if (!$resolvedSource.StartsWith($outputRoot, [StringComparison]::OrdinalIgnoreCase)) {
        throw "Refusing to remove source outside '$outputRoot'."
    }
    Remove-Item -LiteralPath $resolvedSource -Recurse -Force
}
New-Item -ItemType Directory -Path $outputRoot -Force | Out-Null

$createdSourceCheckout = !(Test-Path -LiteralPath (Join-Path $sourceRoot '.git'))
if ($createdSourceCheckout) {
    & git -c core.autocrlf=false clone --filter=blob:none --no-checkout $pin.repository $sourceRoot
    if ($LASTEXITCODE -ne 0) { throw 'Failed to clone the pinned Holochain Android runtime source.' }
}
& git -C $sourceRoot config core.autocrlf false
if ($LASTEXITCODE -ne 0) { throw 'Failed to enforce LF source checkout for the WSL Android build.' }
& git -C $sourceRoot fetch --depth 1 origin $pin.commit
if ($LASTEXITCODE -ne 0) { throw "Failed to fetch pinned runtime commit '$($pin.commit)'." }
$actualCommit = (& git -C $sourceRoot rev-parse HEAD).Trim()
if ($createdSourceCheckout -or $actualCommit -ne $pin.commit) {
    & git -C $sourceRoot checkout --detach $pin.commit
    if ($LASTEXITCODE -ne 0) { throw "Failed to checkout pinned runtime commit '$($pin.commit)'." }
    $actualCommit = (& git -C $sourceRoot rev-parse HEAD).Trim()
}
if ($actualCommit -ne $pin.commit) { throw "Runtime checkout is '$actualCommit', expected '$($pin.commit)'." }
$dirty = @(& git -C $sourceRoot status --porcelain --untracked-files=no)
if ($dirty.Count -eq 0) {
    foreach ($patch in $compatibilityPatches) {
        & git -C $sourceRoot apply --check $patch
        if ($LASTEXITCODE -ne 0) { throw "The reviewed Android compatibility patch no longer applies: '$patch'." }
        & git -C $sourceRoot apply $patch
        if ($LASTEXITCODE -ne 0) { throw "Failed to apply the reviewed Android compatibility patch: '$patch'." }
    }
}
$actualDirtyPaths = @(& git -C $sourceRoot status --porcelain --untracked-files=no | ForEach-Object { $_.Substring(3).Replace('\', '/') } | Sort-Object)
$expectedPatchedPaths = @($pin.patchedFiles.PSObject.Properties.Name | Sort-Object)
if (Compare-Object $expectedPatchedPaths $actualDirtyPaths) {
    throw 'The pinned runtime source contains modifications outside the reviewed patch set. Use -CleanSource.'
}
foreach ($property in $pin.patchedFiles.PSObject.Properties) {
    $patchedPath = Join-Path $sourceRoot $property.Name
    $actualHash = (Get-FileHash -LiteralPath $patchedPath -Algorithm SHA256).Hash
    if ($actualHash -ne $property.Value) {
        throw "Patched runtime file does not match its reviewed hash: '$($property.Name)'. Use -CleanSource."
    }
}

function ConvertTo-WslPath {
    param([Parameter(Mandatory)][string]$WindowsPath)
    $fullPath = [IO.Path]::GetFullPath($WindowsPath)
    if (-not $IsWindows) {
        return $fullPath
    }
    if ($fullPath -notmatch '^([A-Za-z]):\\(.*)$') {
        throw "Only absolute Windows drive paths can be mapped into WSL: '$fullPath'."
    }
    return "/mnt/$($matches[1].ToLowerInvariant())/$($matches[2].Replace('\', '/'))"
}

function Invoke-NixBuildCommand {
    param([Parameter(Mandatory)][string]$Command)
    if ($IsWindows) {
        & wsl.exe bash -lc $Command
    }
    else {
        & bash -lc $Command
    }
    if ($LASTEXITCODE -ne 0) {
        throw "Pinned Holochain Android build command failed with exit code $LASTEXITCODE."
    }
}
$wslSource = ConvertTo-WslPath $sourceRoot
$gradleUserHome = Join-Path $outputRoot 'gradle-user-home'
New-Item -ItemType Directory -Path $gradleUserHome -Force | Out-Null
Set-Content -LiteralPath (Join-Path $gradleUserHome 'gradle.properties') `
    -Value "android.useAndroidX=true`n" -Encoding ascii
$wslGradleUserHome = ConvertTo-WslPath $gradleUserHome
$runtimeBuild = "pnpm run build:single-target:runtime-types-ffi $($pin.rustTarget)" +
    " && pnpm run build:client && pnpm run publish:local:client" +
    " && pnpm run build:single-target:runtime-ffi $($pin.rustTarget)" +
    " && pnpm run build:service && pnpm run publish:local:service"
$buildCommand = "cd '$wslSource' && export GRADLE_USER_HOME='$wslGradleUserHome' && nix develop --command bash -lc '$runtimeBuild'"
if (!$PackageExistingBuild) {
    Invoke-NixBuildCommand $buildCommand
}

$bridgeRoot = Join-Path $repoRoot 'Providers/Network/NextGenSoftware.OASIS.API.Providers.HoloOASIS.Unity/AndroidBridge'
$wslBridge = ConvertTo-WslPath $bridgeRoot
$gradleWrapper = "$wslSource/libraries/client/gradlew"
if (!$PackageExistingBuild) {
    Invoke-NixBuildCommand "cd '$wslSource' && export GRADLE_USER_HOME='$wslGradleUserHome' && nix develop --command bash -lc 'cd libraries/client && ./gradlew -p $wslBridge :bridge:assembleRelease :bridge:exportReleaseRuntimeDependencies'"
}

$serviceAar = Join-Path $sourceRoot 'libraries/service/build/outputs/aar/service-release.aar'
$clientAar = Join-Path $sourceRoot 'libraries/client/build/outputs/aar/client-release.aar'
$bridgeAar = Join-Path $bridgeRoot 'bridge/build/outputs/aar/bridge-release.aar'
foreach ($artifact in @($serviceAar, $clientAar, $bridgeAar)) {
    if (!(Test-Path -LiteralPath $artifact -PathType Leaf)) { throw "Expected Android artifact is missing: '$artifact'." }
}

Add-Type -AssemblyName System.IO.Compression.FileSystem
$archive = [IO.Compression.ZipFile]::OpenRead($serviceAar)
try {
    if ($null -eq $archive.GetEntry('jni/arm64-v8a/libholochain_conductor_runtime_ffi.so')) {
        throw 'The service AAR does not contain the required ARM64 Holochain runtime.'
    }
}
finally { $archive.Dispose() }

$packageRoot = Join-Path $outputRoot 'package'
New-Item -ItemType Directory -Path $packageRoot -Force | Out-Null
Copy-Item -LiteralPath $serviceAar -Destination (Join-Path $packageRoot 'holochain-service.aar') -Force
Copy-Item -LiteralPath $clientAar -Destination (Join-Path $packageRoot 'holochain-client.aar') -Force
Copy-Item -LiteralPath $bridgeAar -Destination (Join-Path $packageRoot 'holooasis-unity-bridge.aar') -Force
$runtimeDependencies = Join-Path $bridgeRoot 'bridge/build/unityRuntimeDependencies'
if (!(Test-Path -LiteralPath $runtimeDependencies -PathType Container)) {
    throw "The Android runtime dependency export is missing: '$runtimeDependencies'."
}
$dependencyPackageRoot = Join-Path $packageRoot 'dependencies'
if (Test-Path -LiteralPath $dependencyPackageRoot) {
    Remove-Item -LiteralPath $dependencyPackageRoot -Recurse -Force
}
New-Item -ItemType Directory -Path $dependencyPackageRoot -Force | Out-Null
Get-ChildItem -LiteralPath $runtimeDependencies -File | Copy-Item -Destination $dependencyPackageRoot -Force
$dependencyArtifacts = @(Get-ChildItem -LiteralPath $dependencyPackageRoot -File | Sort-Object Name | ForEach-Object {
    [ordered]@{ name = "dependencies/$($_.Name)"; sha256 = (Get-FileHash $_.FullName -Algorithm SHA256).Hash; bytes = $_.Length }
})
$manifest = [ordered]@{
    schemaVersion = 1
    repository = $pin.repository
    sourceCommit = $actualCommit
    compatibilityPatches = @($compatibilityPatches | ForEach-Object {
        [ordered]@{ name = Split-Path $_ -Leaf; sha256 = (Get-FileHash $_ -Algorithm SHA256).Hash }
    })
    upstreamRelease = $pin.upstreamRelease
    holochainCompatibility = $pin.holochainCompatibility
    androidAbi = $pin.androidAbi
    artifacts = @(
        [ordered]@{ name = 'holochain-service.aar'; sha256 = (Get-FileHash $serviceAar -Algorithm SHA256).Hash; bytes = (Get-Item $serviceAar).Length },
        [ordered]@{ name = 'holochain-client.aar'; sha256 = (Get-FileHash $clientAar -Algorithm SHA256).Hash; bytes = (Get-Item $clientAar).Length },
        [ordered]@{ name = 'holooasis-unity-bridge.aar'; sha256 = (Get-FileHash $bridgeAar -Algorithm SHA256).Hash; bytes = (Get-Item $bridgeAar).Length }
    ) + $dependencyArtifacts
}
$manifest | ConvertTo-Json -Depth 5 | Set-Content -LiteralPath (Join-Path $packageRoot 'build-manifest.json') -Encoding utf8
Write-Host "Pinned Holochain Android runtime created at '$packageRoot'."
