param(
    [Parameter(Mandatory)][string] $Daprd,
    [Parameter(Mandatory)][string] $Garnet
)

$ErrorActionPreference = 'Stop'
$root = (Resolve-Path (Join-Path $PSScriptRoot '../..')).Path
$core = Join-Path $root 'OASIS Architecture/NextGenSoftware.OASIS.API.Core'
$project = Join-Path $root 'ONODE/NextGenSoftware.OASIS.API.ONODE.WebAPI.UnitTests/NextGenSoftware.OASIS.API.ONODE.WebAPI.UnitTests.csproj'
$fixture = Join-Path $PSScriptRoot 'run-cosmic-dapr-web4-native.py'
$Daprd = (Resolve-Path -LiteralPath $Daprd).Path
$Garnet = (Resolve-Path -LiteralPath $Garnet).Path
$output = Join-Path $root 'LocalState/COSMIC/LocalDaprCqlQualification'
& (Join-Path $PSScriptRoot 'Invoke-WithProviderBuildLease.ps1') -Owner cosmic-dapr-web4-native -Operation {
    $original = git -C $core rev-parse HEAD
    if ($LASTEXITCODE -or (git -C $core status --porcelain)) { throw 'Core must start clean' }
    $pin = git -C $root rev-parse 'HEAD:OASIS Architecture/NextGenSoftware.OASIS.API.Core'
    if ($LASTEXITCODE) { throw 'Parent Core pin unavailable' }
    New-Item -ItemType Directory -Path $output -Force | Out-Null
    $pin | Set-Content -LiteralPath (Join-Path $output 'garnet-web4-core-pin.txt')
    try {
        git -C $core switch --detach $pin
        if ($LASTEXITCODE) { throw 'Could not select Core pin' }
        $buildLog = Join-Path $output 'garnet-web4-build.log'
        dotnet build $project /m:2 /nr:false /p:UseSharedCompilation=false -v:q *> $buildLog
        if ($LASTEXITCODE) { throw "Dapr WEB4 test build failed. See $buildLog" }
        python -B $fixture --daprd $Daprd --garnet $Garnet --results-directory $output
        if ($LASTEXITCODE) { throw 'Dapr/Garnet WEB4 HTTP qualification failed' }
    }
    finally {
        git -C $core switch --detach $original
        if ($LASTEXITCODE) { throw 'Could not restore the original Core checkout' }
    }
}
