param([Parameter(Mandatory)][string] $DenoKv)

$ErrorActionPreference = 'Stop'
$root = (Resolve-Path (Join-Path $PSScriptRoot '../..')).Path
$core = Join-Path $root 'OASIS Architecture/NextGenSoftware.OASIS.API.Core'
$project = Join-Path $root 'Providers/Storage/TestProjects/NextGenSoftware.OASIS.API.Providers.EdgeStorage.ProtocolTests/NextGenSoftware.OASIS.API.Providers.EdgeStorage.ProtocolTests.csproj'
$fixture = Join-Path $PSScriptRoot 'run-cosmic-denokv-windows.py'
$DenoKv = (Resolve-Path -LiteralPath $DenoKv).Path
$output = Join-Path $root 'LocalState/COSMIC/HostedDenoKvCqlQualification'
& (Join-Path $PSScriptRoot 'Invoke-WithProviderBuildLease.ps1') -Owner cosmic-denokv-native -Operation {
    $original = git -C $core rev-parse HEAD
    if ($LASTEXITCODE -or (git -C $core status --porcelain)) { throw 'Core must start clean' }
    $pin = git -C $root rev-parse 'HEAD:OASIS Architecture/NextGenSoftware.OASIS.API.Core'
    if ($LASTEXITCODE) { throw 'Parent Core pin unavailable' }
    New-Item -ItemType Directory -Path $output -Force | Out-Null
    $pin | Set-Content -LiteralPath (Join-Path $output 'standalone-core-pin.txt')
    try {
        git -C $core switch --detach $pin
        if ($LASTEXITCODE) { throw 'Could not select Core pin' }
        $buildLog = Join-Path $output 'standalone-build.log'
        dotnet build $project /m:2 /nr:false /p:UseSharedCompilation=false -v:q *> $buildLog
        if ($LASTEXITCODE) { throw "Deno KV native test build failed. See $buildLog" }
        python -B $fixture --denokv $DenoKv --results-directory $output
        if ($LASTEXITCODE) { throw 'Native Deno KV test failed' }
    }
    finally {
        git -C $core switch --detach $original
        if ($LASTEXITCODE) { throw 'Could not restore Core' }
    }
}
