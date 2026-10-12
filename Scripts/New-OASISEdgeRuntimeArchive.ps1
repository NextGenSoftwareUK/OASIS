[CmdletBinding()]
param(
    [Parameter(Mandatory)][string]$EvidenceDirectory,
    [Parameter(Mandatory)][string]$OutputPath
)
$ErrorActionPreference = 'Stop'
$package = Join-Path $EvidenceDirectory 'com.nextgensoftware.oasis.edge.tgz'
if (-not (Test-Path -LiteralPath $package -PathType Leaf)) { throw 'Validated Unity runtime package is missing.' }
$stage = Join-Path (Split-Path ([IO.Path]::GetFullPath($OutputPath)) -Parent) 'edge-runtime-public'
$extracted = Join-Path $stage 'unpacked'
$public = Join-Path $stage 'runtime'
if (Test-Path -LiteralPath $stage) { throw 'Public packaging stage already exists; use a clean hosted job.' }
New-Item -ItemType Directory -Path $extracted,$public -Force | Out-Null
& tar -xzf $package -C $extracted
if ($LASTEXITCODE -ne 0) { throw 'Runtime package extraction failed.' }
$source = Join-Path $extracted 'com.nextgensoftware.oasis.edge'
if (-not (Test-Path (Join-Path $source 'Runtime')) -or -not (Test-Path (Join-Path $source 'package.json'))) {
    throw 'The validated package lacks Runtime or package.json.'
}
# Explicit installation payload allowlist: never copy the evidence directory.
foreach ($name in @('Runtime','Editor','package.json','README.md','LICENSE','LICENSE.md','LICENSE.txt')) {
    $path = Join-Path $source $name
    if (Test-Path -LiteralPath $path) { Copy-Item -LiteralPath $path -Destination $public -Recurse }
}
$unwanted = @(Get-ChildItem -LiteralPath $public -Recurse -File | Where-Object {
    $_.Extension -in @('.trx','.log','.nupkg') -or $_.Name -match 'acceptance-report|sbom|SHA256SUMS|provenance'
})
if ($unwanted.Count) { throw "Non-runtime files leaked into the public payload: $($unwanted.Name -join ', ')" }
$assemblies = @(Get-ChildItem -LiteralPath (Join-Path $public 'Runtime') -Recurse -Filter '*.dll' -File)
if (-not @($assemblies | Where-Object Name -eq 'NextGenSoftware.OASIS.Edge.Runtime.dll').Count) {
    throw 'Edge Runtime assembly is missing from the installable payload.'
}
Compress-Archive -Path "$public/*" -DestinationPath $OutputPath -Force
Write-Host "Runtime-only archive: $OutputPath; assemblies: $($assemblies.Count)"
