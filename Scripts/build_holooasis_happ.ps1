[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'
$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path
$sourceRoot = Join-Path (Split-Path $repoRoot -Parent) 'OASIS-Holochain-hApp'
$destinationDirectory = Join-Path $repoRoot 'Providers/Network/NextGenSoftware.OASIS.API.Providers.HoloOASIS/OASIS_hAPP'
$sourceArtifact = Join-Path $sourceRoot 'workdir/oasis.happ'
$destinationArtifact = Join-Path $destinationDirectory 'oasis.happ'
$manifestPath = Join-Path $destinationDirectory 'build-manifest.json'

if (!(Test-Path -LiteralPath $sourceRoot -PathType Container)) {
    throw "Checkout NextGenSoftwareUK/OASIS-Holochain-hApp beside OASIS at '$sourceRoot'."
}
$sourceStatus = & git -C $sourceRoot status --porcelain
if ($LASTEXITCODE -ne 0) { throw "Unable to inspect the Holochain hApp source repository." }
if ($sourceStatus) {
    throw 'The Holochain hApp source tree must be clean so the tested artifact can be tied to an exact commit.'
}
$sourceCommit = (& git -C $sourceRoot rev-parse HEAD).Trim()
if ($LASTEXITCODE -ne 0 -or [string]::IsNullOrWhiteSpace($sourceCommit)) {
    throw 'Unable to resolve the Holochain hApp source commit.'
}

Push-Location $sourceRoot
try {
    # The hApp's Git-backed flake deliberately excludes ignored target/node_modules
    # content. Do not change this to `path:.`: doing so copies those directories into
    # the Nix store before the flake is evaluated and can consume tens of gigabytes.
    $buildCommand = 'cargo test -p oasis_integrity --lib && cargo build --release --target wasm32-unknown-unknown && hc dna pack dnas/oasis/workdir && cargo test -p oasis --test sweettest && hc app pack workdir'
    if ($IsWindows) {
        if (!(Get-Command wsl.exe -ErrorAction SilentlyContinue)) {
            throw 'WSL with Nix is required to build the pinned Holochain toolchain on Windows.'
        }
        $wslSourceRoot = (& wsl.exe wslpath -a ($sourceRoot -replace '\\', '/')).Trim()
        if ($LASTEXITCODE -ne 0 -or [string]::IsNullOrWhiteSpace($wslSourceRoot)) {
            throw "Unable to resolve the WSL path for '$sourceRoot'."
        }
        & wsl.exe --cd $wslSourceRoot nix develop . --command bash -lc $buildCommand
    }
    else {
        if (!(Get-Command nix -ErrorAction SilentlyContinue)) {
            throw 'Nix is required to build and test the pinned Holochain toolchain.'
        }
        & nix develop . --command bash -lc $buildCommand
    }
    if ($LASTEXITCODE -ne 0) { throw "Holochain hApp tests/build failed with exit code $LASTEXITCODE." }
}
finally { Pop-Location }

if (!(Test-Path -LiteralPath $sourceArtifact -PathType Leaf)) {
    throw "The Holochain build completed without producing '$sourceArtifact'."
}

$sourcePaths = @('Cargo.toml', 'Cargo.lock', 'package.json', 'package-lock.json', 'flake.nix', 'flake.lock', 'dnas', 'tests', 'workdir/happ.yaml')
$files = foreach ($relativePath in $sourcePaths) {
    $path = Join-Path $sourceRoot $relativePath
    if (Test-Path -LiteralPath $path -PathType Leaf) { Get-Item -LiteralPath $path }
    elseif (Test-Path -LiteralPath $path -PathType Container) { Get-ChildItem -LiteralPath $path -File -Recurse }
    else { throw "Required Holochain hApp source path is missing: $path" }
}
$lines = $files | Sort-Object FullName | ForEach-Object {
    $relative = [IO.Path]::GetRelativePath($sourceRoot, $_.FullName).Replace('\', '/')
    "$relative=$((Get-FileHash -LiteralPath $_.FullName -Algorithm SHA256).Hash)"
}
$sha = [Security.Cryptography.SHA256]::Create()
try {
    $sourceDigest = ([BitConverter]::ToString($sha.ComputeHash([Text.Encoding]::UTF8.GetBytes(($lines -join "`n"))))).Replace('-', '')
}
finally { $sha.Dispose() }

New-Item -ItemType Directory -Path $destinationDirectory -Force | Out-Null
Copy-Item -LiteralPath $sourceArtifact -Destination $destinationArtifact -Force
$manifest = [ordered]@{
    schemaVersion = 1
    sourceCommit = $sourceCommit
    sourceSha256 = $sourceDigest
    artifactSha256 = (Get-FileHash -LiteralPath $destinationArtifact -Algorithm SHA256).Hash
    builtAtUtc = [DateTime]::UtcNow.ToString('O')
}
$manifest | ConvertTo-Json | Set-Content -LiteralPath $manifestPath -Encoding utf8
Write-Host "Built, tested and bundled HoloOASIS hApp: $destinationArtifact"
