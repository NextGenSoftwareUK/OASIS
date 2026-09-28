[CmdletBinding()]
param(
    [ValidateSet('Pack', 'PublishAndUnlist')]
    [string]$Operation = 'Pack',
    [string]$ManifestPath = (Join-Path $PSScriptRoot '..\Docs\Releases\NUGET_INITIAL_VERSION_REPAIR_2026-09-28.json'),
    [string]$OutputDirectory = (Join-Path $PSScriptRoot '..\artifacts\nuget-initial-version-repair'),
    [string]$NuGetApiKey = $env:NUGET_API_KEY
)

$ErrorActionPreference = 'Stop'
$repoRoot = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$manifestPath = [IO.Path]::GetFullPath($ManifestPath)
$outputDirectory = [IO.Path]::GetFullPath($OutputDirectory)

if (-not $manifestPath.StartsWith($repoRoot, [StringComparison]::OrdinalIgnoreCase)) {
    throw "Repair manifest must be inside the repository: $manifestPath"
}
if (-not (Test-Path -LiteralPath $manifestPath -PathType Leaf)) { throw "Repair manifest not found: $manifestPath" }
if ($Operation -eq 'PublishAndUnlist' -and [string]::IsNullOrWhiteSpace($NuGetApiKey)) {
    throw 'NUGET_API_KEY is required for PublishAndUnlist.'
}

$manifest = Get-Content -LiteralPath $manifestPath -Raw | ConvertFrom-Json
$packages = @($manifest.packages)
if ($packages.Count -eq 0) { throw 'Repair manifest contains no packages.' }
$duplicateIds = @($packages | Group-Object packageId | Where-Object Count -gt 1)
if ($duplicateIds.Count -gt 0) { throw "Repair manifest contains duplicate package IDs: $($duplicateIds.Name -join ', ')" }
foreach ($package in $packages) {
    if ($package.correctVersion -ne '1.0.0') { throw "$($package.packageId) must be repaired to 1.0.0." }
    if (@($package.erroneousVersions).Count -eq 0) { throw "$($package.packageId) has no erroneous versions to unlist." }
}

function Set-ProjectVersion([string]$Path, [string]$Version) {
    $content = Get-Content -LiteralPath $Path -Raw
    if ($content -match '<Version>[^<]+</Version>') {
        $content = [regex]::Replace($content, '<Version>[^<]+</Version>', "<Version>$Version</Version>")
    }
    else {
        $content = [regex]::Replace($content, '(?s)(<PropertyGroup(?:\s[^>]*)?>)', "`$1`r`n    <Version>$Version</Version>", 1)
    }
    [IO.File]::WriteAllText($Path, $content, [Text.UTF8Encoding]::new($false))
}

New-Item -ItemType Directory -Path $outputDirectory -Force | Out-Null
Get-ChildItem -LiteralPath $outputDirectory -Filter '*.nupkg' -File | Remove-Item -Force
$originalProjects = @{}
try {
    foreach ($package in $packages) {
        $projectPath = [IO.Path]::GetFullPath((Join-Path $repoRoot $package.project))
        if (-not $projectPath.StartsWith($repoRoot, [StringComparison]::OrdinalIgnoreCase)) { throw "Project escapes repository: $($package.project)" }
        if (-not (Test-Path -LiteralPath $projectPath -PathType Leaf)) { throw "Project not found: $($package.project)" }
        $xml = [xml](Get-Content -LiteralPath $projectPath -Raw)
        $declaredId = @($xml.Project.PropertyGroup.PackageId | Where-Object { $_ } | Select-Object -First 1)
        if ($declaredId.Count -eq 0) { $declaredId = @([IO.Path]::GetFileNameWithoutExtension($projectPath)) }
        if ($declaredId[0] -ne $package.packageId) {
            throw "Manifest package ID $($package.packageId) does not match $($declaredId[0]) in $($package.project)."
        }
        $originalProjects[$projectPath] = [IO.File]::ReadAllBytes($projectPath)
        Set-ProjectVersion $projectPath $package.correctVersion
    }
    foreach ($package in $packages) {
        $projectPath = [IO.Path]::GetFullPath((Join-Path $repoRoot $package.project))
        & dotnet pack $projectPath -c Release -o $outputDirectory --nologo
        if ($LASTEXITCODE -ne 0) { throw "Packing $($package.packageId) $($package.correctVersion) failed." }
    }
}
finally {
    foreach ($entry in $originalProjects.GetEnumerator()) { [IO.File]::WriteAllBytes($entry.Key, $entry.Value) }
}

Add-Type -AssemblyName System.IO.Compression.FileSystem
$built = @{}
foreach ($file in Get-ChildItem -LiteralPath $outputDirectory -Filter '*.nupkg' -File | Where-Object Name -notlike '*.symbols.nupkg') {
    $archive = [IO.Compression.ZipFile]::OpenRead($file.FullName)
    try {
        $nuspecEntry = @($archive.Entries | Where-Object FullName -like '*.nuspec')
        if ($nuspecEntry.Count -ne 1) { throw "$($file.Name) must contain exactly one nuspec." }
        $reader = [IO.StreamReader]::new($nuspecEntry[0].Open())
        try { $nuspec = [xml]$reader.ReadToEnd() } finally { $reader.Dispose() }
        $id = [string]$nuspec.package.metadata.id
        $version = [string]$nuspec.package.metadata.version
        if ($version -ne '1.0.0') { throw "$($file.Name) contains version $version, expected 1.0.0." }
        if ($built.ContainsKey($id)) { throw "Multiple repair packages were built for $id." }
        $built[$id] = $file.FullName
    }
    finally { $archive.Dispose() }
}
foreach ($package in $packages) {
    if (-not $built.ContainsKey($package.packageId)) { throw "No 1.0.0 package was built for $($package.packageId)." }
}
if ($built.Count -ne $packages.Count) { throw "Built $($built.Count) packages for a $($packages.Count)-package manifest." }

if ($Operation -eq 'Pack') {
    Write-Host "Validated $($built.Count) corrected NuGet packages at version 1.0.0."
    exit 0
}

# Publish every corrected package before unlisting any bad version. This keeps the repair atomic from a consumer's perspective.
foreach ($package in $packages) {
    & dotnet nuget push $built[$package.packageId] --source https://api.nuget.org/v3/index.json --api-key $NuGetApiKey --skip-duplicate
    if ($LASTEXITCODE -ne 0) { throw "Publishing $($package.packageId) 1.0.0 failed; no versions were unlisted." }
}
foreach ($package in $packages) {
    foreach ($version in @($package.erroneousVersions)) {
        & dotnet nuget delete $package.packageId $version --source https://api.nuget.org/v3/index.json --api-key $NuGetApiKey --non-interactive
        if ($LASTEXITCODE -ne 0) { throw "Unlisting $($package.packageId) $version failed." }
    }
}

Write-Host "Published $($packages.Count) corrected 1.0.0 packages and unlisted every manifest-recorded erroneous version."
