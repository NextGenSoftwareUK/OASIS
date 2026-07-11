# Build Windows installer for STAR CLI (Inno Setup).
# Prerequisites:
#   1. .NET 8 SDK
#   2. Inno Setup 6 (https://jrsoftware.org/isinfo.php) - optional; if not installed, only publish step runs.
# Run from: STAR ODK\NextGenSoftware.OASIS.STAR.CLI (or repo root).

$ErrorActionPreference = "Stop"
$ScriptDir = if ($PSScriptRoot) { $PSScriptRoot } else { "." }
$ProjectDir = Resolve-Path (Join-Path $ScriptDir "..\..")
$PublishDir = Join-Path $ProjectDir "publish"
$WinPublishDir = Join-Path $PublishDir "win-x64"
$InstallerOutputDir = Join-Path $PublishDir "installers"

# 1. Publish win-x64 single-file executable
Write-Host "Publishing STAR CLI for win-x64..." -ForegroundColor Cyan
Push-Location $ProjectDir
try {
    dotnet publish (Join-Path $ProjectDir "NextGenSoftware.OASIS.STAR.CLI.csproj") `
        -c Release `
        -r win-x64 `
        -p:PublishSingleFile=true `
        -p:SelfContained=true `
        -o $WinPublishDir
    if ($LASTEXITCODE -ne 0) { throw "dotnet publish failed" }
} finally {
    Pop-Location
}

if (-not (Test-Path (Join-Path $WinPublishDir "star.exe"))) {
    throw "Publish did not produce star.exe in $WinPublishDir"
}

# 2. Compile Inno Setup script if ISCC is available
$iscc = $null
$paths = @(
    "${env:ProgramFiles(x86)}\Inno Setup 6\ISCC.exe",
    "${env:ProgramFiles}\Inno Setup 6\ISCC.exe",
    "${env:ProgramFiles(x86)}\Inno Setup 5\ISCC.exe"
)
foreach ($p in $paths) {
    if (Test-Path $p) { $iscc = $p; break }
}

if ($iscc) {
    Write-Host "Building installer with Inno Setup..." -ForegroundColor Cyan
    New-Item -ItemType Directory -Force -Path $InstallerOutputDir | Out-Null
    & $iscc (Join-Path $ScriptDir "star-cli.iss")
    if ($LASTEXITCODE -ne 0) { throw "Inno Setup compilation failed" }
    Write-Host "Installer created in: $InstallerOutputDir" -ForegroundColor Green
} else {
    Write-Host "Inno Setup not found. Install from https://jrsoftware.org/isinfo.php and re-run, or use: $WinPublishDir\star.exe" -ForegroundColor Yellow
}

Write-Host "Done." -ForegroundColor Green
