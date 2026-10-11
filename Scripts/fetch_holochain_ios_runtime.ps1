[CmdletBinding()]
param(
    [string]$VersionFile = (Join-Path $PSScriptRoot 'holochain_ios_runtime.version.json')
)

# Downloads the pinned Holochain 0.7 iOS conductor (HolochainConductor.xcframework + Swift bindings) from our
# fork's GitHub release into artifacts\holochain-ios-runtime, and checks every file against the pinned SHA-256s
# and the release's own build manifest. Built by .github/workflows/ios-conductor.yml on
# NextGenSoftwareUK/android-service-runtime.

$ErrorActionPreference = 'Stop'
$repoRoot = Split-Path $PSScriptRoot -Parent
$pin = Get-Content -LiteralPath $VersionFile -Raw | ConvertFrom-Json
$outputRoot = Join-Path $repoRoot 'artifacts\holochain-ios-runtime'
New-Item -ItemType Directory -Path $outputRoot -Force | Out-Null

foreach ($asset in $pin.assets) {
    & gh release download $pin.tag -R $pin.repository -p $asset.name -D $outputRoot --clobber
    if ($LASTEXITCODE -ne 0) { throw "Could not download '$($asset.name)' from $($pin.repository) $($pin.tag)." }
    $actual = (Get-FileHash -LiteralPath (Join-Path $outputRoot $asset.name) -Algorithm SHA256).Hash
    if ($actual -ne ([string]$asset.sha256).ToUpperInvariant()) {
        throw "SHA-256 mismatch for '$($asset.name)': expected $($asset.sha256), got $actual."
    }
}

$manifest = Get-Content -LiteralPath (Join-Path $outputRoot 'build-manifest.json') -Raw | ConvertFrom-Json
if ($manifest.commit -ne $pin.commit -or $manifest.holochainCompatibility -ne '0.7') {
    throw "The release manifest does not match the pinned commit $($pin.commit) / Holochain 0.7."
}
Write-Host "Holochain iOS conductor $($pin.tag) ($($pin.commit)) verified in $outputRoot"
