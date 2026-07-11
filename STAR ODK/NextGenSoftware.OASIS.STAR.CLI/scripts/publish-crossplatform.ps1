# Build STAR CLI for all platforms (single-file, self-contained).
# Run from repo root or from STAR ODK\NextGenSoftware.OASIS.STAR.CLI.
# Requires: .NET 8 SDK

$ErrorActionPreference = "Stop"
$ProjectDir = if ($PSScriptRoot) { Split-Path $PSScriptRoot } else { "." }
$PublishDir = Join-Path $ProjectDir "publish"

$Rids = @(
    "win-x64",
    "win-arm64",
    "linux-x64",
    "linux-arm64",
    "osx-x64",
    "osx-arm64"
)

foreach ($rid in $Rids) {
    $out = Join-Path $PublishDir $rid
    Write-Host "Publishing $rid -> $out" -ForegroundColor Cyan
    dotnet publish $ProjectDir\NextGenSoftware.OASIS.STAR.CLI.csproj `
        -c Release `
        -r $rid `
        -p:PublishSingleFile=true `
        -p:SelfContained=true `
        -o $out
    if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
}

Write-Host "Done. Output: $PublishDir" -ForegroundColor Green
Write-Host "  Windows: star.exe in win-x64 / win-arm64"
Write-Host "  Linux:   star in linux-x64 / linux-arm64"
Write-Host "  macOS:   star in osx-x64 / osx-arm64"
