[CmdletBinding()]
param([ValidateSet('Debug', 'Release')][string]$Configuration = 'Release')

$ErrorActionPreference = 'Stop'
$repoRoot = Split-Path $PSScriptRoot -Parent
$project = Join-Path $repoRoot 'OASIS Omniverse\OGEngineClient\OGEngineClient.csproj'
$profileRoot = Join-Path $repoRoot 'artifacts\ogengine-profiles'
if (Test-Path -LiteralPath $profileRoot) { Remove-Item -LiteralPath $profileRoot -Recurse -Force }

foreach ($profile in @(
    @{ Name = 'edge'; IncludeEdge = 'true' },
    @{ Name = 'remote-only'; IncludeEdge = 'false' }
)) {
    $output = Join-Path $profileRoot $profile.Name
    & dotnet publish $project -c $Configuration -p:PublishAot=false `
        -p:OGEngineIncludeEdge=$($profile.IncludeEdge) -o $output --nologo
    if ($LASTEXITCODE -ne 0) { throw "OGEngineClient $($profile.Name) profile failed to build." }
    $edgeAssembly = Join-Path $output 'NextGenSoftware.OGEngine.Client.Edge.dll'
    if ($profile.IncludeEdge -eq 'true' -and -not (Test-Path -LiteralPath $edgeAssembly -PathType Leaf)) {
        throw 'The standard OGEngineClient Edge profile omitted its Edge assembly.'
    }
    if ($profile.IncludeEdge -eq 'false' -and (Test-Path -LiteralPath $edgeAssembly -PathType Leaf)) {
        throw 'The OGEngineClient Remote-Only profile incorrectly contains the Edge assembly.'
    }
}

Write-Host "OGEngineClient Edge and Remote-Only profiles passed: $profileRoot"
