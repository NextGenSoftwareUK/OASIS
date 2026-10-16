[CmdletBinding()]
param()
$ErrorActionPreference = 'Stop'
if ($env:GITHUB_ACTIONS -ne 'true' -or -not $IsLinux) { throw 'Generated fixture builds are hosted Linux only.' }
$root = Split-Path $PSScriptRoot -Parent
$workspace = Split-Path $root -Parent
$client = Join-Path $workspace 'holochain-client-csharp'
$output = Join-Path $root 'artifacts/holonet-generated-fixture'
$source = Join-Path $output 'source'
$project = Join-Path $client 'NextGenSoftware.Holochain.HoloNET.Client.Tests/NextGenSoftware.Holochain.HoloNET.Client.Tests.csproj'
$commit = (& git -C $client rev-parse HEAD).Trim()
if ($LASTEXITCODE -ne 0) { throw 'Cannot resolve generator source.' }
$env:HOLONET_GENERATE_HAPP_TO = $source
$env:HOLONET_GENERATE_HAPP_VERSION = '0.7.1'
& dotnet test $project -c Release --filter 'FullyQualifiedName~WriteSampleHappForTheBuildScript' --logger 'trx;LogFileName=generation.trx' --results-directory $output
if ($LASTEXITCODE -ne 0) { throw 'Sample generation failed.' }
[xml]$trx = Get-Content (Join-Path $output 'generation.trx') -Raw
$counts = $trx.SelectSingleNode('//*[local-name()="Counters"]')
if ([int]$counts.total -ne 1 -or [int]$counts.passed -ne 1) { throw 'Generator test must execute and pass.' }
$env:HOLONET_GENERATED_SOURCE = $source
Push-Location (Join-Path $workspace 'OASIS-Holochain-hApp')
try {
    # Reuse the already pinned Nix toolchain used for the primary hApp build.
    & nix develop . --command bash -euo pipefail -c 'cd "$HOLONET_GENERATED_SOURCE"; export RUSTFLAGS=''--cfg getrandom_backend="custom"''; cargo build --release --target wasm32-unknown-unknown; hc dna pack dnas/holonet_game/workdir; hc app pack workdir'
    if ($LASTEXITCODE -ne 0) { throw 'Generated hApp compilation/packing failed.' }
} finally { Pop-Location }
$bundle = Join-Path $source 'workdir/holonet_game.happ'
if (-not (Test-Path $bundle)) { throw 'Generated hApp bundle missing.' }
# Transfer only runtime fixtures and model sources, never the Cargo target tree.
$package = Join-Path $output 'package'
New-Item -ItemType Directory -Force $package | Out-Null
Copy-Item $bundle (Join-Path $package 'holonet_game.happ')
Copy-Item (Join-Path $source 'CSharp') $package -Recurse
$files = @{}
Get-ChildItem $package -Recurse -File | ForEach-Object {
    $files[[IO.Path]::GetRelativePath($package, $_.FullName).Replace('\','/')] = (Get-FileHash $_.FullName -Algorithm SHA256).Hash
}
@{ sourceCommit=$commit; holochainVersion='0.7.1'; files=$files } | ConvertTo-Json -Depth 5 | Set-Content (Join-Path $package 'manifest.json')
