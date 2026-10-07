[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [string]$PackageDirectory,

    [string]$OurWorldProjectRoot = ''
)

$ErrorActionPreference = 'Stop'
$repoRoot = Split-Path $PSScriptRoot -Parent
$source = if ([IO.Path]::IsPathRooted($PackageDirectory)) {
    [IO.Path]::GetFullPath($PackageDirectory)
}
else {
    [IO.Path]::GetFullPath((Join-Path $repoRoot $PackageDirectory))
}
$artifactsRoot = [IO.Path]::GetFullPath((Join-Path $repoRoot 'artifacts'))
$manifest = Join-Path $source 'build-manifest.json'
$artifactsPrefix = $artifactsRoot.TrimEnd([IO.Path]::DirectorySeparatorChar) + [IO.Path]::DirectorySeparatorChar
if (-not $source.StartsWith($artifactsPrefix, [StringComparison]::OrdinalIgnoreCase) -or
    -not (Test-Path -LiteralPath $manifest -PathType Leaf)) {
    throw "A generated, manifested Edge package inside '$artifactsRoot' is required."
}

$projectRoot = if ([string]::IsNullOrWhiteSpace($OurWorldProjectRoot)) {
    [IO.Path]::GetFullPath((Join-Path $repoRoot 'OASIS Omniverse\OASIS Hub'))
}
elseif ([IO.Path]::IsPathRooted($OurWorldProjectRoot)) {
    [IO.Path]::GetFullPath($OurWorldProjectRoot)
}
else {
    [IO.Path]::GetFullPath((Join-Path $repoRoot $OurWorldProjectRoot))
}

$projectManifestPath = Join-Path $projectRoot 'Packages\manifest.json'
if (-not (Test-Path -LiteralPath $projectManifestPath -PathType Leaf)) {
    throw "Our World project manifest not found: $projectManifestPath"
}

$projectManifest = Get-Content -LiteralPath $projectManifestPath -Raw | ConvertFrom-Json
$edgeDependency = $projectManifest.dependencies.'com.nextgensoftware.oasis.edge'
if ($edgeDependency -ne 'file:com.nextgensoftware.oasis.edge') {
    throw "Our World must reference the synchronized local Edge package as 'file:com.nextgensoftware.oasis.edge'; found '$edgeDependency'."
}

$packagesRoot = [IO.Path]::GetFullPath((Join-Path $projectRoot 'Packages'))
$destination = [IO.Path]::GetFullPath((Join-Path $packagesRoot 'com.nextgensoftware.oasis.edge'))
$expectedDestination = [IO.Path]::GetFullPath((Join-Path $projectRoot 'Packages\com.nextgensoftware.oasis.edge'))
if (-not $destination.Equals($expectedDestination, [StringComparison]::OrdinalIgnoreCase) -or
    -not $destination.StartsWith($packagesRoot.TrimEnd([IO.Path]::DirectorySeparatorChar) + [IO.Path]::DirectorySeparatorChar,
        [StringComparison]::OrdinalIgnoreCase)) {
    throw "Refusing to synchronize an Edge package outside the selected Our World Packages directory: $destination"
}

if (Test-Path -LiteralPath $destination) { Remove-Item -LiteralPath $destination -Recurse -Force }
New-Item -ItemType Directory -Path (Split-Path $destination -Parent) -Force | Out-Null
Copy-Item -LiteralPath $source -Destination $destination -Recurse -Force
Write-Host "Our World Edge package synchronized: $destination"
