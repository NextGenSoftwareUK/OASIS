[CmdletBinding()]
param(
    [string]$PackageDirectory = 'artifacts\unity-store-candidate\com.nextgensoftware.oasis.edge',
    [string]$Archive = 'artifacts\unity-store-candidate\com.nextgensoftware.oasis.edge.tgz'
)

$ErrorActionPreference = 'Stop'
$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path
$artifactsRoot = [IO.Path]::GetFullPath((Join-Path $repoRoot 'artifacts'))
$packageRoot = [IO.Path]::GetFullPath((Join-Path $repoRoot $PackageDirectory))
$archivePath = [IO.Path]::GetFullPath((Join-Path $repoRoot $Archive))
if (-not $packageRoot.StartsWith($artifactsRoot, [StringComparison]::OrdinalIgnoreCase) -or
    -not (Test-Path -LiteralPath $packageRoot -PathType Container)) {
    throw "The generated package must exist inside '$artifactsRoot'."
}
if (-not $archivePath.StartsWith($artifactsRoot, [StringComparison]::OrdinalIgnoreCase) -or
    -not (Test-Path -LiteralPath $archivePath -PathType Leaf)) {
    throw "The generated package archive must exist inside '$artifactsRoot'."
}
if ((Get-Item -LiteralPath $archivePath).Length -gt 700MB) {
    throw 'The UPM archive exceeds the Unity Asset Store 700 MB limit.'
}

$manifestPath = Join-Path $packageRoot 'package.json'
$manifest = Get-Content -LiteralPath $manifestPath -Raw | ConvertFrom-Json
foreach ($field in @('name', 'displayName', 'version', 'unity', 'unityRelease', 'author', 'description')) {
    if ($null -eq $manifest.$field -or [string]::IsNullOrWhiteSpace([string]$manifest.$field)) {
        throw "package.json is missing required Asset Store field '$field'."
    }
}
if ($manifest.name -ne 'com.nextgensoftware.oasis.edge' -or $manifest.displayName -ne 'OGEngineClient for Unity') {
    throw 'The public package identity must describe the Edge-enabled OGEngineClient unambiguously.'
}
if ($manifest.version -notmatch '^\d+\.\d+\.\d+([+-][0-9A-Za-z.-]+)?$') {
    throw "Package version '$($manifest.version)' is not semantic versioning."
}
if ($manifest.author.name -ne 'NextGen Software Ltd') {
    throw 'The package author must exactly match the Asset Store publisher identity.'
}

$required = @(
    'README.md', 'CHANGELOG.md', 'LICENSE.md', 'THIRD PARTY NOTICES.md', 'Documentation~\index.md',
    'ThirdPartyLicenses\MIT.txt', 'ThirdPartyLicenses\SQLite-Public-Domain.txt',
    'Samples~\QuickStart\README.md',
    'Samples~\QuickStart\NextGenSoftware.OASIS.Edge.Unity.Samples.asmdef',
    'Samples~\QuickStart\OASISEdgeStatusPresenter.cs',
    'Runtime\NextGenSoftware.OASIS.Edge.Unity.asmdef', 'Runtime\OASISEdgeUnityHost.cs',
    'Runtime\UnityEdgeConnectivityMonitor.cs', 'Runtime\UnityPlatformSecureSessionStore.cs'
)
foreach ($relative in $required) {
    if (-not (Test-Path -LiteralPath (Join-Path $packageRoot $relative) -PathType Leaf)) {
        throw "Required public package content is missing: $relative"
    }
}
if (Test-Path -LiteralPath (Join-Path $packageRoot 'Editor\OASISEdgePackageValidator.cs')) {
    throw 'Internal release-validation code must not be shipped to Asset Store customers.'
}

$forbiddenExtensions = @('.apk', '.aab', '.exe', '.pdb', '.user', '.suo')
$files = @(Get-ChildItem -LiteralPath $packageRoot -File -Recurse)
foreach ($file in $files) {
    $relative = [IO.Path]::GetRelativePath($packageRoot, $file.FullName).Replace('\', '/')
    if ($relative.Length -ge 150) { throw "Asset Store path is 150 characters or longer: $relative" }
    if ($file.Extension.ToLowerInvariant() -in $forbiddenExtensions) {
        throw "Forbidden build/debug artifact is present in the public package: $relative"
    }
    if ($file.Extension -ne '.meta' -and -not (Test-Path -LiteralPath "$($file.FullName).meta")) {
        throw "Unity metafile is missing for '$relative'."
    }
}
foreach ($meta in @($files | Where-Object Extension -eq '.meta')) {
    $target = $meta.FullName.Substring(0, $meta.FullName.Length - 5)
    if (-not (Test-Path -LiteralPath $target)) { throw "Redundant Unity metafile has no asset: $($meta.FullName)" }
}

$textFiles = $files | Where-Object { $_.Extension -in @('.cs', '.json', '.md', '.txt', '.xml', '.java', '.mm') }
$forbiddenPatterns = @(
    '-----BEGIN (RSA |EC |OPENSSH )?PRIVATE KEY-----',
    '(?i)(api[_-]?key|client[_-]?secret|signing[_-]?secret)\s*[:=]\s*["''][^"'']+["'']',
    '(?i)https?://[^\s"'']*railway\.app',
    '(?i)https?://dev\.api\.'
)
foreach ($file in $textFiles) {
    $content = Get-Content -LiteralPath $file.FullName -Raw
    foreach ($pattern in $forbiddenPatterns) {
        if ($content -match $pattern) {
            throw "Potential private configuration or credential material matched '$pattern' in '$($file.FullName)'."
        }
    }
}

$archiveEntries = @(& tar -tzf $archivePath)
if ($LASTEXITCODE -ne 0 -or $archiveEntries.Count -eq 0) { throw 'The generated UPM archive cannot be read.' }
$archivePrefix = 'com.nextgensoftware.oasis.edge/'
if (@($archiveEntries | Where-Object { -not $_.StartsWith($archivePrefix, [StringComparison]::Ordinal) }).Count -ne 0) {
    throw 'Every archive entry must be nested under the single package root.'
}
foreach ($file in $files) {
    $entry = $archivePrefix + [IO.Path]::GetRelativePath($packageRoot, $file.FullName).Replace('\', '/')
    if ($entry -notin $archiveEntries) { throw "Archive is missing generated package file '$entry'." }
}

Write-Host "OGEngineClient Unity Asset Store package readiness passed: $archivePath"
