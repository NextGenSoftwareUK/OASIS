[CmdletBinding()]
param(
    [ValidateSet('Debug', 'Release')]
    [string]$Configuration = 'Release',
    [string]$OutputDirectory
)

$ErrorActionPreference = 'Stop'
$repoRoot = Split-Path $PSScriptRoot -Parent
if ([string]::IsNullOrWhiteSpace($OutputDirectory)) {
    $OutputDirectory = Join-Path $repoRoot 'artifacts\unity'
}
$outputRoot = [IO.Path]::GetFullPath($OutputDirectory)
$artifactsRoot = [IO.Path]::GetFullPath((Join-Path $repoRoot 'artifacts'))
if (-not $outputRoot.StartsWith($artifactsRoot, [StringComparison]::OrdinalIgnoreCase)) {
    throw "Unity package output must be inside '$artifactsRoot'."
}

$packageName = 'com.nextgensoftware.oasis.edge'
$packageRoot = Join-Path $outputRoot $packageName
if (Test-Path -LiteralPath $packageRoot) { Remove-Item -LiteralPath $packageRoot -Recurse -Force }
New-Item -ItemType Directory -Path $outputRoot -Force | Out-Null
Copy-Item -LiteralPath (Join-Path $repoRoot "UnityPackages\$packageName") -Destination $outputRoot -Recurse -Force
Get-ChildItem -LiteralPath $packageRoot -Directory -Recurse | Sort-Object FullName -Descending | ForEach-Object {
    if (@(Get-ChildItem -LiteralPath $_.FullName -Force).Count -eq 0) { Remove-Item -LiteralPath $_.FullName }
}

$endpointProject = Join-Path $repoRoot 'OASIS Omniverse\OGEngineClient\Edge\NextGenSoftware.OGEngine.Client.Edge.csproj'
& dotnet build $endpointProject --configuration $Configuration --framework netstandard2.1 --nologo
if ($LASTEXITCODE -ne 0) { throw "OGEngineClient Edge build failed with exit code $LASTEXITCODE." }

$buildOutput = Join-Path (Split-Path $endpointProject -Parent) "bin\$Configuration\netstandard2.1"
$depsPath = Join-Path $buildOutput 'NextGenSoftware.OGEngine.Client.Edge.deps.json'
if (-not (Test-Path -LiteralPath $depsPath)) { throw "Edge dependency manifest is missing: $depsPath" }
$deps = Get-Content -LiteralPath $depsPath -Raw | ConvertFrom-Json
$forbidden = @('NextGenSoftware.OASIS.API.Core', 'NextGenSoftware.OASIS.Common', 'Microsoft.Data.SqlClient', 'MailKit', 'NBitcoin', 'AutoMapper')
$libraryNames = @($deps.libraries.PSObject.Properties.Name | ForEach-Object { ($_ -split '/')[0] })
$leaked = @($forbidden | Where-Object { $libraryNames -contains $_ })
if ($leaked.Count -ne 0) { throw "Server/full-runtime dependencies leaked into Unity Edge: $($leaked -join ', ')." }

$managedRoot = Join-Path $packageRoot 'Runtime\Plugins\Managed'
New-Item -ItemType Directory -Path $managedRoot -Force | Out-Null
$nativeEndpointOutput = Join-Path $repoRoot "Native EndPoint\NextGenSoftware.OASIS.API.Native.Integrated.EndPoint.Edge\bin\$Configuration\netstandard2.1"
Get-ChildItem -LiteralPath $nativeEndpointOutput -Filter '*.dll' -File | Copy-Item -Destination $managedRoot -Force
Get-ChildItem -LiteralPath $buildOutput -Filter '*.dll' -File | Copy-Item -Destination $managedRoot -Force

$nugetRoot = if ($env:NUGET_PACKAGES) { $env:NUGET_PACKAGES } else { Join-Path $env:USERPROFILE '.nuget\packages' }
$sqliteVersion = '2.1.13'
$nativeInputs = @(
    @{ Source = "sqlitepclraw.lib.e_sqlite3\$sqliteVersion\runtimes\win-x86\native\e_sqlite3.dll"; Destination = 'Runtime\Plugins\x86\e_sqlite3.dll' },
    @{ Source = "sqlitepclraw.lib.e_sqlite3\$sqliteVersion\runtimes\win-x64\native\e_sqlite3.dll"; Destination = 'Runtime\Plugins\x86_64\e_sqlite3.dll' },
    @{ Source = "sqlitepclraw.lib.e_sqlite3\$sqliteVersion\runtimes\osx-x64\native\libe_sqlite3.dylib"; Destination = 'Runtime\Plugins\macOS\x86_64\libe_sqlite3.dylib' },
    @{ Source = "sqlitepclraw.lib.e_sqlite3\$sqliteVersion\runtimes\osx-arm64\native\libe_sqlite3.dylib"; Destination = 'Runtime\Plugins\macOS\arm64\libe_sqlite3.dylib' },
    @{ Source = "sqlitepclraw.lib.e_sqlite3\$sqliteVersion\runtimes\linux-x64\native\libe_sqlite3.so"; Destination = 'Runtime\Plugins\Linux\x86_64\libe_sqlite3.so' },
    @{ Source = "sqlitepclraw.lib.e_sqlite3.ios\$sqliteVersion\static\device\e_sqlite3.a"; Destination = 'Runtime\Plugins\iOS\device\libe_sqlite3.a' },
    @{ Source = "sqlitepclraw.lib.e_sqlite3.ios\$sqliteVersion\static\simulator\e_sqlite3.a"; Destination = 'Runtime\Plugins\iOS\simulator\libe_sqlite3.a' }
)
foreach ($input in $nativeInputs) {
    $source = Join-Path $nugetRoot $input.Source
    if (-not (Test-Path -LiteralPath $source)) { throw "Required Unity native SQLite artifact is missing: $source" }
    $destination = Join-Path $packageRoot $input.Destination
    New-Item -ItemType Directory -Path (Split-Path $destination -Parent) -Force | Out-Null
    Copy-Item -LiteralPath $source -Destination $destination -Force
}

# The SQLitePCLRaw Android NuGet payload is a JNI-only zip with an .aar suffix,
# not an Android library. Import its four native libraries as architecture-bound
# Unity plugins so Gradle has exactly one owner for each packaged ABI.
$androidSource = Join-Path $nugetRoot "sqlitepclraw.lib.e_sqlite3.android\$sqliteVersion\lib\net6.0-android31.0\SQLitePCLRaw.lib.e_sqlite3.android.aar"
if (-not (Test-Path -LiteralPath $androidSource -PathType Leaf)) {
    throw "Required Unity Android SQLite artifact is missing: $androidSource"
}
Add-Type -AssemblyName System.IO.Compression.FileSystem
$androidArchive = [IO.Compression.ZipFile]::OpenRead($androidSource)
try {
    foreach ($abi in @('armeabi-v7a', 'arm64-v8a', 'x86', 'x86_64')) {
        $entry = $androidArchive.GetEntry("jni/$abi/libe_sqlite3.so")
        if ($null -eq $entry) { throw "SQLite Android payload is missing ABI '$abi'." }
        $destination = Join-Path $packageRoot "Runtime\Plugins\Android\libs\$abi\libe_sqlite3.so"
        New-Item -ItemType Directory -Path (Split-Path $destination -Parent) -Force | Out-Null
        $inputStream = $entry.Open()
        $outputStream = [IO.File]::Create($destination)
        try { $inputStream.CopyTo($outputStream) }
        finally { $outputStream.Dispose(); $inputStream.Dispose() }
    }
}
finally { $androidArchive.Dispose() }

function Get-DeterministicUnityGuid {
    param([Parameter(Mandatory)][string]$RelativePath)
    $bytes = [Text.Encoding]::UTF8.GetBytes("$packageName/$($RelativePath.Replace('\', '/').ToLowerInvariant())")
    $sha = [Security.Cryptography.SHA256]::Create()
    try { return ([BitConverter]::ToString($sha.ComputeHash($bytes))).Replace('-', '').Substring(0, 32).ToLowerInvariant() }
    finally { $sha.Dispose() }
}

function Write-UnityMeta {
    param([Parameter(Mandatory)][string]$AssetPath, [Parameter(Mandatory)][string]$Importer)
    $relative = [IO.Path]::GetRelativePath($packageRoot, $AssetPath).Replace('\', '/')
    $guid = Get-DeterministicUnityGuid $relative
    $content = "fileFormatVersion: 2`nguid: $guid`n$Importer"
    [IO.File]::WriteAllText("$AssetPath.meta", $content.Replace("`n", [Environment]::NewLine),
        [Text.UTF8Encoding]::new($false))
}

$defaultImporter = "DefaultImporter:`n  externalObjects: {}`n  userData: `n  assetBundleName: `n  assetBundleVariant: `n"
$folderImporter = "folderAsset: yes`nDefaultImporter:`n  externalObjects: {}`n  userData: `n  assetBundleName: `n  assetBundleVariant: `n"
$monoImporter = "MonoImporter:`n  externalObjects: {}`n  serializedVersion: 2`n  defaultReferences: []`n  executionOrder: 0`n  icon: {instanceID: 0}`n  userData: `n  assetBundleName: `n  assetBundleVariant: `n"
$asmdefImporter = "AssemblyDefinitionImporter:`n  externalObjects: {}`n  userData: `n  assetBundleName: `n  assetBundleVariant: `n"
$managedPluginImporter = "PluginImporter:`n  externalObjects: {}`n  serializedVersion: 2`n  iconMap: {}`n  executionOrder: {}`n  defineConstraints: []`n  isPreloaded: 0`n  isOverridable: 1`n  isExplicitlyReferenced: 0`n  validateReferences: 1`n  platformData:`n  - first:`n      Any: `n    second:`n      enabled: 1`n      settings: {}`n  userData: `n  assetBundleName: `n  assetBundleVariant: `n"
$iosPluginImporter = "PluginImporter:`n  externalObjects: {}`n  serializedVersion: 2`n  iconMap: {}`n  executionOrder: {}`n  defineConstraints: []`n  isPreloaded: 0`n  isOverridable: 0`n  isExplicitlyReferenced: 0`n  validateReferences: 1`n  platformData:`n  - first:`n      Any: `n    second:`n      enabled: 0`n      settings: {}`n  - first:`n      iPhone: iOS`n    second:`n      enabled: 1`n      settings: {}`n  userData: `n  assetBundleName: `n  assetBundleVariant: `n"
$linuxPluginImporter = "PluginImporter:`n  externalObjects: {}`n  serializedVersion: 2`n  iconMap: {}`n  executionOrder: {}`n  defineConstraints: []`n  isPreloaded: 0`n  isOverridable: 0`n  isExplicitlyReferenced: 0`n  validateReferences: 1`n  platformData:`n  - first:`n      Any: `n    second:`n      enabled: 0`n      settings: {}`n  - first:`n      Standalone: Linux64`n    second:`n      enabled: 1`n      settings:`n        CPU: x86_64`n  userData: `n  assetBundleName: `n  assetBundleVariant: `n"
$macPluginImporter = "PluginImporter:`n  externalObjects: {}`n  serializedVersion: 2`n  iconMap: {}`n  executionOrder: {}`n  defineConstraints: []`n  isPreloaded: 0`n  isOverridable: 0`n  isExplicitlyReferenced: 0`n  validateReferences: 1`n  platformData:`n  - first:`n      Any: `n    second:`n      enabled: 0`n      settings: {}`n  - first:`n      Editor: Editor`n    second:`n      enabled: 1`n      settings:`n        DefaultValueInitialized: true`n        OS: OSX`n  - first:`n      Standalone: OSXUniversal`n    second:`n      enabled: 1`n      settings:`n        CPU: AnyCPU`n  userData: `n  assetBundleName: `n  assetBundleVariant: `n"

# UPM Asset Store submissions require committed metadata for every shipped asset. Generated assemblies receive
# deterministic GUIDs so rebuilding the package never breaks Unity references.
Get-ChildItem -LiteralPath $packageRoot -Directory -Recurse | ForEach-Object {
    if (-not (Test-Path -LiteralPath "$($_.FullName).meta")) { Write-UnityMeta $_.FullName $folderImporter }
}
Get-ChildItem -LiteralPath $packageRoot -File -Recurse | Where-Object { $_.Extension -ne '.meta' } | ForEach-Object {
    if (Test-Path -LiteralPath "$($_.FullName).meta") { return }
    $relative = [IO.Path]::GetRelativePath($packageRoot, $_.FullName).Replace('\', '/')
    if ($relative.StartsWith('Runtime/Plugins/Managed/', [StringComparison]::Ordinal) -and $_.Extension -eq '.dll') {
        Write-UnityMeta $_.FullName $managedPluginImporter
    }
    elseif ($relative.StartsWith('Runtime/Plugins/iOS/', [StringComparison]::Ordinal) -and
        $_.Extension -in @('.a', '.mm')) { Write-UnityMeta $_.FullName $iosPluginImporter }
    elseif ($relative.StartsWith('Runtime/Plugins/Linux/', [StringComparison]::Ordinal) -and
        $_.Extension -eq '.so') { Write-UnityMeta $_.FullName $linuxPluginImporter }
    elseif ($relative.StartsWith('Runtime/Plugins/macOS/', [StringComparison]::Ordinal) -and
        $_.Extension -eq '.dylib') { Write-UnityMeta $_.FullName $macPluginImporter }
    elseif ($_.Extension -eq '.cs') { Write-UnityMeta $_.FullName $monoImporter }
    elseif ($_.Extension -eq '.asmdef') { Write-UnityMeta $_.FullName $asmdefImporter }
    else { Write-UnityMeta $_.FullName $defaultImporter }
}

$files = Get-ChildItem -LiteralPath $packageRoot -Recurse -File | Sort-Object FullName
$manifest = [ordered]@{
    schemaVersion = 1
    package = $packageName
    configuration = $Configuration
    sourceCommit = (& git -C $repoRoot rev-parse HEAD).Trim()
    files = @($files | ForEach-Object {
        [ordered]@{
            path = $_.FullName.Substring($packageRoot.Length + 1).Replace('\', '/')
            sha256 = (Get-FileHash -LiteralPath $_.FullName -Algorithm SHA256).Hash
            bytes = $_.Length
        }
    })
}
$buildManifestPath = Join-Path $packageRoot 'build-manifest.json'
$manifest | ConvertTo-Json -Depth 5 | Set-Content -LiteralPath $buildManifestPath -Encoding utf8
Write-UnityMeta $buildManifestPath $defaultImporter

$archive = Join-Path $outputRoot "$packageName.tgz"
if (Test-Path -LiteralPath $archive) { Remove-Item -LiteralPath $archive -Force }
& tar -czf $archive -C $outputRoot $packageName
if ($LASTEXITCODE -ne 0 -or -not (Test-Path -LiteralPath $archive)) { throw 'Failed to create the Unity package archive.' }
Write-Host "Unity Edge package created: $archive"
