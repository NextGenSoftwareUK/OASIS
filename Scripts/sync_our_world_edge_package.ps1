[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [string]$PackageDirectory
)

$ErrorActionPreference = 'Stop'
$repoRoot = Split-Path $PSScriptRoot -Parent
$source = [IO.Path]::GetFullPath((Join-Path $repoRoot $PackageDirectory))
$artifactsRoot = [IO.Path]::GetFullPath((Join-Path $repoRoot 'artifacts'))
$manifest = Join-Path $source 'build-manifest.json'
if (-not $source.StartsWith($artifactsRoot, [StringComparison]::OrdinalIgnoreCase) -or
    -not (Test-Path -LiteralPath $manifest -PathType Leaf)) {
    throw "A generated, manifested Edge package inside '$artifactsRoot' is required."
}
$destination = Join-Path $repoRoot 'OASIS Omniverse\OASIS Hub\Packages\com.nextgensoftware.oasis.edge'
if (Test-Path -LiteralPath $destination) { Remove-Item -LiteralPath $destination -Recurse -Force }
New-Item -ItemType Directory -Path (Split-Path $destination -Parent) -Force | Out-Null
Copy-Item -LiteralPath $source -Destination $destination -Recurse -Force
Write-Host "Our World Edge package synchronized: $destination"
